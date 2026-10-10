using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Explicit representation tolerances for the signed world.
    /// These grant no body overlap, route credit or lifecycle authority.</summary>
    [DataContract]
    public sealed class ManualNumericalToleranceDefinition
    {
        private ManualNumericalToleranceDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="terminal_rest_speed_mps",IsRequired=true)] public double TerminalRestSpeed {get;private set;}
        [DataMember(Name="terminal_rest_yaw_radps",IsRequired=true)] public double TerminalRestYaw {get;private set;}
        [DataMember(Name="course_clearance_allowance_m",IsRequired=true)] public double CourseClearanceAllowance {get;private set;}
        [DataMember(Name="portal_position_tolerance_m")] public double PortalPositionTolerance {get;private set;}
        [DataMember(Name="use_round_snow_corridor")] public bool UseRoundSnowCorridor {get;private set;}
        [DataMember(Name="connected_snow_window_multiplier")] public double ConnectedSnowWindowMultiplier {get;private set;}=1;
        [DataMember(Name="course_sweep_tolerance_m")] public double CourseSweepTolerance {get;private set;}
        [DataMember(Name="ordinary_portal_retreat_m")] public double OrdinaryPortalRetreat {get;private set;}
        [DataMember(Name="soft_snow_shoulder_m")] public double SoftSnowShoulder {get;private set;}
        [DataMember(Name="ordinary_handoff_window_m")] public double OrdinaryHandoffWindow {get;private set;}
        public static ManualNumericalToleranceDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualNumericalToleranceDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid numerical tolerance schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.TerminalRestSpeed,0,.01,nameof(TerminalRestSpeed));
            TrackValidation.Finite(value.TerminalRestYaw,0,.01,nameof(TerminalRestYaw));
            TrackValidation.Finite(value.CourseClearanceAllowance,0,.05,nameof(CourseClearanceAllowance));
            TrackValidation.Finite(value.PortalPositionTolerance,0,.01,nameof(PortalPositionTolerance));
            if(value.ConnectedSnowWindowMultiplier==0)value.ConnectedSnowWindowMultiplier=1;
            TrackValidation.Finite(value.ConnectedSnowWindowMultiplier,1,8,nameof(ConnectedSnowWindowMultiplier));
            TrackValidation.Finite(value.CourseSweepTolerance,0,.05,nameof(CourseSweepTolerance));
            TrackValidation.Finite(value.OrdinaryPortalRetreat,0,.25,nameof(OrdinaryPortalRetreat));
            TrackValidation.Finite(value.SoftSnowShoulder,0,2,nameof(SoftSnowShoulder));
            TrackValidation.Finite(value.OrdinaryHandoffWindow,0,4,nameof(OrdinaryHandoffWindow));
            return value;
        }
        public void Validate(OccupancyDefinition body)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(CourseClearanceAllowance>body.EdgeClearance*.5||CourseClearanceAllowance+CourseSweepTolerance>body.EdgeClearance)
                throw new ArgumentException("At least half the authored course clearance must remain.");
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
