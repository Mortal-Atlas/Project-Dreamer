using System;
using System.IO;

namespace DreamMachine.Sim
{
    public enum SpatialScenario : byte { Relocated, Familiar, MisleadingCue, Delayed, SimilarToy, Unavailable }
    public enum SpatialStage : byte { Introduction, Waiting, Searching, Found, Exhausted }
    public enum SpatialSelection : byte { None, LastSighting, VisibleLabel, NearestUnchecked }

    public readonly struct SpatialSearchView
    {
        public readonly SpatialStage Stage;
        public readonly SpatialSelection Selection;
        public readonly int Sighting, LastObservation, Expected, Observed, Selected, Visited, Negative, Candidates, Remaining, Blocked;
        internal SpatialSearchView(SpatialStage stage, SpatialSelection selection, int sighting, int observation, int expected,
            int observed, int selected, int visited, int negative, int candidates, int remaining, int blocked)
        {
            Stage = stage; Selection = selection; Sighting = sighting; LastObservation = observation;
            Expected = expected; Observed = observed; Selected = selected; Visited = visited;
            Negative = negative; Candidates = candidates; Remaining = remaining; Blocked = blocked;
        }

        // An operational confidence tag for the present expectation, derived from its evidence.
        public string Confidence => Observed >= 0 ? "Seen" : Expected >= 0 ? "Remembered" : "Unresolved";
    }

    public sealed partial class HouseholdLife
    {
        public const int FirstSearchBox = 14, SearchBoxCount = 3, SearchActor = 2;
        public const int TargetToy = 1, SimilarToy = 2;
        public const string TargetToyId = "household.toy.red-boat";
        public const string SimilarToyId = "household.toy.red-boat-striped";
        const int OpenLid = 4, AllBoxes = (1 << SearchBoxCount) - 1;
        const int InitialSearchTick = 320;
        SpatialScenario _scenario;
        SpatialStage _searchStage;
        SpatialSelection _selection;
        int _searchAt = InitialSearchTick, _origin, _cue = -1, _relocationTick = -1;
        int _sighting = -1, _observedEpisode = -1, _plan = -1;
        int _expected = -1, _observed = -1, _selected = -1, _visited, _negative, _candidates, _remaining, _blocked;
        bool _relocated, _pending;

        public SpatialScenario SearchScenario => _scenario;
        public int SpatialEnvironmentChangeTick => _relocationTick;
        public bool IsToyOutsideSearchArea => _scenario == SpatialScenario.Unavailable && _relocated;

        // Library I projects supported containment from an immutable Library III sighting.
        public (int Box, int Tick, int Evidence) ReadSupportedToyLocation()
        {
            int evidence = _searchStage == SpatialStage.Found ? _observedEpisode : _sighting;
            return evidence < 0 ? (-1, -1, -1) :
                (_memory[evidence].Target - FirstSearchBox, _memory[evidence].Tick, evidence);
        }
        public bool HasSearchLabel(int box) => _cue == box;
        public SpatialSearchView ReadSpatialSearch() => new SpatialSearchView(_searchStage, _selection, _sighting,
            _observedEpisode, _expected, _observed, _selected, _visited, _negative, _candidates, _remaining, _blocked);
        public (bool Open, int VisibleToy) ReadSearchBox(int box)
        {
            int value = _items[FirstSearchBox + box].Value;
            return (value >= OpenLid, value >= OpenLid ? value - OpenLid : 0);
        }

        public void ConfigureSpatialScenario(SpatialScenario scenario, int origin)
        {
            if (_world.TickCount != 0) throw new InvalidOperationException("Configure the scenario before running it.");
            if (!Enum.IsDefined(typeof(SpatialScenario), scenario)) throw new ArgumentOutOfRangeException(nameof(scenario));
            if ((uint)origin >= SearchBoxCount) throw new ArgumentOutOfRangeException(nameof(origin));
            _scenario = scenario;
            _origin = origin;
            for (int box = 0; box < SearchBoxCount; box++)
                _items[FirstSearchBox + box].Value = box == origin ? TargetToy : 0;
        }

