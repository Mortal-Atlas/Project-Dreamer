using System;
using System.Collections.Generic;
using DreamMachine.Util;
using DreamMachine.World;

namespace DreamMachine.Libraries
{
    /// <summary>
    /// Library I. General knowledge: what is "right in general."
    /// A future LLM adapter implements this same interface.
    /// </summary>
    public interface ILibraryOne
    {
        string Name { get; }
        float PriorHarm(ObjectInstance obj);
    }

    /// <summary>
    /// Rule-of-thumb priors. Coarse on purpose: right-ish about red things,
    /// wrong about spots, blind to the mid-run rule change.
    /// </summary>
    public sealed class StatisticalLibraryOne : ILibraryOne
    {
        public string Name { get { return "statistical-prior"; } }

        public float PriorHarm(ObjectInstance obj)
        {
            float color = obj.Color == "Red" ? 0.72f
                : obj.Color == "Green" ? 0.40f
                : obj.Color == "Blue" ? 0.35f
                : 0.38f;
            float shape = obj.Shape == "Cube" ? 0.30f
                : obj.Shape == "Capsule" ? 0.55f
                : obj.Shape == "Sphere" ? 0.48f
                : 0.45f;
            float mark = obj.Mark == "Spot" ? 0.36f
                : obj.Mark == "Stripe" ? 0.50f
                : 0.42f;
            return (color + shape + mark) / 3f;
        }

        public static float FeaturePrior(string predicate)
        {
            if (predicate == null) return 0.45f;
            if (predicate.StartsWith("color:Red")) return 0.72f;
            if (predicate.StartsWith("color:Blue")) return 0.35f;
            if (predicate.StartsWith("color:Green")) return 0.40f;
            if (predicate.StartsWith("color:Yellow")) return 0.38f;
            if (predicate.StartsWith("shape:Cube")) return 0.30f;
            if (predicate.StartsWith("shape:Capsule")) return 0.55f;
            if (predicate.StartsWith("shape:Sphere")) return 0.48f;
            if (predicate.StartsWith("shape:Cylinder")) return 0.45f;
            if (predicate.StartsWith("mark:Spot")) return 0.36f;
            if (predicate.StartsWith("mark:Stripe")) return 0.50f;
            if (predicate.StartsWith("mark:None")) return 0.42f;
            int plus = predicate.IndexOf('+');
            if (plus > 0 && predicate.IndexOf(':') > 0)
            {
                // Conjunctions average the two feature priors they name.
                string left = predicate.Substring(predicate.IndexOf(':') + 1, plus - (predicate.IndexOf(':') + 1));
                string right = predicate.Substring(plus + 1);
                float a = FeaturePrior(GuessKind(left) + left);
                float b = FeaturePrior(GuessKind(right) + right);
                return (a + b) * 0.5f;
            }
            return 0.45f;
        }

        static string GuessKind(string token)
        {
            if (token == "Red" || token == "Blue" || token == "Green" || token == "Yellow") return "color:";
            if (token == "Spot" || token == "Stripe" || token == "None") return "mark:";
            return "shape:";
        }
    }

    /// <summary>
    /// Hook for a real language-model prior. Nothing is called unless a transport
    /// is assigned. The prototype ships the statistical library instead.
    /// </summary>
    public sealed class LlmLibraryOneHook : ILibraryOne
    {
        public string Name { get { return "llm-hook"; } }
        public string Endpoint = "https://example.invalid/v1/prior";

        /// <summary>
        /// Assign this to send a prompt and receive a reply. The reply should
        /// contain a single probability between 0 and 1.
        /// </summary>
        public Func<string, string> Transport;

