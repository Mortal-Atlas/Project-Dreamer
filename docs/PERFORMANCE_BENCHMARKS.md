# Performance and verification baseline

Windows host measured November 26, 2026. Intel Core i7-14700HX, 20 cores and 28 logical processors, 15.7 GiB usable physical RAM, RTX 4070 Laptop with 8,188 MiB VRAM reported by `nvidia-smi`, and approximately 237 GiB free on the project drive. The machine had .NET SDK 9.0.315 with the .NET 8 runtime and Unity 6000.3.19f1 with Android tools installed.

## Headless results

The Release benchmark used a 16×16 Laptop grid, seed 11, 30 warm-up ticks and 2,000 measured whole-world ticks per launch. Results below are medians and min–max ranges from nine process launches per variant per cast, alternating before/after order on the same host. The historical 75.9k family measurement has different or unspecified conditions. The paired comparison isolates the change more closely, with short process timings still sensitive to scheduling and JIT warm-up.

| Cast | Before, ticks/s | After, ticks/s | Median change |
| --- | ---: | ---: | ---: |
| Four, two learners | 50,213 (49,171–52,246) | 50,000 (47,944–51,861) | −0.4% |
| Twelve, six learners | 30,799 (28,671–32,930) | 31,826 (29,857–33,084) | +3.3% |

At 0.05 simulated seconds per tick, 1,000× needs 20,000 world ticks/s. The headless median exceeds that rate for both fixtures. Unity and Android frame budgets, rendering and thermals need separate measurement. Achieved-time HUD values can differ from requested speed and fluctuate when sampled frame by frame.

The existing belief-shift experiment ran 12 seeds × 80 trials per condition and shifted the rule at trial 40. A (prior) and B (prior plus hypotheses) both scored 70.0% pre-shift and 42.9% in the late window. C (full with lived amendments) scored 80.0% and 73.8%. The three-condition trial log and summary, growth sample and physiology CSV were byte-identical before and after the timing repair. These outcomes measure the existing task and support no conclusion about the proposed room loop.

```sh
dotnet test dotnet/DreamMachine.Core.Tests -c Release
dotnet build dotnet/DreamMachine.Cli -c Release
dotnet run --project dotnet/DreamMachine.Cli -c Release --no-build -- --benchmark
dotnet run --project dotnet/DreamMachine.Cli -c Release --no-build -- --benchmark --agents 12
dotnet run --project dotnet/DreamMachine.Cli -c Release --no-build -- --seeds 12 --trials 80 --shift 40 --out <scratch-dir>/belief_trials.csv
```

## Editor and regression coverage

- Before the repair, 23 shared tests passed in Release .NET and in the existing Unity workspace. A clean Unity import blocked tests because the assembly definition referenced both explicit test runners and the older `TestAssemblies` alias. After removing the duplicate entries, the isolated Unity checkout passed 25 Edit Mode tests. Release .NET passed 24. The batch timing regression failed before the repair. It replays the exact tick count into a same-seed kernel, compares state hashes, and checks the time budget. The current state hash samples selected fields.
- The original scene, household hierarchy, camera panels and HUD rendered in Unity 6.3.19f1 on DX12. No magenta shading appeared in the captured view. Labels overlapped in the captured viewport and a background Unity AI package reported account errors. The first Play Mode exit triggered an access violation in the native PhysX scene release path.
- The rolling Edit Mode `QuaternionToEuler` error traced to the main camera's serialized rotation. After normalization and scene reload the live log stopped recording that error. An Edit Mode test now checks the scene camera's unit rotation. In an isolated project copy using the same Unity version and DX12, five 12-second batch Play Mode runs initialized the family and returned to Edit Mode with no observed crash. The batch harness explicitly bootstrapped the family app. The isolated desktop editor also returned to Edit Mode five times, with the harness confirming family startup and runtime checks in two of those cycles. The desktop editor reported JobTempAlloc warnings during process shutdown. The underlying cause of the original native crash remains unconfirmed.
- The Android APK, target-device performance and detailed CPU/GPU allocation profiles are pending. Current benchmark numbers describe the headless shared kernel on this PC.

### Latch-room pilot verification

The opt-in room pilot introduces no calls in the family tick loop. Nine alternating Release process launches per build and cast measured the audit branch at 54,569 family ticks/s and 34,693 twelve-person ticks/s. The pilot branch measured 53,435 and 34,778 respectively. These are median short-process timings on the same machine. The family ranges overlapped, and the median difference was −2.1%.

