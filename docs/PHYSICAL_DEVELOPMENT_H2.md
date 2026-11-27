# H2 supported household practice

## Entry and authority

In the local Unity household, open **Physical development**, choose **Enter station**, then **Try grasp**. Use the existing Simulation controls to resume or single-step. Arthur is seated at the dedicated station by a recorded researcher transition. The wrist support and opposing guides are visible. The body view follows his existing age and seated pose. The table uses H1's reduced native arm/wrist fixture, seeded from the same person's birth seed. Arthur's full skinned or procedural body remains kinematic around that reduced fixture.

The shared household tick advances one complete H1 tick, ten substeps at 5 ms, during an active attempt. Arthur's ordinary household activity selection is reserved while he is in the station. His physiology and the rest of the household still run on the same timeline. There is no second learner or second physics clock. The simulated speed and work budget control how many complete ticks run. Terminal attempts pause that clock. Returning to Living releases Arthur's ordinary activity scheduling. Active work must reach its attempt boundary before return, restore or reset. [Household state](../Assets/DreamMachine/Core/Sim/HouseholdGripSession.cs) [Unity station](../Assets/DreamMachine/Unity/DreamMachineApp.Practice.cs)

## Actual behavior

| Attempt | Household ticks | Cap used | Slip ticks | Hold | Result |
| --- | --- | ---: | ---: | ---: | --- |
| 1 | 0–78 | 0.18 N | 6 | 0 | Failed hold, cap increased to 0.46 N |
| 2 | 78–164 | 0.46 N | 7 | 0 | Failed hold, cap increased to 0.74 N |
| 3 | 164–235 | 0.74 N | 0 | 0.60 s | 58.4 mm peak clearance, settled release |
| 4 | 235–306 | 0.74 N restored | 0 | 0.60 s | 58.4 mm peak clearance, settled release |

The stimulus and task are engineered. The existing H1 update uses actual slip and failure, with no input from hidden friction or mass. The verified scene uses Arthur's configured birth seed 303. The restart reproduced success on this familiar fixture. H1's partial transfer result remains the comparison for unfamiliar objects. H3 owns context-sensitive policy comparisons against the fixed engineered reference.

## Durable evidence and playback

Each finished attempt stores actual outcome, exposure interval, slip and hold counts, peak lift, settled sphere position, and a reference to the timed trajectory. The cap is reconstructed from the durable observations. Arthur's Library III receives a distinct firsthand entry with a stable attempt identifier and the source hash. The user can open the station tray after returning to Living and inspect outcome counts, cap history, force, slip and the current or recorded stage. The tray can choose earlier or later attempts and play, pause, restart or advance one frame of recorded data without executing physics. The trajectory includes object, wrist and digit poses, contact positions and forces, target travel and applied closing effort. The initial grip sequence is authored. Separate Library I indexing, Library II construction, richer browser comparisons and independent pressure calibration remain open.

The v6 world checkpoint stores the mode, subject, calibration, compact attempt evidence and last measured sphere location alongside the complete existing world state. Prior household versions remain readable. While a native attempt is in flight the app refuses save and restore. Authoritative physical-mode saves are taken at a settled object boundary. The trajectory is a bounded content-addressed local binary, outside the primary checkpoint. Retain the `motor-trajectories` directory alongside a copied save to retain playback. The restart uses the recorded sphere position for the settled view and initializes the next trial as a new, visible test setup. This preserves gameplay state and the learned policy at a known boundary. It makes no claim about reconstruction of arbitrary PhysX contact solver state.

## Validation and continuation

The guarded Unity 6000.3.19f1 D3D12 smoke exercised an actual failed and successful grasp, one native tick per authoritative single step, a settled save at tick 235, restoration by a separate editor process, passive playback, retained grasp success, and return to Living at tick 307. The fresh process found the previous memory and four consecutive motor references by the end of the run. Playback preserved the authoritative checkpoint bytes. The scratch product keeps `phase-h2-failed.png`, `phase-h2-hold.png` and local `motor-trajectories` files. LFS uploads were avoided. [Smoke entry points](../tools/unity/HouseholdPracticeSmoke.cs) [Compact evidence](evidence/phase-h2-household.csv)

Release .NET passed 113 tests and Unity Edit Mode passed 114. The measured native-plus-household tick, tape sampling and view draw averaged 0.89-1.21 ms across the four smoke attempts. Tapes were 24,344-29,414 bytes and the complete settled checkpoint was 23,007 bytes. Single cached-process kernel samples during other local work measured 37,378 ticks/s in four-person Living and 25,050 in the twelve-person kernel. Use the existing paired methodology for a relative throughput conclusion.

Next, use this actor-bound station to compare context-sensitive and fixed/grip-cap policies on individual fresh objects. Lock the exact H3 protocol before opening held-out rows. Keep body self-collision and garment improvement separate. Expand the motor browser and practice station to Alice only after checking her own calibration and records.

Reproduce by copying the current changed Unity and Core files and the H2 harness into the guarded scratch editor. Run `HouseholdPracticeSmoke.Attempt` and `HouseholdPracticeSmoke.Restore` in separate processes.
