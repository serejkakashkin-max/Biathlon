using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class FixedTickFrameBudgetDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="maximum_steps_per_frame",IsRequired=true)] public int MaximumSteps {get;private set;}
        [DataMember(Name="maximum_backlog_seconds",IsRequired=true)] public double MaximumBacklog {get;private set;}
        [DataMember(Name="frame_work_budget_ms",IsRequired=true)] public double WorkBudgetMilliseconds {get;private set;}
        public static FixedTickFrameBudgetDefinition FromJson(string json)
        {
            var d=RouteJson.Read<FixedTickFrameBudgetDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Versioned fixed tick frame budget required.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.MaximumSteps,1,16,"frame steps");
            TrackValidation.Finite(d.MaximumBacklog,.05,1,"frame backlog");TrackValidation.Finite(d.WorkBudgetMilliseconds,1,50,"frame work budget");return d;
        }
    }
}
