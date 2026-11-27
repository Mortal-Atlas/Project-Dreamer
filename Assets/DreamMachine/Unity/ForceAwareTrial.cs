using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DreamMachine.Body;
using DreamMachine.Sim;

namespace DreamMachine.UnityBridge
{
    public sealed class ForceAwareTrial : IDisposable
    {
        readonly PredictiveNativeTrial _trial;
        readonly ImpulsePredictor _impulse;
        public ForceResidual Residual { get; }
        public NativeElbowLab Lab => _trial.Lab;
        public bool Finished => _trial.Finished;
        public double Prediction { get; private set; }
        public double Uncertainty { get; private set; }

        public ForceAwareTrial(PredictiveNativeTrial trial, ImpulsePredictor impulse, ForceResidual residual)
        {
            _trial = trial;
            _impulse = impulse;
            Residual = residual;
        }

        public ElbowLab.StepSample Step()
        {
            var context = new ImpulsePredictor.Example(Lab.Latest, Lab.RequestedTarget, 0);
            (Prediction, Uncertainty) = _impulse.Predict(context);
            double correction = Residual.Before(Lab.Latest, Lab.RequestedTarget, Prediction, Uncertainty);
            ElbowLab.StepSample sample = _trial.Step(correction);
            Residual.After(sample, Lab.RequestedTarget, Prediction, Uncertainty);
            return sample;
        }

        public byte[] CaptureCheckpoint()
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write("DreamMachine.ForceAware");
            writer.Write(1);
            _impulse.Write(writer);
            Residual.Write(writer);
            writer.Write(Prediction);
            writer.Write(Uncertainty);
            byte[] trial = _trial.CaptureCheckpoint();
            writer.Write(trial.Length);
            writer.Write(trial);
            using var digest = SHA256.Create();
            writer.Write(digest.ComputeHash(memory.ToArray()));
            return memory.ToArray();
        }

        public void SaveCheckpoint(string path) =>
            WorldSimulation.PublishCheckpoint(path, CaptureCheckpoint(), file => ReadFile(file) != null);

        static byte[] ReadFile(string path)
        {
            if (!File.Exists(path) && !File.Exists(path + ".previous")) return null;
            using var stream = File.OpenRead(path);
            if (stream.Length > 131072) throw new InvalidDataException("Force checkpoint too large.");
            using var reader = new BinaryReader(stream);
            byte[] data = reader.ReadBytes((int)stream.Length);
            Decode(data);
            return data;
        }

        public static ForceAwareTrial LoadCheckpoint(string path)
        {
            byte[] data = ReadFile(path);
            if (data == null) return null;
            var restored = Decode(data);
            return new ForceAwareTrial(PredictiveNativeTrial.RestoreCheckpoint(restored.Trial), restored.Impulse, restored.Residual)
            {
                Prediction = restored.Prediction, Uncertainty = restored.Uncertainty
            };
        }

        static (ImpulsePredictor Impulse, ForceResidual Residual, double Prediction, double Uncertainty, byte[] Trial) Decode(byte[] data)
        {
            try
            {
                int size = data.Length - 32;
                using var digest = SHA256.Create();
                if (size <= 0 || !digest.ComputeHash(data, 0, size).SequenceEqual(data.Skip(size)))
                    throw new InvalidDataException("Force checkpoint checksum failed.");
                using var reader = new BinaryReader(new MemoryStream(data, 0, size, writable: false));
                if (reader.ReadString() != "DreamMachine.ForceAware" || reader.ReadInt32() != 1)
                    throw new InvalidDataException("Invalid force checkpoint version.");
                var impulse = ImpulsePredictor.Read(reader);
                var residual = ForceResidual.Read(reader);
                double prediction = reader.ReadDouble(), uncertainty = reader.ReadDouble();
                int length = reader.ReadInt32();
                if (!PredictiveMotor.Finite(prediction) || !PredictiveMotor.Finite(uncertainty) || prediction < 0 ||
                    uncertainty < 0 || length <= 0 || length != size - reader.BaseStream.Position)
                    throw new InvalidDataException("Invalid force checkpoint telemetry.");
                byte[] trial = reader.ReadBytes(length);
                PredictiveNativeTrial.ValidateCheckpoint(trial);
                return (impulse, residual, prediction, uncertainty, trial);
            }
            catch (Exception error) when (error is EndOfStreamException || error is ArgumentException)
            {
                throw new InvalidDataException("Invalid force checkpoint.", error);
            }
        }

        public void Dispose() => _trial.Dispose();
    }
}
