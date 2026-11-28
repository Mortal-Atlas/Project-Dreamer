using System;
using DreamMachine.Sim;

namespace DreamMachine.Body
{
    public interface IElbowFixture
    {
        string Backend { get; }
        string IndividualId { get; }
        int Tick { get; }
        double SimSeconds { get; }
        bool Finished { get; }
        double RequestedTarget { get; }
        double Angle { get; }
        double X { get; }
        double Y { get; }
        double VX { get; }
        double VY { get; }
        double ObjectAngle { get; }
        double Spin { get; }
        ElbowLab.StepSample Latest { get; }
        ElbowLab.Receptor ReadRegion(int index);
        void RequestTarget(double radians);
        ElbowLab.StepSample Step();
    }

    // Planar reduced-order contact model. Metres, radians, seconds, kilograms and newtons.
    public sealed partial class ElbowLab : IElbowFixture
    {
        public const string JointId = "elbow";
        public const string ObjectId = "contact-puck";
        public const int ReceptorCount = 5;
        public const int Substeps = 10;
        public const double ArmLength = 0.32;
        const double PadRadius = 0.035;
        const double PuckRadius = 0.045;
        const double Inertia = 0.06;
        const double Damping = 0.10;
        const double MaxTorque = 2;
        const double MaxForce = 35;
        const double PatchArea = 0.0004;

        public readonly struct Settings
        {
            public readonly double Lower, Upper, Initial, Mass, Friction, X, Y;
            public readonly int Duration;

            public Settings(double lower, double upper, double initial, double mass,
                double friction, double x, double y, int duration)
            {
                if (!(lower >= -Math.PI && upper <= Math.PI && lower < upper && initial >= lower &&
                    initial <= upper && mass >= 0.05 && mass <= 5 && friction >= 0 && friction <= 1 &&
                    Math.Abs(x) <= 2 && Math.Abs(y) <= 2 && duration > 0 && duration <= 72000))
                    throw new ArgumentOutOfRangeException(nameof(initial), "Invalid elbow trial configuration.");
                Lower = lower; Upper = upper; Initial = initial; Mass = mass; Friction = friction;
                X = x; Y = y; Duration = duration;
            }

            public static Settings Standard => new Settings(-0.7, 1.3, 0, 0.25, 0.4, 0.28, 0.20, 150);
        }

        public readonly struct Receptor
        {
            public readonly double Force, PressureEstimate, ContactSeconds, SlipSpeed, TangentialForce;

            public Receptor(double force, double seconds, double slip, double tangential)
            {
                Force = force; PressureEstimate = force / PatchArea; ContactSeconds = seconds;
                SlipSpeed = slip; TangentialForce = tangential;
            }
        }

        public readonly struct Proprioception
        {
            public readonly double Angle, Velocity, Effort, Resistance;

            public Proprioception(double angle, double velocity, double effort, double resistance)
            {
                Angle = angle; Velocity = velocity; Effort = effort; Resistance = resistance;
            }
        }

        public readonly struct StepSample
        {
            public readonly int Tick;
            public readonly double Target, PreviousAngle, NormalImpulse, ObjectDeltaX, ObjectDeltaY;
            public readonly Proprioception Feedback;

            public StepSample(int tick, double target, double previous, double impulse, double dx, double dy,
                Proprioception feedback)
            {
                Tick = tick; Target = target; PreviousAngle = previous; NormalImpulse = impulse;
                ObjectDeltaX = dx; ObjectDeltaY = dy; Feedback = feedback;
            }
        }

