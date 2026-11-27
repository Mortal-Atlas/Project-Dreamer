using System;

namespace DreamMachine.Body
{
    public readonly struct LoadedFootRegion
    {
        public readonly float MinX, MaxX, MinZ, MaxZ;

        public LoadedFootRegion(float minX, float maxX, float minZ, float maxZ)
        {
            MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ;
        }

        public bool Valid => Finite(MinX) && Finite(MaxX) && Finite(MinZ) && Finite(MaxZ) && MaxX > MinX && MaxZ > MinZ;
        public float Width => Valid ? MaxX - MinX : 0f;
        public float Length => Valid ? MaxZ - MinZ : 0f;
        public float FrontalMargin(float x) => Valid && Finite(x) ? Math.Min(x - MinX, MaxX - x) : float.NaN;
        public float SagittalMargin(float z) => Valid && Finite(z) ? Math.Min(z - MinZ, MaxZ - z) : float.NaN;
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Flags]
    public enum PreliftFailure
    {
        None = 0, Contact = 1, StanceImpulse = 2, SwingImpulse = 4, Region = 8, Pressure = 16,
        LateralSpeed = 32, Margin = 64, Tilt = 128, Effort = 256, Slip = 512
    }

    public static class DynamicSupport
    {
        public const int PreliftTicks = 8, FinalSupportTicks = 10, LiftHoldTicks = 3;
        public const float Clearance = .01f, Advancement = .08f, Slip = .02f;

        public static float Extrapolate(float position, float velocity, float gravity, float height)
        {
            if (!(height > 0f && gravity > 0f && LoadedFootRegion.Finite(height) && LoadedFootRegion.Finite(gravity) &&
                LoadedFootRegion.Finite(position) && LoadedFootRegion.Finite(velocity))) return float.NaN;
            return position + velocity * (float)Math.Sqrt(height / gravity);
        }

        public static PreliftFailure Evaluate(LoadedFootRegion region, bool contact, float contactFraction, float stanceImpulse,
            float swingImpulse, float staticImpulse, bool pressureValid, float pressureX, float pressureZ, float xCom, float zCom,
            float lateralSpeed, float tilt, float peakEffortFraction, float drift)
        {
            PreliftFailure reasons = PreliftFailure.None;
            if (!contact || !(contactFraction >= .8f)) reasons |= PreliftFailure.Contact;
            if (!(staticImpulse > 0f && stanceImpulse >= .70f * staticImpulse)) reasons |= PreliftFailure.StanceImpulse;
            if (!(swingImpulse >= 0f && swingImpulse <= .15f * staticImpulse)) reasons |= PreliftFailure.SwingImpulse;
            if (!region.Valid || region.Width < .02f || region.Length < .035f) reasons |= PreliftFailure.Region;
            if (!pressureValid || !(region.FrontalMargin(pressureX) >= -.0001f && region.SagittalMargin(pressureZ) >= -.0001f))
                reasons |= PreliftFailure.Pressure;
            if (!(Math.Abs(lateralSpeed) <= .05f)) reasons |= PreliftFailure.LateralSpeed;
            if (!(region.FrontalMargin(xCom) >= .002f && region.SagittalMargin(zCom) >= 0f)) reasons |= PreliftFailure.Margin;
            if (!(tilt >= 0f && tilt <= 8f)) reasons |= PreliftFailure.Tilt;
            if (!(peakEffortFraction >= 0f && peakEffortFraction <= .85f)) reasons |= PreliftFailure.Effort;
            if (!(drift >= 0f && drift <= Slip)) reasons |= PreliftFailure.Slip;
            return reasons;
        }

        public static bool HoldsStance(bool contact, float impulse, float weightImpulse, float drift) =>
            contact && weightImpulse > 0f && impulse >= .55f * weightImpulse && drift <= Slip;

        public static bool WarnOutward(LoadedFootRegion region, bool contact, float xCom, float lateralSpeed, int direction)
        {
            if (!contact || !region.Valid || !(direction * lateralSpeed > .067f)) return false;
            float distance = direction < 0 ? xCom - region.MinX : region.MaxX - xCom;
            // Restrict the warning to the outward edge and a still-reachable neighborhood.
            return distance >= -.008f && distance < .018f && distance < region.Width * .5f;
        }

        public static bool WarnPrematureTouchdown(float previousHeight, float height) =>
            height > 0f && height < Clearance && previousHeight - height > .00025f;

        public static float RecoveryForward(float offset) => Math.Max(-.045f, Math.Min(.065f, offset));

        // Quintic endpoint velocity and acceleration are zero, even at the join of two stages.
        public static float Smooth(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * t * (10f + t * (-15f + 6f * t));
        }
    }
}
