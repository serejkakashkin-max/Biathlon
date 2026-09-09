using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class HeartbeatDefinition
    {
        private HeartbeatDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "amplitude_rad", IsRequired = true)] public double Amplitude { get; private set; }
        [DataMember(Name = "high_hr_multiplier", IsRequired = true)] public double HighHrMultiplier { get; private set; }
        [DataMember(Name = "pulse_width_cycles", IsRequired = true)] public double PulseWidth { get; private set; }
        [DataMember(Name = "attack_fraction", IsRequired = true)] public double AttackFraction { get; private set; }
        public static HeartbeatDefinition FromJson(string json)
        {
            var v = RouteJson.Read<HeartbeatDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported heartbeat schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.Amplitude, 0.000001, 0.01, nameof(Amplitude));
            TrackValidation.Finite(v.HighHrMultiplier, 1, 4, nameof(HighHrMultiplier));
            TrackValidation.Finite(v.PulseWidth, 0.01, 0.9, nameof(PulseWidth));
            TrackValidation.Finite(v.AttackFraction, 0.01, 0.5, nameof(AttackFraction));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
