using System;
using System.IO;

namespace DreamMachine.Body
{
    public struct JointState
    {
        public string Id;
        public string ParentId;
        public double Angle;
        public double Velocity;
        public double MinAngle;
        public double MaxAngle;
        public double Length;
    }

    public struct EndEffectorPose
    {
        public double X;
        public double Y;
        public double Orientation;
    }

    /// <summary>
    /// Track A: Isolated 3-DOF planar Shoulder-Elbow-Wrist laboratory.
    /// Uses dependable engineered PD control with joint limits, forward kinematics,
    /// contact-aware reaching, mechanical work accounting, and checkpoint persistence.
    /// </summary>
    public sealed class ShoulderElbowWristLab
    {
        public JointState Shoulder;
        public JointState Elbow;
        public JointState Wrist;

        public double TotalMechanicalWork { get; private set; }
        public double PeakContactImpulse { get; private set; }
        public int Tick { get; private set; }

        public ShoulderElbowWristLab()
        {
            Shoulder = new JointState { Id = "shoulder", ParentId = "torso", MinAngle = -1.5, MaxAngle = 1.8, Length = 0.30 };
            Elbow = new JointState { Id = "elbow", ParentId = "shoulder", MinAngle = 0.0, MaxAngle = 2.4, Length = 0.28 };
            Wrist = new JointState { Id = "wrist", ParentId = "elbow", MinAngle = -1.2, MaxAngle = 1.2, Length = 0.12 };
        }

        public EndEffectorPose ComputeForwardKinematics()
        {
            double a1 = Shoulder.Angle;
            double a2 = a1 + Elbow.Angle;
            double a3 = a2 + Wrist.Angle;

            double x = Shoulder.Length * Math.Cos(a1) + Elbow.Length * Math.Cos(a2) + Wrist.Length * Math.Cos(a3);
            double y = Shoulder.Length * Math.Sin(a1) + Elbow.Length * Math.Sin(a2) + Wrist.Length * Math.Sin(a3);
            return new EndEffectorPose { X = x, Y = y, Orientation = a3 };
        }

        /// <summary>
        /// Engineered 2-link + wrist-orientation inverse kinematics.
        /// Explicitly engineered control (not learned movement).
        /// </summary>
        public (double shoulder, double elbow, double wrist, bool reachable) SolveEngineeredIK(
            double targetX, double targetY, double targetOrientation = 0.0, bool elbowUp = false)
        {
            double wx = targetX - Wrist.Length * Math.Cos(targetOrientation);
            double wy = targetY - Wrist.Length * Math.Sin(targetOrientation);
            double r2 = wx * wx + wy * wy;
            double l1 = Shoulder.Length;
            double l2 = Elbow.Length;
            double maxReach = l1 + l2;
            double minReach = Math.Abs(l1 - l2);
            double r = Math.Sqrt(r2);
            bool reachable = r <= maxReach + 1e-6 && r >= minReach - 1e-6;

            if (!reachable && r > 1e-9)
            {
                wx *= maxReach / r;
                wy *= maxReach / r;
                r2 = wx * wx + wy * wy;
            }

            double cosE = DecomposedForceResidual.Clamp((r2 - l1 * l1 - l2 * l2) / (2.0 * l1 * l2), -1.0, 1.0);
            double sinE = Math.Sqrt(Math.Max(0.0, 1.0 - cosE * cosE)) * (elbowUp ? -1.0 : 1.0);
            double elbow = Math.Atan2(sinE, cosE);
            double shoulder = Math.Atan2(wy, wx) - Math.Atan2(l2 * sinE, l1 + l2 * cosE);
            double wrist = targetOrientation - (shoulder + elbow);

            return (
                DecomposedForceResidual.Clamp(shoulder, Shoulder.MinAngle, Shoulder.MaxAngle),
                DecomposedForceResidual.Clamp(elbow, Elbow.MinAngle, Elbow.MaxAngle),
                DecomposedForceResidual.Clamp(wrist, Wrist.MinAngle, Wrist.MaxAngle),
                reachable
            );
        }

        public EndEffectorPose StepCoordinatedReach(
            double targetShoulder,
            double targetElbow,
            double targetWrist,
            double wallX = 0.65,
            double dt = 0.05)
        {
            Tick++;
            StepJoint(ref Shoulder, targetShoulder, 3.2, 0.7, dt);
            StepJoint(ref Elbow, targetElbow, 2.8, 0.6, dt);
            StepJoint(ref Wrist, targetWrist, 2.0, 0.4, dt);

            var pose = ComputeForwardKinematics();
            if (pose.X > wallX)
            {
                double penetration = pose.X - wallX;
                double impulse = penetration * 20.0;
                PeakContactImpulse = Math.Max(PeakContactImpulse, impulse);
                // Contact-aware damping
                Shoulder.Velocity *= 0.5;
                Elbow.Velocity *= 0.5;
                Wrist.Velocity *= 0.5;
            }
            return pose;
        }

        private void StepJoint(ref JointState j, double target, double kp, double kd, double dt)
        {
            double clampedTarget = DecomposedForceResidual.Clamp(target, j.MinAngle, j.MaxAngle);
            double torque = DecomposedForceResidual.Clamp(kp * (clampedTarget - j.Angle) - kd * j.Velocity, -2.0, 2.0);
            j.Velocity += (torque - 0.15 * j.Velocity) * dt;
            j.Angle += j.Velocity * dt;
            if (j.Angle < j.MinAngle) { j.Angle = j.MinAngle; j.Velocity = 0; }
            if (j.Angle > j.MaxAngle) { j.Angle = j.MaxAngle; j.Velocity = 0; }
            TotalMechanicalWork += Math.Abs(torque * j.Velocity * dt);
        }

        public void Write(BinaryWriter w)
        {
            w.Write(1);
            w.Write(Tick);
            w.Write(TotalMechanicalWork);
            w.Write(PeakContactImpulse);
            w.Write(Shoulder.Angle); w.Write(Shoulder.Velocity);
            w.Write(Elbow.Angle); w.Write(Elbow.Velocity);
            w.Write(Wrist.Angle); w.Write(Wrist.Velocity);
        }

        public static ShoulderElbowWristLab Read(BinaryReader r)
        {
            if (r.ReadInt32() != 1) throw new InvalidDataException("Unknown ShoulderElbowWristLab version.");
            var lab = new ShoulderElbowWristLab
            {
                Tick = r.ReadInt32(),
                TotalMechanicalWork = r.ReadDouble(),
                PeakContactImpulse = r.ReadDouble()
            };
            lab.Shoulder.Angle = r.ReadDouble(); lab.Shoulder.Velocity = r.ReadDouble();
            lab.Elbow.Angle = r.ReadDouble(); lab.Elbow.Velocity = r.ReadDouble();
            lab.Wrist.Angle = r.ReadDouble(); lab.Wrist.Velocity = r.ReadDouble();
            return lab;
        }
    }
}
