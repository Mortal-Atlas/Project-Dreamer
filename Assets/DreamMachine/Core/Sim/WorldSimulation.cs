using System;
using System.Collections.Generic;
using System.Diagnostics;
using DreamMachine.Affect;
using DreamMachine.Body;
using DreamMachine.Development;
using DreamMachine.Elements;
using DreamMachine.Libraries;
using DreamMachine.Mind;
using DreamMachine.Physiology;
using DreamMachine.Social;
using DreamMachine.Util;
using DreamMachine.Wetware;
using DreamMachine.World;

namespace DreamMachine.Sim
{
    public enum QualityPreset
    {
        Phone = 0,
        Laptop = 1,
        Beefy = 2
    }

    public enum LifeStage
    {
        Newborn = 0,
        Toddler = 1,
        Child = 2,
        Teen = 3,
        Adult = 4
    }

    public sealed class SimProfile
    {
        public float PhysiologyMs;
        public float ElementsMs;
        public float LearningMs;
        public float OtherMs;
        public int Ticks;
        public float TicksPerSecond;
        public float AchievedScale;
    }

    /// <summary>
    /// One fixed-step tick of the whole household.
    /// Fast-forward calls this same method. It never coarsens the step.
    /// </summary>
    public sealed class WorldSimulation
    {
        public const float Dt = 0.05f;
        public const float DayLength = 52f;
        public const float AwakeLength = 36f;
        public const float HalfExtent = 8f;
        const int LearnEvery = 24;
        const int PerceiveEvery = 16;

        public int TickCount;
        public float SimSeconds;
        public int GridSize;
        public QualityPreset Preset;
        public ElementGrid Elements;
        public PopulationConfig Population;
        public readonly List<SimChild> Children = new List<SimChild>();
        public readonly List<PhysiologySample> PhysiologyLog = new List<PhysiologySample>();
        readonly float[] _parentA = new float[10];
        readonly float[] _parentB = new float[10];
        readonly float[] _wetIn = new float[4];
        int _lastLoggedDay = -1;

        public bool IsNight
        {
            get { return (SimSeconds % DayLength) >= AwakeLength; }
        }

        public int DayIndex
        {
            get { return (int)(SimSeconds / DayLength); }
        }

