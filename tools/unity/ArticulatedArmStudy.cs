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
internal static class ArticulatedArmStudy
{
    const string Key = "Dreamer.H6.ArmBench";
    readonly struct ReachCase
    {
        public readonly string Id;
        public readonly Vector3 Target;
        public readonly float Yaw, Age, Load;
        public readonly bool Obstruction;
        public readonly int Seed;
        public ReachCase(string id, float x, float y, float z, float yaw = 0, bool obstruction = false, int seed = 303,
            float age = 3f, float load = 0f)
        { Id = id; Target = new Vector3(x, y, z); Yaw = yaw; Obstruction = obstruction; Seed = seed; Age = age; Load = load; }
    }

    static readonly ReachCase[] Development =
    {
        new ReachCase("dev-pickup", 0, .13f, -.29f),
        new ReachCase("dev-side", .14f, .25f, -.31f, 15),
        new ReachCase("dev-cross", -.28f, .34f, -.30f),
        new ReachCase("dev-waist", -.07f, .24f, -.45f),
        new ReachCase("dev-obstruction", .01f, .31f, -.20f, obstruction: true),
        new ReachCase("dev-far", -.48f, .21f, -.19f),
    };
    static readonly ReachCase[] HeldOut =
    {
        new ReachCase("near-centre", .025f, .18f, -.273f),
        new ReachCase("reach-high", .025f, .33f, -.273f),
        new ReachCase("natural-plane", .12f, .215f, -.295f),
        new ReachCase("natural-plane-loaded", .12f, .215f, -.295f, load: .62f),
        new ReachCase("natural-plane-turn", .12f, .215f, -.295f, yaw: -23f),
        new ReachCase("natural-plane-blocked", .12f, .215f, -.295f, obstruction: true),
        new ReachCase("opposite-waist", -.13f, .20f, -.423f),
        new ReachCase("opposite-side", -.24f, .325f, -.278f),
        new ReachCase("outside-envelope", -.514f, .315f, -.203f),
        new ReachCase("alice-reach", .12f, .215f, -.295f, seed: 404),
        new ReachCase("alice-shorter-calibration", .12f, .215f, -.295f, seed: 404, age: 2.55f),
    };

    static ArticulatedArmStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Develop() => Start("development");
    public static void Final() => Start("heldout");

