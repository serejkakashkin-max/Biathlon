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
        public static ShootingPoseSettings Load()
        {
            var d=JsonUtility.FromJson<ShootingPoseSettings>(Resources.Load<TextAsset>("Biathlon/athlete_shooting_pose_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.lower_start>0&&d.lower_start<d.kneel_at&&d.kneel_at<1)||
                !(d.prone_hip_height_m>0&&d.prone_hip_height_m<d.kneel_hip_height_m&&d.kneel_hip_height_m<1))
                throw new ArgumentException("Invalid shooting presentation pose.");
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
    }
}
