using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class RangeGuidanceDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="mat_assist_lead_m",IsRequired=true)] public double MatAssistLead {get;private set;}
        [DataMember(Name="approach_effort",IsRequired=true)] public int ApproachEffort {get;private set;}
        [DataMember(Name="approach_coast_above_mps",IsRequired=true)] public double ApproachCoastAbove {get;private set;}
        [DataMember(Name="departure_assist_m",IsRequired=true)] public double DepartureAssist {get;private set;}
        [DataMember(Name="departure_effort",IsRequired=true)] public int DepartureEffort {get;private set;}
        [DataMember(Name="mat_show_lead_m",IsRequired=true)] public double MatShowLead {get;private set;}
        [DataMember(Name="fork_show_lead_m",IsRequired=true)] public double ForkShowLead {get;private set;}
        [DataMember(Name="chevron_count",IsRequired=true)] public int ChevronCount {get;private set;}
        [DataMember(Name="chevron_spacing_m",IsRequired=true)] public double ChevronSpacing {get;private set;}
        [DataMember(Name="flash_hz",IsRequired=true)] public double FlashHz {get;private set;}
        [DataMember(Name="ground_offset_m",IsRequired=true)] public double GroundOffset {get;private set;}
        public static RangeGuidanceDefinition FromJson(string json)
        {
            var d=RouteJson.Read<RangeGuidanceDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Versioned range guidance required.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.MatAssistLead,.1,3,"mat assist lead");
            AthleteState.ValidateEffort(d.ApproachEffort);AthleteState.ValidateEffort(d.DepartureEffort);
            TrackValidation.Finite(d.ApproachCoastAbove,.1,5,"approach coasting speed");
            TrackValidation.Finite(d.DepartureAssist,.1,2,"short departure assist");
            TrackValidation.Finite(d.MatShowLead,d.MatAssistLead,100,"mat guide distance");
            TrackValidation.Finite(d.ForkShowLead,1,100,"fork guide distance");
            if(d.ChevronCount<2||d.ChevronCount>12)throw new SerializationException("Bounded chevron count required.");
            TrackValidation.Finite(d.ChevronSpacing,.1,5,"chevron spacing");
            TrackValidation.Finite(d.FlashHz,.1,5,"guidance pulse");TrackValidation.Finite(d.GroundOffset,.01,.2,"ground offset");return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
