using System;
using System.Diagnostics;
using System.IO;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class PreliftStabilizationStudy
{
    const string Key = "Dreamer.C14";
    static PreliftStabilizationStudy()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Development() => Start(1);
    public static void HeldOut() => Start(2);

    static void Start(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetInt(Key, mode);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseInt(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Run(SessionState.GetInt(Key, 0)); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Run(int mode)
    {
        string cohort = mode == 1 ? "development" : "heldout";
        string directory = Application.persistentDataPath;
        using var summary = File.CreateText(Path.Combine(directory, $"phase-c14-{cohort}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(directory, $"phase-c14-{cohort}-trace.csv"));
        summary.WriteLine("seed,age,side,policy,step_ms,perturbation_ns,initial_support,initial_left_ns,initial_right_ns,initial_tilt,active_tick,cancel_tick,recovered,recovered_tick,recovered_duration_ticks,swing_started,airborne,held_lift,touchdown_tick,completed_step,fall_tick,body_contact_ticks,peak_clearance_mm,advance_mm,work_j,peak_effort,saturation_ticks,mean_native_us");
        LowerBodyTrace.WriteHeader(trace, dynamics: true, recovery: true);
        var configurations = mode == 1 ? new[] { (1010, 6f, 0f), (303, 3f, 0f) } :
            new[] { (1901, 6f, .75f), (2383, 3f, .95f), (3347, 6f, 1.05f), (4021, 3f, .85f) };
        foreach (var (seed, age, perturbation) in configurations)
        for (int side = 0; side < 2; side++)
        foreach (var policy in new[] { LowerBodyRecovery.Combined, LowerBodyRecovery.EarlyCancellation,
            LowerBodyRecovery.ActivePrelift, LowerBodyRecovery.ActivePreliftAndLanding })
            Trial(seed, age, perturbation, side, policy, summary, trace);
        Debug.Log("C14_COMPLETE " + cohort);
    }

    static void Trial(int seed, float age, float perturbation, int side, LowerBodyRecovery policy, TextWriter summary, TextWriter trace)
    {
        using var lab = new NativeLowerBodyLab(seed, age, controller: LowerBodyController.FrontalPose,
            stepProtocol: LowerBodyStepProtocol.Prepared, recoveryPolicy: policy);
        for (int i = 0; i < 180; i++) lab.Step();
        float left = lab.LeftImpulse.y, right = lab.RightImpulse.y, initialTilt = lab.PelvisTilt;
        bool admitted = lab.RequestPreparedStep(side);
        int stop = lab.Tick + 340, started = lab.Tick, saturation = 0, bodyContacts = 0, recovered = -1;
        bool tapped = false, airborne = false;
        float peak = 0f, work = 0f;
        long cost = 0;
        while (admitted && lab.Tick < stop)
        {
            bool perturb = !tapped && perturbation > 0f && lab.MotionPhase == StepPhase.Prelift &&
                lab.Tick - lab.PhaseStartedTick == 8;
            var last = lab.MotionPhase;
            long start = Stopwatch.GetTimestamp();
            lab.Step(perturb ? Vector3.right * ((2 * side - 1) * perturbation) : Vector3.zero);
            cost += Stopwatch.GetTimestamp() - start;
            tapped |= perturb;
            work += lab.AbsoluteWork;
            peak = Mathf.Max(peak, lab.PeakEffortFraction);
            if (lab.Saturated) saturation++;
            if (lab.NonFootContacts > 0) bodyContacts++;
            var foot = lab.ReadSole(1 - side);
            airborne |= lab.SwingStartedTick >= 0 && !foot.Contact && foot.Clearance > .001f;
            if (last != lab.MotionPhase)
            {
                if (lab.MotionPhase == StepPhase.Stabilize && lab.SwingStartedTick >= 0)
                    throw new InvalidOperationException("Prelift response followed swing.");
                if (lab.MotionPhase == StepPhase.Returned) recovered = lab.Tick;
                if (lab.MotionPhase == StepPhase.Fell || lab.MotionPhase == StepPhase.Returned ||
                    lab.MotionPhase == StepPhase.Rejected || lab.MotionPhase == StepPhase.Complete)
                    stop = Math.Min(stop, lab.Tick + 50);
            }
            LowerBodyTrace.WriteRow(trace, lab, "prelift-study", dynamics: true, recovery: true);
        }
        bool complete = lab.MotionPhase == StepPhase.Complete;
        if (complete && (!lab.DynamicGatePassed || !lab.ForwardPassed || !lab.LiftPassed || lab.FirstFallTick >= 0))
            throw new InvalidOperationException("Step skipped original gates.");
        if (lab.ControlledReturn && (lab.FootContactMask != 3 || lab.FirstFallTick >= 0))
            throw new InvalidOperationException("Recovery lost stable support.");
        int duration = recovered < 0 || lab.CancellationTick < 0 ? -1 : recovered - lab.CancellationTick;
        string row = FormattableString.Invariant($"{seed},{age},{side},{policy},{lab.PhysicsStepSeconds * 1000f:F2},{(tapped ? perturbation : 0):F3},{admitted},{left:F5},{right:F5},{initialTilt:F4},{lab.StabilizationTick},{lab.CancellationTick},{lab.ControlledReturn},{recovered},{duration},{lab.SwingStartedTick},{airborne},{lab.LiftPassed},{lab.RecoveryTouchdownTick},{complete},{lab.FirstFallTick},{bodyContacts},{lab.SwingClearance * 1000f:F3},{lab.SwingTravel * 1000f:F3},{work:F4},{peak:F4},{saturation},{cost * 1e6 / Stopwatch.Frequency / Math.Max(1, lab.Tick - started):F2}");
        summary.WriteLine(row);
        summary.Flush();
        Debug.Log("C14_TRIAL " + row);
    }
}
