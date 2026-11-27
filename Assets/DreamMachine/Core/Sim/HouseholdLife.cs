using System;
using System.Collections.Generic;
using System.IO;
using DreamMachine.Social;
using DreamMachine.World;

namespace DreamMachine.Sim
{
    public enum HomeActivity { Rest, Inspect, Roll, Stack, Open, Close, Watch, Eat, Drink, Sleep, Sort, Tidy, FindToy }
    public enum HomePhase { Resting, Approach, Act, Observe, Scan }
    public enum HomeEvidence { Firsthand, Demonstration, Witnessed, Imitation, Failed, Replay, SocketDecision, ToySighting, SearchCandidate, ToyAbsent, ToyFound, SearchInterrupted, SearchOutcome }
    public enum HomeReason { Resting, Curiosity, Practice, ObservedExample, Teach, Hunger, Thirst, Night, Blocked, Search }

    public readonly struct HomeExperience
    {
        public readonly int Tick, Actor, Target, Source, Revision, Details;
        public readonly HomeActivity Activity;
        public readonly HomeEvidence Evidence;
        public readonly bool Changed;

        internal HomeExperience(int tick, int actor, HomeActivity activity, int target, HomeEvidence evidence, int source, int revision, bool changed, int details = 0)
        {
            Tick = tick; Actor = actor; Activity = activity; Target = target; Evidence = evidence;
            Source = source; Revision = revision; Changed = changed; Details = details;
        }
    }

    public readonly struct HomeCompetence
    {
        public readonly int IndependentAttempts, IndependentSuccesses, Witnesses, Imitations, Failures, LastIndependentSuccess;
        internal HomeCompetence(int attempts, int successes, int witnesses, int imitations, int failures, int lastSuccess)
        { IndependentAttempts = attempts; IndependentSuccesses = successes; Witnesses = witnesses; Imitations = imitations; Failures = failures; LastIndependentSuccess = lastSuccess; }
    }

    public readonly struct HomePersonView
    {
        public readonly float X, Z, Yaw, Thirst;
        public readonly HomeActivity Activity, Prediction, Knowledge;
        public readonly HomePhase Phase;
        public readonly HomeReason Reason;
        public readonly int Item, Person, Attempts, Successes, Observed, Imitated, Last, Replay, KnowledgeEvidence;

        internal HomePersonView(HouseholdLife.Person p)
        {
            X = p.X; Z = p.Z; Yaw = p.Yaw; Thirst = p.Thirst;
            Activity = p.Activity; Prediction = p.Prediction; Knowledge = p.Knowledge;
            Phase = p.Phase; Reason = p.Reason; Item = p.Item; Person = p.Watch;
            Attempts = p.Attempts; Successes = p.Successes; Observed = p.Observed; Imitated = p.Imitated;
            Last = p.Last; Replay = p.Replay; KnowledgeEvidence = p.KnowledgeEvidence;
        }
    }

    /// <summary>Fixed-tick household affordances. All contacts here are kinematic references.</summary>
    public sealed partial class HouseholdLife
    {
        const int PreviousItemCount = 12;
        const int SocketItemCount = 14;
        public const int ItemCount = 17;
        public const int SortingBoard = 12, ToyBin = 13;
        static readonly string[] Names = { "Picture cards", "Ball", "Wooden blocks", "Lidded box", "Food", "Water", "Left seat", "Right seat", "Merlin's bed", "Marie's bed", "Arthur's bed", "Alice's bed", "Shape board", "Toy bin", "Cedar chest", "Blue basket", "Green trunk" };
        static readonly (int Item, HomeActivity Activity)[] Play = { (0, HomeActivity.Inspect), (1, HomeActivity.Roll),
            (2, HomeActivity.Stack), (3, HomeActivity.Open), (SortingBoard, HomeActivity.Sort), (ToyBin, HomeActivity.Tidy) };
        readonly WorldSimulation _world;
        readonly Person[] _people = new Person[4];
        readonly Item[] _items = new Item[ItemCount];
        readonly List<HomeExperience> _memory = new List<HomeExperience>();
        bool _wasNight;

