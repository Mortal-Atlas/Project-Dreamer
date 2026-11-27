# Implementation progress

## Phase C.1.2 · dynamic stability and physical lift · November 27, 2026

Added absolute support, contact-region, XCoM and signed margin measurements, a staged preparation and swing controller, and native timestep comparisons. Four of six held-out combined trials achieved supported lift. Maximum clearance was 17.04 mm and forward displacement 57.14 mm. No trial completed a step. Two combined trials fell before lift and four after abort. The 5 ms native substep preserved eligibility where all three coarser settings failed. [Audit, failed gates and native evidence](LOWER_BODY_C12.md)

Passed 74 .NET and 75 Unity Edit Mode tests. Native DX12 exercised the staged controller, tray and fixed-clock observer comparison. Household Play Mode preserved the observed and control checkpoint digest. [Numerical, native and household measurements](PERFORMANCE_BENCHMARKS.md#phase-c12-dynamic-support)

## Phase C.1.1 · lateral balance and first swing experiment · November 27, 2026

Added native roll actuation, idealized pelvis sensors, sole pressure measurements and a gated physical swing. On three fresh seeds, both sides of frontal pose passed the 85% requirement, with 86.33–88.36% mean loading. The feedback variant passed through 80%. All six physical attempts lost stance contact and aborted. Five subsequently made non-foot contact. The verified transfer controller remains selectable apart from the swing. [Experiment, negative results and live observatory](LOWER_BODY_C11.md)

The regressions passed 70 .NET and 71 Unity Edit Mode tests. Native DX12 case runs, a 200-tick observed 4× comparison, an actual 281-tick tray export, and the household restore and pose checks passed. [Measured performance](PERFORMANCE_BENCHMARKS.md#phase-c11-lateral-balance)

The external registry records eight candidate sources and one decoded force-plate trial. Read-only VRM inspection confirmed the lower-leg mapping and measured rest proportions. Authorized import and physical-to-visual skinning remain pending. [Research sources](EXTERNAL_RESEARCH_RESOURCES.md) · [Avatar compatibility](ASSET_AND_LICENSE_REGISTRY.md#phase-c11-lower-body-retargeting-compatibility)

## Phase C.1 · native standing and transfer · November 27, 2026

Built an opt-in PhysX rig with a free pelvis, six sagittal hinges and an actuated internal ballast. Five seeded anatomies passed six seconds of standing, left/re-centered/right transfer and momentum checks. The highest stance share was 67.2%, below the 85% stepping prerequisite. The perturbation trials produced measured falls under selected and full drive disabling. The lower-body tray exports both foot trajectories and stays separate from household saves and motor policies. [Protocol, traces and actual Unity capture](LOWER_BODY_C1.md)

Release .NET passed 70 tests and isolated Unity Edit Mode passed 71. DX12 Play Mode exercised the laboratory study, visible tray and household scene. GitHub listed no submitted reviews or inline comments on #6 or #8 at the Phase C.1 check. [Native and household timing](PERFORMANCE_BENCHMARKS.md#phase-c1-native-lower-body)

## Phase C.0 · family standing, movement and controls · November 27, 2026

Native DX12 family startup reproduced the hovering teacher rest poses and movement cycles at a stopped root. Neutral knee flexion now measures 5.1° instead of 65.9° for the teachers in the matched rest sample. Seeded crown height is within 1.1 mm and the sampled stance soles remain at ground height. Both teachers moved more than 0.55 m in the matched Play interval. The idle learner that moved zero metres kept a constant knee angle. The stable Simulation and Reset layout buttons preserved the paused checkpoint. An isolated static bilateral support reference begins C.1 without altering the household.

The current regressions passed 70 .NET and 71 Unity Edit Mode tests. The isolated Play Mode harness verified the actual scene and produced before-and-after screenshots. The household remains kinematic. Genuine physical balance, falls, crawling and recovery are pending. [Audit, measured outcomes and screenshots](FUNCTIONAL_EMBODIMENT_C0.md)

## Phase B.1 · independent impulse prediction and force-aware residuals

The development forecast gate passed and held-out impulse RMSE was 0.083652 N s versus persistence 0.132759, engineered 0.106060 and frozen learned 0.100720. The engineered estimate retained a slight MAE advantage. Across 16 final configurations and 80 matched native trials the ungated controller narrowly missed the fixed tracking margin. Uncertainty gating met both tracking and load margins relative to the engineered baseline while underperforming frozen and Phase B control on work and cumulative impulse. No wrist work was started. [Experiment, data and genuine Unity capture](FORCE_AWARE_PHASE_B1.md)

57 .NET tests and 58 Unity Edit Mode tests passed. A fresh DX12 process restored the force-aware checkpoint and continued for 18 matched steps. The actual tray stepped once, then twelve passive close/open cycles preserved the entire checkpoint. No fallback fired in these 80 trials. The dedicated guard test verifies rollback and reason persistence. No temporary Git worktree was created. Section reconciliation awaits the unavailable v239 and §§240–316 texts.

## Milestone Two Phase B · predictive motor adaptation

The owner reconciled packages, project settings and looks in `adbc5af` and `5b7ebe6`. Phase B started at the synchronized `5b7ebe6`, with a clean worktree. Locally excluded Roslyn plug-ins and recovery files were untouched. [Draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) remains on the existing #6 → #7 → #8 stack. The audit found zero PR #8 reviews and inline comments. No temporary Git worktree was created.

The native controller now predicts before the physical step, collects the signed contact error afterward, and learns a bounded gain that changes later torque. The native Phase A.2 controller remains the zero-residual baseline. Fifty-six .NET tests and 57 Unity Edit Mode tests passed. The isolated DX12 study ran 48 paired physical trials and the restart check verified 18 subsequent steps in a separate process with no observed deviation.

Native adaptation reduced mean absolute command work from 1.244 to 1.122 J and cumulative impulse from 0.4070 to 0.3737 N s. Tracking error increased from 0.12781 to 0.13062 rad and mean peak impulse rose from 0.23488 to 0.24634 N s. Online adaptation and the frozen predictor performed similarly. These results support a causal learned change with tradeoffs and do not demonstrate a net motor advantage. [Phase B protocol, matched outcomes and state persistence](PREDICTIVE_MOTOR_PHASE_B.md)

## Milestone Two Phase A.2 · native contact and learned prediction

[Draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) remains on the existing stack over #7 and #6, targeting protected main. All three PR heads were reviewed for ancestry and GitHub feedback before coding. No submitted reviews or inline findings were present. The local exclusion audit remains applicable and the new fixture requires no local package overrides. [Excluded local files](WORLD_CHECKPOINT.md)

The independent local Unity physics fixture and shared sensory contract now produce matched traces beside the preserved numerical model. In the standard trial the onset matched at tick 4, while native cumulative normal impulse was 0.29405 N s versus 0.41990 N s. Native restart checks over eight ticks from five save boundaries and the native tray's pause, single-step, checkpoint and dismissal paths passed in isolated DX12 Play Mode. The active contact round trip had two sub-tolerance JSON double changes. The fixture does not restore internal PhysX solver caches.

Learned next-tick contact prediction was tested on eight held-out native trials. Native scratch scored Brier 0.06355 and F1 0.857. Planar-to-native transfer scored 0.10567 and 0.400. Eight native calibration trials improved transfer to 0.07498 and 0.769. No learned policy controlled the motor. A fresh Unity process recovered 288 identical probe predictions from three saved models. Final .NET Release and Unity Edit Mode suites passed 55 and 56 tests. The clean worktree verified the pre-predictor native foundation and retained the numerical trace digest. Visual screenshots and matched tray memory/CPU costs are open. [Native and learning experiment](NATIVE_ELBOW_LAB.md)

## Milestone Two Phase A · elbow and tactile contact

Draft PR #8 introduces the isolated fixed-step numerical elbow and puck, sensor records, fixture checkpoint, and shared Unity laboratory tray. It stacks on PR #7 at `e9ceeaf`, which depends on PR #6. GitHub showed no submitted or inline reviews on either prerequisite at the audit. Compare the milestone with `git diff origin/feature/authoritative-simulation-clock...origin/feature/embodiment-contact-lab`. PRs remain unmerged. The [local exclusion inventory](WORLD_CHECKPOINT.md) still applies and the isolated build required none of those local files.

54 Release .NET tests passed in a clean checkout, 55 isolated Unity Edit Mode tests passed, and the DX12 lab Play Mode smoke matched the unobserved model through 15 steps. The trace matrix demonstrated force-driven contact, localization, mass and friction effects, and exact checkpoint continuation on the Windows numerical implementation. The native solver, retained observer cost, actual rendered alignment, and learned prediction are open. [Experiments and reproduction](ELBOW_LAB.md)

## Modular workspace and recovery · November 26, 2026

The Simulation panel is now registered as `simulation`. The shared [tray workspace](../Assets/DreamMachine/Unity/TrayWorkspace.cs) owns its Canvas, minimum dimensions, open, close and visible-only refresh callbacks. Versioned PlayerPrefs keep normalized layout and visibility independent of the simulation checkpoint. Closing destroys the tray and removes its telemetry update. Presentation quality now releases and destroys superseded render textures. Interactive docking, drag/resize, tab grouping, and named presets remain outside this first registry.

**50 .NET Release tests and 51 isolated Unity Edit Mode tests passed.** Windows tests exercised manifest damage, truncated payloads, and a read-sharing lock that prevents file replacement. The primary and backup were preserved and the pending file cleaned on the tested failure. The prior Windows tests also cover missing primary with backup, incomplete pending files and incompatible versions. Storage exhaustion, process termination during publication, and power-loss durability remain unverified. [Recovery details](WORLD_CHECKPOINT.md)

Four DX12 Unity Play Mode runs paired control and observation for seeds 18 and 101. Each began at tick 690 and ended at tick 1140. Each observed run opened the tray, switched the selected learner and display quality, rendered previews, and repeated open/close through the day transition. The workspace reopened with its saved geometry and closed with exactly one shutdown callback. Its changes left per-seed full checkpoint digests identical. The existing fast hash was `d5b13b29` in both seeds and did not distinguish their checkpoint digests.

| Seed | Full checkpoint SHA-256, equal in control and observed runs |
| ---: | --- |
| 18 | `13ffff7d9d089b331554ad08adae22171ababf3cb56bf45c3f8dd7565dee562d` |
| 101 | `c45f59dd2fa02044c1427e6bedef11d3d6b32f73a0948b3c277880d58c4f1cb1` |

Both conditions verified save and restore in the running app. The serialized fields include current learner memories, interventions, motor state, and ordered physiology records. The embedded random streams used in this scenario are derived from seed and tick. The household has no live read-only memory browser. The existing Library Observatory browses separate archived experiment data and was outside this experiment. Batch graphics did not produce the requested screenshot. Controls and resource lifecycle were exercised programmatically, and visual clipping and alignment remain unverified. [Reproduction harness](../tools/unity/README.md)

See the [observer resource measurements](PERFORMANCE_BENCHMARKS.md). The next slice is a dedicated supported elbow and constrained object fixture with measured contact and position feedback before any adaptive model is trained. [Embodiment preparation and physical ownership](EMBODIMENT_NEXT_SLICE.md)

## Atomic world-and-learner checkpoint · November 26, 2026

[Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) now contains the embedded kernel checkpoint, load-on-start and in-place Unity restore, atomic stage export, and a persisted intervention ledger for explicit learner resets. The previous valid primary stays recoverable after publication. An invalid primary blocks startup and remains on disk. A failed in-place restore preserves the active world. Legacy individual files remain available for first-run migration when no world checkpoint exists. [State inventory, repository audit and recovery](WORLD_CHECKPOINT.md)

48 .NET Release tests and 49 isolated Unity 6000.3.19f1 Edit Mode tests passed. Three cases save at tick 690 and compare a continuous run to a file-restored instance through tick 1140, crossing the night and day transitions. They compare ordered recorded history, named learner and grid fields, and full checkpoint bytes. File tests cover interrupted pending writes, missing primary with backup, corrupt payloads and incompatible format versions. The explicit reset test verifies that passive capture creates no intervention record.

Two isolated DX12 Play Mode launches restored the same seed-18 checkpoint at tick 690. The control stepped without opening the panel. The observed run opened and closed the panel repeatedly, switched between the learners, rendered camera previews and used the Step button. They reached tick 1140 with the same checkpoint SHA-256 (`13ffff7d9d089b331554ad08adae22171ababf3cb56bf45c3f8dd7565dee562d`) and world hash `d5b13b29`. Both called the application's save and restore handlers, took an intervening tick, then recovered the exact saved bytes. This validates the tested UI paths for one observer seed. There was no visual screenshot inspection, memory-panel test, presentation-mode test or external physics restore.

See the [measured performance](PERFORMANCE_BENCHMARKS.md). The embedded household checkpoint covers the current grid and compact learner model. Persistent movable Unity props, separate asynchronous/remote subsystems and workspace layout remain open. The next infrastructure pass should add a stable panel registry with workspace persistence, extend observer controls and seed coverage, and validate the checkpoint on the target filesystem before functional embodiment adds contact and motor state.

## Milestone One clock and observer checkpoint · November 26, 2026

[Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) builds on open [PR #6](https://github.com/Mortal-Atlas/Project-Dreamer/pull/6). Reviewed PR #6 before branching. GitHub returned no submitted reviews, inline comments, or discussion findings. Code inspection found two local simulation writes in Unity presentation. The vision preview added mask observations to private learner state, and the night display blended the walker policy and recorded an additional dream hypothesis. The preview now renders only. Night presentation uses a separate derived seed for displayed dream props, while the shared kernel retains its own tick-driven cognition.

The runtime now consumes one explicit step request per evaluation, keeps the scheduler paused afterward, and honors pause during accelerated stage advancement. Pose playback uses simulated progress and camera navigation remains available during pause. The separate CLI server still owns its own clock when connected. The local pause freezes remote playback, and local step commands apply to the embedded kernel. The Simulation button opens a dismissible controls panel with pause, resume, step, speed and learner save. Closing it destroys the panel and its listeners. The panel shows the fixed simulation tick and simulated seconds separately from measured throughput.

- 43 Release .NET tests and 44 isolated Unity 6000.3.19f1 Edit Mode tests passed. The new clock test checks a single tick against the direct kernel path and verifies that paused catch-up preserves scheduler debt.
- Two isolated DX12 Play Mode launches loaded identical learner files and world seed 7. In the observed condition the harness opened the panel, rendered the vision preview, stepped via the real button and closed the panel. The control condition sent the equivalent step commands without either interface. Both reached exactly tick 24, produced world hash `d19b8f7b` and produced byte-identical serialized Arthur state. The current world hash and learner JSON cover selected state. They cannot establish full event ordering, every RNG stream, or changes over longer runs. The harness ran from a scratch project, and no rendered screenshot was inspected.
- The serialized learner payload was 8,898 bytes. Average parse time over 100 warm parses was 0.250–0.271 ms. One panel creation took about 2.9 ms and coincided with a 192 KiB managed-memory increase. This single reading includes other managed allocations. Raw managed totals were about 987 MiB for the control process and 893 MiB for the observed process. Process-to-process differences prevent treating these as a controlled panel-memory comparison.
- Five Release CLI launches per population gave medians of 41,145 ticks/s for four people and 28,349 ticks/s for 12. These runs overlapped an isolated Unity process. Treat them as throughput observations under contention. No change enters the headless tick loop.
- The clock and panel are partial implementations of §§6, 124, 133 and 169. Restore controls, a reusable docking registry and workspace layout persistence remain open. The existing per-learner save still omits live world time, environment, wetware, and some physiological and RNG state. The master roster and VRM rights record carry forward with the existing stable IDs. The retargeting adapter and authorized avatar tests remain open.
- Next, inventory mutable kernel fields and write a versioned, atomic checkpoint that restores them together into a fresh kernel instance. Assert deterministic continuation over a day transition before adding a restore control. Add full-state, event-order and RNG comparison to the observer experiment once those states can be snapshotted.

## Milestone Zero and first Milestone One slice · November 26, 2026

The active branch began at `635c55f`, the recovered rig commit in [PR #5](https://github.com/Mortal-Atlas/Project-Dreamer/pull/5). The historical `FormatException` compile failure was already fixed. The preceding [audit](https://github.com/Mortal-Atlas/Project-Dreamer/pull/1), [latch pilot](https://github.com/Mortal-Atlas/Project-Dreamer/pull/2), [Discovery Room](https://github.com/Mortal-Atlas/Project-Dreamer/pull/3) and [archive](https://github.com/Mortal-Atlas/Project-Dreamer/pull/4) PRs were open and are ancestors of this branch. `origin/main` was `02fbc84` during the audit. Local Unity settings, packages, recovery scenes, appearance edits and plug-ins were excluded from this work. The existing rescue branch still points at `e5bacd4`.

GitHub's branch rules endpoint reported ruleset 28195710 on `main`. It showed PRs required, deletion and force-push blocked, zero required approvals, stale review dismissal, additional unattributed-change approval, three permitted merge methods, zero named required checks, and Copilot review with draft and new-push review disabled. The bypass list was reported empty by the owner. No changes were made to GitHub protection. [Active branch rules](https://api.github.com/repos/Mortal-Atlas/Project-Dreamer/rules/branches/main)

The full specification was saved with its original Phase II appendix. The source and workspace copy both hashed to SHA-256 `F0BEA08038136BE775FFB340BC3B3911824536D4349005CFEA889E19B4B19620`. Appendix A matched the previously supplied Phase II text after normalization of line endings and outer whitespace. [Master specification](MASTER_IMPLEMENTATION_SPECIFICATION.md) · [255 traceability rows](REQUIREMENTS_TRACEABILITY.md)

The first infrastructure slice covers part of §§5, 10, 57, 162 and 169. Unity now validates the saved individual before constructing the yard and pauses startup with a diagnostic if data is damaged or belongs to another identity or birth seed. An absent save still initializes a newborn. Each child save is flushed to a pending file and published by a same-directory move or replacement, so an interrupted pending write cannot appear as the live save. The on-disk child schema and legacy generation-on-load behavior are preserved. The roster save remains two separate operations. The world tick, world state and wetware parameters still require a versioned checkpoint, transaction and explicit restore checks. No new runtime package was added. [Current interfaces](SYSTEM_INTERFACES.md)

### Observed results

| Check | Result |
| --- | --- |
| .NET Release tests | 42 passed |
| Unity 6000.3.19f1 Edit Mode | 43 passed in an isolated copy |
| Unity DX12 Play Mode with valid saved learner | Loaded Arthur's legacy `son` identity at a saved age of 2.5 simulation days, invoked the Unity save action, checked the saved identity and age, and returned to Edit Mode. |
| Unity DX12 Play Mode with damaged saved learner | Observed startup suspension and diagnostic. Verified the damaged bytes survived and the runtime returned to Edit Mode. |
| Latch pilot | Both probes passed 32/32 in each of four paired conditions. Noise visits were 143 each with curiosity off and 145 each with curiosity on, regardless of replay. The ceiling precludes a retention or transfer benefit claim. |
| Family benchmark | Median 48,682 → 48,433 ticks/s (−0.5%) against `635c55f`. |
| 12-person benchmark | Median 29,981 → 30,647 ticks/s (+2.2%). |

Benchmark conditions were nine alternating launches per build and population on the same i7-14700HX host with 15.71 GiB RAM and an RTX 4070 Laptop GPU (8,188 MiB VRAM). D: had about 304 GiB free. Each measurement used the Laptop preset, a 16×16 grid, 30 warm-up ticks and 2,000 measured ticks. The change adds no work to the tick loop. These numbers measure warm headless throughput. Earlier runs with lower contention were faster. Save latency, workload scaling beyond 12, sustained Unity frame times and Android filesystem behavior remain unmeasured.

The isolated rig smoke on the predecessor verified four live rigs, stable elbow anchors and anatomy-tracked reach from 0.150 m to 0.697 m. The bodies are kinematic and their collider envelopes are disabled. The Desktop Play Mode checks for this slice exercised app state and the startup diagnostic programmatically. Visual appearance, interactive body controls, the new diagnostic layout and Android are still unverified for this slice. [Rig report](HUMANOID_RIG.md)

All four supplied visual sources passed structural VRM inspection. Their usage terms require verification before integrating them. The authored files remain local. [Asset and license registry](ASSET_AND_LICENSE_REGISTRY.md)

### Next verified slice

Extend the existing fixed-step kernel for one learner and a small world. Put pause and single-step commands through one authoritative simulation clock. Save tick, identity and enough world state to make a two-run comparison after reloading. Run the same command sequence with an observer open and closed, and compare the resulting snapshots. Expand the checkpoint contents only as the available state can be restored and verified. [Continuation checkpoint](CONTINUATION_CHECKPOINT.md)
