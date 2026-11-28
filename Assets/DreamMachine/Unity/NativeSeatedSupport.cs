using System;
using DreamMachine.Body;
using UnityEngine;
using NVector = System.Numerics.Vector3;

namespace DreamMachine.UnityBridge
{
    public readonly struct SeatedBenchSetup
    {
        public readonly SeatedSupportMode Mode;
        public readonly float Stiffness, InitialPitch, HeadYaw;

        public SeatedBenchSetup(SeatedSupportMode mode, float stiffness = 30f, float initialPitch = 0f, float headYaw = 0f)
        {
            if (!(mode >= SeatedSupportMode.Rigid && mode <= SeatedSupportMode.Anticipatory &&
                stiffness >= 20f && stiffness <= 70f && Mathf.Abs(initialPitch) <= 12f && Mathf.Abs(headYaw) <= 28f))
                throw new ArgumentOutOfRangeException(nameof(stiffness));
            Mode = mode; Stiffness = stiffness; InitialPitch = initialPitch; HeadYaw = headYaw;
        }
    }

    public readonly struct PostureReading
    {
        public readonly SeatedSupportMode Mode;
        public readonly Vector3 TorsoAngular, Head, Eye, HeadAngular, HeadAcceleration, Shoulder, Pelvis, SupportForce, Command;
        public readonly Quaternion TorsoRotation, HeadRotation;
        public readonly float HeadDiameter, Work, Effort, ContactSeconds, ContactImpulse, Penetration;
        public readonly float SeatForce, SeatSeconds, SeatImpulse;

        public PostureReading(SeatedSupportMode mode, Vector3 torsoAngular, Vector3 head, Quaternion headRotation,
            Vector3 eye, Vector3 headAngular, Vector3 headAcceleration, Vector3 shoulder, Vector3 pelvis,
            Quaternion torsoRotation, Vector3 supportForce, Vector3 command, float headDiameter,
            float work, float effort, float contactSeconds, float contactImpulse, float penetration,
            float seatForce, float seatSeconds, float seatImpulse)
        {
            Mode = mode; TorsoAngular = torsoAngular; Head = head; HeadRotation = headRotation; Eye = eye;
            HeadAngular = headAngular; HeadAcceleration = headAcceleration; Shoulder = shoulder; Pelvis = pelvis;
            TorsoRotation = torsoRotation; SupportForce = supportForce; Command = command; HeadDiameter = headDiameter;
            Work = work; Effort = effort; ContactSeconds = contactSeconds; ContactImpulse = contactImpulse;
            Penetration = penetration;
            SeatForce = seatForce; SeatSeconds = seatSeconds; SeatImpulse = seatImpulse;
        }
    }

    // The pelvis is fixed to the bench. The lumbar joint carries the upper-body load.
    internal sealed class NativeSeatedSupport
    {
        public static readonly Vector3 PelvisSize = new Vector3(.214f, .093f, .138f);
        public static readonly Vector3 SeatSize = new Vector3(.28f, .044f, .23f);
        public static readonly Vector3 SeatOffset = new Vector3(0f, -.0685f, -.03f);
        static readonly Vector3 Lumbar = new Vector3(0f, -.122f, -.018f);
        static readonly Vector3 SkipOwnHead = new Vector3(-.038f, .055f, .103f);
        readonly SeatedBenchSetup _setup;
        readonly Rigidbody _torso, _head;
        readonly ConfigurableJoint _spine;
        readonly Quaternion _neckRest;
        readonly Collider _pelvisShape, _headShape, _seatShape;
        readonly int _pelvisId, _headId, _seatId;
        bool _hit, _seatHit;
        float _work, _effort, _contact, _impulse, _penetration;
        float _seatForce, _seatSeconds, _seatImpulse, _seatWindow;
        Vector3 _lastHeadSpeed, _headAcceleration, _command;

        public Vector3 Pelvis { get; }
        public Vector3 RestEye { get; }
        public Vector3 Eye => _head.position + _head.rotation * SkipOwnHead;
        public Quaternion EyeRotation => _head.rotation;
        public bool DynamicTorso => _setup.Mode != SeatedSupportMode.Rigid;

