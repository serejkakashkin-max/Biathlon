using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class BreathingDefinition
    {
        private BreathingDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "base_amplitude_rad", IsRequired = true)] public double BaseAmplitude { get; private set; }
        [DataMember(Name = "high_hr_amplitude_multiplier", IsRequired = true)] public double HighHrMultiplier { get; private set; }
        [DataMember(Name = "fatigue_amplitude_gain", IsRequired = true)] public double FatigueGain { get; private set; }
        [DataMember(Name = "expert_amplitude_multiplier", IsRequired = true)] public double ExpertMultiplier { get; private set; }
        [DataMember(Name = "rest_frequency_hz", IsRequired = true)] public double RestFrequency { get; private set; }
        [DataMember(Name = "high_hr_frequency_hz", IsRequired = true)] public double HighHrFrequency { get; private set; }
        public static BreathingDefinition FromJson(string json)
        {
            var v = RouteJson.Read<BreathingDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported breathing schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.BaseAmplitude, 0.000001, 0.01, nameof(BaseAmplitude));
            TrackValidation.Finite(v.HighHrMultiplier, 1, 4, nameof(HighHrMultiplier));
            TrackValidation.Finite(v.FatigueGain, 0, 4, nameof(FatigueGain));
            TrackValidation.Finite(v.ExpertMultiplier, 0.01, 1, nameof(ExpertMultiplier));
            TrackValidation.Finite(v.RestFrequency, 0.01, 1, nameof(RestFrequency));
            TrackValidation.Finite(v.HighHrFrequency, v.RestFrequency, 2, nameof(HighHrFrequency));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
