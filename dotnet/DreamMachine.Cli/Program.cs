using System;
using System.Diagnostics;
using DreamMachine.Body;
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
                    if (args[i] == "--lower-body") return RunLowerBodyReference();
                    if (args[i] == "--retention" && i + 1 < args.Length) return RunHouseholdRetention(args[i + 1]);
                    if (args[i] == "--delayed-retention" && i + 1 < args.Length) return RunDelayedRetention(args[i + 1]);
                    if (args[i] == "--spatial-study" && i + 1 < args.Length) return SpatialStudyCommand.Run(args[i + 1]);
                    if (args[i] == "--spatial-checkpoint" && i + 2 < args.Length) return SpatialStudyCommand.Checkpoint(args[i + 1], args[i + 2]);
                    if (args[i] == "--socket-confirmation" && i + 1 < args.Length) return SocketConfirmationCommand.Run(args[i + 1]);
                    if (args[i] == "--active-socket" && i + 1 < args.Length) return RunActiveSocket(args[i + 1]);
                    if (args[i] == "--elbow-lab" && i + 1 < args.Length) return ElbowLabCommand.Run(args[i + 1]);
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
                        bool living = Array.IndexOf(args, "--living") >= 0;
                        if (living && !world.EnableHouseholdLife())
                        {
                            Console.Error.WriteLine("The living benchmark uses the four-person founding household.");
                            return 1;
                        }
                        int warmup = 30;
                        world.RunTicks(warmup);
                        var watch = Stopwatch.StartNew();
                        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
                        int measured = 2000;
                        world.RunTicks(measured);
                        watch.Stop();
                        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                        double seconds = watch.Elapsed.TotalSeconds;
                        double rate = measured / seconds;
                        Console.WriteLine("agents " + (pop.BabyCount + pop.MentorCount));
                        Console.WriteLine("learners " + pop.BabyCount);
                        Console.WriteLine("grid " + world.GridSize);
                        Console.WriteLine("ticks " + measured);
                        Console.WriteLine("seconds " + seconds.ToString("0.000"));
                        Console.WriteLine("ticks_per_second " + rate.ToString("0.0"));
                        Console.WriteLine("sim_seconds_per_wall_second " + (rate * WorldSimulation.Dt).ToString("0.00"));
                        if (living)
                        {
                            Console.WriteLine("experience_records " + world.Home.ExperienceCount);
                            Console.WriteLine("measured_allocated_bytes " + allocated);
                            Console.WriteLine("checkpoint_bytes " + world.CaptureCheckpoint().Length);
                        }
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

        static int RunHouseholdRetention(string path)
        {
            using var report = System.IO.File.CreateText(path);
            report.WriteLine("cohort,seed,access,cue,arrangement,choice,decision,source,transfer,success,repeated_training_mistake");
            for (int seed = 771; seed <= 796; seed++)
            {
                string cohort = seed < 781 ? "development" : "heldout";
                foreach (RetentionAccess access in Enum.GetValues(typeof(RetentionAccess)))
                foreach (RetentionTrial trial in HouseholdRetention.Probe(seed, access))
                    report.WriteLine(FormattableString.Invariant($"{cohort},{trial.Seed},{trial.Access},{trial.Cue},{trial.Rotation},{trial.Choice},{trial.Decision},{trial.Source},{trial.Transfer},{trial.Success},{trial.RepeatedTrainingMistake}"));
            }
            return 0;
        }

        static int RunDelayedRetention(string path)
        {
            using var report = System.IO.File.CreateText(path);
            report.WriteLine("cohort,seed,access,cue,choice,actual_slot,source,transfer,success,repeated_error,training_tick,probe_tick");
            for (int seed = 1201; seed <= 1224; seed++)
            {
                string cohort = seed < 1209 ? "development" : "heldout";
                foreach (var trial in HouseholdDelayedRetention.Run(seed))
                    report.WriteLine(FormattableString.Invariant($"{cohort},{seed},{trial.Access},{trial.Cue},{trial.Choice},{trial.ActualSlot},{trial.Source},{trial.Transfer},{trial.Success},{trial.RepeatedError},{trial.LastTrainingTick},{trial.ProbeTick}"));
            }
            return 0;
        }

        static int RunActiveSocket(string path)
        {
            using var report = System.IO.File.CreateText(path);
            report.WriteLine("cohort,seed,policy,cue,choice,answer,changed,success,repeated_error,inspections,retrievals,uncertainty_before,uncertainty_after,contradiction,start_tick,action_tick,data");
            for (int seed = 1401; seed <= 1424; seed++)
            {
                string cohort = seed < 1409 ? "development" : "heldout";
                foreach (var trial in ActiveSocketStudy.Run(seed))
                {
                    var d = trial.Decision;
                    report.WriteLine(FormattableString.Invariant($"{cohort},{seed},{trial.Policy},{trial.Cue},{d.Choice},{trial.Answer},{trial.Changed},{trial.Success},{trial.RepeatedError},{d.Observations},{trial.Retrievals},{d.UncertaintyBefore},{d.UncertaintyAfter},{d.Contradiction},{trial.StartTick},{trial.ActionTick},{d.Data}"));
                }
            }
            return 0;
        }

        static int RunLowerBodyReference()
        {
            Console.WriteLine("age,condition,pelvis_m,left_knee_rad,right_knee_rad,left_support,right_support,load_supported,left_N,right_N");
            var anatomy = new HumanoidAnatomy(101);
            foreach (float age in new[] { 0f, 3f, 6f })
            {
                var d = anatomy.AtAge(age);
                foreach (var trial in new[]
                {
                    (Name: "centred", Distance: 0f, Stepping: false, X: 0f),
                    (Name: "shift-left", Distance: 0f, Stepping: false, X: -d.HipWidth / 4f),
                    (Name: "supported-step", Distance: GroundedGait.Stride(d) * 0.125f, Stepping: true, X: d.HipWidth / 2f),
                    (Name: "unsupported-step", Distance: GroundedGait.Stride(d) * 0.125f, Stepping: true, X: -d.HipWidth / 2f)
                })
                {
                    var sample = LowerBodyReference.Evaluate(d, trial.Distance, trial.Stepping, trial.X, 0f, 10f);
                    Console.WriteLine(FormattableString.Invariant($"{age},{trial.Name},{GroundedGait.PelvisHeight(d, trial.Stepping):F5},{sample.Left.Knee:F5},{sample.Right.Knee:F5},{sample.Left.Supports},{sample.Right.Supports},{sample.LoadInsideSupport},{sample.LeftVerticalNewtons:F4},{sample.RightVerticalNewtons:F4}"));
                }
            }
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
