using System;
using System.Diagnostics;
using System.IO;
using DreamMachine.Sim;
using DreamMachine.Util;

namespace DreamMachine.Cli
{
    static class SocketConfirmationCommand
    {
        public static int Run(string path)
        {
            using var output = File.CreateText(path);
            output.WriteLine("seed,layout,policy,cue,attempt,success,detailed,glances,observation_ticks,decision_stopwatch_ticks,contradicted,recovered_after_error");
            Console.WriteLine($"Stopwatch frequency {Stopwatch.Frequency}");
            foreach (var policy in new[] { SocketPolicy.Direct, SocketPolicy.Inspect, SocketPolicy.Contextual, SocketPolicy.Confirmed, SocketPolicy.Unified })
            for (int trial = 0; trial < 24; trial++)
            {
                int seed = 17431 + trial * 127;
                var random = new SeededRandom(seed);
                for (int layout = 0; layout < 3; layout++)
                {
                    int shift = layout == 0 ? 0 : 1 + random.NextInt(2), shade = random.NextInt(3);
                    int marker = layout == 2 ? 2 : 1;
                    for (int cue = 0; cue < 3; cue++)
                    {
                        int rejected = -1;
                        for (int attempt = 0; attempt < 2; attempt++)
                        {
                            int ShapeAt(int slot) => (slot + shift) % 3;
                            SocketFeatures Sense(int slot)
                            {
                                var sample = SocketFeatures.Shape(ShapeAt(slot));
                                int edge = layout != 0 && slot == shade ? 0 : sample.Edge + (seed + slot) % 3 - 1;
                                return new SocketFeatures(sample.Aspect, edge);
                            }
                            var piece = SocketFeatures.Shape(cue);
                            bool glanced = false;
                            long start = Stopwatch.GetTimestamp();
                            var decision = policy == SocketPolicy.Confirmed
                                ? SocketInspection.ChooseConfirmed(piece, marker, cue, 1, rejected, cue, Sense, Sense, out glanced)
                                : SocketInspection.Choose(policy, piece, marker, cue, 1, rejected, cue, Sense);
                            long cost = Stopwatch.GetTimestamp() - start;
                            bool success = ShapeAt(decision.Choice) == cue;
                            int observationCost = decision.Observations * SocketInspection.InspectionTicks +
                                (glanced ? SocketInspection.ConfirmationTicks : 0);
                            output.WriteLine($"{seed},{(layout == 0 ? "Familiar" : layout == 1 ? "Aliased" : "ChangedMarker")}," +
                                $"{policy},{cue},{attempt},{Flag(success)},{decision.Observations},{Flag(glanced)},{observationCost}," +
                                $"{cost},{Flag(decision.Contradiction)},{Flag(rejected >= 0 && success)}");
                            rejected = success ? -1 : decision.Choice;
                        }
                    }
                }
            }
            return 0;
        }

        static int Flag(bool value) => value ? 1 : 0;
    }
}
