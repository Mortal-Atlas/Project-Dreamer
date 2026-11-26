using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DreamMachine.Experiment;
using DreamMachine.Util;

namespace DreamMachine.Libraries
{
    public enum LibraryShelf { Knowledge = 1, Possibility = 2, LivedMemory = 3 }

    public sealed class LibraryBook
    {
        public int Id { get; }
        public LibraryShelf Shelf { get; }
        public string Title { get; }
        public string Source { get; }
        public int Tick { get; }
        public IReadOnlyList<int> Links { get; }
        internal string ContentHash { get; }
        internal int ContentLength { get; }

        internal LibraryBook(Dictionary<string, object> row)
        {
            Id = MiniJson.AsInt(row["id"]);
            Shelf = (LibraryShelf)MiniJson.AsInt(row["library"]);
            Title = MiniJson.AsString(row["title"]);
            Source = MiniJson.AsString(row["source"]);
            Tick = MiniJson.AsInt(row["tick"]);
            Links = MiniJson.AsArray(row["links"]).Select(MiniJson.AsInt).ToList().AsReadOnly();
            ContentHash = MiniJson.AsString(row["blob"]);
            ContentLength = MiniJson.AsInt(row["length"]);
        }
    }

    /// <summary>The deterministic latch fixture's executable checkpoint recipe.</summary>
    public sealed class PilotVolume
    {
        internal int Version { get; }
        public string Id { get; }
        public int Seed { get; }
        public bool Curiosity { get; }
        public bool Replay { get; }
        public int Tick { get; }
        public string CapturedUtc { get; }
        internal string TraceHash { get; }

        internal PilotVolume(Dictionary<string, object> header)
        {
            Version = MiniJson.AsInt(header["version"]);
            if ((Version != 1 && Version != 2) || MiniJson.AsString(header["fixture"]) != "latch-v1")
                throw new InvalidDataException("Unsupported latch checkpoint version.");
            Id = MiniJson.AsString(header["id"]);
            Seed = MiniJson.AsInt(header["seed"]);
            Curiosity = MiniJson.AsBool(header["curiosity"]);
            Replay = MiniJson.AsBool(header["replay"]);
            Tick = MiniJson.AsInt(header["tick"]);
            CapturedUtc = MiniJson.AsString(header["capturedUtc"]);
            TraceHash = MiniJson.AsString(header["traceHash"]);
            if (Tick < 0 || Tick > LatchRoomSimulation.EndTick) throw new InvalidDataException("Invalid checkpoint tick.");
        }
    }

    public readonly struct LibraryStorage
    {
        public long PhysicalBytes { get; }
        public long LogicalContentBytes { get; }
        public int Blobs { get; }
        public int Books { get; }
        public int Volumes { get; }
        public long AvailableBytes { get; }

        internal LibraryStorage(long physicalBytes, long logicalContentBytes, int blobs, int books, int volumes, long availableBytes)
        {
            PhysicalBytes = physicalBytes;
            LogicalContentBytes = logicalContentBytes;
            Blobs = blobs;
            Books = books;
            Volumes = volumes;
            AvailableBytes = availableBytes;
        }
    }

    /// <summary>Immutable per-volume streaming indexes and individually verified compressed bodies.</summary>
    public sealed class PilotLibraryArchive
    {
        const int CurrentVersion = 2;
        const int MaximumBodyBytes = 64 * 1024;
        public const long DefaultBudget = 64 * 1024 * 1024;
        public const long DefaultReserve = 256 * 1024 * 1024;
        public const int PageSize = 32;
        readonly string _root;
        readonly string _catalog;
        readonly string _blobs;
        readonly long _budget;
        readonly long _reserve;

