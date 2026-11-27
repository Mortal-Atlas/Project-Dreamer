using System;
using System.IO;
using System.Linq;
using DreamMachine.Sim;
using DreamMachine.Social;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class HouseholdGripTests
    {
        [Test]
        public void PracticeLivesOnTheSameLearnerAndRoundTripsAtASettledTick()
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 7);
            world.EnableHouseholdLife();
            var arthur = world.Find("son");
            arthur.State.Library.NoteIdea("prior", "old observation", .2f, 0, "test");
            Assert.That(world.EnterPhysicalPractice(), Is.True);
            var location = world.Home.ReadPerson(HouseholdLife.SearchActor);
            Assert.That(world.StartGripAttempt(), Is.True);
            Assert.Throws<InvalidDataException>(() => world.CaptureCheckpoint());
            Assert.That(world.LeavePhysicalPractice(), Is.False);
            world.RunTicks(30);
            Assert.That(world.Home.ReadPerson(HouseholdLife.SearchActor).X, Is.EqualTo(location.X));
            Assert.That(world.Find("son"), Is.SameAs(arthur));
            var record = world.CompleteGripAttempt(6, 0, 0, false, "hold-gate-failed", new string('a', 64), 0, .028f, 0);
            Assert.That(record.End, Is.EqualTo(world.TickCount));
            Assert.That(world.PhysicalPractice.Cap, Is.GreaterThan(.18f));
            var bytes = world.CaptureCheckpoint();
            var restored = WorldSimulation.RestoreCheckpoint(bytes);
            Assert.That(restored.CaptureCheckpoint(), Is.EqualTo(bytes));
            Assert.That(restored.PhysicalPractice.Active, Is.True);
            Assert.That(restored.Find("son").Spec.BirthSeed, Is.EqualTo(arthur.Spec.BirthSeed));
            Assert.That(restored.Find("son").State.Library.GetOrNull("prior"), Is.Not.Null);
            Assert.That(restored.Find("son").State.Library.GetOrNull(record.MemoryId).Source, Is.EqualTo("firsthand-supported-grip"));
            Assert.That(restored.LeavePhysicalPractice(), Is.True);
            Assert.That(WorldSimulation.RestoreCheckpoint(restored.CaptureCheckpoint()).PhysicalPractice.Active, Is.False);
        }
    }
}
