using System;
using System.Collections.Generic;
using System.IO;

namespace DreamMachine.Body
{
    public readonly struct GripPractice
    {
        public readonly int Attempt, SlipTicks, HoldTicks;
        public readonly float Before, After, PeakClearance;
        public readonly bool Success;

        internal GripPractice(int attempt, float before, float after, int slip, int hold, float clearance, bool success)
        {
            Attempt = attempt; Before = before; After = after; SlipTicks = slip; HoldTicks = hold;
            PeakClearance = clearance; Success = success;
        }
    }

    // An engineered update rule learns a scalar effort residual solely from physical slip evidence.
    public sealed class GripAcquisition
    {
        public const float InitialCap = .18f, ReferenceCap = 3.2f;
        const float SlipIncrement = .28f;
        const int RecordLimit = 256;
        const string Format = "DreamMachine.H1.Acquisition";
        public const string SkillId = "h1.supported-opposed-grip";
        public const string CalibrationId = "supported-guides.v1.age3";
        readonly List<GripPractice> _history = new List<GripPractice>();
        public string Individual { get; }
        public int BirthSeed { get; }
        public float Cap { get; private set; } = InitialCap;
        public IReadOnlyList<GripPractice> History { get; }

        public GripAcquisition(string individual, int birthSeed)
        {
            if (string.IsNullOrWhiteSpace(individual)) throw new ArgumentException("A stable individual ID is required.", nameof(individual));
            Individual = individual;
            BirthSeed = birthSeed;
            History = _history.AsReadOnly();
        }

        public GripPractice Observe(int slipTicks, int heldTicks, float peakClearance, bool success)
        {
            if (slipTicks < 0 || heldTicks < 0 || !float.IsFinite(peakClearance) || _history.Count >= RecordLimit)
                throw new ArgumentOutOfRangeException(nameof(slipTicks));
            float previous = Cap;
            if (!success && slipTicks >= 2) Cap = Math.Min(ReferenceCap, Cap + SlipIncrement);
            var attempt = new GripPractice(_history.Count + 1, previous, Cap, slipTicks, heldTicks, peakClearance, success);
            _history.Add(attempt);
            return attempt;
        }

        public byte[] Capture()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Format); writer.Write(1); writer.Write(SkillId); writer.Write(CalibrationId); writer.Write(Individual);
            writer.Write(BirthSeed); writer.Write(_history.Count); writer.Write(Cap);
            foreach (var item in _history)
            {
                writer.Write(item.SlipTicks); writer.Write(item.HoldTicks); writer.Write(item.PeakClearance); writer.Write(item.Success);
            }
            return stream.ToArray();
        }

        public static GripAcquisition Restore(byte[] checkpoint, string individual, int birthSeed)
        {
            if (checkpoint == null || checkpoint.Length > 16384) throw new InvalidDataException("Invalid grip checkpoint size.");
            using var stream = new MemoryStream(checkpoint, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadString() != Format || reader.ReadInt32() != 1 || reader.ReadString() != SkillId ||
                reader.ReadString() != CalibrationId || reader.ReadString() != individual || reader.ReadInt32() != birthSeed)
                throw new InvalidDataException("Incompatible grip identity or calibration.");
            int count = reader.ReadInt32();
            float cap = reader.ReadSingle();
            if ((uint)count > RecordLimit) throw new InvalidDataException("Invalid grip history length.");
            var state = new GripAcquisition(individual, birthSeed);
            for (int i = 0; i < count; i++)
                state.Observe(reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadBoolean());
            if (cap != state.Cap || stream.Position != stream.Length)
                throw new InvalidDataException("Grip parameter disagrees with the acquisition evidence.");
            return state;
        }
    }
}
