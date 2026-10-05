using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ManualSkiSteeringDefinition
    {
        private ManualSkiSteeringDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "maximum_yaw_rate_radps", IsRequired = true)] public double MaximumYawRate { get; private set; }
        [DataMember(Name = "yaw_acceleration_radps2", IsRequired = true)] public double YawAcceleration { get; private set; }
        [DataMember(Name = "neutral_yaw_deceleration_radps2", IsRequired = true)] public double NeutralYawDeceleration { get; private set; }
        [DataMember(Name = "high_speed_reference_mps", IsRequired = true)] public double HighSpeedReference { get; private set; }
        [DataMember(Name = "high_speed_response_multiplier", IsRequired = true)] public double HighSpeedResponseMultiplier { get; private set; }
        [DataMember(Name = "minimum_speed_for_grip_limit_mps", IsRequired = true)] public double MinimumSpeedForGripLimit { get; private set; }
        [DataMember(Name = "path_integration_substeps", IsRequired = true)] public int PathIntegrationSubsteps { get; private set; }

        public static ManualSkiSteeringDefinition FromJson(string json)
        {
            var value = RouteJson.Read<ManualSkiSteeringDefinition>(json);
            if (value == null || value.SchemaVersion != 1) throw new SerializationException("Invalid manual steering schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.MaximumYawRate, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MaximumYawRate));
            TrackValidation.Finite(value.YawAcceleration, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(YawAcceleration));
            TrackValidation.Finite(value.NeutralYawDeceleration, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(NeutralYawDeceleration));
            TrackValidation.Finite(value.HighSpeedReference, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HighSpeedReference));
            TrackValidation.Finite(value.HighSpeedResponseMultiplier, PhysiologyNumbers.MinimumPositive, 1, nameof(HighSpeedResponseMultiplier));
            TrackValidation.Finite(value.MinimumSpeedForGripLimit, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MinimumSpeedForGripLimit));
            if (value.PathIntegrationSubsteps < 1 || value.PathIntegrationSubsteps > 1024)
                throw new ArgumentException("Invalid manual trajectory integration capacity.");
            return value;
        }

        public string ToJson() => RouteJson.Write(this);
    }
}
