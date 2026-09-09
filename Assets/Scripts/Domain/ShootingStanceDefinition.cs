using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingStanceProfile
    {
        private ShootingStanceProfile() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "stance", IsRequired = true)] public ShootingStance Stance { get; private set; }
        [DataMember(Name = "breathing_multiplier", IsRequired = true)] public double BreathingMultiplier { get; private set; }
        [DataMember(Name = "heartbeat_multiplier", IsRequired = true)] public double HeartbeatMultiplier { get; private set; }
        [DataMember(Name = "muscular_multiplier", IsRequired = true)] public double MuscularMultiplier { get; private set; }
        [DataMember(Name = "setup_multiplier", IsRequired = true)] public double SetupMultiplier { get; private set; }
        [DataMember(Name = "exit_multiplier", IsRequired = true)] public double ExitMultiplier { get; private set; }
        [DataMember(Name = "aim_rate_multiplier", IsRequired = true)] public double AimRateMultiplier { get; private set; }
        [DataMember(Name = "recovery_multiplier", IsRequired = true)] public double RecoveryMultiplier { get; private set; }
        [DataMember(Name = "pressure_sensitivity", IsRequired = true)] public double PressureSensitivity { get; private set; }
        internal void Validate()
        {
            TrackValidation.Id(Id);
            if (Stance != ShootingStance.Prone && Stance != ShootingStance.Standing) throw new ArgumentException("Unknown shooting stance.");
            TrackValidation.Finite(BreathingMultiplier, 0.01, 4, nameof(BreathingMultiplier));
            TrackValidation.Finite(HeartbeatMultiplier, 0.01, 4, nameof(HeartbeatMultiplier));
            TrackValidation.Finite(MuscularMultiplier, 0.01, 4, nameof(MuscularMultiplier));
            TrackValidation.Finite(SetupMultiplier, 0.1, 4, nameof(SetupMultiplier));
            TrackValidation.Finite(ExitMultiplier, 0.1, 4, nameof(ExitMultiplier));
            TrackValidation.Finite(AimRateMultiplier, 0.1, 2, nameof(AimRateMultiplier));
            TrackValidation.Finite(RecoveryMultiplier, 0.1, 2, nameof(RecoveryMultiplier));
            TrackValidation.Finite(PressureSensitivity, 0.1, 2, nameof(PressureSensitivity));
        }
    }
    [DataContract]
    public sealed class ShootingStanceDefinition
    {
        private ShootingStanceDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "prone", IsRequired = true)] public ShootingStanceProfile Prone { get; private set; }
        [DataMember(Name = "standing", IsRequired = true)] public ShootingStanceProfile Standing { get; private set; }
        public static ShootingStanceDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingStanceDefinition>(json);
            if (v == null || v.SchemaVersion != 1 || v.Prone == null || v.Standing == null) throw new SerializationException("Invalid stance schema.");
            TrackValidation.Id(v.Id); v.Prone.Validate(); v.Standing.Validate();
            if (v.Prone.Stance != ShootingStance.Prone || v.Standing.Stance != ShootingStance.Standing || v.Prone.Id == v.Standing.Id)
                throw new ArgumentException("Stance identities must be unique and match their slots.");
            if (v.Prone.BreathingMultiplier > v.Standing.BreathingMultiplier || v.Prone.HeartbeatMultiplier > v.Standing.HeartbeatMultiplier ||
                v.Prone.MuscularMultiplier > v.Standing.MuscularMultiplier || v.Prone.RecoveryMultiplier < v.Standing.RecoveryMultiplier ||
                v.Prone.PressureSensitivity > v.Standing.PressureSensitivity)
                throw new ArgumentException("Prone must preserve the GDD stability/recovery advantage.");
            return v;
        }
        public ShootingStanceProfile Get(ShootingStance stance) => stance == ShootingStance.Prone ? Prone :
            stance == ShootingStance.Standing ? Standing : throw new ArgumentOutOfRangeException(nameof(stance));
        public string ToJson() => RouteJson.Write(this);
    }
}
