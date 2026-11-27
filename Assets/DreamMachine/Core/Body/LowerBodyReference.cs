using System;

namespace DreamMachine.Body
{
    /// <summary>Isolated flat-floor support reference with a prescribed static point load.</summary>
    public static class LowerBodyReference
    {
        public readonly struct Sample
        {
            public readonly GroundedGait.Leg Left, Right;
            public readonly bool LoadInsideSupport;
            public readonly float LeftVerticalNewtons, RightVerticalNewtons;

            internal Sample(GroundedGait.Leg left, GroundedGait.Leg right, bool inside, float leftLoad, float rightLoad)
            {
                Left = left;
                Right = right;
                LoadInsideSupport = inside;
                LeftVerticalNewtons = leftLoad;
                RightVerticalNewtons = rightLoad;
            }
        }

        public static Sample Evaluate(HumanoidDimensions dimensions, float distance, bool stepping,
            float loadLateral, float loadForward, float massKg)
        {
            if (!(massKg > 0f) || float.IsInfinity(massKg)) throw new ArgumentOutOfRangeException(nameof(massKg));
            var left = GroundedGait.Sample(dimensions, distance, stepping, true);
            var right = GroundedGait.Sample(dimensions, distance, stepping, false);
            float hip = dimensions.HipWidth * 0.5f;
            float footHalfWidth = dimensions.Foot * 0.235f;
            float heel = -dimensions.Foot * 0.26f, toe = dimensions.Foot * 0.74f;
            float leftX = left.Supports ? -hip : hip;
            float rightX = right.Supports ? hip : -hip;
            float width = rightX - leftX;
            float tMin = 0f, tMax = 1f;
            if (width > 0f)
            {
                tMin = Math.Max(0f, (loadLateral - footHalfWidth - leftX) / width);
                tMax = Math.Min(1f, (loadLateral + footHalfWidth - leftX) / width);
            }
            else if (Math.Abs(loadLateral - leftX) > footHalfWidth)
                return new Sample(left, right, false, 0, 0);

            // Convex envelope of equal-size supported foot rectangles. The point load
            // must fit a footprint swept between the admissible lateral centres.
            float leftZ = left.Supports ? left.Forward : right.Forward;
            float rightZ = right.Supports ? right.Forward : left.Forward;
            float a = leftZ + (rightZ - leftZ) * tMin, b = leftZ + (rightZ - leftZ) * tMax;
            bool inside = tMin <= tMax && loadForward >= Math.Min(a, b) + heel && loadForward <= Math.Max(a, b) + toe;
            if (!inside) return new Sample(left, right, false, 0, 0);

            float fractionRight = !left.Supports ? 1f : !right.Supports ? 0f :
                Math.Max(0f, Math.Min(1f, (loadLateral + hip) / (2f * hip)));
            float weight = massKg * 9.80665f;
            return new Sample(left, right, true, weight * (1f - fractionRight), weight * fractionRight);
        }
    }
}
