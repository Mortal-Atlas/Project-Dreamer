# Avatar asset and license registry

The owner supplied four local VRMs on November 26, 2026. Mapping comes from the owner’s supplied filenames and the master roster. The legacy simulation IDs still identify the same individuals in the kernel and save paths.

| Character | Classroom role | Stable ID | Local source | Embedded model and credited author | Bytes | SHA-256 |
| --- | --- | --- | --- | --- | ---: | --- |
| Merlin | Established teacher | `father` | `Merlin.vrm` | Able · Vincent Baelin-Grayson | 17,085,336 | `71DD74929142FBC4168F4AB1CDCFD497999EFCC6194720B3103F8D06533C1FE4` |
| Marie | Established teacher | `mother` | `Marie.vrm` | Student2 · pixiv VRoid Project | 18,046,108 | `2D226F936E59D544175D7BE4C3752D1AA47D26E6CEB3911897CAACAFC881CBD3` |
| Arthur | Developing student | `son` | `Arthur.vrm` | Student1 · pixiv VRoid Project | 17,376,700 | `9A39C36243858C83CBE92E09553F2C174DAFF03FAF3143EB040D393B4E84E0A9` |
| Alice | Developing student | `daughter` | `Alice.vrm` | Alice · pixiv VRoid Project | 20,930,536 | `E8DD583778E158AF844E956B0141D36DEBB32D5D2C030B37905038CE0E414E3D` |

Each file is GLB 2.0 with a VRM 1.0 humanoid extension exported by VRoid Studio 2.17.0, three meshes, 54 mapped human bones, and MToon and spring-bone extensions. The hips, spine, head, both arm and hand chains, and both leg and foot chains are present. These are structural checks of the exported data. Skinning, expression mapping, coordinate conventions, render quality and runtime costs await import.

All four exports embed `onlyAuthor`, `personalNonProfit`, required credit, `allowRedistribution: false`, and `modification: prohibited`. The owner's subsequent origin clarification and the upstream license assessment below govern the intended private preview. Preserve these embedded fields and credits. [VRM Public License 1.0](https://vrm.dev/en/licenses/1.0/) · [Current assessment](#l2-owner-provenance-and-local-use)

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

