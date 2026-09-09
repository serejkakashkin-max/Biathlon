using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class MuscularSwayDefinition
    {
        private MuscularSwayDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "frequencies_hz", IsRequired = true)] private double[] frequencies { get; set; }
        [DataMember(Name = "weights", IsRequired = true)] private double[] weights { get; set; }
        [DataMember(Name = "amplitude_rad", IsRequired = true)] public double Amplitude { get; private set; }
        [DataMember(Name = "fatigue_gain", IsRequired = true)] public double FatigueGain { get; private set; }
        [DataMember(Name = "arm_fatigue_gain", IsRequired = true)] public double ArmFatigueGain { get; private set; }
        [DataMember(Name = "expert_multiplier", IsRequired = true)] public double ExpertMultiplier { get; private set; }
        [DataMember(Name = "vertical_ratio", IsRequired = true)] public double VerticalRatio { get; private set; }
        public int ToneCount => frequencies.Length;
        public double Frequency(int index) => frequencies[index];
        public double Weight(int index) => weights[index];
        public static MuscularSwayDefinition FromJson(string json)
        {
            var v = RouteJson.Read<MuscularSwayDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported muscular sway schema.");
            TrackValidation.Id(v.Id);
            if (v.frequencies == null || v.weights == null || v.frequencies.Length < 2 || v.frequencies.Length > 16 || v.weights.Length != v.frequencies.Length)
                throw new ArgumentException("Invalid harmonic spectrum.");
            double total = 0;
            for (int i = 0; i < v.ToneCount; i++)
            {
                TrackValidation.Finite(v.frequencies[i], 0.01, 8, "frequency");
                TrackValidation.Finite(v.weights[i], 0.001, 1, "weight"); total += v.weights[i];
            }
            if (Math.Abs(total - 1) > 1e-9) throw new ArgumentException("Spectrum weights must sum to one.");
            TrackValidation.Finite(v.Amplitude, 0.000001, 0.01, nameof(Amplitude));
            TrackValidation.Finite(v.FatigueGain, 0, 4, nameof(FatigueGain));
            TrackValidation.Finite(v.ArmFatigueGain, 0, 4, nameof(ArmFatigueGain));
            TrackValidation.Finite(v.ExpertMultiplier, 0.01, 1, nameof(ExpertMultiplier));
            TrackValidation.Finite(v.VerticalRatio, 0, 1, nameof(VerticalRatio));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
