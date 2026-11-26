using System;
using System.Collections.Generic;
using System.IO;
using DreamMachine.Affect;
using DreamMachine.Elements;
using DreamMachine.Physiology;
using DreamMachine.Sim;
using DreamMachine.Body;
using DreamMachine.Development;
using DreamMachine.Experiment;
using DreamMachine.Libraries;
using DreamMachine.Mind;
using DreamMachine.Social;
using DreamMachine.Util;
using DreamMachine.Vision;
using DreamMachine.Wetware;
using DreamMachine.World;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class SeedAndAtlasTests
    {
        [Test]
        public void SameSeed_SameNumbers()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(a.NextFloat(), b.NextFloat());
            }
            Assert.AreNotEqual(new SeededRandom(1).NextFloat(), new SeededRandom(2).NextFloat());
        }

        [Test]
        public void Atlas_DependsOnBirthSeed_AndCoordinateLandsInside()
        {
            PossibilityAtlas a = PossibilityAtlas.FromBirth(7);
            PossibilityAtlas b = PossibilityAtlas.FromBirth(7);
            PossibilityAtlas c = PossibilityAtlas.FromBirth(8);
            Assert.AreEqual(a.Options.Count, b.Options.Count);
            for (int i = 0; i < a.Options.Count; i++)
            {
                Assert.AreEqual(a.Options[i].Predicate, b.Options[i].Predicate);
                Assert.AreEqual(a.Options[i].Thinkable, b.Options[i].Thinkable);
            }
            Assert.IsTrue(a.Allows("color:Red"));
            DecisionCoordinate coord = a.Coordinate("color:Red", 0.25f);
            Assert.GreaterOrEqual(coord.Index, 0);
            Assert.AreEqual("color:Red", a.Options[coord.Index].Predicate);
            bool differ = false;
            for (int i = 0; i < a.Options.Count; i++)
            {
                if (a.Options[i].Thinkable != c.Options[i].Thinkable || Math.Abs(a.Options[i].Weight - c.Options[i].Weight) > 0.0001f)
                {
                    differ = true;
                    break;
                }
            }
            Assert.IsTrue(differ);
        }
    }

    public class LibraryTests
    {
        [Test]
        public void Beliefs_AreAmended_NeverDeleted()
        {
            var lib = new LibraryThree();
            lib.Observe("color:Red", true, 1, "touched obj-1", "lived-experience");
            int afterFirst = lib.Count;
            int apps = lib.AppendixCount;
            Assert.AreEqual(1, afterFirst);
            Assert.GreaterOrEqual(apps, 1);
            for (int i = 0; i < 12; i++)
            {
                lib.Observe("color:Red", i > 6, i + 2, "again", "lived-experience");
            }
            lib.Observe("shape:Cube", false, 20, "cube was safe", "lived-experience");
            Assert.GreaterOrEqual(lib.Count, afterFirst);
            Assert.IsNotNull(lib.GetOrNull("color:Red"));
            Assert.Greater(lib.AppendixCount, apps);
            bool amended = false;
            foreach (AppendixEntry entry in lib.GetOrNull("color:Red").Appendices)
            {
                if (entry.OldStatement.Length > 0 && entry.OldStatement != entry.NewStatement)
                {
                    amended = true;
                }
            }
            Assert.IsTrue(amended);
        }

        [Test]
        public void Library_RoundTrips_ThroughJson()
        {
            var lib = new LibraryThree();
            lib.Observe("mark:Spot", true, 3, "spot stung", "lived-experience");
            lib.Observe("mark:Spot", true, 4, "spot stung again", "lived-experience");
            string json = lib.ToJson();
            LibraryThree copy = LibraryThree.FromJson(json);
            Assert.AreEqual(lib.Count, copy.Count);
            BeliefRecord original = lib.GetOrNull("mark:Spot");
            BeliefRecord loaded = copy.GetOrNull("mark:Spot");
            Assert.AreEqual(original.Statement, loaded.Statement);
            Assert.AreEqual(original.Appendices.Count, loaded.Appendices.Count);
            Assert.AreEqual(original.Appendices[0].Evidence, loaded.Appendices[0].Evidence);
            Assert.AreEqual(original.Confidence, loaded.Confidence, 0.0001f);
        }

        [Test]
        public void StatisticalPrior_IsBetweenZeroAndOne_AndHookIsSeparate()
        {
            var stats = new StatisticalLibraryOne();
            var obj = new ObjectInstance { Id = "obj-1", Color = "Red", Shape = "Sphere", Mark = "Spot" };
            float prior = stats.PriorHarm(obj);
            Assert.Greater(prior, 0f);
            Assert.Less(prior, 1f);
            var hook = new LlmLibraryOneHook();
            Assert.AreEqual("llm-hook", hook.Name);
            Assert.Throws<InvalidOperationException>(delegate { hook.PriorHarm(obj); });
            hook.Transport = delegate { return "0.25"; };
            Assert.AreEqual(0.25f, hook.PriorHarm(obj), 0.0001f);
        }
    }

    public class ExperimentTests
    {
        [Test]
        public void FullMind_BecomesMoreAccurate_ThanPriorOnly()
        {
            var experiment = new BeliefExperiment();
            ExperimentReport report = experiment.Run(ExperimentConfig.Standard());
            float priorBefore = report.MeanAccuracy(MindCondition.LibraryOneOnly, 20, 40);
            float fullBefore = report.MeanAccuracy(MindCondition.Full, 20, 40);
            float priorAfter = report.MeanAccuracy(MindCondition.LibraryOneOnly, 60, 80);
            float fullAfter = report.MeanAccuracy(MindCondition.Full, 60, 80);
            Assert.Greater(fullBefore, priorBefore + 0.04f);
            Assert.Greater(fullAfter, priorAfter + 0.08f);
            Assert.IsTrue(report.Verdict().StartsWith("SUPPORTS"));
        }

        [Test]
        public void RuleShift_ProducesAmendments_AndInstanceCanOverrideCategory()
        {
            var world = new HiddenRuleWorld { ShiftAt = 40 };
            var mind = new AgentMind(3, MindCondition.Full);
            List<ObjectInstance> pool = HiddenRuleWorld.StandardPool();
            var rng = new SeededRandom(3);
            for (int t = 0; t < 70; t++)
            {
                mind.Encounter(pool[rng.NextInt(pool.Count)], t, world);
            }
            int flips = 0;
            foreach (BeliefRecord belief in mind.Library.All())
            {
                for (int i = 0; i < belief.Appendices.Count; i++)
                {
                    AppendixEntry entry = belief.Appendices[i];
                    if (entry.Timestamp >= 40 && entry.OldStatement.Length > 0 && entry.OldStatement != entry.NewStatement)
                    {
                        flips++;
                    }
                }
            }
            Assert.Greater(flips, 0);
            Assert.Greater(mind.Library.Count, 0);

            var particular = new AgentMind(9, MindCondition.Full);
            var exception = new ObjectInstance { Id = "obj-11", Color = "Blue", Shape = "Sphere", Mark = "None" };
            var ordinary = new ObjectInstance { Id = "obj-3", Color = "Blue", Shape = "Sphere", Mark = "None" };
            for (int t = 0; t < 8; t++)
            {
                particular.Encounter(exception, t, world);
                particular.Encounter(ordinary, t, world);
            }
            TrialDecision againEx = particular.Encounter(exception, 8, world);
            TrialDecision againOrd = particular.Encounter(ordinary, 9, world);
            Assert.AreNotEqual(againEx.PredictedHarm, againOrd.PredictedHarm);
            Assert.IsTrue(againEx.Hypothesis.StartsWith("object:"));
        }

        [Test]
        public void Conditions_A_And_B_DoNotAccumulateBeliefs()
        {
            var world = new HiddenRuleWorld();
            var pool = HiddenRuleWorld.StandardPool();
            var a = new AgentMind(1, MindCondition.LibraryOneOnly);
            var b = new AgentMind(1, MindCondition.LibraryOneAndTwo);
            for (int t = 0; t < 15; t++)
            {
                a.Encounter(pool[t % pool.Count], t, world);
                b.Encounter(pool[t % pool.Count], t, world);
            }
            Assert.AreEqual(0, a.Library.Count);
            Assert.AreEqual(0, b.Library.Count);
        }
    }

    public class EmotionTests
    {
        [Test]
        public void Emotions_MoveContinuously_Blend_AndDecay()
        {
            var mood = new EmotionState(4);
            float start = mood.Curiosity;
            mood.Excite(0.9f, 0f, 0f, 0f, 0f);
            mood.Tick(0.05f);
            float nudged = mood.Curiosity;
            Assert.Greater(nudged, start);
            Assert.Less(nudged - start, 0.2f);
            mood.Tick(0.25f);
            Assert.Greater(mood.Curiosity, nudged);
            Assert.Less(mood.Curiosity, 0.98f);
            mood.Settle(3f);
            Assert.Greater(mood.Curiosity, 0.65f);
            mood.Excite(0.4f, 0f, 0f, 0.85f, 0f);
            mood.Settle(1.2f);
            Assert.Greater(mood.Curiosity, 0.45f);
            Assert.Greater(mood.FearOfFalling, 0.45f);
            mood.Settle(40f);
            Assert.Less(mood.FearOfFalling, 0.35f);
        }

        [Test]
        public void Exploration_ScalesWithCuriosity_NotASwitch()
        {
            float low = EmotionState.ExplorationFrom(0.2f, 0.15f, 0.1f);
            float mid = EmotionState.ExplorationFrom(0.55f, 0.15f, 0.1f);
            float high = EmotionState.ExplorationFrom(0.9f, 0.15f, 0.1f);
            Assert.Greater(mid, low + 0.05f);
            Assert.Greater(high, mid + 0.05f);
            var mood = new EmotionState();
            mood.Rest();
            float before = mood.Exploration;
            mood.Excite(0.4f, 0f, 0f, 0f, 0f);
            mood.Settle(1.2f);
            Assert.Greater(mood.Exploration, before);
            Assert.AreEqual(
                EmotionState.ExplorationFrom(mood.Curiosity, mood.FearOfFalling, mood.Fatigue),
                mood.Exploration,
                0.0001f);
        }
    }

    public class SocietyTests
    {
        [Test]
        public void DefaultPopulation_IsPaired_Sexed_AndHierarchical()
        {
            PopulationConfig pop = PopulationConfig.CreateDefault();
            Assert.IsTrue(pop.IsHousehold);
            Assert.AreEqual(2, pop.BabyCount);
            Assert.AreEqual(2, pop.MentorCount);
            Assert.AreEqual("Father", pop.Find("father").DisplayName);
            Assert.AreEqual("Mother", pop.Find("mother").DisplayName);
            Assert.AreEqual("Son", pop.Find("son").DisplayName);
            Assert.AreEqual("Daughter", pop.Find("daughter").DisplayName);
            Assert.IsTrue(pop.Find("father").IsLeader && pop.Find("father").Sex == Sex.Male);
            Assert.IsTrue(pop.Find("mother").IsLeader && pop.Find("mother").Sex == Sex.Female);
            Assert.AreEqual(GroupStyle.Household, pop.Find("father").Style);
            PersonSpec son = pop.Find("son");
            PersonSpec daughter = pop.Find("daughter");
            Assert.AreEqual("father", pop.PrimaryRoleModel(son).Id);
            Assert.AreEqual("mother", pop.PrimaryRoleModel(daughter).Id);
            Assert.Greater(SocialAttention.RoleModelWeight(son, pop.Find("father")), SocialAttention.RoleModelWeight(son, pop.Find("mother")));
            Assert.Greater(SocialAttention.RoleModelWeight(son, pop.Find("mother")), SocialAttention.RoleModelWeight(son, daughter));
            Assert.Greater(SocialAttention.RoleModelWeight(daughter, pop.Find("mother")), SocialAttention.RoleModelWeight(daughter, pop.Find("father")));
            Assert.AreEqual(SocialAttention.BondCouple(), 0.9f, 0.001f);
            Assert.Greater(SocialAttention.BondParentChild(son, pop.Find("father")), SocialAttention.BondParentChild(son, pop.Find("mother")));
            string renamed = "{\"members\":[{\"id\":\"father\",\"name\":\"Father\"},{\"id\":\"mother\",\"name\":\"Mother\"},{\"id\":\"son\",\"name\":\"Son\"},{\"id\":\"daughter\",\"name\":\"Daughter\"}]}";
            PopulationConfig loaded = PopulationConfig.FromJson(renamed);
            Assert.AreEqual("Daughter", loaded.Find("daughter").DisplayName);
            Assert.AreEqual(FamilyRoutine.Bedtime, PopulationConfig.RoutineAt(40f, 52f, 36f));
            Assert.AreEqual(FamilyRoutine.Play, PopulationConfig.RoutineAt(10f, 52f, 36f));
        }

        [Test]
        public void UnequalCounts_AreRejected()
        {
            PopulationConfig pop = PopulationConfig.CreateDefault();
            pop.People.RemoveAt(pop.People.Count - 1);
            Assert.Throws<InvalidOperationException>(delegate { pop.Validate(); });
        }

        [Test]
        public void Looks_AreDistinct_AndLeadersStandOut()
        {
            List<AppearanceProfile> looks = AppearanceCatalog.Defaults(PopulationConfig.CreateDefault());
            Assert.AreEqual(4, looks.Count);
            var cloth = new HashSet<string>();
            foreach (AppearanceProfile look in looks)
            {
                cloth.Add(look.Cloth);
                Assert.IsFalse(string.IsNullOrEmpty(look.HairStyle));
                if (look.IsLeader)
                {
                    Assert.AreEqual("circlet", look.Marker);
                    Assert.Greater(look.Height, 1.05f);
                }
            }
            Assert.GreaterOrEqual(cloth.Count, 3);
            List<AppearanceProfile> loaded = AppearanceCatalog.FromJson(AppearanceCatalog.ToJson(looks));
            Assert.AreEqual("Mother", loaded.Find(delegate (AppearanceProfile p) { return p.Id == "mother"; }).DisplayName);
            Assert.IsTrue(loaded.Find(delegate (AppearanceProfile p) { return p.Id == "father"; }).IsLeader);
            Assert.IsTrue(loaded.Find(delegate (AppearanceProfile p) { return p.Id == "mother"; }).IsLeader);
        }
    }

    public class BodyTests
    {
        [Test]
        public void Walking_Improves_AndDiffersBySeed()
        {
            var a = new WalkLearner(1);
            var b = new WalkLearner(2);
            float start = a.InitialDistance;
            a.Practice(30, 1f, new SeededRandom(99), null, 0f);
            Assert.Greater(a.BestDistance, start + 0.4f);
            Assert.AreNotEqual(a.Signature(), b.Signature());
            a.Babble(new SeededRandom(5), 4);
            Assert.Greater(a.Memory.Count, 4);
            int before = a.Memory.GetOrNull("rom:leftHip").Appendices.Count;
            a.Memory.NoteRange("rom:leftHip", -1.4f, 1.4f, 2, "wider reach");
            Assert.GreaterOrEqual(a.Memory.Count, before);
            Assert.IsNotNull(a.Memory.GetOrNull("rom:leftHip"));
            Assert.Greater(a.Memory.GetOrNull("rom:leftHip").Appendices.Count, before);
        }

        [Test]
        public void Imitation_UsesObservedAngles_AndMovesTowardThem()
        {
            PoseTrace trace = MentorMotion.Record("father", "walk", 1.2f, 0.1f, 3);
            Assert.Greater(trace.Frames.Count, 4);
            Assert.IsNull(trace.GetType().GetField("Style"));
            var baby = new WalkLearner(11);
            var mapper = new PoseMapper(11);
            PoseTrace seen = mapper.MapTrace(trace);
            float before = baby.Similarity(seen);
            baby.BlendToward(seen, 0.95f);
            float after = baby.Similarity(seen);
            Assert.Greater(after, before + 0.05f);
        }
    }

    public class WetwareTests
    {
        [Test]
        public void Hebbian_AndLif_Respond()
        {
            var hebb = new HebbianWetware(3, 2, 5);
            WetwareSnapshot before = hebb.Snapshot();
            hebb.Step(0.1f, new[] { 1f, 0.2f, -0.4f });
            hebb.ApplyPlasticity(1f);
            WetwareSnapshot after = hebb.Snapshot();
            bool changed = false;
            for (int i = 0; i < before.Weights.Length; i++)
            {
                if (Math.Abs(before.Weights[i] - after.Weights[i]) > 0.000001f) changed = true;
            }
            Assert.IsTrue(changed);
            float[] rates = hebb.Readout();
            Assert.AreEqual(2, rates.Length);

            var lif = new LeakyIntegrateFireWetware(4, 5);
            bool spiked = false;
            for (int i = 0; i < 30; i++)
            {
                lif.Step(0.01f, new[] { 1f, 1f, 1f });
                float[] spikes = lif.Readout();
                for (int s = 0; s < spikes.Length; s++) if (spikes[s] > 0.5f) spiked = true;
            }
            Assert.IsTrue(spiked);
        }

        [Test]
        public void ReplayFile_ReadsRows()
        {
            string csv = "t,n0,n1,n2,n3,s0,s1,s2,s3\n0,0.1,0.2,0.3,0.4,0,1,0,0\n0.1,0.5,0.6,0.7,0.8,1,0,0,0\n";
            var replay = new ReplayFileWetware();
            replay.LoadText(csv);
            float[] first = replay.Readout();
            Assert.AreEqual(0.1f, first[0], 0.0001f);
            replay.Step(0.01f, null);
            Assert.AreEqual(0.5f, replay.Readout()[0], 0.0001f);
        }
    }

    public class VisionAndGrowthTests
    {
        [Test]
        public void MaskDecoder_FindsOneObject_AndDreamsOnlyReplayIt()
        {
            int w = 8, h = 6;
            var pixels = new byte[w * h * 4];
            for (int y = 1; y <= 3; y++)
            {
                for (int x = 2; x <= 5; x++) IdMaskDecoder.WriteId(pixels, w, x, y, 11);
            }
            List<MaskRegion> regions = IdMaskDecoder.Decode(pixels, w, h);
            Assert.AreEqual(1, regions.Count);
            Assert.AreEqual(11, regions[0].NumericId);
            Assert.AreEqual(2, regions[0].MinX);
            Assert.AreEqual(5, regions[0].MaxX);
            Assert.AreEqual(12, regions[0].PixelCount);

            var memory = new List<VisualObservation>
            {
                new VisualObservation { ObjectId = "obj-2", Color = "Blue", Shape = "Cube", WorldX = 1f, WorldZ = 2f },
                new VisualObservation { ObjectId = "obj-2", Color = "Blue", Shape = "Cube", WorldX = 1f, WorldZ = 2f },
                new VisualObservation { ObjectId = "obj-5", Color = "Green", Shape = "Capsule", WorldX = -1f, WorldZ = 0.5f }
            };
            List<DreamProp> dream = new DreamRecombinator().Remix(memory, new LibraryTwo(), new SeededRandom(2));
            Assert.AreEqual(2, dream.Count);
            foreach (DreamProp prop in dream)
            {
                Assert.IsTrue(prop.SourceObjectId == "obj-2" || prop.SourceObjectId == "obj-5");
            }
        }

        [Test]
        public void Nursery_GrowsOneModel_AndRoundTrips()
        {
            NurserySimulator nursery = NurserySimulator.Create(PopulationConfig.CreateDefault(), new NurseryOptions
            {
                WalkIterations = 6,
                ObjectEncounters = 2,
                PerformanceMode = true
            });
            nursery.RunDays(1);
            AgentPersistentState ada = nursery.Babies["daughter"];
            Assert.Greater(ada.Library.Count, 0);
            Assert.Greater(ada.Observations.Count, 0);
            Assert.Greater(nursery.Growth.Count, 0);
            Assert.AreEqual("awake", nursery.Growth[0].Phase);
            GrowthSample night = nursery.Growth[nursery.Growth.Count - 1];
            Assert.AreEqual("night", night.Phase);
            Assert.GreaterOrEqual(night.Curiosity, 0f);
            Assert.LessOrEqual(night.Curiosity, 1f);
            string json = ada.ToJson();
            AgentPersistentState loaded = AgentPersistentState.FromJson(json, true);
            Assert.AreEqual(ada.Generation + 1, loaded.Generation);
            Assert.AreEqual(ada.Walker.BestDistance, loaded.Walker.BestDistance, 0.0001f);
            Assert.AreEqual(ada.Walker.Genome[0], loaded.Walker.Genome[0], 0.0001f);
            Assert.AreEqual(ada.Library.Count, loaded.Library.Count);
            Assert.AreEqual(ada.Emotions.Curiosity, loaded.Emotions.Curiosity, 0.0001f);
            string dir = Path.Combine(Path.GetTempPath(), "dream-machine-tests");
            nursery.WriteGrowthCsv(Path.Combine(dir, "growth.csv"));
            string csv = File.ReadAllText(Path.Combine(dir, "growth.csv"));
            StringAssert.Contains("curiosity", csv);
            StringAssert.Contains("motor_distance", csv);
            StringAssert.Contains("sleep_pressure", csv);
        }
    }

    public class PhysiologyTests
    {
        [Test]
        public void SeededDials_ReflexDelay_Cardio_Energy_Hormones_AndGrowth()
        {
            var a = new PhysiologyState(4);
            var b = new PhysiologyState(4);
            var c = new PhysiologyState(9);
            Assert.AreEqual(a.RestingHeartRate, b.RestingHeartRate, 0.001f);
            Assert.AreNotEqual(a.RestingHeartRate, c.RestingHeartRate);
            float hr0 = a.HeartRate;
            float delay = a.NerveDelaySeconds;
            var input = new PhysInput
            {
                Pain = 1f,
                Arousal = 0.9f,
                Exertion = 0.8f,
                JointAngles = new float[] { 1.2f, 0f, 1.2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f },
                Descending = new float[] { 0.2f, 0.2f, 0.2f, 0.2f }
            };
            a.Step(input, 0.02f, null);
            Assert.Less(a.FlexorDrive(), 0.2f);
            for (int i = 0; i < 8; i++) a.Step(input, 0.02f, null);
            Assert.Greater(a.FlexorDrive(), 0.4f);
            Assert.Greater(a.HeartRate, hr0 + 5f);
            float energy = a.Energy;
            for (int i = 0; i < 40; i++) a.Step(input, 0.05f, null);
            Assert.Less(a.Energy, energy);
            var bonded = new PhysInput { Eating = true, BondSameParent = 1f, BondOtherParent = 0.7f, BondSibling = 0.5f, CoupleWarmth = 0.9f };
            float energyBeforeEat = a.Energy;
            for (int i = 0; i < 25; i++) a.Step(bonded, 0.05f, null);
            Assert.Greater(a.Energy, energyBeforeEat);
            Assert.Greater(a.Oxytocin, 0.45f);
            var mood = new EmotionState(4);
            float fatigue = mood.Fatigue;
            var night = new PhysInput { Night = true, Smoke = 0.8f, Pain = 0.6f };
            for (int i = 0; i < 40; i++)
            {
                a.Step(night, 0.05f, mood);
                mood.Tick(0.05f);
            }
            Assert.Greater(a.Melatonin, 0.4f);
            Assert.Greater(a.Cortisol, 0.2f);
            Assert.Greater(mood.Fatigue, fatigue + 0.02f);
            Assert.Less(a.BreathingEfficiency, 0.6f);
            float babyHr = a.RestingHeartRate;
            float babyDelay = a.NerveDelaySeconds;
            float babyStrength = a.MuscleStrength;
            a.AgeDays = 40f;
            a.SetGeneration(3);
            Assert.Greater(a.Maturity, 0.8f);
            Assert.Less(a.RestingHeartRate, babyHr - 15f);
            Assert.Less(a.NerveDelaySeconds, babyDelay);
            Assert.Greater(a.MuscleStrength, babyStrength);
            Assert.AreEqual(delay > 0f, true);
            var memory = new MuscleMemoryLibrary();
            a.WriteRom(memory, 3);
            Assert.IsNotNull(memory.GetOrNull("rom:leftHip"));
        }
    }

    public class ElementTests
    {
        [Test]
        public void PhaseChanges_FireSpreads_WaterExtinguishes_SmokeRises()
        {
            var grid = new ElementGrid(8, 8);
            int ice = grid.Index(2, 2);
            grid.Ice[ice] = 1f;
            grid.Temperature[ice] = 15f;
            grid.Step(0.2f);
            Assert.Greater(grid.Water[ice], 0.1f);
            Assert.Less(grid.Ice[ice], 1f);
            grid.Water[ice] = 1f;
            grid.Temperature[ice] = 140f;
            grid.Step(0.2f);
            Assert.Greater(grid.Steam[ice], 0.05f);
            grid.Steam[ice] = 0.5f;
            grid.Temperature[ice] = 10f;
            grid.Step(0.2f);
            Assert.Less(grid.Steam[ice], 0.5f);

            int wood = grid.Index(5, 4);
            grid.Material[wood] = ElementGrid.Wood;
            grid.Fuel[wood] = 1.5f;
            grid.Temperature[wood] = 400f;
            grid.Burning[wood] = 1;
            grid.Material[wood + 1] = ElementGrid.Wood;
            grid.Fuel[wood + 1] = 1.2f;
            for (int i = 0; i < 40; i++) grid.Step(0.05f);
            Assert.Greater(grid.Temperature[wood + 1], 40f);
            Assert.Greater(grid.Smoke[wood], 0.01f);
            float smokeBelow = grid.Smoke[wood];
            int above = wood + grid.Width;
            grid.Smoke[above] = 0f;
            grid.Step(0.1f);
            Assert.Greater(grid.Smoke[above] + grid.Smoke[wood], smokeBelow * 0.2f);
            grid.Pour(wood, 0.8f);
            Assert.AreEqual(0, grid.Burning[wood]);
            grid.Fuel[wood + 1] = 1f;
            grid.Water[wood + 1] = 0.8f;
            grid.Temperature[wood + 1] = 500f;
            grid.Burning[wood + 1] = 0;
            grid.Step(0.05f);
            Assert.AreEqual(0, grid.Burning[wood + 1]);
        }
    }

    public class TimeFidelityTests
    {
        [Test]
        public void RunBudget_MeasuresTheWholeBatchAndPreservesTickState()
        {
            PopulationConfig population = PopulationConfig.CreateDefault();
            WorldSimulation budgeted = WorldSimulation.Create(population, QualityPreset.Laptop, 5);
            WorldSimulation replay = WorldSimulation.Create(population, QualityPreset.Laptop, 5);
            const double budgetMs = 20;
            const int hardCap = 100000;
            budgeted.RunTicks(256);
            replay.RunTicks(256);

            SimProfile profile = budgeted.RunBudget(hardCap, budgetMs, 0.016f, 1000f, false, 0f);

            Assert.Greater(profile.Ticks, 0);
            Assert.Less(profile.Ticks, hardCap);
            Assert.GreaterOrEqual(profile.Ticks / (double)profile.TicksPerSecond * 1000, budgetMs - 0.01);
            replay.RunTicks(profile.Ticks);
            Assert.AreEqual(replay.TickCount, budgeted.TickCount);
            Assert.AreEqual(replay.StateHash(), budgeted.StateHash());
        }

        [Test]
        public void FastForward_MatchesOneByOne_AndJumpIsJustMoreTicks()
        {
            PopulationConfig pop = PopulationConfig.CreateDefault();
            WorldSimulation one = WorldSimulation.Create(pop, QualityPreset.Phone, 5);
            WorldSimulation fast = WorldSimulation.Create(pop, QualityPreset.Phone, 5);
            one.RunTicks(96);
            int left = 96;
            while (left > 0)
            {
                int batch = left > 17 ? 17 : left;
                fast.RunTicks(batch);
                left -= batch;
            }
            Assert.AreEqual(one.TickCount, fast.TickCount);
            Assert.AreEqual(96, one.TickCount);
            Assert.AreEqual(one.StateHash(), fast.StateHash());
            Assert.Greater(one.MinAgeDays(), 0f);
            Assert.AreEqual(one.Children[0].Body.HeartRate, fast.Children[0].Body.HeartRate, 0.001f);

            WorldSimulation jumped = WorldSimulation.Create(pop, QualityPreset.Phone, 5);
            WorldSimulation manual = WorldSimulation.Create(pop, QualityPreset.Phone, 5);
            float goal = WorldSimulation.AgeOf(LifeStage.Toddler);
            int guard = 0;
            while (manual.MinAgeDays() < goal && guard < 20000)
            {
                manual.Tick();
                guard++;
            }
            int ran = 0;
            while (jumped.MinAgeDays() < goal && ran < 20000)
            {
                ran += jumped.RunUntilAge(goal, 13);
            }
            Assert.AreEqual(manual.TickCount, jumped.TickCount);
            Assert.AreEqual(manual.StateHash(), jumped.StateHash());
            Assert.GreaterOrEqual(jumped.MinAgeDays(), goal);
            TimeGovernor gov = new TimeGovernor();
            gov.TargetScale = 1000f;
            int planned = gov.Plan(0.016f, WorldSimulation.Dt, 64);
            Assert.LessOrEqual(planned, 64);
            Assert.Greater(planned, 1);
        }
    }

