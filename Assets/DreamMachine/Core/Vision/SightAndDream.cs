using System.Collections.Generic;
using DreamMachine.Libraries;
using DreamMachine.Util;
using DreamMachine.World;

namespace DreamMachine.Vision
{
    public struct MaskRegion
    {
        public int NumericId;
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;
        public int PixelCount;

        public int Width { get { return MaxX - MinX + 1; } }
        public int Height { get { return MaxY - MinY + 1; } }
    }

    /// <summary>
    /// Decodes a Unity Perception-style ID buffer.
    /// Red + green*256 is the object id. Black is background.
    /// A learned detector can replace this later and still emit regions.
    /// </summary>
    public interface IObjectDetector
    {
        List<MaskRegion> Detect(byte[] rgba, int width, int height);
    }

    public sealed class IdMaskDecoder : IObjectDetector
    {
        public List<MaskRegion> Detect(byte[] rgba, int width, int height)
        {
            return Decode(rgba, width, height);
        }

        public static List<MaskRegion> Decode(byte[] rgba, int width, int height)
        {
            var found = new Dictionary<int, MaskRegion>();
            if (rgba == null || width <= 0 || height <= 0) return new List<MaskRegion>();
            int pixels = width * height;
            int bytes = rgba.Length / 4;
            int n = pixels < bytes ? pixels : bytes;
            for (int p = 0; p < n; p++)
            {
                int o = p * 4;
                int id = rgba[o] + (rgba[o + 1] << 8);
                if (id == 0) continue;
                int x = p % width;
                int y = p / width;
                MaskRegion region;
                if (!found.TryGetValue(id, out region))
                {
                    region = new MaskRegion
                    {
                        NumericId = id,
                        MinX = x,
                        MinY = y,
                        MaxX = x,
                        MaxY = y,
                        PixelCount = 0
                    };
                }
                if (x < region.MinX) region.MinX = x;
                if (y < region.MinY) region.MinY = y;
                if (x > region.MaxX) region.MaxX = x;
                if (y > region.MaxY) region.MaxY = y;
                region.PixelCount++;
                found[id] = region;
            }
            var list = new List<MaskRegion>();
            foreach (var pair in found) list.Add(pair.Value);
            return list;
        }

        public static void WriteId(byte[] rgba, int width, int x, int y, int id)
        {
            int o = (y * width + x) * 4;
            rgba[o] = (byte)(id & 255);
            rgba[o + 1] = (byte)((id >> 8) & 255);
            rgba[o + 2] = 1;
            rgba[o + 3] = 255;
        }
    }

    public sealed class VisualObservation
    {
        public string ObjectId;
        public string Category;
        public string Color;
        public string Shape;
        public int Day;
        public int Trial;
        public float BoundX;
        public float BoundY;
        public float BoundW;
        public float BoundH;
        public float WorldX;
        public float WorldY;
        public float WorldZ;
        public string Appearance;
        public int PixelCount;
    }

    public sealed class DreamProp
    {
        public string SourceObjectId;
        public string Shape;
        public string Color;
        public float X;
        public float Z;
        public bool Recombined;
    }

    /// <summary>
    /// Night scene. Only props the baby actually saw are rebuilt.
    /// Library II may swap their places. It may not invent a new object.
    /// </summary>
    public sealed class DreamRecombinator
    {
        public List<DreamProp> Remix(IList<VisualObservation> memory, LibraryTwo library, SeededRandom rng)
        {
            var props = new List<DreamProp>();
            if (memory == null || memory.Count == 0) return props;
            var seen = new List<VisualObservation>();
            var used = new HashSet<string>();
            for (int i = 0; i < memory.Count; i++)
            {
                if (memory[i] == null || string.IsNullOrEmpty(memory[i].ObjectId)) continue;
                if (!used.Add(memory[i].ObjectId)) continue;
                seen.Add(memory[i]);
            }
            for (int i = 0; i < seen.Count; i++)
            {
                VisualObservation obs = seen[i];
                props.Add(new DreamProp
                {
                    SourceObjectId = obs.ObjectId,
                    Shape = obs.Shape,
                    Color = obs.Color,
                    X = obs.WorldX,
                    Z = obs.WorldZ,
                    Recombined = false
                });
            }
            if (props.Count >= 2 && rng.NextFloat() < 0.75f)
            {
                int a = rng.NextInt(props.Count);
                int b = rng.NextInt(props.Count);
                if (a != b)
                {
                    float x = props[a].X;
                    float z = props[a].Z;
                    props[a].X = props[b].X;
                    props[a].Z = props[b].Z;
                    props[b].X = x;
                    props[b].Z = z;
                    props[a].Recombined = true;
                    props[b].Recombined = true;
                }
            }
            // Library II is consulted so the dream is a formulation, not a copy.
            // The sentences are not turned into new objects.
            if (library != null && seen.Count > 0)
            {
                var standIn = new ObjectInstance
                {
                    Id = seen[0].ObjectId,
                    Color = seen[0].Color,
                    Shape = seen[0].Shape,
                    Mark = "None"
                };
                library.Generate(standIn, null, rng.NextInt(100000));
            }
            return props;
        }
    }
}
