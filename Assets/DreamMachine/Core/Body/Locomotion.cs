using System;
using System.Collections.Generic;
using DreamMachine.Libraries;
using DreamMachine.Util;

namespace DreamMachine.Body
{
    /// <summary>Joint order used by poses, muscle memory, and the Unity body.</summary>
    public static class JointNames
    {
        public const int LeftHip = 0;
        public const int LeftKnee = 1;
        public const int RightHip = 2;
        public const int RightKnee = 3;
        public const int LeftShoulder = 4;
        public const int LeftElbow = 5;
        public const int RightShoulder = 6;
        public const int RightElbow = 7;
        public const int Head = 8;
        public const int Torso = 9;
        public const int Count = 10;

        public static readonly string[] All =
        {
            "leftHip", "leftKnee", "rightHip", "rightKnee",
            "leftShoulder", "leftElbow", "rightShoulder", "rightElbow",
            "head", "torso"
        };
    }

    public struct BodyPose
    {
        public float LeftHip;
        public float LeftKnee;
        public float RightHip;
        public float RightKnee;
        public float LeftShoulder;
        public float LeftElbow;
        public float RightShoulder;
        public float RightElbow;
        public float HeadPitch;
        public float TorsoPitch;
        public float RootX;

        public float[] ToArray()
        {
            return new[]
            {
                LeftHip, LeftKnee, RightHip, RightKnee,
                LeftShoulder, LeftElbow, RightShoulder, RightElbow,
                HeadPitch, TorsoPitch
            };
        }
    }

    public sealed class PoseFrame
    {
        public string SourceId;
        public float Time;
        public float[] Angles;
    }

    /// <summary>
    /// What a baby can perceive: joint angles over time.
    /// No gait parameters, no animation curves, no mentor code.
    /// </summary>
    public sealed class PoseTrace
    {
        public string SourceId;
        public List<PoseFrame> Frames = new List<PoseFrame>();
    }

    public sealed class MotorMemory
    {
        public string Id;
        public string Statement;
        public float Confidence;
        public float MinAngle;
        public float MaxAngle;
        public bool Retired;
        public List<AppendixEntry> Appendices = new List<AppendixEntry>();
    }

    /// <summary>
    /// Muscle memory. Ranges and successful sequences are amended, never deleted.
    /// </summary>
    public sealed class MuscleMemoryLibrary
    {
        readonly Dictionary<string, MotorMemory> _items = new Dictionary<string, MotorMemory>();

        public IReadOnlyDictionary<string, MotorMemory> Items { get { return _items; } }
        public int Count { get { return _items.Count; } }

        public MotorMemory GetOrNull(string id)
        {
            MotorMemory item;
            return _items.TryGetValue(id, out item) ? item : null;
        }

        public void NoteRange(string joint, float minAngle, float maxAngle, int day, string evidence)
        {
            MotorMemory item;
            if (!_items.TryGetValue(joint, out item))
            {
                item = new MotorMemory
                {
                    Id = joint,
                    Statement = "unmapped",
                    MinAngle = minAngle,
                    MaxAngle = maxAngle,
                    Confidence = 0.1f
                };
                _items[joint] = item;
            }
            float oldMin = item.MinAngle;
            float oldMax = item.MaxAngle;
            bool expanded = minAngle < item.MinAngle - 0.02f || maxAngle > item.MaxAngle + 0.02f;
            if (minAngle < item.MinAngle) item.MinAngle = minAngle;
            if (maxAngle > item.MaxAngle) item.MaxAngle = maxAngle;
            float span = item.MaxAngle - item.MinAngle;
            float confidence = span / 1.4f;
            if (confidence > 1f) confidence = 1f;
            if (item.Appendices.Count == 0 || expanded || Math.Abs(item.Confidence - confidence) >= 0.05f)
            {
                item.Appendices.Add(new AppendixEntry
                {
                    Timestamp = day,
                    Evidence = evidence,
                    OldStatement = item.Statement,
                    NewStatement = "reachable " + item.MinAngle.ToString("0.00") + " to " + item.MaxAngle.ToString("0.00"),
                    OldConfidence = item.Confidence,
                    NewConfidence = confidence,
                    Reason = item.Appendices.Count == 0 ? "first-commitment" : (expanded ? "amend" : "reinforce")
                });
                item.Statement = "reachable " + item.MinAngle.ToString("0.00") + " to " + item.MaxAngle.ToString("0.00");
                item.Confidence = confidence;
            }
            if (oldMin == item.MinAngle && oldMax == item.MaxAngle && confidence < 0.05f)
            {
                item.Retired = true;
            }
        }

