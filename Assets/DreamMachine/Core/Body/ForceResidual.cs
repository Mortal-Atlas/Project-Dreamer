using System;
using System.IO;

namespace DreamMachine.Body
{
    public enum ForceFallback { None, PeakImpulse, RepeatedPredictionMiss, Tracking, AlternatingCorrection }

    public sealed class ForceResidual
    {
        public const double PeakGuard = 0.72;
        const double Slew = 0.18;
        public readonly bool Gated;
        public double Gain { get; private set; } = 0.75;
        public double PreviousCorrection { get; private set; }
        public double SignedError { get; private set; }
        public int FallbackTick { get; private set; }
        public ForceFallback Fallback { get; private set; }
        public int Updates { get; private set; }
        int _misses, _tracking, _reversals;

        public ForceResidual(bool gated) { Gated = gated; }

        public double Before(ElbowLab.StepSample previous, double target, double predicted, double uncertainty)
        {
            if (!PredictiveMotor.Finite(predicted) || !PredictiveMotor.Finite(uncertainty) ||
                predicted < 0 || uncertainty < 0)
                throw new ArgumentOutOfRangeException(nameof(predicted));
            if (Fallback != ForceFallback.None) return 0;
            double gap = target - previous.Feedback.Angle;
            double direction = Math.Sign(gap);
            double approach = Math.Max(0, direction * previous.Feedback.Velocity);
            double confidence = Gated ? 1 / (1 + uncertainty / 0.12) : 1;
            double desired = -direction * Gain * Math.Min(0.8, predicted * 3.6) * confidence *
                Math.Min(1, approach / 1.2);
            double next = Math.Max(PreviousCorrection - Slew, Math.Min(PreviousCorrection + Slew, desired));
            bool reversal = Math.Abs(next) > 0.025 && Math.Abs(PreviousCorrection) > 0.025 &&
                Math.Sign(next) != Math.Sign(PreviousCorrection);
            _reversals = reversal ? _reversals + 1 : Math.Max(0, _reversals - 1);
            PreviousCorrection = next;
            return next;
        }

        public void After(ElbowLab.StepSample observed, double target, double predicted, double uncertainty)
        {
            SignedError = observed.NormalImpulse - predicted;
            if (Fallback != ForceFallback.None) return;
            _misses = Math.Abs(SignedError) > Math.Max(0.12, uncertainty) ? _misses + 1 : Math.Max(0, _misses - 1);
            double tracking = Math.Abs(target - observed.Feedback.Angle);
            _tracking = observed.Tick > 8 && tracking > 0.18 ? _tracking + 1 : 0;
            if (observed.NormalImpulse > PeakGuard) RollBack(ForceFallback.PeakImpulse);
            else if (_misses >= 3) RollBack(ForceFallback.RepeatedPredictionMiss);
            else if (_tracking >= 5) RollBack(ForceFallback.Tracking);
            else if (_reversals >= 3) RollBack(ForceFallback.AlternatingCorrection);
            if (Fallback != ForceFallback.None) return;
            Gain = Math.Max(0, Math.Min(1, Gain + 0.22 * Math.Max(-0.25, Math.Min(0.25, SignedError))
                - 0.015 * Math.Min(1, tracking)));
            Updates++;

            void RollBack(ForceFallback reason)
            {
                Fallback = reason;
                FallbackTick = observed.Tick;
                Gain = 0.75;
                PreviousCorrection = 0;
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(1);
            writer.Write(Gated); writer.Write(Gain); writer.Write(PreviousCorrection);
            writer.Write(SignedError); writer.Write(FallbackTick); writer.Write((int)Fallback);
            writer.Write(Updates); writer.Write(_misses); writer.Write(_tracking); writer.Write(_reversals);
        }

        public static ForceResidual Read(BinaryReader reader)
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unknown force motor version.");
            var result = new ForceResidual(reader.ReadBoolean())
            {
                Gain = reader.ReadDouble(), PreviousCorrection = reader.ReadDouble(), SignedError = reader.ReadDouble(),
                FallbackTick = reader.ReadInt32(), Fallback = (ForceFallback)reader.ReadInt32(),
                Updates = reader.ReadInt32(), _misses = reader.ReadInt32(), _tracking = reader.ReadInt32(),
                _reversals = reader.ReadInt32()
            };
            if (!PredictiveMotor.Finite(result.Gain) || result.Gain < 0 || result.Gain > 1 ||
                !PredictiveMotor.Finite(result.PreviousCorrection) || Math.Abs(result.PreviousCorrection) > 0.8 ||
                !PredictiveMotor.Finite(result.SignedError) || !Enum.IsDefined(typeof(ForceFallback), result.Fallback) ||
                result.FallbackTick < 0 || result.Updates < 0 || result._misses < 0 || result._tracking < 0 ||
                result._reversals < 0 || ((result.FallbackTick == 0) != (result.Fallback == ForceFallback.None)))
                throw new InvalidDataException("Invalid force motor state.");
            return result;
        }
    }
}
