using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public sealed class AnatomyContactTests
    {
        [TestCase(BodySegment.RightHand, BodySegment.RightIndex, true)]
        [TestCase(BodySegment.Torso, BodySegment.LeftUpperArm, true)]
        [TestCase(BodySegment.Pelvis, BodySegment.LeftThigh, true)]
        [TestCase(BodySegment.Head, BodySegment.Torso, false)]
        [TestCase(BodySegment.RightForearm, BodySegment.Torso, false)]
        [TestCase(BodySegment.RightHand, BodySegment.Torso, false)]
        [TestCase(BodySegment.LeftForearm, BodySegment.RightForearm, false)]
        [TestCase(BodySegment.LeftFoot, BodySegment.RightFoot, false)]
        [TestCase(BodySegment.RightThumb, BodySegment.RightIndex, false)]
        [TestCase(BodySegment.RightHand, BodySegment.Object, false)]
        [TestCase(BodySegment.LeftFoot, BodySegment.Environment, false)]
        public void ContactRegistryIsSymmetric(BodySegment a, BodySegment b, bool excluded)
        {
            Assert.That(AnatomyContacts.ExcludePair(a, b), Is.EqualTo(excluded));
            Assert.That(AnatomyContacts.ExcludePair(b, a), Is.EqualTo(excluded));
        }
    }
}
