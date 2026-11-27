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

        static PreliftFailure Gate(LoadedFootRegion region, float stance, float swing, bool pressure) =>
            DynamicSupport.Evaluate(region, true, 1f, stance, swing, 4.905f, pressure, 0f, 0f, 0f, 0f, .02f, 1f, .6f, .001f);
    }
}
