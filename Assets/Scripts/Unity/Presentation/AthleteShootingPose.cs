using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class ShootingPoseSettings
    {
        public int schema_version;
        public string id;
        public float lower_start, kneel_at, kneel_hip_height_m, prone_hip_height_m;
        public float prone_hip_pitch_deg, prone_spine_pitch_deg, prone_chest_pitch_deg, prone_neck_pitch_deg;
        public Vector3 kneel_left_foot, prone_left_foot, prone_rifle_butt;
        public float standing_start, standing_hip_height_m, standing_yaw_deg;
        public Vector3 standing_left_foot, standing_right_foot, standing_rifle_butt;
        public static ShootingPoseSettings Load()
        {
            var d=JsonUtility.FromJson<ShootingPoseSettings>(Resources.Load<TextAsset>("Biathlon/athlete_shooting_pose_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.lower_start>0&&d.lower_start<d.kneel_at&&d.kneel_at<1)||
                !(d.prone_hip_height_m>0&&d.prone_hip_height_m<d.kneel_hip_height_m&&d.kneel_hip_height_m<1))
                throw new ArgumentException("Invalid shooting presentation pose.");
            if(!(d.standing_start>0&&d.standing_start<1)||!(d.standing_hip_height_m>.5f&&d.standing_hip_height_m<1.2f)||!(d.standing_yaw_deg>=0&&d.standing_yaw_deg<=90))
                throw new ArgumentException("Invalid standing pose envelope.");
            foreach(float angle in new[]{d.prone_hip_pitch_deg,d.prone_spine_pitch_deg,d.prone_chest_pitch_deg,d.prone_neck_pitch_deg})
                if(!float.IsFinite(angle)||Mathf.Abs(angle)>120)throw new ArgumentException("Invalid pose angle.");
            foreach(var v in new[]{d.kneel_left_foot,d.prone_left_foot,d.prone_rifle_butt,d.standing_left_foot,d.standing_right_foot,d.standing_rifle_butt})
                if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))throw new ArgumentException("Invalid pose target.");
            return d;
        }
    }

    /// <summary>Read-only body transition. Neither camera nor shot resolver reads these transforms.</summary>
    public sealed class AthleteShootingPose : MonoBehaviour
    {
        private ShootingPoseSettings settings;
        private bool initialized;
        private Vector3 readyPosition;
        private Quaternion readyRotation;
        public float ProneWeight { get; private set; }
        public void Render(AthleteRigBinding rig,AthleteAnimationFrame frame)
        {
            var a=frame.Athlete;
            Sample(rig,a.Stance,AthleteRiflePresentation.Progress(a.ShootingPhase,a.ShootingPhaseElapsedTicks,a.ShootingPhaseDurationTicks));
        }
        // Called after locomotion/baseline animation, before rifle/hand alignment.
        public void Sample(AthleteRigBinding rig,ShootingStance stance,float progress)
        {
            if(!float.IsFinite(progress)||progress<0||progress>1)throw new ArgumentOutOfRangeException(nameof(progress));
            if(settings==null)settings=ShootingPoseSettings.Load();
            var ready=rig.Socket(AthleteSocket.RifleAim);
            if(!initialized){readyPosition=ready.localPosition;readyRotation=ready.localRotation;initialized=true;}
            ready.SetLocalPositionAndRotation(readyPosition,readyRotation);
            ProneWeight=stance==ShootingStance.Prone?Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.kneel_at,1,progress)):0;
            if(stance==ShootingStance.Standing){Standing(rig,ready,progress);return;}
            if(stance!=ShootingStance.Prone||progress<=settings.lower_start)return;
            float lower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.lower_start,settings.kneel_at,progress));
            var left=rig.Bone(AthleteBone.LeftFoot);var right=rig.Bone(AthleteBone.RightFoot);
            var leftAt=rig.ModelRoot.InverseTransformPoint(left.position);var rightAt=rig.ModelRoot.InverseTransformPoint(right.position);
            var hips=rig.Bone(AthleteBone.Hips);var at=rig.ModelRoot.InverseTransformPoint(hips.position);
            at.y=Mathf.Lerp(Mathf.Lerp(at.y,settings.kneel_hip_height_m,lower),settings.prone_hip_height_m,ProneWeight);
            hips.position=rig.ModelRoot.TransformPoint(at);
            rig.Rotate(AthleteBone.Hips,Quaternion.Euler(settings.prone_hip_pitch_deg*ProneWeight,0,0));
            rig.Rotate(AthleteBone.Spine,Quaternion.Euler(settings.prone_spine_pitch_deg*ProneWeight,0,0));
            rig.Rotate(AthleteBone.Chest,Quaternion.Euler(settings.prone_chest_pitch_deg*ProneWeight,0,0));
            rig.Rotate(AthleteBone.Neck,Quaternion.Euler(settings.prone_neck_pitch_deg*ProneWeight,0,0));
            Leg(false,leftAt);Leg(true,rightAt);
            ready.SetPositionAndRotation(Vector3.Lerp(ready.position,rig.ModelRoot.TransformPoint(settings.prone_rifle_butt),ProneWeight),
                rig.ModelRoot.rotation);

            void Leg(bool isRight,Vector3 baseline)
            {
                var kneel=settings.kneel_left_foot;var prone=settings.prone_left_foot;
                if(isRight){kneel.x=-kneel.x;prone.x=-prone.x;}
                var target=Vector3.Lerp(Vector3.Lerp(baseline,kneel,lower),prone,ProneWeight);
                var upper=rig.Bone(isRight?AthleteBone.RightUpperLeg:AthleteBone.LeftUpperLeg);
                var knee=rig.Bone(isRight?AthleteBone.RightLowerLeg:AthleteBone.LeftLowerLeg);
                var foot=rig.Bone(isRight?AthleteBone.RightFoot:AthleteBone.LeftFoot);
                // Open knees outward as the body lowers; a downward bend plane drives
                // the kneecaps through the ground during the kneel-to-prone transition.
                var hint=Vector3.Slerp(rig.ModelRoot.forward,rig.ModelRoot.right*(isRight?1:-1),ProneWeight);
                AthleteLocomotionDriver.SolveTwoBone(upper,knee,foot,rig.ModelRoot.TransformPoint(target),hint,rig.ModelRoot.rotation);
                foot.rotation=rig.ModelRoot.rotation;
            }
        }

        private void Standing(AthleteRigBinding rig,Transform ready,float progress)
        {
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.standing_start,1,progress));
            if(weight<=0)return;
            var left=rig.Bone(AthleteBone.LeftFoot);var right=rig.Bone(AthleteBone.RightFoot);
            var leftAt=left.position;var rightAt=right.position;
            var hips=rig.Bone(AthleteBone.Hips);var at=rig.ModelRoot.InverseTransformPoint(hips.position);
            at.y=Mathf.Lerp(at.y,settings.standing_hip_height_m,weight);hips.position=rig.ModelRoot.TransformPoint(at);
            rig.Rotate(AthleteBone.Hips,Quaternion.Euler(0,settings.standing_yaw_deg*weight,0));
            rig.Rotate(AthleteBone.Neck,Quaternion.Euler(0,-settings.standing_yaw_deg*weight,0));
            Leg(false,leftAt,settings.standing_left_foot);Leg(true,rightAt,settings.standing_right_foot);
            ready.SetPositionAndRotation(Vector3.Lerp(ready.position,rig.ModelRoot.TransformPoint(settings.standing_rifle_butt),weight),rig.ModelRoot.rotation);
            void Leg(bool isRight,Vector3 baseline,Vector3 target)
            {
                var upper=rig.Bone(isRight?AthleteBone.RightUpperLeg:AthleteBone.LeftUpperLeg);
                var knee=rig.Bone(isRight?AthleteBone.RightLowerLeg:AthleteBone.LeftLowerLeg);
                var foot=rig.Bone(isRight?AthleteBone.RightFoot:AthleteBone.LeftFoot);
                AthleteLocomotionDriver.SolveTwoBone(upper,knee,foot,Vector3.Lerp(baseline,rig.ModelRoot.TransformPoint(target),weight),rig.ModelRoot.forward,rig.ModelRoot.rotation);
                foot.rotation=Quaternion.Slerp(foot.rotation,rig.ModelRoot.rotation,weight);
            }
        }
    }
}
