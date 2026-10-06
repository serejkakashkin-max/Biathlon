using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    /// <summary>Optional decision policy for athletes using authored physical range paths.</summary>
    [DataContract] public sealed class AiRangeTrafficPolicyDefinition
    {
        private AiRangeTrafficPolicyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="align_physical_range_paths",IsRequired=true)] public bool AlignPhysicalPaths { get; private set; }
        [DataMember(Name="alignment_lead_m",IsRequired=true)] public double AlignmentLeadMeters { get; private set; }
        [DataMember(Name="blocked_alignment_effort",EmitDefaultValue=false)] public int? BlockedAlignmentEffort { get; private set; }
        [DataMember(Name="entry_lateral_m",EmitDefaultValue=false)] public double? EntryLateralMeters { get; private set; }
        [DataMember(Name="exit_lateral_m",EmitDefaultValue=false)] public double? ExitLateralMeters {get;private set;}
        [DataMember(Name="retain_shared_exit_line",EmitDefaultValue=false)] public bool RetainSharedExitLine {get;private set;}
        [DataMember(Name="penalty_lateral_m",EmitDefaultValue=false)] public double? PenaltyLateralMeters {get;private set;}
        [DataMember(Name="world_bypass_after_seconds",EmitDefaultValue=false)] public double? WorldBypassAfterSeconds {get;private set;}
        [DataMember(Name="world_bypass_lateral_step_m",EmitDefaultValue=false)] public double? WorldBypassLateralStep {get;private set;}
        [DataMember(Name="world_bypass_preview_m",EmitDefaultValue=false)] public double? WorldBypassPreview {get;private set;}
        [DataMember(Name="world_bypass_hold_seconds",EmitDefaultValue=false)] public double? WorldBypassHoldSeconds {get;private set;}
        [DataMember(Name="world_bypass_paid_route_loops",EmitDefaultValue=false)] public bool WorldBypassPaidRouteLoops {get;private set;}
        [DataMember(Name="world_bypass_lookahead_m",EmitDefaultValue=false)] public double? WorldBypassLookaheadMeters {get;private set;}
        public static AiRangeTrafficPolicyDefinition FromJson(string json)
        {
            var value=RouteJson.Read<AiRangeTrafficPolicyDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Unsupported AI range traffic policy.");
            TrackValidation.Id(value.Id);TrackValidation.Finite(value.AlignmentLeadMeters,.001,1000,nameof(AlignmentLeadMeters));
            if(value.BlockedAlignmentEffort.HasValue)AthleteState.ValidateEffort(value.BlockedAlignmentEffort.Value);
            if(value.EntryLateralMeters.HasValue)TrackValidation.Finite(value.EntryLateralMeters.Value,-1000,1000,nameof(EntryLateralMeters));
            if(value.ExitLateralMeters.HasValue)TrackValidation.Finite(value.ExitLateralMeters.Value,-1000,1000,nameof(ExitLateralMeters));
            if(value.PenaltyLateralMeters.HasValue)TrackValidation.Finite(value.PenaltyLateralMeters.Value,-1000,1000,nameof(PenaltyLateralMeters));
            if(value.WorldBypassLookaheadMeters.HasValue||value.WorldBypassPaidRouteLoops||value.WorldBypassAfterSeconds.HasValue||value.WorldBypassLateralStep.HasValue||value.WorldBypassPreview.HasValue||value.WorldBypassHoldSeconds.HasValue)
            {
                if(!value.WorldBypassAfterSeconds.HasValue||!value.WorldBypassLateralStep.HasValue||!value.WorldBypassPreview.HasValue||!value.WorldBypassHoldSeconds.HasValue)throw new SerializationException("Complete world bypass policy required.");
                TrackValidation.Finite(value.WorldBypassAfterSeconds.Value,.05,60,"bypass delay");
                TrackValidation.Finite(value.WorldBypassLateralStep.Value,.05,5,"bypass spacing");
                TrackValidation.Finite(value.WorldBypassPreview.Value,.001,5,"bypass preview");
                TrackValidation.Finite(value.WorldBypassHoldSeconds.Value,.05,10,"bypass hold");
                if(value.WorldBypassLookaheadMeters.HasValue)TrackValidation.Finite(value.WorldBypassLookaheadMeters.Value,.1,100,"bypass lookahead");
            }
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
