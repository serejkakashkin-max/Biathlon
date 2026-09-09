using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiPhysicsDefinition
    {
        private SkiPhysicsDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "mass_kg", IsRequired = true)] public double MassKilograms { get; private set; }
        [DataMember(Name = "gravity_mps2", IsRequired = true)] public double GravityMetersPerSecondSquared { get; private set; }
        [DataMember(Name = "air_density_kg_per_m3", IsRequired = true)] public double AirDensity { get; private set; }
        [DataMember(Name = "drag_area_m2", IsRequired = true)] public double DragAreaSquareMeters { get; private set; }
        [DataMember(Name = "snow_friction_coefficient", IsRequired = true)] public double SnowFrictionCoefficient { get; private set; }
        [DataMember(Name = "power_force_min_speed_mps", IsRequired = true)] public double PowerForceMinimumSpeed { get; private set; }
        [DataMember(Name = "maximum_propulsive_force_n", IsRequired = true)] public double MaximumPropulsiveForceNewtons { get; private set; }
        [DataMember(Name = "maximum_speed_mps", IsRequired = true)] public double MaximumSpeedMetersPerSecond { get; private set; }
        [DataMember(Name = "maximum_power_watts", IsRequired = true)] public double MaximumPowerWatts { get; private set; }
        [DataMember(Name = "maximum_headwind_mps", IsRequired = true)] public double MaximumHeadwindMetersPerSecond { get; private set; }
        public static SkiPhysicsDefinition FromJson(string json)
        {
            var result = RouteJson.Read<SkiPhysicsDefinition>(json);
            if (result == null || result.SchemaVersion != 1) throw new SerializationException("Unsupported ski schema.");
            TrackValidation.Id(result.Id);
            TrackValidation.Finite(result.MassKilograms, 0.001, 1000000, nameof(MassKilograms));
            TrackValidation.Finite(result.GravityMetersPerSecondSquared, 0, 1000000, nameof(GravityMetersPerSecondSquared));
            TrackValidation.Finite(result.AirDensity, 0, 1000000, nameof(AirDensity));
            TrackValidation.Finite(result.DragAreaSquareMeters, 0, 1000000, nameof(DragAreaSquareMeters));
            TrackValidation.Finite(result.SnowFrictionCoefficient, 0, 1000000, nameof(SnowFrictionCoefficient));
            TrackValidation.Finite(result.PowerForceMinimumSpeed, 0.000000001, 1000000, nameof(PowerForceMinimumSpeed));
            TrackValidation.Finite(result.MaximumPropulsiveForceNewtons, 0, 1000000, nameof(MaximumPropulsiveForceNewtons));
            TrackValidation.Finite(result.MaximumSpeedMetersPerSecond, 0.000000001, 1000000, nameof(MaximumSpeedMetersPerSecond));
            TrackValidation.Finite(result.MaximumPowerWatts, 0, 1000000, nameof(MaximumPowerWatts));
            TrackValidation.Finite(result.MaximumHeadwindMetersPerSecond, 0, 1000000, nameof(MaximumHeadwindMetersPerSecond));
            if (Math.Round(result.MaximumSpeedMetersPerSecond, 9) != result.MaximumSpeedMetersPerSecond)
                throw new ArgumentException("Speed envelope must be representable at nine decimal places.");
            double maxFlow = result.MaximumSpeedMetersPerSecond + result.MaximumHeadwindMetersPerSecond;
            TrackValidation.Finite(0.5 * result.AirDensity * result.DragAreaSquareMeters * maxFlow * maxFlow +
                result.MassKilograms * result.GravityMetersPerSecondSquared * (1 + result.SnowFrictionCoefficient) + result.MaximumPropulsiveForceNewtons,
                0, 1000000, "maximum force magnitude");
            return result;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
