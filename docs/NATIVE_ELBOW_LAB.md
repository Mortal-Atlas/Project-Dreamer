# Native elbow laboratory · Phase A.2

**Dream Machine → Start Native Elbow Laboratory** runs the native fixture. The existing **Start Elbow Laboratory** opens the unchanged numerical reference. Both use the same tray and observe the same `IElbowFixture` contract. The native lab owns a local 3D physics scene, a world-anchored Unity HingeJoint, and physical sphere colliders for the palm and puck. Rigidbody torque and collision impulses drive the observed movement. Display geometry has no authoritative collider or actuator. [Unity scene simulation API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PhysicsScene.Simulate.html) · [Unity hinge API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/HingeJoint.html)

## Physical and sensory protocol

Coordinates are metres in the XY plane, with joint rotation about +Z and angles in radians. Nominal arm length is 0.32 m, palm radius 0.035 m and puck radius 0.045 m. Gravity is disabled. The native arm has 0.06 kg m² rotational inertia about its supported pivot. Puck inertia uses the circular cross-section model. Damping on the puck is 2 s⁻¹. The shared target clamps to configured limits. Native PD command is capped at ±2 N m, with an additional viscous damping term. Solver settings are 12 position iterations and four velocity iterations.

Each 0.05 s authoritative tick applies the motor and steps the local scene ten times at 0.005 s. The collision callback records contact after each solver step. After the ten substeps it publishes previous and measured joint state, impulse, object displacement and five receptor readings. No local scene steps are performed by ordinary Unity frame or fixed-update callbacks. The smoke confirms unchanged serialized state over eight paused frames and exactly ten substeps after using Step.

The sphere pair uses the first manifold point and aggregate collision impulse. Its normal component is a sampled solver impulse. Tangential velocity is estimated from relative post-solve point velocities. Resistance torque is reconstructed from the impulse and contact lever arm. The contact point in the local palm frame distributes signals over the same five named patches and adjacent regions used by the planar backend. Tick force is impulse divided by 0.05 s and pressure uses the fixture's nominal 0.0004 m² patch area. Native manifold duration and the planar force-positive duration have distinct definitions. Native normal and contact point are recorded separately. The scientific trace includes backend provenance and privileged physical outcomes. [Representative matched trace](evidence/native-elbow-contact-excerpts.csv)

## Cross-backend observations

Each condition uses 80 ticks. Values separated by a slash show native then numerical.

| Condition | First contact tick | Impulse-positive ticks | Total impulse (N s) | Peak tick force (N) | Peak weighted slip (m/s) | Max angle difference (rad) |
| --- | --- | --- | --- | --- | --- | --- |
| Free, initial 0.12 rad | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0.00153 |
| Standard | 4/4 | 2/2 | 0.29405/0.41990 | 5.867/8.237 | 0.0609/0.0953 | 0.02233 |
| Mirrored | 4/4 | 2/2 | 0.29405/0.41990 | 5.867/8.237 | 0.0609/0.0953 | 0.02233 |
| Heavy, 0.9 kg | 4/4 | 3/2 | 0.77889/1.04239 | 12.927/13.543 | 0.0508/0.1573 | 0.05438 |

The native standard puck ended at (0.0855, 0.7648) m versus (0.1236, 1.0247) m in the numerical scene. Final speed was 0.000536/0.000832 m/s. Maximum joint velocity difference was 0.62916 rad/s and maximum effort difference was 0.38156 N m. Native standard impulse was about 30% lower. The native active patch reversed from `palm/4` to `palm/0` when mirrored. The free-motion gate requires 0.02 rad target accuracy and zero false impulse. Contact separation is a measured result. Constraint solving, friction and slip sampling differ, so the fixture does not use impulse or pose equivalence as a passing cross-backend test.

## Native restore and observer scope

The validated, SHA-256-checked native checkpoint records both rigidbody poses and velocities, angular velocities, trial settings, IDs, pending command, tactile durations and last sensed feedback. The existing atomic publisher retains the previous valid generation. Native files use a separate path from the preserved numerical v1 files. Settings rebuild the joint and material before restored body and feedback state is applied. Internal solver caches are outside the format.

At save ticks 0, 1, 3, 4 and 18, the harness compared the next eight ticks against continuous execution. Joint angle, velocity, puck position and impulse differences all rounded to zero at eight decimal places. While restoring an active contact at tick 4, two serialized doubles changed by approximately 2.2e-16 N m (resistance) and 3.3e-24 N (tangential patch force) during JSON readback. This run fits the harness's 1e-6 serialized scalar tolerance. It does not establish restart fidelity for sustained constraints, other platforms or other shapes. Visible and unobserved first steps matched exactly. Save, Restore and Close passed through the native tray. Batch screenshot capture produced no image.

## First learned contact predictor

The logistic model predicts **next-tick contact probability** from the requested relative angle, prior sensed speed, prior impulse and prior torque. It receives no puck parameters, object coordinates or future state. The normalizations and compatibility fields are saved with the learner ID, trial provenance and evaluation. Contact force, receptor identity and onset time are separate future targets. This experiment measures representation learning and transfer. It does not change the engineered controller or household memories.

The study generates 32 complete native trials. Trials 0–15 train, 16–23 calibrate numerical transfer, and 24–31 form the untouched test. Numerical training uses the same first 16 settings. No adjacent ticks from the same trial cross a split. The held-out set has 24 positives in 96 ticks. Native scratch uses 250 full-batch epochs. Numerical-only transfer uses the same budget on the planar backend, and calibration adds 100 epochs over the eight designated native trials. [Recorded complete trial settings](evidence/native-contact-trials.csv)

| Condition | Brier loss ↓ | F1 at 0.5 ↑ | TP / FP / FN |
| --- | ---: | ---: | --- |
| Native scratch | 0.06355 | 0.857 | 18 / 0 / 6 |
| Numerical only | 0.10567 | 0.400 | 6 / 0 / 18 |
| Numerical plus native calibration | 0.07498 | 0.769 | 15 / 0 / 9 |
| Constant native training prevalence | 0.18848 | 0.000 | 0 / 0 / 24 |
| Recent-contact persistence | 0.12500 | 0.750 | 18 / 6 / 6 |

Calibration improved numerical transfer in this matched experiment. Native scratch remained strongest. The study does not establish how many native trials training from scratch would require to match calibrated transfer. The eight trials and correlated test ticks give descriptive evidence. A fresh Unity process reloaded three saved models and reproduced all 288 held-out probe probabilities exactly. See [reproduction commands](../tools/unity/README.md).
