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
        public static DeviceControlDefinition FromJson(string json)
        {
            var v = RouteJson.Read<DeviceControlDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported device control schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.StickDeadzone, 0, 0.99, nameof(StickDeadzone));
            TrackValidation.Finite(v.StickExponent, 1, 4, nameof(StickExponent));
            TrackValidation.Finite(v.KeyboardEffortRate, 1, 1000, nameof(KeyboardEffortRate));
            return v;
        }
    }
}