        public PilotLibraryArchive(string directory, long budget = DefaultBudget, long reserve = DefaultReserve)
        {
            if (budget <= 0) throw new ArgumentOutOfRangeException(nameof(budget));
            if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve));
            _root = Path.GetFullPath(directory);
            _catalog = Path.Combine(_root, "volumes");
            _blobs = Path.Combine(_root, "blobs");
            _budget = budget;
            _reserve = reserve;
        }

        public PilotVolume Capture(int seed, bool curiosity, bool replay, int tick = LatchRoomSimulation.EndTick)
        {
            if (tick < 0 || tick > LatchRoomSimulation.EndTick) throw new ArgumentOutOfRangeException(nameof(tick));
            var description = Recipe(CurrentVersion, seed, curiosity, replay, tick);
            string id = Hash(Encoding.UTF8.GetBytes(MiniJson.Stringify(description)));
            string path = VolumePath(id);
            if (File.Exists(path)) return GetVolume(id);
            var room = new LatchRoomSimulation(seed, curiosity, replay);
            room.RunTicks(tick);
            description.Add("id", id);
            description.Add("traceHash", TraceHash(room, CurrentVersion));
            description.Add("capturedUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(_root);
            using (new FileStream(Path.Combine(_root, "writer.lock"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                if (File.Exists(path)) return GetVolume(id);
                Directory.CreateDirectory(_catalog);
                Directory.CreateDirectory(_blobs);
                long allocated = Files(_catalog, "*").Concat(Files(_blobs, "*")).Sum(f => new FileInfo(f).Length);
                var index = new StringBuilder(MiniJson.Stringify(description)).Append('\n');
                var pending = new Dictionary<string, byte[]>();
                foreach (var row in Project(room, CurrentVersion))
                {
                    var bytes = Encoding.UTF8.GetBytes((string)row["body"]);
                    if (bytes.Length > MaximumBodyBytes) throw new InvalidDataException("Book exceeds the supported body length.");
                    string hash = Hash(bytes);
                    string blob = BlobPath(hash);
                    if (File.Exists(blob)) ReadBlob(hash, bytes.Length);
                    else if (!pending.ContainsKey(blob))
                    {
                        using (var buffer = new MemoryStream())
                        {
                            using (var zip = new GZipStream(buffer, CompressionLevel.Optimal, true))
                                zip.Write(bytes, 0, bytes.Length);
                            pending.Add(blob, buffer.ToArray());
                        }
                    }
                    row.Remove("body");
                    row.Add("blob", hash);
                    row.Add("length", bytes.Length);
                    index.Append(MiniJson.Stringify(row)).Append('\n');
                }
                byte[] catalog = Encoding.UTF8.GetBytes(index.ToString());
                CheckBudget(catalog.LongLength + pending.Values.Sum(bytes => bytes.LongLength), allocated);
                foreach (var blob in pending) Publish(blob.Key, blob.Value, ref allocated);
                Publish(path, catalog, ref allocated);
            }
            return new PilotVolume(description);
        }

        public IReadOnlyList<PilotVolume> Volumes(int offset = 0) =>
            Files(_catalog, "*.jsonl").Skip(offset).Take(PageSize).Select(path => ReadVolume(path)).ToList().AsReadOnly();

        public PilotVolume GetVolume(string id)
        {
            PilotVolume volume = ReadVolume(VolumePath(id));
            string expected = Hash(Encoding.UTF8.GetBytes(MiniJson.Stringify(Recipe(volume.Version, volume.Seed, volume.Curiosity, volume.Replay, volume.Tick))));
            if (volume.Id != id || expected != id) throw new InvalidDataException("Volume identifier does not match the checkpoint recipe.");
            return volume;
        }

        public IReadOnlyList<LibraryBook> Search(string volume, LibraryShelf shelf, string query = "", int after = 0) =>
            Books(volume).Where(book => book.Shelf == shelf && book.Id > after &&
                (Contains(book.Title, query) || Contains(book.Source, query))).Take(PageSize).ToList().AsReadOnly();

        public IReadOnlyList<LibraryBook> ReferencedBy(string volume, int id) =>
            Books(volume).Where(book => book.Links.Contains(id)).Take(PageSize).ToList().AsReadOnly();

        public LibraryBook GetBook(string volume, int id) => Books(volume).FirstOrDefault(row => row.Id == id)
            ?? throw new KeyNotFoundException("Book is unavailable in this volume.");

        public string Read(string volume, int id)
        {
            LibraryBook row = GetBook(volume, id);
            return ReadBlob(row.ContentHash, row.ContentLength);
        }

        public LatchRoomSimulation Restore(string id)
        {
            PilotVolume volume = GetVolume(id);
            var room = new LatchRoomSimulation(volume.Seed, volume.Curiosity, volume.Replay);
            room.RunTicks(volume.Tick);
            if (TraceHash(room, volume.Version) != volume.TraceHash) throw new InvalidDataException("Checkpoint trace integrity check failed.");
            return room;
        }

        public LibraryStorage Storage()
        {
            long physical = 0;
            long logical = 0;
            int blobs = 0, books = 0, volumes = 0;
            foreach (string path in Files(_blobs, "*"))
            {
                physical += new FileInfo(path).Length;
                if (path.EndsWith(".gz", StringComparison.Ordinal)) blobs++;
            }
            foreach (string path in Files(_catalog, "*"))
            {
                physical += new FileInfo(path).Length;
                if (!path.EndsWith(".jsonl", StringComparison.Ordinal)) continue;
                volumes++;
                foreach (LibraryBook row in Books(Path.GetFileNameWithoutExtension(path)))
                {
                    books++;
                    logical += row.ContentLength;
                }
            }
            return new LibraryStorage(physical, logical, blobs, books, volumes, FreeBytes());
        }

        static IEnumerable<Dictionary<string, object>> Project(LatchRoomSimulation room, int version)
        {
            foreach (RoomEvent entry in room.Trace)
            {
                bool possibility = entry.Kind == RoomEventKind.Question || entry.Kind == RoomEventKind.Hypothesis;
                string source = entry.Kind == RoomEventKind.TeacherDemonstration ? "Teacher observation" :
                    entry.Kind == RoomEventKind.Hypothesis || entry.Kind == RoomEventKind.Replay ? "Night reconstruction" :
                    entry.Kind == RoomEventKind.Feedback || entry.Kind == RoomEventKind.Contact ? "Waking sensory feedback" : "Latch pilot trace";
                string title = entry.Kind + (entry.Object.HasValue ? " " + entry.Object : "") + (entry.Action.HasValue ? " " + entry.Action : "");
                var facts = new Dictionary<string, object>
                {
                    { "object", entry.Object?.ToString() }, { "action", entry.Action?.ToString() },
                    { "prediction", entry.Prediction.HasValue ? Probability(entry.Prediction.Value, version) : null },
                    { "effect", entry.Effect }, { "position", entry.Position }, { "transfer", entry.Transfer }
                };
                yield return Row(entry.Id, possibility ? LibraryShelf.Possibility : LibraryShelf.LivedMemory,
                    title, source, entry.Tick, entry.ParentId.HasValue ? new[] { entry.ParentId.Value } : new int[0], MiniJson.Stringify(facts));
            }
            int id = room.Trace.Count;
            foreach (RoomObject obj in Enum.GetValues(typeof(RoomObject)))
            foreach (RoomAction action in Enum.GetValues(typeof(RoomAction)))
            {
                var evidence = room.Trace.Where(entry => entry.Object == obj && entry.Action == action &&
                    (entry.Kind == RoomEventKind.Feedback || entry.Kind == RoomEventKind.TeacherDemonstration)).ToList();
                if (evidence.Count == 0) continue;
                var details = new Dictionary<string, object>
                {
                    { "probabilityOfEffect", Probability(room.EffectProbability(obj, action), version) },
                    { "effectsObserved", evidence.Count(e => e.Effect == true) },
                    { "noEffectObserved", evidence.Count(e => e.Effect == false) }
                };
                yield return Row(++id, LibraryShelf.Knowledge, obj + " " + action + " acquired prediction",
                    "Inferred from observations", evidence[evidence.Count - 1].Tick, evidence.Select(e => e.Id).ToArray(), MiniJson.Stringify(details));
            }
        }

        static Dictionary<string, object> Row(int id, LibraryShelf shelf, string title, string source, int tick, int[] links, string body) =>
            new Dictionary<string, object> { { "id", id }, { "library", (int)shelf }, { "title", title },
                { "source", source }, { "tick", tick }, { "links", links }, { "body", body } };

        static Dictionary<string, object> Recipe(int version, int seed, bool curiosity, bool replay, int tick) =>
            new Dictionary<string, object> { { "version", version }, { "fixture", "latch-v1" }, { "seed", seed },
                { "curiosity", curiosity }, { "replay", replay }, { "tick", tick } };

        static string TraceHash(LatchRoomSimulation room, int version) =>
            Hash(Encoding.UTF8.GetBytes(MiniJson.Stringify(Project(room, version).ToArray())));

        // Fixed digits on an exact float-to-double conversion avoid Mono and .NET shortest-format differences.
        static string Probability(float value, int version) => version == 1
            ? value.ToString("R", CultureInfo.InvariantCulture)
            : ((double)value).ToString("F9", CultureInfo.InvariantCulture);

        static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        string ReadBlob(string hash, int expectedLength)
        {
            if (expectedLength < 0 || expectedLength > MaximumBodyBytes) throw new InvalidDataException("Invalid body length.");
            using (var file = File.OpenRead(BlobPath(hash)))
            using (var zip = new GZipStream(file, CompressionMode.Decompress))
            using (var body = new MemoryStream(expectedLength))
            {
                var buffer = new byte[4096];
                int count;
                try
                {
                    while ((count = zip.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (body.Length + count > expectedLength) throw new InvalidDataException("Compressed body exceeds the recorded length.");
                        body.Write(buffer, 0, count);
                    }
                }
                catch (IOException error)
                {
                    throw new InvalidDataException("Compressed book body could not be read.", error);
                }
                byte[] bytes = body.ToArray();
                if (bytes.Length != expectedLength || Hash(bytes) != hash) throw new InvalidDataException("Book checksum mismatch.");
                return Encoding.UTF8.GetString(bytes);
            }
        }

        IEnumerable<LibraryBook> Books(string id)
        {
            using (var reader = File.OpenText(VolumePath(id)))
            {
                var volume = new PilotVolume(Parse(reader.ReadLine()));
                if (volume.Id != id) throw new InvalidDataException("Catalog identifier does not match its path.");
                string line;
                while ((line = reader.ReadLine()) != null) yield return new LibraryBook(Parse(line));
            }
        }

        static PilotVolume ReadVolume(string path)
        {
            using (var reader = File.OpenText(path)) return new PilotVolume(Parse(reader.ReadLine()));
        }

        void CheckBudget(long bytes, long allocated)
        {
            if (bytes > _budget - allocated || bytes > FreeBytes() - _reserve)
                throw new IOException("Archive storage budget or free-space reserve reached.");
        }

        void Publish(string path, byte[] bytes, ref long allocated)
        {
            CheckBudget(bytes.LongLength, allocated);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".pending";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                {
                    file.Write(bytes, 0, bytes.Length);
                    file.Flush(true);
                }
                File.Move(temporary, path);
                allocated += bytes.LongLength;
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        long FreeBytes() => new DriveInfo(Path.GetPathRoot(_root)).AvailableFreeSpace;
        string VolumePath(string id) => Path.Combine(_catalog, ValidateHash(id) + ".jsonl");
        string BlobPath(string id) => Path.Combine(_blobs, ValidateHash(id) + ".gz");
        static Dictionary<string, object> Parse(string json) => MiniJson.AsObject(MiniJson.Parse(json));
        static IEnumerable<string> Files(string path, string pattern) =>
            Directory.Exists(path) ? Directory.EnumerateFiles(path, pattern) : Enumerable.Empty<string>();
        static bool Contains(string value, string search) => value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        static string ValidateHash(string value)
        {
            if (value == null || value.Length != 64 || value.Any(c => !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')))
                throw new ArgumentException("Expected a lowercase SHA-256 identifier.");
            return value;
        }
    }
}
