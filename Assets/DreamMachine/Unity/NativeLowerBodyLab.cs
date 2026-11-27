using System;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamMachine.UnityBridge
{
    public sealed partial class NativeLowerBodyLab : IDisposable
    {
        public const int Substeps = 10;
        public const float Substep = WorldSimulation.Dt / Substeps;
        public const float TotalMassKg = 10f;
        const float Clearance = 0.002f;
        readonly Scene _scene;
        readonly PhysicsScene _physics;
        readonly PhysicsMaterial _material;
        readonly Rigidbody[] _bodies = new Rigidbody[8];
        readonly GroundReceiver[] _contacts = new GroundReceiver[8];
        readonly JointDrive[] _joints = new JointDrive[6];
        readonly BoxCollider[] _feet = new BoxCollider[2];
        readonly Vector3[] _initialFeet = new Vector3[2];
        readonly Rigidbody _pelvis, _ballast;
        readonly float _travel;
        bool _disposed;
        float _requestedLoad;
        public uint DisabledDrives { get; set; }
        public int Seed { get; }
        public float Stage { get; }
        public float Friction { get; }
        public HumanoidDimensions Dimensions { get; }
        public int Tick { get; private set; }
        public float RequestedLoad => _requestedLoad;
        public float BallastOffset => Vector3.Dot(_ballast.position - _pelvis.position, _pelvis.rotation * Vector3.right);
        public float NominalPelvisHeight => GroundedGait.PelvisHeight(Dimensions, false);
        public Vector3 PelvisPosition => _pelvis.position;
        public Vector3 PelvisVelocity => _pelvis.linearVelocity;
        public float PelvisTilt => Vector3.Angle(_pelvis.rotation * Vector3.up, Vector3.up);
        public Vector3 CenterOfMass { get; private set; }
        public Vector3 Momentum { get; private set; }
        public Vector3 MomentumDelta { get; private set; }
        public Vector3 ExternalImpulse { get; private set; }
        public Vector3 MomentumResidual { get; private set; }
        public uint FootContactMask { get; private set; }
        public float MaximumPenetration { get; private set; }
        public float FootDrift { get; private set; }
        public float JointLimitOvershoot { get; private set; }
        public float AbsoluteWork { get; private set; }
        public float SignedWork { get; private set; }
        public bool Saturated { get; private set; }
        public int BodyCount => _bodies.Length;
        public int JointCount => _joints.Length;

        public NativeLowerBodyLab(int seed, float stage, float friction = 0.85f,
            LowerBodyController controller = LowerBodyController.Sagittal, float vestibularNoise = 0f,
            LowerBodyStepProtocol stepProtocol = LowerBodyStepProtocol.Legacy, int physicsSubsteps = Substeps)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Native contact callbacks require Play Mode.");
            if (!(stage >= 0f && stage <= HumanoidAnatomy.AdultAgeDays && friction >= 0f && friction <= 1f))
                throw new ArgumentOutOfRangeException(nameof(stage));
            Seed = seed;
            Stage = stage;
            Friction = friction;
            Controller = controller;
            if (physicsSubsteps < 1 || physicsSubsteps > 40) throw new ArgumentOutOfRangeException(nameof(physicsSubsteps));
            PhysicsSubsteps = physicsSubsteps;
            StepProtocol = stepProtocol;
            _vestibularNoise = Mathf.Clamp01(vestibularNoise);
            Dimensions = new HumanoidAnatomy(seed).AtAge(stage);
            _travel = Dimensions.HipWidth * 0.78f;
            _scene = SceneManager.CreateScene("Lower-body PhysX " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physics = _scene.GetPhysicsScene();
            _material = new PhysicsMaterial("Lower-body contact")
            {
                staticFriction = friction, dynamicFriction = friction * 0.8f,
                frictionCombine = PhysicsMaterialCombine.Average, bounciness = 0f
            };
            var ground = Make("Ground", new Vector3(0, -0.1f, 0), Quaternion.identity);
            var groundBox = ground.AddComponent<BoxCollider>();
            groundBox.size = new Vector3(8, 0.2f, 8);
            groundBox.sharedMaterial = _material;
            groundBox.contactOffset = 0.001f;
            _groundId = groundBox.GetInstanceID();

            float height = NominalPelvisHeight + Clearance;
            _pelvis = MakeBody(0, "Pelvis", Vector3.up * height, Quaternion.identity,
                new Vector3(Dimensions.HipWidth * 1.38f, Dimensions.Foot * 0.50f, Dimensions.Foot * 0.85f), 2.5f, groundBox);
            _ballast = MakeBody(1, "Sliding ballast", _pelvis.position + Vector3.up * (Dimensions.Foot * 0.53f),
                Quaternion.identity, Vector3.one * (Dimensions.Foot * 0.33f), 3f, groundBox);
            var slider = _ballast.gameObject.AddComponent<ConfigurableJoint>();
            slider.connectedBody = _pelvis;
            slider.axis = Vector3.right;
            slider.secondaryAxis = Vector3.up;
            slider.xMotion = ConfigurableJointMotion.Limited;
            slider.yMotion = slider.zMotion = slider.angularXMotion = slider.angularYMotion =
                slider.angularZMotion = ConfigurableJointMotion.Locked;
            slider.linearLimit = new SoftJointLimit { limit = _travel, contactDistance = 0.001f };

            BuildLeg(0, groundBox);
            BuildLeg(1, groundBox);
            // Segment self-collision is deliberately omitted; ground collision remains enabled on every body.
            for (int i = 0; i < _bodies.Length; i++)
            for (int j = i + 1; j < _bodies.Length; j++)
                Physics.IgnoreCollision(_bodies[i].GetComponent<BoxCollider>(), _bodies[j].GetComponent<BoxCollider>());
            SampleMass();
            CaptureSensors(Vector3.zero);
            CaptureDynamics();
            if (Controller != LowerBodyController.Sagittal) Physics.ContactEvent += RecordPressure;
        }

        void BuildLeg(int side, Collider ground)
        {
            int body = 2 + side * 3, joint = side * 3;
            string prefix = side == 0 ? "Left " : "Right ";
            var rest = GroundedGait.Sample(Dimensions, 0, false, side == 0);
            Quaternion thighRot = Quaternion.AngleAxis(rest.Hip * Mathf.Rad2Deg, Vector3.right);
            Quaternion shinRot = Quaternion.AngleAxis((rest.Hip + rest.Knee) * Mathf.Rad2Deg, Vector3.right);
            float lateral = (side == 0 ? -0.5f : 0.5f) * Dimensions.HipWidth;
            Vector3 hip = _pelvis.position + Vector3.right * lateral;
            Vector3 knee = hip + thighRot * (Vector3.down * Dimensions.Thigh);
            Vector3 ankle = knee + shinRot * (Vector3.down * Dimensions.Shin);
            float width = Dimensions.Foot * 0.47f;
            var thigh = MakeBody(body, prefix + "thigh", (hip + knee) * 0.5f, thighRot,
                new Vector3(width * 0.82f, Dimensions.Thigh, width * 0.94f), 1.15f, ground);
            var shin = MakeBody(body + 1, prefix + "shin", (knee + ankle) * 0.5f, shinRot,
                new Vector3(width * 0.70f, Dimensions.Shin, width * 0.75f), 0.65f, ground);
            Vector3 centre = ankle + new Vector3(0, -Dimensions.AnkleHeight * 0.5f, Dimensions.Foot * 0.24f);
            var foot = MakeBody(body + 2, prefix + "foot", centre, Quaternion.identity,
                new Vector3(width, Dimensions.AnkleHeight, Dimensions.Foot), 0.45f, ground);
            _feet[side] = foot.GetComponent<BoxCollider>();
            _initialFeet[side] = centre;
            bool lateralDrive = Controller != LowerBodyController.Sagittal;
            _joints[joint] = new JointDrive(prefix + "hip", _pelvis, thigh, hip, rest.Hip, -0.75f, 0.65f, 27f, lateralDrive ? .30f : 0f, 16f);
            _joints[joint + 1] = new JointDrive(prefix + "knee", thigh, shin, knee, rest.Knee, 0.015f, 1.28f, 27f);
            _joints[joint + 2] = new JointDrive(prefix + "ankle", shin, foot, ankle, rest.Ankle, -0.55f, 0.55f, 21f, lateralDrive ? .27f : 0f, 12f);
        }

        GameObject Make(string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        Rigidbody MakeBody(int index, string name, Vector3 position, Quaternion rotation, Vector3 dimensions, float mass, Collider ground)
        {
            var go = Make(name, position, rotation);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = dimensions;
            box.sharedMaterial = _material;
            box.contactOffset = 0.001f;
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = true;
            body.linearDamping = body.angularDamping = 0f;
            body.sleepThreshold = 0f;
            body.centerOfMass = Vector3.zero;
            Vector3 q = Vector3.Scale(dimensions, dimensions);
            body.inertiaTensor = (mass / 12f) * new Vector3(q.y + q.z, q.x + q.z, q.x + q.y);
            body.inertiaTensorRotation = Quaternion.identity;
            body.solverIterations = 32;
            body.solverVelocityIterations = 12;
            body.maxAngularVelocity = 25f;
            _bodies[index] = body;
            _contacts[index] = go.AddComponent<GroundReceiver>();
            _contacts[index].Ground = ground;
            return body;
        }

        public void RequestLoad(float side)
        {
            if (float.IsNaN(side) || float.IsInfinity(side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (Action == SwingOutcome.Swing || Action == SwingOutcome.Recontact || PreparationRunning) return;
            float requested = Mathf.Clamp(side, -1f, 1f);
            if (_requestedLoad != requested) _stanceTicks = _preliftTicks = 0;
            _requestedLoad = requested;
        }

        public void Step(Vector3 disturbanceImpulse = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NativeLowerBodyLab));
            if (!(disturbanceImpulse.sqrMagnitude <= 16f))
                throw new ArgumentOutOfRangeException(nameof(disturbanceImpulse), "Research impulse capped at 4 N s.");
            Vector3 before = Momentum;
            Vector3 pelvisVelocity = PelvisVelocity;
            PrepareDynamicLoad();
            PlanStagedFeet();
            PrepareLateralTargets();
            Array.Clear(_pressure, 0, _pressure.Length);
            Array.Clear(_contactRegions, 0, _contactRegions.Length);
            Array.Clear(_footLoadedSteps, 0, _footLoadedSteps.Length);
            foreach (var c in _contacts) c.ResetTick();
            AbsoluteWork = SignedWork = 0;
            Saturated = false;
            PeakEffortFraction = 0f;
            ExternalImpulse = disturbanceImpulse;
            if (disturbanceImpulse != Vector3.zero) _pelvis.AddForce(disturbanceImpulse, ForceMode.Impulse);
            for (int substep = 0; substep < PhysicsSubsteps; substep++)
            {
                SampleMass();
                // Positive ankle torque on the foot applies the opposite torque to
                // the shin. Ground contact supplies the resulting external moment.
                float ankleZ = (_bodies[4].position.z + _bodies[7].position.z) * .5f - Dimensions.Foot * .24f;
                float offset = CenterOfMass.z - ankleZ;
                float balance = Mathf.Clamp(-Physics.gravity.y * TotalMassKg * .5f * offset +
                    60f * (offset - Dimensions.Foot * .08f) + 20f * Momentum.z / TotalMassKg, -12f, 12f);
                for (int joint = 0; joint < _joints.Length; joint++)
                {
                    int foot = joint == 2 ? 4 : joint == 5 ? 7 : -1;
                    bool enabled = (DisabledDrives & (1u << joint)) == 0;
                    _joints[joint].Apply(enabled, foot >= 0 && _contacts[foot].Impulse.y > 0f ? balance : 0f,
                        _rollTarget[joint], PitchTarget(joint), StanceAnkleReflex(joint), PhysicsStepSeconds);
                    Saturated |= _joints[joint].Saturated;
                    PeakEffortFraction = Mathf.Max(PeakEffortFraction, _joints[joint].EffortFraction);
                }
                ShiftBallast(PhysicsStepSeconds);
                float leftBefore = LeftImpulse.y, rightBefore = RightImpulse.y;
                _physics.Simulate(PhysicsStepSeconds);
                if (LeftImpulse.y - leftBefore > .0001f) _footLoadedSteps[0]++;
                if (RightImpulse.y - rightBefore > .0001f) _footLoadedSteps[1]++;
                foreach (var drive in _joints)
                {
                    float work = drive.Torque * (drive.Angle - drive.Previous);
                    SignedWork += work;
                    AbsoluteWork += Mathf.Abs(work);
                    if (drive.RollLimit > 0)
                    {
                        float rollWork = drive.RollTorque * (drive.RollAngle - drive.PreviousRoll);
                        SignedWork += rollWork;
                        AbsoluteWork += Mathf.Abs(rollWork);
                    }
                }
            }

            Tick++;
            SampleMass();
            MomentumDelta = Momentum - before;
            MomentumResidual = LeftImpulse + RightImpulse + OtherImpulse +
                (Physics.gravity * (TotalMassKg * WorldSimulation.Dt)) + ExternalImpulse - MomentumDelta;
            FootContactMask = (LeftImpulse.y > 0f ? 1u : 0u) | (RightImpulse.y > 0f ? 2u : 0u);
            FootDrift = MaximumPenetration = JointLimitOvershoot = 0;
            for (int i = 0; i < _feet.Length; i++)
            {
                var bounds = _feet[i].bounds;
                FootDrift = Mathf.Max(FootDrift, Vector3.ProjectOnPlane(bounds.center - _initialFeet[i], Vector3.up).magnitude);
                MaximumPenetration = Mathf.Max(MaximumPenetration, -bounds.min.y);
            }
            foreach (var drive in _joints)
            {
                JointLimitOvershoot = Mathf.Max(JointLimitOvershoot, Mathf.Max(drive.Min - drive.Angle, drive.Angle - drive.Max));
                if (drive.RollLimit > 0)
                    JointLimitOvershoot = Mathf.Max(JointLimitOvershoot, Mathf.Abs(drive.RollAngle) - drive.RollLimit);
            }
            CaptureSensors(pelvisVelocity);
            CaptureDynamics();
            UpdateSupportGate();
        }

        void ShiftBallast(float stepSeconds)
        {
            Vector3 axis = _pelvis.rotation * Vector3.right;
            Vector3 point = _ballast.worldCenterOfMass;
            float speed = Vector3.Dot(_ballast.linearVelocity - _pelvis.GetPointVelocity(point), axis);
            float force = Mathf.Clamp(115f * (_requestedLoad * _travel * 0.9f - BallastOffset) - 35f * speed, -25f, 25f);
            Vector3 applied = axis * force;
            _ballast.AddForce(applied);
            _pelvis.AddForceAtPosition(-applied, point);
            float work = force * speed * stepSeconds;
            SignedWork += work;
            AbsoluteWork += Mathf.Abs(work);
            Saturated |= Mathf.Abs(force) >= 25f;
            PeakEffortFraction = Mathf.Max(PeakEffortFraction, Mathf.Abs(force) / 25f);
        }

        void SampleMass()
        {
            Vector3 center = Vector3.zero, momentum = Vector3.zero;
            foreach (var body in _bodies)
            {
                center += body.worldCenterOfMass * body.mass;
                momentum += body.linearVelocity * body.mass;
            }
            CenterOfMass = center / TotalMassKg;
            Momentum = momentum;
        }

        public Vector3 LeftImpulse => _contacts[4].Impulse;
        public Vector3 RightImpulse => _contacts[7].Impulse;
        public Vector3 OtherImpulse => _contacts[0].Impulse + _contacts[1].Impulse + _contacts[2].Impulse +
            _contacts[3].Impulse + _contacts[5].Impulse + _contacts[6].Impulse;
        public Vector3 ContactPoint(int side) => _contacts[side == 0 ? 4 : 7].Point;
        public Bounds FootBounds(int side) => _feet[side].bounds;
        public (Vector3 Position, Quaternion Rotation, Vector3 Size) ReadBody(int index) =>
            (_bodies[index].position, _bodies[index].rotation, _contacts[index].Size);

        public (string Name, Vector3 Position, Vector3 Axis, float Angle, float Target, float Torque) ReadJoint(int index)
        {
            var j = _joints[index];
            return (j.Name, j.Position, j.Axis, j.Angle, j.AppliedTarget, j.Torque);
        }

        public int NonFootContacts => _contacts[0].Touches + _contacts[1].Touches + _contacts[2].Touches +
            _contacts[3].Touches + _contacts[5].Touches + _contacts[6].Touches;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (Controller != LowerBodyController.Sagittal) Physics.ContactEvent -= RecordPressure;
            if (_scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
            UnityEngine.Object.Destroy(_material);
        }

        sealed class JointDrive
        {
            public readonly string Name;
            public readonly float Target, Min, Max;
            readonly Rigidbody _parent, _child;
            readonly Vector3 _anchor;
            readonly float _cap, _effectiveInertia;
            readonly float _rollCap, _rollInertia;
            public readonly float RollLimit;
            public float RollTarget, RollTorque, PreviousRoll;
            public float Torque, Previous, AppliedTarget;
            public float EffortFraction => Mathf.Max(Mathf.Abs(Torque) / _cap, RollLimit > 0 ? Mathf.Abs(RollTorque) / _rollCap : 0f);
            public bool Saturated;
            public Vector3 Axis => _parent.rotation * Vector3.right;
            public Vector3 Position => _child.position + _child.rotation * _anchor;
            public Vector3 RollAxis => _parent.rotation * Vector3.forward;
            public float Velocity => Vector3.Dot(_child.angularVelocity - _parent.angularVelocity, Axis);
            public float RollVelocity => Vector3.Dot(_child.angularVelocity - _parent.angularVelocity, RollAxis);
            public float RollAngle => Twist(Quaternion.Inverse(_parent.rotation) * _child.rotation, false);
            public float Angle
            {
                get
                {
                    Quaternion relative = Quaternion.Inverse(_parent.rotation) * _child.rotation;
                    return RollLimit > 0 ? Twist(relative, true) : Mathf.DeltaAngle(0, relative.eulerAngles.x) * Mathf.Deg2Rad;
                }
            }

            public JointDrive(string name, Rigidbody parent, Rigidbody child, Vector3 anchor, float target,
                float min, float max, float cap, float rollLimit = 0f, float rollCap = 0f)
            {
                Name = name; _parent = parent; _child = child; Target = target; Min = min; Max = max; _cap = cap;
                AppliedTarget = target;
                RollLimit = rollLimit;
                _rollCap = rollCap;
                float parentI = Inertia(parent, anchor, Vector3.right), childI = Inertia(child, anchor, Vector3.right);
                _effectiveInertia = parentI * childI / (parentI + childI);
                _anchor = Quaternion.Inverse(child.rotation) * (anchor - child.position);
                Vector3 parentAnchor = Quaternion.Inverse(parent.rotation) * (anchor - parent.position);
                if (RollLimit > 0)
                {
                    float a = Inertia(parent, anchor, Vector3.forward), b = Inertia(child, anchor, Vector3.forward);
                    _rollInertia = a * b / (a + b);
                    var joint = child.gameObject.AddComponent<ConfigurableJoint>();
                    joint.axis = Vector3.right;
                    joint.secondaryAxis = Vector3.up;
                    joint.anchor = _anchor;
                    joint.connectedBody = parent;
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedAnchor = parentAnchor;
                    joint.xMotion = joint.yMotion = joint.zMotion = joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularXMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
                    joint.lowAngularXLimit = new SoftJointLimit { limit = (min - target) * Mathf.Rad2Deg };
                    joint.highAngularXLimit = new SoftJointLimit { limit = (max - target) * Mathf.Rad2Deg };
                    joint.angularZLimit = new SoftJointLimit { limit = rollLimit * Mathf.Rad2Deg };
                }
                else
                {
                    var hinge = child.gameObject.AddComponent<HingeJoint>();
                    hinge.axis = Vector3.right;
                    hinge.anchor = _anchor;
                    hinge.connectedBody = parent;
                    hinge.autoConfigureConnectedAnchor = false;
                    hinge.connectedAnchor = parentAnchor;
                    hinge.limits = new JointLimits { min = (min - target) * Mathf.Rad2Deg, max = (max - target) * Mathf.Rad2Deg };
                    hinge.useLimits = true;
                }
            }

            public void Apply(bool enabled, float balanceTorque, float rollTarget, float pitchTarget, float rollReflex, float stepSeconds)
            {
                Previous = Angle;
                Vector3 axis = Axis;
                float speed = Velocity;
                // Evaluate spring and damper at the predicted end of the substep.
                // The projected two-box inertia keeps small distal links from overshooting in one explicit update.
                const float stiffness = 150f, damping = 5f;
                float effectiveDamping = damping + stiffness * stepSeconds;
                AppliedTarget = Mathf.Clamp(pitchTarget, Min, Max);
                float effort = enabled ? (stiffness * (AppliedTarget - Previous) - effectiveDamping * speed) /
                    (1f + effectiveDamping * stepSeconds / _effectiveInertia) + balanceTorque : 0f;
                Torque = Mathf.Clamp(effort, -_cap, _cap);
                Saturated = Mathf.Abs(effort) >= _cap;
                _child.AddTorque(axis * Torque);
                _parent.AddTorque(-axis * Torque);
                if (RollLimit <= 0) return;
                RollTarget = rollTarget;
                PreviousRoll = RollAngle;
                const float rollStiffness = 220f, rollDamping = 9f;
                float effectiveRollDamping = rollDamping + rollStiffness * stepSeconds;
                float rollEffort = enabled ? (rollStiffness * (RollTarget - PreviousRoll) - effectiveRollDamping * RollVelocity) /
                    (1f + effectiveRollDamping * stepSeconds / _rollInertia) + rollReflex : 0f;
                RollTorque = Mathf.Clamp(rollEffort, -_rollCap, _rollCap);
                Saturated |= Mathf.Abs(rollEffort) >= _rollCap;
                _child.AddTorque(RollAxis * RollTorque);
                _parent.AddTorque(-RollAxis * RollTorque);
            }

            static float Twist(Quaternion q, bool pitch) => Mathf.DeltaAngle(0,
                2f * Mathf.Atan2(pitch ? q.x : q.z, q.w) * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            static float Inertia(Rigidbody body, Vector3 pivot, Vector3 axis)
            {
                Vector3 arm = Vector3.ProjectOnPlane(body.worldCenterOfMass - pivot, body.rotation * axis);
                return Vector3.Dot(body.inertiaTensor, axis) + body.mass * arm.sqrMagnitude;
            }
        }
    }

    public sealed class GroundReceiver : MonoBehaviour
    {
        internal Collider Ground;
        internal Vector3 Impulse, Point;
        internal int Touches;
        internal Vector3 Size { get; private set; }
        void Awake() => Size = GetComponent<BoxCollider>().size;
        internal void ResetTick() { Impulse = Point = Vector3.zero; Touches = 0; }
        void OnCollisionEnter(Collision collision) => Record(collision);
        void OnCollisionStay(Collision collision) => Record(collision);

        void Record(Collision collision)
        {
            if (collision.collider != Ground || collision.contactCount == 0) return;
            Impulse += collision.impulse;
            Point = collision.GetContact(0).point;
            Touches++;
        }
    }
}
