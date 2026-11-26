using System.Collections.Generic;
using DreamMachine.Mind;
using DreamMachine.Util;
using DreamMachine.World;

namespace DreamMachine.Experiment
{
    public sealed class ExperimentConfig
    {
        public int[] Seeds;
        public int Trials = 80;
        public int ShiftAt = 40;
        public string CsvPath = "ExperimentOutput/belief_trials.csv";

        public static ExperimentConfig Standard()
        {
            return new ExperimentConfig
            {
                Seeds = Sequence(1, 12),
                Trials = 80,
                ShiftAt = 40
            };
        }

        public static ExperimentConfig Short()
        {
            return new ExperimentConfig
            {
                Seeds = Sequence(1, 6),
                Trials = 60,
                ShiftAt = 30,
                CsvPath = "ExperimentOutput/belief_trials_short.csv"
            };
        }

        public static ExperimentConfig ParseArgs(string[] args)
        {
            ExperimentConfig config = Standard();
            if (args == null) return config;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "--trials" && i + 1 < args.Length) config.Trials = int.Parse(args[++i]);
                else if (a == "--shift" && i + 1 < args.Length) config.ShiftAt = int.Parse(args[++i]);
                else if (a == "--seeds" && i + 1 < args.Length) config.Seeds = Sequence(1, int.Parse(args[++i]));
                else if (a == "--out" && i + 1 < args.Length) config.CsvPath = args[++i];
                else if (a == "--short") config = Short();
            }
            return config;
        }

        static int[] Sequence(int start, int count)
        {
            var values = new int[count];
            for (int i = 0; i < count; i++) values[i] = start + i;
            return values;
        }
    }

    public sealed class TrialRow
    {
        public string Condition;
        public int Seed;
        public int Trial;
        public int Correct;
        public float CumulativeAccuracy;
        public string Hypothesis;
        public float Confidence;
        public int RulePhase;
        public int AppendixCount;
        public int BeliefCount;
        public string ObjectId;
        public string Coordinate;
    }

    public sealed class ExperimentReport
    {
        public List<TrialRow> Rows = new List<TrialRow>();
        public ExperimentConfig Config;

        public float MeanAccuracy(MindCondition condition, int startInclusive, int endExclusive)
        {
            string name = ConditionName(condition);
            int correct = 0;
            int total = 0;
            for (int i = 0; i < Rows.Count; i++)
            {
                TrialRow row = Rows[i];
                if (row.Condition != name) continue;
                if (row.Trial < startInclusive || row.Trial >= endExclusive) continue;
                correct += row.Correct;
                total++;
            }
            return total == 0 ? 0f : (float)correct / total;
        }

        public string Verdict()
        {
            int mid = Config.ShiftAt;
            int lateStart = mid + (Config.Trials - mid) / 2;
            float aEarly = MeanAccuracy(MindCondition.LibraryOneOnly, mid / 2, mid);
            float cEarly = MeanAccuracy(MindCondition.Full, mid / 2, mid);
            float aLate = MeanAccuracy(MindCondition.LibraryOneOnly, lateStart, Config.Trials);
            float cLate = MeanAccuracy(MindCondition.Full, lateStart, Config.Trials);
            bool supports = cEarly > aEarly && cLate > aLate;
            return (supports ? "SUPPORTS" : "DOES NOT SUPPORT")
                + " the claim. Before the shift, full mind accuracy "
                + cEarly.ToString("0.000") + " vs prior-only " + aEarly.ToString("0.000")
                + ". After the world changed, full mind " + cLate.ToString("0.000")
                + " vs prior-only " + aLate.ToString("0.000")
                + ". Beliefs were amended (appendices kept), not erased.";
        }

        public void WriteCsv(string path)
        {
            var header = new List<string>
            {
                "condition", "seed", "trial", "correct", "accuracy_cum", "hypothesis",
                "confidence", "rule_phase", "appendix_count", "belief_count", "object_id", "decision_coordinate"
            };
            var rows = new List<IList<string>>();
            for (int i = 0; i < Rows.Count; i++)
            {
                TrialRow r = Rows[i];
                rows.Add(new List<string>
                {
                    r.Condition,
                    r.Seed.ToString(),
                    r.Trial.ToString(),
                    r.Correct.ToString(),
                    CsvTable.Format(r.CumulativeAccuracy),
                    CsvTable.Escape(r.Hypothesis),
                    CsvTable.Format(r.Confidence),
                    r.RulePhase.ToString(),
                    r.AppendixCount.ToString(),
                    r.BeliefCount.ToString(),
                    r.ObjectId,
                    CsvTable.Escape(r.Coordinate)
                });
            }
            CsvTable.Write(path, header, rows);
        }

        public void WriteSummary(string path)
        {
            int mid = Config.ShiftAt;
            int lateStart = mid + (Config.Trials - mid) / 2;
            var header = new List<string> { "condition", "window", "start_trial", "end_trial", "mean_accuracy" };
            var rows = new List<IList<string>>();
            MindCondition[] conditions =
            {
                MindCondition.LibraryOneOnly,
                MindCondition.LibraryOneAndTwo,
                MindCondition.Full
            };
            int[][] windows =
            {
                new[] { 0, mid / 2 },
                new[] { mid / 2, mid },
                new[] { mid, lateStart },
                new[] { lateStart, Config.Trials }
            };
            string[] names = { "opening", "pre_shift", "post_shift", "late" };
            for (int c = 0; c < conditions.Length; c++)
            {
                for (int w = 0; w < windows.Length; w++)
                {
                    rows.Add(new List<string>
                    {
                        ConditionName(conditions[c]),
                        names[w],
                        windows[w][0].ToString(),
                        windows[w][1].ToString(),
                        CsvTable.Format(MeanAccuracy(conditions[c], windows[w][0], windows[w][1]))
                    });
                }
            }
            CsvTable.Write(path, header, rows);
        }

        public static string ConditionName(MindCondition condition)
        {
            if (condition == MindCondition.LibraryOneOnly) return "A_library_I";
            if (condition == MindCondition.LibraryOneAndTwo) return "B_library_I_II";
            return "C_library_I_II_III";
        }
    }

    public sealed class BeliefExperiment
    {
        public ExperimentReport Run(ExperimentConfig config)
        {
            var report = new ExperimentReport { Config = config };
            var pool = HiddenRuleWorld.StandardPool();
            var world = new HiddenRuleWorld { ShiftAt = config.ShiftAt };
            MindCondition[] conditions =
            {
                MindCondition.LibraryOneOnly,
                MindCondition.LibraryOneAndTwo,
                MindCondition.Full
            };
            for (int s = 0; s < config.Seeds.Length; s++)
            {
                int seed = config.Seeds[s];
                for (int c = 0; c < conditions.Length; c++)
                {
                    var mind = new AgentMind(seed, conditions[c]);
                    var rng = new SeededRandom(seed);
                    int correct = 0;
                    for (int trial = 0; trial < config.Trials; trial++)
                    {
                        ObjectInstance obj = pool[rng.NextInt(pool.Count)];
                        TrialDecision decision = mind.Encounter(obj, trial, world);
                        if (decision.Correct) correct++;
                        report.Rows.Add(new TrialRow
                        {
                            Condition = ExperimentReport.ConditionName(conditions[c]),
                            Seed = seed,
                            Trial = trial,
                            Correct = decision.Correct ? 1 : 0,
                            CumulativeAccuracy = (float)correct / (trial + 1),
                            Hypothesis = decision.Hypothesis,
                            Confidence = decision.Confidence,
                            RulePhase = decision.Phase,
                            AppendixCount = decision.AppendixCount,
                            BeliefCount = decision.BeliefCount,
                            ObjectId = decision.ObjectId,
                            Coordinate = decision.Coordinate.ToString()
                        });
                    }
                }
            }
            return report;
        }
    }
}
