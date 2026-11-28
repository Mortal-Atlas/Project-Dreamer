using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class HandBrowserSmoke
{
    const string Key = "Dreamer.HandBrowser";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    static HandBrowserSmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Play;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.embodiment.hand-attempts");
        new GameObject("Supported hand laboratory").AddComponent<HandLabView>();
        EditorApplication.EnterPlaymode();
    }

    static void Play(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += Poll;
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseBool(Key); EditorApplication.Exit(0); }
    }

    static void Poll()
    {
        var view = Object.FindAnyObjectByType<HandLabView>();
        if (view == null || Read<Text>(view, "_details") == null) return;
        EditorApplication.update -= Poll;
        try
        {
            void Click(string text) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == text).onClick.Invoke();
            int holdTick = -1;
            void StepToEnd()
            {
                for (int i = 0; i < 120 && !Read<NativeHandLab>(view, "_lab").Terminal; i++)
                {
                    Click("Step");
                    typeof(HandLabView).GetMethod("Update", Fields).Invoke(view, null);
                    var hand = Read<NativeHandLab>(view, "_lab");
                    if (hand.Phase == GripPhase.Hold && hand.HeldTicks == 6) holdTick = hand.Tick;
                }
                typeof(HandLabView).GetMethod("Refresh", Fields).Invoke(view, null);
            }
            StepToEnd();
            var details = Read<Text>(view, "_details");
            if (!details.text.Contains("physical-lift-hold-release")) throw new InvalidOperationException("The standard record did not complete.");
            Click("Low grip");
            StepToEnd();
            if (!details.text.Contains("hold-gate-failed")) throw new InvalidOperationException("The failed record is missing.");
            int liveTick = Read<NativeHandLab>(view, "_lab").Tick;
            Click("Earlier attempt");
            var slider = Read<Slider>(view, "_scrubber");
            slider.value = 50;
            if (!details.text.Contains("Hold") || !details.text.Contains("tick 50"))
                throw new InvalidOperationException("Recorded hold is absent from playback.");
            var refresh = typeof(HandLabView).GetMethod("Refresh", Fields).CreateDelegate(typeof(Action), view) as Action;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 120; i++) refresh();
            watch.Stop();
            int bytes = Marshal.SizeOf(typeof(HandLabView).GetNestedType("Frame", BindingFlags.NonPublic));
            Debug.Log(FormattableString.Invariant($"H1_BROWSER refresh_ms={watch.Elapsed.TotalMilliseconds/120:F4} marshalled_frame_bytes={bytes}"));
            Capture(view, "phase-h3-fixed-playback");
            Click("Later attempt");
            if (Read<NativeHandLab>(view, "_lab").Tick != liveTick) throw new InvalidOperationException("Playback modified physics.");
            Click("Feedback grip");
            holdTick = -1;
            StepToEnd();
            var feedback = Read<NativeHandLab>(view, "_lab");
            int feedbackEnd = feedback.Tick;
            if (feedback.Phase != GripPhase.Complete || feedback.GripCorrections == 0 || holdTick < 0)
                throw new InvalidOperationException("Live contact-gated correction did not produce a measured hold.");
            slider.value = holdTick;
            if (!details.text.Contains("Contact-gated feedback") || !details.text.Contains("Hold") || feedback.Tick != feedbackEnd)
                throw new InvalidOperationException("Feedback replay lost evidence or modified physics.");
            Capture(view, "phase-h3-feedback-playback");
            Debug.Log($"H3_BROWSER fixed success, weak failure, feedback success, {feedback.GripCorrections} corrections, passive replay");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static T Read<T>(HandLabView view, string field) => (T)typeof(HandLabView).GetField(field, Fields).GetValue(view);

    static void Capture(HandLabView view, string name)
    {
        var camera = view.GetComponentInChildren<Camera>();
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .17f; }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 12; i++) camera.Render();
            watch.Stop();
            Debug.Log(FormattableString.Invariant($"H1_BROWSER camera_cpu_submit_ms={watch.Elapsed.TotalMilliseconds/12:F3}"));
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name + ".png"), texture.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(texture);
        }
    }
}
