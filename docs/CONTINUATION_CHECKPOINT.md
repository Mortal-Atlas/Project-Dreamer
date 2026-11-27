# Continuation checkpoint

Updated November 26, 2026.

## Active Phase B.1 force-aware study

- Branch `feature/embodiment-contact-lab`, [draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) against protected main. Started from `4c77bd4ec3b37662d31e452c3a808fca73dec1e3`. The original Phase B result, owner exclusions and PR stack are preserved. One worktree was present at start and finish.
- Ran 57 .NET tests and 58 isolated Unity Edit Mode tests. Isolated DX12 commands `ForceMotorStudy.Run`, `ForceMotorStudy.Reload` and `PredictiveTraySmoke.ForceAware` completed. Results and an actual Unity screenshot are tracked, and the full timeline and restart probes are archived under the owner's `phase-b1` backup subdirectory. [Reproduction and acceptance](FORCE_AWARE_PHASE_B1.md)
- Next, redesign the load-aware residual with a separate development set and preregister a comparison against frozen and Phase B adaptive control, not only engineered control. Test an independent final configuration block, retention on earlier configurations, and deliberate fallback scenarios. Do not add wrist coordination. Reconcile v239 and §§240–316 only once the complete authoritative texts are available.

## Active Phase B predictive motor study

- Branch `feature/embodiment-contact-lab`, [draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) to protected main. The Phase B implementation checkpoint is `a24ad02526effc2fc910926075a139d6f4509fa5`. Owner reconciliation at `5b7ebe6` is an ancestor. Main checkout began clean. Do not alter excluded Roslyn and recovery assets. No experimental Git worktree was created.
- Executed `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` (56 passed) and isolated Unity Edit Mode (57 passed). Ran separate Unity DX12 processes using `PredictiveMotorStudy.Run`, `PredictiveMotorStudy.Reload` and `PredictiveTraySmoke.Run`. See [study and restart protocol](PREDICTIVE_MOTOR_PHASE_B.md) and [reproduction commands](../tools/unity/README.md). Full results, trace, checkpoint and probe outputs were copied to `phase-b` under the owner-supplied backup.
- Next, run a matched study with an explicit multiobjective acceptance gate. Preserve peak impulse and tracking limits while trying a force-aware residual with a separately validated impulse predictor. Reuse the existing four conditions, add rate-matched baselines and hold out complete initial configurations. Introduce wrist coordination only after this single-joint check. No merge authorized.

## Active Phase A.2 native fixture and prediction

- Branch `feature/embodiment-contact-lab`, [draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) against protected main. Native fixture checkpoint commit `084f8e1a0d512fdd1c18faffeeb93da97b896e62`. Existing stack remains #6 → #7 → #8. No authorized merge. Existing user changes, recovery assets and VRMs remain excluded.
- Ran Release .NET tests (55 passed), isolated Unity 6000.3.19f1 Edit Mode (56 passed), DX12 `-executeMethod NativeElbowSmoke.Run` and a separate `-executeMethod NativeContactStudy.Reload` (288 identical probe predictions). The clean native-foundation checkout passed its 54 tests and reproduced the numerical CSV digest. Recreate the optional harness using [tools/unity](../tools/unity/README.md).
- Next, export per-tick signed contact-probability error alongside measured impulse and region for the native feedback loop. Match frozen and bounded adaptive motor controls on held-out masses and positions, and compare force/work, tracking, retention, and restart against the engineered controller. Add additional target labels only with explicit held-out evaluation. Verify rendered trays and profile matched open/closed cost before broadening to multijoint coordination. [Results and limits](NATIVE_ELBOW_LAB.md)

## Active Phase A embodiment branch

