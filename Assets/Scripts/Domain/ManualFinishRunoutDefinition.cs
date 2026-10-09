using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Explicit post-finish physical runway and shared braking intent.
    /// It grants no race progress or finish/lifecycle authority.</summary>
    [DataContract]
    public sealed class ManualFinishRunoutDefinition
    {
        private ManualFinishRunoutDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="distance_m",IsRequired=true)] public double DistanceMeters {get;private set;}
        [DataMember(Name="brake_fraction",IsRequired=true)] public double BrakeFraction {get;private set;}
        public static ManualFinishRunoutDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualFinishRunoutDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid manual finish runout schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.DistanceMeters,1,10000,nameof(DistanceMeters));
            TrackValidation.Finite(value.BrakeFraction,PhysiologyNumbers.MinimumPositive,1,nameof(BrakeFraction));
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
