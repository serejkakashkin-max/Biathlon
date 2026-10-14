using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum SkiMotionState { Flat, Uphill, SteepUphill, FastSkate, MaxSprint, NoPole, DoublePole, Tuck, CornerLeft, CornerRight, Fatigued, SlowStop }

    [Serializable] public sealed class SkiMotionProfile
    {
        public string state;
        public float poles_per_cycle, cadence_scale, stride_scale, width_scale, lift_scale, arm_scale;
        public float hand_stagger_m, torso_twist_deg, compression_deg, hip_transfer_m, ski_edge_deg, ready_weight;
        public static void Blend(SkiMotionProfile a, SkiMotionProfile b, float t, SkiMotionProfile d)
        {
            d.poles_per_cycle=Mathf.Lerp(a.poles_per_cycle,b.poles_per_cycle,t);
            d.cadence_scale=Mathf.Lerp(a.cadence_scale,b.cadence_scale,t);
            d.stride_scale=Mathf.Lerp(a.stride_scale,b.stride_scale,t);
            d.width_scale=Mathf.Lerp(a.width_scale,b.width_scale,t);
            d.lift_scale=Mathf.Lerp(a.lift_scale,b.lift_scale,t);
            d.arm_scale=Mathf.Lerp(a.arm_scale,b.arm_scale,t);
            d.hand_stagger_m=Mathf.Lerp(a.hand_stagger_m,b.hand_stagger_m,t);
            d.torso_twist_deg=Mathf.Lerp(a.torso_twist_deg,b.torso_twist_deg,t);
            d.compression_deg=Mathf.Lerp(a.compression_deg,b.compression_deg,t);
            d.hip_transfer_m=Mathf.Lerp(a.hip_transfer_m,b.hip_transfer_m,t);
            d.ski_edge_deg=Mathf.Lerp(a.ski_edge_deg,b.ski_edge_deg,t);
            d.ready_weight=Mathf.Lerp(a.ready_weight,b.ready_weight,t);
        }
    }

    [Serializable] public sealed class SkiMotionCurve
    {
        public string id;
        public float[] time,value;
        public float At(float phase)
        {
            phase=Mathf.Repeat(phase,1);
            for(int i=1;i<time.Length;i++)if(phase<=time[i])
            {float t=(phase-time[i-1])/(time[i]-time[i-1]);return Mathf.Lerp(value[i-1],value[i],Ease(t));}
            return value[value.Length-1];
        }
        public static float Ease(float t) {t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
    }

    [Serializable] public sealed class SkiMotionSettings
    {
        public int schema_version; public string id,provenance;
        public float transition_s, phase_response_s, speed_reference_mps, min_cadence_hz, max_cadence_hz;
        public float steep_grade, uphill_grade, fast_speed_mps, sprint_effort, no_pole_effort, corner_curvature, fatigue_threshold, slow_speed_mps;
        public float glide_fraction, yaw_deg, recovery_yaw_fraction, ankle_height_m, hip_compression_m, max_hip_transfer_m, arm_reach_margin_m;
        public float pole_height_scale, pole_drive_start, pole_drive_end, max_pole_cadence_hz;
        public float ready_hand_height_m, ready_hand_forward_m, ready_hand_width_m;
        public Vector3 elbow_plant_hint, elbow_release_hint;
        public SkiMotionProfile[] profiles; public SkiMotionCurve[] curves;
        private static SkiMotionSettings cached;
        public static SkiMotionSettings Load()=>cached??(cached=FromJson(Resources.Load<TextAsset>("Biathlon/ski_motion_v2").text));
        public static SkiMotionSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<SkiMotionSettings>(json);
            if(d==null||d.schema_version!=2||string.IsNullOrWhiteSpace(d.id)||d.profiles==null||d.profiles.Length!=12||d.curves==null||d.curves.Length!=5)
                throw new ArgumentException("Invalid hybrid ski motion definition.");
            if(!(d.transition_s>0&&d.phase_response_s>0&&d.speed_reference_mps>0&&d.min_cadence_hz>0&&d.max_cadence_hz>=d.min_cadence_hz&&d.max_cadence_hz<=3&&
                d.glide_fraction>.5f&&d.glide_fraction<.9f&&d.ankle_height_m>0&&d.max_hip_transfer_m>0&&d.max_hip_transfer_m<=.025f&&d.arm_reach_margin_m>=.04f&&d.arm_reach_margin_m<=.15f&&
                d.pole_height_scale>.5f&&d.pole_height_scale<1.5f&&d.pole_drive_start>0&&d.pole_drive_end>d.pole_drive_start&&d.pole_drive_end<.6f&&
                d.max_pole_cadence_hz>=1&&d.max_pole_cadence_hz<=2.5f&&d.ready_hand_height_m>1.15f&&d.ready_hand_height_m<1.4f&&
                d.ready_hand_forward_m>=0&&d.ready_hand_forward_m<.3f&&d.ready_hand_width_m>.15f&&d.ready_hand_width_m<.3f))
                throw new ArgumentException("Hybrid motion exceeds supported contact envelope.");
            if(!ValidHint(d.elbow_plant_hint)||!ValidHint(d.elbow_release_hint))throw new ArgumentException("Invalid pole elbow envelope.");
            for(int i=0;i<d.profiles.Length;i++)
            {
                var p=d.profiles[i];if(p.state!=((SkiMotionState)i).ToString()||p.poles_per_cycle<0||p.poles_per_cycle>2||
                    p.hip_transfer_m<0||p.hip_transfer_m>d.max_hip_transfer_m||p.ski_edge_deg<0||p.ski_edge_deg>15||
                    p.cadence_scale<0||p.cadence_scale>2||p.stride_scale<0||p.stride_scale>2||p.width_scale<0||p.width_scale>2||p.lift_scale<0||p.lift_scale>2||p.ready_weight<0||p.ready_weight>1)
                    throw new ArgumentException("Invalid hybrid motion profile.");
                foreach(float v in new[]{p.poles_per_cycle,p.cadence_scale,p.stride_scale,p.width_scale,p.lift_scale,p.arm_scale,p.hand_stagger_m,p.torso_twist_deg,p.compression_deg,p.hip_transfer_m,p.ski_edge_deg,p.ready_weight})
                    if(!float.IsFinite(v))throw new ArgumentException("Non-finite hybrid motion profile.");
            }
            string[] ids={"support_transfer","paired_pole_stroke","pole_compression","paired_pole_height","pole_recovery_clearance"};
            for(int index=0;index<d.curves.Length;index++)
            {
                var c=d.curves[index];if(c.id!=ids[index])throw new ArgumentException("Invalid authored curve identity/order.");
                if(c.time==null||c.value==null||c.time.Length!=c.value.Length||c.time.Length<3||c.time[0]!=0||c.time[c.time.Length-1]!=1||Mathf.Abs(c.value[0]-c.value[c.value.Length-1])>.0001f)
                    throw new ArgumentException("Authored curves must be closed and normalized.");
                for(int i=0;i<c.time.Length;i++)if(!float.IsFinite(c.time[i])||!float.IsFinite(c.value[i])||(i>0&&c.time[i]<=c.time[i-1]))throw new ArgumentException("Invalid authored curve keys.");
            }
            return d;
            bool ValidHint(Vector3 hint)=>float.IsFinite(hint.x)&&float.IsFinite(hint.y)&&float.IsFinite(hint.z)&&hint.sqrMagnitude>.01f&&hint.sqrMagnitude<4;
        }
        public SkiMotionState Select(LocomotionInput i,ExertionInput e)
        {
            if(!i.Travelling||(i.Speed<slow_speed_mps&&Mathf.Abs(i.LateralSpeed)<slow_speed_mps))return SkiMotionState.SlowStop;
            if(i.Tuck)return SkiMotionState.Tuck;
            // Turning is an overlay on propulsion, never a reason to coast uphill.
            if(i.Grade>steep_grade)return SkiMotionState.SteepUphill;
            if(i.Grade>uphill_grade)return SkiMotionState.Uphill;
            if(i.Sprint||i.Effort>sprint_effort)return SkiMotionState.MaxSprint;
            if(e.Fatigue>fatigue_threshold)return SkiMotionState.Fatigued;
            if(i.Effort<no_pole_effort)return SkiMotionState.NoPole;
            if(Mathf.Abs(i.Curvature)>corner_curvature)return i.Curvature<0?SkiMotionState.CornerLeft:SkiMotionState.CornerRight;
            if(i.Grade<-uphill_grade)return SkiMotionState.DoublePole;
            return i.Speed>fast_speed_mps?SkiMotionState.FastSkate:SkiMotionState.Flat;
        }
    }

    /// <summary>Original authored cyclic paths plus read-only cadence, phase blending and analytic IK.
    /// This is not motion capture. It never moves the race root or alters physics/shooting.</summary>
    public sealed class AthleteHybridLocomotionDriver : AthletePoseDriver
    {
        public bool UseOriginalMotion;
        public bool Active {get;private set;}
        public SkiMotionState MotionState {get;private set;}
        public SkiLocomotion ContactState {get;private set;}
        public double Cycle {get;private set;}
        public double PoleCycle {get;private set;}
        public float CadenceHz {get;private set;}
        public float LeftEdgeDegrees {get;private set;}
        public float RightEdgeDegrees {get;private set;}
        public bool PoleDriveActive=>Active&&blended.poles_per_cycle>.05f&&(float)PoleCycle>=settings.pole_drive_start&&(float)PoleCycle<=settings.pole_drive_end;
        public float PoleRecoveryClearance=>Active?settings.curves[4].At((float)PoleCycle):0;
        private AthleteLocomotionDriver fallback;
        private SkiMotionSettings settings;
        private readonly LocomotionPose pose=new LocomotionPose();
        private readonly SkiMotionProfile start=new SkiMotionProfile(),blended=new SkiMotionProfile();
        private double previousTime=-1,transitionAt;
        private string athleteId;
        private float athletePhase;
        public override void Apply(AthleteRigBinding rig,AthleteAnimationFrame frame)
        {
            var a=frame.Athlete;if(athleteId!=a.AthleteId){previousTime=-1;athleteId=a.AthleteId;athletePhase=StablePhase(athleteId);}
            Sample(rig,new LocomotionInput((float)a.SpeedMetersPerSecond,(float)frame.PresentationGrade,(float)a.Effort.DeliveredRelativeEffort,
                a.TuckActive,a.FinalSprintActive,a.OnCourse&&!a.ShootingActive&&a.Phase!=CompetitionPhase.Finished,
                (float)frame.PresentationCurvature,(float)frame.LateralMetersPerSecond),frame.TimeSeconds,ExertionInput.From(a));
        }
        public void Sample(AthleteRigBinding rig,LocomotionInput input,double time,ExertionInput? observed=null)
        {
            if(!double.IsFinite(time)||time<0)throw new ArgumentOutOfRangeException(nameof(time));
            if(fallback==null)fallback=GetComponent<AthleteLocomotionDriver>();
            if(settings==null)settings=SkiMotionSettings.Load();
            // The same exertion/tuck envelope and lateral bypass remain authoritative inputs to presentation.
            fallback.Sample(rig,input,time,observed,renderPose:UseOriginalMotion,preserveForwardPoling:!UseOriginalMotion);
            Active=!UseOriginalMotion;
            if(!Active){previousTime=-1;LeftEdgeDegrees=RightEdgeDegrees=0;return;}
            fallback.CopyVisualPose(pose);
            var e=observed??new ExertionInput(0,1,0);var selected=settings.Select(input,e);
            double dt=previousTime<0?0:Math.Max(0,time-previousTime);
            bool reset=previousTime<0||time<previousTime||dt>1;
            float targetCadence=Mathf.Clamp(pose.cadence_hz*Mathf.Sqrt(Mathf.Max(0,input.Speed)/settings.speed_reference_mps),settings.min_cadence_hz,settings.max_cadence_hz);
            if(reset)
            {
                MotionState=selected;transitionAt=time-settings.transition_s;
                SkiMotionProfile.Blend(settings.profiles[(int)selected],settings.profiles[(int)selected],1,blended);
                SkiMotionProfile.Blend(blended,blended,1,start);
                CadenceHz=BoundCadence();Cycle=(time*CadenceHz+athletePhase)%1;PoleCycle=(time*CadenceHz*blended.poles_per_cycle+athletePhase)%1;dt=0;
            }
            else if(selected!=MotionState){SkiMotionProfile.Blend(blended,blended,1,start);MotionState=selected;transitionAt=time;}
            float blend=SkiMotionCurve.Ease((float)((time-transitionAt)/settings.transition_s));
            SkiMotionProfile.Blend(start,settings.profiles[(int)MotionState],blend,blended);
            CadenceHz=Mathf.Lerp(CadenceHz,BoundCadence(),1-Mathf.Exp(-(float)dt/settings.phase_response_s));
            Cycle=(Cycle+dt*CadenceHz)%1;PoleCycle=(PoleCycle+dt*CadenceHz*blended.poles_per_cycle)%1;previousTime=time;
            ContactState=blended.ready_weight>.95f?SkiLocomotion.Idle:
                blended.poles_per_cycle<.05f&&MotionState==SkiMotionState.NoPole?SkiLocomotion.TechnicalDescent:fallback.State;
            Pose(rig);
            float BoundCadence()=>Mathf.Min(targetCadence*blended.cadence_scale,settings.max_pole_cadence_hz/Mathf.Max(1,blended.poles_per_cycle));
        }
        private void Pose(AthleteRigBinding rig)
        {
            rig.ResetPose();float phase=(float)Cycle,pole=(float)PoleCycle;
            float ready=blended.ready_weight*(1-fallback.FinishWeight);
            float activity=Mathf.Clamp01(pose.arm_swing_m/.2f);
            float transfer=settings.curves[0].At(phase)*blended.hip_transfer_m*activity;
            float compression=settings.curves[2].At(pole)*blended.arm_scale*activity;
            rig.Offset(AthleteBone.Hips,new Vector3(transfer,-pose.hip_drop_m-settings.hip_compression_m*compression,-pose.hip_back_m));
            float lean=pose.torso_lean_deg+blended.compression_deg*compression,hinge=lean*pose.hip_hinge_fraction;
            float twist=settings.curves[0].At(phase)*blended.torso_twist_deg*activity;
            rig.Rotate(AthleteBone.Hips,Quaternion.Euler(hinge,twist*.25f,-fallback.CornerLeanDegrees));
            rig.Rotate(AthleteBone.Spine,Quaternion.Euler((lean-hinge)*.6f,twist*.5f,0));
            rig.Rotate(AthleteBone.Chest,Quaternion.Euler((lean-hinge)*.4f,twist*.25f,0));
            rig.Rotate(AthleteBone.Neck,Quaternion.Euler(-pose.torso_lean_deg*.7f,-twist,0));
            Leg(false,phase);Leg(true,Mathf.Repeat(phase+.5f,1));Arm(false);Arm(true);
            void Leg(bool right,float at)
            {
                float side=right?1:-1;
                float recovery=Mathf.Clamp01((at-settings.glide_fraction)/(1-settings.glide_fraction));
                float stroke=at<settings.glide_fraction?SkiMotionCurve.Ease(at/settings.glide_fraction):1-SkiMotionCurve.Ease(recovery);
                float lift=Mathf.Sin(recovery*Mathf.PI);lift*=lift;
                float stride=pose.stride_m*blended.stride_scale,width=pose.skate_width_m*blended.width_scale;
                var target=new Vector3(side*(.105f+width*stroke),settings.ankle_height_m+lift*pose.foot_lift_m*blended.lift_scale,(1-2*stroke)*stride);
                // Preserve the existing recovery/bypass step when travel is mostly lateral.
                float sideWeight=fallback.SidestepWeight;
                target.x=Mathf.Lerp(target.x,side*.18f+Mathf.Sign(transfer)*.09f*(.5f-stroke),sideWeight);
                var upper=rig.Bone(right?AthleteBone.RightUpperLeg:AthleteBone.LeftUpperLeg);
                var lower=rig.Bone(right?AthleteBone.RightLowerLeg:AthleteBone.LeftLowerLeg);
                var foot=rig.Bone(right?AthleteBone.RightFoot:AthleteBone.LeftFoot);
                AthleteLocomotionDriver.SolveTwoBone(upper,lower,foot,rig.ModelRoot.TransformPoint(target),rig.ModelRoot.forward,rig.ModelRoot.rotation);
                float yaw=side*settings.yaw_deg*Mathf.Clamp01(width/.15f)*Mathf.Lerp(1,settings.recovery_yaw_fraction,lift);
                foot.rotation=rig.ModelRoot.rotation*Quaternion.Euler(0,yaw,0);
                float edge=-side*blended.ski_edge_deg*(1-lift)*Mathf.Sin(stroke*Mathf.PI);
                if(right)RightEdgeDegrees=edge;else LeftEdgeDegrees=edge;
            }
            void Arm(bool right)
            {
                float side=right?1:-1;
                float stroke=settings.curves[1].At(pole)*pose.arm_swing_m*blended.arm_scale;
                float height=settings.curves[3].At(pole)*pose.arm_swing_m*blended.arm_scale*settings.pole_height_scale;
                var target=new Vector3(side*pose.hand_width_m,pose.hand_height_m+height+side*blended.hand_stagger_m,
                    pose.hand_forward_m+stroke-side*blended.hand_stagger_m);
                target=Vector3.Lerp(target,new Vector3(side*settings.ready_hand_width_m,settings.ready_hand_height_m,settings.ready_hand_forward_m),ready);
                // Carry both grips with the leaning torso. World/root-centred hands
                // made the inside forearm reach across the chest on a tight turn.
                target.x+=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Chest).position).x;
                var upper=rig.Bone(right?AthleteBone.RightUpperArm:AthleteBone.LeftUpperArm);
                var lower=rig.Bone(right?AthleteBone.RightLowerArm:AthleteBone.LeftLowerArm);
                var hand=rig.Bone(right?AthleteBone.RightHand:AthleteBone.LeftHand);
                float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position)-settings.arm_reach_margin_m;
                Vector3 world=upper.position+Vector3.ClampMagnitude(rig.ModelRoot.TransformPoint(target)-upper.position,reach);
                // Leave room for the existing grip socket and bounded pole-anchor correction.
                // Elbows lead the forward recovery/plant; they pass behind the torso
                // during release. A constant backward hint produced a rowing/curl pose.
                Vector3 hint=Vector3.Lerp(settings.elbow_release_hint,settings.elbow_plant_hint,(settings.curves[1].At(pole)+1)*.5f);
                hint=Vector3.Lerp(hint,settings.elbow_plant_hint,ready);
                hint.x*=side;
                hint=Quaternion.Euler(0,0,-fallback.CornerLeanDegrees)*hint;
                AthleteLocomotionDriver.SolveTwoBone(upper,lower,hand,world,rig.ModelRoot.TransformDirection(hint),rig.ModelRoot.rotation);
                hand.rotation=rig.ModelRoot.rotation;
            }
        }
        public void ApplySkiEdges(AthleteRigBinding rig)
        {
            if(!Active)return;
            rig.Bone(AthleteBone.LeftFoot).rotation*=Quaternion.Euler(0,0,LeftEdgeDegrees);
            rig.Bone(AthleteBone.RightFoot).rotation*=Quaternion.Euler(0,0,RightEdgeDegrees);
        }
        private static float StablePhase(string id)
        {
            // Visual desynchronization only. Stable IDs give replayable phases without
            // random state, allocations, or a clock offset shared by the whole field.
            uint hash=2166136261;unchecked{for(int i=0;i<id.Length;i++)hash=(hash^id[i])*16777619;}
            return (hash&1023)/1024f;
        }
    }
}
