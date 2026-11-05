using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class AthleteCornerPoseSettings
    {
        public int schema_version;public string id;
        public float maximum_bank_degrees,hips_bank_fraction,spine_bank_fraction,pelvis_transfer_meters,pelvis_drop_meters;
        private static AthleteCornerPoseSettings cached;
        public static AthleteCornerPoseSettings Load()=>cached??(cached=FromJson(Resources.Load<TextAsset>("Biathlon/athlete_corner_pose_v1").text));
        public static AthleteCornerPoseSettings FromJson(string json)
        {
            var s=JsonUtility.FromJson<AthleteCornerPoseSettings>(json);
            if(s==null||s.schema_version!=1||string.IsNullOrWhiteSpace(s.id)||
                !(s.maximum_bank_degrees>0&&s.maximum_bank_degrees<=20&&s.hips_bank_fraction>0&&s.hips_bank_fraction<1&&
                s.spine_bank_fraction>0&&s.spine_bank_fraction<1-s.hips_bank_fraction&&s.pelvis_transfer_meters>0&&s.pelvis_transfer_meters<=.10f&&
                s.pelvis_drop_meters>=0&&s.pelvis_drop_meters<=.03f))throw new ArgumentException("Invalid visual turn balance definition.");
            return s;
        }
    }
    // Read-only pose intent. Transfer the pelvis before leg IK, leaving the
    // skis on their authored support path and the authoritative root untouched.
    public static class AthleteCornerPose
    {
        public static float Bank(float requested)=>Mathf.Clamp(requested,-AthleteCornerPoseSettings.Load().maximum_bank_degrees,AthleteCornerPoseSettings.Load().maximum_bank_degrees);
        public static Vector3 PelvisOffset(float requested)
        {
            var s=AthleteCornerPoseSettings.Load();float weight=Bank(requested)/s.maximum_bank_degrees;
            return new Vector3(weight*s.pelvis_transfer_meters,-Mathf.Abs(weight)*s.pelvis_drop_meters,0);
        }
        public static void Apply(AthleteRigBinding rig,float requested)
        {
            var s=AthleteCornerPoseSettings.Load();float bank=-Bank(requested);
            rig.Bone(AthleteBone.Hips).localRotation*=Quaternion.AngleAxis(bank*s.hips_bank_fraction,Vector3.forward);
            rig.Bone(AthleteBone.Spine).localRotation*=Quaternion.AngleAxis(bank*s.spine_bank_fraction,Vector3.forward);
            rig.Bone(AthleteBone.Chest).localRotation*=Quaternion.AngleAxis(bank*(1-s.hips_bank_fraction-s.spine_bank_fraction),Vector3.forward);
        }
    }
}
