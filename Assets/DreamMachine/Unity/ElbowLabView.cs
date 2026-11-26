using System;
using System.IO;
using System.Text;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    public sealed class ElbowLabView : MonoBehaviour
    {
        readonly TimeGovernor _clock = new TimeGovernor { Paused = true };
        readonly Transform[] _patches = new Transform[ElbowLab.ReceptorCount];
        readonly Material[] _skin = new Material[ElbowLab.ReceptorCount];
        readonly System.Collections.Generic.List<Material> _materials = new System.Collections.Generic.List<Material>();
        readonly StringBuilder _readout = new StringBuilder(512);
        IElbowFixture _lab;
        public bool UseNativePhysics;
        public bool UsePredictiveMotor;
        PredictiveNativeTrial _predictive;
        TrayWorkspace _workspace;
        Transform _arm, _puck;
        Text _telemetry, _statusLabel;
        Font _font;
        string _status = "";
        float _nextRefresh;
        const string Tray = "embodiment.elbow";
        string Checkpoint => Path.Combine(Application.persistentDataPath,
            UsePredictiveMotor ? "elbow-predictive.checkpoint" :
            UseNativePhysics ? "elbow-native.checkpoint" : "elbow-lab.checkpoint");

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TryFile(InitializeLab);
            var camera = new GameObject("Laboratory Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.transform.position = new Vector3(0.07f, 0.40f, -2f);
            camera.orthographic = true;
            camera.orthographicSize = 0.68f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.08f, 0.11f);
            if (EventSystem.current == null)
            {
                var input = new GameObject("Lab Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.transform.SetParent(transform);
            }
            _arm = new GameObject("Elbow pivot").transform;
            _arm.SetParent(transform);
            Primitive("Fixed shoulder", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * 0.10f, Color.gray);
            Primitive("Forearm", PrimitiveType.Cube, _arm, new Vector3(0.16f, 0, 0),
                new Vector3(0.32f, 0.038f, 0.04f), new Color(0.26f, 0.64f, 0.8f));
            _puck = Primitive("Contact puck", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * 0.09f,
                new Color(0.9f, 0.63f, 0.25f));
            Primitive("Orientation mark", PrimitiveType.Cube, _puck, new Vector3(0.30f, 0, -0.36f),
                new Vector3(0.48f, 0.06f, 0.04f), new Color(0.2f, 0.13f, 0.06f));
            for (int i = 0; i < _patches.Length; i++)
            {
                _patches[i] = Primitive(ElbowLab.ReceptorId(i), PrimitiveType.Sphere, _arm,
                    new Vector3((float)ElbowLab.ArmLength, (float)ElbowLab.RegionCoordinate(i) * 0.032f, -0.04f),
                    Vector3.one * 0.016f, Color.cyan);
                _skin[i] = _patches[i].GetComponent<Renderer>().sharedMaterial;
            }
            _workspace = new TrayWorkspace("DreamMachine.Workspace.v1.");
            _workspace.Register(Tray, "Elbow laboratory", new Vector2(480, 340), new TrayWorkspace.Layout
            {
                AnchorMin = new Vector2(0.03f, 0.61f), AnchorMax = new Vector2(0.97f, 0.98f), Visible = true
            }, BuildTray, () => { _telemetry = null; _statusLabel = null; }, RefreshTray);
            _workspace.RestoreVisibility();
            DrawBody();
        }

        void InitializeLab()
        {
            if (UsePredictiveMotor)
            {
                _predictive = PredictiveNativeTrial.LoadCheckpoint(Checkpoint);
                if (_predictive == null)
                {
                    string path = Path.Combine(Application.persistentDataPath, "native-scratch.predictor");
                    if (!File.Exists(path))
                        throw new InvalidDataException("Train or supply an identity-compatible native-scratch.predictor in " +
                            Application.persistentDataPath);
                    string model = File.ReadAllText(path);
                    ContactPredictor.Restore(model, "son");
                    _predictive = new PredictiveNativeTrial(new NativeElbowLab(ElbowLab.Settings.Standard, "son"),
                        model, MotorCondition.Adaptive);
                }
                _lab = _predictive.Lab;
                return;
            }
            _lab = UseNativePhysics
                ? (IElbowFixture)(NativeElbowLab.LoadCheckpoint(Checkpoint) ?? new NativeElbowLab(ElbowLab.Settings.Standard, "son"))
                : ElbowLab.LoadCheckpoint(Checkpoint) ?? new ElbowLab(ElbowLab.Settings.Standard, "son");
        }

        void Update()
        {
            if (_workspace == null) return;
            if (DreamInput.WasPressed(KeyCode.Tab)) _workspace.Toggle(Tray);
            if (_lab != null && !(_predictive?.Finished ?? _lab.Finished))
            {
                int count = _clock.TakeSingleStep() ? 1 : _clock.Plan(Time.unscaledDeltaTime, WorldSimulation.Dt, 8);
                for (int i = 0; i < count && !(_predictive?.Finished ?? _lab.Finished); i++)
                {
                    if (_predictive == null) _lab.Step();
                    else _predictive.Step();
                }
                if (count > 0) DrawBody();
            }
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.15f;
                _workspace.RefreshVisible();
            }
        }

        Transform Primitive(string label, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(shape);
            item.name = label;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            Destroy(item.GetComponent<Collider>());
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = color };
            item.GetComponent<Renderer>().sharedMaterial = material;
            _materials.Add(material);
            return item.transform;
        }

        void DrawBody()
        {
            if (_lab == null) return;
            _arm.localRotation = Quaternion.Euler(0, 0, (float)_lab.Angle * Mathf.Rad2Deg);
            _puck.localPosition = new Vector3((float)_lab.X, (float)_lab.Y, 0);
            _puck.localRotation = Quaternion.Euler(0, 0, (float)_lab.ObjectAngle * Mathf.Rad2Deg);
            for (int i = 0; i < _skin.Length; i++)
                _skin[i].color = Color.Lerp(new Color(0.23f, 0.5f, 0.6f), new Color(0.98f, 0.38f, 0.24f),
                    (float)(_lab.ReadRegion(i).Force / 12));
        }

        void BuildTray(RectTransform parent)
        {
            _telemetry = AddText(parent, new Vector2(0.03f, 0.23f), new Vector2(0.97f, 0.98f), 19);
            _statusLabel = AddText(parent, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.23f), 16);
            AddButton(parent, 0, "Run / pause", () => _clock.Paused = !_clock.Paused);
            AddButton(parent, 1, "Step", _clock.RequestSingleStep);
            AddButton(parent, 2, "Reverse target", () =>
            {
                if (_lab != null) _lab.RequestTarget(_lab.RequestedTarget > 0 ? -0.55 : 0.9);
            });
            AddButton(parent, 3, "Save", () => TryFile(() =>
            {
                if (_predictive != null) _predictive.SaveCheckpoint(Checkpoint);
                else if (_lab is NativeElbowLab native) native.SaveCheckpoint(Checkpoint);
                else if (_lab is ElbowLab reference) reference.SaveCheckpoint(Checkpoint);
            }));
            AddButton(parent, 4, "Restore", () => TryFile(() =>
            {
                if (UsePredictiveMotor)
                {
                    var restoredTrial = PredictiveNativeTrial.LoadCheckpoint(Checkpoint) ??
                        throw new InvalidDataException("No predictive checkpoint.");
                    _predictive?.Dispose();
                    _predictive = restoredTrial;
                    _lab = restoredTrial.Lab;
                    _clock.Paused = true;
                    DrawBody();
                    return;
                }
                IElbowFixture restored = UseNativePhysics
                    ? (IElbowFixture)NativeElbowLab.LoadCheckpoint(Checkpoint)
                    : ElbowLab.LoadCheckpoint(Checkpoint);
                if (restored == null) throw new InvalidDataException("No lab checkpoint.");
                (_lab as IDisposable)?.Dispose();
                _lab = restored;
                _clock.Paused = true;
                DrawBody();
            }));
            AddButton(parent, 5, "Close", () => _workspace.Toggle(Tray));
        }

        void TryFile(Action action)
        {
            try { action(); _status = ""; }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                _status = error.Message;
                Debug.LogError(error.Message, this);
            }
        }

        void RefreshTray()
        {
            _statusLabel.text = _status;
            if (_lab == null)
            {
                _telemetry.text = "Elbow laboratory · checkpoint unavailable";
                return;
            }
            ElbowLab.Proprioception sensed = _lab.Latest.Feedback;
            _readout.Clear().AppendLine($"Elbow laboratory · {_lab.Backend} · tick {_lab.Tick} · {_lab.SimSeconds:F2} s");
            _readout.AppendLine($"Target {_lab.RequestedTarget:F2} rad · joint {sensed.Angle:F2} rad / {sensed.Velocity:F2} rad/s");
            _readout.AppendLine($"Motor {sensed.Effort:F2} N m · resistance {sensed.Resistance:F2} N m");
            _readout.AppendLine($"Puck ({_lab.X:F3}, {_lab.Y:F3}) m · contact {_lab.Latest.NormalImpulse:F3} N s");
            if (_predictive != null)
            {
                var native = _predictive.Lab;
                var motor = _predictive.Motor;
                _readout.AppendLine($"{motor.Condition} · gain {motor.Gain:F3} · updates {motor.Updates}" +
                    (motor.Stopped ? " · protective stop" : ""));
                if (_predictive.HasFeedback)
                {
                    _readout.AppendLine($"Baseline {native.LastBaselineTorque:F2} · correction {native.LastAppliedCorrection:F2} · final {sensed.Effort:F2} N m");
                    _readout.AppendLine($"Predicted {_predictive.LastProbability:P1} · observed {(_lab.Latest.NormalImpulse > 0 ? 1 : 0)} · error {_predictive.LastError:+0.000;-0.000;0.000}");
                }
            }
            for (int i = 0; i < ElbowLab.ReceptorCount; i++)
            {
                ElbowLab.Receptor region = _lab.ReadRegion(i);
                _readout.AppendLine($"{ElbowLab.ReceptorId(i)} · {region.Force:F2} N · {region.PressureEstimate / 1000:F2} kPa est. · {region.SlipSpeed:F2} m/s");
            }
            _telemetry.text = _readout.ToString();
        }

        Text AddText(Transform parent, Vector2 min, Vector2 max, int size)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = min;
            label.rectTransform.anchorMax = max;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.font = _font;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = size;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        void AddButton(RectTransform parent, int index, string name, UnityEngine.Events.UnityAction action)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(index / 6f + 0.005f, 0.02f);
            rect.anchorMax = new Vector2((index + 1) / 6f - 0.005f, 0.13f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            button.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.3f);
            button.onClick.AddListener(action);
            Text text = AddText(rect, Vector2.zero, Vector2.one, 17);
            text.text = name;
            text.alignment = TextAnchor.MiddleCenter;
        }

        void OnDestroy()
        {
            _workspace?.Dispose();
            (_lab as IDisposable)?.Dispose();
            foreach (Material material in _materials) Destroy(material);
        }
    }
}
