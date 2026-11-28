using System.Numerics;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class SeatedPostureTests
    {
        [Test]
        public void PassiveModesDoNotApplyActiveCompensation()
        {
            Assert.That(SeatedPosture.Command(SeatedSupportMode.Passive, Vector3.UnitX, Vector3.UnitY,
                Vector3.UnitZ, 8f), Is.EqualTo(Vector3.Zero));
            Assert.That(SeatedPosture.Command(SeatedSupportMode.Reactive, Vector3.Zero, Vector3.Zero,
                Vector3.UnitZ, 8f), Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void AnticipationOpposesExpectedGravityAndRespectsActuatorLimit()
        {
            var expected = SeatedPosture.Command(SeatedSupportMode.Anticipatory,
                Vector3.Zero, Vector3.Zero, new Vector3(.5f, 0f, 0f), 4f);
            Assert.That(expected, Is.EqualTo(2f * Vector3.UnitZ));
            var bounded = SeatedPosture.Command(SeatedSupportMode.Anticipatory, Vector3.UnitX,
                -Vector3.UnitX, Vector3.UnitZ, 100f);
            Assert.That(bounded.Length(), Is.EqualTo(12f).Within(.00001f));
        }
    }
}
