using System;
using System.Diagnostics;
using System.IO;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class LowerBodyDynamicStudy
{
    const string Key = "Dreamer.C12";

    static LowerBodyDynamicStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Development() => Start("development");
    public static void Final() => Start("heldout");

    static void Start(string study)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        SessionState.SetString(Key, study);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseString(Key);
            EditorApplication.Exit(0);
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Run(SessionState.GetString(Key, "")); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Run(string cohort)
    {
        VerifyIsolation();
        string directory = Application.persistentDataPath;
        using var summary = File.CreateText(Path.Combine(directory, $"phase-c12-{cohort}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(directory, $"phase-c12-{cohort}-trace.csv"));
        summary.WriteLine("seed,age,side,controller,protocol,substeps,step_ms,eligible,rejected,attempted,dynamic_gate,lift,forward,complete,abort,controlled_return,fall_tick,phase,reason,stance_loss_ticks,clearance_m,advance_m,final_bilateral,stance_share_before,stance_impulse_before_ns,frontal_margin_before_m,sagittal_margin_before_m,prelift_mask_before,phase_gate_mask,min_action_frontal_margin_m,min_action_sagittal_margin_m,peak_body_impulse_ns,activity_work_j,saturation_ticks,static_total_impulse_ns,static_peak_residual_ns,static_dual_ticks,mean_native_us,ticks");
        LowerBodyTrace.WriteHeader(trace, true);
        if (cohort == "development")
        {
            foreach (int seed in new[] { 101, 303 })
            foreach (int substeps in new[] { 10, 5, 4, 2 })
            for (int side = 0; side < 2; side++)
                Trial(seed, side, LowerBodyStepProtocol.Legacy, LowerBodyController.FrontalPose, substeps, summary, trace);
            foreach (int seed in new[] { 101, 303 })
            foreach (var protocol in new[] { LowerBodyStepProtocol.Interlock, LowerBodyStepProtocol.Smooth, LowerBodyStepProtocol.Prepared, LowerBodyStepProtocol.LiftOnly })
            for (int side = 0; side < 2; side++)
                Trial(seed, side, protocol, LowerBodyController.FrontalPose, 10, summary, trace);
            foreach (int seed in new[] { 101, 303 })
            foreach (var controller in new[] { LowerBodyController.SensorFeedback, LowerBodyController.DampedSensorFeedback })
            for (int side = 0; side < 2; side++)
                Trial(seed, side, LowerBodyStepProtocol.Interlock, controller, 10, summary, trace);
        }
        else
        {
            foreach (int seed in new[] { 909, 1010, 1111 })
            {
                foreach (LowerBodyStepProtocol protocol in Enum.GetValues(typeof(LowerBodyStepProtocol)))
                for (int side = 0; side < 2; side++)
                    Trial(seed, side, protocol, LowerBodyController.FrontalPose, 10, summary, trace);
                foreach (var controller in new[] { LowerBodyController.SensorFeedback, LowerBodyController.DampedSensorFeedback })
                for (int side = 0; side < 2; side++)
                    Trial(seed, side, LowerBodyStepProtocol.Interlock, controller, 10, summary, trace);
            }
        }
        Debug.Log("C12_COMPLETE " + cohort);
    }

    static void Trial(int seed, int side, LowerBodyStepProtocol protocol, LowerBodyController controller,
        int substeps, TextWriter summary, TextWriter trace)
    {
        float age = seed == 303 || seed == 1111 ? 3f : 6f;
        using var lab = new NativeLowerBodyLab(seed, age, controller: controller, stepProtocol: protocol, physicsSubsteps: substeps);
        long totals = 0;
        void Step(string note, bool record = true)
        {
            long begin = Stopwatch.GetTimestamp();
            lab.Step();
            totals += Stopwatch.GetTimestamp() - begin;
            if (record) LowerBodyTrace.WriteRow(trace, lab, note, true);
        }
        float vertical = 0f, residual = 0f;
        int dual = 0;
        for (int i = 0; i < 180; i++)
        {
            Step("standing", i >= 140);
            if (i < 140) continue;
            vertical += lab.LeftImpulse.y + lab.RightImpulse.y;
            residual = Mathf.Max(residual, lab.MomentumResidual.magnitude);
            if (lab.FootContactMask == 3) dual++;
        }
        bool prepared = protocol == LowerBodyStepProtocol.Prepared || protocol == LowerBodyStepProtocol.LiftOnly;
        if (!prepared)
        {
            lab.RequestLoad(side == 0 ? -.875f : .875f);
            for (int i = 0; i < 100; i++) Step("transfer");
        }
        bool eligible = prepared ? lab.FootContactMask == 3 && lab.NetPressureValid : lab.SwingReady;
        float initialShare = lab.ReadLoad(side), initialImpulse = lab.ReadSole(side).Impulse.y;
        float frontal = lab.FootRegion(side).FrontalMargin(lab.XCom.x), sagittal = lab.FootRegion(side).SagittalMargin(lab.XCom.z);
        int initialMask = (int)lab.PreliftFailures;
        bool requested = prepared ? lab.RequestPreparedStep(side) : lab.TryRequestSwing();
        float work = 0f;
        int saturation = 0;
        int stopTick = int.MaxValue;
        float minFrontal = float.PositiveInfinity, minSagittal = float.PositiveInfinity;
        int phaseGateMask = initialMask;
        if (requested)
        {
            for (int i = 0; i < 360; i++)
            {
                int wasStarted = lab.SwingStartedTick;
                StepPhase previous = lab.MotionPhase;
                Step("action");
                if (prepared) VerifyPhase(previous, lab.MotionPhase, lab.DynamicGatePassed);
                if (wasStarted < 0 && lab.SwingStartedTick >= 0) phaseGateMask = (int)lab.PreliftFailures;
                if (lab.SwingStartedTick >= 0 && stopTick == int.MaxValue)
                {
                    work += lab.AbsoluteWork;
                    saturation += lab.Saturated ? 1 : 0;
                    if (!float.IsNaN(lab.FrontalMargin)) minFrontal = Mathf.Min(minFrontal, lab.FrontalMargin);
                    if (!float.IsNaN(lab.SagittalMargin)) minSagittal = Mathf.Min(minSagittal, lab.SagittalMargin);
                }
                bool ended = lab.MotionPhase == StepPhase.Returned || lab.MotionPhase == StepPhase.Complete || lab.MotionPhase == StepPhase.Rejected ||
                    lab.MotionPhase == StepPhase.Fell || (!prepared && protocol != LowerBodyStepProtocol.Smooth && lab.Action != SwingOutcome.Swing && lab.Action != SwingOutcome.Recontact);
                if (ended && stopTick == int.MaxValue) stopTick = lab.Tick + 40;
                if (lab.Tick >= stopTick) break;
            }
        }
        bool attempted = lab.SwingStartedTick >= 0;
        bool complete = lab.MotionPhase == StepPhase.Complete;
        if (complete && (!lab.DynamicGatePassed || !lab.LiftPassed || !lab.ForwardPassed || lab.FirstFallTick >= 0 || lab.StanceLossTicks != 0))
            throw new Exception("Completed step bypassed a physical gate.");
        if (prepared && attempted && !lab.DynamicGatePassed) throw new Exception("Staged lift bypassed the interlock.");
        double us = totals * 1e6 / Stopwatch.Frequency / lab.Tick;
        string reason = lab.AbortReason != "none" ? lab.AbortReason : lab.ActionReason == "none" ? lab.TransitionReason : lab.ActionReason;
        string row = FormattableString.Invariant($"{seed},{age},{side},{controller},{protocol},{substeps},{lab.PhysicsStepSeconds * 1000f:F2},{eligible},{!attempted},{attempted},{lab.DynamicGatePassed},{lab.LiftPassed},{lab.ForwardPassed},{complete},{lab.Action == SwingOutcome.Aborted},{lab.ControlledReturn},{lab.FirstFallTick},{lab.MotionPhase},{reason},{lab.StanceLossTicks},{lab.SwingClearance:F6},{lab.SwingTravel:F6},{lab.FootContactMask == 3},{initialShare:F6},{initialImpulse:F6},{frontal:F6},{sagittal:F6},{initialMask},{phaseGateMask},{minFrontal:F6},{minSagittal:F6},{lab.PeakBodyImpact:F6},{work:F6},{saturation},{vertical / 40f:F6},{residual:F6},{dual},{us:F2},{lab.Tick}");
        summary.WriteLine(row);
        summary.Flush();
        Debug.Log("C12_TRIAL " + row);
    }

    static void VerifyPhase(StepPhase previous, StepPhase current, bool dynamicGate)
    {
        if (previous == current || current == StepPhase.Fell || current == StepPhase.Return || current == StepPhase.Rejected) return;
        bool valid = previous switch
        {
            StepPhase.Anticipation => current == StepPhase.Transfer,
            StepPhase.Transfer => current == StepPhase.Prelift,
            StepPhase.Prelift => current == StepPhase.Lift && dynamicGate,
            StepPhase.Lift => current == StepPhase.Advance || current == StepPhase.Lower,
            StepPhase.Advance => current == StepPhase.Lower,
            StepPhase.Lower => current == StepPhase.Reacquire,
            StepPhase.Reacquire => current == StepPhase.FinalSupport,
            StepPhase.FinalSupport => current == StepPhase.Complete || current == StepPhase.Returned,
            StepPhase.Return => current == StepPhase.Returned,
            _ => false
        };
        if (!valid) throw new Exception($"Unverified physical transition {previous} to {current}.");
    }

    static void VerifyIsolation()
    {
        using var plain = new NativeLowerBodyLab(101, 6, controller: LowerBodyController.FrontalPose);
        using var observed = new NativeLowerBodyLab(101, 6, controller: LowerBodyController.FrontalPose);
        if (observed.TryRequestSwing()) throw new Exception("Unsupported initial swing passed.");
        var refs = new (Vector3 Position, Vector3 Impulse, Vector3 Angular)[200];
        for (int tick = 0; tick < refs.Length; tick++)
        {
            if (tick == 160) plain.RequestLoad(-.875f);
            plain.Step();
            refs[tick] = (plain.PelvisPosition, plain.LeftImpulse, plain.AngularMomentum);
        }
        if (observed.Tick != 0) throw new Exception("Independent scene advanced.");
        var clock = new TimeGovernor { TargetScale = 4f };
        float error = 0f;
        for (int frame = 0; frame < 50; frame++)
        {
            int count = clock.Plan(WorldSimulation.Dt, WorldSimulation.Dt, 8);
            if (count != 4) throw new Exception("Fixed tick budget changed.");
            for (int step = 0; step < count; step++)
            {
                int index = observed.Tick;
                if (index == 160) observed.RequestLoad(-.875f);
                observed.Step();
                _ = LowerBodyTrace.Capture(observed, "observation", true);
                var reference = refs[index];
                error = Mathf.Max(error, Vector3.Distance(reference.Position, observed.PelvisPosition));
                error = Mathf.Max(error, Vector3.Distance(reference.Impulse, observed.LeftImpulse));
                error = Mathf.Max(error, Vector3.Distance(reference.Angular, observed.AngularMomentum));
            }
        }
        if (error > .00001f) throw new Exception("Observed fixed-step run diverged from reference.");
        Debug.Log(FormattableString.Invariant($"C12_CLOCK ticks={observed.Tick} max_abs_delta={error:R}"));
    }
}
