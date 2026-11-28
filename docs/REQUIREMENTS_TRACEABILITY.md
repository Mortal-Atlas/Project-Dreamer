# Requirements traceability

The [M01-M46 workstream register](MASTER_WORKSTREAM_TRACEABILITY.md) tracks the November 27 consolidated PDF. This register retains the detailed archival numbered evidence.

The source is the [consolidated specification](MASTER_IMPLEMENTATION_SPECIFICATION.md). The inventory covers master requirements 1–186 and original Phase II requirements A.1–A.69. Each source link opens the acceptance criteria in that section. Dependencies show the principal gates for each workstream. An earlier milestone may already supply experimental evidence for a later requirement.

**Verified** means the complete, applicable requirement shown in the row was observed. **Partial** means an implementation, policy, or recorded audit covers some of its criteria. **Experimental** is evidence from a bounded pilot. **Blocked** records a specific dependency in the evidence register. **Not started** means this audit identified no implementation that satisfies the core requirement. A dash means evidence or cost has yet to be established. Measurements apply only to the configuration stated in the evidence.

| Source and acceptance criteria | Owner | Principal dependencies | Status | Implementation, tests and cost |
| --- | --- | --- | --- | --- |
| [1 Fundamental objective](MASTER_IMPLEMENTATION_SPECIFICATION.md#1-fundamental-objective) | Project and research | — | Partial | [K](#k-evidence) |
| [2 Recover and preserve existing work](MASTER_IMPLEMENTATION_SPECIFICATION.md#2-recover-and-preserve-existing-work) | Project and research | — | Partial | [Z](#z-evidence) |
| [3 Persistent implementation documentation](MASTER_IMPLEMENTATION_SPECIFICATION.md#3-persistent-implementation-documentation) | Project and research | — | Partial | [Z](#z-evidence) |
| [4 Layered simulation architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#4-layered-simulation-architecture) | Core contracts | §168 | Partial | [K](#k-evidence) |
| [5 Authoritative state ownership](MASTER_IMPLEMENTATION_SPECIFICATION.md#5-authoritative-state-ownership) | Core contracts | §168 | Partial | [K](#k-evidence) |
| [6 Central simulation clock](MASTER_IMPLEMENTATION_SPECIFICATION.md#6-central-simulation-clock) | Core contracts | §168 | Partial | [K](#k-evidence) |
| [7 Typed inter-system communication](MASTER_IMPLEMENTATION_SPECIFICATION.md#7-typed-inter-system-communication) | Core contracts | §168 | Partial | [K](#k-evidence) |
| [8 Module registry and lifecycle](MASTER_IMPLEMENTATION_SPECIFICATION.md#8-module-registry-and-lifecycle) | Core contracts | §168 | Not started | — |
| [9 Correct founding character roster](MASTER_IMPLEMENTATION_SPECIFICATION.md#9-correct-founding-character-roster) | Identity and education | §§4–5, 57 | Partial | [V](#v-evidence) |
| [10 Persistent identity](MASTER_IMPLEMENTATION_SPECIFICATION.md#10-persistent-identity) | Identity and education | §§4–5, 57 | Partial | [S](#s-evidence) |
| [11 Teacher, mentor and squire responsibilities](MASTER_IMPLEMENTATION_SPECIFICATION.md#11-teacher-mentor-and-squire-responsibilities) | Identity and education | §§4–5, 57 | Partial | [T](#t-evidence) |
| [12 Seeded anatomical generation](MASTER_IMPLEMENTATION_SPECIFICATION.md#12-seeded-anatomical-generation) | Anatomy and motion | §§5–7, 10 | Partial | [R](#r-evidence) |
| [13 Growth and recalibration](MASTER_IMPLEMENTATION_SPECIFICATION.md#13-growth-and-recalibration) | Anatomy and motion | §§5–7, 10 | Partial | [R](#r-evidence) |
| [14 Functional skeleton and movement](MASTER_IMPLEMENTATION_SPECIFICATION.md#14-functional-skeleton-and-movement) | Anatomy and motion | §§5–7, 10 | Partial | [R](#r-evidence) |
| [15 Physical affordances and interaction](MASTER_IMPLEMENTATION_SPECIFICATION.md#15-physical-affordances-and-interaction) | Anatomy and motion | §§5–7, 10 | Partial | [R](#r-evidence) · [C.1](LOWER_BODY_C1.md) |
| [16 Topological skin architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#16-topological-skin-architecture) | Contact and motor learning | §§7, 12–15 | Not started | — |
| [17 Material interaction and tactile signals](MASTER_IMPLEMENTATION_SPECIFICATION.md#17-material-interaction-and-tactile-signals) | Contact and motor learning | §§7, 12–15 | Partial | [C.1.1 sole and pressure measurements](LOWER_BODY_C11.md#implementation) |
| [18 Proprioception and body schema](MASTER_IMPLEMENTATION_SPECIFICATION.md#18-proprioception-and-body-schema) | Contact and motor learning | §§7, 12–15 | Partial | [C.1.1 calibrated sensor model](LOWER_BODY_C11.md#implementation) |
| [19 Sensorimotor prediction and error correction](MASTER_IMPLEMENTATION_SPECIFICATION.md#19-sensorimotor-prediction-and-error-correction) | Contact and motor learning | §§7, 12–15 | Not started | — |
| [20 Topological muscle memory](MASTER_IMPLEMENTATION_SPECIFICATION.md#20-topological-muscle-memory) | Contact and motor learning | §§7, 12–15 | Not started | — |
| [21 Sensorimotor inheritance experiment](MASTER_IMPLEMENTATION_SPECIFICATION.md#21-sensorimotor-inheritance-experiment) | Contact and motor learning | §§7, 12–15 | Not started | — |
| [22 Separate computational training from lived childhood](MASTER_IMPLEMENTATION_SPECIFICATION.md#22-separate-computational-training-from-lived-childhood) | Education | §§9–21, 157 | Partial | [T](#t-evidence) |
| [23 Foundational training incubator](MASTER_IMPLEMENTATION_SPECIFICATION.md#23-foundational-training-incubator) | Education | §§9–21, 157 | Partial | [K](#k-evidence) |
| [24 Developmental preschool](MASTER_IMPLEMENTATION_SPECIFICATION.md#24-developmental-preschool) | Education | §§9–21, 157 | Not started | — |
| [25 Handwriting implementation](MASTER_IMPLEMENTATION_SPECIFICATION.md#25-handwriting-implementation) | Education | §§9–21, 157 | Not started | — |
| [26 Teacher demonstration and physical guidance](MASTER_IMPLEMENTATION_SPECIFICATION.md#26-teacher-demonstration-and-physical-guidance) | Education | §§9–21, 157 | Partial | [T](#t-evidence) |
| [27 Curriculum evaluation and generalization](MASTER_IMPLEMENTATION_SPECIFICATION.md#27-curriculum-evaluation-and-generalization) | Education | §§9–21, 157 | Partial | [K](#k-evidence) |
| [28 Curriculum editor and training queue](MASTER_IMPLEMENTATION_SPECIFICATION.md#28-curriculum-editor-and-training-queue) | Education | §§9–21, 157 | Not started | — |
| [29 Vision and visual learning](MASTER_IMPLEMENTATION_SPECIFICATION.md#29-vision-and-visual-learning) | Perception and voice | §§7, 12, 34 | Partial | [K](#k-evidence) |
| [30 Hearing and continuous acoustic environment](MASTER_IMPLEMENTATION_SPECIFICATION.md#30-hearing-and-continuous-acoustic-environment) | Perception and voice | §§7, 12, 34 | Not started | — |
| [31 Hybrid Vocal Physiology Engine](MASTER_IMPLEMENTATION_SPECIFICATION.md#31-hybrid-vocal-physiology-engine) | Perception and voice | §§7, 12, 34 | Experimental | [U](#u-evidence) |
| [32 Phonetic learning and language grounding](MASTER_IMPLEMENTATION_SPECIFICATION.md#32-phonetic-learning-and-language-grounding) | Perception and voice | §§7, 12, 34 | Not started | — |
| [33 Nonverbal expression and vocal health](MASTER_IMPLEMENTATION_SPECIFICATION.md#33-nonverbal-expression-and-vocal-health) | Perception and voice | §§7, 12, 34 | Not started | — |
| [34 Reduced-order physiological architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#34-reduced-order-physiological-architecture) | Physiology | §§5–7, 12 | Partial | [B](#b-evidence) |
| [35 Muscle-specific fatigue and endurance](MASTER_IMPLEMENTATION_SPECIFICATION.md#35-muscle-specific-fatigue-and-endurance) | Physiology | §§5–7, 12 | Partial | [B](#b-evidence) |
| [36 Homeostasis, illness and recovery](MASTER_IMPLEMENTATION_SPECIFICATION.md#36-homeostasis-illness-and-recovery) | Physiology | §§5–7, 12 | Partial | [B](#b-evidence) |
| [37 Environmental pressure and chemical signals](MASTER_IMPLEMENTATION_SPECIFICATION.md#37-environmental-pressure-and-chemical-signals) | Physiology | §§5–7, 12 | Partial | [B](#b-evidence) |
| [38 Biological connectome bootstrap](MASTER_IMPLEMENTATION_SPECIFICATION.md#38-biological-connectome-bootstrap) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [39 Complementary processing and bridge](MASTER_IMPLEMENTATION_SPECIFICATION.md#39-complementary-processing-and-bridge) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [40 Neural plasticity and development](MASTER_IMPLEMENTATION_SPECIFICATION.md#40-neural-plasticity-and-development) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [41 Peripheral and motor nervous systems](MASTER_IMPLEMENTATION_SPECIFICATION.md#41-peripheral-and-motor-nervous-systems) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [42 Attention and automatic processing](MASTER_IMPLEMENTATION_SPECIFICATION.md#42-attention-and-automatic-processing) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [43 Executive cognition](MASTER_IMPLEMENTATION_SPECIFICATION.md#43-executive-cognition) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [44 Goal formation and revision](MASTER_IMPLEMENTATION_SPECIFICATION.md#44-goal-formation-and-revision) | Cognition | §§5–8, 29–37 | Partial | [K](#k-evidence) |
| [45 Unified multimodal associative foundation](MASTER_IMPLEMENTATION_SPECIFICATION.md#45-unified-multimodal-associative-foundation) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [46 Library I — Knowledge](MASTER_IMPLEMENTATION_SPECIFICATION.md#46-library-i--knowledge) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [47 Library II — Possibility Space (Level 10)](MASTER_IMPLEMENTATION_SPECIFICATION.md#47-library-ii--possibility-space-level-10) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [48 Library III — Lived Memory](MASTER_IMPLEMENTATION_SPECIFICATION.md#48-library-iii--lived-memory) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [49 Memory and association schema](MASTER_IMPLEMENTATION_SPECIFICATION.md#49-memory-and-association-schema) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [50 Fourteen functional memory categories](MASTER_IMPLEMENTATION_SPECIFICATION.md#50-fourteen-functional-memory-categories) | Libraries | §§5, 7, 10 | Not started | — |
| [51 Forgetting and retrieval accessibility](MASTER_IMPLEMENTATION_SPECIFICATION.md#51-forgetting-and-retrieval-accessibility) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [52 Cognitive provenance and continuity](MASTER_IMPLEMENTATION_SPECIFICATION.md#52-cognitive-provenance-and-continuity) | Libraries | §§5, 7, 10 | Partial | [L](#l-evidence) |
| [53 Working memory and long-term retrieval](MASTER_IMPLEMENTATION_SPECIFICATION.md#53-working-memory-and-long-term-retrieval) | Archive and checkpoint | §§5–10, 45–52 | Partial | [L](#l-evidence) |
| [54 Content-addressed multimedia archive](MASTER_IMPLEMENTATION_SPECIFICATION.md#54-content-addressed-multimedia-archive) | Archive and checkpoint | §§5–10, 45–52 | Partial | [L](#l-evidence) |
| [55 Recording fidelity and storage management](MASTER_IMPLEMENTATION_SPECIFICATION.md#55-recording-fidelity-and-storage-management) | Archive and checkpoint | §§5–10, 45–52 | Partial | [L](#l-evidence) |
| [56 Generational learning and inheritance](MASTER_IMPLEMENTATION_SPECIFICATION.md#56-generational-learning-and-inheritance) | Archive and checkpoint | §§5–10, 45–52 | Partial | [T](#t-evidence) |
| [57 Checkpoint structure and atomic restoration](MASTER_IMPLEMENTATION_SPECIFICATION.md#57-checkpoint-structure-and-atomic-restoration) | Archive and checkpoint | §§5–10, 45–52 | Partial | [S](#s-evidence) |
| [58 Portable artificial individual packages](MASTER_IMPLEMENTATION_SPECIFICATION.md#58-portable-artificial-individual-packages) | Archive and checkpoint | §§5–10, 45–52 | Partial | [L](#l-evidence) |
| [59 Day/night architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#59-daynight-architecture) | Sleep and imagination | §§34, 45–57 | Partial | [K](#k-evidence) |
| [60 Ordinary and constructive replay](MASTER_IMPLEMENTATION_SPECIFICATION.md#60-ordinary-and-constructive-replay) | Sleep and imagination | §§34, 45–57 | Experimental | [D](#d-evidence) |
| [61 Reflection and practical wisdom](MASTER_IMPLEMENTATION_SPECIFICATION.md#61-reflection-and-practical-wisdom) | Sleep and imagination | §§34, 45–57 | Not started | — |
| [62 Constructive 3D imagination](MASTER_IMPLEMENTATION_SPECIFICATION.md#62-constructive-3d-imagination) | Sleep and imagination | §§34, 45–57 | Not started | — |
| [63 Mental physics](MASTER_IMPLEMENTATION_SPECIFICATION.md#63-mental-physics) | Sleep and imagination | §§34, 45–57 | Not started | — |
| [64 Library of Babel and Hilbert's Hotel](MASTER_IMPLEMENTATION_SPECIFICATION.md#64-library-of-babel-and-hilberts-hotel) | Sleep and imagination | §§34, 45–57 | Not started | — |
| [65 Conceptual Genesis and Infinite Craft](MASTER_IMPLEMENTATION_SPECIFICATION.md#65-conceptual-genesis-and-infinite-craft) | Sleep and imagination | §§34, 45–57 | Not started | — |
| [66 Seed hierarchy](MASTER_IMPLEMENTATION_SPECIFICATION.md#66-seed-hierarchy) | World | §§5–7, 57 | Partial | [K](#k-evidence) |
| [67 Semantic-to-physical world representation](MASTER_IMPLEMENTATION_SPECIFICATION.md#67-semantic-to-physical-world-representation) | World | §§5–7, 57 | Partial | [W](#w-evidence) |
| [68 Procedural detail and persistent consequences](MASTER_IMPLEMENTATION_SPECIFICATION.md#68-procedural-detail-and-persistent-consequences) | World | §§5–7, 57 | Not started | — |
| [69 Unified materials](MASTER_IMPLEMENTATION_SPECIFICATION.md#69-unified-materials) | World | §§5–7, 57 | Partial | [W](#w-evidence) |
| [70 Physical sandbox and transformations](MASTER_IMPLEMENTATION_SPECIFICATION.md#70-physical-sandbox-and-transformations) | World | §§5–7, 57 | Partial | [W](#w-evidence) |
| [71 Dynamic atmosphere and water cycle](MASTER_IMPLEMENTATION_SPECIFICATION.md#71-dynamic-atmosphere-and-water-cycle) | World | §§5–7, 57 | Not started | — |
| [72 Cloud generation and visual continuity](MASTER_IMPLEMENTATION_SPECIFICATION.md#72-cloud-generation-and-visual-continuity) | World | §§5–7, 57 | Not started | — |
| [73 Ecosystems and natural development](MASTER_IMPLEMENTATION_SPECIFICATION.md#73-ecosystems-and-natural-development) | World | §§5–7, 57 | Not started | — |
| [74 Underlying, untaught philosophical principle](MASTER_IMPLEMENTATION_SPECIFICATION.md#74-underlying-untaught-philosophical-principle) | Abstraction | §§29, 40, 45, 77–79 | Not started | — |
| [75 Concept formation and abstraction](MASTER_IMPLEMENTATION_SPECIFICATION.md#75-concept-formation-and-abstraction) | Abstraction | §§29, 40, 45, 77–79 | Partial | [K](#k-evidence) |
| [76 Incorrect organization and uncertainty](MASTER_IMPLEMENTATION_SPECIFICATION.md#76-incorrect-organization-and-uncertainty) | Abstraction | §§29, 40, 45, 77–79 | Partial | [K](#k-evidence) |
| [77 Curiosity and Discovery Room](MASTER_IMPLEMENTATION_SPECIFICATION.md#77-curiosity-and-discovery-room) | Curiosity | §§7, 45–52, 157 | Experimental | [D](#d-evidence) |
| [78 Persistent questions](MASTER_IMPLEMENTATION_SPECIFICATION.md#78-persistent-questions) | Curiosity | §§7, 45–52, 157 | Experimental | [D](#d-evidence) |
| [79 Prediction ledger](MASTER_IMPLEMENTATION_SPECIFICATION.md#79-prediction-ledger) | Curiosity | §§7, 45–52, 157 | Experimental | [D](#d-evidence) |
| [80 Intuition, familiarity and cognitive phenomena](MASTER_IMPLEMENTATION_SPECIFICATION.md#80-intuition-familiarity-and-cognitive-phenomena) | Curiosity | §§7, 45–52, 157 | Not started | — |
| [81 Learning how to learn](MASTER_IMPLEMENTATION_SPECIFICATION.md#81-learning-how-to-learn) | Curiosity | §§7, 45–52, 157 | Not started | — |
| [82 Paradox-based cognitive development](MASTER_IMPLEMENTATION_SPECIFICATION.md#82-paradox-based-cognitive-development) | Reasoning and creativity | §§32, 45–65, 74–81 | Not started | — |
| [83 Creativity and divergent thinking](MASTER_IMPLEMENTATION_SPECIFICATION.md#83-creativity-and-divergent-thinking) | Reasoning and creativity | §§32, 45–65, 74–81 | Not started | — |
| [84 Delusion-like beliefs and reality testing](MASTER_IMPLEMENTATION_SPECIFICATION.md#84-delusion-like-beliefs-and-reality-testing) | Reasoning and creativity | §§32, 45–65, 74–81 | Not started | — |
| [85 Spontaneous thought and background imagination](MASTER_IMPLEMENTATION_SPECIFICATION.md#85-spontaneous-thought-and-background-imagination) | Reasoning and creativity | §§32, 45–65, 74–81 | Not started | — |
| [86 Scientific evidence repository](MASTER_IMPLEMENTATION_SPECIFICATION.md#86-scientific-evidence-repository) | Optional research | §§8, 34–44, 156–161 | Not started | — |
| [87 Neurochemical modulation architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#87-neurochemical-modulation-architecture) | Optional research | §§8, 34–44, 156–161 | Not started | — |
| [88 Cognitive perturbation laboratory](MASTER_IMPLEMENTATION_SPECIFICATION.md#88-cognitive-perturbation-laboratory) | Optional research | §§8, 34–44, 156–161 | Not started | — |
| [89 Alzheimer's-inspired memory experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#89-alzheimers-inspired-memory-experiments) | Optional research | §§8, 34–44, 156–161 | Not started | — |
| [90 Other neurological phenomena and compensation](MASTER_IMPLEMENTATION_SPECIFICATION.md#90-other-neurological-phenomena-and-compensation) | Optional research | §§8, 34–44, 156–161 | Not started | — |
| [91 Individual personality development](MASTER_IMPLEMENTATION_SPECIFICATION.md#91-individual-personality-development) | Personality and skills | §§10, 20, 32, 59, 75 | Partial | [K](#k-evidence) |
| [92 Habit formation, stimming and regulation](MASTER_IMPLEMENTATION_SPECIFICATION.md#92-habit-formation-stimming-and-regulation) | Personality and skills | §§10, 20, 32, 59, 75 | Partial | [K](#k-evidence) |
| [93 Rhythm and synchronization](MASTER_IMPLEMENTATION_SPECIFICATION.md#93-rhythm-and-synchronization) | Personality and skills | §§10, 20, 32, 59, 75 | Not started | — |
| [94 Improvisational cognition](MASTER_IMPLEMENTATION_SPECIFICATION.md#94-improvisational-cognition) | Personality and skills | §§10, 20, 32, 59, 75 | Not started | — |
| [95 Personal aesthetic taste](MASTER_IMPLEMENTATION_SPECIFICATION.md#95-personal-aesthetic-taste) | Personality and skills | §§10, 20, 32, 59, 75 | Not started | — |
| [96 Heroes, aspirations and determination](MASTER_IMPLEMENTATION_SPECIFICATION.md#96-heroes-aspirations-and-determination) | Personality and skills | §§10, 20, 32, 59, 75 | Not started | — |
| [97 Social cognition and theory of mind](MASTER_IMPLEMENTATION_SPECIFICATION.md#97-social-cognition-and-theory-of-mind) | Social development | §§10–11, 25–33, 45–60 | Partial | [K](#k-evidence) |
| [98 Communication beyond literal language](MASTER_IMPLEMENTATION_SPECIFICATION.md#98-communication-beyond-literal-language) | Social development | §§10–11, 25–33, 45–60 | Partial | [K](#k-evidence) |
| [99 Fairy tales and childhood imagination](MASTER_IMPLEMENTATION_SPECIFICATION.md#99-fairy-tales-and-childhood-imagination) | Social development | §§10–11, 25–33, 45–60 | Not started | — |
| [100 Five lived foundations of existential development](MASTER_IMPLEMENTATION_SPECIFICATION.md#100-five-lived-foundations-of-existential-development) | Social development | §§10–11, 25–33, 45–60 | Not started | — |
| [101 Unstructured childhood and leisure](MASTER_IMPLEMENTATION_SPECIFICATION.md#101-unstructured-childhood-and-leisure) | Social development | §§10–11, 25–33, 45–60 | Partial | [K](#k-evidence) |
| [102 Personal significance and autobiographical meaning](MASTER_IMPLEMENTATION_SPECIFICATION.md#102-personal-significance-and-autobiographical-meaning) | Social development | §§10–11, 25–33, 45–60 | Not started | — |
| [103 Lived-life epiphanies](MASTER_IMPLEMENTATION_SPECIFICATION.md#103-lived-life-epiphanies) | Social development | §§10–11, 25–33, 45–60 | Not started | — |
| [104 Truman milestone and existential inquiry](MASTER_IMPLEMENTATION_SPECIFICATION.md#104-truman-milestone-and-existential-inquiry) | Social development | §§10–11, 25–33, 45–60 | Not started | — |
| [105 Formal mentor–squire architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#105-formal-mentorsquire-architecture) | Generational development | §§11, 21, 56–58, 97–104 | Partial | [K](#k-evidence) |
| [106 Controlled generational experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#106-controlled-generational-experiments) | Generational development | §§11, 21, 56–58, 97–104 | Not started | — |
| [107 Cumulative cultural learning](MASTER_IMPLEMENTATION_SPECIFICATION.md#107-cumulative-cultural-learning) | Generational development | §§11, 21, 56–58, 97–104 | Not started | — |
| [108 Emerging institutions and community organization](MASTER_IMPLEMENTATION_SPECIFICATION.md#108-emerging-institutions-and-community-organization) | Generational development | §§11, 21, 56–58, 97–104 | Not started | — |
| [109 Culture, tradition, finitude and legacy](MASTER_IMPLEMENTATION_SPECIFICATION.md#109-culture-tradition-finitude-and-legacy) | Generational development | §§11, 21, 56–58, 97–104 | Not started | — |
| [110 Scientific discovery environment](MASTER_IMPLEMENTATION_SPECIFICATION.md#110-scientific-discovery-environment) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [111 Scientific instruments and measurement](MASTER_IMPLEMENTATION_SPECIFICATION.md#111-scientific-instruments-and-measurement) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [112 Specialized scientific solvers](MASTER_IMPLEMENTATION_SPECIFICATION.md#112-specialized-scientific-solvers) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [113 Scientific method and internal validation](MASTER_IMPLEMENTATION_SPECIFICATION.md#113-scientific-method-and-internal-validation) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [114 Simulation-to-reality transfer](MASTER_IMPLEMENTATION_SPECIFICATION.md#114-simulation-to-reality-transfer) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [115 Creation, true making and the Ten Lens framework](MASTER_IMPLEMENTATION_SPECIFICATION.md#115-creation-true-making-and-the-ten-lens-framework) | Science and engineering | §§15–21, 66–70, 77–79, 157 | Not started | — |
| [116 Independent simulation, agent perception and observer presentation](MASTER_IMPLEMENTATION_SPECIFICATION.md#116-independent-simulation-agent-perception-and-observer-presentation) | Presentation | §§4–5, 9–15, 67–72 | Partial | [K](#k-evidence) |
| [117 Original anime art direction](MASTER_IMPLEMENTATION_SPECIFICATION.md#117-original-anime-art-direction) | Presentation | §§4–5, 9–15, 67–72 | Not started | — |
| [118 VRoid and VRM asset integration](MASTER_IMPLEMENTATION_SPECIFICATION.md#118-vroid-and-vrm-asset-integration) | Presentation | §§4–5, 9–15, 67–72 | Blocked | [V](#v-evidence) |
| [119 Avatar retargeting and procedural character variety](MASTER_IMPLEMENTATION_SPECIFICATION.md#119-avatar-retargeting-and-procedural-character-variety) | Presentation | §§4–5, 9–15, 67–72 | Partial | [V](#v-evidence) |
| [120 Visual developmental growth](MASTER_IMPLEMENTATION_SPECIFICATION.md#120-visual-developmental-growth) | Presentation | §§4–5, 9–15, 67–72 | Partial | [R](#r-evidence) |
| [121 Facial expression and vocal synchronization](MASTER_IMPLEMENTATION_SPECIFICATION.md#121-facial-expression-and-vocal-synchronization) | Presentation | §§4–5, 9–15, 67–72 | Partial | [K](#k-evidence) |
| [122 Stylized environmental effects and fidelity modes](MASTER_IMPLEMENTATION_SPECIFICATION.md#122-stylized-environmental-effects-and-fidelity-modes) | Presentation | §§4–5, 9–15, 67–72 | Not started | — |
| [123 Immersive three-library presentation](MASTER_IMPLEMENTATION_SPECIFICATION.md#123-immersive-three-library-presentation) | Presentation | §§4–5, 9–15, 67–72 | Partial | [L](#l-evidence) |
| [124 Modular workspace foundation](MASTER_IMPLEMENTATION_SPECIFICATION.md#124-modular-workspace-foundation) | Workspace and observatories | §§4, 7–8, 116 | Partial | [C.1](LOWER_BODY_C1.md#observatory-and-continuation) |
| [125 Workspace persistence and presets](MASTER_IMPLEMENTATION_SPECIFICATION.md#125-workspace-persistence-and-presets) | Workspace and observatories | §§4, 7–8, 116 | Not started | — |
| [126 Shared interface components and accessibility](MASTER_IMPLEMENTATION_SPECIFICATION.md#126-shared-interface-components-and-accessibility) | Workspace and observatories | §§4, 7–8, 116 | Partial | [O](#o-evidence) |
| [127 Brain observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#127-brain-observatory) | Workspace and observatories | §§4, 7–8, 116 | Partial | [O](#o-evidence) |
| [128 Body observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#128-body-observatory) | Workspace and observatories | §§4, 7–8, 116 | Partial | [R](#r-evidence) |
| [129 Topological skin observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#129-topological-skin-observatory) | Workspace and observatories | §§4, 7–8, 116 | Not started | — |
| [130 Physiology and neurochemistry observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#130-physiology-and-neurochemistry-observatory) | Workspace and observatories | §§4, 7–8, 116 | Not started | — |
| [131 Library and memory observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#131-library-and-memory-observatory) | Workspace and observatories | §§4, 7–8, 116 | Partial | [L](#l-evidence) |
| [132 Imagination observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#132-imagination-observatory) | Workspace and observatories | §§4, 7–8, 116 | Not started | — |
| [133 World, science and researcher control trays](MASTER_IMPLEMENTATION_SPECIFICATION.md#133-world-science-and-researcher-control-trays) | Workspace and observatories | §§4, 7–8, 116 | Partial | [K](#k-evidence) |
| [134 Performance and character management trays](MASTER_IMPLEMENTATION_SPECIFICATION.md#134-performance-and-character-management-trays) | Workspace and observatories | §§4, 7–8, 116 | Partial | [V](#v-evidence) |
| [135 Optimization as a cross-cutting requirement](MASTER_IMPLEMENTATION_SPECIFICATION.md#135-optimization-as-a-cross-cutting-requirement) | Performance | §§5–8, 54, 116 | Partial | [K](#k-evidence) |
| [136 Central resource governor](MASTER_IMPLEMENTATION_SPECIFICATION.md#136-central-resource-governor) | Performance | §§5–8, 54, 116 | Partial | [P](#p-evidence) |
| [137 Multirate scheduling](MASTER_IMPLEMENTATION_SPECIFICATION.md#137-multirate-scheduling) | Performance | §§5–8, 54, 116 | Partial | [K](#k-evidence) |
| [138 Cognitive level of detail](MASTER_IMPLEMENTATION_SPECIFICATION.md#138-cognitive-level-of-detail) | Performance | §§5–8, 54, 116 | Not started | — |
| [139 Biological level of detail and solver escalation](MASTER_IMPLEMENTATION_SPECIFICATION.md#139-biological-level-of-detail-and-solver-escalation) | Performance | §§5–8, 54, 116 | Partial | [K](#k-evidence) |
| [140 Shared neural foundations and private learning](MASTER_IMPLEMENTATION_SPECIFICATION.md#140-shared-neural-foundations-and-private-learning) | Performance | §§5–8, 54, 116 | Partial | [K](#k-evidence) |
| [141 Centralized local inference and model routing](MASTER_IMPLEMENTATION_SPECIFICATION.md#141-centralized-local-inference-and-model-routing) | Performance | §§5–8, 54, 116 | Not started | — |
| [142 Hierarchical skill compression](MASTER_IMPLEMENTATION_SPECIFICATION.md#142-hierarchical-skill-compression) | Performance | §§5–8, 54, 116 | Not started | — |
| [143 Sparse neural and sensory processing](MASTER_IMPLEMENTATION_SPECIFICATION.md#143-sparse-neural-and-sensory-processing) | Performance | §§5–8, 54, 116 | Partial | [K](#k-evidence) |
| [144 Spatial partitioning and background population simulation](MASTER_IMPLEMENTATION_SPECIFICATION.md#144-spatial-partitioning-and-background-population-simulation) | Performance | §§5–8, 54, 116 | Not started | — |
| [145 Procedural asset streaming](MASTER_IMPLEMENTATION_SPECIFICATION.md#145-procedural-asset-streaming) | Performance | §§5–8, 54, 116 | Not started | — |
| [146 Parallel and asynchronous processing](MASTER_IMPLEMENTATION_SPECIFICATION.md#146-parallel-and-asynchronous-processing) | Performance | §§5–8, 54, 116 | Not started | — |
| [147 Optimized storage, retrieval and telemetry](MASTER_IMPLEMENTATION_SPECIFICATION.md#147-optimized-storage-retrieval-and-telemetry) | Performance | §§5–8, 54, 116 | Partial | [L](#l-evidence) |
| [148 Performance regression protocol](MASTER_IMPLEMENTATION_SPECIFICATION.md#148-performance-regression-protocol) | Performance | §§5–8, 54, 116 | Partial | [P](#p-evidence) |
| [149 Mandatory quality cycle](MASTER_IMPLEMENTATION_SPECIFICATION.md#149-mandatory-quality-cycle) | Quality | §§135–148 | Partial | [Z](#z-evidence) |
| [150 Actual visual inspection](MASTER_IMPLEMENTATION_SPECIFICATION.md#150-actual-visual-inspection) | Quality | §§135–148 | Partial | [Z](#z-evidence) |
| [151 Animation and physical presentation](MASTER_IMPLEMENTATION_SPECIFICATION.md#151-animation-and-physical-presentation) | Quality | §§135–148 | Partial | [Z](#z-evidence) |
| [152 Audio quality pass](MASTER_IMPLEMENTATION_SPECIFICATION.md#152-audio-quality-pass) | Quality | §§135–148 | Not started | — |
| [153 UI and usability pass](MASTER_IMPLEMENTATION_SPECIFICATION.md#153-ui-and-usability-pass) | Quality | §§135–148 | Partial | [Z](#z-evidence) |
| [154 Visual regression testing](MASTER_IMPLEMENTATION_SPECIFICATION.md#154-visual-regression-testing) | Quality | §§135–148 | Not started | — |
| [155 Long-duration stability and recovery](MASTER_IMPLEMENTATION_SPECIFICATION.md#155-long-duration-stability-and-recovery) | Quality | §§135–148 | Partial | [Z](#z-evidence) |
| [156 Scientific claim classification](MASTER_IMPLEMENTATION_SPECIFICATION.md#156-scientific-claim-classification) | Research and ethics | §§1, 45–52 | Partial | [Z](#z-evidence) |
| [157 Experimental conditions and baselines](MASTER_IMPLEMENTATION_SPECIFICATION.md#157-experimental-conditions-and-baselines) | Research and ethics | §§1, 45–52 | Experimental | [D](#d-evidence) |
| [158 Knowledge and capability provenance audit](MASTER_IMPLEMENTATION_SPECIFICATION.md#158-knowledge-and-capability-provenance-audit) | Research and ethics | §§1, 45–52 | Partial | [Z](#z-evidence) |
| [159 Artificial phronesis evaluation](MASTER_IMPLEMENTATION_SPECIFICATION.md#159-artificial-phronesis-evaluation) | Research and ethics | §§1, 45–52 | Not started | — |
| [160 Ethical development and experimental safeguards](MASTER_IMPLEMENTATION_SPECIFICATION.md#160-ethical-development-and-experimental-safeguards) | Research and ethics | §§1, 45–52 | Partial | [E](#e-evidence) |
| [161 Truthfulness and creator–creation trust](MASTER_IMPLEMENTATION_SPECIFICATION.md#161-truthfulness-and-creatorcreation-trust) | Research and ethics | §§1, 45–52 | Partial | [E](#e-evidence) |
| [162 Persistence reliability](MASTER_IMPLEMENTATION_SPECIFICATION.md#162-persistence-reliability) | Reliability and release | §§3–8, 57, 181–186 | Partial | [S](#s-evidence) |
| [163 Dependency and license management](MASTER_IMPLEMENTATION_SPECIFICATION.md#163-dependency-and-license-management) | Reliability and release | §§3–8, 57, 181–186 | Partial | [V](#v-evidence) |
| [164 Safe local AI integration and subsystem failure isolation](MASTER_IMPLEMENTATION_SPECIFICATION.md#164-safe-local-ai-integration-and-subsystem-failure-isolation) | Reliability and release | §§3–8, 57, 181–186 | Not started | — |
| [165 Extension architecture and SDK](MASTER_IMPLEMENTATION_SPECIFICATION.md#165-extension-architecture-and-sdk) | Reliability and release | §§3–8, 57, 181–186 | Not started | — |
| [166 Reproducible installation, build and CI](MASTER_IMPLEMENTATION_SPECIFICATION.md#166-reproducible-installation-build-and-ci) | Reliability and release | §§3–8, 57, 181–186 | Partial | [Z](#z-evidence) |
| [167 Version control and continuation](MASTER_IMPLEMENTATION_SPECIFICATION.md#167-version-control-and-continuation) | Reliability and release | §§3–8, 57, 181–186 | Partial | [Z](#z-evidence) |
| [168 Milestone Zero — Repository recovery and audit](MASTER_IMPLEMENTATION_SPECIFICATION.md#168-milestone-zero--repository-recovery-and-audit) | Milestone Zero | §§2–3, 181–186 | Partial | [Z](#z-evidence) |
| [169 Milestone One — Core infrastructure](MASTER_IMPLEMENTATION_SPECIFICATION.md#169-milestone-one--core-infrastructure) | Milestone delivery | §168 | Not started | — |
| [170 Milestone Two — Functional embodiment](MASTER_IMPLEMENTATION_SPECIFICATION.md#170-milestone-two--functional-embodiment) | Milestone delivery | §169 | Partial | [C.1](LOWER_BODY_C1.md) · [C.1.1](LOWER_BODY_C11.md) |
| [171 Milestone Three — Founding preschool](MASTER_IMPLEMENTATION_SPECIFICATION.md#171-milestone-three--founding-preschool) | Milestone delivery | §170 | Not started | — |
| [172 Milestone Four — Integrated memory and cognition](MASTER_IMPLEMENTATION_SPECIFICATION.md#172-milestone-four--integrated-memory-and-cognition) | Milestone delivery | §171 | Not started | — |
| [173 Milestone Five — Persistent living world](MASTER_IMPLEMENTATION_SPECIFICATION.md#173-milestone-five--persistent-living-world) | Milestone delivery | §172 | Not started | — |
| [174 Milestone Six — Advanced individual development](MASTER_IMPLEMENTATION_SPECIFICATION.md#174-milestone-six--advanced-individual-development) | Milestone delivery | §173 | Not started | — |
| [175 Milestone Seven — Generational development](MASTER_IMPLEMENTATION_SPECIFICATION.md#175-milestone-seven--generational-development) | Milestone delivery | §174 | Not started | — |
| [176 Milestone Eight — Advanced scientific research](MASTER_IMPLEMENTATION_SPECIFICATION.md#176-milestone-eight--advanced-scientific-research) | Milestone delivery | §175 | Not started | — |
| [177 Preschool handwriting demonstration and acceptance protocol](MASTER_IMPLEMENTATION_SPECIFICATION.md#177-preschool-handwriting-demonstration-and-acceptance-protocol) | Integrated handwriting | §§170–172 | Not started | — |
| [178 Definition of done](MASTER_IMPLEMENTATION_SPECIFICATION.md#178-definition-of-done) | Milestone reporting | §§3, 149–157, 167 | Partial | [Z](#z-evidence) |
| [179 Required milestone report](MASTER_IMPLEMENTATION_SPECIFICATION.md#179-required-milestone-report) | Milestone reporting | §§3, 149–157, 167 | Partial | [Z](#z-evidence) |
| [180 Final governing principles](MASTER_IMPLEMENTATION_SPECIFICATION.md#180-final-governing-principles) | Milestone reporting | §§3, 149–157, 167 | Partial | [Z](#z-evidence) |
| [181 Active main branch protection](MASTER_IMPLEMENTATION_SPECIFICATION.md#181-active-main-branch-protection) | Repository governance | Owner policy | Verified | [G](#g-evidence) |
| [182 Mandatory feature/agent branch workflow](MASTER_IMPLEMENTATION_SPECIFICATION.md#182-mandatory-featureagent-branch-workflow) | Repository governance | Owner policy | Partial | [Z](#z-evidence) |
| [183 Pull request requirements and reviewability](MASTER_IMPLEMENTATION_SPECIFICATION.md#183-pull-request-requirements-and-reviewability) | Repository governance | Owner policy | Partial | [Z](#z-evidence) |
| [184 Automated review, CI and quality gates](MASTER_IMPLEMENTATION_SPECIFICATION.md#184-automated-review-ci-and-quality-gates) | Repository governance | Owner policy | Not started | — |
| [185 Merge authorization and owner control](MASTER_IMPLEMENTATION_SPECIFICATION.md#185-merge-authorization-and-owner-control) | Repository governance | Owner policy | Verified | [G](#g-evidence) |
| [186 Interrupted development and recovery](MASTER_IMPLEMENTATION_SPECIFICATION.md#186-interrupted-development-and-recovery) | Repository governance | Owner policy | Partial | [Z](#z-evidence) |
| [A.1 Current Project Status](MASTER_IMPLEMENTATION_SPECIFICATION.md#1-current-project-status) | Project and research | §§2–3, 168 | Partial | [Z](#z-evidence) |
| [A.2 Preserve the Original Master Directive](MASTER_IMPLEMENTATION_SPECIFICATION.md#2-preserve-the-original-master-directive) | Project and research | §§2–3, 168 | Partial | [Z](#z-evidence) |
| [A.3 Two Primary Learning Mechanisms](MASTER_IMPLEMENTATION_SPECIFICATION.md#3-two-primary-learning-mechanisms) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.4 Learning Through Recreation](MASTER_IMPLEMENTATION_SPECIFICATION.md#4-learning-through-recreation) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.5 Audit the First Latch-Room Pilot](MASTER_IMPLEMENTATION_SPECIFICATION.md#5-audit-the-first-latch-room-pilot) | Discovery, education and motion | §§15–26, 77–81, 157 | Experimental | [D](#d-evidence) |
| [A.6 Develop the Discovery Room](MASTER_IMPLEMENTATION_SPECIFICATION.md#6-develop-the-discovery-room) | Discovery, education and motion | §§15–26, 77–81, 157 | Experimental | [D](#d-evidence) |
| [A.7 Expand the Curiosity Engine](MASTER_IMPLEMENTATION_SPECIFICATION.md#7-expand-the-curiosity-engine) | Discovery, education and motion | §§15–26, 77–81, 157 | Experimental | [D](#d-evidence) |
| [A.8 Persistent Questions](MASTER_IMPLEMENTATION_SPECIFICATION.md#8-persistent-questions) | Discovery, education and motion | §§15–26, 77–81, 157 | Not started | — |
| [A.9 Implement the Mimicry Engine](MASTER_IMPLEMENTATION_SPECIFICATION.md#9-implement-the-mimicry-engine) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.10 Observation-to-Recreation Cycle](MASTER_IMPLEMENTATION_SPECIFICATION.md#10-observation-to-recreation-cycle) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.11 Motor Babbling](MASTER_IMPLEMENTATION_SPECIFICATION.md#11-motor-babbling) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.12 Forms of Mimicry](MASTER_IMPLEMENTATION_SPECIFICATION.md#12-forms-of-mimicry) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.13 Teacher Guidance](MASTER_IMPLEMENTATION_SPECIFICATION.md#13-teacher-guidance) | Discovery, education and motion | §§15–26, 77–81, 157 | Partial | [T](#t-evidence) |
| [A.14 Handwriting Experiment](MASTER_IMPLEMENTATION_SPECIFICATION.md#14-handwriting-experiment) | Discovery, education and motion | §§15–26, 77–81, 157 | Not started | — |
| [A.15 Mirror-Neuron-Inspired Representations](MASTER_IMPLEMENTATION_SPECIFICATION.md#15-mirror-neuron-inspired-representations) | Discovery, education and motion | §§15–26, 77–81, 157 | Not started | — |
| [A.16 General Sensory Requirement](MASTER_IMPLEMENTATION_SPECIFICATION.md#16-general-sensory-requirement) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.17 Continuous Sensory Fields](MASTER_IMPLEMENTATION_SPECIFICATION.md#17-continuous-sensory-fields) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.18 Continuous Full-Spectrum Hearing](MASTER_IMPLEMENTATION_SPECIFICATION.md#18-continuous-full-spectrum-hearing) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.19 Biological Ear Architecture](MASTER_IMPLEMENTATION_SPECIFICATION.md#19-biological-ear-architecture) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.20 Auditory Research Libraries](MASTER_IMPLEMENTATION_SPECIFICATION.md#20-auditory-research-libraries) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.21 Persistent Acoustic Environment](MASTER_IMPLEMENTATION_SPECIFICATION.md#21-persistent-acoustic-environment) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.22 Auditory Attention](MASTER_IMPLEMENTATION_SPECIFICATION.md#22-auditory-attention) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.23 Individual Hearing](MASTER_IMPLEMENTATION_SPECIFICATION.md#23-individual-hearing) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.24 Hearing One's Own Body](MASTER_IMPLEMENTATION_SPECIFICATION.md#24-hearing-ones-own-body) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.25 Cost Requirements](MASTER_IMPLEMENTATION_SPECIFICATION.md#25-cost-requirements) | Hearing and perception | §§7, 29–32, 137 | Not started | — |
| [A.26 Speech Technology Assessment](MASTER_IMPLEMENTATION_SPECIFICATION.md#26-speech-technology-assessment) | Vocal development | §§12, 29–37, 157, 163 | Experimental | [U](#u-evidence) |
| [A.27 Separate Pretrained Speech from Acquired Speech](MASTER_IMPLEMENTATION_SPECIFICATION.md#27-separate-pretrained-speech-from-acquired-speech) | Vocal development | §§12, 29–37, 157, 163 | Experimental | [U](#u-evidence) |
| [A.28 Architectural Decision](MASTER_IMPLEMENTATION_SPECIFICATION.md#28-architectural-decision) | Vocal development | §§12, 29–37, 157, 163 | Experimental | [U](#u-evidence) |
| [A.29 Pink Trombone — Initial Physical Vocal Apparatus](MASTER_IMPLEMENTATION_SPECIFICATION.md#29-pink-trombone--initial-physical-vocal-apparatus) | Vocal development | §§12, 29–37, 157, 163 | Experimental | [U](#u-evidence) |
| [A.30 Individual Vocal Anatomy](MASTER_IMPLEMENTATION_SPECIFICATION.md#30-individual-vocal-anatomy) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.31 Continuous Vocal Parameters](MASTER_IMPLEMENTATION_SPECIFICATION.md#31-continuous-vocal-parameters) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.32 High-Fidelity Neural Rendering](MASTER_IMPLEMENTATION_SPECIFICATION.md#32-high-fidelity-neural-rendering) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.33 Vocal Expression Beyond Words](MASTER_IMPLEMENTATION_SPECIFICATION.md#33-vocal-expression-beyond-words) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.34 Continuous Expressive Transitions](MASTER_IMPLEMENTATION_SPECIFICATION.md#34-continuous-expressive-transitions) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.35 Crying and Trembling Voice](MASTER_IMPLEMENTATION_SPECIFICATION.md#35-crying-and-trembling-voice) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.36 Yelling and Screaming](MASTER_IMPLEMENTATION_SPECIFICATION.md#36-yelling-and-screaming) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.37 Respiratory System](MASTER_IMPLEMENTATION_SPECIFICATION.md#37-respiratory-system) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.38 Vocal Health State](MASTER_IMPLEMENTATION_SPECIFICATION.md#38-vocal-health-state) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.39 Vocal Load and Overuse](MASTER_IMPLEMENTATION_SPECIFICATION.md#39-vocal-load-and-overuse) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.40 Vocal Soreness and Fatigue](MASTER_IMPLEMENTATION_SPECIFICATION.md#40-vocal-soreness-and-fatigue) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.41 Recovery](MASTER_IMPLEMENTATION_SPECIFICATION.md#41-recovery) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.42 Vocal Babbling](MASTER_IMPLEMENTATION_SPECIFICATION.md#42-vocal-babbling) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.43 Parrot-Inspired Vocal Mimicry](MASTER_IMPLEMENTATION_SPECIFICATION.md#43-parrot-inspired-vocal-mimicry) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.44 Acoustic Comparison](MASTER_IMPLEMENTATION_SPECIFICATION.md#44-acoustic-comparison) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.45 Phonics, Phonetics, and Phonology](MASTER_IMPLEMENTATION_SPECIFICATION.md#45-phonics-phonetics-and-phonology) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.46 Intonation](MASTER_IMPLEMENTATION_SPECIFICATION.md#46-intonation) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.47 Language Grounding](MASTER_IMPLEMENTATION_SPECIFICATION.md#47-language-grounding) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.48 Contextual Understanding of Vocalizations](MASTER_IMPLEMENTATION_SPECIFICATION.md#48-contextual-understanding-of-vocalizations) | Vocal development | §§12, 29–37, 157, 163 | Not started | — |
| [A.49 Three-Library Integration](MASTER_IMPLEMENTATION_SPECIFICATION.md#49-three-library-integration) | Libraries and mentorship | §§45–60, 105 | Partial | [L](#l-evidence) |
| [A.50 Auditory Mental Workspace](MASTER_IMPLEMENTATION_SPECIFICATION.md#50-auditory-mental-workspace) | Libraries and mentorship | §§45–60, 105 | Not started | — |
| [A.51 Reflective Replay](MASTER_IMPLEMENTATION_SPECIFICATION.md#51-reflective-replay) | Libraries and mentorship | §§45–60, 105 | Experimental | [D](#d-evidence) |
| [A.52 Mentor–Squire Integration](MASTER_IMPLEMENTATION_SPECIFICATION.md#52-mentorsquire-integration) | Libraries and mentorship | §§45–60, 105 | Partial | [T](#t-evidence) |
| [A.53 Verify Existing Unity Trace](MASTER_IMPLEMENTATION_SPECIFICATION.md#53-verify-existing-unity-trace) | Observatories | §§124–134, 150 | Verified | [D](#d-evidence) |
| [A.54 Neural Observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#54-neural-observatory) | Observatories | §§124–134, 150 | Partial | [O](#o-evidence) |
| [A.55 Auditory Observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#55-auditory-observatory) | Observatories | §§124–134, 150 | Not started | — |
| [A.56 Vocal Apparatus Observatory](MASTER_IMPLEMENTATION_SPECIFICATION.md#56-vocal-apparatus-observatory) | Observatories | §§124–134, 150 | Not started | — |
| [A.57 Discovery and Curiosity Experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#57-discovery-and-curiosity-experiments) | Scientific experiments | §§77–81, 156–158 | Experimental | [D](#d-evidence) |
| [A.58 Mimicry Experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#58-mimicry-experiments) | Scientific experiments | §§77–81, 156–158 | Partial | [T](#t-evidence) |
| [A.59 Vocal Experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#59-vocal-experiments) | Scientific experiments | §§77–81, 156–158 | Not started | — |
| [A.60 Hearing Experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#60-hearing-experiments) | Scientific experiments | §§77–81, 156–158 | Not started | — |
| [A.61 Vocal Physiology Experiments](MASTER_IMPLEMENTATION_SPECIFICATION.md#61-vocal-physiology-experiments) | Scientific experiments | §§77–81, 156–158 | Not started | — |
| [A.62 Scientific Integrity](MASTER_IMPLEMENTATION_SPECIFICATION.md#62-scientific-integrity) | Scientific experiments | §§77–81, 156–158 | Partial | [Z](#z-evidence) |
| [A.63 Computational Requirements](MASTER_IMPLEMENTATION_SPECIFICATION.md#63-computational-requirements) | Performance and persistence | §§53–58, 135–148, 163 | Partial | [P](#p-evidence) |
| [A.64 Persistence](MASTER_IMPLEMENTATION_SPECIFICATION.md#64-persistence) | Performance and persistence | §§53–58, 135–148, 163 | Partial | [S](#s-evidence) |
| [A.65 Dependency and Licensing Review](MASTER_IMPLEMENTATION_SPECIFICATION.md#65-dependency-and-licensing-review) | Performance and persistence | §§53–58, 135–148, 163 | Experimental | [U](#u-evidence) |
| [A.66 Development Workstreams](MASTER_IMPLEMENTATION_SPECIFICATION.md#66-development-workstreams) | Delivery and verification | §§168–179, 181–186 | Partial | [Z](#z-evidence) |
| [A.67 Immediate Next Milestones](MASTER_IMPLEMENTATION_SPECIFICATION.md#67-immediate-next-milestones) | Delivery and verification | §§168–179, 181–186 | Partial | [Z](#z-evidence) |
| [A.68 Required Deliverables](MASTER_IMPLEMENTATION_SPECIFICATION.md#68-required-deliverables) | Delivery and verification | §§168–179, 181–186 | Partial | [Z](#z-evidence) |
| [A.69 Immediate Assignment](MASTER_IMPLEMENTATION_SPECIFICATION.md#69-immediate-assignment) | Delivery and verification | §§168–179, 181–186 | Partial | [Z](#z-evidence) |

## Evidence register

The playable household adds shared fixed-tick access to real object state, autonomous actions, teacher demonstrations, separate learner witnesses and imitation outcomes, optional presentation, and persistent replay references. Retained identity, observer independence and legacy save migration have measured evidence. Licensed import, native grasp and full developmental locomotion remain open. [Household crosswalk and actual Unity evidence](PLAYABLE_HOUSEHOLD.md#verification)

Phase C.1.2 adds evidence for §§14, 17–19, 116, 128, 148, 150–151, 153, 157 and 170. XCoM, two signed support margins and absolute impulse gate native staged movement. Four held-out combined trials held the intended swing foot above 10 mm while supported. Forward placement and controlled return failed, including two prelift falls. No complete step or learned movement is credited. [Phase C.1.2 crosswalk](LOWER_BODY_C12.md#requirements-crosswalk)

Phase C.1.1 extends §§12–19, 116, 124–125, 128, 148, 150–151, 153, 157 and 170. Native frontal joints and bounded actuation pass six held-out 85% stance windows. Engineered sensor feedback passes through 80%. Vestibular and proprioceptive timing, CoP measurement, and provenance are exercised in isolated experiments. Six gated physical swings aborted on lost stance contact. The separate VRM audit measures lower-body rest transforms and skin references with permissions still unresolved. [Crosswalk and measured outcomes](LOWER_BODY_C11.md#requirements-crosswalk)

Phase C.1 extends §§12–15, 116, 124–125, 128, 148, 150–151, 153, 157 and 170. The native pelvis and six driven leg joints stand freely and transfer load in both directions. The fixed stance threshold blocks stepping. The isolated experiment produces measured collapses when drive torque is removed. The fixture is separate from household cognition, developmental motor learning and checkpoint requirements. [Phase C.1 requirements and evidence](LOWER_BODY_C1.md#requirements-crosswalk)

Phase C.0 adds evidence for §§6, 12–15, 116, 124–125, 128, 148, 150–151, 153 and 170. Isolated native family runs verify grounded pose and movement-coupled display, scene startup, layout recovery without checkpoint changes and the stable geometry anchors. C.0 also supplied the ideal static bilateral support reference now used for C.1. [Audit and screenshots](FUNCTIONAL_EMBODIMENT_C0.md)

Phase B.1 extends §§14, 16–21, 57, 124, 128–129, 148, 157 and 170 with a separately scored impulse predictor, bounded and uncertainty-gated corrections, a persisted fallback record, and predeclared matched control gates. Passing a baseline margin does not establish a competitive force-aware advantage. Proposed §§240–316 and their v239 base remain programmatically untraceable until the authoritative texts are supplied. Existing Appendix A is unchanged. [B.1 evidence and source gap](FORCE_AWARE_PHASE_B1.md)

Phase B adds evidence for §§14, 16–21, 57, 124, 128–129, 148, 157 and 170. Actual signed contact prediction error changes a bounded corrective torque. Native comparisons cover engineered, frozen, adaptive, and numerically pretrained/calibrated controllers on complete held-out configurations. A separate Unity process continued learning from a joint physics/model checkpoint. Motor outcomes include lower work and cumulative impulse alongside higher mean tracking error and peak impulse. Receptor-valued prediction, multi-joint transfer and developmental growth calibration remain open. [Phase B experimental evidence](PREDICTIVE_MOTOR_PHASE_B.md)

Phase A.2 extends §§14–21, 57, 116, 124, 128–129, 148, 157 and 170. Native movement, topology, fixed-step scheduling and the tested restart envelope have executable evidence. Cross-backend measurement preserves contact differences. A sensory-only contact-probability predictor has observed held-out scores, negative transfer findings and restart persistence. Richer sensory targets, closed-loop motor adaptation, sustained native solver constraints and visual/resource evaluation remain partial. [Native fixture and learning evidence](NATIVE_ELBOW_LAB.md)

Phase A adds partial evidence for §§14–21, 57, 116, 124, 128–129, 148, 157 and 170. Same-runtime numerical contact, tactile localization and proprioception are verified in the bounded fixture, as are physical checkpoint continuation and read-only observation. The Unity view renders the shared numerical model. A native solver comparison and learned sensorimotor checkpoints remain open. [Phase A matrix and reproducible traces](ELBOW_LAB.md)

The workspace and filesystem follow-up adds evidence for §§57, 124, 125, 133, 150, 153, 157, 162 and 169. The Simulation tray has a stable registration, persisted visibility and normalized anchors, shared styling, and open/close/refresh hooks. Windows replacement tests and the [isolated Unity observer harness](../tools/unity/README.md) exercise the owned paths. Docking, live resizing, grouped tabs, presets and a household memory inspection interface remain separate work. The [embodiment plan](EMBODIMENT_NEXT_SLICE.md) documents future physical ownership, sensory and checkpoint acceptance under §§12–21, 119, 128–129 and 170.

The embedded v1 checkpoint adds evidence for §§5, 6, 10, 52, 57, 116, 133, 150, 157, 162 and 169. Three kernel continuations match after disk restore across a day boundary, and one paired Unity observer run matches all serialized state. The reset intervention ledger is persisted. Wider observer coverage, workspace controls, movable physics state, mobile filesystem behavior and cross-process recovery remain partial. [Checkpoint owner inventory](WORLD_CHECKPOINT.md) · [Verification results](IMPLEMENTATION_PROGRESS.md) · [Performance](PERFORMANCE_BENCHMARKS.md)

The clock checkpoint extends K evidence for §§5–7, 116, 124, 133 and 169. Pause and single-step have executed regression tests. The control panel is dismissible, and live preview and night display no longer write to authoritative learner state. Complete checkpoint and observer invariance criteria remain open. [Clock results](IMPLEMENTATION_PROGRESS.md)

| Owner addendum | Owner | Dependencies | Status | Evidence |
| --- | --- | --- | --- | --- |
| [187 Commit and push](GITHUB_SYNCHRONIZATION.md#187-commit-and-push-after-every-implementation-session) | Repository governance | §§181–186 | Verified for this session | [Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) |
| [188 Remote source of truth](GITHUB_SYNCHRONIZATION.md#188-remote-repository-as-the-source-of-truth) | Repository governance | §187 | Verified for this session | [Continuation](CONTINUATION_CHECKPOINT.md) |
| [189 Incremental checkpoints](GITHUB_SYNCHRONIZATION.md#189-incremental-checkpoint-commits) | Repository governance | §187 | Verified for this session | [Continuation](CONTINUATION_CHECKPOINT.md) |
| [190 PR synchronization](GITHUB_SYNCHRONIZATION.md#190-pull-request-synchronization) | Repository governance | §§182–185, 187 | Verified for this session | [Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) |
| [191 End-of-response checks](GITHUB_SYNCHRONIZATION.md#191-mandatory-end-of-response-verification) | Repository governance | §§178–186 | Verified for this session | [Continuity and tests](CONTINUATION_CHECKPOINT.md) |
| [192 GitHub references](GITHUB_SYNCHRONIZATION.md#192-mandatory-github-references-in-every-implementation-report) | Repository governance | §§187–191 | Verified for this session | [Clock report](IMPLEMENTATION_PROGRESS.md) |
| [193 Permanent history](GITHUB_SYNCHRONIZATION.md#193-permanent-development-history) | Repository governance | §§181–186 | Verified for this session | [Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) |

### Z evidence

[Milestone report](IMPLEMENTATION_PROGRESS.md), [continuation checkpoint](CONTINUATION_CHECKPOINT.md), [interface inventory](SYSTEM_INTERFACES.md) and [architecture](ARCHITECTURE.md). The baseline includes the five open PRs, known non-release packages and local modifications. Full quality, long-duration, CI and editor/device coverage remain pending.

### K evidence

[Shared world kernel](../Assets/DreamMachine/Core/Sim/WorldSimulation.cs), [Unity adapter](../Assets/DreamMachine/Unity/DreamMachineApp.cs), [shared Edit Mode tests](../Assets/DreamMachine/Tests/EditMode/DreamMachineTests.cs) and [interfaces](SYSTEM_INTERFACES.md). Determinism and batch stepping tests cover the existing fixed-step world. Observation, clock and motion interfaces remain incomplete. Family throughput is in the [milestone report](IMPLEMENTATION_PROGRESS.md).

### S evidence

[Agent save API](../Assets/DreamMachine/Core/Development/LifeCycle.cs), [identity binding](../Assets/DreamMachine/Core/Sim/WorldSimulation.cs) and `AgentSaveFileTests` in the [shared tests](../Assets/DreamMachine/Tests/EditMode/DreamMachineTests.cs). Old anatomy versions without the added field still load. Measured in the two suites. Writes publish atomically per individual. Full world checkpoint, transaction across the roster, RNG and wetware state, recovery from valid-looking data corruption and restoration latency metrics remain open.

### T evidence

[Legacy household and teacher selection](../Assets/DreamMachine/Core/Social/Society.cs), [nursery development](../Assets/DreamMachine/Core/Development/LifeCycle.cs), [pilot teacher trace](../Assets/DreamMachine/Core/Experiment/LatchRoomSimulation.cs) and [research protocols](EXPERIMENTAL_PROTOCOLS.md). The pilot uses an engineered demonstration and compact learned actions. Physical imitation, independent pedagogy, and validated inheritance have no measured result.

### R evidence

[Anatomical profile and Unity rig](HUMANOID_RIG.md). Shared tests exercise 64 seeds, sleep-aligned growth, changing reach and old/new save reconstruction. Isolated DX12 play measured about 0.150 m newborn reach and 0.697 m mature reach. The live observatory is an anatomy inspector. Collider envelopes are inactive. Contact, skin, motor learning and panel cost remain open.

### V evidence

[User asset verification and usage terms](ASSET_AND_LICENSE_REGISTRY.md). Four VRM 1.0 sources and their manual mappings were inspected. About 73.4 MB on disk total. Import, shader validation, retargeting, runtime measurement and licensed growth variants remain pending. The running roster still uses legacy household roles and default names.

### U evidence

[Pink Trombone dependency review](PHASE_II_DEPENDENCIES.md) and [offline experiment](PHASE_II_RESULTS.md). A one-second mono render took a median 71.6 ms at 24 kHz and 140.5 ms at 48 kHz in the scratch browser. Unity integration, audition, continuous acoustics and vocal learning remain open.

### B evidence

[Reduced-order physiology and neural foundations](RESEARCH_INTEGRATION.md), [shared tests](../Assets/DreamMachine/Tests/EditMode/DreamMachineTests.cs) and [interfaces](SYSTEM_INTERFACES.md). Existing fatigue, energy, age, injury and modeled internal signals lack full empirical calibration. Per-system CPU and allocation costs remain unmeasured.

### L evidence

[Three-Library Observatory and archive](LIBRARY_OBSERVATORY.md), [archive implementation](../Assets/DreamMachine/Core/Libraries/PilotLibraryArchive.cs) and `PilotLibraryArchiveTests` in the [shared tests](../Assets/DreamMachine/Tests/EditMode/DreamMachineTests.cs). The report contains storage measurements and cross-runtime tests. Referential integrity and replay are demonstrated for the seeded latch recipe. A complete multimodal history, family checkpoint and cognitive retrieval model remain open.

### D evidence

[Latch protocol and earlier results](EXPERIMENTAL_PROTOCOLS.md), [Discovery Room scoring and trace verification](PHASE_II_RESULTS.md), and [rerun](IMPLEMENTATION_PROGRESS.md). Both probes again hit a ceiling in all four original 32-seed conditions. The documented short-budget Discovery Room effect applies to this toy prior and task. No physical learning or independent replay improvement follows from the results.

### W evidence

[Element grid](../Assets/DreamMachine/Core/Elements/ElementGrid.cs), [shared tests](../Assets/DreamMachine/Tests/EditMode/DreamMachineTests.cs) and [existing architectural limits](ARCHITECTURE.md). Approximate cellular phenomena operate at compact grid fidelity. Unified materials, world promotion and region recovery remain open. The throughput benchmark includes the 16×16 Laptop grid.

### O evidence

[Unity adapter](../Assets/DreamMachine/Unity/DreamMachineApp.cs), [library UI](LIBRARY_OBSERVATORY.md) and [body inspector](HUMANOID_RIG.md). Existing windows visualize selected telemetry. A common runtime docking registry, fully mapped brain observatory, subscription budgets and accessibility tests remain open.

### P evidence

[Performance protocol and historical runs](PERFORMANCE_BENCHMARKS.md) and [new baseline](IMPLEMENTATION_PROGRESS.md). Tick benchmarks and existing per-frame scheduling budgets are available. The central multi-resource governor, matched editor/Android performance measurements and named CI gates remain open.

### E evidence

[Research boundaries and ethical considerations](ETHICAL_CONSIDERATIONS.md) and [matched experiment design](EXPERIMENTAL_PROTOCOLS.md). Provenance and safeguards are documented with bounded pilots. Full operational oversight, intervention recovery and age-appropriate artificial-origin inquiry await their dependent milestones.

### G evidence

[Active branch rules](IMPLEMENTATION_PROGRESS.md) and [continuation checkpoint](CONTINUATION_CHECKPOINT.md). GitHub confirmed the active main rules on November 26, 2026. Owner-controlled merging remains in effect.


## Phase B.2 & Extended Master Specification (§§187–362)

| Req ID | Summary | Status | Evidence / Implementation |
| --- | --- | --- | --- |
| §§187–210 | Phase B.2 Decomposed Residual, Safeguards, A–B–A Retention, Fallback Ledger | Implemented (Core + Tests) | `Assets/DreamMachine/Core/Body/DecomposedForceResidual.cs`, `RetentionExperiment.cs`, `DecomposedForceResidualTests.cs`, `RetentionExperimentTests.cs` |
| §§211–260 | Three-Library $2^3$ Factorial Study Harness | Implemented (Core + Tests) | `Assets/DreamMachine/Core/Libraries/LibraryFactorialStudy.cs`, `LibraryFactorialTests.cs` |
| §§261–348 | Developmental Embodiment, Senses & Town Director | Not started / Experimental | Planned for subsequent phases |
| §§349–362 | Unity CLI/MCP, Agent Interop, Security & Reproducibility | In progress | `AGENTS.md`, `unityMCP` console/refresh integration |
| Track A | `ShoulderElbowWristLab` + `SolveEngineeredIK` | Implemented (.NET Core + Tests); Unity verification pending | `Assets/DreamMachine/Core/Body/ShoulderElbowWristLab.cs`, `ShoulderElbowWristLabTests.cs` |
| Track C | `IntegratedThreeLibrary` vs `UnifiedMemoryBaseline` | Implemented (21/24 vs 21/24; held-out eval pending) | `Assets/DreamMachine/Core/Libraries/LibraryFactorialStudy.cs` |

## Phase L.2 and character model clarification

| Requirement | Dependency | Status | Implementation and verification |
| --- | --- | --- | --- |
| L.2 §§1–2, 12, 18 · Preserve original research, identity and checkpoints | Shared clock, atomic checkpoint and founding cast | Verified on fixtures | `WorldCheckpoint.cs` and `HouseholdLife.cs` preserve v1/v2 imports and write v3. The 80 .NET and 81 Unity Edit Mode tests, DX12 restore and prior version 2 synthetic continuation passed. Long-duration bounds remain open. [Verification](ACTIVE_LEARNING_L2.md#native-verification-and-continuation) |
| L.2 §3 and clarification 1–5 · Publish the exact origin, legal basis, attribution and official import path | Owner provenance, Pixiv terms and Unity UPM | Partial | Per-file embedded identities, metadata and terms verified. Owner-installed UniVRM now resolves at its pinned release. Studio selector aliases for the three stock entries remain unverified. [Asset workflow](ASSET_AND_LICENSE_REGISTRY.md#l3-private-unity-integration) |
| Clarification 6–13 · Use supplied models in Living, preserve functional anatomy and research view | Isolated UniVRM installation, authorized local sources and age-appropriate meshes | Partial | `HouseholdAvatar.cs` uses all four validated mature meshes and switches to the procedural rig in Research. DX12 verified standing, motion, state-driven expressions and unchanged authority. Age-appropriate learner meshes and physical contact remain open. [Skinned evidence](ACTIVE_LEARNING_L3.md#native-verification) |
| L.2 §§4, 9–10 · Audit repetitive teaching, evolving interests and independent activity | Provenance and deterministic seed-matched replay | Partial | Per-person audit, bounded preference scoring and teaching cooldown in `HouseholdLife.cs`. One four-day matched run measured a rise in firsthand outcomes and a decline in demonstrations, witnesses and imitations. Personal interest trends and learner requests are open. [Audit](ACTIVE_LEARNING_L2.md#matched-household-run) |
| L.2 §§5, 11–12 · Isolate direct retrieval and replay, compare retention and transfer | Shared outcome rules and matched cohorts | Experimental | `HouseholdRetention.cs`, CLI `--retention` and `HouseholdRetentionTests`. Replay-only improves over exploration. Direct and combined conditions tie at ceiling with the supplied transform and no elapsed retention interval. Production replay uses a target hint. Learned transfer and revised plans after elapsed time remain open. [Probe results](ACTIVE_LEARNING_L2.md#retention-and-transfer-results) |
| L.2 §§6–8 · Evidence-based competence, sorting, cleanup and self-initiated activities | Stable affordance revisions and immutable experience references | Verified for the kinematic scope | Tests exercise real fits, wrong guesses, and cache reconstruction. Native activity recorded independent fits for both learners and visible state changes. Physical manipulation, recurring successful transfer in household and skill confidence remain open. [Household run](ACTIVE_LEARNING_L2.md#matched-household-run) |
| L.2 §§13–14 · Passive daily observatory and improved scene | Modular tray, kernel experience view and test harness | Partial | `DreamMachineApp.Living.cs` displays bounded tick-day history and fit/replay evidence. Native harness compared byte-identical observed and control checkpoints after tray actions. Interest history and mature skinned presentation remain open. [Native captures](ACTIVE_LEARNING_L2.md#native-verification-and-continuation) |
| L.2 §15 · C.1.3 physical step | Native support and cancellation controls | Partial | C.1.2 established supported lift. Early cancellation, return to double support, lowering and a verified step remain outstanding. [Native gates](LOWER_BODY_C12.md) |
| L.2 §§16–18 · Profile and synchronize current work through draft PR | Windows, Unity batch harness and protected branch | Partial | Numerical throughput, allocation, growth, replay, selection and CPU camera cost were measured. Long-run memory, GPU and Android limits still need verification. [Measurements](PERFORMANCE_BENCHMARKS.md#phase-l2-independent-activity) · [Continuation](CONTINUATION_CHECKPOINT.md) |
| L.3 · Delayed retention and withheld rearrangement | Shared fixed-tick clock, matched exposures and independent evaluation | Experimental | `HouseholdDelayedRetention.cs`, `--delayed-retention`, deterministic trace and scope-specific test. Positive fixed-layout retention and lower hidden-layout performance. The experiment has one delay and no decay model. [Results](ACTIVE_LEARNING_L3.md#executed-comparison) |

## Phase L.4

| Requirement | Dependency | Status | Implementation and verification |
| --- | --- | --- | --- |
| §§1–5 · Preserve identity, age and checkpoints, add interaction and developmental visuals | Native avatar binding, immutable identity and existing growth | Partial | `CharacterFactory.cs` adds original infant and toddler geometry. `HouseholdAvatar.cs` retargets reachable palms and bed-facing poses. Mature, infant and toddler private captures and checkpoint parity passed. Child and adolescent meshes, grasping, furniture contacts and measured foot slip remain open. [Private visual results](ACTIVE_PERCEPTION_L4.md#developmental-avatar-and-interaction-verification) |
| §§6–9, 13 and 15 · Inspect sockets, gate remembered actions and show actual evidence | Fixed clock, feature port, immutable decision/outcome records | Verified for the shape board | `SocketInspection.cs`, `HouseholdLife.cs`, Life & experience and v4 checkpoint tests. Three decisions occurred in the four-day native run. Source, geometry and applicability remain inspectable. Teacher queries and robust confidence under an aliased cue remain open. [Household verification](ACTIVE_PERCEPTION_L4.md#household-behavior-and-persistence) |
| §§10–12 · Compare integrated libraries and transfer | Independent baselines, a second task and partitioned memory | Experimental | `ActiveSocketStudy.cs` matches five conditions, including a unified scorer with equivalent inputs. No evidence of an architecture advantage. Full library integration and a second task family remain open. [Results](ACTIVE_PERCEPTION_L4.md#held-out-results) |
| §§14, 16–17 · Extend activity scope, grounded words and independent locomotion | Existing household and isolated C.1.3 fixture | Partial | Active inspection enriches sorting. Cooperative activities, grounded vocabulary and a physical first step remain future requirements. [Continuation checkpoint](CONTINUATION_CHECKPOINT.md) |
| §§18–20 · Profile and synchronize | Desktop DX12, .NET, GitHub draft review | Partial | Measured numerical throughput, close-look cost, checkpoint growth, restoration and CPU view and camera cost. Device GPU and Android verification remain open. [Benchmarks](PERFORMANCE_BENCHMARKS.md#phase-l4-active-inspection) |

## Phase L.5 spatial learning

| Requirement | Implementation and dependency | Status | Verification |
| --- | --- | --- | --- |
| L.5 sections 3-6, 9. Stable identity, uncertainty, bounded search and library links | Fixed-tick household approach, reached observation, typed containment projection and linked decision/lived episodes | Verified for the household fixture | Six held-out conditions, including lookalike and exhaustion. [Results and code](SPATIAL_LEARNING_L5.md#held-out-household-study) |
| L.5 sections 7, 21, 23. Restoration, visible experience and observer independence | v5 checkpoint, passive tray and native props | Verified for the household fixture | Fresh-process byte match and actual D3D12 render and observer comparison. [Validation](SPATIAL_LEARNING_L5.md#native-verification-and-cost) |
| L.5 section 8. Cheap context confirmation | Coarse visible rim and detailed fallback | Verified as a separate experiment | Five policies, held-out aliases and unchanged L.4 CSV. [Comparison](SPATIAL_LEARNING_L5.md#separate-socket-confirmation-study) |
| L.5 section 22. Cost | Release Laptop benchmark and native scratch camera | Partial | Numerical throughput, allocations, checkpoint and CPU view measured. Isolated decision, GPU and Android are open. [Costs](SPATIAL_LEARNING_L5.md#native-verification-and-cost) |
| Grounded words, precision manipulation and additional developmental meshes | Live conversation, native hand fixture, age-calibrated visual assets | Open | Kinematic reach verified. [Continuation](CONTINUATION_CHECKPOINT.md) |

## Phase C.1.3 physical recovery

| Requirement | Implementation and dependency | Status | Verification |
| --- | --- | --- | --- |
| C.1.3 sections 10-12. Preserve gates, audit deterioration and cancel early | Archived C.1.2 traces, 5 ms PhysX, support-edge and descending-clearance warnings | Verified signals and early cancellation | Both prelift cases remained unrecovered. A separate warning advanced short-swing cancellation before touchdown. [Trace audit](LOWER_BODY_C13.md#preserved-baseline-and-trace-audit) |
| C.1.3 sections 13-14, 22-23. Recovery, comparison, visual verification and cost | Bounded joint commands, plant-relative target, actual pressure continuity and bilateral gate | Verified for eight fresh swing attempts per motor policy | Native console, capture and observer control. Zero complete steps through the unchanged 80 mm gate. [Native results](LOWER_BODY_C13.md) |
| Sections 15-18. Body integrity, articulated hand, acquisition and motor vocabulary | Collider-pair audit, calibration and acquisition protocol | Design ready | No hand acquisition or body-fit fixes measured. [Body audit](BODY_INTEGRITY_AUDIT.md) [Hand plan](ARTICULATED_HAND_FIXTURE_PLAN.md) |

## Phase C.1.4

| Requirement | Dependency and implementation | Status | Verification |
| --- | --- | --- | --- |
| C.1.4 sections 4-7. Prelift diagnosis, physical braking and intact gates | Existing PhysX actuation, pressure and edge support, explicit stabilization stage | Verified for the matched native cohort | Both 1010 sides and all eight fresh perturbed attempts returned under the active policy. Complete step remains open. [C.1.4 measurements](LOWER_BODY_C14.md) |

## Phase H1 body integrity and physical manipulation

| Requirement | Dependency and implementation | Status | Verification |
| --- | --- | --- | --- |
| C.1.4 sections 8-11. Body integrity and visual-versus-physical diagnosis | Seeded source proxies, executable adjacent collision registry and private posed Unity captures | Partial | 52 imposed poses, 520 pair samples, 9 over the 2 mm reporting threshold. No moving skin or garment correction or solver claim. [Audit](BODY_INTEGRITY_AUDIT.md) |
| C.1.4 sections 12-16. Supported physical hand, calibrated contact and grasp | Native isolated constrained wrist, opposing guides and a free object under gravity | Verified for H1 fixture | Known table load, actual hold and release, native 4/4 fixed-policy fresh results and weak-grip failure. Effective pad area and angular digit dexterity open. [Physical hand](PHYSICAL_HAND_H1.md) |
| C.1.4 sections 17-20. Adaptation, retention and transfer | Measured slip update, stable individual and versioned skill | Verified for bounded scalar practice, partial transfer | Separate Unity process restored eight practice records and scored 2/2 familiar and 2/4 held-out. [Acquisition evidence](PHYSICAL_HAND_H1.md#acquisition-retention-and-transfer) |
| C.1.4 sections 21-24. Browser, movement composition, libraries and live integration | Bounded laboratory replay, typed practice records, shared clock and household-mode prerequisites | Partial | Actual laboratory playback passes observer checks. Babel composition, durable library links and learner-bound household practice remain open. [Browser](PHYSICAL_HAND_H1.md#browser-and-provenance) |

## Phase H2 persistent household practice

| Requirement | Implementation and dependency | Status | Evidence |
| --- | --- | --- | --- |
| H2 sections 4-8 and 19. Named individual, shared clock, physical practice, return | Household state with actual H1 native substeps and supported station | Verified for Arthur on the familiar sphere | Two slip failures, successful hold/release, fresh retained success, return to Living on his existing identity. [H2 validation](PHYSICAL_DEVELOPMENT_H2.md) |
| Sections 12-18 and 21. Evidence, browser, playback and restart | Version 6 world envelope, content-addressed tapes, firsthand Library III records | Verified for settled boundary and minimal tray | Save and restore, immutable playback and observed contact values. Arbitrary mid-contact restore, pressure and full browser comparisons remain open. [H2 persistence](PHYSICAL_DEVELOPMENT_H2.md#durable-evidence-and-playback) |
| Sections 9-11, 20, 22-30. Context, broader libraries and other embodiments | H1 baseline and newly verified H2 world boundary | Partial | H3 comparison, Library I and II indexing, Alice, garment corrections and composition still require independent work. [Continuation](CONTINUATION_CHECKPOINT.md) |

## Phase H3 comparative adaptive grasping

| Requirement | Implementation and dependency | Status | Evidence |
| --- | --- | --- | --- |
| H3 sections 4-7, 15-17 and M06/M12/M45 | Matched native object presets, bounded pad-sensed response and controlled trials | Verified in isolation | Fixed 6/10, trained scalar 4/10, online feedback 3/10. Fresh/reset ablation reproduced feedback exactly. [Protocol, failure modes and raw compact telemetry](PHYSICAL_HAND_H3.md) |
| H3 sections 8-14 and M02/M09/M15-M18 | Native object setup and force interface with learner-bound H2 settled evidence | Partially implemented | Shared fixture contract and actual H2 records. Household transporting, chairs, collision/material fidelity, Alice, generalized library indexing and creative tools remain dependent. [M01-M46 register](MASTER_WORKSTREAM_TRACEABILITY.md) |

## Phase H4 tactile exploration and controlled placement

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H4 sections 1–3, 9–10, 17 | Matched H3 mechanical references, development, frozen cases and scoring | Verified in isolation | H3 unchanged. Fourteen reference diagnoses, 84 development and 82 post-restart attempts. [H4 experiment](PHYSICAL_PLACEMENT_H4.md) |
| H4 sections 4–8 | Contact-local proposals, bounded effort, lateral and vertical carriage targets, wrist pitch and finite lowering | Experimental | Held-out Tactile 10/11 matched same-cap ForceOnly 10/11. Applied-load case passed; offset capsule remains open. Independent finger effort, wrist yaw and hand-eye estimates remain outstanding. [Policy](../Assets/DreamMachine/Core/Body/TactileGrasp.cs) |
| H4 sections 11–12, 16 | Actual support centroid, load transfer and settled release within a destination region | Verified in isolation | Native carriage and free object. Persistent household object binding, balance, joints, garment integrity, carrying and arbitrary native restoration are open. [Placement and dependencies](PHYSICAL_PLACEMENT_H4.md#unity-workflow-and-remaining-work) |
| H4 sections 13–14 | Independent research archives and firsthand Library III records | Verified in isolation | Arthur and Alice trained separate scalar archives and reloaded in another process. Alice has no household practice integration. Library I is linked via scalar evidence and Library II has engineered proposal traces. Generalized belief and Babel integration are open. [Retention and provenance](PHYSICAL_PLACEMENT_H4.md#retention-and-provenance) |
| H4 section 15 | Existing Unity hand lab, object queue, live placement and native replay | Verified in isolation | DX12 confirmed load, regrasp, controlled placement and playback. H2 keeps the existing household route. [Unity smoke](../tools/unity/HandBrowserSmoke.cs) |

## Phase H5 persistent household transfer

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H5 sections 1–5, 8–9, 19–20 | Existing Ball item, supported native body and settled commit | Verified and integrated | Successful transfers and off-table displacement update the scene and same household item. [Native runner](../tools/unity/HouseholdBallSmoke.cs) [H5 results](HOUSEHOLD_TRANSFER_H5.md#physical-results-and-restart) |
| H5 sections 6–7 | Bounded carriage, wrist and digit guides, quantized pickup observation and explicit destination | Partially implemented | Surface load, pitch and physical effort measured. Dynamic arm and torso, eye alignment and full environment colliders are open. [Physics bridge](../Assets/DreamMachine/Unity/NativeHandLab.cs) |
| H5 sections 10–13 | Separate sighting and attempt histories, append-only lived evidence, v7 settled persistence | Verified and integrated | Arthur and Alice use independent state. Fresh success and failure saves restore exact bytes. Library I projection and H4-to-H5 calibration still have explicit limits. [World object and observations](../Assets/DreamMachine/Core/Sim/HouseholdBallTransfer.cs) |
| H5 sections 14–15 | Once managed, actual object consequences remain in Living | Partially implemented | Shared authority permits later containers, furniture and cleanup. Independent reaching and motivation remain open. [H5 dependencies](HOUSEHOLD_TRANSFER_H5.md#unity-workflow-and-dependencies) |
| H5 sections 16–18 | Existing physical and Simulation trays, native playback, dated performance evidence | Verified and integrated | Paired observer/camera run, save, restores, UI and benchmark. [H5 report](HOUSEHOLD_TRANSFER_H5.md) |

## Phase H6 supported articulated upper body

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H6 sections 1–5, 7–9 | Physical upper arm, forearm, wrist and digits, fixed torso, configured joints and bounded torque | Verified in isolation | Fitted proxy contacts with measured duration and penetration. Dynamic torso, forearm pronation, full articulated digits and avatars remain open. [Native arm](../Assets/DreamMachine/Unity/NativeArticulatedArm.cs) |
| H6 sections 6, 10, 13–14, 18 | Geometric reach proposals, measured responses, high versus lateral elbow and separate tests | Experimental | New targets, poses, yaw and load scored. Live visual control and durable acquisition are open. [Reach proposals](../Assets/DreamMachine/Core/Body/SupportedArmReach.cs) [H6 report](ARTICULATED_UPPER_BODY_H6.md) |
| H6 sections 11–12, 15–16, 20, 22–23 | Shared world Ball, existing settlement gates, separate learner outcomes and stored histories | Partially implemented | Held and transferred weight with the higher elbow. Full placement gate remains unverified. Library III records physical outcomes, with wider learned affordances and candidate indexing open. [Physical findings](ARTICULATED_UPPER_BODY_H6.md#loaded-transfer-and-persistence) |
| H6 sections 19, 21 | Arm trajectory overlay and replay in the existing tray, native observer comparison and paired throughput | Verified and integrated | Arm motion, goal and contacts are inspectable. Desktop profiling covers physics and controller. [Unity smoke](../tools/unity/HouseholdArmSmoke.cs) |
| H6 section 17 | Reusable articulated reach, grasp, support and evidence interfaces | Partially implemented | Next establish visually grounded support and deceleration. Ordinary use of tools and furniture awaits dynamic body support. [Integration path](ARTICULATED_UPPER_BODY_H6.md#observation-and-integration) |

## Phase H7 coordinated placement

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H7 §§1–3, 9, 22–23 · recovery, diagnosis, frozen evaluation and reporting | Separate H6 development diagnosis and 33 matched fresh trials with native contact telemetry | Verified in isolation | Controller and success gates frozen before the independent run. The complete cohort includes reach and hold failures. [Results and frozen procedure](SENSORIMOTOR_PLACEMENT_H7.md) |
| H7 §§4–7 · continuous movement, deceleration, support and withdrawal | Acceleration-shaped targets, steady support checks, digit unweighting, outward-only opening and a physical withdrawal | Partially implemented | Verified from the loaded transport stage in the supported fixture. Upstream grasp and hold remain limited. [Shared controller](../Assets/DreamMachine/Core/Body/PlacementCoordination.cs) [PhysX integration](../Assets/DreamMachine/Unity/NativeHandLab.Placement.cs) |
| H7 §§8, 12 · observed consequences and hand-eye control | Bench visibility, quantized Ball estimate and instrumented orientation, separate authoritative pose | Partially implemented | Observes aftermath without using the settled world pose as learner input. Ray targeting and seam orientation are instrumented. Actual camera and hand pose with fused proprioception and confidence remain open. [Sensor boundary](SENSORIMOTOR_PLACEMENT_H7.md#development-and-mechanism) |
| H7 §§10–11, 24 · alternatives, durable acquisition and continued embodiment | High and lateral comparison, shared physical feedback, separate learner records | Experimental | No new acquired parameters or autonomous strategy selection. Matched feedback generalized within one object geometry. Live perception, loaded lateral/heavy recovery, pronation and dynamic body support are the dependencies. [Held-out results and limits](SENSORIMOTOR_PLACEMENT_H7.md#held-out-results) |
| H7 §§13–17 · arousal, suspicion, attention and physical consequence | Audited existing causal paths and registered missing dependencies | Documented | Regulatory integration and matched attention/automaticity experiments remain future work. [Master workstreams](MASTER_WORKSTREAM_TRACEABILITY.md#h7-nervous-system-and-regulatory-requirements) |
| H7 §§18–21 · independence, libraries, Unity and persistence | New policy in the existing tray, versioned passive trajectory, same settled transaction and learner-owned Library III | Integrated at the settled boundary | Observer pair, fresh desktop process and prior tape regressions exercise continuation. Library I affordance projection, Library II alternative evaluation and mid-contact continuation remain open. [Household verification](SENSORIMOTOR_PLACEMENT_H7.md#playable-household-and-persistence) |

## Phase H8 embodied depth and hand-eye coordination

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H8 §§1–3, 23–24, 28–29 · history, frozen comparison and source integrity | Matched H7 and three H8 policies, 15 conditions and source-verified Unity launcher | Verified in isolation | H7 data and release scorer retained. All outcomes, configuration hashes and summary metrics are available. [Frozen protocol](VISUAL_COORDINATION_H8.md#frozen-protocol) |
| H8 §§4–9, 11, 14 · visibility, proprioception, prediction and correction | Fixed nearest-depth aperture, finite confidence, delayed samples, ideal joint forward kinematics and bounded physical descent commands | Partially implemented | Natural blockers and experimental channel filtering hide depth returns. No image recognition, optical palm orientation, articulated digits, dynamic gaze or camera-derived Ball seam. [Sensor boundary](VISUAL_COORDINATION_H8.md#what-the-learner-receives) |
| H8 §§15–18 · early failures, persistence through occlusion and acquisition | Matched early-stage probes, uncertainty history, forced environmental change and measured reacquisition | Experimental | Descent feedback cannot repair upstream reach/lift gates. Fixed calibration and transient feedback remain distinct from retention, acquisition reset and transfer. [H8 comparison](VISUAL_COORDINATION_H8.md) |
| H8 §§10, 12–13 · active perception and regulated attention | Fixed anatomy-bound eye, explicit source time, confidence and model discrepancy | Partially implemented | Add intentional head/gaze motion and limited attention allocations linked to task choices. Preserve separate arousal, uncertainty, suspicion, motivation and valence. [Regulatory architecture](MASTER_WORKSTREAM_TRACEABILITY.md#h7-nervous-system-and-regulatory-requirements) |
| H8 §§19–20, 25–27 · learner independence, libraries, replay, persistence and performance | H8 tray, individual depth/timestamp tapes, measured spatial update, shared settled transaction and passive replay | Integrated at the settled boundary | Library III owns observations and consequences. The bounded runtime prediction is transient. Library I acquired calibration, Library II stored candidate alternatives and mid-contact pose recovery remain open. [H8 implementation and UI](VISUAL_COORDINATION_H8.md) |
| H8 §§21–22, 30 · indirect views, creative manipulation and onward integration | Depth observation contract accepts alternative geometry views and their timestamps | Dependency documented | Reflective depth, self-correspondence, adaptive expressive tool control, dynamic posture, forearm rotation, balance and room mobility still need their own physical comparisons. [Continuation checkpoint](CONTINUATION_CHECKPOINT.md) |

## Phase H9 dynamic seated torso and moving perception

| Requirement | Implementation | Status | Evidence and dependencies |
| --- | --- | --- | --- |
| H9 §§1–3, 26–27, 30–31 · preservation, methodology and measurements | Verified-source Unity audit, unchanged release scorer and comparison with H8 | Verified in isolation | The study records posture, contacts, sense, actuation, endpoints, timing and failures. [Frozen comparison](DYNAMIC_SEATED_SUPPORT_H9.md#frozen-comparison) |
| H9 §§4–9, 13–17 · coupled anatomy, postural effort and physical support | Native pelvis anchor, compliant lumbar torque, torso-relative shoulder, neck/head joint and physical chair contacts | Partially implemented | The pelvis is fixed. Separate reactions distinguish the seat from the lumbar anchor. Joint ledger, work and contact evidence are recorded. [Physical chain](DYNAMIC_SEATED_SUPPORT_H9.md#physical-chain-and-sensing) |
| H9 §§10–12 · moving view, vestibular inputs and gaze | H8 depth capture from head pose, sampled position with capture-time extrinsics, angular and acceleration proxies | Partially implemented | Calibration and upright references are engineered. Ocular articulation, active gaze choice and vestibular acquisition remain open. [Physical sensing](DYNAMIC_SEATED_SUPPORT_H9.md#physical-chain-and-sensing) |
| H9 §§18–21, 25 · regulation, curiosity, automaticity, acquisition and creative prerequisites | Bounded automatic postural controller runs independently of depth cadence. Readiness and attention requirements remain registered. | Experimental | Retained compensation, motivational involvement, gaze and resource competition need separate studies. Seated positioning, manual artistic control and creative autonomy remain later dependencies. [Master workstreams](MASTER_WORKSTREAM_TRACEABILITY.md#h9-postural-integration) |
| H9 §§22–24, 28–29 · independence, libraries, household demonstration, restoration | H9 torso conditions use the existing learner transactions, camera-independent depth, settled results and posture playback | Integrated at the settled boundary | Actual physical outcomes, individual sights and recorded body poses survive with a versioned tape. Personal calibration, native mid-contact state, generalized Library I knowledge and Library II generated alternatives remain open. [H9 workflow](DYNAMIC_SEATED_SUPPORT_H9.md) |
| H9 §§32–33 · completion boundary and next dependency | Supported torso experiment connects to the ordinary household Ball fixture | Partially implemented | Move next to a dynamic pelvis with fixed foot contacts and measured load transfer. Independent balance, sitting transitions and gait await that coupling. [H9 report](DYNAMIC_SEATED_SUPPORT_H9.md) |

## Physical development and motor vocabulary addendum

The [nervous-system audit](PHYSICAL_NERVOUS_SYSTEM_SPECIFICATION.md#existing-system-audit) identifies existing fixture evidence and integration gaps. The [acceptance plan](PHYSICAL_NERVOUS_SYSTEM_SPECIFICATION.md#experimental-acceptance) records matched tests and scope for the related master requirements §§5–6, 12–21, 46–49, 56–57, 62–64, 92, 128–132, 137, 156–158, 170, 172 and 175.

| Requirement | Dependency | Status | Existing evidence and planned acceptance |
| --- | --- | --- | --- |
| Physical Development Mode | Shared clock, compatible checkpoints, body integrity, supported articulated-hand fixture | Partially implemented | Arthur and Alice practice at the supported station with persistent identity and an actual world-owned Ball. Dynamic torso and ordinary household mobility remain open. [Household H7](SENSORIMOTOR_PLACEMENT_H7.md#playable-household-and-persistence) |
| Persistent motor evidence and browser | Stable individual and skill IDs, tactile region data, versioned trajectories | Partially implemented | Learner-owned attempts link actual success or failure to synchronized passive playback. Skills, linked library search and broader retention, transfer and composition browsing remain open. [Record fields](PHYSICAL_DEVELOPMENT_ADDENDUM.md#motor-record-and-evidence) |
| Babel movement composition | Motor records with preconditions, isolated prediction and practice | Not started | Record a candidate sequence, a separate evaluation, and a firsthand physical outcome with traceable transitions. [Composition acceptance](PHYSICAL_DEVELOPMENT_ADDENDUM.md#babel-and-movement-composition) |
| Iterative generational learning | Provenanced acquisition, matched baselines and lineage isolation | Not started | Independently vary teaching, demonstration, cultural transmission and policy inheritance. Verify acquisition, retention, transfer and composition against matched conditions. [Generational acceptance](PHYSICAL_DEVELOPMENT_ADDENDUM.md#iterative-generational-learning) |
| Expression and regulation | Supported motor actuation, modeled internal state, sensory feedback | Not started | Demonstrate seated rocking with a measured feedback and interruption trace. [Regulation acceptance](PHYSICAL_DEVELOPMENT_ADDENDUM.md#expression-and-regulation) |
