# Phase B.1 · Force-aware elbow control

The original Phase B results remain intact, including its unfavorable tracking and peak-impulse outcomes. This follow-up evaluates a separate one-tick normal-impulse predictor and changes only the experimental torque residual. The original native, planar, frozen and Phase B paths are still available. [Phase B evidence](PREDICTIVE_MOTOR_PHASE_B.md)

## Impulse forecast and gate

An inverse-distance weighted model uses the nine nearest training samples in absolute joint angle, absolute velocity divided by 5, absolute target gap and previous normal impulse. It predicts the next tick's normal impulse in N s before PhysX steps. Signed error is observed minus predicted impulse. Its uncertainty band uses the larger of the development 90th percentile absolute residual and local weighted label spread. It is an empirical interval, with coverage measured below. Probability remains a separate logistic output. Locations, directions, contact duration, and peak force are not prediction targets.

The split uses complete configurations from a fixed seed schedule. Trials 0–31 train, 32–47 develop and calibrate, and 48–63 are final. The forecast evaluation uses 12 ticks per trial. The model had to beat persistence, an engineered 0.35×previous-impulse decay, and a frozen estimated contact probability times the training mean positive impulse on **development RMSE** before torque use. It passed. No configuration is split across sets. [Recorded configuration and split](evidence/phase-b1-configs.csv) · [Per-tick predictions, residuals and uncertainty](evidence/phase-b1-predictions.csv)

| Model | Development RMSE / MAE (N s) | Held-out RMSE / MAE (N s) |
| --- | ---: | ---: |
| Independent magnitude | 0.085866 / 0.034219 | 0.083652 / 0.033300 |
| Persistence | 0.121158 / 0.042788 | 0.132759 / 0.047612 |
| Engineered decay | 0.096825 / 0.028151 | 0.106060 / 0.032138 |
| Frozen probability × typical impulse | 0.092237 / 0.040739 | 0.100720 / 0.045049 |

Coverage was 180/192 in development and 181/192 held out. The calibration floor was 0.087060 N s. Lower RMSE accompanied slightly larger MAE than the engineered estimator. The squared-error gate emphasizes larger misses in this experiment. Direction and distribution over receptors remain measured outcomes, and tick impulse divided by 0.05 s would be a mean force estimate rather than a substep peak force.

## Motor experiment and fixed limits

The forecast drives a velocity-gated braking residual. The controller reduces braking when tracking error grows, smooths it with a 0.18 N m/tick slew limit, and learns a gain in [0, 1] after each impulse residual. The gated variant scales the correction by `1 / (1 + uncertainty / 0.12 N s)`. Both leave the engineered PD joint commands capped at ±2 N m before damping. The fallback latches the engineered path and restores the initial gain after excessive measured impulse, repeated large forecast misses, sustained tracking error, or alternating corrections. The ledger records the typed reason and original tick in both the trace and checkpoint. An existing separate safety latch can stop physical progression.

Before evaluation, maximum allowed mean paired regressions against engineered control were set at **+0.002 rad tracking error and +0.005 N s peak impulse**. Worst per-trial paired limits were **+0.020 rad and +0.050 N s**. These acceptance margins differ from the protective trip thresholds. All five methods ran the same 16 held-out configurations for 24 ticks, in fixed order. No final data were used to tune parameters. [All 80 trial outcomes](evidence/phase-b1-motor.csv)

| Mode | Tracking MAE, mean / median (rad) | Peak impulse, mean / median (N s) | Cumulative impulse mean (N s) | Absolute commanded work mean (J) | Correction variation mean (N m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| A · Engineered | .121218 / .118408 | .285618 / .242453 | .399828 | 1.180442 | .000000 |
| B · Frozen | .123051 / .118973 | .273460 / .222664 | .376221 | 1.086829 | .795480 |
| C · Phase B adaptive | .123120 / .119002 | .273963 / .223268 | .374528 | 1.083736 | .853518 |
| D · Force-aware | .123252 / .119597 | .266992 / .204496 | .380841 | 1.101072 | .565622 |
| E · Force-aware, uncertainty gated | .122358 / .119123 | .275927 / .227252 | .388404 | 1.135550 | .327238 |

D missed the mean tracking margin by 0.000034 rad. E passed all four point-estimate margins. Its mean tracking difference was +0.001140 rad, paired bootstrap 95% interval [+0.000627, +0.001711], and peak impulse difference was −0.009691 N s [−0.015470, −0.004395]. Intervals resample the 16 complete pairs 10,000 times with seed 6021 and are descriptive. E's worst paired tracking and peak increases were +0.003605 rad and +0.003956 N s. Its absolute work reduction was 0.044892 J [0.030971, 0.059358]. The lowest peak impulse was D's, but its tracking result bars advancement under the agreed gate. E reduced correction variation relative to both B and C while giving up work savings and peak reduction relative to B. E's largest peak was 0.596415 N s versus 0.619798 N s engineered. Several individual contacts did concentrate more load despite aggregate improvements. No online fallback was triggered. The targeted test verifies a peak guard, persisted reason/tick and subsequent zero residual. None of the results by itself establishes a reproducible advantage over the frozen and original adaptive controls.

Across configurations, tracking SD was 0.042639 rad engineered and 0.043578 rad gated. Peak SD was 0.217285 and 0.209847 N s respectively. Mean velocity reversal count stayed at 0.9375 in both. All force-aware trials made 24 online updates. The comparison measures short-run adaptation from a common initial gain. Repeated return to old tasks, cross-anatomy retention and automatic cross-configuration negative-transfer blocking remain unverified. Negative transfer and prior capability retention are checked offline by the matched baseline and gates, not inferred from optimizer state. Commanded work is torque-angle work, not metabolic energy.

## Restart, observability and library boundaries

The atomic checkpoint nests native physical state, Phase B probability state, independent impulse examples and calibration, and the force residual with its latest prediction and fallback ledger. A fresh Unity process restored at tick 6 and matched 18 subsequent probe rows with per-field tolerance 1e-6 while updates continued from 6 to 24. The checkpoint does not expose PhysX solver caches. The full 1,920-row timeline and executable checkpoint remain in the owner's experiment backup under `phase-b1`, with trace SHA-256 `0a7bae1a220a330399498b5c6611b56634698006f36affa19587e0ad90a074dc`.

The **Start Force-Aware Elbow Laboratory** menu opens a validated experiment checkpoint, stored locally as `elbow-force.checkpoint`. See the scratch [commands](../tools/unity/README.md). The tray displays prediction ± uncertainty in N s, signed error, correction, gain, update count and fallback. High-frequency control reads the procedural model directly. Library I could reference validated procedural knowledge. Library II could reference hypothetical alternatives. Library III could retain selected actual experience and signed errors. This experiment does not write to those libraries or validate future integration.

An isolated DX12 run stepped the actual tray from tick 6 to 7, then captured the paused viewport. The image shows readable controls, unobscured joint geometry and the real no-contact sample at tick 7. Contact-frame and alternate-resolution inspection remain open. Twelve close/open cycles left the complete experimental checkpoint identical. [Actual Unity capture](evidence/phase-b1-force-tray.png)

## Specification reconciliation

The repository's authoritative original file contains master §§1–186 and the retained 69-section Phase II appendix. The owner addendum covers §§187–193. This directive refers to v239 and proposed §§240–316 without their source text. The attachment, workspace and prior supplied specification were checked. Reconciliation is blocked on those complete sources. The original text and appendix remain intact. Keep unverified later cognitive and behavioral requirements outside this embodiment gate.
