# Phase L.2 · Independent play, retention and transfer

## Baseline audit

The preserved 307 records contain 142 demonstrations, 67 firsthand outcomes, 57 witnesses, nine successful imitations, 16 failures and 16 replay references. Learners generated 57 firsthand outcomes and six failure records. The 57 witness records link to 54 distinct demonstrations. There were 132 repeat demonstrations after each teacher’s first demonstration per activity. Of the 307 records, 194 report a real object mutation. Replay selections covered Stack (4), Roll (3), Inspect (3), Drink (3), Open (2) and Close (1). Old episodes describe outcomes and references. The archived CSV alone cannot reconstruct time spent resting, obstructed or observing, individual interest changes, or competence retained in a new arrangement. [Original archive](evidence/household-experiences.csv)

## Fixed retention comparison

Use the same three-socket abstract shape board as the household. All conditions receive the same 36 acquisition trials across four simulated acquisition days. Each cue gets all three sockets tested once per day in a seeded cyclic order. The logged outcome comes from the real shape-board fit rule. The probe uses the same fixed exploration draws, six delayed trials on the learned board and twelve trials with two rotated arrangements. Probe feedback stays outside later decisions. Use development seeds 771–780 and held-out seeds 781–796. Compare mean delayed and transfer success separately. Count wrong choices that match earlier errors. With exhaustive acquisition, every wrong probe choice matches an earlier error under this definition.

| Condition | Information available at probe time |
| --- | --- |
| A | Matched exploration draws. The experiment keeps the same acquisition archive. |
| B | Direct retrieval of the latest actual successful placement for the current cue. |
| C | Replay builds plans for two of three cues from actual outcome references before the probe. Daytime selection reads only the compiled plans. |
| D | The same replay plans with direct retrieval for uncovered cues. |

Both reuse paths can apply the same explicit observed arrangement transform. This is a controlled test of access and an engineered transform, with an immediate execution of the delayed protocol and no simulated time-decay mechanism. It does not demonstrate discovery of the transform, biological sleep, or unconstrained transfer. [Methods background](EXTERNAL_RESEARCH_RESOURCES.md)

## Matched household run

Replayed the original and current compiled kernels with the same Phone preset, default cast, seed 37 and 4,700 fixed ticks. The original produced 307 records, matching the saved Unity archive. The following counts distinguish who taught, acted or witnessed. Firsthand includes successful need-driven actions. A failure can come from a missed fit, blocked approach, unavailable result or night interrupt. The old archive cannot identify whether every unrecorded decision was autonomous or whether an individual changed its preference during play.

| Person | Demonstrations old → new | Firsthand old → new | Witnessed old → new | Imitations old → new | Failures old → new |
| --- | ---: | ---: | ---: | ---: | ---: |
| Merlin | 69 → 11 | 5 → 68 | 0 → 0 | 0 → 0 | 5 → 4 |
| Marie | 73 → 16 | 5 → 73 | 0 → 0 | 0 → 0 | 5 → 4 |
| Arthur | 0 → 0 | 31 → 42 | 28 → 1 | 4 → 0 | 3 → 4 |
| Alice | 0 → 0 | 26 → 27 | 29 → 4 | 5 → 2 | 3 → 5 |

Merlin's old archive most often mentions stacking (21 records) and inspecting (19). Marie's most often mentions rolling (30) and stacking (19). Arthur's most often mentions stacking (20) and eating (19). Alice's most often mentions rolling (17) and stacking (16). Those counts include all provenance types. Each person had four replays in both runs. The new replay references covered Rest (4), Roll (2), Drink (2), Open (2), Stack (2), Sort (2), Close (1) and Tidy (1). [Original trace](evidence/household-experiences.csv) · [L.2 trace](evidence/household-l2-experiences.csv)

Purposeful downtime counted post-tick resting and sleep-in-bed states, excluding blocked rest. Unintended idle counted time explicitly marked blocked while resting. Observe counted deliberate watch state. Approach and interaction states, including a possibly slow route, stay outside these totals. Values are simulated seconds at 0.05 s per tick.

