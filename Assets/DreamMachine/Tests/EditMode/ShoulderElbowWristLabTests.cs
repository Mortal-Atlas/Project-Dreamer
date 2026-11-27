using System.IO;
using DreamMachine.Body;
using NUnit.Framework;

namespace DreamMachine.Tests
{
    public class ShoulderElbowWristLabTests
    {
        [Test]
        public void CoordinatedReach_ReachesTargetWithinJointLimits_AndPersists()
        {
            var lab = new ShoulderElbowWristLab();
            EndEffectorPose pose = default;
            for (int i = 0; i < 60; i++)
            {
                pose = lab.StepCoordinatedReach(0.6, 0.8, 0.2, wallX: 0.65);
            }

            Assert.Greater(lab.TotalMechanicalWork, 0.0);
            Assert.GreaterOrEqual(lab.Shoulder.Angle, lab.Shoulder.MinAngle);
            Assert.LessOrEqual(lab.Shoulder.Angle, lab.Shoulder.MaxAngle);
            Assert.GreaterOrEqual(lab.Elbow.Angle, lab.Elbow.MinAngle);
            Assert.LessOrEqual(lab.Elbow.Angle, lab.Elbow.MaxAngle);
            Assert.GreaterOrEqual(lab.Wrist.Angle, lab.Wrist.MinAngle);
            Assert.LessOrEqual(lab.Wrist.Angle, lab.Wrist.MaxAngle);

            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                lab.Write(bw);
            ms.Position = 0;
            using var br = new BinaryReader(ms);
            var restored = ShoulderElbowWristLab.Read(br);

            Assert.AreEqual(lab.Tick, restored.Tick);
            Assert.AreEqual(lab.Shoulder.Angle, restored.Shoulder.Angle, 1e-9);
            Assert.AreEqual(lab.TotalMechanicalWork, restored.TotalMechanicalWork, 1e-9);
        }
    }
}
