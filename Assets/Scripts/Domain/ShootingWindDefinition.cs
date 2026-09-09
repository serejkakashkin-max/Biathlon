using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingWindDefinition
    {
        private ShootingWindDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "maximum_speed_mps", IsRequired = true)] public double MaximumSpeed { get; private set; }
        [DataMember(Name = "projectile_speed_mps", IsRequired = true)] public double ProjectileSpeed { get; private set; }
        [DataMember(Name = "ballistic_scale", IsRequired = true)] public double BallisticScale { get; private set; }
        [DataMember(Name = "calm_threshold_mps", IsRequired = true)] public double CalmThreshold { get; private set; }
        [DataMember(Name = "moderate_threshold_mps", IsRequired = true)] public double ModerateThreshold { get; private set; }
        [DataMember(Name = "strong_threshold_mps", IsRequired = true)] public double StrongThreshold { get; private set; }
        public static ShootingWindDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingWindDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported shooting wind schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.MaximumSpeed, 1, 100, nameof(MaximumSpeed));
            TrackValidation.Finite(v.ProjectileSpeed, 100, 1000, nameof(ProjectileSpeed));
            TrackValidation.Finite(v.BallisticScale, 0, 1, nameof(BallisticScale));
            TrackValidation.Finite(v.CalmThreshold, 0, v.MaximumSpeed, nameof(CalmThreshold));
            TrackValidation.Finite(v.ModerateThreshold, v.CalmThreshold, v.MaximumSpeed, nameof(ModerateThreshold));
            TrackValidation.Finite(v.StrongThreshold, v.ModerateThreshold, v.MaximumSpeed, nameof(StrongThreshold));
            if (v.CalmThreshold == v.ModerateThreshold || v.ModerateThreshold == v.StrongThreshold)
                throw new SerializationException("Wind indicator thresholds must increase.");
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct WindVector
    {
        public double EastMetersPerSecond { get; } public double NorthMetersPerSecond { get; }
        public WindVector(double east, double north)
        {
            EastMetersPerSecond = PhysiologyNumbers.RequireRange(east, -100, 100, nameof(east));
            NorthMetersPerSecond = PhysiologyNumbers.RequireRange(north, -100, 100, nameof(north));
        }
    }
}
