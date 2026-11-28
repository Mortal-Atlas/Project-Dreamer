# H3 comparative adaptive grasping

## Frozen experimental protocol

This experiment covers Section 4 of the H3 continuation directive and the master roadmap's M06, M12 and M45. It compares a small contact-gated controller with the existing physical fixture and scalar learning baseline. The H2 household station continues to use its existing policy. The experimental fixture runs the same ten 5 ms PhysX substeps in each 50 ms control tick.

The development pass alternates four table objects twice, using Arthur's existing fixture calibration (seed 303). Each adaptive training arm starts at 0.18 N and uses the existing H1 observation-only scalar rule between attempts. The fixed arm uses 3.2 N. The contact arm additionally requires three consecutive bilateral contact readings before lifting. Each loaded pad must report at least 0.05 N over at least 35 ms of the preceding tick. Actual slip or rapid relative tangential movement raises the applied cap within an attempt. Losing contact during lift or hold also raises the cap. An uncertain lift advances its target more slowly. The cap cannot exceed 3.2 N. This feedback state is discarded between attempts. Only the scalar rule persists.

Freeze ten independently chosen object and condition pairs before scoring. Recreate the same physical scene for every policy on each object, rotate the policy execution order across objects, and score one attempt per policy/object. Restore the two trained scalar records in a separate Unity process. Run five conditions:

| Condition | Initial cap | Online contact gating and correction |
| --- | --- | --- |
| Fixed | 3.2 N | Off |
| Scalar | Restored scalar-arm cap | Off |
| Contact | Restored contact-arm scalar cap | On |
| ContactFresh | Reset 0.18 N | On |
| ContactFrozen | Same restored contact-arm cap as Contact | Off |

The held-out suite uses spheres, upright capsules and yawed boxes, including offset centers of mass, 52–182 g objects, varied dimensions and friction, a 0.012 N s lateral impulse and a six-tick 0.45 N downward load. The two interventions start nine completed ticks after entry into lift and are applied only while lifting or holding. Caps, object dimensions, friction, mass, center of mass and perturbation are assigned by the evaluator. The online controller receives only pad measurements. The research fixture uses measured settled table load for load-relative release criteria. Each case is matched across policies. Factors vary together across objects, so these comparisons do not isolate single-factor causal effects or estimate population-wide manipulation rates.

Success requires 35 mm physical clearance, 12 consecutive hold ticks (0.60 s) with opposed contact, a settled controlled release, and the original penetration and joint bounds. An attempt may establish a grasp, lift or release without completing the sequence. A drop is clearance below 10 mm during lift or hold after entering the accepted hold phase. Force and tangential movement are sensor measurements with no assumed effective pressure area. Record every failed attempt and distinguish a perturbation assigned by the evaluator from one that actually reached the object.

