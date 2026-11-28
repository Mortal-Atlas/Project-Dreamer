# Playable household and experiential learning

## Starting point

The branch started at `05c733b857e8246ce5df83e2c2502ab02e495eb2`. The matched legacy Unity scene sent teachers after moving scene anchors during Play and kept learners in a seeded heading with speed derived from the walking exercise, including zero movement when the learned distance was nonpositive. Four existing identities remained. The yard props were a demonstration grid generated from the unrelated belief trial pool. The activity, motion and gaze paths had no shared target, and the persistent world had no state for a household object's position or use. The lower-body C.1.2 study and all its negative outcomes stay on their own native fixture. [Physical research baseline](LOWER_BODY_C12.md)

## Living mode

Open `Assets/Scenes/DreamMachine.unity` in Unity 6000.3.19f1 and enter Play Mode. The founding household starts in a furnished activity area. **Life & experience** opens a panel with the selected individual's intention, target, selection reason, needs, observed examples, actual outcomes, recent evidence and replay. **Next person** cycles all four IDs. **Living / research** shows or hides the existing research board. **Simulation** and **Reset layout** are pinned in either mode. The living-mode clock tray starts closed for a fresh preference and remains reachable through those controls. The viewer and tray read the same fixed-tick state. [Living room](evidence/household-living.png) · [Experience panel](evidence/household-experience.png) · [Legacy comparison](evidence/household-before.png)

The visual labels for the legacy default names show Merlin, Marie, Arthur and Alice. Custom display names and all serialized IDs, seeds, ages, walker state, personal histories and anatomical bindings remain in their original fields. A verified version-1 world can initialize the new activity module using its existing people and learner positions. Version 2 adds object state, teacher positions, pending activities, counters, replay pointers and an append-only experience archive. The atomic checkpoint publication path remains in use. On the first write after upgrading a legacy checkpoint, the original is copied once to `world.checkpoint.pre-household`. The new authoring uses no imported model or textures.

## Simulation behavior and provenance

The shared C# kernel makes event-driven selections at wake, completion, or a bounded retry. Needs can override play. Candidate selection uses reachability checks, reservations, distance, seed-based interests, curiosity, practice, a recent demonstration, or a retained replay target. Movement goes to the selected object or rest location, with a bounded assisted speed and a route that sweeps against the fixed house. Objects reserve one acting individual while an observer can watch from a visible location. Newborn movement uses an engineered kinematic reference. This phase has no dynamic grasp or learned bipedal control.

| Activity | Preconditions and actual state changes |
| --- | --- |
| Investigate picture cards | Approach within reach and pause to inspect. The firsthand result records inspection without claiming a prop mutation. |
| Push or roll the ball | Reach and attempt the push. A successful interaction changes ball velocity. Fixed ticks advance the position with bounded friction and play-area limits. |
| Move wooden blocks | Reach an occupied block station. The indexed stack changes and the renderer transitions the blocks to their new state. |
| Open or close the box | Reach the box and apply a command appropriate to the current lid state. Opening exposes its contents. The revision number advances on each successful change. |
| Observe or imitate a teacher | An eligible learner observes an actual, in-view action within range. Its witness record links to the demonstration. An imitation is a separate attempted action with its own range check, result and reference to that witness. |
| Eat, drink, sit and sleep | Reaching an available supply decrements its quantity and changes a modeled need. A low-energy learner can select a seat. At night each person heads to a separate bed and selects a real prior experience for replay. |

The household uses an evidence-backed affordance summary for Library I, activity proposals and replay-derived possibilities for Library II, and append-only measured action records for Library III. Every archive row holds actor, tick, action, target, provenance, revision, effect and source reference. An observation and a replay do not increment skill-practice outcomes. A witness can influence the next action through an explicit imitation attempt. Night replay also biases later selection toward the retained topic and adds a distinct Library II idea. The new slice preserves the earlier abstract motor and belief skills without injecting synthetic hidden-rule outcomes into the active household experience loop. The modeled competence here measures successful kinematic activities. Native motor learning and formal replay-versus-no-replay comparisons remain open.

