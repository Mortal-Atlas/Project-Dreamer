using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Sim;
using DreamMachine.Social;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class HouseholdPracticeSmoke
{
    const string Key = "Dreamer.H2";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static HouseholdPracticeSmoke()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += State;
    }

    public static void Attempt() => Start(1);
    public static void Restore() => Start(2);

    static void Start(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        if (mode == 1)
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
            world.EnableHouseholdLife();
            world.Find("son").State.Library.NoteIdea("phase-h2-baseline", "Existing Arthur memory", .3f, 0, "smoke-baseline");
            string path = Path.Combine(Application.persistentDataPath, "world.checkpoint");
            if (File.Exists(path) && !File.Exists(path + ".before-h2-smoke")) File.Copy(path, path + ".before-h2-smoke");
            world.SaveCheckpoint(path);
        }
        SessionState.SetInt(Key, mode);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }

    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseInt(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        var app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null) app = new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
        Get<TimeGovernor>(app, "_governor").Paused = true;
        EditorApplication.update += Verify;
    }

    static T Get<T>(DreamMachineApp app, string field) => (T)typeof(DreamMachineApp).GetField(field, Private).GetValue(app);
    static void Invoke(DreamMachineApp app, string method, params object[] args) =>
        typeof(DreamMachineApp).GetMethod(method, Private).Invoke(app, args);
    static void Click(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name).onClick.Invoke();

    static void Verify()
    {
        var app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null || !Get<bool>(app, "_ready")) return;
        EditorApplication.update -= Verify;
        try
        {
            var world = Get<WorldSimulation>(app, "_sim");
            var arthur = world.Find("son");
            int seed = arthur.Spec.BirthSeed;
            byte[] before = world.CaptureCheckpoint();
            if (Get<Text>(app, "_practiceText") != null) Click("Physical development");
            Click("Physical development");
            if (!world.CaptureCheckpoint().SequenceEqual(before)) throw new InvalidOperationException("Opening the observer mutated the household.");
            var clock = Get<TimeGovernor>(app, "_governor");
            void Step()
            {
                clock.RequestSingleStep();
                if (!clock.TakeSingleStep()) throw new InvalidOperationException("Step request missing.");
                Invoke(app, "AdvanceHouseholdTick");
                Invoke(app, "StepActors", WorldSimulation.Dt);
            }
            void RunAttempt(bool captures)
            {
                Click("Try grasp");
                var hand = Get<NativeHandLab>(app, "_practiceHand");
                if (hand == null) throw new InvalidOperationException("Native body missing.");
                int start = world.TickCount;
                long elapsed = 0;
                if (!clock.Paused || hand.Tick != 0) throw new InvalidOperationException("Paused clock advanced.");
                for (int i = 0; i < 240 && world.PhysicalPractice.AttemptRunning; i++)
                {
                    long begin = System.Diagnostics.Stopwatch.GetTimestamp();
                    Step();
                    elapsed += System.Diagnostics.Stopwatch.GetTimestamp() - begin;
                    if (hand.Tick != world.TickCount - start) throw new InvalidOperationException("Clock divergence.");
                    if (captures && (hand.Phase == GripPhase.Hold && hand.HeldTicks == 6 ||
                        hand.Terminal && hand.Phase == GripPhase.Failed))
                        Capture(app, hand.Terminal ? "phase-h2-failed" : "phase-h2-hold");
                }
                if (world.PhysicalPractice.AttemptRunning || !hand.FreeObject || !hand.Settled) throw new InvalidOperationException("Expected a free object and completed attempt.");
                var record = world.PhysicalPractice.Records.Last();
                Debug.Log(FormattableString.Invariant($"H2_ATTEMPT id={record.Practice.Attempt} start={record.Start} end={record.End} cap={record.Practice.Before:F2}->{record.Practice.After:F2} slip={record.Practice.SlipTicks} held={record.Practice.HoldTicks} lift={record.Practice.PeakClearance:F4} outcome={record.Outcome} tick_ms={elapsed * 1000d / System.Diagnostics.Stopwatch.Frequency / hand.Tick:F3} tape={new FileInfo(Path.Combine(Application.persistentDataPath, "motor-trajectories", record.Tape + ".h2")).Length}"));
            }
            if (SessionState.GetInt(Key, 0) == 1)
            {
                Click("Enter station");
                for (int i = 0; i < 4 && !world.PhysicalPractice.Records.Any(r => r.Practice.Success); i++) RunAttempt(true);
                if (world.PhysicalPractice.Records.First().Practice.Success || !world.PhysicalPractice.Records.Any(r => r.Practice.Success))
                    throw new InvalidOperationException("No measured failure and success sequence.");
                Invoke(app, "SaveAll");
                var restored = WorldSimulation.LoadPrimaryCheckpoint(Path.Combine(Application.persistentDataPath, "world.checkpoint"));
                if (!world.CaptureCheckpoint().SequenceEqual(restored.CaptureCheckpoint())) throw new InvalidOperationException("Settled save mismatch.");
                Debug.Log($"H2_SAVED tick={world.TickCount} bytes={world.CaptureCheckpoint().Length} seed={seed}");
            }
            else
            {
                if (world.PhysicalPractice?.Active != true || world.PhysicalPractice.Records.Count < 2 ||
                    arthur.State.Library.GetOrNull("phase-h2-baseline") == null)
                    throw new InvalidOperationException("Fresh launch did not restore the same practice session.");
                byte[] checkpoint = world.CaptureCheckpoint();
                var count = world.PhysicalPractice.Records.Count;
                Click("Earlier attempt");
                Click("Next frame");
                if (Get<Array>(app, "_practiceReplay") == null || !checkpoint.SequenceEqual(world.CaptureCheckpoint()))
                    throw new InvalidOperationException("Playback changed authoritative state or lost the source.");
                Click("Live view");
                RunAttempt(false);
                if (!world.PhysicalPractice.Records.Last().Practice.Success) throw new InvalidOperationException("Retained grasp failed.");
                Click("Return to Living");
                Step();
                if (world.PhysicalPractice.Active || world.Find("son") != arthur || arthur.Spec.BirthSeed != seed ||
                    world.PhysicalPractice.Records.Count != count + 1 || arthur.State.Library.GetOrNull("phase-h2-baseline") == null)
                    throw new InvalidOperationException("Return to Living lost the individual.");
                Debug.Log($"H2_RESTORED tick={world.TickCount} seed={seed} records={world.PhysicalPractice.Records.Count}");
            }
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Capture(DreamMachineApp app, string name)
    {
        Invoke(app, "RefreshPractice");
        Invoke(app, "OrbitCamera");
        var camera = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            camera.targetTexture = rt;
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .44f; }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name + ".png"), tex.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }
}
