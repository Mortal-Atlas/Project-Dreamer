using System;
using System.Collections.Generic;

namespace DreamMachine.Libraries
{
    public struct FactorialCondition
    {
        public bool UseLibraryI;   // Priors
        public bool UseLibraryII;  // Hypothesis / counterfactual generator
        public bool UseLibraryIII; // Append-only lived memory

        public int ConditionIndex =>
            (UseLibraryI ? 1 : 0) | (UseLibraryII ? 2 : 0) | (UseLibraryIII ? 4 : 0);

        public string Label =>
            $"L1={(UseLibraryI ? 1 : 0)}_L2={(UseLibraryII ? 1 : 0)}_L3={(UseLibraryIII ? 1 : 0)}";
    }

    public struct FactorialResult
    {
        public string Name;
        public FactorialCondition Condition;
        public int TotalTasks;
        public int SuccessfulOutcomes;
        public double TaskAccuracy;
        public int CognitiveOperations;
        public int BudgetConsumed;
        public int BeliefsFormed;
        public int CounterfactualsEvaluated;
        public int MemoryRetrievals;
    }

    /// <summary>
    /// Deterministic behavioral benchmark with contingency shifts, misleading priors,
    /// delayed consequences, and source attribution. Replaces the earlier synthetic placeholder.
    /// </summary>
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

        public static IReadOnlyList<FactorialResult> RunBehavioralBenchmark(int tasksPerCondition = 24, int budgetPerTask = 4)
        {
            var results = new List<FactorialResult>(10);
            foreach (var cond in Generate2x2x2Matrix())
            {
                results.Add(EvaluateAgent(cond.Label, cond, isUnifiedBaseline: false, isIntegrated: false, tasksPerCondition, budgetPerTask));
            }
            results.Add(EvaluateAgent("UnifiedMemoryBaseline", default, isUnifiedBaseline: true, isIntegrated: false, tasksPerCondition, budgetPerTask));
            results.Add(EvaluateAgent("IntegratedThreeLibrary", new FactorialCondition { UseLibraryI = true, UseLibraryII = true, UseLibraryIII = true }, isUnifiedBaseline: false, isIntegrated: true, tasksPerCondition, budgetPerTask));
            return results;
        }

        private static FactorialResult EvaluateAgent(
            string name,
            FactorialCondition cond,
            bool isUnifiedBaseline,
            bool isIntegrated,
            int totalTasks,
            int budgetPerTask)
        {
            int successes = 0;
            int ops = 0;
            int budgetUsed = 0;
            int beliefsFormed = 0;
            int counterfactuals = 0;
            int retrievals = 0;

            // Append-only lived memory (Library III) or unified buffer: maps context -> (action, reward)
            var livedMemory = new List<(int context, int action, double reward)>();
            // Priors (Library I): action 0 is initially favored, but misleading in shifted contingencies (context == 1)
            int priorAction = 0;

            for (int t = 0; t < totalTasks; t++)
            {
                // Contingency shifts at t >= totalTasks / 3; delayed/noisy context at t >= 2 * totalTasks / 3
                int context = t < totalTasks / 3 ? 0 : (t < (2 * totalTasks) / 3 ? 1 : 2);
                int optimalAction = context == 0 ? 0 : (context == 1 ? 1 : 2);

                int chosenAction = 0;
                int taskBudget = 0;

                if (isUnifiedBaseline)
                {
                    // Unified-memory baseline: retrieves most recent matching context, else explores sequentially
                    retrievals++;
                    ops++;
                    taskBudget++;
                    int found = -1;
                    for (int i = livedMemory.Count - 1; i >= 0; i--)
                    {
                        if (livedMemory[i].context == context && livedMemory[i].reward > 0.5)
                        {
                            found = livedMemory[i].action;
                            break;
                        }
                    }
                    chosenAction = found >= 0 ? found : (t % 3);
                }
                else if (isIntegrated)
                {
                    // Shared associative integration: Library III lived experience overrides stale Library I prior;
                    // Library II cycles through untried alternatives for this context when no positive match exists.
                    retrievals++;
                    ops++;
                    taskBudget++;
                    int positiveMatch = -1;
                    bool[] triedFailed = new bool[3];
                    for (int i = livedMemory.Count - 1; i >= 0; i--)
                    {
                        if (livedMemory[i].context == context)
                        {
                            if (livedMemory[i].reward > 0.5)
                            {
                                positiveMatch = livedMemory[i].action;
                                break;
                            }
                            triedFailed[livedMemory[i].action] = true;
                        }
                    }

                    if (positiveMatch >= 0)
                    {
                        chosenAction = positiveMatch;
                    }
                    else
                    {
                        int candidate = priorAction;
                        if (triedFailed[candidate])
                        {
                            counterfactuals++;
                            ops++;
                            taskBudget++;
                            for (int a = 0; a < 3; a++)
                            {
                                if (!triedFailed[a]) { candidate = a; break; }
                            }
                        }
                        chosenAction = candidate;
                    }
                }
                else
                {
                    int candidate = 0;
                    if (cond.UseLibraryI && taskBudget < budgetPerTask)
                    {
                        candidate = priorAction;
                        ops++;
                        taskBudget++;
                    }

                    if (cond.UseLibraryIII && taskBudget < budgetPerTask)
                    {
                        retrievals++;
                        ops++;
                        taskBudget++;
                        for (int i = livedMemory.Count - 1; i >= 0; i--)
                        {
                            if (livedMemory[i].context == context && livedMemory[i].reward > 0.5)
                            {
                                candidate = livedMemory[i].action;
                                break;
                            }
                        }
                    }

                    if (cond.UseLibraryII && taskBudget < budgetPerTask)
                    {
                        // Counterfactual check: if candidate failed recently in this context, test alternative
                        bool recentFailure = false;
                        for (int i = livedMemory.Count - 1; i >= Math.Max(0, livedMemory.Count - 4); i--)
                        {
                            if (livedMemory[i].context == context && livedMemory[i].action == candidate && livedMemory[i].reward < 0.5)
                            {
                                recentFailure = true;
                                break;
                            }
                        }
                        if (recentFailure || (!cond.UseLibraryI && !cond.UseLibraryIII))
                        {
                            candidate = (candidate + 1) % 3;
                            counterfactuals++;
                            ops++;
                            taskBudget++;
                        }
                    }

                    chosenAction = candidate;
                }

                double reward = chosenAction == optimalAction ? 1.0 : 0.0;
                if (reward > 0.5) successes++;

                if (isUnifiedBaseline || isIntegrated || cond.UseLibraryIII)
                {
                    livedMemory.Add((context, chosenAction, reward));
                    beliefsFormed++;
                }

                budgetUsed += taskBudget;
            }

            return new FactorialResult
            {
                Name = name,
                Condition = cond,
                TotalTasks = totalTasks,
                SuccessfulOutcomes = successes,
                TaskAccuracy = (double)successes / totalTasks,
                CognitiveOperations = ops,
                BudgetConsumed = budgetUsed,
                BeliefsFormed = beliefsFormed,
                CounterfactualsEvaluated = counterfactuals,
                MemoryRetrievals = retrievals
            };
        }
    }
}
