using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RenderQualityTier
    {
        public string id;
        public string display_name;
        public float render_scale, shadow_distance_m;
        public int shadow_resolution, shadow_cascades;
        public bool soft_shadows, post_processing;
    }
    [Serializable] public sealed class RenderQualityDefinition
    {
        public int schema_version;
        public string id, default_tier;
        public int target_frame_rate;
        public RenderQualityTier[] tiers;
        public static RenderQualityDefinition Load() => FromJson(Resources.Load<TextAsset>("Biathlon/render_quality_v1").text);
        public static RenderQualityDefinition FromJson(string json)
        {
            var d = JsonUtility.FromJson<RenderQualityDefinition>(json);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) || d.tiers == null || d.tiers.Length != 3 || d.target_frame_rate<30 || d.target_frame_rate>240)
                throw new ArgumentException("Invalid render quality definition.");
            for (int i = 0; i < d.tiers.Length; i++)
            {
                var t = d.tiers[i];
                if (t == null || (t.id != "low" && t.id != "balanced" && t.id != "high") ||
                    !float.IsFinite(t.render_scale) || t.render_scale < .5f || t.render_scale > 2 ||
                    !float.IsFinite(t.shadow_distance_m) || t.shadow_distance_m <= 0 || t.shadow_distance_m > 500 ||
                    (t.shadow_resolution != 1024 && t.shadow_resolution != 2048 && t.shadow_resolution != 4096) ||
                    t.shadow_cascades < 1 || t.shadow_cascades > 4) throw new ArgumentException("Invalid render quality tier.");
                for (int j = 0; j < i; j++) if (d.tiers[j].id == t.id) throw new ArgumentException("Duplicate render tier.");
            }
            d.Get(d.default_tier); return d;
        }
        public RenderQualityTier Get(string tier)
        {
            foreach (var t in tiers) if (t.id == tier) return t;
            throw new ArgumentException("Unknown render tier: " + tier);
        }
        public static string DisplayName(string id)=>id=="low"?"Performance":id=="balanced"?"High":id=="high"?"Ultra":throw new ArgumentException("Unknown tier.");
    }
}
