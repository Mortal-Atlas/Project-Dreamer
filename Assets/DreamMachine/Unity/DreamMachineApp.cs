using System.Collections.Generic;
using System.IO;
using System.Text;
using DreamMachine.Affect;
using DreamMachine.Body;
using DreamMachine.Development;
using DreamMachine.Experiment;
using DreamMachine.Libraries;
using DreamMachine.Mind;
using DreamMachine.Social;
using DreamMachine.Util;
using DreamMachine.Vision;
using DreamMachine.Wetware;
using DreamMachine.Sim;
using DreamMachine.World;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Builds the yard at play time: 6 babies, 6 mentors, objects, day and night, vision.
    /// The scored learning lives in the pure C# core. This class only shows it and feeds it
    /// what the bodies and cameras can actually sense.
    /// </summary>
    public sealed partial class DreamMachineApp : MonoBehaviour
    {
        const float AwakeSeconds = 36f;
        const float NightSeconds = 16f;

        sealed class Actor
        {
            public PersonSpec Spec;
            public AppearanceProfile Look;
            public SegmentedBody Body;
            public AgentPersistentState State;
            public AgentMind Mind;
            public PoseMapper Mapper;
            public HebbianWetware Wetware;
            public float[] Smoothed;
            public Vector3 Pos;
            public float Yaw;
            public float GaitDistance;
            public float NextPractice;
            public float NextEncounter;
            public bool LocalAvatarChecked;
            public HouseholdAvatar LocalAvatar;
            public readonly List<PoseFrame> Seen = new List<PoseFrame>();
        }

        PopulationConfig _pop;
        GroupMind _groups;
        HiddenRuleWorld _world;
        List<ObjectInstance> _pool;
        readonly Dictionary<string, Actor> _actors = new Dictionary<string, Actor>();
        readonly List<Actor> _babies = new List<Actor>();
        readonly Dictionary<string, Transform> _props = new Dictionary<string, Transform>();
        readonly List<GameObject> _dreams = new List<GameObject>();
        readonly List<GrowthSample> _growth = new List<GrowthSample>();
        readonly Vector3[] _anchors = { new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f) };

        MobileHud _hud;
        Camera _eye;
        Camera _seg;
        RenderTexture _eyeRt;
        RenderTexture _segRt;
        Light _sun;
        Material _unlit;
        Shader _idShader;
        Font _font;
        Transform _dreamAnchor;
        TextMesh _banner;
        string _startupFailure;
        bool _archiveHouseholdBaseline;

        int _focus;
        int _day;
        int _encounters;
        float _clock;
        float _visionTimer;
        float _orbitYaw = 35f;
        float _orbitPitch = 28f;
        float _zoom = 8f;
        Vector3 _lastPointer;
        bool _night;
        bool _performance = true;
        bool _ruleAnnounced;
        bool _ready;
        int _replayRows;
        WorldSimulation _sim;
        const string SimulationTrayId = "simulation";
        TrayWorkspace _workspace;
        float _nextTrayRefresh;
        TimeGovernor _governor = new TimeGovernor();
        QualityPreset _preset = QualityPreset.Phone;
        float _achieved = 1f;
        float _ticksPerSec;
        float[] _poseBuf = new float[10];
        readonly System.Collections.Generic.List<GameObject> _elementMarks = new System.Collections.Generic.List<GameObject>();
        int _markFrame;
        RemoteView _remote;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Application.isBatchMode) return;
            if (Object.FindAnyObjectByType<ElbowLabView>() != null) return;
            if (Object.FindAnyObjectByType<LowerBodyLabView>() != null) return;
            if (Object.FindAnyObjectByType<HandLabView>() != null) return;
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<DreamMachineApp>() != null) return;
#else
            if (Object.FindObjectOfType<DreamMachineApp>() != null) return;
