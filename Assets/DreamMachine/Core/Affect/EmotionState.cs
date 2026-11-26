using System;
using System.Collections.Generic;
using DreamMachine.Util;

namespace DreamMachine.Affect
{
    /// <summary>
    /// Continuous feelings. Each dial has inertia: events push a target,
    /// the felt intensity eases toward it, and targets leak back toward
    /// a personal baseline. Several can be high at once.
    /// Loosely the "feeling and drive" lens: curiosity, frustration,
    /// satisfaction, fear of falling, fatigue.
    /// </summary>
    public sealed class EmotionState
    {
        public float Curiosity;
        public float Frustration;
        public float Satisfaction;
        public float FearOfFalling;
        public float Fatigue;

        float _vC, _vFr, _vS, _vFear, _vFat;
        float _tC, _tFr, _tS, _tFear, _tFat;

        public float BaselineCuriosity = 0.35f;
        public float BaselineFrustration = 0.12f;
        public float BaselineSatisfaction = 0.20f;
        public float BaselineFear = 0.15f;
        public float BaselineFatigue = 0.10f;

        public EmotionState()
        {
            Rest();
        }

        public EmotionState(int birthSeed)
        {
            var rng = new SeededRandom(birthSeed ^ 0x0E40);
            BaselineCuriosity = 0.28f + rng.NextFloat() * 0.16f;
            BaselineFear = 0.10f + rng.NextFloat() * 0.12f;
            Rest();
        }

        public void Rest()
        {
            _tC = Curiosity = BaselineCuriosity;
            _tFr = Frustration = BaselineFrustration;
            _tS = Satisfaction = BaselineSatisfaction;
            _tFear = FearOfFalling = BaselineFear;
            _tFat = Fatigue = BaselineFatigue;
            _vC = _vFr = _vS = _vFear = _vFat = 0f;
        }

        public void Excite(float curiosity, float frustration, float satisfaction, float fear, float fatigue)
        {
            _tC = Clamp01(_tC + curiosity);
            _tFr = Clamp01(_tFr + frustration);
            _tS = Clamp01(_tS + satisfaction);
            _tFear = Clamp01(_tFear + fear);
            _tFat = Clamp01(_tFat + fatigue);
        }

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            Spring(ref Curiosity, ref _vC, _tC, dt);
            Spring(ref Frustration, ref _vFr, _tFr, dt);
            Spring(ref Satisfaction, ref _vS, _tS, dt);
            Spring(ref FearOfFalling, ref _vFear, _tFear, dt);
            Spring(ref Fatigue, ref _vFat, _tFat, dt);
            Leak(ref _tC, BaselineCuriosity, dt);
            Leak(ref _tFr, BaselineFrustration, dt);
            Leak(ref _tS, BaselineSatisfaction, dt);
            Leak(ref _tFear, BaselineFear, dt);
            Leak(ref _tFat, BaselineFatigue, dt);
            Curiosity = Clamp01(Curiosity);
            Frustration = Clamp01(Frustration);
            Satisfaction = Clamp01(Satisfaction);
            FearOfFalling = Clamp01(FearOfFalling);
            Fatigue = Clamp01(Fatigue);
        }

        public void Settle(float seconds)
        {
            int steps = Math.Max(1, (int)(seconds / 0.05f));
            for (int i = 0; i < steps; i++) Tick(0.05f);
        }

        public float Valence
        {
            get
            {
                return Clamp01(0.5f + 0.35f * Satisfaction - 0.30f * FearOfFalling
                    - 0.25f * Frustration + 0.10f * Curiosity);
            }
        }

        public float Arousal
        {
            get
            {
                return Clamp01(0.15f + 0.35f * Curiosity + 0.30f * FearOfFalling
                    + 0.25f * Frustration - 0.20f * Fatigue);
            }
        }

        /// <summary>How widely to babble. Rises smoothly with curiosity, falls with fear and fatigue.</summary>
        public float Exploration
        {
            get { return ExplorationFrom(Curiosity, FearOfFalling, Fatigue); }
        }

        public float LearningRate
        {
            get
            {
                return Clamp01(0.12f + 0.50f * Curiosity + 0.22f * Frustration
                    + 0.18f * Satisfaction - 0.45f * Fatigue);
            }
        }

        public float SleepPressure
        {
            get
            {
                return Clamp01(0.72f * Fatigue + 0.18f * (1f - Curiosity) + 0.10f * FearOfFalling);
            }
        }

        public static float ExplorationFrom(float curiosity, float fear, float fatigue)
        {
            return Clamp01(0.08f + 0.85f * curiosity - 0.45f * fear - 0.40f * fatigue);
        }

        public Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "curiosity", Curiosity },
                { "frustration", Frustration },
                { "satisfaction", Satisfaction },
                { "fear", FearOfFalling },
                { "fatigue", Fatigue },
                { "targetCuriosity", _tC },
                { "targetFrustration", _tFr },
                { "targetSatisfaction", _tS },
                { "targetFear", _tFear },
                { "targetFatigue", _tFat },
                { "baselineCuriosity", BaselineCuriosity },
                { "baselineFrustration", BaselineFrustration },
                { "baselineSatisfaction", BaselineSatisfaction },
                { "baselineFear", BaselineFear },
                { "baselineFatigue", BaselineFatigue }
            };
        }

        public void LoadData(Dictionary<string, object> data)
        {
            Curiosity = MiniJson.AsFloat(data["curiosity"]);
            Frustration = MiniJson.AsFloat(data["frustration"]);
            Satisfaction = MiniJson.AsFloat(data["satisfaction"]);
            FearOfFalling = MiniJson.AsFloat(data["fear"]);
            Fatigue = MiniJson.AsFloat(data["fatigue"]);
            _tC = data.ContainsKey("targetCuriosity") ? MiniJson.AsFloat(data["targetCuriosity"]) : Curiosity;
            _tFr = data.ContainsKey("targetFrustration") ? MiniJson.AsFloat(data["targetFrustration"]) : Frustration;
            _tS = data.ContainsKey("targetSatisfaction") ? MiniJson.AsFloat(data["targetSatisfaction"]) : Satisfaction;
            _tFear = data.ContainsKey("targetFear") ? MiniJson.AsFloat(data["targetFear"]) : FearOfFalling;
            _tFat = data.ContainsKey("targetFatigue") ? MiniJson.AsFloat(data["targetFatigue"]) : Fatigue;
            if (data.ContainsKey("baselineCuriosity")) BaselineCuriosity = MiniJson.AsFloat(data["baselineCuriosity"]);
            if (data.ContainsKey("baselineFrustration")) BaselineFrustration = MiniJson.AsFloat(data["baselineFrustration"]);
            if (data.ContainsKey("baselineSatisfaction")) BaselineSatisfaction = MiniJson.AsFloat(data["baselineSatisfaction"]);
            if (data.ContainsKey("baselineFear")) BaselineFear = MiniJson.AsFloat(data["baselineFear"]);
            if (data.ContainsKey("baselineFatigue")) BaselineFatigue = MiniJson.AsFloat(data["baselineFatigue"]);
        }

        static void Spring(ref float value, ref float velocity, float target, float dt)
        {
            const float stiffness = 4.5f;
            const float damping = 3.2f;
            velocity += (target - value) * stiffness * dt - velocity * damping * dt;
            value += velocity * dt;
        }

        static void Leak(ref float target, float baseline, float dt)
        {
            target += (baseline - target) * (1f - (float)Math.Exp(-0.05f * dt));
        }

        public static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
