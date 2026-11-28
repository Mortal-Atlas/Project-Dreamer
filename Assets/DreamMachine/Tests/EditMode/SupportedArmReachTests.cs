using System.Numerics;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class SupportedArmReachTests
    {
        [Test]
        public void SameGoalSupportsTwoBendPlanesAtFixedLinkLengths()
        {
            var wrist = new Vector3(.17f, -.09f, .25f);
            var a = SupportedArmReach.Propose(Vector3.Zero, wrist, Vector3.UnitX, .31f, .24f);
            var b = SupportedArmReach.Propose(Vector3.Zero, wrist, Vector3.UnitY, .31f, .24f);
            Assert.That(a.WithinEnvelope && b.WithinEnvelope, Is.True);
            Assert.That(Vector3.Distance(a.Elbow, b.Elbow), Is.GreaterThan(.08f));
            Assert.That(Vector3.Distance(a.Wrist, b.Wrist), Is.LessThan(1e-5f));
            Assert.That(a.Elbow.Length(), Is.EqualTo(.31f).Within(1e-5f));
            Assert.That(Vector3.Distance(a.Elbow, a.Wrist), Is.EqualTo(.24f).Within(1e-5f));
        }

        [Test]
        public void UnreachableAndSingularTargetsStayBoundedAndFinite()
        {
            var distant = SupportedArmReach.Propose(Vector3.Zero, new Vector3(0, 0, 2f), Vector3.UnitZ, .29f, .23f);
            var collapsed = SupportedArmReach.Propose(Vector3.Zero, Vector3.Zero, Vector3.Zero, .29f, .23f);
            Assert.That(distant.WithinEnvelope || collapsed.WithinEnvelope, Is.False);
            Assert.That(distant.Wrist.Length(), Is.LessThan(.52f));
            Assert.That(collapsed.Wrist.Length(), Is.GreaterThan(.06f));
            Assert.That(float.IsFinite(collapsed.Elbow.Length()), Is.True);
            Assert.That(Vector3.Distance(distant.Elbow, distant.Wrist), Is.EqualTo(.23f).Within(1e-5f));
        }
    }
}
