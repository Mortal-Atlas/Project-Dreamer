# Phase C.1.2 · Dynamic support and first step experiment

## Audit before redesign

At tick 280 in the six preserved C.1.1 trials, stance impulse was 4.22–4.33 N·s, both feet touched down and lateral COM speed was approximately zero. The first 50 ms command raised the swing knee reference from 0.090 to 0.319–0.354 rad, changed hip and ankle references by approximately 0.11–0.14 rad, and was followed by 0.016–0.017 m/s forward COM speed. Stance impulse initially rose. By tick 283 the intended swing foot was briefly unloaded. It reloaded as the body developed 0.134–0.169 m/s lateral speed away from the stance side at tick 288. The late lowering command nearly extended the swing knee. At tick 295 stance impulse had fallen to 0.32–0.47 N·s and the opposite foot took 5.44–5.86 N·s. At tick 296 the stance impulse was zero and its sole was **6.59–9.45 mm above the floor**, beyond the 1 mm contact offset. This was physical separation by the recorded boundary. The intended stance foot rose. Stance drift reached 15.95–21.22 mm. The trace has boundary gyro and effort, with no cap at the sampled boundaries, and lacks whole-body angular momentum and exact first-separation substep. Five trials recorded body contact at ticks 326–333 after recentering.

The first measured divergence was the swing command and ensuing COM motion while stance loading was present. The original command lowered by elapsed time and aborted after stance loss. Physics already ran ten 5 ms substeps per 50 ms clock tick with 32 position and 12 velocity iterations. Stage and contact decisions ran at 50 ms boundaries. [Preserved trajectories](evidence/phase-c11-swing-trace.csv)

## Prespecified evaluation

Use development seeds 101 and 303 and fresh held-out seeds 909, 1010 and 1111, with both stance sides. Retain the 0.05 s observation tick, original geometry, friction and drive caps. Compare the original controller, dynamic interlock alone, smooth staged movement alone, combined preparation and smooth movement, and a separate combined lift-only trial. Compare 5, 10, 12.5 and 25 ms physics steps in development and aggregate actual contact impulses over the 50 ms window.

Retain 85% loading for 20 consecutive ticks. The interlock additionally requires eight consecutive ticks with stance impulse ≥70% of static impulse, swing impulse ≤15%, reliable stance CoP in the loaded region, ≥20 mm frontal and ≥35 mm sagittal region span, stance contact in ≥80% of substeps, |lateral speed| ≤0.05 m/s, lateral XCoM margin ≥2 mm and sagittal margin ≥0, tilt ≤8°, peak actuator use ≤85%, and stance drift ≤20 mm. Missing contact data fails the gate. Only legacy and smooth-only ablations may attempt after the historical gate alone. A qualifying step additionally needs the dynamic gate, ≥10 mm actual clearance, ≥80 mm forward position at physical recontact, continued valid stance, no body impact, and ten final stable bilateral ticks. During action, require positive stance impulse ≥55% of static impulse per clock tick, through first renewed swing contact. Lift-only needs three measured ticks at ≥10 mm airborne with stance support, then controlled double support. Record abort, return and later physical fall separately. A result blocked by a gate remains in the trial denominator.

## Measurement and controller

COM velocity is whole-fixture linear momentum divided by the measured fixture mass. Angular momentum about COM is the sum of each rigid body's world-frame inertia times angular velocity and the orbital contribution `(position − COM) × mass × velocity`. Impulse and CoP are measured from native contacts. The support region is bounded along each world axis by positively loaded contact points collected across a clock interval. It is an axis-projected region. It can overestimate instantaneous support when the foot rolls or contact points change during the interval. Stance substep contact fraction uses independent callback impulse increments. XCoM is `position + velocity × sqrt(height/g)` in world X and Z, using instantaneous COM height above the plane. Each signed margin measures distance to the nearest loaded edge along that axis. Missing, degenerate or invalid support produces an unavailable margin. The pendulum approximation lacks variable-height dynamics, articulated angular momentum and out-of-plane contacts. Angular momentum is exported separately. These estimates help choose when to attempt a lift while retaining actual physical success checks. [Biomechanics references and assumptions](EXTERNAL_RESEARCH_RESOURCES.md#dynamic-stability-and-gait-initiation)

