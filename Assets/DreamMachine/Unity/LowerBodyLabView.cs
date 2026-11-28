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
        static readonly float[] Targets = { .60f, .70f, .80f, .85f, .90f };
        readonly TimeGovernor _clock = new TimeGovernor { Paused = true };
        readonly string[] _trace = new string[TraceCapacity];
        readonly Transform[] _segments = new Transform[8];
        readonly Transform[] _axes = new Transform[6];
        readonly Transform[] _rollAxes = new Transform[4];
        readonly Transform[] _contactMarkers = new Transform[2];
        readonly Transform[] _forces = new Transform[2];
        readonly System.Collections.Generic.List<Material> _materials = new System.Collections.Generic.List<Material>();
        NativeLowerBodyLab _lab;
        TrayWorkspace _workspace;
        Transform _com, _xCom, _support;
        Text _telemetry;
        Font _font;
        Vector3 _impulse, _initialPelvis;
        int _seedIndex, _traceCount, _targetIndex = 3, _loadSide;
        float _nextRefresh;
        string _status = "";
        bool _perturbationEnabled;
        LowerBodyController _controller = LowerBodyController.FrontalPose;
        LowerBodyStepProtocol _protocol = LowerBodyStepProtocol.Prepared;

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var camera = new GameObject("Lower-body camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.transform.position = new Vector3(1.5f, 1.20f, 2.6f);
            camera.transform.LookAt(new Vector3(0, .87f, 0));
            camera.orthographic = true;
            camera.orthographicSize = 1.10f;
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
            for (int i = 0; i < _rollAxes.Length; i++) _rollAxes[i] = Shape("Frontal axis " + i, PrimitiveType.Cube, new Color(.80f, .58f, .93f));
            _com = Shape("Measured centre of mass", PrimitiveType.Sphere, new Color(1f, .72f, .24f));
            _com.localScale = Vector3.one * .045f;
            _xCom = Shape("Estimated extrapolated centre of mass", PrimitiveType.Sphere, new Color(.90f, .54f, .91f));
            _xCom.localScale = Vector3.one * .027f;
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
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.02f, .58f), AnchorMax = new Vector2(.98f, .98f), Visible = true },
                BuildTray, () => _telemetry = null, Refresh);
            _workspace.RestoreVisibility();
        }

        void ResetFixture()
        {
            _lab?.Dispose();
            _lab = new NativeLowerBodyLab(Seeds[_seedIndex], _seedIndex == 2 || _seedIndex == 4 ? 3f : 6f,
                controller: _controller, stepProtocol: _protocol);
            _initialPelvis = _lab.PelvisPosition;
            _clock.Paused = true;
            _clock.TakeSingleStep();
            _clock.Accumulator = 0;
            _traceCount = 0;
            Array.Clear(_trace, 0, _trace.Length);
            _loadSide = 0;
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
            _trace[_traceCount++ % TraceCapacity] = LowerBodyTrace.Capture(_lab, "interactive", true);
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
            for (int i = 0; i < _rollAxes.Length; i++)
            {
                int joint = (i / 2) * 3 + (i % 2) * 2;
                Vector3 pivot = _lab.ReadJoint(joint).Position, axis = _lab.RollAxis(joint);
                _rollAxes[i].gameObject.SetActive(_controller != LowerBodyController.Sagittal);
                Line(_rollAxes[i], pivot - axis * .065f, pivot + axis * .065f, .008f);
            }
            _com.position = _lab.CenterOfMass;
            Vector3 estimated = _lab.XCom;
            bool estimateVisible = !float.IsNaN(estimated.x) && !float.IsNaN(estimated.z) &&
                Mathf.Abs(estimated.x) < 1f && Mathf.Abs(estimated.z) < .8f;
            _xCom.gameObject.SetActive(estimateVisible);
            if (estimateVisible) _xCom.position = new Vector3(estimated.x, .012f, estimated.z);
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
                var sole = _lab.ReadSole(i);
                Vector3 point = sole.PressureValid ? sole.PressureCenter : _lab.ContactPoint(i);
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
            _telemetry = Label(parent, new Vector2(.025f, .35f), new Vector2(.985f, .98f), 17, TextAnchor.UpperLeft);
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
            Button(parent, 2, 0, "Controller", () => { _controller = (LowerBodyController)(((int)_controller + 1) % 4); ResetFixture(); });
            Button(parent, 2, 1, "Next target", () =>
            {
                _targetIndex = (_targetIndex + 1) % Targets.Length;
                RequestLoad(_loadSide, "Transfer target changed");
            });
            Button(parent, 2, 2, "Request swing", () => _status = _lab.TryRequestSwing() ? "Swing requested" : $"Swing request · {_lab.Action}");
            Button(parent, 2, 3, "Step protocol", () => { _protocol = (LowerBodyStepProtocol)(((int)_protocol + 1) % 5); ResetFixture(); });
        }

        void RequestLoad(float load, string status)
        {
            _loadSide = (int)load;
            _lab.RequestLoad(load * (Targets[_targetIndex] - .5f) / .4f);
            _status = status;
        }

        void Refresh()
        {
            var message = new StringBuilder(640);
            message.AppendLine($"Lower body · {_controller} / {_protocol} · seed {_lab.Seed} · {_lab.Tick * WorldSimulation.Dt:F2} s · tick {_lab.Tick} · {(_clock.Paused ? "paused" : "running")} · F6 tray");
            message.AppendLine($"Pelvis {_lab.PelvisPosition.y:F3} m · displacement {Vector3.Distance(_lab.PelvisPosition, _initialPelvis):F4} m · tilt {_lab.PelvisTilt:F1}° · COM {_lab.CenterOfMass:F3}");
            var l = _lab.ReadSole(0);
            var r = _lab.ReadSole(1);
            message.AppendLine($"Measured load L {_lab.ReadLoad(0):P0} / R {_lab.ReadLoad(1):P0} · stance {_lab.ReadSole(_lab.DynamicStanceSide).Impulse.y:F2} N s · requested {_lab.TargetShare:P0} · {_lab.StanceTicks}/{NativeLowerBodyLab.RequiredStanceTicks} ticks");
            message.AppendLine($"CoP L {PressureLabel(l)} / R {PressureLabel(r)} · clearance {l.Clearance:F3} / {r.Clearance:F3} m · caps {(_lab.Saturated ? "hit" : "clear")}");
            message.AppendLine($"Estimated XCoM {_lab.XCom:F3} m · margins lateral {Margin(_lab.FrontalMargin)} / forward {Margin(_lab.SagittalMargin)} · lateral momentum {_lab.Momentum.x:F3} N s");
            message.AppendLine($"{_lab.MotionPhase} · {_lab.TransitionReason} · prelift {_lab.PreliftTicks}/{DreamMachine.Body.DynamicSupport.PreliftTicks} · {_lab.PreliftFailures}");
            var v = _lab.Vestibular;
            message.AppendLine($"Vestibular {v.Tick} · gravity {v.Gravity:F2} m/s² · gyro {v.AngularVelocity:F2} rad/s · acceleration {v.LinearAcceleration:F2} m/s²");
            for (int i = 0; i < _lab.JointCount; i++)
            {
                var j = _lab.ReadJoint(i);
                var p = _lab.ReadProprioception(i);
                string frontal = p.RollLimit > 0 ? $" · roll {p.Roll:F3}/{p.RollTarget:F3} rad, {p.RollTorque:F2} N m" : "";
                message.AppendLine($"{j.Name} · pitch {j.Angle:F3}/{j.Target:F3} rad, {j.Torque:F2} N m{frontal}");
            }
            string fall = _lab.FirstFallTick < 0 ? "" : $" · fall {_lab.FirstFallTick}, peak impact {_lab.PeakBodyImpact:F1} N s";
            message.Append($"Stance foot offset {_lab.ReadSole(_lab.DynamicStanceSide).Drift:F4} m · work {_lab.AbsoluteWork:F3} J · body contacts {_lab.NonFootContacts} · swing {_lab.SwingClearance:F3}/{_lab.SwingTravel:F3} m · {_lab.ActionReason}{fall} · {_status}");
            _telemetry.text = message.ToString();
        }

        static string PressureLabel(SoleReading foot) => foot.PressureValid ? $"{foot.PressureCenter:F3}" : "—";
        static string Margin(float metres) => float.IsNaN(metres) ? "—" : $"{metres * 1000f:F1} mm";

        void Export()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, $"lower-body-{_lab.Seed}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.csv");
                using var writer = new StreamWriter(path);
                LowerBodyTrace.WriteHeader(writer, true);
                for (int i = Math.Max(0, _traceCount - TraceCapacity); i < _traceCount; i++)
                    writer.WriteLine(_trace[i % TraceCapacity]);
                _status = "Exported " + Path.GetFileName(path);
            }
            catch (IOException error) { _status = error.Message; }
            catch (UnauthorizedAccessException error) { _status = error.Message; }
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
            rect.anchorMin = new Vector2(column * .2f + .006f, .02f + row * .105f);
            rect.anchorMax = new Vector2((column + 1) * .2f - .006f, .115f + row * .105f);
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
