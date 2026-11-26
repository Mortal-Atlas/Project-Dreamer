using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DreamMachine.Body;
using DreamMachine.UnityBridge;
using DreamMachine.Util;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class NativeElbowSmoke
{
    const string Key = "NativeElbowSmoke";
    static int waitFrames;
    static NativeElbowLab displayed;
    static byte[] paused;

    static NativeElbowSmoke()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += Changed;
    }

    public static void Run()
    {
        if (Application.productName != "Dreamer Audit Isolated Smoke") throw new Exception("Scratch project required.");
        string path = Path.Combine(Application.persistentDataPath, "elbow-native.checkpoint");
        File.Delete(path);
        File.Delete(path + ".previous");
        PlayerPrefs.DeleteKey("DreamMachine.Workspace.v1.embodiment.elbow");
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Native Elbow Laboratory").AddComponent<ElbowLabView>().UseNativePhysics = true;
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(0);
        }
        if (change == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += Verify;
    }

    static NativeElbowLab Displayed()
    {
        var view = Object.FindAnyObjectByType<ElbowLabView>();
        return (NativeElbowLab)typeof(ElbowLabView).GetField("_lab",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
    }

    static void Click(string name) => GameObject.Find("Elbow laboratory")
        .GetComponentsInChildren<Button>().Single(button => button.name == name).onClick.Invoke();

    static void Verify()
    {
        try
        {
            displayed = Displayed();
            if (displayed == null) return;
            if (waitFrames++ == 0) paused = displayed.CaptureCheckpoint();
            if (waitFrames <= 8) return;
            if (waitFrames == 9)
            {
                if (!paused.SequenceEqual(displayed.CaptureCheckpoint()))
                    throw new Exception("Native local physics advanced while paused.");
                Click("Step");
                return;
            }
            if (displayed.Tick == 0) return;
            if (displayed.Tick != 1 || displayed.SubstepCount != 10)
                throw new Exception("Single-step should advance exactly one tick.");

            string directory = Application.persistentDataPath;
            using (var csv = File.CreateText(Path.Combine(directory, "native-elbow-traces.csv")))
            {
                csv.Write("trial,backend,tick,sim_s,target_rad,angle_rad,velocity_rad_s,effort_nm,resistance_nm,normal_ns,x_m,y_m,vx_m_s,vy_m_s,spin_rad_s,point_x_m,point_y_m,normal_x,normal_y");
                for (int r = 0; r < ElbowLab.ReceptorCount; r++)
                    csv.Write($",{ElbowLab.ReceptorId(r)}_force_n,{ElbowLab.ReceptorId(r)}_duration_s,{ElbowLab.ReceptorId(r)}_slip_m_s");
                csv.WriteLine();
                Compare(csv, "free", 1, 1, 0.9, 0.12);
                Compare(csv, "standard", 0.28, 0.20, 0.9, 0);
                Compare(csv, "opposite", 0.28, -0.20, -0.9, 0);
                Compare(csv, "heavy", 0.28, 0.20, 0.9, 0, 0.9);
            }

            using (var expected = new NativeElbowLab(ElbowLab.Settings.Standard, "son"))
            {
                expected.Step();
                if (!expected.CaptureCheckpoint().SequenceEqual(displayed.CaptureCheckpoint()))
                    throw new Exception("Visible tray changed first native tick.");
                Click("Save");
                displayed.Step();
                Click("Restore");
                displayed = Displayed();
                if (!expected.CaptureCheckpoint().SequenceEqual(displayed.CaptureCheckpoint()))
                    throw new Exception("Native tray did not restore the recorded boundary.");
                Click("Close");
                if (GameObject.Find("Elbow laboratory") != null)
                    throw new Exception("Native tray did not release.");
            }

            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "native-elbow.png"));
            Debug.Log("NATIVE_SMOKE native free/contact/localization, pause, step, observer and disk restore checked");
            EditorApplication.update -= Verify;
            EditorApplication.ExitPlaymode();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static void Compare(StreamWriter csv, string name, double x, double y, double target, double initial, double mass = 0.25)
    {
        var spec = new ElbowLab.Settings(-1.1, 1.3, initial, mass, 0.4, x, y, 80);
        using var native = new NativeElbowLab(spec, "son") { ProfileEnabled = true };
        var planar = new ElbowLab(spec, "son");
        native.RequestTarget(target);
        planar.RequestTarget(target);
        double maxAngle = 0, maxVelocity = 0, maxEffort = 0, nativeImpulse = 0, planarImpulse = 0;
        double nativePeak = 0, planarPeak = 0, nativeSlip = 0, planarSlip = 0;
        int nativeFirst = 0, planarFirst = 0, nativeContactTicks = 0, planarContactTicks = 0;
        int activeRegion = -1;
        byte[][] snapshots = new byte[5][];
        int[] saveTicks = { 0, 1, 3, 4, 18 };
        snapshots[0] = native.CaptureCheckpoint();
        var states = new (double Angle, double X, double Y, double Velocity, double Impulse)[spec.Duration + 1];
        long allocated = 0;
        while (!native.Finished)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var a = native.Step();
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            var b = planar.Step();
            double stepImpulse = a.NormalImpulse;
            if (stepImpulse > 0)
            {
                if (nativeFirst == 0)
                {
                    nativeFirst = a.Tick;
                    activeRegion = Enumerable.Range(0, ElbowLab.ReceptorCount)
                        .OrderByDescending(r => native.ReadRegion(r).Force).First();
                }
                nativeContactTicks++;
            }
            if (b.NormalImpulse > 0)
            {
                if (planarFirst == 0) planarFirst = b.Tick;
                planarContactTicks++;
            }
            nativeImpulse += stepImpulse;
            planarImpulse += b.NormalImpulse;
            nativePeak = Math.Max(nativePeak, stepImpulse / 0.05);
            planarPeak = Math.Max(planarPeak, b.NormalImpulse / 0.05);
            for (int r = 0; r < ElbowLab.ReceptorCount; r++)
            {
                nativeSlip = Math.Max(nativeSlip, native.ReadRegion(r).SlipSpeed);
                planarSlip = Math.Max(planarSlip, planar.ReadRegion(r).SlipSpeed);
            }
            maxAngle = Math.Max(maxAngle, Math.Abs(native.Angle - planar.Angle));
            maxVelocity = Math.Max(maxVelocity, Math.Abs(a.Feedback.Velocity - b.Feedback.Velocity));
            maxEffort = Math.Max(maxEffort, Math.Abs(a.Feedback.Effort - b.Feedback.Effort));
            Write(csv, name, native, native.LastContactPoint, native.LastContactNormal);
            Write(csv, name, planar, Vector3.zero, Vector3.zero);
            states[native.Tick] = (native.Angle, native.X, native.Y, a.Feedback.Velocity, a.NormalImpulse);
            int slot = Array.IndexOf(saveTicks, native.Tick);
            if (slot >= 0) snapshots[slot] = native.CaptureCheckpoint();
        }
        if (name == "free" && (nativeImpulse != 0 || Math.Abs(native.Angle - target) > 0.02))
            throw new Exception("Native free movement produced false contact or failed to track the target.");
        if (name != "free" && (nativeFirst <= 0 || Math.Abs(native.Y - y) < 0.1 ||
            (target > 0 ? activeRegion < 3 : activeRegion > 1)))
            throw new Exception("Native contact did not move the puck or localize to the correct edge.");
        Debug.Log(FormattableString.Invariant(
            $"NATIVE_COMPARE {name} first={nativeFirst}/{planarFirst} contactTicks={nativeContactTicks}/{planarContactTicks} region={activeRegion} impulse={nativeImpulse:F5}/{planarImpulse:F5} peakN={nativePeak:F3}/{planarPeak:F3} slip={nativeSlip:F4}/{planarSlip:F4} angleMax={maxAngle:F5} velocityMax={maxVelocity:F5} effortMax={maxEffort:F5} puck=({native.X:F4},{native.Y:F4})/({planar.X:F4},{planar.Y:F4}) finalSpeed={Math.Sqrt(native.VX * native.VX + native.VY * native.VY):F6}/{Math.Sqrt(planar.VX * planar.VX + planar.VY * planar.VY):F6}"));
        double toMicroseconds = 1e6 / Stopwatch.Frequency / spec.Duration;
        Debug.Log(FormattableString.Invariant(
            $"NATIVE_COST {name} motor_us={native.MotorTimestampTicks * toMicroseconds:F3} simulate_us={native.PhysicsTimestampTicks * toMicroseconds:F3} contact_in_simulate_us={native.ContactTimestampTicks * toMicroseconds:F3} sensor_us={native.SensorTimestampTicks * toMicroseconds:F3} bytes_per_tick={allocated / (double)spec.Duration:F1}"));
        if (name != "standard") return;
        for (int i = 0; i < saveTicks.Length; i++)
        {
            using var restored = NativeElbowLab.RestoreCheckpoint(snapshots[i]);
            byte[] roundTrip = restored.CaptureCheckpoint();
            if (!snapshots[i].SequenceEqual(roundTrip))
            {
                object before = MiniJson.Parse(Encoding.UTF8.GetString(snapshots[i], 32, snapshots[i].Length - 32));
                object after = MiniJson.Parse(Encoding.UTF8.GetString(roundTrip, 32, roundTrip.Length - 32));
                CompareSnapshotField("checkpoint", before, after);
            }
            double angleError = 0, positionError = 0, velocityError = 0, impulseError = 0;
            for (int j = 0; j < 8; j++)
            {
                restored.Step();
                var expected = states[restored.Tick];
                angleError = Math.Max(angleError, Math.Abs(expected.Angle - restored.Angle));
                positionError = Math.Max(positionError, Math.Sqrt(Math.Pow(expected.X - restored.X, 2) + Math.Pow(expected.Y - restored.Y, 2)));
                velocityError = Math.Max(velocityError, Math.Abs(expected.Velocity - restored.Latest.Feedback.Velocity));
                impulseError = Math.Max(impulseError, Math.Abs(expected.Impulse - restored.Latest.NormalImpulse));
            }
            Debug.Log(FormattableString.Invariant(
                $"NATIVE_RESTART tick={saveTicks[i]} angle_rad={angleError:F8} joint_velocity_rad_s={velocityError:F8} position_m={positionError:F8} impulse_ns={impulseError:F8}"));
        }
    }

    static void CompareSnapshotField(string name, object before, object after)
    {
        if (Equals(before, after)) return;
        if (before is Dictionary<string, object> previous && after is Dictionary<string, object> current)
        {
            foreach (var field in previous) CompareSnapshotField(name + "." + field.Key, field.Value, current[field.Key]);
            return;
        }
        if (before is List<object> p && after is List<object> c && p.Count == c.Count)
        {
            for (int i = 0; i < p.Count; i++) CompareSnapshotField(name + "[" + i + "]", p[i], c[i]);
            return;
        }
        if (before is double a && after is double b && Math.Abs(a - b) <= 1e-6)
        {
            Debug.Log(FormattableString.Invariant($"NATIVE_FIELD {name}: {a:R} => {b:R}"));
            return;
        }
        throw new Exception($"{name} differs at restoration: {before} => {after}");
    }

    static void Write(StreamWriter csv, string name, IElbowFixture lab, Vector3 point, Vector3 normal)
    {
        var s = lab.Latest;
        csv.Write(FormattableString.Invariant($"{name},{lab.Backend},{lab.Tick},{lab.SimSeconds:R},{s.Target:R},{lab.Angle:R},{s.Feedback.Velocity:R},{s.Feedback.Effort:R},{s.Feedback.Resistance:R},{s.NormalImpulse:R},{lab.X:R},{lab.Y:R},{lab.VX:R},{lab.VY:R},{lab.Spin:R}"));
        csv.Write(lab is NativeElbowLab ? FormattableString.Invariant($",{point.x:R},{point.y:R},{normal.x:R},{normal.y:R}") : ",,,,");
        for (int r = 0; r < ElbowLab.ReceptorCount; r++)
        {
            ElbowLab.Receptor p = lab.ReadRegion(r);
            csv.Write(FormattableString.Invariant($",{p.Force:R},{p.ContactSeconds:R},{p.SlipSpeed:R}"));
        }
        csv.WriteLine();
    }
}
