using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class OvertakeDefinition
    {
        private OvertakeDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "initiation_gap_m", IsRequired = true)] public double InitiationGap { get; private set; }
        [DataMember(Name = "passing_clearance_m", IsRequired = true)] public double PassingClearance { get; private set; }
        [DataMember(Name = "lateral_speed_mps", IsRequired = true)] public double LateralSpeed { get; private set; }
        [DataMember(Name = "lateral_effort_surcharge", IsRequired = true)] public int LateralEffortSurcharge { get; private set; }
        [DataMember(Name = "lateral_propulsion_loss", IsRequired = true)] public double LateralPropulsionLoss { get; private set; }
        [DataMember(Name = "timeout_seconds", IsRequired = true)] public double TimeoutSeconds { get; private set; }
        public static OvertakeDefinition FromJson(string json)
        {
            var v = RouteJson.Read<OvertakeDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid overtake schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.InitiationGap, 0.000000001, 1000000, nameof(InitiationGap));
            TrackValidation.Finite(v.PassingClearance, 0, 1000000, nameof(PassingClearance));
            TrackValidation.Finite(v.LateralSpeed, 0.000000001, 1000000, nameof(LateralSpeed));
            AthleteState.ValidateEffort(v.LateralEffortSurcharge);
            TrackValidation.Finite(v.LateralPropulsionLoss, 0, 1, nameof(LateralPropulsionLoss));
            TrackValidation.Finite(v.TimeoutSeconds, 0.000000001, 1000000, nameof(TimeoutSeconds));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
