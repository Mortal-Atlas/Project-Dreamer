# Body integrity audit

## Observed sources and symptoms

Living mode follows the shared household kernel's fixed-tick positions using procedural targets. The body primitives disable or remove their generated colliders. The supplied skinned meshes follow the same reference anatomy through a retargeter. A mesh intersection, a character that stays upright, or a hand moving a prop in this mode cannot diagnose the physical solver. The L.5 private captures show the learner beside the open containers. They do not supply synchronized skin penetration or contact measurements. [Household poses](../Assets/DreamMachine/Unity/DreamMachineApp.Living.cs) [Procedural geometry](../Assets/DreamMachine/Unity/CharacterFactory.cs)

The separate lower-body fixture has eight solve bodies, collision with the ground, limited joints and a sliding ballast. It explicitly excludes every segment pair from self-collision. C.1.3 measures genuine body-ground impacts and recovery in this fixture; adjacent and nonadjacent collisions remain unmeasured. Its pelvic and limb boxes are research proxies without hands or garment surfaces. The native elbow records physical palm contact against an external body. The numerical shoulder–elbow–wrist fixture supplies a reachability reference. [Native lower body](../Assets/DreamMachine/Unity/NativeLowerBodyLab.cs) [Native elbow](../Assets/DreamMachine/Unity/NativeElbowLab.cs) [Numerical arm](../Assets/DreamMachine/Core/Body/ShoulderElbowWristLab.cs)

The four local VRMs contain spring-bone definitions. Read-only inspection found 22–36 chains, 84–148 configured joints and 22–28 spring colliders per model. The household manually processes the UniVRM runtime after applying its pose and expression inputs. This establishes configuration and execution paths. Garment fit, spring collider coverage and correct secondary motion require pose-matched comparisons. Keep the original assets and generated derivatives local. [Avatar binding](../Assets/DreamMachine/Unity/HouseholdAvatar.cs) [Asset registry](ASSET_AND_LICENSE_REGISTRY.md)

## Initial collision registry for supported manipulation

Register semantic segment IDs and local anchors against the seeded anatomy. Validate capsule and box lengths in the neutral arm pose. Introduce the pairs below in separate, matched trials and leave the C.1.2/C.1.3 lower-body baseline intact.

| Pair | Treatment | Reason |
| --- | --- | --- |
| Torso–upper arm, upper arm–forearm, forearm–palm | Exclude only the immediately linked colliders after fitting the joint anchors. Keep distal overlaps in the diagnostic. | Linked shafts need clearance at their shared shoulder, elbow and wrist pivots. A connected-pair exclusion can also hide an incorrectly long shaft. |
| Palm–proximal digit and successive segments of one digit | Same adjacent-joint exception at the knuckle or hinge. | Permit the shared attachment volume while retaining palm contact with distal digits. |
| Thumb pad–index pad, distal finger–palm, digits on opposite sides of the hand | Enable as supported by geometry and record intended self-touch. | A natural close can meet these surfaces and should stop under contact. |
| Forearm–torso, palm–torso and hand–contralateral arm | Enable only where both bodies exist in the fixture. Distinguish intended bracing. | These are nonadjacent physical contacts and should not pass through one another. |
| Palm and digits–object, arm–support, object–support and object–ground | Enable and log eligible contact points and impulses. | Support, grasp, slip and release depend on actual reactions. |
| Pelvis–thigh, thigh–shin, shin–foot, and lower-body nonadjacent pairs | Preserve current exclusions in the baseline. Test opposite feet, shins and knees first in a separate physical branch. | Their prior recovery and standing outcomes were measured without these pairs. |
| Hair, clothes and accessory proxies | Presentation layer with separate coverage and overlay data. | A spring collider governs presentation and conveys no physical grip or learner contact. |

## Proposed measurement and thresholds

These thresholds are development proposals. Calibrate them on the current rig and freeze them before scoring held-out tests.

- Use a static signed-separation check and a dynamic sweep of standing, sitting, reach, gait, and the physical fall fixtures where available. Flag a forbidden physical overlap deeper than 2 mm for two consecutive 5 ms substeps if the pair's contact offset is 1 mm. For other offsets use twice the larger offset and report the exact value. Record peak depth, duration, relative velocity, joint error, pressure validity and paired screenshots.
- Apply the original 8 mm penetration and 0.025 rad joint overshoot limits when comparing the C.1 lower-body reference. A transient impact or contact patch may need a separate tolerance in the hand fixture. Regress stance duration, body-ground impulse and solver residual whenever a collider pair changes.
- Compare baked avatar vertices against named body proxies at the same observer tick. Exclude documented attachment seams. Start by flagging garment penetration deeper than 5% of the local segment width for three rendered samples at a fixed cadence. Review static neutral and moving variants separately. The proposed limit requires calibration against visibly good and visibly bad examples.
- Diagnose in order from anchors, axes, lengths and joint limits through mesh skinning and spring placement. Keep signed depths and the simulation pose linked to the frame. Compare default spring settings, disabled spring updates and calibrated proxy settings before changing stiffness or damping. Natural falling and actual object forces require a validated physical body in household practice.

No collision or garment policy changed in the live household during this audit. The first runtime work is a neutral geometry and swept-contact diagnostic on one supported arm and hand, followed by a pose-matched VRM visual comparison. [Hand fixture plan](ARTICULATED_HAND_FIXTURE_PLAN.md) [Required system boundaries](PHYSICAL_NERVOUS_SYSTEM_SPECIFICATION.md#authoritative-anatomy-and-collision-policy)
