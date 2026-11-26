# Project Dreamer

This Unity **6.3 LTS** project (`6000.3.19f1`, URP 17.3.0) contains the Dream Machine family simulation.

The authoritative implementation requirements are in the [master specification](docs/MASTER_IMPLEMENTATION_SPECIFICATION.md) and [mandatory GitHub synchronization addendum, §§187–193](docs/GITHUB_SYNCHRONIZATION.md). The [traceability map](docs/REQUIREMENTS_TRACEABILITY.md) records the evidence and remaining work.

The November 2026 audit documents the [current architecture](docs/ARCHITECTURE.md), [research and licenses](docs/RESEARCH_INTEGRATION.md), [benchmarks](docs/PERFORMANCE_BENCHMARKS.md), [roadmap](docs/DEVELOPMENT_ROADMAP.md), and [first room experiment](docs/EXPERIMENTAL_PROTOCOLS.md).

Run the latch-room pilot with `dotnet run --project dotnet/DreamMachine.Cli -c Release -- --room` or open **Dream Machine → Latch Room Trace** in the Unity Editor. The [pilot protocol](docs/EXPERIMENTAL_PROTOCOLS.md#latch-room-pilot) explains the controls and measured results.

Phase II adds an opt-in two-stage Discovery Room through `--discover`. See the [room protocol and developmental design](docs/PHASE_II.md) and [held-out results](docs/PHASE_II_RESULTS.md).

Open **Dream Machine → Library Observatory** to archive a latch pilot, browse its three libraries, and verify its checkpoint. See the [archive format and controls](docs/LIBRARY_OBSERVATORY.md).

Open **Dream Machine → Body Observatory** to inspect a live rig or compare seeded body profiles across growth. See the [anatomy and rig design](docs/HUMANOID_RIG.md).

## Open and press Play

1. Install **Unity 6000.3.19f1** with the **Universal Render Pipeline** (the editor version in `ProjectSettings/ProjectVersion.txt`).
2. In Unity Hub, choose **Add** → **Add project from disk** and select this folder.
3. Open `Assets/Scenes/DreamMachine.unity`.
4. Press **Play**.

You should see one house, four people, and a panel of beliefs and feelings. The displayed names come from `Assets/DreamMachine/Resources/family.json`. Drag the yard to look around. Open **Simulation** to pause, step, change speed, save, or restore. The workspace remembers whether the tray was open independently of the world checkpoint.

The rest of this file is the research notes for the kernel, the experiment, and the limits of the prototype.

A research prototype of a small cognitive architecture. Six baby agents live in one yard with six mentor characters. The babies try to learn two things at once:

1. Which particular objects are safe or harmful, by testing guesses against what actually happens and **amending** the old belief instead of deleting it.
2. How to walk, by babbling with their own bodies and by watching other bodies. They never read a mentor's animation parameters.

The experiment asks one question and writes the answer to a CSV file:

> Does a mind whose beliefs are tested against lived experience, and amended rather than erased, become more accurate over time than a mind that only keeps a general prior?

On the sample run in `ExperimentOutput/` (8 seeds, 80 trials), the logged answer is **yes**. Before the hidden rule changes, the full mind is a bit more accurate than the prior (0.775 versus 0.706). After the world changes, the prior falls to 0.394 and the full mind, which amends its beliefs, is at 0.738. Condition B, ideas without lived amendments, stays tied with the prior. The code does not hard-code that verdict. It measures it. The Edit Mode test repeats the comparison on 12 seeds.

This repository was checked with a plain .NET test project, because the machine that prepared it does not have the Unity Editor or an Android build toolchain. Opening the project in Unity Hub is the step that still belongs to you. Details are in [What was verified here](#what-was-verified-here) and [Build an Android APK](#build-an-android-apk).

## Open it in Unity Hub

This repository is already **Unity 6000.3.19f1** with URP. Use that editor. Do not retarget it to 2022.3.

1. Add **Android Build Support** only if you want an APK (SDK, NDK, and OpenJDK).
2. Open `Assets/Scenes/DreamMachine.unity`.
3. Press **Play**. The family, the house, and the belief panel are built when play mode starts.

The first time Unity opens the project it imports scripts and shaders. That can take a few minutes. When you see a green yard, twelve blocky people, and a dark panel of text, it is running.

There is no login and no account. If you press Play and the yard is empty for a second, wait. The scene builds itself when play mode starts.

## What you should see

- **3 baby boys and 3 baby girls**, smaller, learning to move.
- **6 mentors**. They are not learners. They walk, idle, gesture, and sit with a procedural body so the babies have motion to watch.
- **Colored objects** in the middle of the yard. Some are harmful under a hidden rule. The rule changes partway through a session. A line of text in the world announces the change.
- **Day and night.** About 36 seconds awake, then about 16 seconds of night. At night the light goes blue, the babies rest, and ghostly copies of objects they actually saw appear in front of the selected baby.
- **A panel** on the right (or the bottom half, on a tall phone screen).

### The panel

- Names along the top switch which baby you are inspecting. Touch or click one.
- **Eye** is that baby's camera. **IDs** is a second camera that paints each object a flat identity color, so the baby can separate "this cube" from "that sphere."
- Five **sliders** are feelings: curiosity, frustration, satisfaction, fear of falling, and fatigue. They are continuous dials from 0 to 1. They ease up and down. They are not buttons. Under them, valence, arousal, and sleep pressure are derived from those dials.
- The scrolling text is **Library III**: beliefs, confidence, and the latest appendix lines (`old -> new`).
- **Experiment** runs a short scientific comparison and prints the verdict on the panel. It also writes CSV files.
- **Save** writes each baby's full state. **Reset** deletes those saves and starts generation 0 again.
- **Fast / Detail** switches vision resolution. Fast is the default and is the right choice on a phone.

Drag on the yard (not on the panel) to orbit the camera. Pinch, or use the mouse wheel, to zoom.

The babies' joint angles come from a learned controller. Mentors play a generated walk, idle, gesture, or sit. A baby copies a mentor only by measuring bone angles on the visible body.

## The three libraries

| Piece | What it is here | In the code |
| --- | --- | --- |
| **Birth seed** | One integer. It drives every random choice, so a run can be repeated. | `SeededRandom`, `PossibilityAtlas` |
| **Possibility atlas** | The descriptions this seed finds thinkable, each with a weight. | `PossibilityAtlas` |
| **Decision coordinate** | Where in that atlas the mind landed for this choice. It is a column in the CSV. | `DecisionCoordinate` |
| **Library I** | General knowledge. "Right in general," which is only roughly right, and which does not notice when the yard changes. | `ILibraryOne`, `StatisticalLibraryOne` |
| **Library I hook** | The same interface, ready for a real language-model call. It does nothing until you assign a transport. | `LlmLibraryOneHook` |
| **Library II** | Borges's library as an idea generator. It combines features of the object in front of the baby into candidate sentences. It does not look at outcomes. | `LibraryTwo` |
| **Library III** | What the baby has lived. Each belief has a source, a confidence, and a history. A change is an appendix: time, evidence, old statement, new statement, old confidence, new confidence. Records are retired, never deleted. | `LibraryThree` |
| **Particular objects** | Two things can share a color and a shape and still differ. A belief about `object:obj-11` can override the category once the baby has met that object enough times. | `ObjectInstance`, `AgentMind` |

Library III exports to JSON with the rest of the baby. That JSON is the save file.

## The experiment

Three conditions, same objects, same hidden rules, many birth seeds:

- **A — Library I only.** The general prior. It never updates.
- **B — Library I + II.** It can formulate ideas, but it scores them against the prior, not against experience. It does not grow a belief library.
- **C — Library I + II + III.** It tests ideas against what happened, and amends Library III.

The yard's hidden rule changes at a fixed trial. Condition A cannot follow. Condition C's confidence and statement change, and the old wording stays in an appendix.

### Run it from the panel

Press **Experiment**. The short run is 6 seeds and 60 trials. The panel shows late-window accuracy for A, B, and C, and the folder where the CSV was written.

### Run it from a terminal without Unity

This uses the same C# core the Unity scene uses.

```bash
dotnet run --project dotnet/DreamMachine.Cli
```

A shorter or longer run:

```bash
dotnet run --project dotnet/DreamMachine.Cli -- --short
dotnet run --project dotnet/DreamMachine.Cli -- --seeds 12 --trials 80 --shift 40 --out ExperimentOutput/belief_trials.csv
```

### Run it inside Unity, with no window

Close Unity, then from a terminal (use the path of your Unity binary):

```bash
Unity -batchmode -nographics -projectPath . -executeMethod DreamMachine.UnityBridge.BatchExperiment.Run -- --seeds 12 --trials 80 --out ExperimentOutput/belief_trials.csv
```

Put a space and `--` before the experiment flags so Unity does not swallow them.

### How to read the CSV

`belief_trials.csv` has one row per seed, condition, and trial:

| Column | Meaning |
| --- | --- |
| `condition` | `A_library_I`, `B_library_I_II`, or `C_library_I_II_III` |
| `seed` | Birth seed |
| `trial` | 0, 1, 2, … |
| `correct` | 1 if the prediction matched the hidden outcome |
| `accuracy_cum` | Accuracy from trial 0 up to this row |
| `hypothesis` | The atlas entry the mind used |
| `confidence` | How sure it was |
| `rule_phase` | 1 before the hidden rule changes, 2 after |
| `appendix_count` | How many amendments exist so far (C only) |
| `belief_count` | How many belief records exist (C only; it never goes down) |
| `object_id` | The particular object, such as `obj-11` |
| `decision_coordinate` | Index and name of the atlas slot, plus a 0–1 position |

`belief_summary.csv` averages those rows into windows: opening, pre-shift, post-shift, and late.

`growth_days.csv` is the nursery, not the belief experiment. One row per baby per phase:

`day`, `agent_id`, `generation`, `phase` (`awake` or `night`), `motor_distance`, `rom_coverage`, `belief_accuracy`, `gait_similarity`, and the feelings as decimals: `curiosity`, `frustration`, `satisfaction`, `fear`, `fatigue`, `valence`, `arousal`, `sleep_pressure`.

On a phone or in the editor, the belief CSVs from the **Experiment** button land in the app's persistent data folder. The panel prints that path. On a desktop editor it also copies them into `ExperimentOutput/` in this project. On Android that folder is private to the app. The on-screen verdict is the copy you can read without a cable.

A sample from the .NET runner is already in `ExperimentOutput/` so you can open a CSV before you install Unity.

## Day, night, growth, and walking

Each baby is **one model**. Saving and loading does not swap in a new network. The next time you press Play, generation goes up by one and the genome, beliefs, feelings, and muscle notes come back.

- **Awake.** The baby practices a few hill-climb steps on its own joint keyframes, watches a body (weighted toward the group leader, then its paired mentor, then the others), and touches a nearby object.
- **Night.** Sleep pressure has been rising with fatigue. The dream stage rebuilds only objects that baby has seen. Library II may swap their places. It may not invent an object that was never seen. The baby replays the leader's measured pose into muscle memory and files a dream hypothesis in Library III.
- **Walking.** There is no walk-cycle clip on a baby. The body is a sagittal model: hips and knees from an evolved keyframe loop, balance, and a fall penalty. Early on, most babies barely move. Distance is the score. Arms swing as a passive opposite of the hips, and each joint keeps its own range-of-motion note.
- **Muscle memory** stores those ranges and the walk-sequence confidence. Updates are appendices. Nothing is deleted.
- **Mentors** use `MentorMotion`, a procedural walk, idle, gesture, and sit. Babies receive joint angles measured off the transforms (`SegmentedBody.Measure`), then mapped through a small body schema (`PoseMapper`). They do not receive the style number that shapes the mentor's stride.

Feelings push exploration, learning rate, and sleep pressure in proportion to their current level. High curiosity widens babble. High fear and fatigue narrow it. This is a loose stand-in for a "feeling and drive" lens, not a clinical model.

## The family

The default scene is one shared home with four roles. The names in the product are placeholders. Edit `Assets/DreamMachine/Resources/family.json` to rename them. The ids stay `father`, `mother`, `son`, and `daughter`, so code does not need to change. Looks for those ids live in `appearances.json`.

| Id | Placeholder | Role |
| --- | --- | --- |
| `father` | Father | Parent. Stands out (taller, circlet). Primary role model for the son. |
| `mother` | Mother | Parent. Stands out. Primary role model for the daughter. |
| `son` | Son | Child. Also watches Mother and his sister. |
| `daughter` | Daughter | Child. Also watches Father and her brother. |

Both parents are a couple. Both children are siblings. Bond strengths are continuous: same-gender parent-child is strongest, the other parent is next, then the sibling, and the parents' bond is part of the home the children feel. The day is a routine: morning meal, play, evening meal, bedtime. Meals and bedtime sit the parents down. Play is when they walk and the children practice.

A larger cast is still available with `PopulationConfig.CreateBalanced(n)` if you need a crowd later. It is not the default.

Bodies stay rigid capsules and boxes: face, hair, clothes. No skinned mesh. No internal organs are drawn.

## Insides (simulation only)

Each child has a `PhysiologyState` that is never meshed.

- Nerves: stretch sensors, a withdrawal reflex, and a descending drive from `IWetware`. Signals sit in a delay line (conduction time). The delay shortens as the child matures.
- Muscles: activation, force, fatigue, and recovery on every joint. Force scales the walk step. Reachable angles are written into the muscle-memory library.
- Heart and breath: rise with exertion, arousal, pain, and smoke.
- Energy and hunger: activity spends energy. Meals and night rest refill it.
- Hormones: cortisol-like stress, oxytocin-like bonding, melatonin-like night pressure. They nudge the emotion dials. They do not replace them.

Age advances only by simulated time. The same object grows. A generation counter still raises maturity when a save is loaded for a new session. Nothing is swapped for an adult model.

`ExperimentOutput/physiology.csv` is written beside `belief_summary.csv`.

## Fire, water, smoke

A coarse grid (`ElementGrid`) is the yard's states of matter. Resolution follows the quality preset: 12 on Phone, 16 on Laptop, 24 on Beefy. Each cell has temperature, fuel, oxygen, water, ice, steam, and smoke.

- Ice melts, water boils, steam condenses.
- Wood burns when it is hot, dry, and has fuel and oxygen. Heat spreads. Water puts a cell out. A wet cell resists ignition.
- Smoke and steam drift upward and with a seeded wind.
- A child near fire feels pain, which trips the withdrawal reflex, raises heart rate, and writes a Library III belief `class:fire`. A parent pours water onto the nearest flame. The child can record `water-puts-out-fire`.
- Markers in the scene are colored spheres. They are a view of the grid, not organs.

## Time

The sim tick is fixed at 0.05 seconds. Pause, 0.1x, 1x, 10x, 100x, and 1000x are on the top bar (and comma, period, 1, and space on a keyboard). Fast-forward only runs that same `Tick` more times per frame. It does not skip, coarsen, or jump the state analytically. If the machine cannot keep the requested rate, the bar shows the achieved multiplier and ticks per second. The unrun request is not turned into a fake age.

**Stage** (or F2–F5) means "keep calling `Tick` at the frame budget until the youngest child reaches toddler, child, teen, or adult, then return to 1x." A test checks that a batched fast run and a one-tick-at-a-time run of the same length produce the same tick count and the same state hash.

**Save age** writes each child's JSON and the sim clock under the app's persistent `stages/` folder.

## Performance

The authoritative sim is the .NET `WorldSimulation`. Unity is a viewer of that state. It can also run the sim locally. A second PC can run:

```bash
dotnet run --project dotnet/DreamMachine.Cli -- --serve
```

That listens on port 4077. In the app, **Remote** connects to `127.0.0.1:4077` and stops stepping a local sim. Point it at another machine by changing the host in `ConnectRemote` if you need a different address.

Quality presets change grid size, how many ticks a frame may attempt, and whether the eye camera runs during fast-forward. They do not drop ticks inside a life. Spike vision (`SpikeVision` plus `brian2-sim/vision_batch.py`) is off on the phone. The Python script eats a batch of frames, not one IPC call per tick. A Burst job colors the debug overlay. The sim kernel itself stays one C# implementation so the phone, the desktop, and the server cannot drift.

Benchmark on this environment (Laptop preset, 16×16 grid, 2000 ticks after a short warmup):

| Roster | Learners | Ticks/sec | Sim seconds per wall second |
| --- | --- | --- | --- |
| 4 (the family) | 2 | 75860 | 3793 |
| 12 (`CreateBalanced(3)`) | 6 | 42691 | 2135 |

```bash
dotnet run --project dotnet/DreamMachine.Cli -- --benchmark
dotnet run --project dotnet/DreamMachine.Cli -- --benchmark --agents 12
```

Your machine will differ. The HUD prints the live ticks/sec while you play.

## Vision

The selected baby has two cameras parented to the head:

1. A normal view (the **Eye** preview).
2. A replacement shader, `DreamMachine/IdColor`, that writes `_IdColor` instead of the painted color. Black is background. The red and green channels encode the object number.

`IdMaskDecoder` turns that image into masks and bounding boxes. Those become `VisualObservation` records (id, mask box, pose in the yard, appearance). At night, `DreamRecombinator` rebuilds props from that list only.

The other babies do not each render two cameras. They record objects they come close to. That is a performance choice. `IObjectDetector` is the seam for a learned detector later. `IdMaskDecoder` is the only implementation shipped.

## Wetware

`IWetware` is the plasticity seam.

- `HebbianWetware` is what the live babies step, using walk progress and feelings as input.
- `LeakyIntegrateFireWetware` is a small spiking population, covered by tests.
- `ReplayFileWetware` plays a CSV. The sample is `Assets/DreamMachine/Resources/Wetware/brian2_replay.csv`.

To regenerate that sample from a real Brian2 STDP run, follow [brian2-sim/README.md](brian2-sim/README.md). There is no organoid in the loop. A future recording can replace the file, or you can add another class that implements `IWetware`.

## Tests

Unity Edit Mode tests live in `Assets/DreamMachine/Tests/EditMode/`. They use only NUnit and the core, so the same files compile without the editor:

```bash
dotnet test dotnet/DreamMachine.Core.Tests
```

They cover seeds, append-only beliefs, the A-versus-C accuracy claim, the rule-shift amendment, instance-specific predictions, continuous feelings, the 6-and-6 pairing and leader salience, walking improvement, imitation from observed angles, both wetware models, the ID mask, dream props limited to what was seen, and save/load across a generation.

## Build an Android APK

**This environment could not produce an APK.** There is no Unity Editor, no Android SDK, and no Unity license here. The project is configured so Hub can build one. Package id `com.jacobgino.dreammachine`, minimum Android 7.1 (API 25), **IL2CPP**, **ARM64** only.

### On your machine (Unity Hub)

1. Use Unity 6000.3.19f1 with Android Build Support (SDK, NDK, OpenJDK).
2. Open this project.
3. Menu **Dream Machine → Build Android APK**.

   That runs `DreamMachine.EditorTools.BuildScript.BuildAndroid`, which sets the package id, IL2CPP, and ARM64, then writes `Builds/Android/DreamMachine.apk`.

   Or do it by hand: **File → Build Settings → Android → Switch Platform → Build**. The player settings are already filled in. Use your own keystore if you want a store build. A debug keystore is enough to install on your phone.
4. Copy the APK to the phone and install it. Android will ask you to allow installs from that source. The app is landscape or portrait. The panel moves to the bottom on a tall screen.
5. Open the app, watch a day, then tap **Experiment** and read the verdict on screen.

### From a terminal, once Unity is installed

```bash
Unity -batchmode -nographics -quit -projectPath . -buildTarget Android -executeMethod DreamMachine.EditorTools.BuildScript.BuildAndroid
```

The first Android build downloads SDK pieces and compiles IL2CPP. It is slow.

### GitHub Actions

`.github/workflows/android-apk.yml` is a GameCI workflow you can run by hand. It does not run on every push. Add repository secrets `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` (see [GameCI's license steps](https://game.ci/docs/github/activation)). The artifact is the APK.

## Performance, and running on more than one machine later

Default play mode is the fast setting: one pair of cameras (the selected baby, 96×64), no realtime shadows, and a short practice burst per baby rather than a full search every frame. Twelve segmented people are comfortable on a laptop. On a phone, leave **Fast** on.

Rough expectations:

- The belief experiment itself is pure C# and finishes in well under a second for 12 seeds.
- The 3D nursery costs whatever the cameras cost. Two extra full-HD cameras per baby, times six, is the thing that will hurt. Do not turn that on until you mean to.
- Walking search is the other cost. The live scene does 2 to 4 hill-climb iterations per baby every couple of seconds. The headless nursery can do more.

Nothing in this build opens a socket. Each baby is already a JSON document (`AgentPersistentState`) plus pose traces (`PoseTrace`) that travel through `IPeerChannel`. The shipped channel is `InProcessPeerChannel`. A later distributed run would replace that class, send the pose trace and the object encounter, and leave Library III on the machine that owns the baby. The README is the contract. The network is not written.

## Project map

```text
Assets/DreamMachine/Core        Pure C#. No UnityEngine. This is the science.
Assets/DreamMachine/Unity       Scene, bodies, cameras, touch panel, batch entry.
Assets/DreamMachine/Editor      Android build menu.
Assets/DreamMachine/Tests       Edit Mode tests, shared with the .NET project.
Assets/DreamMachine/Resources   appearances.json and the Brian2 replay CSV.
Assets/Scenes/DreamMachine.unity
brian2-sim                      Python STDP sidecar.
dotnet                          dotnet test and a headless experiment runner.
ExperimentOutput                Sample CSVs from the runner.
```

The core assembly is `noEngineReferences`, so a Unity compile cannot quietly start calling `UnityEngine` from the experiment.

## What is done, and what is only a seam

**Done**

- Birth seed, possibility atlas, decision coordinate.
- Library I (statistical prior), Library II (seeded combinations), Library III (append-only beliefs, JSON).
- The three-condition experiment, CSV, on-screen verdict, and a batch-mode method.
- Continuous feelings with inertia, blending, and proportional effects on exploration, learning, and sleep.
- One persistent baby state, save/load, generation counter, day/night consolidation.
- Self-learned walking in a tested sagittal body. Mentors are procedural and observed as joint angles.
- Muscle-memory ranges and sequence notes with appendices.
- Default household of four placeholders (Father, Mother, Son, Daughter) renamed from `family.json`, with same-gender role models, sibling attention, and couple/parent/sibling bonds. `CreateBalanced` still scales the cast.
- Hidden physiology (nerves with delay, muscles, heart, breath, energy, hormones) logged to `physiology.csv`.
- Coarse fire, water, ice, steam, and smoke grid. Parents pour water. Children can believe fire hurts.
- Fixed-step time scale, achieved-speed HUD, stage catch-up that is only more full ticks, and per-child stage snapshots.
- Headless sim server on port 4077 and a Remote viewer button. Burst colors the grid overlay. Phone / Laptop / Beefy budgets.
- Data-driven low-poly looks. Leaders are visually distinct.
- ID-color segmentation, bounding boxes, and a dream stage that only restages seen props.
- Hebbian wetware, a leaky integrate-and-fire toy, and CSV replay. Brian2 script included.
- Edit Mode tests and a .NET mirror that runs without Unity.
- Android player settings and `BuildScript.BuildAndroid`.

**Not done, on purpose**

- No Unity Editor and no Android SDK in the environment that prepared this repo, so the scene was not pressed Play in Hub, and **no APK was produced**. Build steps are above.
- Babies are not simulated by PhysX ragdolls or `ArticulationBody`. The scored physics is the C# sagittal model. The scene poses the segmented body from that model. A PhysX body that learns in the editor's solver is future work. It is easy to destabilize, and it would not match the headless CSV.
- Mentor motion is procedural, not a Mecanim humanoid clip from Mixamo or Unity's asset store. `MentorMotion.PoseAngles` is the function to replace if you add clips. Keep publishing bone angles, not curves, to the babies.
- Library I does not call a language model. Set `LlmLibraryOneHook.Transport` when you have an endpoint. Do not ship a key in the repo.
- No organoid and no live Brian2 process inside Unity. Replay a file, or implement `IWetware`.
- No learned object detector. `IObjectDetector` is the hook.
- No networking. `IPeerChannel` is the hook.
- Elbows and arms are partly a passive coupling to the hips, plus their own babble ranges. The search that improves distance is on the legs.
- The social rules are a small household routine, not a society simulator.
- Physiology is a compact continuous model (one delay line, one muscle array, hormone dials). It is not an anatomical atlas, and it is not drawn.
- Fire and water are a coarse grid, not a 3D fluid solver or SPH with thousands of particles.
- The Burst job colors the overlay. The sim tick is the shared C# kernel, on purpose, so fast-forward cannot fork a second physics.
- Remote view applies positions, age, night, and heart rate from the server. It does not stream every grid cell.

## License of third-party pieces

Unity's own packages are whatever license Unity Hub applies when you install the editor. Brian2 is the open-source simulator from the Brian project. This repository does not include paid animation packs or downloaded character models.
