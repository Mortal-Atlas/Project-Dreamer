# Phase B · Predictive motor adaptation

## Feedback cycle

At each native tick boundary the controller sees the pending joint target and the preceding sample. A trained logistic predictor estimates next-tick contact probability. The motor layer proposes a velocity-gated braking correction against the intended approach. PhysX receives the original engineered PD command with the residual added and the final commanded torque again capped at ±2 N m, before separate viscous damping. The native fixture steps ten substeps. The outcome is 1 when the resulting normal impulse is positive, otherwise 0. Signed prediction error is **observed outcome − predicted probability**. It is dimensionless. The physical trace also records measured joint feedback, resistance torque, impulses and receptor forces. The present model has no force, joint trajectory or receptor-location prediction targets.

The gain starts at 0.75. Adaptive modes apply `gain = clamp(gain + 0.12 × signed error, 0, 1)` after observing the step. The proposed residual is bounded at 0.8 N m. The next command uses the revised gain. Engineered mode applies zero residual and the frozen mode holds the same gain fixed. The safety latch stops the trial at nonfinite observed state, speed above 9 rad/s, impulse above 1.25 N s or a sequence of five rapid velocity reversals. These are fixture-specific experimental thresholds. A native engineered run remains available through the existing menu. Unit tests exercise an excessive impulse and learning bounds.

The experiment stores the applied command, predictions, receptor measurements and resulting correction together. A scalar gain is the only adaptive parameter. The checkpoint retains it and the fixed predictor with feature normalization, topology, learner identity, provenance, safety history, pending target, latest signed residual and native physical state. No trial enters autobiographical memory. Stable palm IDs carry forward. Transfer across growth or limb length has yet to be tested.

## Matched native experiment

The experiment used 12 configurations unseen during Phase A.2 training. A fixed seed schedule varied puck mass, friction, position, target direction and magnitude, and initial elbow angle. All four conditions ran each configuration for 24 ticks. Native scratch consumed 16 complete native trials. Transfer consumed eight complete planar trials and eight disjoint native calibration trials. Both predictor training schedules made 48,000 example-gradient evaluations. Online gain updates were allowed only in the two adaptive conditions. Trial state resets across configurations. The last samples from one configuration do not train predictions for another.

| Condition | Mean absolute tracking error (rad) | Mean peak impulse per trial (N s) | Mean cumulative impulse (N s) | Mean signed command work (J) | Mean absolute command work (J) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Engineered | 0.12781 | 0.23488 | 0.40699 | 0.65538 | 1.24385 |
| Frozen predictive | 0.13031 | 0.24455 | 0.37580 | 0.59661 | 1.12761 |
| Native adaptive | 0.13062 | 0.24634 | 0.37375 | 0.59378 | 1.12176 |
| Transfer adaptive | 0.13059 | 0.24359 | 0.37505 | 0.59241 | 1.11803 |

Native-adaptive absolute work decreased in all 12 paired trials and cumulative impulse decreased in nine. Tracking improved in two. Peak impulse decreased in seven, yet the condition mean increased because some peaks rose. The largest recorded adaptive per-tick impulse was 0.72858 N s versus a baseline maximum of 0.57880 N s. Estimated mean force within a 0.05 s tick is impulse divided by 0.05 s. These are not substep peak forces. Command work is the per-substep torque-angle sum and excludes passive damping and contact work. It does not represent metabolic expenditure.

There were no protective stops. All engineered trials had one measured velocity reversal above the 0.15 rad/s reporting deadband. The other conditions had one trial with zero reversals each. Native-adaptive final gain ranged from 0.453 to 0.872, and the largest mean applied correction in a tick was 0.631 N m. The adaptive and frozen results were close. This study shows that signed sensory errors caused persistent motor changes. It does not establish that adaptation improves overall motor quality or that numerical pretraining is superior. Twelve paired configurations support descriptive comparisons only. [All individual trial outcomes](evidence/phase-b-motor-results.csv) · [Representative full sensorimotor trace](evidence/phase-b-motor-trace-trial-02.csv)

## Restart and operation

A fresh Unity process reloaded the native adaptive trial at tick 6 with gain 0.65232236346168415 and six updates. The next 18 probe rows matched the uninterrupted run with zero observed deviation under a 1e-6 per-field comparison. This included probability, signed error, command proposal and applied correction, joint and puck positions, and controller gain. The update count reached 24, verifying further learning from restored state. The checkpoint does not include internal PhysX caches and the current result is limited to this fixture and platform.

Use **Dream Machine → Start Predictive Elbow Laboratory**. If a saved experiment is absent, the menu requires a compatible `native-scratch.predictor` generated by the prerequisite training study. Restore and the original native engineered path use separate files. [Isolated study and restart commands](../tools/unity/README.md)

The actual Unity camera and tray were captured through a 1280×720 render target. The first capture showed the tray covering the puck at tick 7. Raising and widening the camera frame put the puck, forearm and palm below the controls at that same state. The final capture shows the actual adaptive probability, error, gain and torque distribution. Other resolutions and full-trial framing have not been visually inspected. The tray's step advanced learning once, and twelve open-close cycles while paused preserved the entire predictive checkpoint. [Unity capture](evidence/phase-b-predictive-tray.png)

Preserve the Phase A.2 results as their own conditions. This study does not rescore them. The full 1,152-row Phase B timeline is saved in the owner's experiment backup under `phase-b`. Its SHA-256 is `d5fca284514da6546ec12b83c4e4b303434a4ed25e8281d025ef802834528d92`.
