using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ManualMatDockingDefinition
    {
        private ManualMatDockingDefinition(){}
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="capture_distance_m",IsRequired=true)] public double CaptureDistanceMeters {get;private set;}
        [DataMember(Name="neutral_steering_distance_m",IsRequired=true)] public double NeutralSteeringDistanceMeters {get;private set;}
        [DataMember(Name="maximum_neutral_heading_error_rad",IsRequired=true)] public double MaximumNeutralHeadingErrorRadians {get;private set;}
        [DataMember(Name="approach_effort_units",IsRequired=true)] public int ApproachEffortUnits {get;private set;}
        public static ManualMatDockingDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualMatDockingDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid mat docking schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.CaptureDistanceMeters,PhysiologyNumbers.MinimumPositive,100,nameof(CaptureDistanceMeters));
            TrackValidation.Finite(value.NeutralSteeringDistanceMeters,PhysiologyNumbers.MinimumPositive,value.CaptureDistanceMeters,nameof(NeutralSteeringDistanceMeters));
            TrackValidation.Finite(value.MaximumNeutralHeadingErrorRadians,PhysiologyNumbers.MinimumPositive,System.Math.PI*.5,nameof(MaximumNeutralHeadingErrorRadians));
            AthleteState.ValidateEffort(value.ApproachEffortUnits);
            if(value.ApproachEffortUnits==0)throw new SerializationException("Mat approach requires a positive bounded effort intent.");
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
