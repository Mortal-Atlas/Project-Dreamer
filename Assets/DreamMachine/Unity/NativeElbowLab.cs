using System;
using System.Diagnostics;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamMachine.UnityBridge
{
    public sealed partial class NativeElbowLab : IElbowFixture, IDisposable
    {
        const float PadRadius = 0.035f;
        const float PuckRadius = 0.045f;
        const float Substep = WorldSimulation.Dt / ElbowLab.Substeps;
        readonly Scene _scene;
        readonly PhysicsScene _physics;
        readonly Rigidbody _arm, _puck;
        readonly PhysicsMaterial _material;
        readonly ElbowCollisionReceiver _receiver;
        readonly ElbowLab.Receptor[] _skin = new ElbowLab.Receptor[ElbowLab.ReceptorCount];
        readonly double[] _normal = new double[ElbowLab.ReceptorCount];
        readonly double[] _tangent = new double[ElbowLab.ReceptorCount];
        readonly double[] _slip = new double[ElbowLab.ReceptorCount];
        readonly double[] _duration = new double[ElbowLab.ReceptorCount];
        bool _disposed;
        double _impulse, _torqueImpulse;
        public bool ProfileEnabled { get; set; }
        public long MotorTimestampTicks { get; private set; }
        public long PhysicsTimestampTicks { get; private set; }
        public long ContactTimestampTicks { get; private set; }
        public long SensorTimestampTicks { get; private set; }
        public int SubstepCount => Tick * ElbowLab.Substeps;

        public string Backend => "unity-physx-v1";
        public ElbowLab.Settings Configuration { get; }
        public string IndividualId { get; }
        public int Tick { get; private set; }
        public bool Finished => Tick >= Configuration.Duration;
        public double SimSeconds => Tick * (double)WorldSimulation.Dt;
        public double Angle => Mathf.DeltaAngle(0, _arm.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        public double X => _puck.position.x;
        public double Y => _puck.position.y;
        public double VX => _puck.linearVelocity.x;
        public double VY => _puck.linearVelocity.y;
        public double ObjectAngle => Mathf.DeltaAngle(0, _puck.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        public double Spin => _puck.angularVelocity.z;
        public double RequestedTarget { get; private set; }
        public ElbowLab.StepSample Latest { get; private set; }
        public Vector3 LastContactPoint { get; private set; }
        public Vector3 LastContactNormal { get; private set; }
        public ElbowLab.Receptor ReadRegion(int index) => _skin[index];

        public NativeElbowLab(ElbowLab.Settings configuration, string individualId)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Native callbacks require Play Mode.");
            if (configuration.Duration == 0 || string.IsNullOrWhiteSpace(individualId) || individualId.Length > 128)
                throw new ArgumentException("Valid settings and a stable individual ID are required.");
            Configuration = configuration;
            IndividualId = individualId;
            RequestTarget(0.9);
            _scene = SceneManager.CreateScene("Elbow PhysX " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physics = _scene.GetPhysicsScene();
            _material = new PhysicsMaterial("Elbow lab contact")
            {
                dynamicFriction = (float)configuration.Friction,
                staticFriction = (float)configuration.Friction,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounciness = 0
            };
            _arm = CreateBody("Forearm physics", Vector3.zero, 1);
            AddSphere(_arm, PadRadius, new Vector3((float)ElbowLab.ArmLength, 0, 0));
            _arm.centerOfMass = Vector3.zero;
            _arm.inertiaTensor = Vector3.one * 0.06f;
            _arm.inertiaTensorRotation = Quaternion.identity;
            var hinge = _arm.gameObject.AddComponent<HingeJoint>();
            hinge.axis = Vector3.forward;
            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedAnchor = Vector3.zero;
            hinge.limits = new JointLimits
            {
                min = (float)(configuration.Lower - configuration.Initial) * Mathf.Rad2Deg,
                max = (float)(configuration.Upper - configuration.Initial) * Mathf.Rad2Deg,
                contactDistance = 0
            };
            _arm.rotation = Quaternion.AngleAxis((float)configuration.Initial * Mathf.Rad2Deg, Vector3.forward);
            hinge.useLimits = true;
            _puck = CreateBody(ElbowLab.ObjectId, new Vector3((float)configuration.X, (float)configuration.Y, 0),
                (float)configuration.Mass);
            AddSphere(_puck, PuckRadius, Vector3.zero);
            _puck.linearDamping = _puck.angularDamping = 2;
            _puck.inertiaTensor = Vector3.one * ((float)configuration.Mass * PuckRadius * PuckRadius * 0.5f);
            _receiver = _arm.gameObject.AddComponent<ElbowCollisionReceiver>();
            _receiver.Lab = this;
            Latest = new ElbowLab.StepSample(0, RequestedTarget, Angle, 0, 0, 0,
                new ElbowLab.Proprioception(Angle, 0, 0, 0));
        }

        Rigidbody CreateBody(string name, Vector3 position, float mass)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = mass;
            body.constraints = RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            body.maxAngularVelocity = 35;
            body.sleepThreshold = 0;
            body.solverIterations = 12;
            body.solverVelocityIterations = 4;
            return body;
        }

        void AddSphere(Rigidbody body, float radius, Vector3 center)
        {
            var sphere = body.gameObject.AddComponent<SphereCollider>();
            sphere.radius = radius;
            sphere.center = center;
            sphere.contactOffset = 0.001f;
            sphere.sharedMaterial = _material;
        }

        public void RequestTarget(double radians)
        {
            if (double.IsNaN(radians) || double.IsInfinity(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
            RequestedTarget = Math.Max(Configuration.Lower, Math.Min(Configuration.Upper, radians));
        }

        public ElbowLab.StepSample Step()
            => Step(0);

        public double LastBaselineTorque { get; private set; }
        public double LastAppliedCorrection { get; private set; }
        public double LastSignedWork { get; private set; }
        public double LastAbsoluteWork { get; private set; }

        public ElbowLab.StepSample Step(double correction)
        {
            if (_disposed || Finished) throw new InvalidOperationException("Native fixture cannot advance.");
            if (!PredictiveMotor.Finite(correction) || Math.Abs(correction) > PredictiveMotor.MaximumCorrection)
                throw new ArgumentOutOfRangeException(nameof(correction));
            double previous = Angle, x = X, y = Y, effort = 0, baseline = 0;
            LastSignedWork = LastAbsoluteWork = 0;
            _impulse = _torqueImpulse = 0;
            Array.Clear(_normal, 0, _normal.Length);
            Array.Clear(_tangent, 0, _tangent.Length);
            Array.Clear(_slip, 0, _slip.Length);
            LastContactPoint = LastContactNormal = Vector3.zero;
            for (int i = 0; i < ElbowLab.Substeps; i++)
            {
                long start = ProfileEnabled ? Stopwatch.GetTimestamp() : 0;
                double velocity = _arm.angularVelocity.z;
                double beforeAngle = Angle;
                double engineered = Math.Max(-2, Math.Min(2, 9 * (RequestedTarget - beforeAngle) - 0.7 * velocity));
                double command = Math.Max(-2, Math.Min(2, engineered + correction));
                _arm.AddTorque(Vector3.forward * (float)(command - 0.1 * velocity), ForceMode.Force);
                effort += command;
                baseline += engineered;
                _receiver.ContactMask = 0;
                if (ProfileEnabled) MotorTimestampTicks += Stopwatch.GetTimestamp() - start;
                start = ProfileEnabled ? Stopwatch.GetTimestamp() : 0;
                _physics.Simulate(Substep);
                if (ProfileEnabled) PhysicsTimestampTicks += Stopwatch.GetTimestamp() - start;
                double work = command * Mathf.DeltaAngle((float)beforeAngle * Mathf.Rad2Deg,
                    (float)Angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                LastSignedWork += work;
                LastAbsoluteWork += Math.Abs(work);
                for (int r = 0; r < _duration.Length; r++)
                    _duration[r] = (_receiver.ContactMask & (1 << r)) != 0 ? _duration[r] + Substep : 0;
            }
            long sensorStart = ProfileEnabled ? Stopwatch.GetTimestamp() : 0;
            for (int r = 0; r < _skin.Length; r++)
                _skin[r] = new ElbowLab.Receptor(_normal[r] / WorldSimulation.Dt, _duration[r],
                    _slip[r] / WorldSimulation.Dt, _tangent[r] / WorldSimulation.Dt);
            Latest = new ElbowLab.StepSample(++Tick, RequestedTarget, previous, _impulse, X - x, Y - y,
                new ElbowLab.Proprioception(Angle, _arm.angularVelocity.z, effort / ElbowLab.Substeps,
                    _torqueImpulse / WorldSimulation.Dt));
            LastBaselineTorque = baseline / ElbowLab.Substeps;
            LastAppliedCorrection = (effort - baseline) / ElbowLab.Substeps;
            if (ProfileEnabled) SensorTimestampTicks += Stopwatch.GetTimestamp() - sensorStart;
            return Latest;
        }

        internal int RecordContact(Collision collision)
        {
            if (collision.rigidbody != _puck || collision.contactCount == 0) return 0;
            long start = ProfileEnabled ? Stopwatch.GetTimestamp() : 0;
            ContactPoint contact = collision.GetContact(0);
            Vector3 tangent = _arm.rotation * Vector3.up;
            Vector3 armPoint = _arm.position + _arm.rotation * (Vector3.right * (float)ElbowLab.ArmLength);
            double region = ElbowLab.ContactRegion(Vector3.Dot(contact.point - armPoint, tangent) / PadRadius);
            double normal = Math.Abs(Vector3.Dot(collision.impulse, contact.normal));
            Vector3 planarTangent = Vector3.Cross(Vector3.forward, contact.normal);
            double lateral = Vector3.Dot(collision.impulse, planarTangent);
            double slip = Math.Abs(Vector3.Dot(_puck.GetPointVelocity(contact.point) -
                _arm.GetPointVelocity(contact.point), planarTangent));
            _impulse += normal;
            _torqueImpulse += Vector3.Cross(contact.point - _arm.position,
                contact.normal * (float)normal + planarTangent * (float)lateral).z;
            LastContactPoint = contact.point;
            LastContactNormal = contact.normal;
            double total = 0;
            for (int i = 0; i < _skin.Length; i++) total += ElbowLab.RegionWeight(region, i);
            int mask = 0;
            for (int i = 0; i < _skin.Length; i++)
            {
                double weight = ElbowLab.RegionWeight(region, i) / total;
                if (weight <= 0) continue;
                mask |= 1 << i;
                _normal[i] += normal * weight;
                _tangent[i] += lateral * weight;
                _slip[i] += slip * Substep * weight;
            }
            if (ProfileEnabled) ContactTimestampTicks += Stopwatch.GetTimestamp() - start;
            return mask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _receiver.Lab = null;
            if (_scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
            UnityEngine.Object.Destroy(_material);
        }
    }

    public sealed class ElbowCollisionReceiver : MonoBehaviour
    {
        internal NativeElbowLab Lab;
        internal int ContactMask;
        void OnCollisionEnter(Collision collision) => Receive(collision);
        void OnCollisionStay(Collision collision) => Receive(collision);

        void Receive(Collision collision)
        {
            if (Lab != null) ContactMask |= Lab.RecordContact(collision);
        }
    }
}
