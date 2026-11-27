using System;
using System.Collections.Generic;
using System.IO;
using DreamMachine.Body;

namespace DreamMachine.Sim
{
    public readonly struct HouseholdGripRecord
    {
        public readonly GripPractice Practice;
        public readonly int Start, End;
        public readonly string Tape, Outcome, MemoryId;
        public readonly float X, Y, Z;
        internal HouseholdGripRecord(GripPractice practice, int start, int end, string tape, string outcome, string memoryId,
            float x, float y, float z)
        {
            Practice = practice; Start = start; End = end; Tape = tape; Outcome = outcome; MemoryId = memoryId;
            X = x; Y = y; Z = z;
        }
    }

    public sealed class HouseholdGripSession
    {
        public const string Arthur = "son";
        public const int MaximumAttempts = 256;
        public const float StationX = -3f, StationZ = .91f;
        public const float Mass = .080f, Radius = .028f, Friction = .75f;
        readonly GripAcquisition _policy;
        readonly List<HouseholdGripRecord> _records = new List<HouseholdGripRecord>();
        internal HouseholdGripSession(SimChild subject)
        {
            _policy = new GripAcquisition(subject.Spec.Id, subject.Spec.BirthSeed);
            Records = _records.AsReadOnly();
        }

        public string Individual => _policy.Individual;
        public int BirthSeed => _policy.BirthSeed;
        public float Cap => _policy.Cap;
        public IReadOnlyList<HouseholdGripRecord> Records { get; }
        public bool Active { get; internal set; }
        public int AttemptStarted { get; internal set; } = -1;
        public bool AttemptRunning => AttemptStarted >= 0;

        internal HouseholdGripRecord Complete(SimChild arthur, int end, int slip, int held, float clearance,
            bool success, string outcome, string tape, float x, float y, float z)
        {
            ValidateRecord(tape, outcome, clearance, x, y, z);
            var result = _policy.Observe(slip, held, clearance, success);
            string memory = "motor:h2:grip:" + result.Attempt;
            var entry = new HouseholdGripRecord(result, AttemptStarted, end, tape, outcome, memory, x, y, z);
            _records.Add(entry);
            AttemptStarted = -1;
            arthur.State.Library.RecordExperience(memory, "Supported grasp attempt " + result.Attempt,
                FormattableString.Invariant($"Lift intention. {outcome}. Slip {slip} ticks. Hold {held} ticks. Peak lift {clearance * 1000:F1} mm. Grip {result.Before:F2} to {result.After:F2} N. Trace {tape}."), end);
            return entry;
        }

        internal void Write(BinaryWriter output)
        {
            output.Write(Active); output.Write(Individual); output.Write(BirthSeed);
            output.Write(GripAcquisition.CalibrationId);
            output.Write(_records.Count);
            foreach (var record in _records)
            {
                var item = record.Practice;
                output.Write(record.Start); output.Write(record.End); output.Write(item.SlipTicks); output.Write(item.HoldTicks);
                output.Write(item.PeakClearance); output.Write(item.Success); output.Write(record.Outcome); output.Write(record.Tape);
                output.Write(record.X); output.Write(record.Y); output.Write(record.Z);
            }
        }

