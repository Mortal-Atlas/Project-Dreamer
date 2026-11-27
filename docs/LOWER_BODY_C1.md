# Phase C.1 · Isolated physical lower body

## Protocol fixed before the native runs

Use the Phase C.0 analytical rectangular support reference and a 10 kg test system. A 0.05 s tick under 9.80665 m/s² corresponds to 4.9033 N·s of vertical loading. Compare measured feet impulses to gravity **plus** the measured change of system momentum. Preserve the analytical geometry and the household experiment independently.

| Gate | Criteria |
| --- | --- |
| Standing and momentum | After 40 ticks of settling, stand for 120 consecutive ticks (6 s). Both feet must carry positive normal impulse on at least 95% of the ticks. No non-foot ground contacts. Pelvis height within 0.025 m of the seed's neutral geometry, pelvis tilt ≤8°, knees above 0 rad and below 0.30 rad, native joint-limit overshoot ≤0.025 rad. RMS three-axis momentum residual ≤0.15 N·s per native tick. |
| Contact quality | Maximum box-sole penetration ≤0.008 m and foot drift from initial contact ≤0.020 m. No actuator cap use on more than 5% of the sampled ticks. Symmetric mean left share (I_L/(I_L+I_R)) within 0.10 of 0.5. |
| Load transfer | Use symmetric, left, re-centred, and right settled samples. Left/right conditions each shift the intended mean load share by at least 0.12 from the symmetric result. Re-centred mean within 0.10 of 0.5. Native share and analytical share from the measured COM projection agree within 0.12 during settled windows. Standing geometry, slip, contact and effort protections remain in force. |
| Step prerequisite | A verified stance foot must carry at least 85% of mean support in the final transfer window without breaking the standing protections. Until then, do not run a commanded step. For a later step, require at least 0.08 m actual swing-foot advancement, at least 0.01 m clearance, recontact and at most 0.020 m stance slip. These limits distinguish planted locomotion from visible cycling. |
| Repeatability and budget | With identical configuration and fixed ticks, compare an observed run against an unobserved run to within 0.001 m pelvis displacement and 0.08 N·s per-foot impulse. Measure warm native stepping separately from observation and household throughput. Use an exploratory budget of 4 ms/tick mean on the Windows development editor. |

Run configurations 101 and 202 plus an independent seeded size 303 sampled with stage 3 dimensions. Gate failure preserves the fixture and trace. Physical falling and recovery do not proceed until stance and feasible unloading are established.

### Development change before the final transfer run

The first stable controller's sliding carriage reached ±0.60 hip widths. It was too short for the bilateral transfer gate. The follow-up increases the range to ±0.78 hip widths, retaining the same mass and actuator cap. This gives an ideal COM excursion of 0.21 hip widths with all other masses held still. Add previously unused seeds 404 (adult) and 505 (stage 3) to the three development configurations for final evaluation. The gates above stay fixed. The bounded post-standing perturbation pilot measures response to known external impulses and disabled drives. It cannot substitute for a commanded step or a recovery experiment.

## Fixture and measurement

The laboratory owns an isolated PhysX scene with free rigidbodies and ten 0.005 s substeps per 0.05 s experiment tick. Every body uses gravity. The floor surface is at Y=0 on an 8 × 0.2 × 8 m box. The mass model uses seeded thigh and shin length, hip width, ankle height and foot length from `HumanoidAnatomy`. The pelvis weighs 2.50 kg, sliding ballast 3.00 kg, and each thigh, shin and foot weighs 1.15, 0.65 and 0.45 kg respectively. Dimensions and collision geometry match the displayed boxes. Each body's center of mass is at its box center, with principal inertia from the solid box formula, `Ixx = m(h²+d²)/12`, and cyclic equivalents for Y and Z. Rigidbodies have zero passive linear and angular damping. Floor and segments use zero bounce, 0.85 static friction and 0.68 dynamic friction by default. Segment pairs ignore self-collision. Every segment can contact the floor.

