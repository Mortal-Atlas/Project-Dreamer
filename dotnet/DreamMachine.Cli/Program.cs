using System;
using System.Diagnostics;
using DreamMachine.Development;
using DreamMachine.Experiment;
using DreamMachine.Sim;
using DreamMachine.Social;

namespace DreamMachine.Cli
{
    /// <summary>
    /// Headless stand-in for Unity batchmode. Same core, same CSV columns.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--export-looks" && i + 1 < args.Length)
                    {
                        string json = AppearanceCatalog.ToJson(AppearanceCatalog.Defaults(PopulationConfig.CreateDefault()));
                        string path = args[i + 1];
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");
                        System.IO.File.WriteAllText(path, json);
                        Console.WriteLine("Wrote " + path);
                        return 0;
                    }
                }
            }
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--room") return RoomExperimentCommand.Run(args);
                    if (args[i] == "--discover") return DiscoveryCommand.Run(args);
                    if (args[i] == "--archive-pilots") return ArchivePilotCommand.Run(args, i);
                    if (args[i] == "--benchmark")
                    {
                        int agents = 4;
                        for (int j = i + 1; j < args.Length; j++)
                        {
                            if (args[j] == "--agents" && j + 1 < args.Length) agents = int.Parse(args[j + 1]);
                        }
                        PopulationConfig pop = agents >= 12 ? PopulationConfig.CreateBalanced(3) : PopulationConfig.CreateDefault();
                        WorldSimulation world = WorldSimulation.Create(pop, QualityPreset.Laptop, 11);
                        int warmup = 30;
                        world.RunTicks(warmup);
                        var watch = Stopwatch.StartNew();
                        int measured = 2000;
                        world.RunTicks(measured);
                        watch.Stop();
                        double seconds = watch.Elapsed.TotalSeconds;
                        double rate = measured / seconds;
                        Console.WriteLine("agents " + (pop.BabyCount + pop.MentorCount));
                        Console.WriteLine("learners " + pop.BabyCount);
                        Console.WriteLine("grid " + world.GridSize);
                        Console.WriteLine("ticks " + measured);
                        Console.WriteLine("seconds " + seconds.ToString("0.000"));
                        Console.WriteLine("ticks_per_second " + rate.ToString("0.0"));
                        Console.WriteLine("sim_seconds_per_wall_second " + (rate * WorldSimulation.Dt).ToString("0.00"));
                        return 0;
                    }
                    if (args[i] == "--serve")
                    {
                        var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Laptop, 11);
                        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 4077);
                        listener.Start();
                        Console.WriteLine("Dream Machine sim listening on 4077");
                        using (var client = listener.AcceptTcpClient())
                        using (var writer = new System.IO.StreamWriter(client.GetStream()) { AutoFlush = true })
                        {
                            while (client.Connected)
                            {
                                world.RunTicks(10);
                                writer.WriteLine(Snapshot(world));
                            }
                        }
                        return 0;
                    }
                }
            }
            ExperimentConfig config = ExperimentConfig.ParseArgs(args);
            if (config.CsvPath == "ExperimentOutput/belief_trials.csv")
            {
                config.CsvPath = "ExperimentOutput/belief_trials.csv";
            }
            var experiment = new BeliefExperiment();
            ExperimentReport report = experiment.Run(config);
            report.WriteCsv(config.CsvPath);
            string summary = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(config.CsvPath) ?? "ExperimentOutput",
                "belief_summary.csv");
            report.WriteSummary(summary);
            Console.WriteLine(report.Verdict());
            Console.WriteLine("Trial log: " + System.IO.Path.GetFullPath(config.CsvPath));
            Console.WriteLine("Summary:   " + System.IO.Path.GetFullPath(summary));

            var nursery = NurserySimulator.Create(PopulationConfig.CreateDefault(), new NurseryOptions
            {
                WalkIterations = 8,
                ObjectEncounters = 3,
                PerformanceMode = true
            });
            nursery.RunDays(2);
            string growth = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(config.CsvPath) ?? "ExperimentOutput",
                "growth_days.csv");
            nursery.WriteGrowthCsv(growth);
            Console.WriteLine("Growth:    " + System.IO.Path.GetFullPath(growth));
            var household = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, 3);
            household.RunTicks(200);
            string physio = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(config.CsvPath) ?? "ExperimentOutput",
                "physiology.csv");
            household.WritePhysiologyCsv(physio);
            Console.WriteLine("Physiology:" + System.IO.Path.GetFullPath(physio));
            Console.WriteLine("Days simulated: " + nursery.Day + ", babies: " + nursery.Babies.Count);
            return 0;
        }

        static string Snapshot(WorldSimulation world)
        {
            var text = new System.Text.StringBuilder();
            text.Append(world.TickCount);
            text.Append(',');
            text.Append(world.SimSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            text.Append(',');
            text.Append(world.IsNight ? 1 : 0);
            for (int i = 0; i < world.Children.Count; i++)
            {
                var child = world.Children[i];
                text.Append(',');
                text.Append(child.Spec.Id);
                text.Append(',');
                text.Append(child.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                text.Append(',');
                text.Append(child.Z.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                text.Append(',');
                text.Append(child.Body.AgeDays.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                text.Append(',');
                text.Append(child.Body.HeartRate.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }
    }
}
