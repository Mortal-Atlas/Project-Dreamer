# C.1.4 active prelift stabilization

## Diagnosis and response

The archived seed-1010 cases began deteriorating before any swing command. Around ticks 242-245, outward COM velocity stopped decaying and then increased while contact covered all ten substeps. At tick 244 on the left, stance supplied 4.73 N s, frontal XCoM margin was 52.5 mm, absolute lateral speed was 0.052 m/s, and roll rate was 0.049 rad/s. Roll tracking error at the left ankle was 0.068 rad with peak commanded effort at 27% of a cap. The right side gave the same sequence with opposite COM velocity. The measured center of pressure remained inside the support region. The later C.1.3 triggers at ticks 251 and 252 still had about 4.79 N s and full stance contact, but the margin reached zero about two ticks later. Torque headroom and continuous support made the earlier acceleration signal actionable. [C.1.3 archived analysis](LOWER_BODY_C13.md) [C.1.4 time-aligned events](evidence/phase-c14-events.csv)

The active policy checks the loaded outward edge, COM velocity and recent velocity change during prelift. It inhibits swing and enters a distinct stabilization phase. The ballast demand slews toward neutral and the existing bounded hip roll targets relax faster. Loaded ankles apply opposing torques using XCoM, COM speed and actual load sharing. The controller keeps the same 5 ms physics, existing drive limits and original ten-tick bilateral return gate. A separate combined variant keeps C.1.3 landing behavior if swing is admitted. No limb transforms, external support or momentum state are written by the response. [Prelift gate](../Assets/DreamMachine/Core/Body/DynamicSupport.cs) [Native response](../Assets/DreamMachine/Unity/NativeLowerBodyRecovery.cs)

## Native comparisons

Development used seed 1010 on both sides with no injected perturbation and seed 303 as a matched healthy contrast. Active stabilization began at ticks 244 and 245 for 1010, seven ticks before its prior warning. Both sides regained bilateral support after 33 ticks and stayed upright through the follow-up. The C.1.3 combined and warning-only comparators fell on both 1010 sides. Both active policies still admitted and lifted the swing foot on both 303 sides. The active-only ablation subsequently fell on both short swings, while the combined policy retained C.1.3 landing recovery. [Development measurements](evidence/phase-c14-development-summary.csv)

After freezing the response, four fresh anatomies at two ages each received a single outward impulse of 0.75-1.05 N s on their eighth completed prelift tick. Both stance sides and all four matched policies were tested. All 32 trials began from verified double support of about 2.45 N s per foot. All perturbed attempts cancelled before lift. No fresh trial reached swing or completed a step. [Held-out trial measurements](evidence/phase-c14-heldout-summary.csv)

| Policy | Active responses / 8 | Returns / 8 | Falls / 8 | Return duration | Mean work | Mean native tick cost |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| C.1.3 combined | 0 | 5 | 1 | 41-54 ticks | 7.96 J | 1.069 ms |
| Warning only | 0 | 0 | 8 | no return | 13.80 J | 1.144 ms |
| Active prelift | 8 | 8 | 0 | 28-32 ticks | 4.27 J | 1.084 ms |
| Active prelift and landing | 8 | 8 | 0 | 28-32 ticks | 4.27 J | 1.067 ms |

The C.1.3 combined comparator also had two return timeouts without a body fall. Peak effort fraction averaged 0.70 for the two active policies, with no recorded saturation. Development peak clearance was 17.352 mm, and maximum travel across development conditions was 51.499 mm versus the unchanged 80 mm full-step gate. No automatic reattempt was evaluated. The prelift response expands the validated recovery envelope on these fixtures. Full locomotion remains open.

## Reproduction and continuation

Run `PreliftStabilizationStudy.Development` and `PreliftStabilizationStudy.HeldOut` from the guarded scratch editor on Unity 6000.3.19f1 and D3D12. Full native telemetry remains under the scratch product's persistent data directory. Run `LowerBodyDynamicStudy.PreliftVisual` for local frames and observer parity. Compact measurements are committed. The next development priority is the supported hand and collision diagnostics. [Study harness](../tools/unity/PreliftStabilizationStudy.cs)
