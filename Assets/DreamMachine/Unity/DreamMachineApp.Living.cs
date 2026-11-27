using System.Text;
using DreamMachine.Body;
using DreamMachine.Sim;
using DreamMachine.Social;
using UnityEngine;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    public sealed partial class DreamMachineApp
    {
        const string LifeTray = "household.experience";
        [SerializeField] bool LegacyHouseholdPresentation;
        readonly Transform[] _livingItems = new Transform[HouseholdLife.ItemCount];
        readonly Actor[] _livingActors = new Actor[4];
        Transform _lid, _revealed, _stackA, _stackB, _food, _water;
        Transform _lidGrip;
        Transform _storedToys, _boardMark;
        readonly Transform[] _socketMarks = new Transform[3];
        static readonly Vector3[] SocketPositions = { new Vector3(-.16f, .107f, -.03f),
            new Vector3(0, .107f, -.03f), new Vector3(.16f, .107f, -.03f) };
        readonly Transform[] _sortPieces = new Transform[3];
        readonly Transform[] _searchLids = new Transform[HouseholdLife.SearchBoxCount];
        readonly Transform[] _searchLabels = new Transform[HouseholdLife.SearchBoxCount];
        Transform _redBoat, _stripedBoat;
        Text _lifeText;
        int _lifeFocus = 2, _historyDaysBack;
        bool _researchPresentation;

        void EnableLivingPresentation()
        {
            bool wasLegacy = _sim.Home == null;
            if (!LegacyHouseholdPresentation && _sim.EnableHouseholdLife() && wasLegacy && System.IO.File.Exists(WorldPath()))
                _archiveHouseholdBaseline = true;
        }

        static string LivingName(PersonSpec spec) => spec.DisplayName == "Father" ? "Merlin" : spec.DisplayName == "Mother" ? "Marie" :
            spec.DisplayName == "Son" ? "Arthur" : spec.DisplayName == "Daughter" ? "Alice" : spec.DisplayName;

        void BuildLivingRoom()
        {
            var root = new GameObject("Household play and rest area").transform;
            Piece(root, PrimitiveType.Cube, "Terrace", new Vector3(0, -.055f, 1.45f), new Vector3(6.60f, .10f, 4.55f), "#C4B59A");
            Piece(root, PrimitiveType.Cube, "Play mat", new Vector3(0, -.004f, 1.50f), new Vector3(3.72f, .009f, 2.20f), "#688C80");
            for (int i = 0; i < _livingItems.Length; i++)
            {
                var item = new GameObject(HouseholdLife.ItemName(i)).transform;
                item.SetParent(root, false);
                _livingItems[i] = item;
            }
            Piece(_livingItems[0], PrimitiveType.Cube, "Picture card", new Vector3(0, .045f, 0), new Vector3(.34f, .08f, .23f), "#EBCFA2");
            Piece(_livingItems[0], PrimitiveType.Cylinder, "Printed moon", new Vector3(-.07f, .086f, 0), new Vector3(.10f, .004f, .10f), "#2E535B");
            Piece(_livingItems[0], PrimitiveType.Cube, "Printed house", new Vector3(.07f, .086f, 0), new Vector3(.07f, .004f, .09f), "#C46C55");
            Piece(_livingItems[1], PrimitiveType.Sphere, "Ball", new Vector3(0, .125f, 0), Vector3.one * .25f, "#E3A540");
            Piece(_livingItems[1], PrimitiveType.Sphere, "Ball seam", new Vector3(0, .125f, 0), new Vector3(.253f, .05f, .253f), "#F4D09F");
            Piece(_livingItems[2], PrimitiveType.Cube, "Block base", new Vector3(0, .075f, 0), Vector3.one * .15f, "#B77443");
            _stackA = Piece(_livingItems[2], PrimitiveType.Cube, "Block A", Vector3.zero, Vector3.one * .14f, "#BD6151");
            _stackB = Piece(_livingItems[2], PrimitiveType.Cube, "Block B", Vector3.zero, Vector3.one * .135f, "#E6BB73");
            Box(_livingItems[3], .37f, .23f, "#A35E49");
            _lid = new GameObject("Working lid").transform;
            _lid.SetParent(_livingItems[3], false);
            _lid.localPosition = new Vector3(0, .23f, -.16f);
            Piece(_lid, PrimitiveType.Cube, "Lid", new Vector3(0, 0, .16f), new Vector3(.40f, .034f, .35f), "#D19465");
            _lidGrip = Piece(_lid, PrimitiveType.Cube, "Lid handle", new Vector3(0, .037f, .27f),
                new Vector3(.11f, .04f, .055f), "#80513D");
            _revealed = Piece(_livingItems[3], PrimitiveType.Sphere, "Contents", new Vector3(0, .10f, 0), Vector3.one * .14f, "#83B8A4");
            Box(_livingItems[4], .37f, .19f, "#A87857");
            _food = Piece(_livingItems[4], PrimitiveType.Sphere, "Food portions", new Vector3(0, .22f, 0), new Vector3(.25f, .12f, .25f), "#87A95D");
            Piece(_livingItems[5], PrimitiveType.Cylinder, "Cup", new Vector3(0, .105f, 0), new Vector3(.24f, .105f, .24f), "#C8DAD7");
            _water = Piece(_livingItems[5], PrimitiveType.Cylinder, "Water fill", new Vector3(0, .212f, 0), new Vector3(.19f, .007f, .19f), "#6BACC0");
            for (int i = 6; i < 8; i++)
            {
                var seat = _livingItems[i];
                Piece(seat, PrimitiveType.Cube, "Learner seat", new Vector3(0, .05f, 0), new Vector3(.32f, .07f, .31f), "#946451");
                Piece(seat, PrimitiveType.Cube, "Chair back", new Vector3(0, .16f, -.15f), new Vector3(.32f, .21f, .04f), "#B4805D");
            }
            for (int i = 8; i < HouseholdLife.SortingBoard; i++)
            {
                var bed = _livingItems[i];
                if (i >= 10) bed.localScale = new Vector3(.84f, 1f, .72f);
                Piece(bed, PrimitiveType.Cube, "Bed frame", new Vector3(0, .10f, -.69f), new Vector3(.66f, .20f, 1.48f), "#775641");
                Piece(bed, PrimitiveType.Cube, "Linen", new Vector3(0, .22f, -.69f), new Vector3(.64f, .14f, 1.42f), "#8FA9AE");
                Piece(bed, PrimitiveType.Cube, "Pillow", new Vector3(0, .31f, -1.27f), new Vector3(.50f, .09f, .25f), "#E8D8BC");
            }
            Piece(root, PrimitiveType.Cube, "Supply shelf", new Vector3(-2.45f, .34f, 1.38f), new Vector3(.54f, .69f, .85f), "#8A614D");
            Piece(root, PrimitiveType.Cube, "Supply table", new Vector3(2.40f, .27f, 2.90f), new Vector3(.55f, .54f, .61f), "#A77454");
            var sort = _livingItems[HouseholdLife.SortingBoard];
            Piece(sort, PrimitiveType.Cube, "Shape board", new Vector3(0, .05f, 0), new Vector3(.60f, .10f, .35f), "#6A858F");
            _socketMarks[0] = Piece(sort, PrimitiveType.Cylinder, "Round socket", SocketPositions[0], new Vector3(.12f,.002f,.12f), "#F0D491");
            _socketMarks[1] = Piece(sort, PrimitiveType.Cube, "Square socket", SocketPositions[1], new Vector3(.11f,.004f,.11f), "#D78A74");
            _socketMarks[2] = Piece(sort, PrimitiveType.Cube, "Long socket", SocketPositions[2], new Vector3(.064f,.008f,.13f), "#B3CCC3");
            _boardMark = Piece(sort, PrimitiveType.Cube, "Orientation mark", Vector3.zero, new Vector3(.044f, .014f, .045f), "#EFD796");
            var cuePosition = new Vector3(0, .065f, .24f);
            _sortPieces[0] = Piece(sort, PrimitiveType.Cylinder, "Round piece", cuePosition, new Vector3(.10f, .054f, .10f), "#F0D491");
            _sortPieces[1] = Piece(sort, PrimitiveType.Cube, "Square piece", cuePosition, new Vector3(.098f, .11f, .098f), "#D78A74");
            _sortPieces[2] = Piece(sort, PrimitiveType.Cube, "Long piece", cuePosition, new Vector3(.055f, .11f, .12f), "#B3CCC3");
            Box(_livingItems[HouseholdLife.ToyBin], .43f, .15f, "#89756A");
            _storedToys = Piece(_livingItems[HouseholdLife.ToyBin], PrimitiveType.Cube, "Blocks stored in bin", new Vector3(0, .097f, 0), new Vector3(.21f,.12f,.17f), "#BC775B");
            BuildLivingSearchProps(root);
            _zoom = 5.5f;
            _orbitYaw = 157f;
            _orbitPitch = 27f;
        }

        void BuildLivingSearchProps(Transform root)
        {
            string[] colors = { "#AD764D", "#658BA1", "#66896C" };
            for (int box = 0; box < _searchLids.Length; box++)
            {
                var parent = _livingItems[HouseholdLife.FirstSearchBox + box];
                Box(parent, .35f, .14f, colors[box]);
                var lid = new GameObject("Search container lid").transform;
                lid.SetParent(parent, false);
                lid.localPosition = new Vector3(0, .14f, -.16f);
                Piece(lid, PrimitiveType.Cube, "Lid panel", new Vector3(0, 0, .16f),
                    new Vector3(.37f, .03f, .35f), colors[box]);
                _searchLabels[box] = Piece(lid, PrimitiveType.Cube, "Visible label", new Vector3(0, .019f, .24f),
                    new Vector3(.075f, .006f, .05f), "#EBCB77");
                _searchLids[box] = lid;
            }
            _redBoat = BuildSearchBoat(root, HouseholdLife.TargetToyId, false);
            _stripedBoat = BuildSearchBoat(root, HouseholdLife.SimilarToyId, true);
        }

        Transform BuildSearchBoat(Transform root, string id, bool stripe)
        {
            Transform boat = new GameObject(id).transform;
            boat.SetParent(root, false);
            Piece(boat, PrimitiveType.Cube, "Boat hull", new Vector3(0, .018f, 0),
                new Vector3(.115f, .033f, .19f), "#CC483A");
            Piece(boat, PrimitiveType.Cube, "Cabin", new Vector3(0, .06f, -.018f),
                new Vector3(.063f, .068f, .071f), "#EAA77B");
            if (stripe) Piece(boat, PrimitiveType.Cube, "Ivory stripe", new Vector3(0, .04f, .064f),
                new Vector3(.105f, .009f, .025f), "#EFE0C2");
            return boat;
        }

        void DrawLivingSearch(HouseholdLife life, float blend)
        {
            int red = -1, striped = -1;
            for (int box = 0; box < _searchLids.Length; box++)
            {
                var visible = life.ReadSearchBox(box);
                var lid = _searchLids[box];
                lid.localRotation = Quaternion.Slerp(lid.localRotation,
                    Quaternion.Euler(visible.Open ? -83f : 0f, 0f, 0f), blend);
                _searchLabels[box].gameObject.SetActive(life.HasSearchLabel(box));
                if (visible.VisibleToy == HouseholdLife.TargetToy) red = box;
                if (visible.VisibleToy == HouseholdLife.SimilarToy) striped = box;
            }
            PlaceSearchBoat(_redBoat, red);
            PlaceSearchBoat(_stripedBoat, striped);
        }

        void PlaceSearchBoat(Transform boat, int box)
        {
            boat.gameObject.SetActive(box >= 0);
            if (box >= 0) boat.position = _livingItems[HouseholdLife.FirstSearchBox + box].position + Vector3.up * .049f;
        }

        void Box(Transform parent, float width, float height, string color)
        {
            Piece(parent, PrimitiveType.Cube, "Base", Vector3.up * .025f, new Vector3(width, .05f, width), color);
            for (int i = -1; i <= 1; i += 2)
            {
                Piece(parent, PrimitiveType.Cube, "Side", new Vector3(i * width * .46f, height * .5f, 0), new Vector3(.025f, height, width), color);
                Piece(parent, PrimitiveType.Cube, "End", new Vector3(0, height * .5f, i * width * .46f), new Vector3(width, height, .025f), color);
            }
        }

        Transform Piece(Transform parent, PrimitiveType shape, string part, Vector3 local, Vector3 scale, string color)
        {
            Transform mesh = CharacterFactory.Primitive(parent, shape, _unlit, CharacterFactory.Hex(color), local, scale, 0);
            mesh.name = part;
            return mesh;
        }

        void DrawLiving(float dt)
        {
            HouseholdLife life = _sim.Home;
            for (int i = 0; i < _livingItems.Length; i++)
            {
                var item = life.ReadItem(i);
                _livingItems[i].position = new Vector3(item.X, 0f, item.Z);
            }
            int stack = life.ReadItem(2).Value;
            float blend = 1f - Mathf.Exp(-9f * dt);
            _stackA.localPosition = Vector3.Lerp(_stackA.localPosition, stack > 0 ? new Vector3(0, .22f, 0) : new Vector3(.18f, .07f, 0), blend);
            _stackB.localPosition = Vector3.Lerp(_stackB.localPosition, stack > 1 ? new Vector3(0, .357f, 0) : new Vector3(-.18f, .068f, 0), blend);
            bool open = life.ReadItem(3).Value != 0;
            _lid.localRotation = Quaternion.Slerp(_lid.localRotation, Quaternion.Euler(open ? -73f : 0f, 0f, 0f), blend);
            _revealed.gameObject.SetActive(open);
            _food.gameObject.SetActive(life.ReadItem(4).Value > 0);
            _water.gameObject.SetActive(life.ReadItem(5).Value > 0);
            int sortingState = life.ReadItem(HouseholdLife.SortingBoard).Value;
            int rotation = ShapeBoard.Rotation(sortingState);
            _boardMark.localPosition = new Vector3(SocketPositions[rotation].x, .107f, -.136f);
            for (int i = 0; i < _sortPieces.Length; i++)
            {
                _socketMarks[i].localPosition = SocketPositions[ShapeBoard.Socket(i, rotation)];
                _sortPieces[i].gameObject.SetActive(i == ShapeBoard.Cue(sortingState));
            }
            _storedToys.gameObject.SetActive(life.ReadItem(HouseholdLife.ToyBin).Value > 0);
            DrawLivingSearch(life, blend);
            for (int i = 0; i < _livingActors.Length; i++)
            {
                if (_livingActors[i] == null) _livingActors[i] = _actors[life.PersonId(i)];
                Actor actor = _livingActors[i];
                var state = life.ReadPerson(i);
                bool sleeping = state.Activity == HomeActivity.Sleep && state.Phase == HomePhase.Act;
                Vector3 previous = actor.Pos;
                actor.Pos = new Vector3(state.X, 0f, state.Z);
                // Every pillow is on the negative-Z side of its bed.
                actor.Yaw = Mathf.LerpAngle(actor.Yaw, sleeping ? 0f : state.Yaw, blend);
                if (actor.State != null) actor.Body.ApplyDevelopmentAge(WorldSimulation.AnatomyAge(actor.State.Physiology.AgeDays));
                actor.Body.ShowEarlyClothes(!_researchPresentation);
                float moved = Vector3.Distance(previous, actor.Pos);
                actor.GaitDistance += moved;
                actor.Body.transform.position = actor.Pos;
                MentorMotion.WriteAngles("idle", _sim.SimSeconds, actor.Spec.BirthSeed, _poseBuf);
                SmoothAndApply(actor, _poseBuf, dt);
                actor.Body.ApplyGroundedGait(actor.GaitDistance, moved, actor.Yaw);
                Vector3? attention = null;
                if (state.Person >= 0)
                {
                    var other = life.ReadPerson(state.Person);
                    attention = new Vector3(other.X, state.Person < 2 ? 1.25f : .45f, other.Z);
                }
                else if (state.Item >= 0)
                    attention = _livingItems[state.Item].position + Vector3.up * (state.Item == 3 ? .18f : .13f);
                else if (state.Last >= 0)
                {
                    var last = life.Experience(state.Last);
                    if (_sim.TickCount - last.Tick <= 20 && last.Evidence != HomeEvidence.Replay)
                        attention = _livingItems[last.Target].position + Vector3.up * .13f;
                }
                bool acting = state.Phase == HomePhase.Act && state.Activity != HomeActivity.Sleep && state.Activity != HomeActivity.Rest &&
                    state.Activity != HomeActivity.Inspect;
                bool seated = state.Activity == HomeActivity.Rest && state.Item >= 0 && moved < .002f;
                actor.Body.ApplyHouseholdPose(attention, acting, seated, sleeping, dt, _sim.SimSeconds);
                Vector3? contact = acting ? ContactPoint(state.Activity, state.Item, actor.Pos) : null;
                int scan = life.ActiveSocketDecision(i);
                if (acting && state.Activity == HomeActivity.Sort && scan >= 0)
                    contact = _livingItems[HouseholdLife.SortingBoard].TransformPoint(SocketPositions[new SocketDecision(life.Experience(scan).Details).Choice]);
                UpdateLocalAvatar(actor, attention, contact,
                    seated, sleeping, acting && (state.Activity == HomeActivity.Eat || state.Activity == HomeActivity.Drink), dt);
            }
        }

        Vector3? ContactPoint(HomeActivity activity, int item, Vector3 origin)
        {
            Vector3 center = _livingItems[item].position;
            switch (activity)
            {
                case HomeActivity.Open:
                case HomeActivity.Close: return _lidGrip.position;
                case HomeActivity.Roll: return center + Vector3.up * .19f + (origin - center).normalized * .09f;
                case HomeActivity.Stack: return _stackA.position + Vector3.up * .071f;
                case HomeActivity.Drink: return center + Vector3.up * .19f + (origin - center).normalized * .1f;
                case HomeActivity.Eat: return _food.position + Vector3.up * .05f;
                case HomeActivity.Sort: return center + Vector3.up * .11f;
                case HomeActivity.Tidy: return center + Vector3.up * .17f;
                case HomeActivity.FindToy: return center + Vector3.up * .16f + (origin - center).normalized * .13f;
                default: return null;
            }
        }

        void UpdateLocalAvatar(Actor actor, Vector3? attention, Vector3? contact, bool seated, bool sleeping, bool consuming, float dt)
        {
            // The supplied learner meshes have mature proportions. Infant and child anatomy retains its own view.
            bool ageAppropriate = HasAgeAppropriateAvatar(actor);
            if (ageAppropriate && !actor.LocalAvatarChecked)
            {
                actor.LocalAvatarChecked = true;
                actor.LocalAvatar = HouseholdAvatar.Load(actor.Body);
            }
            if (actor.LocalAvatar == null) return;
            bool visible = ageAppropriate && !_researchPresentation;
            actor.LocalAvatar.SetVisible(visible);
            if (visible) actor.LocalAvatar.Follow(attention, contact, seated, sleeping, consuming, _sim.SimSeconds, dt);
        }

        static bool HasAgeAppropriateAvatar(Actor actor) =>
            actor.State == null || actor.Body.SampledDevelopmentDays >= HumanoidAnatomy.AdultAgeDays;

        void ToggleResearchPresentation()
        {
            _researchPresentation = !_researchPresentation;
            foreach (var actor in _livingActors)
                if (actor != null)
                {
                    bool visible = !_researchPresentation && (_remote == null || !_remote.Active) && HasAgeAppropriateAvatar(actor);
                    actor.Body.ShowEarlyClothes(!_researchPresentation && (_remote == null || !_remote.Active));
                    actor.LocalAvatar?.SetVisible(visible);
                }
            _hud.ShowResearch(_researchPresentation);
        }

        void RegisterLivingTray()
        {
            _workspace.Register(LifeTray, "Life and experience", new Vector2(248f, 286f),
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.73f, .045f), AnchorMax = new Vector2(.98f, .73f), Visible = false },
                parent =>
                {
                    _lifeText = NewLabel(parent, "Experience", 20);
                    _lifeText.alignment = TextAnchor.UpperLeft;
                    _lifeText.resizeTextForBestFit = true;
                    _lifeText.resizeTextMinSize = 12;
                    _lifeText.resizeTextMaxSize = 20;
                    _lifeText.rectTransform.anchorMin = new Vector2(.05f, .20f);
                    _lifeText.rectTransform.anchorMax = new Vector2(.95f, .96f);
                    _lifeText.rectTransform.offsetMin = _lifeText.rectTransform.offsetMax = Vector2.zero;
                    var controls = new GameObject("Experience controls", typeof(RectTransform)).GetComponent<RectTransform>();
                    controls.SetParent(parent, false);
                    controls.anchorMin = new Vector2(.03f, .01f); controls.anchorMax = new Vector2(.97f, .20f);
                    controls.offsetMin = controls.offsetMax = Vector2.zero;
                    AddTimeButton(controls, "Next person", 0f, .43f, () => { _lifeFocus = (_lifeFocus + 1) % 4; RefreshLife(); });
                    AddTimeButton(controls, "History day", .43f, .76f, () => { _historyDaysBack = (_historyDaysBack + 1) % (_sim.DayIndex + 1); RefreshLife(); });
                    AddTimeButton(controls, "Close", .76f, 1f, () => _workspace.Toggle(LifeTray));
                }, () => _lifeText = null, RefreshLife);
        }

        void RefreshLife()
        {
            if (_lifeText == null) return;
            if (_remote != null && _remote.Active)
            {
                _lifeText.text = "The remote stream provides learner pose and age. Household activity records are available in local mode.";
                return;
            }
            var home = _sim.Home;
            var p = home.ReadPerson(_lifeFocus);
            string id = home.PersonId(_lifeFocus);
            var child = _sim.Find(id);
            var text = new StringBuilder(600);
            text.AppendLine($"{LivingName(_pop.Find(id))}  ·  Day {_sim.DayIndex + 1}");
            string target = p.Item >= 0 ? HouseholdLife.ItemName(p.Item) : p.Person >= 0 ? LivingName(_pop.Find(home.PersonId(p.Person))) : "—";
            text.AppendLine($"{p.Activity}  ·  {p.Phase}");
            text.AppendLine($"Attention  {target}\nSelection  {p.Reason}");
            if (child != null) text.AppendLine($"Energy {child.Body.Energy:P0}  ·  Hunger {child.Body.Hunger:P0}");
            text.AppendLine($"Thirst {p.Thirst:P0}  ·  Planned {p.Prediction}");
            if (p.KnowledgeEvidence >= 0) text.AppendLine($"Learned affordance  {p.Knowledge}  ·  evidence {p.KnowledgeEvidence}");
            text.AppendLine($"Attempts {p.Attempts}  ·  Outcomes {p.Successes}\nWitnessed {p.Observed}  ·  Imitated {p.Imitated}");
            if (_lifeFocus == HouseholdLife.SearchActor) AppendSpatialSearch(text, home);
            var sort = home.ReadCompetence(_lifeFocus, HouseholdLife.SortingBoard);
            text.AppendLine($"Shape board  ·  independent {sort.IndependentSuccesses}/{sort.IndependentAttempts}  ·  failed {sort.Failures}");
            if (sort.LastIndependentSuccess >= 0)
            {
                var evidence = home.Experience(sort.LastIndependentSuccess);
                text.AppendLine(evidence.Source >= 0 ? $"Last fit  ·  source {evidence.Source}" : $"Last fit  ·  tick {evidence.Tick}");
            }
            int scan = home.LastSocketDecision(_lifeFocus);
            if (scan >= 0)
            {
                var record = home.Experience(scan);
                var decision = new SocketDecision(record.Details);
                text.AppendLine($"Socket {decision.Choice + 1}  ·  {decision.Observations} inspections");
                text.AppendLine($"Uncertainty {decision.UncertaintyBefore} → {decision.UncertaintyAfter}{(decision.Contradiction ? "  ·  context changed" : "")}");
                if (record.Source >= 0) text.AppendLine($"Prior fit  ·  episode {record.Source}");
                for (int slot = 0; slot < 3; slot++)
                    if ((decision.ObservedSlots & (1 << slot)) != 0)
                    {
                        SocketFeatures visible = decision.Observed(slot);
                        text.AppendLine($"Socket {slot + 1}  ·  aspect {visible.Aspect}  ·  edge {visible.Edge}");
                    }
            }
            if (p.Replay >= 0) text.AppendLine($"Night replay  {home.Experience(p.Replay).Activity}  ·  tick {home.Experience(p.Replay).Tick}");
            int historyDay = Mathf.Max(0, _sim.DayIndex - _historyDaysBack);
            int dayStart = Mathf.FloorToInt(historyDay * WorldSimulation.DayLength / WorldSimulation.Dt);
            int dayEnd = Mathf.FloorToInt((historyDay + 1) * WorldSimulation.DayLength / WorldSimulation.Dt);
            int start = LowerExperienceBound(home, dayStart), end = LowerExperienceBound(home, dayEnd);
            int count = 0, shown = 0;
            for (int i = start; i < end; i++) if (home.Experience(i).Actor == _lifeFocus) count++;
            text.AppendLine($"Tick day {historyDay + 1}  ·  {count} records");
            for (int i = end - 1; i >= start && shown < 4; i--)
            {
                var e = home.Experience(i);
                if (e.Actor != _lifeFocus) continue;
                text.AppendLine($"{e.Tick}  {e.Evidence}  ·  {e.Activity}{(e.Changed ? "  ·  changed state" : "")}");
                shown++;
            }
            _lifeText.text = text.ToString();
        }

        static void AppendSpatialSearch(StringBuilder text, HouseholdLife home)
        {
            var search = home.ReadSpatialSearch();
            if (search.Sighting < 0) return;
            var knowledge = home.ReadSupportedToyLocation();
            text.AppendLine($"Red boat search | {search.Stage}");
            text.AppendLine($"Last seen {SearchBoxName(knowledge.Box)} | tick {knowledge.Tick} | episode {knowledge.Evidence}");
            if (search.Stage == SpatialStage.Introduction || search.Stage == SpatialStage.Waiting) return;
            text.AppendLine($"Expected {SearchBoxName(search.Expected)} | {search.Confidence}");
            text.AppendLine($"Observed {SearchBoxName(search.Observed)} | {search.Remaining} checks left");
            bool approaching = home.ReadPerson(HouseholdLife.SearchActor).Activity == HomeActivity.FindToy;
            if (search.Stage == SpatialStage.Searching && search.Selected >= 0)
                text.AppendLine($"{(approaching ? "Selected" : "Last checked")} {SearchBoxName(search.Selected)} | {search.Selection}");
            for (int box = 0; box < HouseholdLife.SearchBoxCount; box++)
            {
                int flag = 1 << box;
                if ((search.Negative & flag) != 0) text.AppendLine($"Boat absent at {SearchBoxName(box)}");
                else if ((search.Candidates & flag) != 0 && box != search.Selected)
                    text.AppendLine($"Possible {SearchBoxName(box)}");
            }
        }

        static string SearchBoxName(int box) => box < 0 ? "unknown" : HouseholdLife.ItemName(HouseholdLife.FirstSearchBox + box);

        static int LowerExperienceBound(HouseholdLife home, int tick)
        {
            int first = 0, end = home.ExperienceCount;
            while (first < end)
            {
                int middle = first + (end - first) / 2;
                if (home.Experience(middle).Tick < tick) first = middle + 1;
                else end = middle;
            }
            return first;
        }
    }
}