        public int PersonCount => _people.Length;
        public int ExperienceCount => _memory.Count;
        public HomeExperience Experience(int index) => _memory[index];
        public int LastSocketDecision(int index) => _people[index].LastScan;
        public int ActiveSocketDecision(int index) => _people[index].ActiveScan;
        public HomePersonView ReadPerson(int index) => new HomePersonView(_people[index]);
        public HomeCompetence ReadCompetence(int actor, int item)
        {
            Person p = _people[actor];
            return new HomeCompetence(p.SoloAttempts[item], p.SoloSuccesses[item], p.Witnesses[item], p.Copies[item],
                p.Failed[item], p.LastSuccess[item]);
        }
        public string PersonId(int index) => _people[index].Spec.Id;
        public static string ItemName(int index) => Names[index];
        public (float X, float Z, int Value, int Revision) ReadItem(int index)
        {
            var i = _items[index];
            return (i.X, i.Z, i.Value, i.Revision);
        }

        internal HouseholdLife(WorldSimulation world)
        {
            _world = world;
            string[] ids = { "father", "mother", "son", "daughter" };
            for (int i = 0; i < ids.Length; i++)
            {
                PersonSpec spec = world.Population.Find(ids[i]);
                SimChild child = world.Find(ids[i]);
                _people[i] = new Person(spec, child, i % 2 == 0 ? -1.72f : 1.72f);
            }
            _items[0] = new Item(-1.8f, 1.00f);
            _items[1] = new Item(-.56f, 1.88f);
            _items[2] = new Item(.60f, 1.84f);
            _items[3] = new Item(1.68f, .94f);
            _items[4] = new Item(.55f, 3.05f, 6);
            _items[5] = new Item(-.55f, 3.05f, 8);
            _items[6] = new Item(-1.85f, 2.50f);
            _items[7] = new Item(1.87f, 2.50f);
            _items[8] = new Item(-2.08f, -.23f);
            _items[9] = new Item(2.08f, -.23f);
            _items[10] = new Item(-2.86f, -.09f);
            _items[11] = new Item(2.86f, -.09f);
            _items[SortingBoard] = new Item(.95f, .53f);
            _items[ToyBin] = new Item(-.87f, .68f);
            BuildSearchBoxes();
        }

        public int FindPerson(string id)
        {
            for (int i = 0; i < _people.Length; i++) if (_people[i].Spec.Id == id) return i;
            return -1;
        }

        internal void Step(bool night)
        {
            int tick = _world.TickCount;
            AdvanceSearchFixture(tick);
            var ball = _items[1];
            if (Math.Abs(ball.V) > .001f)
            {
                float next = ball.X + ball.V * WorldSimulation.Dt;
                ball.X = Math.Max(-1.20f, Math.Min(1.23f, next));
                ball.V = next == ball.X ? ball.V * .95f : 0f;
            }
            if (_wasNight != night && !night)
            {
                Refill(4, 6);
                Refill(5, 8);
            }
            for (int index = 0; index < _people.Length; index++)
            {
                Person p = _people[index];
                p.Thirst = Math.Min(1f, p.Thirst + .0005f);
                p.Eating = false;
                if (index == SearchActor && _world.PhysicalPractice?.Active == true) continue;
                if (night && p.Activity != HomeActivity.Sleep)
                {
                    if (p.Activity == HomeActivity.FindToy) InterruptSearch(index, tick);
                    else if (p.Phase == HomePhase.Approach || p.Phase == HomePhase.Act || p.Phase == HomePhase.Scan)
                        Record(index, HomeEvidence.Failed, p.Source, false);
                    Assign(p, HomeActivity.Sleep, 8 + index, HomeReason.Night, tick);
                    if (p.LastActual >= 0)
                    {
                        p.Replay = p.LastActual;
                        var remembered = _memory[p.Replay];
                        Append(index, remembered.Activity, remembered.Target, HomeEvidence.Replay, p.Replay, remembered.Revision, false);
                        p.Child?.State.Library.NoteIdea("home:replay:" + remembered.Activity, "revisit " + ItemName(remembered.Target),
                            .25f, tick, "library-II-household-replay");
                    }
                }
                if (!night && (p.Activity == HomeActivity.Sleep || (p.Phase == HomePhase.Resting && tick >= p.Until)))
                    Select(index, tick);
                if (p.Phase == HomePhase.Approach) Approach(index, tick);
                if (p.Phase == HomePhase.Scan && tick >= p.Until) ScanSockets(index, tick);
                if (p.Phase == HomePhase.Act && tick >= p.Until) Execute(index, tick);
                if (p.Phase == HomePhase.Observe)
                {
                    if (tick >= p.Until) Rest(p, tick + 8, HomeReason.Resting);
                    else Face(p, _people[p.Watch].X, _people[p.Watch].Z);
                }
                if (p.Child != null) { p.Child.X = p.X; p.Child.Z = p.Z; p.Child.Yaw = p.Yaw; }
            }
            _wasNight = night;
        }

