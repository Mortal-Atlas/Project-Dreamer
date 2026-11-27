using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using DreamMachine.Body;
using DreamMachine.UnityBridge;
using DreamMachine.Util;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
internal static class ForceMotorStudy
{
    const string Key = "Dreamer.ForceMotorStudy";
    const int Ticks = 24;
    const double MeanTrackingMargin = 0.002;
    const double MeanPeakMargin = 0.005;
    const double WorstTrackingMargin = 0.020;
    const double WorstPeakMargin = 0.050;
    static string PathFor(string file) => Path.Combine(Application.persistentDataPath, "phase-b1-" + file);

    static ForceMotorStudy()
    {
        if (SessionState.GetInt(Key, 0) != 0) EditorApplication.playModeStateChanged += Changed;
    }
    public static void Run() => Enter(1);
    public static void Reload() => Enter(2);
    static void Enter(int mode)
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        SessionState.SetInt(Key, mode);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetInt(Key, 0);
            EditorApplication.Exit(0);
        }
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            if (SessionState.GetInt(Key, 0) == 1) Study();
            else VerifyRestart();
            EditorApplication.ExitPlaymode();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static (ElbowLab.Settings Config, double Target) Configuration(int i)
    {
        var rng = new SeededRandom(79301 + i * 149);
        double side = i % 2 == 0 ? 1 : -1;
        double radius = i % 4 == 0 ? 0.44 : rng.Range(0.285f, 0.375f);
        double angle = side * rng.Range(0.23f, 0.91f);
        return (new ElbowLab.Settings(-1.2, 1.2, rng.Range(-0.17f, 0.17f), rng.Range(0.13f, 1.3f),
            rng.Range(0.04f, 0.93f), radius * Math.Cos(angle), radius * Math.Sin(angle), Ticks),
            side * rng.Range(0.62f, 1.10f));
    }

    static void Study()
    {
        var input = new List<ImpulsePredictor.Example>[3] { new(), new(), new() };
        var probability = new List<ContactPredictor.Example>[3] { new(), new(), new() };
        using var configs = File.CreateText(PathFor("configs.csv"));
        configs.WriteLine("trial,split,initial_rad,target_rad,x_m,y_m,mass_kg,friction");
        for (int i = 0; i < 64; i++)
        {
            int split = i < 32 ? 0 : i < 48 ? 1 : 2;
            var (s, target) = Configuration(i);
            configs.WriteLine(I($"{i},{split},{s.Initial:R},{target:R},{s.X:R},{s.Y:R},{s.Mass:R},{s.Friction:R}"));
            using var lab = new NativeElbowLab(s, "son");
            lab.RequestTarget(target);
            for (int step = 0; step < 12; step++)
            {
                var previous = lab.Latest;
                double impulse = lab.Step().NormalImpulse;
                input[split].Add(new ImpulsePredictor.Example(previous, target, impulse));
                probability[split].Add(new ContactPredictor.Example(previous, target, impulse > 0));
            }
        }
        var predictor = new ContactPredictor();
        predictor.Train(probability[0], 250);
        string model = predictor.Capture("son", "B.1 PhysX seed 79301+149*trial; trials 0–31 train", "");
        var impulseModel = new ImpulsePredictor(input[0]).Calibrate(input[1]);
        double typical = input[0].Where(x => x.Impulse > 0).Average(x => x.Impulse);
        bool gated = false;
        using (var report = File.CreateText(PathFor("predictions.csv")))
        {
            report.WriteLine("trial,tick,horizon_ticks,observed_ns,predicted_ns,signed_error_ns,absolute_error_ns,uncertainty_ns,persistence_ns,engineered_ns,frozen_ns");
            for (int split = 1; split <= 2; split++)
            {
                double[] squared = new double[4], absolute = new double[4];
                int cover = 0;
                long predictTicks = 0;
                for (int j = 0; j < input[split].Count; j++)
                {
                    var row = input[split][j];
                    var p = probability[split][j];
                    long start = Stopwatch.GetTimestamp();
                    var prediction = impulseModel.Predict(row);
                    predictTicks += Stopwatch.GetTimestamp() - start;
                    double persistence = row.Previous;
                    double engineered = 0.35 * row.Previous;
                    double frozen = predictor.Predict(p.Previous, p.Requested) * typical;
                    double[] estimates = { prediction.Impulse, persistence, engineered, frozen };
                    for (int m = 0; m < estimates.Length; m++)
                    {
                        double error = row.Impulse - estimates[m];
                        squared[m] += error * error;
                        absolute[m] += Math.Abs(error);
                    }
                    double signed = row.Impulse - prediction.Impulse;
                    if (Math.Abs(signed) <= prediction.Uncertainty) cover++;
                    report.WriteLine(I($"{(split == 1 ? 32 : 48) + j / 12},{j % 12 + 1},1,{row.Impulse:R},{prediction.Impulse:R},{signed:R},{Math.Abs(signed):R},{prediction.Uncertainty:R},{persistence:R},{engineered:R},{frozen:R}"));
                }
                int n = input[split].Count;
                Debug.Log(I($"FORCE_PREDICT split={split} RMSE={string.Join("/", squared.Select(x => Math.Sqrt(x / n).ToString("F6", CultureInfo.InvariantCulture)))} MAE={string.Join("/", absolute.Select(x => (x / n).ToString("F6", CultureInfo.InvariantCulture)))} coverage={cover}/{n} q90={impulseModel.Uncertainty90:F6} inference_us={predictTicks * 1e6 / Stopwatch.Frequency / n:F3}"));
                if (split == 1) gated = squared[0] < squared[1] && squared[0] < squared[2] && squared[0] < squared[3];
            }
        }
        if (!gated)
        {
            Debug.Log("FORCE_GATE rejected impulse predictor on development RMSE. No native force corrections executed.");
            return;
        }
        RunControllers(model, impulseModel);
    }

    static void RunControllers(string model, ImpulsePredictor predictor)
    {
        using var report = File.CreateText(PathFor("motor.csv"));
        using var trace = File.CreateText(PathFor("trace.csv"));
        report.WriteLine("trial,mode,tracking_rad,peak_ns,cumulative_ns,absolute_work_j,reversals,correction_variation_nm,fallback,fallback_tick,updates,ticks,mean_us");
        trace.WriteLine("trial,mode,tick,target_rad,angle_rad,velocity_rad_s,predicted_ns,uncertainty_ns,observed_ns,signed_error_ns,corrected_nm,work_j,fallback,fallback_tick");
        bool save = true;
        for (int i = 48; i < 64; i++)
        {
            var (s, target) = Configuration(i);
            for (int mode = 0; mode < 5; mode++)
            {
                var lab = new NativeElbowLab(s, "son");
                lab.RequestTarget(target);
                using var phaseB = new PredictiveNativeTrial(lab, model,
                    mode == 0 ? MotorCondition.Engineered : mode == 1 ? MotorCondition.Frozen : MotorCondition.Adaptive);
                using var force = mode >= 3 ? new ForceAwareTrial(phaseB, predictor, new ForceResidual(mode == 4)) : null;
                double tracking = 0, peak = 0, sum = 0, work = 0, variation = 0, previous = 0;
                int reversals = 0, direction = 0;
                long time = 0;
                bool writingProbe = false;
                while (!(force?.Finished ?? phaseB.Finished))
                {
                    long start = Stopwatch.GetTimestamp();
                    var t = force == null ? phaseB.Step() : force.Step();
                    time += Stopwatch.GetTimestamp() - start;
                    tracking += Math.Abs(t.Feedback.Angle - target);
                    peak = Math.Max(peak, t.NormalImpulse);
                    sum += t.NormalImpulse;
                    work += lab.LastAbsoluteWork;
                    variation += Math.Abs(lab.LastAppliedCorrection - previous);
                    previous = lab.LastAppliedCorrection;
                    int next = Math.Abs(t.Feedback.Velocity) >= 0.15 ? Math.Sign(t.Feedback.Velocity) : 0;
                    if (next != 0 && direction != 0 && next != direction) reversals++;
                    if (next != 0) direction = next;
                    string forceFields = force == null ? ",," : I($"{force.Prediction:R},{force.Uncertainty:R},");
                    string signed = force == null ? "" : I($"{force.Residual.SignedError:R}");
                    trace.WriteLine(I($"{i},{mode},{lab.Tick},{target:R},{lab.Angle:R},{t.Feedback.Velocity:R},{forceFields}{t.NormalImpulse:R},{signed},{lab.LastAppliedCorrection:R},{lab.LastAbsoluteWork:R},{force?.Residual.Fallback},{force?.Residual.FallbackTick}"));
                    if (writingProbe) File.AppendAllText(PathFor("probes.csv"), Probe(force) + Environment.NewLine);
                    if (save && mode == 4 && lab.Tick == 6 && force.Residual.Fallback == ForceFallback.None)
                    {
                        force.SaveCheckpoint(PathFor("resume.checkpoint"));
                        File.WriteAllText(PathFor("probes.csv"), "");
                        writingProbe = true;
                        save = false;
                    }
                }
                report.WriteLine(I($"{i},{mode},{tracking / lab.Tick:R},{peak:R},{sum:R},{work:R},{reversals},{variation:R},{force?.Residual.Fallback},{force?.Residual.FallbackTick},{force?.Residual.Updates ?? phaseB.Motor.Updates},{lab.Tick},{time * 1e6 / Stopwatch.Frequency / lab.Tick:R}"));
            }
        }
        Debug.Log(I($"FORCE_STUDY gates mean_tracking={MeanTrackingMargin} mean_peak={MeanPeakMargin} worst_tracking={WorstTrackingMargin} worst_peak={WorstPeakMargin}"));
    }

    static string Probe(ForceAwareTrial t) => I($"{t.Lab.Tick},{t.Prediction:R},{t.Uncertainty:R},{t.Residual.SignedError:R},{t.Residual.Gain:R},{t.Lab.LastAppliedCorrection:R},{t.Lab.Angle:R},{t.Lab.X:R},{t.Residual.Updates}");
    static string I(FormattableString data) => FormattableString.Invariant(data);
    static void VerifyRestart()
    {
        using var trial = ForceAwareTrial.LoadCheckpoint(PathFor("resume.checkpoint")) ?? throw new Exception("No force checkpoint.");
        int count = 0, updates = trial.Residual.Updates;
        foreach (string expected in File.ReadLines(PathFor("probes.csv")))
        {
            trial.Step();
            string[] actual = Probe(trial).Split(','), prior = expected.Split(',');
            for (int i = 0; i < actual.Length; i++)
                if (Math.Abs(double.Parse(actual[i], CultureInfo.InvariantCulture) -
                    double.Parse(prior[i], CultureInfo.InvariantCulture)) > 1e-6)
                    throw new Exception("Force continuation mismatch at field " + i);
            count++;
        }
        if (count == 0 || trial.Residual.Updates <= updates) throw new Exception("No continued adaptation.");
        Debug.Log($"FORCE_RELOAD steps={count} updates={updates}→{trial.Residual.Updates}");
    }
}
