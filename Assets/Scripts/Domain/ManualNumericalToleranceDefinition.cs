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
        public static ManualNumericalToleranceDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualNumericalToleranceDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid numerical tolerance schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.TerminalRestSpeed,0,.01,nameof(TerminalRestSpeed));
            TrackValidation.Finite(value.TerminalRestYaw,0,.01,nameof(TerminalRestYaw));
            TrackValidation.Finite(value.CourseClearanceAllowance,0,.05,nameof(CourseClearanceAllowance));
            return value;
        }
        public void Validate(OccupancyDefinition body)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(CourseClearanceAllowance>body.EdgeClearance*.5)
                throw new ArgumentException("At least half the authored course clearance must remain.");
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
