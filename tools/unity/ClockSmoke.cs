using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DreamMachine.Development;
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
internal static class ClockSmoke
{
    const string Key = "ClockSmoke.Mode";
    static DreamMachineApp app;
    static TimeGovernor governor;
    static WorldSimulation world;
        static int steps = 690;
    const string Pref = "DreamMachine.Workspace.v1.simulation";
    static long retainedBefore;
    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static ClockSmoke()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += Changed;
    }
    public static void Control() => Run(1);
    public static void Observed() => Run(2);
    static void Run(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        PlayerPrefs.DeleteKey(Pref);
        var path = Path.Combine(Application.persistentDataPath, "world.checkpoint");
        File.Delete(path);
        File.Delete(path + ".previous");
        WorldSimulation seed = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-dreamerSeed101") ? 101 : 18);
        seed.RunTicks(690);
        seed.SaveCheckpoint(path);
        SessionState.SetInt(Key, mode);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetInt(Key, 0);
            EditorApplication.Exit(0);
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        app = Object.FindAnyObjectByType<DreamMachineApp>();
        if (app == null) app = new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
        governor = (TimeGovernor)Field("_governor");
        governor.Paused = true;
        EditorApplication.update += Check;
    }
    static object Field(string name) => typeof(DreamMachineApp).GetField(name, Flags).GetValue(app);
    static void Invoke(string name) => typeof(DreamMachineApp).GetMethod(name, Flags).Invoke(app, null);
    static void Click(string name)
    {
        var tray = GameObject.Find("Simulation controls");
        foreach (var button in tray.GetComponentsInChildren<Button>())
        {
            if (button.name != name) continue;
            button.onClick.Invoke();
            return;
        }
        throw new Exception("Missing button " + name);
    }
    static void Check()
    {
        try
        {
            world = (WorldSimulation)Field("_sim");
            if (world == null) return;
            if (world.TickCount < steps) return;
            if (world.TickCount != steps) throw new Exception("Unexpected tick " + world.TickCount);
            bool observed = SessionState.GetInt(Key, 0) == 2;
            if (steps == 690)
            {
                // The standard household now defaults to a visible tray. Establish both
                // experimental conditions explicitly before collecting paired observations.
                if (GameObject.Find("Simulation controls") != null) Click("Close");
                if (observed)
                {
                    long before = retainedBefore = GC.GetTotalMemory(true);
                    var watch = Stopwatch.StartNew();
                    Invoke("ToggleSimulationTray");
                    watch.Stop();
                    Debug.Log($"CLOCK_TRAY open_ms={watch.Elapsed.TotalMilliseconds:F3} managed_delta={GC.GetTotalMemory(false) - before}");
                    Invoke("CaptureVision");
                }
            }
            if (observed && steps > 690 && steps % 40 == 0 && steps < 1140)
            {
                Click("Close");
                Invoke("ToggleSimulationTray");
                Invoke("TogglePerformance");
                if (steps == 720) ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath, "workspace-simulation.png"));
                typeof(DreamMachineApp).GetMethod("SelectBaby", Flags).Invoke(app, new object[] { (steps / 40) % 2 });
                Invoke("CaptureVision");
            }
            if (steps == 1140)
            {
                if (observed) Click("Close");
                if (GameObject.Find("Simulation controls") != null) throw new Exception("Tray still active");
                if (observed)
                {
                    long retained = GC.GetTotalMemory(true) - retainedBefore;
                    var workspace = (TrayWorkspace)Field("_workspace");
                    var layout = workspace.ReadLayout("simulation");
                    layout.AnchorMin = new Vector2(0.04f, 0.72f);
                    layout.Visible = true;
                    workspace.SetLayout("simulation", layout);
                    workspace.Dispose();
                    var second = new TrayWorkspace("DreamMachine.Workspace.v1.");
                    int opened = 0, closed = 0;
                    second.Register("simulation", "Test workspace", Vector2.zero,
                        new TrayWorkspace.Layout { AnchorMin = Vector2.zero, AnchorMax = Vector2.one },
                        _ => opened++, () => closed++);
                    second.RestoreVisibility();
                    if (opened != 1 || second.ReadLayout("simulation").AnchorMin != layout.AnchorMin)
                        throw new Exception("Workspace preferences were not restored.");
                    second.Dispose();
                    if (closed != 1) throw new Exception("Tray close callback was not invoked.");
                    Debug.Log($"WORKSPACE_RESTORED opened={opened} closed={closed} managed_delta_after_cycles={retained}");
                    PlayerPrefs.DeleteKey(Pref);
                }
                var saved = world.Find("son").State;
                var json = saved.ToJson();
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++) AgentPersistentState.FromJson(json, false);
                watch.Stop();
                var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var exact = world.CaptureCheckpoint();
                Invoke("SaveAll");
                world.Tick();
                Invoke("RestoreWorld");
                world = (WorldSimulation)Field("_sim");
                if (!System.Linq.Enumerable.SequenceEqual(exact, world.CaptureCheckpoint()))
                    throw new Exception("Unity restore did not restore all checkpoint fields.");
                Debug.Log($"CLOCK_SMOKE tick={world.TickCount} hash={world.StateHash()} learner={BitConverter.ToString(sha.ComputeHash(world.CaptureCheckpoint()))} bytes={bytes.Length} parse_ms={watch.Elapsed.TotalMilliseconds / 100:F3} managed={GC.GetTotalMemory(false)}");
                EditorApplication.update -= Check;
                EditorApplication.ExitPlaymode();
            }
            else
            {
                steps++;
                if (observed) Click("Step");
                else governor.RequestSingleStep();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }
}
