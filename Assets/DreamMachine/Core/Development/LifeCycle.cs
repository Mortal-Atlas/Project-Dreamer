using System.Collections.Generic;
using DreamMachine.Affect;
using DreamMachine.Physiology;
using DreamMachine.Body;
using DreamMachine.Libraries;
using DreamMachine.Mind;
using DreamMachine.Social;
using DreamMachine.Util;
using DreamMachine.Vision;
using DreamMachine.Wetware;
using DreamMachine.World;

namespace DreamMachine.Development
{
    public sealed class GrowthSample
    {
        public int Day;
        public string AgentId;
        public int Generation;
        public float MotorDistance;
        public float RomCoverage;
        public float BeliefAccuracy;
        public float GaitSimilarity;
        public float Curiosity;
        public float Frustration;
        public float Satisfaction;
        public float Fear;
        public float Fatigue;
        public float Valence;
        public float Arousal;
        public float SleepPressure;
        public string Phase;
    }

    /// <summary>
    /// Messages a later multi-machine run could send.
    /// This build only uses the in-process channel. No sockets.
    /// </summary>
    public interface IPeerChannel
    {
        void PublishPose(string agentId, PoseTrace trace);
        PoseTrace Latest(string agentId);
    }

    public sealed class InProcessPeerChannel : IPeerChannel
    {
        readonly Dictionary<string, PoseTrace> _latest = new Dictionary<string, PoseTrace>();

        public void PublishPose(string agentId, PoseTrace trace)
        {
            _latest[agentId] = trace;
        }

        public PoseTrace Latest(string agentId)
        {
            PoseTrace trace;
            return _latest.TryGetValue(agentId, out trace) ? trace : null;
        }
    }

    /// <summary>
    /// The whole persistent baby: one model, saved and loaded, never swapped.
    /// </summary>
    public sealed class AgentPersistentState
    {
        public string AgentId;
        public string DisplayName;
        public int BirthSeed;
        public int Generation;
        public int Day;
        public LibraryThree Library = new LibraryThree();
        public MuscleMemoryLibrary Muscles = new MuscleMemoryLibrary();
        public EmotionState Emotions = new EmotionState();
        public PhysiologyState Physiology = new PhysiologyState(1);
        public WalkLearner Walker;
        public List<VisualObservation> Observations = new List<VisualObservation>();
        public int BeliefsCorrect;
        public int BeliefsTried;
        public string Sex = "";

        public AgentPersistentState()
        {
        }

        public static AgentPersistentState CreateNew(PersonSpec person)
        {
            var state = new AgentPersistentState
            {
                AgentId = person.Id,
                DisplayName = person.DisplayName,
                BirthSeed = person.BirthSeed,
                Generation = 0,
                Day = 0,
                Sex = person.Sex.ToString(),
                Emotions = new EmotionState(person.BirthSeed),
                Physiology = new PhysiologyState(person.BirthSeed),
                Walker = new WalkLearner(person.BirthSeed)
            };
            state.Walker.Memory = state.Muscles;
            state.Walker.Babble(new SeededRandom(person.BirthSeed ^ 0xBABB), 6);
            return state;
        }

        public float BeliefAccuracy
        {
            get { return BeliefsTried == 0 ? 0f : (float)BeliefsCorrect / BeliefsTried; }
        }

        public string ToJson()
        {
            return MiniJson.Stringify(ToData());
        }

