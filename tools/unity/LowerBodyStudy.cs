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
internal static class LowerBodyStudy
{
    const string Key = "Dreamer.LowerBodyStudy";
    const int SettlingTicks = 40;
    const int StandingTicks = 120;
    const int TransferTicks = 80;

    static LowerBodyStudy()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(0);
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            RunTrials();
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static void RunTrials()
    {
        using var report = File.CreateText(Path.Combine(Application.persistentDataPath, "phase-c1-load-summary.csv"));
        using var trace = File.CreateText(Path.Combine(Application.persistentDataPath, "phase-c1-load-trace.csv"));
        report.WriteLine("seed,stage,condition,pass,mean_left_share,analytic_left_share,bilateral_fraction,mean_vertical_ns,rms_momentum_residual_ns,max_height_error_m,max_tilt_deg,min_knee_rad,max_knee_rad,max_joint_overshoot_rad,max_slip_m,max_penetration_m,saturation_fraction,non_foot_contacts,mean_abs_work_j,native_us");
        trace.Write("seed,stage,condition,tick,settled,ballast_x_m,left_x_ns,left_y_ns,left_z_ns,right_x_ns,right_y_ns,right_z_ns,com_x_m,com_y_m,com_z_m,com_vx_m_s,com_vy_m_s,com_vz_m_s,pelvis_x_m,pelvis_y_m,pelvis_z_m,pelvis_vx_m_s,pelvis_vy_m_s,pelvis_vz_m_s,delta_px,delta_py,delta_pz,residual_ns,drift_m,penetration_m,signed_work_j,absolute_work_j");
        for (int j = 0; j < 6; j++) trace.Write($",joint{j}_target_rad,joint{j}_angle_rad,joint{j}_torque_nm");
        trace.WriteLine();
        bool transferVerified = false;
        foreach (var configuration in new[] { (Seed: 101, Stage: 6f), (Seed: 202, Stage: 6f), (Seed: 303, Stage: 3f),
            (Seed: 404, Stage: 6f), (Seed: 505, Stage: 3f) })
        {
            using var lab = new NativeLowerBodyLab(configuration.Seed, configuration.Stage);
            bool stand = Measure(lab, "standing", 0, StandingTicks, trace, report, out float neutral);
            if (!stand)
            {
                Debug.Log($"LOWER_BODY_GATE {lab.Seed} standing failed; transfer and step not run");
                continue;
            }
            bool left = Measure(lab, "left", -1, TransferTicks, trace, report, out float leftShare, neutral);
            bool middle = Measure(lab, "recenter", 0, TransferTicks, trace, report, out _, neutral);
            bool right = Measure(lab, "right", 1, TransferTicks, trace, report, out float rightShare, neutral);
            bool ready = left && middle && right && Mathf.Max(leftShare, 1f - rightShare) >= .85f;
            transferVerified |= left && middle && right;
            Debug.Log(FormattableString.Invariant($"LOWER_BODY_GATE {lab.Seed} transfer={left && middle && right} strongest_stance={Mathf.Max(leftShare, 1f - rightShare):F4} step_ready={ready}"));
        }
        Repeatability();
        if (transferVerified) Perturbations();
    }

