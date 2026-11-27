using System.Collections.Generic;
using DreamMachine.Body;
using DreamMachine.Social;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Stylized low-poly person. Limbs are rigid capsules and boxes, not a skinned mesh.
    /// Looks come from AppearanceProfile so they can be edited or replaced later.
    /// </summary>
    public sealed class SegmentedBody : MonoBehaviour
    {
        public string PersonId;
        public bool IsLeader;
        readonly Dictionary<string, Transform> _pivots = new Dictionary<string, Transform>();
        TextMesh _label;
        Transform _rootSpin;
        Transform _pelvis, _chest, _neck, _head, _torsoMesh, _neckMesh;
        readonly List<Limb> _limbs = new List<Limb>(4);
        HumanoidAnatomy _anatomy;
        float _sampledAge = -1f;
        Transform _leftHip, _leftKnee, _leftAnkle, _rightHip, _rightKnee, _rightAnkle;
        float _seatAmount, _sleepAmount;

        public HumanoidDimensions Dimensions { get; private set; }
        public int AnatomySeed => _anatomy.BirthSeed;
        public float SampledDevelopmentDays => _sampledAge;
        public bool Walking { get; private set; }
        public bool LeftSupport { get; private set; }
        public bool RightSupport { get; private set; }
        public float LastDisplacementMetres { get; private set; }

        internal sealed class Limb
        {
            public bool Arm;
            public float Side;
            public Transform Root, Hinge, End, Grip, FirstMesh, SecondMesh, EndMesh;
            public Transform[] Digits;
            public Transform Sole;
        }

        internal void BindAnatomy(HumanoidAnatomy anatomy, Transform pelvis, Transform chest, Transform neck,
            Transform head, Transform torsoMesh, Transform neckMesh)
        {
            _anatomy = anatomy;
            _pelvis = pelvis;
            _chest = chest;
            _neck = neck;
            _head = head;
            _torsoMesh = torsoMesh;
            _neckMesh = neckMesh;
        }

        internal void AddLimb(Limb limb) => _limbs.Add(limb);

        /// <summary>View-only sampling. The kernel owns age and the anatomy curve.</summary>
        public void ApplyDevelopmentAge(float developmentDays)
        {
            float age = Mathf.Floor(Mathf.Clamp(developmentDays, 0f, HumanoidAnatomy.AdultAgeDays) * 64f) / 64f;
            if (age == _sampledAge) return;
            _sampledAge = age;
            HumanoidDimensions d = _anatomy.AtAge(age);
            Dimensions = d;
            _pelvis.localPosition = Vector3.up * d.PelvisHeight;
            _chest.localPosition = Vector3.up * (d.Torso * 0.82f);
            _neck.localPosition = Vector3.up * (d.Torso * 0.18f);
            _head.localPosition = Vector3.up * (d.Neck + d.Head * 0.5f);
            _head.localScale = Vector3.one * (d.Head / 0.24f);
            _torsoMesh.localScale = new Vector3(d.ShoulderWidth, d.Torso, d.HipWidth);
            _neckMesh.localPosition = Vector3.up * (d.Neck * 0.5f);
            _neckMesh.localScale = new Vector3(d.Head * 0.42f, d.Neck * 0.5f, d.Head * 0.42f);
            if (_label != null) _label.transform.localPosition = Vector3.up * (d.Height + 0.16f);
            foreach (Limb limb in _limbs)
            {
                float first = limb.Arm ? d.UpperArm : d.Thigh;
                float second = limb.Arm ? d.Forearm : d.Shin;
                limb.Root.localPosition = Vector3.right * (limb.Side * (limb.Arm ? d.ShoulderWidth : d.HipWidth) * 0.5f);
                limb.Hinge.localPosition = Vector3.down * first;
                limb.End.localPosition = Vector3.down * second;
                ResizeCapsule(limb.FirstMesh, first, limb.Arm ? d.ShoulderWidth * 0.23f : d.HipWidth * 0.48f);
                ResizeCapsule(limb.SecondMesh, second, limb.Arm ? d.ShoulderWidth * 0.18f : d.HipWidth * 0.34f);
                if (limb.Arm)
                {
                    limb.EndMesh.localPosition = Vector3.down * (d.Hand * 0.5f);
                    limb.EndMesh.localScale = new Vector3(d.Hand * 0.58f, d.Hand, d.Hand * 0.44f);
                    limb.Grip.localPosition = limb.EndMesh.localPosition;
                    for (int i = 0; i < limb.Digits.Length; i++)
                    {
                        float x = limb.Side * (i - 2) * d.Hand * 0.11f;
                        limb.Digits[i].localPosition = new Vector3(x, -d.Hand * (i == 0 ? 0.48f : 0.86f), 0f);
                    }
                }
                else
                {
                    limb.EndMesh.localPosition = new Vector3(0f, -d.AnkleHeight * 0.5f, d.Foot * 0.24f);
                    limb.EndMesh.localScale = new Vector3(d.Foot * 0.47f, d.AnkleHeight, d.Foot);
                    limb.Sole.localPosition = new Vector3(0f, -d.AnkleHeight, d.Foot * 0.24f);
                }
            }
        }

        static void ResizeCapsule(Transform mesh, float length, float diameter)
        {
            mesh.localPosition = Vector3.down * (length * 0.5f);
            mesh.localScale = new Vector3(diameter, length * 0.5f, diameter);
        }

        public Transform RigPivot(string joint) => _pivots.TryGetValue(joint, out Transform pivot) ? pivot : null;

        public void Bind(Transform rootSpin, TextMesh label)
        {
            _rootSpin = rootSpin;
            _label = label;
        }

        public void SetPivot(string joint, Transform pivot)
        {
            _pivots[joint] = pivot;
        }

        public void Apply(float[] radians, float yawDegrees)
        {
            if (_rootSpin != null)
            {
                _rootSpin.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            }
            if (radians == null) return;
            for (int i = 0; i < radians.Length && i < JointNames.All.Length; i++)
            {
                SetJoint(JointNames.All[i], radians[i]);
            }
        }

        public void ApplyGroundedGait(float distance, float displacement, float yaw)
        {
            if (_leftHip == null)
            {
                _leftHip = RigPivot("leftHip"); _leftKnee = RigPivot("leftKnee"); _leftAnkle = RigPivot("leftAnkle");
                _rightHip = RigPivot("rightHip"); _rightKnee = RigPivot("rightKnee"); _rightAnkle = RigPivot("rightAnkle");
            }
            LastDisplacementMetres = displacement;
            Walking = displacement > 0.0001f;
            _rootSpin.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _pelvis.localPosition = Vector3.up * GroundedGait.PelvisHeight(Dimensions, Walking);
            var left = GroundedGait.Sample(Dimensions, distance, Walking, true);
            var right = GroundedGait.Sample(Dimensions, distance, Walking, false);
            LeftSupport = left.Supports;
            RightSupport = right.Supports;
            SetLeg(_leftHip, _leftKnee, _leftAnkle, left);
            SetLeg(_rightHip, _rightKnee, _rightAnkle, right);
            if (Walking)
            {
                float stride = GroundedGait.Stride(Dimensions);
                SetJoint("leftShoulder", left.Forward / stride * 1.1f);
                SetJoint("rightShoulder", right.Forward / stride * 1.1f);
            }
        }

        static void SetLeg(Transform hip, Transform knee, Transform ankle, GroundedGait.Leg pose)
        {
            hip.localRotation = Quaternion.Euler(pose.Hip * Mathf.Rad2Deg, 0f, 0f);
            knee.localRotation = Quaternion.Euler(pose.Knee * Mathf.Rad2Deg, 0f, 0f);
            ankle.localRotation = Quaternion.Euler(pose.Ankle * Mathf.Rad2Deg, 0f, 0f);
        }

        public void ApplyHouseholdPose(Vector3? target, bool reach, bool seated, bool sleeping, float dt, float time)
        {
            float blend = 1f - Mathf.Exp(-8f * dt);
            _seatAmount = Mathf.Lerp(_seatAmount, seated ? 1f : 0f, blend);
            _sleepAmount = Mathf.Lerp(_sleepAmount, sleeping ? 1f : 0f, blend);
            if (!Walking)
            {
                float seatedHeight = Dimensions.AnkleHeight + Dimensions.Shin;
                _pelvis.localPosition = Vector3.up * Mathf.Lerp(Mathf.Lerp(_pelvis.localPosition.y, seatedHeight, _seatAmount), .36f, _sleepAmount);
                float bend = _seatAmount * (1f - _sleepAmount);
                _leftHip.localRotation = Quaternion.Slerp(_leftHip.localRotation, Quaternion.Euler(-89f, 0f, 0f), bend);
                _rightHip.localRotation = Quaternion.Slerp(_rightHip.localRotation, Quaternion.Euler(-89f, 0f, 0f), bend);
                _leftKnee.localRotation = Quaternion.Slerp(_leftKnee.localRotation, Quaternion.Euler(89f, 0f, 0f), bend);
                _rightKnee.localRotation = Quaternion.Slerp(_rightKnee.localRotation, Quaternion.Euler(89f, 0f, 0f), bend);
            }
            _pelvis.localRotation = Quaternion.Euler(-90f * _sleepAmount, 0f, 0f);
            Quaternion head = Quaternion.identity;
            if (target.HasValue && !sleeping)
            {
                Vector3 aim = _head.parent.InverseTransformDirection(target.Value - _head.position);
                if (aim.sqrMagnitude > .0001f)
                {
                    float yaw = Mathf.Clamp(Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, -58f, 58f);
                    float pitch = Mathf.Clamp(-Mathf.Atan2(aim.y, new Vector2(aim.x, aim.z).magnitude) * Mathf.Rad2Deg, -20f, 32f);
                    head = Quaternion.Euler(pitch, yaw, 0f);
                }
                if (reach && !Walking)
                {
                    Transform shoulder = RigPivot("rightShoulder");
                    Vector3 handAim = shoulder.parent.InverseTransformDirection(target.Value - shoulder.position);
                    if (handAim.sqrMagnitude > .0001f)
                        shoulder.localRotation = Quaternion.Slerp(shoulder.localRotation, Quaternion.FromToRotation(Vector3.down, handAim.normalized), .72f);
                    SetJoint("rightElbow", -.36f);
                }
            }
            _head.localRotation = Quaternion.Slerp(_head.localRotation, head, blend);
            float blinkTime = Mathf.Repeat(time + (AnatomySeed % 11) * .29f, 4.7f);
            float blink = sleeping ? .12f : blinkTime < .14f ? Mathf.Lerp(.12f, 1f, Mathf.Abs(blinkTime - .07f) / .07f) : 1f;
            RigPivot("leftEye").localScale = new Vector3(1f, blink, 1f);
            RigPivot("rightEye").localScale = new Vector3(1f, blink, 1f);
        }

        public void SetJoint(string joint, float radians)
        {
            Transform pivot;
            if (!_pivots.TryGetValue(joint, out pivot) || pivot == null) return;
            pivot.localRotation = Quaternion.Euler(radians * Mathf.Rad2Deg, 0f, 0f);
        }

        /// <summary>
        /// What another mind is allowed to perceive: bone angles, not animation parameters.
        /// </summary>
        public float[] Measure()
        {
            var angles = new float[JointNames.Count];
            for (int i = 0; i < JointNames.All.Length; i++)
            {
                Transform pivot;
                if (!_pivots.TryGetValue(JointNames.All[i], out pivot) || pivot == null) continue;
                float degrees = pivot.localEulerAngles.x;
                if (degrees > 180f) degrees -= 360f;
                angles[i] = degrees * Mathf.Deg2Rad;
            }
            return angles;
        }

        public void SetCaption(string text, Color color)
        {
            if (_label == null) return;
            _label.text = text;
            _label.color = color;
        }

    }

    public static class CharacterFactory
    {
        public static SegmentedBody Build(Transform parent, AppearanceProfile look, Material source,
            HumanoidAnatomy anatomy, float developmentDays)
        {
            var root = new GameObject(look.DisplayName);
            root.transform.SetParent(parent, false);
            var body = root.AddComponent<SegmentedBody>();
            body.PersonId = look.Id;
            body.IsLeader = look.IsLeader;

            var spin = new GameObject("Spin").transform;
            spin.SetParent(root.transform, false);
            body.Bind(spin, null);

            const float headR = 0.12f;
            Color skin = Hex(look.Skin);
            Color hair = Hex(look.Hair);
            Color cloth = Hex(look.Cloth);
            Color accent = Hex(look.Accent);

            Transform pelvis = Pivot(body, spin, "pelvis", Vector3.zero);
            Transform torso = Pivot(body, pelvis, "torso", Vector3.zero);
            Transform chest = Pivot(body, torso, "chest", Vector3.zero);
            Transform neck = Pivot(body, chest, "neck", Vector3.zero);
            Transform torsoGeometry = Pivot(body, torso, "torsoGeometry", Vector3.zero);
            Primitive(torsoGeometry, PrimitiveType.Capsule, source, cloth, Vector3.up * 0.51f,
                new Vector3(0.92f, 0.49f, 0.58f), 0, true);
            if (look.Outfit == "wrap" || look.Outfit == "robe")
            {
                Primitive(torsoGeometry, PrimitiveType.Cube, source, cloth,
                    new Vector3(0f, 0.16f, 0f), new Vector3(0.94f, 0.29f, 0.62f), 0);
            }
            if (look.IsLeader)
            {
                Primitive(torsoGeometry, PrimitiveType.Cube, source, accent, new Vector3(0f, 0.50f, -0.30f), new Vector3(0.91f, 0.66f, 0.10f), 0);
                Primitive(torsoGeometry, PrimitiveType.Cube, source, accent, new Vector3(0f, 0.58f, 0.32f), new Vector3(0.29f, 0.48f, 0.08f), 0);
            }

            Transform head = Pivot(body, neck, "head", Vector3.zero);
            Transform neckMesh = Primitive(neck, PrimitiveType.Capsule, source, skin, Vector3.zero, Vector3.one, 0, true);
            Primitive(head, PrimitiveType.Sphere, source, skin, Vector3.zero, Vector3.one * headR * 2f, 0, true);
            AddFace(body, head, source, skin, hair, headR, look);
            AddHair(head, source, hair, accent, headR, look);

            BuildLimb(body, chest, source, skin, cloth, "left", -1f, true);
            BuildLimb(body, chest, source, skin, cloth, "right", 1f, true);
            BuildLimb(body, pelvis, source, skin, cloth, "left", -1f, false);
            BuildLimb(body, pelvis, source, skin, cloth, "right", 1f, false);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.up * 2.15f;
            var label = labelGo.AddComponent<TextMesh>();
            label.text = look.IsLeader ? look.DisplayName + "  ★" : look.DisplayName;
            label.characterSize = 0.025f;
            label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = look.IsLeader ? accent : Color.white;
            body.Bind(spin, label);

            body.BindAnatomy(anatomy, pelvis, chest, neck, head, torsoGeometry, neckMesh);
            body.ApplyDevelopmentAge(developmentDays);
            return body;
        }

        static void BuildLimb(SegmentedBody body, Transform parent, Material source, Color skin, Color cloth,
            string side, float sign, bool arm)
        {
            if (arm) parent = Pivot(body, parent, side + "Clavicle", Vector3.zero);
            Transform root = Pivot(body, parent, side + (arm ? "Shoulder" : "Hip"), Vector3.zero);
            Transform hinge = Pivot(body, root, side + (arm ? "Elbow" : "Knee"), Vector3.zero);
            Transform end = Pivot(body, hinge, side + (arm ? "Wrist" : "Ankle"), Vector3.zero);
            Transform tip = Pivot(body, end, side + (arm ? "Hand" : "Foot"), Vector3.zero);
            Transform[] palms = null;
            if (arm)
            {
                string[] digitNames = { "Thumb", "Index", "Middle", "Ring", "Little" };
                palms = new Transform[digitNames.Length];
                for (int i = 0; i < palms.Length; i++)
                    palms[i] = Pivot(body, tip, side + digitNames[i], Vector3.zero);
            }
            body.AddLimb(new SegmentedBody.Limb
            {
                Arm = arm, Side = sign, Root = root, Hinge = hinge, End = end,
                Digits = palms,
                Sole = arm ? null : Pivot(body, tip, side + "Sole", Vector3.zero),
                Grip = arm ? Pivot(body, tip, side + "Grip", Vector3.zero) : null,
                FirstMesh = Primitive(root, PrimitiveType.Capsule, source, cloth, Vector3.zero, Vector3.one, 0, true),
                SecondMesh = Primitive(hinge, PrimitiveType.Capsule, source, skin, Vector3.zero, Vector3.one, 0, true),
                EndMesh = Primitive(tip, arm ? PrimitiveType.Sphere : PrimitiveType.Cube, source,
                    arm ? skin : Hex("#2B2B2B"), Vector3.zero, Vector3.one, 0, true)
            });
        }

        static void AddFace(SegmentedBody body, Transform head, Material source, Color skin, Color hair, float headR, AppearanceProfile look)
        {
            Color eyeWhite = Hex("#F7F4EF");
            Color pupil = Hex("#1B1B1B");
            Color mouth = Hex("#8C3B4A");
            float z = headR * 0.78f;
            Pivot(body, head, "gaze", new Vector3(0f, headR * 0.12f, headR * 1.06f));
            Pivot(body, head, "leftEar", Vector3.left * headR);
            Pivot(body, head, "rightEar", Vector3.right * headR);
            Transform jaw = Pivot(body, head, "jaw", new Vector3(0f, -headR * 0.28f, z));
            Transform leftEye = Pivot(body, head, "leftEye", new Vector3(-headR * 0.32f, headR * 0.12f, z));
            Transform rightEye = Pivot(body, head, "rightEye", new Vector3(headR * 0.32f, headR * 0.12f, z));
            foreach (Transform eye in new[] { leftEye, rightEye })
            {
                Primitive(eye, PrimitiveType.Sphere, source, eyeWhite, Vector3.zero, Vector3.one * headR * 0.38f, 0);
                Primitive(eye, PrimitiveType.Sphere, source, pupil, Vector3.forward * (headR * 0.12f), Vector3.one * headR * 0.18f, 0);
            }
            Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(-headR * 0.32f, headR * 0.38f, z), new Vector3(headR * 0.28f, headR * 0.08f, headR * 0.08f), 0);
            Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(headR * 0.32f, headR * 0.38f, z), new Vector3(headR * 0.28f, headR * 0.08f, headR * 0.08f), 0);
            Primitive(jaw, PrimitiveType.Cube, source, mouth, Vector3.zero, new Vector3(headR * 0.36f, headR * 0.08f, headR * 0.08f), 0);
            if (look.FacialHair == "short-beard")
            {
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(0f, -headR * 0.55f, z * 0.7f), new Vector3(headR * 0.7f, headR * 0.28f, headR * 0.35f), 0);
            }
        }

        static void AddHair(Transform head, Material source, Color hair, Color accent, float headR, AppearanceProfile look)
        {
            string style = look.HairStyle ?? "crop";
            if (style == "crown" || look.IsLeader)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    var pos = new Vector3(Mathf.Cos(a) * headR * 0.95f, headR * 0.55f, Mathf.Sin(a) * headR * 0.95f);
                    Primitive(head, PrimitiveType.Cube, source, accent, pos, Vector3.one * headR * 0.22f, 0);
                }
            }
            if (style == "long")
            {
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(0f, headR * 0.2f, -headR * 0.2f), new Vector3(headR * 1.5f, headR * 0.35f, headR * 1.1f), 0);
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(-headR * 0.7f, -headR * 0.8f, -headR * 0.1f), new Vector3(headR * 0.28f, headR * 1.6f, headR * 0.28f), 0);
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(headR * 0.7f, -headR * 0.8f, -headR * 0.1f), new Vector3(headR * 0.28f, headR * 1.6f, headR * 0.28f), 0);
            }
            else if (style == "bob")
            {
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(0f, headR * 0.15f, 0f), new Vector3(headR * 2.1f, headR * 0.7f, headR * 2.1f), 0);
            }
            else if (style == "bun")
            {
                Primitive(head, PrimitiveType.Sphere, source, hair, new Vector3(0f, headR * 1.05f, -headR * 0.1f), Vector3.one * headR * 0.7f, 0);
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(0f, headR * 0.45f, -headR * 0.15f), new Vector3(headR * 1.8f, headR * 0.35f, headR * 1.6f), 0);
            }
            else if (style == "swept")
            {
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(headR * 0.15f, headR * 0.75f, 0f), new Vector3(headR * 1.5f, headR * 0.28f, headR * 1.3f), 0);
            }
            else
            {
                Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(0f, headR * 0.72f, 0f), new Vector3(headR * 1.7f, headR * 0.32f, headR * 1.7f), 0);
            }
        }

        static Transform Pivot(SegmentedBody body, Transform parent, string joint, Vector3 localPos)
        {
            var go = new GameObject(joint);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            body.SetPivot(joint, go.transform);
            return go.transform;
        }

        public static Transform Primitive(Transform parent, PrimitiveType type, Material source, Color color,
            Vector3 localPos, Vector3 localScale, int numericId, bool anatomyEnvelope = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (anatomyEnvelope) collider.enabled = false;
                else Object.Destroy(collider);
            }
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = MakeMaterial(source, color, numericId);
            return go.transform;
        }

        public static Material MakeMaterial(Material source, Color color, int numericId)
        {
            var mat = new Material(source);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_IdColor")) mat.SetColor("_IdColor", numericId <= 0 ? Color.black : IdColor(numericId));
            return mat;
        }

        public static Color IdColor(int id)
        {
            return new Color32((byte)(id & 255), (byte)((id >> 8) & 255), 1, 255);
        }

        public static Color Hex(string hex)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(hex, out color)) color = Color.gray;
            return color;
        }
    }
}
