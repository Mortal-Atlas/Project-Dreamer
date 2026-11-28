# H5 Persistent household ball transfer

## Implementation and authority

H5 uses household item 1, the existing Ball, and gives it persistent ID **household.toy.ball**. Its sphere collider preserves the original 0.25 m diameter. It has 0.080 kg mass, a centered spherical inertia tensor of approximately 0.00050 kg m² per axis, and surface friction 0.75. The reduced native fixture uses 0.27 s⁻¹ linear damping and 0.8 s⁻¹ angular damping. The ball begins at its existing settled location. Entering the station is a logged researcher intervention that places the selected learner at an engineered supported seat and installs a 50 mm raised destination beside the visible original ball site. The bench and tabletop remain fixed after installation.

The household owns the item, revision and settled pose. During an attempt, the same identity leases a free PhysX body initialized from the last settled pose. The ordinary ball renderer follows the live body. The ball has gravity, inertia and real colliders, and is free to fall or roll. Other activities cannot issue the transitional roll command once this ball belongs to the supported physical path. While the body is live, the world keeps stepping under the same clock and settled saves are unavailable. After a real release and stability check, the simulation commits the actual native pose and rotation to item 1 and appends its outcome and firsthand reference. The position survives loss and drift away from either marked site. Files for passive trajectory replay use a content digest and contain the joint poses, forces, velocities and tick stamps within the retained observation window.

The fixture reuses the H4 opposed digit guides, bounded carriage and wrist. The 0.25 m household ball requires scaling the reduced geometry by 0.125/0.028. The collision scene contains the local household floor and raised table. Engineering references use 3.2 N fixed and 0.18 N light grip. Feedback uses the H4 force-only policy with a fresh 0.18 N cap. It adjusts commanded effort from the two pad contacts. The fixture uses a servo carriage and pitch hinge with explicit support. Articulated shoulder mass, anatomical reach, torso interference, balance and unsupported locomotion are additional dependencies.

The observer supplies a destination. The learner’s estimated horizontal pickup position is a 2 mm quantized, range- and line-of-sight-qualified sighting from the supported seat. Setup uses the full authoritative pose to initialize the temporary native representation. Sensor evidence traces grip and joint movement. Instrumented clearance, velocity, source elevation and destination support phase and score the experiment. Live visual hand-eye correction is pending.

## Frozen native verification

Run separate guarded Unity 6000.3.19f1 DX12 processes with the default household roster, seed 37 and Phone preset. Use the normal household tray and save controls. Ten 5 ms physics substeps run per authoritative 50 ms tick. The engineered table and floor are the environment colliders in the native workspace. The scene contains no externally imposed perturbation.

| Scenario | Setup and primary gate |
| --- | --- |
| Run | Arthur uses fixed grip to move the ball from its original floor site to the raised table. Alice uses fixed grip from Arthur’s actual released pose to the floor destination. Check independent histories and Arthur’s stale sighting until active inspection. |
| Unobserved | Repeat Run with the observation tray closed and camera disabled during physical execution. Compare whole world checkpoint and actual trace bytes against Run. |
| Failure | Arthur’s fixed transfer first succeeds. A light second attempt from the same table must fail the lift and leave the ball at its actual settled position at least 150 mm away. Preserve the failure in a separate checkpoint. |
| Feedback | Arthur starts from the same fresh floor pose and uses grip feedback. Record the full result and require a native force correction. |
| Restart | From a fresh Unity process restore Run. Confirm the pose, observation and world bytes, inspect using the remembered sighting, then transfer the same item again. |
| RestartFailure | From a fresh Unity process restore Failure. Confirm its actual pose, ledger and replay, including unintended displacement. |

A successful native trial requires opposing contact and 12 uninterrupted hold ticks clear of non-pad contact. Required clearance is 35 mm multiplied by the fixture geometry scale. Transport uses force actuation toward the assigned target. A placement requires opposed contact and at least 45% of measured settled weight on the correct destination surface, near the intended contact region with vertical speed under 60 mm/s, before release. Release requires five motion-qualified ticks with both pads off and support carrying at least half the measured load. Eight additional ticks with linear speed at most 12 mm/s and angular speed at most 0.15 rad/s must elapse before a terminal commit. Pending unsettled outcomes stay live. Confirm a floor-to-table translation greater than 200 mm and a rise of at least 45 mm. Record peak force, support, displacement, velocity, wrist excursion, effort, work and native step time. A failed trial retains its support and location evidence.

The cognitive comparison uses explicit attention samples. Arthur receives a valid first sighting of his own placed ball. Alice’s return occurs without another Arthur sample. Afterward he inspects a 105 mm region centered on his prior sighting, and then searches a 650 mm region. A miss preserves the old location with uncertainty. Only a positive sample replaces the old sighting. Both sighting and outcome effects are private to the individual and append to Library III. Record whether the prediction, actual environment and subsequent sensed result disagree.

## Physical results and restart

The native protocol was committed before formal runs at **cf0b2f5ecfbd9ca8e1cc138b951f6dcc6122291d**. The original H4 cases and evidence were preserved.

| Trial | Start → settled center in m (x, y, z) | Outcome | Ticks |
| --- | --- | --- | ---: |
| Arthur, fixed floor → table | (−0.560, 0.125, 1.880) → (−0.868, 0.175, 1.886) | Placed on the table | 100 |
| Alice, fixed table → floor | (−0.868, 0.175, 1.886) → (−0.523, 0.125, 1.885) | Placed in the floor region | 103 |
| Arthur, light grip from table | (−0.868, 0.175, 1.886) → (−2.378, 0.125, 2.014) | Lift failed. Ball rolled off and rested on the floor. | 307 |
| Arthur, fixed after restart | (−0.523, 0.125, 1.885) → (−0.865, 0.175, 1.889) | Same ball placed back on the table | 100 |
| Arthur, grip feedback | (−0.560, 0.125, 1.880) → (−0.830, 0.175, 1.893) | Placed on the table with pad-driven correction | 140 |

