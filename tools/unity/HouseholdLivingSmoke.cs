using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
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
internal static class HouseholdLivingSmoke
{
    const string Key = "Dreamer.HouseholdLivingSmoke";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool BeforeMode => SessionState.GetInt(Key, 0) == 1;
    static int _frames;

    static HouseholdLivingSmoke()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += OnPlay;
    }

    public static void Before() => Start(1);
    public static void Run() => Start(2);

    static void Start(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        var reference = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
        File.Delete(Path.Combine(Application.persistentDataPath, "world.checkpoint.pre-household"));
        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "world.checkpoint"), reference.CaptureCheckpoint());
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.simulation");
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.household.experience");
        SessionState.SetInt(Key, mode);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlay(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            var app = Object.FindAnyObjectByType<DreamMachineApp>() ?? new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
            typeof(DreamMachineApp).GetField("LegacyHouseholdPresentation", Flags).SetValue(app, BeforeMode);
            Read<TimeGovernor>(app, "_governor").Paused = true;
            EditorApplication.update += Verify;
        }
        if (change == PlayModeStateChange.EnteredEditMode) { SessionState.EraseInt(Key); EditorApplication.Exit(0); }
    }

    static T Read<T>(DreamMachineApp app, string name) => (T)typeof(DreamMachineApp).GetField(name, Flags).GetValue(app);
    static void Invoke(DreamMachineApp app, string method, params object[] args) => typeof(DreamMachineApp).GetMethod(method, Flags).Invoke(app, args);
    static void Click(string name, GameObject root = null)
    {
        var buttons = root == null ? Object.FindObjectsByType<Button>(FindObjectsSortMode.None) : root.GetComponentsInChildren<Button>();
        buttons.Single(b => b.name == name).onClick.Invoke();
    }

    static void Verify()
    {
        if (++_frames < 10) return;
        EditorApplication.update -= Verify;
        try
        {
            var app = Object.FindAnyObjectByType<DreamMachineApp>();
            var world = Read<WorldSimulation>(app, "_sim");
            bool expectLive = !BeforeMode;
            if ((world.Home != null) != expectLive) throw new Exception("Unexpected household baseline mode.");
            if (world.TickCount != 0) throw new Exception("Household advanced while the clock was paused.");
            void Tick()
            {
                world.Tick();
                Invoke(app, "StepActors", WorldSimulation.Dt);
            }
            while (world.TickCount < 420) Tick();
            if (BeforeMode)
            {
                Invoke(app, "RefreshSimulationControls");
                Capture("household-before");
                Debug.Log("HOUSEHOLD_BEFORE captured at tick 420");
                EditorApplication.ExitPlaymode();
                return;
            }
            byte[] initial = world.CaptureCheckpoint();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 160; i++) Invoke(app, "DrawLiving", WorldSimulation.Dt);
            double viewMs = watch.Elapsed.TotalMilliseconds / 160;
            Click("Life & experience");
            Click("Living / research");
            Click("Living / research");
            Click("Reset layout");
            if (!initial.SequenceEqual(world.CaptureCheckpoint())) throw new Exception("Presentation controls changed authoritative household state.");
            Click("Resume", GameObject.Find("Simulation controls"));
            if (Read<TimeGovernor>(app, "_governor").Paused) throw new Exception("Resume did not release clock.");
            Click("Pause", GameObject.Find("Simulation controls"));
            Click("Step", GameObject.Find("Simulation controls"));
            if (!Read<TimeGovernor>(app, "_governor").TakeSingleStep()) throw new Exception("Pinned single-step did not queue a tick.");
            Tick();
            if (world.TickCount != 421) throw new Exception("Single-step was not one authoritative boundary.");
            Click("Close", GameObject.Find("Simulation controls"));
            Invoke(app, "RefreshLife");
            Capture("household-l2-experience");
            Click("Close", GameObject.Find("Life and experience"));
            Capture("household-l2-living");
            var bodies = Object.FindObjectsByType<SegmentedBody>(FindObjectsSortMode.None);
            int moving = 0, planted = 0, gaze = 0;
            for (int i = 0; i < 100; i++)
            {
                Tick();
                foreach (var b in bodies)
                {
                    int index = world.Home.FindPerson(b.PersonId);
                    var state = world.Home.ReadPerson(index);
                    if (Vector3.Distance(b.transform.position, new Vector3(state.X, 0, state.Z)) > .00001f)
                        throw new Exception("The rendered root diverged from authoritative movement.");
                    if (b.Walking && b.LastDisplacementMetres > .0001f) moving++;
                    if (!b.Walking && b.LeftSupport && b.RightSupport) planted++;
                    if ((state.Item >= 0 || state.Person >= 0) && Quaternion.Angle(b.RigPivot("head").localRotation, Quaternion.identity) > 4f) gaze++;
                }
            }
            if (moving == 0 || planted == 0 || gaze == 0) throw new Exception("No rendered movement, still support or directed gaze was exercised.");
            var control = WorldSimulation.RestoreCheckpoint(world.CaptureCheckpoint());
            int ticks = 4700 - world.TickCount;
            bool capturedActivity = false;
            bool lifeOpen = false;
            for (int i = 0; i < ticks; i++)
            {
                world.Tick();
                if (i % 530 == 0)
                {
                    Click("Life & experience");
                    lifeOpen = !lifeOpen;
                }
                int sortingLearner = -1;
                if (!capturedActivity)
                    for (int actor = 2; actor < 4; actor++)
                    {
                        var person = world.Home.ReadPerson(actor);
                        int scan = world.Home.ActiveSocketDecision(actor);
                        if (person.Activity == HomeActivity.Sort && person.Phase == HomePhase.Act && scan >= 0 &&
                            new SocketDecision(world.Home.Experience(scan).Details).Observations > 0)
                            sortingLearner = actor;
                    }
                if (sortingLearner >= 0)
                {
                    if (!lifeOpen) Click("Life & experience");
                    var tray = GameObject.Find("Life and experience");
                    while (Read<int>(app, "_lifeFocus") != sortingLearner) Click("Next person", tray);
                    for (int frame = 0; frame < 8; frame++) Invoke(app, "DrawLiving", WorldSimulation.Dt);
                    Invoke(app, "RefreshLife");
                    Click("History day", tray);
                    Click("History day", tray);
                    Capture("household-l2-sorting");
                    if (!lifeOpen) Click("Close", tray);
                    capturedActivity = true;
                }
                if (i % 43 == 0)
                {
                    Invoke(app, "DrawLiving", WorldSimulation.Dt);
                    Invoke(app, "RefreshLife");
                }
            }
            control.RunTicks(ticks);
            if (!capturedActivity) throw new Exception("No learner's sorting action was captured.");
            byte[] exact = world.CaptureCheckpoint();
            if (!exact.SequenceEqual(control.CaptureCheckpoint())) throw new Exception("Observed household diverged from accelerated control.");
            var events = Enumerable.Range(0, world.Home.ExperienceCount).Select(world.Home.Experience).ToArray();
            foreach (var e in events.GroupBy(e => e.Evidence)) Debug.Log($"HOUSEHOLD_EVIDENCE {e.Key}={e.Count()}");
            foreach (var e in events.Where(e => e.Changed).GroupBy(e => e.Activity)) Debug.Log($"HOUSEHOLD_CHANGE {e.Key}={e.Count()}");
            for (int actor = 2; actor < 4; actor++)
            {
                var sort = world.Home.ReadCompetence(actor, HouseholdLife.SortingBoard);
                Debug.Log($"HOUSEHOLD_COMPETENCE {world.Home.PersonId(actor)} solo={sort.IndependentSuccesses}/{sort.IndependentAttempts} source={sort.LastIndependentSuccess} witnessed={sort.Witnesses} imitated={sort.Imitations}");
            }
            using (var trace = File.CreateText(Path.Combine(Application.persistentDataPath, "household-l2-experiences.csv")))
            {
                trace.WriteLine("id,tick,actor,target,activity,provenance,source,revision,changed,details");
                for (int i = 0; i < events.Length; i++)
                {
                    var e = events[i];
                    trace.WriteLine($"{i},{e.Tick},{world.Home.PersonId(e.Actor)},{HouseholdLife.ItemName(e.Target)},{e.Activity},{e.Evidence},{e.Source},{e.Revision},{e.Changed},{e.Details}");
                }
            }
            if (world.DayIndex < 4 || !events.Any(e => e.Evidence == HomeEvidence.Replay) || !events.Any(e => e.Evidence == HomeEvidence.Imitation))
                throw new Exception("Multi-day observation, imitation or replay was not demonstrated.");
            if (!events.Any(e => e.Evidence == HomeEvidence.SocketDecision && new SocketDecision(e.Details).Observations > 0) ||
                !events.Any(e => e.Activity == HomeActivity.Sort && e.Changed && e.Details != 0))
                throw new Exception("Learners did not observe the board and apply its results.");
            Invoke(app, "SaveAll");
            var archive = WorldSimulation.LoadCheckpoint(Path.Combine(Application.persistentDataPath, "world.checkpoint.pre-household"));
            if (archive == null || archive.Home != null || archive.TickCount != 0)
                throw new Exception("Expected intact version-1 checkpoint archive.");
            world.RunTicks(15);
            Invoke(app, "RestoreWorld");
            var restored = Read<WorldSimulation>(app, "_sim");
            if (!exact.SequenceEqual(restored.CaptureCheckpoint()) || !Read<TimeGovernor>(app, "_governor").Paused)
                throw new Exception("Unity restore did not bring back the exact active household.");
            Debug.Log(FormattableString.Invariant($"HOUSEHOLD_SMOKE days={restored.DayIndex} ticks={restored.TickCount} episodes={events.Length} bytes={exact.Length} moving={moving} planted={planted} gaze={gaze} direct_view_ms={viewMs:F4}"));
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Capture(string title)
    {
        var camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var original = RenderTexture.active;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .4f; }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 12; i++) camera.Render();
            Debug.Log(FormattableString.Invariant($"HOUSEHOLD_RENDER {title} cpu_submit_ms={watch.Elapsed.TotalMilliseconds / 12:F3}"));
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, title + ".png"), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = original;
            camera.targetTexture = null;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            Object.Destroy(target);
            Object.Destroy(texture);
        }
    }
}
