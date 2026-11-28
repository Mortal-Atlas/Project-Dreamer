using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using DreamMachine.Body;
using DreamMachine.Libraries;
using DreamMachine.Sim;
using DreamMachine.Social;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class TactilePlacementStudy
{
    const string SessionKey = "Dreamer.H4.Placement";
    const int Limit = 235;
    enum Policy { Fixed, Scalar, H3Contact, Tactile, ForceOnly, Reset }

    readonly struct Case
    {
        public readonly string Name;
        public readonly float Mass, Radius, Friction, Yaw, Load, Impulse;
        public readonly HandObjectSetup Shape;
        public readonly Vector2 Offset;

        public Case(string name, float mass, float radius, float friction, float yaw, float load = 0, float impulse = 0,
            float capsule = 0, Vector3 box = default, Vector3 com = default, Vector2 offset = default)
        {
            Name = name; Mass = mass; Radius = radius; Friction = friction; Yaw = yaw; Load = load; Impulse = impulse; Offset = offset;
            var shape = box != default ? HandObjectShape.Box : capsule > 0f ? HandObjectShape.Capsule : HandObjectShape.Sphere;
            Shape = new HandObjectSetup(shape, box != default ? box : new Vector3(2 * radius, capsule > 0f ? capsule : 2 * radius, 2 * radius), com);
        }
    }

    // New development cases only. The original H3 held-out set is never run by this harness.
    static readonly Case[] Develop =
    {
        new Case("dev-sphere", .081f, .027f, .74f, 7f),
        new Case("dev-load", .091f, .031f, .43f, 23f, load: .40f),
        new Case("dev-box", .070f, .024f, .68f, 16f, box: new Vector3(.045f,.042f,.052f)),
        new Case("dev-capsule", .113f, .0255f, .40f, 46f, capsule: .071f),
        new Case("dev-offset-capsule", .120f, .0245f, .56f, 57f, capsule: .066f, com: new Vector3(.005f,-.004f,.003f)),
        new Case("dev-impulse", .076f, .028f, .63f, 32f, impulse: -.010f),
        new Case("dev-displaced-box", .070f, .024f, .68f, 16f, box: new Vector3(.045f,.042f,.052f), offset: new Vector2(.019f,.022f))
    };

    // Fixed before scoring; the two mass/friction contrasts and yaw contrast share all other descriptors.
    static readonly Case[] HeldOutCases =
    {
        new Case("new-reference", .086f, .029f, .64f, 11f),
        new Case("new-mass", .139f, .029f, .64f, 11f),
        new Case("new-friction", .086f, .029f, .34f, 11f),
        new Case("new-load", .086f, .029f, .64f, 11f, load: .58f),
        new Case("new-impulse", .086f, .029f, .64f, 11f, impulse: .016f),
        new Case("new-box-low-yaw", .072f, .024f, .62f, 9f, box: new Vector3(.043f,.049f,.051f)),
        new Case("new-box-high-yaw", .072f, .024f, .62f, 41f, box: new Vector3(.043f,.049f,.051f)),
        new Case("new-capsule", .096f, .024f, .71f, 17f, capsule: .060f),
        new Case("new-offset-capsule", .126f, .026f, .47f, 68f, capsule: .076f, com: new Vector3(-.004f,-.004f,.004f)),
        new Case("new-wide-loaded", .104f, .034f, .45f, 47f, load: .49f),
        new Case("new-box-displaced", .072f, .024f, .62f, 9f, box: new Vector3(.043f,.049f,.051f), offset: new Vector2(-.016f,.024f))
    };

    static TactilePlacementStudy()
    {
        if (SessionState.GetString(SessionKey, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Diagnose() => Start("diagnosis");
    public static void Development() => Start("development");
    public static void HeldOut() => Start("heldout");

    static void Start(string cohort)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetString(SessionKey, cohort);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseString(SessionKey); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Run(SessionState.GetString(SessionKey, "")); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Run(string cohort)
    {
        string folder = Application.persistentDataPath;
        string prefix = Path.Combine(folder, "phase-h4-" + cohort);
        using var summary = File.CreateText(prefix + "-summary.csv");
        using var trace = File.CreateText(prefix + "-trace.csv");
        using var events = File.CreateText(prefix + "-events.csv");
        summary.WriteLine("cohort,subject,seed,case,repeat,policy,shape,mass_kg,size_m,friction,yaw_deg,com_m,start_offset_m,load_n,impulse_ns,applied_ticks,cap_before_n,peak_cap_n,cap_after_n,corrections,repositions,early_lowers,first_effect_tick,grasp,bilateral_ticks,asymmetric_ticks,non_pad_ticks,contact_loss_edges,lift,held_s,placement,release,success,escaped,slip_ticks,peak_slip_m_s,peak_force_n,work_linear_j,work_wrist_j,effort_ns,settle_turn_deg,peak_turn_deg,peak_clearance_mm,max_displacement_mm,terminal_x_mm,support_x_mm,postrelease_drift_mm,peak_penetration_mm,native_ticks,tick_us,outcome,lived_record");
        const string traceHeader = "cohort,subject,case,repeat,policy,tick,before,phase,load_n,impulse_ns,cap_applied_n,cap_next_n,corrections,repositions,early_lowers,grasp_offset_m,pitch_cmd_deg,palm_m,wrist_xyzw,position_m,rotation_xyzw,velocity_m_s,angular_rad_s,left_travel_m,right_travel_m,left_speed_m_s,right_speed_m_s,left_effort_n,right_effort_n,left_n,right_n,left_tangent_n,right_tangent_n,left_duration_s,right_duration_s,left_speed_m_s_contact,right_speed_m_s_contact,left_slip,right_slip,left_point_m,right_point_m,non_pad,clearance_m,support_n,support_x_m,held_ticks,controlled_placement";
        trace.WriteLine(traceHeader);
        events.WriteLine(traceHeader);
        int rows = 0;
        if (cohort == "diagnosis")
        {
            foreach (Case item in Develop)
            {
                Attempt(cohort, item, 1, "son", 303, Policy.Fixed, 3.2f, false, null, null, summary, trace, events);
                Attempt(cohort, item, 1, "son", 303, Policy.H3Contact, .18f, false, null, null, summary, trace, events);
                rows += 2;
            }
        }
        else if (cohort == "development")
        {
            var scalar = NewSkill("son");
            var tactile = NewSkill("son");
            var alice = NewSkill("daughter");
            var scalarHistory = new LibraryThree();
            var tactileHistory = new LibraryThree();
            var aliceHistory = new LibraryThree();
            for (int repeat = 1; repeat <= 2; repeat++)
            foreach (Case item in Develop)
            {
                for (int i = 0; i < 5; i++)
                {
                    var policy = (Policy)i;
                    var training = policy == Policy.Scalar ? scalar : policy == Policy.Tactile ? tactile : null;
                    var history = policy == Policy.Scalar ? scalarHistory : policy == Policy.Tactile ? tactileHistory : null;
                    Attempt(cohort, item, repeat, tactile.Individual, tactile.BirthSeed, policy,
                        training?.Cap ?? (policy == Policy.Fixed ? GripAcquisition.ReferenceCap : GripAcquisition.InitialCap),
                        true, training, history, summary, trace, events);
                    rows++;
                }
                Attempt(cohort, item, repeat, alice.Individual, alice.BirthSeed, Policy.Tactile, alice.Cap,
                    true, alice, aliceHistory, summary, trace, events);
                rows++;
            }
            Save(folder, "arthur-scalar", scalar, scalarHistory);
            Save(folder, "arthur-tactile", tactile, tactileHistory);
            Save(folder, "alice-tactile", alice, aliceHistory);
            Debug.Log(FormattableString.Invariant($"H4_TRAINING scalar={scalar.Cap:F2} arthur={tactile.Cap:F2} alice={alice.Cap:F2}"));
        }
        else
        {
            var scalar = Load(Path.Combine(folder, "phase-h4-arthur-scalar.practice"), "son");
            var arthur = Load(Path.Combine(folder, "phase-h4-arthur-tactile.practice"), "son");
            var alice = Load(Path.Combine(folder, "phase-h4-alice-tactile.practice"), "daughter");
            for (int i = 0; i < HeldOutCases.Length; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    var policy = (Policy)((i + j) % 6);
                    Attempt(cohort, HeldOutCases[i], 1, arthur.Individual, arthur.BirthSeed, policy,
                        StartCap(policy, arthur.Cap, scalar.Cap), true, null, null, summary, trace, events);
                    rows++;
                }
                if (i != 0 && i != 5 && i != 7 && i != 9) continue;
                RunRetention(HeldOutCases[i], cohort, alice, summary, trace, events);
                rows += 2;
            }
            foreach (var familiar in new[] { Develop[0], Develop[2] })
            {
                RunRetention(familiar, "familiar", arthur, summary, trace, events);
                RunRetention(familiar, "familiar", alice, summary, trace, events);
                rows += 4;
            }
        }
        Debug.Log($"H4_COMPLETE cohort={cohort} rows={rows}");
    }

    static GripAcquisition NewSkill(string individual) =>
        new GripAcquisition(individual, PopulationConfig.CreateDefault().Find(individual).BirthSeed);

    static float StartCap(Policy policy, float tactile, float scalar) => policy switch
    {
        Policy.Fixed => GripAcquisition.ReferenceCap,
        Policy.Scalar => scalar,
        Policy.Tactile or Policy.ForceOnly => tactile,
        _ => GripAcquisition.InitialCap
    };

    static void RunRetention(Case item, string cohort, GripAcquisition skill, TextWriter summary, TextWriter trace, TextWriter events)
    {
        Attempt(cohort, item, 1, skill.Individual, skill.BirthSeed, Policy.Tactile, skill.Cap, true, null, null, summary, trace, events);
        Attempt(cohort, item, 1, skill.Individual, skill.BirthSeed, Policy.Reset, GripAcquisition.InitialCap, true, null, null, summary, trace, events);
    }

    static void Save(string folder, string name, GripAcquisition skill, LibraryThree history)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write("DreamMachine.H4.SupportedResearch.v1");
        byte[] bytes = skill.Capture();
        writer.Write(bytes.Length);
        writer.Write(bytes);
        writer.Write(history.ToJson());
        writer.Flush();
        WorldSimulation.PublishCheckpoint(Path.Combine(folder, "phase-h4-" + name + ".practice"),
            stream.ToArray(), path => File.Exists(path) && Load(path, skill.Individual).History.Count == skill.History.Count);
    }

    static GripAcquisition Load(string path, string individual)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (stream.Length > 1048576 || reader.ReadString() != "DreamMachine.H4.SupportedResearch.v1")
            throw new InvalidDataException("Invalid research practice archive.");
        int count = reader.ReadInt32();
        if (count <= 0 || count > 16384) throw new InvalidDataException("Invalid research skill payload.");
        var skill = GripAcquisition.Restore(reader.ReadBytes(count), individual, NewSkill(individual).BirthSeed);
        var history = LibraryThree.FromJson(reader.ReadString());
        if (stream.Position != stream.Length || history.All().Count != skill.History.Count)
            throw new InvalidDataException("Research acquisition and lived evidence disagree.");
        for (int i = 1; i <= skill.History.Count; i++)
            if (history.GetOrNull("motor:h4:grip:" + i)?.Source != "firsthand-supported-grip")
                throw new InvalidDataException("Missing firsthand evidence for supported practice.");
        return skill;
    }

    static void Attempt(string cohort, Case item, int repeat, string subject, int seed, Policy policy, float cap, bool placement,
        GripAcquisition acquisition, LibraryThree library, TextWriter summary, TextWriter trace, TextWriter events)
    {
        var exploration = policy == Policy.Tactile || policy == Policy.Reset ? GraspExploration.Tactile :
            policy == Policy.ForceOnly ? GraspExploration.ForceOnly : GraspExploration.Off;
        using var lab = new NativeHandLab(seed, item.Mass, item.Radius, item.Friction, cap, orientation: item.Yaw,
            objectSetup: item.Shape, contactGated: policy == Policy.H3Contact, placement: placement, exploration: exploration,
            objectOffset: item.Offset);
        var initial = lab.ObjectPosition;
        var rotation = lab.ObjectRotation;
        var release = initial;
        float peakCap = cap, turn = 0f, settleTurn = 0f, travel = 0f, slip = 0f, drift = 0f;
        int lift = -1, applied = 0, held = 0, stable = 0, asymmetric = 0, nonPad = 0, loss = 0, firstCorrection = -1;
        bool airborne = false, opposed = false, escaped = false, grasp = false;
        long cpu = 0;
        while (!lab.Terminal && lab.Tick < Limit)
        {
            var before = lab.Phase;
            if (before == GripPhase.Lift && lift < 0) lift = lab.Tick;
            int sinceLift = lift < 0 ? -1 : lab.Tick - lift;
            bool exposed = before == GripPhase.Lift || before == GripPhase.Hold;
            float load = exposed && sinceLift >= 9 && sinceLift < 15 ? item.Load : 0f;
            float impulse = exposed && sinceLift == 9 ? item.Impulse : 0f;
            if (load != 0f || impulse != 0f) applied++;
            float previous = lab.EffectiveGripCap;
            long start = Stopwatch.GetTimestamp();
            lab.Step(Vector3.down * load, Vector3.forward * impulse);
            cpu += Stopwatch.GetTimestamp() - start;
            var frame = lab.ReadFrame();
            bool left = frame.Left.NormalForce >= GripCalibration.ContactForce, right = frame.Right.NormalForce >= GripCalibration.ContactForce;
            bool contact = left && right;
            bool lost = opposed && !contact && (exposed || before == GripPhase.Transport);
            if (lost) loss++;
            if (contact) stable++;
            if (left != right) asymmetric++;
            if (lab.OtherContacts > 0) nonPad++;
            grasp |= contact;
            airborne |= frame.Phase == GripPhase.Hold;
            escaped |= frame.Clearance < -.025f;
            opposed = contact;
            held = Math.Max(held, frame.HeldTicks);
            if (previous != lab.EffectiveGripCap && firstCorrection < 0) firstCorrection = frame.Tick + 1;
            peakCap = Mathf.Max(peakCap, lab.EffectiveGripCap);
            travel = Mathf.Max(travel, Vector3.Distance(initial, frame.Object));
            turn = Mathf.Max(turn, Quaternion.Angle(rotation, frame.ObjectRotation));
            if (before == GripPhase.Settle) settleTurn = turn;
            if (exposed || before == GripPhase.Transport)
                slip = Mathf.Max(slip, frame.Left.RelativeSpeed, frame.Right.RelativeSpeed);
            if (before != GripPhase.Release && frame.Phase == GripPhase.Release) release = frame.Object;
            if (before == GripPhase.Release)
                drift = Mathf.Max(drift, Vector2.Distance(new Vector2(release.x, release.z), new Vector2(frame.Object.x, frame.Object.z)));
            string row = FormattableString.Invariant($"{cohort},{subject},{item.Name},{repeat},{policy},{frame.Tick},{before},{frame.Phase},{load:F3},{impulse:F4},{previous:F3},{lab.EffectiveGripCap:F3},{lab.GripCorrections},{lab.Repositions},{lab.EarlyLowers},{Vec(lab.GraspOffset)},{lab.PitchTarget:F3},{Vec(frame.Palm)},{Rot(frame.WristRotation)},{Vec(frame.Object)},{Rot(frame.ObjectRotation)},{Vec(frame.ObjectVelocity)},{Vec(lab.ObjectAngularVelocity)},{frame.LeftTravel:F5},{frame.RightTravel:F5},{lab.FingerVelocity(0):F5},{lab.FingerVelocity(1):F5},{frame.LeftClosingForce:F5},{frame.RightClosingForce:F5},{frame.Left.NormalForce:F5},{frame.Right.NormalForce:F5},{frame.Left.TangentForce:F5},{frame.Right.TangentForce:F5},{frame.Left.ContactSeconds:F3},{frame.Right.ContactSeconds:F3},{frame.Left.RelativeSpeed:F5},{frame.Right.RelativeSpeed:F5},{frame.Left.Slip},{frame.Right.Slip},{Vec(frame.Left.Point)},{Vec(frame.Right.Point)},{lab.OtherContacts},{frame.Clearance:F5},{frame.SupportForce:F5},{lab.SupportX:F5},{frame.HeldTicks},{lab.ControlledPlacement}");
            trace.WriteLine(row);
            if (frame.Phase != before || load != 0f || impulse != 0f || lost || previous != lab.EffectiveGripCap) events.WriteLine(row);
        }
        if (!lab.Terminal || !lab.FreeObject) throw new InvalidOperationException("Unfinished trial or constrained object: " + item.Name);
        bool success = lab.Phase == GripPhase.Complete;
        var practice = acquisition?.Observe(lab.SlipTicks, held, lab.PeakClearance, success);
        string record = "";
        if (library != null && practice.HasValue)
        {
            int index = practice.Value.Attempt;
            record = "motor:h4:grip:" + index;
            library.RecordSupportedGraspObservation(index, index * Limit + lab.Tick, lab.Outcome,
                FormattableString.Invariant($"phase-h4-{cohort}-trace.csv/{subject}/{item.Name}/{repeat}/{policy}"));
        }
        double us = cpu * 1e6 / Stopwatch.Frequency / lab.Tick;
        summary.WriteLine(FormattableString.Invariant($"{cohort},{subject},{seed},{item.Name},{repeat},{policy},{item.Shape.Shape},{item.Mass:F3},{Vec(item.Shape.Size)},{item.Friction:F2},{item.Yaw:F1},{Vec(item.Shape.CentreOfMass)},{item.Offset.x:F5};{item.Offset.y:F5},{item.Load:F3},{item.Impulse:F4},{applied},{cap:F3},{peakCap:F3},{practice?.After ?? cap:F3},{lab.GripCorrections},{lab.Repositions},{lab.EarlyLowers},{firstCorrection},{grasp},{stable},{asymmetric},{nonPad},{loss},{airborne},{held * WorldSimulation.Dt:F3},{lab.ControlledPlacement},{lab.Settled},{success},{escaped},{lab.SlipTicks},{slip:F5},{lab.PeakPadForce:F5},{lab.Work:F6},{lab.WristWork:F6},{lab.DigitEffort:F5},{settleTurn:F2},{turn:F2},{lab.PeakClearance * 1000:F3},{travel * 1000:F3},{lab.ObjectPosition.x * 1000:F3},{lab.SupportX * 1000:F3},{drift * 1000:F3},{lab.PeakPenetration * 1000:F4},{lab.Tick},{us:F2},{lab.Outcome},{record}"));
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F5};{v.y:F5};{v.z:F5}");
    static string Rot(Quaternion q) => FormattableString.Invariant($"{q.x:F5};{q.y:F5};{q.z:F5};{q.w:F5}");
}
