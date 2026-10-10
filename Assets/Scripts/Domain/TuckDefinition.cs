using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class TuckDefinition
    {
        private TuckDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "maximum_grade", IsRequired = true)] public double MaximumGrade { get; private set; }
        [DataMember(Name = "minimum_speed_mps", IsRequired = true)] public double MinimumSpeed { get; private set; }
        [DataMember(Name = "novice_max_curvature_per_m", IsRequired = true)] public double NoviceMaximumCurvature { get; private set; }
        [DataMember(Name = "expert_max_curvature_per_m", IsRequired = true)] public double ExpertMaximumCurvature { get; private set; }
        [DataMember(Name = "novice_max_risk", IsRequired = true)] public double NoviceMaximumRisk { get; private set; }
        [DataMember(Name = "expert_max_risk", IsRequired = true)] public double ExpertMaximumRisk { get; private set; }
        [DataMember(Name = "maximum_effort_control", IsRequired = true)] public int MaximumEffortControl { get; private set; }
        [DataMember(Name = "drag_area_multiplier", IsRequired = true)] public double DragAreaMultiplier { get; private set; }
        [DataMember(Name = "steering_response_multiplier", IsRequired = true)] public double SteeringResponseMultiplier { get; private set; }
        [DataMember(Name = "world_hold_through_turns", EmitDefaultValue = false)] public bool WorldHoldThroughTurns {get;private set;}
        public static TuckDefinition FromJson(string json)
        {
            var value = RouteJson.Read<TuckDefinition>(json);
            if (value == null || value.SchemaVersion != 1) throw new SerializationException("Invalid tuck schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.MaximumGrade, -1, -0.000000001, nameof(MaximumGrade));
            TrackValidation.Finite(value.MinimumSpeed, 0, 1000000, nameof(MinimumSpeed));
            TrackValidation.Finite(value.NoviceMaximumCurvature, 0, 1, nameof(NoviceMaximumCurvature));
            TrackValidation.Finite(value.ExpertMaximumCurvature, value.NoviceMaximumCurvature, 1, nameof(ExpertMaximumCurvature));
            TrackValidation.Finite(value.NoviceMaximumRisk, 0, 1, nameof(NoviceMaximumRisk));
            TrackValidation.Finite(value.ExpertMaximumRisk, value.NoviceMaximumRisk, 1, nameof(ExpertMaximumRisk));
            AthleteState.ValidateEffort(value.MaximumEffortControl);
            TrackValidation.Finite(value.DragAreaMultiplier, 0.000000001, 1, nameof(DragAreaMultiplier));
            TrackValidation.Finite(value.SteeringResponseMultiplier, 0.000000001, 1, nameof(SteeringResponseMultiplier));
            return value;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
