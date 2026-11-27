using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DreamMachine.Body;
using DreamMachine.Sim;

namespace DreamMachine.UnityBridge
{
    public sealed class PredictiveNativeTrial : IDisposable
    {
        readonly string _model;
        public NativeElbowLab Lab { get; }
        public PredictiveMotor Motor { get; }
        public double LastProbability { get; private set; }
        public double LastError { get; private set; }
        public double LastProposal { get; private set; }
        public bool HasFeedback { get; private set; }
        public bool Finished => Lab.Finished || Motor.Stopped;

        public PredictiveNativeTrial(NativeElbowLab lab, string model, MotorCondition condition)
            : this(lab, model, new PredictiveMotor(condition, ContactPredictor.Restore(model, lab.IndividualId))) { }

        PredictiveNativeTrial(NativeElbowLab lab, string model, PredictiveMotor motor)
        {
            Lab = lab;
            Motor = motor;
            _model = model;
        }

        public ElbowLab.StepSample Step()
            => Step(null);

        public ElbowLab.StepSample Step(double? replacementCorrection)
        {
            var plan = Motor.Before(Lab.Latest, Lab.RequestedTarget);
            LastProbability = plan.Probability;
            LastProposal = replacementCorrection ?? plan.Correction;
            ElbowLab.StepSample result = Lab.Step(LastProposal);
            LastError = Motor.After(plan.Probability, result);
            HasFeedback = true;
            return result;
        }

        public byte[] CaptureCheckpoint()
        {
            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer);
            writer.Write("DreamMachine.PredictiveNative");
            writer.Write(1);
            writer.Write(Lab.IndividualId);
            writer.Write(_model);
            Motor.WriteState(writer);
            writer.Write(HasFeedback);
            writer.Write(LastProbability);
            writer.Write(LastError);
            writer.Write(LastProposal);
            byte[] physics = Lab.CaptureCheckpoint();
            writer.Write(physics.Length);
            writer.Write(physics);
            using var digest = SHA256.Create();
            writer.Write(digest.ComputeHash(buffer.ToArray()));
            return buffer.ToArray();
        }

        public void SaveCheckpoint(string path) =>
            WorldSimulation.PublishCheckpoint(path, CaptureCheckpoint(), file => ReadFile(file) != null);

        static byte[] ReadFile(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length > 32768) throw new InvalidDataException("Predictive checkpoint is oversized.");
                using var reader = new BinaryReader(stream);
                byte[] data = reader.ReadBytes((int)stream.Length);
                Decode(data);
                return data;
            }
            catch (Exception error) when (error is FileNotFoundException || error is DirectoryNotFoundException)
            {
                if (File.Exists(path + ".previous"))
                    throw new InvalidDataException("Predictive checkpoint primary missing with previous generation available.");
                return null;
            }
        }

        public static PredictiveNativeTrial LoadCheckpoint(string path)
        {
            byte[] data = ReadFile(path);
            return data == null ? null : RestoreCheckpoint(data);
        }

        public static PredictiveNativeTrial RestoreCheckpoint(byte[] data)
        {
            var state = Decode(data);
            NativeElbowLab lab = NativeElbowLab.RestoreCheckpoint(state.Physics);
            if (lab.IndividualId != state.Id)
            {
                lab.Dispose();
                throw new InvalidDataException("Predictive checkpoint identity mismatch.");
            }
            return new PredictiveNativeTrial(lab, state.Model, state.Motor)
            {
                HasFeedback = state.Feedback, LastProbability = state.Probability,
                LastError = state.Error, LastProposal = state.Proposal
            };
        }

        public static void ValidateCheckpoint(byte[] data) => Decode(data);

        static (string Id, string Model, PredictiveMotor Motor, bool Feedback, double Probability,
            double Error, double Proposal, byte[] Physics) Decode(byte[] data)
        {
            try
            {
                int size = data.Length - 32;
                if (size <= 0 || data.Length > 32768) throw new InvalidDataException("Invalid predictive checkpoint size.");
                using var digest = SHA256.Create();
                if (!digest.ComputeHash(data, 0, size).SequenceEqual(data.Skip(size)))
                    throw new InvalidDataException("Predictive checkpoint checksum failed.");
                using var reader = new BinaryReader(new MemoryStream(data, 0, size, writable: false));
                if (reader.ReadString() != "DreamMachine.PredictiveNative" || reader.ReadInt32() != 1)
                    throw new InvalidDataException("Unsupported predictive checkpoint.");
                string id = reader.ReadString(), model = reader.ReadString();
                var motor = PredictiveMotor.ReadState(reader, ContactPredictor.Restore(model, id));
                bool feedback = reader.ReadBoolean();
                double probability = reader.ReadDouble(), error = reader.ReadDouble(), proposal = reader.ReadDouble();
                int length = reader.ReadInt32();
                if (!PredictiveMotor.Finite(probability) || probability < 0 || probability > 1 ||
                    !PredictiveMotor.Finite(error) || Math.Abs(error) > 1 ||
                    !PredictiveMotor.Finite(proposal) || Math.Abs(proposal) > PredictiveMotor.MaximumCorrection ||
                    length <= 0 || length != size - reader.BaseStream.Position)
                    throw new InvalidDataException("Invalid predictive motor record.");
                byte[] physics = reader.ReadBytes(length);
                if (NativeElbowLab.ValidateCheckpoint(physics) != id)
                    throw new InvalidDataException("Predictive checkpoint identity mismatch.");
                return (id, model, motor, feedback, probability, error, proposal, physics);
            }
            catch (Exception error) when (error is EndOfStreamException || error is FormatException)
            {
                throw new InvalidDataException("Invalid predictive checkpoint.", error);
            }
        }

        public void Dispose() => Lab.Dispose();
    }
}
