using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingPressureDefinition
    {
        private ShootingPressureDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "expert_sensitivity", IsRequired = true)] public double ExpertSensitivity { get; private set; }
        [DataMember(Name = "low_confidence_gain", IsRequired = true)] public double LowConfidenceGain { get; private set; }
        [DataMember(Name = "final_standing_gain", IsRequired = true)] public double FinalStandingGain { get; private set; }
        [DataMember(Name = "first_podium_gain", IsRequired = true)] public double FirstPodiumGain { get; private set; }
        [DataMember(Name = "rise_tau_s", IsRequired = true)] public double RiseTau { get; private set; }
        [DataMember(Name = "recovery_tau_s", IsRequired = true)] public double RecoveryTau { get; private set; }
        [DataMember(Name = "breathing_gain", IsRequired = true)] public double BreathingGain { get; private set; }
        [DataMember(Name = "heartbeat_gain", IsRequired = true)] public double HeartbeatGain { get; private set; }
        [DataMember(Name = "muscular_gain", IsRequired = true)] public double MuscularGain { get; private set; }
        [DataMember(Name = "decision_delay_gain", IsRequired = true)] public double DecisionDelayGain { get; private set; }
        public static ShootingPressureDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingPressureDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported pressure schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.ExpertSensitivity, 0.01, 1, nameof(ExpertSensitivity));
            TrackValidation.Finite(v.LowConfidenceGain, 0, 1, nameof(LowConfidenceGain));
            TrackValidation.Finite(v.FinalStandingGain, 0, 1, nameof(FinalStandingGain));
            TrackValidation.Finite(v.FirstPodiumGain, 0, 1, nameof(FirstPodiumGain));
            TrackValidation.Finite(v.RiseTau, 0.01, 60, nameof(RiseTau));
            TrackValidation.Finite(v.RecoveryTau, 0.01, 60, nameof(RecoveryTau));
            TrackValidation.Finite(v.BreathingGain, 0, 2, nameof(BreathingGain));
            TrackValidation.Finite(v.HeartbeatGain, 0, 2, nameof(HeartbeatGain));
            TrackValidation.Finite(v.MuscularGain, 0, 2, nameof(MuscularGain));
            TrackValidation.Finite(v.DecisionDelayGain, 0, 2, nameof(DecisionDelayGain));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct ShootingPressureContext
    {
        public double RaceImportance { get; } public double PositionImportance { get; } public double RivalProximity { get; }
        public double Confidence { get; } public bool FinalStanding { get; } public bool FirstPodium { get; }
        public ShootingPressureContext(double raceImportance, double positionImportance, double rivalProximity, double confidence,
            bool finalStanding = false, bool firstPodium = false)
        {
            RaceImportance = PhysiologyNumbers.RequireRange(raceImportance, 0, 1, nameof(raceImportance));
            PositionImportance = PhysiologyNumbers.RequireRange(positionImportance, 0, 1, nameof(positionImportance));
            RivalProximity = PhysiologyNumbers.RequireRange(rivalProximity, 0, 1, nameof(rivalProximity));
            Confidence = PhysiologyNumbers.RequireRange(confidence, 0, 1, nameof(confidence));
            FinalStanding = finalStanding; FirstPodium = firstPodium;
        }
    }
}
