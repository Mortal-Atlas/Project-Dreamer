using System;
using DreamMachine.Body;
using UnityEngine;
using NVector = System.Numerics.Vector3;

namespace DreamMachine.UnityBridge
{
    public readonly struct ArmBenchSetup
    {
        public readonly bool HighElbow, Obstruction;
        public readonly float WristYaw, DevelopmentDays;
        public ArmBenchSetup(bool highElbow, float wristYaw = 0, bool obstruction = false, float developmentDays = 3f)
        {
            if (!(Mathf.Abs(wristYaw) <= 40f && developmentDays >= 1f && developmentDays <= 6f))
                throw new ArgumentOutOfRangeException(nameof(wristYaw));
            HighElbow = highElbow; WristYaw = wristYaw; Obstruction = obstruction; DevelopmentDays = developmentDays;
        }
    }

    public readonly struct ArmReading
    {
        public readonly Vector3 Target, ProposedElbow, Shoulder, Elbow, Wrist, JointDegrees, JointSpeed, ActuatorTorque;
        public readonly float TorsoSeconds, TorsoImpulse, TorsoDepth, ObstacleSeconds, UpperDistalSeconds, UpperWork, ElbowWork, WristWork;
        public readonly int Corrections, LimitExcursions;
        public readonly bool WithinEnvelope;
        internal ArmReading(NativeArticulatedArm arm)
        {
            Target = arm.Target; ProposedElbow = arm.ProposedElbow;
            Shoulder = arm.Shoulder; Elbow = arm.Forearm.position; Wrist = arm.Palm.position;
            JointDegrees = arm.JointDegrees; JointSpeed = arm.JointSpeed; ActuatorTorque = arm.TorqueMagnitude;
            TorsoSeconds = arm.TorsoSeconds; TorsoImpulse = arm.TorsoImpulse; TorsoDepth = arm.TorsoDepth;
            ObstacleSeconds = arm.ObstacleSeconds; UpperDistalSeconds = arm.UpperDistalSeconds;
            UpperWork = arm.UpperWork; ElbowWork = arm.ElbowWork; WristWork = arm.WristWork;
            Corrections = arm.Corrections; LimitExcursions = arm.LimitExcursions; WithinEnvelope = arm.WithinEnvelope;
        }
    }

    // Physical bench chain. Only the torso is supported. All following poses come from PhysX.
    internal sealed class NativeArticulatedArm
    {
        public const string TorsoName = "body.torso", UpperName = "body.right.upper-arm", ForearmName = "body.right.forearm";
        readonly Rigidbody _torso, _upper;
        public readonly Rigidbody Forearm, Palm;
        readonly CapsuleCollider _upperShape, _forearmShape;
        readonly BoxCollider _torsoShape;
        readonly Collider[] _distal;
        readonly int _upperId, _forearmId, _torsoId;
        int _obstacleId;
        readonly float _length, _forearmLength, _yaw;
        readonly Vector3 _initialPole;
        readonly bool _contactResponse;
        Quaternion _upperTarget, _forearmTarget, _wristTarget;
        float _previousContact;
        bool _touch, _obstacleTouch, _upperDistalTouch, _corrected;
        public Vector3 Shoulder { get; }
        public Vector3 Target { get; private set; }
        public Vector3 ProposedElbow { get; private set; }
        public bool WithinEnvelope { get; private set; }
        public Vector3 TorqueMagnitude { get; private set; }
        public Vector3 JointSpeed { get; private set; }
        public Vector3 JointDegrees { get; private set; }
        public float UpperWork { get; private set; }
        public float ElbowWork { get; private set; }
        public float WristWork { get; private set; }
        public float TorsoSeconds { get; private set; }
        public float TorsoImpulse { get; private set; }
        public float TorsoDepth { get; private set; }
        public float ObstacleSeconds { get; private set; }
        public float UpperDistalSeconds { get; private set; }
        public float UpperTorsoSeconds { get; private set; }
        public float ForearmTorsoSeconds { get; private set; }
        public float HandTorsoSeconds { get; private set; }
        public int Corrections { get; private set; }
        public int LimitExcursions { get; private set; }
        bool _upperTouch, _forearmTouch, _handTouch;