        void BuildSearchBoxes()
        {
            _items[FirstSearchBox] = new Item(-1.67f, 2.01f, TargetToy);
            _items[FirstSearchBox + 1] = new Item(-.71f, 2.60f);
            _items[FirstSearchBox + 2] = new Item(.61f, 2.40f);
        }

        // Fixture events change the closed containers. The learner's selection receives no contents.
        void AdvanceSearchFixture(int tick)
        {
            if (_searchStage != SpatialStage.Waiting || _relocated) return;
            if (tick == _memory[_sighting].Tick + 31) SetSearchBox(_origin, TargetToy);
            if (tick < _searchAt) return;
            int destination = (_origin + 1 + (_people[SearchActor].Spec.BirthSeed & 1)) % SearchBoxCount;
            int other = AllBoxes ^ (1 << _origin) ^ (1 << destination);
            int distractor = other == 1 ? 0 : other == 2 ? 1 : 2;
            if (_scenario != SpatialScenario.Familiar)
            {
                _relocationTick = tick;
                SetSearchBox(_origin, 0);
                if (_scenario != SpatialScenario.Unavailable) SetSearchBox(destination, TargetToy);
                if (_scenario == SpatialScenario.SimilarToy) SetSearchBox(distractor, SimilarToy);
                if (_scenario == SpatialScenario.MisleadingCue || _scenario == SpatialScenario.SimilarToy) _cue = distractor;
            }
            else SetSearchBox(_origin, TargetToy);
            _relocated = true;
        }

        void SetSearchBox(int box, int value)
        {
            var item = _items[FirstSearchBox + box];
            item.Value = value;
            item.Revision++;
        }

        bool SelectSearch(int index, int tick)
        {
            if (index != SearchActor || tick < _searchAt) return false;
            Person person = _people[index];
            if (_searchStage == SpatialStage.Introduction)
            {
                Assign(person, HomeActivity.FindToy, FirstSearchBox + _origin, HomeReason.Curiosity, tick);
                return true;
            }
            if (_searchStage == SpatialStage.Waiting)
            {
                _searchStage = SpatialStage.Searching;
                _remaining = SearchBoxCount;
                _expected = _memory[_sighting].Target - FirstSearchBox;
                // The prior sighting is historical when this new retrieval starts.
                _observed = -1;
            }
            if (_searchStage != SpatialStage.Searching) return false;

            if (_pending)
            {
                Assign(person, HomeActivity.FindToy, FirstSearchBox + _selected, HomeReason.Search, tick);
                person.Source = _plan;
                return true;
            }
            _candidates = AllBoxes & ~(_visited | _blocked);
            if (_remaining == 0 || _candidates == 0)
            {
                FinishSearch(index, SpatialStage.Exhausted);
                return false;
            }
            _selection = SpatialSelection.LastSighting;
            _selected = _expected >= 0 && (_candidates & (1 << _expected)) != 0 ? _expected : -1;
            if (_selected < 0)
            {
                _selection = SpatialSelection.VisibleLabel;
                _selected = _cue;
                if (_selected < 0 || (_candidates & (1 << _selected)) == 0)
                {
                    _selection = SpatialSelection.NearestUnchecked;
                    _selected = NearestSearchBox(person, _candidates);
                }
            }
            int item = FirstSearchBox + _selected;
            _plan = Append(index, HomeActivity.FindToy, item, HomeEvidence.SearchCandidate,
                _observedEpisode, 0, false, _candidates | ((int)_selection << 3));
            Assign(person, HomeActivity.FindToy, item, HomeReason.Search, tick);
            person.Source = _plan;
            _pending = true;
            return true;
        }

        int NearestSearchBox(Person person, int candidates)
        {
            float best = float.PositiveInfinity;
            int nearest = -1;
            for (int box = 0; box < SearchBoxCount; box++)
            {
                if ((candidates & (1 << box)) == 0) continue;
                Item item = _items[FirstSearchBox + box];
                float distance = Distance2(person.X, person.Z, item.X, item.Z);
                if (distance < best) { nearest = box; best = distance; }
            }
            return nearest;
        }

