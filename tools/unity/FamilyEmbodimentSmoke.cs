using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class FamilyEmbodimentSmoke
{
    const string Key = "Dreamer.FamilyEmbodimentSmoke";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int frames;
    static bool Baseline => SessionState.GetBool(Key + ".Baseline", false);

    static FamilyEmbodimentSmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnPlay;
    }

    public static void Before() => Run(true);
    public static void After() => Run(false);

    static void Run(bool baseline)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        string path = Application.persistentDataPath;
        foreach (string file in new[] { "world.checkpoint", "world.checkpoint.previous" }) File.Delete(Path.Combine(path, file));
        string learners = Path.Combine(path, "agents");
        foreach (string child in new[] { "daughter.json", "son.json" }) File.Delete(Path.Combine(learners, child));
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.simulation");
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Baseline", baseline);
        EditorSceneManager.OpenScene("Assets/Scenes/DreamMachine.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlay(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            if (Object.FindAnyObjectByType<DreamMachineApp>() == null)
                new GameObject("DreamMachine").AddComponent<DreamMachineApp>();
            EditorApplication.update += Verify;
        }
        if (change != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(0);
    }

    static void Verify()
    {
        if (++frames < 10) return;
        EditorApplication.update -= Verify;
        try
        {
            var app = Object.FindAnyObjectByType<DreamMachineApp>();
            var world = Field<WorldSimulation>(app, "_sim");
            var governor = Field<TimeGovernor>(app, "_governor");
            if (!Field<bool>(app, "_ready") || world.TickCount == 0) throw new Exception("Family failed to start and advance.");
            governor.Paused = true;
            byte[] paused = world.CaptureCheckpoint();
            var bodies = Object.FindObjectsByType<SegmentedBody>(FindObjectsSortMode.None).OrderBy(b => b.PersonId).ToArray();
            if (bodies.Length != 4) throw new Exception("Expected four family bodies.");
            Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
            bool initialTray = GameObject.Find("Simulation controls") != null;
            var simulation = Button("Simulation");
            if (!initialTray) simulation.onClick.Invoke();
            if (GameObject.Find("Simulation controls") == null) throw new Exception("Simulation button did not show controls.");
            Capture(bodies, "controls");
            Button("Close").onClick.Invoke();
            simulation.onClick.Invoke();
            if (GameObject.Find("Simulation controls") == null) throw new Exception("Simulation button did not reopen controls.");
            if (!Baseline)
            {
                MethodInfo map = typeof(DreamMachineApp).Assembly.GetType("DreamMachine.UnityBridge.DreamInput")
                    .GetMethod("TryMap", BindingFlags.NonPublic | BindingFlags.Static);
                foreach (KeyCode key in new[] { KeyCode.N, KeyCode.F6, KeyCode.F7 })
                    if (!(bool)map.Invoke(null, new object[] { key, null }))
                        throw new Exception("Unmapped Input System key " + key);
                Button("Close").onClick.Invoke();
                Button("Reset layout").onClick.Invoke();
                if (GameObject.Find("Simulation controls") == null || !governor.Paused)
                    throw new Exception("Layout reset did not recover controls while preserving pause.");
                Button("Close").onClick.Invoke();
            }
            else Button("Close").onClick.Invoke();
            if (!paused.SequenceEqual(world.CaptureCheckpoint()))
                throw new Exception("Toggling and recovering the layout modified the paused world.");
            Debug.Log($"FAMILY_UI default_open={initialTray} simulation_accessible={simulation.gameObject.activeInHierarchy}");

            MethodInfo update = typeof(DreamMachineApp).GetMethod("StepActors", Private);
            void Step()
            {
                world.Tick();
                update.Invoke(app, new object[] { WorldSimulation.Dt });
            }
            while (world.TickCount < 120) Step();
            LogBodies("meal", bodies);
            if (!Baseline)
            {
                foreach (var b in bodies)
                {
                    if (RigFlag(b, "Walking") || Math.Abs(b.Measure()[JointNames.LeftKnee]) > 0.1222f ||
                        Math.Abs(b.RigPivot("leftSole").position.y) > 0.002f)
                        throw new Exception("Neutral planted posture failed for " + b.PersonId);
                    if (b.RigPivot("leftThumb") == null || b.RigPivot("rightLittle") == null)
                        throw new Exception("Semantic digit anchors missing on " + b.PersonId);
                }
            }
            Capture(bodies, "standing");
            while (world.TickCount < 240) Step();
            var before = bodies.Select(b => b.transform.position).ToArray();
            var firstKnees = bodies.Select(b => b.Measure()[JointNames.LeftKnee]).ToArray();
            float supportError = 0;
            for (int i = 0; i < 30; i++)
            {
                Step();
                if (Baseline) continue;
                foreach (var b in bodies)
                {
                    if (RigFlag(b, "LeftSupport"))
                        supportError = Mathf.Max(supportError, Mathf.Abs(b.RigPivot("leftSole").position.y));
                    if (RigFlag(b, "RightSupport"))
                        supportError = Mathf.Max(supportError, Mathf.Abs(b.RigPivot("rightSole").position.y));
                }
            }
            LogBodies("play", bodies);
            for (int i = 0; i < bodies.Length; i++)
            {
                var b = bodies[i];
                Debug.Log(FormattableString.Invariant($"FAMILY_MOTION {b.PersonId} metres={Vector3.Distance(before[i], b.transform.position):F6} knee_change_rad={b.Measure()[JointNames.LeftKnee] - firstKnees[i]:F6}"));
            }
            Capture(bodies, "walking");
            if (!Baseline)
            {
                Debug.Log(FormattableString.Invariant($"FAMILY_SUPPORT max_height_error_m={supportError:F8}"));
                if (supportError > 0.002f) throw new Exception("A stance foot left the ground plane.");
                if (!bodies.Select((body, index) => body.IsLeader &&
                    Vector3.Distance(before[index], body.transform.position) >= 0.05f).Any(moved => moved))
                    throw new Exception("Neither teacher completed a walking displacement.");
            }
            if (!Baseline && bodies.Where(b => !b.IsLeader).Any(b => Math.Abs(b.Measure()[JointNames.LeftKnee]) > 1.3f))
                throw new Exception("Learner knee exceeded the reference envelope.");
            Debug.Log("FAMILY_SMOKE completed " + (Baseline ? "baseline" : "corrected"));
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static T Field<T>(DreamMachineApp app, string name) =>
        (T)typeof(DreamMachineApp).GetField(name, Private).GetValue(app);

    static bool RigFlag(SegmentedBody body, string name) =>
        (bool)typeof(SegmentedBody).GetProperty(name).GetValue(body);

    static void LogBodies(string phase, SegmentedBody[] bodies)
    {
        foreach (var body in bodies)
        {
            Transform head = body.RigPivot("head");
            float crown = head.position.y + body.Dimensions.Head * .5f;
            float ankle = body.RigPivot("leftAnkle").position.y;
            float[] angles = body.Measure();
            Debug.Log(FormattableString.Invariant($"FAMILY_RIG {phase} {body.PersonId} height={crown:F5} anatomy={body.Dimensions.Height:F5} ankle={ankle:F5} knee_rad={angles[JointNames.LeftKnee]:F5}"));
        }
    }

    static void Capture(SegmentedBody[] bodies, string name)
    {
        var camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var old = RenderTexture.active;
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        var ortho = camera.orthographic;
        var size = camera.orthographicSize;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
        try
        {
            if (name != "controls")
            {
                var focus = bodies.First(b => b.PersonId == "father");
                camera.transform.position = focus.transform.position + new Vector3(2.5f, 1.05f, 3.5f);
                camera.transform.LookAt(focus.transform.position + Vector3.up * 0.88f);
                camera.orthographic = true;
                camera.orthographicSize = 1.05f;
                if (!Baseline)
                    foreach (var body in bodies)
                        body.transform.Find("Label").rotation = camera.transform.rotation;
            }
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                if (name != "controls") { canvas.enabled = false; continue; }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.7f - 0.005f * canvas.sortingOrder;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath,
                $"phase-c0-{(Baseline ? "before" : "after")}-{name}.png"), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = old;
            camera.targetTexture = null;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographic = ortho;
            camera.orthographicSize = size;
            foreach (var canvas in canvases)
            {
                canvas.enabled = true;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            Object.Destroy(target);
            Object.Destroy(texture);
        }
    }
}
