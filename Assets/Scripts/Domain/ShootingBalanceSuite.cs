using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingBalanceScenario
    {
        private ShootingBalanceScenario() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "ai_definition", IsRequired = true)] public string AiDefinition { get; private set; }
        [DataMember(Name = "athlete_definition", EmitDefaultValue = false)] public string AthleteDefinition { get; private set; }
        [DataMember(Name = "stance", IsRequired = true)] public ShootingStance Stance { get; private set; }
        [DataMember(Name = "hr_bpm", IsRequired = true)] public double HeartRate { get; private set; }
        [DataMember(Name = "fatigue", IsRequired = true)] public double Fatigue { get; private set; }
        [DataMember(Name = "arm_fatigue", IsRequired = true)] public double ArmFatigue { get; private set; }
        [DataMember(Name = "crosswind_mps", IsRequired = true)] public double Crosswind { get; private set; }
        [DataMember(Name = "estimated_crosswind_mps", EmitDefaultValue = false)] public double EstimatedCrosswind { get; private set; }
        internal void Validate()
        {
            TrackValidation.Id(Id); TrackValidation.Id(AiDefinition);
            if (AthleteDefinition != null)
            {
                TrackValidation.Id(AthleteDefinition);
                if (AthleteDefinition.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || AthleteDefinition == "." || AthleteDefinition == "..")
                    throw new ArgumentException("Athlete definition must be a local data ID.");
            }
            if (AiDefinition.IndexOfAny(new[] { '/', '\\', ':' }) >= 0) throw new ArgumentException("AI definition must be a local data ID.");
            if (Stance != ShootingStance.Prone && Stance != ShootingStance.Standing) throw new ArgumentException("Unknown stance.");
            TrackValidation.Finite(HeartRate, 1, 1000000, nameof(HeartRate)); TrackValidation.Finite(Fatigue, 0, 1, nameof(Fatigue));
            TrackValidation.Finite(ArmFatigue, 0, 1, nameof(ArmFatigue)); TrackValidation.Finite(Crosswind, -100, 100, nameof(Crosswind));
            TrackValidation.Finite(EstimatedCrosswind, -100, 100, nameof(EstimatedCrosswind));
        }
    }
    [DataContract]
    public sealed class ShootingBalanceSuite
    {
        private ShootingBalanceSuite() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "tick_frequency", IsRequired = true)] public int Frequency { get; private set; }
        [DataMember(Name = "seed_start", IsRequired = true)] public ulong SeedStart { get; private set; }
        [DataMember(Name = "samples_per_scenario", IsRequired = true)] public int Samples { get; private set; }
        [DataMember(Name = "maximum_ticks", IsRequired = true)] public int MaximumTicks { get; private set; }
        [DataMember(Name = "scenarios", IsRequired = true)] private ShootingBalanceScenario[] scenarios { get; set; }
        public int Count => scenarios.Length;
        public ShootingBalanceScenario Get(int index) => scenarios[index];
        public static ShootingBalanceSuite FromJson(string json)
        {
            var v = RouteJson.Read<ShootingBalanceSuite>(json);
            if (v == null || v.SchemaVersion != 1 || v.scenarios == null || v.scenarios.Length == 0) throw new SerializationException("Unsupported shooting balance schema.");
            TrackValidation.Id(v.Id);
            if (v.Frequency < 1 || v.Frequency > 10000 || v.Samples < 1 || v.Samples > 10000 || v.MaximumTicks < 1 || v.MaximumTicks > 1000000 ||
                v.SeedStart > ulong.MaxValue - (ulong)v.Samples) throw new SerializationException("Invalid suite size/clock/seeds.");
            for (int i = 0; i < v.Count; i++)
            {
                if (v.scenarios[i] == null) throw new SerializationException("Null shooting balance scenario.");
                v.scenarios[i].Validate();
                for (int j = 0; j < i; j++) if (v.scenarios[i].Id == v.scenarios[j].Id) throw new SerializationException("Duplicate balance scenario.");
            }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