#endif
            var go = new GameObject("DreamMachine");
            go.AddComponent<DreamMachineApp>();
        }

        void Start()
        {
            TextAsset familyFile = Resources.Load<TextAsset>("family");
            _pop = familyFile != null ? PopulationConfig.FromJson(familyFile.text) : PopulationConfig.CreateDefault();
            try
            {
                _sim = WorldSimulation.LoadPrimaryCheckpoint(WorldPath())
                    ?? WorldSimulation.Create(_pop, _preset, 7, spec => AgentSaveFile.Load(AgentPath(spec.Id)));
                _pop = _sim.Population;
                _preset = _sim.Preset;
                _night = _sim.IsNight;
                _day = _sim.DayIndex;
                EnableLivingPresentation();
            }
            catch (System.Exception error) when (error is IOException || error is InvalidDataException ||
                error is System.UnauthorizedAccessException)
            {
                _startupFailure = "Simulation paused. A saved individual could not be restored.\n" + error.Message
                    + "\nInspect the save or restore a known-good copy, then restart.\n"
                    + Application.persistentDataPath;
                Debug.LogError(_startupFailure, this);
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            EnsureSun();
            Shader visible = Shader.Find("DreamMachine/ColorUnlit");
            if (visible == null) visible = Shader.Find("Unlit/Color");
            _unlit = new Material(visible);
            BuildGround();
            _idShader = Shader.Find("DreamMachine/IdColor");
            _groups = new GroupMind();
            _groups.Step(0, 0, 7);
            _world = new HiddenRuleWorld { ShiftAt = 18 };
            _pool = HiddenRuleWorld.StandardPool();
            var looks = LoadLooks();
            if (_sim.Home == null) BuildProps();
            else BuildLivingRoom();
            BuildPeople(looks);
            BuildCameras();
            BuildHud();
            _dreamAnchor = new GameObject("DreamAnchor").transform;
            _banner = CreateBanner();
            if (_sim.Home != null) _banner.text = "";
            TextAsset replay = Resources.Load<TextAsset>("Wetware/brian2_replay");
            if (replay != null)
            {
                var wet = new ReplayFileWetware();
                wet.LoadText(replay.text);
                _replayRows = wet.Rows.Count;
            }
            BuildHome();
            _ready = true;
            SelectBaby(0);
            _workspace = new TrayWorkspace("DreamMachine.Workspace.v1.");
            var clockLayout = DefaultSimulationTray();
            if (_sim.Home != null) clockLayout.Visible = false;
            _workspace.Register(SimulationTrayId, "Simulation controls", new Vector2(480, 105), clockLayout,
                BuildSimulationControls, () => _timeLabel = null, RefreshSimulationControls);
            if (_sim.Home != null) RegisterLivingTray();
            _workspace.RestoreVisibility();
            if (_sim.Home != null) DrawLiving(0.1f);
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_startupFailure)) return;
            var style = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.Label(new Rect(16, 16, Mathf.Max(16, Screen.width - 32), 180), _startupFailure, style);
        }

        void Update()
        {
            if (!_ready || _sim == null) return;
            float real = Time.unscaledDeltaTime;
            if (real > 0.1f) real = 0.1f;
            HandleTimeKeys();
            if (_workspace != null && Time.unscaledTime >= _nextTrayRefresh)
            {
                _nextTrayRefresh = Time.unscaledTime + 0.2f;
                _workspace.RefreshVisible();
            }
            if (_remote != null && _remote.Active && _remote.Latest != null && _remote.Latest.Length > 0)
            {
                if (!_governor.Paused)
                {
                    ApplyRemote(_remote.Latest);
                    _night = _sim.IsNight;
                    StepActors(Mathf.Min(0.05f, real));
                }
                OrbitCamera();
                BillboardLabels();
                RefreshHud();
                if (_timeLabel != null) _timeLabel.text = _remote.Status + "  " + _remote.Latest;
                return;
            }
            int budget = _preset == QualityPreset.Phone ? 40 : _preset == QualityPreset.Laptop ? 140 : 500;
            float budgetMs = _preset == QualityPreset.Phone ? 8f : _preset == QualityPreset.Laptop ? 14f : 22f;
            int ran = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (_governor.TakeSingleStep())
            {
                _sim.Tick();
                ran = 1;
            }
            else if (_governor.CatchingUp && !_governor.Paused)
            {
                float goal = WorldSimulation.AgeOf(_governor.CatchStage);
                while (ran < budget && watch.Elapsed.TotalMilliseconds < budgetMs && _sim.MinAgeDays() < goal)
                {
                    _sim.Tick();
                    ran++;
                }
                if (_sim.MinAgeDays() >= goal)
                {
                    _governor.CatchingUp = false;
                    _governor.TargetScale = 1f;
                }
            }
            else
            {
                int want = _governor.Plan(real, WorldSimulation.Dt, budget);
                while (ran < want && watch.Elapsed.TotalMilliseconds < budgetMs)
                {
                    _sim.Tick();
                    ran++;
                }
                _governor.Accumulator += (want - ran) * WorldSimulation.Dt;
            }
            watch.Stop();
            _achieved = real > 0.000001f ? (ran * WorldSimulation.Dt) / real : 0f;
            _ticksPerSec = watch.Elapsed.TotalSeconds > 0.000001 ? (float)(ran / watch.Elapsed.TotalSeconds) : 0f;
            bool wasNight = _night;
            _night = _sim.IsNight;
            _day = _sim.DayIndex;
            _clock = _sim.SimSeconds % WorldSimulation.DayLength;
            if (_night && !wasNight) BeginNight();
            if (!_night && wasNight) EndNight();
            float dt = Mathf.Min(0.1f, ran * WorldSimulation.Dt);
            if (ran > 0) StepActors(dt);
            OrbitCamera();
            BillboardLabels();
            _visionTimer += dt;
            float visionEvery = _performance ? 0.8f : 0.45f;
            if ((_sim.Home == null || _researchPresentation) && _achieved < 12f && _visionTimer >= visionEvery)
            {
                _visionTimer = 0f;
                CaptureVision();
            }
            if (_achieved < 20f || (_markFrame++ % 5) == 0) RefreshElementMarks();
            RefreshHud();
            if (_sun != null)
            {
                _sun.intensity = _night ? 0.12f : 1.05f;
                _sun.color = _night ? new Color(0.55f, 0.65f, 0.95f) : new Color(1f, 0.96f, 0.88f);
            }
            Camera.main.backgroundColor = _night
                ? new Color(0.04f, 0.06f, 0.12f)
                : new Color(0.62f, 0.78f, 0.90f);
        }

        void StepActors(float dt)
        {
            if (_sim.Home != null && (_remote == null || !_remote.Active)) { DrawLiving(dt); return; }
            for (int g = 0; g < 2; g++)
            {
                MoveAnchor(g, dt);
            }
            foreach (Actor actor in _actors.Values)
            {
                actor.LocalAvatar?.SetVisible(false);
                actor.Body.ShowEarlyClothes(false);
                Vector3 before = actor.Pos;
                if (actor.Spec.Role == MemberRole.Baby) StepBaby(actor, dt);
                else StepMentor(actor, dt);
                Vector3 change = actor.Pos - before;
                float moved = change.magnitude;
                actor.GaitDistance += moved;
                float yaw = moved > 0.0001f ? Mathf.Atan2(change.x, change.z) * Mathf.Rad2Deg : actor.Yaw;
                actor.Body.ApplyGroundedGait(actor.GaitDistance, moved, yaw);
                actor.Body.transform.position = actor.Pos;
                if (actor.Spec.Role != MemberRole.Baby) RememberPose(actor);
            }
        }

        void MoveAnchor(int groupIndex, float dt)
        {
            Vector3 home = groupIndex == 0 ? new Vector3(-4.2f, 0f, 0f) : new Vector3(4.2f, 0f, 0f);
            if (PopulationConfig.RoutineAt(_sim.SimSeconds, WorldSimulation.DayLength, WorldSimulation.AwakeLength)
                != FamilyRoutine.Play) return;
            const float speed = 0.4f;
            float ang = _sim.SimSeconds * 0.18f + groupIndex;
            Vector3 desired = home + new Vector3(Mathf.Sin(ang) * 2.4f, 0f, Mathf.Cos(ang) * 2.4f);
            Vector3 dir = desired - _anchors[groupIndex];
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.05f)
            {
                _anchors[groupIndex] += dir.normalized * speed * dt;
            }
        }

        void StepMentor(Actor actor, float dt)
        {
            PersonSpec baby = _pop.Find(actor.Spec.PairedWithId);
            Actor babyActor;
            _actors.TryGetValue(baby.Id, out babyActor);
            Vector3 anchor = _anchors[actor.Spec.Group == GroupId.Boys ? 0 : 1];
            Vector3 babyPos = babyActor != null ? babyActor.Pos : anchor;
            float duty = SocialAttention.DutyToLeader(actor.Spec);
            Vector3 slot = anchor + Slot(actor.Spec);
            Vector3 goal = actor.Spec.IsLeader
                ? Vector3.Lerp(anchor, babyPos, 0.2f)
                : Vector3.Lerp(slot, babyPos, 0.45f + 0.2f * duty);
            goal.y = 0f;
            Vector3 delta = goal - actor.Pos;
            delta.y = 0f;
            if (PopulationConfig.RoutineAt(_sim.SimSeconds, WorldSimulation.DayLength, WorldSimulation.AwakeLength)
                == FamilyRoutine.Play && delta.sqrMagnitude > 0.0025f)
            {
                Vector3 change = Vector3.ClampMagnitude(delta, dt * 0.48f);
                float clear = YardClearance.MotionFraction(actor.Pos.x, actor.Pos.z, change.x, change.z, 0.18f);
                if (clear < 0.01f)
                {
                    // Retain a tangential component when the direct follow route hits a fixed obstacle.
                    var x = new Vector3(change.x, 0f, 0f);
                    var z = new Vector3(0f, 0f, change.z);
                    Vector3 stepX = x * YardClearance.MotionFraction(actor.Pos.x, actor.Pos.z, x.x, 0f, 0.18f);
                    Vector3 stepZ = z * YardClearance.MotionFraction(actor.Pos.x, actor.Pos.z, 0f, z.z, 0.18f);
                    change = stepX.sqrMagnitude >= stepZ.sqrMagnitude ? stepX : stepZ;
                }
                else change *= clear;
                actor.Pos += change;
                if (change.sqrMagnitude > 0.00000001f)
                    actor.Yaw = Mathf.Atan2(change.x, change.z) * Mathf.Rad2Deg;
            }
            MentorMotion.WriteAngles("idle", _sim.SimSeconds, actor.Spec.BirthSeed, _poseBuf);
            SmoothAndApply(actor, _poseBuf, dt);
        }

        void StepBaby(Actor actor, float dt)
        {
            actor.Body.ApplyDevelopmentAge(WorldSimulation.AnatomyAge(actor.State.Physiology.AgeDays));
            if (_sim != null)
            {
                SimChild child = _sim.Find(actor.Spec.Id);
                if (child != null)
                {
                    actor.Pos = new Vector3(child.X, 0f, child.Z);
                    actor.Yaw = child.Yaw;
                    MentorMotion.WriteAngles("idle", _sim.SimSeconds, actor.Spec.BirthSeed, _poseBuf);
                    SmoothAndApply(actor, _poseBuf, dt);
                    return;
                }
            }
            EmotionState mood = actor.State.Emotions;
            mood.Tick(dt);
            if (!_night) mood.Excite(0.02f * dt, 0f, 0f, 0f, 0.03f * dt);
            int groupIndex = actor.Spec.Group == GroupId.Boys ? 0 : 1;
            Vector3 home = _anchors[groupIndex] + Slot(actor.Spec);
            float learned = Mathf.Max(0f, actor.State.Walker.BestDistance) / 4f;
            if (learned > 0.55f) learned = 0.55f;
            if (_night) learned = 0f;
            Vector3 face = home - actor.Pos;
            face.y = 0f;
            if (face.sqrMagnitude > 0.05f)
            {
                actor.Yaw = Mathf.LerpAngle(actor.Yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-dt * 2f));
            }
            Vector3 forward = Quaternion.Euler(0f, actor.Yaw, 0f) * Vector3.forward;
            actor.Pos += forward * learned * dt;
            if (actor.Pos.magnitude > 12f) actor.Pos = actor.Pos.normalized * 12f;
            actor.Pos.y = 0f;
            if (_night)
            {
                float[] rest = MentorMotion.PoseAngles("sit", 0f, 0);
                SmoothAndApply(actor, rest, dt);
            }
            else
            {
                BodyPose pose = actor.State.Walker.PoseAt(_sim != null ? _sim.SimSeconds : Time.time);
                SmoothAndApply(actor, pose.ToArray(), dt);
                actor.NextPractice -= dt;
                if (actor.NextPractice <= 0f)
                {
                    actor.NextPractice = _performance ? 2.4f : 1.3f;
                    Practice(actor);
                }
                actor.NextEncounter -= dt;
                if (actor.NextEncounter <= 0f)
                {
                    actor.NextEncounter = 4.5f;
                    EncounterNearest(actor);
                }
            }
            RememberPose(actor);
        }

        void Practice(Actor actor)
        {
            PoseTrace trace = ObserveSomeone(actor);
            float weight = 0.3f * actor.State.Emotions.LearningRate;
            var rng = new SeededRandom(actor.Spec.BirthSeed + _day * 97 + _encounters + 3);
            float before = actor.State.Walker.BestDistance;
            int iterations = _performance ? 2 : 4;
            actor.State.Walker.Practice(iterations, actor.State.Emotions.Exploration, rng, trace, weight);
            float gain = actor.State.Walker.BestDistance - before;
            if (gain > 0.02f) actor.State.Emotions.Excite(0f, 0f, 0.12f, -0.04f, 0.02f);
            else actor.State.Emotions.Excite(0.04f, 0.06f, 0f, 0.05f, 0.02f);
            if (actor.State.Walker.BestDistance < 0.05f) actor.State.Emotions.Excite(0f, 0.02f, 0f, 0.08f, 0f);
            actor.Wetware.Step(0.1f, new[]
            {
                actor.State.Walker.BestDistance,
                actor.State.Emotions.Curiosity,
                actor.State.Emotions.FearOfFalling,
                actor.State.Emotions.Fatigue
            });
            actor.Wetware.ApplyPlasticity(gain);
        }

        PoseTrace ObserveSomeone(Actor baby)
        {
            float total = 0f;
            foreach (Actor other in _actors.Values)
            {
                total += SocialAttention.AttentionWeight(baby.Spec, other.Spec);
            }
            var rng = new SeededRandom(baby.Spec.BirthSeed + Mathf.RoundToInt(Time.time * 10f) + 19);
            float pick = rng.NextFloat() * Mathf.Max(0.0001f, total);
            float run = 0f;
            Actor chosen = null;
            foreach (Actor other in _actors.Values)
            {
                run += SocialAttention.AttentionWeight(baby.Spec, other.Spec);
                if (run >= pick)
                {
                    chosen = other;
                    break;
                }
            }
            if (chosen == null || chosen.Seen.Count == 0)
            {
                PersonSpec leader = _pop.LeaderOf(baby.Spec.Group);
                Actor leaderActor;
                if (leader != null && _actors.TryGetValue(leader.Id, out leaderActor)) chosen = leaderActor;
            }
            if (chosen == null || chosen.Seen.Count == 0) return null;
            return CopyTrace(chosen, baby.Mapper);
        }

        void EncounterNearest(Actor actor)
        {
            ObjectInstance nearest = null;
            float best = 6f;
            for (int i = 0; i < _pool.Count; i++)
            {
                var p = new Vector3(_pool[i].WorldX, 0f, _pool[i].WorldZ);
                float d = Vector3.Distance(new Vector3(actor.Pos.x, 0f, actor.Pos.z), p);
                if (d < best)
                {
                    best = d;
                    nearest = _pool[i];
                }
            }
            if (nearest == null) nearest = _pool[_encounters % _pool.Count];
            TrialDecision decision = actor.Mind.Encounter(nearest, _encounters, _world);
            _encounters++;
            actor.State.BeliefsTried++;
            if (decision.Correct) actor.State.BeliefsCorrect++;
            if (!_ruleAnnounced && decision.Phase == 2)
            {
                _ruleAnnounced = true;
                _banner.text = "The yard changed its hidden rule.\nOld beliefs must be amended, not erased.";
            }
            actor.State.Observations.Add(new VisualObservation
            {
                ObjectId = nearest.Id,
                Category = nearest.CategoryLabel,
                Color = nearest.Color,
                Shape = nearest.Shape,
                Day = _day,
                Trial = _encounters,
                WorldX = nearest.WorldX,
                WorldY = nearest.WorldY,
                WorldZ = nearest.WorldZ,
                Appearance = nearest.CategoryLabel
            });
            Trim(actor);
            if (!decision.Correct) actor.State.Emotions.Excite(0.03f, 0.07f, 0f, 0f, 0f);
            else actor.State.Emotions.Excite(0f, 0f, 0.06f, 0f, 0f);
        }

        void BeginNight()
        {
            _night = true;
            if (_sim.Home != null) { _banner.text = ""; return; }
            _banner.text = "Night. They dream what they saw, and amend what they believe.";
            for (int i = 0; i < _babies.Count; i++)
            {
                Actor baby = _babies[i];
                var rng = new SeededRandom(baby.Spec.BirthSeed + _day * 13 + 5);
                List<DreamProp> props = new DreamRecombinator().Remix(baby.State.Observations, new LibraryTwo(), rng);
                if (i == _focus) ShowDream(baby, props);
                LogGrowth(baby, "night");
            }
        }

        void EndNight()
        {
            for (int i = 0; i < _dreams.Count; i++) Destroy(_dreams[i]);
            _dreams.Clear();
            _night = false;
            _clock = 0f;
            _banner.text = _sim.Home == null ? "Day " + (_day + 1) + ". Awake. Touch the world and try the legs again." : "";
            for (int i = 0; i < _babies.Count; i++) LogGrowth(_babies[i], "awake");
            SaveAll();
        }

        void ShowDream(Actor baby, List<DreamProp> props)
        {
            for (int i = 0; i < _dreams.Count; i++) Destroy(_dreams[i]);
            _dreams.Clear();
            _dreamAnchor.position = baby.Pos + Quaternion.Euler(0f, baby.Yaw, 0f) * Vector3.forward * 1.6f;
            for (int i = 0; i < props.Count; i++)
            {
                DreamProp prop = props[i];
                PrimitiveType type = prop.Shape == "Sphere" ? PrimitiveType.Sphere
                    : prop.Shape == "Capsule" ? PrimitiveType.Capsule
                    : prop.Shape == "Cylinder" ? PrimitiveType.Cylinder
                    : PrimitiveType.Cube;
                var go = GameObject.CreatePrimitive(type);
                go.name = "Dream " + prop.SourceObjectId;
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                go.transform.position = _dreamAnchor.position + new Vector3((i % 4) * 0.55f - 0.8f, 0.45f, (i / 4) * 0.55f);
                go.transform.localScale = Vector3.one * 0.35f;
                go.GetComponent<Renderer>().sharedMaterial = CharacterFactory.MakeMaterial(_unlit, CharacterFactory.Hex(ColorHex(prop.Color)), 0);
                _dreams.Add(go);
            }
        }

        void CaptureVision()
        {
            if (_eye == null || _seg == null || _babies.Count == 0) return;
            Actor baby = _babies[_focus];
            Transform gaze = baby.Body.RigPivot("gaze");
            _eye.transform.SetPositionAndRotation(gaze.position, gaze.rotation);
            _seg.transform.position = _eye.transform.position;
            _seg.transform.rotation = _eye.transform.rotation;
            _eye.Render();
            if (_idShader != null) _seg.Render();
            _hud.SetPreviews(_eyeRt, _segRt);
        }

        void SmoothAndApply(Actor actor, float[] target, float dt)
        {
            if (actor.Smoothed == null || actor.Smoothed.Length != target.Length)
            {
                actor.Smoothed = (float[])target.Clone();
            }
            float blend = 1f - Mathf.Exp(-dt * 8f);
            for (int i = 0; i < target.Length; i++)
            {
                actor.Smoothed[i] = Mathf.Lerp(actor.Smoothed[i], target[i], blend);
            }
            actor.Body.Apply(actor.Smoothed, actor.Yaw);
        }

        void RememberPose(Actor actor)
        {
            float[] measured = actor.Body.Measure();
            actor.Seen.Add(new PoseFrame { SourceId = actor.Spec.Id, Time = _sim.SimSeconds, Angles = measured });
            if (actor.Seen.Count > 18) actor.Seen.RemoveAt(0);
        }

        static PoseTrace CopyTrace(Actor source, PoseMapper mapper)
        {
            var trace = new PoseTrace { SourceId = source.Spec.Id };
            if (source.Seen.Count == 0) return trace;
            float origin = source.Seen[0].Time;
            for (int i = 0; i < source.Seen.Count; i++)
            {
                float[] angles = source.Seen[i].Angles;
                if (mapper != null) angles = mapper.MapToSelf(angles);
                trace.Frames.Add(new PoseFrame
                {
                    SourceId = source.Spec.Id,
                    Time = source.Seen[i].Time - origin,
                    Angles = angles
                });
            }
            return trace;
        }

        void BuildPeople(Dictionary<string, AppearanceProfile> looks)
        {
            var parent = new GameObject("People").transform;
            for (int i = 0; i < _pop.People.Count; i++)
            {
                PersonSpec spec = _pop.People[i];
                AppearanceProfile look;
                if (!looks.TryGetValue(spec.Id, out look)) look = AppearanceCatalog.For(spec);
                if (_sim.Home != null) look.DisplayName = LivingName(spec);
                SimChild child = _sim.Find(spec.Id);
                HumanoidAnatomy anatomy = child == null ? new HumanoidAnatomy(spec.BirthSeed) : child.State.Anatomy;
                float developmentAge = child == null ? HumanoidAnatomy.AdultAgeDays : WorldSimulation.AnatomyAge(child.Body.AgeDays);
                SegmentedBody body = CharacterFactory.Build(parent, look, _unlit, anatomy, developmentAge);
                var actor = new Actor
                {
                    Spec = spec,
                    Look = look,
                    Body = body,
                    Mapper = new PoseMapper(spec.BirthSeed),
                    Yaw = spec.Group == GroupId.Boys ? 20f : -20f
                };
                int groupIndex = spec.Group == GroupId.Boys ? 0 : 1;
                actor.Pos = _anchors[groupIndex] + Slot(spec);
                if (child != null)
                {
                    BindChild(actor, child);
                    actor.Pos = new Vector3(child.X, 0f, child.Z);
                    actor.Yaw = child.Yaw;
                    actor.NextPractice = 0.4f * _babies.Count;
                    actor.NextEncounter = 1.5f + _babies.Count;
                    _babies.Add(actor);
                }
                body.transform.position = actor.Pos;
                body.ApplyGroundedGait(0f, 0f, actor.Yaw);
                _actors[spec.Id] = actor;
            }
        }

        static void BindChild(Actor actor, SimChild child)
        {
            actor.State = child.State;
            actor.Mind = child.Mind;
            actor.Wetware = child.Wetware;
        }

        void BuildProps()
        {
            var parent = new GameObject("Yard").transform;
            for (int i = 0; i < _pool.Count; i++)
            {
                ObjectInstance obj = _pool[i];
                PrimitiveType type = obj.Shape == "Sphere" ? PrimitiveType.Sphere
                    : obj.Shape == "Capsule" ? PrimitiveType.Capsule
                    : obj.Shape == "Cylinder" ? PrimitiveType.Cylinder
                    : PrimitiveType.Cube;
                var go = GameObject.CreatePrimitive(type);
                go.name = obj.Id + " " + obj.CategoryLabel;
                go.transform.SetParent(parent, false);
                float scale = type == PrimitiveType.Capsule ? 0.55f : 0.42f;
                float supportHeight = type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder ? scale : scale * 0.5f;
                go.transform.position = new Vector3(obj.WorldX, supportHeight, obj.WorldZ);
                go.transform.localScale = Vector3.one * scale;
                int numeric = i + 1;
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = CharacterFactory.MakeMaterial(_unlit, CharacterFactory.Hex(ColorHex(obj.Color)), numeric);
                if (obj.Mark == "Spot")
                {
                    CharacterFactory.Primitive(go.transform, PrimitiveType.Sphere, _unlit, Color.black, new Vector3(0.2f, 0.4f, 0.2f), Vector3.one * 0.35f, numeric);
                }
                else if (obj.Mark == "Stripe")
                {
                    CharacterFactory.Primitive(go.transform, PrimitiveType.Cube, _unlit, new Color(0.95f, 0.95f, 0.9f), Vector3.up * 0.2f, new Vector3(1.1f, 0.18f, 1.1f), numeric);
                }
                _props[obj.Id] = go.transform;
            }
        }

        void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
            var renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = CharacterFactory.MakeMaterial(_unlit, new Color(0.36f, 0.48f, 0.33f), 0);
        }

        void BuildCameras()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.nearClipPlane = 0.05f;
            Camera.main.farClipPlane = 80f;
            int w = _performance ? 96 : 176;
            int h = _performance ? 64 : 120;
            _eyeRt = new RenderTexture(w, h, 16);
            _segRt = new RenderTexture(w, h, 16);
            _eye = new GameObject("Baby Eye").AddComponent<Camera>();
            _eye.clearFlags = CameraClearFlags.SolidColor;
            _eye.backgroundColor = new Color(0.62f, 0.78f, 0.90f);
            _eye.targetTexture = _eyeRt;
            _eye.enabled = false;
            _seg = new GameObject("Baby Segmentation").AddComponent<Camera>();
            _seg.clearFlags = CameraClearFlags.SolidColor;
            _seg.backgroundColor = Color.black;
            _seg.targetTexture = _segRt;
            _seg.enabled = false;
            if (_idShader != null) _seg.SetReplacementShader(_idShader, "");
        }

        void BuildHud()
        {
            var hudGo = new GameObject("MobileHud");
            _hud = hudGo.AddComponent<MobileHud>();
            _hud.Build(_font);
            for (int i = 0; i < _babies.Count; i++)
            {
                int index = i;
                _hud.AddPersonButton(_babies[i].Look.DisplayName, delegate { SelectBaby(index); });
            }
            _hud.AddActionButton("Experiment", RunShortExperiment);
            _hud.AddSimulationEntryPoints(ToggleSimulationTray, ResetSimulationLayout);
            if (_sim.Home != null)
            {
                _hud.AddLivingEntryPoints(() => _workspace.Toggle(LifeTray), ToggleResearchPresentation);
                _hud.ShowResearch(false);
            }
            _hud.AddActionButton(_performance ? "Detail" : "Fast", TogglePerformance);
            _hud.AddActionButton("Reset", ResetBabies);
            _hud.AddActionButton("Quality", CyclePreset);
            _hud.AddActionButton("Save age", SaveAge);
            _hud.AddActionButton("Remote", ConnectRemote);
            _hud.SetResult("Tap Experiment for a short logged comparison of the three minds. Drag the view to look around. The sliders are live feelings, 0 to 1.");
        }

        void SelectBaby(int index)
        {
            if (_babies.Count == 0) return;
            _focus = index % _babies.Count;
            if (_focus < 0) _focus += _babies.Count;
            _hud.HighlightPerson(_babies[_focus].Spec.DisplayName);
        }

        void TogglePerformance()
        {
            _performance = !_performance;
            int w = _performance ? 96 : 176;
            int h = _performance ? 64 : 120;
            ReleasePreviewTextures();
            _eyeRt = new RenderTexture(w, h, 16);
            _segRt = new RenderTexture(w, h, 16);
            _eye.targetTexture = _eyeRt;
            _seg.targetTexture = _segRt;
            if (_sun != null) _sun.shadows = _performance ? LightShadows.None : LightShadows.Soft;
        }

        void RunShortExperiment()
        {
            _hud.SetResult("Running 6 seeds, 60 trials, three conditions...");
            ExperimentConfig config = ExperimentConfig.Short();
            ExperimentReport report = new BeliefExperiment().Run(config);
            string dir = Application.persistentDataPath;
            report.WriteCsv(Path.Combine(dir, "belief_trials_short.csv"));
            report.WriteSummary(Path.Combine(dir, "belief_summary.csv"));
            if (Application.isEditor)
            {
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ExperimentOutput"));
                report.WriteCsv(Path.Combine(project, "belief_trials_short.csv"));
                report.WriteSummary(Path.Combine(project, "belief_summary.csv"));
            }
            float a = report.MeanAccuracy(MindCondition.LibraryOneOnly, 45, 60);
            float b = report.MeanAccuracy(MindCondition.LibraryOneAndTwo, 45, 60);
            float c = report.MeanAccuracy(MindCondition.Full, 45, 60);
            _hud.SetResult(report.Verdict()
                + "\n\nLate window accuracy"
                + "\nA  prior only     " + a.ToString("0.000")
                + "\nB  prior + ideas  " + b.ToString("0.000")
                + "\nC  lived amendments " + c.ToString("0.000")
                + "\n\nCSV folder:\n" + dir);
        }

        void SaveAll()
        {
            try
            {
                if (_remote != null && _remote.Active) return;
                if (_archiveHouseholdBaseline)
                {
                    string source = WorldPath(), archive = source + ".pre-household";
                    if (File.Exists(source) && !File.Exists(archive)) File.Copy(source, archive);
                    _archiveHouseholdBaseline = false;
                }
                _sim.SaveCheckpoint(WorldPath());
                WriteGrowth();
            }
            catch (System.Exception error) when (error is IOException || error is InvalidDataException ||
                error is System.UnauthorizedAccessException)
            {
                _banner.text = "Save interrupted. See the console for the affected file.";
                Debug.LogError(error, this);
            }
        }

        static string WorldPath() => Path.Combine(Application.persistentDataPath, "world.checkpoint");

        void RestoreWorld()
        {
            if (_remote != null && _remote.Active) return;
            try
            {
                WorldSimulation restored = WorldSimulation.LoadCheckpoint(WorldPath());
                if (restored == null) throw new InvalidDataException("No world checkpoint exists.");
                if (restored.Population.People.Count != _actors.Count)
                    throw new InvalidDataException("The checkpoint roster differs from this scene.");
                foreach (PersonSpec person in restored.Population.People)
                {
                    if (!_actors.TryGetValue(person.Id, out Actor actor) ||
                        person.BirthSeed != actor.Spec.BirthSeed || person.Role != actor.Spec.Role)
                        throw new InvalidDataException("The checkpoint roster differs from this scene.");
                }
                _sim = restored;
                EnableLivingPresentation();
                _pop = restored.Population;
                _preset = restored.Preset;
                _day = restored.DayIndex;
                _night = restored.IsNight;
                _governor = new TimeGovernor { Paused = true };
                foreach (Actor actor in _actors.Values)
                {
                    actor.Spec = _pop.Find(actor.Spec.Id);
                    SimChild child = _sim.Find(actor.Spec.Id);
                    if (child != null)
                    {
                        BindChild(actor, child);
                        actor.Pos = new Vector3(child.X, 0f, child.Z);
                        actor.Yaw = child.Yaw;
                        actor.GaitDistance = 0f;
                    }
                }
                foreach (GameObject dream in _dreams) Destroy(dream);
                _dreams.Clear();
                StepActors(WorldSimulation.Dt);
                _banner.text = "World restored. Simulation paused.";
            }
            catch (System.Exception error) when (error is IOException || error is InvalidDataException ||
                error is System.UnauthorizedAccessException)
            {
                _banner.text = "Restore failed. The current world is still active.";
                Debug.LogError(error, this);
            }
        }

        void ResetBabies()
        {
            if (_remote != null && _remote.Active) return;
            for (int i = 0; i < _babies.Count; i++)
            {
                Actor baby = _babies[i];
                BindChild(baby, _sim.ResetLearnerForResearch(baby.Spec.Id, "Reset requested in the Unity HUD."));
            }
            _encounters = 0;
            _ruleAnnounced = false;
            _banner.text = "Newborns. Same seeds, empty memory. Generation 0.";
            SaveAll();
        }

        void RefreshHud()
        {
            if (_sim.Home != null && !_researchPresentation) return;
            if (_babies.Count == 0) return;
            Actor baby = _babies[_focus];
            EmotionState mood = baby.State.Emotions;
            string phase = _night ? "Night / dream" : "Awake";
            PersonSpec model = _pop.PrimaryRoleModel(baby.Spec);
            string leader = model != null ? model.DisplayName : "";
            float age = baby.State.Physiology != null ? baby.State.Physiology.AgeDays : 0f;
            float hr = baby.State.Physiology != null ? baby.State.Physiology.HeartRate : 0f;
            _hud.SetStatus(
                baby.Look.DisplayName + "  ·  " + baby.Spec.Sex + "  ·  gen " + baby.State.Generation,
                "Day " + (_day + 1) + "  " + phase
                + "   age " + age.ToString("0.00") + " d"
                + "   role model " + leader
                + "   " + SpeedLabel()
                + "\nhr " + hr.ToString("0")
                + "   walk " + baby.State.Walker.BestDistance.ToString("0.00")
                + "   beliefs " + baby.State.Library.Count
                + "   appendices " + baby.State.Library.AppendixCount
                + "   vision " + (_performance ? "fast" : "detail")
                + "   wetware replay rows " + _replayRows
                + (_idShader == null ? "   (no ID shader)" : ""));
            _hud.SetEmotions(mood.Curiosity, mood.Frustration, mood.Satisfaction, mood.FearOfFalling, mood.Fatigue, mood.Valence, mood.Arousal, mood.SleepPressure);
            _hud.SetBeliefs(BeliefText(baby));
            _hud.HighlightPerson(baby.Look.DisplayName);
        }

        static string BeliefText(Actor baby)
        {
            var sb = new StringBuilder();
            List<BeliefRecord> beliefs = baby.State.Library.All();
            int shown = 0;
            for (int i = beliefs.Count - 1; i >= 0 && shown < 8; i--)
            {
                BeliefRecord belief = beliefs[i];
                sb.Append(belief.Kind);
                if (belief.Retired) sb.Append(" retired");
                sb.Append("  ");
                sb.Append(belief.Confidence.ToString("0.00"));
                sb.Append("  ");
                sb.Append(belief.FullStatement);
                sb.Append("\n");
                int start = belief.Appendices.Count - 2;
                if (start < 0) start = 0;
                for (int a = start; a < belief.Appendices.Count; a++)
                {
                    AppendixEntry entry = belief.Appendices[a];
                    sb.Append("  app ");
                    sb.Append(entry.Timestamp);
                    sb.Append(" ");
                    sb.Append(entry.Reason);
                    sb.Append(": ");
                    sb.Append(entry.OldStatement);
                    sb.Append(" -> ");
                    sb.Append(entry.NewStatement);
                    sb.Append("\n");
                }
                shown++;
            }
            if (shown == 0) sb.Append("No lived beliefs yet. The baby is still using the general prior.");
            MotorMemory walk = baby.State.Muscles.GetOrNull("walk-sequence");
            if (walk != null)
            {
                sb.Append("\nMuscle memory: ");
                sb.Append(walk.Statement);
                sb.Append("  (");
                sb.Append(walk.Appendices.Count);
                sb.Append(" notes)");
            }
            return sb.ToString();
        }

        void LogGrowth(Actor baby, string phase)
        {
            EmotionState mood = baby.State.Emotions;
            float similarity = 0f;
            PersonSpec leader = _pop.LeaderOf(baby.Spec.Group);
            Actor leaderActor;
            if (leader != null && _actors.TryGetValue(leader.Id, out leaderActor) && leaderActor.Seen.Count > 0)
            {
                similarity = baby.State.Walker.Similarity(CopyTrace(leaderActor, baby.Mapper));
            }
            _growth.Add(new GrowthSample
            {
                Day = _day,
                AgentId = baby.Spec.Id,
                Generation = baby.State.Generation,
                MotorDistance = baby.State.Walker.BestDistance,
                RomCoverage = baby.State.Muscles.RomCoverage(),
                BeliefAccuracy = baby.State.BeliefAccuracy,
                GaitSimilarity = similarity,
                Curiosity = mood.Curiosity,
                Frustration = mood.Frustration,
                Satisfaction = mood.Satisfaction,
                Fear = mood.FearOfFalling,
                Fatigue = mood.Fatigue,
                Valence = mood.Valence,
                Arousal = mood.Arousal,
                SleepPressure = mood.SleepPressure,
                Phase = phase
            });
        }

        void WriteGrowth()
        {
            var header = new List<string>
            {
                "day", "agent_id", "generation", "phase", "motor_distance", "rom_coverage",
                "belief_accuracy", "gait_similarity", "curiosity", "frustration", "satisfaction",
                "fear", "fatigue", "valence", "arousal", "sleep_pressure"
            };
            var rows = new List<IList<string>>();
            for (int i = 0; i < _growth.Count; i++)
            {
                GrowthSample g = _growth[i];
                rows.Add(new List<string>
                {
                    g.Day.ToString(), g.AgentId, g.Generation.ToString(), g.Phase,
                    CsvTable.Format(g.MotorDistance), CsvTable.Format(g.RomCoverage),
                    CsvTable.Format(g.BeliefAccuracy), CsvTable.Format(g.GaitSimilarity),
                    CsvTable.Format(g.Curiosity), CsvTable.Format(g.Frustration),
                    CsvTable.Format(g.Satisfaction), CsvTable.Format(g.Fear),
                    CsvTable.Format(g.Fatigue), CsvTable.Format(g.Valence),
                    CsvTable.Format(g.Arousal), CsvTable.Format(g.SleepPressure)
                });
            }
            CsvTable.Write(Path.Combine(Application.persistentDataPath, "growth_days.csv"), header, rows);
        }

        void OrbitCamera()
        {
            if (Camera.main == null || _babies.Count == 0) return;
            DreamInput.Sample();
            bool overUi = false;
            if (EventSystem.current != null)
            {
                overUi = EventSystem.current.IsPointerOverGameObject();
            }
            if (!overUi && DreamInput.TouchCount == 1)
            {
                _orbitYaw += DreamInput.PointerDelta.x * 0.15f;
                _orbitPitch -= DreamInput.PointerDelta.y * 0.1f;
            }
            else if (!overUi && DreamInput.PointerHeld)
            {
                Vector3 delta = (Vector3)DreamInput.PointerPosition - _lastPointer;
                if (DreamInput.PointerPressed) delta = Vector3.zero;
                _orbitYaw += delta.x * 0.2f;
                _orbitPitch -= delta.y * 0.12f;
            }
            _lastPointer = DreamInput.PointerPosition;
            float scroll = DreamInput.ScrollY;
            if (Mathf.Abs(scroll) > 0.01f) _zoom -= scroll * 0.02f;
            _orbitPitch = Mathf.Clamp(_orbitPitch, 12f, 70f);
            _zoom = Mathf.Clamp(_zoom, 3.5f, 16f);
            Transform target = _babies[_focus].Body.transform;
            Vector3 look = target.position + Vector3.up * 0.9f;
            if (_sim.Home != null && !_researchPresentation) look = new Vector3(0f, .63f, 1.27f);
            Vector3 offset = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f) * new Vector3(0f, 0f, -_zoom);
            Camera.main.transform.position = look + offset;
            Camera.main.transform.LookAt(look);
        }

        void BillboardLabels()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            if (_banner != null) _banner.transform.rotation = camera.transform.rotation;
            foreach (Actor actor in _actors.Values)
            {
                Transform label = actor.Body.transform.Find("Label");
                if (label == null) continue;
                label.rotation = camera.transform.rotation;
            }
        }

        Dictionary<string, AppearanceProfile> LoadLooks()
        {
            var map = new Dictionary<string, AppearanceProfile>();
            TextAsset asset = Resources.Load<TextAsset>("appearances");
            if (asset == null) return map;
            try
            {
                List<AppearanceProfile> list = AppearanceCatalog.FromJson(asset.text);
                for (int i = 0; i < list.Count; i++) map[list[i].Id] = list[i];
            }
            catch (System.Exception)
            {
                // The built-in catalog still dresses everyone if the file is edited badly.
            }
            return map;
        }

        static Vector3 Slot(PersonSpec spec)
        {
            if (spec.Id == "father") return new Vector3(-0.55f, 0f, -0.4f);
            if (spec.Id == "mother") return new Vector3(0.55f, 0f, -0.4f);
            if (spec.Id == "son") return new Vector3(-0.45f, 0f, 0.55f);
            if (spec.Id == "daughter") return new Vector3(0.45f, 0f, 0.55f);
            return new Vector3(spec.Role == MemberRole.Baby ? 0.8f : 0f, 0f, 0f);
        }

        static string ColorHex(string name)
        {
            if (name == "Red") return "#C44536";
            if (name == "Blue") return "#3D6F9A";
            if (name == "Green") return "#3E7A55";
            if (name == "Yellow") return "#E1A322";
            return "#888888";
        }

        static string AgentPath(string id)
        {
            return Path.Combine(Application.persistentDataPath, "agents", id + ".json");
        }

        static void Trim(Actor actor)
        {
            while (actor.State.Observations.Count > 64)
            {
                actor.State.Observations.RemoveAt(0);
            }
        }

        void EnsureSun()
        {
#if UNITY_2023_1_OR_NEWER
            _sun = Object.FindAnyObjectByType<Light>();
#else
            _sun = Object.FindObjectOfType<Light>();
#endif
            if (_sun == null)
            {
                var go = new GameObject("Sun");
                _sun = go.AddComponent<Light>();
                _sun.type = LightType.Directional;
                go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            }
            _sun.shadows = LightShadows.None;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);
        }

        void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