        void Refill(int item, int quantity)
        {
            if (_items[item].Value == quantity) return;
            _items[item].Value = quantity;
            _items[item].Revision++;
        }

        void Select(int index, int tick)
        {
            Person p = _people[index];
            if (p.Child != null && p.Child.Body.Hunger > .43f && _items[4].Value > 0 && Available(index, 4))
            { Assign(p, HomeActivity.Eat, 4, HomeReason.Hunger, tick); return; }
            if (p.Thirst > .42f && _items[5].Value > 0 && Available(index, 5))
            { Assign(p, HomeActivity.Drink, 5, HomeReason.Thirst, tick); return; }
            if (p.Child != null && p.Child.Body.Energy < .21f && tick >= p.RestAllowedAfter)
            {
                p.RestAllowedAfter = tick + 220;
                Assign(p, HomeActivity.Rest, 6 + index % 2, HomeReason.Resting, tick);
                return;
            }

            if (SelectSearch(index, tick)) return;

            // Recent observed actions are candidates. They never increment independent practice.
            if (p.Child != null && p.ObservedSource >= 0 && p.ObservedSource != p.LastImitation && tick - _memory[p.ObservedSource].Tick < 220)
            {
                HomeExperience model = _memory[p.ObservedSource];
                if (model.Activity != HomeActivity.Open && model.Activity != HomeActivity.Close && Available(index, model.Target))
                {
                    Assign(p, model.Activity, model.Target, HomeReason.ObservedExample, tick);
                    p.Source = p.ObservedSource;
                    p.LastImitation = p.Source;
                    return;
                }
            }
            int teacher = index - 2;
            if (p.Child != null && p.LastWatched != _people[teacher].Sequence && _people[teacher].Phase == HomePhase.Act &&
                _people[teacher].Reason == HomeReason.Teach &&
                Distance2(p.X, p.Z, _people[teacher].X, _people[teacher].Z) <= 9f)
            {
                p.Activity = HomeActivity.Watch; p.Phase = HomePhase.Observe; p.Reason = HomeReason.Teach;
                p.Watch = teacher; p.Item = -1; p.Until = tick + 34;
                p.LastWatched = _people[teacher].Sequence;
                return;
            }
            float best = float.NegativeInfinity;
            int chosen = -1;
            HomeActivity activity = HomeActivity.Rest;
            foreach (var candidate in Play)
            {
                int item = candidate.Item;
                if (!Available(index, item) || (item == ToyBin && _items[2].Value == 0)) continue;
                Item target = _items[item];
                int preference = (p.Spec.BirthSeed + 3 * item) & 7;
                float novelty = p.Practice[item] == 0 ? .30f : 0f;
                float retained = p.Replay >= 0 && _memory[p.Replay].Target == item ? .16f : 0f;
                float curiosity = p.Child == null ? .45f : p.Child.State.Emotions.Curiosity;
                float evidenceInterest = Math.Min(.24f, .05f * p.SoloSuccesses[item]) - Math.Min(.35f, .08f * p.Failed[item]);
                float score = preference * .07f + curiosity * novelty + retained -
                    Math.Min(2f, Distance2(p.X, p.Z, target.X, target.Z) * .13f) -
                    (p.LastItem == item ? 1.4f : 0f) - Math.Min(.55f, p.Practice[item] * .025f) + evidenceInterest;
                if (score > best) { best = score; chosen = item; activity = candidate.Activity; }
            }
            if (chosen < 0) { Rest(p, tick + 14, HomeReason.Resting); return; }
            if (chosen == 3 && _items[3].Value == 1) activity = HomeActivity.Close;
            bool demonstrate = p.Child == null && UsefulDemonstration(index, chosen, tick);
            Assign(p, activity, chosen, demonstrate ? HomeReason.Teach : p.Practice[chosen] == 0 ? HomeReason.Curiosity : HomeReason.Practice, tick);
        }

