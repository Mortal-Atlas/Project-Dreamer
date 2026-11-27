using DreamMachine.Body;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public enum LowerBodyRecovery { ExistingAbort, EarlyCancellation, ControlledLowering, PlantedTarget, Combined }

    public sealed partial class NativeLowerBodyLab
    {
        float _previousSwingHeight, _lastLandingForward;
        bool _recoveryFootDown;
        public LowerBodyRecovery RecoveryPolicy { get; }
        public int CancellationTick { get; private set; } = -1;
        public int RecoveryTouchdownTick { get; private set; } = -1;
        public bool CancelledBeforeLift { get; private set; }
        public bool CancelledWhileSupported { get; private set; }
        public float LandingTargetZ { get; private set; } = float.NaN;

        bool EarlyRecovery => RecoveryPolicy == LowerBodyRecovery.EarlyCancellation || RecoveryPolicy == LowerBodyRecovery.Combined;
        bool LoweringRecovery => RecoveryPolicy == LowerBodyRecovery.ControlledLowering || RecoveryPolicy == LowerBodyRecovery.Combined;
        bool PlantedRecovery => RecoveryPolicy == LowerBodyRecovery.PlantedTarget || RecoveryPolicy == LowerBodyRecovery.Combined;
        bool MotorRecovery => (LoweringRecovery || PlantedRecovery) && _abortReturn &&
            (MotionPhase == StepPhase.Return || MotionPhase == StepPhase.Returned);

        bool WarnBeforeLift(int stance)
        {
            return EarlyRecovery && (MotionPhase == StepPhase.Transfer || MotionPhase == StepPhase.Prelift) &&
                DynamicSupport.WarnOutward(FootRegion(stance), _sole[stance].Contact, XCom.x, CenterVelocity.x, stance == 0 ? -1 : 1);
        }

        string SwingWarning(int stance, SoleReading swing)
        {
            float previousHeight = _previousSwingHeight;
            _previousSwingHeight = swing.Clearance;
            if (!EarlyRecovery) return null;
            if (DynamicSupport.WarnOutward(FootRegion(stance), _sole[stance].Contact, XCom.x, CenterVelocity.x, stance == 0 ? -1 : 1))
                return "outward-support-warning";
            if (MotionPhase == StepPhase.Advance && !swing.Contact && swing.Position.z - _swingOrigin.z < DynamicSupport.Advancement &&
                DynamicSupport.WarnPrematureTouchdown(previousHeight, swing.Clearance))
                return "descending-short-of-target";
            return null;
        }

        void BeginRecovery()
        {
            int stance = 1 - _swingSide;
            var swing = _sole[_swingSide];
            if (SwingStartedTick >= 0 && swing.Contact) RecoveryTouchdownTick = Tick;
            if (!MotorRecovery) return;
            _recoveryFootDown = swing.Contact;
            _lastLandingForward = _plannedForward;
            if (PlantedRecovery)
                LandingTargetZ = _sole[stance].Position.z + DynamicSupport.RecoveryForward(swing.Position.z - _sole[stance].Position.z);
        }

        void ReadRecoveryTouchdown()
        {
            var swing = _sole[_swingSide];
            if (SwingStartedTick < 0 || !swing.Contact) return;
            if (RecoveryTouchdownTick < 0) RecoveryTouchdownTick = Tick;
            if (_recoveryFootDown) return;
            _recoveryFootDown = true;
            if (PlantedRecovery) LandingTargetZ = swing.Position.z;
        }

        void PlanRecoveryFeet()
        {
            if (SwingStartedTick < 0) return;
            _plannedLift = Mathf.MoveTowards(_plannedLift, 0f, LoweringRecovery ? .004f : .002f);
            if (_recoveryFootDown) _plannedLift = 0f;
            if (PlantedRecovery)
            {
                // The target is a measured foot-relative offset. Only sagittal joint drives receive commands.
                float target = LandingTargetZ - (PelvisPosition.z + _initialFeet[_swingSide].z);
                _lastLandingForward = Mathf.MoveTowards(_lastLandingForward, Mathf.Clamp(target, -.065f, .075f), .004f);
                _plannedForward = _lastLandingForward;
            }
            else if (!_recoveryFootDown)
            {
                _plannedForward = Mathf.MoveTowards(_plannedForward,
                    DynamicSupport.RecoveryForward(_sole[_swingSide].Position.z - _swingOrigin.z), .003f);
            }
        }

        void UpdateRecoveryLoad()
        {
            if (FootContactMask != 3 || !NetPressureValid)
            {
                // Preserve stance loading in flight. Before lift, reduce the outward demand immediately.
                if (SwingStartedTick < 0) _requestedLoad = Mathf.MoveTowards(_requestedLoad, 0f, .045f);
                return;
            }
            float left = ReadLoad(0);
            // Increase weight on the new support only after a sustained physical touchdown.
            bool shared = ContactFraction(0) >= .8f && ContactFraction(1) >= .8f;
            float rate = shared && left > .12f && left < .88f ? .045f : .027f;
            _requestedLoad = Mathf.MoveTowards(_requestedLoad, 0f, rate);
        }

        float RecoveryAnkleReflex(int side)
        {
            if (!_sole[side].Contact || float.IsNaN(XCom.x)) return 0f;
            int stance = 1 - _swingSide;
            if (FootContactMask != 3 && side != stance) return 0f;
            float supportCenter = _sole[stance].Position.x;
            if (FootContactMask == 3)
            {
                float midpoint = (_sole[0].Position.x + _sole[1].Position.x) * .5f;
                supportCenter = Mathf.Lerp(midpoint, supportCenter, Mathf.Abs(_requestedLoad) * .65f);
            }
            float share = FootContactMask == 3 ? Mathf.Clamp(ReadLoad(side), .18f, .82f) : 1f;
            return share * Mathf.Clamp(-125f * (XCom.x - supportCenter) - 20f * CenterVelocity.x, -8f, 8f);
        }
    }
}
