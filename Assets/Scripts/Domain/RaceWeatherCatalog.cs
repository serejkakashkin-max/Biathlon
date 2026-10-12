using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    /// <summary>One immutable, recorded set of conditions for the whole race.</summary>
    [DataContract] public sealed class RaceWeatherPreset
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="visual_preset",IsRequired=true)] public string VisualPreset {get;private set;}
        [DataMember(Name="lighting_preset",IsRequired=true)] public string LightingPreset {get;private set;}
        [DataMember(Name="snow_profile",IsRequired=true)] public string SnowProfile {get;private set;}
        [DataMember(Name="snow_material",IsRequired=true)] public string SnowMaterial {get;private set;}
        [DataMember(Name="air_temperature_c",IsRequired=true)] public double AirTemperatureC {get;private set;}
        [DataMember(Name="snow_temperature_c",IsRequired=true)] public double SnowTemperatureC {get;private set;}
        [DataMember(Name="wind_east_mps",IsRequired=true)] public double WindEast {get;private set;}
        [DataMember(Name="wind_north_mps",IsRequired=true)] public double WindNorth {get;private set;}
        [DataMember(Name="course_contact_resource",EmitDefaultValue=false)] public string CourseContactResource {get;private set;}
        public double WindSpeed=>Math.Sqrt(WindEast*WindEast+WindNorth*WindNorth);
        public void Validate()
        {
            if(SchemaVersion!=1||string.IsNullOrWhiteSpace(Id)||
                (VisualPreset!="clear"&&VisualPreset!="overcast"&&VisualPreset!="snowfall")||
                (LightingPreset!="clear_cold"&&LightingPreset!="overcast")||
                (SnowMaterial!="cold"&&SnowMaterial!="wet")||string.IsNullOrWhiteSpace(SnowProfile)||
                !Finite(AirTemperatureC)||AirTemperatureC<-30||AirTemperatureC>10||
                !Finite(SnowTemperatureC)||SnowTemperatureC<-30||SnowTemperatureC>0||
                !Finite(WindEast)||!Finite(WindNorth)||WindSpeed>15||
                CourseContactResource!=null&&CourseContactResource!="manual_course_contact_ice_v1.json")
                throw new SerializationException("Invalid race weather.");
        }
        private static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        public RaceSnowConditions SnowConditions(RaceSnowConditions baseline)
        {
            if(baseline==null)throw new ArgumentNullException(nameof(baseline));
            string json=baseline.ToJson().Replace("\""+baseline.Id+"\"","\""+Id+"_snow\"");
            for(int i=0;i<baseline.SurfaceCount;i++)json=json.Replace("\"profile_id\":\""+baseline.GetSurface(i).ProfileId+"\"","\"profile_id\":\""+SnowProfile+"\"");
            return RaceSnowConditions.FromJson(json);
        }
        public string ToJson(){Validate();return WeatherJson.Write(this);}
        public static RaceWeatherPreset FromJson(string json){var p=WeatherJson.Read<RaceWeatherPreset>(json);if(p==null)throw new SerializationException("Missing weather.");p.Validate();return p;}
    }
    [DataContract] public sealed class RaceWeatherCatalog
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="presets",IsRequired=true)] private RaceWeatherPreset[] presets {get;set;}
        public int Count=>presets.Length;
        public RaceWeatherPreset Get(int i)=>presets[i];
        // Successive calendar events rotate through conditions. Season rotation changes
        // next year's forecast without consuming the career RNG or changing on reload.
        public RaceWeatherPreset Select(int eventOrdinal,int season)
        {if(eventOrdinal<0||season<1)throw new ArgumentOutOfRangeException();return presets[(int)(((long)eventOrdinal+season-1)%Count)];}
        public static RaceWeatherCatalog FromJson(string json)
        {
            var d=WeatherJson.Read<RaceWeatherCatalog>(json);
            if(d==null||d.SchemaVersion!=1||string.IsNullOrWhiteSpace(d.Id)||d.presets==null||d.Count<3||d.Count>16)throw new SerializationException("Invalid weather catalog.");
            for(int i=0;i<d.Count;i++){if(d.presets[i]==null)throw new SerializationException("Missing weather preset.");d.presets[i].Validate();for(int j=0;j<i;j++)if(d.presets[j].Id==d.presets[i].Id)throw new SerializationException("Duplicate weather identity.");}
            return d;
        }
    }
    internal static class WeatherJson
    {
        internal static T Read<T>(string json){using(var m=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json)))))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(m);}
        internal static string Write<T>(T value){using(var m=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(m,value);return Encoding.UTF8.GetString(m.ToArray());}}
    }
}