        bool UsefulDemonstration(int teacher, int item, int tick)
        {
            var learner = _people[teacher + 2];
            var actor = _people[teacher];
            int threshold = learner.Failed[item] > learner.SoloSuccesses[item] ? 3 : 2;
            return learner.SoloSuccesses[item] < threshold && tick - actor.LastDemo[item] >= 230 &&
                Distance2(learner.X, learner.Z, _items[item].X, _items[item].Z) <= 5f;
        }

        bool Available(int index, int item)
        {
            for (int i = 0; i < _people.Length; i++)
                if (i != index && _people[i].Item == item && _people[i].Phase != HomePhase.Resting) return false;
            return true;
        }

        static void Assign(Person p, HomeActivity activity, int item, HomeReason reason, int tick)
        {
            p.Activity = p.Prediction = activity; p.Phase = HomePhase.Approach; p.Reason = reason; p.Item = item;
            p.Watch = p.Source = -1; p.Until = tick + 240; p.Sequence++;
            p.ActiveScan = -1;
        }

        static void Rest(Person p, int until, HomeReason reason)
        {
            p.Activity = HomeActivity.Rest; p.Phase = HomePhase.Resting; p.Reason = reason;
            p.Item = p.Watch = p.Source = -1; p.Until = until;
            p.ActiveScan = -1;
        }

        void Approach(int index, int tick)
        {
            Person p = _people[index];
            Item target = _items[p.Item];
            float dx = target.X - p.X, dz = target.Z - p.Z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            Face(p, target.X, target.Z);
            float reach = p.Activity == HomeActivity.Sleep || p.Activity == HomeActivity.Rest ? .10f : p.Child == null ? .54f : .32f;
            if (distance <= reach + .004f)
            {
                bool inspect = p.Activity == HomeActivity.Sort && p.Child != null;
                p.Phase = inspect ? HomePhase.Scan : HomePhase.Act;
                p.Until = tick + (inspect ? 4 : p.Activity == HomeActivity.Sleep ? 2000 : 18);
                return;
            }
            if (tick >= p.Until)
            {
                if (p.Activity == HomeActivity.FindToy) InterruptSearch(index, tick, blocked: true);
                else Record(index, HomeEvidence.Failed, p.Source, false);
                Rest(p, tick + 25, HomeReason.Blocked);
                return;
            }
            // A route around the front of the fixed house also recovers old saves behind it.
            if (YardClearance.HouseMotionFraction(p.X, p.Z, dx, dz, .15f) < .99f)
            {
                float edge = p.X < 0f ? -1.52f : 1.52f;
                if (Math.Abs(p.X) < 1.45f) { dx = edge - p.X; dz = 0f; }
                else { dx = edge - p.X; dz = Math.Max(.25f, target.Z) - p.Z; }
                distance = (float)Math.Sqrt(dx * dx + dz * dz);
            }
            if (distance <= .001f) return;
            float advance = Math.Min((p.Child == null ? .98f : .56f) * WorldSimulation.Dt, distance);
            dx *= advance / distance;
            dz *= advance / distance;
            float clear = YardClearance.HouseMotionFraction(p.X, p.Z, dx, dz, .13f);
            p.X += dx * clear; p.Z += dz * clear;
        }