#if UNITY_EDITOR
    public class DreamMachineSceneTests
    {
        [Test]
        public void MainCamera_HasNormalizedRotationInEditMode()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/DreamMachine.unity");
            try
            {
                var camera = Array.Find(scene.GetRootGameObjects(), root => root.CompareTag("MainCamera"));
                Assert.IsNotNull(camera);
                Assert.IsNotNull(camera.GetComponent<UnityEngine.Camera>());
                var rotation = camera.transform.localRotation;
                Assert.AreEqual(1f, UnityEngine.Quaternion.Dot(rotation, rotation), 0.000001f);
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
#endif

    public class MiniJsonTests
    {
        [Test]
        public void NestedJson_RoundTrips()
        {
            var data = new Dictionary<string, object>
            {
                { "name", "Ada \"bee\"" },
                { "n", 3 },
                { "ok", true },
                { "values", new List<object> { 1.5d, "x" } }
            };
            object parsed = MiniJson.Parse(MiniJson.Stringify(data));
            var back = MiniJson.AsObject(parsed);
            Assert.AreEqual("Ada \"bee\"", MiniJson.AsString(back["name"]));
            Assert.AreEqual(3, MiniJson.AsInt(back["n"]));
            Assert.IsTrue(MiniJson.AsBool(back["ok"]));
            Assert.AreEqual(1.5d, MiniJson.AsDouble(MiniJson.AsArray(back["values"])[0]), 0.0001d);
        }
    }
}
