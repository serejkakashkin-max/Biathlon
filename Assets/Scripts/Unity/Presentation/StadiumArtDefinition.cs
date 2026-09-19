using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class StadiumArtDefinition
    {
        public int schema_version,lane_count,stand_rows;public string id;
        public float lane_pitch_m,target_height_m,mat_width_m,mat_length_m,barrier_height_m,barrier_module_m,finish_width_m,finish_height_m,stand_width_m,stand_row_depth_m,stand_rise_m;
        public static StadiumArtDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/stadium_art_v1").text);
        public static StadiumArtDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<StadiumArtDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.lane_count<1||d.lane_count>30||d.stand_rows<1||d.stand_rows>12)throw new ArgumentException("Invalid stadium art schema/budget.");
            Bound(d.lane_pitch_m,2,4);Bound(d.target_height_m,.5f,1.5f);Bound(d.mat_width_m,1,d.lane_pitch_m-.3f);Bound(d.mat_length_m,1.5f,3);
            Bound(d.barrier_height_m,.8f,1.5f);Bound(d.barrier_module_m,1,3);Bound(d.finish_width_m,6,14);Bound(d.finish_height_m,3.5f,6);
            Bound(d.stand_width_m,8,30);Bound(d.stand_row_depth_m,.6f,1.2f);Bound(d.stand_rise_m,.2f,.5f);return d;
        }
        public float LaneX(int lane){if(lane<0||lane>=lane_count)throw new ArgumentOutOfRangeException(nameof(lane));return (lane-(lane_count-1)*.5f)*lane_pitch_m;}
        private static void Bound(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid stadium art dimensions.");}
    }
}
