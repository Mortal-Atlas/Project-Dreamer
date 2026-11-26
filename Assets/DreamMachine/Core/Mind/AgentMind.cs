using System.Collections.Generic;
using DreamMachine.Libraries;
using DreamMachine.World;

namespace DreamMachine.Mind
{
    public enum MindCondition
    {
        LibraryOneOnly = 0,
        LibraryOneAndTwo = 1,
        Full = 2
    }

    public sealed class TrialDecision
    {
        public bool PredictedHarm;
        public bool ActualHarm;
        public bool Correct;
        public string Hypothesis;
        public float Confidence;
        public DecisionCoordinate Coordinate;
        public int BeliefCount;
        public int AppendixCount;
        public string ObjectId;
        public int Phase;
    }

    /// <summary>
    /// One mind. Condition A trusts Library I. Condition B can formulate
    /// ideas (Library II) but does not keep lived amendments. Condition C
    /// tests ideas against experience and amends Library III without erasing it.
    /// </summary>
    public sealed class AgentMind
    {
        public MindCondition Condition = MindCondition.Full;
        public ILibraryOne LibraryOne;
        public LibraryTwo LibraryTwo = new LibraryTwo();
        public LibraryThree Library = new LibraryThree();
        public PossibilityAtlas Atlas;
        public int BirthSeed;

        public AgentMind(int birthSeed, MindCondition condition)
        {
            BirthSeed = birthSeed;
            Condition = condition;
            LibraryOne = new StatisticalLibraryOne();
            Atlas = PossibilityAtlas.FromBirth(birthSeed);
        }

        public TrialDecision Encounter(ObjectInstance obj, int trial, HiddenRuleWorld world)
        {
            float prior = LibraryOne.PriorHarm(obj);
            bool actual = world.IsHarmful(obj, trial);
            string hypothesis = "prior:general";
            float confidence = Abs(prior - 0.5f) * 2f;
            bool predicted;

            if (Condition == MindCondition.LibraryOneOnly)
            {
                predicted = prior >= 0.5f;
                hypothesis = "prior:general";
            }
            else if (Condition == MindCondition.LibraryOneAndTwo)
            {
                predicted = ChooseByPrior(obj, prior, out hypothesis, out confidence);
            }
            else
            {
                predicted = ChooseByExperience(obj, prior, out hypothesis, out confidence);
            }

            if (Condition == MindCondition.Full)
            {
                string[] keys = Predicates.For(obj);
                string evidence = obj.Id + " was " + (actual ? "harmful" : "safe") + " (" + obj.CategoryLabel + ")";
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!Atlas.Allows(keys[i]) && !keys[i].StartsWith("object:")) continue;
                    Library.Observe(keys[i], actual, trial, evidence, "lived-experience");
                }
            }

            float u = confidence;
            return new TrialDecision
            {
                PredictedHarm = predicted,
                ActualHarm = actual,
                Correct = predicted == actual,
                Hypothesis = hypothesis,
                Confidence = confidence,
                Coordinate = Atlas.Coordinate(hypothesis, u),
                BeliefCount = Condition == MindCondition.Full ? Library.Count : 0,
                AppendixCount = Condition == MindCondition.Full ? Library.AppendixCount : 0,
                ObjectId = obj.Id,
                Phase = world.Phase(trial)
            };
        }

        bool ChooseByPrior(ObjectInstance obj, float prior, out string hypothesis, out float confidence)
        {
            List<Hypothesis> ideas = LibraryTwo.Generate(obj, Atlas, BirthSeed);
            string best = "prior:general";
            float bestScore = prior >= 0.5f ? prior : 1f - prior;
            bool bestHarm = prior >= 0.5f;
            for (int i = 0; i < ideas.Count; i++)
            {
                Hypothesis idea = ideas[i];
                if (!idea.Thinkable || idea.Predicate == "prior:general") continue;
                float feature = StatisticalLibraryOne.FeaturePrior(idea.Predicate);
                bool harm = feature >= 0.5f;
                float score = (harm ? prior : 1f - prior) * (0.5f + 0.5f * idea.GenerativeWeight);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = idea.Predicate;
                    bestHarm = harm;
                }
            }
            hypothesis = best;
            confidence = Abs(bestScore - 0.5f);
            if (confidence > 1f) confidence = 1f;
            return bestHarm;
        }

        bool ChooseByExperience(ObjectInstance obj, float prior, out string hypothesis, out float confidence)
        {
            string instanceKey = "object:" + obj.Id;
            if (Library.Seen(instanceKey) >= 4f)
            {
                float rate = Library.Rate(instanceKey);
                hypothesis = instanceKey;
                confidence = Abs(rate - 0.5f) * 2f;
                return rate >= 0.5f;
            }

            string[] keys = Predicates.For(obj);
            float num = 0f;
            float den = 0f;
            string strongest = "prior:general";
            float strongestW = -1f;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].StartsWith("object:")) continue;
                if (!Atlas.Allows(keys[i])) continue;
                float n = Library.Seen(keys[i]);
                if (n < 2f) continue;
                float w = keys[i].IndexOf('+') >= 0 ? n * 1.4f : n;
                num += Library.Rate(keys[i]) * w;
                den += w;
                if (w > strongestW)
                {
                    strongestW = w;
                    strongest = keys[i];
                }
            }
            if (den < 1f)
            {
                hypothesis = "prior:general";
                confidence = Abs(prior - 0.5f) * 2f;
                return prior >= 0.5f;
            }
            float empirical = num / den;
            float trust = den / 40f;
            if (trust > 1f) trust = 1f;
            float blended = prior * (1f - trust) + empirical * trust;
            hypothesis = strongest;
            confidence = trust * Abs(blended - 0.5f) * 2f;
            return blended >= 0.5f;
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }
    }
}