        static void ValidateRecord(string tape, string outcome, float clearance, float x, float y, float z)
        {
            if (tape == null || tape.Length != 64 || outcome == null || outcome.Length == 0 || outcome.Length > 80 ||
                !float.IsFinite(clearance) || !float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
                throw new InvalidDataException("Invalid physical practice evidence.");
            foreach (char digit in tape)
                if (!(digit >= '0' && digit <= '9' || digit >= 'a' && digit <= 'f'))
                    throw new InvalidDataException("Invalid physical trajectory reference.");
        }

        internal static HouseholdGripSession Read(BinaryReader input, SimChild subject, int tick)
        {
            bool active = input.ReadBoolean();
            if (subject == null || input.ReadString() != subject.Spec.Id || input.ReadInt32() != subject.Spec.BirthSeed ||
                input.ReadString() != GripAcquisition.CalibrationId)
                throw new InvalidDataException("Physical practice belongs to another learner or calibration.");
            int count = input.ReadInt32(), previous = 0;
            if ((uint)count > MaximumAttempts) throw new InvalidDataException("Invalid physical practice count.");
            var result = new HouseholdGripSession(subject) { Active = active };
            for (int i = 0; i < count; i++)
            {
                int start = input.ReadInt32(), end = input.ReadInt32(), slip = input.ReadInt32(), held = input.ReadInt32();
                float clearance = input.ReadSingle();
                bool success = input.ReadBoolean();
                string outcome = input.ReadString(), tape = input.ReadString();
                float x = input.ReadSingle(), y = input.ReadSingle(), z = input.ReadSingle();
                ValidateRecord(tape, outcome, clearance, x, y, z);
                if (start < previous || end <= start || end > tick || slip > end - start || held > end - start)
                    throw new InvalidDataException("Invalid physical practice timing.");
                var practice = result._policy.Observe(slip, held, clearance, success);
                string id = "motor:h2:grip:" + practice.Attempt;
                if (subject.State.Library.GetOrNull(id)?.Source != "firsthand-supported-grip")
                    throw new InvalidDataException("Physical practice is missing its lived evidence.");
                result._records.Add(new HouseholdGripRecord(practice, start, end, tape, outcome, id, x, y, z));
                previous = end;
            }
            return result;
        }
    }

    public sealed partial class WorldSimulation
    {
        public HouseholdGripSession PhysicalPractice { get; private set; }

        public bool EnterPhysicalPractice()
        {
            var arthur = Find(HouseholdGripSession.Arthur);
            if (arthur == null || Home == null || !Home.CanReserveForPractice() || PhysicalPractice?.AttemptRunning == true) return false;
            if (PhysicalPractice?.Active == true) return true;
            PhysicalPractice ??= new HouseholdGripSession(arthur);
            PhysicalPractice.Active = true;
            Home.ReserveForPractice();
            _interventions.Add(new ResearchIntervention(TickCount, arthur.Spec.Id, "physical-practice", "Living",
                "Enter supported station", "Physical development", "Researcher assigned a supported grip objective."));
            return true;
        }

        public bool StartGripAttempt()
        {
            if (PhysicalPractice?.Active != true || PhysicalPractice.AttemptRunning ||
                PhysicalPractice.Records.Count >= HouseholdGripSession.MaximumAttempts) return false;
            PhysicalPractice.AttemptStarted = TickCount;
            return true;
        }

        public HouseholdGripRecord CompleteGripAttempt(int slip, int held, float clearance, bool success,
            string outcome, string tape, float x, float y, float z)
        {
            var session = PhysicalPractice;
            if (session?.AttemptRunning != true || TickCount <= session.AttemptStarted) throw new InvalidOperationException("No completed grip tick.");
            return session.Complete(Find(session.Individual), TickCount, slip, held, clearance, success, outcome, tape, x, y, z);
        }

        public bool LeavePhysicalPractice()
        {
            if (PhysicalPractice?.Active != true || PhysicalPractice.AttemptRunning) return false;
            PhysicalPractice.Active = false;
            _interventions.Add(new ResearchIntervention(TickCount, PhysicalPractice.Individual, "physical-practice",
                "Physical development", "Return to Living", "Living", "Researcher ended the settled practice session."));
            return true;
        }
    }

    public sealed partial class HouseholdLife
    {
        internal bool CanReserveForPractice() => _people[SearchActor].Activity != HomeActivity.FindToy;
        internal void ReserveForPractice()
        {
            var person = _people[SearchActor];
            Rest(person, _world.TickCount, HomeReason.Resting);
            person.Child.X = person.X = HouseholdGripSession.StationX;
            person.Child.Z = person.Z = HouseholdGripSession.StationZ;
            person.Child.Yaw = person.Yaw = 0f;
        }
    }
}