        public Dictionary<string, object> ToData()
        {
            var observations = new List<object>();
            int start = Observations.Count > 48 ? Observations.Count - 48 : 0;
            for (int i = start; i < Observations.Count; i++)
            {
                VisualObservation o = Observations[i];
                observations.Add(new Dictionary<string, object>
                {
                    { "objectId", o.ObjectId ?? "" },
                    { "category", o.Category ?? "" },
                    { "color", o.Color ?? "" },
                    { "shape", o.Shape ?? "" },
                    { "day", o.Day },
                    { "trial", o.Trial },
                    { "boundX", o.BoundX },
                    { "boundY", o.BoundY },
                    { "boundW", o.BoundW },
                    { "boundH", o.BoundH },
                    { "worldX", o.WorldX },
                    { "worldY", o.WorldY },
                    { "worldZ", o.WorldZ },
                    { "appearance", o.Appearance ?? "" },
                    { "pixelCount", o.PixelCount }
                });
            }
            return new Dictionary<string, object>
            {
                { "agentId", AgentId ?? "" },
                { "displayName", DisplayName ?? "" },
                { "birthSeed", BirthSeed },
                { "generation", Generation },
                { "day", Day },
                { "sex", Sex ?? "" },
                { "beliefsCorrect", BeliefsCorrect },
                { "beliefsTried", BeliefsTried },
                { "bestDistance", Walker == null ? 0f : Walker.BestDistance },
                { "genome", Walker == null ? new List<object>() : Walker.GenomeData() },
                { "emotions", Emotions.ToData() },
                { "ageDays", Physiology == null ? 0f : Physiology.AgeDays },
                { "heartRate", Physiology == null ? 0f : Physiology.HeartRate },
                { "energy", Physiology == null ? 0f : Physiology.Energy },
                { "library", Library.ToData() },
                { "muscles", Muscles.ToData() },
                { "observations", observations }
            };
        }