        void ScanSockets(int index, int tick)
        {
            Person p = _people[index];
            Item board = _items[SortingBoard];
            int cue = ShapeBoard.Cue(board.Value), marker = ShapeBoard.Rotation(board.Value);
            int previousId = p.SortSources[cue], memorySlot = -1, memoryMarker = 7;
            if (previousId >= 0)
            {
                var previous = _memory[previousId];
                var observed = new SocketDecision(previous.Details);
                memorySlot = previous.Details == 0 ? ShapeBoard.Socket(cue, ShapeBoard.Rotation(previous.Revision - 1)) : observed.Choice;
                if (previous.Details != 0) memoryMarker = observed.Marker;
            }
            var failure = new SocketDecision(p.LastSortFailure[cue]);
            int rejected = failure.Data != 0 && failure.Marker == marker ? failure.Choice : -1;
            int explore = (int)((uint)(p.Spec.BirthSeed + p.Failed[SortingBoard] + p.SoloSuccesses[SortingBoard]) % 3);
            var decision = SocketInspection.Choose(SocketPolicy.Contextual, SocketFeatures.Shape(cue), marker,
                memorySlot, memoryMarker, rejected, explore, slot => SocketInspection.ObserveBoard(marker, slot));
            p.ActiveScan = Append(index, HomeActivity.Sort, SortingBoard, HomeEvidence.SocketDecision, previousId, board.Revision, false, decision.Data);
            p.Phase = HomePhase.Act;
            p.Until = tick + 18 + decision.Observations * SocketInspection.InspectionTicks;
        }

        void Execute(int index, int tick)
        {
            Person p = _people[index];
            Item target = _items[p.Item];
            if (p.Activity == HomeActivity.FindToy) { InspectSearchBox(index, tick); return; }
            if (p.Activity == HomeActivity.Sleep) { p.Until = tick + 2000; return; }
            if (p.Activity == HomeActivity.Rest) { p.Phase = HomePhase.Resting; p.Until = tick + 40; return; }
            bool changed = false;
            int appliedSource = p.Source;
            int details = 0;
            bool reached = Distance2(p.X, p.Z, target.X, target.Z) <= (p.Child == null ? .34f : .15f);
            bool success = reached;
            if (reached)
            {
                switch (p.Activity)
                {
                    case HomeActivity.Inspect: break;
                    case HomeActivity.Roll:
                        target.V = target.X >= .54f ? -.68f : .68f; changed = true; break;
                    case HomeActivity.Stack:
                        target.Value = target.Value == 2 ? 0 : target.Value + 1; changed = true; break;
                    case HomeActivity.Sort:
                        int cue = ShapeBoard.Cue(target.Value), rotation = ShapeBoard.Rotation(target.Value);
                        int guess = p.Child == null ? ShapeBoard.Socket(cue, rotation) : (int)((uint)(p.Spec.BirthSeed + p.Failed[p.Item] + p.SoloSuccesses[p.Item]) % 3);
                        if (p.ActiveScan >= 0)
                        {
                            details = _memory[p.ActiveScan].Details;
                            guess = new SocketDecision(details).Choice;
                            if (p.Source < 0) appliedSource = p.ActiveScan;
                        }
                        success = changed = ShapeBoard.Place(ref target.Value, guess);
                        break;
                    case HomeActivity.Tidy:
                        success = changed = _items[2].Value > 0;
                        if (changed) { target.Value += _items[2].Value; _items[2].Value = 0; _items[2].Revision++; }
                        break;
                    case HomeActivity.Open:
                    case HomeActivity.Close:
                        int next = p.Activity == HomeActivity.Open ? 1 : 0;
                        success = changed = target.Value != next;
                        if (changed) target.Value = next;
                        break;
                    case HomeActivity.Eat:
                    case HomeActivity.Drink:
                        success = changed = target.Value > 0;
                        if (changed)
                        {
                            target.Value--;
                            if (p.Activity == HomeActivity.Eat) p.Eating = true;
                            else p.Thirst = Math.Max(0f, p.Thirst - .55f);
                        }
                        break;
                }
            }
            if (changed) target.Revision++;
            p.Attempts++;
            HomeEvidence evidence = !success ? HomeEvidence.Failed : p.Source >= 0 ? HomeEvidence.Imitation :
                p.Child == null && p.Reason == HomeReason.Teach ? HomeEvidence.Demonstration : HomeEvidence.Firsthand;
            int eventIndex = Record(index, evidence, success ? appliedSource : p.Source, changed, details);
            if (success)
            {
                p.Successes++;
                p.Practice[p.Item]++;
                if (p.Source >= 0) p.Imitated++;
                if (p.KnowledgeEvidence < 0 || p.Practice[p.Item] >= 2)
                { p.Knowledge = p.Activity; p.KnowledgeEvidence = eventIndex; }
            }
            if (evidence == HomeEvidence.Demonstration)
            {
                for (int i = 2; i < _people.Length; i++)
                {
                    var learner = _people[i];
                    bool facing = learner.Watch == index || IsFacing(learner, p.X, p.Z);
                    if (!facing || Distance2(learner.X, learner.Z, p.X, p.Z) > 4f ||
                        YardClearance.HouseMotionFraction(learner.X, learner.Z, p.X - learner.X, p.Z - learner.Z, 0) < .99f) continue;
                    learner.Observed++;
                    learner.ObservedSource = Append(i, p.Activity, p.Item, HomeEvidence.Witnessed, eventIndex, target.Revision, false);
                    if (learner.Phase == HomePhase.Observe) learner.Until = Math.Min(learner.Until, tick + 10);
                }
            }
            p.LastItem = p.Item;
            Rest(p, tick + 10, success ? HomeReason.Resting : HomeReason.Blocked);
        }