        void InspectSearchBox(int index, int tick)
        {
            Person person = _people[index];
            int box = person.Item - FirstSearchBox;
            Item item = _items[person.Item];
            if (Distance2(person.X, person.Z, item.X, item.Z) > .15f)
            {
                InterruptSearch(index, tick, blocked: true);
                Rest(person, tick + 25, HomeReason.Blocked);
                return;
            }

            // Opening and inspecting are the only path from contents to an observation.
            if (item.Value < OpenLid) SetSearchBox(box, item.Value + OpenLid);
            int toy = item.Value - OpenLid;
            if (_searchStage == SpatialStage.Introduction)
            {
                _sighting = _observedEpisode = Append(index, HomeActivity.FindToy, person.Item,
                    HomeEvidence.ToySighting, -1, item.Revision, true, toy);
                _observed = box;
                _searchStage = SpatialStage.Waiting;
                _searchAt = tick + (_scenario == SpatialScenario.Delayed ? 1550 : 130);
            }
            else
            {
                _pending = false;
                _remaining--;
                _visited |= 1 << box;
                bool found = toy == TargetToy;
                _observedEpisode = Append(index, HomeActivity.FindToy, person.Item,
                    found ? HomeEvidence.ToyFound : HomeEvidence.ToyAbsent, _plan, item.Revision, true, toy);
                if (found)
                {
                    _observed = box;
                    FinishSearch(index, SpatialStage.Found);
                }
                else
                {
                    _negative |= 1 << box;
                    _expected = -1;
                    _candidates &= ~(1 << box);
                    if (_remaining == 0) FinishSearch(index, SpatialStage.Exhausted);
                }
            }
            Rest(person, tick + 32, HomeReason.Resting);
        }

        void FinishSearch(int index, SpatialStage stage)
        {
            _searchStage = stage;
            _expected = _observed;
            _candidates = 0;
            Append(index, HomeActivity.FindToy, _memory[_observedEpisode].Target, HomeEvidence.SearchOutcome,
                _observedEpisode, 0, false, (int)stage);
        }

        void InterruptSearch(int index, int tick, bool blocked = false)
        {
            if (_searchStage == SpatialStage.Searching)
            {
                // An interruption is not evidence that the container was empty.
                Append(index, HomeActivity.FindToy, _people[index].Item, HomeEvidence.SearchInterrupted,
                    _plan, 0, false);
                if (blocked)
                {
                    _pending = false;
                    _blocked |= 1 << _selected;
                    _candidates &= ~(1 << _selected);
                    _remaining--;
                }
            }
            _searchAt = tick + 25;
        }

        void WriteSearch(BinaryWriter writer)
        {
            writer.Write((byte)_scenario); writer.Write((byte)_searchStage); writer.Write((byte)_selection);
            writer.Write(_relocated); writer.Write(_pending);
            foreach (int value in new[] { _searchAt, _origin, _cue, _sighting, _observedEpisode, _plan,
                _expected, _observed, _selected, _visited, _negative, _candidates, _remaining, _blocked, _relocationTick })
                writer.Write(value);
        }

