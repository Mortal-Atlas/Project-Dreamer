using System;
using System.Collections.Generic;
using DreamMachine.Util;

namespace DreamMachine.Sim
{
    public static class ActiveSocketStudy
    {
        public readonly struct Trial
        {
            public readonly SocketPolicy Policy;
            public readonly SocketDecision Decision;
            public readonly int Cue, Answer, StartTick, Retrievals;
            public readonly bool Changed, RepeatedError;
            public int ActionTick => StartTick + 18 + Decision.Observations * SocketInspection.InspectionTicks;
            public bool Success => Decision.Choice == Answer;
            internal Trial(SocketPolicy policy, SocketDecision decision, int cue, int answer, int startTick,
                int retrievals, bool changed, bool repeatedError)
            {
                Policy = policy; Decision = decision; Cue = cue; Answer = answer; StartTick = startTick;
                Retrievals = retrievals; Changed = changed; RepeatedError = repeatedError;
            }
        }

        public static IReadOnlyList<Trial> Run(int seed)
        {
            var archive = HouseholdDelayedRetention.Acquire(seed, out var clock);
            clock.RunTicks(HouseholdDelayedRetention.DelayTicks);
            var result = new List<Trial>(60);
            for (int condition = 0; condition < 5; condition++)
            {
                var policy = (SocketPolicy)condition;
                var rng = new SeededRandom(unchecked(69 * seed + 2039));
                for (int layout = 0; layout < 2; layout++)
                for (int repeat = 0; repeat < 2; repeat++)
                for (int cue = 0; cue < 3; cue++)
                {
                    int fallback = rng.NextInt(3), occluded = rng.NextInt(3);
                    int observedMarker = layout == 0 || seed % 4 == 0 ? 1 : 2;
                    int shift = layout == 0 ? 0 : 1 + (seed + repeat) % 2;
                    bool flip = layout != 0 && (seed + repeat) % 3 == 0;
                    int ShapeAt(int slot) => (flip ? 3 - slot + shift : slot + shift) % 3;
                    SocketFeatures Sense(int slot)
                    {
                        var shape = SocketFeatures.Shape(ShapeAt(slot));
                        int edge = slot == occluded && layout != 0 ? 0 : shape.Edge + ((seed + 2 * slot + repeat) % 3 - 1);
                        return new SocketFeatures(shape.Aspect, edge);
                    }
                    int source = policy == SocketPolicy.Exploration ? -1 : HouseholdRetention.LastSuccess(archive, cue);
                    int slotHint = source >= 0 ? archive[source].Choice : -1;
                    var decision = SocketInspection.Choose(policy, SocketFeatures.Shape(cue), observedMarker, slotHint, 1, -1, fallback, Sense);
                    int answer = 0;
                    while (ShapeAt(answer) != cue) answer++;
                    bool error = decision.Choice != answer && Array.Exists(archive,
                        record => record.Cue == cue && !record.Success && record.Choice == decision.Choice);
                    result.Add(new Trial(policy, decision, cue, answer, clock.TickCount, policy == SocketPolicy.Exploration ? 0 : 1,
                        layout != 0, error));
                }
            }
            return result;
        }
    }
}
