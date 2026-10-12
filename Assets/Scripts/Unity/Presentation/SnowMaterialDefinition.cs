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
        /// <summary>Keep authored snow/ice/edge weights and texture detail while changing conditions.</summary>
        public void ConfigureBlended(Material material,string preset,string surfaceProfile=null)
        {
            if(material==null)throw new ArgumentNullException(nameof(material));
            float blend=material.GetFloat("_TerrainBlend");
            Color packed=material.GetColor("_PackedColor"),ice=material.GetColor("_IceColor"),edge=material.GetColor("_EdgeColor");
            Vector4 roughness=material.GetVector("_LayerRoughness");
            SnowMaterialPreset cold=null,chosen=null;foreach(var p in presets){if(p.id=="cold")cold=p;if(p.id==preset)chosen=p;}
            Configure(material,preset);
            if(preset!="cold")
            {
                var tint=new Color(cold.albedo.r>0?chosen.albedo.r/cold.albedo.r:1,cold.albedo.g>0?chosen.albedo.g/cold.albedo.g:1,cold.albedo.b>0?chosen.albedo.b/cold.albedo.b:1,1);
                material.SetColor("_PackedColor",packed*tint);material.SetColor("_IceColor",ice*tint);material.SetColor("_EdgeColor",edge*tint);
                material.SetVector("_LayerRoughness",roughness*(chosen.roughness/cold.roughness));
            }
            // Reuse the authored loose/ice layers on the groomed surface. No new
            // meshes, textures or per-frame shader work are required.
            if(surfaceProfile=="fresh_soft"||surfaceProfile=="icy_fast")
            {
                Vector4 layers=material.GetVector("_LayerRoughness"),normals=material.GetVector("_LayerNormals");
                bool fresh=surfaceProfile=="fresh_soft";
                material.SetColor("_PackedColor",material.GetColor(fresh?"_BaseColor":"_IceColor"));
                layers.x=fresh?material.GetFloat("_Roughness"):layers.y;normals.x=fresh?material.GetFloat("_NormalStrength"):normals.y;
                material.SetVector("_LayerRoughness",layers);material.SetVector("_LayerNormals",normals);
            }
            material.SetFloat("_TerrainBlend",blend);
        }
    }
}
