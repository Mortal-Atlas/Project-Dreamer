# Library Observatory and pilot archive

## What the existing systems preserve

The family kernel keeps authored statistical priors in Library I, uses the seeded possibility atlas and `LibraryTwo.Generate` in Library II, and holds current beliefs and their appendices through `LibraryThree`. The nursery and family paths persist `AgentPersistentState` via JSON. Their snapshots include the walker, physiology, beliefs and recent observations. The family view writes individual files and separate stage clocks. This schema has no archive index, content sharing, durable per-tick provenance, or complete deterministic checkpoint contract. Unity also has a separate frame-driven state mutation path. The belief CSVs and the latch/discovery event CSVs are independent experimental outputs.

The latch pilot already emits immutable event IDs with source links for contact, outcome, memory, questions, rehearsal and probes. Its prediction counts and replay decisions are reproducible from fixture settings and completed ticks. This gives the archive a narrow, verifiable starting point. The observatory consumes a projection of the actual latch fixture. It makes no changes to the running household, original pilot logic or existing save formats.

## Use the Unity Editor

Open **Dream Machine → Library Observatory**. Choose a birth seed, curiosity/replay settings and a checkpoint tick. **Capture pilot** explicitly runs the standalone latch fixture and saves its record volume. Select an archived run in the dropdown. The capture time shown beside its label is a UTC archive timestamp. The fixture does not track a birth date or named identity beyond its seed.

- **I Knowledge** displays observed action-effect predictions, counts of effects and failures, and links to teacher/personal feedback. The probability is the actual current latch predictor. Every entry requires at least one observation. General pretrained or implicit knowledge is unavailable in this pilot.
- **II Possibility Space** displays actual recorded questions and dream-generated alternative actions. The fixture recipe, trace IDs and ticks make them reproducible. The household's existing seeded possibility atlas is preserved. A procedural Babel browser and researcher exploration are future features.
- **III Lived Memory** shows actual trace events such as teacher observation, personal attempts, waking feedback, memory entries and overnight replay. Search titles and provenance labels to find a type of experience. Book IDs remain in original event order and show the tick and simulated seconds.
- Follow **Source book** and **Referenced by** links across shelves. **Unload book from view** releases the displayed payload. Reopening it reads and checks the on-disk body. The browser keeps a bounded page of catalog metadata and one decoded book. It has no retained body cache.
- **Dream Machine → Archive Manager** opens the Archive tab. Set a capture budget in MiB, measure archive file size and body reuse, and verify restoration of the selected checkpoint. Capture and storage scans run on a worker. Browsing reads immutable files. Restoration creates a separate fixture and checks the replay fingerprint. It leaves any live learner alone.

## Storage and checkpoint schema

The default archive is `library-observatory-v1` beneath Unity's persistent data path. The headless command accepts an explicit directory and uses the same format.

| Address | Contents |
| --- | --- |
| Volume ID | SHA-256 of schema version, latch fixture version, birth seed, curiosity/replay flags and completed tick. One volume represents one pilot snapshot. |
| `volumes/<volume-id>.jsonl` | Header with UTC capture time and fingerprint of the generated trace/prediction projection. Subsequent rows give book ID, shelf, title, source, simulated tick, related book IDs, UTF-8 body length and SHA-256 body address. Links use the volume's namespace. |
| `blobs/<body-hash>.gz` | Independently compressed UTF-8 book payload with a 64 KiB decoded limit. The address covers the original bytes. Timestamps, claims and relationships stay in each separate catalog row. Identical payloads from multiple rows or volumes share one file. |

Capture locks writers within a root, preflights the new byte cost and free-space reserve, and publishes compressed bodies before the volume. Files are flushed and renamed from sibling temporary files on the same filesystem. The volume is the publication boundary. An interrupted process can leave unreferenced bodies or pending files. Pending catalogs stay invisible, existing memories remain intact, and later captures can reuse verified bodies. No garbage collection or privacy deletion is performed in this release. Reported physical bytes are archive file lengths, excluding filesystem allocation overhead. The default total capture budget is 64 MiB with a 256 MiB free-space reserve. Archive writes stop with a visible error under these limits rather than deleting existing records.

Search streams one volume's line index and returns at most 32 rows. It covers titles and provenance. It never opens payload files during search. It is a linear metadata scan with no full-text or semantic index. A corrupt or missing payload fails on the requested book with a checksum/decompression error. The format currently relies on a single local archive and OS access to that directory. Scoped IDs keep application lookups in the selected volume and provide no OS-level user isolation.

