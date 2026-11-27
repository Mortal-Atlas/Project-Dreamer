using System;

namespace DreamMachine.Body
{
    public static class GripCalibration
    {
        public const float PadHeight = .026f, PadLength = .025f;
        public const float ContactForce = .05f, SlipSpeed = .025f;
        public const float LiftClearance = .035f, MaximumPenetration = .004f;
        public const int HoldTicks = 12;

        public static float MeanForce(float impulse, float duration)
        {
            if (!(duration > 0f) || !float.IsFinite(impulse) || !float.IsFinite(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));
            return impulse / duration;
        }

        public static bool Slipping(float force, float speed, int movingSubsteps) =>
            force >= ContactForce && speed >= SlipSpeed && movingSubsteps >= 3;
    }
}
