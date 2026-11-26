# Experimental protocols

## First integrated room experiment

Use one developing child, one scripted teacher, and a room containing a low-force two-position latch plus a similarly accessible object with stochastic feedback. The teacher demonstrates one safe latch movement in view once under a fixed schedule. Hidden latch state, evaluator seeds and future outcomes stay inside the environment. Both learners get the same available actions, sensing budget, teacher opportunity, rest schedule and day/night boundary. Only observed motion and sensory consequences enter a learner's observations.

1. Perceive a familiarizing room frame and discover an unfamiliar control. The learner can ask whether press or slide changes the latch and select a safe approach or investigation.
2. Observe the scripted teacher's demonstration, then independently approach and act using bounded position or joint commands. Record proprioception/touch plus a visible or otherwise sensed effect.
3. Append observation and evidence with stable object/action IDs and ticks. Update an action-conditioned prediction from the result. Log confidence, pre-update loss, activity and decision reasons. The sandbox should permit the learner to select or ignore the teacher's example based on its available observations.
4. At night reconstruct a selected observed sequence and create one testable alternative linked to the waking trial. Record the dream separately from waking evidence. The next waking cycle gives a matched probe opportunity with early assistance withdrawn.
5. Show the actual trace in Unity and write an independent scientific event log. Run the same fixture headless and with the HUD attached. Replay tick batches of several widths to verify event order and final state.

The first room can use a constrained deterministic actuator in the shared kernel. Unity renders its positions and action effects. This demonstrates closed-loop interaction and measured adaptation in a bounded mechanism. Full free-body physics and physical hand guidance have their own acceptance tests.

| Measure | Operational definition |
| --- | --- |
| Autonomous investigation | A learner-generated question and object-directed action before any object-specific instruction, attributable to its own observation and prediction. |
| Learning | Change in prequential prediction loss and successful independent actuation, reported per seed and object. |
| Noise control | Allocation of discretionary actions to the stochastic object after familiarization compared with the learnable object and with the prediction-error policy. |
| Retention and transfer | Unassisted next-day success and prediction loss on the same latch, followed by a held-out placement or orientation. |
| Reflection | A waking test references a dream alternative and the underlying observation. Compare assisted practice before sleep with independent probes after sleep across conditions. |
| Reproducibility and performance | Same-seed traces including tick, actor, action, evidence and provenance, plus throughput and allocations with telemetry disabled/enabled. |

Start with a paired 2×2 comparison (curiosity off/on and reflective replay off/on). The full condition and the condition with both off address the milestone, and the other two help attribute effects. Equalize action opportunities and compute budgets. Introduce instruction-only, random, novelty, prediction-error and progress policies in the same harness as data allow. Pre-register trial count, seeds, effect threshold and exclusions from pilot variance, then use held-out seeds for evaluation. Failure includes no attributable autonomous episode, comparable or worse unassisted probe performance, persistent attraction to unsolvable feedback, or renderer-dependent scientific outcomes. Record negative results and uncertainty.

## Latch-room pilot

`LatchRoomSimulation` implements a bounded fixture in the shared C# assembly. The child observes two controls, then moves one position per 0.05-second tick until contact. Pressing or sliding the latch emits a feedback signal according to a per-seed action rule. The noise panel emits a seeded signal independent of the child's action. Each run gets nine daytime investigations, one scripted demonstration of pressing at tick 40, an overnight interval, and two unassisted waking probes. The second probe moves the latch. The teacher demonstrates the same action regardless of the hidden rule, so the effect can be present or absent.

With curiosity enabled, the child asks an action–object question before investigating and scores choices using a diminishing novelty term, observed outcome consistency and recent reduction in prediction loss. The baseline explores randomly. Both observe the same teacher and update a probability for each action–object pair from feedback. Observed teacher feedback updates prediction without adding a child encounter to lived memory. When replay is on, the child reconstructs its most recent failed latch action from lived memory and proposes the alternative movement. The first waking probe tests that proposal if available. The later moved-latch test uses predictions updated by the first waking probe.

```sh
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --room --seeds 32 --out ExperimentOutput/room
```

- `room_events.csv` is the observer trace. Each event has schema version, seed, condition, tick, kind, and event ID. Parent IDs refer to earlier records in the same run and connect questions, physical contact, feedback, memory, replay and probes. The action records capture predictions before feedback. The trace reports the current one-dimensional fixture positions when relevant.
- `room_summary.csv` has one row per seed and condition. Daytime Brier loss averages squared prediction error on each of the child's nine daytime attempts. Next-day and transfer success score observed effects on the first and second waking probes respectively.
- **Dream Machine → Latch Room Trace** runs the same kernel in Unity Edit Mode. Select a seed and condition, run, then scrub the event index or follow a source link. The window reads actual room events and works independently of the family scene.

### Pilot results

The same 32 seeds ran under each condition. Both replay conditions generated a hypothesis in 32/32 runs. Each condition achieved 32/32 next-day and 32/32 moved-latch successes. With curiosity disabled the child used 143 of 288 daytime attempts on the noise panel and averaged 0.191 Brier loss. With curiosity enabled it used 145 of 288 and averaged 0.196. The task reached a ceiling on both probes, and the curiosity policy did not suppress noise-panel sampling in this pilot.

These are development seeds and descriptive results. The moved-latch test keeps the same action rule. The pilot has one-dimensional motion and a local append-only encounter ledger. Full embodiment, a persistent Library III episode schema, and the 3D Neural Observatory remain later increments. Before evaluating benefits, use a harder held-out task with several action rules, record learning curves, and pre-register effect and noise-allocation thresholds.

## Broader experiment backlog

| Experiment | Controls and measured outcomes |
| --- | --- |
| A. Biological connectivity | Random, degree-matched, designed, imported and imported-plus-extensions graphs. Match neurons, edge budget, integration, seed protocol and compute. Measure dynamics and closed-loop behavior separately. |
| B. Curiosity | Instruction-only, random, novelty, prediction error, learning progress and integrated policies. Measure prediction, transfer, safety and action allocation. |
| C. Dreaming | No replay, ordinary replay and constructive counterfactual replay. Attribute outcomes to retention and generalization probes. Later measure responsiveness to another agent's needs. |
| D. Bridge | Full, restricted, delayed and disconnected. Log delivery, conflict, recovery, latency and task effects, using matched budgets. |
| E. Mentorship | Independent, teacher-guided and teacher-plus-mentor groups. Start with three pairs on benign tasks. Withdraw help at evaluation to measure dependence and retained judgment. |
| F. Individuality | Same and distinct seeds crossed with controlled environmental histories. Measure persistent interests and behavioral trajectories under repeated observations. |
| G. Phronesis | Hold out scenarios with incomplete information, competing needs, consequences and conflicting constraints. Use blinded independent raters, outcome metrics and explicit failure criteria. |

For developmental assessments, start with unassisted object manipulation, retention, transfer and question generation. Then add object permanence, joint attention, pointing, imitation, causal inference, independent motor control, collaborative composition and contextual judgment. Track teacher assistance and prior information by condition. The audit baseline is the [belief-shift and timing suite](PERFORMANCE_BENCHMARKS.md), a different experiment from the room.
