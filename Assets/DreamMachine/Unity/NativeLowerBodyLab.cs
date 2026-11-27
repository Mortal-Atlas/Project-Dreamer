using System;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamMachine.UnityBridge
{
    public sealed class NativeLowerBodyLab : IDisposable
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

        public NativeLowerBodyLab(int seed, float stage, float friction = 0.85f)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Native contact callbacks require Play Mode.");
            if (!(stage >= 0f && stage <= HumanoidAnatomy.AdultAgeDays && friction >= 0f && friction <= 1f))
                throw new ArgumentOutOfRangeException(nameof(stage));
            Seed = seed;
            Stage = stage;
            Friction = friction;
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
            _joints[joint] = new JointDrive(prefix + "hip", _pelvis, thigh, hip, rest.Hip, -0.75f, 0.65f, 27f);
            _joints[joint + 1] = new JointDrive(prefix + "knee", thigh, shin, knee, rest.Knee, 0.015f, 1.28f, 27f);
            _joints[joint + 2] = new JointDrive(prefix + "ankle", shin, foot, ankle, rest.Ankle, -0.55f, 0.55f, 21f);
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
            _requestedLoad = Mathf.Clamp(side, -1f, 1f);
        }

        public void Step(Vector3 disturbanceImpulse = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NativeLowerBodyLab));
            if (!(disturbanceImpulse.sqrMagnitude <= 16f))
                throw new ArgumentOutOfRangeException(nameof(disturbanceImpulse), "Research impulse capped at 4 N s.");
            Vector3 before = Momentum;
            foreach (var c in _contacts) c.ResetTick();
            AbsoluteWork = SignedWork = 0;
            Saturated = false;
            ExternalImpulse = disturbanceImpulse;
            if (disturbanceImpulse != Vector3.zero) _pelvis.AddForce(disturbanceImpulse, ForceMode.Impulse);
            for (int substep = 0; substep < Substeps; substep++)
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
                    _joints[joint].Apply(enabled, foot >= 0 && _contacts[foot].Impulse.y > 0f ? balance : 0f);
                    Saturated |= _joints[joint].Saturated;
                }
                ShiftBallast();
                _physics.Simulate(Substep);
                foreach (var drive in _joints)
                {
                    float work = drive.Torque * (drive.Angle - drive.Previous);
                    SignedWork += work;
                    AbsoluteWork += Mathf.Abs(work);
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
                JointLimitOvershoot = Mathf.Max(JointLimitOvershoot, Mathf.Max(drive.Min - drive.Angle, drive.Angle - drive.Max));
        }

        void ShiftBallast()
        {
            Vector3 axis = _pelvis.rotation * Vector3.right;
            Vector3 point = _ballast.worldCenterOfMass;
            float speed = Vector3.Dot(_ballast.linearVelocity - _pelvis.GetPointVelocity(point), axis);
            float force = Mathf.Clamp(115f * (_requestedLoad * _travel * 0.9f - BallastOffset) - 35f * speed, -25f, 25f);
            Vector3 applied = axis * force;
            _ballast.AddForce(applied);
            _pelvis.AddForceAtPosition(-applied, point);
            float work = force * speed * Substep;
            SignedWork += work;
            AbsoluteWork += Mathf.Abs(work);
            Saturated |= Mathf.Abs(force) >= 25f;
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
            return (j.Name, j.Position, j.Axis, j.Angle, j.Target, j.Torque);
        }

        public int NonFootContacts => _contacts[0].Touches + _contacts[1].Touches + _contacts[2].Touches +
            _contacts[3].Touches + _contacts[5].Touches + _contacts[6].Touches;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
            UnityEngine.Object.Destroy(_material);
        }

        sealed class JointDrive
        {
            public readonly string Name;
            public readonly float Target, Min, Max;
            readonly Rigidbody _parent, _child;
            readonly HingeJoint _hinge;
            readonly float _cap, _effectiveInertia;
            public float Torque, Previous;
            public bool Saturated;
            public Vector3 Axis => _parent.rotation * Vector3.right;
            public Vector3 Position => _child.position + _child.rotation * _hinge.anchor;
            public float Angle
            {
                get
                {
                    Quaternion relative = Quaternion.Inverse(_parent.rotation) * _child.rotation;
                    return Mathf.DeltaAngle(0, relative.eulerAngles.x) * Mathf.Deg2Rad;
                }
            }

            public JointDrive(string name, Rigidbody parent, Rigidbody child, Vector3 anchor, float target,
                float min, float max, float cap)
            {
                Name = name; _parent = parent; _child = child; Target = target; Min = min; Max = max; _cap = cap;
                float parentI = PitchInertia(parent, anchor), childI = PitchInertia(child, anchor);
                _effectiveInertia = parentI * childI / (parentI + childI);
                _hinge = child.gameObject.AddComponent<HingeJoint>();
                _hinge.axis = Vector3.right;
                _hinge.anchor = Quaternion.Inverse(child.rotation) * (anchor - child.position);
                _hinge.connectedBody = parent;
                _hinge.autoConfigureConnectedAnchor = false;
                _hinge.connectedAnchor = Quaternion.Inverse(parent.rotation) * (anchor - parent.position);
                _hinge.limits = new JointLimits
                {
                    min = (min - target) * Mathf.Rad2Deg, max = (max - target) * Mathf.Rad2Deg,
                    contactDistance = 0f
                };
                _hinge.useLimits = true;
            }

            public void Apply(bool enabled, float balanceTorque)
            {
                Previous = Angle;
                Vector3 axis = Axis;
                float speed = Vector3.Dot(_child.angularVelocity - _parent.angularVelocity, axis);
                // Evaluate spring and damper at the predicted end of the substep.
                // The projected two-box inertia keeps small distal links from overshooting in one explicit update.
                const float stiffness = 150f, damping = 5f;
                float effectiveDamping = damping + stiffness * Substep;
                float effort = enabled ? (stiffness * (Target - Previous) - effectiveDamping * speed) /
                    (1f + effectiveDamping * Substep / _effectiveInertia) + balanceTorque : 0f;
                Torque = Mathf.Clamp(effort, -_cap, _cap);
                Saturated = Mathf.Abs(effort) >= _cap;
                _child.AddTorque(axis * Torque);
                _parent.AddTorque(-axis * Torque);
            }

            static float PitchInertia(Rigidbody body, Vector3 pivot)
            {
                Vector3 arm = Vector3.ProjectOnPlane(body.worldCenterOfMass - pivot, body.rotation * Vector3.right);
                return body.inertiaTensor.x + body.mass * arm.sqrMagnitude;
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
