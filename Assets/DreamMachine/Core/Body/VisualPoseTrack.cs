using System;
using System.Numerics;

namespace DreamMachine.Body
{
    public enum VisualEstimateSource : byte { Unavailable, GeometryObservation, Predicted }

    public readonly struct DepthObservation
    {
        public readonly Vector3 Position;
        public readonly int Tick, Rays;
        public readonly float Confidence;

        public DepthObservation(Vector3 position, int tick, int rays, float confidence)
        { Position = position; Tick = tick; Rays = rays; Confidence = Math.Clamp(confidence, 0f, 1f); }
    }

    public readonly struct VisualPoseEstimate
    {
        public readonly Vector3 Position, Velocity;
        public readonly int ObservationTick, Age, VisibleRays;
        public readonly float Confidence, PredictionError;
        public readonly VisualEstimateSource Source;

        public VisualPoseEstimate(Vector3 position, Vector3 velocity, int observationTick, int age,
            int rays, float confidence, float error, VisualEstimateSource source)
        {
            Position = position; Velocity = velocity; ObservationTick = observationTick; Age = age;
            VisibleRays = rays; Confidence = confidence; PredictionError = error; Source = source;
        }
    }

    /// <summary>One individual's transient geometry observations and bounded motion belief.</summary>
    public sealed class VisualPoseTrack
    {
        public const int PredictionHorizon = 12;
        DepthObservation _last;
        Vector3 _velocity, _estimate;
        int _arrival = -1, _attempt = -1;
        bool _seen, _visible;
        float _error;

        public float PredictionError => _error;
        public int LastObservationTick => _seen ? _last.Tick : -1;

        public void Advance(int tick, Vector3? proprioceptiveDelta, float dt)
        {
            if (!_seen || tick - _last.Tick > PredictionHorizon) return;
            _estimate += proprioceptiveDelta ?? _velocity * dt;
        }

        public bool Receive(DepthObservation sample, int arrived, float dt)
        {
            if (sample.Tick > arrived || sample.Tick <= _attempt) return false;
            int elapsed = _seen ? sample.Tick - _last.Tick : 0;
            _attempt = sample.Tick;
            _visible = sample.Rays > 0;
            if (!_visible) return false;
            _error = elapsed > 0 ? Vector3.Distance(_estimate, sample.Position + _velocity * ((arrived - sample.Tick) * dt)) : 0f;
            if (elapsed > 0 && elapsed <= PredictionHorizon)
                _velocity = Limit((sample.Position - _last.Position) / (elapsed * dt), .60f);
            else _velocity = Vector3.Zero;
            _last = sample;
            _seen = true;
            _arrival = arrived;
            _estimate = sample.Position + _velocity * ((arrived - sample.Tick) * dt);
            return true;
        }

        public VisualPoseEstimate Read(int now, int recentTicks, bool predict)
        {
            if (!_seen) return new VisualPoseEstimate(default, default, -1, -1, 0, 0f, 0f, VisualEstimateSource.Unavailable);
            int age = now - _last.Tick;
            bool recent = _visible && now - _arrival <= recentTicks && age <= recentTicks + 4;
            float confidence = _last.Confidence / (1f + age * .16f + _error * 8f);
            var source = recent && (!predict || age == 0) ? VisualEstimateSource.GeometryObservation :
                predict && age <= PredictionHorizon ? VisualEstimateSource.Predicted : VisualEstimateSource.Unavailable;
            return new VisualPoseEstimate(predict && now > _last.Tick ? _estimate : _last.Position, _velocity,
                _last.Tick, age, _visible ? _last.Rays : 0, source == VisualEstimateSource.Unavailable ? 0f : confidence, _error, source);
        }

        static Vector3 Limit(Vector3 value, float max) =>
            value.LengthSquared() <= max * max ? value : Vector3.Normalize(value) * max;
    }
}
