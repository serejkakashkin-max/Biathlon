using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SprintRegressionScenario
    {
        private SprintRegressionScenario() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "effort_control", IsRequired = true)] public int EffortControl { get; private set; }
        [DataMember(Name = "intentional_first_miss", IsRequired = true)] public bool IntentionalFirstMiss { get; private set; }
        [DataMember(Name = "snow_profile", IsRequired = true)] public string SnowProfile { get; private set; }
        [DataMember(Name = "ski_id", IsRequired = true)] public string SkiId { get; private set; }
        internal void Validate()
        { TrackValidation.Id(Id); TrackValidation.Id(SnowProfile); TrackValidation.Id(SkiId); AthleteState.ValidateEffort(EffortControl);
          if (Id.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || Id == "." || Id == "..") throw new ArgumentException("Scenario ID must be a plain filename."); }
    }
    [DataContract]
    public sealed class SprintRegressionSuite
    {
        private SprintRegressionSuite() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "tick_frequency", IsRequired = true)] public int Frequency { get; private set; }
        [DataMember(Name = "seed", IsRequired = true)] public ulong Seed { get; private set; }
        [DataMember(Name = "maximum_ticks", IsRequired = true)] public int MaximumTicks { get; private set; }
        [DataMember(Name = "maximum_command_runs", IsRequired = true)] public int MaximumCommandRuns { get; private set; }
        [DataMember(Name = "intentional_miss_yaw_rad", IsRequired = true)] public double IntentionalMissYaw { get; private set; }
        [DataMember(Name = "scenarios", IsRequired = true)] private SprintRegressionScenario[] scenarios { get; set; }
        public int Count => scenarios.Length;
        public SprintRegressionScenario Get(int i) => scenarios[i];
        public static SprintRegressionSuite FromJson(string json)
        {
            var v = RouteJson.Read<SprintRegressionSuite>(json);
            if (v == null || v.SchemaVersion != 1 || v.scenarios == null || v.scenarios.Length == 0 || v.Frequency < 10 || v.Frequency > 1000 ||
                v.MaximumTicks < 1 || v.MaximumTicks > 1000000 || v.MaximumCommandRuns < 1 || v.MaximumCommandRuns > 1000000)
                throw new SerializationException("Invalid Sprint regression suite.");
            TrackValidation.Id(v.Id); TrackValidation.Finite(v.IntentionalMissYaw, 0.001, 0.1, nameof(IntentionalMissYaw));
            for (int i = 0; i < v.Count; i++)
            { if (v.scenarios[i] == null) throw new SerializationException("Null Sprint scenario."); v.scenarios[i].Validate();
              for (int j = 0; j < i; j++) if (v.scenarios[j].Id == v.scenarios[i].Id) throw new SerializationException("Duplicate Sprint scenario ID."); }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
