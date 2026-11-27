using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DreamMachine.Sim;
using DreamMachine.Social;

namespace DreamMachine.Cli
{
    static class SpatialStudyCommand
    {
        const int EndTick = 3600;

        static WorldSimulation Create(SpatialScenario scenario, int seed, int origin)
        {
            var world = WorldSimulation.Create(PopulationConfig.CreateDefault(), QualityPreset.Phone, seed);
            if (!world.EnableHouseholdLife()) throw new InvalidOperationException("The search requires the founding household.");
            world.Home.ConfigureSpatialScenario(scenario, origin);
            return world;
        }

        public static int Run(string path)
        {
            using var output = File.CreateText(path);
            output.WriteLine("condition,seed,origin,first_candidate,observed,stage,sighting_tick,relocation_tick,last_observation_tick,delay_to_intention_ticks,intervening_activities,checks,negative_checks,similar_observations,label_selections,search_records");
            foreach (SpatialScenario scenario in Enum.GetValues<SpatialScenario>())
            {
                for (int i = 0; i < 12; i++)
                {
                    int seed = 16661 + i * 149, origin = i % HouseholdLife.SearchBoxCount;
                    var world = Create(scenario, seed, origin);
                    world.RunTicks(EndTick);
                    var home = world.Home;
                    var search = home.ReadSpatialSearch();
                    if (search.Sighting < 0) throw new InvalidOperationException($"Sighting absent for {scenario}, {seed}.");
                    var records = Enumerable.Range(0, home.ExperienceCount).Select(home.Experience).ToArray();
                    var plans = records.Where(e => e.Evidence == HomeEvidence.SearchCandidate).ToArray();
                    int first = plans.Length > 0 ? plans[0].Target - HouseholdLife.FirstSearchBox : -1;
                    int firstTick = plans.Length > 0 ? plans[0].Tick : EndTick;
                    int sighting = home.Experience(search.Sighting).Tick;
                    int between = records.Count(e => e.Actor == HouseholdLife.SearchActor && e.Tick > sighting &&
                        e.Tick < firstTick && e.Activity != HomeActivity.FindToy);
                    int checks = records.Count(e => e.Evidence == HomeEvidence.ToyAbsent || e.Evidence == HomeEvidence.ToyFound);
                    int negative = records.Count(e => e.Evidence == HomeEvidence.ToyAbsent);
                    int similar = records.Count(e => e.Evidence == HomeEvidence.ToyAbsent && e.Details == HouseholdLife.SimilarToy);
                    int labels = plans.Count(e => e.Details >> 3 == (int)SpatialSelection.VisibleLabel);
                    output.WriteLine($"{scenario},{seed},{origin},{first},{search.Observed},{search.Stage},{sighting}," +
                        $"{home.SpatialEnvironmentChangeTick},{home.Experience(search.LastObservation).Tick},{firstTick-sighting},{between}," +
                        $"{checks},{negative},{similar},{labels},{records.Count(e => e.Activity == HomeActivity.FindToy)}");
                }
            }
            return 0;
        }

        public static int Checkpoint(string mode, string path)
        {
            if (mode == "save")
            {
                var world = Create(SpatialScenario.SimilarToy, 7669, 0);
                while (world.TickCount < EndTick)
                {
                    world.Tick();
                    var search = world.Home.ReadSpatialSearch();
                    var person = world.Home.ReadPerson(HouseholdLife.SearchActor);
                    if (search.Negative == 0 || person.Activity != HomeActivity.FindToy || person.Phase != HomePhase.Approach) continue;
                    byte[] checkpoint = world.CaptureCheckpoint();
                    File.WriteAllBytes(path, checkpoint);
                    Console.WriteLine($"midsearch tick={world.TickCount} visited={search.Visited} negative={search.Negative} selected={search.Selected} remaining={search.Remaining} bytes={checkpoint.Length}");
                    world.RunTicks(EndTick - world.TickCount);
                    File.WriteAllBytes(path + ".control", world.CaptureCheckpoint());
                    return 0;
                }
                return 1;
            }
            if (mode != "resume") throw new ArgumentException("Use save or resume.");
            var timer = Stopwatch.StartNew();
            var restored = WorldSimulation.LoadCheckpoint(path);
            timer.Stop();
            double restorationMs = timer.Elapsed.TotalMilliseconds;
            restored.RunTicks(EndTick - restored.TickCount);
            byte[] continuation = restored.CaptureCheckpoint();
            bool exact = continuation.SequenceEqual(File.ReadAllBytes(path + ".control"));
            Console.WriteLine($"restored_ms={restorationMs:F3} exact={exact} outcome={restored.Home.ReadSpatialSearch().Stage} " +
                $"end_bytes={continuation.Length} episodes={restored.Home.ExperienceCount}");
            return exact ? 0 : 1;
        }
    }
}