The pilot passed 27 Release .NET and 28 isolated Unity Edit Mode tests. Two fresh 32-seed runs produced byte-identical event and summary CSVs. The [protocol](EXPERIMENTAL_PROTOCOLS.md#pilot-results) records the evidence and limitations of the comparison.

## Connectome feasibility

Approximate sizing uses 27 directed weighted neuron pairs per neuron for reduced test graphs. The full reference row uses about 3.7 million connected pairs as an illustrative aggregate, based on published analysis thresholds. The source also has around 54.5 million individual chemical contacts. Recompute counts from the specific release and filter and preserve them in the experiment manifest. Storage estimates cover 8 bytes per edge for shared indexed connectivity and base weight, plus 4 bytes per edge for each of two learners. CSR row offsets, metadata, delays, activity traces, checkpoints and scratch memory are extra. [FlyWire paper](https://doi.org/10.1038/s41586-024-07558-y) · [v783 dataset](https://zenodo.org/records/10676866)

| Neurons | Illustrative pairs | Packed edges plus two learner weights | Full edge visits/sec at 20 Hz per learner |
| ---: | ---: | ---: | ---: |
| 1,000 | 27,000 | 0.43 MB | 0.54 million |
| 10,000 | 270,000 | 4.32 MB | 5.4 million |
| 50,000 | 1.35 million | 21.6 MB | 27 million |
| 139,255 | ~3.7 million | ~59 MB | ~74 million |

At 1,000×, a full 20 Hz edge scan would demand roughly 74 billion visits per wall second per learner. Spiking reference models can require finer integration and incur higher cost. A contact-level array at the same 16-byte shared-plus-private budget would take about 872 MB before overhead. The v783 raw synapse Feather download is about 9.49 GB; the proofread connections file is about 852 MB. Parse the reference offline and consider event-driven updates and calibrated condensed graphs. Compare synthetic and biological variants with matched edge/activation budgets. On Android measure thermal and allocation behavior independently. Keep every world tick and validate any neural scheduling choices on event order, learning and replay before use. [Published neural model](https://doi.org/10.1038/s41586-024-07763-9) · [Dataset sizes](https://zenodo.org/records/10676866)
# Atomic checkpoint measurements · November 26, 2026

Release .NET, Laptop preset, 16×16 grid, seed 7, after 1,140 ticks. Nine repetitions after one warm operation for each row. Windows workstation with i7-14700HX and 15.71 GiB RAM. The owner's Unity editor remained open. These timings measure allocation and checkpoint cost on this workstation and do not qualify as a clean headless throughput comparison.

| People | Payload | Operation | Median ms | Min–max ms | Thread allocations per operation |
| ---: | ---: | --- | ---: | ---: | ---: |
| 4 | 54,475 bytes | Capture | 1.25 | 1.18–4.79 | 660,824 bytes |
| 4 | 54,475 bytes | Validate and reconstruct | 2.83 | 1.37–17.95 | 992,081 bytes |
| 4 | 54,475 bytes | Publish with prior backup | 13.08 | 12.24–48.04 | 2,762,861 bytes |
| 4 | 54,475 bytes | Read and restore | 0.99 | 0.92–3.79 | 1,047,008 bytes |
| 12 | 142,838 bytes | Capture | 2.99 | 1.94–13.74 | 2,060,180 bytes |
| 12 | 142,838 bytes | Validate and reconstruct | 2.42 | 2.25–6.15 | 2,847,267 bytes |
| 12 | 142,838 bytes | Publish with prior backup | 34.96 | 23.74–55.83 | 8,049,186 bytes |
| 12 | 142,838 bytes | Read and restore | 5.14 | 4.24–6.10 | 2,990,600 bytes |

An isolated Play Mode panel creation took 4.565 ms and coincided with a 216 KiB managed-memory increase. Allocated bytes include temporaries and are not retained-memory measurements. The single panel sample includes concurrent allocations. There is no fresh process-isolated throughput or retained-memory comparison. The existing CLI has four- and twelve-person cases. A one-individual comparison requires a supported single-individual scenario.

## Modular tray follow-up

In isolated Unity Play Mode, first open took 5.815 ms (seed 18) and 4.832 ms (seed 101), each with an immediate 216 KiB managed-memory increase. Timings include preference publication. After eleven repeated close/open cycles and a final close, forced-GC managed memory was 3,280,896 and 3,313,664 bytes above each run's start reading. World state advanced by 450 ticks, memories grew and camera textures changed quality during each interval. These readings include application and editor state and cannot isolate retained tray memory or establish a leak. Native GPU memory was not measured. Older checkpoint capture and restore baselines above remain applicable as historical measurements. The owner's Unity editor remained open, so the clean throughput rerun remains pending.

## Isolated elbow laboratory

`--elbow-lab` on the clean Windows worktree measured 1.3 µs per tick for ten substeps plus tactile/proprioceptive feedback (15,000 ticks), with 6 B/tick amortized trial resets. The 464-byte checkpoint took 3.4 µs to capture and 3.4 µs to reconstruct, with 2,104 and 2,000 temporary bytes/op respectively (500 measurements each). Atomic publication averaged 9.30 ms and about 21.6 kB allocated (15 publications), including flush and validation. These are means from one warm run, with the owner's Unity editor open. Core timings exclude the CSV writer and do not isolate each sensor stage. Observer rendering and retained memory costs are not measured for this fixture.

## Native elbow and learned prediction

Unity 6000.3.19f1, Windows DX12 isolated Play Mode, four 80-tick cases and ten 5 ms substeps per tick. The owner's editor remained open. Per tick means were 7.36–10.01 µs for motor application, 355.92–438.77 µs for native `Simulate` including callbacks, and 0.97–1.16 µs for final sensor formatting. Standard contact processing accounted for 9.27 µs inside the 438.77 µs simulation interval. The callbacks and timings are nested. Recorder allocations and one-time scene creation were outside the timed steps. The thread allocation counter recorded 0 B/tick around native Step in these runs.

Four model fits (including calibration) took 18.35 ms combined. Inference averaged 0.050–0.065 µs over 10,000 calls per model. Saved model files were 457–502 bytes. Native checkpoint publication and restoration were exercised but not separately timed. Tray-open/closed costs, retained GPU memory and process-isolated native throughput remain open. The measured numerical baseline above is historical and is not an equivalent replacement for the native solver.

## Phase B closed-loop motor study

The same Unity 6000.3.19f1 Windows DX12 workstation ran 12 configurations × four conditions × 24 ticks with the owner's editor open. Time around each whole study step covered prediction, adaptation, ten physics substeps, contact sensors and mechanical-work instrumentation. Mean per-condition step times were 335.0 µs engineered, 300.4 µs frozen, 289.5 µs native adaptive and 297.5 µs transfer adaptive. Within-condition trial means ranged from 254.8 to 584.9 µs across all four. Conditions ran in the same fixed order per trial, so timing differences do not isolate adaptation overhead or establish improvements over Phase A.2.

One 1,596-byte composite checkpoint published in 15.823 ms. A fresh process loaded and reconstructed it in 25.043 ms. Both measurements are single observations and include file validation and reconstruction where applicable. No owner editor session was closed.

At the same paused predictive checkpoint, 200 direct tray refresh calls averaged 19.204 µs open versus 0.071 µs closed. This measures formatting and workspace dispatch and excludes camera rendering, UI layout work in subsequent frames and graphics cost. After twelve same-frame close/open cycles followed by seven frames for deferred destruction, forced-GC managed memory was 2,359,296 bytes above the paused baseline. This one short editor measurement includes UI and editor initialization and cannot distinguish reusable retained caches from a leak.

## Phase B.1 force-aware study

On the same workstation with the owner's Unity editor open, held-out impulse inference averaged 2.384 µs over 192 predictions. Whole study steps averaged 270.6 µs engineered, 270.1 µs frozen, 271.3 µs Phase B adaptive, 281.4 µs force-aware and 279.6 µs uncertainty gated. These are fixed-order exploratory timings with plotting and CSV outside the timed section. The 17,006-byte force-aware checkpoint was saved and restored. Separate I/O timing was not collected for this format.

At tick 7, 200 direct workspace refreshes averaged 20.442 µs open and 0.079 µs closed. The measurement excludes the separate camera render and later Canvas layout. Twelve cycles left 2,387,968 additional managed bytes following forced GC at the final paused sample. The single run cannot isolate a long-term leak or native/GPU allocation.

## Phase C.0 family stepping

Seven alternating Windows Release pairs used seed 11, Laptop preset, the 16×16 grid, 30 warm-up ticks and 2,000 measured ticks. The owner editor remained open and isolated validation activity overlapped part of the benchmark. No Unity rendering or C.1 support experiment entered the timed core loop.

| Cast | Baseline `4f03b84` median ticks/s | Corrected median ticks/s | Difference |
| --- | ---: | ---: | ---: |
| Four | 45,452 | 45,224 | −0.5% |
| Twelve | 29,621 | 29,094 | −1.8% |

These measured deltas are smaller than the between-run variation and do not replace the historical environment-specific baseline. Exact tick progression is unchanged. The yard sweep runs only when a learner requests movement.

## Phase C.1 native lower body

In the isolated DX12 editor, native stepping averaged 0.831 ms per 0.05 s experiment tick across 20 configuration and condition means (0.788–0.937 ms range). The timed scope includes ten PhysX substeps, drive evaluation and physics telemetry and excludes CSV formatting and rendering. This corresponds to about 1,200 fixture ticks/s for the measured workload. The 200-tick matched run used four ticks per frame in its observed condition and returned identical pelvis and foot-impulse samples. This does not establish a sustained rendered acceleration rate.

At paused tick 101, 100 direct tray refreshes averaged 27.296 µs with the tray open and 0.090 µs closed. These timings cover dispatch and telemetry formatting. Frame layout, GPU work and retained allocations are outside the timed scope. The export control wrote 101 tick rows with both world-space foot trajectories.

Five fresh Release CLI process launches per cast gave household medians of 44,720 ticks/s for four people (34,814–45,653) and 28,870 for twelve (26,547–29,471). The owner's Unity editor remained open and isolated editor work overlapped this benchmark. The opt-in PhysX fixture has no household kernel call path. The timings are descriptive and do not establish that C.1 changed household throughput.

## Phase C.1.1 lateral balance

Held-out DX12 trials on the same Windows workstation averaged **0.801 ms/tick** for the sagittal ballast reference, **0.835 ms/tick** for frontal pose, and **0.833 ms/tick** for the feedback variant. Each mean combines 36 standing and transfer windows. The timed scope includes ten native substeps, drives, sensor and contact accumulation, including CoP for the extended variants. It excludes logs, CSV capture and rendering. Per-window values and failure conditions are in the [matched data](evidence/phase-c11-heldout-summary.csv).

At the verified paused stance, 100 direct workspace refreshes averaged 47.440 µs open and 0.089 µs closed. These measurements include telemetry formatting and dispatch. Retained memory, Canvas layout, and GPU rendering are outside this timed comparison.

The separate Release household benchmark again used five process runs for each cast. The four-person median was 45,785 ticks/s (45,357–47,485), and the twelve-person median was 29,331 ticks/s (28,316–29,436). The owner Unity editor was open and isolated experiments overlapped the run. These are descriptive figures for the headless kernel.

## Phase C.1.2 dynamic support

The pure Release .NET dynamic support calculation averaged 21.3 ns per sample across five warmed one-million-sample trials, with zero measured per-operation allocations. The timed sample computed both XCoM coordinates and the full numerical prelift check with varying inputs. It excludes physics, observed contact collection and telemetry formatting.

Native development timing per 50 ms clock tick was 0.912 ms at 5 ms substeps, 0.465 ms at 10 ms, 0.377 ms at 12.5 ms, and 0.211 ms at 25 ms. These are means of the four matched legacy configuration means per setting. Only the 5 ms step preserved 85% stance eligibility. Faster coarse trials measured failed transfer or standing. [Timestep study](LOWER_BODY_C12.md#timestep-sensitivity)

Held-out native means at 5 ms were 0.970 ms/tick for legacy, 0.942 for interlock-only, 0.957 for smooth-only, 0.968 for prepared and 0.959 for prepared lift-only. Each is the mean of six trial means and includes contact, angular momentum and diagnostic collection. CSV generation and rendering are outside those timings. Direct paused tray refreshes averaged 55.353 µs open and 0.102 µs closed. [Per-trial data](evidence/phase-c12-heldout-summary.csv)

Five separate Release headless runs per cast gave median household throughput of 48,117 ticks/s for four people (44,845–52,596) and 30,719 for twelve (28,753–32,550). These were concurrent with editor work on the same workstation. The native rig remains an isolated opt-in experiment.

## Phase L.3 private avatars

The unchanged numerical tick path measured a median of 45,692 ticks/s in four-person Living and 27,303 in the twelve-person legacy benchmark across five processes each on this workstation. Four-person allocation stayed at 387,752 B during 2,000 measured ticks. Unity editors were active during measurement. DX12 posture updates averaged 0.4892 ms for the four skinned mature models and 0.2682 ms for the two skinned teachers and two procedural newborns, across 1,240 steps each. These values time the view update, including retargeting, eyes, expressions and UniVRM's view-side spring processing. The four-day newborn regression measured 0.2742 ms per direct view call. Rendering, GPU skinning time, memory on Android and thermal headroom require separate measurement. [Private model protocol](ACTIVE_LEARNING_L3.md#native-verification)

## Phase L.2 independent activity

Five Release CLI processes per configuration, Laptop 16×16 grid, seed 11, 30 warm-up ticks and 2,000 timed ticks. The twelve-person option measures the legacy kernel. The household affordance scene supports the four founding people.

| Fixture | Median ticks/s | Min–max ticks/s |
| --- | ---: | ---: |
| Four-person Living | 48,697 | 45,094–49,517 |
| Four-person legacy | 46,623 | 44,986–47,075 |
| Twelve-person legacy | 28,582 | 28,360–28,639 |

The previous session reported 49,531 Living ticks/s. The current value is 1.7% lower across separate sessions. The Living benchmark recorded 129 episodes and 387,752 bytes of measured thread allocation in its 2,000 ticks. Its checkpoint was 26,984 bytes. These allocations include the world tick. They do not describe resident heap or process size.

| Nominal day in matched 4,700-tick Phone run, seed 37 | New episodes | Total saved bytes |
| --- | ---: | ---: |
| 1 | 67 | 21,850 |
| 2 | 58 | 23,885 |
| 3 | 57 | 25,393 |
| 4 | 57 | 26,900 |

The active history appends 17 serialized bytes per record. Total checkpoint growth from nominal day 1 through 4 was 5,050 bytes including other systems. Longer runs grow with recorded episodes. Those runs need an append-only external archive and bounded recent working set before they can establish a memory bound. At tick 4,700 the headless checkpoint was 27,583 bytes, and 30 restorations averaged 0.660 ms. The Unity fixture checkpoint at the same tick was 27,668 bytes, including its additional existing data. Whole ticks with an event averaged 16.045 μs and other ticks averaged 14.246 μs in the headless audit. Differences include work associated with the interaction, physiological cadence and timing noise.

A scratch Release build timed selection, recording and nighttime replay at their call sites on 40 fresh runs after six warm-ups, using the same Phone workload. Selection averaged 0.197 μs across 276 calls per run. Append and evidence indexing averaged 0.061 μs across 277 calls. Nighttime replay averaged 0.440 μs across 16 calls, including the nested append and the note to Library II. The instrumented build reproduced the 277 records and 27,583-byte checkpoint. Stopwatch overhead is included, so treat these small intervals as estimates.

Isolated Unity DX12 submitted 1280×720 camera renders at a mean 0.786 ms without the tray, 0.935 ms with the tray at the same checkpoint, and 1.141 ms during the Alice sorting capture. This samples 12 camera calls per image. Direct household view updates averaged 0.0219 ms across 160 calls. Full end-to-end frame timing, GPU time, skinned asset overhead, peak working set and Android thermals remain unmeasured. [Native fixture and reproduction](ACTIVE_LEARNING_L2.md#native-verification-and-continuation)

## Playable household

Five matched-process triples used Release, the Laptop 16×16 grid, seed 11, 30 warm-up ticks and 2,000 measured ticks. The active four-person household gets new event selection, object updates and experience recording in place of the repeated abstract walking exercise and hidden-rule encounters. These numbers measure the whole kernel. They do not isolate each changed operation.

| Mode | Median ticks/s | Range |
| --- | ---: | ---: |
| Four-person legacy | 44,286 | 41,782–44,689 |
| Four-person living | 49,531 | 49,143–51,625 |
| Twelve-person legacy | 29,535 | 27,438–31,118 |

The living benchmark recorded 137 episodes, allocated 387,896 current-thread bytes across the timed whole ticks, and captured a 26,649-byte checkpoint. The fixed binary archive stores 17 bytes per episode before outer checkpoint overhead. Whole-file size also depends on older learner and environmental state. Run `dotnet run --project dotnet/DreamMachine.Cli -c Release -- --benchmark --living` to reproduce the active household benchmark.

In DX12 editor Play Mode, 160 direct household view updates averaged 0.0223 ms each, including applying actor root, foot pose, gaze, blink and current object presentation. Twelve warmed 1280×720 `Camera.Render` calls averaged 1.228 ms with the legacy presentation, 0.787 ms in living mode, and 1.305 ms with the experience panel visible. These are CPU-side elapsed submit/call timings on a frozen scene with each mode's default camera composition. GPU-only time, per-operation activity attribution and supplied-model costs remain unmeasured.