        void ReadSearch(BinaryReader reader)
        {
            _scenario = (SpatialScenario)reader.ReadByte();
            _searchStage = (SpatialStage)reader.ReadByte();
            _selection = (SpatialSelection)reader.ReadByte();
            _relocated = reader.ReadBoolean(); _pending = reader.ReadBoolean();
            _searchAt = reader.ReadInt32(); _origin = reader.ReadInt32(); _cue = reader.ReadInt32();
            _sighting = reader.ReadInt32(); _observedEpisode = reader.ReadInt32(); _plan = reader.ReadInt32();
            _expected = reader.ReadInt32(); _observed = reader.ReadInt32(); _selected = reader.ReadInt32();
            _visited = reader.ReadInt32(); _negative = reader.ReadInt32(); _candidates = reader.ReadInt32();
            _remaining = reader.ReadInt32(); _blocked = reader.ReadInt32(); _relocationTick = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(SpatialScenario), _scenario) || !Enum.IsDefined(typeof(SpatialStage), _searchStage) ||
                !Enum.IsDefined(typeof(SpatialSelection), _selection) || (uint)_origin >= SearchBoxCount || _searchAt < 0 ||
                !ValidBox(_cue) || !ValidBox(_expected) || !ValidBox(_observed) || !ValidBox(_selected) ||
                (uint)_remaining > SearchBoxCount || ((_visited | _negative | _candidates | _blocked) & ~AllBoxes) != 0 ||
                (_negative & ~_visited) != 0 || (_visited & _blocked) != 0 ||
                _relocationTick < -1 || _relocationTick > _world.TickCount ||
                (_relocationTick >= 0) != (_relocated && _scenario != SpatialScenario.Familiar) ||
                !ValidSearchReference(_sighting, HomeEvidence.ToySighting) ||
                !ValidSearchReference(_plan, HomeEvidence.SearchCandidate) ||
                !ValidSearchReference(_observedEpisode, HomeEvidence.ToySighting, HomeEvidence.ToyAbsent, HomeEvidence.ToyFound) ||
                (_pending && (_searchStage != SpatialStage.Searching || _selected < 0 || _plan < 0 ||
                    _memory[_plan].Target != _selected + FirstSearchBox || _remaining == 0)) ||
                (_searchStage != SpatialStage.Introduction && (_sighting < 0 || _observedEpisode < 0)))
                throw new InvalidDataException("Invalid spatial search state.");
            int targetCount = 0, similarCount = 0;
            for (int box = 0; box < SearchBoxCount; box++)
            {
                int value = _items[FirstSearchBox + box].Value;
                int toy = value & ~OpenLid;
                if (value < 0 || toy > SimilarToy) throw new InvalidDataException("Invalid container contents.");
                if (toy == TargetToy) targetCount++;
                if (toy == SimilarToy) similarCount++;
            }
            if (targetCount != (_scenario == SpatialScenario.Unavailable && _relocated ? 0 : 1) || similarCount > 1)
                throw new InvalidDataException("Invalid spatial object identity.");
        }

        static bool ValidBox(int box) => box >= -1 && box < SearchBoxCount;

        bool ValidSearchReference(int reference, params HomeEvidence[] kinds) => reference == -1 ||
            ((uint)reference < (uint)_memory.Count && _memory[reference].Actor == SearchActor &&
                Array.IndexOf(kinds, _memory[reference].Evidence) >= 0);

        bool ValidSearchRecord(HomeExperience e)
        {
            if (e.Activity != HomeActivity.FindToy || e.Actor != SearchActor || e.Target < FirstSearchBox) return false;
            switch (e.Evidence)
            {
                case HomeEvidence.ToySighting: return e.Changed && e.Details == TargetToy && e.Source == -1;
                case HomeEvidence.ToyFound: return e.Changed && e.Details == TargetToy && MatchesPlan(e);
                case HomeEvidence.ToyAbsent: return e.Changed && (e.Details == 0 || e.Details == SimilarToy) && MatchesPlan(e);
                case HomeEvidence.SearchCandidate: return !e.Changed && e.Source >= 0 &&
                    ValidSearchReference(e.Source, HomeEvidence.ToySighting, HomeEvidence.ToyAbsent) &&
                    (e.Details & (1 << (e.Target - FirstSearchBox))) != 0 &&
                    (uint)((e.Details >> 3) - 1) < 3;
                case HomeEvidence.SearchInterrupted: return !e.Changed && e.Details == 0 && MatchesPlan(e);
                case HomeEvidence.SearchOutcome: return !e.Changed && e.Source >= 0 && e.Target == _memory[e.Source].Target &&
                    (e.Details == (int)SpatialStage.Found && _memory[e.Source].Evidence == HomeEvidence.ToyFound ||
                    e.Details == (int)SpatialStage.Exhausted && ValidSearchReference(e.Source, HomeEvidence.ToyAbsent, HomeEvidence.ToySighting));
                case HomeEvidence.Replay: return !e.Changed && e.Details == 0 && e.Source >= 0;
                default: return false;
            }
        }

        bool MatchesPlan(HomeExperience e) => e.Source >= 0 && e.Target == _memory[e.Source].Target &&
            _memory[e.Source].Evidence == HomeEvidence.SearchCandidate;
    }
}
