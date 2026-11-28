using System;
using System.Numerics;

namespace DreamMachine.Body
{
    public readonly struct ArmReachProposal
    {
        public readonly Vector3 Elbow, Wrist;
        public readonly bool WithinEnvelope;
        internal ArmReachProposal(Vector3 elbow, Vector3 wrist, bool withinEnvelope)
        { Elbow = elbow; Wrist = wrist; WithinEnvelope = withinEnvelope; }
    }

    /// <summary>Geometric motor proposal in metres. The native links decide the executed movement.</summary>
    public static class SupportedArmReach
    {
        public const float MinFlexionDegrees = 9f, MaxFlexionDegrees = 148f;

        public static ArmReachProposal Propose(Vector3 shoulder, Vector3 intendedWrist, Vector3 elbowPreference,
            float upperLength, float forearmLength)
        {
            if (!(upperLength > 0f && forearmLength > 0f && float.IsFinite(upperLength + forearmLength)))
                throw new ArgumentOutOfRangeException(nameof(upperLength));
            var axis = intendedWrist - shoulder;
            float distance = axis.Length();
            axis = distance > 1e-5f ? axis / distance : Vector3.UnitZ;
            float squared = upperLength * upperLength + forearmLength * forearmLength;
            float product = 2f * upperLength * forearmLength;
            float low = MathF.Sqrt(squared + product * MathF.Cos(MaxFlexionDegrees * MathF.PI / 180f));
            float high = MathF.Sqrt(squared + product * MathF.Cos(MinFlexionDegrees * MathF.PI / 180f));
            float reach = Math.Clamp(distance, low, high);
            var pole = elbowPreference - Vector3.Dot(elbowPreference, axis) * axis;
            if (pole.LengthSquared() < 1e-6f)
            {
                var fallback = Math.Abs(axis.Y) < .8f ? Vector3.UnitY : Vector3.UnitX;
                pole = fallback - Vector3.Dot(fallback, axis) * axis;
            }
            float along = (upperLength * upperLength - forearmLength * forearmLength + reach * reach) / (2f * reach);
            float height = MathF.Sqrt(Math.Max(0f, upperLength * upperLength - along * along));
            return new ArmReachProposal(shoulder + along * axis + height * Vector3.Normalize(pole),
                shoulder + reach * axis, distance >= low && distance <= high);
        }
    }
}
