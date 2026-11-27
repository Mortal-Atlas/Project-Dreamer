using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public static class LowerBodyTrace
    {
        public static void WriteHeader(TextWriter writer)
        {
            writer.Write("schema,provenance,backend,controller_version,seed,stage,hip_width_m,thigh_m,shin_m,foot_m,controller,tick,substep_s,substeps,phase,stance_side,target_share,stance_ticks,ready,outcome,action_reason,roll_noise_bound_m_s2,gravity_body_m_s2,gyro_body_rad_s,linear_accel_body_m_s2,orientation_xyzw,pelvis_m,com_m,momentum_ns,residual_ns,signed_work_j,absolute_work_j,non_foot_contacts,tap_ns,peak_swing_clearance_m,peak_swing_travel_m,first_fall_tick");
            for (int side = 0; side < 2; side++)
                writer.Write($",foot{side}_impulse_ns,foot{side}_cop_m,foot{side}_cop_valid,foot{side}_position_m,foot{side}_contact,foot{side}_changed,foot{side}_clearance_m,foot{side}_drift_m");
            for (int joint = 0; joint < 6; joint++)
                writer.Write($",joint{joint}_pitch_target_rad,joint{joint}_pitch_rad,joint{joint}_pitch_rad_s,joint{joint}_pitch_nm,joint{joint}_roll_target_rad,joint{joint}_roll_rad,joint{joint}_roll_rad_s,joint{joint}_roll_nm,joint{joint}_saturated");
            writer.WriteLine();
        }

        public static void WriteRow(TextWriter writer, NativeLowerBodyLab lab, string phase) => writer.WriteLine(Capture(lab, phase));

        public static string Capture(NativeLowerBodyLab lab, string phase)
        {
            var row = new StringBuilder(1000);
            var v = lab.Vestibular;
            var q = v.Orientation;
            Add(row, $"2,isolated-physical-measurement,PhysX,C1.1,{lab.Seed},{lab.Stage:R},{lab.Dimensions.HipWidth:R},{lab.Dimensions.Thigh:R},{lab.Dimensions.Shin:R},{lab.Dimensions.Foot:R},{lab.Controller},{v.Tick},{NativeLowerBodyLab.Substep:R},{NativeLowerBodyLab.Substeps},{phase},{lab.ActionStanceSide},{lab.TargetShare:R},{lab.StanceTicks},{lab.SwingReady},{lab.Action},{lab.ActionReason},{v.GravityErrorBound:R},{Vector(v.Gravity)},{Vector(v.AngularVelocity)},{Vector(v.LinearAcceleration)},{q.x:R};{q.y:R};{q.z:R};{q.w:R},{Vector(lab.PelvisPosition)},{Vector(lab.CenterOfMass)},{Vector(lab.Momentum)},{Vector(lab.MomentumResidual)},{lab.SignedWork:R},{lab.AbsoluteWork:R},{lab.NonFootContacts},{Vector(lab.ExternalImpulse)},{lab.SwingClearance:R},{lab.SwingTravel:R},{lab.FirstFallTick}");
            for (int side = 0; side < 2; side++)
            {
                var sole = lab.ReadSole(side);
                string center = sole.PressureValid ? Vector(sole.PressureCenter) : "";
                Add(row, $",{Vector(sole.Impulse)},{center},{sole.PressureValid},{Vector(sole.Position)},{sole.Contact},{sole.Changed},{sole.Clearance:R},{sole.Drift:R}");
            }
            for (int index = 0; index < 6; index++)
            {
                var j = lab.ReadJoint(index);
                var p = lab.ReadProprioception(index);
                Add(row, $",{j.Target:R},{j.Angle:R},{p.Velocity:R},{j.Torque:R},{p.RollTarget:R},{p.Roll:R},{p.RollVelocity:R},{p.RollTorque:R},{p.Saturated}");
            }
            return row.ToString();
        }

        static string Vector(Vector3 v) => FormattableString.Invariant($"{v.x:R};{v.y:R};{v.z:R}");
        static void Add(StringBuilder row, FormattableString fields) => row.Append(FormattableString.Invariant(fields));
    }
}
