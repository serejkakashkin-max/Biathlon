using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class SkiTechniqueVisualSettings
    {
        public int schema_version;public string id;
        public float hip_transfer_fraction,hip_pulse_m,torso_pulse_deg,flat_poles_per_cycle,climb_poles_per_cycle,motion_reference_swing_m;
        public float contact_max_grip_correction_m,contact_reset_gap_s,contact_reset_distance_m;
        public float glide_fraction,recovery_yaw_fraction;
        public static SkiTechniqueVisualSettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/ski_technique_visual_v1").text);
        public static SkiTechniqueVisualSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<SkiTechniqueVisualSettings>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id))throw new ArgumentException("Invalid ski presentation definition.");
            Check(d.hip_transfer_fraction,0,1);Check(d.hip_pulse_m,0,.05f);Check(d.torso_pulse_deg,0,8);
            Check(d.flat_poles_per_cycle,1,2);Check(d.climb_poles_per_cycle,1,2);Check(d.motion_reference_swing_m,.1f,.5f);
            Check(d.contact_max_grip_correction_m,.01f,.15f);Check(d.contact_reset_gap_s,.05f,.3f);Check(d.contact_reset_distance_m,.5f,3);
            Check(d.glide_fraction,.55f,.8f);Check(d.recovery_yaw_fraction,0,1);
            return d;
        }
        private static void Check(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Ski presentation exceeds envelope.");}
    }
}