    static bool Measure(NativeLowerBodyLab lab, string phase, float load, int count, StreamWriter trace,
        StreamWriter report, out float leftShare, float neutral = .5f)
    {
        lab.RequestLoad(load);
        float left = 0, right = 0, analytical = 0, errorSquared = 0, height = 0, tilt = 0;
        float slip = 0, penetration = 0, overshoot = 0, work = 0, minKnee = float.PositiveInfinity, maxKnee = 0;
        int both = 0, saturated = 0, badContact = 0;
        long elapsed = 0;
        for (int i = 0; i < SettlingTicks + count; i++)
        {
            long start = Stopwatch.GetTimestamp();
            lab.Step();
            elapsed += Stopwatch.GetTimestamp() - start;
            Vector3 l = lab.LeftImpulse, r = lab.RightImpulse, com = lab.CenterOfMass, p = lab.PelvisPosition;
            Vector3 v = lab.PelvisVelocity, cv = lab.Momentum / NativeLowerBodyLab.TotalMassKg, dp = lab.MomentumDelta;
            bool settled = i >= SettlingTicks;
            trace.Write(FormattableString.Invariant($"{lab.Seed},{lab.Stage},{phase},{lab.Tick},{settled},{lab.BallastOffset:R},{l.x:R},{l.y:R},{l.z:R},{r.x:R},{r.y:R},{r.z:R},{com.x:R},{com.y:R},{com.z:R},{cv.x:R},{cv.y:R},{cv.z:R},{p.x:R},{p.y:R},{p.z:R},{v.x:R},{v.y:R},{v.z:R},{dp.x:R},{dp.y:R},{dp.z:R},{lab.MomentumResidual.magnitude:R},{lab.FootDrift:R},{lab.MaximumPenetration:R},{lab.SignedWork:R},{lab.AbsoluteWork:R}"));
            for (int j = 0; j < lab.JointCount; j++)
            {
                var joint = lab.ReadJoint(j);
                trace.Write(FormattableString.Invariant($",{joint.Target:R},{joint.Angle:R},{joint.Torque:R}"));
            }
            trace.WriteLine();
            if (!settled) continue;
            left += l.y;
            right += r.y;
            Vector3 center = (lab.FootBounds(0).center + lab.FootBounds(1).center) * .5f;
            var reference = LowerBodyReference.Evaluate(lab.Dimensions, 0f, false, com.x - center.x,
                com.z - center.z + lab.Dimensions.Foot * .24f, NativeLowerBodyLab.TotalMassKg);
            analytical += reference.LeftVerticalNewtons / (NativeLowerBodyLab.TotalMassKg * 9.80665f);
            both += lab.FootContactMask == 3 ? 1 : 0;
            saturated += lab.Saturated ? 1 : 0;
            badContact += lab.NonFootContacts;
            errorSquared += lab.MomentumResidual.sqrMagnitude;
            height = Mathf.Max(height, Mathf.Abs(p.y - lab.NominalPelvisHeight));
            tilt = Mathf.Max(tilt, lab.PelvisTilt);
            slip = Mathf.Max(slip, lab.FootDrift);
            penetration = Mathf.Max(penetration, lab.MaximumPenetration);
            overshoot = Mathf.Max(overshoot, lab.JointLimitOvershoot);
            minKnee = Mathf.Min(minKnee, Mathf.Min(lab.ReadJoint(1).Angle, lab.ReadJoint(4).Angle));
            maxKnee = Mathf.Max(maxKnee, Mathf.Max(lab.ReadJoint(1).Angle, lab.ReadJoint(4).Angle));
            work += lab.AbsoluteWork;
        }
        leftShare = left + right > 0 ? left / (left + right) : 0f;
        float rms = Mathf.Sqrt(errorSquared / count), simulatedUs = (float)(elapsed * 1e6 / Stopwatch.Frequency / (SettlingTicks + count));
        float shareTarget = analytical / count;
        bool support = both >= .95f * count && badContact == 0 && rms <= .15f && height <= .025f && tilt <= 8f &&
            slip <= .020f && penetration <= .008f && overshoot <= .025f && saturated <= .05f * count &&
            minKnee > 0 && maxKnee <= .30f && simulatedUs <= 4000f;
        bool loadGate = Mathf.Abs(leftShare - shareTarget) <= .12f &&
            (load == 0 ? Mathf.Abs(leftShare - .5f) <= .10f : (leftShare - neutral) * -load >= .12f);
        bool pass = support && loadGate;
        string summary = FormattableString.Invariant($"{lab.Seed},{lab.Stage},{phase},{pass},{leftShare:F6},{shareTarget:F6},{both / (float)count:F4},{(left + right) / count:F6},{rms:F6},{height:F6},{tilt:F4},{minKnee:F6},{maxKnee:F6},{overshoot:F6},{slip:F6},{penetration:F6},{saturated / (float)count:F4},{badContact},{work / count:F6},{simulatedUs:F2}");
        report.WriteLine(summary);
        Debug.Log("LOWER_BODY_SAMPLE " + summary);
        return pass;
    }

