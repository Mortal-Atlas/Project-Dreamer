using System;
using System.IO;

namespace DreamMachine.Body
{
    public enum MotorCondition { Engineered, Frozen, Adaptive, TransferAdaptive }

    public sealed class PredictiveMotor
    {
        public const double MaximumCorrection = 0.8;
        public const double LearningRate = 0.12;
        public readonly MotorCondition Condition;
        readonly ContactPredictor _predictor;
        int _rapidReversals, _lastDirection;
        public double Gain { get; private set; } = 0.75;
        public int Updates { get; private set; }
        public int ProtectiveStops { get; private set; }
        public bool Stopped { get; private set; }

        public PredictiveMotor(MotorCondition condition, ContactPredictor predictor)
        {
            if (!Enum.IsDefined(typeof(MotorCondition), condition) || predictor == null)
                throw new ArgumentException("A valid controller condition and predictor are required.");
            Condition = condition;
            _predictor = predictor;
        }

        public (double Probability, double Correction) Before(ElbowLab.StepSample previous, double target)
        {
            if (Stopped) throw new InvalidOperationException("The protective stop is latched.");
            double probability = _predictor.Predict(previous, target);
            if (!Finite(probability) || !Finite(previous.Feedback.Velocity) ||
                !Finite(previous.Feedback.Angle) || !Finite(target))
                throw new InvalidOperationException("Nonfinite predictive motor input.");
            double direction = Math.Sign(target - previous.Feedback.Angle);
            double approach = Math.Max(0, direction * previous.Feedback.Velocity);
            double correction = Condition == MotorCondition.Engineered ? 0 :
                -direction * MaximumCorrection * probability * Gain * Math.Min(1, approach / 1.5);
            return (probability, correction);
        }

        public double After(double probability, ElbowLab.StepSample observed)
        {
            // Observed minus predicted. This residual is dimensionless.
            double error = (observed.NormalImpulse > 0 ? 1 : 0) - probability;
            double velocity = observed.Feedback.Velocity;
            int direction = Finite(velocity) && Math.Abs(velocity) >= 0.5 ? Math.Sign(velocity) : 0;
            if (direction != 0 && _lastDirection != 0 && direction != _lastDirection) _rapidReversals++;
            else _rapidReversals = Math.Max(0, _rapidReversals - 1);
            _lastDirection = direction;
            if (!Finite(error) || !Finite(observed.Feedback.Angle) || !Finite(observed.Feedback.Velocity) ||
                !Finite(observed.NormalImpulse) || Math.Abs(observed.Feedback.Velocity) > 9 ||
                observed.NormalImpulse > 1.25 || _rapidReversals >= 5)
            {
                Stopped = true;
                ProtectiveStops++;
                return error;
            }
            if (Condition == MotorCondition.Adaptive || Condition == MotorCondition.TransferAdaptive)
            {
                Gain = Math.Max(0, Math.Min(1, Gain + LearningRate * error));
                Updates++;
            }
            return error;
        }

        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public void WriteState(BinaryWriter writer)
        {
            writer.Write(1);
            writer.Write((int)Condition);
            writer.Write(Gain);
            writer.Write(Updates);
            writer.Write(ProtectiveStops);
            writer.Write(Stopped);
            writer.Write(_rapidReversals);
            writer.Write(_lastDirection);
        }

        public static PredictiveMotor ReadState(BinaryReader reader, ContactPredictor predictor)
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Incompatible predictive motor state.");
            var condition = (MotorCondition)reader.ReadInt32();
            if (!Enum.IsDefined(typeof(MotorCondition), condition)) throw new InvalidDataException("Invalid motor condition.");
            var controller = new PredictiveMotor(condition, predictor)
            {
                Gain = reader.ReadDouble(), Updates = reader.ReadInt32(),
                ProtectiveStops = reader.ReadInt32(), Stopped = reader.ReadBoolean(),
                _rapidReversals = reader.ReadInt32(), _lastDirection = reader.ReadInt32()
            };
            if (!Finite(controller.Gain) || controller.Gain < 0 || controller.Gain > 1 ||
                controller.Updates < 0 || controller.ProtectiveStops < 0 ||
                controller._rapidReversals < 0 || controller._rapidReversals > 5 ||
                controller._lastDirection < -1 || controller._lastDirection > 1 ||
                (controller.Stopped != (controller.ProtectiveStops > 0)))
                throw new InvalidDataException("Invalid predictive motor state.");
            return controller;
        }
    }
}
