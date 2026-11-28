using System;
using System.Numerics;

namespace DreamMachine.Body
{
    public enum PlacementObservation : byte { None, Occluded, Moving, StillAtDestination, Displaced, BelowDestination }

    public readonly struct PlacementSense
    {
        public readonly bool Tracked, AtDestination, Opposed, Slip;
        public readonly float SupportFraction, PalmSpeed, PalmSpin, BallSpeed, BallSpin;

        public PlacementSense(bool tracked, bool atDestination, bool opposed, bool slip, float supportFraction,
            float palmSpeed, float palmSpin, float ballSpeed, float ballSpin)
        {
            Tracked = tracked; AtDestination = atDestination; Opposed = opposed; Slip = slip;
            SupportFraction = supportFraction; PalmSpeed = palmSpeed; PalmSpin = palmSpin;
            BallSpeed = ballSpeed; BallSpin = ballSpin;
        }
    }

    /// <summary>Engineered target shaping and transient release admission for a supported articulated hand.</summary>
    public sealed class PlacementCoordination
    {
        const int RequiredStableTicks = 5;
        public const int UnweightTicks = 8, OpenTicks = 10;
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public int StableTicks { get; private set; }
        public bool Ready => StableTicks >= RequiredStableTicks;

        public void Begin(Vector3 position)
        {
            Position = position;
            Velocity = Vector3.Zero;
            StableTicks = 0;
        }

        public Vector3 Command(Vector3 target, float speed, float verticalSpeed, float dt)
        {
            const float acceleration = .80f;
            var delta = target - Position;
            var velocity = Velocity;
            float x = AdvanceAxis(delta.X, ref velocity.X, speed, acceleration, dt);
            float y = AdvanceAxis(delta.Y, ref velocity.Y, verticalSpeed, acceleration, dt);
            float z = AdvanceAxis(delta.Z, ref velocity.Z, speed, acceleration, dt);
            Velocity = velocity;
            return Position += new Vector3(x, y, z);
        }

        static float AdvanceAxis(float error, ref float velocity, float speed, float acceleration, float dt)
        {
            float requested = Math.Min(speed, MathF.Sqrt(2f * acceleration * Math.Abs(error)));
            if (error < 0f) requested = -requested;
            velocity += Math.Clamp(requested - velocity, -acceleration * dt, acceleration * dt);
            float delta = velocity * dt;
            if (Math.Sign(delta) != Math.Sign(error) || Math.Abs(delta) < Math.Abs(error)) return delta;
            velocity = error / dt;
            return error;
        }

        public void Observe(PlacementSense sense)
        {
            bool stable = sense.Tracked && sense.AtDestination && sense.Opposed && !sense.Slip &&
                sense.SupportFraction >= .72f && sense.SupportFraction <= 1.28f &&
                sense.PalmSpeed <= .043f && sense.PalmSpin <= .60f &&
                sense.BallSpeed <= .045f && sense.BallSpin <= .34f;
            StableTicks = stable ? Math.Min(RequiredStableTicks, StableTicks + 1) : 0;
        }

        public static float Opening(int tick) => Math.Clamp((tick - UnweightTicks) / (float)OpenTicks, 0f, 1f);

        public static float ReleaseCap(float cap, int tick) =>
            cap + (.12f - cap) * Math.Clamp(tick / (float)UnweightTicks, 0f, 1f);
    }
}