        public static AgentPersistentState FromJson(string json, bool beginNewSession)
        {
            var data = MiniJson.AsObject(MiniJson.Parse(json));
            var state = new AgentPersistentState
            {
                AgentId = MiniJson.AsString(data["agentId"]),
                DisplayName = MiniJson.AsString(data["displayName"]),
                BirthSeed = MiniJson.AsInt(data["birthSeed"]),
                Generation = MiniJson.AsInt(data["generation"]),
                Day = MiniJson.AsInt(data["day"]),
                Sex = data.ContainsKey("sex") ? MiniJson.AsString(data["sex"]) : "",
                BeliefsCorrect = data.ContainsKey("beliefsCorrect") ? MiniJson.AsInt(data["beliefsCorrect"]) : 0,
                BeliefsTried = data.ContainsKey("beliefsTried") ? MiniJson.AsInt(data["beliefsTried"]) : 0
            };
            state.Emotions = new EmotionState(state.BirthSeed);
            state.Physiology = new PhysiologyState(state.BirthSeed);
            if (data.ContainsKey("emotions")) state.Emotions.LoadData(MiniJson.AsObject(data["emotions"]));
            if (data.ContainsKey("ageDays")) state.Physiology.AgeDays = MiniJson.AsFloat(data["ageDays"]);
            if (data.ContainsKey("heartRate")) state.Physiology.HeartRate = MiniJson.AsFloat(data["heartRate"]);
            if (data.ContainsKey("energy")) state.Physiology.Energy = MiniJson.AsFloat(data["energy"]);
            state.Physiology.SetGeneration(state.Generation);
            if (data.ContainsKey("library")) state.Library = LibraryThree.FromData(MiniJson.AsObject(data["library"]));
            if (data.ContainsKey("muscles")) state.Muscles = MuscleMemoryLibrary.FromData(MiniJson.AsArray(data["muscles"]));
            state.Walker = new WalkLearner(state.BirthSeed);
            state.Walker.Memory = state.Muscles;
            if (data.ContainsKey("genome"))
            {
                float best = data.ContainsKey("bestDistance") ? MiniJson.AsFloat(data["bestDistance"]) : state.Walker.BestDistance;
                state.Walker.LoadGenome(MiniJson.AsArray(data["genome"]), best);
            }
            if (data.ContainsKey("observations"))
            {
                List<object> rows = MiniJson.AsArray(data["observations"]);
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = MiniJson.AsObject(rows[i]);
                    state.Observations.Add(new VisualObservation
                    {
                        ObjectId = MiniJson.AsString(row["objectId"]),
                        Category = MiniJson.AsString(row["category"]),
                        Color = MiniJson.AsString(row["color"]),
                        Shape = MiniJson.AsString(row["shape"]),
                        Day = MiniJson.AsInt(row["day"]),
                        Trial = MiniJson.AsInt(row["trial"]),
                        BoundX = MiniJson.AsFloat(row["boundX"]),
                        BoundY = MiniJson.AsFloat(row["boundY"]),
                        BoundW = MiniJson.AsFloat(row["boundW"]),
                        BoundH = MiniJson.AsFloat(row["boundH"]),
                        WorldX = MiniJson.AsFloat(row["worldX"]),
                        WorldY = MiniJson.AsFloat(row["worldY"]),
                        WorldZ = MiniJson.AsFloat(row["worldZ"]),
                        Appearance = MiniJson.AsString(row["appearance"]),
                        PixelCount = MiniJson.AsInt(row["pixelCount"])
                    });
                }
            }
            if (beginNewSession)
            {
                state.Generation += 1;
                state.Physiology.SetGeneration(state.Generation);
            }
            return state;
        }
    }

    public sealed class NurseryOptions
    {
        public int WalkIterations = 12;
        public int ObjectEncounters = 3;
        public bool PerformanceMode = true;
    }

    /// <summary>
    /// One persistent nursery. Awake time gathers experience.
    /// Night replays it, amends Library III, and lets Library II dream.
    /// </summary>
    public sealed class NurserySimulator
    {
        public PopulationConfig Population;
        public GroupMind Groups = new GroupMind();
        public IPeerChannel Peers;
        public HiddenRuleWorld World = new HiddenRuleWorld { ShiftAt = 2 };
        public List<ObjectInstance> Objects;
        public Dictionary<string, AgentPersistentState> Babies = new Dictionary<string, AgentPersistentState>();
        public Dictionary<string, AgentMind> Minds = new Dictionary<string, AgentMind>();
        public List<GrowthSample> Growth = new List<GrowthSample>();
        public List<DreamProp> LastDream = new List<DreamProp>();
        public NurseryOptions Options = new NurseryOptions();
        public int Day;

        public static NurserySimulator Create(PopulationConfig population, NurseryOptions options)
        {
            var sim = new NurserySimulator
            {
                Population = population,
                Objects = HiddenRuleWorld.StandardPool(),
                Options = options ?? new NurseryOptions(),
                Peers = new InProcessPeerChannel()
            };
            List<PersonSpec> babies = population.Babies();
            for (int i = 0; i < babies.Count; i++)
            {
                AgentPersistentState state = AgentPersistentState.CreateNew(babies[i]);
                sim.Babies[babies[i].Id] = state;
                var mind = new AgentMind(babies[i].BirthSeed, MindCondition.Full);
                mind.Library = state.Library;
                sim.Minds[babies[i].Id] = mind;
            }
            return sim;
        }

        public void RunDays(int days)
        {
            for (int i = 0; i < days; i++) RunDay();
        }

        public void RunDay()
        {
            Groups.Step(Day, 0, 99);
            PublishMentorPoses();
            List<PersonSpec> babies = Population.Babies();
            for (int i = 0; i < babies.Count; i++)
            {
                Awake(babies[i]);
            }
            for (int i = 0; i < babies.Count; i++)
            {
                Night(babies[i]);
            }
            Day++;
            foreach (var pair in Babies) pair.Value.Day = Day;
        }

        void PublishMentorPoses()
        {
            List<PersonSpec> mentors = Population.Mentors();
            for (int i = 0; i < mentors.Count; i++)
            {
                PersonSpec mentor = mentors[i];
                string clip = ClipFor(Groups.GoalFor(mentor));
                PoseTrace trace = MentorMotion.Record(mentor.Id, clip, 1.2f, 0.1f, mentor.BirthSeed);
                Peers.PublishPose(mentor.Id, trace);
            }
            // Babies also publish their current attempt, so peers can imitate a body, not a file.
            foreach (var pair in Babies)
            {
                var trace = new PoseTrace { SourceId = pair.Key };
                for (int f = 0; f < 8; f++)
                {
                    BodyPose pose = pair.Value.Walker.PoseAt(f * 0.15f);
                    trace.Frames.Add(new PoseFrame
                    {
                        SourceId = pair.Key,
                        Time = f * 0.15f,
                        Angles = pose.ToArray()
                    });
                }
                Peers.PublishPose(pair.Key, trace);
            }
        }

        void Awake(PersonSpec baby)
        {
            AgentPersistentState state = Babies[baby.Id];
            AgentMind mind = Minds[baby.Id];
            EmotionState mood = state.Emotions;
            mood.Excite(0.15f, 0f, 0f, 0f, 0.12f);
            for (int i = 0; i < 8; i++) mood.Tick(0.2f);
            var rng = new SeededRandom(baby.BirthSeed + Day * 131 + 7);
            PoseTrace model = WeightedTrace(baby, rng);
            var mapper = new PoseMapper(baby.BirthSeed);
            if (model != null) model = mapper.MapTrace(model);
            int iterations = Options.PerformanceMode ? System.Math.Max(4, Options.WalkIterations / 2) : Options.WalkIterations;
            float before = state.Walker.BestDistance;
            state.Walker.Practice(iterations, mood.Exploration, rng, model, SocialAttention.RoleModelWeight(baby, Population.Find(DominantModelId(baby))) * 0.35f);
            if (state.Walker.BestDistance > before + 0.05f) mood.Excite(0f, 0f, 0.25f, -0.05f, 0.05f);
            else mood.Excite(0.05f, 0.12f, 0f, 0.08f, 0.05f);
            var worldRng = new SeededRandom(baby.BirthSeed + Day * 17);
            int hits = 0;
            int tries = 0;
            for (int e = 0; e < Options.ObjectEncounters; e++)
            {
                ObjectInstance obj = Objects[worldRng.NextInt(Objects.Count)];
                TrialDecision decision = mind.Encounter(obj, Day * 10 + e, World);
                tries++;
                if (decision.Correct) hits++;
                state.Observations.Add(new VisualObservation
                {
                    ObjectId = obj.Id,
                    Category = obj.CategoryLabel,
                    Color = obj.Color,
                    Shape = obj.Shape,
                    Day = Day,
                    Trial = e,
                    WorldX = obj.WorldX,
                    WorldY = obj.WorldY,
                    WorldZ = obj.WorldZ,
                    Appearance = obj.CategoryLabel,
                    PixelCount = 12,
                    BoundW = 0.2f,
                    BoundH = 0.2f
                });
                if (!decision.Correct) mood.Excite(0.04f, 0.08f, 0f, 0f, 0f);
            }
            state.BeliefsCorrect += hits;
            state.BeliefsTried += tries;
            PoseTrace leaderTrace = Peers.Latest(Population.LeaderOf(baby.Group).Id);
            float similarity = state.Walker.Similarity(leaderTrace);
            Growth.Add(Sample(state, "awake", similarity));
        }

        void Night(PersonSpec baby)
        {
            AgentPersistentState state = Babies[baby.Id];
            var rng = new SeededRandom(baby.BirthSeed + Day * 900 + 3);
            var recombinator = new DreamRecombinator();
            List<DreamProp> dream = recombinator.Remix(state.Observations, new LibraryTwo(), rng);
            LastDream = dream;
            PersonSpec leader = Population.LeaderOf(baby.Group);
            PoseTrace replay = Peers.Latest(leader.Id);
            if (replay != null)
            {
                var mapper = new PoseMapper(baby.BirthSeed);
                state.Walker.BlendToward(mapper.MapTrace(replay), 0.25f * state.Emotions.LearningRate + 0.05f);
            }
            // Dream hypotheses are filed, not substituted for experience.
            if (state.Observations.Count > 0)
            {
                VisualObservation last = state.Observations[state.Observations.Count - 1];
                string idea = "dream:" + last.Color + "+" + last.Shape;
                state.Library.NoteIdea(idea, "harmful", 0.15f, Day, "library-II-dream");
            }
            state.Emotions.Excite(-0.05f, -0.1f, 0.05f, -0.15f, -0.45f);
            for (int i = 0; i < 10; i++) state.Emotions.Tick(0.25f);
            PoseTrace leaderTrace = Peers.Latest(leader.Id);
            Growth.Add(Sample(state, "night", state.Walker.Similarity(leaderTrace)));
        }

        PoseTrace WeightedTrace(PersonSpec baby, SeededRandom rng)
        {
            // Sample a person to imitate with probability proportional to attention.
            // The baby only receives that person's pose trace.
            List<PersonSpec> others = Population.People;
            float total = 0f;
            for (int i = 0; i < others.Count; i++)
            {
                total += SocialAttention.AttentionWeight(baby, others[i]);
            }
            float pick = rng.NextFloat() * total;
            float run = 0f;
            for (int i = 0; i < others.Count; i++)
            {
                run += SocialAttention.AttentionWeight(baby, others[i]);
                if (run >= pick)
                {
                    return Peers.Latest(others[i].Id);
                }
            }
            return Peers.Latest(Population.LeaderOf(baby.Group).Id);
        }

        string DominantModelId(PersonSpec baby)
        {
            PersonSpec model = Population.PrimaryRoleModel(baby);
            return model != null ? model.Id : Population.LeaderOf(baby.Group).Id;
        }

        GrowthSample Sample(AgentPersistentState state, string phase, float similarity)
        {
            EmotionState e = state.Emotions;
            return new GrowthSample
            {
                Day = Day,
                AgentId = state.AgentId,
                Generation = state.Generation,
                MotorDistance = state.Walker.BestDistance,
                RomCoverage = state.Muscles.RomCoverage(),
                BeliefAccuracy = state.BeliefAccuracy,
                GaitSimilarity = similarity,
                Curiosity = e.Curiosity,
                Frustration = e.Frustration,
                Satisfaction = e.Satisfaction,
                Fear = e.FearOfFalling,
                Fatigue = e.Fatigue,
                Valence = e.Valence,
                Arousal = e.Arousal,
                SleepPressure = e.SleepPressure,
                Phase = phase
            };
        }

        public static string ClipFor(GroupGoal goal)
        {
            if (goal == GroupGoal.Rest) return "sit";
            if (goal == GroupGoal.PracticeGesture) return "gesture";
            if (goal == GroupGoal.InspectObjects) return "idle";
            return "walk";
        }

        public void WriteGrowthCsv(string path)
        {
            var header = new List<string>
            {
                "day", "agent_id", "generation", "phase", "motor_distance", "rom_coverage",
                "belief_accuracy", "gait_similarity", "curiosity", "frustration", "satisfaction",
                "fear", "fatigue", "valence", "arousal", "sleep_pressure"
            };
            var rows = new List<IList<string>>();
            for (int i = 0; i < Growth.Count; i++)
            {
                GrowthSample g = Growth[i];
                rows.Add(new List<string>
                {
                    g.Day.ToString(),
                    g.AgentId,
                    g.Generation.ToString(),
                    g.Phase,
                    CsvTable.Format(g.MotorDistance),
                    CsvTable.Format(g.RomCoverage),
                    CsvTable.Format(g.BeliefAccuracy),
                    CsvTable.Format(g.GaitSimilarity),
                    CsvTable.Format(g.Curiosity),
                    CsvTable.Format(g.Frustration),
                    CsvTable.Format(g.Satisfaction),
                    CsvTable.Format(g.Fear),
                    CsvTable.Format(g.Fatigue),
                    CsvTable.Format(g.Valence),
                    CsvTable.Format(g.Arousal),
                    CsvTable.Format(g.SleepPressure)
                });
            }
            CsvTable.Write(path, header, rows);
        }
    }
}
