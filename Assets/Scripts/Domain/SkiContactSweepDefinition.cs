using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiContactSweepDefinition
    {
        private SkiContactSweepDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="maximum_depth",IsRequired=true)] public int MaximumDepth {get;private set;}
        [DataMember(Name="maximum_interval_checks",IsRequired=true)] public int MaximumIntervalChecks {get;private set;}
        [DataMember(Name="time_tolerance_s",IsRequired=true)] public double TimeToleranceSeconds {get;private set;}
        public static SkiContactSweepDefinition FromJson(string json)
        {
            var d=RouteJson.Read<SkiContactSweepDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid ski sweep schema.");
            TrackValidation.Id(d.Id);
            if(d.MaximumDepth<1||d.MaximumDepth>40||d.MaximumIntervalChecks<1||d.MaximumIntervalChecks>100000)
                throw new SerializationException("Invalid ski sweep work limits.");
            TrackValidation.Finite(d.TimeToleranceSeconds,1e-9,1,nameof(TimeToleranceSeconds));
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
