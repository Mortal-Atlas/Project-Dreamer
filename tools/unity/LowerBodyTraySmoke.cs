using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class LowerBodyTraySmoke
{
    const string Key = "Dreamer.LowerBodyTraySmoke";
    const string Tray = "embodiment.lower-body";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static int _frames, _phase;
    static Vector3 _initial;

    static LowerBodyTraySmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1." + Tray);
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Isolated lower-body fixture").AddComponent<LowerBodyLabView>();
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += Verify;
        if (change != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(0);
    }

    static void Verify()
    {
        try
        {
            var view = Object.FindAnyObjectByType<LowerBodyLabView>();
            var lab = (NativeLowerBodyLab)typeof(LowerBodyLabView).GetField("_lab", Flags).GetValue(view);
            if (lab == null) return;
            if (_frames++ == 0) _initial = lab.PelvisPosition;
            if (_frames < 8) return;
            if (_phase == 0)
            {
                if (lab.Tick != 0 || lab.PelvisPosition != _initial) throw new Exception("Native physics advanced while paused.");
                Click("Request swing");
                if (lab.Action != SwingOutcome.GateBlocked) throw new Exception("The tray bypassed the swing interlock.");
                Click("Step");
                _phase++;
                return;
            }
            if (_phase == 1 && lab.Tick == 0) return;
            if (lab.Tick != 1) throw new Exception("The Step button should consume exactly one 0.05 s tick.");
            var workspace = (TrayWorkspace)typeof(LowerBodyLabView).GetField("_workspace", Flags).GetValue(view);
            var clock = (TimeGovernor)typeof(LowerBodyLabView).GetField("_clock", Flags).GetValue(view);
            if (!clock.Paused) throw new Exception("Single-step left the lab running.");
            var step = typeof(LowerBodyLabView).GetMethod("StepFixture", Flags).CreateDelegate(typeof(Action), view) as Action;
            for (int i = 0; i < 180; i++) step();
            Click("Load left");
            if (Mathf.Abs(lab.RequestedLoad + .875f) > .0001f) throw new Exception("Left load command did not reach the fixture.");
            for (int i = 0; i < 100; i++) step();
            if (!lab.SwingReady || !lab.ReadSole(0).PressureValid) throw new Exception("The standing tray fixture failed its transfer or contact check.");
            Vector3 position = lab.PelvisPosition, velocity = lab.PelvisVelocity;
            Vector3 leftImpulse = lab.LeftImpulse, rightImpulse = lab.RightImpulse;
            typeof(LowerBodyLabView).GetMethod("Draw", Flags).Invoke(view, null);
            workspace.RefreshVisible();
            Capture(view);
            Click("Export");
            var exported = Directory.EnumerateFiles(Application.persistentDataPath, "lower-body-101-*.csv")
                .OrderByDescending(File.GetLastWriteTimeUtc).First();
            var rows = File.ReadAllLines(exported);
            if (rows.Length != 282 || !rows[0].StartsWith("schema,provenance,") || !rows[1].StartsWith("2,isolated-physical-measurement,"))
                throw new Exception("The exported trajectory must contain the schema and every recorded tick.");
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) workspace.RefreshVisible();
            watch.Stop();
            double open = watch.Elapsed.TotalMilliseconds * 10d;
            workspace.Toggle(Tray);
            watch.Restart();
            for (int i = 0; i < 100; i++) workspace.RefreshVisible();
            watch.Stop();
            if (lab.Tick != 281 || lab.PelvisPosition != position || lab.PelvisVelocity != velocity ||
                lab.LeftImpulse != leftImpulse || lab.RightImpulse != rightImpulse)
                throw new Exception("View refresh altered the paused native state.");
            int measuredTick = lab.Tick;
            double closed = watch.Elapsed.TotalMilliseconds * 10d;
            workspace.Toggle(Tray);
            Click("Pause / run");
            if (clock.Paused) throw new Exception("Resume did not release the clock.");
            Click("Pause / run");
            if (!clock.Paused) throw new Exception("Pause did not stop the clock.");
            Click("Next body");
            var next = (NativeLowerBodyLab)typeof(LowerBodyLabView).GetField("_lab", Flags).GetValue(view);
            if (next.Tick != 0 || next.Seed != 202 || !clock.Paused) throw new Exception("Anatomy selection did not reset the isolated fixture.");
            Click("Reset lab");
            var reset = (NativeLowerBodyLab)typeof(LowerBodyLabView).GetField("_lab", Flags).GetValue(view);
            if (reset == next || reset.Tick != 0 || reset.Seed != next.Seed) throw new Exception("Reset did not rebuild the selected fixture.");
            Debug.Log(FormattableString.Invariant($"LOWER_BODY_TRAY paused=8frames stepped=1 observer_tick={measuredTick} export=281_rows resume_pause=True select_reset=True refresh_open_us={open:F3} refresh_closed_us={closed:F3}"));
            EditorApplication.update -= Verify;
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static void Click(string name) => GameObject.Find("Lower-body laboratory")
        .GetComponentsInChildren<Button>().Single(button => button.name == name).onClick.Invoke();

    static void Capture(LowerBodyLabView view)
    {
        var camera = view.GetComponentInChildren<Camera>();
        var canvas = GameObject.Find("Lower-body laboratory").GetComponent<Canvas>();
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var original = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = .5f;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "phase-c11-observatory.png"), image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = original;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            Object.Destroy(target);
            Object.Destroy(image);
        }
    }
}
