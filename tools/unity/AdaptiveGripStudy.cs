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
internal static class AdaptiveGripStudy
{
    const string Key = "Dreamer.H3.GripStudy";
    const int SubjectSeed = 303;
    const int TrialLimit = 240;
    const float DropClearance = .010f;
    const int LoadDurationTicks = 6;
    enum Policy { Fixed, Scalar, Contact, ContactFresh, ContactFrozen }
    enum Perturbation { None, LateralImpulse, AddedLoad }
    readonly struct Trial
    {
        public readonly string Id;
        public readonly float Mass, Radius, Friction, Yaw;
        public readonly HandObjectSetup Object;
        public readonly Perturbation Change;

        public Trial(string id, float mass, float radius, float friction, float yaw, float height = 0,
            Vector3 box = default, Vector3 offset = default, Perturbation change = Perturbation.None)
        {
            Id = id; Mass = mass; Radius = radius; Friction = friction; Yaw = yaw; Change = change;
            var shape = box != default ? HandObjectShape.Box : height > 0 ? HandObjectShape.Capsule : HandObjectShape.Sphere;
            Object = new HandObjectSetup(shape, box != default ? box : new Vector3(2 * radius, height > 0 ? height : 2 * radius, 2 * radius), offset);
        }
    }

    static readonly Trial[] DevelopmentObjects =
    {
        new Trial("train-a", .088f, .0285f, .69f, 7f),
        new Trial("train-b", .108f, .030f, .46f, 19f),
        new Trial("train-c", .078f, .024f, .67f, 12f, box: new Vector3(.046f, .048f, .044f)),
        new Trial("train-d", .101f, .0275f, .50f, 31f, change: Perturbation.AddedLoad)
    };

    // Frozen before the independent run. Neither adaptation policy receives these descriptors.
    static readonly Trial[] HeldOutObjects =
    {
        new Trial("sphere-light-narrow", .052f, .0235f, .82f, 10f),
        new Trial("sphere-heavy-low-friction", .182f, .033f, .20f, 79f),
        new Trial("sphere-wide", .097f, .035f, .51f, 103f),
        new Trial("box-light-yawed", .067f, .024f, .65f, 14f, box: new Vector3(.044f, .044f, .053f)),
        new Trial("box-steep-yaw", .094f, .025f, .60f, 37f, box: new Vector3(.047f, .047f, .044f)),
        new Trial("capsule-offset", .124f, .025f, .57f, 52f, height: .064f, offset: new Vector3(.006f, -.006f, .004f)),
        new Trial("sphere-disturbance", .078f, .029f, .66f, 22f, change: Perturbation.LateralImpulse),
        new Trial("sphere-added-load", .093f, .0305f, .39f, 17f, change: Perturbation.AddedLoad),
        new Trial("box-offset", .106f, .024f, .56f, 29f, box: new Vector3(.042f, .051f, .046f), offset: new Vector3(.007f, .004f, .005f)),
        new Trial("capsule-low-friction", .118f, .026f, .31f, 83f, height: .075f)
    };

    static AdaptiveGripStudy()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Development() => Start("development");
    public static void HeldOut() => Start("heldout");