        int Record(int index, HomeEvidence evidence, int source, bool changed, int details = 0)
        {
            Person p = _people[index];
            return Append(index, p.Activity, p.Item, evidence, source, _items[p.Item].Revision, changed, details);
        }

        // Library III household evidence. Indexes are immutable references for imitation and replay.
        int Append(int index, HomeActivity activity, int item, HomeEvidence kind, int source, int revision, bool changed, int details = 0)
        {
            int id = _memory.Count;
            var record = new HomeExperience(_world.TickCount, index, activity, item, kind, source, revision, changed, details);
            _memory.Add(record);
            IndexEvidence(record, id);
            _people[index].Last = id;
            if (IsLivedEvidence(kind)) _people[index].LastActual = id;
            return id;
        }

        static bool IsLivedEvidence(HomeEvidence kind) => kind != HomeEvidence.Replay &&
            kind != HomeEvidence.SocketDecision && kind != HomeEvidence.SearchCandidate && kind != HomeEvidence.SearchOutcome;

        void IndexEvidence(HomeExperience record, int id)
        {
            Person p = _people[record.Actor];
            int item = record.Target;
            switch (record.Evidence)
            {
                case HomeEvidence.Firsthand:
                    p.SoloAttempts[item]++;
                    p.SoloSuccesses[item]++;
                    p.LastSuccess[item] = id;
                    if (item == SortingBoard && record.Changed && record.Revision > 0)
                        p.SortSources[ShapeBoard.Cue(record.Revision - 1)] = id;
                    break;
                case HomeEvidence.Failed:
                    p.Failed[item]++;
                    if (item == SortingBoard && record.Details != 0)
                        p.LastSortFailure[ShapeBoard.Cue(record.Revision)] = record.Details;
                    if (record.Source < 0) p.SoloAttempts[item]++;
                    break;
                case HomeEvidence.SocketDecision: p.LastScan = id; break;
                case HomeEvidence.Witnessed: p.Witnesses[item]++; break;
                case HomeEvidence.Imitation: p.Copies[item]++; break;
                case HomeEvidence.Demonstration: p.LastDemo[item] = record.Tick; break;
            }
        }

        static float Distance2(float x, float z, float tx, float tz) => (tx - x) * (tx - x) + (tz - z) * (tz - z);
        static void Face(Person p, float x, float z)
        {
            if (Distance2(p.X, p.Z, x, z) > .00001f) p.Yaw = (float)(Math.Atan2(x - p.X, z - p.Z) * 180 / Math.PI);
        }
        static bool IsFacing(Person p, float x, float z)
        {
            float angle = (float)(Math.Atan2(x - p.X, z - p.Z) * 180 / Math.PI);
            return Math.Abs(Math.IEEERemainder(angle - p.Yaw, 360)) < 65;
        }

        internal bool Ate(string id)
        {
            int i = FindPerson(id);
            return i >= 0 && _people[i].Eating;
        }

        internal void ReplaceLearner(SimChild learner)
        {
            int index = FindPerson(learner.Spec.Id);
            if (index < 2) return;
            _people[index] = new Person(learner.Spec, learner, learner.X);
            for (int i = 0; i < _memory.Count; i++)
                if (_memory[i].Actor == index) IndexEvidence(_memory[i], i);
        }

