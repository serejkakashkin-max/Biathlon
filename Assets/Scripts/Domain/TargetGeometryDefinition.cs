using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public enum ShootingStance { Prone = 0, Standing = 1 }
    [DataContract]
    public sealed class TargetGeometryDefinition
    {
        private TargetGeometryDefinition() { }
        public const int TargetCount = 5;
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "distance_m", IsRequired = true)] public double DistanceMeters { get; private set; }
        [DataMember(Name = "spacing_m", IsRequired = true)] public double SpacingMeters { get; private set; }
        [DataMember(Name = "prone_diameter_m", IsRequired = true)] public double ProneDiameterMeters { get; private set; }
        [DataMember(Name = "standing_diameter_m", IsRequired = true)] public double StandingDiameterMeters { get; private set; }
        public static TargetGeometryDefinition FromJson(string json)
        {
            var v = RouteJson.Read<TargetGeometryDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported target geometry schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.DistanceMeters, 1, 1000, nameof(DistanceMeters));
            TrackValidation.Finite(v.SpacingMeters, 0.001, 10, nameof(SpacingMeters));
            TrackValidation.Finite(v.ProneDiameterMeters, 0.001, 1, nameof(ProneDiameterMeters));
            TrackValidation.Finite(v.StandingDiameterMeters, v.ProneDiameterMeters, 1, nameof(StandingDiameterMeters));
            if (v.SpacingMeters <= v.StandingDiameterMeters) throw new ArgumentException("Apertures must not touch or overlap.");
            return v;
        }
        public double CenterX(int index)
        {
            if (index < 0 || index >= TargetCount) throw new ArgumentOutOfRangeException(nameof(index));
            return (index - (TargetCount - 1) / 2) * SpacingMeters;
        }
        public double Radius(ShootingStance stance)
        {
            if (stance != ShootingStance.Prone && stance != ShootingStance.Standing) throw new ArgumentOutOfRangeException(nameof(stance));
            return (stance == ShootingStance.Prone ? ProneDiameterMeters : StandingDiameterMeters) / 2;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct ShotRay
    {
        public double OriginX { get; } public double OriginY { get; } public double OriginZ { get; }
        public double DirectionX { get; } public double DirectionY { get; } public double DirectionZ { get; }
        public bool IsValid => DirectionX != 0 || DirectionY != 0 || DirectionZ != 0;
        public ShotRay(double ox, double oy, double oz, double dx, double dy, double dz)
        {
            PhysiologyNumbers.RequireRange(ox, -1000000, 1000000, nameof(ox));
            PhysiologyNumbers.RequireRange(oy, -1000000, 1000000, nameof(oy));
            PhysiologyNumbers.RequireRange(oz, -1000000, 1000000, nameof(oz));
            PhysiologyNumbers.RequireRange(dx, -1000000, 1000000, nameof(dx));
            PhysiologyNumbers.RequireRange(dy, -1000000, 1000000, nameof(dy));
            PhysiologyNumbers.RequireRange(dz, -1000000, 1000000, nameof(dz));
            if (dx == 0 && dy == 0 && dz == 0) throw new ArgumentException("Ray direction cannot be zero.");
            OriginX = ox; OriginY = oy; OriginZ = oz; DirectionX = dx; DirectionY = dy; DirectionZ = dz;
        }
    }
}
