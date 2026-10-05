using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class AiSkiPolicyDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="downhill_tuck_enabled",IsRequired=true)] public bool DownhillTuckEnabled {get;private set;}
        public static AiSkiPolicyDefinition FromJson(string json)
        {
            var value=RouteJson.Read<AiSkiPolicyDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new ArgumentException("Versioned AI ski policy required.");
            TrackValidation.Id(value.Id);return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
