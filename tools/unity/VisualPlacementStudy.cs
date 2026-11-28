using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class VisualPlacementStudy
{
    const string Key = "Dreamer.H8.VisualStudy";
    const int Limit = 700;
    enum Policy { H7, MeasureOnly, VisualOnly, Fused }

    readonly struct Trial
    {
        public readonly string Name;
        public readonly Vector3 Site, Pickup;
        public readonly int Seed, Obstructed, Period, Delay;
        public readonly float Yaw, Mass, Load, HalfWidth, ArmYaw, Age, Impulse;
        public readonly BenchOcclusion Occlusion;
        public readonly bool High;

        public Trial(string name, BenchOcclusion occlusion = BenchOcclusion.Clear, int obstructed = 0,
            int seed = 303, float x = -.272f, float z = .003f, float pickup = 0f, float yaw = 0f,
            int period = 2, int delay = 1, float mass = .080f, float load = 0f, bool high = true,
            float halfWidth = .115f, float armYaw = 0f, float age = 3f, float impulse = 0f)
        {
            Name = name; Site = new Vector3(x, .05f, z); Pickup = new Vector3(pickup, .125f, 0f);
            Seed = seed; Obstructed = obstructed; Period = period; Delay = delay; Yaw = yaw; Mass = mass;
            Load = load; High = high; HalfWidth = halfWidth; ArmYaw = armYaw; Age = age; Impulse = impulse;
            Occlusion = occlusion;
        }
    }

    static readonly Trial[] Development =
    {
        new Trial("dev-clear", x: -.263f, z: -.005f),
        new Trial("dev-together", BenchOcclusion.Both, 13, 404, x: -.265f, yaw: 3f),
        new Trial("dev-delayed-ball", BenchOcclusion.Ball, 16, yaw: -5f, delay: 3),
        new Trial("dev-narrow", x: -.270f, halfWidth: .070f),
    };

    static readonly Trial[] Independent =
    {
        new Trial("clear"),
        new Trial("partial-hand", BenchOcclusion.PartialHand, 22),
        new Trial("partial-ball", BenchOcclusion.PartialBall, 22),
        new Trial("hand-reappears", BenchOcclusion.Hand, 15),
        new Trial("ball-reappears", BenchOcclusion.Ball, 21),
        new Trial("both-reappear", BenchOcclusion.Both, 31),
        new Trial("delayed", period: 4, delay: 3, yaw: 9f),
        new Trial("alice-angle", seed: 404, x: -.255f, z: -.012f, pickup: .012f, yaw: -9f, armYaw: 4f, age: 3.4f),
        new Trial("shift-during-occlusion", BenchOcclusion.Both, 26, impulse: .012f),
        new Trial("occluded-surface", BenchOcclusion.Surface, 30, x: -.284f),
        new Trial("disturbance", load: .30f),
        new Trial("narrow", halfWidth: .071f),
        new Trial("far-diagonal", x: -.315f, z: .029f),
        new Trial("heavy", mass: .117f),
        new Trial("lateral", high: false)
    };

    static VisualPlacementStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Develop() => Start("development");
    public static void HeldOut() => Start("heldout");

    static void Start(string cohort)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Guarded scratch only.");
        VerifySources(cohort);
        SessionState.SetString(Key, cohort);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void VerifySources(string cohort)
    {
        string manifest = Environment.GetEnvironmentVariable("DREAMER_H8_SOURCE_MANIFEST");
        if (string.IsNullOrWhiteSpace(manifest))
        {
            if (cohort == "heldout") throw new InvalidOperationException("Run the verified audit launcher before scoring held-outs.");
            return;
        }
        var project = Directory.GetParent(Application.dataPath).FullName;
        using var sha = SHA256.Create();
        foreach (var line in File.ReadLines(manifest))
        {
            if (line.StartsWith("#", StringComparison.Ordinal)) continue;
            using var input = File.OpenRead(Path.Combine(project, line.Substring(65)));
            string hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            if (!string.Equals(hash, line.Substring(0, 64), StringComparison.Ordinal))
                throw new InvalidDataException("Audit source changed before execution. " + line.Substring(65));
        }
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
        string prefix = Path.Combine(Application.persistentDataPath, "phase-h8-" + cohort);
        var manifest = Environment.GetEnvironmentVariable("DREAMER_H8_SOURCE_MANIFEST");
        if (!string.IsNullOrWhiteSpace(manifest)) File.Copy(manifest, prefix + "-source.txt", true);
        using var output = File.CreateText(prefix + "-summary.csv");
        using var trace = File.CreateText(prefix + "-trace.csv");
        output.WriteLine("cohort,case,policy,seed,destination_m,pickup_m,eye_yaw,arm_yaw,body_days,high,mass_kg,shelf_half_width,occlusion,occlusion_ticks,period_ticks,delay_ticks,load_n,loads,impulse_ns,reach,grasp,loaded_lift,support,release,settled,complete,ticks,release_window_mm,total_drift_mm,goal_error_mm,outcome,proprio_rmse_mm,hand_rmse_mm,ball_rmse_mm,predicted_ball_rmse_mm,seen_hand,seen_ball,predicted_ball,mean_age_ticks,low_conf_rmse_mm,high_conf_rmse_mm,low_conf_count,high_conf_count,ball_reacquire_ticks,hand_reacquire_ticks,first_correction_ticks,corrections,work_j,effort_ns,torso_s,limit_excursions,visual_us,fusion_us,correction_us,physics_us");
        trace.WriteLine("case,policy,tick,phase_before,phase,aperture,proprio_m,actual_hand_m,actual_ball_m,hand_source,hand_obs_tick,hand_age,hand_hits,hand_conf,hand_m,ball_source,ball_obs_tick,ball_age,ball_hits,ball_conf,ball_m,ball_velocity,innovation_mm,surface_obs_tick,surface_conf,surface_m,contact_s,correction_m,correction_tick,release_ticks,left_n,right_n,support_n,joints_deg,torque_nm,arm_work_j");
        var cases = cohort == "heldout" ? Independent : Development;
        for (int i = 0; i < cases.Length; i++)
            for (int p = 0; p < 4; p++) Measure(cohort, cases[i], (Policy)((i + p) % 4), output, trace);
        Debug.Log($"H8_COMPLETE {cohort} {cases.Length * 4}");
    }

    static void Measure(string cohort, Trial t, Policy policy, TextWriter output, TextWriter trace)
    {
        VisualFeedback feedback = policy == Policy.Fused ? VisualFeedback.Fused :
            policy == Policy.VisualOnly ? VisualFeedback.VisualOnly : VisualFeedback.MeasureOnly;
        using var hand = new NativeHandLab(t.Seed, t.Mass, HouseholdLife.BallRadius, HouseholdLife.BallFriction, 3.2f,
            placement: true, household: new HouseholdHandSetup(new Vector3(-.56f, 0f, 1.88f), t.Pickup,
                Quaternion.identity, t.Site, t.Site, 0f, t.HalfWidth),
            articulated: new ArmBenchSetup(t.High, t.ArmYaw, developmentDays: t.Age), coordinated: true,
            visual: policy == Policy.H7 ? null : new VisualBenchSetup(feedback, t.Occlusion, t.Period, t.Delay, t.Obstructed, t.Yaw));
        hand.Profile = true;
        int loads = 0, lower = -1, release = -1, quiet = 0, firstCorrection = -1, corrections = 0, sawAfterBall = -1, sawAfterHand = -1;
        int lastCorrection = -1, occlusionEnd = -1;
        bool reach = false, grasp = false, loaded = false, kicked = false;
        Vector3 atRelease = default;
        float maxRelease = 0f, maxTotal = 0f, totalAge = 0f;
        Errors proprio = default, directHand = default, directBall = default, predicted = default, low = default, high = default;
        while (!hand.Terminal && hand.Tick < Limit)
        {
            GripPhase phase = hand.Phase;
            float load = phase == GripPhase.Hold && hand.HeldTicks >= 4 && loads < 5 ? t.Load : 0f;
            if (load > 0f) loads++;
            if (phase == GripPhase.Lower && lower < 0) lower = hand.Tick;
            // An evaluator-applied impulse moves the real object during windowed sensory loss.
            float impulse = !kicked && phase == GripPhase.Lower && hand.Tick == lower + 11 ? t.Impulse : 0f;
            if (impulse != 0f) kicked = true;
            hand.Step(Vector3.down * load, Vector3.forward * impulse);
            reach |= hand.Phase == GripPhase.Close;
            grasp |= hand.OpposingContact;
            loaded |= hand.HeldTicks >= 12;
            var v = hand.VisualReading;
            var a = hand.ReadArm();
            if (hand.Phase == GripPhase.Release && release < 0) { release = hand.Tick; atRelease = hand.ObjectPosition; }
            if (release >= 0)
            {
                float drift = Horizontal(hand.ObjectPosition - atRelease);
                maxTotal = Mathf.Max(maxTotal, drift);
                if (phase == GripPhase.Release || hand.Phase == GripPhase.Release) maxRelease = Mathf.Max(maxRelease, drift);
                quiet = Math.Max(quiet, hand.ReleaseQualificationTicks);
            }
            if (hand.VisualCoordination)
            {
                bool action = phase == GripPhase.Transport || phase == GripPhase.Lower || phase == GripPhase.Release;
                if (lower >= 0 && t.Obstructed > 0) occlusionEnd = lower + 4 + t.Obstructed;
                if (occlusionEnd >= 0 && v.Ball.VisibleRays > 0 && v.Ball.ObservationTick >= occlusionEnd && sawAfterBall < 0) sawAfterBall = hand.Tick - occlusionEnd;
                if (occlusionEnd >= 0 && v.Hand.VisibleRays > 0 && v.Hand.ObservationTick >= occlusionEnd && sawAfterHand < 0) sawAfterHand = hand.Tick - occlusionEnd;
                if (v.CorrectionTick > lastCorrection)
                {
                    if (firstCorrection < 0 && lower >= 0) firstCorrection = hand.Tick - lower;
                    corrections++; lastCorrection = v.CorrectionTick;
                }
                if (action) proprio.Add(Vector3.Distance(v.ProprioceptiveWrist, hand.PalmPosition));
                if (action && v.Hand.Source != VisualEstimateSource.Unavailable && v.Hand.VisibleRays > 0)
                    directHand.Add(Error(v.Hand, hand.PalmPosition));
                if (action && v.Ball.Source != VisualEstimateSource.Unavailable)
                {
                    float error = Error(v.Ball, hand.ObjectPosition);
                    if (v.Ball.VisibleRays > 0) { directBall.Add(error); totalAge += v.Ball.Age; }
                    if (v.Ball.Source == VisualEstimateSource.Predicted && v.Ball.VisibleRays == 0) predicted.Add(error);
                    if (v.Ball.Confidence < .40f) low.Add(error);
                    else high.Add(error);
                }
            }
            trace.WriteLine(FormattableString.Invariant($"{t.Name},{policy},{hand.Tick},{phase},{hand.Phase},{v.Occlusion},{Vec(v.ProprioceptiveWrist)},{Vec(hand.PalmPosition)},{Vec(hand.ObjectPosition)},{v.Hand.Source},{v.Hand.ObservationTick},{v.Hand.Age},{v.Hand.VisibleRays},{v.Hand.Confidence:F3},{Vec(v.Hand.Position)},{v.Ball.Source},{v.Ball.ObservationTick},{v.Ball.Age},{v.Ball.VisibleRays},{v.Ball.Confidence:F3},{Vec(v.Ball.Position)},{Vec(v.Ball.Velocity)},{v.Ball.PredictionError * 1000f:F3},{v.Surface.ObservationTick},{v.Surface.Confidence:F3},{Vec(v.Surface.Position)},{v.ExpectedContactSeconds:F3},{Vec(v.Correction)},{v.CorrectionTick},{hand.ReleaseQualificationTicks},{hand.ReadPad(0).NormalForce:F3},{hand.ReadPad(1).NormalForce:F3},{hand.SupportForce:F3},{Vec(a.JointDegrees)},{Vec(a.ActuatorTorque)},{a.UpperWork+a.ElbowWork+a.WristWork:F4}"));
        }
        double us = 1000000d / Stopwatch.Frequency / hand.Tick;
        var arm = hand.ReadArm();
        string result = FormattableString.Invariant($"{cohort},{t.Name},{policy},{t.Seed},{Vec(t.Site)},{Vec(t.Pickup)},{t.Yaw},{t.ArmYaw},{t.Age:F1},{t.High},{t.Mass:F3},{t.HalfWidth:F3},{t.Occlusion},{t.Obstructed},{t.Period},{t.Delay},{t.Load:F2},{loads},{(kicked ? t.Impulse : 0f):F3},{reach},{grasp},{loaded},{hand.ControlledPlacement},{quiet >= 5 && hand.ControlledPlacement},{hand.HouseholdSettled},{hand.Phase == GripPhase.Complete},{hand.Tick},{maxRelease*1000f:F3},{maxTotal*1000f:F3},{Horizontal(hand.ObjectPosition - t.Site)*1000f:F3},{hand.Outcome},{proprio.Rms:F3},{directHand.Rms:F3},{directBall.Rms:F3},{predicted.Rms:F3},{directHand.Count},{directBall.Count},{predicted.Count},{(directBall.Count == 0 ? -1f : totalAge / directBall.Count):F2},{low.Rms:F3},{high.Rms:F3},{low.Count},{high.Count},{sawAfterBall},{sawAfterHand},{firstCorrection},{corrections},{arm.UpperWork+arm.ElbowWork+arm.WristWork:F4},{hand.DigitEffort:F4},{arm.TorsoSeconds:F3},{arm.LimitExcursions},{hand.VisualCpuTicks*us:F3},{hand.FusionCpuTicks*us:F3},{hand.CorrectionCpuTicks*us:F3},{hand.PhysicsCpuTicks*us:F3}");
        output.WriteLine(result);
        Debug.Log("H8_TRIAL " + result);
    }

    struct Errors
    {
        double _squares;
        public int Count;
        public double Rms => Count == 0 ? -1d : Math.Sqrt(_squares / Count) * 1000d;
        public void Add(float value) { _squares += value * value; Count++; }
    }

    static float Error(VisualPoseEstimate p, Vector3 actual) => Vector3.Distance(new Vector3(p.Position.X, p.Position.Y, p.Position.Z), actual);
    static float Horizontal(Vector3 point) => new Vector2(point.x, point.z).magnitude;
    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F5};{v.y:F5};{v.z:F5}");
    static string Vec(System.Numerics.Vector3 v) => FormattableString.Invariant($"{v.X:F5};{v.Y:F5};{v.Z:F5}");
}