The combined variant begins by shifting measured pressure 8 mm toward the intended swing foot while both feet are supported. It then ramps load toward stance and waits for measured prelift stability. A bounded stance-knee setpoint correction and a small coordinated hip/ankle trim accompany preparation. During motion the stance ankle corrects measured XCoM and lateral velocity with an equal-and-opposite torque, capped at 6 N·m inside the original ankle actuator limit. Quintic lift, forward and lowering targets use only joint actuation. Clearance and contact measurements trigger stage changes. The forward leg can add up to 24 mm of requested lift to compensate for clearance error. The intended swing foot must hold at least 10 mm for three native observations before advancement. On abort, the joint targets ease back toward neutral. The stance reflex can operate across both grounded ankles after the second foot establishes contact and commanded lift is nearly zero. Support at return requires 12% of static weight impulse on each foot and 78% combined, tilt ≤8°, speed ≤0.05 m/s on both horizontal axes, and ten consecutive ticks. A later fall invalidates a previously reported controlled return within the trial observation.

The combined controller is an engineered laboratory reference with privileged COM velocity, XCoM and contact geometry. The low-gain sensor variant is compared separately. It reads the prior tick's body-frame gravity, gyro and filtered sole impulse, with smaller gain and angle limits than `SensorFeedback`. Neither the privileged nor sensory controller is used by household learners.

## Development observations

The first smooth pilot still reloaded the intended swing foot as the COM moved toward it. An ankle reflex reduced early lateral motion and allowed a supported lift in one stage-3 development case. Continuing the lowering target after recontact and coordinating bilateral ankle response improved the stage sequence. In the pilot preceding the final forward clearance trim, the combined controller achieved supported lift on both sides of both development anatomies, but touched down early at roughly 54–59 mm. Some trials later fell. Those interventions preceded the held-out evaluation. The thresholds stayed fixed. [Development pilot outcomes](evidence/phase-c12-pilots.csv)

### Timestep sensitivity

Each substep setting tested two development anatomies on both stance sides. Geometry, caps and solver iterations were matched. The damping discretization follows the tested substep. Contact impulses always sum over 0.05 s. Means below include the trials’ different outcomes. [Development summary](evidence/phase-c12-development-summary.csv)

| PhysX interval | Eligible at 85% | Static foot impulse per tick | Mean native tick time |
| --- | ---: | ---: | ---: |
| 5 ms | 4/4 | 4.905 N·s | 0.912 ms |
| 10 ms | 0/4 | 4.905 N·s | 0.465 ms |
| 12.5 ms | 0/4 | 4.905 N·s | 0.377 ms |
| 25 ms | 0/4 | 0.753–0.781 N·s | 0.211 ms |

At 10 and 12.5 ms, neutral standing passed the sampled force check, and all four trials made body contact after the lateral transfer. At 25 ms all four fell during standing. The 5 ms baseline alone preserved eligible stance for these matched conditions. The test cannot resolve exactly where between 5 and 10 ms stability changes. The development observation supports retaining 5 ms physics and inspecting contact fraction within each 50 ms control interval.

The original feedback controller used body gravity and gyro from the preceding 50 ms tick and filtered stance load, but greater correction amplitudes destabilized transfer. In development the low-gain revision cleared the interlock for both mature rig sides, where the original failed to sustain the threshold. Both feedback variants failed on the stage-3 anatomy. Stage-3 readings of 100% occurred after falling, with less than 1 N·s stance impulse. These measured cases favor direct orientation- and load-aware filtering with explicit support checks and leave general sensor-only balance open.

## Held-out results

Seeds 909, 1010 and 1111 contributed both sides to each comparison. Eligible trials started from qualified C.1.1 stance or, for the combined variants, verified double support. The next column counts trials that never began a lift, including falls during preparation. Fall counts include the follow-up period. [Per-trial summary](evidence/phase-c12-heldout-summary.csv) · [Transition and impact samples](evidence/phase-c12-heldout-events.csv)

| Controller or ablation | Eligible | No swing | Attempts | Supported lifts | Forward / steps | Aborts | Falls |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Legacy | 6 | 0 | 6 | 0 | 0 / 0 | 6 | 6 |
| Interlock only | 6 | 0 | 6 | 0 | 0 / 0 | 6 | 6 |
| Smooth only | 6 | 0 | 6 | 0 | 0 / 0 | 6 | 6 |
| Prepared and smooth | 6 | 2 | 4 | 4 | 0 / 0 | 4 | 6 |
| Prepared lift-only | 6 | 2 | 4 | 4 | 0 / 0 | 0 | 6 |
| Original sensory feedback, interlocked | 2 | 4 | 2 | 0 | 0 / 0 | 2 | 5 |
| Low-gain sensory feedback, interlocked | 6 | 0 | 6 | 0 | 0 / 0 | 6 | 6 |

