using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace DreamMachine.UnityBridge
{
    public sealed partial class DreamMachineApp
    {
        const string PracticeTray = "household.physical-practice";
        NativeHandLab _practiceHand;
        Transform _practiceRoot;
        GameObject _sphereSupport, _ballSupport;
        readonly Transform[] _armParts = new Transform[3];
        Transform _armTarget;
        readonly Transform[] _practiceParts = new Transform[7];
        readonly Transform[] _practiceContacts = new Transform[2];
        readonly List<PracticeFrame> _practiceFrames = new List<PracticeFrame>(96);
        PracticeFrame[] _practiceReplay;
        int _practiceFrameIndex, _practiceRecordIndex = -1;
        float _practiceReplayAt;
        bool _practicePlaying;
        Text _practiceText;
        string _practiceStatus;

        void RegisterPracticeTray()
        {
            _workspace.Register(PracticeTray, "Physical development", new Vector2(236, 205),
                new TrayWorkspace.Layout { AnchorMin = new Vector2(.58f, .18f), AnchorMax = new Vector2(.98f, .93f) },
                BuildPracticeTray, ClosePracticeTray, RefreshPractice);
        }

        void ClosePracticeTray()
        {
            _practiceText = null;
            _practiceReplay = null;
            _practicePlaying = false;
            if (_ready && _practiceRoot != null) DrawPractice();
        }

        void OpenPracticeTray() => _workspace?.Toggle(PracticeTray);

        void BuildPracticeTray(RectTransform area)
        {
            _practiceText = NewLabel(area, "Physical evidence", 18);
            var rect = _practiceText.rectTransform;
            rect.anchorMin = new Vector2(.055f, .40f);
            rect.anchorMax = new Vector2(.945f, .96f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            AddPracticeRow(area, .30f, .39f, ("Sphere practice", () => SetBallTab(false)), ("Ball / switch learner", OpenBallTab),
                ("Articulated arm", () => { SetBallTab(true); BeginBallTransfer(HouseholdGripPolicy.Articulated); }),
                ("Coordinated placement", () => { SetBallTab(true); BeginBallTransfer(HouseholdGripPolicy.CoordinatedPlacement); }));
            _sphereActions = AddPracticeRow(area, .19f, .29f, ("Enter station", EnterPractice), ("Try grasp", BeginPracticeAttempt),
                ("Return to Living", LeavePractice));
            _ballActions = AddPracticeRow(area, .19f, .29f, ("Transfer ball", () => BeginBallTransfer(HouseholdGripPolicy.Fixed)),
                ("Visual coordination", () => BeginBallTransfer(HouseholdGripPolicy.VisualCoordination)),
                ("Light grip", () => BeginBallTransfer(HouseholdGripPolicy.Light)),
                ("Grip feedback", () => BeginBallTransfer(HouseholdGripPolicy.Feedback)), ("Inspect sighting", () => LookForBall(false)),
                ("Search nearby", () => LookForBall(true)));
            AddPracticeRow(area, .09f, .19f, ("Earlier attempt", () => SelectPracticeRecord(-1)),
                ("Later attempt", () => SelectPracticeRecord(1)), ("Live view", StopPracticeReplay));
            AddPracticeRow(area, 0f, .09f, ("Play / pause", () => _practicePlaying = _practiceReplay != null && !_practicePlaying),
                ("First frame", () => SeekPracticeFrame(0)), ("Next frame", () => SeekPracticeFrame(_practiceFrameIndex + 1)),
                ("Close", OpenPracticeTray));
            SetBallTab(_ballTab);
        }

        GameObject AddPracticeRow(RectTransform parent, float bottom, float top, params (string Label, Action Click)[] actions)
        {
            var row = new GameObject("Practice controls", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = new Vector2(.04f, bottom); row.anchorMax = new Vector2(.96f, top);
            row.offsetMin = row.offsetMax = Vector2.zero;
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                AddTimeButton(row, action.Label, i / (float)actions.Length, (i + 1) / (float)actions.Length, () => action.Click());
                ((RectTransform)row.GetChild(i)).anchorMax = new Vector2((i + 1) / (float)actions.Length, .97f);
            }
            return row.gameObject;
        }

        void EnterPractice()
        {
            if (_remote != null && _remote.Active || !_sim.EnterPhysicalPractice())
            {
                _practiceStatus = "Wait for Arthur's present activity to finish.";
                RefreshPractice();
                return;
            }
            _practiceStatus = "Objective ready. Start a live grasp, then use Simulation to resume or step.";
            StopPracticeReplay();
            DrawLiving(WorldSimulation.Dt);
            RefreshPractice();
        }

        void BeginPracticeAttempt()
        {
            if (_sim.PhysicalPractice?.Active != true) return;
            if (_sim.PhysicalPractice.AttemptRunning)
            {
                if (_practiceHand?.Terminal == true) FinalizePracticeAttempt();
                return;
            }
            if (_sim.PhysicalPractice.Records.Count >= HouseholdGripSession.MaximumAttempts)
            {
                _practiceStatus = "The archive has reached its practice limit.";
                RefreshPractice();
                return;
            }
            StopPracticeReplay();
            var session = _sim.PhysicalPractice;
            var hand = new NativeHandLab(session.BirthSeed, HouseholdGripSession.Mass, HouseholdGripSession.Radius,
                HouseholdGripSession.Friction, session.Cap);
            if (!_sim.StartGripAttempt()) { hand.Dispose(); return; }
            _practiceHand?.Dispose();
            _practiceHand = hand;
            _practiceFrames.Clear();
            _practiceFrames.Add(new PracticeFrame(hand));
            _practiceStatus = "Live intention | lift, hold and release the sphere";
            DrawPractice();
            RefreshPractice();
        }

        void LeavePractice()
        {
            if (!_sim.LeavePhysicalPractice())
            {
                _practiceStatus = "Finish the live attempt before returning.";
                RefreshPractice();
                return;
            }
            ClearPracticeNative();
            _practiceStatus = "Arthur returned to Living. His lived practice and retained grip stayed with him.";
            DrawLiving(WorldSimulation.Dt);
            RefreshPractice();
        }

        void ClearPracticeNative()
        {
            _practiceHand?.Dispose();
            _practiceHand = null;
            _practiceFrames.Clear();
            _practiceReplay = null;
            _practicePlaying = false;
            _practiceRecordIndex = -1;
            if (_practiceRoot != null) _practiceRoot.gameObject.SetActive(false);
        }

        void AdvanceHouseholdTick()
        {
            if (_sim.Home?.BallTransferRunning == true) { AdvanceBallTransfer(); return; }
            if (_practiceHand?.Terminal == true && _sim.PhysicalPractice.AttemptRunning)
            {
                FinalizePracticeAttempt();
                return;
            }
            _sim.Tick();
            if (_practiceHand == null || !_sim.PhysicalPractice.AttemptRunning) return;
            _practiceHand.Step();
            _practiceFrames.Add(new PracticeFrame(_practiceHand));
            if (_practiceHand.Terminal) FinalizePracticeAttempt();
        }

        void FinalizePracticeAttempt()
        {
            _governor.Paused = true;
            var hand = _practiceHand;
            string tape;
            try { tape = SavePracticeTape(); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                _practiceStatus = "Could not archive the completed grasp. Use Try grasp to retry the save.";
                Debug.LogError(error, this);
                RefreshPractice();
                return;
            }
            var position = hand.ObjectPosition;
            _sim.CompleteGripAttempt(hand.SlipTicks, hand.HeldTicks, hand.PeakClearance, hand.Phase == GripPhase.Complete,
                hand.Outcome, tape, position.x, position.y, position.z);
            _practiceStatus = hand.Outcome;
            RefreshPractice();
        }

        void StopPracticeReplay()
        {
            _practiceReplay = null;
            _practicePlaying = false;
            DrawPractice();
            RefreshPractice();
        }

        void SelectPracticeRecord(int delta)
        {
            if (_ballTab) { ReadBallReplay(delta); return; }
            var session = _sim.PhysicalPractice;
            if (session == null || session.Records.Count == 0 || session.AttemptRunning) return;
            int index = _practiceRecordIndex < 0 ? session.Records.Count - 1 :
                Mathf.Clamp(_practiceRecordIndex + delta, 0, session.Records.Count - 1);
            try
            {
                _practiceReplay = ReadPracticeTape(session.Records[index].Tape);
                _practiceRecordIndex = index;
                SeekPracticeFrame(0);
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                _practiceStatus = "The recorded frame file is unavailable.";
                RefreshPractice();
            }
        }

        void SeekPracticeFrame(int index)
        {
            if (_practiceReplay == null) return;
            _practiceFrameIndex = Mathf.Clamp(index, 0, _practiceReplay.Length - 1);
            DrawPractice();
            RefreshPractice();
        }

        void UpdatePracticeReplay()
        {
            if (!_practicePlaying || _practiceReplay == null || Time.unscaledTime < _practiceReplayAt) return;
            _practiceReplayAt = Time.unscaledTime + WorldSimulation.Dt;
            SeekPracticeFrame(_practiceFrameIndex + 1);
            if (_practiceFrameIndex == _practiceReplay.Length - 1) _practicePlaying = false;
        }

        void MakePracticeStation()
        {
            _practiceRoot = new GameObject("Arthur's supported grasp station").transform;
            string[] names = { "Support table", "Wrist", "Left digit", "Left pad", "Right digit", "Right pad", "Free sphere" };
            for (int i = 0; i < names.Length; i++)
                _practiceParts[i] = Piece(_practiceRoot, i == 6 ? PrimitiveType.Sphere : PrimitiveType.Cube, names[i],
                    Vector3.zero, Vector3.one, i == 0 ? "#456373" : i == 6 ? "#D78C47" : "#70B5AC");
            for (int i = 0; i < 2; i++)
                _practiceContacts[i] = Piece(_practiceRoot, PrimitiveType.Sphere, "Measured pad contact", Vector3.zero, Vector3.zero, "#F2D46C");
            _sphereSupport = new GameObject("Sphere fixture support");
            _sphereSupport.transform.SetParent(_practiceRoot, false);
            Piece(_sphereSupport.transform, PrimitiveType.Cube, "Visible wrist support", new Vector3(0, .005f, -.158f),
                new Vector3(.12f, .026f, .070f), "#456373");
            Piece(_sphereSupport.transform, PrimitiveType.Cube, "Table pedestal", new Vector3(0, -.15f, .02f),
                new Vector3(.20f, .25f, .20f), "#456373");
            Piece(_sphereSupport.transform, PrimitiveType.Cube, "Supported seat", new Vector3(0, -.112f, -.37f),
                new Vector3(.21f, .064f, .19f), "#624D43");
            _ballSupport = new GameObject("Ball wrist carriage support");
            _ballSupport.transform.SetParent(_practiceRoot, false);
            Piece(_ballSupport.transform, PrimitiveType.Cube, "Carriage rail", new Vector3(0, .038f, -.53f),
                new Vector3(1.05f, .076f, .18f), "#486E77");
            Piece(_ballSupport.transform, PrimitiveType.Cube, "Supported learner seat", new Vector3(-.135f, .037f, -.77f),
                new Vector3(.26f, .074f, .22f), "#624D43");
            _ballTargetMarker = Piece(_practiceRoot, PrimitiveType.Cylinder, "Assigned placement area", Vector3.zero,
                new Vector3(.21f, .001f, .24f), "#A2D7C6");
            _armParts[0] = Piece(_practiceRoot, PrimitiveType.Cube, "Supported torso collider", Vector3.zero, Vector3.one, "#516677");
            _armParts[1] = Piece(_practiceRoot, PrimitiveType.Capsule, "Upper arm collider", Vector3.zero, Vector3.one, "#71BFAB");
            _armParts[2] = Piece(_practiceRoot, PrimitiveType.Capsule, "Forearm collider", Vector3.zero, Vector3.one, "#81CABE");
            _armTarget = Piece(_practiceRoot, PrimitiveType.Sphere, "Requested wrist location", Vector3.zero, Vector3.one * .034f, "#EFC763");
        }

        void DrawPractice()
        {
            bool show = _practiceReplay != null || (_ballTab ? _practiceHand != null : _sim?.PhysicalPractice?.Active == true);
            if (!show) { if (_practiceRoot != null) _practiceRoot.gameObject.SetActive(false); return; }
            if (!_actors.TryGetValue(HouseholdGripSession.Arthur, out var actor)) return;
            if (_practiceRoot == null) MakePracticeStation();
            _practiceRoot.gameObject.SetActive(true);
            _sphereSupport.SetActive(!_ballTab);
            _ballSupport.SetActive(_ballTab);
            _ballTargetMarker.gameObject.SetActive(_ballTab);
            _practiceRoot.SetPositionAndRotation(new Vector3(HouseholdGripSession.StationX,
                Mathf.Max(.22f, actor.Body.Dimensions.Height * .48f), HouseholdGripSession.StationZ + .37f), Quaternion.identity);
            if (_ballTab)
            {
                var intent = _practiceReplay != null ? _sim.Home.BallTransfer(_practiceRecordIndex).Intent : _sim.Home.BallIntent.Value;
                _practiceRoot.position = new Vector3(intent.X, 0f, intent.Z);
                _ballTargetMarker.position = new Vector3(intent.TargetX, intent.TargetTop + .002f, intent.TargetZ);
            }
            var pose = _practiceReplay != null ? _practiceReplay[_practiceFrameIndex] :
                _practiceFrames.Count == 0 ? null : _practiceFrames[_practiceFrames.Count - 1];
            var arm = pose?.Arm;
            bool hasArm = arm != null;
            foreach (var part in _armParts) part.gameObject.SetActive(hasArm);
            _armTarget.gameObject.SetActive(hasArm);
            if (hasArm)
            {
                _ballSupport.SetActive(false);
                _armTarget.localPosition = arm.Target;
                for (int i = 0; i < _armParts.Length; i++)
                {
                    var part = _armParts[i];
                    part.localPosition = arm.Positions[i];
                    part.localRotation = arm.Rotations[i] * (i == 0 ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f));
                    var size = arm.Sizes[i];
                    part.localScale = i == 0 ? size : new Vector3(size.x, size.z * .5f, size.y);
                }
            }
            if (pose == null)
            {
                _practiceParts[0].localPosition = new Vector3(0, -.025f, .025f);
                _practiceParts[0].localRotation = _practiceParts[6].localRotation = Quaternion.identity;
                _practiceParts[0].localScale = new Vector3(.40f, .05f, .50f);
                for (int i = 1; i < 6; i++) _practiceParts[i].gameObject.SetActive(false);
                Vector3 at = new Vector3(0f, HouseholdGripSession.Radius, 0f);
                var records = _sim.PhysicalPractice.Records;
                if (records.Count > 0)
                {
                    var last = records[records.Count - 1];
                    at = new Vector3(last.X, last.Y, last.Z);
                }
                _practiceParts[6].localPosition = at;
                _practiceParts[6].localScale = Vector3.one * (2f * HouseholdGripSession.Radius);
                foreach (var marker in _practiceContacts) marker.gameObject.SetActive(false);
                return;
            }
            for (int i = 0; i < _practiceParts.Length; i++)
            {
                var part = _practiceParts[i];
                part.gameObject.SetActive(!_ballTab || (i != 0 && (i != 6 || _practiceReplay != null)));
                part.localPosition = pose.Positions[i]; part.localRotation = pose.Rotations[i]; part.localScale = pose.Sizes[i];
            }
            PaintPracticeContact(0, pose.LeftPoint, pose.LeftForce);
            PaintPracticeContact(1, pose.RightPoint, pose.RightForce);
        }

        void PaintPracticeContact(int side, Vector3 point, float force)
        {
            var marker = _practiceContacts[side];
            marker.gameObject.SetActive(force > GripCalibration.ContactForce);
            marker.localPosition = point;
            marker.localScale = Vector3.one * Mathf.Clamp(.005f + force * .002f, .005f, .020f);
        }

        void RefreshPractice()
        {
            if (_practiceText == null) return;
            if (_ballTab) { RefreshBall(); return; }
            var session = _sim.PhysicalPractice;
            if (session == null)
            {
                _practiceText.text = "Arthur | Supported physical practice\n" + (_practiceStatus ?? "Enter station to reserve Arthur's next activity.");
                return;
            }
            int total = session.Records.Count, passed = 0;
            foreach (var record in session.Records) if (record.Practice.Success) passed++;
            var pose = _practiceReplay != null ? _practiceReplay[_practiceFrameIndex] :
                _practiceFrames.Count > 0 ? _practiceFrames[_practiceFrames.Count - 1] : null;
            string state = _practiceReplay != null ? $"Recorded playback | attempt {_practiceRecordIndex + 1} | frame {_practiceFrameIndex}" :
                session.AttemptRunning ? "Live physical execution" : session.Active ? "Supported station | settled" : "Living | recorded library";
            _practiceText.text = $"Arthur | {state}\nLift, hold and release\n" +
                $"Retained cap {session.Cap:F2} N | {passed} successful / {total - passed} failed\n";
            if (pose != null)
                _practiceText.text += $"{pose.Phase} | pad force {pose.LeftForce:F2} / {pose.RightForce:F2} N | slip {(pose.Slip ? "observed" : "clear")}\n" +
                    $"Digit travel {pose.LeftTravel * 1000f:F1} / {pose.RightTravel * 1000f:F1} mm | target {pose.Target * 1000f:F1} mm\n" +
                    $"Effort {pose.LeftEffort:F2} / {pose.RightEffort:F2} N | clearance {pose.Clearance * 1000f:F1} mm\n";
            int evidence = _practiceReplay != null ? _practiceRecordIndex : total - 1;
            if (evidence >= 0)
            {
                var record = session.Records[evidence];
                _practiceText.text += $"Firsthand {record.Outcome} | ticks {record.Start}-{record.End}\n" +
                    $"Grip {record.Practice.Before:F2} to {record.Practice.After:F2} N | slip {record.Practice.SlipTicks} ticks\n{record.MemoryId}\n";
            }
            _practiceText.text += _practiceStatus;
        }

        static string PracticePath(string hash, bool ball) => Path.Combine(Application.persistentDataPath, "motor-trajectories", hash + (ball ? ".h5" : ".h2"));

        string SavePracticeTape(bool ball = false)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var first = _practiceFrames[0];
            int version = ball ? (first.Vision.HasValue ? 6 : first.Placement != null ? 5 : first.Arm != null ? 4 : 3) : 2;
            writer.Write(version);
            writer.Write(_practiceFrames.Count);
            foreach (var frame in _practiceFrames) frame.Write(writer, version);
            var bytes = stream.ToArray();
            string name = PracticeHash(bytes);
            string path = PracticePath(name, ball);
            if (!File.Exists(path))
                WorldSimulation.PublishCheckpoint(path, bytes, candidate => File.Exists(candidate) &&
                    PracticeHash(File.ReadAllBytes(candidate)) == name);
            return name;
        }

        static string PracticeHash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        static PracticeFrame[] ReadPracticeTape(string hash, bool ball = false)
        {
            var path = PracticePath(hash, ball);
            if (!File.Exists(path) || new FileInfo(path).Length > (ball ? 1250000 : 100000))
                throw new InvalidDataException("Missing or oversized grip trajectory.");
            byte[] bytes = File.ReadAllBytes(path);
            if (PracticeHash(bytes) != hash) throw new InvalidDataException("Grip trajectory checksum failed.");
            using var reader = new BinaryReader(new MemoryStream(bytes, false));
            int version = reader.ReadInt32();
            if (ball ? version < 3 || version > 6 : version != 1 && version != 2) throw new InvalidDataException("Unsupported grip trajectory.");
            int count = reader.ReadInt32();
            if (count <= 0 || count > (ball ? BallFrameLimit : 240)) throw new InvalidDataException("Invalid grip trajectory frame count.");
            var frames = new PracticeFrame[count];
            for (int i = 0; i < count; i++) frames[i] = new PracticeFrame(reader, version);
            return frames;
        }

        sealed class PracticeFrame
        {
            public readonly Vector3[] Positions = new Vector3[7], Sizes = new Vector3[7];
            public readonly Quaternion[] Rotations = new Quaternion[7];
            public readonly GripPhase Phase;
            public readonly float LeftForce, RightForce, LeftTravel, RightTravel, Clearance;
            public readonly float Target, LeftEffort, RightEffort;
            public readonly float Support, Cap;
            public readonly Vector3 LinearVelocity, AngularVelocity;
            public readonly int Tick, Corrections;
            public readonly PracticeArmFrame Arm;
            public readonly PracticePlacementFrame Placement;
            public readonly VisualPlacementReading? Vision;
            public readonly bool Slip;
            public readonly Vector3 LeftPoint, RightPoint;

            public PracticeFrame(NativeHandLab lab)
            {
                var f = lab.ReadFrame();
                Phase = f.Phase; Target = f.TravelTarget; LeftEffort = f.LeftClosingForce; RightEffort = f.RightClosingForce;
                LeftForce = f.Left.NormalForce; RightForce = f.Right.NormalForce;
                LeftTravel = f.LeftTravel; RightTravel = f.RightTravel; Clearance = f.Clearance; Slip = f.Left.Slip || f.Right.Slip;
                LeftPoint = f.Left.Point; RightPoint = f.Right.Point;
                Support = f.SupportForce; Cap = lab.EffectiveGripCap; Tick = f.Tick; Corrections = lab.GripCorrections;
                LinearVelocity = f.ObjectVelocity; AngularVelocity = lab.ObjectAngularVelocity;
                if (lab.Articulated) Arm = new PracticeArmFrame(lab);
                if (lab.Coordinated) Placement = new PracticePlacementFrame(lab);
                if (lab.VisualCoordination) Vision = lab.VisualReading;
                for (int i = 0; i < Positions.Length; i++)
                {
                    var part = lab.ReadPart(i);
                    Positions[i] = part.Position; Rotations[i] = part.Rotation; Sizes[i] = part.Size;
                }
            }

            public PracticeFrame(BinaryReader reader, int version)
            {
                Phase = (GripPhase)reader.ReadByte(); LeftForce = reader.ReadSingle(); RightForce = reader.ReadSingle();
                LeftTravel = reader.ReadSingle(); RightTravel = reader.ReadSingle(); Clearance = reader.ReadSingle();
                if (version >= 2) { Target = reader.ReadSingle(); LeftEffort = reader.ReadSingle(); RightEffort = reader.ReadSingle(); }
                Slip = reader.ReadBoolean(); LeftPoint = ReadVector(reader); RightPoint = ReadVector(reader);
                for (int i = 0; i < Positions.Length; i++)
                {
                    Positions[i] = ReadVector(reader); Sizes[i] = ReadVector(reader);
                    Rotations[i] = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }
                if (version >= 3)
                {
                    Support = reader.ReadSingle(); Cap = reader.ReadSingle(); Tick = reader.ReadInt32(); Corrections = reader.ReadInt32();
                    LinearVelocity = ReadVector(reader); AngularVelocity = ReadVector(reader);
                }
                if (version >= 4) Arm = new PracticeArmFrame(reader);
                if (version >= 5) Placement = new PracticePlacementFrame(reader);
                if (version >= 6)
                    Vision = new VisualPlacementReading(ReadEstimate(reader), ReadEstimate(reader), ReadEstimate(reader),
                        ReadVector(reader), ReadVector(reader), reader.ReadInt32(), reader.ReadSingle(), (BenchOcclusion)reader.ReadByte());
            }

            public void Write(BinaryWriter output, int version)
            {
                output.Write((byte)Phase); output.Write(LeftForce); output.Write(RightForce); output.Write(LeftTravel);
                output.Write(RightTravel); output.Write(Clearance);
                output.Write(Target); output.Write(LeftEffort); output.Write(RightEffort); output.Write(Slip);
                WriteVector(output, LeftPoint); WriteVector(output, RightPoint);
                for (int i = 0; i < Positions.Length; i++)
                {
                    WriteVector(output, Positions[i]); WriteVector(output, Sizes[i]);
                    var q = Rotations[i];
                    output.Write(q.x); output.Write(q.y); output.Write(q.z); output.Write(q.w);
                }
                if (version >= 3)
                {
                    output.Write(Support); output.Write(Cap); output.Write(Tick); output.Write(Corrections);
                    WriteVector(output, LinearVelocity); WriteVector(output, AngularVelocity);
                }
                if (version >= 4) Arm.Write(output);
                if (version >= 5) Placement.Write(output);
                if (version >= 6)
                {
                    var visual = Vision.Value;
                    WriteEstimate(output, visual.Hand); WriteEstimate(output, visual.Ball); WriteEstimate(output, visual.Surface);
                    WriteVector(output, visual.ProprioceptiveWrist); WriteVector(output, visual.Correction);
                    output.Write(visual.CorrectionTick); output.Write(visual.ExpectedContactSeconds); output.Write((byte)visual.Occlusion);
                }
            }

            static VisualPoseEstimate ReadEstimate(BinaryReader input)
            {
                var position = new System.Numerics.Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
                var velocity = new System.Numerics.Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
                return new VisualPoseEstimate(position, velocity, input.ReadInt32(), input.ReadInt32(), input.ReadInt32(),
                    input.ReadSingle(), input.ReadSingle(), (VisualEstimateSource)input.ReadByte());
            }

            static void WriteEstimate(BinaryWriter output, VisualPoseEstimate value)
            {
                output.Write(value.Position.X); output.Write(value.Position.Y); output.Write(value.Position.Z);
                output.Write(value.Velocity.X); output.Write(value.Velocity.Y); output.Write(value.Velocity.Z);
                output.Write(value.ObservationTick); output.Write(value.Age); output.Write(value.VisibleRays);
                output.Write(value.Confidence); output.Write(value.PredictionError); output.Write((byte)value.Source);
            }

            internal static Vector3 ReadVector(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            internal static void WriteVector(BinaryWriter output, Vector3 v) { output.Write(v.x); output.Write(v.y); output.Write(v.z); }
        }

        sealed class PracticePlacementFrame
        {
            public readonly float PalmSpeed, PalmSpin, EstimatedSpeed, Drift;
            public readonly int StableTicks;
            public readonly bool Tracked;
            public readonly PlacementObservation Observation;
            public PracticePlacementFrame(NativeHandLab lab)
            {
                PalmSpeed = lab.PalmVelocity.magnitude; PalmSpin = lab.PalmAngularVelocity.magnitude;
                EstimatedSpeed = lab.EstimatedBallSpeed; Drift = lab.ReleaseDrift;
                StableTicks = lab.StableSupportTicks; Tracked = lab.PlacementTracked; Observation = lab.PlacementObservation;
            }
            public PracticePlacementFrame(BinaryReader input)
            {
                PalmSpeed = input.ReadSingle(); PalmSpin = input.ReadSingle(); EstimatedSpeed = input.ReadSingle(); Drift = input.ReadSingle();
                StableTicks = input.ReadInt32(); Tracked = input.ReadBoolean(); Observation = (PlacementObservation)input.ReadByte();
            }
            public void Write(BinaryWriter output)
            {
                output.Write(PalmSpeed); output.Write(PalmSpin); output.Write(EstimatedSpeed); output.Write(Drift);
                output.Write(StableTicks); output.Write(Tracked); output.Write((byte)Observation);
            }
        }

        sealed class PracticeArmFrame
        {
            public readonly Vector3[] Positions = new Vector3[3], Sizes = new Vector3[3];
            public readonly Quaternion[] Rotations = new Quaternion[3];
            public readonly Vector3 Target, ProposedElbow, Joints;
            public readonly float ContactSeconds, Penetration;
            public readonly int Corrections;

            public PracticeArmFrame(NativeHandLab lab)
            {
                var a = lab.ReadArm();
                Target = a.Target; ProposedElbow = a.ProposedElbow; Joints = a.JointDegrees;
                ContactSeconds = a.TorsoSeconds; Penetration = a.TorsoDepth; Corrections = a.Corrections;
                for (int i = 0; i < 3; i++)
                {
                    var segment = lab.ReadArmPart(i);
                    Positions[i] = segment.Position; Sizes[i] = segment.Size; Rotations[i] = segment.Rotation;
                }
            }

            public PracticeArmFrame(BinaryReader input)
            {
                Target = PracticeFrame.ReadVector(input); ProposedElbow = PracticeFrame.ReadVector(input); Joints = PracticeFrame.ReadVector(input);
                ContactSeconds = input.ReadSingle(); Penetration = input.ReadSingle(); Corrections = input.ReadInt32();
                for (int i = 0; i < 3; i++)
                {
                    Positions[i] = PracticeFrame.ReadVector(input); Sizes[i] = PracticeFrame.ReadVector(input);
                    Rotations[i] = new Quaternion(input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
                }
            }

            public void Write(BinaryWriter output)
            {
                PracticeFrame.WriteVector(output, Target); PracticeFrame.WriteVector(output, ProposedElbow); PracticeFrame.WriteVector(output, Joints);
                output.Write(ContactSeconds); output.Write(Penetration); output.Write(Corrections);
                for (int i = 0; i < 3; i++)
                {
                    PracticeFrame.WriteVector(output, Positions[i]); PracticeFrame.WriteVector(output, Sizes[i]);
                    var q = Rotations[i];
                    output.Write(q.x); output.Write(q.y); output.Write(q.z); output.Write(q.w);
                }
            }
        }
    }
}
