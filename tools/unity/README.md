# Isolated Unity observer experiment

`ClockSmoke.cs` exercises the same embedded world with and without observer controls. Use a disposable Git checkout or project copy. Set its Unity product name to `Dreamer Audit Isolated Smoke`, then copy the script to `Assets/Editor/ClockSmoke.cs` in that copy. The script intentionally overwrites checkpoint and workspace data in that exact isolated product's persistent directory. Never reuse a directory with data you intend to keep.

Run separate Unity 6000.3.19f1 processes for each condition and seed:

```text
-batchmode -force-d3d12 -projectPath <copy> -executeMethod ClockSmoke.Control
-batchmode -force-d3d12 -projectPath <copy> -executeMethod ClockSmoke.Observed
```

Add `-dreamerSeed101` to both commands for the second seed. Pass separate `-logFile` values. The harness steps from tick 690 to 1140, compares save and restore within each process, and prints a digest of all serialized fields. The observed run also reconstructs a workspace from its independent preferences. Compare `CLOCK_SMOKE` digests within each seed. The digest is labeled `learner` for continuity with the initial smoke. It includes the full embedded world checkpoint. Kernel regression tests provide named field assertions after restore. Camera screenshots depend on the batch graphics target.

For the elbow fixture, copy `ElbowSmoke.cs` to `Assets/Editor/ElbowSmoke.cs` in the same disposable project and use `-executeMethod ElbowSmoke.Run`. It replaces only the isolated elbow checkpoint and its matching workspace preference. Verify the `ELBOW_SMOKE` completion line. The experiment compares the visible Unity path to an unobserved model every tick and exercises Save, Restore and Close.

For Phase A.2, copy `NativeElbowSmoke.cs` and `NativeContactStudy.cs` into the same scratch project's `Assets/Editor`. Run `-batchmode -force-d3d12 -executeMethod NativeElbowSmoke.Run`, then start a separate Unity process with `-batchmode -nographics -executeMethod NativeContactStudy.Reload`. Use separate logs and the same isolated product name. The first command replaces the isolated native fixture checkpoint, traces and training outputs in its persistent data directory. The second uses the saved models and probes from that directory. Look for `NATIVE_SMOKE`, `CONTACT_SCORE` and `CONTACT_RELOAD`. [Protocol and observed results](../../docs/NATIVE_ELBOW_LAB.md)

For Phase B, copy `PredictiveMotorStudy.cs` and `PredictiveTraySmoke.cs` to the same scratch Assets/Editor directory. Keep the generated Phase A.2 native-contact dataset. Run separate DX12 graphics batch processes using `-executeMethod PredictiveMotorStudy.Run`, `-executeMethod PredictiveMotorStudy.Reload`, and `-executeMethod PredictiveTraySmoke.Run`. The first command writes results, trace and continuation probes. The second reconstructs and verifies the learned continuation. The final command uses an isolated copy of the saved run in the actual tray. [Study results](../../docs/PREDICTIVE_MOTOR_PHASE_B.md)
