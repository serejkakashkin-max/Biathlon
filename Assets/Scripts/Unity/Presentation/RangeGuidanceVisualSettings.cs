using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RangeGuidanceVisualSettings
    {
        public int schema_version;
        public string id;
        public float corridor_extra_clearance_m,mat_edge_clearance_m,turn_lead_m,final_spacing_m;
        public static RangeGuidanceVisualSettings Load()
        {
            var d=JsonUtility.FromJson<RangeGuidanceVisualSettings>(Resources.Load<TextAsset>("Biathlon/range_guidance_visual_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||
                !float.IsFinite(d.corridor_extra_clearance_m)||d.corridor_extra_clearance_m<0||d.corridor_extra_clearance_m>3||
                !float.IsFinite(d.mat_edge_clearance_m)||d.mat_edge_clearance_m<.1f||d.mat_edge_clearance_m>1||
                !float.IsFinite(d.turn_lead_m)||d.turn_lead_m<5||d.turn_lead_m>30||
                !float.IsFinite(d.final_spacing_m)||d.final_spacing_m<.5f||d.final_spacing_m>1)
                throw new ArgumentException("Invalid presentation-only mat guidance corridor.");
            return d;
        }
    }
}
