using System;

namespace DreamMachine.Body
{
    public readonly struct GripContactSense
    {
        public readonly float Normal, Duration, TangentialSpeed;
        public readonly bool Slip;
        public GripContactSense(float normal, float duration, float tangentialSpeed, bool slip)
        {
            Normal = normal;
            Duration = duration;
            TangentialSpeed = tangentialSpeed;
            Slip = slip;
        }
    }

    // A transient motor response. The familiar scalar cap remains the only durable parameter.
    public sealed class ContactGatedGrip
    {
        const float MinimumContactTime = .035f;
        const float FastSlip = .060f;
        const int RequiredStableTicks = 3;
        int _stable;
        public float Cap { get; private set; }
        public int Corrections { get; private set; }
        public bool Ready => _stable >= RequiredStableTicks;
        public bool Uncertain { get; private set; }

        public ContactGatedGrip(float initialCap)
        {
            if (!(initialCap >= GripAcquisition.InitialCap && initialCap <= GripAcquisition.ReferenceCap))
                throw new ArgumentOutOfRangeException(nameof(initialCap));
            Cap = initialCap;
        }

        public void Observe(GripContactSense left, GripContactSense right, bool lifting)
        {
            bool bilateral = Loaded(left) && Loaded(right);
            bool slip = Slipping(left) || Slipping(right);
            Uncertain = !bilateral || slip;
            _stable = Uncertain ? 0 : Math.Min(RequiredStableTicks, _stable + 1);
            // A lack of contact during approach is not a reason to squeeze harder.
            if (!slip && !(lifting && !bilateral)) return;
            float next = Math.Min(GripAcquisition.ReferenceCap, Cap + (lifting ? .44f : .24f));
            if (next == Cap) return;
            Cap = next;
            Corrections++;
        }

        static bool Loaded(GripContactSense pad) =>
            pad.Normal >= GripCalibration.ContactForce && pad.Duration >= MinimumContactTime;

        static bool Slipping(GripContactSense pad) =>
            pad.Normal >= GripCalibration.ContactForce && (pad.Slip || pad.TangentialSpeed >= FastSlip);
    }
}