        internal sealed class Person
        {
            public readonly PersonSpec Spec;
            public readonly SimChild Child;
            public float X, Z, Yaw, Thirst = .15f;
            public HomeActivity Activity, Prediction, Knowledge;
            public HomePhase Phase;
            public HomeReason Reason;
            public int Item = -1, Watch = -1, Until, Source = -1, Last = -1, LastActual = -1, Replay = -1, Sequence,
                Attempts, Successes, Observed, Imitated, LastItem = -1, ObservedSource = -1, LastImitation = -1, LastWatched = -1, KnowledgeEvidence = -1,
                RestAllowedAfter;
            public bool Eating;
            public readonly int[] Practice = new int[ItemCount];
            public readonly int[] SoloAttempts = new int[ItemCount], SoloSuccesses = new int[ItemCount], Witnesses = new int[ItemCount],
                Copies = new int[ItemCount], Failed = new int[ItemCount], LastSuccess = new int[ItemCount], LastDemo = new int[ItemCount];
            public readonly int[] SortSources = { -1, -1, -1 };
            public readonly int[] LastSortFailure = new int[3];
            public int ActiveScan = -1, LastScan = -1;
            public Person(PersonSpec spec, SimChild child, float x)
            {
                Spec = spec; Child = child; X = child?.X ?? x; Z = child?.Z ?? .13f; Yaw = child?.Yaw ?? 0f;
                Array.Fill(LastSuccess, -1); Array.Fill(LastDemo, -230);
            }
        }

        sealed class Item
        {
            public float X, Z, V;
            public int Value, Revision;
            public Item(float x, float z, int value = 0) { X = x; Z = z; Value = value; }
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(_wasNight);
            foreach (Item item in _items)
            { writer.Write(item.X); writer.Write(item.Z); writer.Write(item.V); writer.Write(item.Value); writer.Write(item.Revision); }
            foreach (Person p in _people)
            {
                writer.Write(p.X); writer.Write(p.Z); writer.Write(p.Yaw); writer.Write(p.Thirst);
                writer.Write((byte)p.Activity); writer.Write((byte)p.Prediction); writer.Write((byte)p.Knowledge);
                writer.Write((byte)p.Phase); writer.Write((byte)p.Reason);
                foreach (int value in new[] { p.Item, p.Watch, p.Until, p.Source, p.Last, p.LastActual, p.Replay, p.Sequence,
                    p.Attempts, p.Successes, p.Observed, p.Imitated, p.LastItem, p.ObservedSource, p.LastImitation, p.LastWatched, p.KnowledgeEvidence, p.RestAllowedAfter })
                    writer.Write(value);
                foreach (int practice in p.Practice) writer.Write(practice);
                writer.Write(p.ActiveScan);
            }
            writer.Write(_memory.Count);
            foreach (var e in _memory)
            { writer.Write(e.Tick); writer.Write((byte)e.Actor); writer.Write((byte)e.Activity); writer.Write((byte)e.Target); writer.Write((byte)e.Evidence); writer.Write(e.Source); writer.Write(e.Revision); writer.Write(e.Changed); writer.Write(e.Details); }
            WriteSearch(writer);
        }

