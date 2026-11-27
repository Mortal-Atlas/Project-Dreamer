using System.Collections.Generic;
using DreamMachine.Body;
using UniVRM10;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    // Private presentation driven by the same seeded skeleton as the procedural view.
    internal sealed class HouseholdAvatar
    {
        readonly SegmentedBody _body;
        readonly Transform _root, _spin, _sourcePelvis;
        readonly Vrm10Instance _vrm;
        readonly Vrm10Runtime _runtime;
        readonly Renderer[] _procedural;
        readonly Bone _hips, _spine, _chest, _neck, _head;
        readonly Chain _leftArm, _rightArm, _leftLeg, _rightLeg;
        readonly Vector3 _restHips;
        readonly float _height, _ground;
        bool? _visible;

        struct Bone
        {
            public Transform Source, Target;
            public Quaternion Rest;

            public Bone(Transform source, Transform target, Transform root)
            {
                Source = source;
                Target = target;
                Rest = target != null ? Quaternion.Inverse(root.rotation) * target.rotation : Quaternion.identity;
            }

            public void Rotate(Transform spin, Transform root)
            {
                if (Target != null) Target.rotation = root.rotation * Quaternion.Inverse(spin.rotation) * Source.rotation * Rest;
            }
        }

        sealed class Chain
        {
            public Bone Top, Mid, End;
            public Vector3 UpperDirection, LowerDirection, RestEnd;
            public float UpperLength, LowerLength, SoleHeight;
            readonly Quaternion _restEndLocal;

            public Chain(Bone top, Bone mid, Bone end, Transform root)
            {
                Top = top;
                Mid = mid;
                End = end;
                Vector3 upper = root.InverseTransformVector(mid.Target.position - top.Target.position);
                Vector3 lower = root.InverseTransformVector(end.Target.position - mid.Target.position);
                UpperLength = upper.magnitude;
                LowerLength = lower.magnitude;
                UpperDirection = upper.normalized;
                LowerDirection = lower.normalized;
                RestEnd = root.InverseTransformPoint(end.Target.position);
                _restEndLocal = end.Target.localRotation;
            }

            void Aim(Transform root, Vector3 upper, Vector3 lower)
            {
                Top.Target.rotation = root.rotation * Quaternion.FromToRotation(UpperDirection, upper) * Top.Rest;
                Mid.Target.rotation = root.rotation * Quaternion.FromToRotation(LowerDirection, lower) * Mid.Rest;
            }

            public void Follow(Transform spin, Transform root)
            {
                Aim(root, spin.InverseTransformDirection(Mid.Source.position - Top.Source.position),
                    spin.InverseTransformDirection(End.Source.position - Mid.Source.position));
                // Preserve the avatar's bind twist relative to the rotated forearm.
                End.Target.localRotation = _restEndLocal * End.Source.localRotation;
            }

            public void Ground(Transform spin, Transform root, float scale, float sourceAnkle, float ground)
            {
                Vector3 expressed = spin.InverseTransformPoint(End.Source.position);
                Vector3 target = new Vector3(RestEnd.x, ground + SoleHeight + (expressed.y - sourceAnkle) / scale,
                    RestEnd.z + expressed.z / scale);
                Vector3 hip = root.InverseTransformPoint(Top.Target.position);
                Vector3 toward = target - hip;
                float distance = Mathf.Clamp(toward.magnitude, Mathf.Abs(UpperLength - LowerLength) + .001f, UpperLength + LowerLength - .001f);
                Vector3 axis = toward.normalized;
                Vector3 bend = Vector3.ProjectOnPlane(Vector3.forward, axis).normalized;
                float along = (UpperLength * UpperLength + distance * distance - LowerLength * LowerLength) / (2f * distance);
                Vector3 knee = axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, UpperLength * UpperLength - along * along));
                Aim(root, knee, axis * distance - knee);
                End.Rotate(spin, root);
            }
        }

        public static HouseholdAvatar Load(SegmentedBody body)
        {
            string name = body.PersonId switch
            {
                "father" => "Merlin",
                "mother" => "Marie",
                "son" => "Arthur",
                "daughter" => "Alice",
                _ => null
            };
            if (name == null) return null;
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DreamMachineLocalAvatars/" + name + ".vrm");
#endif
            if (prefab == null) prefab = Resources.Load<GameObject>("DreamerLocalAvatars/" + name);
            if (prefab == null) return null;
            var procedural = new List<Renderer>();
            foreach (var renderer in body.GetComponentsInChildren<Renderer>())
                if (!renderer.TryGetComponent<TextMesh>(out _)) procedural.Add(renderer);

            var model = Object.Instantiate(prefab, body.transform, false);
            model.name = name + " (local avatar)";
            var avatar = model.GetComponent<Vrm10Instance>();
            var animator = model.GetComponentInChildren<Animator>();
            if (avatar == null || animator == null || animator.avatar == null || !animator.avatar.isValid ||
                !animator.avatar.isHuman || model.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 0)
            {
                Debug.LogWarning("Could not bind a skinned humanoid for " + body.PersonId);
                Object.Destroy(model);
                return null;
            }
            return new HouseholdAvatar(body, avatar, animator, procedural.ToArray());
        }

        HouseholdAvatar(SegmentedBody body, Vrm10Instance vrm, Animator animator, Renderer[] procedural)
        {
            _body = body;
            _vrm = vrm;
            _root = vrm.transform;
            _sourcePelvis = body.RigPivot("pelvis");
            _spin = _sourcePelvis.parent;
            _procedural = procedural;

            Bone Map(string joint, HumanBodyBones bone) => new Bone(body.RigPivot(joint), animator.GetBoneTransform(bone), _root);
            _hips = Map("pelvis", HumanBodyBones.Hips);
            _spine = Map("torso", HumanBodyBones.Spine);
            _chest = Map("chest", HumanBodyBones.Chest);
            _neck = Map("neck", HumanBodyBones.Neck);
            _head = Map("head", HumanBodyBones.Head);
            _restHips = _root.InverseTransformPoint(_hips.Target.position);
            _leftLeg = new Chain(Map("leftHip", HumanBodyBones.LeftUpperLeg), Map("leftKnee", HumanBodyBones.LeftLowerLeg),
                Map("leftAnkle", HumanBodyBones.LeftFoot), _root);
            _rightLeg = new Chain(Map("rightHip", HumanBodyBones.RightUpperLeg), Map("rightKnee", HumanBodyBones.RightLowerLeg),
                Map("rightAnkle", HumanBodyBones.RightFoot), _root);
            _leftArm = new Chain(Map("leftShoulder", HumanBodyBones.LeftUpperArm), Map("leftElbow", HumanBodyBones.LeftLowerArm),
                Map("leftWrist", HumanBodyBones.LeftHand), _root);
            _rightArm = new Chain(Map("rightShoulder", HumanBodyBones.RightUpperArm), Map("rightElbow", HumanBodyBones.RightLowerArm),
                Map("rightWrist", HumanBodyBones.RightHand), _root);
            (_ground, _height) = MeasureMesh();
            vrm.UpdateType = Vrm10Instance.UpdateTypes.None;
            vrm.LookAtTarget = null;
            _runtime = vrm.Runtime;
            animator.enabled = false;
        }

        // Sample the actual bind-pose soles, including the shoes' offset below each foot bone.
        (float ground, float height) MeasureMesh()
        {
            float left = float.PositiveInfinity, right = float.PositiveInfinity, top = float.NegativeInfinity;
            var vertices = new List<Vector3>();
            var baked = new Mesh();
            foreach (var skin in _root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.BakeMesh(baked, useScale: true);
                baked.GetVertices(vertices);
                foreach (var vertex in vertices)
                {
                    Vector3 point = _root.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                    top = Mathf.Max(top, point.y);
                    if (Mathf.Abs(point.x - _leftLeg.RestEnd.x) < Mathf.Abs(point.x - _rightLeg.RestEnd.x))
                        left = Mathf.Min(left, point.y);
                    else right = Mathf.Min(right, point.y);
                }
            }
            Object.Destroy(baked);
            float ground = Mathf.Min(left, right);
            _leftLeg.SoleHeight = _leftLeg.RestEnd.y - left;
            _rightLeg.SoleHeight = _rightLeg.RestEnd.y - right;
            return (ground, Mathf.Max(.1f, top - ground));
        }

        public void SetVisible(bool visible)
        {
            if (visible == _visible) return;
            _visible = visible;
            foreach (Renderer renderer in _procedural) renderer.enabled = !visible;
            _vrm.gameObject.SetActive(visible);
        }

        public void Follow(Vector3? attention, bool seated, bool sleeping, bool consuming, float time)
        {
            var dimensions = _body.Dimensions;
            float scale = dimensions.Height / _height;
            _root.localScale = Vector3.one * scale;
            _root.localPosition = Vector3.up * (-_ground * scale);
            _root.rotation = _spin.rotation;
            float stand = GroundedGait.PelvisHeight(dimensions, false);
            float lift = (_sourcePelvis.localPosition.y - stand) / scale;
            // A few millimetres of flexion avoids straight-leg locking at the imported leg length.
            _hips.Target.position = _root.TransformPoint(_restHips + Vector3.up * (lift - .004f));
            _hips.Rotate(_spin, _root);
            _spine.Rotate(_spin, _root);
            _chest.Rotate(_spin, _root);
            _neck.Rotate(_spin, _root);
            _head.Rotate(_spin, _root);
            _leftArm.Follow(_spin, _root);
            _rightArm.Follow(_spin, _root);
            if (sleeping || seated || _sourcePelvis.localPosition.y < stand * .9f)
            {
                _leftLeg.Follow(_spin, _root);
                _rightLeg.Follow(_spin, _root);
            }
            else
            {
                _leftLeg.Ground(_spin, _root, scale, dimensions.AnkleHeight, _ground);
                _rightLeg.Ground(_spin, _root, scale, dimensions.AnkleHeight, _ground);
            }
            if (attention.HasValue && !sleeping)
                _runtime.LookAt.LookAtInput = new LookAtInput { WorldPosition = attention.Value };
            else
                _runtime.LookAt.SetYawPitchManually(0f, 0f);
            float openness = _body.RigPivot("leftEye").localScale.y;
            _runtime.Expression.SetWeight(ExpressionKey.Blink, Mathf.Clamp01((1f - openness) / .88f));
            _runtime.Expression.SetWeight(ExpressionKey.Aa, consuming ? .18f + .12f * Mathf.Sin(14f * time) : 0f);
            _runtime.Process();
        }
    }
}