        public NativeSeatedSupport(SeatedBenchSetup setup, HumanoidDimensions anatomy, Rigidbody torso,
            Func<string, Vector3, float, Rigidbody> body, PhysicsMaterial material)
        {
            _setup = setup; _torso = torso;
            Vector3 neutral = torso.position;
            Pelvis = neutral + Lumbar;
            var pelvis = body("body.pelvis", Pelvis, 2.4f);
            pelvis.isKinematic = true;
            _pelvisShape = Box(pelvis, PelvisSize, material);
            var seat = body("fixture.seat", Pelvis + SeatOffset, 4f);
            seat.isKinematic = true;
            _seatShape = Box(seat, SeatSize, material);

            float headRadius = Mathf.Clamp(.56f * anatomy.Head, .070f, .105f);
            Vector3 headRest = neutral + new Vector3(-.04f, .238f, .021f);
            _neckRest = Quaternion.Euler(0f, setup.HeadYaw, 0f);
            RestEye = headRest + SkipOwnHead;
            _head = body("body.head", headRest, .84f);
            _headShape = _head.gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)_headShape).radius = headRadius;
            ContactSurface(_headShape, material);
            _head.ResetCenterOfMass();
            _head.ResetInertiaTensor();
            _torso.maxAngularVelocity = 6f;
            _head.maxAngularVelocity = 9f;
            if (DynamicTorso)
            {
                _torso.isKinematic = false;
                _torso.ResetCenterOfMass();
                _torso.ResetInertiaTensor();
                _spine = Pivot(torso, pelvis, Lumbar, Vector3.zero, 26f, 24f, 19f);
            }
            else Physics.IgnoreCollision(torso.GetComponent<Collider>(), _pelvisShape);
            Pivot(_head, torso, new Vector3(0f, -.065f, -.008f), headRest - neutral + new Vector3(0f, -.065f, -.008f),
                26f, 38f, 23f);
            var initial = Quaternion.Euler(setup.InitialPitch, 0f, 0f);
            Bind(_torso, Pelvis + initial * (neutral - Pelvis), initial);
            Bind(_head, Pelvis + initial * (headRest - Pelvis), initial * _neckRest);
            _pelvisId = _pelvisShape.GetInstanceID();
            _headId = _headShape.GetInstanceID();
            _seatId = _seatShape.GetInstanceID();
        }

        public void Drive(float dt, Vector3 intent, float heldWeight)
        {
            _hit = _seatHit = false;
            if (DynamicTorso)
            {
                var upright = Error(Quaternion.identity, _torso.rotation);
                var speed = _torso.angularVelocity;
                var passive = Vector3.ClampMagnitude(_setup.Stiffness * upright - 2.5f * speed, 18f);
                _command = U(SeatedPosture.Command(_setup.Mode, N(upright), N(speed), N(intent - Pelvis), 4.4f + heldWeight));
                _torso.AddTorque(passive + _command);
                _work += Mathf.Abs(Vector3.Dot(_command, speed)) * dt;
                _effort += _command.magnitude * dt;
            }
            var desiredHead = (_setup.Mode >= SeatedSupportMode.Reactive ? Quaternion.identity : _torso.rotation) * _neckRest;
            var neck = Vector3.ClampMagnitude(4.2f * Error(desiredHead, _head.rotation) -
                .85f * (_head.angularVelocity - (_setup.Mode >= SeatedSupportMode.Reactive ? Vector3.zero : _torso.angularVelocity)), 2.1f);
            _head.AddTorque(neck);
            if (DynamicTorso) _torso.AddTorque(-neck);
            _work += Mathf.Abs(Vector3.Dot(neck, _head.angularVelocity - _torso.angularVelocity)) * dt;
        }

        public void Disturb(Vector3 angularImpulse)
        { if (DynamicTorso && angularImpulse != Vector3.zero) _torso.AddTorque(angularImpulse, ForceMode.Impulse); }

        public bool Monitors(int id) => id == _seatId || id == _pelvisId || id == _headId;

        public void Contact(int first, int second, float impulse, Collider armPiece)
        {
            if (armPiece == null || impulse <= 1e-6f) return;
            int id = Monitors(first) ? first : second;
            var self = id == _headId ? _headShape : id == _seatId ? _seatShape : _pelvisShape;
            _hit = true;
            _impulse += impulse;
            if (id == _seatId)
            {
                _seatHit = true;
                _seatWindow += impulse;
                _seatImpulse += impulse;
            }
            var a = self.transform;
            var b = armPiece.transform;
            if (Physics.ComputePenetration(self, a.position, a.rotation, armPiece, b.position, b.rotation, out _, out float depth))
                _penetration = Mathf.Max(_penetration, depth);
        }

        public void EndSubstep(float dt)
        {
            if (_hit) _contact += dt;
            if (_seatHit) _seatSeconds += dt;
        }

        public void Sample(float dt)
        {
            var speed = _head.linearVelocity;
            _headAcceleration = (speed - _lastHeadSpeed) / dt;
            _lastHeadSpeed = speed;
            _seatForce = _seatWindow / dt;
            _seatWindow = 0f;
        }

        public PostureReading Read(Vector3 shoulder) => new PostureReading(_setup.Mode, _torso.angularVelocity,
            _head.position, _head.rotation, Eye, _head.angularVelocity, _headAcceleration, shoulder, Pelvis, _torso.rotation,
            _spine == null ? Vector3.zero : _spine.currentForce, _command, ((SphereCollider)_headShape).radius * 2f,
            _work, _effort, _contact, _impulse, _penetration, _seatForce, _seatSeconds, _seatImpulse);

        static ConfigurableJoint Pivot(Rigidbody child, Rigidbody parent, Vector3 local, Vector3 connected,
            float pitch, float yaw, float roll)
        {
            var joint = child.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = parent; joint.autoConfigureConnectedAnchor = false;
            joint.anchor = local; joint.connectedAnchor = connected;
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -pitch };
            joint.highAngularXLimit = new SoftJointLimit { limit = pitch };
            joint.angularYLimit = new SoftJointLimit { limit = yaw };
            joint.angularZLimit = new SoftJointLimit { limit = roll };
            return joint;
        }

        static BoxCollider Box(Rigidbody body, Vector3 size, PhysicsMaterial material)
        {
            var collider = body.gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            ContactSurface(collider, material);
            return collider;
        }

        static void ContactSurface(Collider collider, PhysicsMaterial material)
        { collider.sharedMaterial = material; collider.contactOffset = .001f; collider.providesContacts = true; }

        static void Bind(Rigidbody body, Vector3 p, Quaternion q)
        { body.transform.SetPositionAndRotation(p, q); body.position = p; body.rotation = q; }

        static Vector3 Error(Quaternion target, Quaternion actual)
        {
            var q = target * Quaternion.Inverse(actual);
            var v = new Vector3(q.x, q.y, q.z) * (q.w < 0f ? -1f : 1f);
            float length = v.magnitude;
            return length < 1e-5f ? 2f * v : v * (2f * Mathf.Atan2(length, Mathf.Abs(q.w)) / length);
        }

        static NVector N(Vector3 v) => new NVector(v.x, v.y, v.z);
        static Vector3 U(NVector v) => new Vector3(v.X, v.Y, v.Z);
    }
}
