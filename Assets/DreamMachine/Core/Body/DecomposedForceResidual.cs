using System;
using System.Collections.Generic;
using System.IO;

namespace DreamMachine.Body
{
    public enum DecomposedFallbackReason
    {
        None = 0,
        SensorInvalid = 1,
        AdaptiveDivergence = 2,
        PeakImpulse = 3,
        RepeatedPredictionMiss = 4,
        Tracking = 5,
        AlternatingCorrection = 6,
        ExcessiveCorrection = 7
    }

    public struct ResidualTelemetry
    {
        public double ProposedTracking;
        public double ProposedAnticipation;
        public double ProposedLoadRegulation;
        public double ProposedEfficiency;
        public double ProposedAggregate;
        public double ClampedAggregate;
        public double SlewedAggregate;
        public double AppliedResidual;
        public double TotalActuatorTorque;
        public double RejectedAmount;
        public bool WasClamped;
        public bool WasSlewed;
    }

    public struct FallbackIncident
    {
        public int Tick;
        public DecomposedFallbackReason Reason;
        public double DiagnosticValue;
    }

    public sealed class FallbackLedger
    {
        public DecomposedFallbackReason ActiveReason { get; private set; } = DecomposedFallbackReason.None;
        public int FallbackTick { get; private set; }
        public int ConsecutiveStableTicks { get; private set; }
        public bool RecoveryAuthorized { get; private set; }
        public const int MinStableTicksForRecovery = 6;
        private readonly List<FallbackIncident> _incidents = new List<FallbackIncident>();
        public IReadOnlyList<FallbackIncident> Incidents => _incidents;

        public bool IsActive => ActiveReason != DecomposedFallbackReason.None;

        public void RecordFallback(int tick, DecomposedFallbackReason reason, double diagnosticValue = 0)
        {
            if (reason == DecomposedFallbackReason.None) return;
            ActiveReason = reason;
            FallbackTick = tick;
            ConsecutiveStableTicks = 0;
            RecoveryAuthorized = false;
            _incidents.Add(new FallbackIncident
            {
                Tick = tick,
                Reason = reason,
                DiagnosticValue = diagnosticValue
            });
        }

        public void ObserveStableTick(bool inputsValid, bool stateValid)
        {
            if (!IsActive) return;
            if (inputsValid && stateValid)
                ConsecutiveStableTicks++;
            else
                ConsecutiveStableTicks = 0;
        }

        public bool TryAuthorizeRecovery()
        {
            if (!IsActive) return true;
            if (ConsecutiveStableTicks >= MinStableTicksForRecovery)
            {
                RecoveryAuthorized = true;
                ActiveReason = DecomposedFallbackReason.None;
                return true;
            }
            return false;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(1);
            writer.Write((int)ActiveReason);
            writer.Write(FallbackTick);
            writer.Write(ConsecutiveStableTicks);
            writer.Write(RecoveryAuthorized);
            writer.Write(_incidents.Count);
            for (int i = 0; i < _incidents.Count; i++)
            {
                writer.Write(_incidents[i].Tick);
                writer.Write((int)_incidents[i].Reason);
                writer.Write(_incidents[i].DiagnosticValue);
            }
        }

