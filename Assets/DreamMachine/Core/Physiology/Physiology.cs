using System;
using DreamMachine.Affect;
using DreamMachine.Body;
using DreamMachine.Util;

namespace DreamMachine.Physiology
{
    /// <summary>
    /// Hidden body of one child. Nothing here is drawn.
    /// Arrays are allocated once. Tick does not allocate.
    /// </summary>
    public sealed class PhysiologyState
    {
        public const int Joints = 10;
        const int Pathways = 3;
        const int MaxTaps = 8;

        public int BirthSeed;
        public float AgeDays;
        public int Generation;
        public float Maturity;
        public float HeartRate;
        public float BreathingRate;
        public float BreathingEfficiency = 1f;
        public float Energy = 0.82f;
        public float Hunger = 0.18f;
        public float Cortisol = 0.12f;
        public float Oxytocin = 0.2f;
        public float Melatonin = 0.1f;
        public float NerveDelaySeconds = 0.12f;
        public float MuscleStrength = 0.4f;
        public float RestingHeartRate = 130f;
        public float RestingBreathRate = 32f;
        public float WalkForceScale = 1f;
        public float MeanActivation;
        public float MeanForce;
        public float MeanFatigue;
        public float Pain;
        public float Warmth;

        public readonly float[] Stretch = new float[Joints];
        public readonly float[] MotorDrive = new float[Joints];
        public readonly float[] Activation = new float[Joints];
        public readonly float[] Force = new float[Joints];
        public readonly float[] Fatigue = new float[Joints];

        readonly float[] _delay = new float[Pathways * Joints * MaxTaps];
        int _taps = 1;
        float _seedHr = 1f;
        float _seedMuscle = 1f;

        public PhysiologyState(int birthSeed)
        {
            BirthSeed = birthSeed;
            var rng = new SeededRandom(birthSeed ^ 0x0B10);
            _seedHr = 0.94f + rng.NextFloat() * 0.12f;
            _seedMuscle = 0.9f + rng.NextFloat() * 0.2f;
            Energy = 0.78f + rng.NextFloat() * 0.12f;
            Hunger = 1f - Energy;
            RecomputeGrowth();
            HeartRate = RestingHeartRate;
            BreathingRate = RestingBreathRate;
        }

        public void AdvanceAge(float dt, float daySeconds)
        {
            if (daySeconds < 0.01f) daySeconds = 0.01f;
            AgeDays += dt / daySeconds;
            RecomputeGrowth();
        }

        public void SetGeneration(int generation)
        {
            Generation = generation;
            RecomputeGrowth();
        }

        public void RecomputeGrowth()
        {
            float life = AgeDays + Generation * 8f;
            if (life < 0f) life = 0f;
            Maturity = 1f - (float)Math.Exp(-life / 18f);
            if (Maturity > 1f) Maturity = 1f;
            RestingHeartRate = Lerp(132f, 68f, Maturity) * _seedHr;
            RestingBreathRate = Lerp(34f, 15f, Maturity);
            MuscleStrength = Lerp(0.38f, 1f, Maturity) * _seedMuscle;
            NerveDelaySeconds = Lerp(0.14f, 0.045f, Maturity);
        }

