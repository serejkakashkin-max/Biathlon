using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class RaceStartDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="countdown_seconds",IsRequired=true)] public double CountdownSeconds {get;private set;}
        [DataMember(Name="skip_maximum_steps_per_frame",EmitDefaultValue=false)] public int SkipMaximumSteps {get;private set;}
        [DataMember(Name="skip_work_budget_ms",EmitDefaultValue=false)] public double SkipWorkBudgetMilliseconds {get;private set;}
        public static RaceStartDefinition FromJson(string json)
        {
            var d=RouteJson.Read<RaceStartDefinition>(json);
            if(d==null||(d.SchemaVersion!=1&&d.SchemaVersion!=2))throw new SerializationException("Unsupported race start schema.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.CountdownSeconds,1,10,nameof(CountdownSeconds));
            if(d.SchemaVersion==2)
            {TrackValidation.Finite(d.SkipMaximumSteps,1,1000,nameof(SkipMaximumSteps));TrackValidation.Finite(d.SkipWorkBudgetMilliseconds,1,50,nameof(SkipWorkBudgetMilliseconds));}
            return d;
        }
    }
}