    static void Start(string cohort)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetString(Key, cohort);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseString(Key); EditorApplication.Exit(0); }
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try { Run(SessionState.GetString(Key, "")); EditorApplication.ExitPlaymode(); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }

    static void Run(string cohort)
    {
        bool scoring = cohort == "heldout";
        string folder = Application.persistentDataPath;
        var scalar = LoadPolicy(folder, "scalar", scoring);
        var contact = LoadPolicy(folder, "contact", scoring);
        int rows = 0;
        using var summary = File.CreateText(Path.Combine(folder, $"phase-h3-{cohort}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(folder, $"phase-h3-{cohort}-trace.csv"));
        summary.WriteLine("cohort,object,attempt,policy,shape,mass_kg,size_m,friction,yaw_deg,com_m,perturbation,applied_ticks,initial_cap_n,peak_cap_n,retained_cap_n,corrections,first_correction_tick,opposed_contact,lift,held_s,settled_release,dropped,success,outcome,slip_ticks,peak_slip_m_s,peak_pad_n,effort_ns,absolute_drive_work_j,peak_lift_mm,max_displacement_mm,peak_penetration_mm,ticks,native_tick_us");
        trace.WriteLine("cohort,object,attempt,policy,tick,phase_before,phase_after,held_ticks,load_n,impulse_ns,cap_applied_n,cap_next_n,corrections,grip_uncertain,palm_m,wrist_xyzw,object_m,object_xyzw,object_velocity_m_s,left_travel_m,right_travel_m,left_speed_m_s,right_speed_m_s,left_effort_n,right_effort_n,left_normal_n,right_normal_n,left_tangent_n,right_tangent_n,left_contact_s,right_contact_s,left_speed_at_contact_m_s,right_speed_at_contact_m_s,left_slip,right_slip,left_point_m,right_point_m,clearance_m,work_j");
        if (!scoring)
        {
            for (int i = 0; i < 2 * DevelopmentObjects.Length; i++)
            {
                var trial = DevelopmentObjects[i % DevelopmentObjects.Length];
                RunTrial(cohort, trial, i + 1, Policy.Fixed, GripAcquisition.ReferenceCap, null, summary, trace);
                RunTrial(cohort, trial, i + 1, Policy.Scalar, scalar.Cap, scalar, summary, trace);
                RunTrial(cohort, trial, i + 1, Policy.Contact, contact.Cap, contact, summary, trace);
                rows += 3;
            }
            SavePolicy(folder, "scalar", scalar);
            SavePolicy(folder, "contact", contact);
        }
        else
        {
            for (int i = 0; i < HeldOutObjects.Length; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var policy = (Policy)((i + j) % 5);
                    float cap = policy switch
                    {
                        Policy.Fixed => GripAcquisition.ReferenceCap,
                        Policy.Scalar => scalar.Cap,
                        Policy.ContactFresh => GripAcquisition.InitialCap,
                        _ => contact.Cap
                    };
                    RunTrial(cohort, HeldOutObjects[i], i + 1, policy, cap, null, summary, trace);
                    rows++;
                }
            }
        }
        Debug.Log(FormattableString.Invariant($"H3_COMPLETE cohort={cohort} trials={rows} scalar={scalar.Cap:F2} contact={contact.Cap:F2}"));
    }

    static GripAcquisition LoadPolicy(string folder, string name, bool scoring) => scoring
        ? GripAcquisition.Restore(File.ReadAllBytes(Path.Combine(folder, $"phase-h3-{name}.skill")), "son", SubjectSeed)
        : new GripAcquisition("son", SubjectSeed);

    static void SavePolicy(string folder, string name, GripAcquisition policy) =>
        WorldSimulation.PublishCheckpoint(Path.Combine(folder, $"phase-h3-{name}.skill"), policy.Capture(),
            file => File.Exists(file) && GripAcquisition.Restore(File.ReadAllBytes(file), "son", SubjectSeed) != null);

    static void RunTrial(string cohort, Trial trial, int attempt, Policy policy, float cap, GripAcquisition acquisition,
        TextWriter summary, TextWriter trace)
    {
        bool adaptive = policy == Policy.Contact || policy == Policy.ContactFresh;
        using var hand = new NativeHandLab(SubjectSeed, trial.Mass, trial.Radius, trial.Friction, cap,
            orientation: trial.Yaw, objectSetup: trial.Object, contactGated: adaptive);
        var origin = hand.ObjectPosition;
        float slip = 0f, displacement = 0f, peakCap = cap;
        int hold = 0, firstCorrection = -1, liftStart = -1, applied = 0;
        bool grasp = false, lifted = false, dropped = false;
        long cpu = 0;
        while (!hand.Terminal && hand.Tick < TrialLimit)
        {
            var before = hand.Phase;
            float appliedCap = hand.EffectiveGripCap;
            if (before == GripPhase.Lift && liftStart < 0) liftStart = hand.Tick;
            int sinceLift = liftStart < 0 ? -1 : hand.Tick - liftStart;
            bool loading = before == GripPhase.Lift || before == GripPhase.Hold;
            var load = loading && trial.Change == Perturbation.AddedLoad && sinceLift >= 9 && sinceLift < 9 + LoadDurationTicks
                ? Vector3.down * .45f : Vector3.zero;
            var impulse = loading && trial.Change == Perturbation.LateralImpulse && sinceLift == 9
                ? Vector3.forward * .012f : Vector3.zero;
            if (load != Vector3.zero || impulse != Vector3.zero) applied++;
            long start = Stopwatch.GetTimestamp();
            hand.Step(load, impulse);
            cpu += Stopwatch.GetTimestamp() - start;
            var frame = hand.ReadFrame();
            if (hand.GripCorrections > 0 && firstCorrection < 0) firstCorrection = frame.Tick;
            peakCap = Mathf.Max(peakCap, hand.EffectiveGripCap);
            grasp |= hand.OpposingContact;
            lifted |= frame.Phase == GripPhase.Hold;
            dropped |= lifted && loading && frame.Clearance < DropClearance;
            hold = Math.Max(hold, frame.HeldTicks);
            displacement = Mathf.Max(displacement, Vector3.Distance(origin, frame.Object));
            if (loading) slip = Mathf.Max(slip, Mathf.Max(frame.Left.RelativeSpeed, frame.Right.RelativeSpeed));
            trace.WriteLine(FormattableString.Invariant($"{cohort},{trial.Id},{attempt},{policy},{frame.Tick},{before},{frame.Phase},{frame.HeldTicks},{Vec(load)},{Vec(impulse)},{appliedCap:F4},{hand.EffectiveGripCap:F4},{hand.GripCorrections},{hand.GripUncertain},{Vec(frame.Palm)},{Rot(frame.WristRotation)},{Vec(frame.Object)},{Rot(frame.ObjectRotation)},{Vec(frame.ObjectVelocity)},{frame.LeftTravel:F6},{frame.RightTravel:F6},{hand.FingerVelocity(0):F6},{hand.FingerVelocity(1):F6},{frame.LeftClosingForce:F5},{frame.RightClosingForce:F5},{frame.Left.NormalForce:F5},{frame.Right.NormalForce:F5},{frame.Left.TangentForce:F5},{frame.Right.TangentForce:F5},{frame.Left.ContactSeconds:F4},{frame.Right.ContactSeconds:F4},{frame.Left.RelativeSpeed:F6},{frame.Right.RelativeSpeed:F6},{frame.Left.Slip},{frame.Right.Slip},{Vec(frame.Left.Point)},{Vec(frame.Right.Point)},{frame.Clearance:F6},{frame.Work:F6}"));
        }
        if (!hand.FreeObject || !hand.Terminal) throw new InvalidOperationException("Trial exceeded its bounds or constrained the object.");
        bool success = hand.Phase == GripPhase.Complete;
        float retained = acquisition?.Observe(hand.SlipTicks, hold, hand.PeakClearance, success).After ?? cap;
        double tickUs = cpu * 1e6 / Stopwatch.Frequency / hand.Tick;
        string row = FormattableString.Invariant($"{cohort},{trial.Id},{attempt},{policy},{trial.Object.Shape},{trial.Mass:F3},{Vec(trial.Object.Size)},{trial.Friction:F2},{trial.Yaw:F0},{Vec(trial.Object.CentreOfMass)},{trial.Change},{applied},{cap:F3},{peakCap:F3},{retained:F3},{hand.GripCorrections},{firstCorrection},{grasp},{lifted},{hold * WorldSimulation.Dt:F3},{hand.Settled},{dropped},{success},{hand.Outcome},{hand.SlipTicks},{slip:F6},{hand.PeakPadForce:F5},{hand.DigitEffort:F5},{hand.Work:F6},{hand.PeakClearance * 1000:F3},{displacement * 1000:F3},{hand.PeakPenetration * 1000:F4},{hand.Tick},{tickUs:F2}");
        summary.WriteLine(row);
        summary.Flush();
        Debug.Log("H3_TRIAL " + row);
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F6};{v.y:F6};{v.z:F6}");
    static string Rot(Quaternion q) => FormattableString.Invariant($"{q.x:F6};{q.y:F6};{q.z:F6};{q.w:F6}");
}
