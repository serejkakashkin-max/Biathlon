using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiContactGroupDefinition
    {
        private SkiContactGroupDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="maximum_passes",IsRequired=true)] public int MaximumPasses {get;private set;}
        [DataMember(Name="closing_speed_tolerance_mps",IsRequired=true)] public double ClosingSpeedTolerance {get;private set;}
        public static SkiContactGroupDefinition FromJson(string json)
        {
            var d=RouteJson.Read<SkiContactGroupDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid ski contact group schema.");
            TrackValidation.Id(d.Id);
            if(d.MaximumPasses<1||d.MaximumPasses>4096)throw new SerializationException("Invalid contact group pass limit.");
            TrackValidation.Finite(d.ClosingSpeedTolerance,1e-9,.01,nameof(ClosingSpeedTolerance));
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
