using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class SnowMaterialPreset
    {
        public string id;
        public Color albedo;
        public float roughness, roughness_variation, normal_strength, slope_darkening, albedo_variation;
    }
    [Serializable] public sealed class SnowMaterialDefinition
    {
        public int schema_version;
        public string id;
        public float macro_scale_m, grain_scale_m;
        public SnowMaterialPreset[] presets;
        public static SnowMaterialDefinition Load() => FromJson(Resources.Load<TextAsset>("Biathlon/snow_material_v1").text);
        public static SnowMaterialDefinition FromJson(string json)
        {
            var d = JsonUtility.FromJson<SnowMaterialDefinition>(json);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) ||
                !float.IsFinite(d.macro_scale_m) || d.macro_scale_m < .1f || d.macro_scale_m > 20 ||
                !float.IsFinite(d.grain_scale_m) || d.grain_scale_m < .005f || d.grain_scale_m > .2f ||
                d.presets == null || d.presets.Length != 2) throw new ArgumentException("Invalid snow definition.");
            for (int i = 0; i < d.presets.Length; i++)
            {
                var p = d.presets[i];
                if (p == null || (p.id != "cold" && p.id != "wet")) throw new ArgumentException("Invalid snow preset.");
                foreach (float v in new[] {p.albedo.r,p.albedo.g,p.albedo.b,p.roughness,p.roughness_variation,p.normal_strength,p.slope_darkening,p.albedo_variation})
                    if (!float.IsFinite(v) || v < 0 || v > 1) throw new ArgumentException("Snow values must be finite and normalized.");
                if (p.albedo.a != 1 || p.roughness < .08f || p.albedo_variation > .3f || p.slope_darkening > .5f)
                    throw new ArgumentException("Snow preset exceeds material bounds.");
                for (int j = 0; j < i; j++) if (d.presets[j].id == p.id) throw new ArgumentException("Duplicate snow preset.");
            }
            return d;
        }
        public void Configure(Material material, string preset)
        {
            if (material == null || material.shader.name != "Biathlon/Snow") throw new ArgumentException("Snow shader material required.");
            SnowMaterialPreset p = null; foreach (var candidate in presets) if (candidate.id == preset) p = candidate;
            if (p == null) throw new ArgumentException("Unknown snow preset.");
            material.SetColor("_BaseColor", p.albedo); material.SetFloat("_Roughness", p.roughness);
            material.SetFloat("_RoughnessVariation", p.roughness_variation); material.SetFloat("_NormalStrength", p.normal_strength);
            material.SetFloat("_SlopeDarkening", p.slope_darkening); material.SetFloat("_AlbedoVariation", p.albedo_variation);
            material.SetFloat("_MacroScale", macro_scale_m); material.SetFloat("_GrainScale", grain_scale_m);
            material.SetFloat("_TerrainBlend",0);
        }
    }
}
