# Current system interfaces

| Contract | Current responsibility | Outstanding boundary |
| --- | --- | --- |
| [WorldSimulation](../Assets/DreamMachine/Core/Sim/WorldSimulation.cs) | Owns household ticks, seeded element grid and child state. Supplies the same fixed-step kernel to Unity and CLI. | Versioned asynchronous results, complete world snapshots and restricted sensory views. |
| [PopulationConfig and PersonSpec](../Assets/DreamMachine/Core/Social/Society.cs) | Stable IDs, household pairing, display name, roles and birth seed. | Classroom roles, avatar references and longitudinal identity metadata. |
| [AgentPersistentState and AgentSaveFile](../Assets/DreamMachine/Core/Development/LifeCycle.cs) | Child JSON schema, legacy anatomy compatibility, per-child file publication and load errors. | Joint transaction, world tick and RNG, wetware state, checksums and explicit backups. |
| [HumanoidAnatomy](../Assets/DreamMachine/Core/Body/HumanoidAnatomy.cs) | Seeded lengths, growth curves and neutral reach query. | Full physical constraints, measured contact, and surface calibration. |
| [DreamMachineApp](../Assets/DreamMachine/Unity/DreamMachineApp.cs) | Views the kernel, maps input to the clock and camera, and calls saves. Startup pauses on a failed restore. | Versioned workspace state, published observer-only snapshots and integrated one-agent fixture. |
| [PilotLibraryArchive](../Assets/DreamMachine/Core/Libraries/PilotLibraryArchive.cs) | Hashes, publishes and reads pilot evidence and reconstructs a deterministic pilot from its recipe. | Complete individual and world archives, privacy and production retention policies. |

The [architecture](ARCHITECTURE.md), [research foundations and dependency assessment](RESEARCH_INTEGRATION.md), [experiment protocols](EXPERIMENTAL_PROTOCOLS.md), and [performance baselines](PERFORMANCE_BENCHMARKS.md) retain their existing paths.
