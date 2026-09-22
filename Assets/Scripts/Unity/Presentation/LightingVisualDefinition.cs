using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class LightingVisualPreset
    {
        public string id,recommended_weather;public float sun_elevation,sun_azimuth,sun_gain,ambient_gain;public Color sun_tint,ambient_tint,sky_tint,horizon_tint;
        public float ambient_equator_fraction=.65f,ambient_ground_fraction=.35f;
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(id)||(recommended_weather!="clear"&&recommended_weather!="overcast"&&recommended_weather!="snowfall"))throw new ArgumentException("Invalid visual lighting identity.");
            Bound(sun_elevation,5,75);Bound(sun_azimuth,-180,180);Bound(sun_gain,0,2);Bound(ambient_gain,.4f,1.5f);Tint(sun_tint);Tint(ambient_tint);Tint(sky_tint);Tint(horizon_tint);
            Bound(ambient_equator_fraction,0,1);Bound(ambient_ground_fraction,0,1);
        }
        private static void Bound(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid lighting range.");}
        private static void Tint(Color c){for(int i=0;i<3;i++)Bound(c[i],.1f,1);if(c.a!=1)throw new ArgumentException("Opaque lighting tint required.");}
    }
    [Serializable] public sealed class LightingVisualDefinition
    {
        public int schema_version;public string id;public LightingVisualPreset[] presets;
        public static LightingVisualDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/lighting_visual_v1").text);
        public static LightingVisualDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<LightingVisualDefinition>(json);if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.presets==null||d.presets.Length!=4)throw new ArgumentException("Invalid lighting schema.");
            for(int i=0;i<d.presets.Length;i++){if(d.presets[i]==null)throw new ArgumentException("Missing lighting preset.");d.presets[i].Validate();for(int j=0;j<i;j++)if(d.presets[j].id==d.presets[i].id)throw new ArgumentException("Duplicate lighting preset.");}
            d.Get("clear_cold");d.Get("overcast");d.Get("dawn");d.Get("late_day");return d;
        }
        public LightingVisualPreset Get(string id){foreach(var p in presets)if(p.id==id)return p;throw new ArgumentException("Unknown lighting preset.");}
    }
}
