using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class CorneringDefinition
    {
        private CorneringDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "novice_lateral_acceleration_mps2", IsRequired = true)] public double NoviceLateralAcceleration { get; private set; }
        [DataMember(Name = "expert_lateral_acceleration_mps2", IsRequired = true)] public double ExpertLateralAcceleration { get; private set; }
        [DataMember(Name = "poor_line_modifier", IsRequired = true)] public double PoorLineModifier { get; private set; }
        [DataMember(Name = "exhausted_modifier", IsRequired = true)] public double ExhaustedModifier { get; private set; }
        [DataMember(Name = "maximum_risk_modifier", IsRequired = true)] public double MaximumRiskModifier { get; private set; }
        [DataMember(Name = "novice_downhill_modifier", IsRequired = true)] public double NoviceDownhillModifier { get; private set; }
        [DataMember(Name = "full_downhill_grade", IsRequired = true)] public double FullDownhillGrade { get; private set; }
        [DataMember(Name = "maximum_braking_mps2", IsRequired = true)] public double MaximumBraking { get; private set; }
        [DataMember(Name = "braking_response_seconds", IsRequired = true)] public double BrakingResponseSeconds { get; private set; }
        public static CorneringDefinition FromJson(string json)
        {
            var v = RouteJson.Read<CorneringDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid cornering schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.NoviceLateralAcceleration, 0.000000001, 1000, nameof(NoviceLateralAcceleration));
            TrackValidation.Finite(v.ExpertLateralAcceleration, v.NoviceLateralAcceleration, 1000, nameof(ExpertLateralAcceleration));
            TrackValidation.Finite(v.PoorLineModifier, 0.000000001, 1, nameof(PoorLineModifier));
            TrackValidation.Finite(v.ExhaustedModifier, 0.000000001, 1, nameof(ExhaustedModifier));
            TrackValidation.Finite(v.MaximumRiskModifier, 0.000000001, 1, nameof(MaximumRiskModifier));
            TrackValidation.Finite(v.NoviceDownhillModifier, 0.000000001, 1, nameof(NoviceDownhillModifier));
            TrackValidation.Finite(v.FullDownhillGrade, -1, -0.000000001, nameof(FullDownhillGrade));
            TrackValidation.Finite(v.MaximumBraking, 0.000000001, 1000, nameof(MaximumBraking));
            TrackValidation.Finite(v.BrakingResponseSeconds, 0.000000001, 1000000, nameof(BrakingResponseSeconds));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
