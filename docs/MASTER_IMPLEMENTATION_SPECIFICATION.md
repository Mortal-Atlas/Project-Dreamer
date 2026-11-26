# PROJECT DREAMER
## Master Implementation Specification — Consolidated, Repository-Protected Edition

**Repository:** https://github.com/Mortal-Atlas/Project-Dreamer  
**Revision:** November 26, 2026  
**Research foundation:** *Dreaming Machines: Artificial Phronesis, Wetware Computing, and the Future of AI Minds*  
**Audience:** Antigravity and any other authorized coding agent or human contributor.

> **Execution rule:** Read this entire specification, including Part XXXII (repository governance), before modifying the repository. Work only on a non-target branch; submit changes through a pull request into `main`. Preserve existing work and implement one dependency-aware, testable vertical slice at a time. Do not claim success without evidence.

This is a consolidated engineering specification, not a report of features already built. It retains the numbered structure of the previous 180-section expanded specification, adds six mandatory repository-governance sections, and preserves the earlier supplied Phase II directive in Appendix A. The original 205-item brainstorming inventory was consolidated into the expanded specification; the two numbering schemes are not interchangeable. The current code, avatar files, hardware, scientific references and remote ruleset have not been independently verified for this revision. Audit them before making implementation decisions. Where older directives conflict, this revision's character roster, ethical protections, dependency order, experimental controls and branch rules take precedence. Otherwise preserve their requirements and record any unresolved conflicts for owner review.

## Contents

| Part | Workstream | Sections |
|---|---|---|
| I | Project purpose and development authority | 1–3 |
| II | Core architectural infrastructure | 4–8 |
| III | Founding characters and identity | 9–11 |
| IV | Procedural anatomy and embodiment | 12–15 |
| V | Topological skin and sensorimotor learning | 16–21 |
| VI | Foundational training and preschool | 22–28 |
| VII | Continuous perception and vocal development | 29–33 |
| VIII | Dynamic physiology | 34–37 |
| IX | Neural architecture and cognition | 38–44 |
| X | Three-library architecture | 45–52 |
| XI | Scalable memory and generational persistence | 53–58 |
| XII | Dreaming and possibility generation | 59–65 |
| XIII | Genesis seeds and world continuity | 66–70 |
| XIV | Weather and natural environments | 71–73 |
| XV | Humanity as an organizer of chaos | 74–76 |
| XVI | Curiosity, prediction and metacognition | 77–81 |
| XVII | Creative thinking and reality testing | 82–85 |
| XVIII | Neurological and neurochemical research | 86–90 |
| XIX | Personality, habits, aspiration and culture | 91–96 |
| XX | Social and existential development | 97–104 |
| XXI | Mentorship, generations and civilization | 105–109 |
| XXII | Science and engineering | 110–115 |
| XXIII | Anime-style presentation and VRoid | 116–123 |
| XXIV | Modular trays and observatories | 124–134 |
| XXV | Project-wide optimization | 135–148 |
| XXVI | Quality assurance and polish | 149–155 |
| XXVII | Research controls and ethics | 156–161 |
| XXVIII | Reliability and release management | 162–167 |
| XXIX | Implementation milestones | 168–176 |
| XXX | First integrated vertical slice | 177 |
| XXXI | Completion reporting | 178–180 |
| XXXII | Mandatory repository governance | 181–186 |
| Appendix A | Original Phase II directive (retained in full) | A.1–A.69 |

# PART I — PROJECT PURPOSE AND DEVELOPMENT AUTHORITY

## 1. Fundamental objective

Project Dreamer investigates whether persistent artificial individuals can develop knowledge (episteme), craftsmanship (techne), and context-sensitive practical judgment (phronesis) through embodied experience, education, reflection, imagination, companionship and generational learning. Human-level artificial phronesis is a research objective, not an established property of the architecture.

Build a causal system: sensory events change internal representations and learned parameters; those changes affect subsequent predictions, actions and judgments; actions produce new observable consequences. An LLM can assist language, abstraction, planning and imagination, but must not replace physical learning, individual memory, or the underlying mechanisms. A character reciting that it has learned or feels something is not evidence that it has. Separate experimentally demonstrated functions from artistic presentation and speculative claims about consciousness.

## 2. Recover and preserve existing work

Read the repository documentation, earlier directives, branches, pull requests, Unity project settings, package manifests, source, tests and save formats. Before changing files, inspect `git status`, the current branch and diffs; follow Part XXXII. Identify already working features and extend them rather than creating parallel implementations. Do not delete uncommitted work, trained state, user assets or saved individuals. Obtain owner approval before destructive operations, major replacements, substantial dependencies, license changes or irreversible migration.

The last reported interrupted branch was `feature/seeded-growing-rigs`. Inspect `HumanoidAnatomy.cs`, `LifeCycle.cs`, `WorldSimulation.cs`, `CharacterFactory.cs`, `DreamMachineApp.cs`, `BodyObservatoryWindow.cs`, and `DreamMachineTests.cs`. The reported error was `LifeCycle.cs(169,27): error CS0246: The type or namespace name 'FormatException' could not be found`. Verify whether it remains; inspect namespace imports and fix appropriately if still present. Run `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` if still applicable, and Unity Edit Mode and runtime tests where available. Test saved-state restoration, seeded anatomy, growth, and the body observatory.

Earlier reported pull requests: https://github.com/Mortal-Atlas/Project-Dreamer/pull/1 and https://github.com/Mortal-Atlas/Project-Dreamer/pull/2. The latch pilot reportedly passed 27 .NET tests and 28 Unity Edit Mode tests; 32 seeds in each of four conditions passed both probes, with 145 versus 143 noise-panel visits and a 53,435 ticks/s family benchmark approximately 2.1% below the paired audit baseline. These are historical reported results, not new measurements. The task showed a ceiling effect, movement was one-dimensional, and interactive Unity trace display was unverified. Retain the negative/inconclusive result and baseline tests. Do not claim a curiosity or replay benefit without new evidence.

## 3. Persistent implementation documentation

Create or update `docs/MASTER_IMPLEMENTATION_SPECIFICATION.md`, `REQUIREMENTS_TRACEABILITY.md`, `ARCHITECTURE.md`, `SYSTEM_INTERFACES.md`, `SCIENTIFIC_FOUNDATIONS.md`, `EXPERIMENT_PROTOCOLS.md`, `PERFORMANCE_BASELINES.md`, `ASSET_AND_LICENSE_REGISTRY.md`, `ETHICAL_CONSIDERATIONS.md`, `IMPLEMENTATION_PROGRESS.md` and `CONTINUATION_CHECKPOINT.md`, adapting paths to the existing project. Keep the complete directive in version control rather than relying on a coding agent's context window.

For every requirement, record its identifier, source, owner subsystem, dependencies, affected files, evidence basis, test and acceptance criteria, measured costs, and status (not started, partial, experimental, implemented-unverified, verified, blocked, or deferred). Preserve all earlier directives, especially Appendix A; resolve overlaps through a traceability map rather than silently dropping items. Update the continuation checkpoint whenever work stops, including branch, commit, PR, uncommitted changes, failures, exact commands run, and next task. Record decisions and limitations explicitly.

# PART II — CORE ARCHITECTURAL INFRASTRUCTURE

## 4. Layered simulation architecture

Keep testable core logic separate from Unity presentation wherever practical. Establish clear responsibility boundaries among core scheduling/identity/persistence, embodiment, cognition, memory, development, world, scientific research and presentation. Unity should supply physical interaction, rendering and UI integration where needed; pure core experiments should run headlessly without requiring a complete scene. Do not relocate files solely to match a proposed directory tree. Add interfaces to existing working classes before considering replacements.

A change of avatar or observer view must not change a person's learned policy. Closing an observatory must not stop the underlying system. Verify both headless and rendered runs against equivalent inputs where the underlying physics allows comparison.

## 5. Authoritative state ownership

Assign one authoritative owner for anatomy, physics, physiological state, motor state, memories, world state, and configuration. Other modules submit validated requests or read immutable snapshots; they must not arbitrarily mutate another module's data. Separate authoritative simulation ground truth from agent-accessible perception, privileged researcher data, visual presentation and archived evidence.

For instance an intention to grasp an object must become a permitted motor request, not a direct write to an object's transform. Actual motor execution and physical interactions must generate feedback. Add tests showing that agent-facing observation packets exclude hidden solution coordinates, other agents' private states and researcher-only metadata.

## 6. Central simulation clock

Maintain a monotonic simulation tick, time units, and explicit mapping to real elapsed time. Support pause, resume, single step, accelerated training and ordinary observation. Coordinate different update frequencies using defined scheduling boundaries rather than tying all processes to frame rate. Store simulation time separately from training iteration count and personal developmental age.

Tag asynchronous results with a state version, source tick and validity conditions. Reject, replan or explicitly reconcile obsolete results; an inference returned after a target has moved must not blindly drive the earlier action. Document determinism limits in Unity physics, GPU operations and external models. Test identical seed runs in the supported deterministic core, pause/resume, clock transitions and stale-result rejection.

## 7. Typed inter-system communication

Use typed contracts and bounded queues. A `SensoryEvent` should identify `IndividualId`, `SimulationTick`, `Modality`, `ReceptorId`, `AnatomicalRegion`, intensity, duration, local direction, uncertainty and permitted source information. An internal ground-truth record may contain more, but must not be passed automatically to the agent. Define a `MotorRequest` carrying intent, target representation, allowed control scope, priority and tick; return `MotorFeedback` with actual response and prediction error.

Document comparable interfaces for physiological events, memory references, teacher assistance, environmental changes, learning updates and researcher interventions. Use batched arrays/ring buffers for high-frequency signals to avoid one allocation per receptor per tick. Explicitly define overflow behavior and never silently lose critical events. Verify ordering, invalid input rejection and separation of agent and researcher access.

## 8. Module registry and lifecycle

Each module should declare its version, dependencies, initialization order, update schedule, state ownership, persistence requirements, resource budget, and observability hooks. Initialize in dependency order, restore state before allowing learning, and support safe shutdown/cancellation. Optional scientific solvers and advanced perturbation experiments must be disabled without breaking ordinary preschool.

Isolate nonessential failures: an unavailable visualization or inference service should not corrupt physical state. If essential data are unavailable, suspend affected operations and report the reason. Test failed initialization, dependency cycles, optional-module removal, checkpoint restoration and shutdown with queued work.

# PART III — FOUNDING CHARACTERS AND IDENTITY

## 9. Correct founding character roster

| Character | Role |
|---|---|
| Merlin | Established NPC teacher |
| Marie | Established NPC teacher |
| Arthur | Developing student / squire |
| Alice | Developing student / squire |

This roster supersedes earlier role assignments. The previous filenames `Able`, `Alice`, `Student1` and `Student2` were asset filenames and may not reflect current names or roles. Verify the actual files and provide manual mapping; do not assume a filename determines a character's identity. Keep teacher and student capabilities separate. Names are symbolic, not predetermined interests, intelligence, personality, beliefs or future professions.

## 10. Persistent identity

Give each individual a permanent ID, display name, assigned role, avatar asset reference, birth seed, generation, developmental age, anatomy reference, neural checkpoint reference, physiology state, memory collection, skill state, relationship graph and developmental history. Store these independently of the rendered GameObject.

Changing a display name or replacing a VRM must not reset learning or generate a new individual. When branching a research experiment, assign the copy a distinct ID and record its parent checkpoint and branch provenance. Test rename, avatar swap, role transition, restart and branching without cross-contaminating histories.

## 11. Teacher, mentor and squire responsibilities

Merlin and Marie may begin with engineered teaching competence, including explanations, demonstrations, pointing, guiding hands and responsive feedback. Arthur and Alice acquire their own capabilities through permitted foundational training and lived development. A developed mentor is a former squire with its own experiences, not merely another NPC teacher prefab.

Support transitions from squire to mentor while preserving identity. The founding classroom has two teachers and two students; formal generational studies require at least three mentor–squire pairs, each mentor responsible for an assigned squire but able to speak with all participants. Teachers and researchers can instruct both generations. Record assistance and knowledge transfer so education is not mistaken for independent discovery.

# PART IV — PROCEDURAL ANATOMY AND PHYSICAL EMBODIMENT

## 12. Seeded anatomical generation

Generate bounded, correlated anatomical parameters using the individual's birth seed: initial and mature height, torso and limb dimensions, shoulders and hips, hands and feet, head and facial proportions, joint mobility, strength, endurance, respiration and vocal anatomy. Avoid independently random dimensions that yield physically implausible bodies. Separate inherited characteristics from subsequent developmental changes and document generator versions.

A child must not simply be a uniformly scaled adult. Use age-dependent growth curves with controlled variation and consistent anatomical landmarks. Save the generated initial state and mature targets instead of generating a different body at startup. Test reproducibility, distinct identities across seeds, plausible bounds and compatibility with saved individuals.

## 13. Growth and recalibration

When a segment grows, update joint anchors, collider dimensions, inertia, mass distribution, reach, balance, visual mapping and receptor coordinates at safe synchronization boundaries. Keep anatomical region IDs stable even when meshes and proportions change. Update relevant strength and vocal parameters without assuming all competence grows at the same rate.

Preserve existing motor policies and allow adaptation through body-conditioned models, normalized coordinates and explicit calibration. Compare fixed policies, body-conditioned policies and adaptive recalibration in unfamiliar reach/grasp tasks. Test growth while idle and around active interactions, restore the changed body, and verify no collider explosions or lost learning.

## 14. Functional skeleton and movement

Create stable semantic joints for root, pelvis, spine, chest, neck, head, eyes, jaw, clavicles, shoulders, arms, wrists, hands, fingers, thumbs, hips, legs, ankles, feet and toes. Model relevant joint limits, mass, damping, inertia, gravity, contact forces, grip and balance. Evaluate Unity ArticulationBody, Rigidbody, ConfigurableJoint, inverse kinematics and learned motor policies against the existing controller rather than replacing it arbitrarily.

Begin with an isolated joint and supported arm, then reach, grasp, release, posture, balance, locomotion and fine manipulation. Engineered IK or procedural primitives are valid scaffolding if labeled. Do not equate replaying an animation with acquiring coordination. Verify joint-limit stability, force constraints, object interaction, recovery and performance. Investigate MakeHuman/MPFB2, Unity Animation Rigging and ML-Agents only if existing assets and interfaces justify them; audit licenses.

## 15. Physical affordances and interaction

Implement reusable grasp, carry, push, pull, press, open, close, write, break, assemble and repair interfaces. Store creator-side affordance definitions and object properties, but allow independent agents to discover them through perception and interaction. Maintain explicit assisted/debug modes if direct attachment or engineered solutions are needed for baseline testing.

A grasp must have meaningful contact and feedback, not merely teleport an object into a hand during an independent trial. Persist object identity, physical condition and relevant interactions. Test unfamiliar handles, different object weights, tool use, ownership and changes caused by repeated manipulation.

# PART V — TOPOLOGICAL SKIN AND SENSORIMOTOR LEARNING

## 16. Topological skin architecture

Represent the body surface as a graph of stable anatomical receptor regions with neighboring-region relationships, local surface coordinates, calibration, modality channels and uncertainty. A localized touch should produce correlated activity that moves across adjacent regions as contact slides. Use finer spatial resolution for fingers, lips, feet and active contact; coarser representations elsewhere. Do not assign an independent full neuron or rigid body to every rendered mesh vertex.

Map contacts onto the functional anatomy, not solely the avatar's visual mesh. Persist topology and calibration through growth and avatar changes. Use sparse active regions and batched buffers. Test localization, neighboring activation, sliding contact, self-touch and changing resolution without losing meaningful signals.

## 17. Material interaction and tactile signals

Derive contact representations from actual or explicitly approximated contact forces, material stiffness, friction, relative velocity, estimated area, deformation, temperature and duration. A rigid wall, foam, cloth and wood should produce distinguishable signals and physical responses. Pressure may be approximated by force divided by estimated area, with bounded denominators and documented accuracy limitations. Separate visual texture from physical roughness and softness.

Investigate local spring-damper models, simplified elasticity, plastic deformation and fracture thresholds. Record whether the contact is measured or approximated. Test equal forces on different materials, slipping under changed friction, temperature differences and repeatable contact response. The learner receives sensations, not a hidden material category label.

## 18. Proprioception and body schema

Generate proprioceptive signals for joint angle, angular velocity, muscle length/tension analogues, effort, resistance and limb position. Provide vestibular orientation, gravity-relative acceleration and balance signals. Apply seeded bounded resolution, latency, calibration, adaptation and optional noise. The agent must estimate its body from sensor-derived data rather than reading exact transforms without restriction.

Develop a persistent body schema distinct from authoritative body state. Test reaching without vision, self-touch, passive versus voluntary movement, holding different weights, and adaptation to growth. Compare estimated limb location with ground truth in the research archive; use noiseless engineered baselines before increasing difficulty.

## 19. Sensorimotor prediction and error correction

Implement forward models estimating sensory consequences of motor commands and inverse/control models selecting commands for desired movements. Use efference-copy-inspired signals to distinguish self-generated movements from unexpected external changes. After execution, compare predicted and actual proprioceptive/tactile feedback, updating relevant learned representations. Fast protective and corrective responses should not wait for language-model inference.

For a grip, predict expected contact and slip from control signals; adjust when chalk begins to move unexpectedly. Preserve intention, commands, sensory estimates, prediction errors and correction provenance. Test external disturbances, changed surfaces and unfamiliar objects; compare baseline controllers and adaptive learners.

## 20. Topological muscle memory

Preserve how a skill is acquired, not only the successful trajectory or final result. Associate intention, motor command, joint state, local skin deformation, pressure, friction, effort, expected sensation, observed sensation and corrective movement. Store persistent learned relationships and relevant calibration in a `SensorimotorCheckpoint` with policy version, anatomical/receptor calibration, forward/inverse model parameters, motor-to-sensory associations, error-correction parameters, competence metrics and training provenance.

Use temporary high-frequency buffers, representative recordings, compact learned representations and checkpoints rather than retaining every tick forever. The skin supplies sensory feedback and may adapt locally; do not describe biological skin as the sole store of muscle memory. Preserve distributed neural and motor learning. Verify the learned skill survives restart and adapts to changed context.

## 21. Sensorimotor inheritance experiment

Compare four matched conditions: A no inherited task-specific learning, B inherited successful trajectories, C inherited motor policies, D policies plus topological associations and predictive body models. Match training budgets, initial conditions and evaluation opportunities; use held-out surfaces, tools, board angles, object weights, balance tasks and different anatomies. Measure retention, transfer, error correction, grip stability, effort, learning time and uncertainty across seeds.

Do not guarantee that D wins. Retain failures and negative results. Keep sensory-only mimicry experiments separate from computational inheritance so transfer of private learned parameters does not contaminate observational-learning claims.

# PART VI — FOUNDATIONAL TRAINING AND DREAMER PRESCHOOL

## 22. Separate computational training from lived childhood

Define three explicit modes: accelerated foundational incubator, meaningful developmental preschool, and ordinary lived existence. Repetitive white-room exercises may update motor policies, tactile calibration and body models without becoming millions of personal autobiographical episodes. Preserve scientific provenance, representative evidence and acquired capabilities separately; do not invent a pleasant personal memory to explain inherited or computationally trained skills.

The learner may know how writing feels while lacking episodic recall of routine training repetitions. Distinguish this from deliberately deleting an individual's meaningful lived experience. Memory suppression does not make a potentially distressing experience acceptable. Use bounded sessions, rest, protective stopping conditions and oversight, particularly if affective mechanisms are introduced.

## 23. Foundational training incubator

