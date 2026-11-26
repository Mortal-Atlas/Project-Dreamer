using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public sealed partial class NativeElbowLab
    {
        [Serializable]
        sealed class Snapshot
        {
            public int Version, Tick, Duration;
            public string Backend, Individual, Object;
            public double Lower, Upper, Initial, Mass, Friction, X, Y, Target;
            public Vector3 ArmPosition, PuckPosition, ArmVelocity, PuckVelocity, ArmSpin, PuckSpin;
            public Quaternion ArmRotation, PuckRotation;
            public Vector3 ContactPoint, ContactNormal;
            public double[] Sensors;
        }

        public byte[] CaptureCheckpoint()
        {
            var data = new Snapshot
            {
                Version = 1, Backend = Backend, Individual = IndividualId, Object = ElbowLab.ObjectId, Tick = Tick,
                Duration = Configuration.Duration, Lower = Configuration.Lower, Upper = Configuration.Upper,
                Initial = Configuration.Initial, Mass = Configuration.Mass, Friction = Configuration.Friction,
                X = Configuration.X, Y = Configuration.Y, Target = RequestedTarget,
                ArmPosition = _arm.position, ArmRotation = _arm.rotation,
                ArmVelocity = _arm.linearVelocity, ArmSpin = _arm.angularVelocity,
                PuckPosition = _puck.position, PuckRotation = _puck.rotation,
                PuckVelocity = _puck.linearVelocity, PuckSpin = _puck.angularVelocity,
                ContactPoint = LastContactPoint, ContactNormal = LastContactNormal,
                Sensors = new double[7 + ElbowLab.ReceptorCount * 4]
            };
            data.Sensors[0] = Latest.Target;
            data.Sensors[1] = Latest.PreviousAngle;
            data.Sensors[2] = Latest.NormalImpulse;
            data.Sensors[3] = Latest.ObjectDeltaX;
            data.Sensors[4] = Latest.ObjectDeltaY;
            data.Sensors[5] = Latest.Feedback.Effort;
            data.Sensors[6] = Latest.Feedback.Resistance;
            for (int i = 0; i < _skin.Length; i++)
            {
                int n = 7 + i * 4;
                data.Sensors[n] = _skin[i].Force;
                data.Sensors[n + 1] = _skin[i].ContactSeconds;
                data.Sensors[n + 2] = _skin[i].SlipSpeed;
                data.Sensors[n + 3] = _skin[i].TangentialForce;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            using var digest = SHA256.Create();
            return digest.ComputeHash(bytes).Concat(bytes).ToArray();
        }

        public static NativeElbowLab RestoreCheckpoint(byte[] bytes)
        {
            Snapshot data = Decode(bytes);
            var lab = new NativeElbowLab(SettingsOf(data), data.Individual) { Tick = data.Tick };
            lab.RequestTarget(data.Target);
            RestoreBody(lab._arm, data.ArmPosition, data.ArmRotation, data.ArmVelocity, data.ArmSpin);
            RestoreBody(lab._puck, data.PuckPosition, data.PuckRotation, data.PuckVelocity, data.PuckSpin);
            lab.LastContactPoint = data.ContactPoint;
            lab.LastContactNormal = data.ContactNormal;
            double[] f = data.Sensors;
            lab.Latest = new ElbowLab.StepSample(lab.Tick, f[0], f[1], f[2], f[3], f[4],
                new ElbowLab.Proprioception(lab.Angle, data.ArmSpin.z, f[5], f[6]));
            for (int i = 0; i < lab._skin.Length; i++)
            {
                int n = 7 + i * 4;
                lab._skin[i] = new ElbowLab.Receptor(f[n], f[n + 1], f[n + 2], f[n + 3]);
                lab._duration[i] = f[n + 1];
            }
            return lab;
        }

        static void RestoreBody(Rigidbody body, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 spin)
        {
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = spin;
        }

        static ElbowLab.Settings SettingsOf(Snapshot s) =>
            new ElbowLab.Settings(s.Lower, s.Upper, s.Initial, s.Mass, s.Friction, s.X, s.Y, s.Duration);

        static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        static bool Valid(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        static bool Valid(Quaternion q) => Finite(q.w) &&
            Valid(new Vector3(q.x, q.y, q.z)) && Mathf.Abs(Quaternion.Dot(q, q) - 1) < 0.0001f;

        static Snapshot Decode(byte[] bytes)
        {
            try
            {
                if (bytes.Length <= 32 || bytes.Length > 16384)
                    throw new InvalidDataException("Invalid native checkpoint size.");
                using var digest = SHA256.Create();
                if (!digest.ComputeHash(bytes, 32, bytes.Length - 32).SequenceEqual(bytes.Take(32)))
                    throw new InvalidDataException("Native checkpoint checksum failed.");
                var s = JsonUtility.FromJson<Snapshot>(Encoding.UTF8.GetString(bytes, 32, bytes.Length - 32));
                if (s == null || s.Version != 1 || s.Backend != "unity-physx-v1" || s.Object != ElbowLab.ObjectId ||
                    string.IsNullOrWhiteSpace(s.Individual) || s.Individual.Length > 128 ||
                    s.Tick < 0 || s.Tick > s.Duration || !Finite(s.Target) || s.Target < s.Lower || s.Target > s.Upper ||
                    !Valid(s.ArmPosition) || !Valid(s.PuckPosition) || !Valid(s.ArmVelocity) || !Valid(s.PuckVelocity) ||
                    !Valid(s.ArmSpin) || !Valid(s.PuckSpin) || !Valid(s.ArmRotation) || !Valid(s.PuckRotation) ||
                    !Valid(s.ContactPoint) || !Valid(s.ContactNormal) || s.Sensors == null ||
                    s.Sensors.Length != 7 + ElbowLab.ReceptorCount * 4 || !s.Sensors.All(Finite))
                    throw new InvalidDataException("Invalid native checkpoint state.");
                SettingsOf(s);
                return s;
            }
            catch (ArgumentException error)
            {
                throw new InvalidDataException("Invalid native checkpoint.", error);
            }
        }

        static byte[] ReadCheckpointFile(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length > 16384) throw new InvalidDataException("Native checkpoint is oversized.");
                using var reader = new BinaryReader(stream);
                byte[] data = reader.ReadBytes((int)stream.Length);
                Decode(data);
                return data;
            }
            catch (Exception error) when (error is FileNotFoundException || error is DirectoryNotFoundException)
            {
                if (File.Exists(path + ".previous"))
                    throw new InvalidDataException("Native primary checkpoint is missing and a previous copy exists.");
                return null;
            }
        }

        public static NativeElbowLab LoadCheckpoint(string path)
        {
            byte[] data = ReadCheckpointFile(path);
            return data == null ? null : RestoreCheckpoint(data);
        }

        public void SaveCheckpoint(string path) =>
            WorldSimulation.PublishCheckpoint(path, CaptureCheckpoint(), file => ReadCheckpointFile(file) != null);
    }
}