| Person | Rest or sleep old → new (s) | Blocked rest old → new (s) | Observe old → new (s) |
| --- | ---: | ---: | ---: |
| Merlin | 90.10 → 90.30 | 0.50 → 0.00 | 0.00 → 0.00 |
| Marie | 91.40 → 100.35 | 1.50 → 0.50 | 0.00 → 0.00 |
| Arthur | 74.25 → 80.00 | 0.00 → 0.00 | 3.50 → 1.40 |
| Alice | 76.20 → 74.25 | 0.50 → 1.00 | 3.45 → 0.00 |

The new household recorded 212 object changes in 277 episodes, including 34 placements and 20 tidies. Shape sorting changes the board revision only when a piece fits and cycles through arrangements. Tidying moves the current stack count into the toy bin and clears the stack. These are kinematic affordances. Arthur logged one independent fit in one attempt. Alice logged one in two. Neither independently reused a placement in this run, so these observations establish independent completion only. The retained-placement pathway reuses earlier successful evidence when a cue repeats. A witness and an imitation remain separate from independent competence. The teaching gate uses proximity to a paired learner, a per-activity demonstration cooldown and successful prior practice. Reduced teaching was accompanied by less witnessing and imitation in this seed. Multi-seed independence and knowledge trends, learner requests for help, and physical skill remain open.

The activity score combines birth-seeded tendencies, curiosity, proximity, novelty, previous firsthand successes and failures, and a small restatement of the last replay target. It penalizes repeating the immediately previous object. The experience tray shows selection reason, attention target, recorded affordance evidence, replay source, independent fit count and the latest fit provenance. Day history queries a bounded tick window and shows the last four entries for the selected person. It groups by nominal ticks and may differ from a day transition on the accumulated floating-point world clock. A separate preference-change history and explicit imagined alternatives are future work.

## Retention and transfer results

| Condition | Delayed, held-out | Rotated, held-out | Repeated mistakes, held-out |
| --- | ---: | ---: | ---: |
| A · Exploration | 39/96 (40.6%) | 61/192 (31.8%) | 188/288 |
| B · Direct retrieval | 96/96 (100%) | 192/192 (100%) | 0/288 |
| C · Replay plans | 76/96 (79.2%) | 146/192 (76.0%) | 66/288 |
| D · Both | 96/96 (100%) | 192/192 (100%) | 0/288 |

Replay alone outperformed matched exploration with its two-cue plan budget. Direct retrieval reached the ceiling. The combined condition equaled direct retrieval, so this protocol establishes no incremental benefit from replay when retrieval is available. The rotated task supplies the arrangement transform; it cannot establish transfer to an unfamiliar rule. The fixture controls exposure and trial budgets. Replay compilation and retrieval have different costs by design, and the fixture does not impose an equal CPU budget. Next, reserve independent layouts that disclose no transform, measure actual elapsed retention intervals, avoid exhaustive acquisition, and hold a separate final test set. [All development and held-out probes](evidence/phase-l2-retention.csv)

```powershell
dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --retention <scratch-retention.csv>
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --benchmark --living
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --benchmark --agents 12
```

## Native verification and continuation

Release .NET passed 80 tests. Unity 6000.3.19f1 passed 81 isolated Edit Mode tests. DX12 Play Mode passed the paused clock and one-tick step, movement tracking, planted stance, directed gaze, observer/control byte equality, and exact save/restore. The run compared the control after opening and closing the tray, changing person and history day and reading the household. The smoke test also verified retention of the original version 1 archive. An old version 2 synthetic household checkpoint restored its 307 records and continued identically for 60 ticks across a version 3 round trip. No original household save was used. The images still show the procedural view. Imported materials, skinning and animation await UniVRM. [Living](evidence/household-l2-living.png) · [Life & experience](evidence/household-l2-experience.png) · [Alice sorting](evidence/household-l2-sorting.png) · [Measurements](PERFORMANCE_BENCHMARKS.md#phase-l2-independent-activity)

Follow the private Unity package workflow, then preview Merlin first. Verify all visual checks against actual materials and motion with the current household identity intact. Confirm the stock Studio entries before expanding private previews. Keep Phase C.1.3's native cancellation and lowering work on its independent fixture. [Package and avatar workflow](ASSET_AND_LICENSE_REGISTRY.md#manual-unity-package-manager-steps) · [Native lower-body gates](LOWER_BODY_C12.md)
