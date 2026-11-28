using System.Collections.Generic;
using DreamMachine.Social;
using DreamMachine.Util;

namespace DreamMachine.Sim
{
    public static class HouseholdDelayedRetention
    {
        public const int DelayTicks = 2080;
        static readonly int[,] HiddenLayouts = { { 0, 2, 1 }, { 1, 0, 2 }, { 1, 2, 0 }, { 2, 0, 1 }, { 2, 1, 0 } };

        public readonly struct Trial
        {
            public readonly RetentionAccess Access;
            public readonly int Cue, Choice, ActualSlot, Source, LastTrainingTick, ProbeTick;
            public readonly bool Transfer, RepeatedError;
            public bool Success => Choice == ActualSlot;

            internal Trial(RetentionAccess access, int cue, int choice, int actualSlot, int source, bool transfer,
                bool repeatedError, int lastTrainingTick, int probeTick)
            {
                Access = access; Cue = cue; Choice = choice; ActualSlot = actualSlot; Source = source;
                Transfer = transfer; RepeatedError = repeatedError; LastTrainingTick = lastTrainingTick; ProbeTick = probeTick;
            }
        }

        public static IReadOnlyList<Trial> Run(int seed)
        {
            ShapeEpisode[] events = Acquire(seed, out var world);
            int lastTrainingTick = world.TickCount;
            world.RunTicks(DelayTicks);
            var layoutRng = new SeededRandom(unchecked(seed * 47 + 941));
            int layout = layoutRng.NextInt(HiddenLayouts.GetLength(0));
            var trials = new List<Trial>(48);
            for (int condition = 0; condition < 4; condition++)
            {
                var access = (RetentionAccess)condition;
                int[] plans = HouseholdRetention.CompilePlans(events, seed, access);
                var planChoices = new int[3];
                for (int cue = 0; cue < 3; cue++) planChoices[cue] = plans[cue] < 0 ? -1 : events[plans[cue]].Choice;
                ShapeEpisode[] episodes = (access & RetentionAccess.Retrieval) != 0 ? events : null;
                var exploration = new SeededRandom(unchecked(seed * 71 + 5501));
                for (int arrangement = 0; arrangement < 2; arrangement++)
                for (int repeat = 0; repeat < 2; repeat++)
                for (int cue = 0; cue < 3; cue++)
                {
                    int choice = exploration.NextInt(3);
                    int source = plans[cue];
                    if (source >= 0) choice = planChoices[cue];
                    else if (episodes != null)
                    {
                        source = HouseholdRetention.LastSuccess(episodes, cue);
                        if (source >= 0) choice = episodes[source].Choice;
                    }
                    // Choice has only the learned slot and matched exploration. The evaluator alone sees the new layout.
                    int answer = arrangement == 0 ? cue : HiddenLayouts[layout, cue];
                    bool repeated = false;
                    if (choice != answer)
                        foreach (var episode in events)
                            if (episode.Cue == cue && !episode.Success && episode.Choice == choice) { repeated = true; break; }
                    trials.Add(new Trial(access, cue, choice, answer, source, arrangement != 0, repeated,
                        lastTrainingTick, world.TickCount));
                }
            }
            return trials;
        }

        internal static ShapeEpisode[] Acquire(int seed, out WorldSimulation world)
        {
            world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, seed);
            var events = new ShapeEpisode[12];
            var training = new SeededRandom(seed);
            int written = 0;
            for (int day = 0; day < 2; day++)
            {
                int dayStart = day * (int)(WorldSimulation.DayLength / WorldSimulation.Dt);
                world.RunTicks(dayStart + 150 - world.TickCount);
                for (int cue = 0; cue < 3; cue++)
                {
                    int first = training.NextInt(3);
                    for (int visit = 0; visit < 2; visit++)
                    {
                        world.RunTicks(45);
                        events[written++] = new ShapeEpisode(world.DayIndex, cue, 0, (first + visit) % 3);
                    }
                }
            }
            return events;
        }
    }
}
