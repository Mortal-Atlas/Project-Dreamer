using System;
using System.Collections.Generic;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    public sealed class HandLabView : MonoBehaviour
    {
        const string Tray = "embodiment.hand-attempts";
        const int MaximumFrames = 240, MaximumAttempts = 4;
        readonly List<Attempt> _attempts = new List<Attempt>(MaximumAttempts);
        readonly Transform[] _parts = new Transform[7];
        readonly Transform[] _contacts = new Transform[2];
        readonly Transform[] _objects = new Transform[3];
        readonly Material[] _colors = new Material[4];
        readonly TimeGovernor _clock = new TimeGovernor { Paused = true };
        NativeHandLab _lab;
        Attempt _live;
        TrayWorkspace _workspace;
        Text _details;
        Slider _scrubber;
        Transform _destination;
        int _nextId, _selected = -1, _cursor;
        int _scenario, _liftStart;
        float _nextRefresh;
        static readonly string[] Scenarios = { "sphere", "added load", "displaced box", "capsule" };

        struct Frame
        {
            public GripFrame Reading;
            public float Cap, Load, SupportX;
            public int Corrections, Repositions;
            public bool Placed;
            public Pose Palm, LeftShaft, LeftPad, RightShaft, RightPad, Object;

            public Frame(NativeHandLab lab, float load = 0f)
            {
                Reading = lab.ReadFrame();
                Cap = lab.EffectiveGripCap;
                Corrections = lab.GripCorrections;
                Repositions = lab.Repositions; Load = load; SupportX = lab.SupportX; Placed = lab.ControlledPlacement;
                Palm = PoseOf(lab, 1); LeftShaft = PoseOf(lab, 2); LeftPad = PoseOf(lab, 3);
                RightShaft = PoseOf(lab, 4); RightPad = PoseOf(lab, 5); Object = PoseOf(lab, 6);
            }
            static Pose PoseOf(NativeHandLab lab, int part)
            {
                var pose = lab.ReadPart(part);
                return new Pose(pose.Position, pose.Rotation);
            }
        }

        sealed class Attempt
        {
            public readonly int Id;
            public readonly float Cap;
            public readonly bool Feedback;
            public readonly bool Placement;
            public readonly int Scenario;
            public readonly HandObjectShape Shape;
            public readonly Vector3 Size;
            public readonly List<Frame> Frames = new List<Frame>(96);
            public string Outcome = "in progress";
            public Attempt(int id, float cap, bool feedback, bool placement, int scenario, HandObjectSetup setup)
            {
                Id = id; Cap = cap; Feedback = feedback; Placement = placement; Scenario = scenario;
                Shape = setup.Shape; Size = setup.Size;
            }
        }

        void Start()
        {
            var camera = new GameObject("Hand laboratory camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.transform.position = new Vector3(.27f, .21f, .32f);
            camera.transform.LookAt(new Vector3(-.073f, .039f, -.026f));
            camera.nearClipPlane = .025f;
            camera.farClipPlane = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.105f, .144f, .179f);
            if (EventSystem.current == null)
                new GameObject("Hand lab input", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var palette = new[] { new Color(.25f,.32f,.40f), new Color(.42f,.76f,.74f), new Color(.99f,.55f,.21f),
                new Color(.96f,.83f,.39f) };
            for (int i = 0; i < palette.Length; i++) _colors[i] = new Material(shader) { color = palette[i] };
            for (int i = 0; i < 6; i++) _parts[i] = Shape(PrimitiveType.Cube, i == 0 ? 0 : 1);
            _objects[0] = Shape(PrimitiveType.Sphere, 2);
            _objects[1] = Shape(PrimitiveType.Cube, 2);
            _objects[2] = Shape(PrimitiveType.Capsule, 2);
            _destination = Shape(PrimitiveType.Cube, 3);
            _destination.position = new Vector3(NativeHandLab.PlacementX, .0005f, 0f);
            _destination.localScale = new Vector3(2f * NativeHandLab.PlacementTolerance, .001f, 2f * NativeHandLab.PlacementHalfDepth);
            for (int i = 0; i < _contacts.Length; i++) _contacts[i] = Shape(PrimitiveType.Sphere, 3);
            Begin(3.2f);
            _workspace = new TrayWorkspace("DreamMachine.Workspace.v1.");
            _workspace.Register(Tray, "Motor attempts", new Vector2(236f, 205f),
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.58f, .16f), AnchorMax = new Vector2(.98f, .84f), Visible = true },
                Build, () => { _details = null; _scrubber = null; }, Refresh);
            _workspace.RestoreVisibility();
        }

        void Begin(float cap, bool feedback = false, bool placement = false)
        {
            if (_live != null) Finish("interrupted by researcher");
            _lab?.Dispose();
            int scenario = placement ? _scenario : 0;
            float radius = scenario == 1 ? .031f : scenario == 3 ? .0255f : .028f;
            var setup = scenario switch
            {
                2 => new HandObjectSetup(HandObjectShape.Box, new Vector3(.045f, .042f, .052f)),
                3 => new HandObjectSetup(HandObjectShape.Capsule, new Vector3(2f * radius, .071f, 2f * radius)),
                _ => new HandObjectSetup(HandObjectShape.Sphere, Vector3.one * (2f * radius))
            };
            float mass = scenario == 1 ? .091f : scenario == 3 ? .113f : .080f;
            float friction = scenario == 1 ? .43f : scenario == 3 ? .40f : .75f;
            _lab = new NativeHandLab(37, mass, radius, friction, cap, orientation: scenario == 2 ? 16f : 0f,
                objectSetup: setup, contactGated: feedback, placement: placement,
                exploration: placement ? GraspExploration.Tactile : GraspExploration.Off,
                objectOffset: scenario == 2 ? new Vector2(.019f, .022f) : Vector2.zero);
            _live = new Attempt(++_nextId, cap, feedback, placement, scenario, setup);
            _selected = -1;
            _liftStart = -1;
            _clock.Paused = true;
            _clock.TakeSingleStep();
            _clock.Accumulator = 0;
            _live.Frames.Add(new Frame(_lab));
            for (int i = 0; i < 6; i++) _parts[i].localScale = _lab.ReadPart(i).Size;
            _parts[0].position = _lab.ReadPart(0).Position;
            Draw(_live.Frames[0], _live);
            Refresh();
        }

        void Finish(string outcome)
        {
            _live.Outcome = outcome;
            if (_attempts.Count == MaximumAttempts) _attempts.RemoveAt(0);
            _attempts.Add(_live);
            _live = null;
            _clock.Paused = true;
            _selected = _attempts.Count - 1;
            _cursor = _attempts[_selected].Frames.Count - 1;
        }

        void Update()
        {
            if (_workspace == null) return;
            if (DreamInput.WasPressed(KeyCode.F6)) _workspace.Toggle(Tray);
            if (_selected < 0 && _live != null)
            {
                int count = _clock.TakeSingleStep() ? 1 : _clock.Plan(Time.unscaledDeltaTime, WorldSimulation.Dt, 4);
                for (int i = 0; i < count && _live != null; i++)
                {
                    if (_lab.Phase == GripPhase.Lift && _liftStart < 0) _liftStart = _lab.Tick;
                    int liftAge = _liftStart < 0 ? -1 : _lab.Tick - _liftStart;
                    float load = _live.Scenario == 1 && liftAge >= 9 && liftAge < 15 &&
                        (_lab.Phase == GripPhase.Lift || _lab.Phase == GripPhase.Hold) ? .40f : 0f;
                    _lab.Step(Vector3.down * load, Vector3.zero);
                    var frame = new Frame(_lab, load);
                    _live.Frames.Add(frame);
                    Draw(frame, _live);
                    if (_lab.Terminal) Finish(_lab.Outcome);
                    else if (_live.Frames.Count >= MaximumFrames) Finish("recording window exhausted");
                }
            }
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .16f;
            _workspace.RefreshVisible();
        }

        void Select(int direction)
        {
            int index = _selected < 0 && direction < 0 ? _attempts.Count - 1 : _selected + direction;
            if (index == _attempts.Count && _live != null) index = -1;
            if (index < -1 || index >= _attempts.Count || (index == -1 && _live == null)) return;
            _clock.Paused = true;
            _selected = index;
            _cursor = 0;
            Draw(VisibleFrame(), VisibleAttempt());
            Refresh();
        }

        Attempt VisibleAttempt() => _selected >= 0 ? _attempts[_selected] : _live;
        Frame VisibleFrame() => VisibleAttempt().Frames[_selected >= 0 ? _cursor : _live.Frames.Count - 1];

        void Draw(Frame frame, Attempt attempt)
        {
            int shape = (int)attempt.Shape;
            if (_parts[6] != _objects[shape])
            {
                for (int i = 0; i < _objects.Length; i++) _objects[i].gameObject.SetActive(i == shape);
                _parts[6] = _objects[shape];
            }
            _parts[6].localScale = attempt.Shape == HandObjectShape.Capsule
                ? new Vector3(attempt.Size.x, .5f * attempt.Size.y, attempt.Size.z) : attempt.Size;
            _destination.gameObject.SetActive(attempt.Placement);
            Move(1, frame.Palm); Move(2, frame.LeftShaft); Move(3, frame.LeftPad);
            Move(4, frame.RightShaft); Move(5, frame.RightPad); Move(6, frame.Object);
            Contact(0, frame.Reading.Left); Contact(1, frame.Reading.Right);
        }

        void Move(int slot, Pose pose) => _parts[slot].SetPositionAndRotation(pose.position, pose.rotation);

        void Contact(int side, GripPadSample sample)
        {
            var marker = _contacts[side];
            marker.gameObject.SetActive(sample.NormalImpulse > 0);
            marker.position = sample.Point;
            marker.localScale = Vector3.one * Mathf.Clamp(.004f + sample.NormalForce * .0018f, .004f, .013f);
        }

        Transform Shape(PrimitiveType primitive, int color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.transform.SetParent(transform, false);
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = _colors[color];
            return part.transform;
        }

        void Build(RectTransform parent)
        {
            _details = new GameObject("Evidence", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _details.transform.SetParent(parent, false);
            Position(_details.rectTransform, .06f, .47f, .94f, .96f);
            _details.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _details.fontSize = 18;
            _details.resizeTextForBestFit = true;
            _details.resizeTextMinSize = 12;
            _details.resizeTextMaxSize = 18;
            _details.color = Color.white;
            _details.raycastTarget = false;
            _scrubber = SliderAt(parent);
            _scrubber.onValueChanged.AddListener(value =>
            {
                if (_selected < 0) return;
                _cursor = Mathf.RoundToInt(value);
                Draw(VisibleFrame(), VisibleAttempt());
                Refresh();
            });
            Button(parent, "Standard grip", 0, 2, () => Begin(3.2f));
            Button(parent, "Low grip", 1, 2, () => Begin(.18f));
            Button(parent, "Feedback grip", 2, 2, () => Begin(.18f, feedback: true));
            Button(parent, "Run / pause", 0, 1, () => { if (_selected < 0) _clock.Paused = !_clock.Paused; });
            Button(parent, "Step", 1, 1, () => { if (_selected < 0) _clock.RequestSingleStep(); });
            Button(parent, "Tactile placement", 2, 1, () => Begin(GripAcquisition.InitialCap, placement: true));
            Button(parent, "Earlier attempt", 0, 0, () => Select(-1));
            Button(parent, "Later attempt", 1, 0, () => Select(1));
            Button(parent, "Next object", 2, 0, () => { _scenario = (_scenario + 1) % Scenarios.Length; Refresh(); });
        }

        void Refresh()
        {
            if (_details == null) return;
            var attempt = VisibleAttempt();
            var frame = VisibleFrame();
            var f = frame.Reading;
            string policy = attempt.Placement ? $"Tactile placement / {Scenarios[attempt.Scenario]}" :
                attempt.Feedback ? "Contact-gated feedback" : attempt.Cap < 1f ? "Low-force control" : "Fixed reference";
            _details.text = $"Attempt {attempt.Id}  |  {policy}\n" +
                $"Individual  |  Unbound research fixture\n" +
                $"Opposed grip  |  {f.Phase}  |  tick {f.Tick}  |  {attempt.Outcome}\n" +
                $"Pad force {f.Left.NormalForce:F2} / {f.Right.NormalForce:F2} N  |  cap {attempt.Cap:F2} to {frame.Cap:F2} N\n" +
                $"Guide {1000f * f.LeftTravel:F1} / {1000f * f.RightTravel:F1} mm  |  target {1000f * f.TravelTarget:F1} mm\n" +
                $"Command {f.LeftClosingForce:F2} / {f.RightClosingForce:F2} N  |  slip {(f.Left.Slip || f.Right.Slip ? "observed" : "clear")}\n" +
                $"Lift {1000f * f.Clearance:F1} mm  |  held {f.HeldTicks * WorldSimulation.Dt:F2} s\n" +
                $"Corrections {frame.Corrections}  |  Repositions {frame.Repositions}  |  Queued {Scenarios[_scenario]}\n" +
                (attempt.Placement ? $"Added load {frame.Load:F2} N  |  support {f.SupportForce:F2} N at {frame.SupportX * 1000f:F0} mm  |  placed {(frame.Placed ? "yes" : "pending")}" :
                    "Research playback");
            _scrubber.interactable = _selected >= 0;
            _scrubber.maxValue = attempt.Frames.Count - 1;
            _scrubber.SetValueWithoutNotify(_selected < 0 ? attempt.Frames.Count - 1 : _cursor);
        }

        static Slider SliderAt(RectTransform parent)
        {
            var root = new GameObject("Attempt playback", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(parent, false);
            Position((RectTransform)root.transform, .06f, .39f, .94f, .43f);
            root.GetComponent<Image>().color = new Color(.22f,.29f,.37f);
            var slider = root.GetComponent<Slider>();
            var handle = new GameObject("Tick", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(root.transform, false);
            var rect = (RectTransform)handle.transform;
            rect.sizeDelta = new Vector2(12f, 0f);
            handle.GetComponent<Image>().color = new Color(.53f,.84f,.80f);
            slider.handleRect = rect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.wholeNumbers = true;
            return slider;
        }

        static void Button(RectTransform parent, string title, int column, int row, Action clicked)
        {
            var root = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Position((RectTransform)root.transform, .06f + .30f * column, .025f + .11f * row,
                .34f + .30f * column, .123f + .11f * row);
            root.GetComponent<Image>().color = new Color(.22f,.29f,.37f);
            root.GetComponent<Button>().onClick.AddListener(() => clicked());
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(root.transform, false);
            Position(label.rectTransform, 0f, 0f, 1f, 1f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 15;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 15;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = title;
        }

        static void Position(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            _lab?.Dispose();
            _workspace?.Dispose();
            foreach (var material in _colors) if (material != null) Destroy(material);
        }
    }
}
