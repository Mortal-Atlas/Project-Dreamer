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
internal static class SeatedPlacementStudy
{
    const string Key = "Dreamer.H9.Cohort";
    const int Limit = 700;
    enum Policy { H8, Rigid, Passive, Reactive, Anticipatory }

    readonly struct Trial
    {
        public readonly string Name;
        public readonly int Seed;
        public readonly Vector3 Goal;
        public readonly float Mass, Load, Spring, Pitch, Yaw, Age, Speed, Impulse;
        public readonly bool High, Mask;
        public Trial(string name, int seed = 303, float x = -.266f, float y = .05f, float z = .006f, float mass = .082f,
            float load = 0f, float spring = 30f, float pitch = 0f, float yaw = 0f, float age = 3f,
            float speed = 1f, float impulse = 0f, bool high = true, bool mask = false)
        {
            Name = name; Seed = seed; Goal = new Vector3(x, y, z); Mass = mass; Load = load; Spring = spring;
            Pitch = pitch; Yaw = yaw; Age = age; Speed = speed; Impulse = impulse; High = high; Mask = mask;
        }
    }

    static readonly Trial[] Development =
    {
        new Trial("dev-reference", x: -.263f, mass: .080f),
        new Trial("dev-loaded", x: -.271f, load: .29f, impulse: .14f),
        new Trial("dev-alice", 404, x: -.261f, yaw: -7f, pitch: 4f),
        new Trial("dev-lateral", x: -.254f, high: false)
    };
    static readonly Trial[] Held =
    {
        new Trial("neutral"),
        new Trial("higher-shelf", y: .078f),
        new Trial("far-diagonal", x: -.301f, z: .024f),
        new Trial("lateral-arm", high: false),
        new Trial("heavy", mass: .117f),
        new Trial("postural-impulse", impulse: .19f, load: .31f),
        new Trial("initial-pitch", pitch: 8f, yaw: 7f),
        new Trial("compliant-slow", spring: 23f, speed: .77f),
        new Trial("stiffer-fast", spring: 41f, speed: 1.19f),
        new Trial("yaw-and-occlusion", yaw: -12f, mask: true),
        new Trial("alice-scaled", 404, x: -.257f, z: -.012f, age: 3.4f, pitch: -4f, yaw: 5f)
    };

