# Implementation progress

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
