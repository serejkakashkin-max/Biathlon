using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class ReplayPresentationSettings
    {
        public int schema_version,seek_step_limit,highlight_cooldown_seconds,tv_rows;
        public string id;
        public float work_budget_ms,seek_seconds,highlight_lead_seconds,close_finish_seconds,tv_refresh_seconds,shot_hold_seconds;
        public static ReplayPresentationSettings Load()
        {
            var d=JsonUtility.FromJson<ReplayPresentationSettings>(ProductionVenueHost.ReadResource("Biathlon/UI/replay_presentation_v1"));
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.seek_step_limit<1||d.seek_step_limit>500||d.tv_rows<1||d.tv_rows>10||
                d.work_budget_ms<1||d.work_budget_ms>30||d.seek_seconds<1||d.highlight_lead_seconds<0||d.close_finish_seconds<0||d.tv_refresh_seconds<.1f||d.shot_hold_seconds<1||d.highlight_cooldown_seconds<0)
                throw new ArgumentException("Invalid replay presentation policy.");return d;
        }
    }
}