In Arthur’s first trial, the destination supported 1.754 N while the opposed pads were still applying force. After release the table carried 0.784 N, approximately the ball’s settled weight. The feedback trial raised the cap twice from 0.18 N to 1.55 N and completed in 140 ticks. It used 8.61 N s of digit actuator effort versus 19.78 N s for Arthur’s fixed transfer. The light-grip trial showed no qualified lift. Pad impact during the failed trajectory reached 12.42 N. The ball settled about 1.52 m from its previous table position, outside the bench’s supported reach. [Trial measurements](evidence/phase-h5-summary.csv) [Native events](evidence/phase-h5-native-events.csv)

Both independent fresh processes restored exactly the bytes written at the settled boundary. The initial world was 19,553 bytes. The success save contained 25,978 bytes at tick 203 and the failure save 25,026 bytes at tick 407, including ordinary time-driven household events. The cold load restored the visible Ball, including its rotation through checkpoint equality, and replayed the recorded source. Beliefs and memory ownership remained independent for Arthur (seed 303) and Alice (seed 404). The observer-open and camera-disabled runs reproduced identical native trace and world checkpoint bytes. [Checkpoint evidence](evidence/phase-h5-checkpoint.csv) [Verification harness](../tools/unity/HouseholdBallSmoke.cs)

## Spatial evidence and learner history

Arthur last saw the ball at approximately (−0.868, 1.886) on tick 100. Alice’s transfer changed the object and her own evidence while Arthur’s existing sighting stayed unchanged. His narrow inspection at tick 203 recorded an absence. His wider inspection then estimated the ball at (−0.524, 1.884). After restart his saved sighting guided another successful inspection before he moved the same ball. The transfer ledger carries the intended site, actual pose, success or failure, policy, correction count and trace reference. Library III stores the learner’s own sensor-qualified evidence and distinguishes researcher goals from observations. [Observation comparison](evidence/phase-h5-observation.csv)

The existing H4 Arthur and Alice archives remain separate. H5 exercises independently owned attempts in a shared household using engineered starting caps. Importing an H4 cap requires cross-calibrating the small fixture against the 0.25 m household ball and tracing the imported policy to its origin. The Library I pathway uses the per-learner location projection and measured motor references, with generalized affordance learning open. Library II retains the existing L.5 search machinery. There is no H5 autonomous generation of alternative motor strategies.

## Unity workflow and dependencies

Open **Physical development → Ball / switch learner**. Select **Transfer ball**, **Grip feedback** or **Light grip**, then run or step from the **Simulation** tray. The physical tray shows the last sighting, assigned site, actual ball pose, pad force, support load and corrections. Use **Inspect sighting** and **Search nearby** to sample from the selected learner’s current viewpoint. After settling, save and restore through **Simulation**. **Earlier attempt** and the replay controls display the recorded ghost trajectory without touching the actual ball. **Sphere practice** opens the H2 route. The H4 hand lab remains available from the Unity menu.

H5’s finite supported workspace can preserve unintended displacement outside reach. The learner’s ordinary housekeeping currently cannot recover a ball beyond that workspace. The next integration should replace the stationary carriage reference with a supported articulated shoulder and elbow, then verify the reach envelope, arm–torso clearance and observed target alignment against this same persistent object. The shared object and memory ownership path also supports future carrying, dropping, toy containers, furniture, drawers, creative tools, cleanup and cooperation. Those tasks still depend on posture support, contact geometry, planning and independent motion.

Recognizing the ball, remembering a site and noticing a mismatch are measured separately. The present experience records do not assign responsibility, motivation to tidy or physical skill beyond the measured moves. Native body state outside a settled checkpoint, clothing interactions, dynamically supported walking and autonomous reaching remain open.

## Verification and performance

Release .NET passed 121 tests and Unity Edit Mode passed 122. Native regressions also restored the H2 v6 checkpoint, retained Arthur’s 0.74 N cap, recorded a 12-tick hold and returned him to Living at tick 307. The H4 hand-lab regression verified load, repositioning, physical placement and passive replay. SHA-256 matched the three retained H4 research archives.

Each native 50 ms tick spent 0.576–0.700 ms in the ten PhysX substeps, including contact callbacks. The supported household step, presentation updates and final archive operation averaged 1.012–1.288 ms across the eight recorded attempts. Motion tapes occupied 38,186–116,432 bytes per attempt and stay local. Actual DX12 captures verified the bench, the raised destination, floor displacement and readable contact and sighting labels. The camera cost was 0.964–1.637 ms of CPU submission. GPU and Android timing remain open. [Timing and work by trial](evidence/phase-h5-summary.csv) [Camera timings](evidence/phase-h5-render.csv)

Sixteen alternating four-person Release Living benchmark pairs had medians of 54,750 H4 and 54,764 H5 ticks/s with overlapping sample ranges. Both allocated 390,120 bytes over 2,000 ticks. The non-practice checkpoint changed from 27,574 to 27,575 bytes. Eight alternating twelve-person kernel pairs had medians of 33,146 and 33,346 ticks/s. These are paired desktop measurements. [Benchmark rows](evidence/phase-h5-kernel-benchmark.csv)
