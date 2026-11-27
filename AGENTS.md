# Project Dreamer — instructions for AI coding agents

Put this file in the repo root. Codex (ChatGPT) reads `AGENTS.md` automatically.
For Antigravity/Gemini, point it at this file or paste it at the start of a chat.

---

## Part 1: For Jacob (the simple version)

**What this is:** A Unity sim of a family of 4 in one house. The two kids are AI
"learners" that start as babies and grow by watching and copying Mom and Dad.
Night is when they dream and lock in what they learned. It's a testbed for your
Dreaming Machines / Ten Lens paper.

**Where things stand right now (Nov 25, 2026):**
- The original sim is on GitHub on the branch `cursor/dream-machine-unity6-54e5`.
  It is NOT in `main` until you open and merge that pull request.
- It's on Unity 6.3 (6000.3.19f1). 23 tests pass, ~75.9k ticks/sec for the family.
- Nobody has pressed Play in the real editor yet, so tell us if anything looks pink or broken.

**How to see it:** open the project in Unity 6.3 → open
`Assets/Scenes/DreamMachine.unity` → press Play.

**Your daily loop:**
1. `git pull` before you start.
2. New feature? Make a branch: `git checkout -b feature/short-name`
3. Commit small and often, push, open a pull request.
4. Tests pass → merge into `main` → everyone runs `git pull`.

**Who does what:**
- Coder 1: builds features + answers tool/setup questions.
- SCRUM MASTER: plans the backlog, reviews, tests, merges.
- ChatGPT/Codex + Antigravity: your hands‑on helpers in the editor (follow Part 2).

**Coming up next (in order):** baby‑sized growing rig → walking → senses
(directional hearing, touch/pain, voices) → brain templates (habit loops,
growth from a fly‑brain starting point) → small town with a "director" that
controls events.

---

## Part 2: For AI agents (Codex, Antigravity, Gemini, ChatGPT)

### Ground rules
- **Performance first.** Every tick is always simulated, even at 1000x. Never
  skip, coarsen, or approximate ticks to go faster. Don't let ticks/sec drop
  meaningfully (baseline ~75.9k/sec for 4 people, Laptop preset, 16x16 grid).
- **No code copying between learners.** Kids learn only by perceiving and
  mimicking. Never copy a parent's weights, beliefs, or code into a child.
- **Library III is append‑only.** Never edit or delete memory entries; add new ones.
- **One sim implementation.** The sim is a shared .NET kernel; Unity only views it.
  Don't fork sim logic into Unity scripts.
- **Unity 6.3 LTS (6000.3.19f1), URP, new Input System.** Use Unity 6 APIs
  (`linearVelocity`, `linearDamping`, not `velocity`/`drag`). Legacy Input Manager is off.
- **Keep the MCP for Unity package** (`com.coplaydev.unity-mcp`) in `Packages/manifest.json`.
- **No VR** for now. Android APK is the target platform.
- **Lean.** Small, focused changes. Don't rewrite working systems.

### Repo map
- `dotnet/` — the sim kernel, CLI (`dotnet run --project dotnet/DreamMachine.Cli -- --serve`
  serves on port 4077), tests, and benchmark.
- `Assets/DreamMachine/` — the Unity view. `Resources/family.json` (names) and
  `appearances.json` (looks).
- `Assets/Scenes/DreamMachine.unity` — the main scene.
- Brian2 sidecar — spiking‑neuron scripts, run in batches, not every tick.
- See `README.md` for exact test and benchmark commands.

### Key systems (don't break these)
- Brain: Library I (priors), II (hypothesis generator), III (append‑only beliefs).
- Hidden `PhysiologyState`: reflexes, muscles/fatigue, heart/breathing, hunger,
  cortisol/oxytocin/melatonin‑like dials. Nothing internal is drawn on screen.
- Coarse grid for fire, water, ice, steam, smoke.
- Time dial: pause, 0.1x, 1x, 10x, 100x, 1000x — only changes how many 0.05s
  ticks run per frame. A test checks batched vs single‑step runs match exactly.

### Design decisions already made (follow these)
- **Rig:** procedural humanoid ragdoll, baby‑sized, growing on real infant→adult
  proportion curves per bone (newborn head ≈ 1/4 body length, adult ≈ 1/8).
  Grow in small steps during sleep. Grab points (torso, hands, head support)
  so parents/NPCs can hold, feed, and carry via physics joints.
- **Hearing:** don't use Unity audio for the AI. Sounds are events; each ear gets
  timing and loudness differences, plus wall muffling from the grid. Kids hear a **phoneme stream at every speed** (audio is only for humans watching).
- **Baby voice:** articulatory synth (Pink Trombone style): tongue, lips, voicing.
- **Touch/pain:** skin patch IDs; pain above a force threshold; seeing the arm
  touched + feeling it binds them (rubber‑hand illusion must be testable).
- **Memory imperfection:** errors happen when a memory is *recorded*, not on
  replay. Entries are stamped with a tick and locked. Keep a hidden true log for
  the director to compare against.
- **Habits:** `HabitLoop` structs that strengthen with reward, "freeze" into
  cheap shortcuts after enough repeats, and unfreeze on surprise.
- **Town:** only the family gets full brains. NPCs are cheap scripted actors run
  by a Director that schedules events (weather, visitors, fights, makeups).

### How to work
1. `git pull`, then branch: `feature/<short-name>`.
2. Make the change. Add or update tests.
3. Run all tests and the benchmark. Report the numbers.
4. Commit with a clear message, push, open a PR into `main`.
5. In the PR, say what changed, test + benchmark results, and anything you
   couldn't verify (e.g., not run in the Unity editor).


### Deterministic Experiment & Validation Commands
- Core .NET + Phase B.2 tests: `dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo`
- Phase B.2 validation output: `dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo --filter PhaseB2ValidationTests --logger "console;verbosity=detailed"`
