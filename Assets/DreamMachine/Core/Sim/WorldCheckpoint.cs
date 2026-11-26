using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DreamMachine.Development;
using DreamMachine.Mind;
using DreamMachine.Social;
using DreamMachine.Util;

namespace DreamMachine.Sim
{
    public sealed class ResearchIntervention
    {
        public int Tick { get; }
        public string EntityId { get; }
        public string Kind { get; }
        public string Previous { get; }
        public string Requested { get; }
        public string Result { get; }
        public string Reason { get; }

        internal ResearchIntervention(int tick, string entityId, string kind, string previous,
            string requested, string result, string reason)
        {
            Tick = tick;
            EntityId = entityId;
            Kind = kind;
            Previous = previous;
            Requested = requested;
            Result = result;
            Reason = reason;
        }
    }

    public sealed partial class WorldSimulation
    {
        // Version 1 stores the embedded kernel at a completed tick boundary.
        const int CheckpointVersion = 1;
        const int MaxCheckpointBytes = 64 * 1024 * 1024;
        readonly System.Collections.Generic.List<ResearchIntervention> _interventions =
            new System.Collections.Generic.List<ResearchIntervention>();

        public System.Collections.Generic.IReadOnlyList<ResearchIntervention> ReadInterventions() =>
            _interventions.AsReadOnly();

        public SimChild ResetLearnerForResearch(string id, string reason)
        {
            SimChild child = Find(id);
            if (child == null) throw new ArgumentException("Unknown learner.", nameof(id));
            string previous = MiniJson.Stringify(child.State.ToData(includeAllObservations: true));
            ResetChild(id);
            _interventions.Add(new ResearchIntervention(TickCount, id, "reset-learner", previous,
                "Return to initial developmental state using the same identity and birth seed.",
                child.State.ToJson(), reason ?? ""));
            return child;
        }

        public byte[] CaptureCheckpoint()
        {
            using var payload = new MemoryStream();
            using var writer = new BinaryWriter(payload);
            writer.Write((int)Preset);
            writer.Write(TickCount);
            writer.Write(SimSeconds);
            writer.Write(_lastLoggedDay);
            writer.Write(Population.IsHousehold);
            writer.Write(Population.People.Count);
            foreach (PersonSpec person in Population.People)
            {
                writer.Write(person.Id);
                writer.Write(person.DisplayName ?? "");
                writer.Write((int)person.Sex);
                writer.Write((int)person.Group);
                writer.Write((int)person.Style);
                writer.Write((int)person.Role);
                writer.Write(person.IsLeader);
                writer.Write(person.PairedWithId);
                writer.Write(person.BirthSeed);
            }
            writer.Write(Elements.Width);
            writer.Write(Elements.Height);
            Elements.WriteCheckpoint(writer);
            foreach (SimChild child in Children)
            {
                writer.Write(MiniJson.Stringify(child.State.ToData(includeAllObservations: true)));
                writer.Write((int)child.Mind.Condition);
                writer.Write(child.X);
                writer.Write(child.Z);
                writer.Write(child.Yaw);
                writer.Write(child.SawParentPour);
                foreach (float angle in child.Pose) writer.Write(angle);
                writer.Write(child.State.Walker.InitialDistance);
                writer.Write(child.State.Walker.RecordRolloutRom);
                child.Body.WriteCheckpoint(writer);
                child.State.Emotions.WriteCheckpoint(writer);
                child.Wetware.WriteCheckpoint(writer);
            }
            writer.Write(PhysiologyLog.Count);
            foreach (PhysiologySample sample in PhysiologyLog)
            {
                writer.Write(sample.AgentId);
                writer.Write(sample.Day);
                writer.Write(sample.Phase);
                sample.WriteCheckpoint(writer);
            }
            writer.Write(_interventions.Count);
            foreach (ResearchIntervention entry in _interventions)
            {
                writer.Write(entry.Tick);
                writer.Write(entry.EntityId);
                writer.Write(entry.Kind);
                writer.Write(entry.Previous);
                writer.Write(entry.Requested);
                writer.Write(entry.Result);
                writer.Write(entry.Reason);
            }
            byte[] bytes = payload.ToArray();
            using var envelope = new MemoryStream();
            using var manifest = new BinaryWriter(envelope);
            using var digest = SHA256.Create();
            manifest.Write("DreamMachine.World");
            manifest.Write(CheckpointVersion);
            manifest.Write(bytes.Length);
            manifest.Write(digest.ComputeHash(bytes));
            manifest.Write(bytes);
            return envelope.ToArray();
        }

        public static WorldSimulation RestoreCheckpoint(byte[] checkpoint)
        {
            try
            {
                using var envelope = new BinaryReader(new MemoryStream(checkpoint, writable: false));
                if (checkpoint.Length > MaxCheckpointBytes || envelope.ReadString() != "DreamMachine.World" ||
                    envelope.ReadInt32() != CheckpointVersion)
                    throw new InvalidDataException("Unsupported world checkpoint.");
                int length = envelope.ReadInt32();
                if (length < 0 || length != envelope.BaseStream.Length - envelope.BaseStream.Position - 32)
                    throw new InvalidDataException("Invalid checkpoint payload length.");
                byte[] expected = envelope.ReadBytes(32);
                byte[] bytes = envelope.ReadBytes(length);
                using var digest = SHA256.Create();
                if (!expected.SequenceEqual(digest.ComputeHash(bytes)))
                    throw new InvalidDataException("World checkpoint checksum failed.");
                using var payload = new BinaryReader(new MemoryStream(bytes, writable: false));
                WorldSimulation world = ReadCheckpoint(payload);
                if (payload.BaseStream.Position != payload.BaseStream.Length)
                    throw new InvalidDataException("Unexpected trailing checkpoint data.");
                return world;
            }
            catch (Exception error) when (error is EndOfStreamException || error is FormatException ||
                error is InvalidOperationException || error is ArgumentException)
            {
                throw new InvalidDataException("Invalid world checkpoint.", error);
            }
        }

