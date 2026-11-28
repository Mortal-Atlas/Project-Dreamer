using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class DynamicSupportTests
    {
        static readonly LoadedFootRegion Sole = new LoadedFootRegion(-.04f, .04f, -.07f, .07f);

        [Test]
        public void XComUsesSignedVelocityAndMeasuredHeight()
        {
            Assert.That(DynamicSupport.Extrapolate(.1f, -.2f, 10f, .4f), Is.EqualTo(.06f).Within(.000001f));
            Assert.That(DynamicSupport.Extrapolate(.1f, .2f, 10f, .4f), Is.EqualTo(.14f).Within(.000001f));
            Assert.That(float.IsNaN(DynamicSupport.Extrapolate(0, 0, 10, 0)), Is.True);
        }

        [Test]
        public void MarginsAreSignedAlongBothWorldAxes()
        {
            Assert.That(Sole.FrontalMargin(.03f), Is.EqualTo(.01f).Within(.000001f));
            Assert.That(Sole.FrontalMargin(-.05f), Is.EqualTo(-.01f).Within(.000001f));
            Assert.That(Sole.SagittalMargin(.08f), Is.EqualTo(-.01f).Within(.000001f));
            Assert.That(float.IsNaN(default(LoadedFootRegion).SagittalMargin(0)), Is.True);
        }

        [Test]
        public void MissingContactAndHighShareWithLowImpulseBlockPrelift()
        {
            Assert.That(Gate(Sole, 4.35f, .55f, true), Is.EqualTo(PreliftFailure.None));
            Assert.That(Gate(Sole, .09f, .01f, true).HasFlag(PreliftFailure.StanceImpulse), Is.True);
            Assert.That(Gate(Sole, 4.35f, .55f, false).HasFlag(PreliftFailure.Pressure), Is.True);
            var missing = Gate(default, 4.35f, .55f, false);
            Assert.That(missing.HasFlag(PreliftFailure.Region | PreliftFailure.Margin), Is.True);
            Assert.That(DynamicSupport.HoldsStance(true, .09f, 4.905f, 0f), Is.False);
            Assert.That(DynamicSupport.HoldsStance(false, 4.905f, 4.905f, 0f), Is.False);
        }

        [Test]
        public void QuinticRampIsBoundedMonotonicAndStartsGently()
        {
            Assert.That(DynamicSupport.Smooth(-1), Is.Zero);
            Assert.That(DynamicSupport.Smooth(2), Is.EqualTo(1));
            Assert.That(DynamicSupport.Smooth(.5f), Is.EqualTo(.5f));
            Assert.That(DynamicSupport.Smooth(.05f), Is.LessThan(.002f));
            float prior = 0;
            for (int i = 1; i <= 100; i++)
            {
                float next = DynamicSupport.Smooth(i / 100f);
                Assert.That(next, Is.InRange(prior, 1.000001f));
                prior = next;
            }
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void OutwardWarningRequiresReachableEdgeContactAndDirectionalVelocity(int direction)
        {
            Assert.That(DynamicSupport.WarnOutward(Sole, true, direction * .029f, direction * .105f, direction), Is.True);
            Assert.That(DynamicSupport.WarnOutward(Sole, true, direction * .029f, -direction * .105f, direction), Is.False);
            Assert.That(DynamicSupport.WarnOutward(Sole, false, direction * .029f, direction * .105f, direction), Is.False);
            Assert.That(DynamicSupport.WarnOutward(Sole, true, direction * .090f, direction * .105f, direction), Is.False);
            Assert.That(DynamicSupport.WarnOutward(Sole, true, 0f, direction * .105f, direction), Is.False);
        }

        [Test]
        public void ShortTouchdownWarningRequiresDescendingAirborneFoot()
        {
            Assert.That(DynamicSupport.WarnPrematureTouchdown(.011f, .008f), Is.True);
            Assert.That(DynamicSupport.WarnPrematureTouchdown(.006f, .008f), Is.False);
            Assert.That(DynamicSupport.WarnPrematureTouchdown(.013f, 0f), Is.False);
            Assert.That(DynamicSupport.RecoveryForward(.15f), Is.EqualTo(.065f));
            Assert.That(DynamicSupport.RecoveryForward(-.15f), Is.EqualTo(-.045f));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void PreliftBrakeDetectsLoadedOutwardAccelerationBeforeTheHardWarning(int side)
        {
            var region = new LoadedFootRegion(-.065f, .065f, -.09f, .09f);
            Assert.That(DynamicSupport.PreliftDeteriorating(region, 1f, .02f * side, .06f * side, .05f * side, side), Is.True);
            Assert.That(DynamicSupport.WarnOutward(region, true, .02f * side, .06f * side, side), Is.False);
            Assert.That(DynamicSupport.PreliftDeteriorating(region, 1f, .02f * side, .06f * side, .06f * side, side), Is.False);
            Assert.That(DynamicSupport.PreliftDeteriorating(region, .6f, .02f * side, .06f * side, .05f * side, side), Is.False);
        }

        static PreliftFailure Gate(LoadedFootRegion region, float stance, float swing, bool pressure) =>
            DynamicSupport.Evaluate(region, true, 1f, stance, swing, 4.905f, pressure, 0f, 0f, 0f, 0f, .02f, 1f, .6f, .001f);
    }
}