#else
            if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void ConnectRemote()
        {
            if (_remote == null) _remote = gameObject.AddComponent<RemoteView>();
            _remote.Connect("127.0.0.1", 4077);
        }

        void ApplyRemote(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3 || _sim == null) return;
            float simSeconds;
            if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out simSeconds))
            {
                _sim.SimSeconds = simSeconds;
            }
            _night = parts[2] == "1";
            for (int i = 3; i + 4 < parts.Length; i += 5)
            {
                SimChild child = _sim.Find(parts[i]);
                if (child == null) continue;
                float x, z, age, hr;
                if (!float.TryParse(parts[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) continue;
                if (!float.TryParse(parts[i + 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) continue;
                if (!float.TryParse(parts[i + 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out age)) continue;
                if (!float.TryParse(parts[i + 4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out hr)) continue;
                child.X = x;
                child.Z = z;
                child.Body.AgeDays = age;
                child.Body.HeartRate = hr;
            }
        }

        void CyclePreset()
        {
            if (_preset == QualityPreset.Phone) _preset = QualityPreset.Laptop;
            else if (_preset == QualityPreset.Laptop) _preset = QualityPreset.Beefy;
            else _preset = QualityPreset.Phone;
        }

        void SaveAge()
        {
            if (_sim == null) return;
            string stage = "age";
            float age = _sim.MinAgeDays();
            if (age >= WorldSimulation.AgeOf(LifeStage.Adult)) stage = "adult";
            else if (age >= WorldSimulation.AgeOf(LifeStage.Teen)) stage = "teen";
            else if (age >= WorldSimulation.AgeOf(LifeStage.Child)) stage = "child";
            else if (age >= WorldSimulation.AgeOf(LifeStage.Toddler)) stage = "toddler";
            else stage = "newborn";
            string dir = Path.Combine(Application.persistentDataPath, "stages", stage);
            try
            {
                _sim.SaveCheckpoint(Path.Combine(dir, "world.checkpoint"));
                if (_hud != null) _hud.SetResult("Saved stage " + stage + " (" + _sim.TickCount + " ticks) to " + dir);
            }
            catch (System.Exception error) when (error is IOException || error is InvalidDataException ||
                error is System.UnauthorizedAccessException)
            {
                if (_hud != null) _hud.SetResult("Stage save failed. See the console.");
                Debug.LogError(error, this);
            }
        }

        void BuildHome()
        {
            var home = GameObject.CreatePrimitive(PrimitiveType.Cube);
            home.name = "Home";
            home.transform.position = new Vector3(0f, 0.7f, YardClearance.HouseZ);
            home.transform.localScale = new Vector3(2f * YardClearance.HouseHalfX, 1.4f, 2f * YardClearance.HouseHalfZ);
            home.GetComponent<Renderer>().sharedMaterial = CharacterFactory.MakeMaterial(_unlit, new Color(0.55f, 0.42f, 0.32f), 0);
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.position = new Vector3(0f, 1.55f, -1.4f);
            roof.transform.localScale = new Vector3(2.7f, 0.28f, 1.9f);
            roof.GetComponent<Renderer>().sharedMaterial = CharacterFactory.MakeMaterial(_unlit, new Color(0.45f, 0.22f, 0.18f), 0);
        }

        void OnDestroy()
        {
            _workspace?.Dispose();
            ReleasePreviewTextures();
        }

        void ReleasePreviewTextures()
        {
            if (_eye != null) _eye.targetTexture = null;
            if (_seg != null) _seg.targetTexture = null;
            if (_eyeRt != null)
            {
                _eyeRt.Release();
                Destroy(_eyeRt);
                _eyeRt = null;
            }
            if (_segRt != null)
            {
                _segRt.Release();
                Destroy(_segRt);
                _segRt = null;
            }
        }

        void ToggleSimulationTray() => _workspace?.Toggle(SimulationTrayId);

        static TrayWorkspace.Layout DefaultSimulationTray() => new TrayWorkspace.Layout
        {
            AnchorMin = new Vector2(0.02f, 0.74f),
            AnchorMax = new Vector2(Screen.width >= Screen.height ? 0.56f : 0.98f, 0.89f),
            Visible = true
        };

        void ResetSimulationLayout() => _workspace?.SetLayout(SimulationTrayId, DefaultSimulationTray());

        void BuildSimulationControls(RectTransform bar)
        {
            _timeLabel = NewLabel(bar, "Clock", 20);
            var clock = _timeLabel.rectTransform;
            clock.anchorMin = new Vector2(0.02f, 0.49f);
            clock.anchorMax = new Vector2(0.98f, 0.98f);
            clock.offsetMin = clock.offsetMax = Vector2.zero;
            AddTimeButton(bar, "Pause", 0f, 0.14f, () => _governor.Paused = true);
            AddTimeButton(bar, "Resume", 0.14f, 0.28f, () => _governor.Paused = false);
            AddTimeButton(bar, "Step", 0.28f, 0.40f, RequestLocalStep);
            AddTimeButton(bar, "Slower", 0.40f, 0.55f, () => SetScale(Mathf.Max(0.1f, _governor.TargetScale / 10f)));
            AddTimeButton(bar, "Faster", 0.55f, 0.70f, () => SetScale(Mathf.Min(1000f, _governor.TargetScale * 10f)));
            AddTimeButton(bar, "Save", 0.70f, 0.77f, SaveAll);
            AddTimeButton(bar, "Restore", 0.77f, 0.88f, RestoreWorld);
            AddTimeButton(bar, "Close", 0.88f, 1f, ToggleSimulationTray);
        }

        Text _timeLabel;

        void AddTimeButton(Transform parent, string name, float x0, float x1, UnityEngine.Events.UnityAction act)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(x0, 0.02f);
            rect.anchorMax = new Vector2(x1, 0.48f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            go.AddComponent<Image>().color = new Color(0.2f, 0.24f, 0.3f, 1f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(act);
            var label = NewLabel(go.transform, "T", 18);
            label.text = name;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
        }

        Text NewLabel(Transform parent, string name, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void SetScale(float scale)
        {
            _governor.CatchingUp = false;
            _governor.TargetScale = scale;
            _governor.Accumulator = 0f;
        }

        void RequestLocalStep()
        {
            if (_remote == null || !_remote.Active) _governor.RequestSingleStep();
        }

        void HandleTimeKeys()
        {
            if (DreamInput.WasPressed(KeyCode.N)) RequestLocalStep();
            if (DreamInput.WasPressed(KeyCode.F6)) ToggleSimulationTray();
            if (DreamInput.WasPressed(KeyCode.F7)) ResetSimulationLayout();
            if (DreamInput.WasPressed(KeyCode.Space)) _governor.Paused = !_governor.Paused;
            if (DreamInput.WasPressed(KeyCode.Comma)) SetScale(Mathf.Max(0.1f, _governor.TargetScale * 0.1f));
            if (DreamInput.WasPressed(KeyCode.Period)) SetScale(Mathf.Min(1000f, Mathf.Max(0.1f, _governor.TargetScale) * 10f));
            if (DreamInput.WasPressed(KeyCode.Alpha1)) SetScale(1f);
            if (DreamInput.WasPressed(KeyCode.F1)) JumpTo(LifeStage.Newborn);
            if (DreamInput.WasPressed(KeyCode.F2)) JumpTo(LifeStage.Toddler);
            if (DreamInput.WasPressed(KeyCode.F3)) JumpTo(LifeStage.Child);
            if (DreamInput.WasPressed(KeyCode.F4)) JumpTo(LifeStage.Teen);
            if (DreamInput.WasPressed(KeyCode.F5)) JumpTo(LifeStage.Adult);
        }

        void JumpTo(LifeStage stage)
        {
            if (stage == LifeStage.Newborn) return;
            _governor.CatchStage = stage;
            _governor.CatchingUp = true;
            _governor.Paused = false;
        }

        string SpeedLabel()
        {
            string mode = _governor.Paused ? "paused (" + _governor.TargetScale.ToString("0.##") + "x)" : _governor.CatchingUp ? "full-sim to " + _governor.CatchStage : _governor.TargetScale.ToString("0.##") + "x asked";
            return mode + "  achieved " + _achieved.ToString("0.0") + "x  " + _ticksPerSec.ToString("0") + " ticks/s  " + _preset;
        }

        void RefreshElementMarks()
        {
            if (_sim == null || _unlit == null) return;
            if (_preset == QualityPreset.Beefy) ColorizeGrid();
            int shown = 0;
            int n = _sim.Elements.Temperature.Length;
            for (int i = 0; i < n && shown < 36; i++)
            {
                bool hot = _sim.Elements.Burning[i] != 0 || _sim.Elements.Temperature[i] > 120f;
                bool wet = _sim.Elements.Water[i] > 0.2f;
                bool smoke = _sim.Elements.Smoke[i] > 0.12f;
                if (!hot && !wet && !smoke) continue;
                while (_elementMarks.Count <= shown)
                {
                    var mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mark.name = "Element";
                    var col = mark.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    _elementMarks.Add(mark);
                }
                int x = i % _sim.Elements.Width;
                int y = i / _sim.Elements.Width;
                float wx = (x + 0.5f) / _sim.Elements.Width * 16f - 8f;
                float wz = (y + 0.5f) / _sim.Elements.Height * 16f - 8f;
                Transform markTransform = _elementMarks[shown].transform;
                markTransform.gameObject.SetActive(true);
                markTransform.position = new Vector3(wx, hot ? 0.45f : 0.18f, wz);
                float s = hot ? 0.35f : wet ? 0.28f : 0.22f;
                markTransform.localScale = Vector3.one * s;
                Color color = hot ? new Color(1f, 0.35f, 0.05f) : wet ? new Color(0.2f, 0.45f, 0.9f) : new Color(0.7f, 0.7f, 0.72f);
                _elementMarks[shown].GetComponent<Renderer>().sharedMaterial = CharacterFactory.MakeMaterial(_unlit, color, 0);
                shown++;
            }
            for (int i = shown; i < _elementMarks.Count; i++) _elementMarks[i].SetActive(false);
        }

        void RefreshSimulationControls()
        {
            _timeLabel.text = "Day " + (_sim.DayIndex + 1)
                + "   tick " + _sim.TickCount + "   " + _sim.SimSeconds.ToString("0.00") + " sim s   "
                + SpeedLabel();
        }

        void ColorizeGrid()
        {
            int n = _sim.Elements.Temperature.Length;
            var temp = new NativeArray<float>(_sim.Elements.Temperature, Allocator.TempJob);
            var water = new NativeArray<float>(_sim.Elements.Water, Allocator.TempJob);
            var smoke = new NativeArray<float>(_sim.Elements.Smoke, Allocator.TempJob);
            var rgba = new NativeArray<float>(n * 4, Allocator.TempJob);
            var job = new GridColorJob
            {
                Temperature = temp,
                Water = water,
                Smoke = smoke,
                Rgba = rgba
            };
            job.Schedule(n, 32).Complete();
            temp.Dispose();
            water.Dispose();
            smoke.Dispose();
            rgba.Dispose();
        }

        TextMesh CreateBanner()
        {
            var go = new GameObject("Banner");
            go.transform.position = new Vector3(0f, 2.05f, YardClearance.HouseZ);
            var text = go.AddComponent<TextMesh>();
            text.characterSize = 0.025f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 0.96f, 0.86f);
            text.text = "Day " + (_day + 1) + " · " + (_night ? "Night" : "Awake");
            return text;
        }

    }
}
