using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public static void RecoveryDevelopment() => Start("recovery-development");
    public static void RecoveryHeldOut() => Start("recovery-heldout");
    public static void RecoveryVisual() => Start("recovery-visual");
    public static void PreliftVisual() => Start("prelift-visual");

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
        if (cohort == "recovery-visual" || cohort == "prelift-visual")
        {
            RenderRecovery(cohort == "prelift-visual");
            return;
        }
        if (cohort.StartsWith("recovery-", StringComparison.Ordinal))
        {
            RunRecovery(cohort);
            return;
        }
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

    static void RunRecovery(string cohort)
    {
        bool heldOut = cohort == "recovery-heldout";
        int[] seeds = heldOut ? new[] { 1771, 2113, 2591, 2677 } : new[] { 101, 303, 1010 };
        string directory = Application.persistentDataPath;
        using var summary = File.CreateText(Path.Combine(directory, $"phase-c13-{cohort}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(directory, $"phase-c13-{cohort}-trace.csv"));
        summary.WriteLine("seed,age,side,policy,step_ms,eligible,rejected_before_lift,swing_started,held_clearance,cancelled,cancelled_supported,cancel_tick,touchdown_tick,controlled_return,complete,fall_tick,body_contacts,peak_clearance_m,advance_m,peak_effort,saturation_ticks,work_j,min_frontal_m,min_stance_contact_fraction,final_bilateral,mean_native_us,reason");
        LowerBodyTrace.WriteHeader(trace, dynamics: true, recovery: true);
        for (int index = 0; index < seeds.Length; index++)
        for (int side = 0; side < 2; side++)
        foreach (var policy in new[] { LowerBodyRecovery.ExistingAbort, LowerBodyRecovery.EarlyCancellation, LowerBodyRecovery.ControlledLowering, LowerBodyRecovery.PlantedTarget, LowerBodyRecovery.Combined })
            RecoveryTrial(seeds[index], index % 2 == 0 ? 6f : 3f, side, policy, summary, trace);
        Debug.Log("C13_COMPLETE " + cohort);
    }

    static void RenderRecovery(bool prelift)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var root = new GameObject("C.1.3 measured lower body", typeof(LowerBodyLabView));
        var view = root.GetComponent<LowerBodyLabView>();
        var type = typeof(LowerBodyLabView);
        type.GetMethod("Start", flags).Invoke(view, null);
        var field = type.GetField("_lab", flags);
        ((NativeLowerBodyLab)field.GetValue(view)).Dispose();
        int seed = prelift ? 1010 : 2113;
        float age = prelift ? 6f : 3f;
        var policy = prelift ? LowerBodyRecovery.ActivePreliftAndLanding : LowerBodyRecovery.Combined;
        var lab = new NativeLowerBodyLab(seed, age, controller: LowerBodyController.FrontalPose,
            stepProtocol: LowerBodyStepProtocol.Prepared, recoveryPolicy: policy);
        field.SetValue(view, lab);
        using var control = new NativeLowerBodyLab(seed, age, controller: LowerBodyController.FrontalPose,
            stepProtocol: LowerBodyStepProtocol.Prepared, recoveryPolicy: policy);
        type.GetField("_status", flags).SetValue(view, prelift ? "Active prelift brake" : "Combined recovery");
        string phase = prelift ? "c14" : "c13";
        type.GetField("_initialPelvis", flags).SetValue(view, lab.PelvisPosition);
        var camera = root.GetComponentInChildren<Camera>();
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        camera.targetTexture = target;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
        { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .4f; }
        void Capture(string state)
        {
            type.GetMethod("Draw", flags).Invoke(view, null);
            type.GetMethod("Refresh", flags).Invoke(view, null);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"phase-{phase}-{state}.png"), texture.EncodeToPNG());
            Debug.Log($"{phase.ToUpperInvariant()}_VISUAL {state} tick={lab.Tick} phase={lab.MotionPhase} contact={lab.FootContactMask}");
        }
        try
        {
            for (int i = 0; i < 180; i++) { lab.Step(); control.Step(); }
            if (!lab.RequestPreparedStep(0) || !control.RequestPreparedStep(0))
                throw new Exception("Representative recovery was ineligible.");
            bool cancelled = false, touched = false, returned = false;
            float difference = 0;
            while (lab.Tick < 480)
            {
                lab.Step();
                control.Step();
                if (lab.MotionPhase != control.MotionPhase || lab.FootContactMask != control.FootContactMask ||
                    lab.CancellationTick != control.CancellationTick || lab.RecoveryTouchdownTick != control.RecoveryTouchdownTick)
                    throw new Exception("Observed recovery diverged from its control.");
                difference = Mathf.Max(difference, Vector3.Distance(lab.PelvisPosition, control.PelvisPosition),
                    Vector3.Distance(lab.LeftImpulse, control.LeftImpulse), Vector3.Distance(lab.RightImpulse, control.RightImpulse));
                if (!cancelled && lab.CancellationTick == lab.Tick)
                { Capture(prelift ? "brake" : "cancel"); cancelled = true; }
                if (!touched && lab.RecoveryTouchdownTick == lab.Tick)
                { Capture("touchdown"); touched = true; }
                if (!returned && lab.ControlledReturn)
                { Capture("double-support"); returned = true; }
            }
            if (!cancelled || (!prelift && !touched) || !returned || lab.FirstFallTick >= 0)
                throw new Exception("Recovery visual sequence incomplete.");
            if (difference > .00001f) throw new Exception("Observed recovery measurements diverged.");
            Debug.Log(FormattableString.Invariant($"{phase.ToUpperInvariant()}_VISUAL observer_ticks={lab.Tick} observed_max_delta={difference:R}"));
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void RecoveryTrial(int seed, float age, int side, LowerBodyRecovery policy, TextWriter summary, TextWriter trace)
    {
        using var lab = new NativeLowerBodyLab(seed, age, controller: LowerBodyController.FrontalPose,
            stepProtocol: LowerBodyStepProtocol.Prepared, physicsSubsteps: 10, recoveryPolicy: policy);
        for (int i = 0; i < 180; i++) lab.Step();
        bool eligible = lab.RequestPreparedStep(side);
        int baseline = lab.Tick, stop = lab.Tick + 360, saturationTicks = 0, bodyContacts = 0;
        float peakEffort = 0, work = 0, minimumMargin = float.PositiveInfinity, minimumContact = 1f;
        long elapsed = 0;
        while (eligible && lab.Tick < stop)
        {
            StepPhase previous = lab.MotionPhase;
            long start = Stopwatch.GetTimestamp();
            lab.Step();
            elapsed += Stopwatch.GetTimestamp() - start;
            VerifyPhase(previous, lab.MotionPhase, lab.DynamicGatePassed);
            LowerBodyTrace.WriteRow(trace, lab, "recovery", dynamics: true, recovery: true);
            bodyContacts += lab.NonFootContacts > 0 ? 1 : 0;
            saturationTicks += lab.Saturated ? 1 : 0;
            peakEffort = Mathf.Max(peakEffort, lab.PeakEffortFraction);
            work += lab.AbsoluteWork;
            if (lab.MotionPhase >= StepPhase.Prelift && lab.MotionPhase <= StepPhase.Return)
            {
                if (!float.IsNaN(lab.FrontalMargin)) minimumMargin = Mathf.Min(minimumMargin, lab.FrontalMargin);
                minimumContact = Mathf.Min(minimumContact, lab.ContactFraction(side));
            }
            if (previous != lab.MotionPhase && (lab.MotionPhase == StepPhase.Returned || lab.MotionPhase == StepPhase.Complete ||
                lab.MotionPhase == StepPhase.Fell || lab.MotionPhase == StepPhase.Rejected))
                stop = Math.Min(stop, lab.Tick + 50);
        }
        bool complete = lab.MotionPhase == StepPhase.Complete;
        if (complete && (!lab.DynamicGatePassed || !lab.LiftPassed || !lab.ForwardPassed || lab.FirstFallTick >= 0 || lab.StanceLossTicks != 0))
            throw new Exception("Completed step bypassed physical admission.");
        if (lab.ControlledReturn && lab.FootContactMask != 3) throw new Exception("Return lost double support during follow-up.");
        bool started = lab.SwingStartedTick >= 0;
        if (started && !lab.DynamicGatePassed) throw new Exception("Lift bypassed physical admission.");
        bool cancelled = lab.CancellationTick >= 0;
        double us = elapsed * 1e6 / Stopwatch.Frequency / Math.Max(1, lab.Tick - baseline);
        string row = FormattableString.Invariant($"{seed},{age},{side},{policy},{lab.PhysicsStepSeconds * 1000f:F2},{eligible},{eligible && !started},{started},{lab.LiftPassed},{cancelled},{lab.CancelledWhileSupported},{lab.CancellationTick},{lab.RecoveryTouchdownTick},{lab.ControlledReturn},{complete},{lab.FirstFallTick},{bodyContacts},{lab.SwingClearance:F6},{lab.SwingTravel:F6},{peakEffort:F6},{saturationTicks},{work:F6},{minimumMargin:F6},{minimumContact:F3},{lab.FootContactMask == 3},{us:F2},{lab.AbortReason}");
        summary.WriteLine(row);
        summary.Flush();
        Debug.Log("C13_TRIAL " + row);
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
