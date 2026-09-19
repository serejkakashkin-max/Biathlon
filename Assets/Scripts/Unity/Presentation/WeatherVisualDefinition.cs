using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class WeatherVisualPreset
    {
        public string id;public float fog_start_m,fog_end_m,light_intensity,ambient_intensity,cloud_cover,snowfall,shadow_strength;
        public Color fog_color,sky_color,light_color;
    }
    [Serializable] public sealed class WeatherVisualDefinition
    {
        public int schema_version,max_particles;public string id;public float low_fraction,balanced_fraction,volume_width_m,volume_height_m,fall_speed_mps,flake_size_m,flake_opacity;
        public WeatherVisualPreset[] presets;
        public static WeatherVisualDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/weather_visual_v1").text);
        public static WeatherVisualDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<WeatherVisualDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.max_particles<1||d.max_particles>8192||d.presets==null||d.presets.Length!=3)throw new ArgumentException("Invalid weather presentation schema.");
            Bound(d.low_fraction,.05f,1);Bound(d.balanced_fraction,d.low_fraction,1);Bound(d.volume_width_m,8,80);Bound(d.volume_height_m,4,40);Bound(d.fall_speed_mps,.1f,5);Bound(d.flake_size_m,.005f,.06f);Bound(d.flake_opacity,0,.7f);
            for(int i=0;i<d.presets.Length;i++) {var p=d.presets[i];if(p==null||(p.id!="clear"&&p.id!="overcast"&&p.id!="snowfall"))throw new ArgumentException("Invalid weather preset.");for(int j=0;j<i;j++)if(d.presets[j].id==p.id)throw new ArgumentException("Duplicate weather preset.");Bound(p.fog_start_m,0,200);Bound(p.fog_end_m,p.fog_start_m+20,500);Bound(p.light_intensity,0,4);Bound(p.ambient_intensity,.1f,2);Bound(p.cloud_cover,0,1);Bound(p.shadow_strength,0,1);Bound(p.snowfall,0,1);foreach(var c in new[]{p.fog_color,p.sky_color,p.light_color}){for(int k=0;k<3;k++)Bound(c[k],0,1);if(c.a!=1)throw new ArgumentException("Opaque weather colors required.");}}
            return d;
        }
        private static void Bound(float v,float min,float max){if(!float.IsFinite(v)||v<min||v>max)throw new ArgumentException("Weather value out of bounds.");}
        public WeatherVisualPreset Get(string name){foreach(var p in presets)if(p.id==name)return p;throw new ArgumentException("Unknown weather preset.");}
        public int Count(string tier,float snowfall){Bound(snowfall,0,1);float fraction=tier=="low"?low_fraction:tier=="balanced"?balanced_fraction:tier=="high"?1:throw new ArgumentException("Unknown weather tier.");return Mathf.RoundToInt(max_particles*fraction*snowfall);}
        public Vector3 Sample(int index,double time,Vector3 center,Vector2 wind)
        {
            if(index<0||index>=max_particles||double.IsNaN(time)||double.IsInfinity(time)||Math.Abs(time)>1e9||!float.IsFinite(center.x)||!float.IsFinite(center.y)||!float.IsFinite(center.z)||!float.IsFinite(wind.x)||!float.IsFinite(wind.y)||wind.sqrMagnitude>1600)throw new ArgumentException("Invalid weather observation.");
            double x=Unit((uint)index*3)*volume_width_m+wind.x*time,z=Unit((uint)index*3+1)*volume_width_m+wind.y*time,y=Unit((uint)index*3+2)*volume_height_m-fall_speed_mps*time;
            return new Vector3(Wrap(x,center.x,volume_width_m),Wrap(y,center.y,volume_height_m),Wrap(z,center.z,volume_width_m));
        }
        private static double Unit(uint v)=>(VegetationDefinition.Mix(v+158)&0xffffff)/16777216.0;
        private static float Wrap(double value,float center,float size){double lower=center-size*.5;return (float)(value-Math.Floor((value-lower)/size)*size);}
    }
}
