using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    /// <summary>Foot placement dissipates residual motion on the assigned mat.
    /// Only the owned, admitted firing endpoint may perform this action.</summary>
    [DataContract] public sealed class ManualMatCaptureDefinition
    {
        private ManualMatCaptureDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="maximum_capture_speed_mps",IsRequired=true)] public double MaximumSpeed {get;private set;}
        [DataMember(Name="maximum_capture_yaw_radps",IsRequired=true)] public double MaximumYaw {get;private set;}
        [DataMember(Name="capture_position_tolerance_m",EmitDefaultValue=false)] private double? positionTolerance {get;set;}
        public double PositionTolerance=>positionTolerance??0;
        public static ManualMatCaptureDefinition FromJson(string json)
        {
            var d=RouteJson.Read<ManualMatCaptureDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid mat capture schema.");
            TrackValidation.Id(d.Id);
            TrackValidation.Finite(d.MaximumSpeed,0,3,nameof(MaximumSpeed));
            TrackValidation.Finite(d.MaximumYaw,0,3,nameof(MaximumYaw));
            TrackValidation.Finite(d.PositionTolerance,0,.8,nameof(PositionTolerance));return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
