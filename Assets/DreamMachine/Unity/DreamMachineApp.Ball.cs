using System;
using System.IO;
using DreamMachine.Body;
using DreamMachine.Sim;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public sealed partial class DreamMachineApp
    {
        const int BallFrameLimit = 1600;
        bool _ballTab;
        int _ballActor = HouseholdLife.SearchActor;
        Vector3 _ballOrigin;
        Transform _ballShelf, _ballTargetMarker;
        GameObject _sphereActions, _ballActions;

        void OpenBallTab()
        {
            if (_sim.PhysicalPractice?.AttemptRunning == true || _sim.Home.BallTransferRunning) return;
            if (_ballTab)
            {
                _ballActor = _ballActor == 2 ? 3 : 2;
                RefreshPractice();
                return;
            }
            SetBallTab(true);
        }

        void SetBallTab(bool ball)
        {
            if (_ballTab != ball && (_sim.PhysicalPractice?.AttemptRunning == true || _sim.Home.BallTransferRunning)) return;
            if (_ballTab != ball)
            {
                ClearPracticeNative();
                _ballTab = ball;
                _practiceStatus = null;
            }
            if (_sphereActions != null) _sphereActions.SetActive(!ball);
            if (_ballActions != null) _ballActions.SetActive(ball);
            RefreshPractice();
        }

        void BeginBallTransfer(HouseholdGripPolicy policy)
        {
            var home = _sim.Home;
            if (_remote != null && _remote.Active) return;
            if (home.BallTransferRunning)
            {
                if (_practiceHand?.HouseholdSettled == true) FinalizeBallTransfer();
                return;
            }
            if (!home.StartBallTransfer(_ballActor, policy))
            {
                _practiceStatus = "Requires a settled ball in reach and a learner free to enter the bench. Leave the sphere station first.";
                RefreshPractice();
                return;
            }
            var intent = home.BallIntent.Value;
            _practiceHand?.Dispose();
            _practiceReplay = null;
            _practicePlaying = false;
            _practiceRecordIndex = -1;
            _practiceFrames.Clear();
            _ballOrigin = new Vector3(intent.X, 0f, intent.Z);
            var before = intent.Before;
            var setup = new HouseholdHandSetup(_ballOrigin, BallPosition(before) - _ballOrigin, BallRotation(before),
                new Vector3(intent.TargetX, intent.TargetTop, intent.TargetZ) - _ballOrigin,
                new Vector3(home.BallBenchX + HouseholdLife.BallShelfOffset, HouseholdLife.BallShelfTop, home.BallBenchZ) - _ballOrigin,
                intent.TargetTop == 0f ? HouseholdLife.BallShelfTop : 0f);
            int seed = _sim.Find(home.PersonId(intent.Actor)).Spec.BirthSeed;
            _practiceHand = new NativeHandLab(seed, HouseholdLife.BallMass, HouseholdLife.BallRadius, HouseholdLife.BallFriction,
                policy == HouseholdGripPolicy.Fixed || policy == HouseholdGripPolicy.Articulated
                    ? GripAcquisition.ReferenceCap : GripAcquisition.InitialCap, placement: true,
                exploration: policy == HouseholdGripPolicy.Feedback ? GraspExploration.ForceOnly : GraspExploration.Off, household: setup,
                articulated: policy == HouseholdGripPolicy.Articulated ? new ArmBenchSetup(true) : null);
            _practiceFrames.Add(new PracticeFrame(_practiceHand));
            _practiceStatus = "Researcher-assigned transfer. Use the Simulation controls to run or step.";
            DrawLiving(WorldSimulation.Dt);
            RefreshPractice();
        }

        void AdvanceBallTransfer()
        {
            if (_practiceHand == null) throw new InvalidOperationException("The ball has no live native body.");
            if (_practiceHand.HouseholdSettled) { FinalizeBallTransfer(); return; }
            _sim.Tick();
            _practiceHand.Step();
            if (_practiceFrames.Count < BallFrameLimit - 1 || _practiceHand.Terminal)
                _practiceFrames.Add(new PracticeFrame(_practiceHand));
            if (_practiceHand.HouseholdSettled) FinalizeBallTransfer();
        }

        void FinalizeBallTransfer()
        {
            _governor.Paused = true;
            try
            {
                var hand = _practiceHand;
                string tape = SavePracticeTape(ball: true);
                var p = hand.ObjectPosition + _ballOrigin;
                var q = hand.ObjectRotation.normalized;
                var actual = new HouseholdBallPose(p.x, p.y, p.z, q.x, q.y, q.z, q.w);
                _sim.Home.CompleteBallTransfer(actual, hand.Phase == GripPhase.Complete,
                    hand.GripCorrections + hand.ReadArm().Corrections, hand.Outcome, tape);
                _practiceStatus = hand.Outcome;
                hand.Dispose();
                _practiceHand = null;
                var last = _practiceFrames[^1];
                _practiceFrames.Clear();
                _practiceFrames.Add(last);
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                _practiceStatus = "The live result is settled. Select Transfer ball to retry archiving.";
                Debug.LogError(error, this);
            }
            RefreshPractice();
        }

        void LookForBall(bool searchArea)
        {
            bool seen = _sim.Home.InspectBall(_ballActor, searchArea);
            _practiceStatus = seen ? "Ball seen. Sighting recorded." : "No sighting from this inspection.";
            RefreshPractice();
        }

        void ReadBallReplay(int delta)
        {
            var home = _sim.Home;
            if (home.BallTransferRunning || home.BallTransferCount == 0) return;
            int index = _practiceRecordIndex < 0 ? home.BallTransferCount - 1 :
                Mathf.Clamp(_practiceRecordIndex + delta, 0, home.BallTransferCount - 1);
            try
            {
                var record = home.BallTransfer(index);
                _practiceReplay = ReadPracticeTape(record.Tape, ball: true);
                _practiceRecordIndex = index;
                SeekPracticeFrame(0);
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                _practiceStatus = "The recorded ball trajectory is unavailable.";
                RefreshPractice();
            }
        }

        void DrawBall()
        {
            var home = _sim.Home;
            if (_ballShelf == null) return;
            _ballShelf.gameObject.SetActive(home.BallManaged);
            if (!home.BallManaged)
            {
                _livingItems[HouseholdLife.BallItem].rotation = Quaternion.identity;
                return;
            }
            _ballShelf.position = new Vector3(home.BallBenchX + HouseholdLife.BallShelfOffset, HouseholdLife.BallShelfTop * .5f, home.BallBenchZ);
            var ball = home.ReadBallPose();
            if (home.BallTransferRunning && _practiceHand != null)
                _livingItems[HouseholdLife.BallItem].SetPositionAndRotation(_ballOrigin + _practiceHand.ObjectPosition,
                    _practiceHand.ObjectRotation);
            else
                _livingItems[HouseholdLife.BallItem].SetPositionAndRotation(BallPosition(ball), BallRotation(ball));
        }

        static Vector3 BallPosition(HouseholdBallPose p) => new Vector3(p.X, p.Y, p.Z);
        static Quaternion BallRotation(HouseholdBallPose p) => new Quaternion(p.Qx, p.Qy, p.Qz, p.Qw);

        void RefreshBall()
        {
            var home = _sim.Home;
            var belief = home.ReadBallBelief(_ballActor);
            var settled = home.ReadBallPose();
            var ball = home.BallIntent;
            int index = _practiceReplay != null ? _practiceRecordIndex : -1;
            if (!ball.HasValue && _practiceReplay == null)
                for (int n = home.BallTransferCount - 1; n >= 0; n--)
                    if (home.BallTransfer(n).Intent.Actor == _ballActor) { index = n; break; }
            BallTransferRecord? record = index >= 0 ? home.BallTransfer(index) : null;
            string name = LivingName(_sim.Population.Find(home.PersonId(_ballActor)));
            string beliefText = belief.LastSeen < 0 ? "no sighting" :
                $"{belief.X:F2}, {belief.Z:F2} at tick {belief.LastSeen}{(belief.Uncertain ? " | uncertain" : "")}";
            string actual = $"Settled ball  {settled.X:F3}, {settled.Y:F3}, {settled.Z:F3}";
            if (ball.HasValue && _practiceHand != null)
            {
                var current = _ballOrigin + _practiceHand.ObjectPosition;
                actual = $"Live ball  {current.x:F3}, {current.y:F3}, {current.z:F3}";
            }
            _practiceText.text = $"Household ball  |  {name}\n{actual}\nLast sighting  {beliefText}\n";
            if (_practiceReplay != null)
            {
                string author = LivingName(_sim.Population.Find(home.PersonId(record.Value.Intent.Actor)));
                _practiceText.text += $"Recorded preview  |  {author}  |  frame {_practiceFrameIndex}\n";
            }
            var intention = ball ?? record?.Intent;
            if (intention.HasValue) _practiceText.text += $"Assigned destination  {intention.Value.TargetX:F2}, {intention.Value.TargetZ:F2}  |  {intention.Value.Policy}\n";
            var pose = _practiceReplay != null ? _practiceReplay[_practiceFrameIndex] : _practiceFrames.Count > 0 ? _practiceFrames[^1] : null;
            if (pose != null)
                _practiceText.text += $"{pose.Phase} at tick {pose.Tick}  |  pads {pose.LeftForce:F2} / {pose.RightForce:F2} N\n" +
                    $"Support {pose.Support:F2} N  |  cap {pose.Cap:F2} N  |  corrections {pose.Corrections}\n";
            if (pose?.Arm != null)
                _practiceText.text += $"Elbow {pose.Arm.Joints.y:F0}°  |  wrist error {Vector3.Distance(pose.Arm.Target, pose.Positions[1]) * 1000f:F0} mm\n" +
                    $"Torso contact {pose.Arm.ContactSeconds:F2} s  |  depth {pose.Arm.Penetration * 1000f:F1} mm\n";
            if (record.HasValue) _practiceText.text += $"{record.Value.Outcome}  |  {record.Value.MemoryId}\n";
            _practiceText.text += _practiceStatus ?? "Choose a learner, transfer the ball or inspect a sighting.";
        }
    }
}
