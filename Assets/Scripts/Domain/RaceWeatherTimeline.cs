using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public readonly struct RaceWeatherFrame
    {
        public double AirTemperatureC {get;} public double SnowTemperatureC {get;}
        public WindVector Wind {get;} public double Snowfall {get;}
        public string SnowFrom {get;} public string SnowTo {get;} public double SnowBlend {get;}
        public string VisualFrom {get;} public string VisualTo {get;} public double VisualBlend {get;}
        public string SnowProfile=>SnowBlend<.5?SnowFrom:SnowTo;
        internal RaceWeatherFrame(double air,double snow,double east,double north,double snowfall,string from,string to,double blend,string visualFrom,string visualTo,double visualBlend)
        {AirTemperatureC=air;SnowTemperatureC=snow;Wind=new WindVector(east,north);Snowfall=snowfall;SnowFrom=from;SnowTo=to;SnowBlend=blend;VisualFrom=visualFrom;VisualTo=visualTo;VisualBlend=visualBlend;}
    }
    [DataContract] public sealed class RaceWeatherFront
    {
        [DataMember(Name="start_seconds",IsRequired=true)] public double Start {get;internal set;}
        [DataMember(Name="air_transition_seconds",IsRequired=true)] public double AirDuration {get;internal set;}
        [DataMember(Name="snow_delay_seconds",IsRequired=true)] public double SnowDelay {get;internal set;}
        [DataMember(Name="snow_transition_seconds",IsRequired=true)] public double SnowDuration {get;internal set;}
        [DataMember(Name="air_temperature_c",IsRequired=true)] public double Air {get;internal set;}
        [DataMember(Name="snow_temperature_c",IsRequired=true)] public double Snow {get;internal set;}
        [DataMember(Name="wind_east_mps",IsRequired=true)] public double East {get;internal set;}
        [DataMember(Name="wind_north_mps",IsRequired=true)] public double North {get;internal set;}
        [DataMember(Name="snowfall",IsRequired=true)] public double Snowfall {get;internal set;}
        [DataMember(Name="snow_profile",IsRequired=true)] public string SnowProfile {get;internal set;}
        [DataMember(Name="visual_preset",IsRequired=true)] public string Visual {get;internal set;}
        internal void Validate()
        {
            TrackValidation.Finite(Start,0,10000,"front start");TrackValidation.Finite(AirDuration,60,1800,"air transition");TrackValidation.Finite(SnowDelay,60,1800,"snow inertia delay");TrackValidation.Finite(SnowDuration,AirDuration,3600,"snow transition");
            TrackValidation.Finite(Air,-40,15,"air temperature");TrackValidation.Finite(Snow,-40,0,"snow temperature");TrackValidation.Finite(East,-12,12,"east wind");TrackValidation.Finite(North,-12,12,"north wind");TrackValidation.Finite(Snowfall,0,1,"precipitation");
            TrackValidation.Id(SnowProfile);if(SnowProfile!="packed_neutral"&&SnowProfile!="fresh_soft"&&SnowProfile!="icy_fast"&&SnowProfile!="wet_slow")throw new SerializationException("Unknown timeline snow profile.");
            if(Visual!="clear"&&Visual!="overcast"&&Visual!="fog"&&Visual!="snowfall"&&Visual!="light_snow"&&Visual!="winter_haze")throw new SerializationException("Unknown timeline visual.");
            if(East*East+North*North>225)throw new SerializationException("Timeline wind exceeds the existing15m/s bound.");
        }
    }
    /// <summary>Pure absolute-time sampling. No accumulated state, RNG, wall time or per-athlete clock.</summary>
    [DataContract] public sealed class RaceWeatherTimeline
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int Version {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="scenario_id",IsRequired=true)] public string ScenarioId {get;private set;}
        [DataMember(Name="initial",IsRequired=true)] public RaceWeatherPreset Initial {get;private set;}
        [DataMember(Name="initial_snowfall",IsRequired=true)] public double InitialSnowfall {get;private set;}
        [DataMember(Name="fronts",IsRequired=true)] private RaceWeatherFront[] fronts {get;set;}
        public int FrontCount=>fronts.Length;public RaceWeatherFront Front(int index)=>fronts[index];
        private RaceWeatherTimeline(){}
        internal RaceWeatherTimeline(RaceWeatherPreset initial,string scenario,RaceWeatherFront[] fronts,double initialSnowfall)
        {Version=1;Id="race_weather_timeline_v1";Initial=initial;InitialSnowfall=initialSnowfall;ScenarioId=scenario;this.fronts=fronts;Validate();}
        public RaceWeatherFrame Sample(double seconds)
        {
            TrackValidation.Finite(seconds,0,1000000,"absolute race seconds");
            double air=Initial.AirTemperatureC,snow=Initial.SnowTemperatureC,east=Initial.WindEast,north=Initial.WindNorth,fall=InitialSnowfall;
            string profile=Initial.SnowProfile,visual=Initial.VisualPreset;
            foreach(var f in fronts)
            {
                if(seconds<f.Start)break;
                double a=Smooth((seconds-f.Start)/f.AirDuration),s=Smooth((seconds-f.Start-f.SnowDelay)/f.SnowDuration);
                if(a<1||s<1)return new RaceWeatherFrame(Lerp(air,f.Air,a),Lerp(snow,f.Snow,s),Lerp(east,f.East,a),Lerp(north,f.North,a),Lerp(fall,f.Snowfall,a),profile,f.SnowProfile,s,visual,f.Visual,a);
                air=f.Air;snow=f.Snow;east=f.East;north=f.North;fall=f.Snowfall;profile=f.SnowProfile;visual=f.Visual;
            }
            return new RaceWeatherFrame(air,snow,east,north,fall,profile,profile,0,visual,visual,0);
        }
        private static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        private static double Lerp(double a,double b,double t)=>a+(b-a)*t;
        private void Validate()
        {
            if(Version!=1||Id!="race_weather_timeline_v1"||Initial==null||fronts==null||fronts.Length>2)throw new SerializationException("Invalid weather timeline.");TrackValidation.Id(ScenarioId);Initial.Validate();TrackValidation.Finite(InitialSnowfall,0,1,"initial precipitation");double end=0;
            foreach(var f in fronts){if(f==null)throw new SerializationException("Missing weather front.");f.Validate();if(f.Start<end)throw new SerializationException("Overlapping snow fronts.");end=f.Start+Math.Max(f.AirDuration,f.SnowDelay+f.SnowDuration);}
        }
        public string ToJson()=>RouteJson.Write(this);
        public static RaceWeatherTimeline FromJson(string json){var v=RouteJson.Read<RaceWeatherTimeline>(json)??throw new SerializationException("Missing timeline.");v.Validate();return v;}
    }
    [DataContract] public sealed class RaceWeatherFrontSpec
    {
        [DataMember(Name="start_seconds",IsRequired=true)] public double Start {get;private set;}
        [DataMember(Name="air_transition_seconds",IsRequired=true)] public double AirDuration {get;private set;}
        [DataMember(Name="snow_delay_seconds",IsRequired=true)] public double SnowDelay {get;private set;}
        [DataMember(Name="snow_transition_seconds",IsRequired=true)] public double SnowDuration {get;private set;}
        [DataMember(Name="air_delta_c",IsRequired=true)] public double AirDelta {get;private set;}
        [DataMember(Name="snow_delta_c",IsRequired=true)] public double SnowDelta {get;private set;}
        [DataMember(Name="wind_multiplier",IsRequired=true)] public double WindMultiplier {get;private set;}
        [DataMember(Name="snowfall",IsRequired=true)] public double Snowfall {get;private set;}
        [DataMember(Name="snow_profile",IsRequired=true)] public string SnowProfile {get;private set;}
        [DataMember(Name="visual_preset",IsRequired=true)] public string Visual {get;private set;}
    }
    [DataContract] public sealed class RaceWeatherScenario
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="weight",IsRequired=true)] public int Weight {get;private set;}
        [DataMember(Name="fronts",IsRequired=true)] public RaceWeatherFrontSpec[] Fronts {get;private set;}
    }
    [DataContract] public sealed class RaceWeatherPolicy
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int Version {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="wet_threshold_c",IsRequired=true)] public double WetThreshold {get;private set;}
        [DataMember(Name="freeze_threshold_c",IsRequired=true)] public double FreezeThreshold {get;private set;}
        [DataMember(Name="snowfall_intensity",IsRequired=true)] public double SnowfallIntensity {get;private set;}
        [DataMember(Name="light_snow_intensity",IsRequired=true)] public double LightSnowIntensity {get;private set;}
        [DataMember(Name="scenarios",IsRequired=true)] private RaceWeatherScenario[] scenarios {get;set;}
        public int Count=>scenarios.Length;public RaceWeatherScenario Scenario(int i)=>scenarios[i];
        public RaceWeatherTimeline Create(RaceWeatherPreset initial,ulong raceSeed)
        {
            int total=0;foreach(var s in scenarios)total+=s.Weight;
            // Independent fixed identity hash, never a draw from the career or athlete random stream.
            ulong hash=raceSeed^0x5745415448455254UL;unchecked{hash^=hash>>30;hash*=0xbf58476d1ce4e5b9UL;hash^=hash>>27;hash*=0x94d049bb133111ebUL;hash^=hash>>31;}
            int choice=(int)(hash%(ulong)total);foreach(var s in scenarios){if(choice<s.Weight)return CreateScenario(initial,s.Id);choice-=s.Weight;}throw new InvalidOperationException();
        }
        public RaceWeatherTimeline CreateScenario(RaceWeatherPreset initial,string id)
        {
            if(initial==null)throw new ArgumentNullException(nameof(initial));initial.Validate();RaceWeatherScenario chosen=null;foreach(var s in scenarios)if(s.Id==id)chosen=s;if(chosen==null)throw new ArgumentException("Unknown weather scenario.");
            double initialFall=initial.VisualPreset=="snowfall"?SnowfallIntensity:initial.VisualPreset=="light_snow"?LightSnowIntensity:0;
            var fronts=new RaceWeatherFront[chosen.Fronts.Length];double air=initial.AirTemperatureC,snow=initial.SnowTemperatureC,east=initial.WindEast,north=initial.WindNorth,fall=initialFall;string surface=initial.SnowProfile,visual=initial.VisualPreset;
            for(int i=0;i<fronts.Length;i++)
            {
                var s=chosen.Fronts[i];air=Math.Max(-40,Math.Min(15,air+s.AirDelta));snow=Math.Max(-40,Math.Min(0,snow+s.SnowDelta));east=Math.Max(-12,Math.Min(12,east*s.WindMultiplier));north=Math.Max(-12,Math.Min(12,north*s.WindMultiplier));if(s.Snowfall>=0)fall=s.Snowfall;
                if(s.SnowProfile=="thermal")surface=snow>=WetThreshold?"wet_slow":snow<=FreezeThreshold&&surface=="wet_slow"?"icy_fast":fall>0?"fresh_soft":"packed_neutral";else if(s.SnowProfile!="keep")surface=s.SnowProfile;
                if(s.Visual!="keep")visual=s.Visual;
                fronts[i]=new RaceWeatherFront{Start=s.Start,AirDuration=s.AirDuration,SnowDelay=s.SnowDelay,SnowDuration=s.SnowDuration,Air=air,Snow=snow,East=east,North=north,Snowfall=fall,SnowProfile=surface,Visual=visual};
            }
            return new RaceWeatherTimeline(initial,chosen.Id,fronts,initialFall);
        }
        public static RaceWeatherPolicy FromJson(string json)
        {
            var p=RouteJson.Read<RaceWeatherPolicy>(json);if(p==null||p.Version!=1||p.scenarios==null||p.scenarios.Length<1||p.scenarios.Length>20)throw new SerializationException("Invalid dynamic weather policy.");TrackValidation.Id(p.Id);TrackValidation.Finite(p.WetThreshold,-2,0,"wet threshold");TrackValidation.Finite(p.FreezeThreshold,-10,-2,"freeze threshold");
            var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            TrackValidation.Finite(p.SnowfallIntensity,0,1,"snowfall intensity");TrackValidation.Finite(p.LightSnowIntensity,0,p.SnowfallIntensity,"light snow intensity");
            foreach(var s in p.scenarios)
            {
                if(s==null||s.Weight<1||s.Weight>100||s.Fronts==null||s.Fronts.Length>2||!ids.Add(s.Id))throw new SerializationException("Invalid weather scenario.");TrackValidation.Id(s.Id);
                foreach(var f in s.Fronts){if(f==null)throw new SerializationException("Missing weather front spec.");TrackValidation.Finite(f.AirDelta,-8,8,"air delta");TrackValidation.Finite(f.SnowDelta,-6,6,"snow delta");TrackValidation.Finite(f.WindMultiplier,.25,2,"wind change");TrackValidation.Finite(f.Snowfall,-1,1,"snowfall override");}
                // Validate resolved bounds, timing and target IDs against a real initial preset at composition.
            }
            return p;
        }
    }
}