        public NativeArticulatedArm(ArmBenchSetup settings, int seed, Vector3 start, Func<string, Vector3, float, Rigidbody> body,
            PhysicsMaterial material, bool contactResponse = true)
        {
            // A documented uniform enlargement fits the existing 250 mm ball bench.
            var anatomy = new HumanoidAnatomy(seed).AtAge(settings.DevelopmentDays);
            _length = anatomy.UpperArm * 1.43f; _forearmLength = anatomy.Forearm * 1.43f;
            Shoulder = new Vector3(.104f, .462f, -.615f);
            _yaw = settings.WristYaw;
            _initialPole = settings.HighElbow ? new Vector3(.70f, 1f, .08f) : new Vector3(1f, -.45f, -.10f);
            _contactResponse = contactResponse;
            _torso = body(TorsoName, Shoulder + new Vector3(-.155f, -.129f, -.047f), 4f);
            _torso.isKinematic = true;
            _torsoShape = _torso.gameObject.AddComponent<BoxCollider>();
            _torsoShape.size = new Vector3(.228f, .319f, .150f);
            Configure(_torsoShape, material);
            Target = start;
            var wrist = BuildCandidate(start, _initialPole);
            _upper = body(UpperName, Shoulder, .43f * _length / .32f);
            BindRotation(_upper, _upperTarget);
            _upperShape = Segment(_upper, _length, .041f, .067f, material);
            Forearm = body(ForearmName, wrist.Elbow, .31f * _forearmLength / .26f);
            BindRotation(Forearm, _forearmTarget);
            _forearmShape = Segment(Forearm, _forearmLength, .033f, .046f, material);
            Palm = body("body.right.hand", wrist.Wrist, .12f);
            BindRotation(Palm, _wristTarget);
            _upper.maxAngularVelocity = Forearm.maxAngularVelocity = Palm.maxAngularVelocity = 12f;
            _upper.maxDepenetrationVelocity = Forearm.maxDepenetrationVelocity = Palm.maxDepenetrationVelocity = 2f;
            BallJoint(_upper, _torso, 100f, 91f, true);
            var elbow = LinkJoint(Forearm, _upper);
            elbow.angularXMotion = ConfigurableJointMotion.Free;
            elbow.angularYMotion = elbow.angularZMotion = ConfigurableJointMotion.Locked;
            BallJoint(Palm, Forearm, 80f, 77f, false);
            _torsoId = _torsoShape.GetInstanceID(); _upperId = _upperShape.GetInstanceID(); _forearmId = _forearmShape.GetInstanceID();
            _distal = new Collider[5];
        }

        public void SetDistal(Collider palm, Collider[] pads, Collider[] digits)
        { _distal[0] = palm; _distal[1] = pads[0]; _distal[2] = pads[1]; _distal[3] = digits[0]; _distal[4] = digits[1]; }

        public void SetObstacle(Collider obstacle)
        {
            obstacle.providesContacts = true;
            _obstacleId = obstacle.GetInstanceID();
        }
        public bool IsMonitoredContact(int a, int b) =>
            a == _torsoId || b == _torsoId || a == _upperId || b == _upperId ||
            (_obstacleId != 0 && (a == _obstacleId || b == _obstacleId));

        (Vector3 Elbow, Vector3 Wrist) BuildCandidate(Vector3 wrist, Vector3 pole)
        {
            var p = SupportedArmReach.Propose(N(Shoulder), N(wrist), N(pole), _length, _forearmLength);
            var elbow = U(p.Elbow);
            ProposedElbow = elbow;
            var endpoint = U(p.Wrist);
            var upper = (elbow - Shoulder).normalized;
            var lower = (endpoint - elbow).normalized;
            var normal = Vector3.Cross(upper, lower).normalized;
            _upperTarget = Quaternion.LookRotation(upper, Vector3.Cross(upper, normal));
            _forearmTarget = Quaternion.LookRotation(lower, Vector3.Cross(lower, normal));
            _wristTarget = Quaternion.Euler(0, _yaw, 0);
            WithinEnvelope = p.WithinEnvelope;
            return (elbow, endpoint);
        }