        public static FallbackLedger Read(BinaryReader reader)
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unknown FallbackLedger version.");
            var ledger = new FallbackLedger
            {
                ActiveReason = (DecomposedFallbackReason)reader.ReadInt32(),
                FallbackTick = reader.ReadInt32(),
                ConsecutiveStableTicks = reader.ReadInt32(),
                RecoveryAuthorized = reader.ReadBoolean()
            };
            int count = reader.ReadInt32();
            if (count < 0) throw new InvalidDataException("Invalid incident count.");
            for (int i = 0; i < count; i++)
            {
                ledger._incidents.Add(new FallbackIncident
                {
                    Tick = reader.ReadInt32(),
                    Reason = (DecomposedFallbackReason)reader.ReadInt32(),
                    DiagnosticValue = reader.ReadDouble()
                });
            }
            return ledger;
        }
    }

    public sealed class DecomposedForceResidual
    {
        public const double MaxComponentTorque = 0.5;
        public const double MaxAggregateResidual = 0.5;
        public const double MaxSlewPerTick = 0.18;
        public const double MaxTotalActuatorTorque = 2.0;
        public const double PeakImpulseGuard = 0.72;
        public const double TrackingDeadband = 0.02;

        public double TrackingGain { get; private set; } = 0.35;
        public double AnticipationGain { get; private set; } = 0.65;
        public double LoadRegulationGain { get; private set; } = 0.25;
        public double EfficiencyGain { get; private set; } = 0.15;

        public double PreviousResidual { get; private set; }
        public double SignedPredictionError { get; private set; }
        public double PredictedTargetDeviation { get; private set; }
        public double ActualTargetDeviation { get; private set; }
        public int Updates { get; private set; }

        public FallbackLedger Ledger { get; private set; } = new FallbackLedger();
        public ResidualTelemetry LastTelemetry { get; private set; }

        private int _misses;
        private int _trackingFails;
        private int _reversals;

        public static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        public double ComputeCommand(
            ElbowLab.StepSample previous,
            double pdTorque,
            double targetAngle,
            double predictedImpulse,
            double contactProbability,
            double uncertainty,
            double desiredImpulse = 0.0)
        {
            // Separate sensor/simulation invalidity from adaptive controller divergence
            if (!IsFinite(previous.Feedback.Angle) || !IsFinite(previous.Feedback.Velocity) || !IsFinite(targetAngle))
            {
                Ledger.RecordFallback(previous.Tick, DecomposedFallbackReason.SensorInvalid, previous.Feedback.Angle);
                PreviousResidual = 0;
                LastTelemetry = default;
                return 0.0; // Emergency zero-torque: PD must not consume NaN/Inf
            }

            if (!IsFinite(pdTorque) || !IsFinite(predictedImpulse) || !IsFinite(uncertainty) ||
                !IsFinite(contactProbability) || predictedImpulse < 0 || uncertainty < 0)
            {
                Ledger.RecordFallback(previous.Tick, DecomposedFallbackReason.AdaptiveDivergence, predictedImpulse);
                PreviousResidual = 0;
                LastTelemetry = default;
                return Clamp(IsFinite(pdTorque) ? pdTorque : 0.0, -MaxTotalActuatorTorque, MaxTotalActuatorTorque);
            }

            if (Ledger.IsActive)
            {
                Ledger.ObserveStableTick(true, true);
                PreviousResidual = 0;
                LastTelemetry = new ResidualTelemetry
                {
                    TotalActuatorTorque = Clamp(pdTorque, -MaxTotalActuatorTorque, MaxTotalActuatorTorque)
                };
                return LastTelemetry.TotalActuatorTorque;
            }

            double angleError = targetAngle - previous.Feedback.Angle;
            double absError = Math.Abs(angleError);
            double direction = Math.Sign(angleError);
            double velocity = previous.Feedback.Velocity;
            double confidence = 1.0 / (1.0 + uncertainty / 0.12);

            // 1. TrackingAssist: bounded proportional assist outside deadband
            double rawTrack = absError > TrackingDeadband
                ? TrackingGain * Math.Sign(angleError) * Math.Min(1.0, absError / 0.5) * confidence
                : 0.0;
            double cTrack = Clamp(rawTrack, -MaxComponentTorque, MaxComponentTorque);

            // 2. ContactAnticipation: active when P(contact) > 0.5 and predicted impulse > 0
            double approach = Math.Max(0.0, direction * velocity);
            double rawAnticipate = (contactProbability > 0.5 && predictedImpulse > 0.0)
                ? -direction * AnticipationGain * Math.Min(0.8, predictedImpulse * 3.6) * confidence * Math.Min(1.0, approach / 1.2)
                : 0.0;
            double cAnticipate = Clamp(rawAnticipate, -MaxComponentTorque, MaxComponentTorque);

            // 3. LoadRegulation: modulates damping based on signed impulse prediction error
            double rawLoad = -LoadRegulationGain * Clamp(SignedPredictionError, -0.5, 0.5) * Math.Sign(velocity + 1e-6) * confidence;
            double cLoad = Clamp(rawLoad, -MaxComponentTorque, MaxComponentTorque);

            // 4. EfficiencyCoasting: reduces actuator effort when momentum is already aligned with target
            double rawEfficiency = (direction * velocity > 0.3 && absError < 0.25)
                ? -EfficiencyGain * Math.Sign(pdTorque) * Math.Min(1.0, Math.Abs(pdTorque) / MaxTotalActuatorTorque) * confidence
                : 0.0;
            double cEfficiency = Clamp(rawEfficiency, -MaxComponentTorque, MaxComponentTorque);

            double proposedAggregate = cTrack + cAnticipate + cLoad + cEfficiency;
            double clampedAggregate = Clamp(proposedAggregate, -MaxAggregateResidual, MaxAggregateResidual);
            double slewedAggregate = Clamp(clampedAggregate, PreviousResidual - MaxSlewPerTick, PreviousResidual + MaxSlewPerTick);

            bool reversal = Math.Abs(slewedAggregate) > 0.025 && Math.Abs(PreviousResidual) > 0.025 &&
                            Math.Sign(slewedAggregate) != Math.Sign(PreviousResidual);
            _reversals = reversal ? _reversals + 1 : Math.Max(0, _reversals - 1);

            PreviousResidual = slewedAggregate;
            double totalTorque = Clamp(pdTorque + slewedAggregate, -MaxTotalActuatorTorque, MaxTotalActuatorTorque);

            LastTelemetry = new ResidualTelemetry
            {
                ProposedTracking = cTrack,
                ProposedAnticipation = cAnticipate,
                ProposedLoadRegulation = cLoad,
                ProposedEfficiency = cEfficiency,
                ProposedAggregate = proposedAggregate,
                ClampedAggregate = clampedAggregate,
                SlewedAggregate = slewedAggregate,
                AppliedResidual = slewedAggregate,
                TotalActuatorTorque = totalTorque,
                RejectedAmount = Math.Abs(proposedAggregate - slewedAggregate),
                WasClamped = Math.Abs(proposedAggregate - clampedAggregate) > 1e-9,
                WasSlewed = Math.Abs(clampedAggregate - slewedAggregate) > 1e-9
            };

            return totalTorque;
        }

        public void ObserveOutcome(
            ElbowLab.StepSample observed,
            double targetAngle,
            double predictedImpulse,
            double uncertainty,
            double desiredImpulse = 0.0)
        {
            if (!IsFinite(observed.NormalImpulse) || !IsFinite(observed.Feedback.Angle) || !IsFinite(predictedImpulse))
            {
                Ledger.RecordFallback(observed.Tick, DecomposedFallbackReason.SensorInvalid, observed.NormalImpulse);
                PreviousResidual = 0;
                return;
            }

            // Explicit prediction error terminology
            SignedPredictionError = observed.NormalImpulse - predictedImpulse;
            PredictedTargetDeviation = predictedImpulse - desiredImpulse;
            ActualTargetDeviation = observed.NormalImpulse - desiredImpulse;

            if (Ledger.IsActive) return;

            _misses = Math.Abs(SignedPredictionError) > Math.Max(0.12, uncertainty) ? _misses + 1 : Math.Max(0, _misses - 1);
            double trackingError = Math.Abs(targetAngle - observed.Feedback.Angle);
            _trackingFails = observed.Tick > 8 && trackingError > 0.18 ? _trackingFails + 1 : 0;

            if (observed.NormalImpulse > PeakImpulseGuard)
                TriggerFallback(observed.Tick, DecomposedFallbackReason.PeakImpulse, observed.NormalImpulse);
            else if (_misses >= 3)
                TriggerFallback(observed.Tick, DecomposedFallbackReason.RepeatedPredictionMiss, _misses);
            else if (_trackingFails >= 5)
                TriggerFallback(observed.Tick, DecomposedFallbackReason.Tracking, trackingError);
            else if (_reversals >= 3)
                TriggerFallback(observed.Tick, DecomposedFallbackReason.AlternatingCorrection, _reversals);

            if (Ledger.IsActive) return;

            // Bounded adaptation updates
            double boundedErr = Clamp(SignedPredictionError, -0.25, 0.25);
            AnticipationGain = Clamp(AnticipationGain + 0.18 * boundedErr - 0.01 * Math.Min(1.0, trackingError), 0.0, 1.0);
            TrackingGain = Clamp(TrackingGain + 0.05 * Math.Min(1.0, trackingError) - 0.01 * Math.Abs(boundedErr), 0.0, 1.0);
            LoadRegulationGain = Clamp(LoadRegulationGain + 0.08 * Math.Abs(boundedErr), 0.0, 1.0);
            Updates++;
        }

        private void TriggerFallback(int tick, DecomposedFallbackReason reason, double diagnostic)
        {
            Ledger.RecordFallback(tick, reason, diagnostic);
            PreviousResidual = 0;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(1);
            writer.Write(TrackingGain);
            writer.Write(AnticipationGain);
            writer.Write(LoadRegulationGain);
            writer.Write(EfficiencyGain);
            writer.Write(PreviousResidual);
            writer.Write(SignedPredictionError);
            writer.Write(PredictedTargetDeviation);
            writer.Write(ActualTargetDeviation);
            writer.Write(Updates);
            writer.Write(_misses);
            writer.Write(_trackingFails);
            writer.Write(_reversals);
            Ledger.Write(writer);
        }

        public static DecomposedForceResidual Read(BinaryReader reader)
        {
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unknown DecomposedForceResidual version.");
            var r = new DecomposedForceResidual
            {
                TrackingGain = reader.ReadDouble(),
                AnticipationGain = reader.ReadDouble(),
                LoadRegulationGain = reader.ReadDouble(),
                EfficiencyGain = reader.ReadDouble(),
                PreviousResidual = reader.ReadDouble(),
                SignedPredictionError = reader.ReadDouble(),
                PredictedTargetDeviation = reader.ReadDouble(),
                ActualTargetDeviation = reader.ReadDouble(),
                Updates = reader.ReadInt32(),
                _misses = reader.ReadInt32(),
                _trackingFails = reader.ReadInt32(),
                _reversals = reader.ReadInt32(),
                Ledger = FallbackLedger.Read(reader)
            };
            return r;
        }
    }
}
