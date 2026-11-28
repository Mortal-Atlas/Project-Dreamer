# H9 · dynamic seated torso and moving perception

## Physical chain and sensing

The supported household Ball station now offers four H9 conditions on the existing arm, hand and H8 depth architecture. The torso can move against a mechanical pelvic anchor. The arm acts back on the torso through the shoulder joint. Both the arm predictor and its joint-derived wrist estimate recompute the shoulder from the current torso pose. The eye follows the physical head, with no dependency on the player's camera.

| Region | Degrees of freedom and support |
| --- | --- |
| Seat and pelvis | Fixed native collision proxies. No mechanical legs, feet or free pelvis. The anchored pelvis carries upper-body load through the lumbar joint. Seat box contacts provide an additional normal reaction. |
| Lumbar and torso | Rigid control fixes its torso at the specified initial angle. The three compliant arms use a 4 kg gravity-loaded torso with collider-derived inertia. Three angular degrees of freedom are limited to ±26° flexion, 24° yaw and 19° roll. Translations at the lumbar anchor are constrained. The remaining spine and shoulder girdle are rigid within the torso. |
| Neck and head | A 0.84 kg head has a seed-scaled spherical collider and three limited rotations, 26° flexion, 38° yaw and 23° roll. The neck anchor transmits forces. Its bounded restoring torque follows the torso in Rigid and Passive, and stabilizes a specified yaw around bench upright in Reactive and Anticipatory. There is no independent ocular controller. |
| Arm, hand and Ball | Three shoulder rotations, one elbow flexion axis with bounded end correction, a three-axis wrist and two guided opposing fingers. Existing contact gates, low-level actuators and free Ball dynamics remain in use. Independent forearm rotation and finer hands remain separate work. |

Nonadjacent contacts use the actual physics proxies and are scored for impulse, duration and penetration. The pelvis–torso and torso–head connected neighbors are excluded. The fixture records arm–torso and other monitored body/furniture contact separately, with seat impulse also available as a distinct channel. Anchor force is a lumbar joint reaction. Seat force is cumulative measured seat-normal impulse divided by the shared tick interval. The rigid torso has no comparable lumbar reaction measurement. Neither is a measure of independent sitting.

Passive uses a lumbar restoring spring and damping. Reactive adds a bounded orientation and angular velocity torque. Anticipatory also opposes the expected gravity moment from the requested arm lever and supported carried weight, with a separate 5 N·m prediction cap and a 12 N·m combined active cap. It uses a general moment relationship across tasks. The torso and neck apply torques at the 5 ms native substep, with the manipulation target updated at 50 ms. The requested upright frame and segment orientations are idealized sensor inputs. No controller resets bodies during a trial.

At depth capture, the H8 aperture follows the current head pose. It quantizes observed centers in calibrated eye-relative coordinates and reconstructs them in the bench frame with the extrinsics from that capture. The delayed inbox therefore retains the observation's original coordinates and timestamp when the head moves again. This is engineered coordinate calibration. Ideal head angular velocity and the 50 ms finite-difference acceleration are available as vestibular proxies. Attention, sensory calibration acquisition, and gaze choice remain open.

## Frozen comparison

Run the four development cases and eleven independent cases from [the locked native harness](../tools/unity/SeatedPlacementStudy.cs). The five policies are the original H8 fused controller with its fixed torso and eye; H9 Rigid with the same arm controller and a physical neck/head but a fixed torso; H9 Passive; H9 Reactive; and H9 Anticipatory. H9 policies share anatomy, the seat, starting configuration, assigned destination and the moving sensor. Rigid and Passive have a torso-following neck. Reactive and Anticipatory also stabilize the head in the bench frame, so the passive-to-active contrast tests the combined postural architecture. Reactive versus Anticipatory holds the neck policy fixed.

Rotate condition order by trial. Stop at native settlement or 700 ticks and keep every result. Keep the inherited release requirement of five qualified ticks with disengaged fingers, support, low velocity and less than 18 mm displacement. Score settlement, total release-to-settlement travel and target-center error separately. Neither original H8 values nor gates are redefined. Evaluate observed positions against the authoritative poses in the harness only.

