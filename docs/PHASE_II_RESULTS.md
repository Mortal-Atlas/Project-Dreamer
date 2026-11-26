# Phase II verification

## Discovery Room

The held-out study paired seeds 1001–1064 across all four conditions at each budget. Training cues were 0, 1 and 3. The familiar probe used 0 and the held-out probe used 2. Each seed produced one score per probe and condition. Noise counts are summed across the 64 matched runs.

| Daily attempts | Curiosity | Replay | Retention | Transfer | Noise visits |
| ---: | --- | --- | ---: | ---: | ---: |
| 6 | Off | Off | 44/64 | 46/64 | 393/768 |
| 6 | Off | On | 46/64 | 48/64 | 372/768 |
| 6 | On | Off | 64/64 | 64/64 | 323/768 |
| 6 | On | On | 64/64 | 64/64 | 319/768 |
| 12 | Off | Off | 57/64 | 55/64 | 778/1536 |
| 12 | Off | On | 59/64 | 55/64 | 755/1536 |
| 12 | On | Off | 64/64 | 64/64 | 368/1536 |
| 12 | On | On | 64/64 | 64/64 | 382/1536 |
| 18 | Off | Off | 62/64 | 61/64 | 1164/2304 |
| 18 | Off | On | 64/64 | 64/64 | 1130/2304 |
| 18 | On | Off | 64/64 | 64/64 | 368/2304 |
| 18 | On | On | 64/64 | 64/64 | 367/2304 |

With replay off and six attempts per day, curiosity changed held-out success by +28.1 percentage points (approximate paired 95% interval +16.8 to +39.4 points) and noise allocation by −9.1 points (−12.7 to −5.5). Mean Brier loss changed by −0.0064 (−0.0180 to +0.0051). These descriptive intervals use the mean and standard error of 64 within-seed differences. Noise allocation improves under the prior that explicitly represents a fair-noise explanation. The curiosity conditions still hit a ceiling on the probes. This study does not isolate a replay benefit in the curiosity policy or establish transfer to mechanics outside the modeled rule family.

## Unity and performance

- 30 Release .NET tests and 31 isolated Unity 6000.3.19f1 Edit Mode tests passed. The original pilot CSVs were byte-identical across branches. Two fresh held-out Discovery Room runs produced byte-identical attempt and summary CSVs and took 79–82 ms each including process launch and export.
- Opened **Dream Machine → Latch Room Trace** in the desktop editor in Edit Mode. Clicked **Run pilot** and **Play filtered events** and confirmed playback advanced. Checked sequential IDs and nondecreasing timestamps in all 71 displayed-run events. The teacher filter returned the demonstration at tick 40. The teacher-plus-night filter rendered its empty state. The viewer also displays physical time and follows parent event IDs. The pilot has one learner per run and uses the seed to select that learner's run.
- Nine alternating launches per cast/build at Laptop quality gave 50,322 family ticks/s before and 50,025 after (−0.6%). The 12-person medians were 31,908 and 32,068 (+0.5%). No new calls enter the family tick loop. Existing paired [benchmark conditions](PERFORMANCE_BENCHMARKS.md) apply.
- The Discovery Room runs headless. Physical room scene, Android device tests and acoustic/physiology integration remain pending.

## Offline Pink Trombone synthesis check

On the i7-14700HX test host, a scratch browser harness called the original MIT exhibit's glottis and oral/nasal waveguide directly. The harness replaced the exhibit's UI startup with an offline sample loop. It reset tract and source state for each take, seeded the simplex source with 84 and the sample-loop LCG with 7231, and used 256-sample control blocks with two tract substeps per output sample. Tongue diameter was 2.3. External turbulence and aspiration input buffers were zero. Each render produced one second of float PCM. The repeated 150 Hz, tongue-index-20, 48 kHz run produced the same float PCM hash. [Original code revision](https://github.com/IMAGINARY/pink-trombone/tree/dd2e9447e1e101139aa37a949dd1e6f2ba0842b6)

| Sample rate | Commanded pitch | Tongue index | Approximate output F0 | RMS on final half |
| ---: | ---: | ---: | ---: | ---: |
| 48 kHz | 150 Hz | 20 | 155 Hz | 0.073 |
| 48 kHz | 235 Hz | 20 | 244 Hz | 0.086 |
| 48 kHz | 150 Hz | 29 | 155 Hz | 0.054 |

Estimated F0 uses normalized periodic autocorrelation in a stable window. Seven one-second renders per rate had medians of 71.6 ms at 24 kHz and 140.5 ms at 48 kHz. These are synthesis-only timings for one instance in a headless browser. They exclude model loading, binaural processing, Unity, listening and browser startup. Sound quality at 24 kHz was not evaluated. Pitch commands and geometry affected actual PCM. These checks neither measure natural speech nor establish independent vocal learning. [Dependency review and listening protocol](PHASE_II_DEPENDENCIES.md)
