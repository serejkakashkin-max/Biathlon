using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Explicit final braking strip of the owner-approved range assist.
    /// This definition grants neither mat ownership nor physical admission.</summary>
    [DataContract]
    public sealed class ManualMatBrakingDefinition
    {
        private ManualMatBrakingDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="terminal_braking_distance_m",IsRequired=true)] public double TerminalDistanceMeters {get;private set;}
        public static ManualMatBrakingDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualMatBrakingDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid terminal mat braking schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.TerminalDistanceMeters,PhysiologyNumbers.MinimumPositive,3,nameof(TerminalDistanceMeters));
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