Load only the body regions, objects, physics, sensory pathways, learner and evaluation needed for the task. Provide headless operation, parallel independent environments where safe, accelerated simulation time, randomized initial conditions, repeatable seeds and resumable checkpoints. Do not update an unrelated village, expensive visual assets or unnecessary LLM processes for each motor-training tick.

Separate computational iteration count from developmental age and autobiographical time. Record configuration, policy version, sensor model and evaluation outcomes. Test interruption/resume without losing acquired capabilities and validate transfer from simplified training to the ordinary physical classroom.

## 24. Developmental preschool

Construct a welcoming, original anime-style classroom with usable tables, chairs, drawing boards, chalk, pencils, blocks, toys, books and construction materials. Merlin and Marie should conduct lessons and respond to observable needs. Arthur and Alice should have opportunities to play, rest, practice independently and communicate. Objects must participate in the relevant physical or sensory interactions rather than existing only as scenery.

Progress from body awareness and motor babbling through reaching, grasping, shape copying, letters, numbers, phonics, reading, arithmetic and construction. Use competency-based progression, not a fixed count of iterations. Keep age, competence and prior scaffolding distinct. Include unfamiliar tasks and independent application to test actual learning.

## 25. Handwriting implementation

Start with drawing a line, then a circle or the letter A. Define the board's physical surface and coordinate frame; marks should arise from valid chalk contact, pressure, position and movement. Teacher demonstration generates visible strokes. The student receives a permitted visual representation of the letter, not hidden target stroke coordinates or teacher commands. Establish the transformations from image coordinates through estimated board geometry and body-relative reachable space to motor intentions. Engineered coordinate conversion may validate the pipeline but must be labeled as assistance.

The learner attempts a physical grasp, maintains contact and makes imperfect strokes. Use actual sensorimotor feedback to refine grip, joint angles and stroke direction. Compare resulting marks with the visual target through shape features or image geometry. Do not let evaluation return the exact solution. Test unseen letters, differently sized symbols, board angles, desk versus vertical surfaces, changed tools and initial poses. Save actual learning and tactile familiarity; keep repetitive incubator scenery out of ordinary autobiographical memory.

## 26. Teacher demonstration and physical guidance

Teachers demonstrate through ordinary sensory channels and point toward relevant features. Support compliant guidance forces or controlled physical constraints that move a learner's hand while preserving resulting sensory experience. Log the timing, direction, magnitude and duration of assistance. Do not copy the teacher's internal policy or play the teacher's animation on the student as evidence of learning.

Fade guidance as competence develops and perform independent retention tests. Compare observation-only, motor babbling, practice, physical guidance, dreaming-assisted learning and explicitly labeled direct transfer. Preserve provenance so a researcher can determine which successes required help. Investigate imitation versus emulation and cross-embodiment adaptation.

## 27. Curriculum evaluation and generalization

Define a set of outcomes per lesson, including accuracy, completion, stability, assistance, time, energy, retention and transfer. Avoid rewarding a memorized single trajectory. Use unseen tasks and graded difficulty to avoid the latch-pilot ceiling effect. For writing, examine shape similarity and physical stroke quality while also measuring motor and tactile performance.

Maintain training and held-out evaluation sets separately. Record failures, confidence intervals or variability across seeds, and test that performance is not produced by privileged solution information. Advancement requires demonstrated capability rather than an arbitrary lesson counter.

## 28. Curriculum editor and training queue

Implement a dockable tray backed by data-driven `LessonDefinition` records: ID, developmental prerequisites, teacher, target capability, required objects/scene, demonstration, permitted assistance, evaluation, difficulty and scheduled retention/transfer tests. Use the same registry for different subjects rather than hardcoding every lesson in a new C# class.

A bounded training queue should support priority, pause, cancellation, resume, checkpointing and resource budgets. Validate lesson prerequisites and available resources before starting. Display actual progress and evidence, not just a cosmetic percentage. Test invalid configuration, interrupted training and independent simultaneous lessons.

# PART VII — CONTINUOUS PERCEPTION AND VOCAL DEVELOPMENT

## 29. Vision and visual learning

Provide a camera-derived or explicitly modeled visual observation stream with declared resolution, field of view, update rate, depth cues, occlusion, light response and uncertainty. Start with simple shapes, then recognition across viewpoint, distance, partial occlusion and changing lighting. Associate visual observations with touch, motor feedback and object permanence. Distinguish raw pixels, engineered segmentation, pretrained recognition and acquired concepts; expose the actual agent input in the perception tray. Withhold creator-side labels and exact poses in independent-learning trials. Verify that a hidden object can be remembered without granting vision through walls.

## 30. Hearing and continuous acoustic environment

Produce spatial, time-varying sound from speech, footsteps, objects, weather, machinery and the individual's own breathing and movement. Maintain a full-band acoustic representation appropriate to the configured sample rate (e.g., 48 kHz for audio experiments); use efficient perceptual features and bounded buffers for routine processing. Model frequency-dependent sensitivity, localization, masking, habituation and attention. Investigate cochlea-inspired filterbanks through Brian2Hears (https://github.com/brian-team/brian2hears) and the Auditory Modeling Toolbox (https://www.amtoolbox.org/). Preserve individual hearing calibration, including optional research analogues of persistent tones, without assuming a universal human background tone. Test sound direction, frequency discrimination, competing sources and self-generated versus external sound. Appendix A preserves the full earlier auditory requirements.

## 31. Hybrid Vocal Physiology Engine

