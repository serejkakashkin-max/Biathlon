using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class BreathControlDefinition
    {
        private BreathControlDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "hold_multiplier", IsRequired = true)] public double HoldMultiplier { get; private set; }
        [DataMember(Name = "debt_per_s", IsRequired = true)] public double DebtRate { get; private set; }
        [DataMember(Name = "recovery_per_s", IsRequired = true)] public double RecoveryRate { get; private set; }
        [DataMember(Name = "expert_debt_multiplier", IsRequired = true)] public double ExpertDebtMultiplier { get; private set; }
        [DataMember(Name = "discomfort_gain", IsRequired = true)] public double DiscomfortGain { get; private set; }
        [DataMember(Name = "rebound_gain", IsRequired = true)] public double ReboundGain { get; private set; }
        [DataMember(Name = "rearm_debt", IsRequired = true)] public double RearmDebt { get; private set; }
        public static BreathControlDefinition FromJson(string json)
        {
            var v = RouteJson.Read<BreathControlDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported breath control schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.HoldMultiplier, 0.01, 0.99, nameof(HoldMultiplier));
            TrackValidation.Finite(v.DebtRate, 0.01, 1, nameof(DebtRate));
            TrackValidation.Finite(v.RecoveryRate, 0.01, 1, nameof(RecoveryRate));
            TrackValidation.Finite(v.ExpertDebtMultiplier, 0.1, 1, nameof(ExpertDebtMultiplier));
            TrackValidation.Finite(v.DiscomfortGain, 1 - v.HoldMultiplier, 4, nameof(DiscomfortGain));
            TrackValidation.Finite(v.ReboundGain, 0.01, 4, nameof(ReboundGain));
            TrackValidation.Finite(v.RearmDebt, 0, 0.5, nameof(RearmDebt));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