The primary comparison is full-sequence success at matched opportunities. Secondary measures include opposed contact, lifts, holds, controlled releases, slip ticks, peak contact speed and force, cumulative absolute digit effort, absolute drive work, displacement, applied interventions, correction tick and elapsed native compute time. The matched Contact versus ContactFrozen arms isolate within-attempt correction and gating at the same retained starting cap. Contact versus ContactFresh tests the starting-cap contribution with the same online mechanism. The source specifies the exact objects, seeds and timing. [Frozen study protocol](../tools/unity/AdaptiveGripStudy.cs) [H1 acceptance](PHYSICAL_HAND_H1.md#prespecified-gates-and-tactile-calibration)

## Measured outcomes

The protocol and 10-object list were committed at `62923a0a3aa1f4e1c54c0298a532451d5f7b3abf` before the held-out run. The four development cases ran twice per arm. The contact controller completed 8/8, the fixed reference 6/8 and scalar 2/8. Scalar training reached 1.58 N. Contact training left its durable cap at 0.18 N because its transient corrections were sufficient for every training attempt. Each of the two persistent training records was 203 bytes. A fresh Unity process restored both before scoring. No held-out outcome updated either record.

| Held-out condition | Opposed contacts | Accepted lifts | Settled releases | Full success | Mean longest hold | Mean slip ticks | Mean absolute digit effort | Mean absolute drive work | Mean native tick |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Fixed | 10/10 | 7/10 | 9/10 | 6/10 | 0.380 s | 4.5 | 12.15 N s | 1.779 J | 0.519 ms |
| Scalar | 10/10 | 5/10 | 8/10 | 4/10 | 0.255 s | 2.1 | 6.02 N s | 1.761 J | 0.500 ms |
| Contact | 9/10 | 4/10 | 8/10 | 3/10 | 0.240 s | 4.5 | 3.94 N s | 1.560 J | 0.513 ms |
| ContactFresh | 9/10 | 4/10 | 8/10 | 3/10 | 0.240 s | 4.5 | 3.94 N s | 1.560 J | 0.511 ms |
| ContactFrozen | 9/10 | 0/10 | 8/10 | 0/10 | 0 s | 6.8 | 0.66 N s | 1.624 J | 0.499 ms |

A settled release counts pads disengaging and measured support resettling. It can occur after a failed pickup. The contact arm and its reset ablation had the same initial cap and identical measured outcomes. Their success over the same-cap frozen arm demonstrates immediate feedback benefit on three cases. It supplies no evidence of a durable H3 benefit. The trained scalar passed four cases, and fixed 3.2 N was the strongest aggregate control at six. With ten matched cases these are descriptive counts. [Development data](evidence/phase-h3-development-summary.csv) [Held-out data](evidence/phase-h3-heldout-summary.csv)

All three primary arms passed the narrow sphere, wide sphere and lateral impulse. Mean absolute digit effort for those jointly successful cases was 12.57 N s fixed, 6.93 scalar and 3.82 contact. Mean work was 1.761, 1.742 and 1.694 J respectively. Contact had two mean slip ticks, while the two comparisons had zero on that subset. Contact corrections on the held-out suite totalled 42 per online arm. Every logged increase became the applied cap at the next authoritative update. This resolves response ordering to one 50 ms tick and does not measure a faster substep reflex. Peak slip-speed and force values for each attempt are included in the data. Measured contact force can exceed a commanded cap during impacts.

All policies received the scheduled lateral impulse and added load. Only Fixed passed the added-load case. Scalar entered an accepted hold and subsequently dropped the object under the declared clearance criterion. Both feedback arms lost the sphere beyond the table edge after failing to establish an accepted hold. That loss is visible in the native trace and lies outside the prespecified post-hold drop definition. At tick 82 the sphere center was below the tabletop and outside the table width. Its later free fall produces the large maximum displacement in those rows. The two capsules and lightly yawed box failed in all five conditions. The steeper box passed Fixed and Scalar, and the offset box passed Fixed. Preserve these cases for explaining contact geometry and release-phase losses. [Time-aligned native events](evidence/phase-h3-native-events.csv)

## Unity and integration

Use **Dream Machine → Start Supported Hand Laboratory**, then compare **Standard grip**, **Low grip** and **Feedback grip**. All three execute actual PhysX attempts on the same visible reference sphere. The tray records the active cap and correction count against the original force, slip and pose data. Native smoke exercised fixed success, weak failure, two feedback adjustments, a physical hold and observer-only replay. A separate native run reopened the H2 checkpoint, completed another retained 0.74 N household grasp and returned Arthur to Living at tick 307. The H3 controller stays in Research because the held-out reliability gate was weaker than the existing fixed policy. The H2 household continues to use its existing scalar behavior.

The new object setup and tactile feedback contracts keep rig commands and observed patch forces independent of object identity. Use them with a persistent household object and explicit supported transport before adding moving furniture or whole-body interaction. Retain the original H2 scene and private assets. See the full M01–M46 reconciliation for chair and sitting, emergent mistakes, Alice, libraries, creative tool use and other dependencies. [Workstream register](MASTER_WORKSTREAM_TRACEABILITY.md) [Native browser smoke](../tools/unity/HandBrowserSmoke.cs)

## Verification and cost

Release .NET passed 115 tests and Unity Edit Mode passed 116. The comparison comprises 24 development and 50 held-out native attempts, plus separate H2 restart and H3 live browser smoke runs. Unity 6000.3.19f1 used D3D12. The existing controlled household path still has no H3 feedback calls. Playback used 368 marshalled bytes per frame in the existing bounded tray, with 0.0059 ms mean active UI refresh time. The fixed and contact replay images averaged 0.619 and 0.490 ms of CPU camera submission, respectively. This is neither GPU timing nor whole-frame profiling.

The 16x16 Release Laptop kernel benchmark alternated eight cold process pairs using the rebuilt H2 commit. Four-person Living medians were 63,274 ticks/s at H2 and 62,966 at H3. The ranges overlapped. One twelve-person run per commit measured 37,737 and 38,912 ticks/s, respectively. The fixed numeric benchmark was separate from the native H3 controller. [Paired benchmark](evidence/phase-h3-kernel-benchmark.csv)

Reproduce via `AdaptiveGripStudy.Development` and `AdaptiveGripStudy.HeldOut` in separate processes in the guarded Unity project. `HandBrowserSmoke.Run` exercises the live research controls alongside recorded playback. The scratch product keeps `phase-h3-*-trace.csv`, `phase-h3-*.skill` and `phase-h3-*-playback.png`. The shared repository contains compact evidence. Keep future characterization on a separate development cohort.
