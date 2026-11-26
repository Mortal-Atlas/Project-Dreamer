using System;
using System.Collections.Generic;
using DreamMachine.Experiment;
using DreamMachine.Sim;
using UnityEditor;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    public sealed class LatchRoomWindow : EditorWindow
    {
        static readonly string[] Actors = { "All", "Learner", "Teacher", "Environment" };
        static readonly RoomEventKind[] EventKinds = (RoomEventKind[])Enum.GetValues(typeof(RoomEventKind));
        static readonly string[] EventLabels = Array.ConvertAll(EventKinds, kind => kind.ToString());
        readonly List<int> _visible = new List<int>();
        int _seed = 1;
        bool _curiosity = true;
        bool _replay = true;
        int _eventIndex;
        int _runSeed;
        bool _runCuriosity;
        bool _runReplay;
        LatchRoomSimulation _room;
        int _actorFilter;
        int _kindFilter = -1;
        bool _playing;
        double _nextStep;

        [MenuItem("Dream Machine/Latch Room Trace")]
        static void Open()
        {
            GetWindow<LatchRoomWindow>("Latch Room Trace");
        }

        void OnGUI()
        {
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _curiosity = EditorGUILayout.Toggle("Curiosity", _curiosity);
            _replay = EditorGUILayout.Toggle("Reflective replay", _replay);
            if (GUILayout.Button("Run pilot"))
            {
                _runSeed = _seed;
                _runCuriosity = _curiosity;
                _runReplay = _replay;
                _room = new LatchRoomSimulation(_runSeed, _runCuriosity, _runReplay);
                _room.RunTicks(LatchRoomSimulation.EndTick);
                Filter();
            }
            if (_room == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Seed {_runSeed}  |  Curiosity {Setting(_runCuriosity)}  |  Replay {Setting(_runReplay)}");
            EditorGUI.BeginChangeCheck();
            _actorFilter = EditorGUILayout.Popup("Actor", _actorFilter, Actors);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool filterKind = EditorGUILayout.ToggleLeft("Filter by event type", _kindFilter >= 0, GUILayout.Width(150));
                _kindFilter = filterKind ? EditorGUILayout.Popup(Math.Max(0, _kindFilter), EventLabels) : -1;
            }
            if (EditorGUI.EndChangeCheck()) Filter();
            if (_visible.Count == 0)
            {
                EditorGUILayout.HelpBox("No events match these filters.", MessageType.Info);
                return;
            }
            if (GUILayout.Button(_playing ? "Pause playback" : "Play filtered events"))
            {
                _playing = !_playing;
                if (_playing && _eventIndex == _visible.Count - 1) _eventIndex = 0;
                _nextStep = EditorApplication.timeSinceStartup + 0.3;
            }
            _eventIndex = EditorGUILayout.IntSlider("Event", _eventIndex + 1, 1, _visible.Count) - 1;
            RoomEvent entry = _room.Trace[_visible[_eventIndex]];
            EditorGUILayout.LabelField("Trace ID", entry.Id.ToString());
            EditorGUILayout.LabelField("Time", $"Tick {entry.Tick} ({entry.Tick * WorldSimulation.Dt:0.00} s)");
            EditorGUILayout.LabelField("Actor", Actors[Actor(entry.Kind)]);
            EditorGUILayout.LabelField("Event type", entry.Kind.ToString());
            if (entry.Object.HasValue) EditorGUILayout.LabelField("Object", entry.Object.Value.ToString());
            if (entry.Position.HasValue) EditorGUILayout.LabelField("Room position", entry.Position.Value.ToString());
            if (entry.Action.HasValue) EditorGUILayout.LabelField("Action", entry.Action.Value.ToString());
            if (entry.Prediction.HasValue) EditorGUILayout.LabelField("Predicted effect", entry.Prediction.Value.ToString("P0"));
            if (entry.Effect.HasValue) EditorGUILayout.LabelField("Effect signal", entry.Effect.Value ? "On" : "Off");
            if (entry.Transfer) EditorGUILayout.LabelField("Probe", "Moved latch");
            if (entry.ParentId.HasValue && GUILayout.Button($"Follow source event {entry.ParentId.Value}"))
            {
                _actorFilter = 0;
                _kindFilter = -1;
                Filter();
                _eventIndex = entry.ParentId.Value - 1;
            }
        }

        void Filter()
        {
            _playing = false;
            _eventIndex = 0;
            _visible.Clear();
            for (int i = 0; i < _room.Trace.Count; i++)
            {
                RoomEventKind kind = _room.Trace[i].Kind;
                if (_actorFilter != 0 && Actor(kind) != _actorFilter) continue;
                if (_kindFilter >= 0 && kind != EventKinds[_kindFilter]) continue;
                _visible.Add(i);
            }
        }

        void Update()
        {
            if (!_playing || EditorApplication.timeSinceStartup < _nextStep) return;
            _nextStep = EditorApplication.timeSinceStartup + 0.3;
            _eventIndex = Math.Min(_eventIndex + 1, _visible.Count - 1);
            _playing = _eventIndex < _visible.Count - 1;
            Repaint();
        }

        static int Actor(RoomEventKind kind)
        {
            if (kind == RoomEventKind.TeacherDemonstration) return 2;
            return kind == RoomEventKind.Night || kind == RoomEventKind.Wake ? 3 : 1;
        }

        static string Setting(bool value) => value ? "on" : "off";
    }
}
