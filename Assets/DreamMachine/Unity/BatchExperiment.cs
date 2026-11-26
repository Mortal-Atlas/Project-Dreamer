using System.IO;
using DreamMachine.Experiment;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Headless entry point.
    /// Unity -batchmode -nographics -executeMethod DreamMachine.UnityBridge.BatchExperiment.Run -- --trials 80 --seeds 12 --out Results/belief_trials.csv
    /// Arguments after -- are read by the experiment config.
    /// </summary>
    public static class BatchExperiment
    {
        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            ExperimentConfig config = ExperimentConfig.ParseArgs(args);
            if (config.CsvPath == "ExperimentOutput/belief_trials.csv")
            {
                config.CsvPath = Path.Combine(Application.persistentDataPath, "belief_trials.csv");
            }
            var report = new BeliefExperiment().Run(config);
            report.WriteCsv(config.CsvPath);
            string summary = Path.Combine(Path.GetDirectoryName(config.CsvPath) ?? Application.persistentDataPath, "belief_summary.csv");
            report.WriteSummary(summary);
            Debug.Log(report.Verdict());
            Debug.Log("Wrote " + config.CsvPath);
            Debug.Log("Wrote " + summary);
            if (Application.isBatchMode)
            {
                Application.Quit(0);
            }
        }
    }
}
