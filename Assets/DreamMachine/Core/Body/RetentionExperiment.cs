using System;
using System.Collections.Generic;

namespace DreamMachine.Body
{
    public enum RetentionMode
    {
        EngineeredBaseline = 0,
        FrozenLearned = 1,
        ContinuedAdaptation = 2,
        ResetAdaptive = 3,
        RetainedAState = 4
    }

    public struct RetentionTrialConfig
    {
        public double TargetAngle;
        public double WallAngle;
        public double WallStiffness;
        public double Damping;
        public int TicksPerTrial;
    }

    public struct RetentionTrialMetrics
    {
        public int TrialIndex;
        public double FinalTrackingError;
        public double PeakImpulse;
        public double CumulativeImpulse;
        public double CommandedMechanicalWork;
        public bool TaskCompleted;
        public double CompositeScore; // Higher is better
    }

    public struct ABARetentionReport
    {
        public RetentionMode Mode;
        public double AcquisitionSpeed;
        public double PhaseA1FinalScore;
        public double PhaseBFinalScore;
        public double PhaseA2InitialScore;
        public double PhaseA2FinalScore;
        public double SignedInterference; // PhaseA2InitialScore - PhaseA1FinalScore
        public double RetainedPerformance; // PhaseA2InitialScore / max(1e-6, PhaseA1FinalScore)
        public double RelearningSpeed;
        public bool PositiveTransfer;
        public bool NegativeTransfer;
    }

    public static class RetentionExperiment
    {
        public static RetentionTrialMetrics RunSingleTrial(
            int trialIndex,
            RetentionTrialConfig config,
            DecomposedForceResidual controller,
            RetentionMode mode)
        {
            double angle = 0.0;
            double velocity = 0.0;
            double peakImpulse = 0.0;
            double cumulativeImpulse = 0.0;
            double mechanicalWork = 0.0;
            const double dt = 0.05;

            for (int tick = 1; tick <= config.TicksPerTrial; tick++)
            {
                double error = config.TargetAngle - angle;
                double pdTorque = DecomposedForceResidual.Clamp(3.0 * error - 0.6 * velocity, -2.0, 2.0);

                var prevSample = new ElbowLab.StepSample(
                    tick - 1, config.TargetAngle, angle, 0, 0, 0,
                    new ElbowLab.Proprioception(angle, velocity, 0, 0));

                double predictedImpulse = angle >= config.WallAngle - 0.1 ? Math.Max(0, velocity * 0.15) : 0.0;
                double contactProb = angle >= config.WallAngle - 0.15 ? 0.85 : 0.1;
                double uncertainty = 0.08;

                double totalTorque = mode == RetentionMode.EngineeredBaseline
                    ? pdTorque
                    : controller.ComputeCommand(prevSample, pdTorque, config.TargetAngle, predictedImpulse, contactProb, uncertainty);

                // Simple 1-DOF elbow physics with wall contact
                double accel = totalTorque - config.Damping * velocity;
                velocity += accel * dt;
                angle += velocity * dt;

                double normalImpulse = 0.0;
                if (angle > config.WallAngle)
                {
                    double penetration = angle - config.WallAngle;
                    normalImpulse = Math.Max(0.0, penetration * config.WallStiffness + Math.Max(0.0, velocity) * 0.2);
                    angle = config.WallAngle;
                    if (velocity > 0) velocity = -0.2 * velocity;
                }

                peakImpulse = Math.Max(peakImpulse, normalImpulse);
                cumulativeImpulse += normalImpulse;
                mechanicalWork += Math.Abs(totalTorque * velocity * dt);

                if (mode == RetentionMode.ContinuedAdaptation || mode == RetentionMode.RetainedAState)
                {
                    var obsSample = new ElbowLab.StepSample(
                        tick, config.TargetAngle, angle, normalImpulse, 0, 0,
                        new ElbowLab.Proprioception(angle, velocity, 0, 0));
                    controller.ObserveOutcome(obsSample, config.TargetAngle, predictedImpulse, uncertainty);
                }
            }

            double finalErr = Math.Abs(config.TargetAngle - angle);
            bool completed = finalErr < 0.15;
            double score = (completed ? 1.0 : 0.0) - finalErr - 0.5 * peakImpulse - 0.05 * mechanicalWork;

            return new RetentionTrialMetrics
            {
                TrialIndex = trialIndex,
                FinalTrackingError = finalErr,
                PeakImpulse = peakImpulse,
                CumulativeImpulse = cumulativeImpulse,
                CommandedMechanicalWork = mechanicalWork,
                TaskCompleted = completed,
                CompositeScore = score
            };
        }

        public static ABARetentionReport RunABA(
            RetentionTrialConfig configA,
            RetentionTrialConfig configB,
            int trialsPerPhase,
            RetentionMode mode)
        {
            var controller = new DecomposedForceResidual();
            var a1 = new List<RetentionTrialMetrics>(trialsPerPhase);
            var b = new List<RetentionTrialMetrics>(trialsPerPhase);
            var a2 = new List<RetentionTrialMetrics>(trialsPerPhase);

            for (int i = 0; i < trialsPerPhase; i++)
                a1.Add(RunSingleTrial(i, configA, controller, mode));

            if (mode == RetentionMode.ResetAdaptive)
                controller = new DecomposedForceResidual();

            for (int i = 0; i < trialsPerPhase; i++)
                b.Add(RunSingleTrial(i, configB, controller, mode));

            if (mode == RetentionMode.ResetAdaptive)
                controller = new DecomposedForceResidual();

            for (int i = 0; i < trialsPerPhase; i++)
                a2.Add(RunSingleTrial(i, configA, controller, mode));

            double a1Final = a1[a1.Count - 1].CompositeScore;
            double bFinal = b[b.Count - 1].CompositeScore;
            double a2Init = a2[0].CompositeScore;
            double a2Final = a2[a2.Count - 1].CompositeScore;
            double signedInterference = a2Init - a1Final;

            return new ABARetentionReport
            {
                Mode = mode,
                AcquisitionSpeed = (a1Final - a1[0].CompositeScore) / Math.Max(1, trialsPerPhase - 1),
                PhaseA1FinalScore = a1Final,
                PhaseBFinalScore = bFinal,
                PhaseA2InitialScore = a2Init,
                PhaseA2FinalScore = a2Final,
                SignedInterference = signedInterference,
                RetainedPerformance = a2Init / Math.Max(1e-6, Math.Abs(a1Final)),
                RelearningSpeed = (a2Final - a2Init) / Math.Max(1, trialsPerPhase - 1),
                PositiveTransfer = signedInterference > 0.01,
                NegativeTransfer = signedInterference < -0.01
            };
        }
    }
}
