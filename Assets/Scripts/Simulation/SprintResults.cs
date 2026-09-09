using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum RaceResultStatus { Finished = 0, DidNotFinish = 1, DidNotStart = 2, Invalid = 3 }
    [DataContract]
    public sealed class SprintResultRow
    {
        private SprintResultRow() { }
        [DataMember(Name = "athlete_id", IsRequired = true)] public string AthleteId { get; private set; }
        [DataMember(Name = "status", IsRequired = true)] public RaceResultStatus Status { get; private set; }
        [DataMember(Name = "rank", IsRequired = true)] public int Rank { get; private set; }
        [DataMember(Name = "registered_units", IsRequired = true)] public long? RegisteredUnits { get; private set; }
        [DataMember(Name = "gap_units", IsRequired = true)] public long? GapUnits { get; private set; }
        [DataMember(Name = "elapsed_whole_ticks", IsRequired = true)] public long? ElapsedWholeTicks { get; private set; }
        [DataMember(Name = "elapsed_fraction_parts", IsRequired = true)] public int? ElapsedFractionParts { get; private set; }
        internal static SprintResultRow Create(AthleteId athlete, RaceResultStatus status, FinishTime elapsed, long? registered) =>
            new SprintResultRow { AthleteId = athlete.Value, Status = status, RegisteredUnits = registered,
                ElapsedWholeTicks = status == RaceResultStatus.Finished ? elapsed.WholeTicks : (long?)null,
                ElapsedFractionParts = status == RaceResultStatus.Finished ? elapsed.FractionParts : (int?)null };
        internal SprintResultRow Ranked(int rank, long? gap) => new SprintResultRow { AthleteId = AthleteId, Status = Status, Rank = rank,
            RegisteredUnits = RegisteredUnits, GapUnits = gap, ElapsedWholeTicks = ElapsedWholeTicks, ElapsedFractionParts = ElapsedFractionParts };
    }
    [DataContract]
    public sealed class SprintResultSnapshot
    {
        private SprintResultSnapshot() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "competition_profile_json", IsRequired = true)] public string CompetitionProfileJson { get; private set; }
        [DataMember(Name = "timing_definition_json", IsRequired = true)] public string TimingDefinitionJson { get; private set; }
        [DataMember(Name = "tick_frequency", IsRequired = true)] public int TickFrequency { get; private set; }
        [DataMember(Name = "rows", IsRequired = true)] private SprintResultRow[] rows;
        public int Count => rows.Length;
        public SprintResultRow Get(int index) => rows[index];
        internal static SprintResultSnapshot Create(string id, CompetitionProfile profile, ResultTimingDefinition timing, int frequency, SprintResultRow[] rows)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Result snapshot ID required.");
            Array.Sort(rows, Compare);
            int rank = 0; long leader = 0, previous = -1;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row.Status != RaceResultStatus.Finished) { rows[i] = row.Ranked(0, null); continue; }
                long units = row.RegisteredUnits.Value;
                if (i == 0) leader = units;
                if (units != previous) rank = i + 1;
                rows[i] = row.Ranked(rank, units - leader); previous = units;
            }
            return new SprintResultSnapshot { SchemaVersion = 1, Id = id, CompetitionProfileJson = profile.ToJson(),
                TimingDefinitionJson = timing.ToJson(), TickFrequency = frequency, rows = rows };
        }
        private static int Compare(SprintResultRow a, SprintResultRow b)
        {
            int order = a.Status.CompareTo(b.Status);
            if (order == 0 && a.Status == RaceResultStatus.Finished) order = a.RegisteredUnits.Value.CompareTo(b.RegisteredUnits.Value);
            return order != 0 ? order : StringComparer.Ordinal.Compare(a.AthleteId, b.AthleteId);
        }
        public string ToJson()
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(SprintResultSnapshot)).WriteObject(stream, this); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public static SprintResultSnapshot FromJson(string json)
        {
            SprintResultSnapshot value;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? throw new ArgumentNullException(nameof(json)))))
                value = (SprintResultSnapshot)new DataContractJsonSerializer(typeof(SprintResultSnapshot)).ReadObject(stream);
            if (value == null || value.SchemaVersion != 1 || string.IsNullOrWhiteSpace(value.Id) || value.TickFrequency <= 0 || value.rows == null || value.rows.Length == 0)
                throw new SerializationException("Invalid Sprint result snapshot.");
            var profile = CompetitionProfile.FromJson(value.CompetitionProfileJson); var timing = ResultTimingDefinition.FromJson(value.TimingDefinitionJson);
            if (profile.Format != CompetitionFormat.Sprint || value.Count > profile.FieldCapacity) throw new SerializationException("Invalid result profile/capacity.");
            var ids = new HashSet<string>(StringComparer.Ordinal); var rebuilt = new SprintResultRow[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                var row = value.rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.AthleteId) || !ids.Add(row.AthleteId) || row.Status < RaceResultStatus.Finished || row.Status > RaceResultStatus.Invalid)
                    throw new SerializationException("Invalid/duplicate result row.");
                FinishTime elapsed = default; long? units = null;
                if (row.Status == RaceResultStatus.Finished)
                {
                    if (!row.ElapsedWholeTicks.HasValue || !row.ElapsedFractionParts.HasValue) throw new SerializationException("Finisher requires exact time.");
                    elapsed = new FinishTime(row.ElapsedWholeTicks.Value, row.ElapsedFractionParts.Value, value.TickFrequency); units = timing.Register(elapsed);
                }
                else if (row.ElapsedWholeTicks.HasValue || row.ElapsedFractionParts.HasValue) throw new SerializationException("Non-finisher cannot have a finish time.");
                rebuilt[i] = SprintResultRow.Create(new AthleteId(row.AthleteId), row.Status, elapsed, units);
            }
            var canonical = Create(value.Id, profile, timing, value.TickFrequency, rebuilt);
            for (int i = 0; i < value.Count; i++)
            {
                var a = value.rows[i]; var b = canonical.rows[i];
                if (a.AthleteId != b.AthleteId || a.Rank != b.Rank || a.RegisteredUnits != b.RegisteredUnits || a.GapUnits != b.GapUnits)
                    throw new SerializationException("Result order, registration, ranks or gaps were altered.");
            }
            return canonical;
        }
    }
    public sealed class SprintResultBook
    {
        private readonly CompetitionProfile profile;
        private readonly string profileJson;
        private readonly ResultTimingDefinition timing;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private readonly Dictionary<AthleteId, int> roster;
        private readonly SprintResultRow[] rows;
        public int Count { get; private set; }
        public SprintResultBook(CompetitionProfile profile, ResultTimingDefinition timing, ReadOnlySpan<AthleteId> athletes, int frequency, IRaceEventSink events)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile)); this.timing = timing ?? throw new ArgumentNullException(nameof(timing));
            profileJson = profile.ToJson();
            if (profile.Format != CompetitionFormat.Sprint || frequency <= 0 || athletes.Length == 0 || athletes.Length > profile.FieldCapacity)
                throw new ArgumentException("Valid Sprint roster and clock required.");
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.frequency = frequency;
            roster = new Dictionary<AthleteId, int>(); rows = new SprintResultRow[athletes.Length];
            for (int i = 0; i < athletes.Length; i++)
            { if (!athletes[i].IsValid || roster.ContainsKey(athletes[i])) throw new ArgumentException("Unique roster required."); roster.Add(athletes[i], i); }
        }
        public void Record(CompetitionLifecycle lifecycle)
        {
            if (lifecycle == null || lifecycle.CompetitionProfileJson != profileJson || lifecycle.TickFrequency != frequency ||
                !roster.TryGetValue(lifecycle.AthleteId, out int slot)) throw new ArgumentException("Incompatible result lifecycle.");
            if (rows[slot] != null) throw new InvalidOperationException("Result already recorded.");
            RaceResultStatus status;
            switch (lifecycle.Phase)
            {
                case CompetitionPhase.Finished: case CompetitionPhase.Classified: status = RaceResultStatus.Finished; break;
                case CompetitionPhase.DidNotFinish: status = RaceResultStatus.DidNotFinish; break;
                case CompetitionPhase.DidNotStart: status = RaceResultStatus.DidNotStart; break;
                case CompetitionPhase.Invalid: status = RaceResultStatus.Invalid; break;
                default: throw new InvalidOperationException("Only a terminal race can produce a result.");
            }
            var row = SprintResultRow.Create(lifecycle.AthleteId, status, lifecycle.ElapsedFinishTime,
                status == RaceResultStatus.Finished ? timing.Register(lifecycle.ElapsedFinishTime) : (long?)null);
            rows[slot] = row; Count++;
            events.Record(new RaceEvent(lifecycle.Tick, frequency, lifecycle.AthleteId, RaceEventType.CompetitionResultRecorded, (int)status));
        }
        public SprintResultSnapshot Complete(string resultId)
        {
            if (Count != rows.Length) throw new InvalidOperationException("Every roster entrant needs an explicit terminal result.");
            return SprintResultSnapshot.Create(resultId, profile, timing, frequency, (SprintResultRow[])rows.Clone());
        }
    }
}