        public float PriorHarm(ObjectInstance obj)
        {
            if (Transport == null)
            {
                throw new InvalidOperationException(
                    "Library I LLM hook has no transport. Assign LlmLibraryOneHook.Transport, " +
                    "or use StatisticalLibraryOne. Endpoint reserved: " + Endpoint);
            }
            string prompt = "Reply with only a probability from 0 to 1 that this particular object is harmful: "
                + obj.Id + " " + obj.CategoryLabel;
            string reply = Transport(prompt);
            float parsed;
            if (!float.TryParse(reply, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                throw new FormatException("LLM reply was not a probability: " + reply);
            }
            if (parsed < 0f) parsed = 0f;
            if (parsed > 1f) parsed = 1f;
            return parsed;
        }
    }

    /// <summary>
    /// One candidate idea from Library II, Borges's formulation space.
    /// It is a sentence the mind could think, not yet a belief it trusts.
    /// </summary>
    public sealed class Hypothesis
    {
        public string Predicate;
        public string Statement;
        public float GenerativeWeight;
        public bool Thinkable;
    }

    /// <summary>
    /// Library II. Seeded combinatorial generation of hypotheses
    /// inside this mind's possibility atlas. It does not look at outcomes.
    /// </summary>
    public sealed class LibraryTwo
    {
        public List<Hypothesis> Generate(ObjectInstance obj, PossibilityAtlas atlas, int birthSeed)
        {
            var made = new List<Hypothesis>();
            string[] keys = Predicates.For(obj);
            var order = new SeededRandom(birthSeed ^ StableHash(obj.Id));
            // Stable but seed-dependent order.
            var ranked = new List<string>(keys);
            for (int i = ranked.Count - 1; i > 0; i--)
            {
                int j = order.NextInt(i + 1);
                string tmp = ranked[i];
                ranked[i] = ranked[j];
                ranked[j] = tmp;
            }
            for (int i = 0; i < ranked.Count; i++)
            {
                string key = ranked[i];
                bool thinkable = atlas == null || atlas.Allows(key);
                made.Add(new Hypothesis
                {
                    Predicate = key,
                    Statement = "Perhaps when " + key + ", harm follows.",
                    GenerativeWeight = thinkable ? WeightOf(atlas, key) : 0f,
                    Thinkable = thinkable
                });
            }
            made.Add(new Hypothesis
            {
                Predicate = "prior:general",
                Statement = "Perhaps the general prior is enough.",
                GenerativeWeight = 0.5f,
                Thinkable = true
            });
            return made;
        }

        public static int StableHash(string text)
        {
            unchecked
            {
                int h = 23;
                if (text == null) return h;
                for (int i = 0; i < text.Length; i++)
                {
                    h = h * 31 + text[i];
                }
                return h;
            }
        }

        static float WeightOf(PossibilityAtlas atlas, string key)
        {
            if (atlas == null) return 0.5f;
            for (int i = 0; i < atlas.Options.Count; i++)
            {
                if (atlas.Options[i].Predicate == key) return atlas.Options[i].Weight;
            }
            return 0.2f;
        }
    }

    /// <summary>
    /// An amendment. The old belief stays in the history. Nothing is erased.
    /// </summary>
    public sealed class AppendixEntry
    {
        public int Timestamp;
        public string Evidence;
        public string OldStatement;
        public string NewStatement;
        public float OldConfidence;
        public float NewConfidence;
        public string Reason;
    }

    /// <summary>
    /// Library III. Lived beliefs. Reinforced, pruned, never deleted.
    /// </summary>
    public sealed class BeliefRecord
    {
        public string Id;
        public string Kind;
        public string Statement;
        public string Source;
        public float Confidence;
        public bool Retired;
        public float HarmCount;
        public float SeenCount;
        public List<AppendixEntry> Appendices = new List<AppendixEntry>();

        public string FullStatement
        {
            get { return "When " + Id + ", the particular case is " + Statement + "."; }
        }
    }

    public sealed class LibraryThree
    {
        public const float Decay = 0.85f;
        readonly Dictionary<string, BeliefRecord> _beliefs = new Dictionary<string, BeliefRecord>();

