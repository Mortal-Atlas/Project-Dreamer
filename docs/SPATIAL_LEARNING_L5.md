# L.5 moved-toy search and contextual confirmation

## Household implementation

Arthur physically approaches three lidded props using the existing fixed-tick kinematic household controller. He first opens a chest and observes the red boat. The chest closes after that encounter. An authored household fixture subsequently relocates the same boat between closed containers while Arthur can do other activities. The live planner can read an earlier sighting, box coordinates, visible labels and results of reached inspections. The destination and the contents of unopened boxes are world state.

The first candidate is the remembered box. An empty box or striped lookalike supplies a local negative observation and clears the current expectation. The historical sighting remains available. After a contradiction, a labeled unvisited box is preferred, then the nearest remaining box. No check is repeated. A blocked path consumes a check and records an interruption. Sleep suspends an active candidate without spending another check. The trial terminates on a positively identified match or exhaustion. The unavailable condition keeps the boat outside the accessible search area. Exhaustion leaves location unresolved.

- Library I projects a supported containment relationship from a sighting or rediscovery, including its observation tick and immutable reference.
- Library II records the candidate mask, selected target and the rule that selected it. Inspection can resolve current contents if the learner reaches the target.
- Library III stores the observation, absence or lookalike, and rediscovery, linked to the associated candidate and its source. A candidate confers no sighting or competence. Stable identifiers distinguish the red boat and its striped lookalike.

Life & experience displays supported location, expectation and confidence tag, observed location, selection, alternatives, negatives and result. Version 5 stores the active candidate and source references, attempted and blocked sets, pacing, remaining budget, box revisions and contents with the complete world checkpoint. Versions 1–4 remain readable; legacy household revisions initialize the new fixture prospectively. No new random stream is used. The founding seed and existing RNG states remain in the world checkpoint. [Kernel](../Assets/DreamMachine/Core/Sim/HouseholdLife.Search.cs) [Unity bindings](../Assets/DreamMachine/Unity/DreamMachineApp.Living.cs) [Regression tests](../Assets/DreamMachine/Tests/EditMode/SpatialSearchTests.cs)

## Held-out household study

The paired study ran 12 world seeds per condition and rotated the starting container. All trials used the same three-check budget. The rules, goal and fixture timing are engineered. The last sighting and later revisions come from actual reached inspections. Results cover this task and provide no estimate of broader search competence. [Per-trial evidence](evidence/phase-l5-spatial-study.csv)

| Condition | Result | Mean checks | Mean ticks from sighting to intent | Mean intervening activities |
| --- | --- | ---: | ---: | ---: |
| Familiar | 12/12 found | 1.00 | 761 | 9.00 |
| Unseen relocation | 12/12 found | 2.67 | 761 | 9.00 |
| Misleading visible label | 12/12 found | 3.00 | 761 | 9.00 |
| Delayed retrieval | 12/12 found | 2.67 | 1,784 | 20.67 |
| Similar-looking toy | 12/12 found | 3.00 | 761 | 9.00 |
| Unavailable | 12/12 exhausted with unresolved location | 3.00 | 761 | 9.00 |

The fresh-process check saved a 23,335-byte checkpoint at tick 1,497, with the old box checked and the next target on approach. Loading took 32.7 ms cold in one process measurement. At tick 3,600 the 26,963-byte checkpoint and 229 household records matched the uninterrupted process byte for byte. The result was rediscovery.

## Separate socket confirmation study

The legacy L.4 CSV reproduced byte-for-byte with SHA-256 `4700681E12416430BA3D19859FC7292774C68C95FC7D922CFF4E104A087AC8F9`. The new study gave all five policies the same remembered slot and detailed measurements. Each of 24 held-out configurations per layout offered three shapes and two attempts per shape. A glance reads rim aspect and coarse edge bands using the same visible evidence as the detailed measurement. Ambiguity forces detailed inspection. The model charges one tick for a glance and five for a detailed inspection. These are simulated costs. All policies receive the previous incorrect slot on the second attempt. Full and unified inspection share the same best-available observations. [Unchanged L.4 evidence](evidence/phase-l4-active-socket.csv) [Confirmation trial evidence](evidence/phase-l5-socket-confirmation.csv)

| Layout and policy | Correct / 144 | Incorrect | Mean detailed scans | Mean glances | Mean cost, ticks | Corrections after error |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Familiar / direct and contextual, each | 144 | 0 | 0 | 0 | 0 | 0 |
| Familiar / full and unified, each | 144 | 0 | 1.00 | 0 | 5.00 | 0 |
| Familiar / confirmed | 144 | 0 | 0.11 | 1.00 | 1.56 | 0 |
| Aliased / direct | 0 | 144 | 0 | 0 | 0 | 0 |
| Aliased / contextual | 70 | 74 | 1.33 | 0 | 6.67 | 70 |
| Aliased / full and unified, each | 137 | 7 | 2.67 | 0 | 13.33 | 7 |
| Aliased / confirmed | 137 | 7 | 2.67 | 0.97 | 14.30 | 7 |
| Changed marker / direct | 0 | 144 | 0 | 0 | 0 | 0 |
| Changed marker / other four, each | 140 | 4 | 2.67 | 0 | 13.33 | 4 |

The glance matched full inspection success on aliases and saved observation on familiar layouts. The contextual baseline was cheaper and less accurate on aliased layouts. The alias cases added roughly a tick over full inspection. Sampled decision medians were about 0.1–0.2 microseconds at 0.1 microsecond timer granularity, including the inspection callback, and are too coarse for ranking. Keep the established L.4 runtime policy until an additional experiment validates adoption.

## Native verification and cost

Release .NET passed 93 tests. Unity Edit Mode passed 94 tests.

On Unity 6000.3.19f1 with D3D12 the isolated household rendered Arthur next to the opened, empty cedar chest at completed tick 1,389 and the boat in the green trunk at tick 1,674. The tray was repeatedly opened and closed and the observer and control checkpoints matched exactly. The run rendered the original camera and close-ups. Captures remain private in the scratch product's persistent data directory as `phase-l5-empty-container*.png` and `phase-l5-rediscovery*.png`. The close-ups showed the lid, box contents and physical proximity. The search still uses kinematic reach and floor-level references. Grounded dialogue, hand-object grip, additional developmental meshes and learned generalization beyond this fixture remain open. [Native harness](../tools/unity/HouseholdLivingSmoke.cs)

The four-person Release Laptop benchmark's seven-run median was 51,099 ticks/s against 52,723 ticks/s for the cached L.4 executable. The 2,000-tick samples include process cold-start effects. The twelve-person kernel ran 30,968 ticks/s without the household fixture. At benchmark end L.5 stored 133 total experience records and 27,573 checkpoint bytes, compared with 129 and 26,984 in L.4. One L.5 run measured 390,120 managed bytes allocated on the sim thread over the 2,000 ticks. Unity overlay refresh averaged 0.0205–0.0206 ms at the two captured states. Camera CPU submission with the tray open averaged 1.13–1.32 ms, and the different close-up viewport without the tray ran 0.75–0.87 ms. The camera comparison changes the angle as well as the tray and therefore cannot isolate graphics overhead. GPU, Android and isolated search/retrieval costs remain unmeasured.

## Reproduction

Build the Release CLI and run `--spatial-study <csv>`, `--socket-confirmation <csv>` and the existing `--active-socket <csv>`. Run `--spatial-checkpoint save <path>` and `--spatial-checkpoint resume <path>` in separate processes. To verify the actual household use the guarded DX12 scratch project with `-executeMethod HouseholdLivingSmoke.RunSpatial`. The owner project and its saves are excluded from the smoke test.