    static void Repeatability()
    {
        using var a = new NativeLowerBodyLab(101, 6);
        using var b = new NativeLowerBodyLab(101, 6);
        var reference = new (Vector3 Pelvis, Vector3 Left, Vector3 Right)[200];
        for (int i = 0; i < reference.Length; i++)
        {
            if (i == 80) a.RequestLoad(-1);
            a.Step();
            reference[i] = (a.PelvisPosition, a.LeftImpulse, a.RightImpulse);
        }
        var clock = new TimeGovernor { TargetScale = 4f };
        float pelvis = 0, impulse = 0;
        for (int frame = 0; frame < 50; frame++)
        {
            int ticks = clock.Plan(WorldSimulation.Dt, WorldSimulation.Dt, 8);
            if (ticks != 4) throw new Exception("A 4x schedule should deliver four fixed ticks per 0.05 s frame.");
            for (int tick = 0; tick < ticks; tick++)
            {
                int i = b.Tick;
                if (i == 80) b.RequestLoad(-1);
                b.Step();
                for (int j = 0; j < b.JointCount; j++) b.ReadJoint(j);
                pelvis = Mathf.Max(pelvis, Vector3.Distance(reference[i].Pelvis, b.PelvisPosition));
                impulse = Mathf.Max(impulse, Mathf.Max((reference[i].Left - b.LeftImpulse).magnitude,
                    (reference[i].Right - b.RightImpulse).magnitude));
            }
        }
        if (pelvis > .001f || impulse > .08f) throw new Exception("Lower-body controlled repeatability failed.");
        Debug.Log(FormattableString.Invariant($"LOWER_BODY_REPEAT pelvis_m={pelvis:R} impulse_ns={impulse:R} ticks={b.Tick} observed_schedule=4x"));
    }

    static void Perturbations()
    {
        using var report = File.CreateText(Path.Combine(Application.persistentDataPath, "phase-c1-perturbations.csv"));
        using var trace = File.CreateText(Path.Combine(Application.persistentDataPath, "phase-c1-perturbation-trace.csv"));
        report.WriteLine("case,static_friction,disabled_drive_mask,impulse_x_ns,impulse_z_ns,peak_tilt_deg,lowest_pelvis_m,max_slip_m,non_bilateral_ticks,non_foot_contacts,peak_body_impulse_ns");
        trace.WriteLine("case,tick,disabled_drive_mask,left_ns,right_ns,other_x_ns,other_y_ns,other_z_ns,pelvis_height_m,tilt_deg,slip_m,non_foot_contacts");
        foreach (var trial in new[]
        {
            (Name: "lateral", Impulse: Vector3.right * .35f, Friction: .85f, Disabled: 0u),
            (Name: "forward", Impulse: Vector3.forward * .35f, Friction: .85f, Disabled: 0u),
            (Name: "backward", Impulse: Vector3.back * .35f, Friction: .85f, Disabled: 0u),
            (Name: "large-lateral", Impulse: Vector3.right * 4f, Friction: .85f, Disabled: 0u),
            (Name: "left-knee-off", Impulse: Vector3.forward * .35f, Friction: .85f, Disabled: 1u << 1),
            (Name: "drives-off", Impulse: Vector3.forward * .35f, Friction: .85f, Disabled: 0x3fu),
            (Name: "low-friction", Impulse: Vector3.forward * 1f, Friction: .10f, Disabled: 0u)
        })
        {
            using var lab = new NativeLowerBodyLab(101, 6, trial.Friction);
            for (int i = 0; i < 60; i++) lab.Step();
            if (lab.FootContactMask != 3 || lab.PelvisTilt > 8)
                throw new Exception("Perturbation fixture failed to establish bilateral support.");
            lab.DisabledDrives = trial.Disabled;
            float tilt = 0, height = float.MaxValue, slip = 0, impact = 0;
            int unsupported = 0, bodyContacts = 0;
            for (int i = 0; i < 100; i++)
            {
                lab.Step(i == 0 ? trial.Impulse : Vector3.zero);
                tilt = Mathf.Max(tilt, lab.PelvisTilt);
                height = Mathf.Min(height, lab.PelvisPosition.y);
                slip = Mathf.Max(slip, lab.FootDrift);
                unsupported += lab.FootContactMask == 3 ? 0 : 1;
                bodyContacts += lab.NonFootContacts;
                Vector3 other = lab.OtherImpulse;
                impact = Mathf.Max(impact, other.magnitude);
                trace.WriteLine(FormattableString.Invariant($"{trial.Name},{lab.Tick},{trial.Disabled},{lab.LeftImpulse.y:R},{lab.RightImpulse.y:R},{other.x:R},{other.y:R},{other.z:R},{lab.PelvisPosition.y:R},{lab.PelvisTilt:R},{lab.FootDrift:R},{lab.NonFootContacts}"));
            }
            string metrics = FormattableString.Invariant($"{trial.Name},{trial.Friction},{trial.Disabled},{trial.Impulse.x},{trial.Impulse.z},{tilt:F4},{height:F6},{slip:F6},{unsupported},{bodyContacts},{impact:F6}");
            report.WriteLine(metrics);
            Debug.Log("LOWER_BODY_PERTURBATION " + metrics);
        }
    }
}
