using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class PreRangeRecoveryDefinition
    {
        private PreRangeRecoveryDefinition() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="approach_distance_m", IsRequired=true)] public double ApproachMeters { get; private set; }
        [DataMember(Name="recovery_start_hr_bpm", IsRequired=true)] public double StartHeartRate { get; private set; }
        [DataMember(Name="full_recovery_hr_bpm", IsRequired=true)] public double FullHeartRate { get; private set; }
        [DataMember(Name="maximum_effort_reduction", IsRequired=true)] public int MaximumReduction { get; private set; }
        public static PreRangeRecoveryDefinition FromJson(string json)
        {
            var v = RouteJson.Read<PreRangeRecoveryDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported pre-range recovery schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.ApproachMeters, 0.001, 10000, nameof(ApproachMeters));
            TrackValidation.Finite(v.StartHeartRate, 1, 299, nameof(StartHeartRate));
            TrackValidation.Finite(v.FullHeartRate, v.StartHeartRate + 0.001, 300, nameof(FullHeartRate));
            AthleteState.ValidateEffort(v.MaximumReduction);
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