        public void Request(Vector3 target, bool allowCorrection, float? yaw = null)
        {
            float contact = TorsoSeconds + ObstacleSeconds;
            if (_contactResponse && allowCorrection && !_corrected && contact - _previousContact >= .010f)
            {
                _corrected = true;
                Corrections++;
            }
            _previousContact = contact;
            Target = target;
            BuildCandidate(target, _corrected ? new Vector3(1f, .85f, .23f) : _initialPole);
            if (yaw.HasValue) _wristTarget = Quaternion.Euler(0f, yaw.Value, 0f);
        }

        public void Drive(float dt)
        {
            _touch = _upperTouch = _forearmTouch = _handTouch = _obstacleTouch = _upperDistalTouch = false;
            var shoulderSpeed = _upper.angularVelocity;
            var elbowSpeed = Forearm.angularVelocity - _upper.angularVelocity;
            var wristSpeed = Palm.angularVelocity - Forearm.angularVelocity;
            var shoulderGravity = GravityTorque(_upper, Shoulder) + GravityTorque(Forearm, Shoulder) + GravityTorque(Palm, Shoulder, .09f);
            var elbowGravity = GravityTorque(Forearm, Forearm.position) + GravityTorque(Palm, Forearm.position, .09f);
            Vector3 axis = _upper.rotation * Vector3.right;
            float desiredFlex = Vector3.SignedAngle(_upperTarget * Vector3.forward, _forearmTarget * Vector3.forward,
                _upperTarget * Vector3.right);
            float actualFlex = Vector3.SignedAngle(_upper.rotation * Vector3.forward, Forearm.rotation * Vector3.forward, axis);
            // The denominator makes the velocity feedback implicit at the controller's native timestep.
            var shoulder = Vector3.ClampMagnitude((28f * Error(_upperTarget, _upper.rotation) - 2.4f * shoulderSpeed) /
                (1f + 2.4f * dt / .012f) + shoulderGravity, 13f);
            float endStop = Mathf.Clamp(actualFlex, 6f, 151f) - actualFlex;
            var elbow = axis * Mathf.Clamp(((17f * (desiredFlex - actualFlex) + 28f * endStop) * Mathf.Deg2Rad -
                1.15f * Vector3.Dot(elbowSpeed, axis)) / (1f + 1.15f * dt / .005f) + Vector3.Dot(elbowGravity, axis), -8f, 8f);
            var wrist = Vector3.ClampMagnitude((9.5f * Error(_wristTarget, Palm.rotation) - .45f * wristSpeed) /
                (1f + .45f * dt / .00075f), 3f);
            Pair(_upper, _torso, shoulder);
            Pair(Forearm, _upper, elbow);
            Pair(Palm, Forearm, wrist);
            JointSpeed = new Vector3(shoulderSpeed.magnitude, Vector3.Dot(elbowSpeed, axis), wristSpeed.magnitude);
            TorqueMagnitude = new Vector3(shoulder.magnitude, elbow.magnitude, wrist.magnitude);
            JointDegrees = new Vector3(Quaternion.Angle(_torso.rotation, _upper.rotation), actualFlex,
                Quaternion.Angle(Forearm.rotation, Palm.rotation));
            UpperWork += Mathf.Abs(Vector3.Dot(shoulder, shoulderSpeed)) * dt;
            ElbowWork += Mathf.Abs(Vector3.Dot(elbow, elbowSpeed)) * dt;
            WristWork += Mathf.Abs(Vector3.Dot(wrist, wristSpeed)) * dt;
            if (actualFlex < 4f || actualFlex > 153f) LimitExcursions++;
        }

        public void ObserveContact(int first, int second, float impulse)
        {
            if (impulse <= 1e-6f) return;
            int otherUpper = first == _upperId ? second : second == _upperId ? first : 0;
            if (otherUpper != 0) _upperDistalTouch |= IsDistal(otherUpper);
            int obstructionContact = first == _obstacleId ? second : second == _obstacleId ? first : 0;
            if (_obstacleId != 0 && obstructionContact != 0)
                _obstacleTouch |= obstructionContact == _upperId || obstructionContact == _forearmId || IsDistal(obstructionContact);
            int other = first == _torsoId ? second : second == _torsoId ? first : 0;
            if (other == 0) return;
            bool upper = other == _upperId, forearm = other == _forearmId, hand = IsDistal(other);
            if (!(upper || forearm || hand)) return;
            _upperTouch |= upper; _forearmTouch |= forearm; _handTouch |= hand;
            _touch = true;
            TorsoImpulse += impulse;
        }

