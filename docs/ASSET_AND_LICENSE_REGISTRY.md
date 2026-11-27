# Avatar asset and license registry

The owner supplied four local VRMs on November 26, 2026. Mapping comes from the owner’s supplied filenames and the master roster. The legacy simulation IDs still identify the same individuals in the kernel and save paths.

| Character | Classroom role | Stable ID | Local source | Embedded model and credited author | Bytes | SHA-256 |
| --- | --- | --- | --- | --- | ---: | --- |
| Merlin | Established teacher | `father` | `Merlin.vrm` | Able · Vincent Baelin-Grayson | 17,085,336 | `71DD74929142FBC4168F4AB1CDCFD497999EFCC6194720B3103F8D06533C1FE4` |
| Marie | Established teacher | `mother` | `Marie.vrm` | Student2 · pixiv VRoid Project | 18,046,108 | `2D226F936E59D544175D7BE4C3752D1AA47D26E6CEB3911897CAACAFC881CBD3` |
| Arthur | Developing student | `son` | `Arthur.vrm` | Student1 · pixiv VRoid Project | 17,376,700 | `9A39C36243858C83CBE92E09553F2C174DAFF03FAF3143EB040D393B4E84E0A9` |
| Alice | Developing student | `daughter` | `Alice.vrm` | Alice · pixiv VRoid Project | 20,930,536 | `E8DD583778E158AF844E956B0141D36DEBB32D5D2C030B37905038CE0E414E3D` |

Each file is GLB 2.0 with a VRM 1.0 humanoid extension exported by VRoid Studio 2.17.0, three meshes, 54 mapped human bones, and MToon and spring-bone extensions. The hips, spine, head, both arm and hand chains, and both leg and foot chains are present. These are structural checks of the exported data. Skinning, expression mapping, coordinate conventions, render quality and runtime costs await an authorized import. Editable `.vroid` source files were not supplied.

All four metadata blocks specify `onlyAuthor`, `personalNonProfit`, required credit, `allowRedistribution: false`, and `modification: prohibited`. These files were inspected locally and remain outside Git and the application build. Usage in a shared build and modification require rights that cover the specific files and use, or replacement assets with suitable terms. Check the specific models with their rights holders before integrating them. [VRM Public License 1.0](https://vrm.dev/licenses/1.0/) · [VRoid usage and asset terms](https://vroid.pixiv.help/hc/en-us/articles/4405813333657-Can-I-use-the-models-created-with-VRoid-Studio-Stable-Ver-for-commercial-purposes)

## Growth and rendering

The same learner and semantic skeleton persist from birth. Keep the seeded functional rig authoritative for segment proportions, joints and control. At birth, use the existing labeled procedural baby representation as a baseline. For the finished art, create or obtain appropriately licensed baby and child meshes for Arthur and Alice. Carry recognizable facial features, hair and palette across age variants. Bind each mesh to the same semantic skeleton and change its visual binding at documented developmental stages. Preserve the identity ID, policy, saved age and receptor bindings through the transition. Use a compatible authorized mature mesh when the body reaches its intended human proportions.

Start with one licensed test avatar in the isolated Unity project. Verify URP and MToon, facing direction, bind pose, joint mapping, and scale. Test that changing the display asset leaves the same simulated pose, geometry and saved identity. Inspect age transitions for clothing fit and skin distortion. No importer or package was installed during this audit. [Anatomy baseline](HUMANOID_RIG.md) · [Existing dependency reviews](PHASE_II_DEPENDENCIES.md)

## Phase C.1.1 lower-body retargeting compatibility

The read-only GLB JSON audit traversed the original scene graph and resolved hips, both upper and lower legs, feet and toes. Distances use the authored glTF rest transforms. The shoe mesh sole can sit below a toe bone. The listed toe heights are landmarks, so the adapter still needs measured sole alignment after an authorized import.

| Avatar | Rest hips Y (m) | Thigh (m) | Shin (m) | Both toe Y (m) |
| --- | ---: | ---: | ---: | ---: |
| Merlin | 1.007 | 0.370 | 0.461 | 0.048 |
| Marie | 0.874 | 0.340 | 0.400 | 0.036 |
| Arthur | 0.940 | 0.356 | 0.414 | 0.053 |
| Alice | 0.758 | 0.291 | 0.346 | 0.039 |

All four export the same local rest orientation offsets for the measured channels. Around their local X axes the hips start at +7.21°, upper legs at −6.01°, lower legs at +2.61° and feet at −3.50°. Three skins and inverse bind matrix accessors are present per model, and the inspected lower-body joint nodes occur in the skin data. Those checks establish a mapping candidate. They leave actual vertex deformation, coordinate conversion and ground alignment for a rendered trial. Compute the observed physical joint delta relative to its own rest frame, convert it to the avatar joint frame and compose it with the avatar rest rotation. Include hip and ankle frontal rotations as well as sagittal pitch. Keep visible bone scale and per-avatar ground offsets on the view side. The seed and identity remain with the physical individual.

Current per-file permission terms keep these four local source assets out of a shared build. No paid or replacement assets were acquired. Assess actual skinning, adult and infant mesh compatibility, and joint transfer with an authorized test model after permission is resolved. [VRM 1.0 humanoid convention](https://vrm.dev/en/vrm1/humanoid/)
