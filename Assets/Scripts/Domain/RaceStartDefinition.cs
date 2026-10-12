using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class RaceStartDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="countdown_seconds",IsRequired=true)] public double CountdownSeconds {get;private set;}
        public static RaceStartDefinition FromJson(string json)
        {
            var d=RouteJson.Read<RaceStartDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Unsupported race start schema.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.CountdownSeconds,1,10,nameof(CountdownSeconds));return d;
        }
    }
}