        static WorldSimulation ReadCheckpoint(BinaryReader reader)
        {
            var preset = (QualityPreset)reader.ReadInt32();
            int tick = reader.ReadInt32();
            float seconds = reader.ReadSingle();
            int lastDay = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(QualityPreset), preset) || tick < 0 ||
                float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
                throw new InvalidDataException("Invalid checkpoint clock or quality preset.");
            var population = new PopulationConfig { IsHousehold = reader.ReadBoolean() };
            int count = reader.ReadInt32();
            if (count < 2 || count > 1024) throw new InvalidDataException("Invalid checkpoint population.");
            for (int i = 0; i < count; i++)
            {
                population.People.Add(new PersonSpec
                {
                    Id = reader.ReadString(), DisplayName = reader.ReadString(), Sex = (Sex)reader.ReadInt32(),
                    Group = (GroupId)reader.ReadInt32(), Style = (GroupStyle)reader.ReadInt32(),
                    Role = (MemberRole)reader.ReadInt32(), IsLeader = reader.ReadBoolean(),
                    PairedWithId = reader.ReadString(), BirthSeed = reader.ReadInt32()
                });
            }
            population.Validate();
            if (population.People.Select(person => person.Id).Distinct().Count() != count)
                throw new InvalidDataException("Duplicate checkpoint identities.");
            var world = Create(population, preset, 0);
            world.TickCount = tick;
            world.SimSeconds = seconds;
            world._lastLoggedDay = lastDay;
            int width = reader.ReadInt32(), height = reader.ReadInt32();
            if (width != world.GridSize || height != world.GridSize)
                throw new InvalidDataException("Checkpoint grid dimensions do not match the preset.");
            world.Elements.ReadCheckpoint(reader);
            foreach (SimChild child in world.Children)
            {
                AgentPersistentState state = AgentPersistentState.FromJson(reader.ReadString(), false);
                if (state.AgentId != child.Spec.Id || state.BirthSeed != child.Spec.BirthSeed)
                    throw new InvalidDataException("Checkpoint child does not match " + child.Spec.Id);
                BindChildState(child, state);
                child.Mind.Condition = (MindCondition)reader.ReadInt32();
                child.X = reader.ReadSingle();
                child.Z = reader.ReadSingle();
                child.Yaw = reader.ReadSingle();
                child.SawParentPour = reader.ReadBoolean();
                for (int i = 0; i < child.Pose.Length; i++) child.Pose[i] = reader.ReadSingle();
                state.Walker.InitialDistance = reader.ReadSingle();
                state.Walker.RecordRolloutRom = reader.ReadBoolean();
                child.Body.ReadCheckpoint(reader);
                state.Emotions.ReadCheckpoint(reader);
                child.Wetware.ReadCheckpoint(reader);
            }
            int samples = reader.ReadInt32();
            if (samples < 0 || samples > MaxCheckpointBytes / 80)
                throw new InvalidDataException("Invalid physiology log length.");
            for (int i = 0; i < samples; i++)
            {
                var sample = new PhysiologySample { AgentId = reader.ReadString(), Day = reader.ReadInt32(), Phase = reader.ReadString() };
                sample.ReadCheckpoint(reader);
                world.PhysiologyLog.Add(sample);
            }
            int interventions = reader.ReadInt32();
            if (interventions < 0 || interventions > MaxCheckpointBytes / 16)
                throw new InvalidDataException("Invalid intervention count.");
            for (int i = 0; i < interventions; i++)
                world._interventions.Add(new ResearchIntervention(reader.ReadInt32(), reader.ReadString(),
                    reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadString()));
            return world;
        }

        public static WorldSimulation LoadCheckpoint(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length > MaxCheckpointBytes) throw new InvalidDataException("World checkpoint exceeds size limit.");
                using var reader = new BinaryReader(stream);
                return RestoreCheckpoint(reader.ReadBytes((int)stream.Length));
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        public static WorldSimulation LoadPrimaryCheckpoint(string path)
        {
            WorldSimulation world = LoadCheckpoint(path);
            if (world == null && File.Exists(path + ".previous"))
                throw new InvalidDataException("The primary world checkpoint is missing. A previous checkpoint is available.");
            return world;
        }

        public void SaveCheckpoint(string path)
        {
            byte[] data = CaptureCheckpoint();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string pending = path + "." + Guid.NewGuid().ToString("N") + ".pending";
            try
            {
                using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write))
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush(true);
                }
                LoadCheckpoint(pending);
                if (LoadPrimaryCheckpoint(path) != null) File.Replace(pending, path, path + ".previous");
                else File.Move(pending, path);
            }
            finally
            {
                File.Delete(pending);
            }
        }
    }
}
