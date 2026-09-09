using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingTrainingScenario
    {
        private ShootingTrainingScenario() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "tick_frequency", IsRequired = true)] public int Frequency { get; private set; }
        [DataMember(Name = "seed", IsRequired = true)] public ulong Seed { get; private set; }
        [DataMember(Name = "maximum_ticks", IsRequired = true)] public int MaximumTicks { get; private set; }
        [DataMember(Name = "stance", IsRequired = true)] public ShootingStance Stance { get; private set; }
        [DataMember(Name = "hr_bpm", IsRequired = true)] public double HeartRate { get; private set; }
        [DataMember(Name = "fatigue", IsRequired = true)] public double Fatigue { get; private set; }
        [DataMember(Name = "arm_fatigue", IsRequired = true)] public double ArmFatigue { get; private set; }
        [DataMember(Name = "crosswind_mps", IsRequired = true)] public double Crosswind { get; private set; }
        [DataMember(Name = "exposure", IsRequired = true)] public double Exposure { get; private set; }
        [DataMember(Name = "aim_period_ticks", IsRequired = true)] public int AimPeriod { get; private set; }
        [DataMember(Name = "aim_horizontal_control", IsRequired = true)] public int AimHorizontal { get; private set; }
        [DataMember(Name = "hold_period_ticks", IsRequired = true)] public int HoldPeriod { get; private set; }
        [DataMember(Name = "hold_ticks", IsRequired = true)] public int HoldTicks { get; private set; }
        public static ShootingTrainingScenario FromJson(string json)
        {
            var v = RouteJson.Read<ShootingTrainingScenario>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported shooting scenario schema.");
            TrackValidation.Id(v.Id);
            if (v.Frequency < 1 || v.Frequency > 10000 || v.MaximumTicks < 1 || v.MaximumTicks > 1000000 ||
                v.AimPeriod < 2 || v.HoldPeriod < 1 || v.HoldTicks < 0 || v.HoldTicks > v.HoldPeriod || v.AimHorizontal < 0 || v.AimHorizontal > 1000 ||
                (v.Stance != ShootingStance.Prone && v.Stance != ShootingStance.Standing)) throw new SerializationException("Invalid shooting scenario intervals/control.");
            TrackValidation.Finite(v.HeartRate, 1, 1000000, nameof(HeartRate)); TrackValidation.Finite(v.Fatigue, 0, 1, nameof(Fatigue));
            TrackValidation.Finite(v.ArmFatigue, 0, 1, nameof(ArmFatigue)); TrackValidation.Finite(v.Crosswind, -100, 100, nameof(Crosswind));
            TrackValidation.Finite(v.Exposure, 0, 1, nameof(Exposure));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
