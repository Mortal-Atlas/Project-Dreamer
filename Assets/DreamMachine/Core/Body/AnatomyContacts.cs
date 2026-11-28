using System;

namespace DreamMachine.Body
{
    public enum BodySegment
    {
        Pelvis, Torso, Neck, Head,
        LeftUpperArm, LeftForearm, LeftHand, LeftThumb, LeftIndex,
        RightUpperArm, RightForearm, RightHand, RightThumb, RightIndex,
        LeftThigh, LeftShin, LeftFoot, RightThigh, RightShin, RightFoot,
        Environment, Object
    }

    public static class AnatomyContacts
    {
        // Permit the fitted attachment volume at one connected joint. Distal and opposing segments stay eligible.
        public static bool ExcludePair(BodySegment a, BodySegment b)
        {
            if (a > b) (a, b) = (b, a);
            return a == b || (a, b) switch
            {
                (BodySegment.Pelvis, BodySegment.Torso or BodySegment.LeftThigh or BodySegment.RightThigh) => true,
                (BodySegment.Torso, BodySegment.Neck or BodySegment.LeftUpperArm or BodySegment.RightUpperArm) => true,
                (BodySegment.Neck, BodySegment.Head) => true,
                (BodySegment.LeftUpperArm, BodySegment.LeftForearm) or (BodySegment.RightUpperArm, BodySegment.RightForearm) => true,
                (BodySegment.LeftForearm, BodySegment.LeftHand) or (BodySegment.RightForearm, BodySegment.RightHand) => true,
                (BodySegment.LeftHand, BodySegment.LeftThumb or BodySegment.LeftIndex) or
                (BodySegment.RightHand, BodySegment.RightThumb or BodySegment.RightIndex) => true,
                (BodySegment.LeftThigh, BodySegment.LeftShin) or (BodySegment.RightThigh, BodySegment.RightShin) => true,
                (BodySegment.LeftShin, BodySegment.LeftFoot) or (BodySegment.RightShin, BodySegment.RightFoot) => true,
                _ => false
            };
        }

        public static float DiagnosticDepth(float firstContactOffset, float secondContactOffset) =>
            2f * Math.Max(firstContactOffset, secondContactOffset);
    }
}
