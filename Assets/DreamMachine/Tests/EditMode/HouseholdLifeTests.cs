using System.Linq;
using DreamMachine.Sim;
using DreamMachine.Social;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class HouseholdLifeTests
    {
        static WorldSimulation Create()
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Laptop, 37);
            Assert.That(world.EnableHouseholdLife(), Is.True);
            return world;
        }

        [Test]
        public void MultidayHouseholdChangesObjectsAndRecordsDistinctExperience()
        {
            var world = Create();
            world.RunTicks(4200);
            Assert.That(world.DayIndex, Is.GreaterThanOrEqualTo(4));
            var events = Enumerable.Range(0, world.Home.ExperienceCount).Select(world.Home.Experience).ToArray();
            foreach (var activity in new[] { HomeActivity.Inspect, HomeActivity.Roll, HomeActivity.Stack, HomeActivity.Open })
                Assert.That(events.Any(e => e.Activity == activity && e.Evidence != HomeEvidence.Failed), Is.True, activity.ToString());
            Assert.That(world.Home.ReadItem(1).Revision, Is.Positive);
            Assert.That(world.Home.ReadItem(2).Revision, Is.Positive);
            Assert.That(world.Home.ReadItem(3).Revision, Is.Positive);
            Assert.That(events.Any(e => e.Evidence == HomeEvidence.Imitation), Is.True, "Imitation needs an actual prior observation");
            Assert.That(events.Any(e => e.Evidence == HomeEvidence.Replay), Is.True);
            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (e.Evidence == HomeEvidence.Witnessed) Assert.That(events[e.Source].Evidence, Is.EqualTo(HomeEvidence.Demonstration));
                if (e.Evidence == HomeEvidence.Imitation) Assert.That(events[e.Source].Evidence, Is.EqualTo(HomeEvidence.Witnessed));
                if (e.Evidence == HomeEvidence.Replay) { Assert.That(e.Changed, Is.False); Assert.That(events[e.Source].Actor, Is.EqualTo(e.Actor)); }
            }
        }

        [Test]
        public void ObserverReadsAndSaveRestoreAreDeterministicAcrossActivityBoundaries()
        {
            var control = Create();
            control.RunTicks(1733);
            byte[] original = control.CaptureCheckpoint();
            var observed = WorldSimulation.RestoreCheckpoint(original);
            Assert.That(observed.CaptureCheckpoint(), Is.EqualTo(original));
            for (int i = 0; i < 600; i++)
            {
                observed.Tick();
                foreach (string id in new[] { "father", "mother", "son", "daughter" }) observed.Home.ReadPerson(observed.Home.FindPerson(id));
                observed.Home.ReadItem(i % HouseholdLife.ItemCount);
            }
            control.RunTicks(600);
            Assert.That(observed.CaptureCheckpoint(), Is.EqualTo(control.CaptureCheckpoint()));
        }

        [Test]
        public void LegacySaveCanEnableLivingWithoutResettingLearners()
        {
            var original = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 18);
            original.RunTicks(500);
            var restored = WorldSimulation.RestoreCheckpoint(original.CaptureCheckpoint());
            var child = restored.Find("son");
            string data = child.State.ToJson();
            Assert.That(restored.Home, Is.Null);
            Assert.That(restored.EnableHouseholdLife(), Is.True);
            Assert.That(restored.Find("son"), Is.SameAs(child));
            Assert.That(child.State.ToJson(), Is.EqualTo(data));
            Assert.That(child.X, Is.EqualTo(original.Find("son").X));
        }
    }
}