        public IReadOnlyDictionary<string, BeliefRecord> Beliefs { get { return _beliefs; } }
        public int Count { get { return _beliefs.Count; } }

        public int AppendixCount
        {
            get
            {
                int n = 0;
                foreach (var pair in _beliefs) n += pair.Value.Appendices.Count;
                return n;
            }
        }

        public BeliefRecord GetOrNull(string id)
        {
            BeliefRecord record;
            return _beliefs.TryGetValue(id, out record) ? record : null;
        }

        public List<BeliefRecord> All()
        {
            var list = new List<BeliefRecord>();
            foreach (var pair in _beliefs) list.Add(pair.Value);
            list.Sort(delegate (BeliefRecord a, BeliefRecord b) { return string.CompareOrdinal(a.Id, b.Id); });
            return list;
        }

        /// <summary>
        /// Fold one lived outcome into a belief. If the statement or the
        /// confidence moves, an appendix is appended. The record stays.
        /// </summary>
        public BeliefRecord Observe(string id, bool harmful, int timestamp, string evidence, string source)
        {
            BeliefRecord record;
            if (!_beliefs.TryGetValue(id, out record))
            {
                record = new BeliefRecord
                {
                    Id = id,
                    Kind = "Guess",
                    Statement = "",
                    Source = source,
                    Confidence = 0f
                };
                _beliefs[id] = record;
            }
            record.HarmCount = record.HarmCount * Decay + (harmful ? 1f : 0f);
            record.SeenCount = record.SeenCount * Decay + 1f;
            float p = (record.HarmCount + 1f) / (record.SeenCount + 2f);
            string statement = p >= 0.5f ? "harmful" : "safe";
            float evidenceWeight = record.SeenCount / (record.SeenCount + 4f);
            float confidence = Math.Abs(p - 0.5f) * 2f * evidenceWeight;
            if (confidence < 0f) confidence = 0f;
            if (confidence > 1f) confidence = 1f;
            bool material = record.Statement != statement || Math.Abs(record.Confidence - confidence) >= 0.05f;
            if (record.Appendices.Count == 0 || material)
            {
                record.Appendices.Add(new AppendixEntry
                {
                    Timestamp = timestamp,
                    Evidence = evidence,
                    OldStatement = record.Statement,
                    NewStatement = statement,
                    OldConfidence = record.Confidence,
                    NewConfidence = confidence,
                    Reason = record.Appendices.Count == 0 ? "first-commitment"
                        : (record.Statement != statement ? "amend" : "reinforce")
                });
                record.Statement = statement;
                record.Confidence = confidence;
            }
            record.Kind = record.SeenCount >= 4f ? "Belief" : "Guess";
            record.Source = source;
            if (record.Confidence < 0.08f && record.SeenCount > 6f)
            {
                record.Retired = true;
                record.Kind = "Assumption";
            }
            else if (record.Confidence >= 0.2f)
            {
                record.Retired = false;
            }
            return record;
        }

        /// <summary>A dream or a hunch, kept even if experience has not tested it yet.</summary>
        public BeliefRecord NoteIdea(string id, string statement, float confidence, int timestamp, string source)
        {
            BeliefRecord existing = GetOrNull(id);
            if (existing != null)
            {
                return existing;
            }
            var record = new BeliefRecord
            {
                Id = id,
                Kind = "Guess",
                Statement = statement,
                Source = source,
                Confidence = confidence
            };
            record.Appendices.Add(new AppendixEntry
            {
                Timestamp = timestamp,
                Evidence = "Formed in the dream library, not yet lived.",
                OldStatement = "",
                NewStatement = statement,
                OldConfidence = 0f,
                NewConfidence = confidence,
                Reason = "dream-hypothesis"
            });
            _beliefs[id] = record;
            return record;
        }