        public void NoteSequence(string id, float confidence, int day, string evidence)
        {
            MotorMemory item;
            if (!_items.TryGetValue(id, out item))
            {
                item = new MotorMemory { Id = id, Statement = "", Confidence = 0f };
                _items[id] = item;
            }
            string statement = "sequence confidence " + confidence.ToString("0.00");
            if (item.Appendices.Count == 0 || Math.Abs(item.Confidence - confidence) >= 0.04f)
            {
                item.Appendices.Add(new AppendixEntry
                {
                    Timestamp = day,
                    Evidence = evidence,
                    OldStatement = item.Statement,
                    NewStatement = statement,
                    OldConfidence = item.Confidence,
                    NewConfidence = confidence,
                    Reason = item.Appendices.Count == 0 ? "first-commitment" : "amend"
                });
                item.Statement = statement;
                item.Confidence = confidence;
            }
            item.Retired = confidence < 0.05f;
        }

        public float RomCoverage()
        {
            float sum = 0f;
            int n = 0;
            for (int i = 0; i < JointNames.Count; i++)
            {
                MotorMemory item = GetOrNull("rom:" + JointNames.All[i]);
                if (item == null) continue;
                float span = item.MaxAngle - item.MinAngle;
                float cover = span / 1.2f;
                if (cover > 1f) cover = 1f;
                sum += cover;
                n++;
            }
            return n == 0 ? 0f : sum / JointNames.Count;
        }

        public List<object> ToData()
        {
            var rows = new List<object>();
            foreach (var pair in _items)
            {
                MotorMemory m = pair.Value;
                var apps = new List<object>();
                for (int i = 0; i < m.Appendices.Count; i++)
                {
                    AppendixEntry e = m.Appendices[i];
                    apps.Add(new Dictionary<string, object>
                    {
                        { "timestamp", e.Timestamp },
                        { "evidence", e.Evidence ?? "" },
                        { "oldStatement", e.OldStatement ?? "" },
                        { "newStatement", e.NewStatement ?? "" },
                        { "oldConfidence", e.OldConfidence },
                        { "newConfidence", e.NewConfidence },
                        { "reason", e.Reason ?? "" }
                    });
                }
                rows.Add(new Dictionary<string, object>
                {
                    { "id", m.Id ?? "" },
                    { "statement", m.Statement ?? "" },
                    { "confidence", m.Confidence },
                    { "minAngle", m.MinAngle },
                    { "maxAngle", m.MaxAngle },
                    { "retired", m.Retired },
                    { "appendices", apps }
                });
            }
            return rows;
        }

