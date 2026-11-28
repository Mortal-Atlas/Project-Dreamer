using System;
using System.IO;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.Social;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class HandAcquisitionStudy
{
    const string Key = "Dreamer.H1.Acquisition";
    static HandAcquisitionStudy()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Train() => Start(1);
    public static void Retained() => Start(2);

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
        var config = Resources.Load<TextAsset>("family");
        var population = config == null ? PopulationConfig.CreateDefault() : PopulationConfig.FromJson(config.text);
        var subject = population.Find("son");
        var folder = Application.persistentDataPath;
        var path = Path.Combine(folder, "phase-h1-grip.skill");
        bool training = mode == 1;
        var policy = training ? new GripAcquisition(subject.Id, subject.BirthSeed) :
            GripAcquisition.Restore(File.ReadAllBytes(path), subject.Id, subject.BirthSeed);
        using var file = File.CreateText(Path.Combine(folder, $"phase-h1-{(training ? "acquisition" : "retention-transfer")}.csv"));
        file.WriteLine("individual,calibration,skill,subject_seed,condition,attempt,object,cap_before_n,cap_after_n,result,slip_ticks,hold_ticks,peak_mm,work_j,ticks,update_us");
        if (training)
        {
            var familiar = new[] { ("training-A", .080f, .028f, .75f), ("training-B", .094f, .030f, .63f) };
            for (int attempt = 1; attempt <= 8; attempt++)
            {
                var obj = familiar[(attempt - 1) % familiar.Length];
                Trial(policy, "unchanged-founding", attempt, obj, GripAcquisition.InitialCap, file);
                Trial(policy, "engineered-reference", attempt, obj, GripAcquisition.ReferenceCap, file);
                Trial(policy, "adaptive-practice", attempt, obj, policy.Cap, file);
            }
            byte[] bytes = policy.Capture();
            WorldSimulation.PublishCheckpoint(path, bytes,
                candidate => File.Exists(candidate) &&
                    GripAcquisition.Restore(File.ReadAllBytes(candidate), subject.Id, subject.BirthSeed).History.Count > 0);
            Debug.Log(FormattableString.Invariant($"H1_LEARNING saved={bytes.Length} bytes cap={policy.Cap:F3} records={policy.History.Count}"));
        }
        else
        {
            var objects = new[] { ("training-A", .080f, .028f, .75f), ("training-B", .094f, .030f, .63f),
                ("fresh-light", .061f, .026f, .66f), ("fresh-heavy-low-friction", .145f, .034f, .30f),
                ("fresh-medium", .104f, .031f, .53f), ("fresh-narrow", .085f, .023f, .79f) };
            int attempt = 0;
            foreach (var obj in objects)
            {
                attempt++;
                Trial(policy, "frozen-retained", attempt, obj, policy.Cap, file);
                Trial(policy, "reset-adaptive", attempt, obj, GripAcquisition.InitialCap, file);
                Trial(policy, "engineered-reference", attempt, obj, GripAcquisition.ReferenceCap, file);
            }
            Debug.Log(FormattableString.Invariant($"H1_LEARNING restored cap={policy.Cap:F3} records={policy.History.Count}"));
        }
    }

    static void Trial(GripAcquisition policy, string condition, int attempt, (string Id, float Mass, float Radius, float Friction) obj,
        float cap, TextWriter file)
    {
        using var lab = new NativeHandLab(policy.BirthSeed, obj.Mass, obj.Radius, obj.Friction, cap);
        while (lab.Tick < 240 && !lab.Terminal) lab.Step();
        if (!lab.Terminal || !lab.FreeObject) throw new InvalidOperationException("Native grasp did not terminate with authority intact.");
        float after = cap;
        double updateUs = 0;
        if (condition == "adaptive-practice")
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            after = policy.Observe(lab.SlipTicks, lab.HeldTicks, lab.PeakClearance, lab.Phase == GripPhase.Complete).After;
            updateUs = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1e6 / System.Diagnostics.Stopwatch.Frequency;
        }
        string row = FormattableString.Invariant($"{policy.Individual},{GripAcquisition.CalibrationId},{GripAcquisition.SkillId},{policy.BirthSeed},{condition},{attempt},{obj.Id},{cap:F3},{after:F3},{lab.Outcome},{lab.SlipTicks},{lab.HeldTicks},{lab.PeakClearance*1000f:F3},{lab.Work:F5},{lab.Tick},{updateUs:F2}");
        file.WriteLine(row);
        file.Flush();
        Debug.Log("H1_PRACTICE " + row);
    }
}
