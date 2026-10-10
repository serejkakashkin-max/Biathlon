using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class PlayerCourseAssistanceDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="release_delay_seconds",IsRequired=true)] public double ReleaseDelay {get;private set;}
        [DataMember(Name="return_gain",IsRequired=true)] public double ReturnGain {get;private set;}
        [DataMember(Name="maximum_return_input",IsRequired=true)] public int MaximumReturnInput {get;private set;}
        [DataMember(Name="return_input_per_second",IsRequired=true)] public double ReturnInputPerSecond {get;private set;}
        [DataMember(Name="arrow_capture_radius_m",IsRequired=true)] public double ArrowCaptureRadius {get;private set;}
        [DataMember(Name="departure_alignment_deg",IsRequired=true)] public double DepartureAlignmentDegrees {get;private set;}
        public static PlayerCourseAssistanceDefinition FromJson(string json)
        {
            var d=RouteJson.Read<PlayerCourseAssistanceDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Versioned player course assistance required.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.ReleaseDelay,0,3,"release delay");
            TrackValidation.Finite(d.ReturnGain,0,1,"return gain");TrackValidation.Finite(d.MaximumReturnInput,1,1000,"return input");
            TrackValidation.Finite(d.ReturnInputPerSecond,1,5000,"return slew");
            TrackValidation.Finite(d.ArrowCaptureRadius,.1,3,"arrow capture radius");
            TrackValidation.Finite(d.DepartureAlignmentDegrees,1,45,"departure alignment");return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
    [DataContract] public sealed class RangeGuidanceTrailDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="corridor_extra_clearance_m",IsRequired=true)] public double CorridorClearance {get;private set;}
        [DataMember(Name="mat_edge_clearance_m",IsRequired=true)] public double MatClearance {get;private set;}
        [DataMember(Name="turn_lead_m",IsRequired=true)] public double TurnLead {get;private set;}
        [DataMember(Name="final_spacing_m",IsRequired=true)] public double FinalSpacing {get;private set;}
        [DataMember(Name="chevron_footprint_m",IsRequired=true)] public double ChevronFootprint {get;private set;}
        public static RangeGuidanceTrailDefinition FromJson(string json)
        {
            var d=RouteJson.Read<RangeGuidanceTrailDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Versioned arrow trail required.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.CorridorClearance,0,3,"corridor clearance");
            TrackValidation.Finite(d.MatClearance,.1,1,"mat clearance");TrackValidation.Finite(d.TurnLead,5,30,"turn lead");
            TrackValidation.Finite(d.FinalSpacing,.5,1,"final spacing");TrackValidation.Finite(d.ChevronFootprint,.1,2,"arrow footprint");return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
