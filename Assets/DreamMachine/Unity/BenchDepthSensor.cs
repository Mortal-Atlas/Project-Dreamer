using DreamMachine.Body;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public enum VisualFeedback : byte { MeasureOnly, VisualOnly, Fused }
    public enum BenchOcclusion : byte { Clear, PartialHand, PartialBall, Hand, Ball, Both, Surface }

    public readonly struct VisualBenchSetup
    {
        public readonly VisualFeedback Feedback;
        public readonly BenchOcclusion Occlusion;
        public readonly int Period, Delay, ObstructedTicks;
        public readonly float ViewYaw;

        public VisualBenchSetup(VisualFeedback feedback, BenchOcclusion occlusion = BenchOcclusion.Clear,
            int period = 2, int delay = 1, int obstructedTicks = 0, float viewYaw = 0f)
        {
            if (period < 1 || period > 5 || delay < 0 || delay > 4 || obstructedTicks < 0 ||
                obstructedTicks > 90 || !(Mathf.Abs(viewYaw) <= 18f))
                throw new System.ArgumentOutOfRangeException(nameof(period));
            Feedback = feedback; Occlusion = occlusion; Period = period; Delay = delay;
            ObstructedTicks = obstructedTicks; ViewYaw = viewYaw;
        }
    }

    internal readonly struct BenchDepthFrame
    {
        public readonly DepthObservation Hand, Ball, Surface;

        public BenchDepthFrame(DepthObservation hand, DepthObservation ball, DepthObservation surface)
        { Hand = hand; Ball = ball; Surface = surface; }
    }

    // Fixed aperture and nearest depth. Target handles label hits only and never supply ray directions.
    internal sealed class BenchDepthSensor
    {
        const int Columns = 29, Rows = 23;
        const float Range = 1.45f;
        readonly PhysicsScene _physics;
        readonly Collider _palm, _ball, _surface;
        readonly Vector3 _eye;
        readonly Vector3[] _rays = new Vector3[Columns * Rows];
        readonly float _radius, _palmHalfDepth;

        public BenchDepthSensor(PhysicsScene physics, Collider palm, Collider ball, Collider surface, float radius,
            float palmHalfDepth, Vector3 eye, float yaw)
        {
            _physics = physics; _palm = palm; _ball = ball; _surface = surface;
            _radius = radius; _palmHalfDepth = palmHalfDepth; _eye = eye;
            var facing = Quaternion.AngleAxis(yaw, Vector3.up) *
                Quaternion.LookRotation(new Vector3(-.10f, .25f, -.11f) - eye);
            float width = Mathf.Tan(37f * Mathf.Deg2Rad), height = Mathf.Tan(31f * Mathf.Deg2Rad);
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                    _rays[row * Columns + column] = facing * new Vector3(
                        (2f * column / (Columns - 1) - 1f) * width,
                        (2f * row / (Rows - 1) - 1f) * height, 1f).normalized;
        }

        public BenchDepthFrame Capture(int tick, BenchOcclusion occlusion)
        {
            var hand = new DepthAccumulator();
            var ball = new DepthAccumulator();
            var surface = new DepthAccumulator();
            for (int i = 0; i < _rays.Length; i++)
            {
                if (!_physics.Raycast(_eye, _rays[i], out RaycastHit hit, Range,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                bool alternate = ((i + i / Columns) & 1) == 0;
                if (hit.collider == _ball && occlusion != BenchOcclusion.Ball && occlusion != BenchOcclusion.Both &&
                    (occlusion != BenchOcclusion.PartialBall || alternate))
                    ball.Add(hit.point - hit.normal * _radius, hit.distance);
                else if (hit.collider == _palm && occlusion != BenchOcclusion.Hand && occlusion != BenchOcclusion.Both &&
                    (occlusion != BenchOcclusion.PartialHand || alternate))
                    hand.Add(hit.point - hit.normal * _palmHalfDepth, hit.distance);
                else if (hit.collider == _surface && occlusion != BenchOcclusion.Surface && hit.normal.y >= .98f)
                    surface.Add(hit.point, hit.distance);
            }
            return new BenchDepthFrame(hand.Sample(tick, 8, .82f), ball.Sample(tick, 12, .97f), surface.Sample(tick, 14, .95f));
        }

        struct DepthAccumulator
        {
            Vector3 _sum;
            float _distance;
            int _count;

            public void Add(Vector3 point, float distance)
            { _sum += point; _distance += distance; _count++; }

            public DepthObservation Sample(int tick, int expected, float ceiling)
            {
                if (_count == 0) return new DepthObservation(default, tick, 0, 0f);
                var point = _sum / _count;
                float confidence = ceiling * Mathf.Clamp01(_count / (float)expected) /
                    (1f + .23f * _distance / _count);
                return new DepthObservation(new System.Numerics.Vector3(Bin(point.x), Bin(point.y), Bin(point.z)),
                    tick, _count, confidence);
            }

            static float Bin(float m) => Mathf.Round(m * 250f) * .004f;
        }
    }
}
