# Phase L.4 · Active socket inspection and developmental visuals

## Prior result and fixed study

The L.3 learner retrieved a successful numeric slot and its past episode. The policy received no observation of the current socket layout. It applied that slot without checking for change. The rearranged evaluator kept the concealed mapping in a separate path, and the exploration baseline sampled the same seeded sequence. Every probe had one action, no feedback, and the same 2,080-tick delay. The archive recorded cue and details of prior placement, which could describe the past without predicting a different board. Retrieval changed from 81/96 on the old layout to 27/96 on the concealed rearrangement. Of the 69 rearranged failures, 58 used an actual previous success and 11 had no retrieved episode. Exploration scored 34/96 and 38/96. [L.3 trial evidence](evidence/phase-l3-delayed-retention.csv)

Use seeds 1401–1408 for development and 1409–1424 for held-out scoring. Each seed reuses the L.3 12-acquisition-trial schedule, a shared world that advances 2,080 ticks after acquisition, and six one-action probes per layout. No probe writes back into the training archive. All policies share the toy piece descriptor, visible orientation mark, sampled exploration and up to three close looks per probe. The sensor exposes quantized aspect and edge readings with bounded noise and one occluded edge on a changed board. The evaluator exclusively holds the placement mapping. On some held-out rearrangements the visible mark is compatible with the remembered board. Charge five ticks per close look and eighteen ticks per placement. The research harness reports scheduled observation and action ticks. The production household actually waits before placement.

Compare exploration, direct episodic slot retrieval, direct retrieval with inspection, contextual retrieval that can skip inspection, and a unified scoring baseline. The unified policy uses the same retained slot, mark and geometric observations and checks the near evidence even when the mark agrees. Match exposure and action budgets. Count actual correct and wrong attempts, retrieval calls, close looks, uncertainty categories, contradictions, repeated numeric errors and simulated timing. Each condition gets the same scenario and exposure. The five policy comparison does not establish an independent test of full three-library architecture.

## Held-out results

Each cell reports successes out of 96 attempts and mean close looks. All policies used one placement per trial. Direct, Inspect, Contextual and Unified retrieved once per trial. Exploration never retrieved.

| Policy | Familiar | Rearranged | Mean placement ticks familiar / rearranged | Repeated errors familiar / rearranged |
| --- | --- | --- | ---: | ---: |
| Exploration | 36, 0.00 looks | 35, 0.00 looks | 18.00 / 18.00 | 52 / 26 |
| Direct retrieval | 90, 0.00 looks | 14, 0.00 looks | 18.00 / 18.00 | 6 / 3 |
| Direct plus inspection | 96, 1.11 looks | 93, 2.43 looks | 23.57 / 30.14 | 0 / 1 |
| Contextual retrieval | 96, 0.22 looks | 74, 1.88 looks | 19.09 / 27.38 | 0 / 1 |
| Unified scoring | 96, 1.11 looks | 93, 2.43 looks | 23.57 / 30.14 | 0 / 1 |

On changed boards, contextual mean uncertainty moved from 2.54 to 1.07 on the heuristic three-level scale. The orientation cue missed some rearrangements. Contextual flagged 64 of 96 changes and the always-inspecting policies flagged 81. The faster contextual policy lost 19 successes to each of the inspection-based alternatives on unfamiliar boards. It saved time on familiar boards. Inspect and Unified tied on success and observation cost. The unified scorer also has access to remembered placements, with a small extra score for a context-supported candidate. No architecture advantage was observed. The fixture encodes geometric cues by design and uses the same learned toy category throughout. Full library integration, teacher queries, recovery from feedback over sequential probes, a second task family and perceptual generalization are still open. The 1,440-row trace reproduced byte-for-byte. [Probe trace](evidence/phase-l4-active-socket.csv) · [Research references](EXTERNAL_RESEARCH_RESOURCES.md#active-inspection-and-context)

## Household behavior and persistence

Learners approach and face the board, reserve it while scanning, and compare at most three abstract observations before acting. The close-look port gives shape proportions and edges, without giving the policy a slot label or an arrangement transform. Choice and historical provenance are recorded separately from physical outcome. A prior failed placement can discourage reuse in the same context. The Life & experience tray shows the last decision, its historical source, sampled geometry and uncertainty. The rendered orientation mark and slot positions follow the board state. Actual movement, capacity, learning and outcome remain in the shared kernel.

The 4,700-tick native run produced 282 records, including three socket decisions, 40 demonstrations, 15 witness records, seven imitations, and 16 replay references. Two decisions took close looks and one used an applicable memory. Exact observer/control comparison and checkpoint restoration passed. Saving mid-decision preserved the exact plan and subsequent placement in .NET. Version 4 continues to read versions 1–3. A real version 3 checkpoint generated by the L.3 binary preserved 277 records and matched a version 4 round trip over 500 additional ticks. The two prior retention CSVs remain unchanged. [Shareable household trace](evidence/phase-l4-household.csv)

## Developmental avatar and interaction verification

The same infant and toddler procedural bodies now wear an original sleeper or overalls from the existing color palette, on their present seeded proportions. Their head, hair and visual identity continue with the same rig. No adult export was rescaled for early development. The mature VRMs and Research representations remain available at their previous transitions. The initial toddler smoke fixture at age 0.70 remained at infant proportions and was rejected. A fixture at age 0.84 attained anatomy age 0.469 and verified both toddlers wearing overalls. Measured heights were 0.527 m and 0.538 m at birth and 0.653 m and 0.670 m in the toddler sample, for Arthur and Alice respectively.

The private adult before and after capture shows the new lid handle, bent knees and a hand aimed at the prop. A two-bone arm solve uses the actual palm landmark when available and follows the handle's moving transform. Selected shape sockets and other supported props use their rendered contact points. The minimum Merlin palm-to-handle distance across sampled lid actions changed from 499.5 mm to 38.8 mm. This is a minimum across two seed-matched runs with different activity timelines and sample counts. The capture still shows occasional actor occlusion and reachable contact does not guarantee a grasp. Adult bed fronts moved 0.57 m out of the walk lane. Sleep now turns toward the pillow side. In the mature test Merlin's head stayed over the bed side in all 247 sampled reclined ticks. The visual changes preserved byte-identical observed and control checkpoints. Exact attachment, fine finger curl, seated alignment, prone garment clearance and foot-slide bounds remain open.

Private images in the ignored avatar folder include `household-private-lid-l4-before.png`, `household-private-lid-l4-after.png`, `household-private-infant-detail-l4-after.png`, `household-private-toddler-detail-l4-after.png`, and `household-l4-inspection.png`. Release .NET passed 83 tests, Unity Edit Mode passed 84 tests, and the mature, infant, toddler and four-day DX12 runs passed.

## Reproduction and next slice

```powershell
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --active-socket <scratch-active-socket.csv>
dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo
```

For the next learning slice, verify a cheap confirming observation when an orientation mark agrees and the cost of a wrong action is high. Use a held-out secondary task with misplaced containers and track a separately stored hypothesis, physical outcome and source. Then evaluate full library separation against the unified scorer with identical observations. For the visual slice, use focused captures to fit independent, licensed or original child and adolescent mesh variants and establish seat and hand-contact error bounds. Track the physical-step fixture as separate work. [Budgets and native timing](PERFORMANCE_BENCHMARKS.md#phase-l4-active-inspection)
