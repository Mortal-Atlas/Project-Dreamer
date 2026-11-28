using System.Numerics;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class VisualPoseTrackTests
    {
        [Test]
        public void DelayedSampleNeverChangesItsCaptureTick()
        {
            var track = new VisualPoseTrack();
            Assert.That(track.Receive(new DepthObservation(Vector3.One, 4, 9, .9f), 3, .05f), Is.False);
            Assert.That(track.Receive(new DepthObservation(Vector3.One, 4, 9, .9f), 6, .05f), Is.True);
            Assert.That(track.Receive(new DepthObservation(Vector3.Zero, 3, 9, .9f), 7, .05f), Is.False);
            var measured = track.Read(7, 2, false);
            Assert.That(measured.Position, Is.EqualTo(Vector3.One));
            Assert.That(measured.Source, Is.EqualTo(VisualEstimateSource.GeometryObservation));
            Assert.That(measured.ObservationTick, Is.EqualTo(4));
            Assert.That(measured.Age, Is.EqualTo(3));
            Assert.That(track.Read(7, 2, true).Source, Is.EqualTo(VisualEstimateSource.Predicted));
        }

        [Test]
        public void OcclusionPreservesBoundedPredictionAndRequiresNewEvidence()
        {
            var track = new VisualPoseTrack();
            var independent = new VisualPoseTrack();
            track.Receive(new DepthObservation(Vector3.Zero, 1, 10, .9f), 1, .05f);
            track.Receive(new DepthObservation(default, 3, 0, 0f), 3, .05f);
            track.Advance(3, new Vector3(.01f, 0, 0), .05f);
            var occluded = track.Read(3, 2, true);
            Assert.That(occluded.Position.X, Is.EqualTo(.01f).Within(.00001f));
            Assert.That(occluded.Source, Is.EqualTo(VisualEstimateSource.Predicted));
            Assert.That(occluded.VisibleRays, Is.Zero);
            Assert.That(track.Read(3, 2, false).Source, Is.EqualTo(VisualEstimateSource.Unavailable));
            var old = track.Read(15, 2, true);
            Assert.That(old.Confidence, Is.Zero);
            Assert.That(old.ObservationTick, Is.EqualTo(1));
            track.Receive(new DepthObservation(Vector3.UnitZ, 16, 12, .9f), 16, .05f);
            Assert.That(track.Read(16, 2, false).Position, Is.EqualTo(Vector3.UnitZ));
            Assert.That(track.PredictionError, Is.GreaterThan(.9f));
            Assert.That(independent.Read(16, 2, true).Source, Is.EqualTo(VisualEstimateSource.Unavailable));
        }
    }
}
