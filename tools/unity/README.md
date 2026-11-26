# Isolated Unity observer experiment

`ClockSmoke.cs` exercises the same embedded world with and without observer controls. Use a disposable Git checkout or project copy. Set its Unity product name to `Dreamer Audit Isolated Smoke`, then copy the script to `Assets/Editor/ClockSmoke.cs` in that copy. The script intentionally overwrites checkpoint and workspace data in that exact isolated product's persistent directory. Never reuse a directory with data you intend to keep.

Run separate Unity 6000.3.19f1 processes for each condition and seed:

```text
-batchmode -force-d3d12 -projectPath <copy> -executeMethod ClockSmoke.Control
-batchmode -force-d3d12 -projectPath <copy> -executeMethod ClockSmoke.Observed
```

Add `-dreamerSeed101` to both commands for the second seed. Pass separate `-logFile` values. The harness steps from tick 690 to 1140, compares save and restore within each process, and prints a digest of all serialized fields. The observed run also reconstructs a workspace from its independent preferences. Compare `CLOCK_SMOKE` digests within each seed. The digest is labeled `learner` for continuity with the initial smoke. It includes the full embedded world checkpoint. Kernel regression tests provide named field assertions after restore. Camera screenshots depend on the batch graphics target.