        public void Step(PhysInput input, float dt, EmotionState feelings)
        {
            if (dt < 0f) dt = 0f;
            if (dt > 0.2f) dt = 0.2f;
            int taps = (int)Math.Round(NerveDelaySeconds / (dt <= 0.0001f ? 0.05f : dt));
            if (taps < 1) taps = 1;
            if (taps > MaxTaps) taps = MaxTaps;
            if (taps != _taps)
            {
                Array.Clear(_delay, 0, _delay.Length);
                _taps = taps;
            }

            Pain = input.Pain;
            if (Pain < 0f) Pain = 0f;
            if (Pain > 1f) Pain = 1f;
            Warmth = input.Warmth;
            float[] angles = input.JointAngles;
            float[] brain = input.Descending;
            float brainMean = 0.35f;
            if (brain != null && brain.Length > 0)
            {
                float sum = 0f;
                for (int i = 0; i < brain.Length; i++) sum += brain[i];
                brainMean = sum / brain.Length;
            }

            for (int j = 0; j < Joints; j++)
            {
                float angle = angles != null && j < angles.Length ? angles[j] : 0f;
                float stretch = angle < 0f ? -angle : angle;
                Stretch[j] = stretch;
                float stretchCmd = stretch > 0.55f ? (stretch - 0.55f) * 2.2f : 0f;
                float withdrawCmd = IsFlexor(j) ? Pain * 1.4f : 0f;
                float desc = brainMean;
                if (brain != null && j < brain.Length) desc = 0.5f * brainMean + 0.5f * brain[j];
                float reflex = Push(0, j, stretchCmd);
                float withdraw = Push(1, j, withdrawCmd);
                float cortical = Push(2, j, desc);
                float drive = reflex * 0.85f + withdraw + cortical;
                drive *= 0.45f + 0.55f * Energy;
                if (drive < 0f) drive = 0f;
                if (drive > 2f) drive = 2f;
                MotorDrive[j] = drive;
                float target = drive > 1f ? 1f : drive;
                Activation[j] += (target - Activation[j]) * (1f - (float)Math.Exp(-dt * 8f));
                if (Activation[j] < 0f) Activation[j] = 0f;
                if (Activation[j] > 1f) Activation[j] = 1f;
                if (Activation[j] > 0.25f) Fatigue[j] += dt * Activation[j] * 0.12f;
                else Fatigue[j] -= dt * 0.07f;
                if (Fatigue[j] < 0f) Fatigue[j] = 0f;
                if (Fatigue[j] > 1f) Fatigue[j] = 1f;
                Force[j] = Activation[j] * (1f - Fatigue[j]) * MuscleStrength;
            }

            float act = 0f, force = 0f, fat = 0f;
            for (int j = 0; j < Joints; j++)
            {
                act += Activation[j];
                force += Force[j];
                fat += Fatigue[j];
            }
            MeanActivation = act / Joints;
            MeanForce = force / Joints;
            MeanFatigue = fat / Joints;

            float smoke = input.Smoke;
            if (smoke < 0f) smoke = 0f;
            if (smoke > 1f) smoke = 1f;
            BreathingEfficiency = 1f - 0.7f * smoke;
            float exertion = input.Exertion > MeanActivation ? input.Exertion : MeanActivation;
            float arousal = input.Arousal;
            float hrTarget = RestingHeartRate + exertion * 48f + arousal * 26f + smoke * 14f + Pain * 10f;
            float brTarget = RestingBreathRate + exertion * 12f + smoke * 10f;
            float ease = 1f - (float)Math.Exp(-dt * 1.4f);
            HeartRate += (hrTarget - HeartRate) * ease;
            BreathingRate += (brTarget - BreathingRate) * ease;

            float burn = dt * (0.015f + exertion * 0.12f + (HeartRate / 200f) * 0.02f);
            Energy -= burn;
            if (input.Eating) Energy += 0.22f;
            if (input.Night) Energy += dt * 0.05f;
            if (Energy < 0.02f) Energy = 0.02f;
            if (Energy > 1f) Energy = 1f;
            float hungerTarget = 1f - Energy;
            Hunger += (hungerTarget - Hunger) * (1f - (float)Math.Exp(-dt * 0.8f));
            if (Hunger < 0f) Hunger = 0f;
            if (Hunger > 1f) Hunger = 1f;

            float cortTarget = 0.1f + 0.55f * Pain + 0.3f * Hunger + 0.2f * (feelings != null ? feelings.FearOfFalling : 0f);
            float oxyTarget = 0.5f * input.BondSameParent + 0.32f * input.BondOtherParent + 0.22f * input.BondSibling + 0.08f * input.CoupleWarmth;
            float melTarget = input.Night ? 0.82f : 0.1f;
            float hEase = 1f - (float)Math.Exp(-dt * 0.7f);
            Cortisol += (Clamp01(cortTarget) - Cortisol) * hEase;
            Oxytocin += (Clamp01(oxyTarget) - Oxytocin) * hEase;
            Melatonin += (Clamp01(melTarget) - Melatonin) * hEase;

            WalkForceScale = Energy * (1f - MeanFatigue) * (0.5f + 0.5f * MuscleStrength);
            if (WalkForceScale < 0.35f) WalkForceScale = 0.35f;
            if (WalkForceScale > 1.15f) WalkForceScale = 1.15f;

            if (feelings != null && dt > 0f)
            {
                feelings.Excite(
                    (0.15f - Cortisol) * 0.04f * dt + Oxytocin * 0.02f * dt,
                    Cortisol * 0.05f * dt + Hunger * 0.03f * dt,
                    Oxytocin * 0.08f * dt,
                    Cortisol * 0.06f * dt + Pain * 0.05f * dt - Oxytocin * 0.03f * dt,
                    Melatonin * 0.1f * dt + (1f - BreathingEfficiency) * 0.08f * dt + (1f - Energy) * 0.04f * dt);
            }
        }

        public void WriteRom(MuscleMemoryLibrary memory, int day)
        {
            if (memory == null) return;
            for (int j = 0; j < Joints && j < JointNames.All.Length; j++)
            {
                float reach = (0.3f + 0.95f * (1f - Fatigue[j])) * MuscleStrength;
                memory.NoteRange("rom:" + JointNames.All[j], -reach, reach, day, "muscle physiology");
            }
        }

        public float FlexorDrive()
        {
            return (MotorDrive[1] + MotorDrive[3] + MotorDrive[5] + MotorDrive[7]) * 0.25f;
        }

