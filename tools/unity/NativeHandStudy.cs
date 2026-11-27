using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DreamMachine.Body;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class NativeHandStudy
{
    const string Key = "Dreamer.HandH1";

    static NativeHandStudy()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Development() => Start(1);
    public static void HeldOut() => Start(2);

    static void Start(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new InvalidOperationException("Scratch project required.");
        SessionState.SetInt(Key, mode);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.EraseInt(Key); EditorApplication.Exit(0); }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try { Run(SessionState.GetInt(Key, 0)); EditorApplication.ExitPlaymode(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    static void Run(int mode)
    {
        bool heldOut = mode == 2;
        string cohort = heldOut ? "heldout" : "development";
        string folder = Application.persistentDataPath;
        using var summary = File.CreateText(Path.Combine(folder, $"phase-h1-{cohort}-summary.csv"));
        using var trace = File.CreateText(Path.Combine(folder, $"phase-h1-{cohort}-trace.csv"));
        summary.WriteLine("attempt,seed,mass_kg,radius_m,friction,closing_cap_n,close_ticks,yaw_deg,result,duration_ticks,hold_ticks,lift_mm,support_mean_n,weight_n,slip_ticks,peak_pad_n,penetration_mm,peak_guide_mm,work_j,mean_tick_us,command_us,sensing_us,record_us");
        trace.WriteLine("attempt,source,skill,tick,phase,held_ticks,palm_m,wrist_quat,puck_m,puck_quat,puck_velocity_m_s,target_m,left_travel_m,right_travel_m,left_closing_n,right_closing_n,drive_n,clearance_mm,support_force_n,left_force_n,right_force_n,left_contact_s,right_contact_s,left_tangent_n,right_tangent_n,left_slip,right_slip,left_contact_m,right_contact_m,work_j,overlap_mm");
        if (!heldOut)
        {
            Trial("nominal", 37, .080f, .028f, .75f, 3.2f, 12, 0, summary, trace, true);
            Trial("low-force", 37, .080f, .028f, .75f, .18f, 12, 0, summary, trace);
            Trial("high-force", 37, .080f, .028f, .75f, 6.5f, 12, 0, summary, trace);
            Trial("smoother-close", 443, .063f, .029f, .68f, 2.6f, 18, 27, summary, trace);
            Trial("lower-friction", 587, .105f, .027f, .26f, 3.2f, 12, 48, summary, trace);
        }
        else
        {
            Trial("light-small", 991, .051f, .024f, .72f, 3.2f, 12, 31, summary, trace);
            Trial("heavy-large", 2003, .102f, .033f, .76f, 3.2f, 12, 57, summary, trace);
            Trial("middle", 3253, .079f, .028f, .64f, 3.2f, 12, 118, summary, trace);
            Trial("heavier-low-friction", 6163, .135f, .030f, .28f, 3.2f, 12, 152, summary, trace);
        }
        Debug.Log("H1_COMPLETE " + cohort);
    }

    static void Trial(string id, int seed, float mass, float radius, float friction, float effort, int close, float yaw,
        TextWriter summary, TextWriter trace, bool visuals = false)
    {
        using var lab = new NativeHandLab(seed, mass, radius, friction, effort, close, yaw) { Profile = true };
        using var control = visuals ? new NativeHandLab(seed, mass, radius, friction, effort, close, yaw) : null;
        if (!lab.FreeObject) throw new InvalidOperationException("Object is not freely simulated.");
        using var camera = visuals ? new HandAuditCamera() : null;
        float rest = 0f, difference = 0f;
        long native = 0, archive = 0;
        while (lab.Tick < 240 && !lab.Terminal)
        {
            var previous = lab.Phase;
            long start = Stopwatch.GetTimestamp();
            lab.Step();
            native += Stopwatch.GetTimestamp() - start;
            start = Stopwatch.GetTimestamp();
            var f = lab.ReadFrame();
            archive += Stopwatch.GetTimestamp() - start;
            if (f.Tick >= 12 && f.Tick <= 18) rest += f.SupportForce / 7f;
            if (visuals && (f.Phase == GripPhase.Hold && f.HeldTicks == 7 ||
                previous != f.Phase && f.Phase is GripPhase.Close or GripPhase.Release or GripPhase.Complete))
                camera.Capture(lab, $"phase-h1-{f.Phase.ToString().ToLowerInvariant()}");
            if (control != null)
            {
                control.Step();
                difference = Mathf.Max(difference, Vector3.Distance(control.ObjectPosition, lab.ObjectPosition),
                    Vector3.Distance(control.PalmPosition, lab.PalmPosition));
                if (control.Phase != lab.Phase) throw new InvalidOperationException("Passive rendering altered actuator trajectory.");
            }
            trace.WriteLine(FormattableString.Invariant($"{id},engineered-reference,{NativeHandLab.FixtureId},{f.Tick},{f.Phase},{f.HeldTicks},{Vec(f.Palm)},{Rot(f.WristRotation)},{Vec(f.Object)},{Rot(f.ObjectRotation)},{Vec(f.ObjectVelocity)},{f.TravelTarget:F5},{f.LeftTravel:F5},{f.RightTravel:F5},{f.LeftClosingForce:F5},{f.RightClosingForce:F5},{Vec(f.DriveForce)},{f.Clearance*1000f:F3},{f.SupportForce:F5},{f.Left.NormalForce:F5},{f.Right.NormalForce:F5},{f.Left.ContactSeconds:F4},{f.Right.ContactSeconds:F4},{f.Left.TangentForce:F5},{f.Right.TangentForce:F5},{f.Left.Slip},{f.Right.Slip},{Vec(f.Left.Point)},{Vec(f.Right.Point)},{f.Work:F5},{f.Penetration*1000f:F4}"));
        }
        if (!lab.Terminal || !lab.FreeObject) throw new InvalidOperationException("Fixture boundary or object authority failed.");
        if (visuals && difference > .00001f) throw new InvalidOperationException("Observer and control diverged.");
        float weight = mass * -Physics.gravity.y;
        if (Mathf.Abs(rest - weight) > .035f + weight * .09f) throw new InvalidOperationException("Known weight calibration failed.");
        double Cost(long ticks) => ticks * 1e6 / Stopwatch.Frequency / lab.Tick;
        string row = FormattableString.Invariant($"{id},{seed},{mass:F3},{radius:F3},{friction:F2},{effort:F3},{close},{yaw:F0},{lab.Outcome},{lab.Tick},{lab.HeldTicks},{lab.PeakClearance*1000f:F3},{rest:F5},{weight:F5},{lab.SlipTicks},{lab.PeakPadForce:F3},{lab.PeakPenetration*1000f:F3},{lab.MaxJointTravel*1000f:F2},{lab.Work:F5},{Cost(native):F2},{Cost(lab.CommandCpuTicks):F2},{Cost(lab.SensorCpuTicks):F2},{Cost(archive):F2}");
        summary.WriteLine(row);
        summary.Flush();
        Debug.Log("H1_TRIAL " + row);
        if (visuals) Debug.Log(FormattableString.Invariant($"H1_OBSERVER ticks={lab.Tick} max_delta_m={difference:R}"));
    }

    static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x:F6};{v.y:F6};{v.z:F6}");
    static string Rot(Quaternion q) => FormattableString.Invariant($"{q.x:F6};{q.y:F6};{q.z:F6};{q.w:F6}");

    sealed class HandAuditCamera : IDisposable
    {
        readonly Camera _camera;
        readonly RenderTexture _surface = new RenderTexture(960, 720, 24);
        readonly Texture2D _image = new Texture2D(960, 720, TextureFormat.RGB24, false);
        readonly Transform[] _parts = new Transform[7];
        readonly Material[] _materials = new Material[3];

        public HandAuditCamera()
        {
            _camera = new GameObject("Hand evidence camera", typeof(Camera)).GetComponent<Camera>();
            _camera.transform.position = new Vector3(.24f, .24f, .28f);
            _camera.transform.LookAt(new Vector3(0, .028f, -.022f));
            _camera.nearClipPlane = .025f;
            _camera.farClipPlane = 10f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(.115f, .16f, .20f);
            _camera.targetTexture = _surface;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var colors = new[] { new Color(.25f,.32f,.40f), new Color(.42f,.76f,.74f), new Color(.99f,.55f,.21f) };
            for (int i = 0; i < colors.Length; i++) _materials[i] = new Material(shader) { color = colors[i] };
            for (int i = 0; i < _parts.Length; i++)
            {
                var part = GameObject.CreatePrimitive(i == 6 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                part.GetComponent<Collider>().enabled = false;
                part.GetComponent<Renderer>().sharedMaterial = _materials[i == 6 ? 2 : i == 0 ? 0 : 1];
                _parts[i] = part.transform;
            }
        }

        public void Capture(NativeHandLab lab, string file)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                var part = lab.ReadPart(i);
                _parts[i].SetPositionAndRotation(part.Position, part.Rotation);
                _parts[i].localScale = part.Size;
            }
            var previous = RenderTexture.active;
            var watch = Stopwatch.StartNew();
            _camera.Render();
            watch.Stop();
            RenderTexture.active = _surface;
            _image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            _image.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, file + ".png"), _image.EncodeToPNG());
            RenderTexture.active = previous;
            Debug.Log(FormattableString.Invariant($"H1_RENDER {file} tick={lab.Tick} cpu_ms={watch.Elapsed.TotalMilliseconds:F3}"));
        }

        public void Dispose()
        {
            foreach (var part in _parts) Object.DestroyImmediate(part.gameObject);
            foreach (var material in _materials) Object.DestroyImmediate(material);
            Object.DestroyImmediate(_camera.gameObject);
            Object.DestroyImmediate(_surface);
            Object.DestroyImmediate(_image);
        }
    }
}
