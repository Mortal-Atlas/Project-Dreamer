using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using DreamMachine.Body;

namespace DreamMachine.Cli
{
    static class ElbowLabCommand
    {
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);
            using var csv = File.CreateText(Path.Combine(directory, "elbow-trace.csv"));
            csv.Write("trial,individual,joint,object,tick,sim_s,target_rad,previous_rad,angle_rad,velocity_rad_s,effort_nm,resistance_nm,normal_ns,puck_x_m,puck_y_m,puck_vx_m_s,puck_vy_m_s,puck_angle_rad,puck_spin_rad_s");
            for (int i = 0; i < ElbowLab.ReceptorCount; i++)
                csv.Write($",{ElbowLab.ReceptorId(i)}_n,{ElbowLab.ReceptorId(i)}_estimated_pa,{ElbowLab.ReceptorId(i)}_contact_s,{ElbowLab.ReceptorId(i)}_slip_m_s,{ElbowLab.ReceptorId(i)}_tangent_n");
            csv.WriteLine();

            Trace("free", 1, 1);
            Trace("standard", 0.28, 0.20);
            Trace("opposite", 0.28, -0.20, target: -0.9);
            Trace("near", 0.30, 0.12);
            Trace("light", 0.28, 0.20, mass: 0.1);
            Trace("heavy", 0.28, 0.20, mass: 0.9);
            Trace("smooth", 0.28, 0.20, friction: 0);
            Trace("rough", 0.28, 0.20, friction: 0.8);
            Profile(directory);
            return 0;

            void Trace(string name, double x, double y, double mass = 0.25, double friction = 0.4, double target = 0.9)
            {
                var lab = new ElbowLab(new ElbowLab.Settings(-1.1, 1.3, 0, mass, friction, x, y, 80), "son");
                int first = 0;
                double impulse = 0;
                lab.RequestTarget(target);
                while (!lab.Finished)
                {
                    var step = lab.Step();
                    if (step.NormalImpulse > 0 && first == 0) first = step.Tick;
                    impulse += step.NormalImpulse;
                    csv.Write($"{name},{lab.IndividualId},{ElbowLab.JointId},{ElbowLab.ObjectId},{step.Tick}");
                    Write(lab.SimSeconds); Write(step.Target); Write(step.PreviousAngle);
                    Write(step.Feedback.Angle); Write(step.Feedback.Velocity); Write(step.Feedback.Effort);
                    Write(step.Feedback.Resistance); Write(step.NormalImpulse); Write(lab.X); Write(lab.Y);
                    Write(lab.VX); Write(lab.VY); Write(lab.ObjectAngle); Write(lab.Spin);
                    for (int i = 0; i < ElbowLab.ReceptorCount; i++)
                    {
                        var p = lab.ReadRegion(i);
                        Write(p.Force); Write(p.PressureEstimate); Write(p.ContactSeconds);
                        Write(p.SlipSpeed); Write(p.TangentialForce);
                    }
                    csv.WriteLine();
                }
                Console.WriteLine(FormattableString.Invariant($"{name}: first={first}, impulse={impulse:F4} N s, puck=({lab.X:F4},{lab.Y:F4}) m"));
            }

            void Write(double value) => csv.Write("," + value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void Profile(string directory)
        {
            var settings = ElbowLab.Settings.Standard;
            var lab = new ElbowLab(settings, "son");
            for (int i = 0; i < 5; i++) lab.Step();
            byte[] checkpoint = lab.CaptureCheckpoint();
            string path = Path.Combine(directory, "elbow-profile.checkpoint");
            lab.SaveCheckpoint(path);
            Measure("fixture tick (10 substeps and sensors)", 15000, i =>
            {
                if (i % settings.Duration == 0 || lab.Finished) lab = new ElbowLab(settings, "son");
                lab.Step();
            });
            Measure("checkpoint capture", 500, _ => lab.CaptureCheckpoint());
            Measure("checkpoint restore", 500, _ => ElbowLab.RestoreCheckpoint(checkpoint));
            Measure("atomic publication", 15, _ => lab.SaveCheckpoint(path));
            Console.WriteLine("Checkpoint bytes: " + checkpoint.Length);
        }

        static void Measure(string name, int count, Action<int> action)
        {
            action(0);
            var watch = new Stopwatch();
            long before = GC.GetAllocatedBytesForCurrentThread();
            watch.Start();
            for (int i = 0; i < count; i++) action(i);
            watch.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine(FormattableString.Invariant($"{name}: {watch.Elapsed.TotalMilliseconds / count:F4} ms/op, {allocated / (double)count:F1} B/op, n={count}"));
        }
    }
}
