using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class AiSkiPolicyDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="downhill_tuck_enabled",IsRequired=true)] public bool DownhillTuckEnabled {get;private set;}
        [DataMember(Name="pace_controller_resource",EmitDefaultValue=false)] public string PaceControllerResource {get;private set;}
        [DataMember(Name="shooting_strategy_resource",EmitDefaultValue=false)] public string ShootingStrategyResource {get;private set;}
        [DataMember(Name="pre_range_recovery_resource",EmitDefaultValue=false)] public string PreRangeRecoveryResource {get;private set;}
        public static AiSkiPolicyDefinition FromJson(string json)
        {
            var value=RouteJson.Read<AiSkiPolicyDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new ArgumentException("Versioned AI ski policy required.");
            TrackValidation.Id(value.Id);foreach(string resource in new[]{value.PaceControllerResource,value.ShootingStrategyResource,value.PreRangeRecoveryResource})if(resource!=null&&(!resource.EndsWith(".json",StringComparison.Ordinal)||resource.IndexOfAny(new[]{'/','\\'})>=0))throw new ArgumentException("Explicit local AI definition resource required.");return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
