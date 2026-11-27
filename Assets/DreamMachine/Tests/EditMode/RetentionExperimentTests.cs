using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class RetentionExperimentTests
    {
        [Test]
        public void RunABA_CompletesAllFiveModes_WithValidMetrics()
        {
            var configA = new RetentionTrialConfig
            {
                TargetAngle = 1.0,
                WallAngle = 1.15,
                WallStiffness = 25.0,
                Damping = 0.4,
                TicksPerTrial = 40
            };
            var configB = new RetentionTrialConfig
            {
                TargetAngle = 0.6,
                WallAngle = 0.7,
                WallStiffness = 45.0,
                Damping = 0.8,
                TicksPerTrial = 40
            };

            foreach (RetentionMode mode in System.Enum.GetValues(typeof(RetentionMode)))
            {
                var report = RetentionExperiment.RunABA(configA, configB, 5, mode);
                Assert.AreEqual(mode, report.Mode);
                Assert.IsFalse(double.IsNaN(report.SignedInterference));
                Assert.IsFalse(double.IsNaN(report.PhaseA1FinalScore));
                Assert.IsFalse(double.IsNaN(report.PhaseA2FinalScore));
            }
        }
    }
}
