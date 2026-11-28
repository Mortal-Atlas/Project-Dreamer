using System.Numerics;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class PlacementCoordinationTests
    {
        [Test]
        public void TrajectoryAcceleratesBrakesAndStopsAtTheRequestedPoint()
        {
            var control = new PlacementCoordination();
            control.Begin(Vector3.Zero);
            Vector3 goal = new Vector3(.3f, .12f, -.24f);
            control.Command(goal, .17f, .083f, .05f);
            Assert.That(control.Velocity.X, Is.EqualTo(.04f).Within(.00001f));
            for (int i = 0; i < 135; i++) control.Command(goal, .17f, .083f, .05f);
            Assert.That(Vector3.Distance(control.Position, goal), Is.LessThan(.00001f));
            Assert.That(control.Velocity.Length(), Is.LessThan(.00001f));
        }

        [Test]
        public void ImpactOcclusionAndExistingMotionCannotAuthorizeOpening()
        {
            var control = new PlacementCoordination();
            var steady = new PlacementSense(true, true, true, false, 1f, .01f, .1f, .01f, .1f);
            for (int i = 0; i < 4; i++) control.Observe(steady);
            control.Observe(new PlacementSense(true, true, true, false, 4f, .01f, .1f, .01f, .1f));
            Assert.That(control.Ready, Is.False);
            for (int i = 0; i < 5; i++) control.Observe(steady);
            Assert.That(control.Ready, Is.True);
            control.Observe(new PlacementSense(false, true, true, false, 1f, .01f, .1f, .01f, .1f));
            Assert.That(control.Ready, Is.False);
            control.Observe(new PlacementSense(true, true, true, false, 1f, .12f, .7f, .06f, .4f));
            Assert.That(control.StableTicks, Is.Zero);
        }

        [Test]
        public void UnweightingPrecedesFullDigitOpening()
        {
            Assert.That(PlacementCoordination.Opening(0), Is.Zero);
            Assert.That(PlacementCoordination.ReleaseCap(3.2f, 1), Is.GreaterThan(1f));
            Assert.That(PlacementCoordination.ReleaseCap(3.2f, PlacementCoordination.UnweightTicks), Is.EqualTo(.12f).Within(.00001f));
            Assert.That(PlacementCoordination.Opening(PlacementCoordination.UnweightTicks), Is.Zero);
            Assert.That(PlacementCoordination.Opening(PlacementCoordination.UnweightTicks + PlacementCoordination.OpenTicks), Is.EqualTo(1f));
        }
    }
}
