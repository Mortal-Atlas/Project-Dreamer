using DreamMachine.Experiment;
using UnityEditor;

namespace DreamMachine.EditorTools
{
    public sealed class LatchRoomWindow : EditorWindow
    {
        int _seed = 1;
        bool _curiosity = true;
        bool _replay = true;
        int _eventIndex;
        int _runSeed;
        bool _runCuriosity;
        bool _runReplay;
        LatchRoomSimulation _room;

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
            if (UnityEngine.GUILayout.Button("Run pilot"))
            {
                _runSeed = _seed;
                _runCuriosity = _curiosity;
                _runReplay = _replay;
                _room = new LatchRoomSimulation(_runSeed, _runCuriosity, _runReplay);
                _room.RunTicks(LatchRoomSimulation.EndTick);
                _eventIndex = 0;
            }
            if (_room == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Seed {_runSeed}  |  Curiosity {Setting(_runCuriosity)}  |  Replay {Setting(_runReplay)}");
            _eventIndex = EditorGUILayout.IntSlider("Event", _eventIndex + 1, 1, _room.Trace.Count) - 1;
            RoomEvent entry = _room.Trace[_eventIndex];
            EditorGUILayout.LabelField("Tick", entry.Tick.ToString());
            EditorGUILayout.LabelField("Event type", entry.Kind.ToString());
            if (entry.Object.HasValue) EditorGUILayout.LabelField("Object", entry.Object.Value.ToString());
            if (entry.Position.HasValue) EditorGUILayout.LabelField("Room position", entry.Position.Value.ToString());
            if (entry.Action.HasValue) EditorGUILayout.LabelField("Action", entry.Action.Value.ToString());
            if (entry.Prediction.HasValue) EditorGUILayout.LabelField("Predicted effect", entry.Prediction.Value.ToString("P0"));
            if (entry.Effect.HasValue) EditorGUILayout.LabelField("Effect signal", entry.Effect.Value ? "On" : "Off");
            if (entry.Transfer) EditorGUILayout.LabelField("Probe", "Moved latch");
            if (entry.ParentId.HasValue && UnityEngine.GUILayout.Button($"Follow source event {entry.ParentId.Value}"))
                _eventIndex = entry.ParentId.Value - 1;
        }

        static string Setting(bool value) => value ? "on" : "off";
    }
}
