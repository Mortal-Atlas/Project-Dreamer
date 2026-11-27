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
        Text _lifeText;
        int _lifeFocus = 2;
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
            for (int i = 8; i < _livingItems.Length; i++)
            {
                var bed = _livingItems[i];
                if (i >= 10) bed.localScale = new Vector3(.84f, 1f, .72f);
                Piece(bed, PrimitiveType.Cube, "Bed frame", new Vector3(0, .10f, -.12f), new Vector3(.66f, .20f, 1.48f), "#775641");
                Piece(bed, PrimitiveType.Cube, "Linen", new Vector3(0, .22f, -.12f), new Vector3(.64f, .14f, 1.42f), "#8FA9AE");
                Piece(bed, PrimitiveType.Cube, "Pillow", new Vector3(0, .31f, -.70f), new Vector3(.50f, .09f, .25f), "#E8D8BC");
            }
            Piece(root, PrimitiveType.Cube, "Supply shelf", new Vector3(-2.45f, .34f, 1.38f), new Vector3(.54f, .69f, .85f), "#8A614D");
            Piece(root, PrimitiveType.Cube, "Supply table", new Vector3(2.40f, .27f, 2.90f), new Vector3(.55f, .54f, .61f), "#A77454");
            _zoom = 5.5f;
            _orbitYaw = 157f;
            _orbitPitch = 27f;
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
            for (int i = 0; i < _livingActors.Length; i++)
            {
                if (_livingActors[i] == null) _livingActors[i] = _actors[life.PersonId(i)];
                Actor actor = _livingActors[i];
                var state = life.ReadPerson(i);
                Vector3 previous = actor.Pos;
                actor.Pos = new Vector3(state.X, 0f, state.Z);
                actor.Yaw = Mathf.LerpAngle(actor.Yaw, state.Yaw, blend);
                if (actor.State != null) actor.Body.ApplyDevelopmentAge(WorldSimulation.AnatomyAge(actor.State.Physiology.AgeDays));
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
                actor.Body.ApplyHouseholdPose(attention, acting, state.Activity == HomeActivity.Rest && state.Item >= 0 && moved < .002f,
                    state.Activity == HomeActivity.Sleep && state.Phase == HomePhase.Act, dt, _sim.SimSeconds);
            }
        }

        void ToggleResearchPresentation()
        {
            _researchPresentation = !_researchPresentation;
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
                    AddTimeButton(controls, "Next person", 0f, .57f, () => { _lifeFocus = (_lifeFocus + 1) % 4; RefreshLife(); });
                    AddTimeButton(controls, "Close", .57f, 1f, () => _workspace.Toggle(LifeTray));
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
            if (p.Replay >= 0) text.AppendLine($"Night replay  {home.Experience(p.Replay).Activity}  ·  tick {home.Experience(p.Replay).Tick}");
            int today = 0, shown = 0, earliest = home.ExperienceCount - 1;
            int dayStart = Mathf.Max(0, Mathf.FloorToInt(_sim.DayIndex * WorldSimulation.DayLength / WorldSimulation.Dt));
            for (; earliest >= 0; earliest--)
            {
                var e = home.Experience(earliest);
                if (e.Tick < dayStart) break;
                if (e.Actor == _lifeFocus) today++;
            }
            text.AppendLine($"Today's records  {today}");
            for (int i = home.ExperienceCount - 1; i >= 0 && shown < 4 && i > earliest - 24; i--)
            {
                var e = home.Experience(i);
                if (e.Actor != _lifeFocus) continue;
                text.AppendLine($"{e.Tick}  {e.Evidence}  ·  {e.Activity}{(e.Changed ? "  ·  changed state" : "")}");
                shown++;
            }
            _lifeText.text = text.ToString();
        }
    }
}