        internal void RecordExperience(string id, string statement, string evidence, int timestamp,
            string source = "firsthand-supported-grip")
        {
            var record = new BeliefRecord
            {
                Id = id, Kind = "Experience", Statement = statement, Source = source
            };
            record.Appendices.Add(new AppendixEntry
            {
                Timestamp = timestamp, Evidence = evidence, NewStatement = statement, Reason = source
            });
            _beliefs.Add(id, record);
        }

        /// <summary>Append a completed physical research trial to this individual's lived evidence.</summary>
        public void RecordSupportedGraspObservation(int attempt, int completedTick, string outcome, string traceReference)
        {
            if (attempt <= 0 || completedTick <= 0 || string.IsNullOrWhiteSpace(outcome) ||
                string.IsNullOrWhiteSpace(traceReference))
                throw new ArgumentException("A completed trial and its recorded trajectory are required.");
            RecordExperience("motor:h4:grip:" + attempt, "Supported grasp and placement attempt " + attempt,
                $"Native outcome {outcome}. Trace {traceReference}.", completedTick);
        }

        public float Rate(string id)
        {
            BeliefRecord record = GetOrNull(id);
            if (record == null || record.SeenCount <= 0f) return 0.5f;
            return (record.HarmCount + 1f) / (record.SeenCount + 2f);
        }

        public float Seen(string id)
        {
            BeliefRecord record = GetOrNull(id);
            return record == null ? 0f : record.SeenCount;
        }

        public string ToJson()
        {
            return MiniJson.Stringify(ToData());
        }

        public Dictionary<string, object> ToData()
        {
            var rows = new List<object>();
            List<BeliefRecord> all = All();
            for (int i = 0; i < all.Count; i++)
            {
                BeliefRecord b = all[i];
                var appendices = new List<object>();
                for (int a = 0; a < b.Appendices.Count; a++)
                {
                    AppendixEntry e = b.Appendices[a];
                    appendices.Add(new Dictionary<string, object>
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
                    { "id", b.Id ?? "" },
                    { "kind", b.Kind ?? "" },
                    { "statement", b.Statement ?? "" },
                    { "source", b.Source ?? "" },
                    { "confidence", b.Confidence },
                    { "retired", b.Retired },
                    { "harmCount", b.HarmCount },
                    { "seenCount", b.SeenCount },
                    { "appendices", appendices }
                });
            }
            return new Dictionary<string, object> { { "beliefs", rows } };
        }

        public static LibraryThree FromJson(string json)
        {
            return FromData(MiniJson.AsObject(MiniJson.Parse(json)));
        }

        public static LibraryThree FromData(Dictionary<string, object> data)
        {
            var lib = new LibraryThree();
            if (data == null || !data.ContainsKey("beliefs")) return lib;
            List<object> rows = MiniJson.AsArray(data["beliefs"]);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = MiniJson.AsObject(rows[i]);
                var record = new BeliefRecord
                {
                    Id = MiniJson.AsString(row["id"]),
                    Kind = MiniJson.AsString(row["kind"]),
                    Statement = MiniJson.AsString(row["statement"]),
                    Source = MiniJson.AsString(row["source"]),
                    Confidence = MiniJson.AsFloat(row["confidence"]),
                    Retired = row.ContainsKey("retired") && MiniJson.AsBool(row["retired"]),
                    HarmCount = row.ContainsKey("harmCount") ? MiniJson.AsFloat(row["harmCount"]) : 0f,
                    SeenCount = row.ContainsKey("seenCount") ? MiniJson.AsFloat(row["seenCount"]) : 0f
                };
                if (row.ContainsKey("appendices"))
                {
                    List<object> apps = MiniJson.AsArray(row["appendices"]);
                    for (int a = 0; a < apps.Count; a++)
                    {
                        var e = MiniJson.AsObject(apps[a]);
                        record.Appendices.Add(new AppendixEntry
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
                lib._beliefs[record.Id] = record;
            }
            return lib;
        }
    }
}
