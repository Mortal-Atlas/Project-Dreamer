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
using UnityEngine;
using Debug = UnityEngine.Debug;

internal static class NativeContactStudy
{
    const int TrialCount = 32;
    const int Ticks = 12;
    const int TrainCount = 16;
    const int CalibrationCount = 8;
    const string Person = "son";

    public static void Run(string directory)
    {
        var native = new List<ContactPredictor.Example>[3] { new(), new(), new() };
        var planar = new List<ContactPredictor.Example>();
        using var csv = File.CreateText(Path.Combine(directory, "native-contact-dataset.csv"));
        csv.WriteLine("trial,split,backend,tick,requested_rad,prior_angle_rad,prior_velocity_rad_s,prior_impulse_ns,prior_resistance_nm,next_contact");
        using var configuration = File.CreateText(Path.Combine(directory, "native-contact-trials.csv"));
        configuration.WriteLine("trial,split,initial_rad,target_rad,x_m,y_m,mass_kg,friction");
        for (int trial = 0; trial < TrialCount; trial++)
        {
            int split = trial < TrainCount ? 0 : trial < TrainCount + CalibrationCount ? 1 : 2;
            var rng = new SeededRandom(1721 + trial * 97);
            double direction = trial % 2 == 0 ? 1 : -1;
            double initial = rng.Range(-0.12f, 0.12f);
            double target = direction * rng.Range(0.65f, 1.08f);
            double radius = trial % 5 == 0 ? 0.47 : rng.Range(0.28f, 0.37f);
            double bearing = direction * rng.Range(0.25f, 0.92f);
            var settings = new ElbowLab.Settings(-1.2, 1.2, initial, rng.Range(0.12f, 0.9f),
                rng.Range(0.08f, 0.82f), radius * Math.Cos(bearing), radius * Math.Sin(bearing), Ticks);
            configuration.WriteLine(FormattableString.Invariant(
                $"{trial},{split},{initial:R},{target:R},{settings.X:R},{settings.Y:R},{settings.Mass:R},{settings.Friction:R}"));
            using var physical = new NativeElbowLab(settings, Person);
            Collect(physical, native[split], trial, split, target, csv);
            if (split == 0) Collect(new ElbowLab(settings, Person), planar, trial, split, target, csv);
        }

        var scratch = new ContactPredictor();
        var transfer = new ContactPredictor();
        var calibrated = new ContactPredictor();
        var watch = Stopwatch.StartNew();
        scratch.Train(native[0], 250);
        transfer.Train(planar, 250);
        calibrated.Train(planar, 250);
        calibrated.Train(native[1], 100);
        watch.Stop();
        Debug.Log($"CONTACT_TRAIN four fits combined {watch.Elapsed.TotalMilliseconds:F3} ms; test positivity {native[2].Count(x => x.Contact)}/{native[2].Count}");
        using var probes = File.CreateText(Path.Combine(directory, "native-predictor-probes.csv"));
        Report("native-scratch", scratch, "16 native trials, 250 epochs");
        Report("planar-transfer", transfer, "16 matched planar trials, 250 epochs");
        Report("calibrated-transfer", calibrated, "16 planar trials, 250 epochs then 8 native calibration trials, 100 epochs");
        double prevalence = native[0].Count(x => x.Contact) / (double)native[0].Count;
        Debug.Log("CONTACT_SCORE constant " + Metrics(native[2], _ => prevalence));
        Debug.Log("CONTACT_SCORE persistence " + Metrics(native[2], x => x.Previous.NormalImpulse > 0 ? 1 : 0));

        void Report(string id, ContactPredictor predictor, string provenance)
        {
            string score = Metrics(native[2], x => predictor.Predict(x.Previous, x.Requested));
            string path = Path.Combine(directory, id + ".predictor");
            predictor.Save(path, Person, "seed schedule 1721+97*trial; native held-out trials 24–31; " + provenance, score);
            var restored = ContactPredictor.Load(path, Person);
            foreach (var row in native[2])
            {
                double expected = predictor.Predict(row.Previous, row.Requested);
                if (restored.Predict(row.Previous, row.Requested) != expected)
                    throw new Exception("Predictor changed after reload.");
                var s = row.Previous;
                probes.WriteLine(FormattableString.Invariant($"{id},{s.Feedback.Angle:R},{s.Feedback.Velocity:R},{s.NormalImpulse:R},{s.Feedback.Resistance:R},{row.Requested:R},{expected:R}"));
            }
            watch.Restart();
            double check = 0;
            for (int i = 0; i < 10000; i++)
            {
                var row = native[2][i % native[2].Count];
                check += predictor.Predict(row.Previous, row.Requested);
            }
            watch.Stop();
            Debug.Log(FormattableString.Invariant($"CONTACT_SCORE {id} {score}; inference_us={watch.Elapsed.TotalMilliseconds / 10:F3}; weight_bytes={new FileInfo(path).Length}; checksum={check:F2}"));
        }
    }

    static string Metrics(List<ContactPredictor.Example> data, Func<ContactPredictor.Example, double> predict)
    {
        int tp = 0, fp = 0, fn = 0;
        double squared = 0;
        foreach (var row in data)
        {
            double probability = predict(row);
            if (probability >= 0.5)
            {
                if (row.Contact) tp++;
                else fp++;
            }
            else if (row.Contact) fn++;
            double error = probability - (row.Contact ? 1 : 0);
            squared += error * error;
        }
        double f1 = 2d * tp / Math.Max(1, 2 * tp + fp + fn);
        return FormattableString.Invariant($"Brier={squared / data.Count:F5} F1={f1:F3} TP={tp} FP={fp} FN={fn}");
    }

    static void Collect(IElbowFixture lab, List<ContactPredictor.Example> data, int trial, int split,
        double target, StreamWriter csv)
    {
        lab.RequestTarget(target);
        while (!lab.Finished)
        {
            var previous = lab.Latest;
            bool contact = lab.Step().NormalImpulse > 0;
            data.Add(new ContactPredictor.Example(previous, target, contact));
            csv.WriteLine(FormattableString.Invariant($"{trial},{split},{lab.Backend},{lab.Tick},{target:R},{previous.Feedback.Angle:R},{previous.Feedback.Velocity:R},{previous.NormalImpulse:R},{previous.Feedback.Resistance:R},{(contact ? 1 : 0)}"));
        }
    }

    public static void Reload()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        string directory = Application.persistentDataPath;
        var models = new Dictionary<string, ContactPredictor>();
        int count = 0;
        foreach (string line in File.ReadLines(Path.Combine(directory, "native-predictor-probes.csv")))
        {
            string[] f = line.Split(',');
            if (!models.TryGetValue(f[0], out var model))
                models.Add(f[0], model = ContactPredictor.Load(Path.Combine(directory, f[0] + ".predictor"), Person));
            var previous = new ElbowLab.StepSample(0, 0, 0, Number(3), 0, 0,
                new ElbowLab.Proprioception(Number(1), Number(2), 0, Number(4)));
            if (model.Predict(previous, Number(5)) != Number(6))
                throw new Exception("Prediction changed across process restart.");
            count++;
            double Number(int i) => double.Parse(f[i], CultureInfo.InvariantCulture);
        }
        Debug.Log($"CONTACT_RELOAD {count} predictions identical in a fresh Unity process");
        EditorApplication.Exit(0);
    }
}
