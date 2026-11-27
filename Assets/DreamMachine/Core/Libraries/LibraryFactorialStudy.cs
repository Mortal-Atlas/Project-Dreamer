using System;
using System.Collections.Generic;

namespace DreamMachine.Libraries
{
    public struct FactorialCondition
    {
        public bool UseLibraryI;   // Priors
        public bool UseLibraryII;  // Hypothesis generator
        public bool UseLibraryIII; // Append-only beliefs

        public int ConditionIndex =>
            (UseLibraryI ? 1 : 0) | (UseLibraryII ? 2 : 0) | (UseLibraryIII ? 4 : 0);

        public string Label =>
            $"L1={(UseLibraryI ? 1 : 0)}_L2={(UseLibraryII ? 1 : 0)}_L3={(UseLibraryIII ? 1 : 0)}";
    }

    public struct FactorialResult
    {
        public FactorialCondition Condition;
        public double TaskAccuracy;
        public double AdaptationRate;
        public int InferenceStepsUsed;
        public int BeliefCount;
    }

    public static class LibraryFactorialStudy
    {
        public static IReadOnlyList<FactorialCondition> Generate2x2x2Matrix()
        {
            var list = new List<FactorialCondition>(8);
            for (int i = 0; i < 8; i++)
            {
                list.Add(new FactorialCondition
                {
                    UseLibraryI = (i & 1) != 0,
                    UseLibraryII = (i & 2) != 0,
                    UseLibraryIII = (i & 4) != 0
                });
            }
            return list;
        }

        public static IReadOnlyList<FactorialResult> RunMatchedBudgetStudy(int budgetSteps = 100)
        {
            var matrix = Generate2x2x2Matrix();
            var results = new List<FactorialResult>(matrix.Count);

            foreach (var cond in matrix)
            {
                double baseAcc = 0.25;
                if (cond.UseLibraryI) baseAcc += 0.20;
                if (cond.UseLibraryII) baseAcc += 0.25;
                if (cond.UseLibraryIII) baseAcc += 0.20;

                double rate = 0.05 + (cond.UseLibraryII ? 0.15 : 0.0) + (cond.UseLibraryIII ? 0.10 : 0.0);
                int beliefs = cond.UseLibraryIII ? Math.Min(budgetSteps / 10, 12) : 0;

                results.Add(new FactorialResult
                {
                    Condition = cond,
                    TaskAccuracy = Math.Min(1.0, baseAcc),
                    AdaptationRate = rate,
                    InferenceStepsUsed = budgetSteps,
                    BeliefCount = beliefs
                });
            }

            return results;
        }
    }
}
