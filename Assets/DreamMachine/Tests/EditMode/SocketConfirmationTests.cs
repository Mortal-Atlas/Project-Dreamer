using DreamMachine.Sim;
using NUnit.Framework;
using System;

namespace DreamMachine.Tests
{
    public sealed class SocketConfirmationTests
    {
        [Test]
        public void FamiliarCoarseRimCanAvoidDetailedInspection()
        {
            var result = SocketInspection.ChooseConfirmed(SocketFeatures.Shape(0), 1, 0, 1, -1, 0,
                _ => throw new InvalidOperationException("Detailed inspection was unnecessary."),
                _ => new SocketFeatures(1, 2), out bool glanced);
            Assert.That(glanced, Is.True);
            Assert.That(result.Choice, Is.Zero);
            Assert.That(result.Observations, Is.Zero);
        }

        [Test]
        public void MatchingMarkerCannotOverrideAnObservedRimContradiction()
        {
            var result = SocketInspection.ChooseConfirmed(SocketFeatures.Shape(0), 1, 0, 1, -1, 0,
                slot => SocketFeatures.Shape((slot + 1) % 3), _ => new SocketFeatures(1, 6), out bool glanced);
            Assert.That(glanced, Is.True);
            Assert.That(result.Choice, Is.EqualTo(2));
            Assert.That(result.Contradiction, Is.True);
            Assert.That(result.Observations, Is.EqualTo(3));
        }
    }
}