The six native hinges rotate about their local sagittal X axes. Hip travel is −0.75 to +0.65 rad, knee travel +0.015 to +1.28 rad and ankle travel −0.55 to +0.55 rad. The respective torque limits are 27, 27 and 21 N·m. `GroundedGait` supplies neutral angles to a relative PD controller with projected inertia, stiffness 150 N·m/rad and damping 5 N·m·s/rad. A contact-gated ankle torque, capped at 12 N·m before the overall ankle cap, responds to sagittal COM offset and velocity. The ballast slides laterally inside a `ConfigurableJoint`. Commands target ±90% of rail travel, using 115 N/m stiffness, 35 N·s/m damping and a 25 N cap. Ballast forces and joint torques are applied in equal-and-opposite pairs. A deliberate researcher tap applies one recorded impulse to the pelvis. Tilting and falling remain physically possible.

Each tick sums the three components of the actual contact impulses and samples the mass-weighted COM and momentum of all eight bodies. The residual is `J_contacts + J_gravity + J_intervention − Δp`. With the project's 9.81 m/s² gravity, the static target is 4.905 N·s. Settled load share is compared with the C.0 support reference evaluated at the measured COM and measured foot centers. The analytical calculation uses idealized support geometry. In PhysX, pressure can move within the finite foot contacts. Slip is horizontal drift of each box center from its initialized stance, penetration is the deepest sole AABB point, and work sums the applied drive torque × joint displacement plus ballast force × slider motion over substeps. The view displays a contact sample and tick-average force. The shaded support envelope is the axis-aligned union bounding box of the loaded soles, an approximation of their support polygon.

## Findings and evidence

The explicit-drive pilot collapsed and saturated. Inertia-aware joint PD alone still let the whole body pitch forward. The first stable version added contact-gated sagittal balance, then failed the shorter rail's bilateral transfer gate. These failures informed the controller and rail changes without changing the gates. [Initial failure](evidence/phase-c1-unstable-pilot.csv) · [Shorter rail](evidence/phase-c1-short-rail.csv)

All five seeds passed standing, left transfer, re-centering and right transfer. The settled windows had 100% bilateral contacts, zero non-foot contacts, zero observed limit overshoot and zero actuator saturation. Total vertical impulse was 4.904998–4.905003 N·s per tick. The worst window RMS momentum residual was 0.000102 N·s, maximum height error 0.336 mm, maximum tilt 0.213°, highest foot drift 0.610 mm and deepest sole penetration 0.134 mm. The knees stayed between 0.094 and 0.105 rad. Typical mean absolute work was 0.29–0.56 mJ per tick.

| Condition | Native left share across seeds | Analytical left share from measured COM |
| --- | ---: | ---: |
| Centered | 47.91–48.01% | 49.93–49.95% |
| Shift left | 62.76–63.67% | 71.24–71.34% |
| Re-center | 48.04–48.14% | 49.94–49.97% |
| Shift right | 32.80–33.79% | 28.60–28.70% |

The mean load shift was at least 14.1 percentage points in both directions. The largest analytical discrepancy was 8.53 percentage points. The best stance supported 67.2%, below the 85% stepping prerequisite. No swing was commanded. The paired 200-tick comparison saw zero pelvis and foot-impulse differences between unobserved single ticks and observed four-tick groups scheduled at 4×. These are same-process, same-host measurements. [Per-seed support data](evidence/phase-c1-load-summary.csv) · [Per-tick support samples](evidence/phase-c1-load-samples.csv)

The bounded perturbations use five-second observations after establishing support. Driven 0.35 N·s lateral, forward and backward taps stayed supported. The 4 N·s lateral tap produced four ticks without bilateral readings and no body impact. A 1 N·s forward tap at 0.10 static/0.08 dynamic friction also stayed supported. Disabling only the left knee produced a fall with peak non-foot impulse of 16.82 N·s. With all six drives disabled, the pelvis descended to 0.110 m and the aggregate non-foot contact impulse reached 20.32 N·s in one tick. A positive foot impulse can still occur while the torso is on the floor. The study has yet to locate the driven controller's fall threshold. [Perturbation results](evidence/phase-c1-perturbations.csv)

