using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RaceReviewSettings
    {
        public int schema_version,finish_max_steps;
        public string id;
        public float checkpoint_notice_seconds,calm_hr_load,high_hr_load,range_guidance_m,finish_work_budget_ms;
        public float checkpoint_approach_m;
        public float spectator_distance_m,spectator_height_m,overhead_height_m,spectator_side_m,spectator_response;
        public float checkpoint_post_height_m,checkpoint_post_offset_m;
        public static RaceReviewSettings Load()
        {
            var d=JsonUtility.FromJson<RaceReviewSettings>(ProductionVenueHost.ReadResource("Biathlon/UI/race_review_v1"));
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.calm_hr_load<=0||d.high_hr_load<=d.calm_hr_load||d.high_hr_load>1||d.finish_max_steps<1||d.finish_max_steps>1000)throw new ArgumentException("Invalid race review presentation.");
            foreach(float value in new[]{d.checkpoint_notice_seconds,d.checkpoint_approach_m,d.range_guidance_m,d.finish_work_budget_ms,d.spectator_distance_m,d.spectator_height_m,d.overhead_height_m,d.spectator_side_m,d.spectator_response,d.checkpoint_post_height_m,d.checkpoint_post_offset_m})
                if(!float.IsFinite(value)||value<=0)throw new ArgumentException("Positive race review settings required.");
            return d;
        }
    }
}
