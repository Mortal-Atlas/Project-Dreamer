using System;
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
internal static class PredictiveMotorStudy
{
    const string Key = "Dreamer.PredictiveMotorStudy";
    const int Ticks = 24;
    static string DirectoryPath => Application.persistentDataPath;
    static string Checkpoint => Path.Combine(DirectoryPath, "phase-b-restart.checkpoint");
    static string Probes => Path.Combine(DirectoryPath, "phase-b-restart-probe.csv");

    static PredictiveMotorStudy()
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

    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetInt(Key, 0);
            EditorApplication.Exit(0);
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            if (SessionState.GetInt(Key, 0) == 1) Study();
            else VerifyRestart();
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static void Study()
    {
        var models = TrainMatchedModels();
        using var results = File.CreateText(Path.Combine(DirectoryPath, "phase-b-motor-results.csv"));
        using var traces = File.CreateText(Path.Combine(DirectoryPath, "phase-b-motor-traces.csv"));
        results.WriteLine("trial,condition,x_m,y_m,mass_kg,friction,initial_rad,target_rad,ticks,mae_rad,peak_impulse_ns,cumulative_ns,work_j,absolute_work_j,velocity_reversals,stops,gain,updates,mean_us");
        traces.Write("trial,condition,backend,tick,sim_s,target_rad,previous_angle_rad,angle_rad,velocity_rad_s,resistance_nm,baseline_nm,proposed_nm,applied_correction_nm,command_nm,prediction,observed,signed_error,gain,impulse_ns,work_j,absolute_work_j");
        for (int r = 0; r < ElbowLab.ReceptorCount; r++) traces.Write("," + ElbowLab.ReceptorId(r) + "_n");
        traces.WriteLine();
        foreach (int index in Enumerable.Range(0, 12))
        {
            var rng = new SeededRandom(43241 + 131 * index);
            double side = index % 2 == 0 ? 1 : -1;
            double reach = index % 4 == 0 ? 0.43 : rng.Range(0.29f, 0.36f);
            double bearing = side * rng.Range(0.26f, 0.89f);
            double target = side * rng.Range(0.65f, 1.09f);
            var settings = new ElbowLab.Settings(-1.2, 1.2, rng.Range(-0.16f, 0.16f), rng.Range(0.13f, 1.2f),
                rng.Range(0.04f, 0.9f), reach * Math.Cos(bearing), reach * Math.Sin(bearing), Ticks);
            foreach (MotorCondition condition in Enum.GetValues(typeof(MotorCondition)))
            {
                string model = condition == MotorCondition.TransferAdaptive ? models.Transfer : models.Native;
                var lab = new NativeElbowLab(settings, "son");
                lab.RequestTarget(target);
                using var run = new PredictiveNativeTrial(lab, model, condition);
                using var engineered = index == 0 && condition == MotorCondition.Engineered ?
                    new NativeElbowLab(settings, "son") : null;
                engineered?.RequestTarget(target);
                double error = 0, impulse = 0, peak = 0, signed = 0, absolute = 0;
                int turns = 0, lastDirection = 0;
                long time = 0;
                StreamWriter probes = null;
                try
                {
                    while (!run.Finished)
                    {
                        long start = Stopwatch.GetTimestamp();
                        var sample = run.Step();
                        time += Stopwatch.GetTimestamp() - start;
                        if (engineered != null)
                        {
                            engineered.Step();
                            if (!lab.CaptureCheckpoint().SequenceEqual(engineered.CaptureCheckpoint()))
                                throw new Exception("Engineered baseline changed.");
                        }
                        error += Math.Abs(target - sample.Feedback.Angle);
                        impulse += sample.NormalImpulse;
                        peak = Math.Max(peak, sample.NormalImpulse);
                        signed += lab.LastSignedWork;
                        absolute += lab.LastAbsoluteWork;
                        int direction = Math.Abs(sample.Feedback.Velocity) < 0.15 ? 0 : Math.Sign(sample.Feedback.Velocity);
                        if (lastDirection != 0 && direction != 0 && direction != lastDirection) turns++;
                        if (direction != 0) lastDirection = direction;
                        traces.Write(FormattableString.Invariant($"{index},{condition},{lab.Backend},{lab.Tick},{lab.SimSeconds:R},{target:R},{sample.PreviousAngle:R},{lab.Angle:R},{sample.Feedback.Velocity:R},{sample.Feedback.Resistance:R},{lab.LastBaselineTorque:R},{run.LastProposal:R},{lab.LastAppliedCorrection:R},{sample.Feedback.Effort:R},{run.LastProbability:R},{(sample.NormalImpulse > 0 ? 1 : 0)},{run.LastError:R},{run.Motor.Gain:R},{sample.NormalImpulse:R},{lab.LastSignedWork:R},{lab.LastAbsoluteWork:R}"));
                        for (int r = 0; r < ElbowLab.ReceptorCount; r++)
                            traces.Write(FormattableString.Invariant($",{lab.ReadRegion(r).Force:R}"));
                        traces.WriteLine();
                        if (index != 2 || condition != MotorCondition.Adaptive) continue;
                        if (lab.Tick == 6)
                        {
                            var watch = Stopwatch.StartNew();
                            run.SaveCheckpoint(Checkpoint);
                            watch.Stop();
                            Debug.Log($"MOTOR_SAVE tick=6 gain={run.Motor.Gain:R} updates={run.Motor.Updates} ms={watch.Elapsed.TotalMilliseconds:F3} bytes={new FileInfo(Checkpoint).Length}");
                            probes = File.CreateText(Probes);
                        }
                        else if (probes != null) probes.WriteLine(Probe(run));
                    }
                }
                finally { probes?.Dispose(); }
                results.WriteLine(FormattableString.Invariant($"{index},{condition},{settings.X:R},{settings.Y:R},{settings.Mass:R},{settings.Friction:R},{settings.Initial:R},{target:R},{lab.Tick},{error / lab.Tick:R},{peak:R},{impulse:R},{signed:R},{absolute:R},{turns},{run.Motor.ProtectiveStops},{run.Motor.Gain:R},{run.Motor.Updates},{time * 1e6 / Stopwatch.Frequency / lab.Tick:R}"));
            }
        }
        Debug.Log("MOTOR_STUDY 12 configurations × 4 conditions completed.");
    }

