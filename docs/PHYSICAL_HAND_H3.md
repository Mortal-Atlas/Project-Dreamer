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