The clear reference uses Arthur (303), a 0.082 kg Ball and a shelf destination at (−0.266, 0.050, 0.006) m. The cohort varies shelf height, diagonal distance, lateral elbow, an 0.117 kg Ball, an approach impulse and carried load, initial pitch/yaw, spring stiffness and movement speed, view yaw with temporary Ball-depth loss, and Alice with a second seeded body profile. The approach disturbance is a 0.19 N·m·s torso angular impulse three ticks into approach wherever that axis is free. The 0.31 N downward object load is applied for up to five ticks after four held ticks. H8 and Rigid still receive the object load if eligible. They have no compliant axis for the angular disturbance. Record which perturbations actually occur. Shoulder and head displacement use their own initial samples as references. Maximum trunk angle is measured from bench upright, including intentionally pitched starts.

In the guarded scratch product, invoke [the audit launcher](../tools/unity/Run-VisualPlacementAudit.ps1) with `-Study Seated -Phase Development` and `-Study Seated -Phase HeldOut` in fresh Unity 6000.3.19f1 processes. The launcher verifies source and physics, timing, asset and package settings again inside the editor. The original H8 evidence remains archived. Full native traces and captures stay local.

## Measured results

The held-out run executed `d5e9fa7ca4744b8fe476acb6821f3e4e560bdc60` with 285 verified input file hashes and two manifest headers. All 55 attempts reached a physical settlement. In development, each policy completed three of four conditions. The lateral trial failed during loaded lift for all five. Support instrumentation was refined in development to separate seat contact from lumbar reaction and match the rigid H9 control's starting pitch to the moving torso arms. [Development outcomes](evidence/phase-h9-development-summary.csv) · [development telemetry](evidence/phase-h9-development-milestones.csv) · [verified development inputs](evidence/phase-h9-development-source.txt)

| Outcome out of 11 | H8 | H9 Rigid | H9 Passive | H9 Reactive | H9 Anticipatory |
| --- | ---: | ---: | ---: | ---: | ---: |
| Reached / opposing grasp | 9 / 9 | 9 / 9 | 9 / 9 | 9 / 9 | 10 / 10 |
| Loaded lift / placement support | 7 / 7 | 7 / 7 | 7 / 7 | 7 / 7 | 7 / 7 |
| Passed release / complete | 7 / 7 | 7 / 7 | 7 / 7 | 7 / 7 | 7 / 7 |

The higher shelf stopped the reach phase in four policies. Anticipatory reached and grasped in that case before failing loaded lift. The far diagonal failed at reach for all policies. The heavy and lateral cases reached and grasped before losing the loaded-lift gate. In the lateral trial, all three moving-torso policies settled the Ball closer to the goal than either fixed reference. They still failed to lift it. H9 preserved the baseline completion count in the tested bench task and shifted some failure trajectories.

The following medians use the seven completed cases for each policy. H8 lacks H9's physical head and seat instrumentation. The rigid control cannot register a lumbar constraint reaction because it fixes the torso directly.

