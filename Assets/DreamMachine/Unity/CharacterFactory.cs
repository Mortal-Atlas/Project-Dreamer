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

        public Transform HeadPivot()
        {
            Transform head;
            return _pivots.TryGetValue("head", out head) ? head : transform;
        }
    }

    public static class CharacterFactory
    {
        public static SegmentedBody Build(Transform parent, AppearanceProfile look, Material source)
        {
            var root = new GameObject(look.DisplayName);
            root.transform.SetParent(parent, false);
            var body = root.AddComponent<SegmentedBody>();
            body.PersonId = look.Id;
            body.IsLeader = look.IsLeader;

            var spin = new GameObject("Spin").transform;
            spin.SetParent(root.transform, false);
            body.Bind(spin, null);

            float shoulder = 0.16f * look.Shoulder;
            float hip = 0.09f * look.Hip;
            float torsoH = look.IsBaby ? 0.34f : 0.52f;
            float headR = (look.IsBaby ? 0.13f : 0.12f) * look.Head;
            Color skin = Hex(look.Skin);
            Color hair = Hex(look.Hair);
            Color cloth = Hex(look.Cloth);
            Color accent = Hex(look.Accent);

            Transform torso = Pivot(body, spin, "torso", Vector3.up * (torsoH * 0.55f));
            Primitive(torso, PrimitiveType.Capsule, source, skin, Vector3.zero, new Vector3(0.28f * look.Shoulder, torsoH * 0.5f, 0.18f * look.Hip), 0);
            Primitive(torso, PrimitiveType.Capsule, source, cloth, new Vector3(0f, -0.02f, 0.02f), new Vector3(0.30f * look.Shoulder, torsoH * 0.42f, 0.20f), 0);
            if (look.Outfit == "wrap" || look.Outfit == "robe")
            {
                Primitive(torso, PrimitiveType.Cube, source, cloth, new Vector3(0f, -torsoH * 0.42f, 0f), new Vector3(0.34f * look.Hip, 0.16f, 0.22f), 0);
            }
            if (look.IsLeader)
            {
                Primitive(torso, PrimitiveType.Cube, source, accent, new Vector3(0f, 0.02f, -0.12f), new Vector3(0.34f, torsoH * 0.7f, 0.04f), 0);
                Primitive(torso, PrimitiveType.Cube, source, accent, new Vector3(0f, torsoH * 0.2f, 0.16f), new Vector3(0.12f, 0.28f, 0.04f), 0);
            }

            Transform head = Pivot(body, torso, "head", Vector3.up * (torsoH * 0.55f + headR * 0.2f));
            Primitive(head, PrimitiveType.Sphere, source, skin, Vector3.zero, Vector3.one * headR * 2f, 0);
            AddFace(head, source, skin, hair, headR, look);
            AddHair(head, source, hair, accent, headR, look);

            float armLen = look.IsBaby ? 0.18f : 0.26f;
            float foreLen = look.IsBaby ? 0.16f : 0.22f;
            BuildArm(body, torso, source, skin, cloth, "left", new Vector3(-shoulder, torsoH * 0.28f, 0f), armLen, foreLen);
            BuildArm(body, torso, source, skin, cloth, "right", new Vector3(shoulder, torsoH * 0.28f, 0f), armLen, foreLen);

            float thigh = look.IsBaby ? 0.20f : 0.32f;
            float shin = look.IsBaby ? 0.18f : 0.30f;
            BuildLeg(body, torso, source, skin, cloth, "left", new Vector3(-hip, -torsoH * 0.42f, 0f), thigh, shin);
            BuildLeg(body, torso, source, skin, cloth, "right", new Vector3(hip, -torsoH * 0.42f, 0f), thigh, shin);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.up * 2.15f;
            var label = labelGo.AddComponent<TextMesh>();
            label.text = look.IsLeader ? look.DisplayName + "  ★" : look.DisplayName;
            label.characterSize = 0.08f;
            label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = look.IsLeader ? accent : Color.white;
            body.Bind(spin, label);

            root.transform.localScale = Vector3.one * look.Height;
            return body;
        }

        static void BuildArm(SegmentedBody body, Transform torso, Material source, Color skin, Color cloth, string side, Vector3 origin, float upper, float fore)
        {
            string shoulder = side + "Shoulder";
            string elbow = side + "Elbow";
            Transform shoulderPivot = Pivot(body, torso, shoulder, origin);
            Primitive(shoulderPivot, PrimitiveType.Capsule, source, cloth, new Vector3(0f, -upper * 0.5f, 0f), new Vector3(0.09f, upper * 0.5f, 0.09f), 0);
            Transform elbowPivot = Pivot(body, shoulderPivot, elbow, new Vector3(0f, -upper, 0f));
            Primitive(elbowPivot, PrimitiveType.Capsule, source, skin, new Vector3(0f, -fore * 0.5f, 0f), new Vector3(0.07f, fore * 0.5f, 0.07f), 0);
            Primitive(elbowPivot, PrimitiveType.Sphere, source, skin, new Vector3(0f, -fore, 0f), Vector3.one * 0.08f, 0);
        }

        static void BuildLeg(SegmentedBody body, Transform torso, Material source, Color skin, Color cloth, string side, Vector3 origin, float thigh, float shin)
        {
            string hip = side + "Hip";
            string knee = side + "Knee";
            Transform hipPivot = Pivot(body, torso, hip, origin);
            Primitive(hipPivot, PrimitiveType.Capsule, source, cloth, new Vector3(0f, -thigh * 0.5f, 0f), new Vector3(0.12f, thigh * 0.5f, 0.12f), 0);
            Transform kneePivot = Pivot(body, hipPivot, knee, new Vector3(0f, -thigh, 0f));
            Primitive(kneePivot, PrimitiveType.Capsule, source, skin, new Vector3(0f, -shin * 0.5f, 0f), new Vector3(0.09f, shin * 0.5f, 0.09f), 0);
            Primitive(kneePivot, PrimitiveType.Cube, source, Hex("#2B2B2B"), new Vector3(0f, -shin, 0.04f), new Vector3(0.10f, 0.05f, 0.16f), 0);
        }

        static void AddFace(Transform head, Material source, Color skin, Color hair, float headR, AppearanceProfile look)
        {
            Color eyeWhite = Hex("#F7F4EF");
            Color pupil = Hex("#1B1B1B");
            Color mouth = Hex("#8C3B4A");
            float z = headR * 0.78f;
            Primitive(head, PrimitiveType.Sphere, source, eyeWhite, new Vector3(-headR * 0.32f, headR * 0.12f, z), Vector3.one * headR * 0.38f, 0);
            Primitive(head, PrimitiveType.Sphere, source, eyeWhite, new Vector3(headR * 0.32f, headR * 0.12f, z), Vector3.one * headR * 0.38f, 0);
            Primitive(head, PrimitiveType.Sphere, source, pupil, new Vector3(-headR * 0.32f, headR * 0.12f, z + headR * 0.12f), Vector3.one * headR * 0.18f, 0);
            Primitive(head, PrimitiveType.Sphere, source, pupil, new Vector3(headR * 0.32f, headR * 0.12f, z + headR * 0.12f), Vector3.one * headR * 0.18f, 0);
            Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(-headR * 0.32f, headR * 0.38f, z), new Vector3(headR * 0.28f, headR * 0.08f, headR * 0.08f), 0);
            Primitive(head, PrimitiveType.Cube, source, hair, new Vector3(headR * 0.32f, headR * 0.38f, z), new Vector3(headR * 0.28f, headR * 0.08f, headR * 0.08f), 0);
            Primitive(head, PrimitiveType.Cube, source, mouth, new Vector3(0f, -headR * 0.28f, z), new Vector3(headR * 0.36f, headR * 0.08f, headR * 0.08f), 0);
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

        public static Transform Primitive(Transform parent, PrimitiveType type, Material source, Color color, Vector3 localPos, Vector3 localScale, int numericId)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
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
