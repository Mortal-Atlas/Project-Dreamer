# Phase L.3 · Household avatars and delayed learning

## Private visual binding

Unity loads the checked local VRMs through UniVRM 0.131.4. Merlin, Marie, Arthur and Alice are keyed by the existing stable IDs `father`, `mother`, `son` and `daughter`. The imported renderers are children of the existing individual. Their root follows authoritative displacement. A view adapter uses the seeded procedural joint state, calibrated soles, humanoid limb chains, and the actual head target for body pose and eye aim. VRM blink and a small eating and drinking mouth pose use the existing simulation clock and action state. The existing Research toggle shows the procedural rig with its existing labels. Switching representations changes no simulation state.

The four supplied meshes have mature proportions. Merlin and Marie can use theirs at startup. Arthur and Alice use theirs at their adult anatomy milestone. Early development continues with its own body proportions. The repository contains the adapter and package references. Models and rendered derivatives stay private. [Model evidence and local paths](ASSET_AND_LICENSE_REGISTRY.md#l3-private-unity-integration)

### Native verification

Unity MCP rendered the isolated Merlin bind pose and checked the live imports. The guarded DX12 household runs rendered all four at maturity and separately rendered Merlin and Marie beside the newborns. The mature standing sample measured baked skinned geometry and verified forward knee orientation before movement. The minimum mesh point for each was at ground within the logged 0.1 mm resolution.

| Person | Seed | Target standing height | Observed standing height |
| --- | ---: | ---: | ---: |
| Merlin | 101 | 1.6160 m | 1.6080 m |
| Marie | 202 | 1.8810 m | 1.8708 m |
| Arthur | 303 | 1.8150 m | 1.8107 m |
| Alice | 404 | 1.8801 m | 1.8746 m |

The three 1,240-tick runs confirmed Research restores the procedural renderers. The mature run observed Merlin moving on 429 ticks, a changed leg pose on 413 of those, directed head and eye input on 720, blink expression on 267, reaching on 274 and sleeping pose on 244. These are sampled pose signals. The activity effects and positions came from the household. In the growth run, both learners crossed the threshold during normal ticks and picked up their mature visuals. The watched and control checkpoints matched byte-for-byte in all three runs. The original four-day household smoke also reproduced 277 records and restored its 27,668-byte checkpoint. Private captures show some foot occlusion near furnishings. Fine hand contact, skirt and furniture clearance, full seated comfort and view alignment with biomechanical contact remain open.

The first scratch shader import ran before Unity had registered MToon. Reimporting the same VRM files after the shader was available resolved it. In the first height test, using the baked scale and then applying a scaled transform counted it twice. The verified harness applies the renderer scale once. Private previews and screenshots are kept in the ignored source folder under `MerlinPreview.unity`, `Merlin-preview.png`, and the `household-private-*.png` captures. Release .NET passed 81 tests. Unity 6000.3.19f1 passed 82 Edit Mode tests. [Performance measurements](PERFORMANCE_BENCHMARKS.md#phase-l3-private-avatars)

## Prespecified follow-up study

The L.2 comparison provided a known rotation and let its acquisition trials exhaust every possible answer. L.3 will measure what the same memory access conditions can do with incomplete exposure, elapsed world ticks and missing layout information. Use the same three cues and the same control, direct retrieval, two-cue limited replay and combined conditions. Acquisition records stay identical within a seed. Use two acquisition days, two deterministic distinct placements per cue per day, and separate eight development seeds and 16 held-out seeds. Preserve each success or failure as the corresponding authoritative board outcome.

After acquisition, advance the shared world clock 2,080 additional fixed ticks. Withhold further shape exposure and probe feedback. Give all four conditions the same cue, exploration draws and interval. The delayed condition uses the original layout. The transfer condition uses a different seeded permutation concealed from every choice policy. Keep answer positions on the evaluation side. Return the same number of probes per cue in both layouts. Evaluate correct placements and repeated placement errors by cohort, without adapting parameters to the held-out data. A ceiling on delayed retrieval or a decline under concealed rearrangement is a valid result. The shared world supplies elapsed simulation time. Any effect of elapsed time on memory will need a separate model and comparison.

### Executed comparison

Development used seeds 1201–1208. Held-out scoring used seeds 1209–1224. For each seed the acquisition schedule ends at tick 1,460 and the first probe runs at tick 3,540. The interval is 104 simulated seconds. A mirrored set of tasks in one unchanged and one hidden board order supplies six probes per condition and layout per seed. Replay compiles known placements for as many as two cues. Repeated error means a currently wrong choice also appeared as a wrong numeric slot for that cue during training. This definition distinguishes old mistakes from a formerly correct placement that fails after rearrangement.

| Condition | Delayed | Hidden rearrangement | Repeated errors across both |
| --- | ---: | ---: | ---: |
| Exploration | 34/96 (35.4%) | 38/96 (39.6%) | 84 |
| Direct retrieval | 81/96 (84.4%) | 27/96 (28.1%) | 20 |
| Limited replay | 70/96 (72.9%) | 27/96 (28.1%) | 38 |
| Direct retrieval and replay | 81/96 (84.4%) | 27/96 (28.1%) | 20 |

Retention of a previously successful slot improved delayed performance relative to exploration. All three memory conditions scored below exploration under the hidden rearrangement in these seeds. Combined access matched direct retrieval, including after the rearrangement. The hidden mapping blocks evidence about a specific new correct slot. This result measures reuse of learned positions under changed affordances. It provides no evidence of memory decay or biological consolidation. The 1,152-row L.3 CSV reproduced byte-for-byte, and rerunning the old L.2 protocol preserved its CSV byte-for-byte. Next, allow the learner to inspect ambiguous socket features before choosing, keep the held-out arrangements private, and use a separately varied delay under the same exposure budget. [Trial records](evidence/phase-l3-delayed-retention.csv)

```powershell
dotnet run --project dotnet/DreamMachine.Cli -c Release -- --delayed-retention <scratch-retention.csv>
dotnet test dotnet/DreamMachine.Core.Tests -c Release --no-restore --nologo
```