The four sources stay out of the shared build while redistribution is unverified. No paid or replacement assets were acquired. Assess actual skinning, adult and infant mesh compatibility, and joint transfer after UniVRM is available. [VRM 1.0 humanoid convention](https://vrm.dev/en/vrm1/humanoid/)

## L.2 owner provenance and local use

The November 27 inventory found the four listed VRM exports in the owner's Documents folder, plus `Able.vroid` (7,714,716 bytes) and `Alice.vroid` (8,332,016 bytes). Editable source filenames do not establish authorship, correspondence or use rights. No associated standalone textures, materials or character animation files were established for these supplied exports. The existing MToon and skin metadata refer to their own GLB data. The directory also contains unrelated FBX files with no confirmed household mapping.

The owner confirms Merlin is their VRoid Studio creation and the other three are unmodified stock VRoid Studio samples with renamed export files. The VRM JSON identifies their embedded names as shown below. Pixiv directs current Studio sample downloads to its VRoidPreset terms. Its older FAQ lists sample families with different terms. The exact local export names are verified from the files and their current Studio origin is owner-attested. The installed Studio catalog exposes `AvatarSample_A` through `AvatarSample_Z`. It does not link those keys to the three embedded names. Confirm the stock models in Studio before recording any letter mapping. [Current Studio sample instructions](https://vroid.pixiv.help/hc/en-us/articles/31627266179865) · [Sample licensing FAQ](https://vroid.pixiv.help/hc/en-us/articles/4402614652569-Do-VRoid-Studio-s-sample-models-come-with-conditions-of-use)

| Household identity and export | Embedded name and credit | Basis for the private preview | Remaining permissions or verification |
| --- | --- | --- | --- |
| Merlin · `Merlin.vrm` | Able · Vincent Baelin-Grayson | The owner identifies themself as the creator and authorizes the local project use. Keep the existing creator credit and Studio preset terms. | Preview locally as the owner-authorized creation. Rights for any separately added third-party content and any redistribution remain separate. |
| Marie · `Marie.vrm` | Student2 · pixiv VRoid Project | Owner-exported stock Studio sample. Covered samples can be imported and used as characters under the publisher terms. | Confirm the original `Student2` entry in the Studio sample selector during setup. Preserve pixiv attribution and the export metadata. |
| Arthur · `Arthur.vrm` | Student1 · pixiv VRoid Project | Owner-exported stock Studio sample under the same publisher process. | Confirm the original `Student1` entry. Preserve its proportions as a later-stage candidate and keep the seeded infant representation. |
| Alice · `Alice.vrm` | Alice · pixiv VRoid Project | Owner-exported stock Studio sample under the same publisher process. This name alone is insufficient to identify a model on a public Hub. | Confirm the `Alice` entry against Studio's sample picker. Preserve its full source and treat it as a later-stage candidate. |

Pixiv permits local character use of covered samples and permits making derivatives under its terms. It limits republishing the stock files for a fee, applying CC0 to them, and certain other uses. That upstream permission is independent of the narrower VRM export fields. Section 7 of the VRM Public License preserves uses independently authorized on another basis. Retargeting consumes the unchanged mesh as a separate visual instance. The project's private copy retains all authorship and embedded flags. All four original model files and later import products remain excluded from Git. [Pixiv sample conditions](https://vroid.pixiv.help/hc/en-us/articles/4402394424089-VRoidPreset-A-Z) · [Studio asset guidelines](https://vroid.com/en/studio/guidelines) · [VRM Public License](https://vrm.dev/en/licenses/1.0/)

UniVRM **v0.131.4** is the candidate for Unity 6000.3.19f1 and URP 17.3.0. The official MIT-licensed release documents Unity 2022.3 LTS or later. It uses the following pinned UPM paths. The automated tool review rejected the isolated installation command with `blocked by policy`, so these package manifests were verified against the publisher and the editor installation remains unverified. The shared project manifest was left intact. [UniVRM release and installation](https://github.com/vrm-c/UniVRM/releases/tag/v0.131.4)

```text
com.vrmc.gltf  https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4
com.vrmc.vrm   https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4
```

### Manual Unity Package Manager steps

1. Open a separate **Unity 6000.3.19f1 URP** project or a disposable private copy of Dreamer. Keep the owner's live project and its package files untouched for the initial check.
2. Open **Window → Package Management → Package Manager**. Use **+ → Install package from git URL**. Paste the `UniGLTF` URL above and wait for Git resolution and compilation. Repeat with the `VRM10` URL. Both resolved packages should display version `0.131.4`. Preserve any resolve or compiler error as an actual package result.
3. Under `Assets/DreamMachineLocalAvatars`, place a **copy** of `Merlin.vrm` and wait for Unity to import it. Create an empty preview scene, add a plane, directional light and camera, and drag Merlin's imported asset into the Hierarchy. Save the scene in the same local folder. Check that the Animator's avatar is human and valid and that the imported renderers are skinned. Check texture and MToon rendering in Play Mode before connecting the household.
4. Compare the displayed and measured foot sole, seeded height, knee and bind pose at a flat ground plane. Check yaw, eyes and head against a real attention target, expressions exposed by the import, movement-coupled gait, and seated and reach references. Keep the controller's skeleton authoritative. Validate each remaining sample individually in the same private workflow. [UniVRM import documentation](https://vrm.dev/en/univrm/import/univrm_import/)

## L.3 private Unity integration

The owner installed UniGLTF and VRM 1.0 at version 0.131.4. Both resolve to authored Git commit `efd20f62f1bf0ac3fcc34de1e497324c5197d146` in the shared manifest and lockfile. MCP 10.3.0 connected to the live Unity 6000.3.19f1 editor. The Editor reported a finished compilation. Its Unity AI subscription messages are independent of UniVRM.

The four source files are now under `Assets/DreamMachineLocalAvatars/`. All four SHA-256 hashes match the table above. Each file has a valid GLB 2 header, VRM 1.0 extension, three skins and 54 humanoid joints. Unity MCP independently reported valid imported humanoids and supported textured URP MToon for 14 Merlin, 18 Marie, 17 Arthur and 19 Alice material slots. Merlin rendered in an isolated preview scene. The owner's provenance and Pixiv's publisher-wide terms support private local character use. The Studio sample selector letter aliases remain unverified. Preserve the source metadata and credits. [Official Studio workflow](https://vroid.pixiv.help/hc/en-us/articles/31627266179865) · [Pixiv sample terms](https://vroid.pixiv.help/hc/en-us/articles/4402394424089-VRoidPreset-A-Z)

The Editor loads the four exact root filenames. A private player build can place needed resources at `Assets/DreamMachineLocalAvatars/Resources/DreamerLocalAvatars/<Name>.vrm`. Local screenshot images, preview scenes, importer settings files, source files and derivative assets stay in the ignored private directory. Use the desktop preview before packaging for a personal device. Repository clones have the procedural fallback. Preserve the same individual and anatomical ratios through growth. Arthur and Alice's supplied mature meshes become eligible when the body reaches the adult milestone. A validated infant or child mesh variant remains open.

## L.4 developmental presentation

Original procedural primitives add an infant sleeper and a toddler overall silhouette on Arthur and Alice's continuous seeded rig. The art uses the existing color palette and generates its geometry at runtime. No external mesh, texture or clothing asset is used. The four supplied originals and metadata remain unchanged. Private before/after, infant, toddler and sleep screenshots stay under `Assets/DreamMachineLocalAvatars/`. Authored child and adolescent meshes, and cross-stage visual identity against the adult assets, remain open for independent fitting and rights review. [Rendered validation](ACTIVE_PERCEPTION_L4.md#developmental-avatar-and-interaction-verification)
