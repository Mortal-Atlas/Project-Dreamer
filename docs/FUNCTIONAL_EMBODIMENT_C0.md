# Phase C.0 · Family standing and locomotion baseline

## Audit and acceptance

The owner handoff `4f03b84a842eed4db1a447806b6629273262641b` was the synchronized branch head at start. The owner editor was open in Edit Mode. Unity MCP connected and reported no compilation in progress. The sampled error list contained Unity cloud-service `NoSubscription` messages. Baseline verification used a disposable native DX12 Play Mode project with the real `Assets/Scenes/DreamMachine.unity` and seeded default family. The tracked local ProBuilder preference was excluded. Baseline suites passed 67 .NET and 68 Unity Edit Mode tests. The owner editor and saves were untouched.

| Pipeline | Baseline observation and cause |
| --- | --- |
| Skeleton and appearance | `CharacterFactory` builds independently seeded torso, pelvis and head dimensions and an ordered pivot hierarchy. Limbs use local X-axis rotations. Art consists of parented capsules, cubes and spheres with disabled collision envelopes. There is no production ragdoll, native joint constraint, skinned avatar, Animator or animation clip. |
| Growth | Learners resample seed-based dimensions at 1/64-day increments. Teachers use mature dimensions. The newborn and adult height samples were distinct and consistent with the generator. |
| Rest | The meal branch sent a 1.15 rad knee angle and −1.0 rad hip angle for teachers while the pelvis remained at straight-leg height. The feet had no ankle counter-rotation or planted target. Learners displayed raw walking-genome joint poses even with no actual root travel. Crown height remained close to the anatomy while the folding legs left a conspicuous ground gap. |
| Walking | Teacher follow used position interpolation and a routine-wide walk loop independently. A teacher could reach its goal and keep cycling. Learner root motion already comes from the kernel. Its display pose came from a separate oscillator driven by absolute time. The old path clamps world bounds without checking the visible house and prop footprints. |
| Balance and development | Body colliders are disabled. There was no stance measurement, measured contact, COM solver, genuine falls, recovery, or crawl state. Upper-limb IK belongs to the separate engineered lab. |
| Controls and rendering | Simulation was initially closed and the HUD button opened it by invocation. Seven action buttons were stacked because their layout routine was never called. The time label retained default offsets. `N` was dispatched by the app but absent from the Input System map. The ground shader read `_BaseColor` while the app set `_Color`, resulting in a white yard. |

Acceptance before candidate testing used a neutral knee cap of 7°, seeded standing-height loss and foot error below 2 mm, at least 5 cm teacher travel during the sampled 1.5 s Play window, and no time-driven leg cycle for stationary bodies. The analytical straight-line stance target holds position within 10 µm in a fixed-height sample. The native screenshot shows the actual kinematic rig. The force, COM, and falls gates are reserved for the physical lower-body lab.

## Corrected reference

The shared two-segment inverse kinematics solves hip and knee angles from a flat ground target; the ankle counter-rotates to keep the shoe level. On neutral ground, the pelvis remains at 99.9% leg extension to avoid a locked knee. In travel, stance phase is driven by distance actually moved. The planted foot target offsets the straight-line root displacement; the swing target advances with positive lift. Finger anchors and sole landmarks are stable children of each hand and foot, with seed-based offsets that resample with growth. These anchors support future visual adapters and contact mapping. They do not articulate fingers or change the saved identity or motor genome.

Teachers now approach their goal at bounded speed during Play and stand at other times. Known yard props and the house are swept in the shared kernel geometry. Learners retain their existing learned speed input and fixed-step root progression, now clipped by obstacles. Legacy overlaps can move out. The application initializes learner transforms from the authoritative loaded coordinates rather than the social slots. Root travel drives the view, with a 0.1 mm activation threshold per display update. A learner that has no positive walking request stays neutral. Turns and starting/stopping can still cause foot sliding. Teacher movement is a display-only engineered baseline. A neutral standing reference is used outside movement, including the old unsupported hover/sit intervals. An upright newborn reference is not developmental standing ability.

**Simulation** and **Reset layout** are pinned outside the crowded HUD row. New layouts start open. Existing intentional closed preferences can be reopened via the button or **F6**. **Reset layout** or **F7** reapplies the default tray layout and shows it. The layout recovery changed none of the paused world checkpoint bytes. The **N** mapping drives the existing single-step request. The body observatory now reads actual pivot heights, knee angles, reference support/swing and the last display update's displacement.

## Matched DX12 runs

The screenshots use the real native editor-rendered viewport from the isolated project. For the standing views a matching scripted camera frames the father's body and hides canvases. Props can partly obscure the shoes. The controls captures show the common overview camera.

[Standing before](evidence/phase-c0-before-standing.png) · [Standing after](evidence/phase-c0-after-standing.png) · [Controls before](evidence/phase-c0-before-controls.png) · [Controls after](evidence/phase-c0-after-controls.png) · [Walking sample after](evidence/phase-c0-after-walking.png)

| Measure | Baseline | Corrected |
| --- | ---: | ---: |
| Father left knee at tick 120 | 1.150 rad (65.9°) | 0.08961 rad (5.1°) |
| Mother left knee at tick 120 | 1.150 rad | 0.08961 rad |
| Father left ankle at tick 120 | 0.24287 m | 0.05818 m (design ankle height) |
| Mother left ankle at tick 120 | 0.29354 m | 0.06772 m (design ankle height) |
| Corrected crown heights, father / mother | 1.61595 / 1.88091 m | 1.61520 / 1.88000 m |
| Father displacement ticks 240–270 | 0.00000 m | 0.55549 m |
| Mother displacement ticks 240–270 | 0.00040 m | 0.58154 m |
| Son displacement and knee change in the same window | 0 m, −0.05539 rad | 0 m, 0 rad |
| Maximum corrected stance height residual in the measured window | — | <0.001 mm |

Run the [isolated smoke](../tools/unity/README.md) to reproduce action callbacks, layout reset, simulated time and limb checks. Three pure .NET regressions exercise the foot solve across 64 seeds and four growth stages, analytic support/slide criteria, obstacle clipping, and the isolated support reference. Existing deterministic checkpoint and Phase B.2 regressions continue passing. The separate B.2 validation reported frozen peak impulse 0.2229 versus 0.7333 N·s engineered, with worse tracking and work. The three-library benchmark still reports 15/24 unintegrated and 21/24 integrated or unified, with 27 and 24 operations respectively. These results remain scoped to those historical protocols.

## Isolated C.1 starting point

`dotnet run --project dotnet/DreamMachine.Cli -c Release -- --lower-body` runs an independent bilateral support reference without the household. For seed 101 it checks newborn, mid-growth and mature geometry with a prescribed 10 kg test load. The centered case gives 49.03 N per foot, a quarter-hip-width left shift gives 73.55 / 24.52 N, and a load over the single stance foot gives 98.07 N. Moving that load over the raised swing foot falls outside the support region. The force values are analytical static weight estimates. They are not PhysX ground reaction measurements or evidence of standing stability in a physical body.

Next build an isolated local physics scene with a bilateral pelvis, hip, knee and ankle mechanism. Measure contact impulse over the fixed step and compare a supported static load against the numerical reference before introducing weight transfer and a perturbation. Native balance, falling, recovery, crawling, Android display behavior, major rig aesthetics and authorized mesh retargeting remain unverified.
