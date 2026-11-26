using System;

namespace DreamMachine.Util
{
    /// <summary>
    /// One integer birth seed drives every random choice in a mind.
    /// Same seed, same sequence, on every machine.
    /// </summary>
    public sealed class SeededRandom
    {
        uint _state;

        public SeededRandom(int seed)
        {
            _state = (uint)seed;
            if (_state == 0)
            {
                _state = 1u;
            }
            // Mix so nearby seeds diverge immediately.
            NextUInt();
            NextUInt();
        }

        public int SeedRoot { get; private set; }

        public static SeededRandom FromBirth(int birthSeed)
        {
            var rng = new SeededRandom(birthSeed);
            rng.SeedRoot = birthSeed;
            return rng;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x == 0u ? 1u : x;
            return _state;
        }

        /// <summary>Float in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public float Signed(float magnitude)
        {
            return Range(-magnitude, magnitude);
        }
    }
}
