using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Body;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class ElbowSmoke
{
    const string Key = "ElbowSmoke.Active";
    static int requested;
    static ElbowLabView view;
    static ElbowLab control;
    static readonly BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;

    static ElbowSmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke")
            throw new Exception("Scratch project required.");
        string path = Path.Combine(Application.persistentDataPath, "elbow-lab.checkpoint");
        File.Delete(path);
        File.Delete(path + ".previous");
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.embodiment.elbow");
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Elbow Laboratory").AddComponent<ElbowLabView>();
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(0);
        }
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        view = Object.FindAnyObjectByType<ElbowLabView>();
        control = new ElbowLab(ElbowLab.Settings.Standard, "son");
        EditorApplication.update += Verify;
    }

    static void Click(string name)
    {
        var tray = GameObject.Find("Elbow laboratory");
        var button = tray.GetComponentsInChildren<Button>().Single(b => b.name == name);
        button.onClick.Invoke();
    }

    static ElbowLab Current() => (ElbowLab)typeof(ElbowLabView).GetField("_lab", Fields).GetValue(view);

    static void Verify()
    {
        try
        {
            ElbowLab observed = Current();
            if (observed == null || observed.Tick < requested) return;
            if (observed.Tick != requested || !control.CaptureCheckpoint().SequenceEqual(observed.CaptureCheckpoint()))
                throw new Exception("Observed and control state differ at tick " + requested);
            if (requested == 4)
            {
                if (!(observed.Latest.NormalImpulse > 0))
                    throw new Exception("Expected actual simulated contact.");
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath, "elbow-contact.png"));
            }
            if (requested == 15)
            {
                Click("Save");
                observed.Step();
                Click("Restore");
                if (!control.CaptureCheckpoint().SequenceEqual(Current().CaptureCheckpoint()))
                    throw new Exception("Restore did not recover the saved boundary.");
                var workspace = (TrayWorkspace)typeof(ElbowLabView).GetField("_workspace", Fields).GetValue(view);
                Click("Close");
                if (workspace.ReadLayout("embodiment.elbow").Visible || GameObject.Find("Elbow laboratory") != null)
                    throw new Exception("Tray did not close.");
                byte[] before = Current().CaptureCheckpoint();
                workspace.RefreshVisible();
                if (!before.SequenceEqual(Current().CaptureCheckpoint()))
                    throw new Exception("Passive refresh changed state.");
                Debug.Log("ELBOW_SMOKE 15 exact ticks, contact, saved state restored, tray released");
                EditorApplication.update -= Verify;
                EditorApplication.ExitPlaymode();
                return;
            }
            control.Step();
            requested++;
            Click("Step");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