        readonly Receptor[] _skin = new Receptor[ReceptorCount];
        readonly double[] _normalImpulse = new double[ReceptorCount];
        readonly double[] _tangentImpulse = new double[ReceptorCount];
        readonly double[] _slipIntegral = new double[ReceptorCount];
        readonly double[] _duration = new double[ReceptorCount];
        readonly double[] _calibration = new double[ReceptorCount] { 1, 1, 1, 1, 1 };
        public Settings Configuration { get; }
        public string IndividualId { get; }
        public string Backend => "planar-reference-v1";
        public int Tick { get; private set; }
        public double Angle { get; private set; }
        public double Velocity { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double VX { get; private set; }
        public double VY { get; private set; }
        public double ObjectAngle { get; private set; }
        public double Spin { get; private set; }
        public StepSample Latest { get; private set; }
        public double RequestedTarget { get; private set; }
        public double SimSeconds => Tick * (double)WorldSimulation.Dt;
        public bool Finished => Tick >= Configuration.Duration;

        public ElbowLab(Settings settings, string individualId)
        {
            if (settings.Duration == 0) throw new ArgumentException("Supply validated trial settings.", nameof(settings));
            if (string.IsNullOrWhiteSpace(individualId) || individualId.Length > 128)
                throw new ArgumentException("Supply a stable individual ID.", nameof(individualId));
            Configuration = settings; IndividualId = individualId;
            Angle = settings.Initial; X = settings.X; Y = settings.Y;
            RequestedTarget = Clamp(0.9, settings.Lower, settings.Upper);
            Latest = new StepSample(0, Angle, Angle, 0, 0, 0, new Proprioception(Angle, 0, 0, 0));
        }

        public static string ReceptorId(int index) => "palm/" + index;
        public static double RegionCoordinate(int index) => index / 2d - 1;
        public static bool AreNeighbors(int a, int b) =>
            a >= 0 && a < ReceptorCount && b >= 0 && b < ReceptorCount && Math.Abs(a - b) == 1;
        public Receptor ReadRegion(int index) => _skin[index];
        public static double ContactRegion(double localTangent) => 2 + 2 * Clamp(localTangent, -1, 1);
        public static double RegionWeight(double center, int index) => Math.Max(0, 1.5 - Math.Abs(center - index));

        public void RequestTarget(double target)
        {
            if (double.IsNaN(target) || double.IsInfinity(target)) throw new ArgumentOutOfRangeException(nameof(target));
            RequestedTarget = Clamp(target, Configuration.Lower, Configuration.Upper);
        }

        public StepSample Step(double target)
        {
            RequestTarget(target);
            return Step();
        }

        public StepSample Step()
        {
            if (Finished) throw new InvalidOperationException("Trial duration has elapsed.");
            double target = RequestedTarget;
            double previous = Angle, startX = X, startY = Y;
            double effort = 0, reaction = 0, totalImpulse = 0;
            double dt = WorldSimulation.Dt / (double)Substeps;
            Array.Clear(_normalImpulse, 0, ReceptorCount);
            Array.Clear(_tangentImpulse, 0, ReceptorCount);
            Array.Clear(_slipIntegral, 0, ReceptorCount);
            for (int i = 0; i < Substeps; i++)
            {
                double motor = Clamp(9 * (target - Angle) - 0.7 * Velocity, -MaxTorque, MaxTorque);
                double contactTorque = Contact(dt, out double impulse);
                Velocity = Clamp(Velocity + (motor + contactTorque - Damping * Velocity) * dt / Inertia, -12, 12);
                Angle = Clamp(Angle + Velocity * dt, Configuration.Lower, Configuration.Upper);
                if (Angle == Configuration.Lower || Angle == Configuration.Upper) Velocity = 0;
                effort += motor;
                reaction += contactTorque;
                totalImpulse += impulse;
            }
            for (int region = 0; region < ReceptorCount; region++)
                _skin[region] = new Receptor(_normalImpulse[region] * _calibration[region] / WorldSimulation.Dt,
                    _duration[region], _slipIntegral[region] / WorldSimulation.Dt, _tangentImpulse[region] / WorldSimulation.Dt);
            Latest = new StepSample(++Tick, target, previous, totalImpulse, X - startX, Y - startY,
                new Proprioception(Angle, Velocity, effort / Substeps, reaction / Substeps));
            return Latest;
        }

        double Contact(double dt, out double impulse)
        {
            double armX = Math.Cos(Angle), armY = Math.Sin(Angle);
            double hx = ArmLength * armX, hy = ArmLength * armY;
            double dx = X - hx, dy = Y - hy;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double nx = distance > 1e-9 ? dx / distance : armX;
            double ny = distance > 1e-9 ? dy / distance : armY;
            double relativeX = VX + Velocity * hy, relativeY = VY - Velocity * hx;
            double normal = distance < PadRadius + PuckRadius ? Clamp(
                650 * (PadRadius + PuckRadius - distance) - 5 * (relativeX * nx + relativeY * ny), 0, MaxForce) : 0;
            double slip = -ny * relativeX + nx * relativeY - Spin * PuckRadius;
            double tangent = Clamp(-4 * slip, -Configuration.Friction * normal, Configuration.Friction * normal);
            double fx = normal * nx - tangent * ny, fy = normal * ny + tangent * nx;
            double decay = 1 / (1 + 2 * dt);
            VX = (VX + fx * dt / Configuration.Mass) * decay;
            VY = (VY + fy * dt / Configuration.Mass) * decay;
            Spin = (Spin - 2 * tangent * dt / (Configuration.Mass * PuckRadius)) * decay;
            X += VX * dt; Y += VY * dt; ObjectAngle += Spin * dt;
            impulse = normal * dt;
            if (normal == 0)
            {
                Array.Clear(_duration, 0, ReceptorCount);
                return 0;
            }
            double center = ContactRegion(-armY * nx + armX * ny);
            double total = 0;
            for (int i = 0; i < ReceptorCount; i++) total += RegionWeight(center, i);
            for (int i = 0; i < ReceptorCount; i++)
            {
                double weight = RegionWeight(center, i) / total;
                if (weight > 0)
                {
                    _normalImpulse[i] += impulse * weight;
                    _tangentImpulse[i] += tangent * dt * weight;
                    _slipIntegral[i] += Math.Abs(slip) * dt * weight;
                    _duration[i] += dt;
                }
                else _duration[i] = 0;
            }
            return hy * fx - hx * fy;
        }

        static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    }
}
