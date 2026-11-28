using System;

namespace DreamMachine.Sim
{
    public enum SocketPolicy { Exploration, Direct, Inspect, Contextual, Unified, Confirmed }

    // Quantized visible proportions. Edge is missing in shade, with no socket identity on the sensor side.
    public readonly struct SocketFeatures
    {
        public readonly int Aspect, Edge;
        public SocketFeatures(int aspect, int edge) { Aspect = aspect; Edge = edge; }
        public static SocketFeatures Shape(int shape) => shape == 0 ? new SocketFeatures(1, 2) :
            shape == 1 ? new SocketFeatures(1, 6) : new SocketFeatures(3, 4);
        public int Distance(SocketFeatures other) => 4 * Math.Abs(Aspect - other.Aspect) +
            (other.Edge == 0 ? 3 : Math.Abs(Edge - other.Edge));
    }

    public readonly struct SocketDecision
    {
        public readonly int Data;
        public SocketDecision(int data) { Data = data; }
        public int Choice => Data & 3;
        public int Marker => Data >> 2 & 7;
        public int Observations => Data >> 5 & 3;
        public int UncertaintyBefore => Data >> 7 & 3;
        public int UncertaintyAfter => Data >> 9 & 3;
        public bool Contradiction => (Data & (1 << 11)) != 0;
        public int ObservedSlots => Data >> 12 & 7;
        public SocketFeatures Observed(int slot)
        {
            int features = Data >> (15 + slot * 5);
            return new SocketFeatures(features & 3, features >> 2 & 7);
        }
    }

    public static class SocketInspection
    {
        public const int InspectionTicks = 5;

        public static SocketDecision Choose(SocketPolicy policy, SocketFeatures piece, int marker, int memorySlot, int memoryMarker,
            int rejectedSlot, int fallback, Func<int, SocketFeatures> inspect)
        {
            if (policy == SocketPolicy.Confirmed) throw new ArgumentException("Use the confirmation sensor entry point.", nameof(policy));
            int observations = 0, observedSlots = 0, features = 0;
            bool contradiction = memorySlot >= 0 && memoryMarker != 7 && marker != memoryMarker;
            bool remembered = memorySlot >= 0 && !contradiction && marker == memoryMarker && memorySlot != rejectedSlot;
            int before = remembered ? 1 : 3, after = before;
            int choice = policy != SocketPolicy.Exploration && memorySlot >= 0 ? memorySlot : fallback;

            if (policy == SocketPolicy.Inspect || policy == SocketPolicy.Unified || policy == SocketPolicy.Contextual && !remembered)
            {
                int best = int.MaxValue, runner = int.MaxValue;
                int first = memorySlot >= 0 ? memorySlot : fallback;
                for (int i = 0; i < 3; i++)
                {
                    int slot = (first + i) % 3;
                    SocketFeatures sample = inspect(slot);
                    observations++;
                    observedSlots |= 1 << slot;
                    features |= (sample.Aspect | sample.Edge << 2) << (slot * 5);
                    int distance = piece.Distance(sample);
                    if (slot == memorySlot && distance > 3) contradiction = true;
                    int score = 2 * distance + (slot == rejectedSlot ? 4 : 0);
                    if (policy == SocketPolicy.Unified && remembered && slot == memorySlot) score--;
                    if (score < best) { runner = best; best = score; choice = slot; }
                    else if (score < runner) runner = score;
                    if (distance <= 1 && slot != rejectedSlot) break;
                }
                after = best <= 2 || runner - best >= 4 && observations == 3 ? 1 : 2;
            }
            return new SocketDecision(choice | marker << 2 | observations << 5 | before << 7 | after << 9 |
                (contradiction ? 1 << 11 : 0) | observedSlots << 12 | features << 15);
        }

        public const int ConfirmationTicks = 1;

        // A glancing rim measurement resolves aspect and coarse edge bands without a detailed scan.
        // Zero edge is missing. Rounded and angular bands leave the middle band ambiguous.
        public static SocketDecision ChooseConfirmed(SocketFeatures piece, int marker, int memorySlot, int memoryMarker,
            int rejectedSlot, int fallback, Func<int, SocketFeatures> inspect, Func<int, SocketFeatures> glance,
            out bool confirmedCheck)
        {
            confirmedCheck = memorySlot >= 0 && marker == memoryMarker && memorySlot != rejectedSlot;
            bool contradiction = false;
            if (confirmedCheck)
            {
                SocketFeatures sample = glance(memorySlot);
                int desiredBand = EdgeBand(piece.Edge);
                int observedBand = EdgeBand(sample.Edge);
                bool distinctAspect = piece.Aspect >= 3 && sample.Aspect == piece.Aspect;
                bool distinctRim = desiredBand != 0 && observedBand == desiredBand;
                if (sample.Aspect == piece.Aspect && (distinctAspect || distinctRim))
                    return Choose(SocketPolicy.Contextual, piece, marker, memorySlot, memoryMarker, rejectedSlot, fallback, inspect);
                contradiction = sample.Aspect != piece.Aspect ||
                    desiredBand != 0 && observedBand != 0 && desiredBand != observedBand;
            }
            var decision = Choose(SocketPolicy.Inspect, piece, marker, memorySlot, memoryMarker, rejectedSlot, fallback, inspect);
            return contradiction ? new SocketDecision(decision.Data | (1 << 11)) : decision;
        }

        static int EdgeBand(int edge) => edge == 0 || edge == 3 || edge == 4 ? 0 : edge < 3 ? 1 : 2;

        public static SocketFeatures ObserveBoard(int rotation, int slot) => SocketFeatures.Shape((slot - rotation + 3) % 3);
    }
}