## Observatory and continuation

Choose **Dream Machine → Start Lower-Body Laboratory** from the Unity Editor. The lab starts paused. Its modular tray exposes the five tested anatomies, fixed single-step, centered and lateral load commands, a separately armed tap, isolated reset and CSV export. F6 opens or closes it. The export preserves the latest 2,048 sampled ticks, including both world-space foot trajectories. Meshes follow the native bodies and have no collider role. There is no learner state, retargeted VRM, full upper-body model, or lower-body checkpoint reconstruction in this fixture. Household saves, cognition and previous experiments use their existing paths.

The tracked data include compact summaries, representative support rows, the [full perturbation trace](evidence/phase-c1-perturbation-trace.csv) and an actual [Unity Play Mode capture](evidence/phase-c1-observatory.png). Complete per-tick traces and the unsuccessful controller trials were archived under `phase-c1` in the owner's experiment backup. See the [Unity smoke tools](../tools/unity/README.md) and [timing measurements](PERFORMANCE_BENCHMARKS.md#phase-c1-native-lower-body).

The next experiment is a small frontal-plane hip and ankle controller that can unload one foot while preserving stance. Use new seeded geometries and the same standing, transfer, and 85% stance limits. Only after a passing stance, target ≥0.08 m supported advancement, ≥0.01 m swing clearance, measured recontact and ≤0.020 m stance drift. Keep native recovery, developmental locomotion and adaptive residuals separate until the engineered baseline is predictable.

## Requirements crosswalk

| Directive sections | Implementation and dependencies | Verification and status |
| --- | --- | --- |
| 1–2 and 16–17. Continue, preserve findings, progress in order and synchronize | [C.0 report](FUNCTIONAL_EMBODIMENT_C0.md), [continuation](CONTINUATION_CHECKPOINT.md), [draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) | Owner assets, saved individuals and previous evidence preserved. Family Play Mode and regression suites passed. |
| 3–6. Seeded fixture, actual support, load validation and standing | [NativeLowerBodyLab](../Assets/DreamMachine/Unity/NativeLowerBodyLab.cs) uses `HumanoidAnatomy`, `GroundedGait`, and Unity local 3D physics. [LowerBodyStudy](../tools/unity/LowerBodyStudy.cs) uses the static `LowerBodyReference`. | Native support and six-second standing passed for all five seeds. [Settled statistics](evidence/phase-c1-load-summary.csv) |
| 7–9. Transfer, step, perturbation and fall | The native ballast moves COM. The study gates controlled stepping and applies bounded impulses, friction changes and drive disabling. | Transfer passed. The 85% stance prerequisite failed. Step and recovery remain pending. Selected drive removal demonstrated a fall. [Perturbation data](evidence/phase-c1-perturbations.csv) |
| 10 and 15. Prior motor research and development | [Native elbow and motor research](NATIVE_ELBOW_LAB.md) remains a separate comparison. The lower-body fixture is driven by an engineered controller. | Developmental skill acquisition and transfer of adaptive residuals remain pending. |
| 11–13. Observatory, time and validation | [LowerBodyLabView](../Assets/DreamMachine/Unity/LowerBodyLabView.cs) reuses `TrayWorkspace` and `TimeGovernor`. [Tray smoke](../tools/unity/LowerBodyTraySmoke.cs) verifies pause, step, export and passive observation in Play Mode. | Fixed native time, isolated repeatability and same-scene visual capture verified. Native and household throughput measured separately. |
| 14. Visual model separation | [LowerBodyLabView](../Assets/DreamMachine/Unity/LowerBodyLabView.cs) renders measured native segment poses with replaceable primitives. | Physics-driven primitive adapter verified. Skinned meshes, VRM retargeting and visual fidelity remain pending. |
