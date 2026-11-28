using System;

namespace DreamMachine.Body
{
    /// <summary>Flat-ground kinematic reference. Angles follow the segmented rig's +X axis.</summary>
    public static class GroundedGait
    {
        public readonly struct Leg
        {
            public readonly float Hip, Knee, Ankle, Forward, Lift;
            public readonly bool Supports;

            internal Leg(float hip, float knee, float forward, float lift, bool supports)
            {
                Hip = hip;
                Knee = knee;
                Ankle = -hip - knee;
                Forward = forward;
                Lift = lift;
                Supports = supports;
            }
        }

        public static float Stride(HumanoidDimensions d) => (d.Thigh + d.Shin) * 0.38f;

        public static float PelvisHeight(HumanoidDimensions d, bool walking) =>
            d.AnkleHeight + (d.Thigh + d.Shin) * (walking ? 0.993f : 0.999f);

        public static Leg Sample(HumanoidDimensions d, float distance, bool walking, bool left)
        {
            float forward = 0f, lift = 0f;
            bool supports = true;
            if (walking)
            {
                float stride = Stride(d);
                float phase = distance / stride + (left ? 0.5f : 0f);
                phase -= (float)Math.Floor(phase);
                supports = phase < 0.5f;
                if (supports) forward = stride * (0.25f - phase);
                else
                {
                    float u = (phase - 0.5f) * 2f;
                    forward = stride * (-0.25f + 0.5f * u * u * (3f - 2f * u));
                    lift = stride * 0.24f * (float)Math.Sin(Math.PI * u);
                }
            }

            float down = PelvisHeight(d, walking) - d.AnkleHeight - lift;
            float cosine = (down * down + forward * forward - d.Thigh * d.Thigh - d.Shin * d.Shin)
                / (2f * d.Thigh * d.Shin);
            float knee = (float)Math.Acos(Math.Max(-1f, Math.Min(1f, cosine)));
            float hip = (float)(Math.Atan2(-forward, down) -
                Math.Atan2(d.Shin * Math.Sin(knee), d.Thigh + d.Shin * Math.Cos(knee)));
            return new Leg(hip, knee, forward, lift, supports);
        }
    }
}
