# H7 · coordinated physical Ball placement

## Frozen evaluation protocol

Use Unity 6000.3.19f1 with the existing isolated desktop audit project and the native PhysX scene. The comparison starts from the same fixture, Ball and learner configuration in each condition. Run every case once per policy with a fresh fixture, rotating the order across cases. The policies are the H5 force-driven carriage, the H6 supported articulated arm, and H7 using the same supported arm with coordinated loaded placement. Existing H5 and H6 published results stay separate from this matched comparison.

[The frozen harness](../tools/unity/PlacementCoordinationStudy.cs) defines eleven fresh cases. The reference uses Arthur (303), a 0.082 kg Ball with a 14° seam rotation, and a destination at (−0.271, 0.050, 0.006) m in fixture space. The other cases change mass to 0.117 kg, destination friction from 0.75 to 0.37, destination half-width from 0.115 m to 0.071 m, or add a 0.34 N downward perturbation for up to five ticks after four held ticks. Additional cases change the reach to (−0.315, 0.050, 0.029) m, increase seam rotation to 73°, change the H7 command speed to 0.74× or 1.21×, substitute Alice (404) with her own seeded setup, or switch to the lateral approach. Speed changes are H7-only and are matched H5/H6 reference repetitions. The fixture samples two seeded configurations and two approach policies. It does not independently sweep initial joint poses or object shapes.

The existing 18 mm horizontal release-window limit remains fixed, alongside five consecutive ticks with the hand disengaged, surface load, low Ball velocity and contact in the target region. Completion requires the same native penetration and joint gates and eight ticks of physical rest, ending on the destination. Record departure after the release window separately. Preserve 700-tick non-completions and failed approaches. Report all denominators, stage admissions, peak/max drift and terminal location. Use actual simulation time, per-tick native telemetry and CPU timings. The design is a deterministic, small, paired fixture comparison.

## Development and mechanism

The separate development cohort uses Arthur, Alice, a lighter Ball, a downward perturbation and a lateral approach. It reproduces H6 release entry at 0.625–0.794 m/s palm speed in the higher-elbow cases with 2.92–4.03 N table load, several times static weight. Arthur and both altered Arthur trials exceeded the 18 mm window. Alice passed that window and moved 393 mm afterward, ending below the destination. The lateral condition failed to hold before placement. The records support residual motion, an abrupt transfer of weight and later rolling as separate effects.

With command braking and a support gate alone, the palm showed repeatable final-substep jitter while its net tick displacement was small. Using full-tick proprioceptive displacement with a separate 0.10 m/s instantaneous ceiling admitted controlled release. The first gradual opening still added 116–149 mm peak drift across the high-elbow cases. The force trace showed asymmetric finger contact at rollout onset. The frozen controller first reduces available pinch force over eight ticks, then opens the digits over ten ticks with inward force disabled as soon as opening begins. The four high-elbow development placements completed with 16.85–28.02 mm maximum total drift. The lateral attempt still failed before loaded transport.

H7 uses the same reach, closing, lift and hold states as H6. In loaded transport the controller limits target acceleration toward the researcher-supplied destination. It shapes descent, responds to measured table support relative to the Ball's initial supported weight, watches for slip and requires five consecutive steady samples before letting go. It executes a bounded withdrawal, then observes settling. Timeout and failed placement remain possible. Actual body and object movement come from actuator forces and contacts.

A fixed bench ray models visibility. The evaluator aims it using the true Ball location, reconstructs translation from the visible surface and calibrated radius in 1 mm bins, and instruments seam rotation. Ball motion inferred from those samples, ideal palm proprioception and measured support enter the controller. The observer can distinguish occlusion, motion, stillness in the target region, displacement and descent below the destination. The release reference is an observed sample. Research telemetry measures actual displacement separately. Live camera-based hand and Ball estimation, uncertainty and perception-driven correction remain open.

## Reproduction

In a guarded audit copy with product name `Dreamer Audit Isolated Smoke`, copy [the placement study](../tools/unity/PlacementCoordinationStudy.cs) and [the household smoke](../tools/unity/HouseholdArmSmoke.cs) into `Assets/Editor`. Run `PlacementCoordinationStudy.Diagnose`, `PlacementCoordinationStudy.Develop`, or `PlacementCoordinationStudy.HeldOut` via Unity batch `-executeMethod`. The audit product's persistent-data directory receives per-tick local trajectories and CSV summaries. Do not run the harnesses against the owner's household save.

Before a fresh experiment, compare file hashes for changed runtime code, tests and the harness in the checkout and the audit copy. Match the engine version and audit product. Run the household methods in fresh Unity processes, starting with `HouseholdArmSmoke.H7Run`, `H7ObserverClosed`, `H7Alice` and `H7Restart`. Run `H7Return` from Arthur's saved placement to test Alice returning the same Ball to the floor. Reopen that outcome with `H7RestartReturn`. The v5 tape lives under `motor-trajectories` in the audit product's local data.

## Held-out results

