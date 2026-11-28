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

The separate diagnostic completed **8/8** trials. All three H8 policies spent 29 of the 30 withholding ticks with no usable surface observation. The first tick still had the sample already in flight. Each policy matched its own clear-control motion and completion metrics. The assigned plane and tactile support carried this static-shelf condition. Moving or physically obstructed surfaces need a separate test. [Supplemental trials](evidence/phase-h8-supplement-summary.csv) · [surface transitions](evidence/phase-h8-supplement-milestones.csv) · [verified inputs](evidence/phase-h8-supplement-source.txt)

## Playable household and persistence

Open `Assets/Scenes/DreamMachine.unity`, enter Play, and select **Physical development → Ball / switch learner → Visual coordination**. The tray labels researcher coordinates separately from bench-local hand and Ball estimates. It shows the source, capture tick, age, confidence, surface height, projected contact time, joint-derived wrist, velocity, prediction error and applied target correction. The actual physical arm remains visible. The **Simulation** tray supplies run, pause and single-step. **Earlier attempt** followed by **Next frame** replays saved sensor estimates alongside the physical chain. **Transfer ball**, **Articulated arm**, and **Coordinated placement** still expose H5, H6 and H7.

Actual desktop DX12 runs placed the Ball for Arthur (303) at tick 197 and for Alice (404) at tick 195. Arthur's open- and closed-tray runs produced identical trajectories and checkpoint SHA-256 `8cb90004dc4cc886b15b3143ff16e2a31bb34a1fa38e8011e39b0670dea6a652`. The closed run also disabled the presentation camera. Its simulated depth sensor continued sampling. Fresh-process verification restored Arthur's settled Ball, observations and replay. Alice then attempted to return the same Ball from Arthur's saved shelf state. The Ball rolled outside the floor destination and ended at (−0.383, 0.125, 2.687) m with 815 mm of travel from release. At the end it was no longer visible in Alice's bench view. The failed world and replay restored at tick 638 with checkpoint SHA-256 `1706c909cf5abaa179604105e0c1d93a221841230a83a17e5a35bf71b3920878`. Arthur's personal Ball sighting matched its previous value throughout Alice's intervention. The live and replay trays were checked in the actual rendered household at 1280×720.

The existing researcher-assigned pickup starts from the household's coarse sighting. The H8 depth loop takes over the descent corrections and final bench sighting. The active learner's Library III record links the native v6 tape and the physical outcome, along with an eligible depth observation or explicit uncertainty. Eligible final measurements update only that learner's personal Ball sighting. The physically settled pose belongs to the shared world. An uncertain or predicted observation leaves the historical sighting in place. Learner-specific capture sequences live in the per-attempt tape. The existing world checkpoint layout continues to store stable identities, spatial beliefs, evidence references and authoritative settled positions. Mid-contact native continuation and persistent calibration acquisition remain open. The recorder stores executed sensory predictions with provenance. Durable knowledge extraction in Library I and deliberative alternatives in Library II remain future work.

## Performance and verification

Across the ten completed Fused trials, median tick cost was 0.095 ms for depth, 0.0015 ms for fusion, 0.00009 ms for correction and 0.516 ms for PhysX. The cadence changes sensor cost without changing the 5 ms physics or 50 ms motor steps. The final Arthur household rerun used 1.320 ms per tick with the tray closed and the camera off, including the audit harness. It used 0.099 ms for depth and 0.005 ms for fusion. The open test's additional DX12 screenshot captures make its 3.583 ms/tick a separate instrumentation measurement. Live and replay render submissions measured 0.991 and 0.820 ms respectively. Default simulator ticks outside the bench avoid depth capture entirely.

Arthur's H8 checkpoint was 23,309 bytes and his tape was 145,736 bytes. His H7 reference files measured 23,272 and 104,520 bytes. The H8 failed shelf return produced a 25,913-byte checkpoint and a 325,320-byte tape. The v6 tape adds 168 bytes of recorded perception per frame. Fresh-process checkpoint publication and validation took 22.66 ms for the success and 25.06 ms for the failure. Native depth precomputes 667 ray directions per active attempt and reuses its sample inbox. The native fixture exercises one learner's depth sensor at a time. The final open run's whole-editor peak working set was 3.48 GB, including Unity and loaded project assets.

The paired Release kernel benchmark used 16 four-person Living pairs and eight 12-agent pairs, alternating H7 and H8. Median throughput was 57,125 and 57,189 ticks/s for Living and 34,519 and 35,480 ticks/s for 12 agents. All four-person runs allocated 390,120 bytes over 2,000 ticks with a 27,575-byte checkpoint. The ranges overlapped in both populations. These figures measure the shared kernel with the native bench inactive. [Paired benchmark](evidence/phase-h8-kernel-paired.csv)

Run the household verification methods in separate processes using the guarded scratch project. All six final H8 checks passed, covering open/closed parity, independent learner attempts and two fresh restart boundaries. Release .NET passed 130/130, and Unity Edit Mode passed 131/131. Existing H2 recovery, H4 browser, H5 success and failure restoration, H6 replay and H7 successful and failed restores passed. The H3 held-out regression reproduced every non-timing field in all 50 original rows. Git and Unity MCP connectivity were verified following the PC restart. The owner editor remained on the real scene while native experiments used the scratch product.

## Next dependency

Build a supported seated fixture with a compliant torso and a head-bound depth origin. Preregister rigid versus compliant torso controls with matched reaches and downward loads. Score torso excursion, seat and foot support, head-relative measurement error, grasp and loaded lift before reusing H8 descent. Keep the heavy and lateral conditions as early checks. Independent forearm rotation, whole-body balance and physical furniture interactions can follow that support comparison. Durable sensory calibration, active gaze, reflected observations and regulation remain distinct integration requirements.
