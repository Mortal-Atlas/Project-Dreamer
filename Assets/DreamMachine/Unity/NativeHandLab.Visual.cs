using System.Collections.Generic;
using System.Diagnostics;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public readonly struct VisualPlacementReading
    {
        public readonly VisualPoseEstimate Hand, Ball, Surface;
        public readonly Vector3 ProprioceptiveWrist, Correction;
        public readonly int CorrectionTick;
        public readonly float ExpectedContactSeconds;
        public readonly BenchOcclusion Occlusion;

        public VisualPlacementReading(VisualPoseEstimate hand, VisualPoseEstimate ball, VisualPoseEstimate surface,
            Vector3 wrist, Vector3 correction, int correctionTick, float contactSeconds, BenchOcclusion occlusion)
        {
            Hand = hand; Ball = ball; Surface = surface; ProprioceptiveWrist = wrist; Correction = correction;
            CorrectionTick = correctionTick; ExpectedContactSeconds = contactSeconds; Occlusion = occlusion;
        }
    }

    public sealed partial class NativeHandLab
    {
        BenchDepthSensor _depthSensor;
        VisualBenchSetup _visualSetup;
        Queue<BenchDepthFrame> _depthInbox;
        VisualPoseTrack _handTrack, _ballTrack, _surfaceTrack;
        Vector3 _previousProprio, _visualCorrection;
        int _visualCorrectionTick = -1;

        public bool VisualCoordination => _depthSensor != null;
        public VisualPlacementReading VisualReading { get; private set; }
        public VisualPoseEstimate FinalBallVisualSample => _ballTrack.Read(Tick, _visualSetup.Period, false);
        public long VisualCpuTicks { get; private set; }
        public long FusionCpuTicks { get; private set; }
        public long CorrectionCpuTicks { get; private set; }

        void InitializeVisual(VisualBenchSetup setup, ArmBenchSetup arm)
        {
            _visualSetup = setup;
            _handTrack = new VisualPoseTrack();
            _ballTrack = new VisualPoseTrack();
            _surfaceTrack = new VisualPoseTrack();
            _depthInbox = new Queue<BenchDepthFrame>(6);
            _previousProprio = _arm.ProprioceptiveWrist;
            var anatomy = new HumanoidAnatomy(Seed).AtAge(arm.DevelopmentDays);
            var eye = _arm.Shoulder + new Vector3(-.245f - anatomy.ShoulderWidth * .16f,
                .23f + anatomy.Head * .8f, .09f);
            _depthSensor = new BenchDepthSensor(_physics, _palmCollider, _objectCollider,
                _household.Value.Target.y > 0f ? _shelf : _table, Radius, .012f * _scale, eye, setup.ViewYaw);
            _visible = false;
            VisualReading = new VisualPlacementReading(_handTrack.Read(0, 0, false), _ballTrack.Read(0, 0, false),
                _surfaceTrack.Read(0, 0, false), _previousProprio, Vector3.zero, -1, -1f, BenchOcclusion.Clear);
        }

        void UpdateVisualEstimate()
        {
            int elapsed = Tick - _phaseTick;
            var occlusion = Phase == GripPhase.Lower && elapsed >= 4 && elapsed < 4 + _visualSetup.ObstructedTicks
                ? _visualSetup.Occlusion : BenchOcclusion.Clear;
            if (Tick % _visualSetup.Period == 0)
            {
                long captureStart = Profile ? Stopwatch.GetTimestamp() : 0;
                _depthInbox.Enqueue(_depthSensor.Capture(Tick, occlusion));
                if (Profile) VisualCpuTicks += Stopwatch.GetTimestamp() - captureStart;
            }

            long start = Profile ? Stopwatch.GetTimestamp() : 0;
            bool predict = _visualSetup.Feedback == VisualFeedback.Fused;
            var wrist = _arm.ProprioceptiveWrist;
            var wristDelta = wrist - _previousProprio;
            _previousProprio = wrist;
            float dt = WorldSimulation.Dt;
            _handTrack.Advance(Tick, N(wristDelta), dt);
            _ballTrack.Advance(Tick, _held && OpposingContact && !_last[0].Slip && !_last[1].Slip ? N(wristDelta) : null, dt);
            while (_depthInbox.Count > 0 && _depthInbox.Peek().Ball.Tick + _visualSetup.Delay <= Tick)
            {
                var frame = _depthInbox.Dequeue();
                _handTrack.Receive(frame.Hand, Tick, dt);
                _ballTrack.Receive(frame.Ball, Tick, dt);
                _surfaceTrack.Receive(frame.Surface, Tick, dt);
            }

            var hand = _handTrack.Read(Tick, _visualSetup.Period, predict);
            var ball = _ballTrack.Read(Tick, _visualSetup.Period, predict);
            var surface = _surfaceTrack.Read(Tick, _visualSetup.Period + 2, false);
            var measured = _ballTrack.Read(Tick, _visualSetup.Period, false);
            _visible = measured.Source == VisualEstimateSource.GeometryObservation;
            _freshTracking = _visible && measured.Confidence >= .17f;
            if (_visible) _trackedBall = U(measured.Position);
            _trackedSpeed = measured.Velocity.Length();

            float contact = -1f;
            if (surface.Confidence > 0f && ball.Confidence > 0f && ball.Velocity.Y < -.018f)
                contact = Mathf.Max(0f, ball.Position.Y - Radius - surface.Position.Y) / -ball.Velocity.Y;
            VisualReading = new VisualPlacementReading(hand, ball, surface, wrist, _visualCorrection, _visualCorrectionTick, contact, occlusion);

            if (Phase == GripPhase.Release || Phase == GripPhase.Withdraw || Phase == GripPhase.RestingObject)
            {
                bool heightKnown = surface.Confidence > 0f;
                PlacementObservation = !_freshTracking ? PlacementObservation.Occluded :
                    heightKnown && _trackedBall.y - Radius < surface.Position.Y - .024f ? PlacementObservation.BelowDestination :
                    _trackedSpeed > .045f ? PlacementObservation.Moving :
                    _hasObservedRelease && Vector3.Distance(_observedRelease, _trackedBall) > .018f
                        ? PlacementObservation.Displaced :
                    AtDestination ? PlacementObservation.StillAtDestination : PlacementObservation.Displaced;
            }
            if (Profile) FusionCpuTicks += Stopwatch.GetTimestamp() - start;
        }

        Vector3 ApplyVisualDescent(Vector3 target, ref float speed)
        {
            long start = Profile ? Stopwatch.GetTimestamp() : 0;
            var sample = VisualReading;
            bool feedback = _visualSetup.Feedback != VisualFeedback.MeasureOnly;
            bool aboveContact = SupportForce < .35f * WeightReference;
            if (feedback && aboveContact)
            {
                Vector3 correction = Vector3.zero;
                if (sample.Ball.Confidence >= .14f)
                {
                    float gain = sample.Ball.Source == VisualEstimateSource.Predicted && sample.Ball.Age > _visualSetup.Period + _visualSetup.Delay
                        ? .25f : .58f;
                    correction = gain * (new Vector3(_radialGoal, 0f, _depthGoal) - U(sample.Ball.Position));
                    correction.y = 0f;
                    if (_visualSetup.Feedback == VisualFeedback.Fused && sample.Hand.Confidence >= .14f)
                    {
                        var offset = U(sample.Hand.Position) - sample.ProprioceptiveWrist;
                        correction -= Vector3.ProjectOnPlane(offset, Vector3.up) * .17f;
                    }
                    correction = Vector3.ClampMagnitude(correction, .045f);
                    var next = Vector3.MoveTowards(_visualCorrection, correction, .0035f);
                    if ((next - _visualCorrection).sqrMagnitude >= .00000025f) _visualCorrectionTick = Tick + 1;
                    _visualCorrection = next;
                }
                else speed *= .62f;
                if (sample.Surface.Confidence > .22f) target.y += sample.Surface.Position.Y - _household.Value.Target.y;
            }
            if (Profile) CorrectionCpuTicks += Stopwatch.GetTimestamp() - start;
            return target + _visualCorrection;
        }
    }
}
