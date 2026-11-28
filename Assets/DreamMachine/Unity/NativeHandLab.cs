using System;
using System.Diagnostics;
using DreamMachine.Body;
using DreamMachine.Sim;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamMachine.UnityBridge
{
    public enum GripPhase { Settle, Approach, Close, Lift, Hold, Lower, Release, Complete, Failed, Reposition, Transport }

    public enum GraspExploration { Off, ForceOnly, Tactile }

    public enum HandObjectShape { Sphere, Box, Capsule }

    public readonly struct HandObjectSetup
    {
        public readonly HandObjectShape Shape;
        public readonly Vector3 Size, CentreOfMass;
        public HandObjectSetup(HandObjectShape shape, Vector3 size, Vector3 centreOfMass = default)
        {
            Shape = shape;
            Size = size;
            CentreOfMass = centreOfMass;
        }
    }

    public readonly struct GripPadSample
    {
        public readonly float NormalImpulse, TangentImpulse, ContactSeconds, RelativeSpeed;
        public readonly bool Slip;
        public readonly Vector3 Point;
        public float NormalForce => GripCalibration.MeanForce(NormalImpulse, WorldSimulation.Dt);
        public float TangentForce => GripCalibration.MeanForce(TangentImpulse, WorldSimulation.Dt);

        internal GripPadSample(float normal, float tangent, float contactSeconds, float speed, bool slip, Vector3 point)
        {
            NormalImpulse = normal; TangentImpulse = tangent; ContactSeconds = contactSeconds;
            RelativeSpeed = speed; Slip = slip; Point = point;
        }
    }

    public readonly struct GripFrame
    {
        public readonly int Tick, HeldTicks;
        public readonly GripPhase Phase;
        public readonly Vector3 Palm, Object, ObjectVelocity, DriveForce;
        public readonly Quaternion WristRotation, ObjectRotation;
        public readonly float LeftTravel, RightTravel, LeftClosingForce, RightClosingForce, TravelTarget, Clearance,
            SupportForce, Work, Penetration;
        public readonly GripPadSample Left, Right;

        internal GripFrame(NativeHandLab lab)
        {
            Tick = lab.Tick; HeldTicks = lab.HeldTicks; Phase = lab.Phase;
            Palm = lab.PalmPosition; Object = lab.ObjectPosition; ObjectVelocity = lab.ObjectVelocity;
            DriveForce = lab.DriveForce; WristRotation = lab.WristRotation; ObjectRotation = lab.ObjectRotation;
            LeftTravel = lab.FingerTravel(0); RightTravel = lab.FingerTravel(1);
            LeftClosingForce = lab.ClosingForce(0); RightClosingForce = lab.ClosingForce(1); TravelTarget = lab.TravelTarget;
            Clearance = lab.Clearance; SupportForce = lab.SupportForce; Work = lab.Work;
            Penetration = lab.Penetration; Left = lab.ReadPad(0); Right = lab.ReadPad(1);
        }
    }

    // Reduced supported opposed-digit engineering fixture. PhysX owns the freely moving object.
    public sealed class NativeHandLab : IDisposable
    {
        public const float PhysicsDt = .005f;
        public const float PlacementX = -.060f, PlacementTolerance = .024f, PlacementHalfDepth = .039f;
        const int Substeps = 10;
        const float PickupY = .029f, StartZ = -.145f, ReachZ = -.065f, LiftY = .091f;
        const float OpenTravel = 0f, ClosedTravel = .033f, GuideLimit = .037f;
        readonly Scene _scene;
        readonly PhysicsScene _physics;
        readonly PhysicsMaterial _padMaterial, _objectMaterial;
        readonly Rigidbody _carriage, _palm, _object;
        readonly Rigidbody[] _fingers = new Rigidbody[2];
        readonly Vector3[] _guideOrigin = new Vector3[2];
        readonly Collider[] _pads = new Collider[2], _shafts = new Collider[2];
        readonly Collider _table, _palmCollider;
        readonly Collider _objectCollider;
        readonly bool _researchObject;
        readonly ContactGatedGrip _feedback;
        readonly TactileGrasp _exploration;
        readonly bool _placement;
        float _settleSupport, _measuredWeight;
        int _settleSamples;
        readonly PadAccumulator[] _reading = new PadAccumulator[2];
        readonly GripPadSample[] _last = new GripPadSample[2];
        readonly float[] _closingForce = new float[2];
        readonly int _closeTicks;
        float _verticalTarget = PickupY;
        float _lateralTarget, _forwardOffset;
        Vector3 _supportMoment, _releaseOrigin;
        int _phaseTick, _releaseTicks, _otherContacts;
        float _supportImpulse;
        bool _held, _disposed, _emergencyLower;
        public const string FixtureId = "engineered.h1.opposed-digits";
        public int Seed { get; }
        public float Radius { get; }
        public float MassKg => _object.mass;
        public float Friction { get; }
        public float GripCap { get; }
        public int Tick { get; private set; }
        public GripPhase Phase { get; private set; }
        public string Outcome { get; private set; } = "in-progress";
        public int HeldTicks { get; private set; }
        public int SlipTicks { get; private set; }
        public float PeakClearance { get; private set; }
        public float PeakPadForce { get; private set; }
        public float Work { get; private set; }
        public float WristWork { get; private set; }
        public float DigitEffort { get; private set; }
        public float PeakPenetration { get; private set; }
        public float MaxJointTravel { get; private set; }
        public long CommandCpuTicks { get; private set; }
        public long SensorCpuTicks { get; private set; }
        public bool Profile { get; set; }
        public Vector3 PalmPosition => _palm.position;
        public Quaternion WristRotation => _palm.rotation;
        public Vector3 ObjectPosition => _object.position;
        public Vector3 ObjectVelocity => _object.linearVelocity;
        public Vector3 ObjectAngularVelocity => _object.angularVelocity;
        public Quaternion ObjectRotation => _object.rotation;
        public float Clearance => _researchObject ? _objectCollider.bounds.min.y : _object.position.y - Radius;
        public float EffectiveGripCap => _exploration?.Cap ?? _feedback?.Cap ?? GripCap;
        public int GripCorrections => _exploration?.Corrections ?? _feedback?.Corrections ?? 0;
        public bool GripUncertain => _exploration?.Uncertain ?? _feedback?.Uncertain == true;
        public int Repositions => _exploration?.Repositions ?? 0;
        public float PitchTarget => _exploration?.Pitch ?? 0f;
        public Vector3 GraspOffset => new Vector3(_exploration?.Lateral ?? 0f, _exploration?.Height ?? 0f, _exploration?.Forward ?? 0f);
        public bool ControlledPlacement { get; private set; }
        public int EarlyLowers { get; private set; }
        public Vector3 SupportPoint => _supportImpulse > 0f ? _supportMoment / _supportImpulse : Vector3.zero;
        public float SupportX => SupportPoint.x;
        public int OtherContacts => _otherContacts;
        public float SupportForce => GripCalibration.MeanForce(_supportImpulse, WorldSimulation.Dt);
        public float Penetration { get; private set; }
        public Vector3 DriveForce { get; private set; }
        public bool OpposingContact => _last[0].NormalForce >= GripCalibration.ContactForce &&
            _last[1].NormalForce >= GripCalibration.ContactForce;
        public bool FreeObject => !_object.isKinematic && _object.useGravity && _object.constraints == RigidbodyConstraints.None &&
            _object.transform.parent == null && _object.GetComponents<Joint>().Length == 0;
        public bool Settled => Terminal && _releaseTicks >= 5;
        public bool Terminal => Phase == GripPhase.Complete || Phase == GripPhase.Failed;
        public float TravelTarget { get; private set; }

        public NativeHandLab(int seed, float mass, float radius, float friction, float gripCap, int closeTicks = 12,
            float orientation = 0f, HandObjectSetup? objectSetup = null, bool contactGated = false,
            bool placement = false, GraspExploration exploration = GraspExploration.Off, Vector2 objectOffset = default)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Native contact callbacks require Play Mode.");
            if (!(mass >= .035f && mass <= .20f && radius >= .021f && radius <= .036f && friction >= .10f && friction <= 1f &&
                gripCap >= .08f && gripCap <= 7f && closeTicks >= 4 && closeTicks <= 30))
                throw new ArgumentOutOfRangeException(nameof(mass), "Unsupported engineering fixture settings.");
            if (!float.IsFinite(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
            if (!(objectOffset.sqrMagnitude <= .0025f) || (objectOffset != Vector2.zero && !objectSetup.HasValue))
                throw new ArgumentOutOfRangeException(nameof(objectOffset));
            if (!Enum.IsDefined(typeof(GraspExploration), exploration) ||
                exploration != GraspExploration.Off && (!placement || contactGated))
                throw new ArgumentOutOfRangeException(nameof(exploration));
            _feedback = contactGated ? new ContactGatedGrip(gripCap) : null;
            _exploration = exploration == GraspExploration.Off ? null : new TactileGrasp(gripCap, exploration == GraspExploration.Tactile);
            _placement = placement;
            _researchObject = objectSetup.HasValue;
            if (objectSetup.HasValue)
            {
                var setup = objectSetup.Value;
                if (!Enum.IsDefined(typeof(HandObjectShape), setup.Shape) ||
                    !(setup.Size.x >= .040f && setup.Size.x <= .078f && setup.Size.y >= .040f && setup.Size.y <= .078f &&
                        setup.Size.z >= .040f && setup.Size.z <= .078f) || !(setup.CentreOfMass.sqrMagnitude <= .000225f) ||
                    (setup.Shape != HandObjectShape.Box && (setup.Size.x != 2f * radius || setup.Size.z != 2f * radius)) ||
                    (setup.Shape == HandObjectShape.Sphere && setup.Size.y != 2f * radius) ||
                    (setup.Shape == HandObjectShape.Capsule && setup.Size.y < 2f * radius))
                    throw new ArgumentOutOfRangeException(nameof(objectSetup));
            }
            Seed = seed; Radius = radius; Friction = friction; GripCap = gripCap; _closeTicks = closeTicks;
            _scene = SceneManager.CreateScene("Supported hand " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physics = _scene.GetPhysicsScene();
            _padMaterial = Material("Pad", friction);
            _objectMaterial = Material("Object", friction);
            _table = Box(New("Table", new Vector3(0, -.025f, .025f)), new Vector3(.40f, .05f, .50f), Vector3.zero, _objectMaterial);
            _carriage = Body("Supported wrist carriage", new Vector3(0, PickupY, StartZ), .85f);
            var rail = _carriage.gameObject.AddComponent<ConfigurableJoint>();
            rail.autoConfigureConnectedAnchor = false;
            rail.connectedAnchor = _carriage.position;
            rail.angularXMotion = rail.angularYMotion = rail.angularZMotion = ConfigurableJointMotion.Locked;
            rail.xMotion = placement ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
            rail.yMotion = rail.zMotion = ConfigurableJointMotion.Limited;
            rail.linearLimit = new SoftJointLimit { limit = .155f, contactDistance = .001f };
            _palm = Body("Palm and wrist", _carriage.position, .12f);
            _palmCollider = Box(_palm.gameObject, new Vector3(.097f, .026f, .024f), Vector3.zero, _padMaterial);
            Hinge(_palm, _carriage, _palm.position, Vector3.right, -11f, 11f);
            float objectHeight = objectSetup?.Size.y ?? 2f * radius;
            _object = Body("Free grasp object", new Vector3(objectOffset.x, objectHeight * .5f + .001f, objectOffset.y), mass);
            _object.rotation = Quaternion.Euler(0f, orientation, 0f);
            _objectCollider = MakeObjectCollider(objectSetup, radius);
            _objectCollider.contactOffset = .001f;
            _objectCollider.providesContacts = true;
            _objectCollider.sharedMaterial = _objectMaterial;
            _object.ResetInertiaTensor();
            if (objectSetup.HasValue) _object.centerOfMass = objectSetup.Value.CentreOfMass;
            _object.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var hand = new HumanoidAnatomy(seed).AtAge(3f).Hand;
            float separation = Mathf.Clamp(hand * .44f, .048f, .054f);
            for (int i = 0; i < 2; i++)
            {
                float side = 2 * i - 1;
                Vector3 anchor = _palm.position + new Vector3(separation * side, 0f, .012f);
                var finger = _fingers[i] = Body(i == 0 ? "Opposing thumb" : "Index finger", anchor + Vector3.forward * .030f, .045f);
                _shafts[i] = Box(finger.gameObject, new Vector3(.012f, .019f, .048f), Vector3.zero, _padMaterial);
                _pads[i] = Box(finger.gameObject, new Vector3(.016f, GripCalibration.PadHeight, GripCalibration.PadLength),
                    new Vector3(0, 0, .027f), _padMaterial);
                _guideOrigin[i] = Quaternion.Inverse(_palm.rotation) * (finger.position - _palm.position);
                finger.ResetInertiaTensor();
                GuideFinger(finger, _palm, _guideOrigin[i]);
                BodySegment segment = i == 0 ? BodySegment.RightThumb : BodySegment.RightIndex;
                bool excluded = AnatomyContacts.ExcludePair(BodySegment.RightHand, segment);
                Physics.IgnoreCollision(_palmCollider, _pads[i], excluded);
                Physics.IgnoreCollision(_palmCollider, _shafts[i], excluded);
            }
            _palm.ResetInertiaTensor();
            Physics.ContactEvent += Contacts;
        }

        Collider MakeObjectCollider(HandObjectSetup? setup, float radius)
        {
            switch (setup?.Shape ?? HandObjectShape.Sphere)
            {
                case HandObjectShape.Box:
                    return Box(_object.gameObject, setup.Value.Size, Vector3.zero, _objectMaterial);
                case HandObjectShape.Capsule:
                    var capsule = _object.gameObject.AddComponent<CapsuleCollider>();
                    capsule.radius = radius;
                    capsule.height = setup.Value.Size.y;
                    return capsule;
                default:
                    var sphere = _object.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = radius;
                    return sphere;
            }
        }

        public GripFrame ReadFrame() => new GripFrame(this);
        public GripPadSample ReadPad(int side) => _last[side];
        public float ClosingForce(int side) => _closingForce[side];
        public float FingerTravel(int side) => Vector3.Dot(_fingers[side].position -
            (_palm.position + _palm.rotation * _guideOrigin[side]), Axis(side));
        public float FingerVelocity(int side)
        {
            var finger = _fingers[side];
            return Vector3.Dot(finger.linearVelocity - _palm.GetPointVelocity(finger.position), Axis(side));
        }

        Vector3 Axis(int side) => _palm.rotation * ((1 - 2 * side) * Vector3.right);

        public (Vector3 Position, Quaternion Rotation, Vector3 Size) ReadPart(int part)
        {
            Collider geometry = part switch { 0 => _table, 1 => _palmCollider, 2 => _shafts[0], 3 => _pads[0],
                4 => _shafts[1], 5 => _pads[1], 6 => _objectCollider, _ => throw new ArgumentOutOfRangeException(nameof(part)) };
            var t = geometry.transform;
            if (geometry is BoxCollider box) return (t.TransformPoint(box.center), t.rotation, box.size);
            var size = Vector3.one * (2f * Radius);
            if (geometry is CapsuleCollider capsule) size.y = capsule.height;
            return (t.position, t.rotation, size);
        }

        public void Step() => Step(Vector3.zero, Vector3.zero);

        public void Step(Vector3 researchLoad, Vector3 researchImpulse)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NativeHandLab));
            if (Terminal) return;
            if (!(researchLoad.sqrMagnitude <= 16f && researchImpulse.sqrMagnitude <= .0025f))
                throw new ArgumentOutOfRangeException(nameof(researchLoad));
            bool closing = Phase >= GripPhase.Close && Phase <= GripPhase.Lower || Phase == GripPhase.Transport;
            TravelTarget = closing ? ClosedTravel : OpenTravel;
            float z = Phase == GripPhase.Settle ? StartZ : ReachZ;
            float y = Phase == GripPhase.Lift || Phase == GripPhase.Hold || Phase == GripPhase.Transport ? LiftY : PickupY;
            float liftStep = Phase == GripPhase.Lift && GripUncertain ? .0025f : .007f;
            if (_placement)
            {
                var offset = GraspOffset;
                bool carrying = _held && (Phase == GripPhase.Transport || Phase == GripPhase.Lower || Phase == GripPhase.Release);
                bool opening = Phase == GripPhase.Reposition && Tick - _phaseTick < 3;
                if (!opening && !_emergencyLower)
                {
                    _lateralTarget = Mathf.MoveTowards(_lateralTarget, carrying ? PlacementX : offset.x, .004f);
                    _forwardOffset = Mathf.MoveTowards(_forwardOffset, offset.z, .003f);
                }
                z += _forwardOffset;
                y += offset.y;
                if (_held && Phase == GripPhase.Lower) y = PickupY - .005f;
                if (_held && Phase == GripPhase.Release) y = _verticalTarget;
                if (_exploration != null && GripUncertain && (Phase == GripPhase.Lift || Phase == GripPhase.Transport))
                {
                    y = _verticalTarget;
                    if (Phase == GripPhase.Transport) _lateralTarget = _carriage.position.x;
                }
                if (opening)
                {
                    // Open and back away before moving the pads around a supported object.
                    y = _verticalTarget;
                    z -= .012f;
                }
            }
            _verticalTarget = Mathf.MoveTowards(_verticalTarget, y, liftStep);
            var target = new Vector3(_lateralTarget, _verticalTarget, z);
            Array.Clear(_reading, 0, _reading.Length);
            _supportImpulse = 0f; _supportMoment = Vector3.zero;
            _otherContacts = 0;
            float cap = closing ? EffectiveGripCap : 3f;
            if (researchImpulse != Vector3.zero) _object.AddForce(researchImpulse, ForceMode.Impulse);
            for (int i = 0; i < Substeps; i++)
            {
                _reading[0].BeginSubstep(); _reading[1].BeginSubstep();
                long start = Profile ? Stopwatch.GetTimestamp() : 0;
                float left = FingerTravel(0), right = FingerTravel(1);
                Vector3 before = _carriage.position;
                DriveForce = Vector3.ClampMagnitude(240f * (target - before) - 27f * _carriage.linearVelocity +
                    Vector3.up * (6.7f * -Physics.gravity.y / 6f), 22f);
                _carriage.AddForce(DriveForce);
                Vector3 axis = _carriage.rotation * Vector3.right;
                float wrist = Vector3.SignedAngle(_carriage.rotation * Vector3.forward, _palm.rotation * Vector3.forward, axis);
                float wristSpeed = Vector3.Dot(_palm.angularVelocity - _carriage.angularVelocity, axis);
                float torque = Mathf.Clamp(.24f * (PitchTarget - wrist) - .08f * wristSpeed, -.9f, .9f);
                TorquePair(_palm, _carriage, axis * torque);
                DriveFinger(0, left, cap);
                DriveFinger(1, right, cap);
                if (_researchObject) DigitEffort += (Mathf.Abs(_closingForce[0]) + Mathf.Abs(_closingForce[1])) * PhysicsDt;
                if (researchLoad != Vector3.zero) _object.AddForce(researchLoad);
                if (Profile) CommandCpuTicks += Stopwatch.GetTimestamp() - start;
                _physics.Simulate(PhysicsDt);
                if (_placement)
                {
                    float turn = Vector3.SignedAngle(_carriage.rotation * Vector3.forward, _palm.rotation * Vector3.forward, axis);
                    WristWork += Mathf.Abs(torque * (turn - wrist) * Mathf.Deg2Rad);
                }
                Work += Mathf.Abs(Vector3.Dot(DriveForce, _carriage.position - before)) +
                    Mathf.Abs(_closingForce[0] * (FingerTravel(0) - left)) + Mathf.Abs(_closingForce[1] * (FingerTravel(1) - right));
            }

            for (int i = 0; i < 2; i++) _last[i] = _reading[i].Finish();
            PeakPadForce = Mathf.Max(PeakPadForce, _last[0].NormalForce, _last[1].NormalForce);
            MaxJointTravel = Mathf.Max(MaxJointTravel, Mathf.Abs(FingerTravel(0)), Mathf.Abs(FingerTravel(1)));
            Penetration = Mathf.Max(Overlap(_pads[0]), Overlap(_pads[1]), Overlap(_table), Overlap(_palmCollider),
                Overlap(_shafts[0]), Overlap(_shafts[1]));
            PeakPenetration = Mathf.Max(PeakPenetration, Penetration);
            PeakClearance = Mathf.Max(PeakClearance, Clearance);
            Tick++;
            bool suspended = Phase == GripPhase.Lift || Phase == GripPhase.Hold || Phase == GripPhase.Transport;
            if (suspended && (_last[0].Slip || _last[1].Slip)) SlipTicks++;
            if (_researchObject && Phase == GripPhase.Settle && Tick >= 12)
            {
                _settleSupport += SupportForce;
                _measuredWeight = _settleSupport / ++_settleSamples;
            }
            if (_feedback != null && (Phase == GripPhase.Close || Phase == GripPhase.Lift || Phase == GripPhase.Hold))
                _feedback.Observe(Sense(_last[0]), Sense(_last[1]), Phase != GripPhase.Close);
            if (_exploration != null && (Phase == GripPhase.Close || suspended))
                _exploration.Observe(Patch(0), Patch(1), suspended);
            Advance(Tick - _phaseTick);
        }

        static GripContactSense Sense(GripPadSample sample) =>
            new GripContactSense(sample.NormalForce, sample.ContactSeconds, sample.RelativeSpeed, sample.Slip);

        GraspPatch Patch(int side)
        {
            var sample = _last[side];
            var point = _pads[side].transform.InverseTransformPoint(sample.Point) - ((BoxCollider)_pads[side]).center;
            return new GraspPatch(Sense(sample), point.z, point.y);
        }

        float WeightReference => _researchObject ? Mathf.Max(.10f, _measuredWeight) : MassKg * -Physics.gravity.y;
        bool AtDestination => Mathf.Abs(SupportPoint.x - PlacementX) <= PlacementTolerance &&
            Mathf.Abs(SupportPoint.z) <= PlacementHalfDepth;

        void Advance(int elapsed)
        {
            switch (Phase)
            {
                case GripPhase.Settle:
                    if (elapsed >= 18) Enter(GripPhase.Approach);
                    break;
                case GripPhase.Approach:
                    if (Mathf.Abs(_carriage.position.z - ReachZ) < .004f || elapsed >= 36) Enter(GripPhase.Close);
                    break;
                case GripPhase.Close:
                    if (_exploration != null)
                    {
                        if (elapsed >= _closeTicks && _exploration.Ready)
                        {
                            var left = Patch(0);
                            var right = Patch(1);
                            Enter(_exploration.PoorPlacement(left, right) && _exploration.Reposition(left, right)
                                ? GripPhase.Reposition : GripPhase.Lift);
                        }
                        else if (elapsed >= _closeTicks + 6)
                            Enter(_exploration.Reposition(Patch(0), Patch(1)) ? GripPhase.Reposition : GripPhase.Lower);
                    }
                    else if (_feedback == null)
                    {
                        if ((elapsed >= _closeTicks && OpposingContact) || elapsed >= _closeTicks + 8) Enter(GripPhase.Lift);
                    }
                    else if (elapsed >= _closeTicks && _feedback.Ready) Enter(GripPhase.Lift);
                    else if (elapsed >= _closeTicks + 8) Enter(GripPhase.Lower);

                    break;
                case GripPhase.Reposition:
                    if (elapsed >= 10) Enter(GripPhase.Close);
                    break;
                case GripPhase.Lift:
                    if (Clearance >= GripCalibration.LiftClearance && OpposingContact && _otherContacts == 0)
                        Enter(GripPhase.Hold);
                    else if (_exploration?.Lower == true) EmergencyLower();
                    else if (elapsed >= 28) Enter(GripPhase.Lower);
                    break;
                case GripPhase.Hold:
                    HeldTicks = Clearance >= GripCalibration.LiftClearance && OpposingContact && _otherContacts == 0
                        ? HeldTicks + 1 : 0;
                    if (HeldTicks >= GripCalibration.HoldTicks) { _held = true; Enter(_placement ? GripPhase.Transport : GripPhase.Lower); }
                    else if (_exploration?.Lower == true) EmergencyLower();
                    else if (elapsed >= 30) Enter(GripPhase.Lower);
                    break;
                case GripPhase.Transport:
                    if (_exploration?.Lower == true) EmergencyLower();
                    else if ((Mathf.Abs(_carriage.position.x - PlacementX) < .006f &&
                        Mathf.Abs(_carriage.linearVelocity.x) < .035f) || elapsed >= 28) Enter(GripPhase.Lower);
                    break;
                case GripPhase.Lower:
                    if (_placement && _held)
                    {
                        ControlledPlacement = OpposingContact && SupportForce >= .45f * WeightReference &&
                            AtDestination && Mathf.Abs(_object.linearVelocity.y) < .06f;
                        if (!ControlledPlacement && elapsed < 38) break;
                        _releaseOrigin = _object.position;
                        Enter(GripPhase.Release);
                    }
                    else if (elapsed >= 10 && SupportForce >= .2f * WeightReference || elapsed >= 34)
                        Enter(GripPhase.Release);
                    break;
                case GripPhase.Release:
                    bool released = !OpposingContact && _last[0].NormalForce < GripCalibration.ContactForce &&
                        _last[1].NormalForce < GripCalibration.ContactForce && SupportForce >= .5f * WeightReference &&
                        _object.linearVelocity.sqrMagnitude < .0025f && Clearance < .004f;
                    if (_placement && _held)
                        released &= AtDestination && new Vector2(_object.position.x - _releaseOrigin.x,
                            _object.position.z - _releaseOrigin.z).sqrMagnitude < .000324f;
                    _releaseTicks = released ? _releaseTicks + 1 : 0;
                    if (_releaseTicks >= 5 || elapsed >= 30)
                    {
                        Outcome = !_held ? "hold-gate-failed" : _placement && !ControlledPlacement ? "placement-gate-failed" :
                            _releaseTicks < 5 ? "release-gate-failed" :
                            PeakPenetration > GripCalibration.MaximumPenetration ? "penetration-gate-failed" :
                            MaxJointTravel > GuideLimit + .002f ? "joint-gate-failed" : "physical-lift-hold-release";
                        if (Outcome == "physical-lift-hold-release")
                        {
                            if (_placement) Outcome = "physical-lift-hold-place-release";
                            Enter(GripPhase.Complete);
                        }
                        else Enter(GripPhase.Failed);
                    }
                    break;
            }
        }

        void EmergencyLower()
        {
            _emergencyLower = true;
            EarlyLowers++;
            Enter(GripPhase.Lower);
        }

        void Enter(GripPhase phase) { Phase = phase; _phaseTick = Tick; }

        void DriveFinger(int side, float travel, float cap)
        {
            var finger = _fingers[side];
            Vector3 axis = Axis(side);
            float speed = FingerVelocity(side);
            const float stiffness = 480f, damping = 8f;
            float effectiveDamping = damping + stiffness * PhysicsDt;
            float request = (stiffness * (TravelTarget - travel) - effectiveDamping * speed) /
                (1f + effectiveDamping * PhysicsDt / finger.mass);
            _closingForce[side] = Mathf.Clamp(request, -cap, cap);
            Vector3 force = axis * _closingForce[side];
            finger.AddForce(force);
            _palm.AddForceAtPosition(-force, finger.position);
        }

        static void GuideFinger(Rigidbody digit, Rigidbody palm, Vector3 origin)
        {
            var joint = digit.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = palm;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = origin;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = joint.zMotion = joint.angularXMotion = joint.angularYMotion =
                joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.linearLimit = new SoftJointLimit { limit = GuideLimit, contactDistance = .001f };
        }

        static void TorquePair(Rigidbody child, Rigidbody parent, Vector3 torque)
        {
            child.AddTorque(torque);
            parent.AddTorque(-torque);
        }

        void Contacts(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headers)
        {
            if (scene != _physics) return;
            long start = Profile ? Stopwatch.GetTimestamp() : 0;
            int target = _objectCollider.GetInstanceID();
            foreach (var header in headers)
            for (int index = 0; index < header.pairCount; index++)
            {
                var pair = header.GetContactPair(index);
                int other = pair.colliderInstanceID == target ? pair.otherColliderInstanceID :
                    pair.otherColliderInstanceID == target ? pair.colliderInstanceID : 0;
                if (other == 0) continue;
                int side = other == _pads[0].GetInstanceID() ? 0 : other == _pads[1].GetInstanceID() ? 1 : -1;
                for (int j = 0; j < pair.contactCount; j++)
                {
                    var cp = pair.GetContactPoint(j);
                    float normal = Mathf.Abs(Vector3.Dot(cp.impulse, cp.normal));
                    if (normal <= .000002f) continue;
                    if (side >= 0)
                    {
                        Vector3 velocity = _object.GetPointVelocity(cp.position) - _fingers[side].GetPointVelocity(cp.position);
                        _reading[side].Add(normal, Vector3.ProjectOnPlane(cp.impulse, cp.normal).magnitude,
                            Vector3.ProjectOnPlane(velocity, cp.normal).magnitude, cp.position);
                    }
                    else if (other == _table.GetInstanceID())
                    {
                        _supportImpulse += normal;
                        if (_placement) _supportMoment += cp.position * normal;
                    }
                    else _otherContacts++;
                }
            }
            if (Profile) SensorCpuTicks += Stopwatch.GetTimestamp() - start;
        }

        float Overlap(Collider other) => Physics.ComputePenetration(_objectCollider, _object.position, _object.rotation,
            other, other.transform.position, other.transform.rotation, out _, out float depth) ? depth : 0f;

        static PhysicsMaterial Material(string name, float friction) => new PhysicsMaterial(name)
        {
            staticFriction = friction, dynamicFriction = .9f * friction, frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f
        };

        GameObject New(string name, Vector3 at)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, _scene);
            obj.transform.position = at;
            return obj;
        }

        Rigidbody Body(string name, Vector3 position, float mass)
        {
            var body = New(name, position).AddComponent<Rigidbody>();
            body.mass = mass;
            body.sleepThreshold = 0f;
            body.linearDamping = .06f;
            body.angularDamping = .22f;
            body.solverIterations = 32;
            body.solverVelocityIterations = 12;
            return body;
        }

        static BoxCollider Box(GameObject owner, Vector3 size, Vector3 center, PhysicsMaterial material)
        {
            var box = owner.AddComponent<BoxCollider>();
            box.size = size; box.center = center; box.contactOffset = .001f; box.sharedMaterial = material;
            return box;
        }

        static void Hinge(Rigidbody body, Rigidbody parent, Vector3 anchor, Vector3 axis, float min, float max)
        {
            var joint = body.gameObject.AddComponent<HingeJoint>();
            joint.connectedBody = parent;
            joint.autoConfigureConnectedAnchor = false;
            joint.axis = axis;
            joint.anchor = body.transform.InverseTransformPoint(anchor);
            joint.connectedAnchor = parent.transform.InverseTransformPoint(anchor);
            joint.limits = new JointLimits { min = min, max = max, contactDistance = 1f };
            joint.useLimits = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Physics.ContactEvent -= Contacts;
            if (_scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
            UnityEngine.Object.Destroy(_padMaterial);
            UnityEngine.Object.Destroy(_objectMaterial);
        }

        struct PadAccumulator
        {
            public float Normal, Tangent, Speed;
            public Vector3 Moment;
            int _contactSteps, _movingSteps;
            bool _contact, _moving;
            public void BeginSubstep() { _contact = false; _moving = false; }
            public void Add(float normal, float tangent, float speed, Vector3 point)
            {
                Normal += normal; Tangent += tangent; Moment += point * normal;
                Speed = Mathf.Max(speed, Speed);
                if (!_contact) { _contactSteps++; _contact = true; }
                if (!_moving && speed >= GripCalibration.SlipSpeed) { _movingSteps++; _moving = true; }
            }
            public GripPadSample Finish() => new GripPadSample(Normal, Tangent, _contactSteps * PhysicsDt, Speed,
                GripCalibration.Slipping(GripCalibration.MeanForce(Normal, WorldSimulation.Dt), Speed, _movingSteps),
                Normal > 0f ? Moment / Normal : Vector3.zero);
        }
    }
}