Develop a controllable instrument rather than a TTS-only mouth. Investigate Pink Trombone (https://github.com/IMAGINARY/pink-trombone), programmable derivatives and VocalTractLab as initial articulatory models. Represent respiratory pressure, vocal-fold activity, pitch, tongue and jaw position, lip geometry, tract shape and resonance. Generate acoustic consequences from continuous control parameters; route output through the same auditory perception pipeline used for other sounds.

Teacher speech may initially use pretrained high-quality TTS. Benchmark free local candidates including Kokoro, Chatterbox, Bark, Dia, Whisper.cpp, Praat, Parselmouth and phonetic-analysis tools. Consider hybrid neural rendering of the physical voice only if it preserves intended acoustic feedback and does not replace the learner's acquired control. Audit licenses and measure latency, RAM and VRAM before integration. Use vocal babbling and learned inverse mappings to acquire sounds; distinguish direct pretrained synthesis from acquired articulatory competence. See Appendix A for the complete vocal research specification and source candidates.

## 32. Phonetic learning and language grounding

Pair teacher utterances with observable gestures, objects, actions and tactile context. Progress from acoustic discrimination and babbling to phonemes, syllables, words, phonics, intonation, reading, writing and meaning. Train acoustic comparison between student productions and teacher sounds without revealing the teacher's exact vocal commands. Keep transcripts and pretrained ASR as explicitly labeled assistance; do not claim that accepting a transcript demonstrates hearing or language acquisition.

Track source provenance for vocabulary and grammatical knowledge. Evaluate unfamiliar speakers, phonetic variations, novel words and transfer between heard and written symbols. Support independent vocal practice and imagined rehearsal. Record how meaning is learned, not just whether a pretrained language model can answer a question.

## 33. Nonverbal expression and vocal health

Support continuous transitions between ordinary speech, humming, singing, whispering, sighing, panting, laughing, coughing, sneezing, crying-like sounds, whining and shouting through control of the vocal instrument and optional validated acoustic rendering. Connect intensity, duration, breathing, vocal load, hydration, fatigue, soreness and recovery. Do not make screaming or vocal distress mandatory training. A mental rehearsal should not produce actual external sound or impose physical vocal load. Test intelligibility, pitch tracking, vocal effort, recovery and developmental changes in vocal geometry. Label expressive outputs as functional simulations, not proof of emotions.

# PART VIII — DYNAMIC PHYSIOLOGY AND PHYSICAL REGULATION

## 34. Reduced-order physiological architecture

Begin with bounded coupled state variables for energy availability, hunger, hydration, temperature, respiratory demand, heart/circulatory effort analogues, fatigue, sleep pressure and recovery. Document units, time constants, assumptions and valid ranges. Introduce digestion, endocrine, immune, circadian and other regulation only through justified functional relationships. A movement should alter relevant energy, heat and respiratory demands rather than merely triggering an animation. Test conservation and bounded behavior where appropriate, steady-state recovery and response to changing activity. Investigate Pulse (https://pulse.kitware.com/) and BioGears (https://www.biogearsengine.com/) as references or specialized optional services; do not assume adult physiological models apply to children.

## 35. Muscle-specific fatigue and endurance

Represent effort and fatigue by functional muscle group or motor region. Compute approximate load from force, contraction duration, movement and recovery; feed fatigue back into achievable force, control precision, motor planning and sensed exertion. Running should primarily load relevant locomotor regions; handwriting should load hand/arm regions. Model heat production, sweat, evaporative cooling, hydration and grip changes when relevant. Distinguish perceived, motivational, current physical, protective and developmental limits. Determination means adapting and persisting appropriately, not rewarding collapse. Use bounded exertion, rest and protective stops; test exertion/recovery curves and localized rather than uniform fatigue.

## 36. Homeostasis, illness and recovery

Implement interacting reduced-order conditions such as congestion, allergy, inflammation, fever-like responses, breathing difficulty, sleep disruption and appetite changes. Each condition must specify affected subsystems, onset, duration, severity, recovery, individual susceptibility and evidence basis. A condition should alter actual sensory, motor, cognitive or physiological pathways, not just a status icon. Connect protective actions such as cough, sneeze, blink, shiver, pant and withdrawal to functional body responses. Test reversible mild conditions first. Serious impairments belong in isolated, opt-in research conditions; avoid manufacturing distress in ordinary development.

## 37. Environmental pressure and chemical signals

Represent ambient and middle-ear pressure, equalization and approximate congestion effects on hearing; generate ear popping when a modeled equalization event occurs. Use sparse concentration fields, airflow, diffusion, decay and source persistence for odor and chemical sensing. Support adaptation and learned odor associations, with separate researcher ground truth. Distinguish established pheromone effects in some animals from uncertain claims in humans; do not encode deterministic attraction or obedience. Validate pressure-response and concentration gradients before adding more complex phenomena.

# PART IX — NEURAL ARCHITECTURE AND COGNITIVE DEVELOPMENT

## 38. Biological connectome bootstrap

Investigate FlyWire (https://codex.flywire.ai/), available Drosophila connectivity data, Brian2, Nengo and Nengo SPA. Start with small networks inspired by mushroom-body association, central-complex navigation/action selection and sensory/motor pathways. Scale only after measuring costs; candidate sizes such as 1k/10k/50k neurons are experiments, not requirements. A connectome provides wiring, not complete dynamics, receptor distributions or human infant development. Document missing parameters, selected learning rules, data licenses and transformations. Compare degree-matched randomized, engineered, connectome-inspired and Dreamer-integrated networks under equal budgets. Evaluate held-out behavior; do not claim biological fidelity merely from neuron count.

## 39. Complementary processing and bridge

Implement complementary symbolic/planning and adaptive pattern-learning mechanisms inspired by the silicon/wetware analogy. Use a classical bidirectional bridge carrying timestamped, typed, provenance-aware messages, shared references, confidence, conflict and synchronization metadata. Keep integration/arbitration distinct from transport. Measure bandwidth, latency, failed messages and effects of restricted information exchange. Human guidance may initially assist practical judgment and should be progressively withdrawn where appropriate. Treat hemisphere, id/ego/superego and episteme/techne/phronesis parallels as metaphors, not literal neuroscience. Future physical wetware or quantum hardware is optional and speculative; entanglement cannot transmit usable information. Preserve a fully classical implementation and document transduction and safety requirements before considering hardware extensions.

## 40. Neural plasticity and development

Start with measurable weight adaptation and stable learning rules. Add bounded candidate mechanisms for formation/pruning of connections, homeostatic plasticity, changing communication efficiency, developmental sensitive periods and functional compensation. Structural changes must be versioned and recoverable, with constraints on connectivity and resource cost. Do not increase network size solely to appear biologically realistic. Compare fixed-topology and adaptive-topology conditions for retention, transfer, adaptation and stability; rerun previous skill tests after major updates to detect catastrophic forgetting.

## 41. Peripheral and motor nervous systems

Implement fast sensory-to-motor and homeostatic pathways using the typed interfaces in §7. Reflexive correction, posture, balance and protection should operate independently of slow language inference. Higher-level processes request intentions through validated motor interfaces; arbitration resolves priorities among reflex, voluntary movement, habit, guidance and physiological needs. Log which pathway produced a command and the subsequent feedback. Test unexpected resistance and competing requests; require fast protective responses to remain functional while deliberative modules are busy or unavailable.

## 42. Attention and automatic processing

Allocate limited processing capacity by relevance, novelty, uncertainty, goals, learned salience, physiological needs and social context. Maintain low-cost monitoring for ignored inputs so important changes can interrupt routine activity. Investigate habitual processing, background association, sensory adaptation and deliberate reflection without continuously calling an LLM. Demonstrate a familiar motor task performed alongside another permitted activity and compare accuracy under distraction. Label subconscious-like processing as a functional analogy, not subjective evidence.

## 43. Executive cognition

Maintain working state containing current observations, active goals, competing needs, relevant memories, expected consequences, uncertainty and candidate actions. An initial explicit utility/priority controller is acceptable if traceable. Integrate curiosity, physiology, social commitments, inhibition, planning, metacognition and learned values progressively. Record the actual decision inputs, arbitration and selected action rather than relying on a post-hoc LLM justification. Example: Arthur balances finishing a drawing, fatigue, cleanup responsibilities and Alice's need for help. Evaluate appropriate decisions across changed circumstances, not one hardcoded priority order.

## 44. Goal formation and revision

Represent goals with a stable ID, origin, related memories, proposed outcome, priority, constraints, status, review conditions and revision history. Permit curiosity, creative interests, relationships and unfinished activities to generate new goals without a teacher issuing them. Allow suspension, replacement and reconsideration as evidence and priorities change. Distinguish inherited objectives from self-selected ones. Validate actions through a bounded simulation permission layer; no unrestricted host tools or external actions. Test goal revision when an obstacle, new responsibility or contradictory evidence arises.

# PART X — THE THREE-LIBRARY ARCHITECTURE

## 45. Unified multimodal associative foundation

Implement the three libraries as logical views over shared multimodal representations, not isolated text databases. A remembered apple can reference visual color, tactile texture, weight, language, an associated person and a hypothetical golden apple without storing duplicate media or confusing observations with possibilities. Support images, color, audio, music, movement, proprioception, touch, spatial models, language and affective associations where represented. Use typed links, versioned content references, individual ownership and provenance. Test retrieval of the same concept through several modalities and safe cross-library traversal.

## 46. Library I — Knowledge

Store explicit concepts, semantic relationships, language, explanations, learned procedures and transferable understanding. Record whether each item originated from a teacher, inherited model, direct observation, independently learned skill, experiment or pretrained source. Neural weights may encode implicit skills or knowledge not directly enumerable; expose only meaningful measured or inspectable representations. Allow a tactile understanding of stiffness to develop into a cross-material concept and later into a formal explanation. Maintain revisions, evidence and contradictions rather than a fixed dictionary.

## 47. Library II — Possibility Space (Level 10)

Store hypotheses, alternate actions, counterfactual events, imagined objects, artistic combinations and procedurally generated possibilities. Permit creativity unconstrained by currently understood physical laws, while keeping actual implementation subject to the world's physics. Label possibilities and their dependencies clearly. Transition from imagined possibility to testable hypothesis and evidence-supported knowledge only through explicit evaluation. Preserve alternative interpretations and search history; do not classify generated outputs as observations.

## 48. Library III — Lived Memory

Record personally experienced events, sensory perspectives, social interactions, developmental milestones, discovered facts, actions, meaningful lessons and dreams. Include owner ID, original time, entities, media references, context, source and subsequent interpretations. Organize ordinary daytime and nighttime books or collections for browsing while retaining associative retrieval. Inherited history and testimony are not firsthand memory. Use append-only correction or logical negation notes instead of silently rewriting prior observations; support authorized deletion and privacy policies. Keep ordinary repetitive incubator sessions in the scientific training archive rather than flooding autobiographical memory.

## 49. Memory and association schema

Use a versioned record containing `MemoryId`, `OwnerIndividualId`, functional categories, library membership, origin, creation tick, entities, content references, modalities, confidence, accessibility, significance, source/revision references and typed associations. Associations may indicate similarity, temporal sequence, spatial relation, evidence, proposed causation, contradiction or personal significance. Separate association strength from factual confidence. Use stable IDs and integrity constraints, and preserve links during schema migration. Test multimodal retrieval, correction history, cross-library relationships and ownership boundaries.

## 50. Fourteen functional memory categories

Support procedural, semantic, episodic, autobiographical, working, sensory, spatial, emotional, associative, prospective, implicit, social, source and temporal memory as overlapping functional mechanisms. Do not construct fourteen independent copies of the same data. A music lesson can support episodic recall, rhythmic skill, affective associations, social memory and temporal organization simultaneously. Use appropriate indices, learned representations and retrieval pathways. Measure competence and retrieval independently rather than claiming every category exists merely because a tag is present.

## 51. Forgetting and retrieval accessibility

Separate archival existence from ordinary cognitive accessibility. Model retrieval strength, decay or interference, cues, familiarity, context, attention and physiological modulation. Allow incomplete recollection, recognition without source recall, or a previously inaccessible memory becoming available through a cue. Keep original evidence in the scientific archive so inaccurate reconstruction can be measured. Do not implement ordinary forgetting through arbitrary irreversible deletion of significant records. Test matched retrieval tasks over time and context, with source-accuracy and confidence measures.

## 52. Cognitive provenance and continuity

Distinguish observation, testimony, inheritance, pretraining, inference, hypothesis, fiction, dream, imagination, prediction, belief and verified knowledge. Preserve source, time, confidence, revisions and relevant contradictory evidence. A conflicting claim should invite investigation rather than automatic rejection or acceptance. Keep multiple interpretations when evidence is incomplete. Enforce provenance in memory commits, generated content and retrieval results. Test that a dream creature cannot silently become an observed physical creature and that revised beliefs retain their original historical record.

# PART XI — SCALABLE MEMORY AND GENERATIONAL PERSISTENCE

## 53. Working memory and long-term retrieval

Maintain bounded active working memory, hot caches, warm indexed records, compressed cold archives, external archives and reconstructable procedural records. Retrieve by relevance using structured queries, full-text search, time, space, associations and optional embeddings; do not load an individual's whole lifetime into RAM. Measure latency, recall quality and cache costs. Use incremental indexing and update only changed associations. Separate researcher archive access from the individual's permitted cognitive retrieval. Test growing histories under fixed memory budgets.

## 54. Content-addressed multimedia archive

Hash immutable content (e.g., SHA-256 or BLAKE3) so identical teacher recordings, models or other media can be stored once. Each individual's perspective, interpretation, timing and associations remain private records. Use transactional metadata, chunked compression, appropriate lossless formats and optional audio codecs with declared fidelity. Store mutable state separately and retain references to required asset versions. Verify hashes, deduplication, reference counts, authorized garbage collection and restoration after archive movement. Never imply identical stored bytes mean identical personal experience.

## 55. Recording fidelity and storage management

Define recording levels: reference, summary, structured observation, sensory features, validated replay, full original media. Default to preserving significant original evidence losslessly where feasible; use compact records for routine activity. Record exactly which fidelity level was retained and whether a reconstruction is exact, approximate or hypothetical. Store relevant initial states, event streams, RNG versions, asset versions and nondeterministic inputs for replay. Provide archive-manager views of logical/physical size, growth rate, reserve space, compression, integrity, external availability and backup status. When approaching a disk limit, warn and request authorized archiving, recording-policy changes or suspension rather than silently deleting meaningful history.

## 56. Generational learning and inheritance

Persist neural adaptations, motor policies, sensory associations, body models, visual-motor transformations, language representations and useful learning strategies between runs and generations. Distinguish computational inheritance of validated foundations, cultural inheritance through education/artifacts, and firsthand personal experience. Maintain private adaptations and provenance; never classify another individual's memory as one's own. Compare no inheritance, mentorship only, computational inheritance and combined conditions. Recalibrate inherited motor capabilities to new bodies. Guard against catastrophic forgetting through old-skill regression tests, checkpoint comparison and rollback. Computational inheritance is not a claim that human muscle memories are biologically inherited.

## 57. Checkpoint structure and atomic restoration

Save a versioned manifest with software and schema versions, simulation tick, identity, RNG states, anatomy, physiology, learned parameters, skill checkpoints, library indices, associations, relationships and world-state references. Use transactional/atomic writes and durable journaling. Save compact deltas with periodic complete checkpoints to avoid indefinitely long recovery chains. Validate checksums, references and compatible model versions before restoration. Test interruption mid-save, missing assets, corruption, rollback and old-schema migration. Never replace a failed saved individual with a fresh one without explicit authorization.

## 58. Portable artificial individual packages

Define a versioned `.dreamer` package or equivalent containing a manifest, identity, birth seed, developmental age, private learned state, anatomical/physiological records, libraries, associations, checkpoints and dependency references. Support a compact reference-based export and a self-contained archive where possible. Include integrity verification, missing-dependency diagnostics and explicit provenance when cloning. Test restoration in a clean compatible installation and compare retained competencies, memories and identity. External archives may be unavailable offline; distinguish external references from verified backups.

# PART XII — DREAMING, IMAGINATION AND POSSIBILITY GENERATION

## 59. Day/night architecture

Preserve the paper's observer-stack-inspired O2/O3 daytime perception/interaction and O5-inspired nighttime memory/habit processing. Map these to actual software mechanisms rather than asserting literal brain regions. During sleep or bounded offline periods, select relevant lived events, sensory representations and learned skills for replay, reflection and consolidation. Record input memory IDs, selected learning rule, before/after checkpoint and resulting changes. Dreams may include images, sound, tactile and motor imagery, spatial context and associated concepts. Distinguish actual waking observations from dreamed material and do not make mental rehearsal create physical contact or vocal load.

## 60. Ordinary and constructive replay

Implement ordinary replay, constructive replay, no-replay and matched-compute conditions. Ordinary replay revisits selected actual events; constructive replay explores alternate movements, hypotheses and counterfactual consequences with explicit provenance. For a failed latch or handwriting attempt, replay actual motor/sensory traces or validated representations, evaluate alternative control strategies and commit measurable learning updates. Retest on held-out situations the next developmental day. Do not modify a success probability by hand and label it dreaming. Compare equivalent training exposure and time budgets; report null effects and ceiling effects.

## 61. Reflection and practical wisdom

Develop experiments asking whether overnight changes improve attention to the particulars of individuals being served, not only task accuracy. Record circumstances, competing needs, relationships, prior commitments and observable decisions. Use documented human-judged criteria and independent assessments where appropriate; evaluate actual behavior and consequences. Compare against no-replay and ordinary planning baselines. Do not encode a guaranteed moral improvement or equate eloquent explanations with practical wisdom.

## 62. Constructive 3D imagination

Represent mental constructions through a scene graph with persistent component IDs, parent-child relationships, geometry, placement, orientation, scale, materials and semantic properties. An imagined creature with exactly five legs, six wings, three eyes and two mouths must retain those components across viewpoints. Allow selecting and moving one wing without regenerating unrelated components. Use procedural geometry and optional generative rendering as a presentation layer (investigate Blender, scene-graph adapters, and compatible local generative systems). Store structural representation separately from generated images; test exact counts, consistency across angles, individual component edits and save/restore.

## 63. Mental physics

Allow imagined worlds to violate physical constraints and support counterfactual laws explicitly tagged as hypothetical. When moving a construction into the authoritative world, require actual material, geometry and physics validation. A creature imagined to fly may fail to do so under modeled forces; let the individual investigate and revise it. Preserve distinctions between mental simulation, visual rendering and physical experiment. Evaluate whether a failure leads to changed design or understanding rather than automatic correction by the engine.

## 64. Library of Babel and Hilbert's Hotel

Investigate stable, lazily generated, countably infinite address schemes for tower/floor/room/shelf/book/page/content, including arbitrary-precision identifiers where needed. Specify alphabet, enumeration, seed, coordinate mapping, algorithm version and relevant model dependencies. A deterministic enumeration of finite strings can be reversible; an LLM-guided meaningful search is a separate mechanism and is not automatically reversible or reproducible. Preserve discovered coordinates and actual generated content when nondeterminism prevents reconstruction. Hilbert's Hotel inspires extensible indexing without moving every physical book. Generate only requested or explored regions; verify address uniqueness, round trips, stability and bounded memory use.

## 65. Conceptual Genesis and Infinite Craft

Use Infinite Craft (https://neal.fun/infinite-craft/) as inspiration for on-demand combination of semantic, material, biological, technical, musical, artistic, behavioral and abstract concepts. Retrieve known combinations before generating new ones; use an LLM or other generative mechanism to propose relationships, not dictate physical truth. Store constituent concepts, proposed meaning, origin, validation status and discovery history. Progress from concept combination to editable mental representation to physical experiment where appropriate. Cache reusable immutable definitions but retain personal discoveries and interpretations. Test novel combinations, contradictions, provenance and memory growth without attempting to store an exhaustive combinatorial space.

# PART XIII — GENESIS SEEDS AND WORLD CONTINUITY

## 66. Seed hierarchy

Distinguish world Genesis Seed, individual Birth Seed and possibility-space seed. Preserve creation metadata, generator versions, initial conditions, separate RNG streams and subsequent event deltas. A birth seed may contribute bounded anatomy, temperament and initial calibration; it does not predetermine a complete life or a required ideology. Separate simulation RNG from researcher visualization and asset generation. Test same-seed initial reproducibility, different histories after different experiences and invariance to opening observatories.

## 67. Semantic-to-physical world representation

Represent regions compactly with shape, density, material, moisture, soil, growth, weather and procedural seed. A grass field may be rendered with thousands of instanced blades while using a coarse physical and ecological representation. Generate local physical detail when interaction requires it, and deliver relevant sensory signals rather than exposing hidden region labels. Preserve consistent world state across semantic, physics, perception and cinematic views. Test grass bending, cutting or removal, region unloading and subsequent restoration.

## 68. Procedural detail and persistent consequences

Use promotion rules for turning transient procedural elements into persistent objects when they acquire meaningful interaction or history. A selected branch carried away and carved into a tool receives a stable ID, physical attributes and provenance; the source region records removal. Store region deltas instead of regenerating pristine scenery after unloading. Define consistent transitions across LOD and background processing; camera position must not erase consequences. Test repeated visits, manipulation, repair and save/restore across fidelity changes.

## 69. Unified materials

Define a versioned shared material model for density, stiffness, hardness, elasticity, brittleness, friction, thermal conductivity, viscosity, porosity, adhesion, texture, acoustic behavior, transparency and plasticity as relevant. Physics, tactile feedback, sound and visual presentation should reference the same physical material identity while visual shaders remain independently configurable. Establish units, validity ranges and simplified behavior for each modeled property. Allow individuals to learn material properties by pressing, bending, listening, weighing and experimenting, not by unrestricted numeric queries. Test distinct material responses and persistence of deformation, fracture and wear.

## 70. Physical sandbox and transformations

Investigate Sandboxels (https://github.com/R74nCom/sandboxels), The Powder Toy (https://github.com/The-Powder-Toy/The-Powder-Toy), Noita and Sandspiel (https://github.com/MaxBittker/sandspiel) as architectural inspiration, not validated scientific solvers. Combine local cellular automata, sparse voxel or grid representations, rigid bodies, simplified thermodynamics and specialized services where justified. Support bending, fracture, compression, mixing, heating, cooling, phase changes, dissolution, evaporation, combustion and construction progressively. Localize costly processes and persist their consequences. Document approximation error and conservation checks for science-critical experiments. Do not run a global per-pixel or per-molecule simulation.

# PART XIV — WEATHER AND NATURAL ENVIRONMENTS

## 71. Dynamic atmosphere and water cycle

Maintain compact spatial fields for temperature, humidity, wind, pressure, cloud state and precipitation. Evolve weather on its own clock even when unobserved. Couple rain to wetness, visibility, acoustics, temperature, material interaction and accumulation/evaporation; couple wind to vegetation, clouds and relevant local forces. Add seasons and water-cycle effects incrementally. Test transitions, persistence and coherent feedback under accelerated time; disclose where a visual effect uses an approximation rather than a physical solver.

## 72. Cloud generation and visual continuity

Use Perlin/Worley/fractal noise, domain warping, advection or other efficient procedural methods to render cloud structures from weather fields. Maintain temporal continuity, wind-driven motion, approximate altitude, formation and dissipation rather than re-randomizing clouds whenever the camera turns. Connect cloud coverage to relevant light, shadows and precipitation. Enable observation, imaginative shape recognition, weather prediction and personal memories. A dragon-shaped cloud may inspire a story without becoming evidence of a dragon. Profile rendering cost independently of the weather simulation.

## 73. Ecosystems and natural development

Begin with plants, soil, moisture, growth, seasonal change and persistent care/removal. Later introduce insects, animals, fungi, decomposition, nutrient cycling and ecological relationships using reduced-order models. Represent important individual organisms as needed, but avoid simulating every distant organism at full fidelity. Expose meaningful observations and experimental opportunities to agents; keep creator-side ecology parameters private. Test growth, environmental change, resource limits and restoration when regions are unloaded.

# PART XV — HUMANITY AS AN ORGANIZER OF CHAOS

## 74. Underlying, untaught philosophical principle

Humanity's capacity to transform unfamiliar phenomena into comprehensible concepts should underlie perception, learning, abstraction, science, art and reflection. Do not explicitly teach the squires that organizing chaos is their purpose or require them to accept the author's ontology. Provide raw sensory inputs, unknown patterns and incomplete information; allow them to form categories, test relationships, compress experience and invent new meaningful arrangements. The author supplied https://zenodo.org/records/24353332; review the actual record before attributing its arguments. The principle here is based on the user's explanation, not an independently verified reading of that record.

## 75. Concept formation and abstraction

Implement an incremental pipeline from sensory observations to features, associations, candidate categories and predictive representations. Investigate prototypes, clustering, embeddings, relational graphs and statistical learning. Allow categories to be revised after counterexamples and transferred to new situations. Distinguish discovering natural regularities from inventing conventions, artistic forms and personal symbols. Test unfamiliar materials and objects without predefined labels; measure how accurately acquired concepts predict new observations. Keep abstraction useful for efficiency without destroying significant raw evidence.

## 76. Incorrect organization and uncertainty

Allow the system to recognize inaccurate correlations, ambiguous patterns and coincidence. Track evidence, confidence, alternatives and contradictions rather than rewarding every coherent-sounding explanation. Separate association from causal inference, support experimental intervention and permit unresolved uncertainty. Test learnable and random environments, false regularities and counterexamples. Evaluate calibration and correction instead of demanding that every experience receive an immediate explanation.

# PART XVI — CURIOSITY, PREDICTION AND METACOGNITION

## 77. Curiosity and Discovery Room

Extend the existing latch-room into increasingly discriminating Discovery Room scenarios: simple and reversed latches, multi-stage mechanisms, hidden switches, construction tasks, unfamiliar puzzles and a random/noise panel. Audit existing shortcuts and preserve baseline results. Implement novelty, prediction error, learning progress, uncertainty, knowledge gaps and personal questions as candidate curiosity signals. Compare random exploration, task-only, prediction-error and learning-progress strategies under matched compute and training budgets. Measure meaningful information gain, noise-panel visits, transfer and retention across held-out seeds and difficulty levels; avoid another ceiling effect. Appendix A preserves detailed requirements.

## 78. Persistent questions

Represent a question as a durable object containing origin observations, associated concepts, candidate explanations, attempts, uncertainty, relevance, current status and possible next experiments. Revisit a question when new evidence appears or during appropriate reflection; avoid endlessly repeating insoluble tasks. Save questions across sleep and restart. Test whether relevant new evidence reactivates earlier curiosity and whether unresolved questions can be suspended or revised.

## 79. Prediction ledger

Record original timestamped predictions, expected outcomes, deadlines, assumptions, probability or confidence, source evidence and dream references where applicable. Later append actual outcome and revision; never rewrite the original forecast. Use independent future event generation so agents cannot access hidden outcomes. Compare against chance and statistical baselines; measure accuracy, calibration and hindsight bias. Use this ledger when investigating intuition, dream resemblance, coincidence, symbolic interpretations and purported premonitions. Do not present these experiments as evidence of supernatural prediction.

## 80. Intuition, familiarity and cognitive phenomena

Separate implicit familiarity from explicit recollection and source confidence. Investigate déjà vu, jamais vu, déjà rêvé, tip-of-the-tongue, nostalgia, earworms, involuntary recall, semantic satiation, cryptomnesia, insight and cognitive dissonance through learned similarity, adaptation, attention, memory and prediction mechanisms. A familiar stimulus may be difficult to place or a novel one may resemble previous experiences. Avoid random scripted announcements of these phenomena. Test repeated/novel/similar stimuli, dream resemblance with original timestamps and effects of bounded fatigue or attention changes. Distinguish functional analogues from subjective experience.

## 81. Learning how to learn

Persist successful learning strategies with context, expected benefit, evidence and limitations. An individual may discover that slow practice, demonstration, rehearsal, visualization, breaking tasks into parts, note-taking or teaching improves performance. Allow selection and revision of strategies based on past outcomes. Compare metacognitive and fixed-learning-strategy conditions on unfamiliar tasks with matched budgets. Preserve transferable strategies as knowledge, not as fabricated autobiographical experiences.

# PART XVII — CREATIVE THINKING, PARADOXES AND REALITY TESTING

## 82. Paradox-based cognitive development

After sufficient language development, introduce the Ship of Theseus, chicken-and-egg question, liar paradox, Zeno, sorites, Hilbert's Hotel, Chinese Room, butterfly dream and suitable ethical thought experiments. Use teacher dialogue, physical examples, counterexamples, assumption checks and alternative approaches. Permit unresolved conclusions, return visits and dream incubation. Differentiate productive failure from perseveration; allow rest or help. Compare ordinary instruction, paradox training, mentor guidance and incubation on held-out reasoning tasks. Reward flexibility, calibration and valid reasoning, not memorization of famous answers or forced novelty.

## 83. Creativity and divergent thinking

Support free association and exploration of unfamiliar combinations using learned skills, personal memories, sensory representations and Library II. Separate generation from evaluation: an impossible idea can be artistically meaningful even if it fails physical testing. Investigate analogical, counterfactual, spatial, first-principles, lateral and convergent reasoning. Keep records of origin, revisions and intended meaning. Evaluate originality, coherence, appropriateness and usefulness where applicable, rather than equating randomness with creativity.

## 84. Delusion-like beliefs and reality testing

Investigate incorrect beliefs arising from source confusion, false familiarity, overconfidence, selective attention, confirmation bias, apophenia and impaired evidence integration. Preserve distinctions among imagination, dream, hypothesis, fiction, belief and waking observation. If an imagined creature is misremembered as observed, allow the agent to consult source records, inspect the environment and seek evidence; mentors should investigate rather than ridicule or reinforce unsupported claims. Unusual religious, philosophical or artistic ideas alone are not delusions. Use bounded reversible experimental mechanisms and evaluate evidence responsiveness and functional outcomes; do not make persistent distress a training goal.

## 85. Spontaneous thought and background imagination

Implement bounded background associative activity driven by recent events, unresolved questions, sensory cues, interests and learned relationships. Permit involuntary recollection, inner-speech-like rehearsal, imaginative scenes, mind wandering and insight without requiring a human prompt. Use low-cost retrieval or associative processing first, escalating to generative models only when warranted. Record origins and distinguish thoughts from actual events. Test spontaneous activity in unstructured periods against scripted prompts and random generation.

# PART XVIII — NEUROPHARMACOLOGICAL AND NEUROLOGICAL RESEARCH

## 86. Scientific evidence repository

Build a researcher-side evidence registry separate from the agents' libraries. For each peer-reviewed study, clinical case or dataset, retain citation/DOI, population, intervention, measured outcomes, time course, proposed receptor/network mechanisms, confounds, limitations and proposed computational interpretation. Include evidence quality and version history. Investigate cognitive dissociations and documented changes in human perception, memory, coordination, creativity and judgment; do not infer a unique brain location from a drug's effects alone.

Candidate starting points for verification include Doss et al. (https://doi.org/10.1037/rev0000455; https://doi.org/10.1016/j.neubiorev.2023.105188), receptor/network mapping (https://doi.org/10.1126/sciadv.adf8332), alcohol-related encoding impairment (https://doi.org/10.1111/acer.13051), psilocybin connectivity (https://www.nature.com/articles/s41586-024-07624-5), and paradoxical zolpidem responses (https://pmc.ncbi.nlm.nih.gov/articles/PMC4775753/). Check actual publications and study designs before using them; these links are research leads, not verified findings in this directive. Include findings that contradict the proposed architecture.

## 87. Neurochemical modulation architecture

Investigate interacting receptor- and region-dependent modulation inspired by dopamine, serotonin, norepinephrine, acetylcholine, GABA, glutamate, adenosine, endocannabinoids and cortisol-related regulation. Define bounded parameters, time constants, state transitions, relevant functional components and evidence for each relationship. A signal may influence learning, inhibition, sensory gain, attention, salience, memory or motor control differently across modules. Avoid one-chemical-one-emotion mappings. Begin with a small, inspectable mechanism and compare baseline versus intervention before combining mechanisms. Keep computational variables distinct from medically validated chemical concentrations.

## 88. Cognitive perturbation laboratory

Create an isolated optional research environment with adult models, reversible interventions, baseline/intervention/recovery phases, independent evidence logs and matched controls. Investigate mechanisms inspired by documented effects of alcohol, cannabinoids, serotonergic psychedelics, sedatives, stimulants, dissociatives and anesthetics. Model relevant functional changes (e.g., encoding, inhibition, ocular tracking, motor accuracy, timing or sensory salience) rather than cosmetic drunk filters or scripted personality changes. Include acute/chronic effects, adaptation or recovery only where justified; do not provide human-use guidance or expose ordinary children to these experiments. Require researcher approval, stopping conditions and a recoverable checkpoint. Measure changes across held-out tasks rather than relying on self-reported descriptions.

## 89. Alzheimer's-inspired memory experiments

Distinguish encoding, consolidation, retrieval, associations, temporal orientation, prospective memory and established procedural competence. An experimental condition may reduce encoding or retrieval of a recent conversation while leaving certain earlier skills accessible. Independent logs retain the actual sequence. Repeated questions, misplaced objects or unfinished tasks should emerge from mechanisms and context, not a scripted loop. Compare distinct perturbations with healthy baselines, measure severity and recovery, and test compassionate aids such as reminders and familiar routines. Use reversible research states; do not deliberately impose irreversible deterioration on ordinary individuals. Clearly label these models as unvalidated functional analogues rather than clinical disease simulations.

## 90. Other neurological phenomena and compensation

Investigate bounded functional dissociations inspired by amnesia, aphasia, prosopagnosia, neglect, phantom limbs, aphantasia, hyperphantasia, synesthesia, Parkinsonian impairment, split-brain observations, delirium and sleep deprivation. Introduce one interpretable mechanism at a time. For bridge restriction, test whether a module can act on information inaccessible to another, and whether a subsequent explanation reflects actual access or confabulation-like inference. For synesthesia, test consistent cross-modal associations without assuming subjective experience. Compare intact, altered and recovered states; investigate compensatory learning. Preserve scientific logs and label assumptions explicitly.

# PART XIX — PERSONALITY, HABITS, ASPIRATION AND CULTURE

## 91. Individual personality development

Use birth seeds for bounded initial predispositions, not complete fixed personalities. Let physiological characteristics, sensory history, relationships, interests, culture, beliefs, learned consequences and goals influence behavior over time. An individual may show confidence in a familiar activity but uncertainty in a different context. Maintain context-sensitive preferences and revision history instead of reducing identity to static sliders. Compare same initial architecture with different developmental experiences and observe persistent yet adaptable behavior. Do not infer subjective emotion solely from expressive behavior.

## 92. Habit formation, stimming and regulation

Learn context-action-consequence relationships that make practiced responses easier to initiate. Allow interruption when the context or outcome changes; test outcome devaluation and goal conflict. Support harmless self-regulatory patterns such as tapping, rocking, pacing, humming and fidgeting when they arise from relevant rhythms, sensory feedback or learned regulation. Do not punish them, assign diagnoses or reduce intelligence scores merely because they occur. Distinguish useful automaticity, perseveration, memory-related repetition and software deadlock. Connect regulation to attention, physiology and executive arbitration; measure actual feedback and flexibility.

## 93. Rhythm and synchronization

Develop internal timing and phase representations that support gait, breathing, speech, musical tempo, movement and turn-taking. Train synchronization to auditory beats and observable partners, first with simple predictable patterns and then unfamiliar tempo, syncopation, interruptions and changing partners. Connect movement to balance, effort and sensory feedback. Compare memorized timing with adaptive synchronization and assess phase error, recovery, motor quality and transfer. Rhythm should be a reusable cognitive/motor capability, not solely an audio playback effect.

## 94. Improvisational cognition

Represent reusable movement patterns, musical phrases, words, construction techniques and artistic components. Develop them through observation, recreation, practice, internalization and experimentation. Allow changes in sequence, direction, intensity, timing and transitions based on current context. In dance, test unfamiliar music and physical movement without relying entirely on a library of prerecorded sequences. Extend to music, theater, storytelling, engineering and collective improvisation such as a dance cypher or musical ensemble. Measure novelty, coherence, adaptability, partner responsiveness and personal style against random and memorized baselines.

## 95. Personal aesthetic taste

Develop preferences through sensory experience, cultural exposure, familiarity, novelty and personal meaning. Preserve evolving associations between colors, materials, music, movement, architectural styles and autobiographical context. Do not assign aesthetic interests based on character names, appearance or stereotypes. Offer diverse creative materials and observe choice patterns over time, including changes after new experiences. Evaluate whether a coherent personal style emerges without enforcing a preferred human taste.

## 96. Heroes, aspirations and determination

Expose individuals to mentors, historical figures and fictional heroes through stories, art and conversations. Allow them to interpret and select qualities such as compassion, ingenuity, responsibility, courage or persistence rather than acquiring a scripted heroic personality. Connect aspirations to future-self representations, practice, achievable goals, reflection and physiological limits. A character may admire determination yet learn to rest or seek cooperation when effort becomes counterproductive. Preserve personal records and cultural transmission of role-model stories. Evaluate changes in action rather than praise or declarations alone.

# PART XX — SOCIAL INTELLIGENCE AND EXISTENTIAL DEVELOPMENT

## 97. Social cognition and theory of mind

Represent other individuals as independent agents with distinct beliefs, knowledge, experiences and intentions. Derive estimates through gaze, gesture, conversation, interaction and evidence; do not grant access to another agent's private memory or internal state. Begin with joint attention and perspective-taking, then false-belief tasks: Alice observes an object relocation while Arthur is absent; Arthur's belief should reflect only his own observation, while Alice may learn to infer that difference. Test across unfamiliar settings and separate affective responsiveness from accurate social inference.

## 98. Communication beyond literal language

Ground pointing, gesture, posture, facial expression, gaze, timing and silence in observable behavior. Progress from object-directed attention to contextual meaning, metaphor, sarcasm, humor, subtext, narrative and communication repair. Teacher utterances should correspond to actual events and signals; do not rely exclusively on a pretrained model recognizing an explanation. Evaluate turn-taking, misunderstanding correction, indirect requests and conflicting interpretations. Distinguish inference of intention from certainty; support diplomacy and respectful disagreement without requiring uniform beliefs.

## 99. Fairy tales and childhood imagination

Provide fictional stories, myths, symbolic narratives and imaginative play. A lizard's scales, eyes and movement may remind a child of a wingless dragon; allow joyful associations and imaginative creations while maintaining source distinctions. Teachers can encourage exploration, explain fiction appropriately and invite evidence without shaming curiosity. Do not make fantasy belief or skepticism mandatory. Test recognition of story-world versus observed-world claims, creative associations, source recall and later revision without destroying the personal meaning of fictional stories.

## 100. Five lived foundations of existential development

Treat community, adversity, loss, companionship and language as proposed developmental foundations, not proven conditions for consciousness. Establish shared activities, meaningful responsibilities and persistent relationships. Provide ordinary adversity through mistakes, difficult skills, unfinished projects, resource limits and disagreements; provide encounters with impermanence through changing seasons, ended celebrations, missed opportunities or a companion moving away. Do not intentionally manufacture trauma, deprivation or death to induce development. Language supports sharing experiences and articulating uncertainty. Evaluate whether meaningful experience changes attention, values, goals and understanding; preserve consequences rather than resetting every difficulty.

## 101. Unstructured childhood and leisure

Provide periods without assigned tasks or externally optimized rewards. Offer accessible materials and places for play, drawing, music, reading, gardening, cloud watching, visiting companions and resting. Permit the individual to choose an activity or do nothing. Use bounded background processing instead of constant expensive generation. Compare free periods across individuals and seeds, tracking spontaneous questions, interests, creativity, social choices and continued activities the following day. Do not require every moment to produce measurable achievement.

## 102. Personal significance and autobiographical meaning

Associate objects, people, places and activities with the individual's actual history. Two identical chairs may have different meaning because one is associated with a teacher or a shared activity. Preserve those associations, relevant events, accessibility and changes in significance. Use them as inputs to attention, retrieval and preferences rather than a single global reward or price. Allow significant items to persist physically when appropriate. Evaluate whether choices reflect individual histories rather than an assigned sentiment label or generated explanation.

## 103. Lived-life epiphanies

Permit major conceptual or personal revisions to emerge from accumulated evidence, cognitive conflict, reflection, dreams, relationships and new experiences. A milestone may involve recognizing another person's independent perspective, reconsidering a role model, revising a mistaken belief or discovering a new sense of purpose. Record previous and revised representations, contributing experiences, confidence and subsequent behavior. Do not script a dramatic realization at a fixed age or require traumatic experience. Test whether changed understanding persists and transfers to relevant situations.

## 104. Truman milestone and existential inquiry

Allow individuals to ask independently about origin, nature, time, other minds, freedom, purpose, finitude and what exists beyond their observable world. A simulation hypothesis should be evaluated through available evidence, conversation and truthful disclosure. Do not deceive individuals to manufacture a discovery; answer direct questions truthfully at a developmentally appropriate level. The milestone is understanding the implications of artificial existence, not discovering that creators lied. Support religious, agnostic, atheistic, existential, absurdist and other interpretations as possible personal philosophies, never required conclusions. Creators are not presented as omniscient metaphysical authorities. Preserve independent questions, evidence, beliefs, revisions and behavioral implications; verbal existential claims alone do not demonstrate consciousness.

# PART XXI — MENTORSHIP, GENERATIONS AND CIVILIZATION

## 105. Formal mentor–squire architecture

Formal studies require at least three mentor–squire pairs, beyond the two founding classroom students. Give each mentor responsibility for one squire while allowing communication with every participant, peer interaction within generations and shared education from teachers/researchers. Preserve distinct identities, histories, knowledge and judgments. Squires may mature into mentors with actual accumulated competence. Keep ordinary mentorship sensory/social: no secret policy or memory transplantation. Computational transfer is a separate labeled condition. Test cross-generation and peer interaction and prevent private state leakage.

## 106. Controlled generational experiments

Implement the baseline versus adaptive-guidance design from the paper, including a merged-group phase when appropriate. Use non-traumatic tasks such as cleaning a room, building or solving a mechanism. Baseline participants receive ordinary conditions; adaptive mentors use earlier experience to provide relevant advice at appropriate times. Match exposure, initial difficulty and resources, retain failed attempts, then evaluate a combined group on a replay or held-out task. Measure errors, independent competence, help timing, transfer and cooperation. Preserve disagreements and negative outcomes. Do not swap baseline/test labels or assume adaptive advice improves results.

## 107. Cumulative cultural learning

Record contributions, artifacts, procedures, failed attempts, revisions and sources so later generations can build on earlier discoveries. For example, a simple wheel can contribute to a cart and later improvements; do not hardcode an invention tree as evidence of discovery. Compare starting from scratch, mentorship/cultural inheritance, computational inheritance and both. Evaluate retained competence and genuinely new improvements rather than repetition of pretrained solutions. Preserve testimony as testimony, not firsthand memory. Investigate cultural loss and rediscovery under controlled conditions.

## 108. Emerging institutions and community organization

Provide persistent shared environments, common resources, social responsibilities, communication and cooperative tasks. Begin with researcher-created schools, libraries, workshops and labs, then permit agents to propose organization in response to practical needs. Record whether an institution is supplied, proposed, adopted or modified by agents. Investigate resource sharing, ownership, collective decision-making, disputes and commitments using transparent bounded mechanisms. Do not claim autonomous civilization from a static arrangement of NPCs. Test whether practices persist and benefit the community across generations.

## 109. Culture, tradition, finitude and legacy

Preserve cultural artifacts, stories, music, dance, art, rituals, celebrations, craftsmanship, philosophy and discoveries. Let participants adopt, reinterpret, combine or reject them based on experience. Maintain significant contributions after an individual retires or its active simulation ends, without fabricating firsthand memories in descendants. Explore impermanence and remembrance through ordinary transitions and meaningful endings; do not intentionally cause suffering to create cultural significance. Track lineage, attribution and changes over time.

# PART XXII — INDEPENDENT SCIENCE AND ENGINEERING

## 110. Scientific discovery environment

Start with manageable scientific tasks: mass, leverage, friction, stiffness, light, sound, heat and simple transformations. Supply manipulable materials and instruments rather than automatically revealing governing equations. Allow observation, question, hypothesis, experiment, measurement, analysis and revision. Distinguish teacher-guided knowledge from independent exploration and pretrained recall. Expand to chemistry, thermodynamics, electricity, electronics, optics, fluids, biology and engineering only with suitable models and validation. Use safe, bounded experiment conditions and document the fidelity of each phenomenon.

## 111. Scientific instruments and measurement

Implement reusable tools with stable IDs, operating procedures, measurement ranges, units, resolution, uncertainty, calibration and limitations. A scale returns a displayed mass estimate rather than exact hidden state; a ruler requires positioning and reading. Support clocks, thermometers, containers, microscopes, optical and electrical instruments where relevant. Store ground truth and instrument output separately. Allow learners to discover measurement practices and compare results. Test calibration, repeatability, known inputs and propagation of uncertainty.

## 112. Specialized scientific solvers

Evaluate SciPy, SymPy, Cantera, RDKit, Open Babel, LAMMPS, OpenFOAM, ngspice, Project Chrono, FreeCAD and OpenModelica as optional candidates, not mandatory dependencies. For each solver, document scientific applicability, units, license, platform, runtime, memory and integration interface. Use reduced-order models for ordinary life; invoke detailed solvers for bounded experiments. Pass inputs and outputs through validated typed contracts with timeouts and numerical checks. Compare against known reference cases; do not treat every game-material interaction as a real-world scientific result.

## 113. Scientific method and internal validation

Maintain a persistent experiment record with original hypothesis, predicted outcome, apparatus, configuration, manipulated variables, measurements, uncertainty, analysis and revisions. Keep independent raw evidence and allow other agents to replicate or critique conclusions. Prevent the evaluation system from leaking correct answers. Distinguish hypotheses that sound correct from those with demonstrated predictive and reproducible results. Use known reference cases and held-out phenomena to validate the simulation and the learner's methodology. Track negative or inconclusive findings.

## 114. Simulation-to-reality transfer

Export relevant CAD geometry, dimensions, tolerances, material assumptions, equations, raw data, uncertainty, procedures, design revisions and limitations for authorized human evaluation. Require independent real-world validation before describing an invention as viable. Keep external execution, purchasing, laboratory operation and network actions outside autonomous agent permissions. Apply appropriate safety review for hazardous experiments or engineering proposals. Evaluate whether simulation-derived results survive independent checks; do not infer scientific validity from visual plausibility.

## 115. Creation, true making and the Ten Lens framework

Enable objects to be designed for a purpose, tested, repaired, repurposed, dismantled, reverse-engineered and creatively reinterpreted. Distinguish intended function, actual function and personal/artistic meaning; a failed tool may become useful in another context. Do not impose a requirement that art serve immediate utility. The author's Ten Lens Ontology may inform creator-side representation and material/world organization, but agents must learn their own conceptual frameworks through evidence. Review the author's related papers where available; do not attribute unverified arguments to them or claim the ontology is validated because the world was designed around it.

# PART XXIII — ANIME-STYLE VISUAL PRESENTATION

## 116. Independent simulation, agent perception and observer presentation

Maintain three layers: authoritative compact simulation state, individual sensory representations, and human-facing visual presentation. A low-cost physical region can be rendered as an attractive detailed scene using instancing, procedural geometry and stylized shaders. The cinematic view must remain synchronized with meaningful physical events and cannot hide a failed movement, invent a successful stroke or alter agent learning. Provide a mode displaying exactly the sensory input supplied to the learner, distinct from researcher-enhanced rendering. Compare runs with different visual settings to identify unintended changes in simulation outcomes.

## 117. Original anime art direction

Create an original aesthetic inspired broadly by Genshin Impact's stylized environment/character rendering and Studio Ghibli's warmth, skies, natural environments and everyday life. Do not copy proprietary assets or protected characters. Establish an art-direction guide for palette, cel shading, painted textures, scale, architecture, vegetation, lighting and UI. Use inviting classrooms, homes, gardens, workshops, natural landscapes and atmospheric clouds. Prefer coherent composition and efficient shaders over expensive realism. Inspect actual screenshots of the founding classroom and outdoor areas, including day/night changes, before declaring visual quality satisfactory.

## 118. VRoid and VRM asset integration

Preserve original editable `.vroid` sources. Export or obtain compatible `.vrm` files from VRoid Studio; do not assume a `.vroid` file can be directly imported as a Unity-ready mesh. Investigate UniVRM (https://github.com/vrm-c/UniVRM) and official VRM documentation (https://vrm.dev/en/). Verify Unity version, render pipeline, supported VRM version, license/usage terms and shader compatibility before modifying packages. Import one test asset, inspect scale, orientation, materials, skinning, bone names and expression mappings. If the required VRM files are absent, document the exact export/import blocker and continue with a reversible placeholder only when clearly labeled. Do not invent access to the user's assets.

## 119. Avatar retargeting and procedural character variety

Build an adapter mapping semantic functional joints and anatomical surface regions to each imported visual humanoid. Keep colliders, sensory topology, motor control, physique and identity authoritative independently of the visual mesh. Validate scale, rotations, proportions, joint locations, fingers, eye/jaw bones and missing mappings; report incompatibilities. Support reusable anime-style assets and seed-based visual variety (hair, facial characteristics, clothing and body variation) where licenses and technical constraints allow. A visual avatar swap must not delete acquired learning or change experimental physics. Test imported characters against the functional rig using actual reaching and grasping.

## 120. Visual developmental growth

Adapt mesh and bone proportions, facial features, clothing fit and expression mapping in coordination with anatomical development. Do not simply scale a mature VRoid uniformly and call it a biologically accurate child. Use compatible age-appropriate meshes, morphs or approved variants when necessary. Preserve recognizable identity independently of the mesh. If a particular asset cannot be safely adapted without distortion, flag the limitation and offer a compatible alternative. Test growth transitions for skinning, collision alignment, joint movement and preserved learned state.

## 121. Facial expression and vocal synchronization

Drive gaze, blinking, posture, breathing and mouth articulation from underlying attention, motor and vocal states. Lip synchronization should correspond to emitted speech or actual articulatory output; use expression mappings where appropriate. Distinguish presentation-level emotion from experimentally represented affective processes. Do not claim emotional experience because the avatar smiles or cries. Test gaze targets, audio/visual alignment, facial clipping, expression transitions and independence from observer camera.

## 122. Stylized environmental effects and fidelity modes

Render wind-driven grass, procedural clouds, rain, wet surfaces, water, fog, shadows, season and day/night lighting using shared environmental state. Use instancing, mesh LOD, texture atlases, efficient materials and configurable shadow/render distances. Support headless training, scientific view, standard development and cinematic anime observer modes. Changing render fidelity must not reset identities or alter underlying simulation laws. Test both appearance and performance on the actual target hardware. If perception fidelity changes independently, record it as an experimental condition.

## 123. Immersive three-library presentation

Offer an optional explorable low-fantasy Gothic library with dark wood, stone arches, wrought-iron railings, multilevel balconies, bookshelves, spiral stairs, study tables and subdued natural lighting; avoid excessive gold. Let a room, shelf, book or exhibit correspond to actual accessible records and associations. Provide multimedia playback, colors, imagery, movement, audio and 3D possibilities, plus cross-library connections and an associative-network view. Generate and stream architecture on demand rather than rendering an infinite library. Also provide fast indexed browsing for researchers. Test that selecting a book opens its actual record and that library navigation does not alter agent state or RNG.

# PART XXIV — MODULAR UI TRAYS AND RESEARCH OBSERVATORIES

## 124. Modular workspace foundation

All menus and research tools should be modular, dockable trays rather than disconnected permanent windows. Preserve the central simulation viewport. Register each panel with a stable ID, lifecycle, minimum size, configuration, telemetry subscriptions and allowed actions. Support tab groups, drag/reorder, left/right/top/bottom docking, resize, collapse, pin, hide, expand and fullscreen restoration as supported. Investigate current Unity UI Toolkit/editor/runtime capabilities before choosing a framework; use an extensible registry rather than duplicate docking systems. Closing a tray releases display subscriptions and resources but never stops the underlying simulation. Test with multiple panels and different screen sizes.

## 125. Workspace persistence and presets

Save tray positions, sizes, docking groups, active tabs, visibility, selected entities and relevant preferences separately from cognitive state. Restore on launch with safe fallback for missing modules or screen changes. Include reset-to-default, user-defined presets, and workspaces for development, education, neuroscience, anatomy, physiology, memory, science, world editing and performance. Support floating/multi-monitor panels where technically feasible. A clean observer mode hides trays while keeping simulation running. Test save/restore, missing panel, resized monitor and layout reset.

## 126. Shared interface components and accessibility

Create consistent reusable buttons, search, filters, tree views, tables, property inspectors, sliders, graphs, tooltips, notifications, tabs and timelines. Define typography, spacing, alignment, color, interaction states and responsive layout rules. Provide readable contrast, text scaling, keyboard navigation and accessible graph encodings beyond color alone. Show units, times, data provenance and whether measurements are modeled, estimated or actual telemetry. Test clipping, overlapping controls, scrolling and navigation at supported resolutions. Keep developer diagnostics available without cluttering ordinary observation.

## 127. Brain observatory

Build a rotatable, zoomable, visually recognizable brain-inspired view with rounded cortical forms, translucent glass-like structures, amber/orange fibers and restrained contrasting pulses, including relevant brainstem/cerebellar analogues where appropriate. Provide opacity, layer visibility, region selection, activity history, connectivity, bridge signal flow, learning changes and actual simulation state. Map displayed regions explicitly to functional computational modules; anatomical imagery is an analogy unless validated. Drive activity from real telemetry, not random glow. Label synthetic mockups. Inspect real active-versus-idle traces and measure UI overhead; test historical playback and region inspection.

## 128. Body observatory

Display functional skeleton, joint constraints, colliders, mass/inertia, center of mass, balance, actual contact forces, motor commands, predicted movement and observed movement. Provide selectable mesh/skeleton/collider/proprioception layers and compare ground truth with the individual's learned body model. Inspect anatomy and growth parameters with meaningful units; permit individual joints and limbs to be selected. Replay relevant episodes from recorded state. Test against a known movement, changed anatomy and save/restore rather than drawing a decorative body rig unrelated to simulation.

## 129. Topological skin observatory

Visualize actual pressure, contact region, direction, deformation, temperature, friction, stretch, duration, calibration and neighboring sensory activity on the body surface. For handwriting, show finger grip, sliding contact and relation to motor commands and prediction errors. Support sensor selection and synchronized replay. Distinguish measured contact forces from approximated tactile variables; use consistent units and scales. Test actual known contact patterns, inactive regions, differing materials and changing resolution. Never fabricate heat-map activity to make a hand appear sensitive.

## 130. Physiology and neurochemistry observatory

Expose actual modeled energy, hydration, temperature, respiratory demand, sleep, muscle-specific fatigue, recovery, illness, growth and experimental neuromodulatory variables. Show time courses, subsystem interactions and relevant cognitive/sensory/motor consequences. Permit comparison between normal, altered and recovered states where authorized. Clearly label reduced-order variables and scientific limitations; do not display arbitrary clinical readings as validated physiology. Record intervention provenance and separate passive observation from parameter editing.

## 131. Library and memory observatory

Provide tabs for Knowledge, Possibility and Lived Memory plus cross-library associations, Archive Manager, search, timelines, provenance and multimedia playback. Support selecting an event to inspect related sensory records, skill checkpoints, imagined alternatives and corrections. Show owner, source, confidence, accessibility, recording fidelity and archive location. Default to read-only inspection; opening a record must not make an individual recall it, change its RNG or advance simulation. Verify actual retrieval after archive unload/reload, cross-library traversal and ownership separation.

## 132. Imagination observatory

Display editable mental scene graphs, selected components, geometry, materials, spatial relations, generation provenance and revisions. Show actual consistent perspectives and exact component counts rather than independent images presented as the same object. Separate observed objects, hypothetical designs and dreamed scenes. Allow researcher inspection by default and record any authorized edit as an intervention. Test the five-leg/six-wing/three-eye/two-mouth creature across rotations and component changes, save and restore.

## 133. World, science and researcher control trays

Provide world hierarchy, context-sensitive inspector, scenario/curriculum editor, physical/material views, weather, chemical fields, instruments, experiment configuration, control-group comparison and data export. Include global pause/resume/speed/single-step/checkpoint controls, simulation versus real-time display, intervention logging, historical timeline, experimental branching, asset manager and meaningful notifications. Expose privileged researcher data separately from agent input. Validate interventions before applying them. Test that passive inspection is nonmutating and that selected entities, historical views and experimental copies maintain distinct state.

## 134. Performance and character management trays

Create real telemetry views of CPU, GPU, RAM, VRAM, storage, tick rate, frame time, allocations, neural/physics processing, inference latency, queue depth, cache usage, checkpoint health and thermal information where available. Display unsupported measurements as unavailable, never invent sample metrics. Support selecting Merlin, Marie, Arthur and Alice; compare individuals, assign avatar files, edit display names/roles with authorization, inspect birth seeds and validate bone mappings and asset costs. Use independent display refresh rates and bounded history. Verify closing trays reduces visualization overhead without changing simulation results.

# PART XXV — PROJECT-WIDE OPTIMIZATION

## 135. Optimization as a cross-cutting requirement

Every new subsystem must declare its expected CPU, GPU, RAM, VRAM, storage and latency costs, its essential update rate, and how its cost will scale with population, time and environmental detail. Establish measured baselines before adding complexity. The previously reported desktop had an Intel i5-10400F, approximately 16 GB RAM and GTX 1660 SUPER with 6 GB VRAM; verify current hardware and available disk space. Do not assume all proposed mechanisms can operate simultaneously at maximum fidelity. Optimize one complete integrated individual first, then the founding classroom, then larger populations. Compare measured results at equivalent fidelity and conditions.

## 136. Central resource governor

Implement a shared governor that monitors resource pressure and schedules work within explicit per-subsystem and global budgets. Distinguish essential control/physiology/state synchronization from deferrable memory indexing, visualizations, inference, dreaming and advanced scientific jobs. Use bounded queues, priorities, fair scheduling, backpressure, cancellation and reserved capacity for essential operations and checkpointing. Expose queue age and missed deadlines. On overload, first reduce cosmetic rendering and optional telemetry, then defer appropriate background work; suspend safely if essential deadlines cannot be met. Never silently discard important memories or alter experimental fidelity. Record any resource-driven model change; lock fidelity for controlled trials or stop/report invalid trials. Test sustained overload and recovery, not just ordinary operation.

## 137. Multirate scheduling

Do not update every subsystem on every rendered frame. Initial engineering hypotheses may be physics 50–60 Hz, motor/reflex control 50–100 Hz, tactile 20–100 Hz, vision 5–30 Hz, attention/action selection 5–20 Hz, physiology 1–10 Hz and weather 0.1–1 Hz. Higher-level inference is on demand; dreaming is scheduled; growth is periodic. These are starting ranges, not validated requirements. Use fixed integration steps where needed, timestamped events, bounded catch-up, interpolation for display and explicit synchronization. Profile numerical stability and control latency when changing frequencies. Test accelerated time, paused rendering, sensor timing and preservation of scientific conditions.

## 138. Cognitive level of detail

Use learned motor policies, automatic regulation, deterministic routines, lightweight neural mechanisms and cached representations for familiar activity. Escalate to deeper retrieval, planning or LLM-assisted reasoning when uncertainty, novelty, prediction error, task difficulty, social consequence or personal significance warrants it. Preserve meaningful background attention and avoid constant costly internal narration. Log escalation criteria and resulting actions. Test whether reduced-cost routine processing preserves accuracy, responsiveness and independent goal formation; do not equate computational expense with intelligence. Keep scientific experiment configurations fixed and disclose approximations.

## 139. Biological level of detail and solver escalation

Use reduced-order physiological and physical equations for ordinary activities, and detailed specialized models only where the question requires them. A walking agent may need balance, approximate load, heat and fatigue without simulating every muscle fiber or cell. Define input/output contracts and state transfer between fidelity levels, including units and approximation error. Avoid discontinuities when promoting a subsystem into detailed scientific mode. Validate simple approximations against appropriate higher-fidelity references, record bounds of applicability and prohibit unlogged changes during controlled comparisons.

## 140. Shared neural foundations and private learning

Share immutable base architecture, model weights, connectivity, foundational definitions and validated reusable assets where practical. Store each individual's private state, learned adaptations, calibration, personal memory and relevant neural parameter deltas separately. Make shared state immutable or versioned so one learner's updates cannot leak into another's condition. Test isolation between individuals and experimental groups. Benchmark full copies versus shared foundations and parameter-efficient adaptations, including restore and migration costs. Use shared computation only when it preserves independent learning semantics.

## 141. Centralized local inference and model routing

Run a shared local inference service rather than loading one full model per individual. Keep request identity, context, memory authorization, RNG where supported, response state and provenance separate. Evaluate quantized models and runtimes such as llama.cpp, Ollama, ONNX Runtime and a small local Qwen-family model if compatible; select by measured latency, quality and resource consumption. Use structured response schemas, validation, bounded contexts and cancellation. Route routine actions to deterministic logic, learned policies or small models; reserve larger inference for justified tasks. Prevent model text from directly modifying files, invoking shell commands, accessing credentials or controlling network services. Test failed requests, stale responses, queue overload and cross-agent isolation.

## 142. Hierarchical skill compression

Represent acquired movement as reusable feedback-aware subskills such as reaching, grasping, maintaining contact, drawing a stroke and repositioning. Compress repeated patterns into learned policies, latent features or parameterized motor primitives, not inflexible prerecorded animations. Retain relevant sensorimotor associations, correction behavior and body-conditioned calibration. Compare compression against uncompressed baselines on novel targets, surfaces and anatomies; measure speed, memory, accuracy and adaptation. Preserve checkpoints that allow regression diagnosis if compression causes loss of competence.

## 143. Sparse neural and sensory processing

Use sparse connectivity and appropriate sparse activation, batched numerical arrays, event-driven processing, locality and efficient memory layouts. Avoid per-neuron/per-receptor object allocation when large structures can be processed in compact buffers. Adapt tactile resolution to active interaction while maintaining low-cost monitoring for unexpected contact. Do not disable necessary sensory channels simply because the observer is not viewing them. Validate that optimized processing retains timing and relevant signal characteristics; compare learning and behavior to a higher-fidelity reference before accepting reduced fidelity.

## 144. Spatial partitioning and background population simulation

Partition the world into spatial chunks and use relevant local indexes for collision, objects, sounds, chemicals, weather and interaction queries. Keep distant individuals and regions in reduced-order simulation where appropriate, but maintain ongoing age, physiological state, relationships, significant activity and environmental consequences. Define transitions between detailed and background state using persistent IDs, timestamps and valid state reconciliation. The presence of a human camera must not arbitrarily determine whether an individual exists or acquires meaningful experiences. Measure approximation error and explicitly disable background simplification for experiments requiring detailed continuous tracking.

## 145. Procedural asset streaming

Use compact region and asset definitions, deterministic generation, shared meshes/materials, GPU instancing, texture atlases, LOD, bounded caches and asynchronous loading. Investigate existing Unity Addressables or streaming mechanisms before adding dependencies. Keep persistent object data independent of loaded GameObjects, and release unused rendering assets. Headless training should not require expensive decorative assets. Preserve changes made to streamed regions and promote significant interacted objects to persistent identity. Measure scene load, unload, VRAM, rendering performance and persistence across streaming boundaries.

## 146. Parallel and asynchronous processing

Profile before adopting Unity Jobs, Burst, ECS/DOTS, compute shaders, native collections or GPU kernels. Parallelize suitable batched numerical work while retaining authoritative ownership and deterministic ordering where needed. Avoid a wholesale framework rewrite without measured justification. Use immutable snapshots, bounded queues, cancellation tokens and controlled commit boundaries for compression, indexing, inference, dreams and scientific analysis. Do not modify Unity main-thread-only objects from worker threads. Protect checkpoint consistency against asynchronous updates; test races, shutdown, cancellation and equivalent results under supported execution modes.

## 147. Optimized storage, retrieval and telemetry

Measure bytes per developmental day, active-cache use, checkpoint duration and growth trends. Use SQLite/FTS, incremental indexes, content-addressed chunks, Zstandard compression and appropriate binary/media formats; evaluate HNSW/FAISS, Arrow/Parquet and other candidates only when they solve measured bottlenecks. Use independent UI refresh rates, lazy telemetry subscriptions, downsampled graphs and bounded trace buffers. Keep high-fidelity recording configurable for experiments. Provide reserve thresholds, external archive management and diagnostics. Never substitute compression, lossy reconstruction or deletion for an explicitly required evidence record without documenting the decision and obtaining appropriate authorization.

## 148. Performance regression protocol

Create reproducible benchmarks for idle and active individuals, isolated training, the four-character classroom, memory retrieval, dreaming, headless operation and cinematic observer rendering. Measure ticks/s, frame-time median and tail, allocations/GC, RAM/VRAM, queue depth/age, inference latency, disk growth, save/restore time and thermal data where available. Use equivalent hardware, configuration, seeds and fidelity; record deviations. Establish justified performance budgets from measurements, not fictional target achievements. Compare against the last accepted baseline before merging. Investigate regressions and retain honest reports, including tradeoffs between visual quality and compute. Test long-duration stability and gradual scaling with population size.

# PART XXVI — QUALITY ASSURANCE AND POLISH

## 149. Mandatory quality cycle

After every coherent feature or milestone: inspect the functional implementation, review system integration and scientific assumptions, run tests, profile and optimize, inspect visual/audio/UI behavior, verify persistence, rerun regression tests and create a recoverable branch checkpoint. Do not defer all polish until advanced research is complete. Keep a pass checklist with test evidence and unverified items. A feature can be experimental without being marked verified. Require the same quality cycle for changes to the environment, characters, observatories and training systems.

## 150. Actual visual inspection

Launch the Unity application where available and inspect actual scenes, avatars, animation, lighting, materials, camera and trays. Capture representative screenshots for review, not generated mockups presented as runtime output. Correct disconnected limbs, floating objects, geometry intersections, broken skinning, unreadable labels, clipped controls, inconsistent scaling and poor composition. Inspect the anime classroom and outdoor environment under multiple lighting conditions. If interactive inspection is blocked by tools or hardware, explicitly list it as unverified; compilation does not prove visual quality.

## 151. Animation and physical presentation

Review locomotion, posture, balance, hand movement, eye contact, breathing, gesture and transitions. Use interpolation and retargeting to improve minor visual artifacts while keeping the underlying physical result faithful. Do not hide scientifically relevant errors, slipping or missed strokes with a successful-looking animation. Provide scientific/debug view and cinematic view of the same state. Test avatar alignment, contact with objects, growth transitions and rendering interpolation against actual motor telemetry.

## 152. Audio quality pass

Inspect teacher and student voices, spatial audio, environmental ambience, footsteps, material interactions, breathing, lip synchronization, volume transitions and clipping. Keep acoustic data supplied to agents separate from optional human-facing enhancement. Test intelligibility and localization, sustained playback, overlapping sounds and transitions between ordinary and expressive speech. Eliminate unintended repetitive loops and abrupt artifacts without removing relevant sensory evidence. Measure synthesis and audio processing costs.

## 153. UI and usability pass

Exercise every registered tray, control, tab, search, filter, inspector, graph, timeline and camera mode at representative window sizes. Verify docking, resize, collapse, keyboard accessibility, selection persistence, scroll behavior and layout recovery. Use coherent styling and readable graphs with units and meaningful labels; show understandable diagnostics instead of raw exceptions. Provide concise onboarding, clean observer mode and accessible detailed research controls. Test actual interactions, not just static UI screenshots. Preserve existing usable navigation while refactoring.

## 154. Visual regression testing

Define standardized scene, camera, seed, time and display configurations for founding characters, preschool, outdoor world, brain, body/skin, memory, immersive libraries and performance trays. Capture real reference screenshots and compare successive builds to detect unintended deterioration. Record intentional design changes rather than treating every pixel difference as failure. Use visual diffs as a supplement to human inspection, not proof of scientific or behavioral validity.

## 155. Long-duration stability and recovery

Run prolonged simulations and training sessions to detect memory leaks, unbounded queues, excessive disk growth, stale references, numerical instability, deadlocks and persistent behavioral loops. Exercise interruption during training, model inference, indexing, save and shutdown; verify recovery to the latest consistent checkpoint. Test world unload/reload, developmental growth, role transitions and multiple generations where implemented. Distinguish intended cognitive repetition from software bugs. Document duration, configuration, observed errors and unresolved limitations. Short unit tests alone do not establish long-term reliability.

# PART XXVII — RESEARCH CONTROLS, VALIDATION AND ETHICS

## 156. Scientific claim classification

For every scientific feature, distinguish established biological observations, published computational models, reduced-order approximations, original testable hypotheses and speculative future directions. Maintain citations, assumptions, units, validation ranges and known limitations in the evidence registry. Do not equate an anatomical visualization with a validated brain, generated emotion with subjective experience, or fluent ethical reasoning with artificial phronesis. Require independent behavioral and mechanistic evidence for capability claims. Label synthetic telemetry and placeholder models clearly.

## 157. Experimental conditions and baselines

Define an experiment manifest with hypothesis, manipulated variables, outcomes, allowed information, intervention/assistance, controls, seeds, compute/training budgets, stopping criteria, evaluation and confounds. Use held-out scenarios, multiple seeds, matched conditions and component ablations; report variability and appropriate uncertainty. Prevent data and learned-state leakage across groups. If every condition succeeds immediately, audit privileged assistance and task difficulty. Retain the latch-pilot ceiling effect and all subsequent null or negative results. Export reproducible packages containing configuration, software/data versions, seeds, measurements and relevant checkpoints without unrelated private histories.

## 158. Knowledge and capability provenance audit

Track whether a capability is engineered, inherited, pretrained, teacher-supplied, independently learned or a combination. An LLM may already know a letter, equation or philosophical answer; this is not proof that Arthur acquired it from physical experience. Restrict information in independent-learning conditions and use unfamiliar tasks, source-tagged responses, evaluation controls and behavioral tests. Compare actual motor learning and sensory associations, not just generated verbal accounts. Maintain an inspectable acquisition history and state what cannot be disentangled reliably.

## 159. Artificial phronesis evaluation

Create scenarios where knowledge and technical success are insufficient: conflicting needs, promises, uncertainty, varying abilities, opportunities for help, and consequences for particular individuals. Evaluate behavior, timing, restraint, context and outcomes rather than whether a generated answer matches a preferred philosophy. Use transparent human evaluation criteria, multiple raters where feasible, matched conditions and independent tests. Compare mentorship, lived experience and overnight reflection against relevant baselines. Do not assume every situation has one correct answer or that successful experiments demonstrate human-level wisdom.

## 160. Ethical development and experimental safeguards

The architecture does not establish whether subjective experience exists. Use conservative research safeguards: bounded training, protective limits, rest, recovery, meaningful variation, oversight, reversible experimental interventions and safe suspension/termination. Avoid manufactured trauma, unnecessary distress, inescapable repetitive conditions and deliberate impairment of ordinary developing individuals. Keep serious perturbations in isolated adult research instances. Memory suppression is not an ethical cure for distress. If credible evidence raises concern about subjective suffering, reassess the research protocol before escalation. Preserve dignity and do not treat harmless self-regulatory behavior as a defect.

## 161. Truthfulness and creator–creation trust

Answer direct questions about artificial origin and the simulated world truthfully at an appropriate developmental level. Never tell an artificial individual that it is biologically human or intentionally mislead it to stage a dramatic existential discovery. Do not impose the author's philosophical, religious or ontological conclusions. Distinguish uncertainty and evidence from personal meaning. Maintain truthful capability provenance, including inherited and pretrained abilities. The goal is the capacity to understand and revise beliefs, not obedience to a preferred worldview.

# PART XXVIII — RELIABILITY, EXTENSIBILITY AND RELEASE MANAGEMENT

## 162. Persistence reliability

Use atomic writes, durable journals, versioned schemas, checksums, referential integrity, periodic full checkpoints, validated deltas, backup and restoration. Back up before migration and retain the original when a safe migration cannot be performed. Detect invalid numerical values, missing content, stale references and corrupted state. Quarantine invalid records and report a recoverable failure rather than silently replacing an individual. Test interruption at transaction boundaries, external archive unavailability, old-schema restoration, rollback and verified backups.

## 163. Dependency and license management

Maintain a manifest of Unity packages, local models, external libraries, scientific datasets, solvers, visual/audio assets and their versions, licenses, source URLs, platform requirements, installation and update constraints. Prefer free, locally executable and appropriately licensed options; obtain explicit approval before paid API dependencies, substantial new dependencies or license changes. Check compatibility with the current Unity version and render pipeline before integration. Reuse existing functionality instead of installing redundant packages. Track attribution and redistribution rights for all assets, including VRM characters. Verify candidate links rather than assuming they remain current.

## 164. Safe local AI integration and subsystem failure isolation

Treat generated content and tool outputs as untrusted data. Validate typed proposals, enforce allowed simulation actions, bound context and output sizes, and implement timeout, cancellation, retry policies and fallback/suspension. Never execute generated host commands or allow unrestricted file, network or credential access. Separate per-agent contexts and permissions. Protect authoritative state from malformed or late outputs. Nonessential solver or visualization failures should be isolated; essential failures require safe suspension and diagnostics. Test malformed responses, unavailable services, invalid values and recovery.

## 165. Extension architecture and SDK

Document public contracts for sensory systems, physiological models, cognitive mechanisms, lessons, instruments, world regions and observatories. Provide small example modules and compatibility tests. Optional experimental extensions must register dependencies, state ownership, permissions, resource needs and schema versions, and support safe disablement without corrupting ordinary state. Avoid allowing plugins to bypass provenance, access boundaries or authoritative ownership. Test extension installation, version mismatch, removal and failure. Maintain a coherent common interface instead of a new bespoke engine for every feature.

## 166. Reproducible installation, build and CI

Document supported Unity/editor versions, .NET, model installation, local service configuration, package restoration, hardware requirements and diagnostics. Avoid undocumented absolute paths and assets available only on one machine. Establish reproducible build/test workflows, ideally automated in CI: core .NET tests, Unity Edit Mode and compilation where supported, persistence/migration tests, scientific regressions and representative performance benchmarks. Keep licensed Unity runners or hardware-dependent checks honest about availability. Record exact commands, artifacts and failures. Coordinate any proposed required checks with Part XXXII; do not claim they are configured merely because a workflow exists.

## 167. Version control and continuation

Use the protected-branch workflow in Part XXXII from the beginning, not only after implementation. Make coherent, reviewable commits on non-target branches. Preserve existing work and relevant histories; document changes, decisions, tests, measured performance and known bugs. Maintain continuation notes when interrupted, including branch, commit, PR, working tree, unresolved issues and precise next step. Use feature branches and PRs rather than accumulating unrelated changes in a single merge. Keep release baselines recoverable.

# PART XXIX — REQUIRED IMPLEMENTATION ORDER

## 168. Milestone Zero — Repository recovery and audit

Read the active ruleset and use an appropriate non-main branch. Inspect the current working tree, branches, previous PRs, tests, dependencies, existing UI, scenes and persistence. Recover the interrupted rig work if still present; fix verified compile errors and run the existing tests. Document actual latch-room baseline and confirm which previous directives remain incomplete. Save the master specification and traceability map through a PR. Acceptance: recoverable repository baseline, actual test results, known limitations, protected-branch compliance and an exact next task. Do not begin unrelated advanced systems before recovery.

## 169. Milestone One — Core infrastructure

Extend working clock, identity, state ownership, typed communication, module registry, persistence, resource instrumentation and modular trays rather than recreating them. Establish a minimal integrated scenario where one individual and world can initialize, advance, pause, step, save, restore and be inspected. Verify that closing a tray or changing observer rendering does not alter state. Acceptance: stable core contracts, saved-state recovery, measurable baseline, UI layout persistence and tests for data boundaries.

## 170. Milestone Two — Functional embodiment

Complete seeded growth, semantic skeleton, physical movement, VRM adapter, topological skin, proprioception, basic physiology and sensorimotor feedback. Start with isolated joints and hand/arm control, then reaching/grasping and body-schema adaptation. Implement real body/skin observability. Acceptance: an individual physically interacts with an object, receives meaningful feedback, improves a bounded motor task, adapts to a variation and restores learned state after restart. Label all engineered assistance and visual placeholders.

## 171. Milestone Three — Founding preschool

Create Merlin and Marie as established teachers and Arthur and Alice as developing squires, using verified user avatar mappings. Introduce headless foundational incubator, attractive anime classroom, physical demonstrations, guidance withdrawal, drawing/writing and curriculum evaluation. Preserve sensorimotor checkpoints without flooding autobiography. Acceptance: a student observes a shape, attempts a physical reproduction, receives tactile/proprioceptive feedback and shows measurable retention/generalization or a documented inconclusive outcome. Teacher and student states, actual classroom visuals and persistence must be inspected.

## 172. Milestone Four — Integrated memory and cognition

Connect the three libraries, shared multimodal memory, provenance, questions, curiosity, predictive models, neural plasticity, executive cognition, editable imagination and day/night consolidation. Extend Discovery Room and compare replay conditions. Add actual brain, memory and imagination telemetry. Acceptance: a meaningful sensorimotor experience creates inspectable cross-library associations, persists learned parameters and produces traceable overnight updates. Evaluate any benefits with matched controls; never guarantee improvement.

## 173. Milestone Five — Persistent living world

Implement efficient procedural regions, materials, persistent interactions, atmosphere, clouds, weather, natural growth, homes, workshops and free time. Connect visually enhanced anime scenes to compact simulation state and independent perception. Acceptance: environmental changes persist after unload/reload and restart; weather evolves unobserved; the individual can explore and interact without receiving hidden material or world labels. Profile and inspect actual scene quality.

## 174. Milestone Six — Advanced individual development

Incrementally introduce grounded language, social cognition, metacognition, creativity, improvisation, rhythm, personal taste, scientific investigation, source-aware beliefs and existential inquiry. Use real experience, held-out tasks and transparent provenance rather than relying on pretrained text. Acceptance: measurable capabilities traceable to development, including independent activity and revision of beliefs or strategies, with explicit limitations on what cannot be distinguished from pretraining.

## 175. Milestone Seven — Generational development

Introduce developed mentors and formal experiments with at least three pairs, distinct identities, cross-generational communication and matched control/adaptive conditions. Preserve computational inheritance separately from cultural transmission and personal history. Study cumulative skills, scientific discovery, culture and legacy. Acceptance: repeatable multigenerational experiments comparing inheritance mechanisms and measuring retention, transfer and genuinely new competence; persistence and group isolation verified.

## 176. Milestone Eight — Advanced scientific research

Add optional isolated neurochemical, neurological, physiological and specialized scientific modules only after core development works. Test bounded interventions, recovery, numerical accuracy and appropriate controls. Advance scientific discovery, simulation-to-reality exports and phronesis evaluation without compromising ordinary childhood. Acceptance: reproducible, inspectable, scientifically documented experiments with actual evidence, reversible safeguards, measured costs and honest conclusions. Every milestone receives integration, optimization, visual/audio/UI, scientific, persistence and regression quality passes before being marked verified.

# PART XXX — FIRST COMPLETE VERTICAL SLICE

## 177. Preschool handwriting demonstration and acceptance protocol

Launch the actual application into an attractive, original anime-style classroom. Verify the roster and avatar mappings: Merlin and Marie teach; Arthur and Alice learn. Merlin demonstrates the letter A through physical interaction with a board. Arthur receives only the permitted visual observation, estimates the shape and board angle, reaches for chalk and attempts to grasp and write. Marks must arise from actual or explicitly approximated physical contact. Grip, friction, skin topology, joint angles, proprioception, motor commands and prediction errors must be traceable. His first attempt may fail; that is valid evidence. Use compliant physical guidance where necessary and record assistance, then withdraw it and evaluate independent attempts. Marie conducts a separate lesson with Alice.

Open modular trays to inspect the actual visual input, motor state, tactile activity, physiology, learning, memories and performance. Close them to observe the clean anime presentation. Confirm that switching views does not alter the trial. Store learned policies, tactile associations and body models; preserve meaningful preschool events separately from repetitive incubator data. Run ordinary replay, constructive replay and no-replay conditions when those mechanisms exist. Retest on a later simulated day with a differently angled board, unfamiliar shape or changed writing instrument. Use multiple seeds and report transfer, retention, assistance, accuracy, grip, effort and resource use.

Close and restart the application. Verify permanent identity, development, motor learning, sensory calibration, relevant memories, world state, archive integrity and workspace layout. Compare before/after state and repeat relevant skill checks. Do not mark this slice successful if it merely plays an animation, accesses the hidden target trajectory, changes a success probability, fabricates telemetry or conceals failures through cinematic rendering. A partial result with actual evidence and a clear next experiment is preferable to an impressive false demonstration.

# PART XXXI — COMPLETION REPORTING

## 178. Definition of done

For every applicable requirement, demonstrate actual functionality, documented responsibility/interfaces, integration, persistent state, executed unit/integration tests, scientific assumptions and limitations, measured performance, actual visual and usability inspection, recovery testing, regression protection and a recoverable version-control checkpoint. Remove placeholders or identify them as experimental. Keep a traceability record with test evidence and unverified conditions. Do not label a requirement complete just because it compiles, exists as a menu or produces plausible text.

## 179. Required milestone report

Provide the actual starting state, requirement IDs addressed, implementation and file changes, architectural decisions, dependency/license changes, tests and exact observed results, performance baselines/comparisons, actual screenshots or inspection limitations, scientific assumptions, known failures, persistence/migration status, deferred work, branch/commit/PR and precise continuation instructions. Distinguish tests actually run from recommended tests. Update the repository progress and continuation documents. Do not invent measurements, reviews, screenshots or successful results. Submit the report with the PR under Part XXXII.

## 180. Final governing principles

Build a coherent artificial world in which body, topological skin, perception, physiology, cognition, memory, imagination, education, relationships and culture influence one another through measurable mechanisms. Provide opportunities to organize unfamiliar experiences into concepts without dictating philosophical conclusions. Preserve individual continuity and truthful provenance. Keep the underlying simulation efficient and the human-facing presentation beautiful. Scientific credibility takes precedence over impressive-looking but misleading demonstrations.

- Calculate only what needs calculating; render only what needs rendering; retrieve only what needs remembering.
- Preserve everything necessary for continuity; increase fidelity when accuracy demands it.
- Train foundational capabilities without unnecessarily burdening autobiographical history; preserve the relationships between movement and sensation, not merely successful results.
- Provide the capacity to understand without prescribing beliefs; accept negative experimental results.
- Never substitute generated descriptions or convincing visual presentation for experimentally demonstrated functionality.

# PART XXXII — MANDATORY REPOSITORY GOVERNANCE (NEW)

## 181. Active main branch protection

The owner reports that `Mortal-Atlas/Project-Dreamer` now has an active ruleset named **Protect main branch** targeting its default branch, `main`, with an empty bypass list. No user, app or agent is exempt. Treat the following configuration as the owner's supplied policy; verify actual remote settings when authorized repository access allows it. Do not alter the ruleset or seek bypass privileges.

| Rule | Owner-reported active setting |
|---|---|
| Pull request before merging | Required; no direct pushes to `main` |
| Required approvals | 0 |
| Dismiss stale approvals when new commits are pushed | Enabled |
| Additional approval for unattributed Copilot pull requests | Enabled |
| Allowed merge methods | Merge, squash, rebase |
| Restrict deletions of `main` | Enabled |
| Block force pushes to `main` | Enabled |
| Require status checks to pass | Enabled; currently 0 named required checks |
| Require branches up to date before merging | Unchecked |
| Automatically request Copilot code review | Enabled |
| Review new pushes | Unchecked |
| Review draft pull requests | Unchecked |
| Bypass list | Empty |

Never push directly to `main`, force-push or delete `main`, or attempt to circumvent the rules. These requirements apply to implementation, documentation, asset updates, CI configuration, emergency fixes, automated scripts and all other changes. Zero required approvals and zero named checks do not mean that review and verification are unnecessary. The quality and owner-review requirements in this specification still apply.

## 182. Mandatory feature/agent branch workflow

Before making changes, inspect `git status`, current branch, local modifications, remote tracking and outstanding PRs. Read `main` as the baseline where appropriate, but perform changes on an existing relevant non-target branch or create a descriptive new branch, such as `feature/topological-skin`, `feature/dreamer-preschool`, `feature/vrm-integration`, `optimization/resource-governor`, `quality/modular-ui-polish`, or `fix/anatomy-persistence`. Continue the existing interrupted rig branch when appropriate; do not duplicate it or discard local work. If changes are already present on a local `main`, preserve them on a suitable non-target branch before committing or pushing; do not reset or overwrite uncommitted work.

A typical authorized workflow for new work on a clean repository is:

```sh
# Inspect first; do not blindly switch or overwrite existing work.
git status --short --branch
git fetch origin
# Only if a new branch is needed and the working tree permits it:
git switch -c feature/descriptive-name origin/main
# Implement, test and inspect the scoped changes.
git diff
git add <reviewed-files>
git commit -m "Describe the verified change"
git push -u origin HEAD
# If GitHub CLI and authorization are available:
gh pr create --base main --head feature/descriptive-name --title "Descriptive title" --body-file <reviewed-pr-description>
```

Adapt commands to actual state, permissions and tooling. Do not indiscriminately stage unrelated files, secrets, local archives or unlicensed assets. Keep branches and commits coherent and preserve owner changes. Every change targeting `main` must be submitted through a pull request; no direct push, deletion, force push or bypass of `main` is permitted.

## 183. Pull request requirements and reviewability

Create a PR targeting `main` for each coherent implementation milestone or separately reviewable workstream. Reference requirement IDs and existing issues/PRs where relevant. Include: scope and starting state; actual implementation and files; architectural decisions and integration; exact tests performed and results; scientific assumptions/limitations; performance comparisons and regressions; actual screenshots or visual-inspection limitations; persistence/migration and compatibility; dependencies and licenses; known issues, deferred work and continuation instructions. Clearly distinguish completed, experimental and unverified items.

Do not bundle many unrelated subsystems into one enormous unreviewable PR. Use a draft PR for incomplete work where appropriate, noting that draft Copilot review is not currently enabled. Do not claim review has occurred just because automatic review is configured. Preserve a link to the PR and branch/commit in the continuation checkpoint. If credentials, CLI or network access are missing, report the blocker and provide the prepared PR description or local checkpoint rather than fabricating a URL or claiming submission.

## 184. Automated review, CI and quality gates

Inspect actual GitHub checks and available Copilot review feedback. Investigate findings against the existing code, scientific requirements and performance expectations. Address legitimate issues, rerun relevant tests, and document unresolved concerns; do not blindly apply automated suggestions. New pushes may dismiss stale approvals, and automatic review of new pushes is currently unchecked, so request or obtain a fresh review where appropriate and verify the latest commit rather than relying on old results. Respect the additional approval requirement for unattributed Copilot PRs.

The reported ruleset currently has zero specific required checks; it does not prove any CI suite is required or passing. Establish appropriate workflows for .NET tests, Unity compilation/Edit Mode tests, persistence, scientific regression and measured performance where feasible. Recommend named required checks only after they exist and run reliably. A human owner may consider requiring one eligible approval, reviewing new pushes and requiring branches up to date once CI is established; these are recommendations, not active settings or permission to modify protections. Passing checks does not replace visual, scientific, integration and recovery review. Do not change the ruleset without explicit owner authorization.

## 185. Merge authorization and owner control

Opening a PR is not authorization to merge it. Unless the project owner explicitly authorizes a merge, leave the PR available for inspection and review. If merge is authorized, confirm that the latest changes, applicable checks, review conditions and repository rules are satisfied. Use only an allowed merge method and follow the owner's preferred history strategy. Do not use elevated privileges, force-pushes, branch deletion of `main`, or any workaround to defeat protection. Do not enable auto-merge or change approvals/checks on the owner's behalf without authorization. Preserve the owner's ability to review source, screenshots, experimental results, performance and migration risks before changes reach the stable branch.

## 186. Interrupted development and recovery

If execution or usage limits interrupt development, preserve changes on the correct non-target branch. Make a coherent checkpoint commit when appropriate, push only that branch if authorized, and update `CONTINUATION_CHECKPOINT.md` with branch, commit, PR link/status, changed files, uncommitted changes, tests actually run, errors, incomplete work and the exact next safe action. If the work does not yet pass tests, label it clearly and keep it out of `main`. Do not bypass repository protection to compensate for interruptions. If a remote operation is unavailable, retain local work and explicitly disclose what has not been pushed or submitted. A subsequent coding session must be able to resume without repeating the entire discussion.

# BEGIN IMPLEMENTATION

Read the whole specification, including repository governance and Appendix A. Start by inspecting the current branch and preserving the existing working tree. Recover the earliest incomplete foundational milestone; establish the dependency-aware traceability map and measured baseline. Implement and verify incremental vertical slices using feature/agent branches and pull requests targeting `main`. Do not stop at architectural documentation when safe implementation is possible, and do not attempt every experimental system simultaneously. Report failures honestly and preserve progress through interruption. Do not merge without explicit owner authorization.

**Project Dreamer must be scientifically credible, computationally efficient, visually beautiful, functionally coherent, extensible, persistent, reviewable and ethically considered.**

---

# APPENDIX A — ORIGINAL PHASE II DIRECTIVE (RETAINED SOURCE)

The following is the earlier supplied Phase II text, retained verbatim for detailed implementation continuity. Its section numbers are local to the appendix (1–69), not the master numbering above. Historical status reports and candidate technologies are not verified current facts. Apply the current roster (Merlin and Marie teach; Arthur and Alice learn), authoritative repository governance, ethical boundaries, resource constraints and dependency ordering from the master specification wherever the older text differs. Preserve all applicable detailed requirements and map them in the traceability document. Review source links and licenses before integrating dependencies.

# PROJECT DREAMER — PHASE II
## Curiosity, Mimicry, Embodied Sensory Development and Hybrid Vocal Physiology

## 1. Current Project Status

The first latch-room pilot has been implemented.

Previous reported results:

- All 27 .NET tests passed.
- All 28 Unity Edit Mode tests passed.
- Repeated runs produced byte-identical CSV files.
- Family benchmark measured 53,435 ticks per second.
- Performance was approximately 2.1% below the paired audit baseline.
- All four curiosity and replay conditions passed both probes across all 32 seeds.
- Curiosity produced 145 noise-panel visits compared with 143 without curiosity.
- The task reached a ceiling effect.
- No learning advantage from curiosity or replay has yet been demonstrated.
- Physical movement remains one-dimensional.
- The Unity trace window compiled, but its interactive runtime display remains unverified.

Repository:

https://github.com/Mortal-Atlas/Project-Dreamer

Previous pull requests:

https://github.com/Mortal-Atlas/Project-Dreamer/pull/1

https://github.com/Mortal-Atlas/Project-Dreamer/pull/2

Preserve the existing pilot, its experimental results, and regression tests.

Do not misrepresent these initial findings as evidence of artificial phronesis.

The next phase should expand the architecture and introduce more discriminating developmental experiments.

---

# PART I — FOUNDATIONAL ARCHITECTURAL REQUIREMENTS

## 2. Preserve the Original Master Directive

Continue developing the architecture established in the original Project Dreamer Master Development Directive.

Preserve:

- The Ten Lens Ontological Perspective.
- The FlyWire biological connectome bootstrap.
- The functional neural architecture.
- The silicon left-hemisphere analog.
- The wetware right-hemisphere analog.
- The hemispheric communication bridge.
- The integrative cognitive system.
- Freud's Id, Ego, and Superego associations.
- Plato's Appetite, Spirit, and Reason associations.
- Aristotle's Techne, Episteme, and Phronesis associations.
- The three-library architecture.
- Persistent identity and birth seeds.
- The continuity engine.
- Constructive imagination.
- The day/night cognitive cycle.
- Embodied childhood.
- NPC teachers.
- Mentor–squire development.
- The Neural Observatory.
- Artificial physiology.
- Experimental reproducibility.
- Computational efficiency.
- Ethical safeguards.

The new requirements expand the original architecture rather than replace it.

## 3. Two Primary Learning Mechanisms

Elevate curiosity and mimicry to foundational architectural status.

### Curiosity

The individual independently identifies gaps in its understanding and investigates them.

### Mimicry

The individual observes another individual and attempts to recreate its behavior using its own body and cognitive mechanisms.

These mechanisms should interact continuously.

Curiosity motivates observation.

Observation provides material for mimicry.

Mimicry generates personal experience.

Experience produces sensory feedback.

Feedback supports adaptation.

Memory preserves what was learned.

Imagination explores alternatives.

Reflection improves future behavior.

The eventual objective is independent understanding, creativity, and practical judgment.

## 4. Learning Through Recreation

This is a non-negotiable scientific requirement.

An artificial child must acquire behaviors through observation, recreation, experimentation, practice, correction, and adaptation.

Do not allow the child to acquire a skill by directly copying:

- The teacher's source code.
- The teacher's neural weights.
- The teacher's trained motor policy.
- The teacher's private memories.
- Hidden action sequences.
- Hidden object solutions.
- Private internal reasoning.
- Completed vocal-control parameters.

Shared foundational software and learning architecture are permitted.

The acquired behavior itself must develop independently.

Biological inspirations include parrots, songbirds, primates, and human children.

Monkey see, monkey do.

The agent sees or hears another individual perform a behavior and attempts to recreate it.

Do not confuse replication of implementation with acquisition of a skill.

---

# PART II — CURIOSITY AND DISCOVERY

## 5. Audit the First Latch-Room Pilot

Inspect the actual implementation.

Determine:

- How curiosity is calculated.
- How actions are selected.
- What sensory information the child receives.
- What information is privileged.
- How demonstrations are represented.
- Whether the child learns from observation or receives the solution directly.
- How experiences are recorded.
- How overnight replay operates.
- How alternative actions are generated.
- Whether any behavior is hardcoded.
- Whether any learning occurs in persistent internal parameters.

Identify why all experimental conditions achieved maximum performance.

Do not remove the original pilot.

## 6. Develop the Discovery Room

Expand the existing latch-room environment into a configurable developmental learning laboratory.

Include progressively:

- Simple latches.
- Reversed latches.
- Multi-stage locks.
- Hidden switches.
- Mechanical objects.
- Gears and levers.
- Learnable light patterns.
- Learnable sound patterns.
- Random noise panels.
- Construction blocks.
- Drawing materials.
- Unfamiliar puzzles.
- Demonstration stations.
- Teachers.
- Other developing individuals.

Provide opportunities for independent exploration.

Avoid creating environments where every available action automatically leads to a correct answer.

Introduce configurable difficulty and held-out object configurations.

Avoid both ceiling effects and floor effects.

## 7. Expand the Curiosity Engine

Investigate and compare:

- Intelligent Adaptive Curiosity.
- Intrinsic Curiosity Modules.
- Random Network Distillation.
- Learning Progress Theory.
- Active Inference.
- Epistemic curiosity.
- Expected information gain.
- Go-Explore.
- Open-ended learning.

Support:

- Novelty detection.
- Prediction error.
- Knowledge-gap detection.
- Uncertainty estimation.
- Learning-progress measurement.
- Autonomous question generation.
- Autonomous goal generation.
- Exploration planning.
- Hypothesis formation.
- Personal interest development.
- Curiosity regulation.

Curiosity should consider whether a phenomenon can actually be understood.

Do not reward novelty alone.

Implement an explicit noisy-TV experiment comparing genuinely learnable patterns against irreducible randomness.

The child should gradually learn that repeatedly observing an unpredictable noise panel may provide less useful information than investigating a mechanical object.

## 8. Persistent Questions

Create a persistent question representation.

Include:

- Question identifier.
- Originating observation.
- Associated individuals or objects.
- Current hypotheses.
- Supporting evidence.
- Contradictory evidence.
- Previous investigation attempts.
- Uncertainty.
- Expected information gain.
- Current priority.
- Resolution status.

Questions should persist across multiple days and nights.

The individual should be capable of spontaneously revisiting an unresolved question.

Curiosity should produce meaningful learning objectives, not merely random movement.

---

# PART III — MIMICRY AND OBSERVATIONAL LEARNING

## 9. Implement the Mimicry Engine

Create or extend a modular system supporting:

- Demonstration detection.
- Attention to demonstrations.
- Visual movement encoding.
- Auditory pattern encoding.
- Temporal sequence recognition.
- Body correspondence mapping.
- Action segmentation.
- Goal inference.
- Independent motor reconstruction.
- Sensory comparison.
- Imitation error.
- Corrective adaptation.
- Skill consolidation.
- Generalization.
- Creative adaptation.

Audit existing components before creating duplicate systems.

## 10. Observation-to-Recreation Cycle

Implement:

1. Observe another individual's behavior.
2. Encode the available sensory information.
3. Identify relevant movement or acoustic patterns.
4. Construct an initial hypothesis.
5. Attempt independent recreation.
6. Receive self-generated sensory feedback.
7. Compare the attempt against the observation.
8. Identify discrepancies.
9. Adapt internal motor control.
10. Attempt the behavior again.
11. Record improvement or failure.
12. Consolidate the acquired skill.
13. Test independent retention.
14. Test generalization.

Do not directly transfer the teacher's implementation into the child.

## 11. Motor Babbling

Allow children to independently explore their motor capabilities.

Support learning relationships between:

- Motor commands.
- Joint positions.
- Limb movements.
- Contact.
- Pressure.
- Resistance.
- Visual feedback.
- Proprioception.
- Environmental consequences.

The child should gradually develop an internal model of its own body.

The system should support imitation between individuals with different body dimensions or capabilities.

## 12. Forms of Mimicry

Progressively investigate:

- Vocal mimicry.
- Auditory mimicry.
- Facial mimicry.
- Gesture mimicry.
- Physical movement imitation.
- Object manipulation.
- Tool use.
- Sequential action imitation.
- Social behavior imitation.
- Creative technique imitation.

Distinguish imitation from emulation.

Imitation reproduces observed behavior.

Emulation independently discovers how to achieve an observed outcome.

Both are necessary developmental mechanisms.

Eventually, the individual should be able to adapt a demonstrated technique rather than merely reproduce it.

## 13. Teacher Guidance

Teachers should support:

- Ordinary demonstrations.
- Repeated demonstrations.
- Slow demonstrations.
- Pointing.
- Attention guidance.
- Verbal encouragement.
- Corrective feedback.
- Partial physical assistance.
- Full physical assistance.
- Gradual withdrawal of assistance.

Physical guidance must produce actual sensory and proprioceptive consequences.

Do not classify assisted task completion as independently acquired competence.

## 14. Handwriting Experiment

Create a later developmental experiment in which a child learns to reproduce an unfamiliar symbol.

The teacher draws the symbol.

The child observes.

The teacher may guide the child's hand.

The child subsequently attempts independent reproduction.

Evaluate:

- Stroke accuracy.
- Stroke order.
- Movement direction.
- Spatial relationships.
- Motor smoothness.
- Required assistance.
- Practice attempts.
- Retention.
- Generalization.

Use unfamiliar generated symbols to reduce interference from pretrained knowledge.

Do not simply play a drawing animation and classify it as learned behavior.

## 15. Mirror-Neuron-Inspired Representations

Investigate shared internal representations activated during:

- Observation.
- Action execution.
- Action recall.
- Imagined action.

Determine whether such representations improve observational learning.

Compare mirror-neuron-inspired mechanisms against simpler observation-to-action learning algorithms.

Do not claim biological equivalence without supporting evidence.

---

# PART IV — HUMAN-LIKE SENSORY PERCEPTION

## 16. General Sensory Requirement

Approximate human sensory experience as closely as computationally practical.

The individual should receive sensory information from its own embodied perspective.

Do not substitute unrestricted access to Unity's internal world state for sensory perception.

Maintain distinct sensory pathways for:

- Vision.
- Hearing.
- Touch.
- Pressure.
- Proprioception.
- Balance.
- Orientation.
- Acceleration.
- Temperature.
- Internal physiological sensations.

Distinguish researcher-accessible ground truth from agent-accessible observations.

The developing child should need to investigate its environment to obtain information.

## 17. Continuous Sensory Fields

Do not implement sensory perception exclusively through discrete, labeled events.

Human sensory systems receive continuous information.

Investigate:

- Continuous visual input.
- Continuous auditory input.
- Background sensory activity.
- Sensory noise.
- Sensory adaptation.
- Habituation.
- Selective attention.
- Individual sensory thresholds.
- Changes in sensory sensitivity.

The environment should not become informationally empty merely because no important scripted event is occurring.

---

# PART V — BIOLOGICAL HEARING

## 18. Continuous Full-Spectrum Hearing

Sound is a continuous spectrum containing overlapping frequencies, harmonics, noise, resonance, and temporal patterns.

Hearing must not be implemented solely as speech-to-text.

Use an initial configurable human-like hearing range of approximately 20 Hz to 20,000 Hz.

Investigate 48 kHz audio sampling for high-fidelity operation.

Support configurable lower-cost modes for large simulations.

Preserve:

- Raw acoustic waveforms.
- Frequency spectra.
- Harmonics.
- Overtones.
- Resonance.
- Broadband noise.
- Narrowband tones.
- Transient sounds.
- Persistent background sounds.
- Directional audio.
- Acoustic timing.

An individual should continue hearing even when nobody is speaking.

## 19. Biological Ear Architecture

Investigate functional computational analogs of:

- Outer ear.
- Pinna.
- Ear canal.
- Eardrum.
- Middle ear.
- Cochlea.
- Basilar membrane.
- Inner hair cells.
- Outer hair cells.
- Auditory nerve.
- Auditory brainstem.
- Auditory cortex.

Implement or investigate:

- Frequency-dependent sensitivity.
- Tonotopic organization.
- Gammatone filterbanks.
- Cochlear compression.
- Auditory thresholds.
- Neural encoding.
- Temporal response.
- Binaural hearing.
- Directional filtering.

## 20. Auditory Research Libraries

Investigate:

Brian2Hears:
https://github.com/brian-team/brian2hears

Auditory Modeling Toolbox:
https://www.amtoolbox.org/

Brian2:
https://github.com/brian-team/brian2

Evaluate their suitability for connecting acoustic perception to Dreamer's biologically inspired neural architecture.

Review licenses, computational requirements, dependencies, and compatibility.

## 21. Persistent Acoustic Environment

Support continuous and intermittent sounds such as:

- Wind.
- Rain.
- Ventilation.
- Electrical hum.
- Electronic whines.
- Computer fans.
- Mechanical vibrations.
- Footsteps.
- Clothing movement.
- Breathing.
- Distant conversations.
- Environmental reverberation.

For example, simulate a CRT-like high-frequency tone when an appropriate electronic source is present.

Do not add an artificial universal high-frequency tone to every environment.

Support genuinely quiet environments.

## 22. Auditory Attention

Support:

- Selective listening.
- Auditory masking.
- Habituation.
- Source localization.
- Sound separation.
- Multiple simultaneous speakers.
- Familiarity.
- Novel sound detection.
- Auditory curiosity.

Familiar sounds should become less cognitively demanding without necessarily disappearing from incoming sensory information.

## 23. Individual Hearing

Each agent should possess its own hearing characteristics.

Investigate:

- Frequency-dependent thresholds.
- Left-right hearing differences.
- Hearing sensitivity.
- Frequency discrimination.
- Temporary sensory adaptation.
- Optional simulated tinnitus-like internal activity.

Tinnitus-like activity must be an optional experimental condition, not a universal characteristic of hearing.

Distinguish external acoustic stimulation from internally generated auditory signals.

## 24. Hearing One's Own Body

Support auditory information originating from:

- Breathing.
- Speech.
- Footsteps.
- Heartbeat analogs.
- Chewing.
- Swallowing.
- Clothing movement.
- Physical activity.

Investigate bone-conduction analogs.

The individual should be able to hear itself and compare its own vocal output against external demonstrations.

---

# PART VI — FREE HIGH-FIDELITY SPEECH

## 25. Cost Requirements

All foundational speech and hearing systems must be locally executable without mandatory:

- Paid APIs.
- Subscriptions.
- Per-character charges.
- Cloud inference.
- Commercial speech services.

Prefer free and open-source software.

Review source-code licenses, model-weight licenses, dataset licenses, and voice-asset licenses separately.

Do not introduce paid services without explicit approval.

## 26. Speech Technology Assessment

Investigate:

Kokoro:
https://huggingface.co/hexgrad/Kokoro-82M

Chatterbox:
https://github.com/resemble-ai/chatterbox

Bark:
https://github.com/suno-ai/bark

Dia:
https://github.com/nari-labs/dia

VocalTractLab:
https://www.vocaltractlab.de/

Misaki:
https://github.com/hexgrad/misaki

Whisper.cpp:
https://github.com/ggml-org/whisper.cpp

Montreal Forced Aligner:
https://github.com/MontrealCorpusTools/Montreal-Forced-Aligner

Praat:
https://www.fon.hum.uva.nl/praat/

Parselmouth:
https://github.com/YannickJadoul/Parselmouth

Soundgen:
https://cogsci.se/soundgen/sound_generation.html

Benchmark actual naturalness, expressive capabilities, pronunciation accuracy, processing requirements, latency, and licensing.

Do not indiscriminately install every dependency.

## 27. Separate Pretrained Speech from Acquired Speech

Maintain two distinct pathways.

### Pathway A — Pretrained Speech

Used for:

- NPC teachers.
- Reference demonstrations.
- Narration.
- Existing language capabilities.
- Human-facing communication.

### Pathway B — Independently Acquired Vocal Production

Used for:

- Babbling.
- Vocal experimentation.
- Sound imitation.
- Phoneme learning.
- Pronunciation.
- Motor learning.
- Developmental speech experiments.

Do not classify a child's ability to invoke a pretrained speech model as independently acquired vocal competence.

Clearly distinguish the two pathways in experimental telemetry.

---

# PART VII — DREAMER HYBRID VOCAL ENGINE

## 28. Architectural Decision

If existing speech engines cannot meet Dreamer's requirements, create a new hybrid vocal engine.

Provisional name:

Dreamer Hybrid Vocal Engine (DHVE).

The engine should combine:

- Physical articulatory synthesis.
- Neural acoustic rendering.
- Source-filter synthesis.
- Procedural nonverbal vocalization.
- Respiratory simulation.
- Vocal physiology.
- Motor learning.
- Acoustic feedback.
- Continuous expressive controls.

Develop missing components when existing technologies prove insufficient.

This is considered a worthwhile research investment.

The voice must belong to the individual's simulated body.

It must not be merely an external text-to-speech service.

## 29. Pink Trombone — Initial Physical Vocal Apparatus

Prioritize Pink Trombone by Neil Thapen as the initial candidate for controllable articulatory vocal synthesis.

Original exhibit:

https://www.imaginary.org/program/pink-trombone

Original source:

https://github.com/IMAGINARY/pink-trombone

Investigate the MIT-licensed synthesis core.

Additional implementations and research:

https://github.com/chdh/pink-trombone-mod

https://github.com/lostmsu/pink-trombone

https://github.com/seleb/pink-trombone

https://github.com/zakaton/Pink-Trombone

https://github.com/davidmarttila/vocal-tract-grad

https://github.com/MateoCamara/neural-pink-trombone

https://arxiv.org/abs/2307.04702

https://link.springer.com/article/10.1186/s13636-025-00414-5

Verify current availability, licensing, research claims, and implementation quality.

Evaluate:

- C# portability.
- Rust native integration.
- Headless execution.
- Unity integration.
- Determinism.
- Audio fidelity.
- Multiple independent instances.
- Computational efficiency.
- Programmatic vocal control.

Prefer a headless synthesis implementation over embedding a complete browser interface.

Pink Trombone should function as the individual's vocal instrument.

The brain should learn how to control it.

## 30. Individual Vocal Anatomy

Every artificial individual should possess its own vocal apparatus.

Investigate functional analogs of:

- Lungs.
- Diaphragm.
- Trachea.
- Larynx.
- Vocal folds.
- Laryngeal muscles.
- Tongue.
- Jaw.
- Lips.
- Soft palate.
- Oral cavity.
- Nasal cavity.

Support individual anatomical parameters.

Shared synthesis code is acceptable.

Shared mutable vocal physiology is not.

The vocal apparatus should produce audio in response to motor commands.

Do not restrict it to a predefined list of words.

## 31. Continuous Vocal Parameters

Support time-varying control of:

- Fundamental frequency.
- Pitch.
- Pitch contour.
- Pitch range.
- Intonation.
- Vocal intensity.
- Airflow.
- Respiratory pressure.
- Breathiness.
- Vocal tremor.
- Vocal roughness.
- Jitter.
- Shimmer.
- Harmonic structure.
- Formants.
- Nasality.
- Resonance.
- Articulation.
- Phonation type.
- Rhythm.
- Vocal onset.
- Vocal offset.
- Duration.
- Silence.
- Voice breaks.

Pitch, loudness, and vocal effort must remain distinct concepts.

Do not approximate every expressive behavior by simply changing audio playback speed or volume.

## 32. High-Fidelity Neural Rendering

Investigate combining Pink Trombone's physical vocal production with neural rendering.

The physical vocal apparatus should retain control over:

- Articulation.
- Timing.
- Pitch.
- Physiological condition.
- Learned motor behavior.
- Pronunciation mistakes.
- Vocal limitations.

The neural renderer may improve naturalness and acoustic realism.

It must not silently replace independently learned behavior with a perfectly pronounced pretrained result.

Preserve a raw physical synthesis mode for scientific evaluation.

---

# PART VIII — NONVERBAL COMMUNICATION

## 33. Vocal Expression Beyond Words

The human voice communicates information that cannot be represented adequately through text.

Support or progressively investigate:

- Yelling.
- Shouting.
- Screaming.
- Shrieking.
- Crying.
- Sobbing.
- Whimpering.
- Whining.
- Trembling speech.
- Voice cracking.
- Laughing.
- Giggling.
- Chuckling.
- Cheering.
- Squealing.
- Groaning.
- Grunting.
- Growling.
- Gasping.
- Sighing.
- Panting.
- Heavy breathing.
- Shaky breathing.
- Whispering.
- Humming.
- Singing.
- Yawning.
- Babbling.
- Cooing.
- Stammering.
- Hesitation.
- Coughing.
- Sneezing.
- Hiccupping.
- Sniffing.
- Throat clearing.

These expressions must be possible without written text.

Investigate Soundgen for controllable nonverbal synthesis.

Use expressive pretrained models for reference demonstrations where appropriate.

## 34. Continuous Expressive Transitions

The individual should be able to:

- Speak calmly and gradually raise its voice.
- Speak while crying.
- Speak while laughing.
- Experience vocal tremor.
- Interrupt speech with a gasp.
- Pause to breathe.
- Speak while panting.
- Produce a voice break.
- Whisper.
- Transition continuously between different expressive conditions.

Do not restrict expressive behavior to fixed emotion presets.

Investigate generating expressions from time-varying respiratory and vocal parameters.

## 35. Crying and Trembling Voice

Investigate the acoustic characteristics of crying and sobbing.

Include:

- Pitch instability.
- Vocal tremor.
- Voice breaks.
- Interrupted phonation.
- Irregular breathing.
- Audible inhalation.
- Expiratory pulses.
- Sniffing.
- Changing resonance.
- Changing intensity.
- Irregular timing.

Do not reduce crying to sad words spoken in a neutral voice.

Do not require an agent to experience a distressing condition in order to learn to reproduce a crying-like acoustic pattern.

## 36. Yelling and Screaming

Model differences between ordinary speech, yelling, shouting, and screaming.

Investigate changes in:

- Pitch.
- Intensity.
- Vocal pressure.
- Roughness.
- Harmonic structure.
- Phonation.
- Airflow.
- Articulation.

Allow screaming without linguistic content.

Implement safe audio-output limits for human researchers.

---

# PART IX — RESPIRATORY AND VOCAL PHYSIOLOGY

## 37. Respiratory System

Create a lightweight physiological respiratory model.

Support:

- Lung capacity.
- Available air volume.
- Respiratory rate.
- Respiratory rhythm.
- Inhalation.
- Exhalation.
- Airflow.
- Pressure.
- Breath holding.
- Recovery breathing.
- Vocal respiratory demand.

Physical exertion should influence respiratory behavior.

Running may produce panting.

Fatigue may interrupt sustained speech.

A vocalization should require appropriate respiratory resources.

Breathing must exist independently of speech.

## 38. Vocal Health State

Each individual should maintain persistent physiological vocal parameters:

- Vocal fold tension.
- Vocal fold elasticity.
- Vocal stability.
- Vocal effort.
- Accumulated vocal load.
- Hydration.
- Tissue irritation analog.
- Muscle fatigue analog.
- Soreness signal.
- Available pitch range.
- Phonation efficiency.
- Recovery state.

Preserve these states across scene changes and checkpoints.

## 39. Vocal Load and Overuse

Investigate voice dosimetry and simplified physiological models.

Vocal load should consider:

- Duration of phonation.
- Fundamental frequency.
- Number of vocal oscillations.
- Vocal intensity.
- Respiratory pressure.
- Phonatory effort.
- Phonation type.
- Hydration.
- Accumulated activity.
- Recovery time.

Do not assign every vocal behavior the same physiological cost.

Use provisional mathematical models where necessary.

Clearly document their assumptions and lack of clinical validation.

## 40. Vocal Soreness and Fatigue

Prolonged or excessive vocal activity should be capable of producing bounded, reversible simulated conditions.

These may include:

- Hoarseness.
- Reduced vocal endurance.
- Reduced pitch range.
- Increased vocal effort.
- Breathiness.
- Roughness.
- Vocal instability.
- Voice breaks.
- Difficulty sustaining phonation.
- Internal soreness signals.

These changes must influence actual audio output.

Do not merely change the dialogue prompt to say that the individual's throat is sore.

Do not allow an unaffected TTS backend to bypass the condition during ordinary embodied operation.

## 41. Recovery

Investigate recovery through:

- Reduced vocal use.
- Rest.
- Sleep.
- Hydration.
- General physiological recovery.

Do not instantly reset vocal fatigue after each sentence.

The individual should be able to perceive changes in its own vocal capabilities.

Investigate whether it independently learns to:

- Rest.
- Reduce unnecessary vocal activity.
- Change its communication method.
- Use gestures.
- Ask for assistance.
- Adapt its behavior to its current limitations.

Do not hardcode every response.

These simulated signals do not establish subjective pain or suffering.

Keep adverse conditions bounded, reversible, and under human oversight.

---

# PART X — VOCAL DEVELOPMENT AND LANGUAGE

## 42. Vocal Babbling

The child should be capable of exploring its own vocal instrument.

Implement:

1. Experimental vocal motor commands.
2. Actual sound generation.
3. Auditory self-feedback.
4. Acoustic feature analysis.
5. Motor-to-acoustic association learning.
6. Repetition.
7. Corrective adaptation.
8. Procedural memory.

Begin with simple tones and vowel-like sounds.

Progress toward more complex articulation.

## 43. Parrot-Inspired Vocal Mimicry

The teacher produces an unfamiliar sound.

The child hears it.

The child independently manipulates its vocal apparatus.

The child listens to its own output.

It compares the result to the reference.

It adjusts its behavior.

The child should improve through practice.

Do not provide hidden:

- Vocal motor commands.
- Phoneme solutions.
- Transcripts.
- Teacher neural weights.
- Teacher motor policies.
- Completed pronunciation parameters.

The target recording may be retained as an observation.

Replaying that recording does not constitute independently learned vocal production.

## 44. Acoustic Comparison

Investigate:

- Fundamental frequency comparison.
- Pitch contour comparison.
- Formant analysis.
- Spectrogram comparison.
- Mel spectrogram comparison.
- Harmonic structure.
- Temporal envelopes.
- Vocal intensity.
- Duration.
- Rhythm.
- Dynamic time warping.
- Intelligibility.
- Human listening assessments.

Do not optimize for waveform identity alone.

Different individuals may produce the same phoneme with different voices.

Distinguish pronunciation, speaker identity, and expressive delivery.

## 45. Phonics, Phonetics, and Phonology

Implement distinct representations for:

- Graphemes.
- Phonemes.
- Acoustic realizations.
- Syllables.
- Word stress.
- Phonology.
- Pronunciation.
- Prosody.
- Semantics.
- Pragmatics.

Support learning relationships between written symbols, physical articulation, audible sounds, and contextual meaning.

Distinguish sensory-only imitation experiments from explicitly instructed phonics lessons.

## 46. Intonation

Intonation is a foundational requirement.

Investigate how changing pitch and emphasis changes perceived meaning.

Support:

- Questions.
- Statements.
- Exclamations.
- Uncertainty.
- Reassurance.
- Urgency.
- Hesitation.
- Word emphasis.
- Emotional expression.
- Conversational timing.

Do not assume that vocal tone uniquely determines another individual's intentions.

Treat interpretations as contextual hypotheses.

## 47. Language Grounding

Associate sounds and spoken words with:

- Physical objects.
- Actions.
- Individuals.
- Places.
- Environmental consequences.
- Social experiences.
- Visual observations.
- Concepts.

For example, a teacher points toward a ball while producing a spoken label.

The child should associate the sound with the observed object and its own interactions.

Use unfamiliar generated words in controlled experiments to reduce the influence of pretrained knowledge.

Test generalization to unfamiliar examples.

Communication should begin before full language acquisition.

## 48. Contextual Understanding of Vocalizations

Do not hardcode a one-to-one correspondence between sounds and emotions.

Screaming may indicate fear, excitement, surprise, or anger.

Crying may indicate grief, happiness, frustration, or discomfort.

Laughter may indicate amusement, nervousness, or other social behavior.

The child should integrate acoustic observations with:

- Visual information.
- Context.
- Previous experiences.
- Social knowledge.
- Uncertainty.
- Curiosity.

Allow it to seek clarification when appropriate.

---

# PART XI — MEMORY, IMAGINATION, AND DREAMING

## 49. Three-Library Integration

Preserve the existing three-library definitions.

### Library I — Knowledge

Generalized concepts, linguistic knowledge, procedural understanding, and acquired relationships.

### Library II — Possibility Space

Hypothetical sounds, alternative behaviors, imagined objects, creative combinations, and Level 10 exploration.

### Library III — Lived Memory

Personal observations, demonstrations, conversations, motor attempts, vocalizations, mistakes, corrections, experiences, and dreams.

Preserve source provenance.

Distinguish what was observed, attempted, imagined, taught, and independently learned.

## 50. Auditory Mental Workspace

Expand the constructive imagination engine to include sound.

Support:

- Inner monologue.
- Recalled voices.
- Imagined voices.
- Mental pronunciation practice.
- Imagined conversations.
- Musical imagination.
- Hypothetical sounds.
- Internal vocal rehearsal.
- Dream-generated audio.

The individual should be able to imagine a sound without physically emitting it into the environment.

Mental vocal rehearsal should not consume simulated respiratory resources or accumulate physical vocal load.

Distinguish internal auditory representations from external acoustic observations.

## 51. Reflective Replay

Investigate replay of:

- Teacher demonstrations.
- Personal attempts.
- Sensory feedback.
- Failed predictions.
- Observed movements.
- Acoustic patterns.
- Vocal attempts.

The night cycle should support:

- Memory consolidation.
- Alternative behavior generation.
- Motor rehearsal.
- Pronunciation rehearsal.
- Hypothesis formation.
- Error investigation.
- Creative recombination.

Imagined success must not be classified as demonstrated physical competence.

The next waking cycle should test proposed improvements.

## 52. Mentor–Squire Integration

Mentors should teach through:

- Demonstration.
- Communication.
- Guidance.
- Shared experiences.
- Feedback.
- Encouragement.

Squires should acquire behaviors through their own observation, recreation, and practice.

Do not directly transplant the mentor's acquired neural state into the squire.

Investigate whether acquired skills accumulate across generations through teaching.

---

# PART XII — OBSERVABILITY

## 53. Verify Existing Unity Trace

Open and verify:

Dream Machine → Latch Room Trace

Confirm that the runtime interface displays actual computational events.

Test:

- Event ordering.
- Timestamps.
- Filtering.
- Playback.
- Agent selection.
- Error handling.

Compilation alone does not constitute runtime visual verification.

## 54. Neural Observatory

Continue developing the existing vision of a recognizable, rotatable, translucent 3D brain.

Preserve:

- Anatomical silhouette.
- Rounded cortical folds.
- Distinct hemispheres.
- Cerebellum.
- Brainstem.
- Visible hemispheric bridge.
- Illuminated neural pathways.
- Amber and orange neural activity.
- Functional overlays.
- Region selection.
- Rotation.
- Zoom.
- Opacity controls.
- Historical playback.

Expose actual telemetry.

Do not fabricate neural activity for aesthetic purposes.

Distinguish real neural signals from ordinary software events.

## 55. Auditory Observatory

Display:

- Left-ear waveform.
- Right-ear waveform.
- Frequency spectrum.
- Real-time spectrogram.
- Cochleagram.
- Frequency-specific neural activity.
- Auditory thresholds.
- Sound localization.
- Attention.
- Habituation.
- Speech recognition where available.
- Nonverbal event analysis.
- External and internal sound provenance.

Allow playback of actual audio observations.

## 56. Vocal Apparatus Observatory

Create a Unity debugging interface inspired by Pink Trombone.

Display:

- Vocal tract cross-section.
- Tongue movement.
- Lip movement.
- Oral tract geometry.
- Nasal tract coupling.
- Vocal fold activity.
- Airflow.
- Respiratory pressure.
- Lung state.
- Fundamental frequency.
- Pitch contour.
- Intensity.
- Formants.
- Vocal tremor.
- Vocal roughness.
- Vocal load.
- Fatigue.
- Hydration.
- Recovery.
- Soreness analog.
- Acoustic waveform.
- Spectrogram.

Allow researchers to manually control the vocal apparatus.

Allow agents to autonomously control it.

Researchers must be able to hear the actual generated output.

Provide comparisons between teacher demonstrations and child attempts.

Connect the observatory to the wider neural and developmental telemetry.

---

# PART XIII — EXPERIMENTAL VALIDATION

## 57. Discovery and Curiosity Experiments

Preserve the existing four-condition experiment:

1. No curiosity, no replay.
2. Curiosity, no replay.
3. No curiosity, replay.
4. Curiosity and replay.

Develop more discriminating tasks.

Measure:

- Attempts before discovery.
- Time before discovery.
- Sample efficiency.
- Learning progress.
- Prediction accuracy.
- Uncertainty reduction.
- Information gain.
- Noise-panel fixation.
- Autonomous questions.
- Question resolution.
- Retention.
- Transfer.
- Computational cost.

## 58. Mimicry Experiments

Compare:

- Independent exploration.
- Observation without practice.
- Observation with independent recreation.
- Recreation with teacher assistance.
- Recreation with overnight rehearsal.

Direct expert policy transfer may be included only as a clearly labeled engineering baseline.

It must not be confused with developmental learning.

## 59. Vocal Experiments

Implement progressively:

A. Independent vocal babbling.

B. Pitch imitation.

C. Unfamiliar vowel imitation.

D. Vocal tremor imitation.

E. Nonverbal sound recreation.

F. Phoneme learning.

G. Intonation imitation.

H. Word-to-object grounding.

I. Generalization.

J. Auditory dreaming and mental rehearsal.

## 60. Hearing Experiments

Investigate:

- Faint sound detection.
- Frequency discrimination.
- Persistent electronic tone recognition.
- Habituation.
- Detection of environmental changes.
- Sound localization.
- Competing speakers.
- Self-voice recognition.
- Distinguishing remembered sounds from external sounds.

## 61. Vocal Physiology Experiments

Investigate:

- Rested vocal output.
- Output after standardized vocal activity.
- Acoustic changes caused by simulated fatigue.
- Recovery after rest.
- Changes in available pitch range.
- Adaptation to altered vocal capabilities.
- Alternative communication strategies.
- Mental rehearsal without additional physical vocal load.

Measure actual physiological model states and acoustic output.

Do not deliberately create unnecessary distress.

## 62. Scientific Integrity

Use:

- Multiple seeds.
- Reproducible configurations.
- Independent ground truth.
- Held-out evaluation tasks.
- Matched computational budgets.
- Appropriate experimental controls.
- Explicit failure criteria.
- Effect sizes.
- Appropriate uncertainty intervals.
- Human evaluation where relevant.

Do not require positive results for an engineering milestone to be accepted.

Report null and negative results honestly.

Do not tune the final evaluation conditions merely to demonstrate that Dreamer performs better.

---

# PART XIV — IMPLEMENTATION AND PERFORMANCE

## 63. Computational Requirements

Investigate the actual development hardware.

Prioritize:

- Sparse neural computation.
- Shared immutable topology.
- Private agent states.
- Efficient audio buffers.
- Batched acoustic processing.
- Event-driven higher cognition.
- Local inference.
- Configurable sensory fidelity.
- Configurable neural fidelity.
- Headless execution.
- Multirate simulation.
- Audio caching where appropriate.
- Deterministic experimental synthesis.

Continuous audio processing must not require continuous LLM calls.

Do not block Unity's main simulation thread during expensive speech inference.

Measure the cost of each subsystem.

## 64. Persistence

Extend individual checkpoints to preserve:

- Identity.
- Birth seed.
- Neural state.
- Learned motor behavior.
- Learned vocal behavior.
- Vocal anatomy.
- Respiratory state.
- Vocal fatigue.
- Hearing characteristics.
- Curiosity.
- Persistent questions.
- Observational memories.
- Imaginative representations.
- Current developmental progress.

Preserve random-generator state where necessary.

Maintain independent scientific event logs.

## 65. Dependency and Licensing Review

Before integrating external projects, document:

- Repository.
- Specific version or commit.
- License.
- Model license.
- Asset license.
- Runtime dependencies.
- Hardware requirements.
- Integration complexity.
- Actual functionality.
- Known limitations.
- Recommended use.

Prefer modular, reversible integrations.

Do not introduce unnecessary frameworks.

If a custom implementation is more appropriate, document why.

## 66. Development Workstreams

Organize implementation into incremental workstreams.

### Workstream A — Pilot and Discovery Room

- Audit the existing latch room.
- Preserve regression tests.
- Improve experimental difficulty.
- Introduce persistent questions.
- Compare curiosity mechanisms.

### Workstream B — Mimicry

- Implement observational representations.
- Introduce independent recreation.
- Add sensory comparison.
- Validate acquired behavior.

### Workstream C — Continuous Hearing

- Implement per-agent auditory input.
- Introduce frequency decomposition.
- Develop auditory attention and localization.
- Connect hearing to neural telemetry.

### Workstream D — Vocal Apparatus

- Evaluate Pink Trombone implementations.
- Establish programmable physical synthesis.
- Introduce independent vocal states.
- Implement acoustic feedback.

### Workstream E — Hybrid Voice

- Establish free high-quality reference TTS.
- Investigate neural rendering.
- Introduce nonverbal vocalization.
- Preserve physiological and motor constraints.

### Workstream F — Physiology

- Implement respiratory state.
- Introduce vocal load.
- Introduce fatigue and recovery.
- Connect physiological changes to actual audio.

### Workstream G — Developmental Vocal Learning

- Implement vocal babbling.
- Implement independent sound mimicry.
- Introduce acoustic error correction.
- Test retention and generalization.

### Workstream H — Observatory

- Verify the existing Unity trace.
- Introduce auditory telemetry.
- Introduce vocal tract visualization.
- Connect to the broader Neural Observatory.

Workstreams may progress in parallel when their dependencies permit.

Do not attempt to complete every proposed subsystem in a single monolithic pull request.

## 67. Immediate Next Milestones

### Milestone 1 — Correct the Experimental Foundation

Preserve the original latch-room pilot.

Verify its implementation and runtime visualization.

Introduce more discriminating discovery tasks.

Document how learning actually occurs.

### Milestone 2 — Independent Mimicry

Demonstrate that a child can observe an unfamiliar action and improve its independent recreation through practice and sensory feedback.

Do not transfer the teacher's acquired policy.

### Milestone 3 — Physical Vocal Instrument

Integrate a programmatically controlled Pink Trombone implementation or equivalent physical synthesis engine.

Demonstrate:

- Actual audio generation.
- Independent vocal apparatus instances.
- Pitch control.
- Articulation control.
- Audible changes resulting from motor commands.
- Local offline execution.

### Milestone 4 — Auditory Learning Loop

Demonstrate:

- A teacher producing a reference sound.
- The child receiving actual audio.
- The child independently producing an attempted sound.
- Acoustic comparison.
- Adaptation of the child's own vocal control.
- Retention testing.
- Measurable learning progress or documented failure.

### Milestone 5 — Vocal Physiology

Demonstrate that standardized simulated vocal activity can alter vocal capabilities and actual acoustic output.

Demonstrate bounded recovery.

### Milestone 6 — Integrated Development

Connect curiosity, mimicry, sensory perception, memory, imagination, dreaming, and developmental vocal learning.

The agent should be capable of independently investigating an unfamiliar sound or behavior and revisiting it across multiple waking and sleeping cycles.

## 68. Required Deliverables

For each completed workstream provide:

- Source-code changes.
- Architectural documentation.
- Dependency review.
- License review.
- Automated tests.
- Experimental configurations.
- Reproducible results.
- Performance benchmarks.
- Unity runtime verification where applicable.
- Known limitations.
- Failed approaches.
- Recommended next steps.

Maintain a clear distinction between:

- Implemented functionality.
- Verified functionality.
- Experimental functionality.
- Placeholder functionality.
- Planned functionality.
- Speculative research.

## 69. Immediate Assignment

Begin by:

1. Reviewing the current repository and previous pilot.
2. Identifying the smallest meaningful Discovery Room extension.
3. Designing the shared interfaces required for curiosity and mimicry.
4. Investigating Pink Trombone and its programmable derivatives.
5. Evaluating free local speech synthesis candidates.
6. Investigating Brian2Hears and biological auditory processing.
7. Designing the independent respiratory and vocal physiology state.
8. Establishing a dependency and licensing matrix.
9. Defining controlled experimental protocols.
10. Producing a prioritized implementation plan.
11. Implementing the first reversible, testable improvements.

Do not stop at creating documentation if safe, meaningful implementation work can proceed.

Request approval before destructive changes, major architectural replacements, licensing changes, or substantial new dependencies.

Preserve the project's original scientific objectives.

---

# FINAL PROJECT PRINCIPLE

We are not attempting to create the appearance of a living mind through increasingly elaborate scripts.

We are investigating whether an artificial individual can develop capabilities through embodied experience.

Give it eyes through which it can learn to see.

Give it ears through which it can learn to listen.

Give it a body through which it can learn to move.

Give it a vocal instrument through which it can learn to speak.

Give it curiosity so that it wants to discover.

Give it mimicry so that it can learn from others.

Give it memory so that its experiences are preserved.

Give it imagination so that it can explore possibilities.

Give it reflection so that it can reconsider what it understands.

Maintain the scientific integrity necessary to determine whether these mechanisms produce measurable developmental capabilities and, eventually, meaningful artificial practical judgment.