| Outcome | H8 | H9 Rigid | H9 Passive | H9 Reactive | H9 Anticipatory |
| --- | ---: | ---: | ---: | ---: | ---: |
| Wrist error entering grasp, mm | 13.37 | 13.37 | 12.51 | 13.31 | 14.36 |
| Peak torso angle, ° | 0 | 0 | 16.93 | 8.26 | 7.76 |
| Mean torso angle, ° | 0 | 0 | 6.75 | 4.79 | 3.34 |
| Peak torso angular speed, rad/s | 0 | 0 | 3.77 | 1.45 | 1.44 |
| Peak head displacement, mm | — | 1.18 | 42.33 | 33.07 | 33.12 |
| Peak head rotation, ° | — | 1.03 | 17.95 | 2.52 | 2.52 |
| Peak shoulder displacement, mm | 0 | 0 | 70.45 | 36.04 | 31.08 |
| Mean lumbar reaction magnitude, N | — | — | 35.78 | 55.63 | 58.50 |
| Mean seat normal force, N | — | 0 | 29.94 | 5.37 | 1.60 |
| Seat contact time, s | — | 0 | 9.46 | 6.71 | 0.85 |
| Release-window displacement, mm | 3.71 | 2.89 | 2.76 | 2.67 | 3.00 |
| Total release-to-settlement displacement, mm | 14.50 | 12.84 | 11.32 | 11.18 | 12.02 |
| Final horizontal center error, mm | 49.94 | 36.79 | 36.78 | 41.97 | 42.31 |
| Ball / hand visual RMS error, mm | 6.51 / 39.60 | 7.12 / 45.11 | 7.94 / 62.26 | 7.29 / 52.04 | 7.20 / 49.26 |
| Joint-derived wrist RMS error, mm | 0.013 | 0.012 | 0.098 | 0.015 | 0.015 |
| Ball innovation RMS, mm | 7.80 | 9.15 | 10.10 | 8.89 | 9.12 |
| Arm work / postural work, J | 20.59 / — | 20.57 / 0.001 | 20.46 / 0.29 | 20.22 / 2.44 | 22.05 / 3.12 |
| Active lumbar effort, N·m·s | — | 0 | 0 | 28.67 | 38.93 |
| Digit effort, N·s | 45.34 | 45.23 | 44.88 | 46.14 | 45.62 |

The active torso reduced peak excursion and moved the dominant support reaction from sustained seat contact toward the fixed pelvic constraint. Anticipatory reduced mean lean and seat contact further while using more active effort. The initial-pitch rigid runs held the prescribed 8° and −4° poses, with zero angular travel in the torso. At the 0.19 N·m·s impulse, Reactive and Anticipatory first registered a change of at least 0.15 N·m in the next 50 ms output sample. Every policy received all five object-load ticks and completed the disturbed transfer. That timing is limited by recording cadence.

The moving camera source preserved useful Ball observations, with the reference and most completed cases visible throughout their action samples. Explicit temporary Ball loss reduced the visible fraction to about 0.88–0.89 across policies. The passive chain moved the head more, and had the largest median Ball and hand error. The joint-derived wrist error remains small because the bench uses exact segment orientation and fixed calibration. H8 and H9 use different calibrated eye positions. The passive/reactive comparison also changes neck stabilization, so these data measure the combined architecture. Independent benefits from active head stabilization and calibration learning remain unmeasured.

No arm–torso contact or elbow limit excursion was reported across these 55 attempts. Contact with the other monitored body/furniture proxies reached up to 0.922 mm penetration. The passive bench carried substantial load at the seat under near-continuous physical contact. The recorded active control still used the constrained pelvis. There was no floor contact from feet, free pelvic balance or garment validation. Retained compensation, calibration reset and transfer across growth remain open. The Alice profile provided an independent anatomical case under the same engineered settings.

[All 55 outcomes](evidence/phase-h9-heldout-summary.csv) · [physical and perceptual events](evidence/phase-h9-heldout-milestones.csv) · [verified held-out sources](evidence/phase-h9-heldout-source.txt)

## Playable household and persistence

In Unity open **Physical development → Ball / switch learner**. The new row contains **H9 rigid torso**, **H9 compliant**, **H9 reactive**, and **H9 anticipatory**. The Native Body proxies show the moving torso, head and shoulder. The tray reports torso and head angles, the lumbar reaction and seat force separately, active torque, and timestamped hand and Ball estimates alongside the physical results. H5 **Transfer ball**, H6 **Articulated arm**, H7 **Coordinated placement** and H8 **Visual coordination** remain available. **Simulation** provides run, pause and single-step. **Earlier attempt** and **Next frame** show the physical and sensory records. The controlling body uses collision proxies alongside the household character presentation. Full skinned alignment and garments remain unverified.

Rendered native runs placed Arthur's and Alice's Ball in the target region at ticks 198 and 203 with H9 Reactive. Arthur's closed-tray and camera-off run matched the open run's physical and perceptual traces and checkpoint bytes. The head continued collecting depth. The successful checkpoint digest was `e6d8808d0ec838ebbe1b72d072b2fea4b95fc1d8086d98a358f5145894ad6dd1`. The household restraint record and head pose replay were restored in a fresh process. Arthur's sensed Ball location stayed unchanged when Alice subsequently tried to return the same Ball. Her return settled outside the intended floor region after 448 attempt ticks, with 948 mm of post-release displacement. The shared Ball ended at (−0.346, 0.125, 2.823) m, outside her final view. The failed return and its separate memory had digest `2f998a0ecb93ff326cb31f496674f2b34ea95f11ece6017e55843fab05bd4237` at world tick 646.