        public static MuscleMemoryLibrary FromData(List<object> rows)
        {
            var lib = new MuscleMemoryLibrary();
            if (rows == null) return lib;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = MiniJson.AsObject(rows[i]);
                var item = new MotorMemory
                {
                    Id = MiniJson.AsString(row["id"]),
                    Statement = MiniJson.AsString(row["statement"]),
                    Confidence = MiniJson.AsFloat(row["confidence"]),
                    MinAngle = row.ContainsKey("minAngle") ? MiniJson.AsFloat(row["minAngle"]) : 0f,
                    MaxAngle = row.ContainsKey("maxAngle") ? MiniJson.AsFloat(row["maxAngle"]) : 0f,
                    Retired = row.ContainsKey("retired") && MiniJson.AsBool(row["retired"])
                };
                if (row.ContainsKey("appendices"))
                {
                    List<object> apps = MiniJson.AsArray(row["appendices"]);
                    for (int a = 0; a < apps.Count; a++)
                    {
                        var e = MiniJson.AsObject(apps[a]);
                        item.Appendices.Add(new AppendixEntry
                        {
                            Timestamp = MiniJson.AsInt(e["timestamp"]),
                            Evidence = MiniJson.AsString(e["evidence"]),
                            OldStatement = MiniJson.AsString(e["oldStatement"]),
                            NewStatement = MiniJson.AsString(e["newStatement"]),
                            OldConfidence = MiniJson.AsFloat(e["oldConfidence"]),
                            NewConfidence = MiniJson.AsFloat(e["newConfidence"]),
                            Reason = MiniJson.AsString(e["reason"])
                        });
                    }
                }
                lib._items[item.Id] = item;
            }
            return lib;
        }
    }

    /// <summary>
    /// Sagittal body. The baby does not play a walk animation.
    /// A genome of joint keyframes is babble at first, then hill-climbed.
    /// Distance comes only from this physics.
    /// </summary>
    public sealed class WalkLearner
    {
        public const int Frames = 8;
        public const int LegJoints = 4;
        public const int Genes = Frames * LegJoints + 1;

        public float[] Genome;
        public float BestDistance;
        public float InitialDistance;
        public bool RecordRolloutRom = true;
        public MuscleMemoryLibrary Memory = new MuscleMemoryLibrary();
        readonly int _seed;
        float[] _scratch;

        public WalkLearner(int birthSeed)
        {
            _seed = birthSeed;
            Genome = RandomGenome(new SeededRandom(birthSeed ^ 0x5A11));
            InitialDistance = Evaluate(Genome, birthSeed, null);
            BestDistance = InitialDistance;
        }

        public void Babble(SeededRandom rng, int samplesPerJoint)
        {
            for (int j = 0; j < JointNames.Count; j++)
            {
                float min = 0f;
                float max = 0f;
                for (int s = 0; s < samplesPerJoint; s++)
                {
                    float angle = rng.Range(-1.1f, 1.2f);
                    if (s == 0 || angle < min) min = angle;
                    if (s == 0 || angle > max) max = angle;
                }
                Memory.NoteRange("rom:" + JointNames.All[j], min, max, 0, "motor babble of " + JointNames.All[j]);
            }
        }

        public void Practice(int iterations, float exploration, SeededRandom rng, PoseTrace imitate, float imitateWeight, float forceScale = 1f)
        {
            if (imitate != null && imitateWeight > 0f)
            {
                BlendToward(imitate, imitateWeight);
            }
            if (forceScale < 0.2f) forceScale = 0.2f;
            if (forceScale > 1.25f) forceScale = 1.25f;
            float explore = exploration;
            if (explore < 0.05f) explore = 0.05f;
            if (explore > 1.5f) explore = 1.5f;
            if (_scratch == null || _scratch.Length != Genes) _scratch = new float[Genes];
            for (int gen = 0; gen < iterations; gen++)
            {
                float sigma = 0.45f * (float)Math.Pow(0.96, gen) * explore;
                float champ = BestDistance;
                int children = 8;
                for (int k = 0; k < children; k++)
                {
                    MutateInto(Genome, _scratch, rng, sigma);
                    float distance = Evaluate(_scratch, _seed, RecordRolloutRom ? Memory : null, forceScale);
                    if (distance > champ)
                    {
                        champ = distance;
                        Array.Copy(_scratch, Genome, Genes);
                    }
                }
                BestDistance = champ;
            }
            float confidence = BestDistance / 3f;
            if (confidence < 0f) confidence = 0f;
            if (confidence > 1f) confidence = 1f;
            Memory.NoteSequence("walk-sequence", confidence, iterations, "practice distance " + BestDistance.ToString("0.00"));
        }

        public BodyPose PoseAt(float time)
        {
            float speed = SpeedOf(Genome);
            float phase = time * speed;
            phase = phase - (float)Math.Floor(phase);
            float[] legs = SampleLegs(Genome, phase);
            float hipDiff = legs[0] - legs[2];
            return new BodyPose
            {
                LeftHip = legs[0],
                LeftKnee = legs[1],
                RightHip = legs[2],
                RightKnee = legs[3],
                LeftShoulder = -0.6f * legs[0],
                LeftElbow = 0.4f + 0.3f * Math.Abs(legs[0]),
                RightShoulder = -0.6f * legs[2],
                RightElbow = 0.4f + 0.3f * Math.Abs(legs[2]),
                HeadPitch = -0.15f * hipDiff,
                TorsoPitch = 0.1f * (legs[0] + legs[2]),
                RootX = 0f
            };
        }

        public float Similarity(PoseTrace trace)
        {
            if (trace == null || trace.Frames.Count == 0) return 0f;
            float best = 0f;
            for (int shift = 0; shift < Frames; shift++)
            {
                float err = 0f;
                int n = 0;
                for (int f = 0; f < Frames; f++)
                {
                    float phase = (f + 0.5f) / Frames;
                    float[] mine = SampleLegs(Genome, phase);
                    float fraction = ((f + shift) % Frames + 0.5f) / Frames;
                    float[] seen = AnglesAtFraction(trace, fraction);
                    int joints = mine.Length < seen.Length ? mine.Length : seen.Length;
                    if (joints > 4) joints = 4;
                    for (int j = 0; j < joints; j++)
                    {
                        float d = mine[j] - seen[j];
                        err += d < 0f ? -d : d;
                        n++;
                    }
                }
                if (n == 0) continue;
                float sim = (float)Math.Exp(-(err / n) * 2.2f);
                if (sim > best) best = sim;
            }
            return best;
        }

        static float[] AnglesAtFraction(PoseTrace trace, float fraction)
        {
            float end = trace.Frames[trace.Frames.Count - 1].Time;
            if (end <= 0f) end = 1f;
            float time = fraction * end;
            PoseFrame nearest = trace.Frames[0];
            float best = 999f;
            for (int i = 0; i < trace.Frames.Count; i++)
            {
                float d = Math.Abs(trace.Frames[i].Time - time);
                if (d < best)
                {
                    best = d;
                    nearest = trace.Frames[i];
                }
            }
            return nearest.Angles ?? new float[4];
        }

        public void WritePose(float time, float[] dest)
        {
            if (dest == null || dest.Length < 10) return;
            BodyPose pose = PoseAt(time);
            dest[0] = pose.LeftHip;
            dest[1] = pose.LeftKnee;
            dest[2] = pose.RightHip;
            dest[3] = pose.RightKnee;
            dest[4] = pose.LeftShoulder;
            dest[5] = pose.LeftElbow;
            dest[6] = pose.RightShoulder;
            dest[7] = pose.RightElbow;
            dest[8] = pose.HeadPitch;
            dest[9] = pose.TorsoPitch;
        }

        public static float Evaluate(float[] genome, int noiseSeed, MuscleMemoryLibrary memory)
        {
            return Evaluate(genome, noiseSeed, memory, 1f);
        }

        public static float Evaluate(float[] genome, int noiseSeed, MuscleMemoryLibrary memory, float forceScale)
        {
            float x = 0f;
            float vx = 0f;
            float pitch = 0.05f;
            float pv = 0f;
            float phase = 0f;
            float speed = SpeedOf(genome);
            bool fallen = false;
            float dt = 0.05f;
            int steps = 80;
            var rng = new SeededRandom(noiseSeed ^ 0x0C05);
            float min0 = 2f, min1 = 2f, min2 = 2f, min3 = 2f;
            float max0 = -2f, max1 = -2f, max2 = -2f, max3 = -2f;
            var j = new float[4];
            for (int s = 0; s < steps; s++)
            {
                phase += speed * dt;
                phase = phase - (float)Math.Floor(phase);
                SampleLegsInto(genome, phase, j);
                if (j[0] < min0) min0 = j[0];
                if (j[1] < min1) min1 = j[1];
                if (j[2] < min2) min2 = j[2];
                if (j[3] < min3) min3 = j[3];
                if (j[0] > max0) max0 = j[0];
                if (j[1] > max1) max1 = j[1];
                if (j[2] > max2) max2 = j[2];
                if (j[3] > max3) max3 = j[3];
                float lHip = j[0], lKnee = j[1], rHip = j[2], rKnee = j[3];
                bool leftStance = lKnee < 0.7f && phase < 0.5f;
                bool rightStance = rKnee < 0.7f && phase >= 0.5f;
                float supportHip = 0f;
                int supports = 0;
                if (leftStance) { supportHip += lHip; supports++; }
                if (rightStance) { supportHip += rHip; supports++; }
                if (supports > 0) supportHip /= supports;
                float com = pitch - supportHip * 0.85f;
                float acc = 7.5f * (float)Math.Sin(com) - 1.6f * pv;
                if (supports == 0) acc += 3.5f * (pitch >= 0f ? 1f : -1f);
                acc += rng.Range(-0.025f, 0.025f);
                pv += acc * dt;
                pitch += pv * dt;
                float push = 0f;
                if (leftStance) push += Math.Max(0f, -lHip) * (1f - Clamp01(lKnee)) * 1.4f;
                if (rightStance) push += Math.Max(0f, -rHip) * (1f - Clamp01(rKnee)) * 1.4f;
                float swing = lHip - rHip;
                if (leftStance && !rightStance) push += Math.Max(0f, -swing) * 0.35f;
                if (rightStance && !leftStance) push += Math.Max(0f, swing) * 0.35f;
                if (forceScale < 0.2f) forceScale = 0.2f;
                if (forceScale > 1.25f) forceScale = 1.25f;
                vx = vx * 0.92f + push * dt * 3.2f * forceScale;
                x += vx * dt;
                if (Math.Abs(pitch) > 0.95f)
                {
                    fallen = true;
                    x -= 0.4f;
                    break;
                }
            }
            if (memory != null)
            {
                for (int k = 0; k < 4; k++)
                {
                    float mn = k == 0 ? min0 : k == 1 ? min1 : k == 2 ? min2 : min3;
                float mx = k == 0 ? max0 : k == 1 ? max1 : k == 2 ? max2 : max3;
                memory.NoteRange("rom:" + JointNames.All[k], mn, mx, 0, "rollout");
                }
            }
            return x + (fallen ? 0f : 0.15f);
        }

        public void BlendToward(PoseTrace trace, float weight)
        {
            if (trace == null || trace.Frames.Count == 0 || weight <= 0f) return;
            if (weight > 1f) weight = 1f;
            for (int f = 0; f < Frames; f++)
            {
                float time = trace.Frames[trace.Frames.Count - 1].Time * (f + 0.5f) / Frames;
                PoseFrame nearest = trace.Frames[0];
                float best = 999f;
                for (int i = 0; i < trace.Frames.Count; i++)
                {
                    float d = Math.Abs(trace.Frames[i].Time - time);
                    if (d < best)
                    {
                        best = d;
                        nearest = trace.Frames[i];
                    }
                }
                for (int k = 0; k < LegJoints; k++)
                {
                    if (nearest.Angles == null || k >= nearest.Angles.Length) continue;
                    float scale = (k % 2 == 1) ? 1.3f : 1.1f;
                    float observed = nearest.Angles[k] / scale;
                    if (observed > 0.95f) observed = 0.95f;
                    if (observed < -0.95f) observed = -0.95f;
                    float gene = 0.5f * (float)Math.Log((1f + observed) / (1f - observed));
                    int idx = f * LegJoints + k;
                    Genome[idx] = Genome[idx] * (1f - weight) + gene * weight;
                }
            }
        }

        public void NudgeToward(float[] angles, float weight)
        {
            if (angles == null || weight <= 0f) return;
            if (weight > 0.5f) weight = 0.5f;
            for (int f = 0; f < Frames; f++)
            {
                for (int k = 0; k < LegJoints; k++)
                {
                    if (k >= angles.Length) continue;
                    float scale = (k % 2 == 1) ? 1.3f : 1.1f;
                    float observed = angles[k] / scale;
                    if (observed > 0.95f) observed = 0.95f;
                    if (observed < -0.95f) observed = -0.95f;
                    float gene = 0.5f * (float)Math.Log((1f + observed) / (1f - observed));
                    int idx = f * LegJoints + k;
                    Genome[idx] = Genome[idx] * (1f - weight) + gene * weight;
                }
            }
        }

        public string Signature()
        {
            return Genome[0].ToString("0.000") + ":" + Genome[3].ToString("0.000") + ":" + Genome[Genes - 1].ToString("0.000");
        }

        public List<object> GenomeData()
        {
            var list = new List<object>();
            for (int i = 0; i < Genome.Length; i++) list.Add((double)Genome[i]);
            return list;
        }

        public void LoadGenome(List<object> data, float bestDistance)
        {
            if (data == null || data.Count != Genes) return;
            for (int i = 0; i < Genes; i++) Genome[i] = MiniJson.AsFloat(data[i]);
            BestDistance = bestDistance;
        }

        static float[] RandomGenome(SeededRandom rng)
        {
            var g = new float[Genes];
            for (int i = 0; i < Genes; i++) g[i] = rng.Range(-1f, 1f);
            return g;
        }

        static void MutateInto(float[] genome, float[] dst, SeededRandom rng, float sigma)
        {
            for (int i = 0; i < genome.Length; i++)
            {
                dst[i] = genome[i] + rng.Range(-sigma, sigma);
            }
        }

        public static float SpeedOf(float[] genome)
        {
            float z = genome[Genes - 1];
            float sig = 1f / (1f + (float)Math.Exp(-z));
            return 0.6f + sig * 2.2f;
        }

        public static float[] SampleLegs(float[] genome, float phase)
        {
            var joints = new float[4];
            SampleLegsInto(genome, phase, joints);
            return joints;
        }

        public static void SampleLegsInto(float[] genome, float phase, float[] joints)
        {
            float pos = phase * Frames;
            int i0 = (int)pos % Frames;
            if (i0 < 0) i0 = 0;
            int i1 = (i0 + 1) % Frames;
            float t = pos - (float)Math.Floor(pos);
            for (int k = 0; k < 4; k++)
            {
                float a = genome[i0 * 4 + k];
                float b = genome[i1 * 4 + k];
                float mixed = a + (b - a) * t;
                float scale = (k % 2 == 1) ? 1.3f : 1.1f;
                joints[k] = (float)Math.Tanh(mixed) * scale;
            }
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }

    /// <summary>
    /// Scripted mentor motion. Babies never receive the style integer.
    /// They only receive <see cref="PoseTrace"/> samples of the resulting body.
    /// </summary>
    public static class MentorMotion
    {
        public static PoseTrace Record(string sourceId, string clip, float seconds, float dt, int style)
        {
            var trace = new PoseTrace { SourceId = sourceId };
            int guard = 0;
            for (float t = 0f; t <= seconds && guard < 4000; t += dt, guard++)
            {
                trace.Frames.Add(new PoseFrame
                {
                    SourceId = sourceId,
                    Time = t,
                    Angles = PoseAngles(clip, t, style)
                });
            }
            return trace;
        }

        public static float[] PoseAngles(string clip, float time, int style)
        {
            var angles = new float[10];
            WriteAngles(clip, time, style, angles);
            return angles;
        }

        public static void WriteAngles(string clip, float time, int style, float[] dest)
        {
            float stride = 0.85f + (style % 5) * 0.06f;
            float freq = 1.15f + (style % 3) * 0.08f;
            float phase = time * freq * 6.2831853f;
            float hipL = 0.55f * stride * (float)Math.Sin(phase);
            float hipR = 0.55f * stride * (float)Math.Sin(phase + 3.1415926f);
            float kneeL = 0.15f + 0.7f * Math.Max(0f, (float)Math.Sin(phase + 0.4f));
            float kneeR = 0.15f + 0.7f * Math.Max(0f, (float)Math.Sin(phase + 3.1415926f + 0.4f));
            float armL = -0.7f * hipL;
            float armR = -0.7f * hipR;
            if (clip == "idle")
            {
                hipL = 0.05f * (float)Math.Sin(time * 1.4f);
                hipR = -hipL;
                kneeL = 0.08f;
                kneeR = 0.08f;
                armL = 0.08f * (float)Math.Sin(time * 1.2f);
                armR = -armL;
            }
            else if (clip == "gesture")
            {
                armR = 1.1f * (float)Math.Sin(Math.Min(1.2f, time) * 2.2f);
                armL = 0.1f;
                hipL *= 0.2f;
                hipR *= 0.2f;
            }
            else if (clip == "sit")
            {
                hipL = -1.0f;
                hipR = -1.0f;
                kneeL = 1.15f;
                kneeR = 1.15f;
                armL = 0.2f;
                armR = 0.2f;
            }
            if (dest == null || dest.Length < 10) return;
            dest[0] = hipL;
            dest[1] = kneeL;
            dest[2] = hipR;
            dest[3] = kneeR;
            dest[4] = armL;
            dest[5] = 0.35f + 0.2f * Math.Abs(armL);
            dest[6] = armR;
            dest[7] = 0.35f + 0.2f * Math.Abs(armR);
            dest[8] = 0.04f * (float)Math.Sin(time * 1.7f);
            dest[9] = 0.05f * (float)Math.Sin(phase * 2f);
        }
    }

    /// <summary>
    /// Body schema. Motor babble calibrates how an observed angle sits on this body.
    /// </summary>
    public sealed class PoseMapper
    {
        public float[] Scale;
        public float[] Offset;

        public PoseMapper(int birthSeed)
        {
            var rng = new SeededRandom(birthSeed ^ 0x0B0D);
            Scale = new float[JointNames.Count];
            Offset = new float[JointNames.Count];
            for (int i = 0; i < JointNames.Count; i++)
            {
                Scale[i] = 0.9f + rng.NextFloat() * 0.2f;
                Offset[i] = rng.Range(-0.05f, 0.05f);
            }
        }

        public float[] MapToSelf(float[] observed)
        {
            var mapped = new float[JointNames.Count];
            int n = observed == null ? 0 : observed.Length;
            for (int i = 0; i < JointNames.Count; i++)
            {
                float raw = i < n ? observed[i] : 0f;
                mapped[i] = raw * Scale[i] + Offset[i];
            }
            return mapped;
        }

        public PoseTrace MapTrace(PoseTrace observed)
        {
            var mapped = new PoseTrace { SourceId = observed.SourceId };
            for (int i = 0; i < observed.Frames.Count; i++)
            {
                mapped.Frames.Add(new PoseFrame
                {
                    SourceId = observed.SourceId,
                    Time = observed.Frames[i].Time,
                    Angles = MapToSelf(observed.Frames[i].Angles)
                });
            }
            return mapped;
        }
    }
}
