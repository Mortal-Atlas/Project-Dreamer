using System;
using System.Collections.Generic;
using System.IO;

namespace DreamMachine.Body
{
    // One tick ahead, normal impulse in N s. No object coordinates, mass or future labels as inputs.
    public sealed class ImpulsePredictor
    {
        const int Neighbors = 9;
        const int MaxSamples = 2048;
        public readonly struct Example
        {
            public readonly double Angle, Speed, Gap, Previous, Impulse;
            public Example(ElbowLab.StepSample sample, double target, double observedImpulse)
                : this(Math.Abs(sample.Feedback.Angle), Math.Abs(sample.Feedback.Velocity) / 5,
                    Math.Abs(target - sample.Feedback.Angle), sample.NormalImpulse, observedImpulse) { }

            internal Example(double angle, double speed, double gap, double previous, double impulse)
            {
                Angle = angle; Speed = speed; Gap = gap; Previous = previous; Impulse = impulse;
            }
        }

        readonly Example[] _examples;
        public double Uncertainty90 { get; }

        public ImpulsePredictor(IReadOnlyList<Example> examples, double uncertainty90 = 0)
        {
            if (examples.Count < Neighbors || examples.Count > MaxSamples ||
                !PredictiveMotor.Finite(uncertainty90) || uncertainty90 < 0)
                throw new ArgumentException("Invalid impulse training data.");
            _examples = new Example[examples.Count];
            for (int i = 0; i < examples.Count; i++)
            {
                Example e = examples[i];
                if (!PredictiveMotor.Finite(e.Angle) || !PredictiveMotor.Finite(e.Speed) ||
                    !PredictiveMotor.Finite(e.Gap) || !PredictiveMotor.Finite(e.Previous) ||
                    !PredictiveMotor.Finite(e.Impulse) || e.Impulse < 0 || e.Previous < 0)
                    throw new ArgumentException("Invalid impulse example.");
                _examples[i] = e;
            }
            Uncertainty90 = uncertainty90;
        }

        public (double Impulse, double Uncertainty) Predict(Example context)
        {
            Span<double> distance = stackalloc double[Neighbors];
            Span<double> outcome = stackalloc double[Neighbors];
            for (int i = 0; i < Neighbors; i++) distance[i] = double.PositiveInfinity;
            foreach (var e in _examples)
            {
                double d = Square(e.Angle - context.Angle) + Square(e.Speed - context.Speed) +
                    Square(e.Gap - context.Gap) + Square(e.Previous - context.Previous);
                if (d >= distance[Neighbors - 1]) continue;
                int at = Neighbors - 1;
                while (at > 0 && d < distance[at - 1])
                {
                    distance[at] = distance[at - 1];
                    outcome[at] = outcome[at - 1];
                    at--;
                }
                distance[at] = d;
                outcome[at] = e.Impulse;
            }
            double weighted = 0, weight = 0, squares = 0;
            for (int i = 0; i < Neighbors; i++)
            {
                double w = 1 / (0.005 + distance[i]);
                weighted += w * outcome[i];
                squares += w * outcome[i] * outcome[i];
                weight += w;
            }
            double prediction = weighted / weight;
            double spread = Math.Sqrt(Math.Max(0, squares / weight - prediction * prediction));
            return (prediction, Math.Max(Uncertainty90, spread));
        }

        public ImpulsePredictor Calibrate(IReadOnlyList<Example> development)
        {
            if (development.Count == 0) throw new ArgumentException("Development set is required.");
            var errors = new double[development.Count];
            for (int i = 0; i < errors.Length; i++)
                errors[i] = Math.Abs(Predict(development[i]).Impulse - development[i].Impulse);
            Array.Sort(errors);
            return new ImpulsePredictor(_examples, errors[(int)Math.Ceiling(errors.Length * 0.9) - 1]);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(1);
            writer.Write(Uncertainty90);
            writer.Write(_examples.Length);
            foreach (Example e in _examples)
            {
                writer.Write(e.Angle); writer.Write(e.Speed); writer.Write(e.Gap);
                writer.Write(e.Previous); writer.Write(e.Impulse);
            }
        }

        public static ImpulsePredictor Read(BinaryReader reader)
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported impulse predictor.");
            double uncertainty = reader.ReadDouble();
            int count = reader.ReadInt32();
            if (count < Neighbors || count > MaxSamples) throw new InvalidDataException("Invalid impulse model size.");
            var examples = new Example[count];
            for (int i = 0; i < count; i++)
                examples[i] = new Example(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                    reader.ReadDouble(), reader.ReadDouble());
            return new ImpulsePredictor(examples, uncertainty);
        }

        static double Square(double value) => value * value;
    }
}