The checkpoint recipe restores the exact versioned latch experiment by replaying its deterministic ticks and comparing the projected trace fingerprint, including recorded predictions. New volumes use schema 2 with fixed decimal probability formatting shared by Unity Mono and .NET. The initial schema 1 prototype used runtime-dependent shortest formatting and remains readable. A regression resumes a pre-wake checkpoint through both waking probes and matches the uninterrupted trace and encounter records. This is reconstruction for an unmodified latch fixture. Arbitrary interventions, household weights, physiological state, external dependencies and `.dreamer` portability are separate work. Preserve a recipe's simulation and projection versions if they change.

## Verification and storage measurements

```sh
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --archive-pilots <fresh-directory> --runs 250
```

The .NET run archived 250 seeded pilot volumes with 19,250 books and 80 unique compressed bodies. The archive used 3,848,005 file bytes, or about 15.4 kB per 1,072-tick pilot. Compressed bodies accounted for 7,969 bytes against 1,945,874 logical body bytes. Catalogs made up the rest of the archive, so reducing duplicate bodies did not make the whole archive smaller than the unindexed payload sum. This is a short-fixture growth measurement. It supplies no lifetime projection for multimodal records.

The run captured and published the volumes in 5.47 s. One cold-to-that-run teacher search/read took 3.34 ms and one verified recipe restore took 0.69 ms. Peak process working set was 58.4 MB for the whole command. These single-run timings are host-specific. Nine alternating family benchmark launches per build measured 47,926 ticks/s before and 47,911 after. The 12-person medians were 30,774 and 31,033. No archive work enters the simulation tick loop.

All 34 Release .NET and 35 Unity Edit Mode tests passed. Unity opened the actual observatory in Edit Mode, showed acquired predictions and a night-generated alternative, and resolved its replay source. Unloading and reopening the book returned the same body. Unity also opened a .NET-produced schema 2 archive, restored tick 1,072, and read the teacher demonstration at tick 40. The established household and Android runtime were outside this archive smoke test.

## Technology review

The current implementation uses the existing JSON reader, .NET GZip and SHA-256. It adds no packages or native libraries. UTF-8 and independent gzip bodies are suitable for small deterministic pilot snapshots. For lifetime archives, benchmark sustained writes, lookup latency and memory using the same representative data before choosing a native index or codec.

| Candidate | Fit and terms |
| --- | --- |
| SQLite and FTS5 | Public-domain SQLite. A candidate for indexed metadata, terms, joins and transactional revisions at higher volume counts. Verify that the eventual Unity/Android provider ships FTS5 in all builds and evaluate migration from current indexes. [SQLite terms](https://sqlite.org/copyright.html) · [FTS5](https://sqlite.org/fts5.html) |
| Zstandard and BLAKE3 | Zstandard supports BSD or GPL-2.0 licensing and BLAKE3 offers public-domain/CC0 and Apache-2.0 terms. Compare speed, compression and Unity/Android packaging before adding either. Existing hash IDs must remain resolvable after a migration. [Zstandard](https://github.com/meta/zstd) · [BLAKE3](https://github.com/BLAKE3-team/BLAKE3) |
| FLAC and Opus | Use a pinned FLAC implementation for lossless measured audio and evaluate explicitly labeled Opus previews only where fidelity rules permit. The reference codec libraries use permissive BSD-style licenses. Individual recordings require independent terms. [FLAC](https://xiph.org/flac/) · [Opus](https://opus-codec.org/) |
| glTF and Parquet | glTF is a Khronos format for structured scene payloads with format and asset rights handled separately. Parquet is an Apache project suitable for offline scientific columnar analysis. Treat both as later attachments or exports before routing cognition through their libraries. [glTF](https://www.khronos.org/gltf/) · [Parquet](https://parquet.apache.org/) |
| Restic and memory mapping | Restic is BSD-2-Clause backup tooling. Verify restore separately from archive moves. A read-only memory-mapped index may help after measuring throughput and Android support. Do not map the full archive into the active process by default. [Restic](https://restic.net/) · [.NET memory mapping](https://learn.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files) |

## Next increments

1. Export real family and Discovery Room states with independent immutable scientific logs and versioned identities. Extend provenance to corrections, dream versus observed data and authorized researcher annotations.
2. Use segmented indexed volumes per individual/day and measure bytes per day, RAM and p50/p95 lookups over long simulated lifetimes. Compare SQLite/FTS5 and lightweight local semantic models. Preserve read-only effects and use separate roles for query and intervention.
3. Add reversible hot/warm/cold/external tiers with bounded caches, catalog-only access while disconnected, verified copy and explicit fidelity metadata. Lossless compression and reconstruction require input and version manifests plus measured fidelity. Preserve exact recordings designated as experimental evidence.
4. Add versioned full and incremental checkpoints and explicitly verified reference-based and self-contained exports. Protect retained references before implementing collection. Multimedia and immersive rooms use the same catalog and preservation rules.
