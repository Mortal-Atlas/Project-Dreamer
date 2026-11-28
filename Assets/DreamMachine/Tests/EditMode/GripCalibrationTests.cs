using System.IO;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class GripCalibrationTests
    {
        [Test]
        public void KnownImpulseProducesWindowMeanForce()
        {
            Assert.That(GripCalibration.MeanForce(.04f, .05f), Is.EqualTo(.8f).Within(1e-6f));
            Assert.That(GripCalibration.Slipping(.8f, .03f, 3), Is.True);
            Assert.That(GripCalibration.Slipping(.8f, .03f, 2), Is.False);
            Assert.That(GripCalibration.Slipping(0f, .2f, 10), Is.False);
        }

        [Test]
        public void GripUpdatesOnlyFromFailedPracticeWithMeasuredSlip()
        {
            var policy = new GripAcquisition("son", 5329);
            policy.Observe(0, 0, 0f, false);
            Assert.That(policy.Cap, Is.EqualTo(GripAcquisition.InitialCap));
            policy.Observe(4, 0, .015f, false);
            float adapted = policy.Cap;
            Assert.That(adapted, Is.GreaterThan(GripAcquisition.InitialCap));
            policy.Observe(10, 12, .05f, true);
            Assert.That(policy.Cap, Is.EqualTo(adapted));
            for (int i = 0; i < 30; i++) policy.Observe(3, 0, .015f, false);
            Assert.That(policy.Cap, Is.EqualTo(GripAcquisition.ReferenceCap));
        }

        [Test]
        public void GripRestoresCalibrationIdentityAndEvidence()
        {
            var policy = new GripAcquisition("son", 5329);
            policy.Observe(6, 0, 0f, false);
            policy.Observe(3, 2, .04f, false);
            var bytes = policy.Capture();
            var restored = GripAcquisition.Restore(bytes, "son", 5329);
            Assert.That(restored.Capture(), Is.EqualTo(bytes));
            Assert.That(restored.History[1].Before, Is.EqualTo(restored.History[0].After));
            Assert.That(restored.History[1].Attempt, Is.EqualTo(2));
            Assert.Throws<InvalidDataException>(() => GripAcquisition.Restore(bytes, "daughter", 5329));
            Assert.Throws<InvalidDataException>(() => GripAcquisition.Restore(bytes, "son", 988));
        }
    }
}
