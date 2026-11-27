using System;
using DreamMachine.Sim;
using Unity.Collections;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public enum LowerBodyController { Sagittal, FrontalPose, SensorFeedback }
    public enum SwingOutcome { Idle, GateBlocked, Swing, Recontact, Complete, Aborted, Fell }

    public readonly struct VestibularReading
    {
        public readonly int Tick;
        public readonly Quaternion Orientation;
        public readonly Vector3 Gravity, AngularVelocity, LinearAcceleration;
        public readonly float GravityErrorBound;

        public VestibularReading(int tick, Quaternion orientation, Vector3 gravity, Vector3 angularVelocity,
            Vector3 worldAcceleration, Vector3 gravityNoise = default, float errorBound = 0f)
        {
            Tick = tick;
            Orientation = orientation;
            Quaternion inverse = Quaternion.Inverse(orientation);
            Gravity = inverse * gravity + gravityNoise;
            AngularVelocity = inverse * angularVelocity;
            LinearAcceleration = inverse * worldAcceleration;
            GravityErrorBound = errorBound;
        }
    }

    public readonly struct SoleReading
    {
        public readonly Vector3 Impulse, PressureCenter, Position;
        public readonly float Clearance, Drift;
        public readonly bool Contact, Changed, PressureValid;

        public SoleReading(Vector3 impulse, Vector3 position, float clearance, float drift, bool contact, bool changed,
            Vector3 pressureCenter, bool pressureValid)
        {
            Impulse = impulse; Position = position; Clearance = clearance; Drift = drift;
            Contact = contact; Changed = changed; PressureCenter = pressureCenter; PressureValid = pressureValid;
        }
    }

    public sealed partial class NativeLowerBodyLab
    {
        public const float RequiredStanceShare = .85f;
        public const int RequiredStanceTicks = 20;
        readonly int _groundId;
        readonly float _vestibularNoise;
        readonly (Vector3 Moment, float Weight)[] _pressure = new (Vector3, float)[2];
        readonly SoleReading[] _sole = new SoleReading[2];
        readonly float[] _rollTarget = new float[6];
        float _commandRoll, _feedbackTrim, _filteredShare = .5f;
        int _standingTicks, _stanceTicks, _actionTicks, _bilateralTicks, _swingSide;
        bool _standingVerified, _swingCleared;
        Vector3 _swingOrigin;

        public LowerBodyController Controller { get; }
        public VestibularReading Vestibular { get; private set; }
        public SwingOutcome Action { get; private set; }
        public string ActionReason { get; private set; } = "none";
        public int FirstFallTick { get; private set; } = -1;
        public float TargetShare => .5f + .4f * Mathf.Abs(_requestedLoad);
        public int StanceSide => _requestedLoad <= 0f ? 0 : 1;
        public int ActionStanceSide => _actionTicks > 0 || Action == SwingOutcome.Swing ? 1 - _swingSide : StanceSide;
        public int StanceTicks => _stanceTicks;
        public bool SwingReady => Controller != LowerBodyController.Sagittal && _standingVerified &&
            TargetShare + .000001f >= RequiredStanceShare && _stanceTicks >= RequiredStanceTicks;
        public float SwingClearance { get; private set; }
        public float SwingTravel { get; private set; }
        public SoleReading ReadSole(int side) => _sole[side];
        public Vector3 RollAxis(int joint) => _joints[joint].RollAxis;
        public float ReadLoad(int side)
        {
            float weight = Mathf.Max(LeftImpulse.y, 0f) + Mathf.Max(RightImpulse.y, 0f);
            return weight > .001f ? Mathf.Max(_sole[side].Impulse.y, 0f) / weight : 0f;
        }

        public (float Velocity, float Roll, float RollTarget, float RollVelocity, float RollTorque, float RollLimit, bool Saturated)
            ReadProprioception(int joint)
        {
            var drive = _joints[joint];
            return (drive.Velocity, drive.RollLimit > 0 ? drive.RollAngle : 0f, drive.RollTarget, drive.RollVelocity,
                drive.RollTorque, drive.RollLimit, drive.Saturated);
        }

        void PrepareLateralTargets()
        {
            if (Controller == LowerBodyController.Sagittal) return;
            float reference = -.075f * _requestedLoad;
            float ankleTrim = 0f;
            if (Controller == LowerBodyController.SensorFeedback)
            {
                float sign = Mathf.Sign(_requestedLoad);
                if (FootContactMask != 0) _filteredShare = Mathf.Lerp(_filteredShare, ReadLoad(StanceSide), .05f);
                float loadError = TargetShare - _filteredShare;
                _feedbackTrim = Mathf.MoveTowards(_feedbackTrim, -sign * Mathf.Clamp(.07f * loadError, -.018f, .020f), .0005f);
                if (Mathf.Abs(_requestedLoad) < .001f) { _feedbackTrim = 0f; _filteredShare = .5f; }
                reference += _feedbackTrim;
                // Use the simulated gravity and gyro sample from the preceding tick.
                ankleTrim = Mathf.Clamp(-.45f * Vestibular.Gravity.x / 9.81f + .065f * Vestibular.AngularVelocity.z, -.045f, .045f);
            }
            _commandRoll = Mathf.MoveTowards(_commandRoll, reference, .002f);
            _rollTarget[0] = _rollTarget[3] = _commandRoll;
            _rollTarget[2] = _rollTarget[5] = -_commandRoll + ankleTrim;
        }

        void RecordPressure(UnityEngine.PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headers)
        {
            if (scene != _physics) return;
            int left = _feet[0].GetInstanceID(), right = _feet[1].GetInstanceID();
            foreach (var header in headers)
            for (int index = 0; index < header.pairCount; index++)
            {
                var pair = header.GetContactPair(index);
                int other = pair.colliderInstanceID == _groundId ? pair.otherColliderInstanceID :
                    pair.otherColliderInstanceID == _groundId ? pair.colliderInstanceID : 0;
                if (other != left && other != right) continue;
                int side = other == left ? 0 : 1;
                for (int point = 0; point < pair.contactCount; point++)
                {
                    var contact = pair.GetContactPoint(point);
                    float weight = Mathf.Abs(contact.impulse.y);
                    _pressure[side].Moment += contact.position * weight;
                    _pressure[side].Weight += weight;
                }
            }
        }

        void CaptureSensors(Vector3 oldVelocity)
        {
            float bound = .06f * _vestibularNoise;
            float noise = bound * Mathf.Sin(Tick * 12.9898f + Seed);
            Vestibular = new VestibularReading(Tick, _pelvis.rotation, Physics.gravity, _pelvis.angularVelocity,
                (PelvisVelocity - oldVelocity) / WorldSimulation.Dt, Vector3.right * noise, bound);
            for (int side = 0; side < 2; side++)
            {
                var bounds = FootBounds(side);
                var impulse = side == 0 ? LeftImpulse : RightImpulse;
                bool contact = impulse.y > .01f;
                float weight = _pressure[side].Weight;
                Vector3 center = weight > .01f ? _pressure[side].Moment / weight : Vector3.zero;
                bool reliable = contact && weight > .01f && Mathf.Abs(weight - impulse.y) <= Mathf.Max(.05f, .08f * impulse.y) &&
                    center.x >= bounds.min.x - .02f && center.x <= bounds.max.x + .02f &&
                    center.z >= bounds.min.z - .02f && center.z <= bounds.max.z + .02f;
                float drift = Vector3.ProjectOnPlane(bounds.center - _initialFeet[side], Vector3.up).magnitude;
                _sole[side] = new SoleReading(impulse, bounds.center, bounds.min.y, drift, contact,
                    contact != _sole[side].Contact, center, reliable);
            }
        }

        public bool StableSupport(int side) => _sole[side].Contact && NonFootContacts == 0 &&
            Mathf.Abs(PelvisPosition.y - NominalPelvisHeight) <= .025f && PelvisTilt <= 8f &&
            ReadJoint(1).Angle > 0 && ReadJoint(1).Angle <= .30f && ReadJoint(4).Angle > 0 && ReadJoint(4).Angle <= .30f &&
            _sole[side].Drift <= .020f && MaximumPenetration <= .008f && JointLimitOvershoot <= .025f &&
            !Saturated && MomentumResidual.magnitude <= .15f;

        void UpdateSupportGate()
        {
            if (NonFootContacts > 0)
            {
                _standingVerified = false;
                if (FirstFallTick < 0) FirstFallTick = Tick;
            }
            if (Mathf.Abs(_requestedLoad) < .001f)
            {
                _standingTicks = StableSupport(0) && StableSupport(1) ? _standingTicks + 1 : 0;
                _standingVerified |= _standingTicks >= 120;
            }
            else _standingTicks = 0;
            _stanceTicks = _standingVerified && TargetShare + .000001f >= RequiredStanceShare &&
                ReadLoad(StanceSide) >= RequiredStanceShare && StableSupport(StanceSide)
                ? _stanceTicks + 1 : 0;
            if (Action == SwingOutcome.GateBlocked && SwingReady)
            {
                Action = SwingOutcome.Idle;
                ActionReason = "none";
            }
            if (Action != SwingOutcome.Swing && Action != SwingOutcome.Recontact) return;
            _actionTicks++;
            var sole = _sole[_swingSide];
            SwingClearance = Mathf.Max(SwingClearance, sole.Clearance);
            SwingTravel = Mathf.Max(SwingTravel, sole.Position.z - _swingOrigin.z);
            _swingCleared |= !sole.Contact && sole.Clearance >= .01f;
            if (NonFootContacts > 0) EndSwing(SwingOutcome.Fell, "body-ground-contact");
            else if (!_sole[1 - _swingSide].Contact) EndSwing(SwingOutcome.Aborted, "stance-contact-lost");
            else if (_sole[1 - _swingSide].Drift > .020f) EndSwing(SwingOutcome.Aborted, "stance-drift");
            else if (PelvisTilt > 12f) EndSwing(SwingOutcome.Aborted, "pelvis-tilt");
            else if (_swingCleared && sole.Contact && _actionTicks >= 12 && Action == SwingOutcome.Swing)
            {
                Action = SwingOutcome.Recontact;
                _requestedLoad = 0f;
            }
            if (Action == SwingOutcome.Recontact)
            {
                _bilateralTicks = FootContactMask == 3 ? _bilateralTicks + 1 : 0;
                if (_bilateralTicks >= 10)
                {
                    if (sole.Position.z - _swingOrigin.z >= .08f) EndSwing(SwingOutcome.Complete, "bilateral-support-restored");
                    else EndSwing(SwingOutcome.Aborted, "insufficient-forward-travel");
                }
            }
            if (_actionTicks >= 50 && (Action == SwingOutcome.Swing || Action == SwingOutcome.Recontact))
                EndSwing(SwingOutcome.Aborted, "recontact-timeout");
        }

        void EndSwing(SwingOutcome outcome, string reason)
        {
            Action = outcome;
            ActionReason = reason;
            _requestedLoad = 0f;
        }

        public bool TryRequestSwing()
        {
            if (Action != SwingOutcome.Idle && Action != SwingOutcome.GateBlocked) return false;
            if (!SwingReady)
            {
                Action = SwingOutcome.GateBlocked;
                ActionReason = "sustained-support-required";
                return false;
            }
            _swingSide = 1 - StanceSide;
            _swingOrigin = _sole[_swingSide].Position;
            Action = SwingOutcome.Swing;
            ActionReason = "none";
            return true;
        }

        float PitchTarget(int joint)
        {
            float rest = _joints[joint].Target;
            if (joint / 3 != _swingSide || (Action != SwingOutcome.Swing && Action != SwingOutcome.Recontact && Action != SwingOutcome.Complete)) return rest;
            float t = Mathf.Clamp01((_actionTicks + 1f) / 16f);
            if (Action != SwingOutcome.Swing) t = 1f;
            float forward = .11f * Mathf.SmoothStep(0f, 1f, t);
            float down = NominalPelvisHeight - Dimensions.AnkleHeight - .045f * Mathf.Sin(Mathf.PI * t);
            float a = Dimensions.Thigh, b = Dimensions.Shin;
            float knee = Mathf.Acos(Mathf.Clamp((down * down + forward * forward - a * a - b * b) / (2f * a * b), -1f, 1f));
            knee = Mathf.Max(.015f, knee);
            float hip = Mathf.Atan2(-forward, down) - Mathf.Atan2(b * Mathf.Sin(knee), a + b * Mathf.Cos(knee));
            return joint % 3 == 0 ? hip : joint % 3 == 1 ? knee : -hip - knee;
        }
    }
}
