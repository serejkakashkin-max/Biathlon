using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class WorkloadSegment
    {
        private WorkloadSegment() { }
        [DataMember(Name = "ticks", IsRequired = true)] public int Ticks { get; private set; }
        [DataMember(Name = "control", IsRequired = true)] public int Control { get; private set; }
        [DataMember(Name = "form", IsRequired = true)] public double Form { get; private set; }
        [DataMember(Name = "terrain_muscle_factor", IsRequired = true)] public double TerrainMuscleFactor { get; private set; }
        internal void Validate()
        {
            if (Ticks <= 0) throw new ArgumentOutOfRangeException(nameof(Ticks));
            AthleteState.ValidateEffort(Control);
            PhysiologyNumbers.RequireRange(Form, 0, 1.10, nameof(Form));
            PhysiologyNumbers.RequireRange(TerrainMuscleFactor, 0, PhysiologyNumbers.MaximumMagnitude, nameof(TerrainMuscleFactor));
        }
    }
    [DataContract]
    public sealed class PhysiologyScenario
    {
        private PhysiologyScenario() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "endurance", IsRequired = true)] public int Endurance { get; private set; }
        [DataMember(Name = "anaerobic_capacity", IsRequired = true)] public int AnaerobicCapacity { get; private set; }
        [DataMember(Name = "recovery", IsRequired = true)] public int Recovery { get; private set; }
        [DataMember(Name = "strength", IsRequired = true)] public int Strength { get; private set; }
        [DataMember(Name = "initial_hr_fraction", IsRequired = true)] public double InitialHrFraction { get; private set; }
        [DataMember(Name = "initial_reserve", IsRequired = true)] public double InitialReserve { get; private set; }
        [DataMember(Name = "initial_fatigue", IsRequired = true)] public double InitialFatigue { get; private set; }
        [DataMember(Name = "segments", IsRequired = true)] private WorkloadSegment[] Segments { get; set; }
        public long TotalTicks { get; private set; }
        public int SegmentCount => Segments.Length;
        public WorkloadSegment GetSegment(int index) => Segments[index];
        public AthletePhysiologyStats Stats => new AthletePhysiologyStats(Endurance, AnaerobicCapacity, Recovery, Strength);
        internal void Validate()
        {
            PhysiologyScenarioSuite.ValidateId(Id);
            _ = Stats;
            PhysiologyNumbers.RequireRange(InitialHrFraction, 0, 1, nameof(InitialHrFraction));
            PhysiologyNumbers.RequireRange(InitialReserve, 0, 1, nameof(InitialReserve));
            PhysiologyNumbers.RequireRange(InitialFatigue, 0, 1, nameof(InitialFatigue));
            if (Segments == null || Segments.Length == 0) throw new ArgumentException("A workload requires segments.");
            TotalTicks = 0;
            foreach (var segment in Segments)
            {
                if (segment == null) throw new ArgumentException("Null workload segment.");
                segment.Validate();
                TotalTicks = checked(TotalTicks + segment.Ticks);
            }
        }
        public WorkloadSegment AtTick(long tick)
        {
            if (tick < 1 || tick > TotalTicks) throw new ArgumentOutOfRangeException(nameof(tick));
            foreach (var segment in Segments)
            {
                if (tick <= segment.Ticks) return segment;
                tick -= segment.Ticks;
            }
            throw new InvalidOperationException("Invalid workload duration.");
        }
    }
    [DataContract]
    public sealed class PhysiologyScenarioSuite
    {
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "tick_frequency", IsRequired = true)] public int TickFrequency { get; private set; }
        [DataMember(Name = "seed", IsRequired = true)] public ulong Seed { get; private set; }
        [DataMember(Name = "scenarios", IsRequired = true)] private PhysiologyScenario[] Scenarios { get; set; }
        public int Count => Scenarios.Length;
        public PhysiologyScenario GetScenario(int index) => Scenarios[index];
        private PhysiologyScenarioSuite() { }
        internal static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A workload ID is required.");
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_' && c != '-')
                    throw new ArgumentException("Workload IDs use ASCII letters/digits, underscore and hyphen.");
        }
        public static PhysiologyScenarioSuite FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var suite = (PhysiologyScenarioSuite)new DataContractJsonSerializer(typeof(PhysiologyScenarioSuite)).ReadObject(stream);
                if (suite == null || suite.SchemaVersion != 1 || suite.TickFrequency <= 0)
                    throw new SerializationException("Unsupported workload schema/frequency.");
                ValidateId(suite.Id);
                if (suite.Scenarios == null || suite.Scenarios.Length == 0) throw new SerializationException("Scenarios required.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var scenario in suite.Scenarios)
                {
                    if (scenario == null) throw new SerializationException("Null scenario.");
                    scenario.Validate();
                    if (!ids.Add(scenario.Id)) throw new SerializationException("Duplicate scenario ID.");
                }
                return suite;
            }
        }
    }
}
