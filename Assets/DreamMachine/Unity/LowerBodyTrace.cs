using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    public static class LowerBodyTrace
    {
        public static void WriteHeader(TextWriter writer, bool dynamics = false, bool recovery = false)
        {
            writer.Write("schema,provenance,backend,controller_version,seed,stage,hip_width_m,thigh_m,shin_m,foot_m,controller,tick,substep_s,substeps,phase,stance_side,target_share,stance_ticks,ready,outcome,action_reason,roll_noise_bound_m_s2,gravity_body_m_s2,gyro_body_rad_s,linear_accel_body_m_s2,orientation_xyzw,pelvis_m,com_m,momentum_ns,residual_ns,signed_work_j,absolute_work_j,non_foot_contacts,tap_ns,peak_swing_clearance_m,peak_swing_travel_m,first_fall_tick");
            for (int side = 0; side < 2; side++)
                writer.Write($",foot{side}_impulse_ns,foot{side}_cop_m,foot{side}_cop_valid,foot{side}_position_m,foot{side}_contact,foot{side}_changed,foot{side}_clearance_m,foot{side}_drift_m");
            for (int joint = 0; joint < 6; joint++)
                writer.Write($",joint{joint}_pitch_target_rad,joint{joint}_pitch_rad,joint{joint}_pitch_rad_s,joint{joint}_pitch_nm,joint{joint}_roll_target_rad,joint{joint}_roll_rad,joint{joint}_roll_rad_s,joint{joint}_roll_nm,joint{joint}_saturated");
            if (dynamics)
                writer.Write(",step_protocol,motion_phase,phase_started_tick,transition_reason,swing_started_tick,com_velocity_m_s,angular_momentum_kg_m2_s,xcom_m,frontal_margin_m,sagittal_margin_m,net_cop_m,stance_x_min_m,stance_x_max_m,stance_z_min_m,stance_z_max_m,stance_contact_fraction,stance_impulse_ns,other_foot_impulse_ns,prelift_ticks,prelift_failure_mask,peak_effort_fraction,lift_passed,forward_passed,dynamic_gate_passed,stance_loss_ticks,peak_body_impulse_ns");
            if (recovery) writer.Write(",recovery_policy,cancellation_tick,cancelled_before_lift,cancelled_supported,recovery_touchdown_tick,landing_target_z_m");
            writer.WriteLine();
        }

        public static void WriteRow(TextWriter writer, NativeLowerBodyLab lab, string phase, bool dynamics = false, bool recovery = false) => writer.WriteLine(Capture(lab, phase, dynamics, recovery));

        public static string Capture(NativeLowerBodyLab lab, string phase, bool dynamics = false, bool recovery = false)
        {
            var row = new StringBuilder(1000);
            var v = lab.Vestibular;
            var q = v.Orientation;
            Add(row, $"{(recovery ? 4 : dynamics ? 3 : 2)},isolated-physical-measurement,PhysX,{(recovery ? "C1.3" : dynamics ? "C1.2" : "C1.1")},{lab.Seed},{lab.Stage:R},{lab.Dimensions.HipWidth:R},{lab.Dimensions.Thigh:R},{lab.Dimensions.Shin:R},{lab.Dimensions.Foot:R},{lab.Controller},{v.Tick},{lab.PhysicsStepSeconds:R},{lab.PhysicsSubsteps},{phase},{(dynamics ? lab.DynamicStanceSide : lab.ActionStanceSide)},{lab.TargetShare:R},{lab.StanceTicks},{lab.SwingReady},{lab.Action},{lab.ActionReason},{v.GravityErrorBound:R},{Vector(v.Gravity)},{Vector(v.AngularVelocity)},{Vector(v.LinearAcceleration)},{q.x:R};{q.y:R};{q.z:R};{q.w:R},{Vector(lab.PelvisPosition)},{Vector(lab.CenterOfMass)},{Vector(lab.Momentum)},{Vector(lab.MomentumResidual)},{lab.SignedWork:R},{lab.AbsoluteWork:R},{lab.NonFootContacts},{Vector(lab.ExternalImpulse)},{lab.SwingClearance:R},{lab.SwingTravel:R},{lab.FirstFallTick}");
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
            if (dynamics)
            {
                int stance = lab.DynamicStanceSide;
                var region = lab.FootRegion(stance);
                string pressure = lab.NetPressureValid ? Vector(lab.NetPressure) : "";
                Add(row, $",{lab.StepProtocol},{lab.MotionPhase},{lab.PhaseStartedTick},{lab.TransitionReason},{lab.SwingStartedTick},{Vector(lab.CenterVelocity)},{Vector(lab.AngularMomentum)},{Vector(lab.XCom)},{lab.FrontalMargin:R},{lab.SagittalMargin:R},{pressure},{region.MinX:R},{region.MaxX:R},{region.MinZ:R},{region.MaxZ:R},{lab.ContactFraction(stance):R},{lab.ReadSole(stance).Impulse.y:R},{lab.ReadSole(1 - stance).Impulse.y:R},{lab.PreliftTicks},{(int)lab.PreliftFailures},{lab.PeakEffortFraction:R},{lab.LiftPassed},{lab.ForwardPassed},{lab.DynamicGatePassed},{lab.StanceLossTicks},{lab.PeakBodyImpact:R}");
            }
            if (recovery)
                Add(row, $",{lab.RecoveryPolicy},{lab.CancellationTick},{lab.CancelledBeforeLift},{lab.CancelledWhileSupported},{lab.RecoveryTouchdownTick},{lab.LandingTargetZ:R}");
            return row.ToString();
        }

        static string Vector(Vector3 v) => FormattableString.Invariant($"{v.x:R};{v.y:R};{v.z:R}");
        static void Add(StringBuilder row, FormattableString fields) => row.Append(FormattableString.Invariant(fields));
    }
}
