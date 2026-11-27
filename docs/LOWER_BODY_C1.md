# Phase C.1 · Isolated physical lower body

## Protocol fixed before the native runs

Use the Phase C.0 analytical rectangular support reference and a 10 kg test system. A 0.05 s tick under 9.80665 m/s² corresponds to 4.9033 N·s of vertical loading. Compare measured feet impulses to gravity **plus** the measured change of system momentum. Preserve the analytical geometry and the household experiment independently.

| Gate | Criteria |
| --- | --- |
| Standing and momentum | After 40 ticks of settling, stand for 120 consecutive ticks (6 s). Both feet must carry positive normal impulse on at least 95% of the ticks. No non-foot ground contacts. Pelvis height within 0.025 m of the seed's neutral geometry, pelvis tilt ≤8°, knees above 0 rad and below 0.30 rad, native joint-limit overshoot ≤0.025 rad. RMS three-axis momentum residual ≤0.15 N·s per native tick. |
| Contact quality | Maximum box-sole penetration ≤0.008 m and foot drift from initial contact ≤0.020 m. No actuator cap use on more than 5% of the sampled ticks. Symmetric mean left share (I_L/(I_L+I_R)) within 0.10 of 0.5. |
| Load transfer | Use symmetric, left, re-centred, and right settled samples. Left/right conditions each shift the intended mean load share by at least 0.12 from the symmetric result. Re-centred mean within 0.10 of 0.5. Native share and analytical share from the measured COM projection agree within 0.12 during settled windows. Standing geometry, slip, contact and effort protections remain in force. |
| Step prerequisite | A verified stance foot must carry at least 85% of mean support in the final transfer window without breaking the standing protections. Until then, do not run a commanded step. For a later step, require at least 0.08 m actual swing-foot advancement, at least 0.01 m clearance, recontact and at most 0.020 m stance slip. These limits distinguish planted locomotion from visible cycling. |
| Repeatability and budget | With identical configuration and fixed ticks, compare an observed run against an unobserved run to within 0.001 m pelvis displacement and 0.08 N·s per-foot impulse. Measure warm native stepping separately from observation and household throughput. Use an exploratory budget of 4 ms/tick mean on the Windows development editor. |

Run configurations 101 and 202 plus an independent seeded size 303 sampled with stage 3 dimensions. Gate failure preserves the fixture and trace. Physical falling and recovery do not proceed until stance and feasible unloading are established.

### Development change before the final transfer run

The first stable controller's sliding carriage reached ±0.60 hip widths. It was too short for the bilateral transfer gate. The follow-up increases the range to ±0.78 hip widths, retaining the same mass and actuator cap. This gives an ideal COM excursion of 0.21 hip widths with all other masses held still. Add previously unused seeds 404 (adult) and 505 (stage 3) to the three development configurations for final evaluation. The gates above stay fixed. The bounded post-standing perturbation pilot measures response to known external impulses and disabled drives. It cannot substitute for a commanded step or a recovery experiment.
