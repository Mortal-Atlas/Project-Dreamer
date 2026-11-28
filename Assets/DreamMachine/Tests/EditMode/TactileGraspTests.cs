using System;
using DreamMachine.Body;
using DreamMachine.Libraries;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class TactileGraspTests
    {
        static readonly GraspPatch Edge = new GraspPatch(new GripContactSense(.4f, .045f, .004f, false), .011f, .009f);

        [Test]
        public void RepositionUsesPadSideAndCentroidWithBoundedAttempts()
        {
            var grasp = new TactileGrasp(.18f);
            Assert.That(grasp.Reposition(default, Edge), Is.True);
            Assert.That(grasp.Lateral, Is.GreaterThan(0f));
            Assert.That(grasp.Forward, Is.GreaterThan(0f));
            Assert.That(grasp.Height, Is.GreaterThan(0f));
            Assert.That(grasp.Pitch, Is.LessThan(0f));
            Assert.That(grasp.Reposition(default, Edge), Is.True);
            Assert.That(grasp.Reposition(default, Edge), Is.False);
            var ablation = new TactileGrasp(.18f, usePlacementFeedback: false);
            Assert.That(ablation.Reposition(default, Edge), Is.False);
            Assert.That(ablation.Forward, Is.Zero);
        }

        [Test]
        public void LossDuringSuspensionRaisesEffortAndThenRequestsLowering()
        {
            var grasp = new TactileGrasp(.18f);
            grasp.Observe(default, default, false);
            Assert.That(grasp.Cap, Is.EqualTo(.18f));
            for (int i = 0; i < 3; i++) grasp.Observe(Edge, Edge, false);
            Assert.That(grasp.Ready, Is.True);
            Assert.That(grasp.Cap, Is.EqualTo(.90f));
            grasp.Observe(Edge, default, true);
            Assert.That(grasp.Lower, Is.False);
            grasp.Observe(Edge, default, true);
            grasp.Observe(Edge, default, true);
            Assert.That(grasp.Lower, Is.True);
            grasp.Observe(Edge, Edge, true);
            Assert.That(grasp.Lower, Is.False);
            for (int i = 0; i < 9; i++) grasp.Observe(default, default, true);
            Assert.That(grasp.Cap, Is.EqualTo(GripAcquisition.ReferenceCap));
        }

        [Test]
        public void SupportedObservationAppendsAFirsthandRecordAndRestoresItsReference()
        {
            var own = new LibraryThree();
            var peer = new LibraryThree();
            own.RecordSupportedGraspObservation(1, 81, "hold-gate-failed", "trace.csv/daughter/1");
            Assert.Throws<ArgumentException>(() => own.RecordSupportedGraspObservation(1, 90, "later", "trace.csv/daughter/2"));
            var saved = LibraryThree.FromJson(own.ToJson());
            var record = saved.GetOrNull("motor:h4:grip:1");
            Assert.That(record.Source, Is.EqualTo("firsthand-supported-grip"));
            Assert.That(record.Appendices[0].Evidence, Does.Contain("trace.csv/daughter/1"));
            Assert.That(peer.GetOrNull("motor:h4:grip:1"), Is.Null);
        }
    }
}