    static void Start(string name)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetString(Key, name);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseString(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Evaluate(SessionState.GetString(Key, "")); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Evaluate(string cohort)
    {
        string prefix = Path.Combine(Application.persistentDataPath, "phase-h6-" + cohort);
        using var rows = File.CreateText(prefix + "-reaches.csv");
        using var trace = File.CreateText(prefix + "-reach-trace.csv");
        rows.WriteLine("cohort,case,seed,age,pole,blocked,yaw_deg,wrist_load_n,target_m,initial_error_mm,end_error_mm,wrist_orientation_error_deg,overshoot_mm,undershoot_mm,completed_tick,geometric_envelope,torso_s,torso_impulse_ns,torso_depth_mm,upper_fore_hand_contact_s,obstacle_s,upper_distal_s,corrections,elbow_limit_excursions,peak_torque_nm,work_j,controller_us,physics_us");
        trace.WriteLine("cohort,case,pole,tick,wrist_load_n,target_m,proposed_elbow_m,elbow_m,wrist_m,joint_degrees,joint_rad_s,torque_nm,torso_s,torso_depth_mm,obstacle_s,upper_distal_s,corrections");
        foreach (var test in cohort == "development" ? Development : HeldOut)
        {
            Reach(cohort, test, false, rows, trace);
            Reach(cohort, test, true, rows, trace);
        }
        using var transfers = File.CreateText(prefix + "-transfer.csv");
        using var transferTrace = File.CreateText(prefix + "-transfer-trace.csv");
        transfers.WriteLine("cohort,case,seed,articulated,pole,disturbance_n,applied_ticks,ticks,phase,outcome,opposed,lift,held_ticks,placement,settled,ball_m,peak_pad_n,slip_ticks,torso_s,torso_depth_mm,upper_distal_s,elbow_limit_excursions,corrections,actuator_work_j,arm_work_j,controller_us,physics_us");
        transferTrace.WriteLine("cohort,case,articulated,tick,phase,load_n,target_m,elbow_m,wrist_m,ball_m,clearance_m,left_n,right_n,support_n,actuator_torque_nm,joint_degrees,torso_s,torso_depth_mm");
        foreach (bool articulated in new[] { false, true })
        {
            Transfer(cohort, "arthur-reference", 303, articulated, false, 0f, transfers, transferTrace);
            if (cohort != "heldout") continue;
            Transfer(cohort, "arthur-load", 303, articulated, false, .30f, transfers, transferTrace);
            Transfer(cohort, "alice-reference", 404, articulated, false, 0f, transfers, transferTrace);
        }
        Transfer(cohort, "arthur-alternate", 303, true, true, 0f, transfers, transferTrace);
        if (cohort == "heldout")
        {
            Transfer(cohort, "arthur-alternate-load", 303, true, true, .30f, transfers, transferTrace);
            Transfer(cohort, "alice-alternate", 404, true, true, 0f, transfers, transferTrace);
        }
        Debug.Log($"H6_ARM_COMPLETE cohort={cohort}");
    }

    static NativeHandLab Build(int seed, bool arm, ArmBenchSetup setup, bool park = false) =>
        new NativeHandLab(seed, HouseholdLife.BallMass, HouseholdLife.BallRadius, HouseholdLife.BallFriction, 3.2f,
            placement: true, household: new HouseholdHandSetup(new Vector3(-.56f, 0, 1.88f),
                park ? new Vector3(.56f,.125f,.32f) : new Vector3(0,.125f,0), Quaternion.identity,
                new Vector3(-.27f,.05f,0), new Vector3(-.27f,.05f,0), 0), articulated: arm ? setup : null);

    static void Reach(string cohort, ReachCase test, bool high, TextWriter rows, TextWriter trace)
    {
        using var hand = Build(test.Seed, true, new ArmBenchSetup(high, obstruction: test.Obstruction, developmentDays: test.Age), true);
        hand.Profile = true;
        var start = hand.PalmPosition;
        Vector3 desired = start;
        Vector3 direction = (test.Target - start).normalized;
        Vector3 peakTorque = default;
        float overshoot = 0;
        int entered = -1, stable = 0;
        for (int i = 0; i < 80; i++)
        {
            desired = Vector3.MoveTowards(desired, test.Target, .018f);
            float appliedLoad = i >= 29 && i < 48 ? test.Load : 0f;
            hand.StepReachProbe(desired, test.Yaw, appliedLoad);
            var a = hand.ReadArm();
            peakTorque = Vector3.Max(peakTorque, a.ActuatorTorque);
            overshoot = Mathf.Max(overshoot, Vector3.Dot(a.Wrist - test.Target, direction));
            bool close = Vector3.Distance(a.Wrist, test.Target) <= .028f && (desired - test.Target).sqrMagnitude < 1e-8f;
            stable = close ? stable + 1 : 0;
            if (stable >= 5 && entered < 0) entered = hand.Tick;
            trace.WriteLine(FormattableString.Invariant($"{cohort},{test.Id},{high},{hand.Tick},{appliedLoad:F3},{Vec(desired)},{Vec(a.ProposedElbow)},{Vec(a.Elbow)},{Vec(a.Wrist)},{Vec(a.JointDegrees)},{Vec(a.JointSpeed)},{Vec(a.ActuatorTorque)},{a.TorsoSeconds:F3},{1000 * a.TorsoDepth:F3},{a.ObstacleSeconds:F4},{a.UpperDistalSeconds:F4},{a.Corrections}"));
        }
        var final = hand.ReadArm();
        float error = Vector3.Distance(final.Wrist, test.Target);
        float undershoot = Mathf.Max(0, Vector3.Dot(test.Target - final.Wrist, direction));
        double scale = 1e6 / Stopwatch.Frequency / hand.Tick;
        float wristError = Quaternion.Angle(hand.WristRotation, Quaternion.Euler(0,test.Yaw,0));
        string row = FormattableString.Invariant($"{cohort},{test.Id},{test.Seed},{test.Age:F2},{(high ? "high" : "lateral")},{test.Obstruction},{test.Yaw:F1},{test.Load:F3},{Vec(test.Target)},{1000 * Vector3.Distance(start,test.Target):F3},{1000 * error:F3},{wristError:F3},{1000 * overshoot:F3},{1000 * undershoot:F3},{entered},{final.WithinEnvelope},{final.TorsoSeconds:F4},{final.TorsoImpulse:F6},{1000 * final.TorsoDepth:F4},{Vec(hand.ArmTorsoContactSeconds)},{final.ObstacleSeconds:F4},{final.UpperDistalSeconds:F4},{final.Corrections},{final.LimitExcursions},{Vec(peakTorque)},{final.UpperWork+final.ElbowWork+final.WristWork:F6},{hand.CommandCpuTicks*scale:F2},{hand.PhysicsCpuTicks*scale:F2}");
        rows.WriteLine(row);
        Debug.Log("H6_REACH " + row);
    }

    static void Transfer(string cohort, string id, int seed, bool arm, bool high, float load, TextWriter rows, TextWriter trace)
    {
        using var hand = Build(seed, arm, new ArmBenchSetup(high));
        hand.Profile = true;
        GripPhase prior = hand.Phase;
        bool opposed = false, lifted = false;
        int held = 0, applied = 0;
        for (int i = 0; i < 700 && !hand.Terminal; i++)
        {
            float disturbance = hand.Phase == GripPhase.Hold && hand.HeldTicks >= 4 && applied < 5 ? load : 0f;
            if (disturbance > 0f) applied++;
            hand.Step(Vector3.down * disturbance, Vector3.zero);
            ArmReading a = hand.ReadArm();
            opposed |= hand.OpposingContact;
            lifted |= hand.Phase == GripPhase.Hold;
            held = Math.Max(held, hand.HeldTicks);
            trace.WriteLine(FormattableString.Invariant($"{cohort},{id},{arm},{hand.Tick},{hand.Phase},{disturbance:F3},{Vec(a.Target)},{Vec(a.Elbow)},{Vec(hand.PalmPosition)},{Vec(hand.ObjectPosition)},{hand.Clearance:F6},{hand.ReadPad(0).NormalForce:F4},{hand.ReadPad(1).NormalForce:F4},{hand.SupportForce:F4},{Vec(a.ActuatorTorque)},{Vec(a.JointDegrees)},{a.TorsoSeconds:F4},{1000 * a.TorsoDepth:F3}"));
            if (prior == hand.Phase) continue;
            Debug.Log(FormattableString.Invariant($"H6_PILOT arm={arm} tick={hand.Tick} {prior}->{hand.Phase} palm={Vec(hand.PalmPosition)} ball={Vec(hand.ObjectPosition)} target={Vec(a.Target)} flex={a.JointDegrees.y:F1} clearance={hand.Clearance:F4} support={hand.SupportForce:F3} pads={hand.ReadPad(0).NormalForce:F2}/{hand.ReadPad(1).NormalForce:F2} torso={a.TorsoSeconds:F3}"));
            prior = hand.Phase;
        }
        var aFinal = hand.ReadArm();
        double us = 1e6 / Stopwatch.Frequency / hand.Tick;
        string row = FormattableString.Invariant($"{cohort},{id},{seed},{arm},{high},{load:F3},{applied},{hand.Tick},{hand.Phase},{hand.Outcome},{opposed},{lifted},{held},{hand.ControlledPlacement},{hand.HouseholdSettled},{Vec(hand.ObjectPosition)},{hand.PeakPadForce:F4},{hand.SlipTicks},{aFinal.TorsoSeconds:F4},{1000 * aFinal.TorsoDepth:F3},{aFinal.UpperDistalSeconds:F4},{aFinal.LimitExcursions},{aFinal.Corrections},{hand.Work:F6},{aFinal.UpperWork+aFinal.ElbowWork+aFinal.WristWork:F6},{hand.CommandCpuTicks*us:F2},{hand.PhysicsCpuTicks*us:F2}");
        rows.WriteLine(row);
        Debug.Log("H6_TRANSFER " + row);
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F5};{v.y:F5};{v.z:F5}");
}