The view follows measured root travel with the seeded grounded gait and uses the authoritative target for head gaze. It blends stand, seated rest, bed posture, procedural blink and an engineered reach. Canonical leg proportions and clock time come from the kernel. Block and lid animations visualize state after a completed interaction. Gripping, exact hand placement, collisions with decorative furniture, expressive facial work on a skinned model and full-body social animation remain open.

## Asset integration status

The initial household audit found author-only VRM export metadata. The later owner clarification establishes the basis for the private Merlin preview and identifies the three other exports as stock VRoid Studio samples. The automated tool review rejected an isolated UPM install command with `blocked by policy`. The owner workflow now uses the official pinned UniVRM candidate and begins with Merlin. The tracked manifest and original files remain intact. [Updated provenance and local workflow](ASSET_AND_LICENSE_REGISTRY.md#l2-owner-provenance-and-local-use)

## Verification

Release .NET passed 77 tests and Unity Edit Mode passed 78. In the isolated DX12 household, the 4,700-tick run advanced through four complete days. It recorded 142 demonstrations, 67 firsthand outcomes, 57 witnessed events, nine completed imitation actions, 16 failed records and 16 replay references. The kernel logged 48 ball pushes, 50 block changes, 24 opens, 23 closes, 30 meals and 19 drinks. The byte-identical observed and control states included all object and provenance records. The actual clock UI queued one tick after pause and resume checks. An isolated save and restore returned the full 27,509-byte checkpoint and kept the game paused. The old version-1 checkpoint was separately checked on migration. The legacy family posture smoke and paired Unity clock smoke provide separate regression comparisons. [Reproduction instructions](../tools/unity/README.md) · [Performance data](PERFORMANCE_BENCHMARKS.md#playable-household)

| Requirement group | Status and evidence |
| --- | --- |
| Household activities and persistent objects | Verified for the four-person roster and the kinematic interaction adapter. [Kernel](../Assets/DreamMachine/Core/Sim/HouseholdLife.cs) · [Deterministic tests](../Assets/DreamMachine/Tests/EditMode/HouseholdLifeTests.cs) · [Recorded Play Mode experiences](evidence/household-experiences.csv) |
| Identity, growth, checkpoints, and observer independence | Verified legacy upgrade and resumed activity, display-only identity labels, seeded gait, live controls, and byte-identical control. [World checkpoint](../Assets/DreamMachine/Core/Sim/WorldCheckpoint.cs) · [Unity smoke](../tools/unity/HouseholdLivingSmoke.cs) |
| Attention, presentation modes and experiential observatory | Verified in the live scene with original primitives. Prepared model import, rendered skinning, and full skeletal visualization remain open. [View](../Assets/DreamMachine/Unity/DreamMachineApp.Living.cs) |
| Teacher and learner interaction | Compared recorded demonstration, witnessing and imitation provenance over four completed simulated days. A separate teacher-assistance interaction and controlled retention study remain open. |
| Provided character models | Inspected assets and importer metadata. Local import and visual validation depend on usage permission and an authorized package install. |
| Independent physical research | C.1.2 evidence retained. C.1.3 cancellation, lowering and controlled return remain the next native experiment. |

## Continuation

Phase L.2 extends this baseline with actor evidence, sorting, cleanup and a controlled retention comparison. The owner-authorized Merlin preview is the first manual dependency. Verify MToon and textures, semantic bone mapping, bind pose, limb and ground alignment, expression binding, and view-only retargeting from the existing anatomy. Keep infancy on age-appropriate proportions. Carry the native C.1.3 early-cancellation and controlled-return experiment on its independent fixture. [L.2 results](ACTIVE_LEARNING_L2.md)
