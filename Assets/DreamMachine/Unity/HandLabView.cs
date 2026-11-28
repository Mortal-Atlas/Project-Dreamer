using System;
using System.Collections.Generic;
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
        readonly Material[] _colors = new Material[4];
        readonly TimeGovernor _clock = new TimeGovernor { Paused = true };
        NativeHandLab _lab;
        Attempt _live;
        TrayWorkspace _workspace;
        Text _details;
        Slider _scrubber;
        int _nextId, _selected = -1, _cursor;
        float _nextRefresh;

        struct Frame
        {
            public GripFrame Reading;
            public float Cap;
            public int Corrections;
            public Pose Palm, LeftShaft, LeftPad, RightShaft, RightPad, Object;

            public Frame(NativeHandLab lab)
            {
                Reading = lab.ReadFrame();
                Cap = lab.EffectiveGripCap;
                Corrections = lab.GripCorrections;
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
            public readonly List<Frame> Frames = new List<Frame>(96);
            public string Outcome = "in progress";
            public Attempt(int id, float cap, bool feedback) { Id = id; Cap = cap; Feedback = feedback; }
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
            for (int i = 0; i < _parts.Length; i++) _parts[i] = Shape(i == 6, i == 6 ? 2 : i == 0 ? 0 : 1);
            for (int i = 0; i < _contacts.Length; i++) _contacts[i] = Shape(true, 3);
            Begin(3.2f);
            _workspace = new TrayWorkspace("DreamMachine.Workspace.v1.");
            _workspace.Register(Tray, "Motor attempts", new Vector2(236f, 205f),
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.58f, .16f), AnchorMax = new Vector2(.98f, .84f), Visible = true },
                Build, () => { _details = null; _scrubber = null; }, Refresh);
            _workspace.RestoreVisibility();
        }

        void Begin(float cap, bool feedback = false)
        {
            if (_live != null) Finish("interrupted by researcher");
            _lab?.Dispose();
            const float radius = .028f;
            var sphere = new HandObjectSetup(HandObjectShape.Sphere, Vector3.one * (2f * radius));
            _lab = new NativeHandLab(37, .080f, radius, .75f, cap, objectSetup: sphere, contactGated: feedback);
            _live = new Attempt(++_nextId, cap, feedback);
            _selected = -1;
            _clock.Paused = true;
            _clock.TakeSingleStep();
            _clock.Accumulator = 0;
            _live.Frames.Add(new Frame(_lab));
            for (int i = 0; i < _parts.Length; i++) _parts[i].localScale = _lab.ReadPart(i).Size;
            _parts[0].position = _lab.ReadPart(0).Position;
            Draw(_live.Frames[0]);
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
                    _lab.Step();
                    var frame = new Frame(_lab);
                    _live.Frames.Add(frame);
                    Draw(frame);
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
            Draw(VisibleFrame());
            Refresh();
        }

        Frame VisibleFrame()
        {
            if (_selected >= 0) return _attempts[_selected].Frames[_cursor];
            return _live.Frames[_live.Frames.Count - 1];
        }

        void Draw(Frame frame)
        {
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

        Transform Shape(bool sphere, int color)
        {
            var part = GameObject.CreatePrimitive(sphere ? PrimitiveType.Sphere : PrimitiveType.Cube);
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
                Draw(VisibleFrame());
                Refresh();
            });
            Button(parent, "Standard grip", 0, 2, () => Begin(3.2f));
            Button(parent, "Low grip", 1, 2, () => Begin(.18f));
            Button(parent, "Feedback grip", 2, 2, () => Begin(.18f, feedback: true));
            Button(parent, "Run / pause", 0, 1, () => { if (_selected < 0) _clock.Paused = !_clock.Paused; });
            Button(parent, "Step", 1, 1, () => { if (_selected < 0) _clock.RequestSingleStep(); });
            Button(parent, "Earlier attempt", 0, 0, () => Select(-1));
            Button(parent, "Later attempt", 1, 0, () => Select(1));
        }

        void Refresh()
        {
            if (_details == null) return;
            var attempt = _selected >= 0 ? _attempts[_selected] : _live;
            var frame = VisibleFrame();
            var f = frame.Reading;
            string policy = attempt.Feedback ? "Contact-gated feedback" : attempt.Cap < 1f ? "Low-force control" : "Fixed reference";
            _details.text = $"Attempt {attempt.Id}  |  {policy}\n" +
                $"Individual  |  Unbound research fixture\n" +
                $"Opposed grip  |  {f.Phase}  |  tick {f.Tick}  |  {attempt.Outcome}\n" +
                $"Pad force {f.Left.NormalForce:F2} / {f.Right.NormalForce:F2} N  |  cap {attempt.Cap:F2} to {frame.Cap:F2} N\n" +
                $"Guide {1000f * f.LeftTravel:F1} / {1000f * f.RightTravel:F1} mm  |  target {1000f * f.TravelTarget:F1} mm\n" +
                $"Command {f.LeftClosingForce:F2} / {f.RightClosingForce:F2} N  |  slip {(f.Left.Slip || f.Right.Slip ? "observed" : "clear")}\n" +
                $"Lift {1000f * f.Clearance:F1} mm  |  held {f.HeldTicks * WorldSimulation.Dt:F2} s\n" +
                $"Within-attempt corrections {frame.Corrections}  |  Research playback";
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
