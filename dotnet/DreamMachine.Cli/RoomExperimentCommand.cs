using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DreamMachine.Experiment;
using DreamMachine.Util;

namespace DreamMachine.Cli
{
    static class RoomExperimentCommand
    {
        public static int Run(string[] args)
        {
            const string usage = "Room experiment options: --room [--seeds positive-count] [--out directory]";
            int seeds = 32;
            string output = "ExperimentOutput/room";
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--room":
                        break;
                    case "--seeds" when i + 1 < args.Length && int.TryParse(args[i + 1], out int count) && count > 0:
                        seeds = count;
                        i++;
                        break;
                    case "--out" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]) && !args[i + 1].StartsWith("--", StringComparison.Ordinal):
                        output = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine(usage);
                        return 2;
                }
            }

            var events = new List<IList<string>>();
            var summaries = new List<IList<string>>();
            foreach (bool curiosity in new[] { false, true })
            {
                foreach (bool replay in new[] { false, true })
                {
                    int successfulProbes = 0;
                    int successfulTransfers = 0;
                    for (int run = 0; run < seeds; run++)
                    {
                        int seed = run + 1;
                        var room = new LatchRoomSimulation(seed, curiosity, replay);
                        room.RunTicks(LatchRoomSimulation.EndTick);
                        foreach (RoomEvent entry in room.Trace)
                        {
                            events.Add(new[]
                            {
                                "1", seed.ToString(), Flag(curiosity), Flag(replay), entry.Id.ToString(),
                                entry.Tick.ToString(), entry.Kind.ToString(), entry.Object?.ToString(),
                                entry.Action?.ToString(), entry.ParentId?.ToString(),
                                entry.Prediction.HasValue ? CsvTable.Format(entry.Prediction.Value) : "",
                                entry.Effect.HasValue ? Flag(entry.Effect.Value) : "",
                                entry.Position?.ToString(), Flag(entry.Transfer)
                            });
                        }
                        RoomEvent[] feedback = room.LivedMemory.Select(memory => room.Trace[memory.ParentId.Value - 1]).ToArray();
                        RoomEvent[] exploration = feedback.Where(entry => entry.Tick < LatchRoomSimulation.NightTick).ToArray();
                        bool retained = feedback.Single(entry => entry.Tick >= LatchRoomSimulation.WakeTick && !entry.Transfer).Effect.Value;
                        bool transferred = feedback.Single(entry => entry.Transfer).Effect.Value;
                        if (retained) successfulProbes++;
                        if (transferred) successfulTransfers++;
                        summaries.Add(new[]
                        {
                            seed.ToString(), Flag(curiosity), Flag(replay), exploration.Length.ToString(),
                            exploration.Count(entry => entry.Object == RoomObject.NoisePanel).ToString(),
                            CsvTable.Format(exploration.Average(entry => Loss(room.Trace[entry.ParentId.Value - 1].Prediction.Value, entry.Effect.Value))),
                            Flag(room.Trace.Any(entry => entry.Kind == RoomEventKind.Hypothesis)),
                            Flag(retained), Flag(transferred)
                        });
                    }
                    Console.WriteLine($"Curiosity {Flag(curiosity)}, replay {Flag(replay)}, next-day probe {successfulProbes}/{seeds}, transfer {successfulTransfers}/{seeds}");
                }
            }
            CsvTable.Write(Path.Combine(output, "room_events.csv"), new[]
            {
                "schema_version", "seed", "curiosity", "replay", "event_id", "tick", "kind", "object",
                "action", "parent_event_id", "prediction", "effect", "position", "transfer"
            }, events);
            CsvTable.Write(Path.Combine(output, "room_summary.csv"), new[]
            {
                "seed", "curiosity", "replay", "day_attempts", "noise_attempts", "day_brier_loss", "hypothesis",
                "next_day_success", "transfer_success"
            }, summaries);
            Console.WriteLine("Reports in " + Path.GetFullPath(output));
            return 0;
        }

        static string Flag(bool value) => value ? "1" : "0";

        static float Loss(float prediction, bool effect)
        {
            float error = prediction - (effect ? 1f : 0f);
            return error * error;
        }
    }
}
