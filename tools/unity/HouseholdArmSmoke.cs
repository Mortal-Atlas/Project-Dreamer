using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DreamMachine.Sim;
using DreamMachine.Social;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class HouseholdArmSmoke
{
    const string Key = "Dreamer.H6.Household";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static string Primary => Path.Combine(Application.persistentDataPath, "world.checkpoint");
    static bool Visual(string mode) => mode.StartsWith("h8-", StringComparison.Ordinal);
    static bool Coordinated(string mode) => Visual(mode) || mode.StartsWith("h7-", StringComparison.Ordinal);
    static string Fixture(string mode) => Visual(mode) ? "phase-h8" : Coordinated(mode) ? "phase-h7" : "phase-h6";
    static string Prefix(string mode) => Fixture(mode) + "-household-";
    static string Action(string mode) => Coordinated(mode) ? mode.Substring(3) : mode;
    static string Archive(string mode) => Path.Combine(Application.persistentDataPath, Prefix(mode) + Action(mode) + ".checkpoint");
    static string RunMode(string mode) => Visual(mode) ? "h8-run" : Coordinated(mode) ? "h7-run" : "run";
    static string Trace(string mode) => Path.Combine(Application.persistentDataPath, Prefix(mode) + Action(mode) + "-trace.csv");
    static bool Restoring(string mode) => Action(mode).EndsWith("restart", StringComparison.Ordinal);
    static string RestoreSource(string mode) => Action(mode) == "return-restart" ? (Visual(mode) ? "h8-return" : "h7-return") : RunMode(mode);
    static HouseholdGripPolicy Policy(string mode) => Visual(mode) ? HouseholdGripPolicy.VisualCoordination :
        Coordinated(mode) ? HouseholdGripPolicy.CoordinatedPlacement : HouseholdGripPolicy.Articulated;

    static HouseholdArmSmoke()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run() => Start("run");
    public static void ObserverClosed() => Start("closed");
    public static void Alice() => Start("alice");
    public static void Restart() => Start("restart");

    public static void H7Run() => Start("h7-run");
    public static void H7ObserverClosed() => Start("h7-closed");
    public static void H7Alice() => Start("h7-alice");
    public static void H7Restart() => Start("h7-restart");
    public static void H7Return() => Start("h7-return");
    public static void H7RestartReturn() => Start("h7-return-restart");
    public static void H8Run() => Start("h8-run");
    public static void H8ObserverClosed() => Start("h8-closed");
    public static void H8Alice() => Start("h8-alice");
    public static void H8Restart() => Start("h8-restart");
    public static void H8Return() => Start("h8-return");
    public static void H8RestartReturn() => Start("h8-return-restart");

    static void Start(string mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        if (Restoring(mode) || Action(mode) == "return")
        {
            long started = Stopwatch.GetTimestamp();
            WorldSimulation.PublishCheckpoint(Primary, File.ReadAllBytes(Archive(RestoreSource(mode))),
                path => WorldSimulation.LoadCheckpoint(path) != null);
            Debug.Log(FormattableString.Invariant($"HOUSEHOLD_RESTORE_READ {Fixture(mode)} publish_and_validate_ms={(Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency:F2}"));
        }
        else
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
            world.EnableHouseholdLife();
            world.Find("son").State.Library.NoteIdea(Fixture(mode) + "-existing", "Existing Arthur record", .3f, 0, "test-setup");
            world.Find("daughter").State.Library.NoteIdea(Fixture(mode) + "-existing", "Existing Alice record", .3f, 0, "test-setup");
            world.SaveCheckpoint(Primary);
        }
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.household.physical-practice");
        SessionState.SetString(Key, mode);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseString(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        var app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null) app = new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
        Get<TimeGovernor>(app, "_governor").Paused = true;
        EditorApplication.update += Verify;
    }

    static void Verify()
    {
        var app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null || !Get<bool>(app, "_ready")) return;
        EditorApplication.update -= Verify;
        try
        {
            var world = Get<WorldSimulation>(app, "_sim");
            var home = world.Home;
            if (!Get<TimeGovernor>(app, "_governor").Paused) throw new InvalidOperationException("Expected a paused household.");
            Click("Physical development");
            Click("Ball / switch learner");
            string mode = SessionState.GetString(Key, "");
            if (Restoring(mode))
            {
                byte[] saved = File.ReadAllBytes(Archive(RestoreSource(mode)));
                var latest = home.BallTransfer(home.BallTransferCount - 1);
                string restoredActor = home.PersonId(latest.Intent.Actor);
                if (!world.CaptureCheckpoint().SequenceEqual(saved) || latest.Intent.Policy != Policy(mode))
                    throw new InvalidOperationException("Fresh process did not restore the articulated outcome.");
                var pose = home.ReadBallPose();
                Vector3 expected = new Vector3(pose.X, pose.Y, pose.Z);
                if (Vector3.Distance(Get<Transform[]>(app, "_livingItems")[HouseholdLife.BallItem].position, expected) > .0001f ||
                    world.Find(restoredActor).State.Library.GetOrNull(latest.MemoryId) == null ||
                    (Coordinated(mode) && !HasTracking(world, restoredActor, latest, mode)))
                    throw new InvalidOperationException("The settled object or firsthand evidence was missing.");
                if (Action(mode) == "return-restart")
                {
                    if (restoredActor != "daughter" || (!Visual(mode) && latest.Success) || !latest.Intent.Before.Equals(home.BallTransfer(0).After))
                        throw new InvalidOperationException("The displaced Ball or the original transfer was replaced.");
                    Click("Ball / switch learner");
                }
                VerifyReplay(app, world, mode, true);
                Debug.Log($"{Fixture(mode)}_RESTART mode={mode} actor={restoredActor} tick={world.TickCount} outcome={latest.Outcome} hash={Digest(saved)}");
                EditorApplication.ExitPlaymode();
                return;
            }
            bool returning = Action(mode) == "return";
            int priorAttempts = home.BallTransferCount;
            if (Action(mode) == "alice" || returning) Click("Ball / switch learner");
            Click(Visual(mode) ? "Visual coordination" : Coordinated(mode) ? "Coordinated placement" : "Articulated arm");
            var arm = Get<NativeHandLab>(app, "_practiceHand");
            if (arm == null || !arm.Articulated || arm.Coordinated != Coordinated(mode) || arm.VisualCoordination != Visual(mode) ||
                !arm.FreeObject || !home.BallTransferRunning)
                throw new InvalidOperationException("Expected the persistent free Ball and supported articulated chain.");
            var intent = home.BallIntent.Value;
            if (returning && (priorAttempts != 1 || !intent.Before.Equals(home.BallTransfer(0).After) || intent.TargetTop != 0f))
                throw new InvalidOperationException("The return must start with the same Ball settled on the previous destination.");
            string actor = home.PersonId(intent.Actor);
            if (actor != (Action(mode) == "alice" || returning ? "daughter" : "son")) throw new InvalidOperationException("Wrong learner selected.");
            var camera = Camera.main;
            if (Action(mode) == "closed") { Click("Physical development"); camera.enabled = false; }
            arm.Profile = true;
            bool captured = false;
            float peakClearance = 0;
            using var trace = File.CreateText(Trace(mode));
            trace.WriteLine("learner,tick,phase,requested_wrist_m,proposed_elbow_m,actual_wrist_m,actual_elbow_m,ball_m,joint_degrees,joint_rad_s,actuator_torque_nm,torso_s,upper_to_hand_s,torso_depth_mm,left_n,right_n,support_n,clearance_m,corrections" +
                (Coordinated(mode) ? ",tracking,estimated_ball_ms,steady_ticks,observation,drift_m" : "") +
                (Visual(mode) ? ",hand_tick,ball_tick,hand_conf,ball_conf,correction_m" : ""));
            long begin = Stopwatch.GetTimestamp();
            for (int i = 0; i < 700 && home.BallTransferRunning; i++)
            {
                bool observeRelease = arm.Phase == GripPhase.Release && arm.PlacementTracked &&
                    arm.PlacementObservation != DreamMachine.Body.PlacementObservation.None;
                if ((Coordinated(mode) ? observeRelease : arm.Phase == GripPhase.Hold) && !captured && Action(mode) == "run")
                {
                    Capture(app, Prefix(mode) + (Coordinated(mode) ? "release" : "hold"));
                    captured = true;
                }
                Step(app);
                var a = arm.ReadArm();
                var ball = arm.ObjectPosition + Get<Vector3>(app, "_ballOrigin");
                peakClearance = Mathf.Max(peakClearance, arm.Clearance);
                string sample = FormattableString.Invariant($"{actor},{arm.Tick},{arm.Phase},{Vec(a.Target)},{Vec(a.ProposedElbow)},{Vec(a.Wrist)},{Vec(a.Elbow)},{Vec(ball)},{Vec(a.JointDegrees)},{Vec(a.JointSpeed)},{Vec(a.ActuatorTorque)},{a.TorsoSeconds:F4},{a.UpperDistalSeconds:F4},{1000f * a.TorsoDepth:F3},{arm.ReadPad(0).NormalForce:F4},{arm.ReadPad(1).NormalForce:F4},{arm.SupportForce:F4},{arm.Clearance:F6},{a.Corrections}");
                if (Coordinated(mode)) sample += FormattableString.Invariant($",{arm.PlacementTracked},{arm.EstimatedBallSpeed:F5},{arm.StableSupportTicks},{arm.PlacementObservation},{arm.ReleaseDrift:F5}");
                if (Visual(mode))
                {
                    var eye = arm.VisualReading;
                    sample += FormattableString.Invariant($",{eye.Hand.ObservationTick},{eye.Ball.ObservationTick},{eye.Hand.Confidence:F4},{eye.Ball.Confidence:F4},{Vec(eye.Correction)}");
                }
                trace.WriteLine(sample);
                if (arm.Tick != world.TickCount - intent.Start) throw new InvalidOperationException("Clock divergence.");
            }
            double ms = (Stopwatch.GetTimestamp() - begin) * 1000d / Stopwatch.Frequency / arm.Tick;
            if (!arm.HouseholdSettled || home.BallTransferRunning || home.BallTransferCount != priorAttempts + 1)
                throw new InvalidOperationException("Arm attempt did not reach its settled commit boundary.");
            if (Action(mode) == "closed") { camera.enabled = true; Click("Physical development"); }
            var record = home.BallTransfer(priorAttempts);
            var owner = world.Find(actor);
            var other = world.Find(actor == "son" ? "daughter" : "son");
            var memory = owner.State.Library.GetOrNull(record.MemoryId);
            if (!record.After.Equals(home.ReadBallPose()) || memory == null ||
                other.State.Library.GetOrNull(record.MemoryId) != null ||
                other.State.Library.GetOrNull(Fixture(mode) + "-existing") == null)
                throw new InvalidOperationException("Persistent object or individual evidence did not survive.");
            if (Coordinated(mode) && ((!Visual(mode) && !returning && !record.Success) || !HasTracking(world, actor, record, mode) ||
                owner.State.Library.GetOrNull(Fixture(mode) + "-existing") == null))
                throw new InvalidOperationException("The coordinated placement or its firsthand tracking evidence was missing.");
            VerifyReplay(app, world, mode, Action(mode) == "run");
            byte[] checkpoint = world.CaptureCheckpoint();
            trace.Dispose();
            if (Coordinated(mode) && Action(mode) == "closed" &&
                (!checkpoint.SequenceEqual(File.ReadAllBytes(Archive(RunMode(mode)))) ||
                 !File.ReadAllLines(Trace(mode)).SequenceEqual(File.ReadAllLines(Trace(RunMode(mode))))))
                throw new InvalidOperationException("Closing observation changed the native trajectory or household.");
            WorldSimulation.PublishCheckpoint(Archive(mode), checkpoint,
                path => WorldSimulation.LoadCheckpoint(path) != null);
            if (!File.ReadAllBytes(Archive(mode)).SequenceEqual(checkpoint))
                throw new InvalidOperationException("The archived bytes differ from the verified outcome.");
            var final = arm.ReadArm();
            double scale = 1000d / Stopwatch.Frequency / arm.Tick;
            int tape = (int)new FileInfo(Path.Combine(Application.persistentDataPath, "motor-trajectories", record.Tape + ".h5")).Length;
            Debug.Log(FormattableString.Invariant($"{Fixture(mode)}_HOUSE mode={mode} actor={actor} seed={owner.Spec.BirthSeed} tick={world.TickCount} outcome={record.Outcome} clearance_mm={1000 * peakClearance:F2} work_j={final.UpperWork+final.ElbowWork+final.WristWork:F4} torso_s={final.TorsoSeconds:F3} self_s={final.UpperDistalSeconds:F3} cpu_ms={ms:F3} physics_ms={arm.PhysicsCpuTicks*scale:F3} command_ms={arm.CommandCpuTicks*scale:F3} tracking_ms={arm.TrackingCpuTicks*scale:F3} visual_ms={arm.VisualCpuTicks*scale:F3} fusion_ms={arm.FusionCpuTicks*scale:F3} bytes={checkpoint.Length} tape={tape} hash={Digest(checkpoint)}"));
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static bool HasTracking(WorldSimulation world, string actor, BallTransferRecord record, string mode) =>
        world.Find(actor).State.Library.GetOrNull(record.MemoryId)?.Appendices.Any(entry => Visual(mode)
            ? entry.Evidence.Contains("Bench depth saw") || entry.Evidence.Contains("Ball uncertain in the final bench view.")
            : entry.Evidence.Contains("Settled bench tracking:")) == true;

    static void VerifyReplay(DreamMachineApp app, WorldSimulation world, string mode, bool capture)
    {
        byte[] before = world.CaptureCheckpoint();
        Click("Earlier attempt");
        for (int i = 0; i < (Coordinated(mode) ? 145 : 45); i++) Click("Next frame");
        if (Get<Array>(app, "_practiceReplay") == null ||
            !Get<Transform[]>(app, "_armParts").All(x => x.gameObject.activeSelf))
            throw new InvalidOperationException("Recorded chain was not reconstructed.");
        if (capture) Capture(app, Prefix(mode) + "replay");
        Click("Live view");
        if (!before.SequenceEqual(world.CaptureCheckpoint())) throw new InvalidOperationException("Replay changed authoritative bytes.");
    }

    static void Step(DreamMachineApp app)
    {
        var clock = Get<TimeGovernor>(app, "_governor");
        clock.RequestSingleStep();
        if (!clock.TakeSingleStep()) throw new InvalidOperationException("Step request missing.");
        Call(app, "AdvanceHouseholdTick");
        Call(app, "StepActors", WorldSimulation.Dt);
    }

    static void Capture(DreamMachineApp app, string label)
    {
        Call(app, "RefreshPractice");
        Call(app, "OrbitCamera");
        var camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .44f; }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < 8; i++) camera.Render();
            Debug.Log(FormattableString.Invariant($"HOUSEHOLD_RENDER {label} cpu_submit_ms={(Stopwatch.GetTimestamp()-start)*1000d/Stopwatch.Frequency/8:F3}"));
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, label + ".png"), texture.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
        }
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F5};{v.y:F5};{v.z:F5}");
    static string Digest(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    static T Get<T>(DreamMachineApp app, string name) => (T)typeof(DreamMachineApp).GetField(name, Private).GetValue(app);
    static void Call(DreamMachineApp app, string name, params object[] args) => typeof(DreamMachineApp).GetMethod(name, Private).Invoke(app, args);
    static void Click(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name).onClick.Invoke();
}
