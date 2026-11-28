using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class ContactGatedGripTests
    {
        static readonly GripContactSense Quiet = new GripContactSense(.4f, .045f, .004f, false);
        static readonly GripContactSense Fast = new GripContactSense(.35f, .045f, .10f, true);

        [Test]
        public void LiftAdmissionRequiresSustainedOpposingPadContact()
        {
            var grip = new ContactGatedGrip(.74f);
            grip.Observe(Quiet, Quiet, lifting: false);
            grip.Observe(Quiet, Quiet, lifting: false);
            Assert.That(grip.Ready, Is.False);
            grip.Observe(Quiet, default, lifting: false);
            Assert.That(grip.Cap, Is.EqualTo(.74f));
            for (int i = 0; i < 3; i++) grip.Observe(Quiet, Quiet, lifting: false);
            Assert.That(grip.Ready, Is.True);
            Assert.That(grip.Corrections, Is.Zero);
        }

        [Test]
        public void FeedbackUsesMeasuredSlipAndKeepsTheEngineeredCap()
        {
            var grip = new ContactGatedGrip(.74f);
            grip.Observe(Quiet, Fast, lifting: false);
            Assert.That(grip.Uncertain, Is.True);
            Assert.That(grip.Cap, Is.EqualTo(.98f).Within(.0001f));
            for (int i = 0; i < 20; i++) grip.Observe(Quiet, default, lifting: true);
            Assert.That(grip.Cap, Is.EqualTo(GripAcquisition.ReferenceCap));
            Assert.That(grip.Corrections, Is.EqualTo(7));
            var nextAttempt = new ContactGatedGrip(.74f);
            Assert.That(nextAttempt.Cap, Is.EqualTo(.74f));
            Assert.That(nextAttempt.Corrections, Is.Zero);
        }
    }
}
