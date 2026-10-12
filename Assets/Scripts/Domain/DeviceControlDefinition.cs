using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class DeviceControlDefinition
    {
        private DeviceControlDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "stick_deadzone", IsRequired = true)] public double StickDeadzone { get; private set; }
        [DataMember(Name = "stick_exponent", IsRequired = true)] public double StickExponent { get; private set; }
        [DataMember(Name = "keyboard_effort_units_per_s", IsRequired = true)] public double KeyboardEffortRate { get; private set; }
        [DataMember(Name="keyboard_effort_ceiling",EmitDefaultValue=false)] public int KeyboardEffortCeiling {get;private set;}=1000;
        // Absent in historical device definitions: retain their immediate axis.
        [DataMember(Name="keyboard_steering_units_per_s",EmitDefaultValue=false)] public double KeyboardSteeringRate {get;private set;}
        [DataMember(Name="manual_brake_fraction",EmitDefaultValue=false)] public double ManualBrakeFraction {get;private set;}
        [DataMember(Name="stop_at_zero_effort",EmitDefaultValue=false)] public bool StopAtZeroEffort {get;private set;}
        public static DeviceControlDefinition FromJson(string json)
        {
            var v = RouteJson.Read<DeviceControlDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported device control schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.StickDeadzone, 0, 0.99, nameof(StickDeadzone));
            TrackValidation.Finite(v.StickExponent, 1, 4, nameof(StickExponent));
            TrackValidation.Finite(v.KeyboardEffortRate, 1, 1000, nameof(KeyboardEffortRate));
            TrackValidation.Finite(v.KeyboardSteeringRate, 0, 10000, nameof(KeyboardSteeringRate));
            TrackValidation.Finite(v.ManualBrakeFraction,0,1,nameof(ManualBrakeFraction));
            if(v.KeyboardEffortCeiling==0)v.KeyboardEffortCeiling=1000;
            AthleteState.ValidateEffort(v.KeyboardEffortCeiling);
            return v;
        }
    }
}
