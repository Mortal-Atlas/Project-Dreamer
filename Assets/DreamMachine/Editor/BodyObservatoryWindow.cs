using DreamMachine.Body;
using DreamMachine.UnityBridge;
using UnityEditor;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    public sealed class BodyObservatoryWindow : EditorWindow
    {
        int _seed = 12;
        float _developmentDays;
        float _distance = 0.35f;
        SegmentedBody _body;
        Vector2 _scroll;

        [MenuItem("Dream Machine/Body Observatory")]
        static void Open() => GetWindow<BodyObservatoryWindow>("Body Observatory");

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            _body = (SegmentedBody)EditorGUILayout.ObjectField("Live rig", _body, typeof(SegmentedBody), true);
            if (GUILayout.Button("Use selected rig") && Selection.activeGameObject != null)
                _body = Selection.activeGameObject.GetComponentInParent<SegmentedBody>();

            HumanoidAnatomy anatomy;
            HumanoidDimensions dimensions;
            if (_body == null)
            {
                EditorGUILayout.LabelField("Seeded reference profile", EditorStyles.boldLabel);
                _seed = EditorGUILayout.IntField("Birth seed", _seed);
                _developmentDays = EditorGUILayout.Slider("Development days", _developmentDays, 0f, HumanoidAnatomy.AdultAgeDays);
                anatomy = new HumanoidAnatomy(_seed);
                dimensions = anatomy.AtAge(_developmentDays);
            }
            else
            {
                anatomy = new HumanoidAnatomy(_body.AnatomySeed);
                dimensions = _body.Dimensions;
                EditorGUILayout.LabelField("Birth seed", anatomy.BirthSeed.ToString());
                EditorGUILayout.LabelField("Sampled development days", _body.SampledDevelopmentDays.ToString("F3"));
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Metres("Standing height", dimensions.Height);
            Metres("Seeded adult height", anatomy.AdultHeight);
            Metres("Head height", dimensions.Head);
            Metres("Torso height", dimensions.Torso);
            Metres("Upper arm / forearm", dimensions.UpperArm, dimensions.Forearm);
            Metres("Thigh / shin", dimensions.Thigh, dimensions.Shin);
            Metres("Shoulder / hip breadth", dimensions.ShoulderWidth, dimensions.HipWidth);
            Metres("Neutral eye height", dimensions.EyeHeight);
            Metres("Grip reach bound", dimensions.GripReach);
            _distance = EditorGUILayout.Slider("Target distance (m)", _distance, 0f, 0.9f);
            EditorGUILayout.LabelField("Geometric reach test", dimensions.WithinGripReach(_distance) ? "Within reach bound" : "Beyond reach bound");
            if (_body != null)
            {
                EditorGUILayout.LabelField("Kinematic gait", _body.Walking ? "Walking" : "Neutral");
                Metres("Root displacement on last update", _body.LastDisplacementMetres);
                Transform pelvis = _body.RigPivot("pelvis");
                if (pelvis != null) Metres("Live pelvis elevation", pelvis.position.y - _body.transform.position.y);
                DrawLeg("Left", _body.LeftSupport);
                DrawLeg("Right", _body.RightSupport);
                Transform gaze = _body.RigPivot("gaze");
                Transform grip = _body.RigPivot("leftGrip");
                if (gaze != null) EditorGUILayout.LabelField("Live gaze location", gaze.position.ToString("F3"));
                if (grip != null) EditorGUILayout.LabelField("Live left grip", grip.position.ToString("F3"));
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawLeg(string side, bool support)
        {
            string id = side.ToLowerInvariant();
            Transform knee = _body.RigPivot(id + "Knee");
            Transform ankle = _body.RigPivot(id + "Ankle");
            if (knee == null || ankle == null) return;
            EditorGUILayout.LabelField(side + " leg", support ? "Reference support" : "Swing");
            EditorGUILayout.LabelField(side + " knee flexion", $"{Mathf.DeltaAngle(0, knee.localEulerAngles.x):F1}°");
            Metres(side + " ankle elevation", ankle.position.y - _body.transform.position.y);
        }

        static void Metres(string label, float size) => EditorGUILayout.LabelField(label, $"{size:F3} m");
        static void Metres(string label, float first, float second) => EditorGUILayout.LabelField(label, $"{first:F3} / {second:F3} m");
    }
}
