# Physical Development Mode and Motor Vocabulary

This planning addendum extends [the master specification](MASTER_IMPLEMENTATION_SPECIFICATION.md) from the verified L.4 implementation at `c5cc3602fb76d4a623b1da8c230ceb3a25f3105f`. L.5's independent search for a moved toy and C.1.3's controlled return to double support remain the first implementation pair. Physical Development Mode and the Motor Vocabulary Browser follow as household integration milestones. [L.4 evidence](ACTIVE_PERCEPTION_L4.md) · [C.1.2 physical gates](LOWER_BODY_C12.md#prespecified-evaluation)

## Physical Development Mode

| Mode | Authority and purpose |
| --- | --- |
| Living | Runs the playable household with clearly labeled kinematic reference activities. A physical capability enters Living individually after it passes its task checks. |
| Research | Runs isolated, controlled trials with explicit initial conditions, interventions, and measurement access. |
| Physical Development | Lets the owner watch an existing individual exercise a physical body inside the playable household. The body uses motor actuation, gravity, joint limits, self-collisions, and tactile feedback. Supports balance loss, falls, incomplete actions, and attempted recovery. |

Use the existing identity, seeded anatomy, calibration, motor history, and authored avatar binding. Starting a household practice session binds the selected individual's current record to the physical body. The authoritative clock controls actuation and physics substeps, including pause, single-step, and restore. At a tick boundary, save the active practice state, including articulated pose and velocity, controller state, calibration, relevant world objects, and random state. Validate checkpoint compatibility before activation. Return to Living at a recorded boundary with the same individual and durable learning records. The household kernel owns goals and memory. The physical backend owns contacts and constraints while a body is active.

**First visible integration.** Carry forward C.1.3's support and recovery gates. Start with an observable supported manipulation task with Arthur or Alice after the articulated-hand fixture passes. Show motor effort, support contact, corrections, and the actual outcome. Compare an attempted movement with and without passive inspection. Verify that a paused body remains stable, one requested step advances the intended boundary, interrupted practice can resume, and old Living checkpoints still load. Predeclare numerical tolerances for native trajectories when exact matching cannot be demonstrated. [Physical interface and integration sequence](PHYSICAL_NERVOUS_SYSTEM_SPECIFICATION.md#dependencies-and-implementation-sequence)

**Developmental acceptance.** Show the owner the same individual attempting a task in the household, retaining an acquired change after restart, using it in a held-out condition, and later composing it with a compatible skill. Compare the learned policy with a frozen or engineered controller under the same conditions. Report unsuccessful attempts and what was actually acquired. An isolated fixture supplies a candidate capability for household integration. The visible household test supplies integration evidence. Track body self-contact events and visual garment penetration independently.

## Motor record and evidence

| Field group | Required content |
| --- | --- |
| Identity and calibration | Stable individual and skill IDs, attempt ID, body and controller versions, anatomy calibration, sensor calibration, and relevant environment. |
| Intent and execution | Objective, preconditions, motor command representation, timed joint trajectories, and predicted sensory consequences. |
| Physical evidence | Observed sensations, joint telemetry, regional contact and pressure measurements, physical outcome, failure information, and corrections. Mark measured and estimated quantities with their units. |
| Acquisition and reuse | Origin, mentor contributions and their source IDs, links to prior attempts and composition relationships, and separately recorded retention and transfer evaluations. |

Elementary, coordinated, functional, and expressive movements share this record format. Keep the attempt outcome separate from how the movement was acquired. An engineered reference, a witnessed demonstration, an inherited controller, a firsthand attempt, and an independently acquired skill can all link to the same task while retaining distinct sources. Criteria for competence come from measured outcomes against a declared task and assistance level. Store compact trajectory references and stable IDs for scientific evidence alongside the individual's motor history. Append corrections and later evaluations to preserve prior evidence. [Capability provenance](MASTER_IMPLEMENTATION_SPECIFICATION.md#158-knowledge-and-capability-provenance-audit) · [Checkpoint requirements](MASTER_IMPLEMENTATION_SPECIFICATION.md#57-checkpoint-structure-and-atomic-restoration)

## Babel and movement composition

Construct candidates from existing motor records, their preconditions, and their expected outcomes. Preserve the component order and intended transitions, such as maintaining support while changing a grip. Keep the candidate and each evaluation independently addressable. A candidate or prediction belongs in Possibility. A counterfactual evaluation gets a separate record linked to its isolated initial state. An actual attempt in Physical Development Mode records the measured experience in Lived Memory. Evidence-supported principles and affordances can link to Knowledge. Promotion to a demonstrated capability requires physical practice and task-specific evidence. [Possibility and lived memory](MASTER_IMPLEMENTATION_SPECIFICATION.md#47-library-ii--possibility-space-level-10) · [Babel](MASTER_IMPLEMENTATION_SPECIFICATION.md#64-library-of-babel-and-hilberts-hotel)

## Motor Vocabulary Browser

Start with a read-only Unity tray. Select an individual and skill category, then open its acquisition timeline. Pick a successful or unsuccessful attempt and play joint trajectories and pressure measurements on the same time cursor. Link source demonstrations, lived attempts, corrections, candidate compositions, and their evaluations to the three libraries. Provide retention and transfer results with task conditions, opportunities, and failed attempts visible. With composition available, choose compatible records in the browser, arrange them into a candidate, and open it for Babel evaluation. Extend that same view to side-by-side mentor and learner attempts and then to lineage comparisons once the underlying records exist.

The first browser milestone passes when the owner can inspect one real household practice record, play a failed and a successful attempt or correction from that record set, and follow source links without reading raw logs. Use a separate preview or recorded trajectory for playback and composition. An explicit request for actual practice is a recorded action. Verify passive browsing by comparing the authoritative state and event order against a control. [Body and tactile observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#128-body-observatory) · [Memory observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#131-library-and-memory-observatory)

## Iterative generational learning

Control teaching, observation of demonstrations, transmission of cultural knowledge, and motor-policy inheritance independently. Preserve individual experiential records and identify mentor input with typed source links. Evaluate policy inheritance in explicitly configured research conditions with calibration for the receiving body. Match starting anatomy distributions, practice opportunities, and computation across comparisons. Compare each transmission mechanism with a cohort that has no task-specific transmission. Report attempts to criterion, delayed retention, held-out transfer, and performance on new compositions across generations. Preserve null effects and declines. Follow the master's matched lineage and isolation requirements. [Generational learning](MASTER_IMPLEMENTATION_SPECIFICATION.md#56-generational-learning-and-inheritance) · [Generational acceptance](MASTER_IMPLEMENTATION_SPECIFICATION.md#175-milestone-seven--generational-development)

## Expression and regulation

Start with interruptible seated rocking on a supported body. Capture the modeled internal condition, intended regulation, motor commands, sensory feedback, and measured change or interruption. The movement uses the same controller interfaces, limits, and evidence format as other motor skills. Progress to finger tapping, leg bouncing, hand gestures, pacing, and stomping as the relevant support, hand, and locomotion capabilities become available. Visible behavior and its measured simulated effects support claims about expression and regulation. Subjective emotion is a separate research question. [Regulation requirements](MASTER_IMPLEMENTATION_SPECIFICATION.md#92-habit-formation-stimming-and-regulation)
