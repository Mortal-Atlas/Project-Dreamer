using System;
using DreamMachine.Body;
using DreamMachine.Libraries;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class PhaseB2ValidationTests
    {
        [Test]
        public void RunPhaseB2_QuantitativeValidationSuite()
        {
            var devConfig = new RetentionTrialConfig
            {
                TargetAngle = 1.0,
                WallAngle = 1.15,
                WallStiffness = 25.0,
                Damping = 0.4,
                TicksPerTrial = 40
            };

            var heldOutConfig = new RetentionTrialConfig
            {
                TargetAngle = 0.85,
                WallAngle = 0.95,
                WallStiffness = 38.0,
                Damping = 0.65,
                TicksPerTrial = 45
            };

            // 1. Learned vs Engineered on Held-Out Config
            var eng = RetentionExperiment.RunSingleTrial(0, heldOutConfig, new DecomposedForceResidual(), RetentionMode.EngineeredBaseline);
            var frozen = RetentionExperiment.RunSingleTrial(0, heldOutConfig, new DecomposedForceResidual(), RetentionMode.FrozenLearned);

            var activeCtrl = new DecomposedForceResidual();
            RetentionTrialMetrics activeLast = default;
            for (int t = 0; t < 10; t++)
                activeLast = RetentionExperiment.RunSingleTrial(t, heldOutConfig, activeCtrl, RetentionMode.ContinuedAdaptation);

            TestContext.Out.WriteLine("=== HELD-OUT EVALUATION ===");
            TestContext.Out.WriteLine($"Engineered: Err={eng.FinalTrackingError:F4}, PeakImp={eng.PeakImpulse:F4}, Work={eng.CommandedMechanicalWork:F4}, Completed={eng.TaskCompleted}, Score={eng.CompositeScore:F4}");
            TestContext.Out.WriteLine($"FrozenDecomposed: Err={frozen.FinalTrackingError:F4}, PeakImp={frozen.PeakImpulse:F4}, Work={frozen.CommandedMechanicalWork:F4}, Completed={frozen.TaskCompleted}, Score={frozen.CompositeScore:F4}");
            TestContext.Out.WriteLine($"ActiveAdapted(t=10): Err={activeLast.FinalTrackingError:F4}, PeakImp={activeLast.PeakImpulse:F4}, Work={activeLast.CommandedMechanicalWork:F4}, Completed={activeLast.TaskCompleted}, Score={activeLast.CompositeScore:F4}, Updates={activeCtrl.Updates}");

            Assert.IsTrue(eng.TaskCompleted);
            Assert.IsTrue(frozen.TaskCompleted);
            Assert.IsTrue(activeLast.TaskCompleted);

            // 2. A-B-A Retention across all 5 modes
            TestContext.Out.WriteLine("=== A-B-A RETENTION STUDY ===");
            foreach (RetentionMode mode in Enum.GetValues(typeof(RetentionMode)))
            {
                var rep = RetentionExperiment.RunABA(devConfig, heldOutConfig, 6, mode);
                TestContext.Out.WriteLine($"Mode={mode}: A1Final={rep.PhaseA1FinalScore:F4}, BFinal={rep.PhaseBFinalScore:F4}, A2Init={rep.PhaseA2InitialScore:F4}, A2Final={rep.PhaseA2FinalScore:F4}, SignedInterf={rep.SignedInterference:F4}, Relearn={rep.RelearningSpeed:F4}");
            }

            // 3. 3-Library Behavioral Benchmark (8 factorial + UnifiedMemoryBaseline)
            TestContext.Out.WriteLine("=== 3-LIBRARY BEHAVIORAL BENCHMARK ===");
            var factorial = LibraryFactorialStudy.RunBehavioralBenchmark(24, 4);
            foreach (var f in factorial)
            {
                TestContext.Out.WriteLine($"{f.Name}: Tasks={f.TotalTasks}, Succ={f.SuccessfulOutcomes}, Acc={f.TaskAccuracy:F3}, Ops={f.CognitiveOperations}, Budget={f.BudgetConsumed}, Beliefs={f.BeliefsFormed}, CF={f.CounterfactualsEvaluated}, MemRet={f.MemoryRetrievals}");
            }
        }
    }
}
