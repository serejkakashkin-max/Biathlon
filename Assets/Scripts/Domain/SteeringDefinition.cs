using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SteeringDefinition
    {
        private SteeringDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "assisted_center_deadzone", IsRequired = true)] public int AssistedCenterDeadzone { get; private set; }
        [DataMember(Name = "lateral_speed_mps", IsRequired = true)] public double LateralSpeed { get; private set; }
        [DataMember(Name = "high_speed_reference_mps", IsRequired = true)] public double HighSpeedReference { get; private set; }
        [DataMember(Name = "high_speed_response_multiplier", IsRequired = true)] public double HighSpeedResponseMultiplier { get; private set; }
        [DataMember(Name = "lateral_propulsion_loss", IsRequired = true)] public double LateralPropulsionLoss { get; private set; }
        [DataMember(Name = "line_error_loss", IsRequired = true)] public double LineErrorLoss { get; private set; }
        public static SteeringDefinition FromJson(string json)
        {
            var v = RouteJson.Read<SteeringDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid steering schema.");
            TrackValidation.Id(v.Id);
            if (v.AssistedCenterDeadzone < 0 || v.AssistedCenterDeadzone >= PlayerCommand.LineIntentUnits) throw new ArgumentException("Invalid center deadzone.");
            TrackValidation.Finite(v.LateralSpeed, 0.000000001, 1000000, nameof(LateralSpeed));
            TrackValidation.Finite(v.HighSpeedReference, 0.000000001, 1000000, nameof(HighSpeedReference));
            TrackValidation.Finite(v.HighSpeedResponseMultiplier, 0.000000001, 1, nameof(HighSpeedResponseMultiplier));
            TrackValidation.Finite(v.LateralPropulsionLoss, 0, 1, nameof(LateralPropulsionLoss));
            TrackValidation.Finite(v.LineErrorLoss, 0, 1, nameof(LineErrorLoss));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
