using System;
using DreamMachine.Util;

namespace DreamMachine.Body
{
    public readonly struct HumanoidAnatomy
    {
        public const int GeneratorVersion = 1;
        public const float AdultAgeDays = 6f;
        public int BirthSeed { get; }
        public float NewbornHeight { get; }
        public float AdultHeight { get; }
        public float SpurtTiming { get; }
        readonly float _breadth, _limbs, _head;

        public HumanoidAnatomy(int birthSeed)
        {
            BirthSeed = birthSeed;
            var rng = new SeededRandom(birthSeed ^ 0x41A70C);
            float frame = rng.NextFloat();
            NewbornHeight = 0.48f + 0.06f * frame;
            AdultHeight = 1.55f + 0.34f * frame;
            SpurtTiming = rng.Range(0.42f, 0.58f);
            _breadth = rng.Range(0.94f, 1.06f);
            _limbs = rng.Range(0.97f, 1.03f);
            _head = rng.Range(0.97f, 1.03f);
        }

        /// <summary>Equivalent continuous age with daytime plateaus and growth during the sleeping fraction.</summary>
        public static float SleepAge(float chronologicalDays, float awakeFraction)
        {
            float days = Math.Max(0f, chronologicalDays);
            float whole = (float)Math.Floor(days);
            return whole + Math.Max(0f, days - whole - awakeFraction) / (1f - awakeFraction);
        }

        public HumanoidDimensions AtAge(float developmentDays)
        {
            float age = Math.Max(0f, Math.Min(1f, developmentDays / AdultAgeDays));
            float baseline = age * (1.5f - 0.5f * age);
            float spurt = Math.Max(0f, Math.Min(1f, (age - SpurtTiming + 0.15f) / 0.30f));
            float body = 0.86f * baseline + 0.14f * spurt * spurt * (3f - 2f * spurt);
            float ends = body * (1.18f - 0.18f * body);
            float head = body * (1.4f - 0.4f * body);
            float height = Lerp(NewbornHeight, AdultHeight, body);
            float lowerLimb = Lerp(0.34f * NewbornHeight, 0.47f * AdultHeight, body) * _limbs;
            float headHeight = Lerp(0.244f * NewbornHeight, 0.128f * AdultHeight, head) * _head;
            float neck = Lerp(0.018f * NewbornHeight, 0.042f * AdultHeight, body);
            float ankle = Lerp(0.039f * NewbornHeight, 0.036f * AdultHeight, ends);
            return new HumanoidDimensions(
                height, headHeight, neck, height - headHeight - neck - lowerLimb - ankle,
                _breadth * Lerp(0.24f * NewbornHeight, 0.23f * AdultHeight, body),
                _breadth * Lerp(0.17f * NewbornHeight, 0.17f * AdultHeight, body),
                _limbs * Lerp(0.127f * NewbornHeight, 0.18f * AdultHeight, body),
                _limbs * Lerp(0.10f * NewbornHeight, 0.145f * AdultHeight, body),
                Lerp(0.105f * NewbornHeight, 0.105f * AdultHeight, ends),
                lowerLimb * 0.53f, lowerLimb * 0.47f, ankle,
                Lerp(0.138f * NewbornHeight, 0.146f * AdultHeight, ends));
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>Neutral-standing geometry in metres. The reach bound is an engineering baseline.</summary>
    public readonly struct HumanoidDimensions
    {
        public readonly float Height, Head, Neck, Torso, ShoulderWidth, HipWidth;
        public readonly float UpperArm, Forearm, Hand, Thigh, Shin, AnkleHeight, Foot;

        internal HumanoidDimensions(float height, float head, float neck, float torso, float shoulderWidth, float hipWidth,
            float upperArm, float forearm, float hand, float thigh, float shin, float ankleHeight, float foot)
        {
            Height = height;
            Head = head;
            Neck = neck;
            Torso = torso;
            ShoulderWidth = shoulderWidth;
            HipWidth = hipWidth;
            UpperArm = upperArm;
            Forearm = forearm;
            Hand = hand;
            Thigh = thigh;
            Shin = shin;
            AnkleHeight = ankleHeight;
            Foot = foot;
        }

        public float PelvisHeight => AnkleHeight + Shin + Thigh;
        public float ShoulderHeight => PelvisHeight + Torso * 0.82f;
        public float EyeHeight => Height - Head * 0.44f;
        public float GripReach => UpperArm + Forearm + Hand * 0.5f;

        public bool WithinGripReach(float shoulderToTargetMetres) =>
            shoulderToTargetMetres >= 0f && shoulderToTargetMetres <= GripReach;

        public void GripPosition(float shoulderRadians, float elbowRadians, out float down, out float forward)
        {
            float bend = shoulderRadians + elbowRadians;
            float distal = Forearm + Hand * 0.5f;
            down = UpperArm * (float)Math.Cos(shoulderRadians) + distal * (float)Math.Cos(bend);
            forward = -UpperArm * (float)Math.Sin(shoulderRadians) - distal * (float)Math.Sin(bend);
        }
    }
}