        internal void Read(BinaryReader reader, int version)
        {
            _wasNight = reader.ReadBoolean();
            int itemCount = version == 2 ? PreviousItemCount : version < 5 ? SocketItemCount : ItemCount;
            for (int i = 0; i < itemCount; i++)
            { Item item = _items[i]; item.X = ReadFinite(reader); item.Z = ReadFinite(reader); item.V = ReadFinite(reader); item.Value = reader.ReadInt32(); item.Revision = reader.ReadInt32(); }
            foreach (Person p in _people)
            {
                p.X = ReadFinite(reader); p.Z = ReadFinite(reader); p.Yaw = ReadFinite(reader); p.Thirst = ReadFinite(reader);
                p.Activity = (HomeActivity)reader.ReadByte(); p.Prediction = (HomeActivity)reader.ReadByte(); p.Knowledge = (HomeActivity)reader.ReadByte();
                p.Phase = (HomePhase)reader.ReadByte(); p.Reason = (HomeReason)reader.ReadByte();
                p.Item = reader.ReadInt32(); p.Watch = reader.ReadInt32(); p.Until = reader.ReadInt32(); p.Source = reader.ReadInt32();
                p.Last = reader.ReadInt32(); p.LastActual = reader.ReadInt32(); p.Replay = reader.ReadInt32(); p.Sequence = reader.ReadInt32();
                p.Attempts = reader.ReadInt32(); p.Successes = reader.ReadInt32(); p.Observed = reader.ReadInt32(); p.Imitated = reader.ReadInt32();
                p.LastItem = reader.ReadInt32(); p.ObservedSource = reader.ReadInt32(); p.LastImitation = reader.ReadInt32();
                p.LastWatched = reader.ReadInt32(); p.KnowledgeEvidence = reader.ReadInt32(); p.RestAllowedAfter = reader.ReadInt32();
                for (int i = 0; i < itemCount; i++) p.Practice[i] = reader.ReadInt32();
                if (version >= 4) p.ActiveScan = reader.ReadInt32();
                if (p.Item < -1 || p.Item >= ItemCount || p.Watch < -1 || p.Watch >= PersonCount ||
                    !Enum.IsDefined(typeof(HomePhase), p.Phase) || !Enum.IsDefined(typeof(HomeActivity), p.Activity) ||
                    ((p.Phase == HomePhase.Approach || p.Phase == HomePhase.Act || p.Phase == HomePhase.Scan) && p.Item < 0) ||
                    (p.Phase == HomePhase.Scan && p.Item != SortingBoard) || (p.Phase == HomePhase.Observe && p.Watch < 0))
                    throw new InvalidDataException("Invalid household activity.");
            }
            int count = reader.ReadInt32();
            if (count < 0 || count > (reader.BaseStream.Length - reader.BaseStream.Position) / (version >= 4 ? 21 : 17))
                throw new InvalidDataException("Invalid household memory length.");
            for (int i = 0; i < count; i++)
            {
                var e = new HomeExperience(reader.ReadInt32(), reader.ReadByte(), (HomeActivity)reader.ReadByte(), reader.ReadByte(),
                    (HomeEvidence)reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadBoolean(),
                    version >= 4 ? reader.ReadInt32() : 0);
                if (e.Actor >= PersonCount || e.Target >= itemCount || e.Source < -1 || e.Source >= i || e.Tick < 0 || e.Tick > _world.TickCount)
                    throw new InvalidDataException("Invalid household memory reference.");
                if (!Enum.IsDefined(typeof(HomeActivity), e.Activity) || !Enum.IsDefined(typeof(HomeEvidence), e.Evidence) ||
                    ((e.Activity == HomeActivity.FindToy || e.Evidence >= HomeEvidence.ToySighting) &&
                        (version < 5 || !ValidSearchRecord(e))))
                    throw new InvalidDataException("Invalid household spatial record.");
                if (e.Activity != HomeActivity.FindToy && e.Details != 0 && (e.Activity != HomeActivity.Sort || e.Target != SortingBoard || new SocketDecision(e.Details).Choice >= 3))
                    throw new InvalidDataException("Invalid household socket decision.");
                if (e.Evidence == HomeEvidence.SocketDecision && (e.Details == 0 || e.Changed))
                    throw new InvalidDataException("Invalid household observation.");
                _memory.Add(e);
                IndexEvidence(e, i);
            }
            foreach (Person p in _people)
            {
                foreach (int reference in new[] { p.Source, p.Last, p.LastActual, p.Replay, p.ObservedSource, p.LastImitation, p.KnowledgeEvidence, p.ActiveScan })
                    if (reference < -1 || reference >= count) throw new InvalidDataException("Invalid household memory index.");
                if (p.ActiveScan >= 0)
                {
                    var inspection = _memory[p.ActiveScan];
                    if (inspection.Evidence != HomeEvidence.SocketDecision || _people[inspection.Actor] != p ||
                        p.Phase != HomePhase.Act || p.Activity != HomeActivity.Sort || p.Item != SortingBoard ||
                        inspection.Revision != _items[SortingBoard].Revision)
                        throw new InvalidDataException("Invalid active household observation.");
                }
            }
            if (version >= 5) ReadSearch(reader);
        }

        static float ReadFinite(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Invalid household coordinate.");
            return value;
        }
    }
}
