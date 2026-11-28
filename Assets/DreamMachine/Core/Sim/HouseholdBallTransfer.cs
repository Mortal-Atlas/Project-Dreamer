using System;
using System.Collections.Generic;
using System.IO;
using DreamMachine.World;
using DreamMachine.Body;

namespace DreamMachine.Sim
{
    public enum HouseholdGripPolicy : byte { Fixed, Light, Feedback, Articulated, CoordinatedPlacement }

    public readonly struct HouseholdBallPose
    {
        public readonly float X, Y, Z, Qx, Qy, Qz, Qw;
        public HouseholdBallPose(float x, float y, float z, float qx = 0, float qy = 0, float qz = 0, float qw = 1)
        { X = x; Y = y; Z = z; Qx = qx; Qy = qy; Qz = qz; Qw = qw; }

        internal void Write(BinaryWriter w)
        { w.Write(X); w.Write(Y); w.Write(Z); w.Write(Qx); w.Write(Qy); w.Write(Qz); w.Write(Qw); }
        internal static HouseholdBallPose Read(BinaryReader r)
        {
            var p = new HouseholdBallPose(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(),
                r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            if (!p.Valid) throw new InvalidDataException("Invalid settled ball pose.");
            return p;
        }
        internal bool Valid => Math.Abs(X) <= 4f && Y >= .10f && Y <= .25f && Z >= -.75f && Z <= 3.75f &&
            Math.Abs(Qx * Qx + Qy * Qy + Qz * Qz + Qw * Qw - 1f) < .002f;
    }

    public readonly struct BallLocationBelief
    {
        public readonly float X, Z;
        public readonly int LastSeen, LastCheck, Sequence;
        public readonly bool Uncertain;
        internal BallLocationBelief(float x, float z, int seen, int check, int sequence, bool uncertain)
        { X = x; Z = z; LastSeen = seen; LastCheck = check; Sequence = sequence; Uncertain = uncertain; }
    }

    public readonly struct BallTransferIntent
    {
        public readonly int Start, Actor;
        public readonly float X, Z, TargetX, TargetZ, TargetTop;
        public readonly HouseholdGripPolicy Policy;
        public readonly HouseholdBallPose Before;
        internal BallTransferIntent(int start, int actor, float x, float z, float targetX, float targetZ,
            float top, HouseholdGripPolicy policy, HouseholdBallPose before)
        { Start = start; Actor = actor; X = x; Z = z; TargetX = targetX; TargetZ = targetZ; TargetTop = top; Policy = policy; Before = before; }
    }

    public readonly struct BallTransferRecord
    {
        public readonly BallTransferIntent Intent;
        public readonly HouseholdBallPose After;
        public readonly int End, Corrections;
        public readonly bool Success, Seen;
        public readonly string Tape, Outcome, MemoryId;
        internal BallTransferRecord(BallTransferIntent intent, HouseholdBallPose after, int end, int corrections,
            bool success, bool seen, string tape, string outcome, string memory)
        { Intent = intent; After = after; End = end; Corrections = corrections; Success = success; Seen = seen;
            Tape = tape; Outcome = outcome; MemoryId = memory; }
    }

    public sealed partial class HouseholdLife
    {
        public const int BallItem = 1;
        public const string BallId = "household.toy.ball";
        public const float BallRadius = .125f, BallMass = .080f, BallFriction = .75f;
        public const float BallShelfOffset = -.270f, BallShelfTop = .050f;
        public const float BallShelfHalfWidth = .115f, BallShelfHalfDepth = .170f;
        const int BallRecordLimit = 256;
        readonly List<BallTransferRecord> _ballRecords = new List<BallTransferRecord>();
        readonly BallLocationBelief[] _ballBeliefs =
        {
            new BallLocationBelief(0, 0, -1, -1, 0, true), new BallLocationBelief(0, 0, -1, -1, 0, true)
        };
        BallTransferIntent? _ballIntent;
        HouseholdBallPose _ballSettled;
        public bool BallManaged { get; private set; }
        public float BallBenchX { get; private set; }
        public float BallBenchZ { get; private set; }
        public bool BallTransferRunning => _ballIntent.HasValue;
        public BallTransferIntent? BallIntent => _ballIntent;
        public int BallTransferCount => _ballRecords.Count;
        public BallTransferRecord BallTransfer(int index) => _ballRecords[index];
        public HouseholdBallPose ReadBallPose() => BallManaged ? _ballSettled :
            new HouseholdBallPose(_items[BallItem].X, BallRadius, _items[BallItem].Z);
        public BallLocationBelief ReadBallBelief(int actor) => _ballBeliefs[actor - 2];

        static float QuantizeBallSight(float position) => MathF.Round(position * 500f) / 500f;
        static string BallSightingId(int sequence) => "space:h5:ball:" + sequence;
        static string BallTransferMemoryId(int sequence) => "motor:h5:ball:" + sequence;

        // Authoritative geometry is used only by the sensor. Its caller receives a local sighting or a miss.
        bool BallInSight(Person person, HouseholdBallPose ball, float lookX, float lookZ, float window) =>
            Distance2(ball.X, ball.Z, lookX, lookZ) <= window * window &&
            Distance2(person.X, person.Z, ball.X, ball.Z) <= 1.65f * 1.65f &&
            YardClearance.HouseMotionFraction(person.X, person.Z, ball.X - person.X, ball.Z - person.Z, 0f) >= .99f;

        bool RecordBallLook(int actor, float x, float z, float window)
        {
            var previous = ReadBallBelief(actor);
            var ball = ReadBallPose();
            bool seen = BallInSight(_people[actor], ball, x, z, window);
            int tick = _world.TickCount;
            var observation = new BallLocationBelief(seen ? QuantizeBallSight(ball.X) : previous.X,
                seen ? QuantizeBallSight(ball.Z) : previous.Z, seen ? tick : previous.LastSeen,
                tick, previous.Sequence + 1, !seen);
            string found = seen ? FormattableString.Invariant($"Sighting ({observation.X:F3}, {observation.Z:F3}).")
                : "No ball in the window.";
            _people[actor].Child.State.Library.RecordExperience(BallSightingId(observation.Sequence),
                seen ? "Saw household ball" : "Household ball absent in the inspected area",
                FormattableString.Invariant($"{BallId}. Looked at ({x:F3}, {z:F3}) with a {window:F3} m window. {found}"),
                tick, "firsthand-ball-observation");
            _ballBeliefs[actor - 2] = observation;
            return seen;
        }

        public bool InspectBall(int actor, bool searchArea)
        {
            if (actor < 2 || actor >= PersonCount || BallTransferRunning || !BallManaged) return false;
            var belief = ReadBallBelief(actor);
            int tick = _world.TickCount;
            float x = belief.LastSeen < 0 ? BallBenchX : belief.X;
            float z = belief.LastSeen < 0 ? BallBenchZ : belief.Z;
            Face(_people[actor], x, z);
            bool seen = RecordBallLook(actor, x, z, searchArea ? .65f : .105f);
            _world.RecordBallIntervention(PersonId(actor), tick, searchArea ? "Search near sighting" : "Inspect last sighting");
            return seen;
        }

        public bool StartBallTransfer(int actor, HouseholdGripPolicy policy)
        {
            if (actor < 2 || actor >= PersonCount || BallTransferRunning || _ballRecords.Count >= BallRecordLimit ||
                _world.PhysicalPractice?.Active == true || Math.Abs(_items[BallItem].V) > .001f ||
                _people[actor].Activity == HomeActivity.FindToy || !Available(actor, BallItem) ||
                !Enum.IsDefined(typeof(HouseholdGripPolicy), policy)) return false;
            var ball = ReadBallPose();
            float benchX = BallManaged ? BallBenchX : QuantizeBallSight(ball.X);
            float benchZ = BallManaged ? BallBenchZ : QuantizeBallSight(ball.Z);
            float shelfX = benchX + BallShelfOffset;
            float seatX = benchX + BallShelfOffset * .5f, seatZ = benchZ - .77f;
            if (benchX < -1.30f || benchX > 1.30f || Math.Abs(ball.X - benchX) > .53f ||
                Math.Abs(ball.Z - benchZ) > .31f ||
                YardClearance.HouseMotionFraction(seatX, seatZ, ball.X - seatX, ball.Z - seatZ, 0f) < .99f) return false;

            // Entering the bench is an explicit researcher intervention. The chair and guide supply support.
            var person = _people[actor];
            Rest(person, _world.TickCount, HomeReason.Resting);
            person.Child.X = person.X = seatX;
            person.Child.Z = person.Z = seatZ;
            Face(person, benchX, benchZ);
            person.Child.Yaw = person.Yaw;
            BallBenchX = benchX; BallBenchZ = benchZ; _ballSettled = ball; BallManaged = true;
            _items[BallItem].V = 0f;
            RecordBallLook(actor, benchX + BallShelfOffset * .5f, benchZ, .65f);
            var seen = ReadBallBelief(actor);
            if (seen.Uncertain) return false;
            bool toShelf = Math.Abs(seen.X - shelfX) > BallShelfHalfWidth || ball.Y < BallRadius + BallShelfTop * .5f;
            _ballIntent = new BallTransferIntent(_world.TickCount, actor, seen.X, seen.Z, toShelf ? shelfX : benchX,
                benchZ, toShelf ? BallShelfTop : 0f, policy, ball);
            _world.RecordBallIntervention(PersonId(actor), _world.TickCount, "Start ball transfer with " + policy);
            return true;
        }

        public BallTransferRecord CompleteBallTransfer(HouseholdBallPose actual, bool success, int corrections, string outcome,
            string tape, PlacementObservation releaseObservation = PlacementObservation.None)
        {
            if (!_ballIntent.HasValue) throw new InvalidOperationException("No live ball transfer.");
            var intent = _ballIntent.Value;
            if (!actual.Valid || corrections < 0 || !BallTapeValid(tape, outcome) || _world.TickCount <= intent.Start ||
                !BallOutcomeValid(intent, actual, success, outcome))
                throw new InvalidDataException("Invalid native ball transfer evidence.");
            var p = _people[intent.Actor];
            string memory = BallTransferMemoryId(_ballRecords.Count + 1);
            if (p.Child.State.Library.GetOrNull(memory) != null) throw new InvalidDataException("Duplicate ball transfer evidence.");
            bool seen = BallInSight(p, actual, intent.TargetX, intent.TargetZ, .65f);
            string observed = seen ? FormattableString.Invariant($"Saw resulting area at ({QuantizeBallSight(actual.X):F3}, {QuantizeBallSight(actual.Z):F3}).")
                : "No final position sighting.";
            if (intent.Policy == HouseholdGripPolicy.CoordinatedPlacement && releaseObservation != PlacementObservation.None)
                observed += " Settled bench tracking: " + releaseObservation + ".";
            p.Child.State.Library.RecordExperience(memory, "Supported household ball transfer",
                FormattableString.Invariant($"{BallId}. Started from a sighting at ({intent.X:F3}, {intent.Z:F3}). Researcher goal ({intent.TargetX:F3}, {intent.TargetZ:F3}). Engineered {intent.Policy} guide. {outcome}. Corrections {corrections}. {observed} Trace {tape}."),
                _world.TickCount, "firsthand-ball-transfer");
            _ballSettled = actual;
            var item = _items[BallItem];
            item.X = actual.X; item.Z = actual.Z; item.V = 0f; item.Revision++;
            var record = new BallTransferRecord(intent, actual, _world.TickCount, corrections, success, seen, tape, outcome, memory);
            _ballRecords.Add(record);
            _ballIntent = null;
            RecordBallLook(intent.Actor, intent.TargetX, intent.TargetZ, .65f);
            return record;
        }

        static bool BallTapeValid(string tape, string outcome)
        {
            if (tape == null || tape.Length != 64 || string.IsNullOrWhiteSpace(outcome) || outcome.Length > 80) return false;
            foreach (char c in tape) if (!(c >= 'a' && c <= 'f' || c >= '0' && c <= '9')) return false;
            return true;
        }

        static bool BallOutcomeValid(BallTransferIntent intent, HouseholdBallPose after, bool success, string outcome) =>
            success == (outcome == "physical-lift-hold-place-release") &&
            (!success || (Math.Abs(after.X - intent.TargetX) <= .108f && Math.Abs(after.Z - intent.TargetZ) <= .175f &&
                Math.Abs(after.Y - BallRadius - intent.TargetTop) < .006f));

        internal void WriteBallTransfer(BinaryWriter w)
        {
            w.Write(BallManaged);
            if (BallManaged)
            {
                w.Write(BallId); w.Write(BallBenchX); w.Write(BallBenchZ); _ballSettled.Write(w);
                w.Write(_ballRecords.Count);
                foreach (var record in _ballRecords)
                {
                    var i = record.Intent;
                    w.Write(i.Actor); w.Write(i.Start); w.Write(i.X); w.Write(i.Z);
                    w.Write(i.TargetX); w.Write(i.TargetZ); w.Write(i.TargetTop); w.Write((byte)i.Policy); i.Before.Write(w);
                    record.After.Write(w); w.Write(record.End); w.Write(record.Corrections); w.Write(record.Success);
                    w.Write(record.Seen); w.Write(record.Tape); w.Write(record.Outcome);
                }
                foreach (var belief in _ballBeliefs)
                {
                    w.Write(belief.X); w.Write(belief.Z); w.Write(belief.LastSeen); w.Write(belief.LastCheck);
                    w.Write(belief.Sequence); w.Write(belief.Uncertain);
                }
            }
        }

        internal void ReadBallTransfer(BinaryReader r)
        {
            if (!(BallManaged = r.ReadBoolean())) return;
            if (r.ReadString() != BallId) throw new InvalidDataException("Wrong household ball identity.");
            BallBenchX = ReadFinite(r); BallBenchZ = ReadFinite(r); _ballSettled = HouseholdBallPose.Read(r);
            var item = _items[BallItem];
            if (item.X != _ballSettled.X || item.Z != _ballSettled.Z || item.V != 0f)
                throw new InvalidDataException("Household ball coordinates disagree.");
            int count = r.ReadInt32(), previous = 0;
            if ((uint)count > BallRecordLimit) throw new InvalidDataException("Invalid ball history length.");
            for (int n = 0; n < count; n++)
            {
                int actor = r.ReadInt32(), start = r.ReadInt32();
                var i = new BallTransferIntent(start, actor, ReadFinite(r), ReadFinite(r), ReadFinite(r), ReadFinite(r),
                    ReadFinite(r), (HouseholdGripPolicy)r.ReadByte(), HouseholdBallPose.Read(r));
                var after = HouseholdBallPose.Read(r);
                int end = r.ReadInt32(), corrections = r.ReadInt32();
                bool success = r.ReadBoolean(), seen = r.ReadBoolean();
                string tape = r.ReadString(), outcome = r.ReadString(), id = BallTransferMemoryId(n + 1);
                if (actor < 2 || actor >= PersonCount || start < previous || end <= start || end > _world.TickCount ||
                    corrections < 0 || !Enum.IsDefined(typeof(HouseholdGripPolicy), i.Policy) ||
                    !BallTapeValid(tape, outcome) || !BallOutcomeValid(i, after, success, outcome) ||
                    _people[actor].Child.State.Library.GetOrNull(id)?.Source != "firsthand-ball-transfer" ||
                    (n > 0 && !i.Before.Equals(_ballRecords[n - 1].After)))
                    throw new InvalidDataException("Invalid ball transfer record.");
                previous = end;
                _ballRecords.Add(new BallTransferRecord(i, after, end, corrections, success, seen, tape, outcome, id));
            }
            if (count > 0 && !_ballSettled.Equals(_ballRecords[count - 1].After))
                throw new InvalidDataException("Settled ball conflicts with its trajectory history.");
            for (int n = 0; n < _ballBeliefs.Length; n++)
            {
                var b = new BallLocationBelief(ReadFinite(r), ReadFinite(r), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadBoolean());
                if (b.Sequence < 0 || b.LastSeen < -1 || b.LastCheck < b.LastSeen || b.LastCheck > _world.TickCount ||
                    (b.Sequence > 0 && _people[n + 2].Child.State.Library.GetOrNull(BallSightingId(b.Sequence))?.Source != "firsthand-ball-observation"))
                    throw new InvalidDataException("Invalid ball sighting reference.");
                _ballBeliefs[n] = b;
            }
        }
    }

    public sealed partial class WorldSimulation
    {
        internal void RecordBallIntervention(string subject, int tick, string requested) =>
            _interventions.Add(new ResearchIntervention(tick, subject, "supported-ball", "Household", requested,
                "Ball observation or motor goal requested", "Researcher-selected task in the supported play area."));
    }
}
