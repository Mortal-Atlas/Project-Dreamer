using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public enum LowerBodyStepProtocol { Legacy, Interlock, Smooth, Prepared, LiftOnly }
    public enum StepPhase { DoubleSupport, Anticipation, Transfer, Prelift, Lift, Advance, Lower, Reacquire, FinalSupport, Complete, Return, Returned, Rejected, Fell }

    public sealed partial class NativeLowerBodyLab
    {
        readonly ContactRegion[] _contactRegions = new ContactRegion[2];
        readonly int[] _footLoadedSteps = new int[2];
        int _preliftTicks, _phaseTick, _liftTicks, _finalTicks, _pressureDirection;
        float _initialPressureX, _plannedLift, _plannedForward, _stageLift, _stageForward, _stanceXComReference, _clearanceTrim;
        bool _staged, _abortReturn;

        public LowerBodyStepProtocol StepProtocol { get; }
        public int PhysicsSubsteps { get; }
        public float PhysicsStepSeconds => WorldSimulation.Dt / PhysicsSubsteps;
        public float StaticWeightImpulse => -Physics.gravity.y * TotalMassKg * WorldSimulation.Dt;
        public Vector3 CenterVelocity => Momentum / TotalMassKg;
        public Vector3 AngularMomentum { get; private set; }
        public Vector3 XCom { get; private set; }
        public Vector3 NetPressure { get; private set; }
        public bool NetPressureValid { get; private set; }
        public float PeakEffortFraction { get; private set; }
        public PreliftFailure PreliftFailures { get; private set; }
        public int PreliftTicks => _preliftTicks;
        public StepPhase MotionPhase { get; private set; }
        public string TransitionReason { get; private set; } = "initial-double-support";
        public string AbortReason { get; private set; } = "none";
        public int PhaseStartedTick => _phaseTick;
        public bool DynamicGatePassed { get; private set; }
        public bool LiftPassed { get; private set; }
        public bool ForwardPassed { get; private set; }
        public int StanceLossTicks { get; private set; }
        public int SwingStartedTick { get; private set; } = -1;
        public float PeakBodyImpact { get; private set; }
        public bool ControlledReturn => MotionPhase == StepPhase.Returned && FirstFallTick < 0;
        public bool PreparationRunning => _staged && MotionPhase >= StepPhase.Anticipation && MotionPhase <= StepPhase.Prelift;
        public int DynamicStanceSide => _staged || SwingStartedTick >= 0 ? 1 - _swingSide : StanceSide;
        public float FrontalMargin => FootRegion(DynamicStanceSide).FrontalMargin(XCom.x);
        public float SagittalMargin => FootRegion(DynamicStanceSide).SagittalMargin(XCom.z);

        public LoadedFootRegion FootRegion(int side) => _contactRegions[side].Region;
        public float ContactFraction(int side) => _footLoadedSteps[side] / (float)PhysicsSubsteps;

        void CaptureDynamics()
        {
            AngularMomentum = Vector3.zero;
            foreach (var body in _bodies)
            {
                Quaternion principal = body.rotation * body.inertiaTensorRotation;
                Vector3 localSpin = Quaternion.Inverse(principal) * body.angularVelocity;
                Vector3 spin = principal * Vector3.Scale(body.inertiaTensor, localSpin);
                AngularMomentum += spin + Vector3.Cross(body.worldCenterOfMass - CenterOfMass, body.mass * body.linearVelocity);
            }
            float g = -Physics.gravity.y;
            XCom = new Vector3(DynamicSupport.Extrapolate(CenterOfMass.x, CenterVelocity.x, g, CenterOfMass.y), 0f,
                DynamicSupport.Extrapolate(CenterOfMass.z, CenterVelocity.z, g, CenterOfMass.y));
            float left = _sole[0].PressureValid ? LeftImpulse.y : 0f;
            float right = _sole[1].PressureValid ? RightImpulse.y : 0f;
            NetPressureValid = left + right > .01f && (!_sole[0].Contact || _sole[0].PressureValid) &&
                (!_sole[1].Contact || _sole[1].PressureValid);
            NetPressure = NetPressureValid ? (_sole[0].PressureCenter * left + _sole[1].PressureCenter * right) / (left + right) : Vector3.zero;
            int stance = DynamicStanceSide;
            var foot = _sole[stance];
            PreliftFailures = DynamicSupport.Evaluate(FootRegion(stance), foot.Contact, ContactFraction(stance), foot.Impulse.y,
                Mathf.Max(0f, _sole[1 - stance].Impulse.y), StaticWeightImpulse, foot.PressureValid, foot.PressureCenter.x,
                foot.PressureCenter.z, XCom.x, XCom.z, CenterVelocity.x, PelvisTilt, PeakEffortFraction, foot.Drift);
            PeakBodyImpact = Mathf.Max(PeakBodyImpact, OtherImpulse.magnitude);
        }

        public bool RequestPreparedStep(int side)
        {
            if ((StepProtocol != LowerBodyStepProtocol.Prepared && StepProtocol != LowerBodyStepProtocol.LiftOnly) ||
                (side != 0 && side != 1) || !_standingVerified || FootContactMask != 3 || _staged || !NetPressureValid)
                return false;
            _staged = true;
            _swingSide = 1 - side;
            _pressureDirection = side == 0 ? 1 : -1;
            _initialPressureX = NetPressure.x;
            Action = SwingOutcome.Idle;
            ActionReason = "none";
            _requestedLoad = 0f;
            _stanceTicks = _preliftTicks = 0;
            EnterPhase(StepPhase.Anticipation, "verified-double-support");
            return true;
        }

        void PrepareDynamicLoad()
        {
            if (!_staged) return;
            float target = _requestedLoad;
            if (MotionPhase == StepPhase.Anticipation) target = .18f * _pressureDirection;
            if (MotionPhase == StepPhase.Transfer || MotionPhase == StepPhase.Prelift) target = -.875f * _pressureDirection;
            bool returning = MotionPhase == StepPhase.Reacquire || MotionPhase == StepPhase.FinalSupport || MotionPhase == StepPhase.Return;
            if (returning && FootContactMask == 3 && _plannedLift <= .002f) target = 0f;
            _requestedLoad = Mathf.MoveTowards(_requestedLoad, target, returning ? .012f : .025f);
        }

        float PreparedRollTrim()
        {
            if (!_staged || (StepProtocol != LowerBodyStepProtocol.Prepared && StepProtocol != LowerBodyStepProtocol.LiftOnly) ||
                MotionPhase < StepPhase.Prelift || MotionPhase > StepPhase.Reacquire) return 0f;
            var region = FootRegion(DynamicStanceSide);
            if (!region.Valid || region.Width < .018f || float.IsNaN(XCom.x)) return 0f;
            float centre = (region.MinX + region.MaxX) * .5f;
            float predicted = Mathf.Clamp(XCom.x, region.MinX + .009f, region.MaxX - .009f);
            return Mathf.Clamp(.30f * (XCom.x - Mathf.Lerp(predicted, centre, .12f)), -.012f, .012f);
        }

        float StanceAnkleReflex(int joint)
        {
            if (!_staged || (StepProtocol != LowerBodyStepProtocol.Prepared && StepProtocol != LowerBodyStepProtocol.LiftOnly) ||
                joint % 3 != 2 || SwingStartedTick < 0 ||
                (MotionPhase != StepPhase.Return && (MotionPhase < StepPhase.Lift || MotionPhase > StepPhase.FinalSupport)))
                return 0f;
            int side = joint / 3;
            if (!_sole[side].Contact || float.IsNaN(XCom.x)) return 0f;
            bool sharing = FootContactMask == 3 && _plannedLift <= .002f &&
                (MotionPhase == StepPhase.Reacquire || MotionPhase == StepPhase.FinalSupport || MotionPhase == StepPhase.Return);
            if (!sharing && side == _swingSide) return 0f;
            // The compensating torque is applied to the shin through the planted ankle.
            float reference = _stanceXComReference;
            if (sharing)
            {
                float neutral = (_sole[0].Position.x + _sole[1].Position.x) * .5f;
                reference = Mathf.Lerp(neutral, reference, Mathf.Clamp01(Mathf.Abs(_requestedLoad) / .875f));
            }
            float ease = DynamicSupport.Smooth((Tick - SwingStartedTick + 1f) / 10f);
            float share = sharing ? .5f : 1f;
            return share * ease * Mathf.Clamp(-155f * (XCom.x - reference) - 14f * CenterVelocity.x, -6f, 6f);
        }

        void BeginStagedLift()
        {
            _staged = true;
            _swingOrigin = _sole[_swingSide].Position;
            var region = FootRegion(1 - _swingSide);
            _stanceXComReference = region.Valid ? Mathf.Lerp(XCom.x, (region.MinX + region.MaxX) * .5f, .65f) : XCom.x;
            SwingStartedTick = Tick;
            DynamicGatePassed = _preliftTicks >= DynamicSupport.PreliftTicks;
            Action = SwingOutcome.Swing;
            ActionReason = "none";
            EnterPhase(StepPhase.Lift, "sustained-stance-verified");
        }

        void EnterPhase(StepPhase phase, string reason)
        {
            MotionPhase = phase;
            TransitionReason = reason;
            _phaseTick = Tick;
            _stageLift = _plannedLift;
            _stageForward = _plannedForward;
        }

        void UpdateStagedStep()
        {
            if (MotionPhase == StepPhase.Rejected || MotionPhase == StepPhase.Fell) return;
            if (NonFootContacts > 0)
            {
                if (Action != SwingOutcome.Aborted) Action = SwingOutcome.Fell;
                ActionReason = "body-ground-contact";
                EnterPhase(StepPhase.Fell, "body-ground-contact");
                return;
            }
            if (MotionPhase == StepPhase.Complete || MotionPhase == StepPhase.Returned) return;
            int elapsed = Tick - _phaseTick;
            var stance = _sole[1 - _swingSide];
            var swing = _sole[_swingSide];
            if (PreparationRunning)
            {
                if (MotionPhase == StepPhase.Anticipation && elapsed >= 8 && FootContactMask == 3 && NetPressureValid &&
                    _pressureDirection * (NetPressure.x - _initialPressureX) >= .008f)
                    EnterPhase(StepPhase.Transfer, "pressure-shift-measured");
                else if (MotionPhase == StepPhase.Transfer && Mathf.Abs(_requestedLoad) >= .874f && ReadLoad(1 - _swingSide) >= .70f)
                    EnterPhase(StepPhase.Prelift, "stance-transfer-measured");
                else if (MotionPhase == StepPhase.Prelift && SwingReady && _preliftTicks >= DynamicSupport.PreliftTicks)
                    BeginStagedLift();
                else if (elapsed >= 100)
                {
                    Action = SwingOutcome.GateBlocked;
                    ActionReason = "preparation-timeout";
                    _requestedLoad = 0f;
                    EnterPhase(StepPhase.Rejected, "preparation-timeout");
                }
                return;
            }

            float advance = swing.Position.z - _swingOrigin.z;
            bool supporting = DynamicSupport.HoldsStance(stance.Contact, stance.Impulse.y, StaticWeightImpulse, stance.Drift);
            bool beforeContact = MotionPhase >= StepPhase.Lift && MotionPhase <= StepPhase.Lower;
            if (beforeContact && !supporting)
            {
                if (!stance.Contact) StanceLossTicks++;
                AbortStaged(!stance.Contact ? "stance-contact-lost" : stance.Drift > DynamicSupport.Slip ? "stance-drift" : "stance-impulse-low");
                return;
            }
            if (beforeContact)
            {
                SwingClearance = Mathf.Max(SwingClearance, swing.Clearance);
                SwingTravel = Mathf.Max(SwingTravel, advance);
                if ((StepProtocol == LowerBodyStepProtocol.Prepared || StepProtocol == LowerBodyStepProtocol.LiftOnly) &&
                    (FrontalMargin < -.012f || Mathf.Abs(CenterVelocity.x) > .10f))
                {
                    AbortStaged("lateral-margin-lost");
                    return;
                }
            }
            switch (MotionPhase)
            {
                case StepPhase.Lift:
                    _liftTicks = swing.Clearance >= DynamicSupport.Clearance && !swing.Contact ? _liftTicks + 1 : 0;
                    if (_liftTicks >= DynamicSupport.LiftHoldTicks)
                    {
                        LiftPassed = true;
                        EnterPhase(StepProtocol == LowerBodyStepProtocol.LiftOnly ? StepPhase.Lower : StepPhase.Advance, "measured-clearance-held");
                    }
                    else if (elapsed >= 42) AbortStaged("lift-timeout");
                    break;
                case StepPhase.Advance:
                    if (swing.Contact) AbortStaged("premature-swing-contact");
                    else if (advance >= DynamicSupport.Advancement && swing.Clearance >= DynamicSupport.Clearance)
                    {
                        ForwardPassed = true;
                        EnterPhase(StepPhase.Lower, "measured-forward-travel");
                    }
                    else if (elapsed >= 40) AbortStaged("forward-timeout");
                    break;
                case StepPhase.Lower:
                    if (swing.Contact)
                    {
                        if (StepProtocol != LowerBodyStepProtocol.LiftOnly && advance < DynamicSupport.Advancement)
                        {
                            AbortStaged("short-contact-placement");
                            break;
                        }
                        Action = SwingOutcome.Recontact;
                        EnterPhase(StepPhase.Reacquire, "physical-recontact");
                    }
                    else if (elapsed >= 36) AbortStaged("recontact-timeout");
                    break;
                case StepPhase.Reacquire:
                    if (BilateralSupport()) EnterPhase(StepPhase.FinalSupport, "bilateral-impulse-restored");
                    else if (elapsed >= 60) AbortStaged("bilateral-support-timeout");
                    break;
                case StepPhase.FinalSupport:
                case StepPhase.Return:
                    _finalTicks = BilateralSupport() ? _finalTicks + 1 : 0;
                    if (_finalTicks >= DynamicSupport.FinalSupportTicks)
                    {
                        bool completed = !_abortReturn && StepProtocol != LowerBodyStepProtocol.LiftOnly && DynamicGatePassed &&
                            LiftPassed && ForwardPassed && advance >= DynamicSupport.Advancement && stance.Drift <= DynamicSupport.Slip;
                        Action = completed ? SwingOutcome.Complete : _abortReturn ? SwingOutcome.Aborted : SwingOutcome.Recontact;
                        EnterPhase(completed ? StepPhase.Complete : StepPhase.Returned, completed ? "stable-physical-step" : "stable-bilateral-return");
                    }
                    else if (elapsed >= 60)
                    {
                        Action = SwingOutcome.Aborted;
                        ActionReason = "support-return-timeout";
                        EnterPhase(StepPhase.Rejected, "support-return-timeout");
                    }
                    break;
            }
        }

        bool BilateralSupport() => _sole[0].Impulse.y >= .12f * StaticWeightImpulse && _sole[1].Impulse.y >= .12f * StaticWeightImpulse &&
            _sole[0].Impulse.y + _sole[1].Impulse.y >= .78f * StaticWeightImpulse && NonFootContacts == 0 &&
            PelvisTilt <= 8f && Mathf.Abs(CenterVelocity.x) <= .05f && Mathf.Abs(CenterVelocity.z) <= .05f;

        void AbortStaged(string reason)
        {
            Action = SwingOutcome.Aborted;
            ActionReason = reason;
            AbortReason = reason;
            _abortReturn = true;
            _finalTicks = 0;
            EnterPhase(StepPhase.Return, reason);
        }

        void PlanStagedFeet()
        {
            if (!_staged) return;
            float elapsed = Tick - _phaseTick + 1;
            switch (MotionPhase)
            {
                case StepPhase.Lift: _plannedLift = Mathf.Lerp(_stageLift, .045f, DynamicSupport.Smooth(elapsed / 30f)); break;
                case StepPhase.Advance:
                    _plannedForward = Mathf.Lerp(_stageForward, .120f, DynamicSupport.Smooth(elapsed / 28f));
                    if (StepProtocol == LowerBodyStepProtocol.Prepared)
                        _clearanceTrim = Mathf.Clamp(_clearanceTrim + .13f * (.018f - _sole[_swingSide].Clearance), 0f, .024f);
                    _plannedLift = Mathf.Lerp(_stageLift, Mathf.Min(.045f, _stageLift + .010f), DynamicSupport.Smooth(elapsed / 16f)) + _clearanceTrim;
                    break;
                case StepPhase.Lower: _plannedLift = Mathf.Lerp(_stageLift, 0f, DynamicSupport.Smooth(elapsed / 22f)); break;
                case StepPhase.Reacquire:
                case StepPhase.FinalSupport:
                    _plannedLift = Mathf.Lerp(_stageLift, 0f, DynamicSupport.Smooth(elapsed / 12f));
                    break;
                case StepPhase.Return:
                    float u = DynamicSupport.Smooth(elapsed / 20f);
                    _plannedLift = Mathf.Lerp(_stageLift, 0f, u);
                    _plannedForward = Mathf.Lerp(_stageForward, 0f, u);
                    break;
            }
        }

        float StagedPitchTarget(int joint, float rest)
        {
            if (joint / 3 != _swingSide)
                return PreparationRunning && joint % 3 == 1 ? rest + .010f : rest;
            if (_plannedLift == 0f && _plannedForward == 0f) return rest;
            float a = Dimensions.Thigh, b = Dimensions.Shin;
            float down = NominalPelvisHeight - Dimensions.AnkleHeight - _plannedLift;
            float knee = Mathf.Max(.015f, Mathf.Acos(Mathf.Clamp((down * down + _plannedForward * _plannedForward - a * a - b * b) / (2f * a * b), -1f, 1f)));
            float hip = Mathf.Atan2(-_plannedForward, down) - Mathf.Atan2(b * Mathf.Sin(knee), a + b * Mathf.Cos(knee));
            return joint % 3 == 0 ? hip : joint % 3 == 1 ? knee : -hip - knee;
        }

        struct ContactRegion
        {
            Vector3 _min, _max;
            int _count;

            internal void Add(Vector3 point)
            {
                _min = _count == 0 ? point : Vector3.Min(_min, point);
                _max = _count == 0 ? point : Vector3.Max(_max, point);
                _count++;
            }

            internal LoadedFootRegion Region => _count >= 2 ? new LoadedFootRegion(_min.x, _max.x, _min.z, _max.z) : default;
        }
    }
}
