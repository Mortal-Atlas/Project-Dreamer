# Elbow laboratory · Milestone Two Phase A

Phase A.2 adds a separate native backend and contact-prediction experiment. This page retains the numerical reference, original trace and checkpoint protocol. [Native comparison and prediction results](NATIVE_ELBOW_LAB.md)

The fixture advances one bounded elbow degree of freedom about a fixed mount and a planar circular puck with position, orientation, linear velocity and spin. A bounded engineered PD controller applies torque to the forearm. At each 0.05 s simulation tick it executes ten fixed substeps. Each substep evaluates the motor, computes spring-damper contact and capped tangential friction, integrates equal-and-opposite contact effects on the puck and elbow, and updates the receptor accumulators. The tick then publishes proprioception and tactile samples. No part of the fixture runs in the household tick loop.

The numerical contact solver is the authority for this fixture. Unity draws the resulting state without smoothing. The primitives are display geometry; Unity's native solver does not produce these results. Cross-platform and in-contact native solver continuation remain unverified. [Core fixture](../Assets/DreamMachine/Core/Body/ElbowLab.cs) · [Unity view](../Assets/DreamMachine/Unity/ElbowLabView.cs)

## Sensory and checkpoint boundaries

The noiseless proprioceptive record gives joint angle, angular velocity, mean command effort, and contact-induced joint torque. It has no reference to puck mass, material or hidden target coordinates. Five palm patches in a chain have stable IDs and normalized local coordinates. Contact normal projected into the local hand tangent selects a patch neighborhood. A normalized weighting distributes modeled impulse, tangential force, and slip speed. Consecutive active substeps accumulate contact duration. Mean normal force is integrated impulse divided by the full tick duration. Pressure is estimated from a nominal 0.0004 m² patch area. It is a model-derived quantity.

The v1 SHA-256 checkpoint stores configuration, stable learner and object IDs, joint and puck state, current and pending targets, receptor feedback, calibration gains, and clock. Its publisher uses the same validated pending-file and previous-generation behavior as the embedded household checkpoint. The fixture has no persistent RNG. No weights or records are copied into Arthur's or Alice's household state. The CLI uses Arthur's stable ID `son` in its separate experiment namespace. No repetitive trial enters Library III.

## Reproduction

Run `dotnet run --project dotnet/DreamMachine.Cli -c Release -- --elbow-lab <output-directory>`. It exports the complete bounded, 640-row `elbow-trace.csv`, with the intent, previous and resulting joint state, effort, resistance, contact impulse, puck state and patch signals for each recorded tick. The clean checkout trace SHA-256 was `f09c796e0ab78b2df502187c2b2a48ee1b3e5699830fa40ca3c10e8b4e7bc5f4`. The eight cases share an engineered target schedule and vary fixture configuration. Researchers can construct additional settings with the public fixture API.

In Unity select **Dream Machine → Start Elbow Laboratory**. The command starts Play Mode with an empty scene after the editor's standard save prompt. The shared modular tray exposes run/pause, single step, reverse target, save, restore and close. Tab reopens the tray. The viewport shows puck pose and patch force by color. This is an opt-in lab. Checkpoints use `elbow-lab.checkpoint` separately from the household save. The detailed limits and material settings are available through the core API. Interactive editing of the full trial configuration is pending.

## Experiment matrix

| Experiment | Observed result |
| --- | --- |
| A · Free movement | Zero tactile activation or resistance. Puck stayed at (1, 1) m. Joint converged to target within 0.02 rad, with effort bounded at ±2 N m. |
| B · Contact | First contact at tick 4, total normal impulse 0.4199 N s. Puck moved from (0.28, 0.20) m to (0.1236, 1.0247) m. |
| C · Object variation | Near puck contacted at tick 2. Light/heavy impulses were 0.1823/1.0424 N s with greater initial displacement for the light puck. Zero-friction spin stayed zero; rough material produced nonzero spin. |
| D · Localization | Opposite contacts mirrored displacement and activated opposite edge regions. Their immediately adjacent patches also activated. |
| E · Checkpoint | Disk restore at tick 22 with a pending reverse target matched the continuous reference byte-for-byte on every remaining tick. Corrupt primary bytes stayed intact after a restore and save rejection. |
| F · Observer | Isolated Unity stepped the visible tray and matched the headless reference every tick through tick 15. In-place restore after an extra tick returned exact bytes; closing the tray released visible telemetry without advancing the model. |
| G · Prediction | Pending. Validate the native physics backend and tolerances before training and evaluating held-out contact prediction. |
| H · Learned persistence | Pending. The current checkpoint preserves physical and sensor state. A learned model is absent. |

54 .NET tests passed from the tracked clean worktree and 55 isolated Unity Edit Mode tests passed. Isolated DX12 Play Mode also passed the [fixture smoke](../tools/unity/ElbowSmoke.cs). No screenshot was produced by batch capture. The numerical traces establish repeatable behavior within the tested implementation. They do not establish muscle memory, acquired motor control or biological tactile fidelity.
