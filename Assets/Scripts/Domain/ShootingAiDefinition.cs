using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingAiDefinition
    {
        private ShootingAiDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "reaction_seconds", IsRequired = true)] public double ReactionSeconds { get; private set; }
        [DataMember(Name = "tracking_error_rad", IsRequired = true)] public double TrackingError { get; private set; }
        [DataMember(Name = "tracking_frequency_hz", IsRequired = true)] public double TrackingFrequency { get; private set; }
        [DataMember(Name = "aim_gain_per_s", IsRequired = true)] public double AimGain { get; private set; }
        [DataMember(Name = "fire_tolerance_rad", IsRequired = true)] public double FireTolerance { get; private set; }
        [DataMember(Name = "stable_seconds", IsRequired = true)] public double StableSeconds { get; private set; }
        [DataMember(Name = "maximum_aim_seconds", IsRequired = true)] public double MaximumAimSeconds { get; private set; }
        public static ShootingAiDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingAiDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported shooting AI schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.ReactionSeconds, 0.01, 2, nameof(ReactionSeconds));
            TrackValidation.Finite(v.TrackingError, 0, 0.005, nameof(TrackingError));
            TrackValidation.Finite(v.TrackingFrequency, 0.01, 2, nameof(TrackingFrequency));
            TrackValidation.Finite(v.AimGain, 0.1, 10, nameof(AimGain));
            TrackValidation.Finite(v.FireTolerance, 0.000001, 0.005, nameof(FireTolerance));
            TrackValidation.Finite(v.StableSeconds, 0.01, 2, nameof(StableSeconds));
            TrackValidation.Finite(v.MaximumAimSeconds, v.StableSeconds, 30, nameof(MaximumAimSeconds));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
        internal ShootingAiDefinition WithDifficulty(AiDifficultyDefinition d)=>new ShootingAiDefinition
        { SchemaVersion=SchemaVersion,Id=d.Id+"_aim",ReactionSeconds=d.ReactionSeconds,TrackingError=d.TrackingError,
          TrackingFrequency=TrackingFrequency,AimGain=AimGain,FireTolerance=FireTolerance,StableSeconds=StableSeconds,MaximumAimSeconds=MaximumAimSeconds };
    }
}
