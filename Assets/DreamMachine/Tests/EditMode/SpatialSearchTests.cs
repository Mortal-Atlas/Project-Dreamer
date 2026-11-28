using System.Linq;
using DreamMachine.Sim;
using DreamMachine.Social;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class SpatialSearchTests
    {
        internal static WorldSimulation Create(SpatialScenario scenario, int seed, int origin = 0)
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, seed);
            Assert.That(world.EnableHouseholdLife(), Is.True);
            world.Home.ConfigureSpatialScenario(scenario, origin);
            return world;
        }

        [TestCase(SpatialScenario.Relocated, 3367, 0, SpatialStage.Found)]
        [TestCase(SpatialScenario.Familiar, 5933, 1, SpatialStage.Found)]
        [TestCase(SpatialScenario.Delayed, 6503, 2, SpatialStage.Found)]
        [TestCase(SpatialScenario.MisleadingCue, 9431, 0, SpatialStage.Found)]
        [TestCase(SpatialScenario.SimilarToy, 12157, 1, SpatialStage.Found)]
        [TestCase(SpatialScenario.Unavailable, 13339, 2, SpatialStage.Exhausted)]
        public void SearchUsesReachedEvidenceAndStableIdentity(SpatialScenario scenario, int seed, int origin, SpatialStage end)
        {
            var world = Create(scenario, seed, origin);
            var home = world.Home;
            world.RunTicks(3600);
            var search = home.ReadSpatialSearch();
            Assert.That(search.Stage, Is.EqualTo(end));
            Assert.That(home.Experience(search.Sighting).Target, Is.EqualTo(HouseholdLife.FirstSearchBox + origin));
            Assert.That(home.Experience(search.Sighting).Details, Is.EqualTo(HouseholdLife.TargetToy));
            var episodes = Enumerable.Range(0, home.ExperienceCount).Select(home.Experience).ToArray();
            var plans = episodes.Where(e => e.Evidence == HomeEvidence.SearchCandidate).ToArray();
            Assert.That(plans.Length, Is.InRange(1, HouseholdLife.SearchBoxCount));
            Assert.That(plans.First().Target, Is.EqualTo(HouseholdLife.FirstSearchBox + origin));
            Assert.That(plans.First().Source, Is.EqualTo(search.Sighting));
            Assert.That(plans.Select(e => e.Target).Distinct().Count(), Is.EqualTo(plans.Length));
            if (scenario != SpatialScenario.Familiar)
            {
                var absent = episodes.First(e => e.Evidence == HomeEvidence.ToyAbsent);
                Assert.That(absent.Target, Is.EqualTo(plans[0].Target));
                Assert.That(absent.Details, Is.Zero);
                Assert.That(home.SpatialEnvironmentChangeTick, Is.InRange(home.Experience(search.Sighting).Tick + 1, plans[0].Tick));
                Assert.That(plans[1].Tick, Is.GreaterThan(absent.Tick));
            }
            if (scenario == SpatialScenario.SimilarToy)
                Assert.That(episodes.Any(e => e.Evidence == HomeEvidence.ToyAbsent && e.Details == HouseholdLife.SimilarToy), Is.True);
            if (scenario == SpatialScenario.MisleadingCue)
                Assert.That(plans[1].Details >> 3, Is.EqualTo((int)SpatialSelection.VisibleLabel));
            if (scenario == SpatialScenario.Delayed)
                Assert.That(episodes.Any(e => e.Actor == HouseholdLife.SearchActor && e.Activity != HomeActivity.FindToy &&
                    e.Tick > home.Experience(search.Sighting).Tick && e.Tick < plans[0].Tick), Is.True);
            if (end == SpatialStage.Found)
            {
                Assert.That(home.ReadSearchBox(search.Observed).VisibleToy, Is.EqualTo(HouseholdLife.TargetToy));
                Assert.That(home.Experience(search.LastObservation).Evidence, Is.EqualTo(HomeEvidence.ToyFound));
            }
            else
            {
                Assert.That(home.IsToyOutsideSearchArea, Is.True);
                Assert.That(search.Observed, Is.EqualTo(-1));
                Assert.That(search.Expected, Is.EqualTo(-1));
                Assert.That(home.ReadSupportedToyLocation().Box, Is.EqualTo(origin));
            }
        }

        [Test]
        public void MidSearchRoundTripAndObserverReadsPreserveTheExactContinuation()
        {
            var world = Create(SpatialScenario.SimilarToy, 7669);
            var home = world.Home;
            while (world.TickCount < 3000)
            {
                world.Tick();
                var search = home.ReadSpatialSearch();
                if (search.Negative != 0 && home.ReadPerson(HouseholdLife.SearchActor).Phase == HomePhase.Approach) break;
            }
            var active = home.ReadSpatialSearch();
            Assert.That(active.Stage, Is.EqualTo(SpatialStage.Searching));
            Assert.That(active.Expected, Is.EqualTo(-1));
            Assert.That(active.Observed, Is.EqualTo(-1));
            Assert.That(active.Remaining, Is.EqualTo(HouseholdLife.SearchBoxCount - 1));
            var checkpoint = world.CaptureCheckpoint();
            var restored = WorldSimulation.RestoreCheckpoint(checkpoint);
            Assert.That(restored.CaptureCheckpoint(), Is.EqualTo(checkpoint));
            for (int tick = 0; tick < 1000; tick++)
            {
                if (tick % 13 == 0)
                {
                    restored.Home.ReadSpatialSearch();
                    restored.Home.ReadSupportedToyLocation();
                    for (int box = 0; box < HouseholdLife.SearchBoxCount; box++) restored.Home.ReadSearchBox(box);
                }
                restored.Tick();
            }
            world.RunTicks(1000);
            Assert.That(restored.CaptureCheckpoint(), Is.EqualTo(world.CaptureCheckpoint()));
            Assert.That(restored.Home.ReadSpatialSearch().Stage, Is.EqualTo(SpatialStage.Found));
        }

        [Test]
        public void FirstDecisionCannotDistinguishClosedFamiliarAndRelocatedWorlds()
        {
            var familiar = Create(SpatialScenario.Familiar, 8069, 1);
            var changed = Create(SpatialScenario.Relocated, 8069, 1);
            while (familiar.TickCount < 3000 && familiar.Home.ReadSpatialSearch().Stage != SpatialStage.Searching)
            {
                familiar.Tick();
                changed.Tick();
            }
            var a = familiar.Home.ReadSpatialSearch();
            var b = changed.Home.ReadSpatialSearch();
            Assert.That(a.Stage, Is.EqualTo(SpatialStage.Searching));
            Assert.That(b.Selected, Is.EqualTo(a.Selected));
            Assert.That(b.Expected, Is.EqualTo(a.Expected));
            Assert.That(b.Candidates, Is.EqualTo(a.Candidates));
            Assert.That(b.Observed, Is.EqualTo(-1));
            for (int box = 0; box < HouseholdLife.SearchBoxCount; box++)
                Assert.That(changed.Home.ReadSearchBox(box).VisibleToy, Is.Zero);
        }
    }
}