    static (string Native, string Transfer) TrainMatchedModels()
    {
        // The predecessor study exports the complete per-tick permitted sensor context and contact labels.
        string source = Path.Combine(DirectoryPath, "native-contact-dataset.csv");
        if (!File.Exists(source)) throw new FileNotFoundException("Run NativeElbowSmoke.Run in the isolated project first.", source);
        var native = new System.Collections.Generic.List<ContactPredictor.Example>();
        var planar = new System.Collections.Generic.List<ContactPredictor.Example>();
        var calibration = new System.Collections.Generic.List<ContactPredictor.Example>();
        foreach (string line in File.ReadLines(source).Skip(1))
        {
            string[] f = line.Split(',');
            int trial = int.Parse(f[0], CultureInfo.InvariantCulture);
            bool physx = f[2] == "unity-physx-v1";
            var previous = new ElbowLab.StepSample(0, 0, 0, Number(7), 0, 0,
                new ElbowLab.Proprioception(Number(5), Number(6), 0, Number(8)));
            var row = new ContactPredictor.Example(previous, Number(4), f[9] == "1");
            if (physx && trial < 16) native.Add(row);
            if (!physx && trial < 8) planar.Add(row);
            if (physx && trial >= 16 && trial < 24) calibration.Add(row);
            double Number(int i) => double.Parse(f[i], CultureInfo.InvariantCulture);
        }
        if (native.Count != 192 || planar.Count != 96 || calibration.Count != 96)
            throw new InvalidDataException("Unexpected Phase A.2 dataset dimensions.");
        var a = new ContactPredictor();
        var d = new ContactPredictor();
        a.Train(native, 250);
        d.Train(planar, 250);
        d.Train(calibration, 250);
        return (a.Capture("son", "Phase B: 16 native trials, 250 epochs", ""),
            d.Capture("son", "Phase B: 8 planar plus 8 native calibration trials, 250 epochs per stage", ""));
    }

    static string Probe(PredictiveNativeTrial run) => FormattableString.Invariant(
        $"{run.Lab.Tick},{run.LastProbability:R},{run.LastProposal:R},{run.Lab.LastAppliedCorrection:R},{run.Lab.Latest.Feedback.Effort:R},{run.Lab.Angle:R},{run.Lab.X:R},{run.Lab.Y:R},{run.Motor.Gain:R},{run.Motor.Updates},{run.LastError:R}");

    static void VerifyRestart()
    {
        var watch = Stopwatch.StartNew();
        using var run = PredictiveNativeTrial.LoadCheckpoint(Checkpoint) ?? throw new Exception("Run the Phase B study first.");
        watch.Stop();
        double max = 0;
        int count = 0;
        int originalUpdates = run.Motor.Updates;
        foreach (string expected in File.ReadLines(Probes))
        {
            run.Step();
            var actual = Probe(run).Split(',');
            string[] wanted = expected.Split(',');
            for (int i = 0; i < actual.Length; i++)
            {
                double delta = Math.Abs(double.Parse(actual[i], CultureInfo.InvariantCulture) -
                    double.Parse(wanted[i], CultureInfo.InvariantCulture));
                if (delta > 1e-6) throw new Exception($"Restart field {i} differed at tick {run.Lab.Tick}: {delta:R}");
                max = Math.Max(max, delta);
            }
            count++;
        }
        if (count != 18 || run.Motor.Updates != originalUpdates + count)
            throw new Exception("The restored controller did not continue learning.");
        Debug.Log($"MOTOR_RELOAD {count} steps, max deviation={max:R}, updates {originalUpdates}→{run.Motor.Updates}, elapsed_ms={watch.Elapsed.TotalMilliseconds:F3}");
    }
}
