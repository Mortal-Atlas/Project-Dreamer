using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class PredictiveTraySmoke
{
    const string Key = "Dreamer.PredictiveTraySmoke";
    const string Tray = "embodiment.elbow";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int frames, phase;
    static byte[] checkpoint;
    static long memory;

    static PredictiveTraySmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        string directory = Application.persistentDataPath;
        File.Copy(Path.Combine(directory, "phase-b-restart.checkpoint"),
            Path.Combine(directory, "elbow-predictive.checkpoint"), true);
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1." + Tray);
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var view = new GameObject("Predictive Lab").AddComponent<ElbowLabView>();
        view.UseNativePhysics = view.UsePredictiveMotor = true;
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
            if (++frames < 8) return;
            var view = Object.FindAnyObjectByType<ElbowLabView>();
            var trial = (PredictiveNativeTrial)typeof(ElbowLabView).GetField("_predictive", Private).GetValue(view);
            var workspace = (TrayWorkspace)typeof(ElbowLabView).GetField("_workspace", Private).GetValue(view);
            if (trial == null) throw new Exception("Predictive laboratory did not load.");
            if (phase == 0)
            {
                if (trial.Lab.Tick != 6 || trial.Motor.Updates != 6) throw new Exception("Unexpected initial phase B state.");
                GameObject.Find("Elbow laboratory").GetComponentsInChildren<Button>()
                    .Single(button => button.name == "Step").onClick.Invoke();
                phase++;
                return;
            }
            if (phase == 1)
            {
                if (trial.Lab.Tick < 7) return;
                if (trial.Lab.Tick != 7 || trial.Motor.Updates != 7 || !trial.HasFeedback)
                    throw new Exception("The tray did not step predictive learning exactly once.");
                checkpoint = trial.CaptureCheckpoint();
                workspace.RefreshVisible();
                Capture(view);
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < 200; i++) workspace.RefreshVisible();
                watch.Stop();
                double open = watch.Elapsed.TotalMilliseconds * 5;
                workspace.Toggle(Tray);
                watch.Restart();
                for (int i = 0; i < 200; i++) workspace.RefreshVisible();
                watch.Stop();
                Debug.Log($"PREDICTIVE_TRAY refresh_us open={open:F3} closed={watch.Elapsed.TotalMilliseconds * 5:F3}");
                memory = GC.GetTotalMemory(true);
                for (int i = 0; i < 12; i++)
                {
                    workspace.Toggle(Tray);
                    workspace.Toggle(Tray);
                }
                frames = 0;
                phase++;
                return;
            }
            if (!checkpoint.SequenceEqual(trial.CaptureCheckpoint()) || workspace.ReadLayout(Tray).Visible)
                throw new Exception("Passive tray use altered predictive state.");
            Debug.Log($"PREDICTIVE_TRAY 12 cycles and capture leave state identical; net managed bytes={GC.GetTotalMemory(true) - memory}");
            EditorApplication.update -= Verify;
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static void Capture(ElbowLabView view)
    {
        var camera = view.GetComponentInChildren<Camera>();
        var canvas = GameObject.Find("Elbow laboratory").GetComponent<Canvas>();
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 0.5f;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            string path = Path.Combine(Application.persistentDataPath, "phase-b-predictive-tray.png");
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Debug.Log("PREDICTIVE_TRAY capture " + path);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            Object.Destroy(target);
            Object.Destroy(pixels);
        }
    }
}
