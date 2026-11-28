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
internal static class PlacementCoordinationStudy
{
    const string Key = "Dreamer.H7.Placement";
    const int TrialLimit = 700;
    enum Policy { Carriage, H6, H7 }

    readonly struct Trial
    {
        public readonly string Name;
        public readonly int Seed;
        public readonly float Mass, Friction, X, Z, Angle, Load, ShelfWidth, Speed;
        public readonly bool High;
        public Trial(string name, int seed = 303, float mass = .080f, float friction = .75f, float x = -.270f, float z = 0f,
            float angle = 0f, float load = 0f, float shelfWidth = .115f, float speed = 1f, bool high = true)
        { Name = name; Seed = seed; Mass = mass; Friction = friction; X = x; Z = z; Angle = angle; Load = load; ShelfWidth = shelfWidth; Speed = speed; High = high; }
    }

    static readonly Trial[] Development =
    {
        new Trial("dev-arthur", x: -.264f, angle: 11f),
        new Trial("dev-alice", seed: 404, x: -.266f, z: -.008f, angle: -21f),
        new Trial("dev-light", mass: .067f, x: -.246f, angle: 32f),
        new Trial("dev-loaded", mass: .087f, x: -.280f, z: .008f, angle: 40f, load: .24f),
        new Trial("dev-lateral", x: -.253f, z: .010f, high: false),
    };
    static readonly Trial[] Independent =
    {
        new Trial("new-reference", mass: .082f, x: -.271f, z: .006f, angle: 14f),
        new Trial("new-mass", mass: .117f, x: -.271f, z: .006f, angle: 14f),
        new Trial("new-surface", mass: .082f, friction: .37f, x: -.271f, z: .006f, angle: 14f),
        new Trial("new-narrow-shelf", mass: .082f, x: -.271f, z: .006f, angle: 14f, shelfWidth: .071f),
        new Trial("new-disturbance", mass: .082f, x: -.271f, z: .006f, angle: 14f, load: .34f),
        new Trial("new-far-diagonal", mass: .082f, x: -.315f, z: .029f, angle: 14f),
        new Trial("new-seam-rotation", mass: .082f, x: -.271f, z: .006f, angle: 73f),
        new Trial("new-slow-command", mass: .082f, x: -.271f, z: .006f, angle: 14f, speed: .74f),
        new Trial("new-fast-command", mass: .082f, x: -.271f, z: .006f, angle: 14f, speed: 1.21f),
        new Trial("new-alice", seed: 404, mass: .082f, x: -.271f, z: .006f, angle: 14f),
        new Trial("new-lateral", mass: .082f, x: -.271f, z: .006f, angle: 14f, high: false),
    };

    static PlacementCoordinationStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Diagnose() => Start("diagnosis");
    public static void Develop() => Start("development");
    public static void HeldOut() => Start("heldout");

