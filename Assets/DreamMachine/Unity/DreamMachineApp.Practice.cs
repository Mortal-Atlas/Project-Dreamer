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
            rect.anchorMin = new Vector2(.055f, .31f);
            rect.anchorMax = new Vector2(.945f, .96f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            AddPracticeRow(area, .19f, .29f, ("Enter station", EnterPractice), ("Try grasp", BeginPracticeAttempt),
                ("Return to Living", LeavePractice));
            AddPracticeRow(area, .09f, .19f, ("Earlier attempt", () => SelectPracticeRecord(-1)),
                ("Later attempt", () => SelectPracticeRecord(1)), ("Live view", StopPracticeReplay));
            AddPracticeRow(area, 0f, .09f, ("Play / pause", () => _practicePlaying = _practiceReplay != null && !_practicePlaying),
                ("First frame", () => SeekPracticeFrame(0)), ("Next frame", () => SeekPracticeFrame(_practiceFrameIndex + 1)),
                ("Close", OpenPracticeTray));
        }

        void AddPracticeRow(RectTransform parent, float bottom, float top, params (string Label, Action Click)[] actions)
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
            Piece(_practiceRoot, PrimitiveType.Cube, "Visible wrist support", new Vector3(0, .005f, -.158f),
                new Vector3(.12f, .026f, .070f), "#456373");
            Piece(_practiceRoot, PrimitiveType.Cube, "Table pedestal", new Vector3(0, -.15f, .02f),
                new Vector3(.20f, .25f, .20f), "#456373");
            Piece(_practiceRoot, PrimitiveType.Cube, "Supported seat", new Vector3(0, -.112f, -.37f),
                new Vector3(.21f, .064f, .19f), "#624D43");
        }

        void DrawPractice()
        {
            bool show = _sim?.PhysicalPractice?.Active == true || _practiceReplay != null;
            if (!show) { if (_practiceRoot != null) _practiceRoot.gameObject.SetActive(false); return; }
            if (!_actors.TryGetValue(HouseholdGripSession.Arthur, out var actor)) return;
            if (_practiceRoot == null) MakePracticeStation();
            _practiceRoot.gameObject.SetActive(true);
            _practiceRoot.SetPositionAndRotation(new Vector3(HouseholdGripSession.StationX,
                Mathf.Max(.22f, actor.Body.Dimensions.Height * .48f), HouseholdGripSession.StationZ + .37f), Quaternion.identity);
            var pose = _practiceReplay != null ? _practiceReplay[_practiceFrameIndex] :
                _practiceFrames.Count == 0 ? null : _practiceFrames[_practiceFrames.Count - 1];
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
                part.gameObject.SetActive(true);
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

        static string PracticePath(string hash) => Path.Combine(Application.persistentDataPath, "motor-trajectories", hash + ".h2");

        string SavePracticeTape()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(2);
            writer.Write(_practiceFrames.Count);
            foreach (var frame in _practiceFrames) frame.Write(writer);
            var bytes = stream.ToArray();
            string name = PracticeHash(bytes);
            string path = PracticePath(name);
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

        static PracticeFrame[] ReadPracticeTape(string hash)
        {
            var path = PracticePath(hash);
            if (!File.Exists(path) || new FileInfo(path).Length > 100000)
                throw new InvalidDataException("Missing or oversized grip trajectory.");
            byte[] bytes = File.ReadAllBytes(path);
            if (PracticeHash(bytes) != hash) throw new InvalidDataException("Grip trajectory checksum failed.");
            using var reader = new BinaryReader(new MemoryStream(bytes, false));
            int version = reader.ReadInt32();
            if (version != 1 && version != 2) throw new InvalidDataException("Unsupported grip trajectory.");
            int count = reader.ReadInt32();
            if (count <= 0 || count > 240) throw new InvalidDataException("Invalid grip trajectory frame count.");
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
            public readonly bool Slip;
            public readonly Vector3 LeftPoint, RightPoint;

            public PracticeFrame(NativeHandLab lab)
            {
                var f = lab.ReadFrame();
                Phase = f.Phase; Target = f.TravelTarget; LeftEffort = f.LeftClosingForce; RightEffort = f.RightClosingForce;
                LeftForce = f.Left.NormalForce; RightForce = f.Right.NormalForce;
                LeftTravel = f.LeftTravel; RightTravel = f.RightTravel; Clearance = f.Clearance; Slip = f.Left.Slip || f.Right.Slip;
                LeftPoint = f.Left.Point; RightPoint = f.Right.Point;
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
            }

            public void Write(BinaryWriter output)
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
            }

            static Vector3 ReadVector(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            static void WriteVector(BinaryWriter output, Vector3 v) { output.Write(v.x); output.Write(v.y); output.Write(v.z); }
        }
    }
}
