using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Original procedural motion, driven only by completed-tick observations.</summary>
    public sealed class AthleteLocomotionDriver : AthletePoseDriver
    {
        private AthleteLocomotionSettings settings;
        private readonly LocomotionPose blended = new LocomotionPose();
        private readonly LocomotionPose start = new LocomotionPose();
        private readonly LocomotionPose motion = new LocomotionPose();
        private readonly AthleteExertionPresentation exertion = new AthleteExertionPresentation();
        private float climbIntensity = 1;
        private double previousTime = -1, transitionAt, cycle;
        private string athleteId;
        public SkiLocomotion State { get; private set; }
        public float BlendProgress { get; private set; }
        public double Cycle => cycle;
        public float CornerLeanDegrees { get; private set; }
        public float ExertionIntensity => exertion.Intensity;
        public float FinishWeight => exertion.FinishWeight;

        public override void Apply(AthleteRigBinding rig, AthleteAnimationFrame frame)
        {
            var a = frame.Athlete;
            if (athleteId != a.AthleteId) { previousTime = -1; athleteId = a.AthleteId; }
            Sample(rig, new LocomotionInput((float)a.SpeedMetersPerSecond, (float)a.CourseGrade,
                (float)a.Effort.DeliveredRelativeEffort, a.TuckActive, a.FinalSprintActive,
                a.OnCourse && !a.ShootingActive && a.Phase != CompetitionPhase.Finished,
                (float)frame.PresentationCurvature), frame.TimeSeconds, ExertionInput.From(a));
        }

        public void Sample(AthleteRigBinding rig, LocomotionInput input, double time, ExertionInput? observedExertion = null)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0) throw new ArgumentOutOfRangeException(nameof(time));
            if (settings == null) settings = AthleteLocomotionSettings.Load();
            var selected = AthleteLocomotion.Select(input, settings);
            float requestedLean = AthleteLocomotion.CornerLean(input, settings);
            float requestedIntensity = selected == SkiLocomotion.Climb ? Mathf.Lerp(settings.climb_motion_low, settings.climb_motion_high,
                Mathf.InverseLerp(settings.climb_effort_low, settings.climb_effort_high, input.Effort)) : 1;
            double dt = previousTime < 0 ? 0 : Math.Max(0, time - previousTime);
            bool reset = previousTime < 0 || time < previousTime || dt > 1;
            if (reset)
            {
                State = selected; transitionAt = time - settings.transition_s;
                LocomotionPose.Blend(settings.poses[(int)State], settings.poses[(int)State], 1, blended);
                LocomotionPose.Blend(blended, blended, 1, start);
                cycle = time * blended.cadence_hz % 1;
                dt = 0;
                climbIntensity = requestedIntensity;
                CornerLeanDegrees = requestedLean;
            }
            else if (selected != State)
            {
                LocomotionPose.Blend(blended, blended, 1, start);
                State = selected; transitionAt = time;
            }
            BlendProgress = Mathf.Clamp01((float)((time - transitionAt) / settings.transition_s));
            LocomotionPose.Blend(start, settings.poses[(int)State], Mathf.SmoothStep(0, 1, BlendProgress), blended);
            climbIntensity = Mathf.Lerp(climbIntensity, requestedIntensity, 1 - Mathf.Exp(-(float)dt / settings.transition_s));
            LocomotionPose.Blend(blended, blended, 1, motion);
            motion.cadence_hz *= climbIntensity;
            motion.torso_lean_deg *= climbIntensity;
            motion.arm_swing_m *= climbIntensity;
            exertion.Apply(motion, observedExertion ?? new ExertionInput(0, 1, 0), State, time, dt, reset);
            cycle = (cycle + dt * motion.cadence_hz) % 1;
            previousTime = time;
            CornerLeanDegrees = Mathf.Lerp(CornerLeanDegrees, requestedLean, 1 - Mathf.Exp(-(float)dt / settings.corner_response_s));
            Pose(rig, motion, (float)(cycle * Math.PI * 2));
        }

        private void Pose(AthleteRigBinding rig, LocomotionPose pose, float phase)
        {
            rig.ResetPose();
            rig.Offset(AthleteBone.Hips, new Vector3(Mathf.Sin(phase) * pose.skate_width_m * .25f, -pose.hip_drop_m, 0));
            rig.Rotate(AthleteBone.Hips, Quaternion.Euler(0, 0, -CornerLeanDegrees));
            rig.Rotate(AthleteBone.Spine, Quaternion.Euler(pose.torso_lean_deg * .6f, 0, 0));
            rig.Rotate(AthleteBone.Chest, Quaternion.Euler(pose.torso_lean_deg * .4f, 0, 0));
            rig.Rotate(AthleteBone.Neck, Quaternion.Euler(-pose.torso_lean_deg * .7f, 0, 0));
            Leg(false, phase); Leg(true, phase + Mathf.PI);
            Arm(false, phase); Arm(true, phase + Mathf.PI);

            void Leg(bool right, float angle)
            {
                float side = right ? 1 : -1;
                float stroke = .5f + .5f * Mathf.Sin(angle);
                var upper = rig.Bone(right ? AthleteBone.RightUpperLeg : AthleteBone.LeftUpperLeg);
                var lower = rig.Bone(right ? AthleteBone.RightLowerLeg : AthleteBone.LeftLowerLeg);
                var foot = rig.Bone(right ? AthleteBone.RightFoot : AthleteBone.LeftFoot);
                var at = new Vector3(side * (.105f + pose.skate_width_m * stroke), settings.ankle_height_m + Mathf.Max(0, Mathf.Cos(angle)) * pose.foot_lift_m,
                    Mathf.Cos(angle) * pose.stride_m);
                SolveTwoBone(upper, lower, foot, rig.ModelRoot.TransformPoint(at), rig.ModelRoot.forward, rig.ModelRoot.rotation);
                foot.rotation = rig.ModelRoot.rotation * Quaternion.Euler(0, side * settings.ski_v_deg * Mathf.Clamp01(pose.skate_width_m / .15f), 0);
            }
            void Arm(bool right, float angle)
            {
                float side = right ? 1 : -1;
                var upper = rig.Bone(right ? AthleteBone.RightUpperArm : AthleteBone.LeftUpperArm);
                var lower = rig.Bone(right ? AthleteBone.RightLowerArm : AthleteBone.LeftLowerArm);
                var hand = rig.Bone(right ? AthleteBone.RightHand : AthleteBone.LeftHand);
                var at = new Vector3(side * pose.hand_width_m, pose.hand_height_m + Mathf.Cos(angle) * pose.arm_swing_m * .3f,
                    pose.hand_forward_m + Mathf.Sin(angle) * pose.arm_swing_m);
                SolveTwoBone(upper, lower, hand, rig.ModelRoot.TransformPoint(at), rig.ModelRoot.TransformDirection(new Vector3(side * .25f, -.3f, -1)), rig.ModelRoot.rotation);
                hand.rotation = rig.ModelRoot.rotation;
            }
        }

        /// <summary>Presentation-only analytic limb solve. Preserves both bone lengths and clamps reach.</summary>
        public static void SolveTwoBone(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 bendHint, Quaternion modelRotation)
        {
            Vector3 origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, end.position);
            Vector3 direction = target - origin;
            float length = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
            direction = direction.sqrMagnitude > .000001f ? direction.normalized : modelRotation * Vector3.down;
            Vector3 bend = Vector3.ProjectOnPlane(bendHint, direction).normalized;
            if (bend.sqrMagnitude < .0001f) bend = Vector3.Cross(direction, modelRotation * Vector3.right).normalized;
            float along = (a * a - b * b + length * length) / (2 * length);
            Vector3 joint = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(modelRotation * Vector3.down, joint - origin) * modelRotation;
            lower.rotation = Quaternion.FromToRotation(modelRotation * Vector3.down, origin + direction * length - joint) * modelRotation;
        }
    }
}
