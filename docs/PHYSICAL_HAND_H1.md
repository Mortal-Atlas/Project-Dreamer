# H1 supported physical hand

## Verified fixture

The reduced engineering fixture uses a constrained wrist carriage, a hinged wrist and two independently driven prismatic digits. Native PhysX owns all contact, material friction, the rigid sphere and the support table. Both finger anchors use seeded hand dimensions. The user's household identities and saves are outside this isolated test. The object is a free Rigidbody under gravity, and the test checks its parentage, constraints and joints. Fixture motors apply bounded carriage forces, wrist torque and guide forces. The collision registry excludes the connected palm–digit surfaces while retaining opposing digits and object contact. [Native fixture](../Assets/DreamMachine/Unity/NativeHandLab.cs) [Pair registry](../Assets/DreamMachine/Core/Body/AnatomyContacts.cs)

Two preliminary rotary-finger pilots are archived locally. The first lacked reported contact impulses until contact provision was enabled on the object collider. The offset pad drove the sphere out of reach. A second counter-hinged pad design reported valid table support, but its face and primary hinge fought under normal effort. None of the second pilot's five settings held the sphere. The guided digits keep contact faces parallel and remove that rotational coupling. A later anatomical hand needs validated angular joints and thumb opposition without that coupling.

### Prespecified gates and tactile calibration

Use ten 5 ms substeps per shared 50 ms tick. A held object must be at least **35 mm** clear of the table, receive at least **0.05 N** tick-mean support from each opposing pad, and satisfy those conditions for **12 consecutive ticks** (0.60 s) without unintended supporting parts. The release passes after both pads unload and there are five successive ticks of settled table support of at least half the object's weight, clearance under 4 mm, and speed under 0.05 m/s. The full attempt additionally caps sampled overlap at 4 mm and guide travel at 39 mm, including a 2 mm numerical allowance on the 37 mm constraint. A weak grip can trigger a lift attempt but receives no success without actual support and the full physical gates.

Pad contact points are normal-impulse-weighted. Normal and tangential impulse use the reported native contact normal and vector, then divide summed magnitude by the 50 ms window for average force. Contact duration counts occupied 5 ms samples. Slip requires relative tangential motion above 25 mm/s in three substeps and nontrivial normal force. Slip evidence is a calibrated engineering cue for these pads, while detailed contact-patch geometry and material shear history remain unmeasured. The nominal pad face is 26 by 25 mm. Its effective contact area against the sphere is unknown, so the interface records force and makes no pressure estimate. The static table reference reproduced known sphere weight within 0.00040 N across the measured masses. The table check validates this projection and integration at rest. A separate instrumented moving-pad force or effective-area calibration remains open. [Shared calibration](../Assets/DreamMachine/Core/Body/GripCalibration.cs) [Calibration regression tests](../Assets/DreamMachine/Tests/EditMode/GripCalibrationTests.cs)

## Native results

| Comparison | Outcome | Peak clearance | Hold | Slip ticks |
| --- | --- | ---: | ---: | ---: |
| Nominal, 3.2 N cap | Lift, hold and physical release | 57.53 mm | 0.60 s | 0 |
| Insufficient grip, 0.18 N cap | Failed hold and remained on support | 0.00 mm | 0 s | 6 |
| Higher cap, 6.5 N | Passed with small extra measured work | 58.53 mm | 0.60 s | 0 |
| Different closing timing, geometry and lower-friction development cases | Both passed | 56.43–58.24 mm | 0.60 s | 0 |
| Four fresh fixed-policy configurations | All passed | 42.42–59.80 mm | 0.60 s each | 1 total |

The held-out fixed policy changed mass, radius and friction. Sphere yaw does not change the sphere's physical contact geometry. No non-spherical grasp was tested. The higher bound on closing effort increased nominal measured work from 1.737 to 1.768 J. Peak contact force rose from 3.20 to 3.34 N, demonstrating that a higher command cap need not be fully expressed. Work includes measured carriage and digit work in this implementation. Native accumulated overlap stayed below 0.12 mm across the final development and held-out configurations. The capture sequence shows approach, actual contact, a visible intervening floor gap during hold, and release under gravity. [Development results](evidence/phase-h1-development-summary.csv) [Held-out results](evidence/phase-h1-heldout-summary.csv) [Native events](evidence/phase-h1-native-events.csv)

## Acquisition, retention and transfer

The isolated learner-bound study used Arthur's configured identity `son` and birth seed 303 at the fixture's fixed age-3 anatomy calibration. A predeclared rule added 0.28 N to the cap only after a failed attempt with at least two measured slip ticks. The rule observed the actual native outcome, hold and slip measurements. Mass and friction were experiment factors withheld from that update. Cap increases were bounded by the 3.2 N engineered reference.

