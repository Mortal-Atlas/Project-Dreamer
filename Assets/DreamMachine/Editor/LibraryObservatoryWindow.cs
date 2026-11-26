using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DreamMachine.Experiment;
using DreamMachine.Libraries;
using DreamMachine.Sim;
using DreamMachine.Util;
using UnityEditor;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    public sealed class LibraryObservatoryWindow : EditorWindow
    {
        static readonly string[] Shelves = { "I  Knowledge", "II  Possibility Space", "III  Lived Memory", "Archive" };
        readonly Stack<int> _previous = new Stack<int>();
        int _shelf, _volumePage, _after;
        int _seed = 12, _tick = LatchRoomSimulation.EndTick, _budgetMiB = 64;
        bool _curiosity = true, _replay = true;
        string _query = "", _status = "", _body;
        PilotVolume _volume;
        LibraryBook _book;
        IReadOnlyList<PilotVolume> _volumes = Array.Empty<PilotVolume>();
        IReadOnlyList<LibraryBook> _books = Array.Empty<LibraryBook>();
        IReadOnlyList<LibraryBook> _incoming = Array.Empty<LibraryBook>();
        LibraryStorage? _storage;
        Vector2 _scroll, _detailScroll;
        Task<Action> _work;

        [MenuItem("Dream Machine/Library Observatory")]
        public static void Open() => GetWindow<LibraryObservatoryWindow>("Library Observatory");

        [MenuItem("Dream Machine/Archive Manager")]
        static void OpenArchive() => GetWindow<LibraryObservatoryWindow>("Library Observatory")._shelf = 3;

        void OnEnable()
        {
            minSize = new Vector2(625, 385);
            Try(RefreshVolumes);
        }

        PilotLibraryArchive Archive() => new PilotLibraryArchive(
            Path.Combine(Application.persistentDataPath, "library-observatory-v1"),
            Math.Max(1, _budgetMiB) * 1024L * 1024L);

        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(_work != null))
            {
                DrawVolumes();
                int shelf = GUILayout.Toolbar(_shelf, Shelves);
                if (shelf != _shelf)
                {
                    _shelf = shelf;
                    UnloadBook();
                    Try(ResetPage);
                }
                if (_shelf == 3) DrawStorage();
                else if (_volume == null)
                    EditorGUILayout.HelpBox("Select an archived individual or capture a latch pilot.", MessageType.Info);
                else DrawBooks();
            }
            if (_work != null) EditorGUILayout.LabelField("Working…");
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        void DrawVolumes()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Birth seed", _seed);
                _curiosity = EditorGUILayout.ToggleLeft("Curiosity", _curiosity, GUILayout.Width(70));
                _replay = EditorGUILayout.ToggleLeft("Replay", _replay, GUILayout.Width(65));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _tick = EditorGUILayout.IntSlider("Checkpoint tick", _tick, 0, LatchRoomSimulation.EndTick);
                if (GUILayout.Button("Capture pilot", GUILayout.Width(108)))
                {
                    var archive = Archive();
                    int seed = _seed, tick = _tick;
                    bool curiosity = _curiosity, replay = _replay;
                    QueueWork(() =>
                    {
                        PilotVolume volume = archive.Capture(seed, curiosity, replay, tick);
                        return () => { RefreshVolumes(); Select(volume); _storage = null; _status = "Pilot snapshot archived."; };
                    });
                }
            }
            string[] options = new[] { "Choose an archived individual" }.Concat(_volumes.Select(Label)).ToArray();
            int selection = _volume == null ? 0 : _volumes.ToList().FindIndex(v => v.Id == _volume.Id) + 1;
            using (new EditorGUILayout.HorizontalScope())
            {
                int next = EditorGUILayout.Popup(selection, options);
                if (next != selection && next > 0) Try(() => Select(_volumes[next - 1]));
                if (GUILayout.Button("Previous", GUILayout.Width(65)) && _volumePage > 0)
                    Try(() => { _volumePage--; RefreshVolumes(); });
                if (GUILayout.Button("More", GUILayout.Width(55)) && _volumes.Count == PilotLibraryArchive.PageSize)
                    Try(() => { _volumePage++; RefreshVolumes(); });
                if (GUILayout.Button("Refresh", GUILayout.Width(65))) Try(RefreshVolumes);
            }
            if (_volume != null) EditorGUILayout.LabelField("Captured (UTC)", _volume.CapturedUtc);
        }

        void DrawBooks()
        {
            string query = EditorGUILayout.TextField("Search titles and sources", _query);
            if (query != _query) Try(() => { _query = query; ResetPage(); });
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.43f)))
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    foreach (LibraryBook book in _books)
                        if (GUILayout.Button($"{book.Id}  {book.Title}")) Try(() => OpenBook(book.Id));
                    if (_books.Count == 0) EditorGUILayout.HelpBox("No recorded books match this shelf and search.", MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Previous page") && _previous.Count > 0)
                            Try(() => { _after = _previous.Pop(); RefreshBooks(); });
                        if (GUILayout.Button("Next page") && _books.Count == PilotLibraryArchive.PageSize)
                            Try(() => { _previous.Push(_after); _after = _books[_books.Count - 1].Id; RefreshBooks(); });
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    if (_book != null)
                    {
                        EditorGUILayout.LabelField(_book.Title, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Source", _book.Source);
                        EditorGUILayout.LabelField("Time", $"Tick {_book.Tick} · {_book.Tick * WorldSimulation.Dt:0.00} s");
                        foreach (var field in MiniJson.AsObject(MiniJson.Parse(_body)))
                            if (field.Value != null) EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Key), field.Value.ToString());
                        foreach (int link in _book.Links)
                            if (GUILayout.Button($"Source book {link}")) { Try(() => OpenBook(link)); break; }
                        foreach (LibraryBook related in _incoming)
                            if (GUILayout.Button($"Referenced by {related.Id} · {related.Title}")) { Try(() => OpenBook(related.Id)); break; }
                        if (GUILayout.Button("Unload book from view")) UnloadBook();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        void DrawStorage()
        {
            _budgetMiB = EditorGUILayout.IntField("Capture budget (MiB)", _budgetMiB);
            EditorGUILayout.LabelField("Free-space reserve", "256 MiB");
            if (GUILayout.Button("Measure archive storage"))
            {
                var archive = Archive();
                QueueWork(() => { LibraryStorage data = archive.Storage(); return () => _storage = data; });
            }
            if (_storage.HasValue)
            {
                LibraryStorage data = _storage.Value;
                EditorGUILayout.LabelField("Volumes / books / unique bodies", $"{data.Volumes} / {data.Books} / {data.Blobs}");
                EditorGUILayout.LabelField("Archive file bytes", data.PhysicalBytes.ToString("N0"));
                EditorGUILayout.LabelField("Logical book body bytes", data.LogicalContentBytes.ToString("N0"));
                EditorGUILayout.LabelField("Available disk bytes", data.AvailableBytes.ToString("N0"));
                EditorGUILayout.LabelField("Loaded book bodies", _body == null ? "0" : "1");
            }
            if (_volume != null && GUILayout.Button("Verify checkpoint restoration"))
            {
                var archive = Archive();
                string id = _volume.Id;
                QueueWork(() =>
                {
                    var restored = archive.Restore(id);
                    return () => _status = $"Checkpoint restored at tick {restored.TickCount} with {restored.Trace.Count} verified events.";
                });
            }
        }

        static string Label(PilotVolume v) => $"Seed {v.Seed} · tick {v.Tick} · curiosity {(v.Curiosity ? "on" : "off")} · replay {(v.Replay ? "on" : "off")}";
        void RefreshVolumes() => _volumes = Archive().Volumes(_volumePage * PilotLibraryArchive.PageSize);
        void Select(PilotVolume volume) { _volume = volume; UnloadBook(); ResetPage(); }
        void ResetPage() { _previous.Clear(); _after = 0; RefreshBooks(); }
        void RefreshBooks()
        {
            if (_volume != null && _shelf < 3) _books = Archive().Search(_volume.Id, (LibraryShelf)(_shelf + 1), _query, _after);
        }
        void OpenBook(int id)
        {
            var archive = Archive();
            LibraryBook book = archive.GetBook(_volume.Id, id);
            string body = archive.Read(_volume.Id, id);
            var incoming = archive.ReferencedBy(_volume.Id, id);
            _book = book;
            _body = body;
            _incoming = incoming;
            _detailScroll = Vector2.zero;
        }
        void UnloadBook() { _book = null; _body = null; _incoming = Array.Empty<LibraryBook>(); }

        void QueueWork(Func<Action> operation)
        {
            _status = "";
            _work = Task.Run(operation);
        }
        void Update()
        {
            if (_work == null || !_work.IsCompleted) return;
            Task<Action> completed = _work;
            _work = null;
            Try(() => completed.GetAwaiter().GetResult()());
            Repaint();
        }
        void Try(Action action)
        {
            try { action(); }
            catch (Exception error) { _status = error.Message; }
        }
    }
}
