using System;
using System.Collections.Generic;
using DreamMachine.Util;

namespace DreamMachine.Sim
{
    /// <summary>Three shaped sockets on a rotatable board. Accepting a piece advances the board state.</summary>
    public static class ShapeBoard
    {
        public static int Rotation(int places) => places / 3 % 3;
        public static int Cue(int places) => places % 3;
        public static int Socket(int cue, int rotation) => (cue + rotation) % 3;
        public static bool Fits(int cue, int rotation, int socket) => socket == Socket(cue, rotation);
        public static bool Place(ref int places, int socket)
        {
            if (!Fits(Cue(places), Rotation(places), socket)) return false;
            places++;
            return true;
        }
    }

    [Flags]
    public enum RetentionAccess { None = 0, Retrieval = 1, Replay = 2, Integrated = Retrieval | Replay }
    public enum RetentionDecision { Exploration, Episode, ReplayPlan }

    public readonly struct ShapeEpisode
    {
        public readonly int Day, Cue, Rotation, Choice;
        public readonly bool Success;
        public ShapeEpisode(int day, int cue, int rotation, int choice)
        { Day = day; Cue = cue; Rotation = rotation; Choice = choice; Success = ShapeBoard.Fits(cue, rotation, choice); }
    }

    public readonly struct RetentionTrial
    {
        public readonly int Seed, Cue, Rotation, Choice, Source;
        public readonly RetentionAccess Access;
        public readonly RetentionDecision Decision;
        public readonly bool Transfer, Success, RepeatedTrainingMistake;

        internal RetentionTrial(int seed, RetentionAccess access, int cue, int rotation, int choice, int source,
            RetentionDecision decision, bool transfer, bool repeatedTrainingMistake)
        {
            Seed = seed; Access = access; Cue = cue; Rotation = rotation; Choice = choice; Source = source;
            Decision = decision; Transfer = transfer; Success = ShapeBoard.Fits(cue, rotation, choice);
            RepeatedTrainingMistake = repeatedTrainingMistake;
        }
    }

    public static class HouseholdRetention
    {
        public static ShapeEpisode[] Acquisition(int seed)
        {
            var rng = new SeededRandom(seed);
            var episodes = new ShapeEpisode[36];
            int index = 0;
            for (int day = 0; day < 4; day++)
            for (int cue = 0; cue < 3; cue++)
            {
                int start = rng.NextInt(3);
                for (int attempt = 0; attempt < 3; attempt++)
                    episodes[index++] = new ShapeEpisode(day, cue, 0, (start + attempt) % 3);
            }
            return episodes;
        }

        public static IReadOnlyList<RetentionTrial> Probe(int seed, RetentionAccess access)
        {
            ShapeEpisode[] events = Acquisition(seed);
            int[] plans = { -1, -1, -1 };
            if ((access & RetentionAccess.Replay) != 0)
            {
                int start = (seed & int.MaxValue) % 3;
                for (int i = 0; i < 2; i++)
                {
                    int cue = (start + i) % 3;
                    plans[cue] = LastSuccess(events, cue);
                }
            }
            // Acquisition episodes remain in the archive. Replay-only daytime code sees only its two plans.
            var visible = (access & RetentionAccess.Retrieval) != 0 ? events : null;
            var planSlots = new int[3];
            for (int i = 0; i < 3; i++) planSlots[i] = plans[i] < 0 ? -1 : events[plans[i]].Choice;
            var trials = new List<RetentionTrial>(18);
            var rng = new SeededRandom(unchecked(seed * 71 + 5501));
            for (int arrangement = 0; arrangement < 3; arrangement++)
            for (int repeat = 0; repeat < 2; repeat++)
            for (int cue = 0; cue < 3; cue++)
            {
                int exploration = rng.NextInt(3);
                int source = plans[cue];
                int learned = planSlots[cue];
                RetentionDecision decision = source >= 0 ? RetentionDecision.ReplayPlan : RetentionDecision.Exploration;
                if (source < 0 && visible != null)
                {
                    source = LastSuccess(visible, cue);
                    if (source >= 0) { learned = visible[source].Choice; decision = RetentionDecision.Episode; }
                }
                int choice = source >= 0 ? ShapeBoard.Socket(learned, arrangement) : exploration;
                bool repeated = false;
                foreach (var record in events)
                    if (record.Cue == cue && !record.Success && (record.Choice + arrangement) % 3 == choice)
                    { repeated = true; break; }
                trials.Add(new RetentionTrial(seed, access, cue, arrangement, choice, source, decision, arrangement != 0, repeated));
            }
            return trials;
        }

        static int LastSuccess(ShapeEpisode[] events, int cue)
        {
            for (int i = events.Length - 1; i >= 0; i--)
                if (events[i].Cue == cue && events[i].Success) return i;
            return -1;
        }
    }
}
