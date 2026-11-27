using System.Linq;
using DreamMachine.Sim;
using DreamMachine.Social;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class HouseholdRetentionTests
    {
        [Test]
        public void RejectedSocketDoesNotChangePersistentBoardAndCorrectChoiceAdvancesIt()
        {
            int state = 3;
            Assert.That(ShapeBoard.Place(ref state, 0), Is.False);
            Assert.That(state, Is.EqualTo(3));
            Assert.That(ShapeBoard.Place(ref state, 1), Is.True);
            Assert.That(state, Is.EqualTo(4));
        }

        [Test]
        public void ReplayOnlyUsesBoundedPlansAndCannotReadDaytimeEpisodes()
        {
            var none = HouseholdRetention.Probe(781, RetentionAccess.None);
            var replay = HouseholdRetention.Probe(781, RetentionAccess.Replay);
            Assert.That(none.All(t => t.Source < 0 && t.Decision == RetentionDecision.Exploration), Is.True);
            Assert.That(replay.Any(t => t.Decision == RetentionDecision.Exploration), Is.True);
            Assert.That(replay.All(t => t.Decision != RetentionDecision.Episode), Is.True);
            Assert.That(replay.Where(t => t.Source >= 0).Select(t => t.Cue).Distinct().Count(), Is.EqualTo(2));
            Assert.That(replay.Where(t => t.Decision == RetentionDecision.ReplayPlan).All(t => t.Success), Is.True);
        }

        [Test]
        public void DelayedStudyKeepsProbeInputsMatchedAndWithholdsTheNewLayout()
        {
            var trials = HouseholdDelayedRetention.Run(1209);
            Assert.That(trials.Count, Is.EqualTo(48));
            Assert.That(trials.All(t => t.ProbeTick - t.LastTrainingTick == HouseholdDelayedRetention.DelayTicks), Is.True);
            Assert.That(trials.Any(t => t.Transfer && t.ActualSlot != t.Cue), Is.True);
            foreach (var group in trials.GroupBy(t => (t.Cue, t.Transfer)))
                Assert.That(group.Select(t => t.ActualSlot).Distinct().Count(), Is.EqualTo(1));
            foreach (var group in trials.Where(t => t.Access == RetentionAccess.Replay && t.Source >= 0).GroupBy(t => t.Cue))
                Assert.That(group.Select(t => t.Choice).Distinct().Count(), Is.EqualTo(1));
            Assert.That(trials.Where(t => t.Access == RetentionAccess.Replay && t.Source >= 0).Select(t => t.Cue).Distinct().Count(),
                Is.LessThanOrEqualTo(2));
            Assert.That(trials.Where(t => t.Access == RetentionAccess.None).All(t => t.Source < 0), Is.True);
        }

        [Test]
        public void ReconstructedCompetenceReferencesOnlyActualIndependentOutcomes()
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Laptop, 37);
            Assert.That(world.EnableHouseholdLife(), Is.True);
            world.RunTicks(4700);
            var replay = WorldSimulation.RestoreCheckpoint(world.CaptureCheckpoint());
            Assert.That(replay.Home.ReadItem(HouseholdLife.SortingBoard).Revision, Is.Positive);
            Assert.That(replay.Home.ReadItem(HouseholdLife.ToyBin).Revision, Is.Positive);
            for (int actor = 2; actor < 4; actor++)
            for (int item = 0; item < HouseholdLife.ItemCount; item++)
            {
                var measure = replay.Home.ReadCompetence(actor, item);
                Assert.That(measure.IndependentSuccesses, Is.LessThanOrEqualTo(measure.IndependentAttempts));
                Assert.That(measure.IndependentSuccesses, Is.EqualTo(world.Home.ReadCompetence(actor, item).IndependentSuccesses));
                if (measure.LastIndependentSuccess >= 0)
                    Assert.That(replay.Home.Experience(measure.LastIndependentSuccess).Evidence, Is.EqualTo(HomeEvidence.Firsthand));
            }
            world.RunTicks(500);
            replay.RunTicks(500);
            Assert.That(replay.CaptureCheckpoint(), Is.EqualTo(world.CaptureCheckpoint()));
        }
    }
}
