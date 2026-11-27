using DreamMachine.Libraries;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class LibraryFactorialTests
    {
        [Test]
        public void BehavioralBenchmark_RunsAllEightConditionsAndUnifiedBaseline()
        {
            var results = LibraryFactorialStudy.RunBehavioralBenchmark(24, 4);
            Assert.AreEqual(9, results.Count);
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(i, results[i].Condition.ConditionIndex);
                Assert.AreEqual(24, results[i].TotalTasks);
            }
            Assert.AreEqual("UnifiedMemoryBaseline", results[8].Name);
            Assert.Greater(results[8].CognitiveOperations, 0);
        }
    }
}
