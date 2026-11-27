# Supported articulated-hand fixture

## First physical task

Use one existing learner in an isolated fixture with a supported upper arm, gravity and a single movable object on a platform. The learner reaches, acquires contact, lifts the object, holds it, then opens the digits and allows release. Start with a rounded rigid object and a two-contact precision grip. Keep the torso seated or the upper arm mechanically supported. Arthur is the first candidate because his identity, anatomy and activity observation path already exist. Release does not edit object position, parentage or velocity.

Choose native PhysX for the first contact result. The numerical shoulder–elbow–wrist chain offers deterministic reachability, joint-angle and command checks. The numerical elbow provides a calibrated starting point for force/pressure and prediction errors. Native PhysX already supplies local scenes, actual contact impulses and constrained actuation for the elbow and lower body. Multiple independently loaded pads, friction, a freely moving object, slip and release warrant native verification. Retain the numerical arm for reachability, controller sanity checks and a reproducible analytical force check. [Numerical arm](../Assets/DreamMachine/Core/Body/ShoulderElbowWristLab.cs) [Elbow sensors](../Assets/DreamMachine/Core/Body/ElbowLab.cs) [Native backend](../Assets/DreamMachine/Unity/NativeElbowLab.cs)

## Mechanical and sensory contracts

| Decision | First implementation |
| --- | --- |
| Joints | Seeded segment and landmark IDs for shoulder, elbow, articulated wrist and palm. Add thumb opposition and flexion, then one or two serial flexion hinges on the index finger. Use the existing native joint style with bounded PD and report any degrees of freedom constrained by the support. |
| Object | Start with one weighed rounded rigid body with a declared center of mass, inertia tensor, friction pair and nonspring support. Record the coefficients, gravity vector, mass, geometry and scale. Vary one mass or friction dimension for held-out transfer. Keep physical contact as the only means of supporting the object. |
| Actuation | Adopt physical torque limits and time-windowed motor commands. Determine caps with static unloaded and loaded trials. Log actual angle, rate, applied torque, saturation and signed and absolute work. Reject contacts in excluded fixture pairs. |
| Contact | Aggregate native normal and tangential impulses by pad and object over each 5 ms substep and over the authoritative tick. Report average force from normal impulse divided by elapsed time. Keep validity, duration and sample count. |
| Area and pressure | Start with measured pad area and a known loaded calibration target. The effective area for a partially loaded pad remains an estimate. Record area, units, validity and calibration version with pressure. Reuse the elbow's region evidence shape, with new pad area calibration. |
| Slip | Estimate relative tangent velocity at the contact in the pad frame. Start with a calibrated threshold above stationary solver and contact jitter. Require persistence across multiple valid samples. Confirm with relative displacement and keep an explicit uncertain state. |
| Collision tolerance | Use the adjacent-pair exclusions and initial geometric diagnostics from the body audit. Separate ordinary solver offset from intrusion. Validate the unloaded hand closing and the loaded grip on the actual rendered geometry. |
| Native restart | Restore durable calibration, object and body state, attempt history and adaptive parameters. First verify a paused, settled, unloaded boundary. Choose and freeze pose, velocity and force tolerances from repeat trials. Mid-loaded-contact solver reconstruction and reusable warm start remain open. |

For the initial feasibility gate, measure lift relative to the support plane, maintained height during a fixed hold, number and location of contacts, slip and physical release. Score unintended support and body contacts separately. Seeded grip calibration, geometry and benchmark thresholds should precede held-out testing. [Body fixture audit](BODY_INTEGRITY_AUDIT.md) [Native checkpoint precedent](../Assets/DreamMachine/Unity/NativeElbowCheckpoint.cs)

## Acquisition experiment and motor record

Use parallel frozen and adaptive policies with identical nominal reach and grip commands and independently matched seeds. First measure initial competence. Run a fixed number of native attempts. Feed the adaptive policy only measured pad impulses, effective pressure, slip and joint readings. Update a bounded grip-force or timing residual after usable errors with fixed gain, saturation, rollback and update provenance. Do not supply the solved grip after a failure. Record changes and failures without rewriting earlier attempts.

Save from a verified fixture boundary, restart the application and score the first post-restart attempts using retained parameters. Run a frozen-retained comparison and a held-out object with declared mass or friction change. Report number of successes, lift height, hold duration, slip, work, saturation, collisions and time to acquire. Keep prior teaching, engineered control and independently acquired residuals independently identifiable.

Each motor attempt should key to individual, skill, body calibration revision, intention and completed tick range. Store a command/trajectory reference, predicted tactile and proprioceptive observations, observed measurements with pressure estimate provenance, physical result including failure, correction and adaptation eligibility. Attach retention, transfer and mentor evidence as later linked records. Library I projects supported affordance/body relationships, Library II stores motor candidates and predictions, and Library III stores actual attempts and results. Playback reads these records passively. Composition, emotional regulation and later generational comparisons consume distinct provenances. [Motor vocabulary addendum](PHYSICAL_DEVELOPMENT_ADDENDUM.md)

## Entry milestone

First construct and validate the native supported grip as an isolated task. Then connect the same learner to the fixture through the authoritative clock, visible practice and checkpoint contract in Physical Development Mode. Existing Living activities remain kinematic references until each physical capability passes its own gate. The first expressive follow-up remains interruptible seated rocking.