    static void Start(string cohort)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Guarded scratch only.");
        SessionState.SetString(Key, cohort);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseString(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Run(SessionState.GetString(Key, "")); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Run(string cohort)
    {
        string prefix = Path.Combine(Application.persistentDataPath, "phase-h7-" + cohort);
        using var output = File.CreateText(prefix + "-summary.csv");
        using var trace = File.CreateText(prefix + "-trace.csv");
        output.WriteLine("cohort,case,seed,policy,mass_kg,surface_friction,goal_m,orientation_deg,load_n,load_ticks,high,shelf_half_width_m,speed,approach,grasp,lift,hold_ticks,lower,transfer,release,settled,ticks,release_tick,release_palm_ms,release_palm_rad_s,release_ball_ms,release_ball_rad_s,release_support_n,max_drift_mm,release_window_drift_mm,angular_drift_deg,ends_on_destination,peak_pad_n,effort_ns,arm_work_j,fixture_work_j,torso_s,torso_depth_mm,penetration_mm,limit_excursions,corrections,controller_us,tracking_us,physics_us,terminal_ball_m,outcome");
        trace.WriteLine("cohort,case,policy,tick,phase_before,phase,load_n,target_m,palm_m,palm_q,palm_ms,palm_rad_s,ball_m,ball_q,ball_ms,ball_rad_s,joint_degrees,joint_rad_s,actuator_nm,left_n,right_n,left_tangent_n,right_tangent_n,left_slip_ms,right_slip_ms,left_slip,right_slip,support_n,support_m,placement,release_ticks,held_ticks,arm_work_j,effort_ns,torso_s,tracking,estimated_ball_ms,estimated_ball_rad_s,steady_ticks,observation");
        if (cohort == "heldout")
        {
            for (int i = 0; i < Independent.Length; i++)
                for (int j = 0; j < 3; j++) Measure(cohort, Independent[i], (Policy)((i + j) % 3), output, trace);
        }
        else
        {
            foreach (var trial in Development)
            {
                Measure(cohort, trial, Policy.H6, output, trace);
                if (cohort != "diagnosis") Measure(cohort, trial, Policy.H7, output, trace);
            }
        }
        Debug.Log($"H7_COMPLETE {cohort}");
    }

    static void Measure(string cohort, Trial trial, Policy policy, TextWriter summary, TextWriter trace)
    {
        var site = new Vector3(trial.X, .05f, trial.Z);
        using var hand = new NativeHandLab(trial.Seed, trial.Mass, HouseholdLife.BallRadius, HouseholdLife.BallFriction, 3.2f,
            placement: true, household: new HouseholdHandSetup(new Vector3(-.56f, 0f, 1.88f),
                new Vector3(0, .125f, 0), Quaternion.Euler(0, trial.Angle, 0), site, site, 0,
                trial.ShelfWidth, trial.Friction == .75f ? -1f : trial.Friction, trial.Speed),
            articulated: policy == Policy.Carriage ? null : new ArmBenchSetup(trial.High), coordinated: policy == Policy.H7);
        hand.Profile = true;
        int applied = 0, held = 0, releaseTick = -1, quiet = 0;
        bool grasp = false, lift = false, placed = false, approach = false, lowered = false;
        Vector3 releasePosition = default;
        Quaternion releaseRotation = Quaternion.identity;
        float drift = 0f, releaseDrift = 0f, spin = 0f, pv = 0f, pa = 0f, bv = 0f, ba = 0f, support = 0f;
        while (!hand.Terminal && hand.Tick < TrialLimit)
        {
            var phase = hand.Phase;
            float load = phase == GripPhase.Hold && hand.HeldTicks >= 4 && applied < 5 ? trial.Load : 0f;
            if (load > 0f) applied++;
            hand.Step(Vector3.down * load, Vector3.zero);
            var a = hand.ReadArm();
            var l = hand.ReadPad(0); var r = hand.ReadPad(1);
            grasp |= hand.OpposingContact; lift |= hand.Phase == GripPhase.Hold;
            approach |= hand.Phase == GripPhase.Close; lowered |= hand.Phase == GripPhase.Lower;
            held = Math.Max(held, hand.HeldTicks); placed |= hand.ControlledPlacement;
            if (hand.Phase == GripPhase.Release && releaseTick < 0)
            {
                releaseTick = hand.Tick; releasePosition = hand.ObjectPosition; releaseRotation = hand.ObjectRotation;
                pv = hand.PalmVelocity.magnitude; pa = hand.PalmAngularVelocity.magnitude;
                bv = hand.ObjectVelocity.magnitude; ba = hand.ObjectAngularVelocity.magnitude; support = hand.SupportForce;
            }
            if (releaseTick >= 0)
            {
                var delta = hand.ObjectPosition - releasePosition;
                float current = new Vector2(delta.x, delta.z).magnitude;
                drift = Mathf.Max(drift, current);
                if (phase == GripPhase.Release || hand.Phase == GripPhase.Release) releaseDrift = Mathf.Max(releaseDrift, current);
                spin = Mathf.Max(spin, Quaternion.Angle(releaseRotation, hand.ObjectRotation));
                quiet = Math.Max(quiet, hand.ReleaseQualificationTicks);
            }
            trace.WriteLine(FormattableString.Invariant($"{cohort},{trial.Name},{policy},{hand.Tick},{phase},{hand.Phase},{load:F3},{Vec(a.Target)},{Vec(hand.PalmPosition)},{Rot(hand.WristRotation)},{Vec(hand.PalmVelocity)},{Vec(hand.PalmAngularVelocity)},{Vec(hand.ObjectPosition)},{Rot(hand.ObjectRotation)},{Vec(hand.ObjectVelocity)},{Vec(hand.ObjectAngularVelocity)},{Vec(a.JointDegrees)},{Vec(a.JointSpeed)},{Vec(a.ActuatorTorque)},{l.NormalForce:F4},{r.NormalForce:F4},{l.TangentForce:F4},{r.TangentForce:F4},{l.RelativeSpeed:F5},{r.RelativeSpeed:F5},{l.Slip},{r.Slip},{hand.SupportForce:F4},{Vec(hand.SupportPoint)},{hand.ControlledPlacement},{quiet},{hand.HeldTicks},{a.UpperWork + a.ElbowWork + a.WristWork:F5},{hand.DigitEffort:F5},{a.TorsoSeconds:F4},{hand.PlacementTracked},{hand.EstimatedBallSpeed:F5},{hand.EstimatedBallSpin:F5},{hand.StableSupportTicks},{hand.PlacementObservation}"));
        }
        var arm = hand.ReadArm();
        bool inGoal = Mathf.Abs(hand.ObjectPosition.y - hand.Radius - site.y) < .006f &&
            Mathf.Abs(hand.SupportPoint.x - site.x) <= .024f * (.125f / .028f) &&
            Mathf.Abs(hand.SupportPoint.z - site.z) <= .039f * (.125f / .028f);
        double us = 1000000d / Stopwatch.Frequency / hand.Tick;
        string row = FormattableString.Invariant($"{cohort},{trial.Name},{trial.Seed},{policy},{trial.Mass:F3},{trial.Friction:F3},{Vec(site)},{trial.Angle:F1},{trial.Load:F3},{applied},{trial.High},{trial.ShelfWidth:F3},{trial.Speed:F2},{approach},{grasp},{lift},{held},{lowered},{placed},{quiet >= 5 && placed},{hand.HouseholdSettled},{hand.Tick},{releaseTick},{pv:F5},{pa:F5},{bv:F5},{ba:F5},{support:F5},{drift * 1000f:F3},{releaseDrift * 1000f:F3},{spin:F3},{inGoal},{hand.PeakPadForce:F4},{hand.DigitEffort:F5},{arm.UpperWork + arm.ElbowWork + arm.WristWork:F5},{hand.Work:F5},{arm.TorsoSeconds:F4},{arm.TorsoDepth * 1000f:F4},{hand.PeakPenetration * 1000f:F4},{arm.LimitExcursions},{arm.Corrections},{hand.CommandCpuTicks * us:F2},{hand.TrackingCpuTicks * us:F2},{hand.PhysicsCpuTicks * us:F2},{Vec(hand.ObjectPosition)},{hand.Outcome}");
        summary.WriteLine(row);
        Debug.Log("H7_TRIAL " + row);
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F5};{v.y:F5};{v.z:F5}");
    static string Rot(Quaternion q) => FormattableString.Invariant($"{q.x:F5};{q.y:F5};{q.z:F5};{q.w:F5}");
}