The existing settled world checkpoint gains no solver state. Version 7 of the Ball tape adds 177 bytes per frame for posture, head and eye poses, lumbar and seat support, actuator effort and measured contacts. H9 records reference the tape from the active learner's own Library III entry. Version 3–6 Ball tapes and version 1–2 sphere tapes remain readable. No new learned postural or sensory parameters are created. Evidence-linked Library I relationships and genuinely constructed Library II alternatives remain future work. A successful settled restore recovers the world plus replayable physical evidence. Resuming arbitrary active PhysX contact remains open.

## Native performance

Across completed cases, median PhysX time was 0.631 ms per shared tick for H8 and 0.654 ms for H9 Reactive. H9 Passive and Anticipatory used 0.666 and 0.668 ms. Reactive spent an additional 0.0094 ms in torso and neck control. Moving-head depth cost 0.1225 ms in Reactive and 0.0967 ms in the matched H8 reference. Fusion used about 0.0016 ms in both. H9 uses the same 200 Hz physics and 20 Hz manipulation cadence, with depth sampled every other shared tick in this study.

The closed Arthur Unity run, including trace instrumentation, used 1.581 ms per tick. PhysX took 0.784 ms, posture 0.016 ms, depth 0.140 ms and fusion 0.006 ms. The open run used 3.965 ms per tick while taking two explicit DX12 captures, so the difference includes screenshot work. Live and replay render submissions measured 1.072 and 0.864 ms respectively. The open run's peak whole-editor working set was 3.52 GB. This measures the loaded project and Unity process, not the incremental allocation for the fixture.

Arthur's 23,309-byte H9 checkpoint matched the size of his H8 settled checkpoint. The H9 tape was 181,695 bytes, compared with the 145,736-byte H8 tape. The failed H9 return produced a 25,890-byte checkpoint and 409,945-byte tape. Fresh-process publication and validation took 29.75 ms for the success and 28.25 ms for the failed return. The household runs one native H9 station at a time. A simultaneous multi-learner native-body benchmark remains open.

The shared kernel was benchmarked separately from Unity with the native station inactive. In 16 alternating pairs, four-person Living medians were 53,058 ticks/s for H8 and 53,229 for H9. Eight alternating 12-agent pairs measured 31,477 and 31,036 ticks/s respectively. The observed ranges overlapped in both populations. The four-person runs allocated 390,120 bytes over 2,000 measured ticks and produced 27,575-byte checkpoints on both revisions. [Paired kernel samples](evidence/phase-h9-kernel-paired.csv)

## Automated verification

Release .NET passed 134/134 and Unity Edit Mode passed 135/135. All eight H9 household checks passed in separate Unity processes. Both H9 rigid and passive modes completed in the household. The rigid torso stayed at 0° and the passive torso reached 17.76°. The observer, independent learner, and replay checks verified actual recorded head transforms and the continuation of each learner's own spatial record. The reactive success and the failed floor return both restored with matching world bytes, replay poses, and persistent Ball placement. The DX12 live, replay, rigid, and passive captures were inspected. Native regressions passed for the H8 success and failed return, H7 success, H6 recorded failed release and H5 success/failure restores. The archived H5–H8 project evidence matched its prior Git state. The guarded launcher rejects an unsupported seated cohort before starting Unity.

## Next dependency

Release the pelvis into a constrained seated fixture with measured seat and bilateral foot contacts. Couple it to the verified lower-body support experiments and compare pelvis load transfer under matched upper-body reaching and downward disturbances. Score seat-to-foot load redistribution, coupled center-of-mass motion, balance loss, head observation quality and the loaded reach gates. Retain the H9 fixed-pelvis control. This is the immediate dependency for controlled crouching, chair approach, sitting down, standing, falling safely in the simulation and coordinated carrying during gait.
