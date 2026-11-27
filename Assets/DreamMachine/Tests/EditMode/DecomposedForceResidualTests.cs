using System.IO;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class DecomposedForceResidualTests
    {
        private static ElbowLab.StepSample MakeSample(int tick, double angle, double velocity, double impulse = 0)
            => new ElbowLab.StepSample(tick, 1.0, angle, impulse, 0, 0, new ElbowLab.Proprioception(angle, velocity, 0, 0));

        [Test]
        public void AggregateResidual_NeverExceedsHalfNewtonMeter_AndSlewRate()
        {
            var residual = new DecomposedForceResidual();
            var prev = MakeSample(1, 0.0, 2.0);

            double total = residual.ComputeCommand(prev, 1.0, 1.0, 0.8, 0.9, 0.05);
            Assert.LessOrEqual(System.Math.Abs(residual.LastTelemetry.AppliedResidual), DecomposedForceResidual.MaxAggregateResidual + 1e-9);
            Assert.LessOrEqual(System.Math.Abs(residual.LastTelemetry.AppliedResidual), DecomposedForceResidual.MaxSlewPerTick + 1e-9);
            Assert.LessOrEqual(System.Math.Abs(total), DecomposedForceResidual.MaxTotalActuatorTorque + 1e-9);
        }

        [Test]
        public void SensorNaN_TriggersSensorInvalidFallback_AndZeroTorque()
        {
            var residual = new DecomposedForceResidual();
            var prev = MakeSample(5, double.NaN, 0.0);

            double total = residual.ComputeCommand(prev, 1.2, 1.0, 0.1, 0.2, 0.05);
            Assert.AreEqual(0.0, total, 1e-9);
            Assert.AreEqual(DecomposedFallbackReason.SensorInvalid, residual.Ledger.ActiveReason);
            Assert.AreEqual(1, residual.Ledger.Incidents.Count);
        }

        [Test]
        public void AdaptiveNaN_TriggersAdaptiveDivergence_AndPreservesClampedPD()
        {
            var residual = new DecomposedForceResidual();
            var prev = MakeSample(6, 0.2, 0.1);

            double total = residual.ComputeCommand(prev, 1.5, 1.0, double.NaN, 0.2, 0.05);
            Assert.AreEqual(1.5, total, 1e-9);
            Assert.AreEqual(DecomposedFallbackReason.AdaptiveDivergence, residual.Ledger.ActiveReason);
        }

        [Test]
        public void Recovery_RequiresSixStableTicks_AndExplicitAuthorization()
        {
            var residual = new DecomposedForceResidual();
            var bad = MakeSample(1, double.NaN, 0.0);
            residual.ComputeCommand(bad, 1.0, 1.0, 0.1, 0.1, 0.05);
            Assert.IsTrue(residual.Ledger.IsActive);

            var good = MakeSample(2, 0.1, 0.1);
            for (int i = 0; i < 5; i++)
            {
                residual.ComputeCommand(good, 0.5, 1.0, 0.1, 0.1, 0.05);
                Assert.IsFalse(residual.Ledger.TryAuthorizeRecovery());
            }

            residual.ComputeCommand(good, 0.5, 1.0, 0.1, 0.1, 0.05);
            Assert.IsTrue(residual.Ledger.TryAuthorizeRecovery());
            Assert.IsFalse(residual.Ledger.IsActive);
        }

        [Test]
        public void BinarySerialization_PreservesStateAndFallbackLedger()
        {
            var residual = new DecomposedForceResidual();
            var bad = MakeSample(3, double.PositiveInfinity, 0.0);
            residual.ComputeCommand(bad, 1.0, 1.0, 0.1, 0.1, 0.05);

            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                residual.Write(bw);

            ms.Position = 0;
            using var br = new BinaryReader(ms);
            var restored = DecomposedForceResidual.Read(br);

            Assert.AreEqual(DecomposedFallbackReason.SensorInvalid, restored.Ledger.ActiveReason);
            Assert.AreEqual(1, restored.Ledger.Incidents.Count);
            Assert.AreEqual(3, restored.Ledger.Incidents[0].Tick);
        }
    }
}
