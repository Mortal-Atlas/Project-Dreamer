using DreamMachine.Libraries;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class LibraryFactorialTests
    {
        [Test]
        public void FactorialStudy_GeneratesAllEightConditions_WithMatchedBudget()
        {
            var results = LibraryFactorialStudy.RunMatchedBudgetStudy(100);
            Assert.AreEqual(8, results.Count);
            for (int i = 0; i < results.Count; i++)
            {
                Assert.AreEqual(i, results[i].Condition.ConditionIndex);
                Assert.AreEqual(100, results[i].InferenceStepsUsed);
            }
            Assert.Greater(results[7].TaskAccuracy, results[0].TaskAccuracy);
        }
    }
}
