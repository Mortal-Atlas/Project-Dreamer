# Implementation progress

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
