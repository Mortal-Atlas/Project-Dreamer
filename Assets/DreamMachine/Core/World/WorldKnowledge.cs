using System;
using System.Collections.Generic;
using DreamMachine.Util;

namespace DreamMachine.World
{
    /// <summary>
    /// A particular thing, not a category. Two red spheres can differ.
    /// Aristotle's practical wisdom is about this object, here, now.
    /// </summary>
    public sealed class ObjectInstance
    {
        public string Id;
        public string Color;
        public string Shape;
        public string Mark;
        public float WorldX;
        public float WorldY;
        public float WorldZ;

        public string CategoryLabel
        {
            get { return Color + " " + Shape + " (" + Mark + ")"; }
        }
    }

    /// <summary>
    /// Hidden rules of the yard. They are not shown to the agent.
    /// At <see cref="ShiftAt"/> the rule changes, so a frozen prior goes stale
    /// and a mind that amends its beliefs has something to amend.
    /// </summary>
    public sealed class HiddenRuleWorld
    {
        public int ShiftAt = 40;

        public int Phase(int trial)
        {
            return trial < ShiftAt ? 1 : 2;
        }

        public bool IsHarmful(ObjectInstance obj, int trial)
        {
            bool harm;
            if (trial < ShiftAt)
            {
                harm = (obj.Color == "Red" && obj.Shape != "Cube") || obj.Mark == "Spot";
            }
            else
            {
                harm = obj.Shape == "Capsule" || obj.Color == "Green";
            }
            if (IsInstanceException(obj.Id))
            {
                harm = !harm;
            }
            return harm;
        }

        public static bool IsInstanceException(string id)
        {
            int n;
            if (!TryNumericId(id, out n)) return false;
            return n % 11 == 0;
        }

        public static bool TryNumericId(string id, out int n)
        {
            n = 0;
            if (string.IsNullOrEmpty(id)) return false;
            int dash = id.LastIndexOf('-');
            string tail = dash >= 0 ? id.Substring(dash + 1) : id;
            return int.TryParse(tail, out n);
        }

        public static List<ObjectInstance> StandardPool()
        {
            string[] colors = { "Red", "Blue", "Green", "Yellow" };
            string[] shapes = { "Sphere", "Cube", "Capsule", "Cylinder" };
            string[] marks = { "None", "Spot", "Stripe" };
            var list = new List<ObjectInstance>();
            for (int i = 0; i < 24; i++)
            {
                list.Add(new ObjectInstance
                {
                    Id = "obj-" + (i + 1),
                    Color = colors[i % 4],
                    Shape = shapes[(i / 4) % 4],
                    Mark = marks[(i / 2) % 3],
                    WorldX = (i % 6) * 1.7f - 4.25f,
                    WorldY = 0.45f,
                    WorldZ = (i / 6) * 1.7f - 1.5f
                });
            }
            return list;
        }
    }

    /// <summary>
    /// The personal possibility atlas: which descriptions this birth seed
    /// makes thinkable, and how strongly. A decision coordinate is the
    /// place inside that atlas where the mind actually landed.
    /// </summary>
    public sealed class AtlasOption
    {
        public string Predicate;
        public float Weight;
        public bool Thinkable;
    }

    public struct DecisionCoordinate
    {
        public int Index;
        public float U;
        public string Predicate;

        public override string ToString()
        {
            return Index.ToString() + ":" + Predicate + "@" + U.ToString("0.00");
        }
    }

    public sealed class PossibilityAtlas
    {
        public readonly int BirthSeed;
        public readonly List<AtlasOption> Options;

        PossibilityAtlas(int birthSeed, List<AtlasOption> options)
        {
            BirthSeed = birthSeed;
            Options = options;
        }

        public static PossibilityAtlas FromBirth(int birthSeed)
        {
            var rng = new SeededRandom(birthSeed ^ 0x51A7);
            string[] colors = { "Red", "Blue", "Green", "Yellow" };
            string[] shapes = { "Sphere", "Cube", "Capsule", "Cylinder" };
            string[] marks = { "None", "Spot", "Stripe" };
            var options = new List<AtlasOption>();
            for (int i = 0; i < colors.Length; i++) AddSingle(options, rng, "color:" + colors[i], true);
            for (int i = 0; i < shapes.Length; i++) AddSingle(options, rng, "shape:" + shapes[i], true);
            for (int i = 0; i < marks.Length; i++) AddSingle(options, rng, "mark:" + marks[i], true);
            for (int c = 0; c < colors.Length; c++)
            {
                for (int s = 0; s < shapes.Length; s++)
                {
                    string key = "color-shape:" + colors[c] + "+" + shapes[s];
                    bool thinkable = rng.NextInt(3) != 0;
                    AddSingle(options, rng, key, thinkable);
                }
            }
            for (int c = 0; c < colors.Length; c++)
            {
                for (int m = 0; m < marks.Length; m++)
                {
                    string key = "color-mark:" + colors[c] + "+" + marks[m];
                    bool thinkable = rng.NextInt(3) != 0;
                    AddSingle(options, rng, key, thinkable);
                }
            }
            for (int s = 0; s < shapes.Length; s++)
            {
                for (int m = 0; m < marks.Length; m++)
                {
                    string key = "shape-mark:" + shapes[s] + "+" + marks[m];
                    bool thinkable = rng.NextInt(3) != 0;
                    AddSingle(options, rng, key, thinkable);
                }
            }
            // Particular objects are always thinkable. That is the practical-wisdom slot.
            for (int i = 1; i <= 24; i++)
            {
                AddSingle(options, rng, "object:obj-" + i, true);
            }
            AddSingle(options, rng, "prior:general", true);
            return new PossibilityAtlas(birthSeed, options);
        }

        static void AddSingle(List<AtlasOption> options, SeededRandom rng, string predicate, bool thinkable)
        {
            options.Add(new AtlasOption
            {
                Predicate = predicate,
                Weight = 0.25f + rng.NextFloat() * 0.75f,
                Thinkable = thinkable
            });
        }

        public bool Allows(string predicate)
        {
            for (int i = 0; i < Options.Count; i++)
            {
                if (Options[i].Predicate == predicate) return Options[i].Thinkable;
            }
            return false;
        }

        public DecisionCoordinate Coordinate(string predicate, float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            for (int i = 0; i < Options.Count; i++)
            {
                if (Options[i].Predicate == predicate && Options[i].Thinkable)
                {
                    return new DecisionCoordinate { Index = i, U = u, Predicate = predicate };
                }
            }
            return new DecisionCoordinate { Index = -1, U = u, Predicate = predicate ?? "" };
        }
    }

    public static class Predicates
    {
        public static string[] For(ObjectInstance obj)
        {
            return new[]
            {
                "color:" + obj.Color,
                "shape:" + obj.Shape,
                "mark:" + obj.Mark,
                "color-shape:" + obj.Color + "+" + obj.Shape,
                "color-mark:" + obj.Color + "+" + obj.Mark,
                "shape-mark:" + obj.Shape + "+" + obj.Mark,
                "object:" + obj.Id
            };
        }
    }
}
