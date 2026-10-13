using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class JointVolumeSettings
    {
        public int schema_version;
        public string id;
        public float start_angle_deg, full_angle_deg, arm_radius_m, leg_radius_m, arm_expansion_m, leg_expansion_m;
        public static JointVolumeSettings Load()
        {
            var d=JsonUtility.FromJson<JointVolumeSettings>(Resources.Load<TextAsset>("Biathlon/joint_volume_visual_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||
                !(d.start_angle_deg>=0&&d.full_angle_deg>d.start_angle_deg&&d.full_angle_deg<=160)||
                !(d.arm_radius_m>0&&d.arm_radius_m<.2f&&d.leg_radius_m>0&&d.leg_radius_m<.2f)||
                !(d.arm_expansion_m>0&&d.arm_expansion_m<.025f&&d.leg_expansion_m>0&&d.leg_expansion_m<.025f))
                throw new ArgumentException("Invalid joint volume definition.");
            return d;
        }
    }
    /// <summary>Bounded mesh correction for LBS volume loss. Never changes bones or prop sockets.</summary>
    public sealed class AthleteJointVolume : MonoBehaviour
    {
        private SkinnedMeshRenderer skin;
        private JointVolumeSettings settings;
        private Mesh validatedMesh;
        private static readonly string[] shapeNames={"LeftLowerArmVolume","RightLowerArmVolume","LeftLowerLegVolume","RightLowerLegVolume"};
        private static readonly AthleteBone[] joints={AthleteBone.LeftLowerArm,AthleteBone.RightLowerArm,AthleteBone.LeftLowerLeg,AthleteBone.RightLowerLeg};
        public void Render(AthleteRigBinding rig)
        {
            if(skin==null){skin=GetComponentInChildren<SkinnedMeshRenderer>();settings=JointVolumeSettings.Load();}
            if(validatedMesh!=skin.sharedMesh)
            {
                if(skin.sharedMesh.blendShapeCount<4)throw new InvalidOperationException("Hero joint shapes missing.");
                for(int i=0;i<4;i++)if(skin.sharedMesh.GetBlendShapeName(i)!=shapeNames[i])throw new InvalidOperationException("Hero joint shape ordering differs from the binding.");
                validatedMesh=skin.sharedMesh;
            }
            for(int i=0;i<4;i++)
            {
                var lower=rig.Bone(joints[i]);var upper=rig.Bone((AthleteBone)((int)joints[i]-1));var end=rig.Bone((AthleteBone)((int)joints[i]+1));
                float angle=Vector3.Angle(lower.position-upper.position,end.position-lower.position);
                skin.SetBlendShapeWeight(i,100*Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.start_angle_deg,settings.full_angle_deg,angle)));
            }
        }
    }
}
