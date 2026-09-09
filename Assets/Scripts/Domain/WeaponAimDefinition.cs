using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class WeaponAimDefinition
    {
        private WeaponAimDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "max_yaw_rad", IsRequired = true)] public double MaxYawRadians { get; private set; }
        [DataMember(Name = "max_pitch_rad", IsRequired = true)] public double MaxPitchRadians { get; private set; }
        [DataMember(Name = "rate_rad_per_s", IsRequired = true)] public double RateRadiansPerSecond { get; private set; }
        [DataMember(Name = "fine_multiplier", IsRequired = true)] public double FineMultiplier { get; private set; }
        public static WeaponAimDefinition FromJson(string json)
        {
            var v = RouteJson.Read<WeaponAimDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported aim schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.MaxYawRadians, 0.001, 0.5, nameof(MaxYawRadians));
            TrackValidation.Finite(v.MaxPitchRadians, 0.001, 0.5, nameof(MaxPitchRadians));
            TrackValidation.Finite(v.RateRadiansPerSecond, 0.000001, 1, nameof(RateRadiansPerSecond));
            TrackValidation.Finite(v.FineMultiplier, 0.001, 1, nameof(FineMultiplier));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct WeaponAimCommand
    {
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public int Horizontal { get; }
        public int Vertical { get; }
        public bool FineControl { get; }
        public WeaponAimCommand(SimulationTick tick, AthleteId athlete, int horizontal, int vertical, bool fineControl = false)
        {
            if (tick.Value <= 0 || !athlete.IsValid) throw new System.ArgumentException("Aim command requires valid tick and athlete.");
            if (horizontal < -1000 || horizontal > 1000 || vertical < -1000 || vertical > 1000)
                throw new System.ArgumentOutOfRangeException(nameof(horizontal));
            Tick = tick; AthleteId = athlete; Horizontal = horizontal; Vertical = vertical; FineControl = fineControl;
        }
    }
}
