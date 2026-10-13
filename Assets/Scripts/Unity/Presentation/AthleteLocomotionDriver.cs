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
        private SkiTechniqueVisualSettings technique;
        private double poleCycle;
        private float polingRatio;
        private readonly LocomotionPose blended = new LocomotionPose();
        private readonly LocomotionPose start = new LocomotionPose();
        private readonly LocomotionPose motion = new LocomotionPose();
        private readonly AthleteExertionPresentation exertion = new AthleteExertionPresentation();
        private float climbIntensity = 1;
        private float climbWeight;
        private float tuckWeight, tuckStartWeight;
        private float sidestepWeight,sidestepDirection;
        public float SidestepWeight=>sidestepWeight;
        private double previousTime = -1, transitionAt, cycle;
        private string athleteId;
        public SkiLocomotion State { get; private set; }
        public float BlendProgress { get; private set; }
        public double Cycle => cycle;
        public double PoleCycle => poleCycle;
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
                (float)frame.PresentationCurvature,(float)frame.LateralMetersPerSecond), frame.TimeSeconds, ExertionInput.From(a));
        }

        public void CopyVisualPose(LocomotionPose destination) => LocomotionPose.Blend(motion, motion, 1, destination);

        public void Sample(AthleteRigBinding rig, LocomotionInput input, double time, ExertionInput? observedExertion = null, bool renderPose = true)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0) throw new ArgumentOutOfRangeException(nameof(time));
            if (settings == null) {settings = AthleteLocomotionSettings.Load();technique=SkiTechniqueVisualSettings.Load();}
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
                polingRatio=selected==SkiLocomotion.Climb?technique.climb_poles_per_cycle:technique.flat_poles_per_cycle;
                poleCycle=(time*blended.cadence_hz*polingRatio+technique.pole_phase_offset_cycles)%1;
                dt = 0;
                climbIntensity = requestedIntensity;
                CornerLeanDegrees = requestedLean;
                tuckStartWeight = tuckWeight = selected == SkiLocomotion.Tuck ? 1 : 0;
            }
            else if (selected != State)
            {
                LocomotionPose.Blend(blended, blended, 1, start);
                tuckStartWeight = tuckWeight;
                State = selected; transitionAt = time;
            }
            BlendProgress = Mathf.Clamp01((float)((time - transitionAt) / settings.transition_s));
            float transitionWeight = Mathf.SmoothStep(0, 1, BlendProgress);
            LocomotionPose.Blend(start, settings.poses[(int)State], transitionWeight, blended);
            tuckWeight = Mathf.Lerp(tuckStartWeight, State == SkiLocomotion.Tuck ? 1 : 0, transitionWeight);
            climbIntensity = Mathf.Lerp(climbIntensity, requestedIntensity, 1 - Mathf.Exp(-(float)dt / settings.transition_s));
            LocomotionPose.Blend(blended, blended, 1, motion);
            climbWeight=Mathf.Lerp(climbWeight,selected==SkiLocomotion.Climb?1:0,reset?1:1-Mathf.Exp(-(float)dt/settings.transition_s));
            float steep=climbWeight*Mathf.InverseLerp(settings.climb_grade,technique.steep_climb_grade,input.Grade);
            motion.stride_m*=Mathf.Lerp(1,technique.steep_stride_multiplier,steep);
            motion.skate_width_m*=Mathf.Lerp(1,technique.steep_skate_multiplier,steep);
            float sideSpeed=Mathf.Abs(input.LateralSpeed);
            float requestedSide=input.Travelling&&!input.Tuck&&settings.sidestep_span_m>0?
                sideSpeed/Mathf.Max(settings.idle_speed_mps,input.Speed+sideSpeed):0;
            float sideBlend=reset?1:1-Mathf.Exp(-(float)dt/settings.transition_s);
            sidestepWeight=Mathf.Lerp(sidestepWeight,requestedSide,sideBlend);
            sidestepDirection=Mathf.Lerp(sidestepDirection,Mathf.Sign(input.LateralSpeed),sideBlend);
            if(sidestepWeight>0)
            {
                float cadence=Mathf.Min(settings.sidestep_max_cadence_hz,sideSpeed/(2*settings.sidestep_span_m));
                motion.cadence_hz=Mathf.Lerp(motion.cadence_hz,cadence,sidestepWeight);
                motion.foot_lift_m=Mathf.Lerp(motion.foot_lift_m,settings.sidestep_lift_m,sidestepWeight);
                motion.arm_swing_m*=1-sidestepWeight;
                motion.stride_m*=1-sidestepWeight;
            }
            motion.cadence_hz *= climbIntensity;
            motion.torso_lean_deg *= climbIntensity;
            motion.arm_swing_m *= climbIntensity;
            exertion.Apply(motion, observedExertion ?? new ExertionInput(0, 1, 0), tuckWeight, time, dt, reset);
            cycle = (cycle + dt * motion.cadence_hz) % 1;
            polingRatio=Mathf.Lerp(polingRatio,selected==SkiLocomotion.Climb?technique.climb_poles_per_cycle:technique.flat_poles_per_cycle,1-Mathf.Exp(-(float)dt/settings.transition_s));
            poleCycle=(poleCycle+dt*motion.cadence_hz*polingRatio)%1;
            previousTime = time;
            CornerLeanDegrees = Mathf.Lerp(CornerLeanDegrees, requestedLean, 1 - Mathf.Exp(-(float)dt / settings.corner_response_s));
            if (renderPose) Pose(rig, motion, (float)(cycle * Math.PI * 2));
        }

        private void Pose(AthleteRigBinding rig, LocomotionPose pose, float phase)
        {
            rig.ResetPose();
            float polePhase=(float)(poleCycle*Math.PI*2);
            float active=Mathf.Clamp01(pose.arm_swing_m/technique.motion_reference_swing_m);
            // Small compression into the paired pole push, followed by a gradual return.
            // Do not rock backwards past the base skiing lean during recovery.
            float pulse=(.5f-.5f*Mathf.Sin(polePhase))*active;
            // Keep weight transfer small and toward the planted ski during recovery.
            // The continuous wave avoids a rapid jump across the grounded overlap;
            // its resource scalar sets the owner-requested minimal body motion.
            // Leg/pole timing and ground contact stay independent of this bone offset.
            float overlap=technique.glide_fraction-.5f;
            float transfer=-Mathf.Sin(phase-Mathf.PI*overlap)*(.105f+pose.skate_width_m)*technique.hip_transfer_fraction;
            rig.Offset(AthleteBone.Hips, new Vector3(transfer*active, -pose.hip_drop_m-technique.hip_pulse_m*pulse, -pose.hip_back_m));
            float lean=pose.torso_lean_deg+technique.torso_pulse_deg*pulse;
            float hinge=lean*pose.hip_hinge_fraction;
            rig.Rotate(AthleteBone.Hips, Quaternion.Euler(hinge, 0, -CornerLeanDegrees));
            rig.Rotate(AthleteBone.Spine, Quaternion.Euler((lean-hinge) * .6f, 0, 0));
            rig.Rotate(AthleteBone.Chest, Quaternion.Euler((lean-hinge) * .4f, 0, 0));
            rig.Rotate(AthleteBone.Neck, Quaternion.Euler(-pose.torso_lean_deg * .7f, 0, 0));
            Leg(false, phase); Leg(true, phase + Mathf.PI);
            Arm(false, polePhase); Arm(true, polePhase);

            void Leg(bool right, float angle)
            {
                float side = right ? 1 : -1;
                float cyclePhase=Mathf.Repeat(angle/(2*Mathf.PI),1);
                float recovery=cyclePhase<technique.glide_fraction?0:(cyclePhase-technique.glide_fraction)/(1-technique.glide_fraction);
                float stroke=Stroke(cyclePhase);
                var upper = rig.Bone(right ? AthleteBone.RightUpperLeg : AthleteBone.LeftUpperLeg);
                var lower = rig.Bone(right ? AthleteBone.RightLowerLeg : AthleteBone.LeftLowerLeg);
                var foot = rig.Bone(right ? AthleteBone.RightFoot : AthleteBone.LeftFoot);
                // Long grounded push/glide, then a shorter lifted return. Opposite legs overlap
                // support instead of tracing two equal airborne/grounded semicircles.
                float lift=Mathf.Sin(recovery*Mathf.PI);lift*=lift;
                var at = new Vector3(side * (.105f + pose.skate_width_m * stroke), settings.ankle_height_m + lift * pose.foot_lift_m,
                    (1-2*stroke) * pose.stride_m);
                // Lift and replace each ski in the observed direction of lateral
                // travel. A zero-forward-speed bypass must not use the idle pose.
                // Span is the complete lateral stroke. Widen its centre by half
                // that span so opposite-phase boots cannot cross on recovery.
                float sideStep=side*(.105f+settings.sidestep_span_m*.5f)+
                    sidestepDirection*settings.sidestep_span_m*(.5f-stroke);
                at.x=Mathf.Lerp(at.x,sideStep,sidestepWeight);
                SolveTwoBone(upper, lower, foot, rig.ModelRoot.TransformPoint(at), rig.ModelRoot.forward, rig.ModelRoot.rotation);
                float yaw=Mathf.Lerp(1,technique.recovery_yaw_fraction,lift);
                float skiYaw=side*settings.ski_v_deg*Mathf.Clamp01(pose.skate_width_m/.15f)*yaw;
                skiYaw=Mathf.Lerp(skiYaw,sidestepDirection*settings.sidestep_ski_yaw_deg*lift,sidestepWeight);
                foot.rotation=rig.ModelRoot.rotation*Quaternion.Euler(0,skiYaw,0);
            }
            float Stroke(float at)=>at<technique.glide_fraction?Mathf.SmoothStep(0,1,at/technique.glide_fraction):
                Mathf.SmoothStep(1,0,(at-technique.glide_fraction)/(1-technique.glide_fraction));
            void Arm(bool right, float angle)
            {
                float side = right ? 1 : -1;
                var upper = rig.Bone(right ? AthleteBone.RightUpperArm : AthleteBone.LeftUpperArm);
                var lower = rig.Bone(right ? AthleteBone.RightLowerArm : AthleteBone.LeftLowerArm);
                var hand = rig.Bone(right ? AthleteBone.RightHand : AthleteBone.LeftHand);
                var at = new Vector3(side * pose.hand_width_m, pose.hand_height_m + Mathf.Cos(angle) * pose.arm_swing_m * .3f,
                    pose.hand_forward_m + Mathf.Sin(angle) * pose.arm_swing_m);
                // V1 offset poling on the uphill support side; paired push once
                // per complete left/right skating cycle, without extra rocking.
                at.y+=side*technique.climb_hand_stagger_m*climbWeight;
                at.z-=side*technique.climb_hand_stagger_m*climbWeight;
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
