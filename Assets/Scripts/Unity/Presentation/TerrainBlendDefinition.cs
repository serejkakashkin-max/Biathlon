using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class TerrainBlendDefinition
    {
        public int schema_version;public string id,snow_preset;
        public float edge_width_m,edge_contribution,groom_spacing_m;
        public Color packed_color,ice_color,edge_color;
        public Vector3 layer_roughness,layer_normals;
        public static TerrainBlendDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/terrain_blend_v1").text);
        public static TerrainBlendDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<TerrainBlendDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||(d.snow_preset!="cold"&&d.snow_preset!="wet"))throw new ArgumentException("Invalid terrain blend schema.");
            Check(d.edge_width_m,.1f,3);Check(d.edge_contribution,0,1);Check(d.groom_spacing_m,.01f,.2f);
            foreach(var c in new[]{d.packed_color,d.ice_color,d.edge_color}){for(int i=0;i<3;i++)Check(c[i],0,1);if(c.a!=1)throw new ArgumentException("Opaque terrain required.");}
            for(int i=0;i<3;i++){Check(d.layer_roughness[i],.08f,1);Check(d.layer_normals[i],0,1);}return d;
        }
        private static void Check(float v,float min,float max){if(!float.IsFinite(v)||v<min||v>max)throw new ArgumentException("Invalid terrain blend range.");}
        // RGBA = loose snow, packed snow, ice, churned edge. Caller supplies an explicit visual ice mask.
        public Color Weights(float distance,float routeHalfWidth,float iceMask)
        {
            Check(distance,0,1000000);Check(routeHalfWidth,edge_width_m,100);Check(iceMask,0,1);
            float t=Mathf.Clamp01((distance-routeHalfWidth+edge_width_m)/(2*edge_width_m));
            float inside=1-t*t*(3-2*t),edge=4*inside*(1-inside)*edge_contribution;
            float packed=inside*(1-edge);return new Color((1-inside)*(1-edge),packed*(1-iceMask),packed*iceMask,edge);
        }
        public void Configure(Material material)
        {
            SnowMaterialDefinition.Load().Configure(material,snow_preset);material.SetFloat("_TerrainBlend",1);
            material.SetColor("_PackedColor",packed_color);material.SetColor("_IceColor",ice_color);material.SetColor("_EdgeColor",edge_color);
            material.SetVector("_LayerRoughness",layer_roughness);material.SetVector("_LayerNormals",layer_normals);material.SetFloat("_GroomSpacing",groom_spacing_m);
        }
    }
}
