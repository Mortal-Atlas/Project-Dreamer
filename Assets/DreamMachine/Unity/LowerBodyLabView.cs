using System;
using System.IO;
using System.Text;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    public sealed class LowerBodyLabView : MonoBehaviour
    {
        const string Tray = "embodiment.lower-body";
        const int TraceCapacity = 2048;
        static readonly int[] Seeds = { 101, 202, 303, 404, 505 };
        readonly TimeGovernor _clock = new TimeGovernor { Paused = true };
        readonly TracePoint[] _trace = new TracePoint[TraceCapacity];
        readonly Transform[] _segments = new Transform[8];
        readonly Transform[] _axes = new Transform[6];
        readonly Transform[] _contactMarkers = new Transform[2];
        readonly Transform[] _forces = new Transform[2];
        readonly System.Collections.Generic.List<Material> _materials = new System.Collections.Generic.List<Material>();
        NativeLowerBodyLab _lab;
        TrayWorkspace _workspace;
        Transform _com, _support;
        Text _telemetry;
        Font _font;
        Vector3 _impulse, _initialPelvis;
        int _seedIndex, _traceCount;
        float _nextRefresh;
        string _status = "";
        bool _perturbationEnabled;

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var camera = new GameObject("Lower-body camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.transform.position = new Vector3(1.5f, 1.20f, 2.6f);
            camera.transform.LookAt(new Vector3(0, .76f, 0));
            camera.orthographic = true;
            camera.orthographicSize = .96f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .075f, .095f);
            if (EventSystem.current == null)
            {
                var input = new GameObject("Laboratory input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.transform.SetParent(transform);
            }
            var floor = Shape("Reference floor", PrimitiveType.Cube, new Color(.13f, .18f, .23f));
            floor.position = new Vector3(0, -.025f, 0);
            floor.localScale = new Vector3(1.8f, .05f, 1.3f);
            for (int i = 0; i < _segments.Length; i++)
                _segments[i] = Shape("Segment " + i, PrimitiveType.Cube, i == 1 ? new Color(.89f, .58f, .24f) :
                    i < 5 ? new Color(.24f, .63f, .77f) : new Color(.33f, .71f, .55f));
            for (int i = 0; i < _axes.Length; i++) _axes[i] = Shape("Joint axis " + i, PrimitiveType.Cube, Color.white);
            _com = Shape("Measured centre of mass", PrimitiveType.Sphere, new Color(1f, .72f, .24f));
            _com.localScale = Vector3.one * .045f;
            _support = Shape("Footprint envelope", PrimitiveType.Cube, new Color(.25f, .34f, .46f));
            for (int i = 0; i < 2; i++)
            {
                _contactMarkers[i] = Shape("Measured contact " + i, PrimitiveType.Sphere, new Color(.95f, .68f, .25f));
                _contactMarkers[i].localScale = Vector3.one * .026f;
                _forces[i] = Shape("Mean reaction " + i, PrimitiveType.Cube, new Color(.95f, .68f, .25f));
            }
            ResetFixture();
            _workspace = new TrayWorkspace("DreamMachine.Workspace.v1.");
            _workspace.Register(Tray, "Lower-body laboratory", new Vector2(480, 300),
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.02f, .64f), AnchorMax = new Vector2(.98f, .98f), Visible = true },
                BuildTray, () => _telemetry = null, Refresh);
            _workspace.RestoreVisibility();
        }

        void ResetFixture()
        {
            _lab?.Dispose();
            _lab = new NativeLowerBodyLab(Seeds[_seedIndex], _seedIndex == 2 || _seedIndex == 4 ? 3f : 6f);
            _initialPelvis = _lab.PelvisPosition;
            _clock.Paused = true;
            _clock.TakeSingleStep();
            _clock.Accumulator = 0;
            _traceCount = 0;
            _impulse = Vector3.zero;
            _perturbationEnabled = false;
            _status = "Fixture reset";
            Draw();
        }

        void Update()
        {
            if (_workspace == null) return;
            if (DreamInput.WasPressed(KeyCode.F6)) _workspace.Toggle(Tray);
            int count = _clock.TakeSingleStep() ? 1 : _clock.Plan(Time.unscaledDeltaTime, WorldSimulation.Dt, 8);
            for (int i = 0; i < count; i++) StepFixture();
            if (count > 0) Draw();
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + .2f;
                _workspace.RefreshVisible();
            }
        }

        void StepFixture()
        {
            _lab.Step(_impulse);
            _impulse = Vector3.zero;
            _trace[_traceCount++ % TraceCapacity] = new TracePoint(_lab);
        }

        void Draw()
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                var b = _lab.ReadBody(i);
                _segments[i].SetPositionAndRotation(b.Position, b.Rotation);
                _segments[i].localScale = b.Size;
            }
            for (int i = 0; i < _axes.Length; i++)
            {
                var j = _lab.ReadJoint(i);
                Line(_axes[i], j.Position - j.Axis * .065f, j.Position + j.Axis * .065f, .009f);
            }
            _com.position = _lab.CenterOfMass;
            bool boundsSet = false;
            var envelope = new Bounds();
            for (int i = 0; i < _forces.Length; i++)
            {
                bool contact = (_lab.FootContactMask & (1u << i)) != 0;
                _contactMarkers[i].gameObject.SetActive(contact);
                _forces[i].gameObject.SetActive(contact);
                if (!contact) continue;
                var foot = _lab.FootBounds(i);
                if (boundsSet) envelope.Encapsulate(foot);
                else envelope = foot;
                boundsSet = true;
                Vector3 point = _lab.ContactPoint(i);
                Vector3 meanForce = (i == 0 ? _lab.LeftImpulse : _lab.RightImpulse) / WorldSimulation.Dt;
                _contactMarkers[i].position = point;
                Line(_forces[i], point, point + meanForce * .0023f, .008f);
            }
            _support.gameObject.SetActive(boundsSet);
            if (boundsSet)
            {
                _support.position = new Vector3(envelope.center.x, .0015f, envelope.center.z);
                _support.localScale = new Vector3(envelope.size.x, .002f, envelope.size.z);
            }
        }

        static void Line(Transform line, Vector3 a, Vector3 b, float width)
        {
            Vector3 end = b - a;
            line.position = (a + b) * .5f;
            line.rotation = end.sqrMagnitude > 1e-12f ? Quaternion.FromToRotation(Vector3.up, end) : Quaternion.identity;
            line.localScale = new Vector3(width, end.magnitude, width);
        }

        Transform Shape(string name, PrimitiveType type, Color color)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(transform, false);
            Collider decoration = item.GetComponent<Collider>();
            decoration.enabled = false;
            Destroy(decoration);
            Shader shader = Shader.Find("DreamMachine/ColorUnlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            item.GetComponent<Renderer>().sharedMaterial = material;
            _materials.Add(material);
            return item.transform;
        }

        void BuildTray(RectTransform parent)
        {
            _telemetry = Label(parent, new Vector2(.025f, .29f), new Vector2(.985f, .98f), 18, TextAnchor.UpperLeft);
            Button(parent, 0, 0, "Pause / run", () => _clock.Paused = !_clock.Paused);
            Button(parent, 0, 1, "Step", _clock.RequestSingleStep);
            Button(parent, 0, 2, "Load left", () => RequestLoad(-1, "Left transfer"));
            Button(parent, 0, 3, "Centre", () => RequestLoad(0, "Centred support"));
            Button(parent, 0, 4, "Load right", () => RequestLoad(1, "Right transfer"));
            Button(parent, 1, 0, "Next body", () => { _seedIndex = (_seedIndex + 1) % Seeds.Length; ResetFixture(); });
            Button(parent, 1, 1, "Reset lab", ResetFixture);
            Button(parent, 1, 2, "Perturbation", () =>
            {
                _perturbationEnabled = !_perturbationEnabled;
                _status = _perturbationEnabled ? "Lateral research impulse armed" : "Perturbations disabled";
            });
            Button(parent, 1, 3, "Tap right", () =>
            {
                if (_perturbationEnabled) _impulse = Vector3.right * .35f;
            });
            Button(parent, 1, 4, "Export", Export);
        }

        void RequestLoad(float load, string status)
        {
            _lab.RequestLoad(load);
            _status = status;
        }

        void Refresh()
        {
            var message = new StringBuilder(640);
            message.AppendLine($"Native lower body · seed {_lab.Seed} · stage {_lab.Stage:F0} · tick {_lab.Tick} · {_lab.Tick * WorldSimulation.Dt:F2} s · {(_clock.Paused ? "paused" : "running")} · F6 toggles tray");
            message.AppendLine($"Pelvis {_lab.PelvisPosition.y:F3} m · displacement {Vector3.Distance(_lab.PelvisPosition, _initialPelvis):F4} m · tilt {_lab.PelvisTilt:F1}° · COM {_lab.CenterOfMass:F3}");
            message.AppendLine($"Left {_lab.LeftImpulse.y:F3} / right {_lab.RightImpulse.y:F3} N s · momentum residual {_lab.MomentumResidual.magnitude:F4} N s");
            for (int i = 0; i < _lab.JointCount; i++)
            {
                var j = _lab.ReadJoint(i);
                message.AppendLine($"{j.Name} · measured {j.Angle:F3} / target {j.Target:F3} rad · {j.Torque:F2} N m");
            }
            message.Append($"Slip {_lab.FootDrift:F4} m · work {_lab.AbsoluteWork:F3} J · body contacts {_lab.NonFootContacts} · {_status}");
            _telemetry.text = message.ToString();
        }

        void Export()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, $"lower-body-{_lab.Seed}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.csv");
                using var writer = new StreamWriter(path);
                writer.WriteLine("tick,load,ballast_m,left_ns,right_ns,pelvis_m,com_x_m,residual_ns,slip_m,abs_work_j,non_foot_contacts,tap_x_ns,left_foot_x_m,left_foot_y_m,left_foot_z_m,right_foot_x_m,right_foot_y_m,right_foot_z_m");
                for (int i = Math.Max(0, _traceCount - TraceCapacity); i < _traceCount; i++)
                    writer.WriteLine(_trace[i % TraceCapacity].Csv());
                _status = "Exported " + Path.GetFileName(path);
            }
            catch (IOException error) { _status = error.Message; }
            catch (UnauthorizedAccessException error) { _status = error.Message; }
        }

        readonly struct TracePoint
        {
            readonly int _tick, _contacts;
            readonly float _load, _ballast, _left, _right, _pelvis, _com, _residual, _slip, _work, _tap;
            readonly Vector3 _leftFoot, _rightFoot;

            internal TracePoint(NativeLowerBodyLab lab)
            {
                _tick = lab.Tick; _contacts = lab.NonFootContacts;
                _load = lab.RequestedLoad; _ballast = lab.BallastOffset;
                _left = lab.LeftImpulse.y; _right = lab.RightImpulse.y;
                _pelvis = lab.PelvisPosition.y; _com = lab.CenterOfMass.x;
                _residual = lab.MomentumResidual.magnitude; _slip = lab.FootDrift;
                _work = lab.AbsoluteWork; _tap = lab.ExternalImpulse.x;
                _leftFoot = lab.FootBounds(0).center; _rightFoot = lab.FootBounds(1).center;
            }

            internal string Csv() => FormattableString.Invariant(
                $"{_tick},{_load:R},{_ballast:R},{_left:R},{_right:R},{_pelvis:R},{_com:R},{_residual:R},{_slip:R},{_work:R},{_contacts},{_tap:R},{_leftFoot.x:R},{_leftFoot.y:R},{_leftFoot.z:R},{_rightFoot.x:R},{_rightFoot.y:R},{_rightFoot.z:R}");
        }

        Text Label(RectTransform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment)
        {
            var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = min; text.rectTransform.anchorMax = max;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = _font; text.fontSize = size;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
            text.alignment = alignment; text.color = Color.white; text.raycastTarget = false;
            return text;
        }

        void Button(RectTransform parent, int row, int column, string caption, UnityEngine.Events.UnityAction action)
        {
            var b = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            var rect = (RectTransform)b.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(column * .2f + .006f, .02f + row * .13f);
            rect.anchorMax = new Vector2((column + 1) * .2f - .006f, .14f + row * .13f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            b.GetComponent<Image>().color = new Color(.20f, .26f, .34f);
            b.onClick.AddListener(action);
            Label(rect, Vector2.zero, Vector2.one, 18, TextAnchor.MiddleCenter).text = caption;
        }

        void OnDestroy()
        {
            _workspace?.Dispose();
            _lab?.Dispose();
            foreach (Material material in _materials) Destroy(material);
        }
    }
}