        internal void WriteCheckpoint(System.IO.BinaryWriter writer)
        {
            writer.Write(AgeDays);
            writer.Write(Maturity);
            writer.Write(HeartRate);
            writer.Write(BreathingRate);
            writer.Write(BreathingEfficiency);
            writer.Write(Energy);
            writer.Write(Hunger);
            writer.Write(Cortisol);
            writer.Write(Oxytocin);
            writer.Write(Melatonin);
            writer.Write(NerveDelaySeconds);
            writer.Write(MuscleStrength);
            writer.Write(RestingHeartRate);
            writer.Write(RestingBreathRate);
            writer.Write(WalkForceScale);
            writer.Write(MeanActivation);
            writer.Write(MeanForce);
            writer.Write(MeanFatigue);
            writer.Write(Pain);
            writer.Write(Warmth);
            writer.Write(_seedHr);
            writer.Write(_seedMuscle);
            writer.Write(BirthSeed);
            writer.Write(Generation);
            writer.Write(_taps);
            for (int i = 0; i < Stretch.Length; i++) writer.Write(Stretch[i]);
            for (int i = 0; i < MotorDrive.Length; i++) writer.Write(MotorDrive[i]);
            for (int i = 0; i < Activation.Length; i++) writer.Write(Activation[i]);
            for (int i = 0; i < Force.Length; i++) writer.Write(Force[i]);
            for (int i = 0; i < Fatigue.Length; i++) writer.Write(Fatigue[i]);
            for (int i = 0; i < _delay.Length; i++) writer.Write(_delay[i]);
        }

        internal void ReadCheckpoint(System.IO.BinaryReader reader)
        {
            AgeDays = reader.ReadSingle();
            Maturity = reader.ReadSingle();
            HeartRate = reader.ReadSingle();
            BreathingRate = reader.ReadSingle();
            BreathingEfficiency = reader.ReadSingle();
            Energy = reader.ReadSingle();
            Hunger = reader.ReadSingle();
            Cortisol = reader.ReadSingle();
            Oxytocin = reader.ReadSingle();
            Melatonin = reader.ReadSingle();
            NerveDelaySeconds = reader.ReadSingle();
            MuscleStrength = reader.ReadSingle();
            RestingHeartRate = reader.ReadSingle();
            RestingBreathRate = reader.ReadSingle();
            WalkForceScale = reader.ReadSingle();
            MeanActivation = reader.ReadSingle();
            MeanForce = reader.ReadSingle();
            MeanFatigue = reader.ReadSingle();
            Pain = reader.ReadSingle();
            Warmth = reader.ReadSingle();
            _seedHr = reader.ReadSingle();
            _seedMuscle = reader.ReadSingle();
            BirthSeed = reader.ReadInt32();
            Generation = reader.ReadInt32();
            _taps = reader.ReadInt32();
            for (int i = 0; i < Stretch.Length; i++) Stretch[i] = reader.ReadSingle();
            for (int i = 0; i < MotorDrive.Length; i++) MotorDrive[i] = reader.ReadSingle();
            for (int i = 0; i < Activation.Length; i++) Activation[i] = reader.ReadSingle();
            for (int i = 0; i < Force.Length; i++) Force[i] = reader.ReadSingle();
            for (int i = 0; i < Fatigue.Length; i++) Fatigue[i] = reader.ReadSingle();
            for (int i = 0; i < _delay.Length; i++) _delay[i] = reader.ReadSingle();
        }

        float Push(int pathway, int joint, float value)
        {
            int basis = (pathway * Joints + joint) * MaxTaps;
            int last = _taps - 1;
            if (last < 0) last = 0;
            if (last >= MaxTaps) last = MaxTaps - 1;
            float oldest = _delay[basis + last];
            for (int i = last; i > 0; i--) _delay[basis + i] = _delay[basis + i - 1];
            _delay[basis] = value;
            return oldest;
        }

        static bool IsFlexor(int joint)
        {
            return joint == 1 || joint == 3 || joint == 5 || joint == 7;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        public int Fold(int hash)
        {
            hash = FoldOne(hash, Q(AgeDays));
            hash = FoldOne(hash, Q(HeartRate));
            hash = FoldOne(hash, Q(BreathingRate));
            hash = FoldOne(hash, Q(Energy));
            hash = FoldOne(hash, Q(Hunger));
            hash = FoldOne(hash, Q(Cortisol));
            hash = FoldOne(hash, Q(Oxytocin));
            hash = FoldOne(hash, Q(Melatonin));
            hash = FoldOne(hash, Q(MeanForce));
            hash = FoldOne(hash, Q(Pain));
            return hash;
        }

        static int FoldOne(int hash, int value)
        {
            unchecked
            {
                return (hash ^ value) * 16777619;
            }
        }

        public static int Q(float v)
        {
            return (int)Math.Round(v * 1000.0);
        }
    }

    public struct PhysInput
    {
        public float Pain;
        public float Warmth;
        public float Smoke;
        public float BondSameParent;
        public float BondOtherParent;
        public float BondSibling;
        public float CoupleWarmth;
        public float Arousal;
        public float Exertion;
        public bool Night;
        public bool Eating;
        public float[] JointAngles;
        public float[] Descending;
    }
}
