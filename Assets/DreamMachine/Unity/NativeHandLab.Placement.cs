using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using NVector = System.Numerics.Vector3;

namespace DreamMachine.UnityBridge
{
    public sealed partial class NativeHandLab
    {
        readonly PlacementCoordination _coordination;
        bool _visible, _freshTracking;
        Vector3 _trackedBall, _approachCorrection, _withdrawPoint, _observedRelease, _sampledPalm;
        Quaternion _trackedSeam, _sampledWrist;
        float _trackedSpeed, _trackedSpin;
        bool _hasObservedRelease, _hasReleaseOrigin;
        public bool Coordinated => _coordination != null;
        public bool PlacementTracked => _freshTracking;
        public int StableSupportTicks => _coordination?.StableTicks ?? 0;
        public float MeasuredWeight => WeightReference;
        public Vector3 PlacementEstimate => _trackedBall;
        public float EstimatedBallSpeed => _trackedSpeed;
        public float EstimatedBallSpin => _trackedSpin;
        public float ReleaseDrift => _hasReleaseOrigin
            ? new Vector2(_object.position.x - _releaseOrigin.x, _object.position.z - _releaseOrigin.z).magnitude : 0f;
        public PlacementObservation PlacementObservation { get; private set; }

        Vector3 CoordinatedTarget(Vector3 fallback)
        {
            bool lowering = Phase == GripPhase.Lower && _held;
            if (Phase != GripPhase.Transport && !lowering && Phase != GripPhase.Release && Phase != GripPhase.Withdraw)
                return fallback;
            if (Phase == GripPhase.Release) return _held ? U(_coordination.Position) : fallback;

            float speed = .17f * _household.Value.MotionScale;
            float vertical = .083f * _household.Value.MotionScale;
            var target = new Vector3(_radialGoal, _liftY, _reachZ + _depthGoal);
            if (lowering)
            {
                target += _approachCorrection;
                target.y = (PickupY - .005f) * _scale + _household.Value.Target.y;
                float fraction = SupportForce / WeightReference;
                if (AtDestination && fraction >= .30f)
                    target.y = _coordination.Position.Y + Mathf.Clamp((fraction - .94f) * .0024f, -.0014f, .0022f);
                speed = .067f * _household.Value.MotionScale;
                if (VisualCoordination) target = ApplyVisualDescent(target, ref speed);
            }
            if (Phase == GripPhase.Withdraw)
            {
                target = _withdrawPoint;
                speed = .15f;
            }
            return U(_coordination.Command(N(target), speed, vertical, WorldSimulation.Dt));
        }

        bool AdvanceCoordination(int elapsed)
        {
            if (Phase == GripPhase.Transport)
            {
                var goal = new Vector3(_radialGoal, _liftY, _reachZ + _depthGoal);
                bool arrived = Vector3.Distance(U(_coordination.Position), goal) <= .006f &&
                    Mathf.Abs(_palm.position.x - goal.x) < .042f && Mathf.Abs(_palm.position.z - goal.z) < .045f &&
                    _palm.linearVelocity.magnitude < .13f;
                if (arrived || elapsed >= 85)
                {
                    if (!VisualCoordination && _freshTracking)
                        _approachCorrection = new Vector3(Mathf.Clamp(_radialGoal - _trackedBall.x, -.052f, .052f), 0f,
                            Mathf.Clamp(_depthGoal - _trackedBall.z, -.040f, .040f));
                    Enter(GripPhase.Lower);
                }
                return true;
            }
            if (Phase == GripPhase.Lower && _held)
            {
                // Sample net proprioceptive motion over a full control tick. Contact impulses
                // can leave a large final-substep velocity even when the palm has barely moved.
                float palmSpeed = Vector3.Distance(_sampledPalm, _palm.position) / WorldSimulation.Dt;
                float palmSpin = Quaternion.Angle(_sampledWrist, _palm.rotation) * Mathf.Deg2Rad / WorldSimulation.Dt;
                _sampledPalm = _palm.position;
                _sampledWrist = _palm.rotation;
                _coordination.Observe(new PlacementSense(_freshTracking, AtDestination, OpposingContact,
                    _last[0].Slip || _last[1].Slip || _palm.linearVelocity.sqrMagnitude > .01f,
                    SupportForce / WeightReference, palmSpeed, palmSpin, _trackedSpeed, _trackedSpin, !VisualCoordination));
                if (_coordination.Ready || elapsed >= 170)
                {
                    ControlledPlacement = _coordination.Ready;
                    Enter(GripPhase.Release);
                }
                return true;
            }
            if (Phase == GripPhase.Withdraw)
            {
                bool clear = Vector3.Distance(_palm.position, _withdrawPoint) <= .041f &&
                    _last[0].NormalForce < GripCalibration.ContactForce && _last[1].NormalForce < GripCalibration.ContactForce;
                if (elapsed >= 12 && clear || elapsed >= 42) Enter(GripPhase.RestingObject);
                return true;
            }
            return false;
        }

