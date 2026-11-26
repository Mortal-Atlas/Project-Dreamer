# Changelog

## Unreleased

- Added a fixed-tick latch-room pilot, paired curiosity and replay controls, CSV reports, and a Unity Editor inspector for the actual event trace.
- Audited the merged family simulation, current dependencies and research options. Documented the implementation sequence, approval points, benchmark and first curiosity/replay experiment in `docs/`.
- Corrected shared batch timing so wall-clock budgets and throughput span every executed full tick. Added a regression test for the budget and same-seed replay.
- Normalized the Main Camera rotation serialized in `DreamMachine.unity` and added an Edit Mode regression test for the rotation.
- Removed duplicated test runner references that blocked compilation in a clean Unity checkout.
