using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    /// <summary>Decision policy for AI on an explicitly shared physical start approach.</summary>
    [DataContract] public sealed class AiStartTrafficPolicyDefinition
    {
        private AiStartTrafficPolicyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="hold_grid_lateral_on_approach",IsRequired=true)] public bool HoldGridLateral { get; private set; }
        [DataMember(Name="stationary_bypass_after_seconds",EmitDefaultValue=false)] public double? StationaryBypassAfterSeconds { get; private set; }
        [DataMember(Name="stationary_maximum_speed_mps",EmitDefaultValue=false)] public double? StationaryMaximumSpeed { get; private set; }
        public static AiStartTrafficPolicyDefinition FromJson(string json)
        {
            var value=RouteJson.Read<AiStartTrafficPolicyDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Unsupported AI start traffic policy.");
            TrackValidation.Id(value.Id);
            if(value.StationaryBypassAfterSeconds.HasValue!=value.StationaryMaximumSpeed.HasValue)
                throw new SerializationException("Both stationary bypass limits must be explicitly bound.");
            if(value.StationaryBypassAfterSeconds.HasValue)
            {TrackValidation.Finite(value.StationaryBypassAfterSeconds.Value,.05,60,"stationary bypass delay");TrackValidation.Finite(value.StationaryMaximumSpeed.Value,0,1,"stationary blocker speed");}
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
