using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DreamMachine.Sim;

namespace DreamMachine.Body
{
    public sealed partial class ElbowLab
    {
        public byte[] CaptureCheckpoint()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write("DreamMachine.Elbow");
            writer.Write(1);
            writer.Write(IndividualId);
            writer.Write(ObjectId);
            writer.Write(Configuration.Lower); writer.Write(Configuration.Upper); writer.Write(Configuration.Initial);
            writer.Write(Configuration.Mass); writer.Write(Configuration.Friction);
            writer.Write(Configuration.X); writer.Write(Configuration.Y); writer.Write(Configuration.Duration);
            writer.Write(Tick);
            writer.Write(RequestedTarget);
            writer.Write(Angle); writer.Write(Velocity);
            writer.Write(X); writer.Write(Y); writer.Write(VX); writer.Write(VY);
            writer.Write(ObjectAngle); writer.Write(Spin);
            writer.Write(Latest.Target); writer.Write(Latest.PreviousAngle); writer.Write(Latest.NormalImpulse);
            writer.Write(Latest.ObjectDeltaX); writer.Write(Latest.ObjectDeltaY);
            writer.Write(Latest.Feedback.Effort); writer.Write(Latest.Feedback.Resistance);
            for (int i = 0; i < ReceptorCount; i++)
            {
                writer.Write(_calibration[i]);
                writer.Write(_skin[i].Force); writer.Write(_skin[i].ContactSeconds);
                writer.Write(_skin[i].SlipSpeed); writer.Write(_skin[i].TangentialForce);
            }
            byte[] payload = stream.ToArray();
            using var digest = SHA256.Create();
            writer.Write(digest.ComputeHash(payload));
            return stream.ToArray();
        }

        public static ElbowLab RestoreCheckpoint(byte[] data)
        {
            try
            {
                if (data.Length <= 32 || data.Length > 8192) throw new InvalidDataException("Invalid elbow checkpoint size.");
                int size = data.Length - 32;
                using var digest = SHA256.Create();
                if (!digest.ComputeHash(data, 0, size).SequenceEqual(data.Skip(size)))
                    throw new InvalidDataException("Elbow checkpoint checksum failed.");
                using var reader = new BinaryReader(new MemoryStream(data, 0, size, writable: false));
                if (reader.ReadString() != "DreamMachine.Elbow" || reader.ReadInt32() != 1)
                    throw new InvalidDataException("Unsupported elbow checkpoint.");
                string id = reader.ReadString();
                if (reader.ReadString() != ObjectId) throw new InvalidDataException("Unexpected contact object identity.");
                var settings = new Settings(ReadFinite(reader), ReadFinite(reader), ReadFinite(reader), ReadFinite(reader),
                    ReadFinite(reader), ReadFinite(reader), ReadFinite(reader), reader.ReadInt32());
                var lab = new ElbowLab(settings, id)
                {
                    Tick = reader.ReadInt32(),
                    RequestedTarget = ReadFinite(reader),
                    Angle = ReadFinite(reader), Velocity = ReadFinite(reader),
                    X = ReadFinite(reader), Y = ReadFinite(reader), VX = ReadFinite(reader), VY = ReadFinite(reader),
                    ObjectAngle = ReadFinite(reader), Spin = ReadFinite(reader)
                };
                if (lab.Tick < 0 || lab.Tick > settings.Duration || lab.Angle < settings.Lower ||
                    lab.Angle > settings.Upper || lab.RequestedTarget < settings.Lower || lab.RequestedTarget > settings.Upper)
                    throw new InvalidDataException("Invalid elbow clock or joint state.");
                lab.Latest = new StepSample(lab.Tick, ReadFinite(reader), ReadFinite(reader), ReadFinite(reader),
                    ReadFinite(reader), ReadFinite(reader), new Proprioception(lab.Angle, lab.Velocity,
                        ReadFinite(reader), ReadFinite(reader)));
                for (int i = 0; i < ReceptorCount; i++)
                {
                    lab._calibration[i] = ReadFinite(reader);
                    lab._skin[i] = new Receptor(ReadFinite(reader), ReadFinite(reader), ReadFinite(reader), ReadFinite(reader));
                    lab._duration[i] = lab._skin[i].ContactSeconds;
                }
                if (reader.BaseStream.Position != size) throw new InvalidDataException("Trailing elbow checkpoint data.");
                return lab;
            }
            catch (Exception error) when (error is EndOfStreamException || error is FormatException || error is ArgumentException)
            {
                throw new InvalidDataException("Invalid elbow checkpoint.", error);
            }
        }

        static double ReadFinite(BinaryReader reader)
        {
            double value = reader.ReadDouble();
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Invalid elbow checkpoint number.");
            return value;
        }

        public void SaveCheckpoint(string path) =>
            WorldSimulation.PublishCheckpoint(path, CaptureCheckpoint(), file => LoadCheckpoint(file) != null);

        public static ElbowLab LoadCheckpoint(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length > 8192) throw new InvalidDataException("Elbow checkpoint is oversized.");
                using var reader = new BinaryReader(stream);
                return RestoreCheckpoint(reader.ReadBytes((int)stream.Length));
            }
            catch (FileNotFoundException)
            {
                if (File.Exists(path + ".previous"))
                    throw new InvalidDataException("The primary elbow checkpoint is missing. A previous checkpoint is available.");
                return null;
            }
            catch (DirectoryNotFoundException) { return null; }
        }
    }
}
