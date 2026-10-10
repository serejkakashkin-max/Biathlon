using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum SkiLocomotion { Idle, Flat, Climb, Acceleration, Sprint, Tuck, TechnicalDescent }

    [Serializable]
    public sealed class LocomotionPose
    {
        public string state;
        public float hip_drop_m, torso_lean_deg, skate_width_m, stride_m, foot_lift_m;
        public float hip_back_m, hip_hinge_fraction;
        public float cadence_hz, hand_forward_m, hand_height_m, arm_swing_m, hand_width_m;

        public static LocomotionPose Blend(LocomotionPose a, LocomotionPose b, float t, LocomotionPose result)
        {
            result.hip_drop_m = Mathf.Lerp(a.hip_drop_m, b.hip_drop_m, t);
            result.hip_back_m = Mathf.Lerp(a.hip_back_m, b.hip_back_m, t);
            result.hip_hinge_fraction = Mathf.Lerp(a.hip_hinge_fraction, b.hip_hinge_fraction, t);
            result.torso_lean_deg = Mathf.Lerp(a.torso_lean_deg, b.torso_lean_deg, t);
            result.skate_width_m = Mathf.Lerp(a.skate_width_m, b.skate_width_m, t);
            result.stride_m = Mathf.Lerp(a.stride_m, b.stride_m, t);
            result.foot_lift_m = Mathf.Lerp(a.foot_lift_m, b.foot_lift_m, t);
            result.cadence_hz = Mathf.Lerp(a.cadence_hz, b.cadence_hz, t);
            result.hand_forward_m = Mathf.Lerp(a.hand_forward_m, b.hand_forward_m, t);
            result.hand_height_m = Mathf.Lerp(a.hand_height_m, b.hand_height_m, t);
            result.arm_swing_m = Mathf.Lerp(a.arm_swing_m, b.arm_swing_m, t);
            result.hand_width_m = Mathf.Lerp(a.hand_width_m, b.hand_width_m, t);
            return result;
        }
    }

    [Serializable]
    public sealed class AthleteLocomotionSettings
    {
        public int schema_version;
        public string id;
        public float idle_speed_mps, climb_grade, descent_grade, attack_effort, sprint_effort;
        public float transition_s, ski_v_deg, ankle_height_m;
        public float corner_max_lean_deg, corner_response_s, corner_gravity_mps2, corner_sample_half_span_m;
        public float climb_effort_low, climb_effort_high, climb_motion_low, climb_motion_high;
        public float sidestep_span_m,sidestep_lift_m,sidestep_max_cadence_hz,sidestep_ski_yaw_deg;
        public LocomotionPose[] poses;
        public static AthleteLocomotionSettings Load() => FromJson(Resources.Load<TextAsset>("Biathlon/athlete_locomotion_v1").text);
        public static AthleteLocomotionSettings FromJson(string json)
        {
            var d = JsonUtility.FromJson<AthleteLocomotionSettings>(json);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) || d.poses == null || d.poses.Length != 7)
                throw new ArgumentException("Invalid athlete locomotion definition.");
            if (d.idle_speed_mps < 0 || d.climb_grade <= 0 || d.descent_grade >= 0 || d.attack_effort <= 0 || d.sprint_effort <= d.attack_effort || d.transition_s <= 0)
                throw new ArgumentException("Invalid locomotion selection/transition bounds.");
            for (int i = 0; i < d.poses.Length; i++)
            {
                var p = d.poses[i];
                if (p == null || p.state != ((SkiLocomotion)i).ToString()) throw new ArgumentException("Locomotion poses require canonical state order.");
                foreach (float value in new[] { p.hip_drop_m, p.hip_back_m, p.hip_hinge_fraction, p.torso_lean_deg, p.skate_width_m, p.stride_m, p.foot_lift_m, p.cadence_hz, p.hand_forward_m, p.hand_height_m, p.arm_swing_m, p.hand_width_m,
                    d.idle_speed_mps, d.climb_grade, d.descent_grade, d.attack_effort, d.sprint_effort, d.transition_s, d.ski_v_deg, d.ankle_height_m,
                    d.corner_max_lean_deg, d.corner_response_s, d.corner_gravity_mps2, d.corner_sample_half_span_m,
                    d.climb_effort_low,d.climb_effort_high,d.climb_motion_low,d.climb_motion_high })
                    if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Locomotion values must be finite.");
                if (p.hip_drop_m < 0 || p.hip_drop_m > .5f || p.cadence_hz < 0 || p.cadence_hz > 4 || p.skate_width_m < 0 || p.skate_width_m > .5f || p.stride_m < 0 || p.stride_m > .5f || p.foot_lift_m < 0 || p.foot_lift_m > .2f)
                    throw new ArgumentException("Pose exceeds supported rig envelope.");
                if(p.hip_back_m<0||p.hip_back_m>.3f||p.hip_hinge_fraction<0||p.hip_hinge_fraction>1)
                    throw new ArgumentException("Invalid hip hinge envelope.");
            }
            if (d.climb_effort_high <= d.climb_effort_low || d.climb_motion_low <= 0 || d.climb_motion_high < d.climb_motion_low)
                throw new ArgumentException("Invalid climb intensity envelope.");
            if (d.corner_max_lean_deg <= 0 || d.corner_max_lean_deg > 40 || d.corner_response_s <= 0 || d.corner_gravity_mps2 <= 0 || d.corner_sample_half_span_m <= 0)
                throw new ArgumentException("Invalid presentation corner envelope.");
            if(!float.IsFinite(d.sidestep_span_m)||d.sidestep_span_m<0||d.sidestep_span_m>.3f||
                !float.IsFinite(d.sidestep_lift_m)||d.sidestep_lift_m<0||d.sidestep_lift_m>.15f||
                !float.IsFinite(d.sidestep_max_cadence_hz)||d.sidestep_max_cadence_hz<0||d.sidestep_max_cadence_hz>3||
                !float.IsFinite(d.sidestep_ski_yaw_deg)||d.sidestep_ski_yaw_deg<0||d.sidestep_ski_yaw_deg>35)
                throw new ArgumentException("Invalid observed lateral step envelope.");
            if(d.sidestep_span_m>0&&(d.sidestep_lift_m<=0||d.sidestep_max_cadence_hz<=0)||
                d.sidestep_span_m==0&&(d.sidestep_lift_m!=0||d.sidestep_max_cadence_hz!=0||d.sidestep_ski_yaw_deg!=0))
                throw new ArgumentException("Lateral steps require a complete span, lift and cadence definition.");
            return d;
        }
    }

    public readonly struct LocomotionInput
    {
        public readonly float Speed, Grade, Effort, Curvature,LateralSpeed;
        public readonly bool Tuck, Sprint, Travelling;
        public LocomotionInput(float speed, float grade, float effort, bool tuck = false, bool sprint = false, bool travelling = true, float curvature = 0,float lateralSpeed=0)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0 || float.IsNaN(grade) || float.IsInfinity(grade) || float.IsNaN(effort) || float.IsInfinity(effort) || effort < 0)
                throw new ArgumentException("Invalid locomotion observation.");
            Speed = speed; Grade = grade; Effort = effort; Tuck = tuck; Sprint = sprint; Travelling = travelling;
            if (float.IsNaN(curvature) || float.IsInfinity(curvature)) throw new ArgumentException("Invalid course curvature.");
            Curvature = curvature;
            if(!float.IsFinite(lateralSpeed))throw new ArgumentException("Invalid lateral motion observation.");
            LateralSpeed=lateralSpeed;
        }
    }

    public static class AthleteLocomotion
    {
        // Positive signed curvature turns toward local +X. This is a visual lean target;
        // it neither applies a force nor reads/modifies the authoritative cornering system.
        public static float CornerLean(LocomotionInput input, AthleteLocomotionSettings settings)
        {
            if (!input.Travelling || input.Speed <= settings.idle_speed_mps) return 0;
            return Mathf.Clamp(Mathf.Atan(input.Speed * input.Speed * input.Curvature / settings.corner_gravity_mps2) * Mathf.Rad2Deg,
                -settings.corner_max_lean_deg, settings.corner_max_lean_deg);
        }
        public static SkiLocomotion Select(LocomotionInput input, AthleteLocomotionSettings settings)
        {
            if (!input.Travelling) return SkiLocomotion.Idle;
            if (input.Tuck) return SkiLocomotion.Tuck;
            if (Mathf.Max(input.Speed,Mathf.Abs(input.LateralSpeed)) <= settings.idle_speed_mps) return SkiLocomotion.Idle;
            if (input.Sprint || input.Effort >= settings.sprint_effort) return SkiLocomotion.Sprint;
            if (input.Grade >= settings.climb_grade) return SkiLocomotion.Climb;
            if (input.Grade <= settings.descent_grade) return SkiLocomotion.TechnicalDescent;
            return input.Effort >= settings.attack_effort ? SkiLocomotion.Acceleration : SkiLocomotion.Flat;
        }
    }
}
