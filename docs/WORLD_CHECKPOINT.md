# Embedded world checkpoint

## Repository audit

The active branch `feature/authoritative-simulation-clock` started this session at `c5a2033`. Remote `main` was `02fbc84`. [PR #7](https://github.com/Mortal-Atlas/Project-Dreamer/pull/7) contains [PR #6](https://github.com/Mortal-Atlas/Project-Dreamer/pull/6) at `49f148d`. GitHub showed both open against main, #7 draft, and zero reviews and inline comments. Both branches remain open. Review the stacked increment with `git diff origin/feature/master-milestone-zero...origin/feature/authoritative-simulation-clock`. The proposed authorization order is #6 before #7, after accounting for predecessor PRs #1–#5. Recheck ancestry if earlier PRs are squash merged.

| Excluded local path | Classification and preservation |
| --- | --- |
| `Assets/DreamMachine/Resources/appearances.json` | Earlier palette and style development. Already recoverable at `e5bacd4` on local `backup/unity-local-2026-11-26`. |
| `Assets/DreamMachine/Tests/DreamMachine.Tests.asmdef` | Local explicit default engine reference flag. Clean-branch Unity tests use the tracked assembly definition. Backed up at `e5bacd4`. |
| `Packages/manifest.json` | Earlier Cinemachine, glTFast, ProBuilder and VFX package selection. This checkpoint adds zero dependencies. Backed up at `e5bacd4`. |
| `Packages/packages-lock.json` | Generated transitive resolution of those package changes. Backed up at `e5bacd4`. |
| `ProjectSettings/VFXManager.asset` | Generated shader references and VFX settings tied to the local package install. Backed up at `e5bacd4`. |
| `ProjectSettings/Packages/com.unity.probuilder/Settings.json` | Local package settings, backed up at `e5bacd4`. |
| `Assets/DreamMachine.meta`, `Assets/Plugins.meta`, `Assets/Plugins/Roslyn.meta` | Local folder metadata. Keep with local package or tool work. |
| `Assets/Plugins/Roslyn/Microsoft.CodeAnalysis.CSharp.dll` and `.meta` | Local editor compiler binary and import metadata, backed up at `e5bacd4`. |
| `Assets/Plugins/Roslyn/Microsoft.CodeAnalysis.dll` and `.meta` | Local editor compiler binary and import metadata, backed up at `e5bacd4`. |
| `Assets/Plugins/Roslyn/System.Collections.Immutable.dll` and `.meta` | Local compiler dependency and import metadata, backed up at `e5bacd4`. |
| `Assets/Plugins/Roslyn/System.Reflection.Metadata.dll` and `.meta` | Local compiler dependency and import metadata, backed up at `e5bacd4`. |
| `Assets/Plugins/Roslyn/System.Runtime.CompilerServices.Unsafe.dll` and `.meta` | Local compiler dependency and import metadata, backed up at `e5bacd4`. |
| `Assets/_Recovery.meta`, `Assets/_Recovery/0.unity`, `Assets/_Recovery/0.unity.meta` | Editor recovery artifacts. The scene and its file metadata are backed up at `e5bacd4`. |

The documents folder VRMs remain local under the restrictions in the [avatar registry](ASSET_AND_LICENSE_REGISTRY.md). The backup reference contains unrelated binaries and recovery assets and remains local.

## Mutable state inventory

Checkpoint coverage below denotes the owned embedded-kernel state. The earlier `StateHash` samples a subset; v1 continuation compares checkpoint bytes and named field assertions.

| Owner | Mutable state and influence | Restart and deterministic comparison | RNG or reconstruction |
| --- | --- | --- | --- |
| [WorldSimulation](../Assets/DreamMachine/Core/Sim/WorldSimulation.cs) | Tick, float simulation time, last physiology log day, quality/grid, population and children. Scheduling derives night, routine, developmental windows and practice/perception cadence from this state. | Serialized together and compared. Physiology log retained in append order. | Parent pose arrays and wetware input buffer are overwritten at the next tick. No persistent scheduler RNG. |
| [PopulationConfig](../Assets/DreamMachine/Core/Social/Society.cs) | Ordered roster, identity, role, group, pairing, birth seed and household flag affect routines and teaching. | Serialized and compared. Derived counts validated and reconstructed. | No persistent stream. |
| [ElementGrid](../Assets/DreamMachine/Core/Elements/ElementGrid.cs) | Cell temperature, fuel, oxygen, water, ice, steam, smoke, material, burning, wind, dimensions. Influences feedback, pain and learning. | All cells serialized and compared. The test changes a world cell before saving. | Grid initialization uses seed-dependent values. Saved cells replace them. Diffusion scratch is overwritten. |
| SimChild, [LifeCycle](../Assets/DreamMachine/Core/Development/LifeCycle.cs), [Locomotion](../Assets/DreamMachine/Core/Body/Locomotion.cs) | Identity metadata, pose and positions, prior pour observation, walker genome, distances, ROM recording flag, lived beliefs and ordered appendices, sensed observations and success counts. | Serialized and compared; legacy individual JSON is imported only when no primary or previous world checkpoint exists. Full observation list retained. | Birth seed reconstructs the versioned anatomical profile and immutable walker seed. Each timed practice derives its stream from birth seed and tick. Optimizer scratch is overwritten. |
| [Physiology](../Assets/DreamMachine/Core/Physiology/Physiology.cs), [EmotionState](../Assets/DreamMachine/Core/Affect/EmotionState.cs) | Age, generation, maturity, physiological values and seed factors, joint sensor and effector arrays, delay history/taps, emotions, targets, baselines and velocities. | Serialized and compared. | Seeded initialization followed by restored values. No retained stream. |
| [HebbianWetware](../Assets/DreamMachine/Core/Wetware/Wetware.cs) and [AgentMind](../Assets/DreamMachine/Core/Mind/AgentMind.cs) | Learned weights, rates, condition, Library III. | Serialized and compared. Mind binds the restored Library III. | Atlas reconstructed by birth seed. Library I/II kernel defaults stateless. Last wetware input is overwritten before use by the next tick. External custom priors and alternate wetware are outside this checkpoint. |
| World knowledge and operational command boundaries | Encounter objects come from a deterministic standard pool. No independently simulated transform store, persistent action queues or external results exist in this kernel. | No placeholder fields. Persist authoritative object IDs/poses, contact state, outstanding actions and exact RNG state when added. | Current encounters are deterministically indexed by tick and birth seed. |
| [Unity app](../Assets/DreamMachine/Unity/DreamMachineApp.cs) | Display poses, camera/preview, props, dream display, real time/catch-up queue, focus, quality request and view caches. Remote service can advance separately. | Presentation cache, wall debt, live Unity transforms, optional external inference and remote clocks are excluded. A successful explicit restore pauses the local scheduler. | Presentation dream generation derives its own seed. Future Unity physics needs explicit state and determinism limits. |
| [WorldCheckpoint](../Assets/DreamMachine/Core/Sim/WorldCheckpoint.cs) | Actual authorized learner resets produce ordered intervention records with tick, entity, request, result and reason. | Serialized and compared. Capture or read does not append. | No stream. Future object/lesson interventions should use typed command entry points. |

## Disk and recovery behavior

At a completed tick, a single v1 envelope contains the binary payload, format identifier, length and SHA-256. Publication writes and flushes a same-directory pending file, verifies reconstruction, and atomically replaces the validated primary while retaining it as `.previous`. A missing primary with a previous checkpoint signals an error. Corrupt primary bytes and the previous checkpoint are preserved for explicit recovery. The payload is capped at 64 MiB at read time. Expected scope is one application writer on a filesystem supporting atomic replace. No cross-process writer protocol or fsync of directory metadata has been tested. The digest detects damage and is not authentication. Unity loads the world before it initializes presentation. Full restart and in-place restore use fresh kernel instances. Learner generation does not advance on checkpoint load.

Standalone latch/Discovery Room experiments, immutable archive volumes, remote CLI sessions and Unity native physics are separate save formats or ownership domains. Version 1 covers the embedded household kernel, including the ordinary tick-based night learning it runs.

### Windows recovery follow-up

Isolated temporary fixtures cover a Windows read-share lock that prevents replacement, a truncated payload, incomplete header, invalid magic, and missing payload. Failed publication preserves both the primary and its prior generation and cleans its own pending file. These tests also preserve all bytes of the corrupted primary rather than rolling back automatically. Earlier tests cover a pending artifact left by an interrupted write and a missing primary with backup. Process termination during an actual publication, storage exhaustion and sudden power loss remain untested. The file flush and Windows rename/replace behavior do not establish directory-entry durability under power loss.

Future authoritative movable-object and attachment state is specified in the [next experiment](EMBODIMENT_NEXT_SLICE.md). The current scene's visual props do not have authoritative movable-body state to round-trip.
