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
internal static class LowerBodyLateralStudy
{
    const string Key = "Dreamer.LowerBodyLateralStudy";
    const int Samples = 40;
    static readonly float[] Targets = { .60f, .70f, .80f, .85f, .90f };

    static LowerBodyLateralStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Development() => Start("development");
    public static void Final() => Start("heldout");

    static void Start(string mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        SessionState.SetString(Key, mode);
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
        try { Study(SessionState.GetString(Key, "")); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Study(string mode)
    {
        ValidateSensors();
        VerifyClockAndIsolation();
        bool heldout = mode == "heldout";
        var seeds = heldout ? new[] { 606, 707, 808 } : new[] { 101, 202, 303 };
        using var summary = File.CreateText(Path.Combine(Application.persistentDataPath, $"phase-c11-{mode}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(Application.persistentDataPath, $"phase-c11-{mode}-trace.csv"));
        summary.WriteLine("seed,stage,controller,stance,target,stable_samples,target_samples,mean_share,min_share,max_share,cop_valid,peak_tilt_deg,peak_drift_m,max_height_error_m,min_knee,max_knee,overshoot_rad,penetration_m,saturation_ticks,body_contacts,rms_residual_ns,mean_abs_work_j,us_per_tick,passed,swing");
        LowerBodyTrace.WriteHeader(trace);
        foreach (int seed in seeds)
        foreach (var controller in new[] { LowerBodyController.Sagittal, LowerBodyController.FrontalPose, LowerBodyController.SensorFeedback })
        for (int side = 0; side < 2; side++)
        {
            float stage = seed == 303 || seed == 808 ? 3f : 6f;
            using (var neutral = new NativeLowerBodyLab(seed, stage, controller: controller))
            {
                if (neutral.TryRequestSwing() || neutral.Action != SwingOutcome.GateBlocked) throw new Exception("Unsupported swing passed its interlock.");
                Measure(neutral, side, .5f, 120, 40, summary, trace, false);
            }
            bool swung = false;
            foreach (float target in Targets)
            {
                using var candidate = new NativeLowerBodyLab(seed, stage, controller: controller);
                for (int tick = 0; tick < 180; tick++) candidate.Step();
                if (Measure(candidate, side, target, Samples, 60, summary, trace, heldout && !swung)) swung = true;
            }
            if (heldout) Perturb(seed, stage, controller, side, trace);
        }
        Debug.Log($"C11_STUDY completed {mode}");
    }

    static bool Measure(NativeLowerBodyLab lab, int side, float target, int samples, int settle, TextWriter summary, TextWriter trace, bool attempt)
    {
        lab.RequestLoad((side == 0 ? -1 : 1) * (target - .5f) / .4f);
        float sum = 0, min = 1, max = 0, tilt = 0, drift = 0, height = 0, kneeMin = 10, kneeMax = 0;
        float overshoot = 0, penetration = 0, work = 0, error = 0;
        int supporting = 0, targetTicks = 0, pressure = 0, saturation = 0, body = 0;
        long elapsed = 0;
        for (int i = 0; i < settle + samples; i++)
        {
            long start = Stopwatch.GetTimestamp();
            lab.Step();
            elapsed += Stopwatch.GetTimestamp() - start;
            if (lab.Vestibular.Tick != lab.Tick) throw new Exception("Vestibular and physical tick diverged.");
            if (i < settle) continue;
            LowerBodyTrace.WriteRow(trace, lab, target == .5f ? "neutral" : "transfer");
            float share = lab.ReadLoad(side);
            sum += share; min = Mathf.Min(min, share); max = Mathf.Max(max, share);
            pressure += lab.ReadSole(side).PressureValid ? 1 : 0;
            supporting += lab.ReadSole(side).Contact && (target > .5f || lab.FootContactMask == 3) ? 1 : 0;
            targetTicks += share >= target ? 1 : 0;
            tilt = Mathf.Max(tilt, lab.PelvisTilt);
            drift = Mathf.Max(drift, target == .5f ? lab.FootDrift : lab.ReadSole(side).Drift);
            height = Mathf.Max(height, Mathf.Abs(lab.PelvisPosition.y - lab.NominalPelvisHeight));
            kneeMin = Mathf.Min(kneeMin, Mathf.Min(lab.ReadJoint(1).Angle, lab.ReadJoint(4).Angle));
            kneeMax = Mathf.Max(kneeMax, Mathf.Max(lab.ReadJoint(1).Angle, lab.ReadJoint(4).Angle));
            overshoot = Mathf.Max(overshoot, lab.JointLimitOvershoot);
            penetration = Mathf.Max(penetration, lab.MaximumPenetration);
            saturation += lab.Saturated ? 1 : 0;
            body += lab.NonFootContacts;
            work += lab.AbsoluteWork;
            error += lab.MomentumResidual.sqrMagnitude;
            for (int joint = 0; joint < 6; joint++)
            {
                var p = lab.ReadProprioception(joint);
                if (Mathf.Abs(p.RollTorque) > (joint % 3 == 0 ? 16 : 12) + .001f || Mathf.Abs(p.RollTarget) > p.RollLimit + .001f)
                    throw new Exception("Lateral effort or target escaped its configured limit.");
            }
        }
        float rms = Mathf.Sqrt(error / samples), mean = sum / samples;
        double us = elapsed * 1e6 / Stopwatch.Frequency / (settle + samples);
        bool stable = supporting >= (target == .5f ? .95f : 1f) * samples && body == 0 && tilt <= 8 && drift <= .020f &&
            height <= .025f && kneeMin > 0f && kneeMax <= .30f && overshoot <= .025f && penetration <= .008f &&
            saturation <= .05f * samples && rms <= .15f;
        bool passed = stable && (target == .5f ? Mathf.Abs(mean - .5f) <= .10f : targetTicks >= .9f * samples && mean >= target);
        bool swung = attempt && target >= .85f && passed && lab.TryRequestSwing();
        if (swung)
        {
            for (int i = 0; i < 60 && (lab.Action == SwingOutcome.Swing || lab.Action == SwingOutcome.Recontact); i++)
            {
                lab.Step();
                LowerBodyTrace.WriteRow(trace, lab, "swing");
            }
            int bilateralAfter = 0;
            const int continuationTicks = 40;
            for (int i = 0; i < continuationTicks; i++)
            {
                lab.Step();
                bilateralAfter += lab.FootContactMask == 3 && lab.NonFootContacts == 0 ? 1 : 0;
                LowerBodyTrace.WriteRow(trace, lab, "after-swing");
            }
            Debug.Log(FormattableString.Invariant($"C11_SWING {lab.Seed},{lab.Controller},{side},{lab.Action},{lab.ActionReason},{lab.SwingClearance:F6},{lab.SwingTravel:F6},{bilateralAfter},{lab.FirstFallTick}"));
            if (lab.Action == SwingOutcome.Complete && (lab.SwingClearance < .01f || lab.SwingTravel < .08f))
                throw new Exception("The native outcome failed its physical clearance or displacement contract.");
        }
        string row = FormattableString.Invariant($"{lab.Seed},{lab.Stage},{lab.Controller},{side},{target:F2},{supporting},{targetTicks},{mean:F5},{min:F5},{max:F5},{pressure},{tilt:F5},{drift:F6},{height:F6},{kneeMin:F5},{kneeMax:F5},{overshoot:F6},{penetration:F6},{saturation},{body},{rms:F6},{work / samples:F6},{us:F2},{passed},{(swung ? lab.Action.ToString() : "Unattempted")}");
        summary.WriteLine(row);
        Debug.Log("C11_LOAD " + row);
        return swung;
    }

    static void Perturb(int seed, float stage, LowerBodyController controller, int side, TextWriter trace)
    {
        using var lab = new NativeLowerBodyLab(seed, stage, controller: controller);
        for (int i = 0; i < 160; i++) lab.Step();
        lab.RequestLoad(side == 0 ? -.5f : .5f);
        for (int i = 0; i < 100; i++) lab.Step();
        Vector3 impulse = Vector3.right * (side == 0 ? -.35f : .35f) * (lab.Seed == 707 ? -1 : 1);
        float maxTilt = 0, maxDrift = 0;
        int contacts = 0;
        for (int i = 0; i < 80; i++)
        {
            lab.Step(i == 0 ? impulse : Vector3.zero);
            LowerBodyTrace.WriteRow(trace, lab, "perturbation");
            maxTilt = Mathf.Max(maxTilt, lab.PelvisTilt);
            maxDrift = Mathf.Max(maxDrift, lab.ReadSole(side).Drift);
            contacts += lab.NonFootContacts;
        }
        Debug.Log(FormattableString.Invariant($"C11_TAP {lab.Seed},{lab.Controller},{side},{impulse.x:F2},{maxTilt:F5},{maxDrift:F6},{contacts}"));
    }

    static void ValidateSensors()
    {
        var sensor = new VestibularReading(20, Quaternion.AngleAxis(90f, Vector3.forward), Vector3.down * 10f,
            Vector3.up, Vector3.right);
        if (sensor.Tick != 20 || (sensor.Gravity - Vector3.left * 10f).magnitude > .0001f ||
            (sensor.AngularVelocity - Vector3.right).magnitude > .0001f || (sensor.LinearAcceleration - Vector3.down).magnitude > .0001f)
            throw new Exception("Vestibular body-frame transform failed.");
        using var noisy = new NativeLowerBodyLab(101, 6f, vestibularNoise: 1f);
        noisy.Step();
        var sample = noisy.Vestibular;
        Vector3 exact = Quaternion.Inverse(sample.Orientation) * Physics.gravity;
        if (sample.Tick != noisy.Tick || (sample.Gravity - exact).magnitude > sample.GravityErrorBound + .00001f)
            throw new Exception("Vestibular noise escaped its advertised bound.");
    }

    static void VerifyClockAndIsolation()
    {
        using var control = new NativeLowerBodyLab(101, 6f, controller: LowerBodyController.FrontalPose);
        using var observed = new NativeLowerBodyLab(101, 6f, controller: LowerBodyController.FrontalPose);
        var samples = new (Vector3 Pelvis, Vector3 Left, Vector3 Right)[200];
        for (int i = 0; i < samples.Length; i++)
        {
            if (i == 160) control.RequestLoad(-.875f);
            control.Step();
            samples[i] = (control.PelvisPosition, control.LeftImpulse, control.RightImpulse);
        }
        if (observed.Tick != 0 || observed.Vestibular.Tick != 0 || observed.ReadSole(0).Contact)
            throw new Exception("One isolated physics scene advanced another.");
        var clock = new TimeGovernor { TargetScale = 4f };
        float displacement = 0, impulse = 0;
        for (int frame = 0; frame < 50; frame++)
        {
            int steps = clock.Plan(WorldSimulation.Dt, WorldSimulation.Dt, 8);
            if (steps != 4) throw new Exception("Fixed-step accelerated schedule drifted.");
            for (int step = 0; step < steps; step++)
            {
                int tick = observed.Tick;
                if (tick == 160) observed.RequestLoad(-.875f);
                observed.Step();
                _ = LowerBodyTrace.Capture(observed, "observation");
                displacement = Mathf.Max(displacement, (observed.PelvisPosition - samples[tick].Pelvis).magnitude);
                impulse = Mathf.Max(impulse, (observed.LeftImpulse - samples[tick].Left).magnitude, (observed.RightImpulse - samples[tick].Right).magnitude);
            }
        }
        if (displacement > .001f || impulse > .08f) throw new Exception("Observed fixed-step replay exceeded tolerance.");
        Debug.Log(FormattableString.Invariant($"C11_CLOCK {observed.Tick} observed=4x pelvis_m={displacement:R} impulse_ns={impulse:R}"));
    }
}