    static SeatedPlacementStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }
    public static void Develop() => Start("development");
    public static void HeldOut() => Start("heldout");

    static void Start(string cohort)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Guarded scratch only.");
        VisualPlacementStudy.VerifySources(cohort);
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
        string prefix = Path.Combine(Application.persistentDataPath, "phase-h9-" + cohort);
        string manifest = Environment.GetEnvironmentVariable("DREAMER_H8_SOURCE_MANIFEST");
        if (!string.IsNullOrEmpty(manifest)) File.Copy(manifest, prefix + "-source.txt", true);
        using var summary = File.CreateText(prefix + "-summary.csv");
        using var trace = File.CreateText(prefix + "-trace.csv");
        summary.WriteLine("cohort,case,policy,seed,goal_m,mass_kg,load_n,load_ticks,spring_nm_rad,pitch_deg,head_yaw_deg,development_days,speed,impulse_nms,applied_impulse,high,masked,reach,grasp,loaded_lift,support,release,complete,settled,outcome,ticks,wrist_endpoint_mm,max_torso_deg,mean_torso_deg,max_torso_rad_s,max_head_motion_mm,max_head_deg,max_shoulder_mm,mean_anchor_n,peak_anchor_n,mean_seat_n,peak_seat_n,seat_contact_s,posture_latency_ticks,release_window_mm,total_drift_mm,goal_error_mm,ball_rmse_mm,hand_rmse_mm,proprio_rmse_mm,innovation_rms_mm,seen_fraction,age_ticks,arm_work_j,posture_work_j,posture_effort_nms,digit_effort_ns,arm_torso_s,arm_torso_impulse_ns,arm_torso_depth_mm,other_body_contact_s,other_body_impulse_ns,other_body_depth_mm,arm_limit_substeps,posture_us,depth_us,fusion_us,physics_us");
        trace.WriteLine("case,policy,tick,phase_before,phase,applied_impulse_nms,applied_load_n,shoulder_m,torso_q,torso_omega,head_m,head_q,eye_m,head_omega,head_acc_mss,anchor_n,seat_n,posture_torque_nm,target_m,actual_wrist_m,estimated_wrist_m,hand_source,hand_m,hand_obs_tick,hand_conf,actual_ball_m,ball_source,ball_m,ball_obs_tick,ball_conf,ball_innovation_mm,release_ticks,pad_left_n,pad_right_n,object_support_n,arm_torso_s,body_contact_s");
        var cases = cohort == "heldout" ? Held : Development;
        for (int c = 0; c < cases.Length; c++)
            for (int p = 0; p < 5; p++) Measure(cohort, cases[c], (Policy)((p + c) % 5), summary, trace);
        Debug.Log($"H9_COMPLETE {cohort} {cases.Length * 5}");
    }

    static void Measure(string cohort, Trial t, Policy policy, TextWriter summary, TextWriter trace)
    {
        SeatedBenchSetup? posture = policy == Policy.H8 ? null :
            new SeatedBenchSetup((SeatedSupportMode)(policy - 1), t.Spring, t.Pitch, t.Yaw);
        using var lab = new NativeHandLab(t.Seed, t.Mass, HouseholdLife.BallRadius, HouseholdLife.BallFriction, 3.2f,
            placement: true, household: new HouseholdHandSetup(new Vector3(-.56f, 0f, 1.88f),
                new Vector3(0f, .125f, 0f), Quaternion.identity, t.Goal, t.Goal, 0f, motionScale: t.Speed),
            articulated: new ArmBenchSetup(t.High, developmentDays: t.Age), coordinated: true,
            visual: new VisualBenchSetup(VisualFeedback.Fused, t.Mask ? BenchOcclusion.Ball : BenchOcclusion.Clear,
                obstructedTicks: t.Mask ? 18 : 0, viewYaw: policy == Policy.H8 ? t.Yaw : 0f), seated: posture);
        lab.Profile = true;
        var initial = lab.ReadPosture();
        var shoulder = lab.ReadArm().Shoulder;
        bool reach = false, grasp = false, lifted = false, applied = false;
        int approach = -1, impact = -1, response = -1, loadTicks = 0, admitted = 0, actionTicks = 0, visualAge = 0;
        float endpoint = -1f, peakTilt = 0f, tiltSum = 0f, spin = 0f, headShift = 0f, headAngle = 0f;
        float shoulderShift = 0f, supportSum = 0f, supportPeak = 0f, seatSum = 0f, seatPeak = 0f, window = 0f, drift = 0f;
        var releaseOrigin = Vector3.zero;
        bool released = false;
        Vector3 torqueBefore = default;
        Errors ball = default, hand = default, wrist = default, innovation = default;
        while (!lab.Terminal && lab.Tick < Limit)
        {
            GripPhase phase = lab.Phase;
            if (phase == GripPhase.Approach && approach < 0) approach = lab.Tick;
            float impulse = lab.Seated && policy != Policy.Rigid && phase == GripPhase.Approach && !applied &&
                lab.Tick == approach + 3 ? t.Impulse : 0f;
            if (impulse != 0f) { impact = lab.Tick; applied = true; torqueBefore = lab.ReadPosture().Command; }
            float load = phase == GripPhase.Hold && lab.HeldTicks >= 4 && loadTicks < 5 ? t.Load : 0f;
            if (load > 0f) loadTicks++;
            if (lab.Seated) lab.StepWithPosturalImpulse(Vector3.down * load, new Vector3(impulse, 0f, 0f));
            else lab.Step(Vector3.down * load, Vector3.zero);
            var post = lab.ReadPosture();
            var arm = lab.ReadArm();
            var v = lab.VisualReading;
            if (!reach && lab.Phase == GripPhase.Close) endpoint = Vector3.Distance(arm.Target, lab.PalmPosition) * 1000f;
            reach |= lab.Phase == GripPhase.Close;
            grasp |= lab.OpposingContact;
            lifted |= lab.HeldTicks >= 12;
            if (lab.Phase == GripPhase.Release && !released) { released = true; releaseOrigin = lab.ObjectPosition; }
            if (released)
            {
                var distance = Horizontal(lab.ObjectPosition - releaseOrigin);
                drift = Mathf.Max(drift, distance);
                if (phase == GripPhase.Release || lab.Phase == GripPhase.Release) window = Mathf.Max(window, distance);
                admitted = Math.Max(admitted, lab.ReleaseQualificationTicks);
            }
            if (lab.Seated)
            {
                float angle = Quaternion.Angle(Quaternion.identity, post.TorsoRotation);
                peakTilt = Mathf.Max(peakTilt, angle); tiltSum += angle;
                spin = Mathf.Max(spin, post.TorsoAngular.magnitude);
                headShift = Mathf.Max(headShift, Vector3.Distance(initial.Head, post.Head));
                headAngle = Mathf.Max(headAngle, Quaternion.Angle(initial.HeadRotation, post.HeadRotation));
                float support = post.SupportForce.magnitude;
                supportSum += support; supportPeak = Mathf.Max(supportPeak, support);
                seatSum += post.SeatForce; seatPeak = Mathf.Max(seatPeak, post.SeatForce);
                if (impact >= 0 && response < 0 && (post.Command - torqueBefore).magnitude >= .15f)
                    response = lab.Tick - impact;
            }
            shoulderShift = Mathf.Max(shoulderShift, Vector3.Distance(shoulder, arm.Shoulder));
            if (phase >= GripPhase.Approach && phase <= GripPhase.Release || phase == GripPhase.Transport)
            {
                actionTicks++;
                wrist.Add(Vector3.Distance(v.ProprioceptiveWrist, lab.PalmPosition));
                if (v.Hand.VisibleRays > 0 && v.Hand.Confidence > 0f) hand.Add(Vector3.Distance(U(v.Hand.Position), lab.PalmPosition));
                if (v.Ball.VisibleRays > 0 && v.Ball.Confidence > 0f)
                {
                    ball.Add(Vector3.Distance(U(v.Ball.Position), lab.ObjectPosition));
                    innovation.Add(v.Ball.PredictionError);
                    visualAge += v.Ball.Age;
                }
            }
            trace.WriteLine(FormattableString.Invariant($"{t.Name},{policy},{lab.Tick},{phase},{lab.Phase},{impulse:F3},{load:F3},{V(arm.Shoulder)},{Q(post.TorsoRotation)},{V(post.TorsoAngular)},{V(post.Head)},{Q(post.HeadRotation)},{V(post.Eye)},{V(post.HeadAngular)},{V(post.HeadAcceleration)},{V(post.SupportForce)},{post.SeatForce:F4},{V(post.Command)},{V(arm.Target)},{V(lab.PalmPosition)},{V(v.ProprioceptiveWrist)},{v.Hand.Source},{V(U(v.Hand.Position))},{v.Hand.ObservationTick},{v.Hand.Confidence:F3},{V(lab.ObjectPosition)},{v.Ball.Source},{V(U(v.Ball.Position))},{v.Ball.ObservationTick},{v.Ball.Confidence:F3},{1000f*v.Ball.PredictionError:F3},{lab.ReleaseQualificationTicks},{lab.ReadPad(0).NormalForce:F3},{lab.ReadPad(1).NormalForce:F3},{lab.SupportForce:F3},{arm.TorsoSeconds:F3},{post.ContactSeconds:F3}"));
        }
        var end = lab.ReadPosture();
        var a = lab.ReadArm();
        double us = 1000000d / Stopwatch.Frequency / lab.Tick;
        string row = FormattableString.Invariant($"{cohort},{t.Name},{policy},{t.Seed},{V(t.Goal)},{t.Mass:F3},{t.Load:F3},{loadTicks},{t.Spring:F1},{t.Pitch:F1},{t.Yaw:F1},{t.Age:F1},{t.Speed:F2},{t.Impulse:F3},{applied},{t.High},{t.Mask},{reach},{grasp},{lifted},{lab.ControlledPlacement},{admitted >= 5 && lab.ControlledPlacement},{lab.Phase == GripPhase.Complete},{lab.HouseholdSettled},{lab.Outcome},{lab.Tick},{endpoint:F3},{peakTilt:F3},{tiltSum/lab.Tick:F3},{spin:F3},{headShift*1000f:F3},{headAngle:F3},{shoulderShift*1000f:F3},{(posture.HasValue && policy != Policy.Rigid ? supportSum/lab.Tick : -1f):F3},{(posture.HasValue && policy != Policy.Rigid ? supportPeak : -1f):F3},{seatSum/lab.Tick:F3},{seatPeak:F3},{end.SeatSeconds:F3},{response},{window*1000f:F3},{drift*1000f:F3},{Horizontal(lab.ObjectPosition-t.Goal)*1000f:F3},{ball.Rms:F3},{hand.Rms:F3},{wrist.Rms:F3},{innovation.Rms:F3},{ball.Count/(float)Math.Max(1,actionTicks):F3},{(ball.Count == 0 ? -1f : visualAge/(float)ball.Count):F3},{a.UpperWork+a.ElbowWork+a.WristWork:F3},{end.Work:F3},{end.Effort:F3},{lab.DigitEffort:F3},{a.TorsoSeconds:F3},{a.TorsoImpulse:F4},{a.TorsoDepth*1000f:F4},{end.ContactSeconds:F3},{end.ContactImpulse:F4},{end.Penetration*1000f:F4},{a.LimitExcursions},{lab.PostureCpuTicks*us:F3},{lab.VisualCpuTicks*us:F3},{lab.FusionCpuTicks*us:F3},{lab.PhysicsCpuTicks*us:F3}");
        summary.WriteLine(row);
        Debug.Log("H9_TRIAL " + row);
    }

    struct Errors
    {
        double _sum;
        public int Count;
        public double Rms => Count == 0 ? -1d : Math.Sqrt(_sum / Count) * 1000d;
        public void Add(float error) { _sum += error * error; Count++; }
    }

    static float Horizontal(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    static Vector3 U(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    static string V(Vector3 v) => FormattableString.Invariant($"{v.x:F6};{v.y:F6};{v.z:F6}");
    static string Q(Quaternion q) => FormattableString.Invariant($"{q.x:F6};{q.y:F6};{q.z:F6};{q.w:F6}");
}
