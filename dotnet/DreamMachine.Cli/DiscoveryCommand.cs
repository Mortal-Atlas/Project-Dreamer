using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamMachine.Experiment;
using DreamMachine.Util;

namespace DreamMachine.Cli
{
    static class DiscoveryCommand
    {
        public static int Run(string[] args)
        {
            int start = 1;
            int seeds = 64;
            int daily = 18;
            string output = "ExperimentOutput/discovery";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--discover") continue;
                if (i + 1 == args.Length) return Usage();
                string option = args[i];
                string value = args[++i];
                switch (option)
                {
                    case "--start-seed" when int.TryParse(value, out start): break;
                    case "--seeds" when int.TryParse(value, out seeds): break;
                    case "--attempts-per-day" when int.TryParse(value, out daily): break;
                    case "--out" when !string.IsNullOrWhiteSpace(value) && !value.StartsWith("--", StringComparison.Ordinal):
                        output = value;
                        break;
                    default: return Usage();
                }
            }
            if (seeds <= 0 || (long)start + seeds - 1 > int.MaxValue ||
                daily < DiscoveryRoom.MinimumAttemptsPerDay || daily > DiscoveryRoom.MaximumAttemptsPerDay)
                return Usage();

            var attempts = new List<IList<string>>();
            var summary = new List<IList<string>>();
            foreach (bool curiosity in new[] { false, true })
            foreach (bool replay in new[] { false, true })
            {
                int retained = 0;
                int transferred = 0;
                long noise = 0;
                for (int run = 0; run < seeds; run++)
                {
                    int seed = start + run;
                    var room = new DiscoveryRoom(seed, daily, curiosity, replay);
                    room.RunTicks(DiscoveryRoom.EndTick);
                    foreach (DiscoveryAttempt attempt in room.Attempts)
                    {
                        attempts.Add(new[]
                        {
                            "1", Count(seed), Flag(curiosity), Flag(replay), Count(attempt.Id), Count(attempt.Tick),
                            Count(attempt.Control), Count(attempt.Cue), Count(attempt.Movement), Flag(attempt.Effect),
                            Format(attempt.Prediction), Format(attempt.InformationGain), OptionalCount(attempt.ReplaySource)
                        });
                    }
                    summary.Add(new[]
                    {
                        Count(seed), Flag(curiosity), Flag(replay), Count(room.Attempts.Count), Count(room.AttemptsOnNoise),
                        OptionalCount(room.FirstUnlockTick), OptionalCount(room.RehearsalSourceId), Format(room.MeanBrierLoss),
                        Format(room.Questions[0].Confidence), Format(room.Questions[1].Confidence),
                        Flag(room.Questions[0].Resolved), Flag(room.Questions[1].Resolved),
                        Flag(room.Retained.Value), Flag(room.Transferred.Value)
                    });
                    if (room.Retained.Value) retained++;
                    if (room.Transferred.Value) transferred++;
                    noise += room.AttemptsOnNoise;
                }
                Console.WriteLine($"Curiosity {Flag(curiosity)}, replay {Flag(replay)} | retention {retained}/{seeds} | transfer {transferred}/{seeds} | noise {noise}/{(long)seeds * daily * 2}");
            }
            CsvTable.Write(Path.Combine(output, "discovery_attempts.csv"), new[]
            {
                "schema_version", "seed", "curiosity", "replay", "attempt_id", "tick", "control", "cue", "movement",
                "effect", "prediction", "expected_information_bits", "replay_source_attempt"
            }, attempts);
            CsvTable.Write(Path.Combine(output, "discovery_summary.csv"), new[]
            {
                "seed", "curiosity", "replay", "attempts", "noise_attempts", "first_unlock_tick", "rehearsal_source_attempt",
                "brier_loss", "control_0_confidence", "control_1_confidence", "control_0_resolved", "control_1_resolved",
                "retention", "transfer"
            }, summary);
            return 0;
        }

        static int Usage()
        {
            Console.Error.WriteLine("Discovery options: --discover [--seeds positive-count] [--start-seed integer] [--attempts-per-day 3-719] [--out directory]");
            return 2;
        }

        static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
        static string OptionalCount(int? value) => value.HasValue ? Count(value.Value) : "";
        static string Flag(bool value) => value ? "1" : "0";
        static string Format(double value) => value.ToString("0.#########", CultureInfo.InvariantCulture);
    }
}
