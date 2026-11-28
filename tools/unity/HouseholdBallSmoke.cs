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
internal static class HouseholdBallSmoke
{
    const string Key = "Dreamer.H5.HouseholdBall";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    const string BaselineMemory = "phase-h5-baseline";
    static string WorldPath => Path.Combine(Application.persistentDataPath, "world.checkpoint");

    static HouseholdBallSmoke()
    {
        if (SessionState.GetString(Key, "") != "") EditorApplication.playModeStateChanged += Changed;
    }

    public static void Pilot() => Start("pilot");
    public static void PilotReturn() => Start("pilot-return");
    public static void Run() => Start("run");
    public static void Unobserved() => Start("unobserved");
    public static void Failure() => Start("failure");
    public static void Feedback() => Start("feedback");
    public static void Restart() => Start("restart");
    public static void RestartFailure() => Start("restart-failure");

    static void Start(string mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Guarded scratch only.");
        if (mode.StartsWith("restart", StringComparison.Ordinal))
        {
            var archive = Archive(mode == "restart-failure" ? "failure" : "run");
            if (!File.Exists(archive)) throw new InvalidOperationException("Recorded native checkpoint is required.");
            WorldSimulation.PublishCheckpoint(WorldPath, File.ReadAllBytes(archive),
                candidate => WorldSimulation.LoadCheckpoint(candidate)?.Home.BallTransferCount == 2);
        }
        else
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
            world.EnableHouseholdLife();
            world.Find("son").State.Library.NoteIdea(BaselineMemory, "Existing Arthur record", .3f, 0, "test-setup");
            string backup = WorldPath + ".before-h5";
            if (File.Exists(WorldPath) && !File.Exists(backup)) File.Copy(WorldPath, backup);
            world.SaveCheckpoint(WorldPath);
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

    static T Get<T>(DreamMachineApp app, string name) => (T)typeof(DreamMachineApp).GetField(name, Private).GetValue(app);
    static void Call(DreamMachineApp app, string name, params object[] args) =>
        typeof(DreamMachineApp).GetMethod(name, Private).Invoke(app, args);
    static void Click(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .Single(button => button.name == name).onClick.Invoke();

    static void Verify()
    {
        var app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null || !Get<bool>(app, "_ready")) return;
        EditorApplication.update -= Verify;
        try
        {
            var world = Get<WorldSimulation>(app, "_sim");
            if (Get<Text>(app, "_practiceText") != null) Click("Physical development");
            Click("Physical development");
            Click("Ball / switch learner");
            string mode = SessionState.GetString(Key, "");
            string prefix = "phase-h5-" + mode;
            using var trace = File.CreateText(Path.Combine(Application.persistentDataPath, prefix + "-trace.csv"));
            trace.WriteLine("label,tick,phase,palm_x_m,palm_y_m,palm_z_m,ball_x_m,ball_y_m,ball_z_m,x_q,y_q,z_q,w_q,ball_speed_m_s,ball_angular_rad_s,left_n,right_n,support_n,clearance_m,peak_penetration_m,work_j,effort_ns,controlled_place,held_ticks,wrist_angle_deg,grip_cap_n,corrections");
            if (mode.StartsWith("restart", StringComparison.Ordinal))
                VerifyRestart(app, world, mode, trace);
            else if (mode == "run" || mode == "unobserved" || mode == "failure")
                VerifyNew(app, world, mode, trace);
            else if (mode == "feedback")
            {
                var result = Transfer(app, world, "arthur-feedback", false, trace, capture: true, feedback: true);
                if (result.Corrections < 1) throw new InvalidOperationException("No native pad feedback was applied.");
            }
            else
            {
                Transfer(app, world, "pilot-fixed", false, trace);
                if (mode == "pilot-return")
                {
                    Click("Ball / switch learner");
                    Transfer(app, world, "pilot-alice-return", false, trace);
                    Click("Ball / switch learner");
                    CheckBelief(world, requireCorrect: false);
                }
                else Transfer(app, world, "pilot-light", true, trace);
            }
            Debug.Log($"H5_COMPLETE mode={mode} tick={world.TickCount} records={world.Home.BallTransferCount}");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static string Archive(string phase) => Path.Combine(Application.persistentDataPath, "phase-h5-" + phase + ".checkpoint");

    static void VerifyNew(DreamMachineApp app, WorldSimulation world, string mode, TextWriter trace)
    {
        bool rendering = mode != "unobserved";
        var before = world.CaptureCheckpoint();
        var first = Transfer(app, world, "arthur-to-table", false, trace, rendering, capture: mode == "run");
        if (!first.Success || !first.Seen || Math.Abs(first.After.X - first.Intent.Before.X) < .20f ||
            first.After.Y < first.Intent.Before.Y + .045f)
            throw new InvalidOperationException("Arthur did not physically place the household ball on its table.");
        if (mode != "failure") Click("Ball / switch learner");
        var prior = world.Home.ReadBallBelief(2);
        var second = Transfer(app, world, mode == "failure" ? "arthur-light-loss" : "alice-to-floor",
            mode == "failure", trace, rendering, capture: mode != "unobserved");
        if (!second.Intent.Before.Equals(first.After)) throw new InvalidOperationException("The second body started from another pose.");
        if (mode == "failure")
        {
            if (second.Success || Math.Abs(second.After.X - second.Intent.Before.X) < .15f || second.After.Y > first.After.Y - .04f ||
                !world.Home.ReadBallBelief(2).Uncertain)
                throw new InvalidOperationException("The failed lift lacked a persistent unexpected consequence.");
            Capture(app, "phase-h5-arthur-light-loss-settled");
        }
        else
        {
            if (!second.Success || !second.Seen || second.Intent.Actor != 3 || Math.Abs(second.After.X - prior.X) < .20f ||
                !world.Home.ReadBallBelief(2).Equals(prior))
                throw new InvalidOperationException("The unsampled transfer did not leave Arthur's old sighting intact.");
            var a = world.Find("son").State.Library;
            var b = world.Find("daughter").State.Library;
            if (a.GetOrNull(first.MemoryId)?.Source != "firsthand-ball-transfer" || a.GetOrNull(second.MemoryId) != null ||
                b.GetOrNull(second.MemoryId)?.Source != "firsthand-ball-transfer" || b.GetOrNull(first.MemoryId) != null)
                throw new InvalidOperationException("Learner memory was shared.");
            Click("Ball / switch learner");
            CheckBelief(world, requireCorrect: true);
        }
        AssertPassive(app, world);
        byte[] after = world.CaptureCheckpoint();
        Call(app, "SaveAll");
        if (!after.SequenceEqual(File.ReadAllBytes(WorldPath))) throw new InvalidOperationException("Unity save payload differs.");
        WorldSimulation.PublishCheckpoint(Archive(mode), after, candidate => File.Exists(candidate) &&
            File.ReadAllBytes(candidate).SequenceEqual(after));
        Debug.Log($"H5_SAVE mode={mode} tick={world.TickCount} initial_bytes={before.Length} bytes={after.Length} sha={Digest(after)}");
    }

    static void VerifyRestart(DreamMachineApp app, WorldSimulation world, string mode, TextWriter trace)
    {
        bool failure = mode == "restart-failure";
        byte[] saved = File.ReadAllBytes(Archive(failure ? "failure" : "run"));
        if (!world.CaptureCheckpoint().SequenceEqual(saved) || world.Find("son").Spec.BirthSeed != 303 ||
            world.Find("daughter").Spec.BirthSeed != 404 || world.Find("son").State.Library.GetOrNull(BaselineMemory) == null)
            throw new InvalidOperationException("Fresh Unity process did not restore identities, history and world bytes.");
        AssertPassive(app, world);
        var pose = world.Home.ReadBallPose();
        if (!pose.Equals(world.Home.BallTransfer(1).After) ||
            Vector3.Distance(Get<Transform[]>(app, "_livingItems")[HouseholdLife.BallItem].position, new Vector3(pose.X, pose.Y, pose.Z)) > .0001f)
            throw new InvalidOperationException("Rendered ball did not resume at its recorded native pose.");
        Debug.Log(FormattableString.Invariant($"H5_RESTORE mode={mode} tick={world.TickCount} id={HouseholdLife.BallId} xyz={pose.X:F5}/{pose.Y:F5}/{pose.Z:F5} sha={Digest(saved)}"));
        if (failure) return;
        var prior = world.Home.ReadBallBelief(2);
        Click("Inspect sighting");
        var again = world.Home.ReadBallBelief(2);
        if (again.Uncertain || again.Sequence <= prior.Sequence || Math.Abs(again.X - pose.X) > .002f)
            throw new InvalidOperationException("The restored observation did not guide a subsequent inspection.");
        if (!Transfer(app, world, "arthur-after-restart", false, trace, capture: true).Success)
            throw new InvalidOperationException("Arthur did not retrieve and transfer the same object after restarting.");
    }

    static void CheckBelief(WorldSimulation world, bool requireCorrect)
    {
        var prior = world.Home.ReadBallBelief(2);
        Click("Inspect sighting");
        var miss = world.Home.ReadBallBelief(2);
        Click("Search nearby");
        var found = world.Home.ReadBallBelief(2);
        Debug.Log(FormattableString.Invariant($"H5_OBSERVATION prior={prior.X:F3}/{prior.Z:F3} at={prior.LastSeen} missed={miss.Uncertain} found={found.X:F3}/{found.Z:F3} at={found.LastSeen} uncertain={found.Uncertain}"));
        if (requireCorrect && (!miss.Uncertain || miss.LastSeen != prior.LastSeen || found.Uncertain ||
            Math.Abs(found.X - world.Home.ReadBallPose().X) > .002f))
            throw new InvalidOperationException("The prior sighting did not produce a miss followed by grounded retrieval.");
    }

    static void AssertPassive(DreamMachineApp app, WorldSimulation world)
    {
        byte[] before = world.CaptureCheckpoint();
        Click("Earlier attempt");
        Click("Next frame");
        if (Get<Array>(app, "_practiceReplay") == null) throw new InvalidOperationException("Recorded trajectory was unavailable.");
        Click("Live view");
        if (!before.SequenceEqual(world.CaptureCheckpoint())) throw new InvalidOperationException("Observer changed household state.");
    }

    static string Digest(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    static BallTransferRecord Transfer(DreamMachineApp app, WorldSimulation world, string label, bool light, TextWriter trace,
        bool rendering = true, bool capture = false, bool feedback = false)
    {
        int start = world.TickCount;
        Click(feedback ? "Grip feedback" : light ? "Light grip" : "Transfer ball");
        var hand = Get<NativeHandLab>(app, "_practiceHand");
        if (!world.Home.BallTransferRunning || hand == null || !hand.FreeObject) throw new InvalidOperationException("Live ball binding missing: " + label);
        if (world.Home.BallIntent.Value.Actor != Get<int>(app, "_ballActor")) throw new InvalidOperationException("Mismatched physical actor.");
        var camera = Camera.main;
        if (!rendering) { Click("Physical development"); camera.enabled = false; }
        bool placement = false;
        long cpu = 0;
        GripPhase previous = hand.Phase;
        for (int i = 0; i < 850 && world.Home.BallTransferRunning; i++)
        {
            long before = Stopwatch.GetTimestamp();
            Step(app);
            cpu += Stopwatch.GetTimestamp() - before;
            var p = hand.PalmPosition;
            var ball = hand.ObjectPosition + Get<Vector3>(app, "_ballOrigin");
            var q = hand.ObjectRotation;
            trace.WriteLine(FormattableString.Invariant($"{label},{hand.Tick},{hand.Phase},{p.x:F6},{p.y:F6},{p.z:F6},{ball.x:F6},{ball.y:F6},{ball.z:F6},{q.x:F5},{q.y:F5},{q.z:F5},{q.w:F5},{hand.ObjectVelocity.magnitude:F6},{hand.ObjectAngularVelocity.magnitude:F6},{hand.ReadPad(0).NormalForce:F5},{hand.ReadPad(1).NormalForce:F5},{hand.SupportForce:F5},{hand.Clearance:F6},{hand.PeakPenetration:F6},{hand.Work:F6},{hand.DigitEffort:F6},{hand.ControlledPlacement},{hand.HeldTicks},{Quaternion.Angle(Quaternion.identity, hand.WristRotation):F4},{hand.EffectiveGripCap:F3},{hand.GripCorrections}"));
            if (capture && hand.Phase == GripPhase.Release && previous != GripPhase.Release && hand.ControlledPlacement)
                Capture(app, "phase-h5-" + label + "-release");
            if (hand.Phase != previous)
            {
                Debug.Log(FormattableString.Invariant($"H5_PHASE {label} {hand.Tick} {previous}->{hand.Phase} clearance={hand.Clearance:F3} support={hand.SupportForce:F3} ball={ball.ToString("F3")} pads={hand.ReadPad(0).NormalForce:F2}/{hand.ReadPad(1).NormalForce:F2}"));
                previous = hand.Phase;
            }
            if (hand.Tick != world.TickCount - start) throw new InvalidOperationException("Native and world tick diverged.");
            placement |= hand.ControlledPlacement;
        }
        if (world.Home.BallTransferRunning || !hand.HouseholdSettled) throw new InvalidOperationException("Did not settle: " + label);
        var record = world.Home.BallTransfer(world.Home.BallTransferCount - 1);
        if (!rendering) { camera.enabled = true; Click("Physical development"); }
        Debug.Log(FormattableString.Invariant($"H5_ATTEMPT {label} before={record.Intent.Before.X:F5}/{record.Intent.Before.Y:F5}/{record.Intent.Before.Z:F5} after={record.After.X:F5}/{record.After.Y:F5}/{record.After.Z:F5} goal={record.Intent.TargetX:F5}/{record.Intent.TargetTop:F5}/{record.Intent.TargetZ:F5} result={record.Outcome} controlled_place={placement} ticks={hand.Tick} tick_ms={cpu * 1000d / Stopwatch.Frequency / hand.Tick:F3}"));
        return record;
    }

    static void Capture(DreamMachineApp app, string name)
    {
        Call(app, "RefreshPractice");
        Call(app, "OrbitCamera");
        var camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
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
            for (int i = 0; i < 8; ++i) camera.Render();
            Debug.Log(FormattableString.Invariant($"H5_RENDER {name} camera_cpu_submit_ms={(Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency / 8:F3}"));
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
    }

    static void Step(DreamMachineApp app)
    {
        var clock = Get<TimeGovernor>(app, "_governor");
        clock.RequestSingleStep();
        if (!clock.TakeSingleStep()) throw new InvalidOperationException("Missing single step.");
        Call(app, "AdvanceHouseholdTick");
        Call(app, "StepActors", WorldSimulation.Dt);
    }
}