        public float MinAgeDays()
        {
            float age = 999f;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Body.AgeDays < age) age = Children[i].Body.AgeDays;
            }
            return Children.Count == 0 ? 0f : age;
        }

        public static float AgeOf(LifeStage stage)
        {
            if (stage == LifeStage.Toddler) return 0.2f;
            if (stage == LifeStage.Child) return 1f;
            if (stage == LifeStage.Teen) return 3f;
            if (stage == LifeStage.Adult) return HumanoidAnatomy.AdultAgeDays;
            return 0f;
        }

        public static float AnatomyAge(float chronologicalDays) =>
            HumanoidAnatomy.SleepAge(chronologicalDays, AwakeLength / DayLength);

        public static int GridFor(QualityPreset preset)
        {
            if (preset == QualityPreset.Beefy) return 24;
            if (preset == QualityPreset.Laptop) return 16;
            return 12;
        }

        public static WorldSimulation Create(PopulationConfig population, QualityPreset preset, int worldSeed)
            => Create(population, preset, worldSeed, null);

        public static WorldSimulation Create(PopulationConfig population, QualityPreset preset, int worldSeed,
            Func<PersonSpec, AgentPersistentState> restore)
        {
            var world = new WorldSimulation();
            world.Population = population;
            world.Preset = preset;
            world.GridSize = GridFor(preset);
            world.Elements = ElementGrid.CreateNursery(world.GridSize, world.GridSize, worldSeed);
            List<PersonSpec> babies = population.Babies();
            for (int i = 0; i < babies.Count; i++)
            {
                PersonSpec spec = babies[i];
                var child = new SimChild();
                child.Spec = spec;
                AgentPersistentState saved = restore?.Invoke(spec);
                if (saved != null && (saved.AgentId != spec.Id || saved.BirthSeed != spec.BirthSeed))
                    throw new System.IO.InvalidDataException("Save identity or birth seed does not match " + spec.Id + ".");
                BindChildState(child, saved ?? AgentPersistentState.CreateNew(spec));
                child.X = spec.Id == "daughter" ? 0.7f : -0.7f;
                child.Z = 0.4f;
                world.Children.Add(child);
            }
            return world;
        }

        static void BindChildState(SimChild child, AgentPersistentState state)
        {
            child.State = state;
            child.Body = state.Physiology;
            child.Mind = new AgentMind(child.Spec.BirthSeed, MindCondition.Full) { Library = state.Library };
            child.Wetware = new HebbianWetware(4, 3, child.Spec.BirthSeed);
            state.Walker.RecordRolloutRom = false;
        }

        public SimChild ResetChild(string id)
        {
            SimChild child = Find(id);
            if (child == null) return null;
            BindChildState(child, AgentPersistentState.CreateNew(child.Spec));
            child.SawParentPour = false;
            return child;
        }

        public SimChild Find(string id)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Spec.Id == id) return Children[i];
            }
            return null;
        }

        public void Tick()
        {
            Elements.Step(Dt);
            bool night = IsNight;
            FamilyRoutine routine = PopulationConfig.RoutineAt(SimSeconds, DayLength, AwakeLength);
            string parentClip = ClipFor(routine, night);
            PersonSpec father = Population.Find("father");
            PersonSpec mother = Population.Find("mother");
            int fatherStyle = father != null ? father.BirthSeed : 1;
            int motherStyle = mother != null ? mother.BirthSeed : 2;
            MentorMotion.WriteAngles(parentClip, SimSeconds, fatherStyle, _parentA);
            MentorMotion.WriteAngles(parentClip, SimSeconds, motherStyle, _parentB);
            for (int i = 0; i < Children.Count; i++)
            {
                StepChild(Children[i], night, routine);
            }
            SimSeconds += Dt;
            TickCount++;
            int day = DayIndex;
            if (day != _lastLoggedDay)
            {
                _lastLoggedDay = day;
                for (int i = 0; i < Children.Count; i++)
                {
                    PhysiologyLog.Add(PhysiologySample.From(Children[i], day, night ? "night" : "awake"));
                }
            }
        }

        public int RunTicks(int count)
        {
            int n = 0;
            while (n < count)
            {
                Tick();
                n++;
            }
            return n;
        }

        public SimProfile RunBudget(int hardCap, double budgetMs, float realDt, float requestedScale, bool catchUp, float catchAge)
        {
            var profile = new SimProfile();
            var clock = Stopwatch.StartNew();
            int n = 0;
            while (n < hardCap && clock.Elapsed.TotalMilliseconds < budgetMs)
            {
                if (catchUp && MinAgeDays() >= catchAge) break;
                Tick();
                n++;
            }
            clock.Stop();
            profile.Ticks = n;
            double seconds = clock.Elapsed.TotalSeconds;
            profile.TicksPerSecond = seconds > 0 ? (float)(n / seconds) : 0f;
            profile.AchievedScale = realDt > 0.000001f ? (float)((n * Dt) / realDt) : 0f;
            if (!catchUp && requestedScale > 0f && profile.AchievedScale > requestedScale)
            {
                profile.AchievedScale = requestedScale;
            }
            return profile;
        }

        /// <summary>
        /// Full ticks until every child reaches the age. Stops at maxTicks so a frame can yield.
        /// The next call continues. There is no coarse step.
        /// </summary>
        public int RunUntilAge(float ageDays, int maxTicks)
        {
            int n = 0;
            while (n < maxTicks && MinAgeDays() < ageDays)
            {
                Tick();
                n++;
            }
            return n;
        }

        public string StateHash()
        {
            unchecked
            {
                int h = unchecked((int)2166136261);
                Mix(ref h, TickCount);
                Mix(ref h, PhysiologyState.Q(SimSeconds));
                float temp, fuel, water, smoke, steam;
                int burning;
                Elements.Sum(out temp, out fuel, out water, out smoke, out steam, out burning);
                Mix(ref h, PhysiologyState.Q(temp));
                Mix(ref h, PhysiologyState.Q(fuel));
                Mix(ref h, PhysiologyState.Q(water));
                Mix(ref h, PhysiologyState.Q(smoke));
                Mix(ref h, PhysiologyState.Q(steam));
                Mix(ref h, burning);
                for (int i = 0; i < Children.Count; i++)
                {
                    SimChild child = Children[i];
                    h = child.Body.Fold(h);
                    Mix(ref h, PhysiologyState.Q(child.X));
                    Mix(ref h, PhysiologyState.Q(child.Z));
                    Mix(ref h, child.State.Library.Count);
                    Mix(ref h, PhysiologyState.Q(child.State.Walker.Genome[0]));
                    Mix(ref h, PhysiologyState.Q(child.State.Walker.BestDistance));
                }
                return h.ToString("x8");
            }
        }

        public void WritePhysiologyCsv(string path)
        {
            var header = new List<string>
            {
                "day", "agent_id", "phase", "age_days", "maturity", "heart_rate", "breathing_rate",
                "energy", "hunger", "cortisol", "oxytocin", "melatonin", "pain", "mean_activation",
                "mean_force", "mean_fatigue", "walk_force_scale", "nerve_delay_ms"
            };
            var rows = new List<IList<string>>();
            for (int i = 0; i < PhysiologyLog.Count; i++)
            {
                PhysiologySample s = PhysiologyLog[i];
                rows.Add(new List<string>
                {
                    s.Day.ToString(), s.AgentId, s.Phase,
                    CsvTable.Format(s.AgeDays), CsvTable.Format(s.Maturity),
                    CsvTable.Format(s.HeartRate), CsvTable.Format(s.BreathingRate),
                    CsvTable.Format(s.Energy), CsvTable.Format(s.Hunger),
                    CsvTable.Format(s.Cortisol), CsvTable.Format(s.Oxytocin), CsvTable.Format(s.Melatonin),
                    CsvTable.Format(s.Pain), CsvTable.Format(s.MeanActivation),
                    CsvTable.Format(s.MeanForce), CsvTable.Format(s.MeanFatigue),
                    CsvTable.Format(s.WalkForceScale), CsvTable.Format(s.NerveDelaySeconds * 1000f)
                });
            }
            CsvTable.Write(path, header, rows);
        }

        void StepChild(SimChild child, bool night, FamilyRoutine routine)
        {
            int cx, cy;
            Elements.CellAt(child.X, child.Z, HalfExtent, out cx, out cy);
            int cell = Elements.Index(cx, cy);
            float temp = Elements.Temperature[cell];
            float warmth = (temp - 18f) / 50f;
            if (warmth < 0f) warmth = 0f;
            if (warmth > 1f) warmth = 1f;
            float pain = 0f;
            if (temp > 48f) pain = (temp - 48f) / 80f;
            if (Elements.Burning[cell] != 0) pain += 0.65f;
            if (pain > 1f) pain = 1f;
            float smoke = Elements.Smoke[cell];
            if (smoke > 1f) smoke = 1f;
            child.State.Walker.WritePose(SimSeconds, child.Pose);
            _wetIn[0] = child.State.Walker.BestDistance;
            _wetIn[1] = child.State.Emotions.Curiosity;
            _wetIn[2] = child.State.Emotions.FearOfFalling;
            _wetIn[3] = smoke;
            child.Wetware.Step(Dt, _wetIn);
            float[] brain = child.Wetware.Readout();
            bool meal = routine == FamilyRoutine.MorningMeal || routine == FamilyRoutine.EveningMeal;
            bool eat = meal && child.Body.Hunger > 0.35f && !night;
            PersonSpec same = Population.PrimaryRoleModel(child.Spec);
            PersonSpec other = Population.OtherParent(child.Spec);
            var input = new PhysInput
            {
                Pain = pain,
                Warmth = warmth,
                Smoke = smoke,
                BondSameParent = SocialAttention.BondParentChild(child.Spec, same),
                BondOtherParent = SocialAttention.BondParentChild(child.Spec, other),
                BondSibling = SocialAttention.BondSibling(),
                CoupleWarmth = SocialAttention.BondCouple(),
                Arousal = child.State.Emotions.Arousal,
                Exertion = child.Body.MeanActivation,
                Night = night,
                Eating = eat,
                JointAngles = child.Pose,
                Descending = brain
            };
            child.Body.Step(input, Dt, child.State.Emotions);
            child.State.Emotions.Tick(Dt);
            child.Body.AdvanceAge(Dt, DayLength);
            if (pain > 0.4f)
            {
                int burn = Elements.NearestBurning(cx, cy, 3);
                if (burn >= 0) Elements.Pour(burn, 0.35f);
                child.SawParentPour = true;
            }
            if ((TickCount % LearnEvery) == 0)
            {
                if (night) Dream(child);
                else Practice(child, same, other);
            }
            if ((TickCount % PerceiveEvery) == 0) Perceive(child, pain, smoke, cell);
            if ((TickCount % 40) == 0) child.Body.WriteRom(child.State.Muscles, DayIndex);
            if (!night && routine == FamilyRoutine.Play)
            {
                float speed = child.State.Walker.BestDistance / 4f;
                if (speed < 0f) speed = 0f;
                if (speed > 0.45f) speed = 0.45f;
                speed *= child.Body.WalkForceScale;
                child.Yaw = child.Spec.Id == "daughter" ? 40f : -30f;
                float rad = child.Yaw * 0.0174533f;
                child.X += (float)Math.Sin(rad) * speed * Dt;
                child.Z += (float)Math.Cos(rad) * speed * Dt;
            }
            if (child.X > 7.5f) child.X = 7.5f;
            if (child.X < -7.5f) child.X = -7.5f;
            if (child.Z > 7.5f) child.Z = 7.5f;
            if (child.Z < -7.5f) child.Z = -7.5f;
        }

        void Practice(SimChild child, PersonSpec same, PersonSpec other)
        {
            float[] primary = same != null && same.Sex == Sex.Female ? _parentB : _parentA;
            float[] secondary = primary == _parentA ? _parentB : _parentA;
            child.State.Walker.NudgeToward(primary, 0.22f * child.State.Emotions.LearningRate + 0.04f);
            child.State.Walker.NudgeToward(secondary, 0.1f);
            SimChild sib = Sibling(child);
            if (sib != null) child.State.Walker.NudgeToward(sib.Pose, 0.06f);
            var rng = new SeededRandom(child.Spec.BirthSeed + TickCount * 17 + 3);
            float explore = child.State.Emotions.Exploration;
            if (child.Body.Energy < 0.25f) explore *= 0.5f;
            child.State.Walker.Practice(1, explore, rng, null, 0f, child.Body.WalkForceScale);
            child.Wetware.ApplyPlasticity(0.2f);
        }

        void Dream(SimChild child)
        {
            float[] primary = child.Spec.Sex == Sex.Female ? _parentB : _parentA;
            child.State.Walker.NudgeToward(primary, 0.08f);
            if (child.State.Observations.Count > 0 && (TickCount % 48) == 0)
            {
                DreamMachine.Vision.VisualObservation last = child.State.Observations[child.State.Observations.Count - 1];
                child.State.Library.NoteIdea("dream:" + last.ObjectId, "safe", 0.12f, DayIndex, "library-II-dream");
            }
        }

        void Perceive(SimChild child, float pain, float smoke, int cell)
        {
            if (Elements.Burning[cell] != 0 || pain > 0.35f)
            {
                child.State.Library.Observe("class:fire", true, TickCount, "heat and pain at the fire", "lived-experience");
                Remember(child, "fire", "class:fire");
            }
            if (Elements.Water[cell] > 0.2f)
            {
                child.State.Library.Observe("class:water", false, TickCount, "water on the ground", "lived-experience");
                if (child.SawParentPour)
                {
                    child.State.Library.NoteIdea("water-puts-out-fire", "safe", 0.4f, TickCount, "lived-experience");
                }
                Remember(child, "water", "class:water");
            }
            if (smoke > 0.15f) Remember(child, "smoke", "class:smoke");
            if (Elements.Steam[cell] > 0.1f) Remember(child, "steam", "class:steam");
            if ((TickCount % 48) == 0 && child.Mind != null)
            {
                var pool = HiddenRuleWorld.StandardPool();
                int pick = (TickCount + child.Spec.BirthSeed) % pool.Count;
                var world = new HiddenRuleWorld { ShiftAt = 400 };
                TrialDecision decision = child.Mind.Encounter(pool[pick], TickCount / 48, world);
                child.State.BeliefsTried++;
                if (decision.Correct) child.State.BeliefsCorrect++;
                Remember(child, pool[pick].Id, pool[pick].CategoryLabel);
            }
        }

        static void Remember(SimChild child, string id, string category)
        {
            if (child.State.Observations.Count > 48) child.State.Observations.RemoveAt(0);
            child.State.Observations.Add(new DreamMachine.Vision.VisualObservation
            {
                ObjectId = id,
                Category = category,
                Appearance = category,
                Day = 0
            });
        }

        SimChild Sibling(SimChild child)
        {
            string id = child.Spec.Id == "son" ? "daughter" : child.Spec.Id == "daughter" ? "son" : null;
            return id == null ? null : Find(id);
        }

        static string ClipFor(FamilyRoutine routine, bool night)
        {
            if (night || routine == FamilyRoutine.Bedtime) return "sit";
            if (routine == FamilyRoutine.MorningMeal || routine == FamilyRoutine.EveningMeal) return "sit";
            if (routine == FamilyRoutine.Play) return "walk";
            return "idle";
        }

        static void Mix(ref int h, int v)
        {
            unchecked
            {
                h = (h ^ v) * 16777619;
            }
        }
    }

    public sealed class SimChild
    {
        public PersonSpec Spec;
        public AgentPersistentState State;
        public AgentMind Mind;
        public HebbianWetware Wetware;
        public PhysiologyState Body;
        public float X;
        public float Z;
        public float Yaw;
        public bool SawParentPour;
        public readonly float[] Pose = new float[10];

        public SimChild()
        {
        }
    }

    public sealed class PhysiologySample
    {
        public int Day;
        public string AgentId;
        public string Phase;
        public float AgeDays;
        public float Maturity;
        public float HeartRate;
        public float BreathingRate;
        public float Energy;
        public float Hunger;
        public float Cortisol;
        public float Oxytocin;
        public float Melatonin;
        public float Pain;
        public float MeanActivation;
        public float MeanForce;
        public float MeanFatigue;
        public float WalkForceScale;
        public float NerveDelaySeconds;

        public static PhysiologySample From(SimChild child, int day, string phase)
        {
            PhysiologyState body = child.Body;
            return new PhysiologySample
            {
                Day = day,
                AgentId = child.Spec.Id,
                Phase = phase,
                AgeDays = body.AgeDays,
                Maturity = body.Maturity,
                HeartRate = body.HeartRate,
                BreathingRate = body.BreathingRate,
                Energy = body.Energy,
                Hunger = body.Hunger,
                Cortisol = body.Cortisol,
                Oxytocin = body.Oxytocin,
                Melatonin = body.Melatonin,
                Pain = body.Pain,
                MeanActivation = body.MeanActivation,
                MeanForce = body.MeanForce,
                MeanFatigue = body.MeanFatigue,
                WalkForceScale = body.WalkForceScale,
                NerveDelaySeconds = body.NerveDelaySeconds
            };
        }
    }

    public sealed class TimeGovernor
    {
        public float TargetScale = 1f;
        public float Accumulator;
        public float AchievedScale;
        public bool Paused;
        public bool CatchingUp;
        public LifeStage CatchStage;
        public const int HardCap = 4000;

        public int Plan(float realDt, float fixedDt, int budgetTicks)
        {
            if (Paused && !CatchingUp)
            {
                AchievedScale = 0f;
                return 0;
            }
            if (CatchingUp) return budgetTicks;
            if (TargetScale <= 0f)
            {
                AchievedScale = 0f;
                return 0;
            }
            Accumulator += realDt * TargetScale;
            float cap = budgetTicks * fixedDt;
            if (Accumulator > cap) Accumulator = cap;
            int ticks = 0;
            if (fixedDt > 0f) ticks = (int)(Accumulator / fixedDt);
            if (ticks > budgetTicks) ticks = budgetTicks;
            if (ticks < 0) ticks = 0;
            Accumulator -= ticks * fixedDt;
            if (Accumulator < 0f) Accumulator = 0f;
            AchievedScale = realDt > 0.000001f ? (ticks * fixedDt) / realDt : 0f;
            return ticks;
        }
    }
}
