using System;
using System.Collections.Generic;
using System.IO;
using DreamMachine.Sim;
using DreamMachine.Util;

namespace DreamMachine.Body
{
    // A one-tick predictor. Inputs are taken before Step(), labels after it.
    public sealed class ContactPredictor
    {
        const int Count = 5;
        const string Features = "bias;requested-minus-sensed-angle/2;previous-rad-s/8;previous-impulse-ns/0.4;previous-resistance-nm/2";
        readonly double[] _weights = new double[Count];

        public readonly struct Example
        {
            public readonly ElbowLab.StepSample Previous;
            public readonly double Requested;
            public readonly bool Contact;

            public Example(ElbowLab.StepSample previous, double requested, bool contact)
            {
                Previous = previous;
                Requested = requested;
                Contact = contact;
            }
        }

        static double Feature(int index, ElbowLab.StepSample previous, double target)
        {
            switch (index)
            {
                case 0: return 1;
                case 1: return Math.Abs(target - previous.Feedback.Angle) / 2;
                case 2: return Math.Abs(previous.Feedback.Velocity) / 8;
                case 3: return previous.NormalImpulse / 0.4;
                default: return Math.Abs(previous.Feedback.Resistance) / 2;
            }
        }

        public double Predict(ElbowLab.StepSample previous, double target)
        {
            double z = 0;
            for (int i = 0; i < Count; i++) z += _weights[i] * Feature(i, previous, target);
            return 1 / (1 + Math.Exp(-z));
        }

        public void Train(IReadOnlyList<Example> training, int epochs)
        {
            if (training.Count == 0 || epochs < 1) throw new ArgumentException("Training data and a positive budget are required.");
            var gradient = new double[Count];
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                Array.Clear(gradient, 0, Count);
                foreach (Example sample in training)
                {
                    double error = Predict(sample.Previous, sample.Requested) - (sample.Contact ? 1 : 0);
                    for (int i = 0; i < Count; i++) gradient[i] += error * Feature(i, sample.Previous, sample.Requested);
                }
                for (int i = 0; i < Count; i++) _weights[i] -= 0.75 *
                    (gradient[i] / training.Count + (i == 0 ? 0 : 0.001 * _weights[i]));
            }
        }

        public void Save(string path, string individual, string provenance, string evaluation)
        {
            if (string.IsNullOrWhiteSpace(individual) || string.IsNullOrWhiteSpace(provenance))
                throw new ArgumentException("A predictor requires an identity and training provenance.");
            var data = new Dictionary<string, object>
            {
                { "version", 1 }, { "topology", "palm-chain-v1" }, { "length_m", ElbowLab.ArmLength },
                { "features", Features }, { "individual", individual }, { "provenance", provenance },
                { "evaluation", evaluation ?? "" }, { "weights", _weights }
            };
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MiniJson.Stringify(data));
            WorldSimulation.PublishCheckpoint(path, bytes, file =>
            {
                if (!File.Exists(file) && !File.Exists(file + ".previous")) return false;
                Load(file, individual);
                return true;
            });
        }

        public static ContactPredictor Load(string path, string individual)
        {
            try
            {
                var d = MiniJson.AsObject(MiniJson.Parse(File.ReadAllText(path)));
                if (d == null || MiniJson.AsInt(d["version"]) != 1 || MiniJson.AsString(d["topology"]) != "palm-chain-v1" ||
                    MiniJson.AsDouble(d["length_m"]) != ElbowLab.ArmLength || MiniJson.AsString(d["features"]) != Features ||
                    MiniJson.AsString(d["individual"]) != individual || string.IsNullOrWhiteSpace(MiniJson.AsString(d["provenance"])))
                    throw new InvalidDataException("Incompatible predictor state.");
                var weights = MiniJson.AsArray(d["weights"]);
                if (weights.Count != Count) throw new InvalidDataException("Invalid predictor weight count.");
                var result = new ContactPredictor();
                for (int i = 0; i < Count; i++)
                {
                    double weight = MiniJson.AsDouble(weights[i]);
                    if (double.IsNaN(weight) || double.IsInfinity(weight)) throw new InvalidDataException("Invalid predictor weight.");
                    result._weights[i] = weight;
                }
                return result;
            }
            catch (Exception error) when (error is FormatException || error is InvalidCastException ||
                error is KeyNotFoundException || error is OverflowException)
            {
                throw new InvalidDataException("Invalid predictor checkpoint.", error);
            }
        }
    }
}
