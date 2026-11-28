using System;
using System.Numerics;

namespace DreamMachine.Body
{
    public enum SeatedSupportMode : byte { Rigid, Passive, Reactive, Anticipatory }

    /// <summary>Bounded extra torque for a mechanically supported torso.</summary>
    public static class SeatedPosture
    {
        public static Vector3 Command(SeatedSupportMode mode, Vector3 uprightError, Vector3 angularVelocity,
            Vector3 intendedLever, float expectedWeight)
        {
            if (mode < SeatedSupportMode.Reactive) return Vector3.Zero;
            var correction = 34f * uprightError - 3.2f * angularVelocity;
            if (mode == SeatedSupportMode.Anticipatory)
            {
                var reaction = Vector3.Cross(intendedLever, Vector3.UnitY * Math.Clamp(expectedWeight, 0f, 15f));
                correction += Bounded(reaction, 5f);
            }
            return Bounded(correction, 12f);
        }

        static Vector3 Bounded(Vector3 value, float max) =>
            value.LengthSquared() <= max * max ? value : Vector3.Normalize(value) * max;
    }
}
