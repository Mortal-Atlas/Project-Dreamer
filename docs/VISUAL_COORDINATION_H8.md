# H8 · embodied depth and hand-eye coordination

## Frozen protocol

This paired native Unity 6000.3.19f1 comparison uses a separate four-case development set and 15 held-out cases. Each case runs four policies in rotating order with the same physical inputs and a fresh supported fixture. Stop at physical settlement or 700 shared ticks. Keep every outcome. The native H7 release scorer still requires five consecutive qualified samples, disengaged digits, safe support, low speed and less than 18 mm window drift. Completion also requires the established contact, joint and settled-destination gates. Report drift from release through settlement and final distance from the assigned center separately.

| Policy | Sensing and steering |
| --- | --- |
| H7 | Original researcher destination, original targeted Ball sighting, and H7 motor logic |
| Measure only | Same H8 depth samples and release admission as the other H8 policies. Nominal researcher horizontal steering plus native proprioception and contact. |
| Visual only | Repeated Ball position and surface-height corrections from recent measured depth. Missing samples suspend correction updates and reduce approach speed. |
| Fused | Visual only plus bounded prediction, joint-derived wrist displacement, and a visual–proprioceptive hand discrepancy term. |

The H8 physical scorer is unchanged. H8's release admission uses linear motion from fresh depth and retains tactile support and wrist limits. H7's instrumented seam-spin check has no equivalent H8 measurement. This changes sensing before admission, not the outcome scorer.

The baseline uses Arthur (303), a 0.080 kg Ball on the floor, and a destination at (−0.272, 0.050, 0.003) m in bench space. In addition to the clear control, the study filters the hand or Ball depth channel using a checkerboard for 22 ticks, fully withholds the hand for 15 ticks or the Ball for 21 ticks, and withholds both for 31 ticks. The windows start four ticks into descent, with observations resuming according to cadence and latency. One 26-tick combined window applies an external 0.012 N·s impulse in its first half. A separate obstructed destination case withholds surface depth for 30 ticks. The delay case samples every four ticks with three ticks of delivery latency and a 9° viewing offset. Another case uses Alice (404), a 12 mm pickup offset, a different target, a −9° viewpoint, a 4° wrist offset and different developmental dimensions. The load case applies 0.30 N downward for up to five ticks after four ticks of hold. The four H7 diagnostic variants use the narrow shelf, far diagonal, 0.117 kg Ball and lateral arm. The precise inputs are frozen in the [native study](../tools/unity/VisualPlacementStudy.cs).

The masks represent occlusion as a controlled loss of available depth returns from a labeled surface. They sit after the nearest-hit test and never reveal what lies behind the discarded ray. Natural blockers in the scene still occlude rays. There is no added physical obstacle from a mask. Absolute observer transforms provide outcome scoring and error estimates only. Cases test generalization within one bench and one Ball geometry.

## What the learner receives

A synthetic 29×23 active-depth grid casts nearest-surface rays from an anatomy-bound, fixed bench eye, with a 74° horizontal by 62° vertical field and 1.45 m range. The simulated eye does not follow the presentation camera and is never aimed from the actual Ball, hand or destination transform. Hit identity supplies an explicit geometry label. The visible Ball surface normal and calibrated radius reconstruct its center. Visible palm surface samples approximate its center by a fixed geometry offset. Upward support hits estimate a plane height. Positions are binned at 4 mm. Ray count and depth set confidence. Occlusion reduces available returns. Hand orientation, digits, seam spin, lighting, image recognition, gaze choice, arbitrary obstacle recognition and mirrors remain open. The task still supplies the destination horizontally. The sensor contract accepts samples from other viewpoints with capture time and source.

Physics and tactile contacts update at 5 ms. The control loop and joint-derived wrist sample run at 50 ms. Normal vision captures at 100 ms with 50 ms delivery delay. Each learner's attempt owns its own visual inbox and tracks. Capture time survives delivery. Joint-chain forward kinematics uses calibrated limb lengths and sensed link orientations. It approximates a joint sensor with ideal segment rotations and does not read the palm's position. While contact remains opposed without slip, the Ball prediction can follow wrist displacement. Within a 12-tick horizon, missing observations preserve a decaying estimate, and later measurements correct it. In descent, the servo incorporates current Ball error, the hand–wrist discrepancy and surface height. Its target correction is bounded and freezes when the table begins taking load. Actual motion arises from PhysX and the existing actuators.

## Reproduction and evidence integrity

Run `tools/unity/Run-VisualPlacementAudit.ps1` with the guarded `Dreamer Audit Isolated Smoke` project, `-Phase Development` or `-Phase HeldOut`, and `-LogFile`. The launcher synchronizes project source, native physics, clock, layer, package and relevant rendering settings by SHA-256. Unity checks them again after startup and writes the verified manifest with the result. The held-out launcher rejects uncommitted modifications in these inputs. Keep the full per-tick files local. H7 source audits, data, and learner histories remain available independently.