        void EnterCoordination(GripPhase phase)
        {
            if (phase == GripPhase.Transport) _coordination.Begin(N(_palm.position));
            if (phase == GripPhase.Lower)
            {
                _sampledPalm = _palm.position;
                _sampledWrist = _palm.rotation;
            }
            if (phase == GripPhase.Release)
            {
                _releaseOrigin = _object.position;
                _hasReleaseOrigin = true;
                _hasObservedRelease = _visible;
                if (_visible) _observedRelease = _trackedBall;
            }
            if (phase == GripPhase.Withdraw)
            {
                _coordination.Begin(N(_palm.position));
                _withdrawPoint = _palm.position + new Vector3(0f, 0f, -.136f);
            }
        }

        void TrackPlacement()
        {
            // A fixture sighting proxy. The ray models visibility from a fixed bench viewpoint.
            // The hit normal and calibrated radius reconstruct translation. The seam pose is instrumented.
            var eye = new Vector3(.015f, .94f, -.50f);
            var sight = _object.position - eye;
            bool visible = _physics.Raycast(eye, sight.normalized, out RaycastHit hit, sight.magnitude + .005f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == _objectCollider;
            _freshTracking = _visible && visible;
            _visible = visible;
            if (visible)
            {
                Vector3 center = hit.point - Radius * hit.normal;
                var measured = new Vector3(Bin(center.x), Bin(center.y), Bin(center.z));
                var seam = _object.rotation;
                if (_freshTracking)
                {
                    _trackedSpeed = Vector3.Distance(measured, _trackedBall) / WorldSimulation.Dt;
                    _trackedSpin = Quaternion.Angle(_trackedSeam, seam) * Mathf.Deg2Rad / WorldSimulation.Dt;
                }
                _trackedBall = measured;
                _trackedSeam = seam;
            }
            if (Phase != GripPhase.Release && Phase != GripPhase.Withdraw && Phase != GripPhase.RestingObject) return;
            PlacementObservation = !_freshTracking ? PlacementObservation.Occluded :
                _trackedBall.y - Radius < _household.Value.Target.y - .024f ? PlacementObservation.BelowDestination :
                _trackedSpeed > .036f || _trackedSpin > .36f ? PlacementObservation.Moving :
                _hasObservedRelease && Vector3.Distance(_observedRelease, _trackedBall) > .018f
                    ? PlacementObservation.Displaced :
                AtDestination ? PlacementObservation.StillAtDestination : PlacementObservation.Displaced;
        }

        static float Bin(float m) => Mathf.Round(m * 1000f) * .001f;
        static NVector N(Vector3 v) => new NVector(v.x, v.y, v.z);
        static Vector3 U(NVector v) => new Vector3(v.X, v.Y, v.Z);
    }
}
