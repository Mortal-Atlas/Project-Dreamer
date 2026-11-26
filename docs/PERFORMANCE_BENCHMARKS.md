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

## Connectome feasibility

Approximate sizing uses 27 directed weighted neuron pairs per neuron for reduced test graphs. The full reference row uses about 3.7 million connected pairs as an illustrative aggregate, based on published analysis thresholds. The source also has around 54.5 million individual chemical contacts. Recompute counts from the specific release and filter and preserve them in the experiment manifest. Storage estimates cover 8 bytes per edge for shared indexed connectivity and base weight, plus 4 bytes per edge for each of two learners. CSR row offsets, metadata, delays, activity traces, checkpoints and scratch memory are extra. [FlyWire paper](https://doi.org/10.1038/s41586-024-07558-y) · [v783 dataset](https://zenodo.org/records/10676866)

| Neurons | Illustrative pairs | Packed edges plus two learner weights | Full edge visits/sec at 20 Hz per learner |
| ---: | ---: | ---: | ---: |
| 1,000 | 27,000 | 0.43 MB | 0.54 million |
| 10,000 | 270,000 | 4.32 MB | 5.4 million |
| 50,000 | 1.35 million | 21.6 MB | 27 million |
| 139,255 | ~3.7 million | ~59 MB | ~74 million |

At 1,000×, a full 20 Hz edge scan would demand roughly 74 billion visits per wall second per learner. Spiking reference models can require finer integration and incur higher cost. A contact-level array at the same 16-byte shared-plus-private budget would take about 872 MB before overhead. The v783 raw synapse Feather download is about 9.49 GB; the proofread connections file is about 852 MB. Parse the reference offline and consider event-driven updates and calibrated condensed graphs. Compare synthetic and biological variants with matched edge/activation budgets. On Android measure thermal and allocation behavior independently. Keep every world tick and validate any neural scheduling choices on event order, learning and replay before use. [Published neural model](https://doi.org/10.1038/s41586-024-07763-9) · [Dataset sizes](https://zenodo.org/records/10676866)