## Frozen comparison results

The verified held-out run used `cbc5108e1dcd1ff6a597693b301a6419b1126725`. The input manifest contains 277 file checksums plus its Git and phase headers. The separate development set produced three placements per policy and one narrow-shelf reach failure per policy. The original H7 phase results were measured on a different cohort and remain recorded separately. [Verified inputs](evidence/phase-h8-heldout-source.txt) · [H7 results](SENSORIMOTOR_PLACEMENT_H7.md#held-out-results)

| Outcome, out of 15 per arm | H7 | Measure only | Visual only | Fused |
| --- | ---: | ---: | ---: | ---: |
| Reached / grasped | 12 / 12 | 12 / 12 | 12 / 12 | 12 / 12 |
| Loaded lift / controlled support | 10 / 10 | 10 / 10 | 10 / 10 | 10 / 10 |
| Passed release window | 10 | 10 | 10 | 10 |
| Settled on the destination | **10** | **9** | **10** | **10** |
| Settled anywhere | 15 | 15 | 15 | 15 |

The three reach failures in every policy were the narrow shelf, far diagonal and original surface-occlusion case. The heavy and lateral arms closed and grasped, then failed to achieve loaded lift before descent feedback could act. Their endpoints matched across all policies. Alice's rotated fixture passed the initial release in Measure only but settled about 2.00 m from the target. Both closed-loop policies and H7 kept the Ball in the intended region in that case.

| Median across completed cases | H7 | Measure only | Visual only | Fused |
| --- | ---: | ---: | ---: | ---: |
| Release-window displacement, mm | 4.80 | 2.68 | 4.32 | 3.83 |
| Total displacement through settlement, mm | 25.23 | 10.84 | 17.11 | 15.35 |
| Final horizontal error from assigned center, mm | **38.38** | 67.46 | 53.77 | 51.44 |
| Arm work, J | 23.21 | 18.01 | 21.08 | 20.48 |
| Digit effort, N·s | 40.99 | 46.42 | 44.65 | 44.18 |
| Duration, 50 ms ticks | 184 | 199 | 193 | 191.5 |

Both visual feedback policies corrected the nominal placement error of Measure only. Fusion improved Ball-state estimation and reduced drift compared with H7. H7 still achieved the best median final position. The fused arm's maximum release-window drift was 6.05 mm and its maximum total drift among completions was 17.66 mm. The placement outcomes alone show no binary benefit for fusion over Visual only in this sample.

### Prediction, visibility and confidence

Across completed cases, median per-attempt Ball-position RMS error in available views was 10.25 mm for Visual only and 4.16 mm for Fused. Fused Ball-position estimates through full Ball loss had 18.32 mm median RMS error over three trials with 10 predicted ticks each. The exposed Ball prediction expired during longer windows. The samples following shutter opening reappeared in one or two 50 ms ticks in the basic occlusion cases. The delayed, rotated view raised mean observation age from the normal 1.5 ticks to 4.5 ticks. The actuator target experienced its first measurable correction one tick after descent began and a median of 32.5 correction changes in Fused. Partial filters, full-hand loss and full-Ball loss all allow physical completion in the paired fixture. The impulse was applied at 0.012 N·s while both channels were withheld. Fusion reacquired after one tick and completed at 52.04 mm from the assigned center.

For Fused, median per-case Ball RMS error was 22.55 mm below 0.40 confidence (four cases with that band) and 4.25 mm at or above 0.40 (ten cases). This measures how the score separated errors in this fixture and leaves calibration as a probability open. The hand surface estimate was much coarser at 37.87 mm median RMS error. The ideal joint-chain estimate was approximately 0.006 mm from the measured palm center, which reflects the constrained fixture and its ideal orientation oracle. There is no claim that visual palm position improves proprioceptive pose accuracy. This phase used fixed calibration and within-attempt predictions. Later repetitions, reset and cross-body transfer are still needed to establish retained learning.

[All 16 development outcomes](evidence/phase-h8-development-summary.csv) · [development samples](evidence/phase-h8-development-milestones.csv) · [all 60 held-out outcomes](evidence/phase-h8-heldout-summary.csv) · [timestamped depth, predictions, joint effort and corrections](evidence/phase-h8-heldout-milestones.csv)

### Separate destination-availability comparison

The frozen surface-occlusion trial changed target geometry and failed the upstream reach gate under every policy. Run a separate matched diagnostic on the standard `clear` geometry, with and without the destination depth channel withheld for 30 ticks starting four ticks into descent. All four policies run once per condition with the original release scorer. Keep the original 60 outcomes and count this comparison separately. The question is whether the controller preserves valid state and behavior during loss of a previously available support estimate. The physical shelf stays in place, so geometric obstruction of the arm remains a distinct dependency. Run the verified launcher with `-Phase Supplement`.