- Branch `feature/embodiment-contact-lab` at the implementation checkpoint `d18bd74d1f2207d49851d2315ace2942296c3af6`. [Draft PR #8](https://github.com/Mortal-Atlas/Project-Dreamer/pull/8) targets protected main and stacks on PR #7 at `e9ceeaf`. The pre-existing owner changes and local-only assets remain excluded as documented in [WORLD_CHECKPOINT.md](WORLD_CHECKPOINT.md).
- Ran the clean checkout .NET suite (54 passed), CLI `--elbow-lab` matrix, Unity Edit Mode suites (55 passed), and isolated DX12 Play Mode `-executeMethod ElbowSmoke.Run` (15 exact ticks and successful contact, save, restore and dismissal). Reproduce through the [lab protocol](ELBOW_LAB.md) and [isolated harness instructions](../tools/unity/README.md).
- Next, add a standalone Unity native-physics backend with a supported elbow and puck, using an isolated physics scene stepped in explicit substeps. Compare angle, contact onset and puck state against the numerical baseline and report same-platform checkpoint tolerances. Only then train a sensory-only predictor against nonlearning baselines.

## Active workspace and recovery slice

- Branch `feature/authoritative-simulation-clock`. Implementation commit `4489675e07f6061fa3a5ea6e8db9bede286c0c11`. [Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) depends on open PR #6 and targets protected main. Neither had GitHub reviews or inline findings at session start. [Dependency and local exclusion audit](WORLD_CHECKPOINT.md)
- Ran `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` (50 passed) and isolated Unity Edit Mode tests (51 passed). Reproduction commands and harness for the paired Unity Play Mode experiment are in [tools/unity](../tools/unity/README.md). See the progress report for the produced evidence and limits.
- Start the next implementation with a fixed-step supported elbow and single constrained contact object in a separate test scene. Define joint/receptor IDs and an authoritative pre/post-physics tick boundary. Verify repeatable contact and checkpoint of the object before adding a learned contact predictor. [Exact fixture and acceptance](EMBODIMENT_NEXT_SLICE.md)

## Active persistence slice

- Branch `feature/authoritative-simulation-clock`, kernel and Unity implementation commit `dbd49e8f5e89c9ce0774adc86e1f00855bb3bf8d`, [draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7). Base is protected main. PR #6 is an ancestor. [Local exclusions and owner inventory](WORLD_CHECKPOINT.md)
- Executed `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` (48 passed) and isolated Unity Edit Mode `-batchmode -nographics -runTests -testPlatform EditMode -assemblyNames DreamMachine.Tests` (49 passed). Ran isolated `-batchmode -force-d3d12 -executeMethod ClockSmoke.Control` and `ClockSmoke.Observed`, both through day one with matched checkpoints and in-place restore. Scratch logs begin `checkpoint-` in the isolated Unity project. Ran a temporary .NET Release profiler at tick 1140 for four and twelve individuals.
- Next, create the stable tray registration and open/close/layout persistence boundary around the current Simulation panel. Expand the matched restored-world observer experiment to multiple seeds and all implemented observer actions. Keep local VRMs and unrelated owner changes out of this branch. Do not start live physics persistence until its ownership and state synchronization rules are explicit.

## Active clock slice

- Branch `feature/authoritative-simulation-clock`. Implementation checkpoint `43bc016`, based on `49f148d`. [Draft PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) targets protected `main`. The owner’s synchronization rules are in the [Part XXXIII addendum](GITHUB_SYNCHRONIZATION.md).
- Ran `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` (43 passed) and `dotnet build dotnet/DreamMachine.Cli -c Release --nologo`. Ran `--benchmark --agents 4` and `--benchmark --agents 12` five times each. Ran isolated Unity 6000.3.19f1 Edit Mode tests for `DreamMachine.Tests` (44 passed). [Implementation report](IMPLEMENTATION_PROGRESS.md)
- Isolated Unity DX12 Play Mode runs used `-executeMethod ClockSmoke.Control` and `ClockSmoke.Observed` from the scratch harness, yielding equal state hashes and learner JSON at tick 24. Artifacts are under the same scratch editor directory listed in the predecessor baseline.
- Preserve the pre-existing local edits listed below. Next open `WorldSimulation.cs`, inventory mutable environmental, learner, clock and RNG state with their owning classes, and add a versioned checkpoint round trip across a day boundary before exposing restore in the tray.

## Predecessor baseline

- Branch `feature/master-milestone-zero`. Parent `635c55f` on `feature/seeded-growing-rigs`. Implementation commit `529f49359a52ff4421fb9e7d54eadd58f1769723`. Review the open [PR #6](https://github.com/Mortal-Atlas/Project-Dreamer/pull/6) against `main` and its [predecessor PR #5](https://github.com/Mortal-Atlas/Project-Dreamer/pull/5).
- Scope comprises the master specification and appendix, traceability and evidence registers, the save-file boundary in `LifeCycle.cs`, identity validation in `WorldSimulation.cs`, Unity startup and save integration in `DreamMachineApp.cs`, and shared Edit Mode tests.
- Local owner files observed before the changes were `Assets/DreamMachine/Resources/appearances.json`, `Assets/DreamMachine/Tests/DreamMachine.Tests.asmdef`, both package manifests, `ProjectSettings/VFXManager.asset`, untracked `Assets/DreamMachine.meta`, `Assets/Plugins*`, `Assets/_Recovery*`, and `ProjectSettings/Packages/com.unity.probuilder/`. Preserve them. The prior rescue reference is `backup/unity-local-2026-11-26` at `e5bacd4`.
- Executed `dotnet test dotnet/DreamMachine.Core.Tests -c Release --nologo` and `dotnet build dotnet/DreamMachine.Cli -c Release --nologo`. With the Release CLI DLL, executed `--room --seeds 32 --out <scratch directory>`, then `--benchmark --agents 4` and `--benchmark --agents 12` in paired batches.
- Executed Unity 6000.3.19f1 `-batchmode -nographics -projectPath <isolated project> -runTests -testPlatform EditMode -assemblyNames DreamMachine.Tests -testResults <results XML> -logFile <test log>`. The temporary DX12 harness ran two separate Unity launches with `-batchmode -force-d3d12 -projectPath <isolated project> -executeMethod SaveSmoke.Valid` and `SaveSmoke.Corrupt`. Harness and logs are under the workstation temporary directory `dreamer-editor-smoke-72ab91aae5194037bced0f66f0f2a4ab`. Play Mode checked app state and disk effects. [Results and limits](IMPLEMENTATION_PROGRESS.md)
- Pending issues include the complete checkpoint contract, native crash reproduction in the owner workspace, visual and Android passes, and asset rights and rendering validation. The four avatar files were structurally inspected in their supplied location. [Avatar mapping and growth plan](ASSET_AND_LICENSE_REGISTRY.md)
- Resume by fetching and inspecting the existing PR stack and uncommitted changes. On a dedicated feature branch start the single-learner step/save/restore scenario in §169. Verify that observer rendering leaves the authoritative state unchanged before introducing a new physics, audio or cognition subsystem.