Across eight alternating familiar trials, the unchanged 0.18 N start passed 0/8 and the 3.2 N engineered reference passed 8/8. The adaptive policy passed 5/8, first succeeded on trial 3, failed again on the other training object on trial 4, and finished with four consecutive successes at 1.02 N. This is feedback-driven acquisition of one parameter in a predefined task. It did not exceed the stronger engineered reference's reliability.

A second Unity editor process loaded the 203-byte versioned skill with eight physical practice records and reconstructed the same 1.02 N cap from the evidence. It compared a frozen retained policy, an independent reset of the adaptive policy for each test, and the unchanged engineered reference. Each scored object was a fresh native scene. No changes were made to the retained cap during testing.

| Evaluation | Frozen retained | Reset adaptive at 0.18 N | Engineered 3.2 N |
| --- | ---: | ---: | ---: |
| Familiar after restart | 2/2 | 0/2 | 2/2 |
| Fresh object configurations | 2/4 | 0/4 | 4/4 |

The retained policy failed on the 145 g low-friction sphere and the 104 g medium-friction sphere. It succeeded on the lighter and narrower fresh configurations. The results establish partial transfer for one calibration and one simple object family. The skill artifact stores controller version, identity, calibration, the performed practice records and learned cap. Detailed trajectories and evaluation rows remain separate evidence files. The restart reconstructs a compatible policy at a fresh attempt boundary. [Acquisition source](../Assets/DreamMachine/Core/Body/GripAcquisition.cs) [Training observations](evidence/phase-h1-acquisition.csv) [Restart and transfer](evidence/phase-h1-retention-transfer.csv)

## Browser and provenance

Use **Dream Machine > Start Supported Hand Laboratory** in Unity. The Motor attempts tray starts paused. Run or single-step a standard grip and a low grip, then use Earlier attempt, Later attempt and the time slider to replay recorded joint and object poses with contact markers and force. The four latest attempts are bounded to 240 ticks each in the current tray. The Unity control test recorded one success and one failure, selected a recorded hold at tick 50, and checked that replay left the live fixture tick unchanged. In the physical capture, a matched unobserved fixture reproduced all measured palm and object positions with zero measured delta across the 71-tick attempt. [Browser](../Assets/DreamMachine/Unity/HandLabView.cs) [Native browser smoke](../tools/unity/HandBrowserSmoke.cs)

The implemented grasp has an authored stage sequence. Approach, close, detect support, lift, hold, lower and release events provide selectable engineering components for future Babel composition. The physical summaries and timed frames supply evidence for Knowledge and Lived Memory links. Separately typed training, reset and frozen-retention records use the same calibrated skill ID. The prototype tray displays session-local engineered attempts. Persistent household library indexing, importing the training archive into the tray, simulated alternatives, demonstrations and learner-bound practice remain future work. These limits preserve the distinction between authored plans and lived outcomes.

## Measured cost

Across the final development and held-out fixed-policy runs, native integration and checks consumed roughly 0.50-0.66 ms per 50 ms tick. The measured mean for hand commands was 27.5-31.8 microseconds across the two cohorts, native contact handling 8.2-9.4 microseconds, and frame capture 1.6-1.9 microseconds. With the playback tray visible, the DX12 camera CPU submit mean was 1.197 ms and a saved-frame UI refresh averaged 0.0096 ms. The marshalled frame comparison measured 360 bytes per frame. Four 240-tick records plus a transient current record put raw frame payload on the order of hundreds of kilobytes. Managed backing buffers, renderer allocations and GPU time are outside that estimate. The acquisition update runs once per attempt. After its first call, the timed samples were 0.4-10.4 microseconds.

The eight-process paired Living benchmark used the previous corrected methodology and the rebuilt L.4 commit. Median four-person Living throughput measured 45,760 ticks/s for C.1.4/H1 and 46,385 for L.4. The ranges overlap and the household path does not invoke either native research controller. One twelve-person kernel run reached 27,648 ticks/s. Earlier concurrently loaded samples were treated as perturbed timing. Android feasibility remains open. [Paired measurements](evidence/phase-h1-living-benchmark.csv)

## Reproduction and scope

Release .NET passed 112 tests and Unity Edit Mode passed 113 tests.

Run `NativeHandStudy.Development`, `NativeHandStudy.HeldOut` and `HandBrowserSmoke.Run` separately in the isolated DX12 editor project. Raw traces, pilot failures and PNG captures remain in the scratch product's persistent data directory. Compact CSVs are in GitHub. The native shape and measured contact experiment is a research fixture, with no change to Living control. The next household milestone is a supported practice boundary that binds Arthur to the existing clock, the calibrated hand and typed durable records, followed by loaded-boundary validation. [Supported-hand plan](ARTICULATED_HAND_FIXTURE_PLAN.md) [Browser harness](../tools/unity/HandBrowserSmoke.cs)