All four admitted combined trials had full substep stance contact at lift onset, with stance impulse **4.228–4.687 N·s**, frontal XCoM margins **18.1–52.3 mm** and sagittal margins **57.1–81.3 mm**. Peak actuator fraction at onset was 0.116–0.271. All four held the swing sole above 10 mm for at least three observations while the stance foot retained valid support. The maximum combined clearance was **17.04 mm** and maximum forward displacement **57.14 mm**. Each forward attempt touched down early, ended its command, and later fell. Seed 1010 fell on both sides during prelift, before any swing command. The interlock correctly prevented a swing in those trials. Preparation itself needs to intervene earlier. The lift-only variant held clearance but never returned to sustained bilateral support. The study logged no controlled return and no complete step. The maximum measured combined body impact was 30.66 N·s for a 50 ms window.

The interlock alone admitted all six legacy swings, which each lost the stance contact. Smooth-only trials aborted on low absolute stance impulse before losing contact during the active swing and eventually fell. The combined variant preserved stance through the supported lift and early swing at the sampled boundaries. It did not reduce trial-level falls. Low-gain sensory feedback increased eligibility from two to six. All six of its admitted legacy swings subsequently lost stance contact and fell. This distinguishes improved load admission from improved locomotion or safety.

## Verification and next experiment

The observatory displays measured load and absolute impulse, measured CoP and body state, estimated XCoM and signed margins, requested target, gate failures, stages, joint caps and fall impact. Schema 3 adds the measured and estimated data without overwriting prior traces. The full development and held-out per-tick files and failed candidates are archived in the owner's `phase-c12` experiment folder. [Unity screenshot](evidence/phase-c12-observatory.png) · [Reproduction commands](../tools/unity/README.md)

Release .NET passed 74 tests and Unity Edit Mode passed 75. The four shared numerical tests exercise signed margins, missing pressure and contact, absolute impulse and motion ramp bounds. Isolated DX12 exercised native phase transitions, the observer and four-tick schedule comparison, impulse aggregation, the tray and a 281-tick schema 3 export. The 200-tick comparison had zero measured difference in pelvis position, left-foot impulse and angular momentum. The family smoke passed and control and observed household restores produced the same byte digest at tick 1,140. [Benchmarks](PERFORMANCE_BENCHMARKS.md#phase-c12-dynamic-support)

Keep the 5 ms physics step and the supported lift as the baseline. Add a monitored return to two-foot support during preparation if the dynamic margin trends toward the edge before admission. Characterize recovery separately on the withheld failure conditions, then test on new seeds. For the forward stage, track actual swing height relative to pelvic descent, preserve stance-ankle authority through landing, and use a planted-foot-referenced placement target. Re-evaluate the same 85% load, 10 mm clearance, 80 mm travel, contact, and stable return gates. A longer motion window alone would leave the observed early contact mechanism unresolved.

The four owner-provided VRMs retain their recorded per-file restrictions and read-only audit. No authorization for a rendered import or skinning preview was established. The native Unity screenshot verifies the procedural physical rig. Authorized preview, visual skinning and retargeting remain a separate workstream. [Avatar compatibility and permissions](ASSET_AND_LICENSE_REGISTRY.md#phase-c11-lower-body-retargeting-compatibility)

## Requirements crosswalk

| Directive | Implementation and measurement | Status |
| --- | --- | --- |
| 1–4 and 18–19. Recovery, prior evidence, audit, sources and repository delivery | [C.1.1 trajectories](evidence/phase-c11-swing-trace.csv), [audit](#audit-before-redesign), [source registry](EXTERNAL_RESEARCH_RESOURCES.md#dynamic-stability-and-gait-initiation), [checkpoint](CONTINUATION_CHECKPOINT.md) | Audit and source registration complete. Prior results preserved. |
| 5–8 and 10. Dynamic estimates, preparation, interlock, stages, timestep | [Numerical support math](../Assets/DreamMachine/Core/Body/DynamicSupport.cs), [native stages](../Assets/DreamMachine/Unity/NativeLowerBodyStep.cs), [development study](../tools/unity/LowerBodyDynamicStudy.cs) | Verified diagnostics, matched timestep comparison and stage boundaries. Preparation fell on one held-out anatomy. |
| 9 and 11–14. Smooth motion, feedback, independent tests, physical completion, abort | [Held-out summary](evidence/phase-c12-heldout-summary.csv) and [controller](../Assets/DreamMachine/Unity/NativeLowerBodyBalance.cs) | Supported lift verified. Forward, step and controlled return failed. |
| 15. Preview and avatars | [Asset audit](ASSET_AND_LICENSE_REGISTRY.md#phase-c11-lower-body-retargeting-compatibility) | Authorized import blocked by unresolved asset terms. |
| 16–17. Observability, validation and performance | [Schema 3](../Assets/DreamMachine/Unity/LowerBodyTrace.cs), [numerical regressions](../Assets/DreamMachine/Tests/EditMode/DynamicSupportTests.cs), [benchmarks](PERFORMANCE_BENCHMARKS.md#phase-c12-dynamic-support) | Measured and estimated data exported independently of cognition. |