        bool IsDistal(int id)
        {
            foreach (Collider piece in _distal) if (id == piece.GetInstanceID()) return true;
            return false;
        }

        public void EndSubstep(float dt)
        {
            if (_obstacleTouch) ObstacleSeconds += dt;
            if (_upperDistalTouch) UpperDistalSeconds += dt;
            if (!_touch) return;
            TorsoSeconds += dt;
            if (_upperTouch) UpperTorsoSeconds += dt;
            if (_forearmTouch) ForearmTorsoSeconds += dt;
            if (_handTouch) HandTorsoSeconds += dt;
            TorsoDepth = Mathf.Max(TorsoDepth, Penetration(_upperShape), Penetration(_forearmShape));
            foreach (var piece in _distal) TorsoDepth = Mathf.Max(TorsoDepth, Penetration(piece));
        }

        float Penetration(Collider c) => Physics.ComputePenetration(_torsoShape, _torso.position, _torso.rotation,
            c, c.transform.position, c.transform.rotation, out _, out float depth) ? depth : 0f;

        public (Vector3 Position, Quaternion Rotation, Vector3 Size) ReadPart(int index)
        {
            if (index == 0) return (_torso.position, _torso.rotation, _torsoShape.size);
            var capsule = index == 1 ? _upperShape : _forearmShape;
            var t = capsule.transform;
            return (t.TransformPoint(capsule.center), t.rotation, new Vector3(2f * capsule.radius, 2f * capsule.radius, capsule.height));
        }

        static CapsuleCollider Segment(Rigidbody b, float length, float radius, float proximal, PhysicsMaterial material)
        {
            var col = b.gameObject.AddComponent<CapsuleCollider>();
            col.direction = 2; col.radius = radius; col.height = Mathf.Max(2f * radius, length - proximal - .019f);
            col.center = Vector3.forward * ((length + proximal - .019f) * .5f);
            Configure(col, material);
            b.ResetCenterOfMass(); b.ResetInertiaTensor();
            return col;
        }

        static void Configure(Collider collider, PhysicsMaterial material)
        { collider.contactOffset = .001f; collider.sharedMaterial = material; collider.providesContacts = true; }

        static void BallJoint(Rigidbody child, Rigidbody parent, float twist, float swing, bool collide)
        {
            var j = LinkJoint(child, parent);
            j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Limited;
            j.lowAngularXLimit = new SoftJointLimit { limit = -twist };
            j.highAngularXLimit = new SoftJointLimit { limit = twist };
            j.angularYLimit = j.angularZLimit = new SoftJointLimit { limit = Mathf.Min(swing, 89f) };
            j.enableCollision = collide;
        }

        static ConfigurableJoint LinkJoint(Rigidbody child, Rigidbody parent)
        {
            var j = child.gameObject.AddComponent<ConfigurableJoint>();
            j.connectedBody = parent; j.autoConfigureConnectedAnchor = false;
            j.anchor = Vector3.zero;
            j.connectedAnchor = Quaternion.Inverse(parent.rotation) * (child.position - parent.position);
            j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
            return j;
        }

        static void BindRotation(Rigidbody body, Quaternion rotation)
        {
            body.transform.rotation = rotation;
            body.rotation = rotation;
        }

        static Vector3 GravityTorque(Rigidbody body, Vector3 pivot, float extraMass = 0f) =>
            Vector3.Cross(body.worldCenterOfMass - pivot, -Physics.gravity * (body.mass + extraMass));

        static void Pair(Rigidbody child, Rigidbody parent, Vector3 torque)
        {
            child.AddTorque(torque);
            if (!parent.isKinematic) parent.AddTorque(-torque);
        }

        static Vector3 Error(Quaternion target, Quaternion actual)
        {
            var q = target * Quaternion.Inverse(actual);
            var vector = new Vector3(q.x, q.y, q.z) * (q.w < 0f ? -1f : 1f);
            float length = vector.magnitude;
            return length > .00001f ? vector * (2f * Mathf.Atan2(length, Mathf.Abs(q.w)) / length) : 2f * vector;
        }

        static NVector N(Vector3 v) => new NVector(v.x, v.y, v.z);
        static Vector3 U(NVector v) => new Vector3(v.X, v.Y, v.Z);
    }
}
