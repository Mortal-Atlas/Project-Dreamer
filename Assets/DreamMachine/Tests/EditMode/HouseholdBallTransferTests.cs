using System.IO;
using System.Numerics;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.Social;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class HouseholdBallTransferTests
    {
        static WorldSimulation Create()
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 37);
            Assert.That(world.EnableHouseholdLife(), Is.True);
            return world;
        }

        static readonly string Trace = new string('a', 64);

        [TestCase(HouseholdGripPolicy.VisualCoordination, VisualEstimateSource.Predicted, false)]
        [TestCase(HouseholdGripPolicy.VisualCoordination, VisualEstimateSource.GeometryObservation, true)]
        [TestCase(HouseholdGripPolicy.H9Passive, VisualEstimateSource.Predicted, false)]
        [TestCase(HouseholdGripPolicy.H9Reactive, VisualEstimateSource.GeometryObservation, true)]
        public void VisualTransferCommitsPhysicsAndOnlyTheActorsMeasuredSighting(HouseholdGripPolicy policy, VisualEstimateSource source, bool seen)
        {
            var world = Create();
            var home = world.Home;
            Assert.That(home.StartBallTransfer(2, policy), Is.True);
            var intent = home.BallIntent.Value;
            var lastSeen = home.ReadBallBelief(2).LastSeen;
            world.Tick();
            var actual = new HouseholdBallPose(intent.TargetX, intent.TargetTop + HouseholdLife.BallRadius, intent.TargetZ);
            var offset = new Vector3(.016f, 0f, .024f);
            var visual = new VisualPoseEstimate(offset, default, 1, 0, 7, .50f, 0f, source);
            var result = home.CompleteBallTransfer(actual, true, 0, "physical-lift-hold-place-release", Trace,
                PlacementObservation.StillAtDestination, visual);
            Assert.That(result.Seen, Is.EqualTo(seen));
            Assert.That(home.ReadBallPose(), Is.EqualTo(actual));
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(intent.X + (seen ? .016f : 0f)).Within(.002f));
            Assert.That(home.ReadBallBelief(2).LastSeen, Is.EqualTo(seen ? intent.Start + 1 : lastSeen));
            Assert.That(home.ReadBallBelief(3).LastSeen, Is.EqualTo(-1));
            var save = world.CaptureCheckpoint();
            Assert.That(WorldSimulation.RestoreCheckpoint(save).CaptureCheckpoint(), Is.EqualTo(save));
        }

        [Test]
        public void SettledNativeResultPersistsAndLeavesAnotherLearnersSightingStale()
        {
            var world = Create();
            var home = world.Home;
            var start = home.ReadBallPose();
            Assert.That(home.StartBallTransfer(2, HouseholdGripPolicy.Fixed), Is.True);
            Assert.That(home.StartBallTransfer(3, HouseholdGripPolicy.Fixed), Is.False);
            Assert.That(world.EnterPhysicalPractice(), Is.False);
            Assert.Throws<InvalidDataException>(() => world.CaptureCheckpoint());
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(start.X).Within(.002f));
            Assert.That(home.ReadBallBelief(3).LastSeen, Is.EqualTo(-1));
            world.Tick();
            var intent = home.BallIntent.Value;
            var onShelf = new HouseholdBallPose(intent.TargetX, intent.TargetTop + HouseholdLife.BallRadius, intent.TargetZ, 0, 1, 0, 0);
            var a = home.CompleteBallTransfer(onShelf, true, 0, "physical-lift-hold-place-release", Trace);
            Assert.That(home.ReadItem(HouseholdLife.BallItem).X, Is.EqualTo(onShelf.X));
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(onShelf.X).Within(.002f));
            Assert.That(home.StartBallTransfer(3, HouseholdGripPolicy.Light), Is.True);
            world.Tick();
            var offTarget = new HouseholdBallPose(onShelf.X + .08f, HouseholdLife.BallRadius, onShelf.Z + .035f);
            var b = home.CompleteBallTransfer(offTarget, false, 0, "hold-gate-failed", Trace);
            Assert.That(home.ReadBallPose(), Is.EqualTo(offTarget));
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(onShelf.X).Within(.002f));
            Assert.That(home.ReadBallBelief(3).X, Is.EqualTo(offTarget.X).Within(.002f));
            Assert.That(world.Find("son").State.Library.GetOrNull(a.MemoryId)?.Source, Is.EqualTo("firsthand-ball-transfer"));
            Assert.That(world.Find("son").State.Library.GetOrNull(b.MemoryId), Is.Null);
            Assert.That(world.Find("daughter").State.Library.GetOrNull(a.MemoryId), Is.Null);
            Assert.That(world.Find("daughter").State.Library.GetOrNull(b.MemoryId)?.Source, Is.EqualTo("firsthand-ball-transfer"));
            var bytes = world.CaptureCheckpoint();
            var restored = WorldSimulation.RestoreCheckpoint(bytes);
            Assert.That(restored.CaptureCheckpoint(), Is.EqualTo(bytes));
            Assert.That(restored.Home.ReadBallPose(), Is.EqualTo(offTarget));
            Assert.That(restored.Home.BallTransfer(0).After, Is.EqualTo(onShelf));
            Assert.That(restored.Home.BallTransfer(1).Intent.Before, Is.EqualTo(onShelf));
        }

        [Test]
        public void MissingObservationKeepsHistoricalPointUntilSearchSucceeds()
        {
            var world = Create();
            var home = world.Home;
            Assert.That(home.StartBallTransfer(2, HouseholdGripPolicy.Light), Is.True);
            world.Tick();
            var prior = home.ReadBallBelief(2);
            var unforeseen = new HouseholdBallPose(prior.X + .38f, HouseholdLife.BallRadius, prior.Z);
            home.CompleteBallTransfer(unforeseen, false, 0, "hold-gate-failed", Trace);
            Assert.That(home.ReadBallBelief(2).Uncertain, Is.True);
            Assert.That(home.ReadBallBelief(2).LastSeen, Is.EqualTo(prior.LastSeen));
            Assert.That(home.InspectBall(2, false), Is.False);
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(prior.X));
            Assert.That(home.InspectBall(2, true), Is.True);
            Assert.That(home.ReadBallBelief(2).X, Is.EqualTo(unforeseen.X).Within(.002f));
        }

        [Test]
        public void SettledTransferCheckpointAlsoRetainsExistingSphereAcquisition()
        {
            var world = Create();
            var home = world.Home;
            Assert.That(home.StartBallTransfer(3, HouseholdGripPolicy.Light), Is.True);
            world.Tick();
            var before = home.ReadBallPose();
            var transfer = home.CompleteBallTransfer(before, false, 0, "hold-gate-failed", Trace);
            Assert.That(world.EnterPhysicalPractice(), Is.True);
            Assert.That(world.StartGripAttempt(), Is.True);
            world.Tick();
            world.Tick();
            var grip = world.CompleteGripAttempt(2, 0, .01f, false, "hold-gate-failed", Trace, 0f, .028f, 0f);
            Assert.That(world.LeavePhysicalPractice(), Is.True);

            var restored = WorldSimulation.RestoreCheckpoint(world.CaptureCheckpoint());
            Assert.That(restored.PhysicalPractice.Cap, Is.GreaterThan(.18f));
            Assert.That(restored.PhysicalPractice.Cap, Is.EqualTo(world.PhysicalPractice.Cap));
            Assert.That(restored.Home.BallTransfer(0).Tape, Is.EqualTo(Trace));
            Assert.That(restored.Find("son").State.Library.GetOrNull(grip.MemoryId)?.Source, Is.EqualTo("firsthand-supported-grip"));
            Assert.That(restored.Find("daughter").State.Library.GetOrNull(transfer.MemoryId)?.Source, Is.EqualTo("firsthand-ball-transfer"));
        }
    }
}