The held-out runs started after the protocol and controller were pushed. The table uses the replay of the original protocol with the exact frozen controllers. The first scratch run accidentally retained an earlier core release ramp. Unity Edit Mode caught that mismatch. Both first-run summaries remain archived, and the rerun verified all changed Unity source hashes before execution. Both runs returned the same binary outcomes, with different drift magnitudes. The [frozen commit](https://github.com/Mortal-Atlas/Project-Dreamer/commit/f5b4b7566031633b34f77641f0b6070b23caeee3) and the [first held-out summary](evidence/phase-h7-preflight-sync-heldout-summary.csv) identify the two runs.

The reach column measures admission to closing. Grasp means opposing contact occurred. Hold means reaching the native 12-tick loaded hold. Lower counts entry to the phase, including emergency lower. H7's controlled lowering requires steady support, so its controlled-lower and weight-transfer counts coincide. Release counts the native five-sample release gate after controlled placement. Completion includes terminal settling on the destination.

| Stage | H5 carriage | H6 articulated | H7 coordinated |
| --- | ---: | ---: | ---: |
| Reach / opposing contact | 11 / 11 | 9 / 9 | 9 / 9 |
| Lift / 12-tick hold | 11 / 11 | 7 / 7 | 7 / 7 |
| Entered lower / transferred weight | 11 / 11 | 9 / 7 | 9 / 7 |
| Released / all attempts settled anywhere | 11 / 11 | 0 / 11 | 7 / 11 |
| Settled on destination / complete | 11 / 11 | 2 / 0 | 7 / 7 |

Each arm ran **11 attempts**. H7 completed all seven attempts that passed loaded hold. H6 and H7 both missed the reach gate with the narrow shelf and far diagonal. The lateral attempt held approximately 3.2 N on each pad with low indicated slip, but the palm stopped rising near 0.29 m fixture height. The higher-elbow reference reached 0.41 m and completed its hold. The heavy trial's palm reached only 0.28 m by the end of lift, also with opposing contact. That evidence places the failures at loaded lift and posture. The comparison leaves relative contributions from wrist geometry, leverage and available torque unresolved. The lateral probe remains available in the study. Independent regrasp, alternative selection, retention, acquisition reset and cross-object transfer remain open. The held-out changes measure generalization of fixed feedback on the same Ball geometry.

Medians below use the cases that passed loaded weight transfer (H5 11, H6 and H7 seven each). Movement duration includes settlement. Force and work columns come from the measured fixture, with arm work only where an arm exists. Instantaneous palm velocity at phase entry differs from the full-tick proprioceptive speed used by H7's gate.

| Measurement | H5 carriage | H6 articulated | H7 coordinated |
| --- | ---: | ---: | ---: |
| Palm linear / angular at release-phase entry | 0.321 m/s / 1.075 rad/s | 0.781 m/s / 2.589 rad/s | 0.062 m/s / 0.132 rad/s |
| Ball linear / angular at release-phase entry | 0.009 m/s / 0.050 rad/s | 0.079 m/s / 0.742 rad/s | 0.015 m/s / 0.125 rad/s |
| Support at release entry | 1.466 N | 4.142 N | 0.640 N |
| Peak horizontal drift during release window | 1.50 mm | 87.89 mm | 4.44 mm |
| Peak horizontal drift through settlement | 3.60 mm | 163.21 mm | 23.49 mm |
| Peak angular drift through settlement | 1.65° | 79.21° | 10.94° |
| Peak pad load / digit effort | 3.26 N / 20.09 N·s | 5.26 N / 35.37 N·s | 5.26 N / 40.88 N·s |
| Articulated arm work / fixture work | — / 16.25 J | 33.82 / 3.60 J | 23.38 / 2.80 J |
| Duration | 99 ticks / 4.95 s | 193 ticks / 9.65 s | 183 ticks / 9.15 s |

H7's worst qualified release-window drift was 5.45 mm. Across those seven trials, total drift through settling ranged from 19.38 to 25.88 mm, with all seven finishing on the destination. The 18 mm gate applies to the release window. The later drift is a remaining source of placement error. H7 accepted placements had 0.042–0.088 mm peak physical penetration. No H7 arm–torso contact or anatomical limit excursion was reported, and no H6 geometric reach corrections ran in this cohort. The H7 target shaping and feedback use continuous actuator commands. The geometric replan counter counts H6 proposals. This fixture measures proxy collisions and supports the torso rigidly. Skinned clearance and fabric contact remain open. The household's peak Ball lift clearance was approximately 249–252 mm.

Both pads read zero force in every recorded withdrawal tick. In the reference, about 4 mm of drift had accumulated when withdrawal began, 16 mm when the hand cleared and 23 mm by the end of rest detection. Residual rolling explains the additional drift observed during this interval. The bench estimate classified the terminal sample as displaced in all seven accepted H7 trials, consistent with the distinct settled-destination and observed-displacement measurements. Final horizontal error from the assigned center was 18.9–35.8 mm, with a 35.5 mm median.

[Diagnosis cases](evidence/phase-h7-diagnosis-summary.csv) · [diagnostic motion](evidence/phase-h7-diagnosis-milestones.csv) · [development cases](evidence/phase-h7-development-summary.csv) · [development motion](evidence/phase-h7-development-milestones.csv) · [all 33 held-out outcomes](evidence/phase-h7-heldout-summary.csv) · [held-out poses, joint effort, slip and support at selected ticks](evidence/phase-h7-heldout-milestones.csv).

## Playable household and persistence

In `Assets/Scenes/DreamMachine.unity`, enter Play and open **Physical development → Ball / switch learner → Coordinated placement**. **Articulated arm** still runs H6 and **Transfer ball** still runs the H5 carriage. Use **Simulation** to pause, run or single-step. The tray exposes joint posture, palm speed, sampled Ball motion, pad loads, transfer support, release, withdrawal and aftermath. The arm, destination and Ball remain visible in the scene. **Earlier attempt**, **Next frame** and **Live view** reconstruct actual recorded poses without advancing the household. The observer view is a supported proxy for the physical task, with age-appropriate skinned embodiment still a separate workstream.

The settled object and release outcome are committed through the existing household transaction. Actual poses stay in world-owned evidence. The active learner retains a sighting only through the existing range-gated observation and an instrumented post-release status in its own Library III. Captures and tapes remain local. The native tape is version 5, with older versions still readable. Existing identities, seeds, spatial beliefs and histories retain their authority. The release policy adds no durable learned state. Library I indexing of physical relationships and Library II evaluation of movement alternatives remain open.

Arthur (303) and Alice (404) each placed the floor Ball on the shelf in the actual household after 183 and 192 ticks respectively. Their independent firsthand records contain the observed displacement, and the other learner retains its original record. Arthur's open and closed attempts, with the camera disabled in the closed condition, produced identical native telemetry and checkpoint SHA-256 `e50e051c299319f274bc51edb4b575468ad59724ef8c4c2acc28423ce0695b98`. The Unity delegate smoke used the normal tray buttons, a paused clock and the shared one-tick path. Captures of the actual tray and recorded arm were inspected at 1280×720 on DX12.

A fresh Unity process restored Arthur's successful checkpoint byte for byte, including the rendered Ball and the original learner's experience. From that save, Alice tried to return the same Ball to the floor. The Ball settled outside the assigned region at (−0.446, 0.125, 2.569) m after 432 attempt ticks. Its drift from release was 648 mm. A second Unity restart restored that failed outcome, the actual Ball position, both separate learner histories and passive replay at world tick 615. The failed-return checkpoint SHA-256 was `c5687c1da10a2d06af25691d2ced9252f47b1d505a09064a39d06dd05fb1bf41`. Mid-contact native restore remains open.

The next integration step is a matched visible/occluded hand-and-Ball tracking test on the same fixture. Feed timestamped body-frame samples and confidence into descent, while keeping the destination task, physics and release evaluator fixed. Use the heavy-Ball and lateral traces to localize the upstream hold failures before repeating the frozen comparison. Independent pronation and a compliant torso support are the next physical prerequisites for transferring the task to a learner that can balance and move in the room. Regulatory integration is tracked in the [master workstreams](MASTER_WORKSTREAM_TRACEABILITY.md#h7-nervous-system-and-regulatory-requirements).

## Runtime performance

The matched native fixture's accepted/weight-transferred cases had median per-tick PhysX times of 0.642 ms for H5, 0.671 ms for H6 and 0.668 ms for H7. Actuator command time was 0.037, 0.059 and 0.058 ms respectively. H7 tracking added 0.0018 ms per tick. The 20 Hz shared clock remained the same across all three policies. The microbenchmarks measure the instrumented desktop fixture.

The Release kernel benchmark alternated H6 (`0dc7d8e`) and H7 in 16 four-agent Living pairs and eight 12-agent pairs. Median throughput was 43,166 and 43,245 ticks/s in Living and 26,519 and 26,583 ticks/s in the 12-agent kernel. The ranges overlap in both comparisons. The four-agent run allocated 390,120 bytes over the 2,000 measured ticks on each branch and produced the same 27,575-byte checkpoint. [Paired raw timing](evidence/phase-h7-kernel-paired.csv).

The Arthur closed-tray run spent 1.505 ms per shared tick end to end, including the smoke instrumentation. PhysX used 0.777 ms, commands 0.081 ms and tracking 0.009 ms. The open-tray smoke also includes two explicit renderer capture loops and took 3.930 ms per tick. The render submissions measured 1.333 ms live and 1.010 ms in replay. These samples include different capture work, so they do not isolate normal tray overhead. Arthur's settled checkpoint was 23,272 bytes and his trajectory tape was 104,520 bytes. Alice's independent placement produced 23,236 and 109,632 bytes respectively. The failed return produced a 25,901-byte checkpoint and a 245,952-byte tape. Checkpoint publication plus checksum and layout validation used 43.17 ms in Arthur's fresh process and 43.13 ms for the failed return. The open-tray test's whole Unity process reached a peak working set of 3.50 GB, including the editor and project resources.

## Regression tests

Release .NET passed 126/126 with `dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo`. Unity Edit Mode passed 127/127 against the synchronized audit project. Prior desktop restoration also replayed H6 version 4 and reopened H5's successful and failed transfers in separate processes. The original private saves, models and full native artifacts stay local.
