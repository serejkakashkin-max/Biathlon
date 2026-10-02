using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    /// <summary>Route-facing projection; full format/start/shooting rules are introduced by WP-053.</summary>
    [DataContract]
    public sealed class CompetitionRouteRequirements
    {
        private CompetitionRouteRequirements() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "lap_count", IsRequired = true)] public int LapCount { get; private set; }
        [DataMember(Name = "distance_m", IsRequired = true)] public double DistanceMeters { get; private set; }
        [DataMember(Name = "range_required_on_laps", IsRequired = true)] private int[] RangeLaps { get; set; }
        public TrackDistance Distance { get; private set; }
        public bool RequiresRange(int zeroBasedLap)
        {
            if (zeroBasedLap < 0 || zeroBasedLap >= LapCount) throw new ArgumentOutOfRangeException(nameof(zeroBasedLap));
            return Array.BinarySearch(RangeLaps, zeroBasedLap + 1) >= 0;
        }
        public static CompetitionRouteRequirements FromJson(string json)
        {
            var result = RouteJson.Read<CompetitionRouteRequirements>(json);
            if (result == null || result.SchemaVersion != 1 || result.LapCount <= 0 || result.RangeLaps == null)
                throw new SerializationException("Invalid route requirements schema/laps.");
            TrackValidation.Id(result.Id);
            result.Distance = TrackDistance.FromMeters(result.DistanceMeters);
            if (result.Distance.Millimeters == 0) throw new ArgumentException("Positive race distance required.");
            int previous = 0;
            foreach (int lap in result.RangeLaps)
            {
                if (lap <= previous || lap > result.LapCount) throw new ArgumentException("Range lap numbers must be ordered, unique and within 1..lap_count.");
                previous = lap;
            }
            return result;
        }
        public string ToJson() => RouteJson.Write(this);
        internal static CompetitionRouteRequirements FromProfile(string id, int lapCount, TrackDistance distance, int[] rangeLaps) =>
            FromJson(RouteJson.Write(new CompetitionRouteRequirements { SchemaVersion = 1, Id = id, LapCount = lapCount,
                DistanceMeters = distance.Meters, RangeLaps = rangeLaps }));
        public static CompetitionRouteRequirements TimeTrial(string id, TrackDistance lapDistance, int lapCount)
        {
            TrackValidation.Id(id);
            if (lapCount <= 0 || lapDistance.Millimeters <= 0) throw new ArgumentOutOfRangeException(nameof(lapCount));
            return FromJson(RouteJson.Write(new CompetitionRouteRequirements {
                SchemaVersion = 1, Id = id, LapCount = lapCount,
                DistanceMeters = new TrackDistance(checked(lapDistance.Millimeters * lapCount)).Meters, RangeLaps = Array.Empty<int>() }));
        }
    }
    [DataContract]
    public sealed class RaceRouteSlicePlan
    {
        private RaceRouteSlicePlan() { }
        [DataMember(Name = "loop_id", IsRequired = true)] public string LoopId { get; private set; }
        [DataMember(Name = "start_m", IsRequired = true)] public double StartMeters { get; private set; }
        [DataMember(Name = "end_m", IsRequired = true)] public double EndMeters { get; private set; }
        [DataMember(Name = "enter_connection_id", EmitDefaultValue = false)] public string EnterConnectionId { get; private set; }
        public TrackDistance Start { get; private set; }
        public TrackDistance End { get; private set; }
        internal void Validate()
        {
            TrackValidation.Id(LoopId);
            if (EnterConnectionId != null) TrackValidation.Id(EnterConnectionId);
            Start = TrackDistance.FromMeters(StartMeters); End = TrackDistance.FromMeters(EndMeters);
            if (End.Millimeters <= Start.Millimeters) throw new ArgumentException("Route slices must be positive, directed and nonwrapping.");
        }
    }
    [DataContract]
    public sealed class RaceLapPlan
    {
        private RaceLapPlan() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "loop_ids", EmitDefaultValue = false)] private string[] LoopIds { get; set; }
        [DataMember(Name = "slices", EmitDefaultValue = false)] private RaceRouteSlicePlan[] Slices { get; set; }
        public int LoopCount => Slices == null ? LoopIds.Length : Slices.Length;
        public string GetLoopId(int index) => Slices == null ? LoopIds[index] : Slices[index].LoopId;
        public RaceRouteSlicePlan GetSlice(int index) => Slices == null ? throw new InvalidOperationException("Legacy laps contain whole loop occurrences.") : Slices[index];
        internal static RaceLapPlan SingleLoop(string id, string loopId) => new RaceLapPlan { Id = id, LoopIds = new[] { loopId } };
        internal void Validate(int version)
        {
            TrackValidation.Id(Id);
            if (version == 1)
            {
                if (LoopIds == null || LoopIds.Length == 0 || Slices != null) throw new ArgumentException("Legacy laps require whole loop occurrences only.");
                foreach (string id in LoopIds) TrackValidation.Id(id);
            }
            else
            {
                if (LoopIds != null || Slices == null || Slices.Length == 0) throw new ArgumentException("Schema2 laps require explicit physical slices only.");
                foreach (var slice in Slices) { if (slice == null) throw new ArgumentException("Null route slice."); slice.Validate(); }
            }
        }
    }
    [DataContract]
    public sealed class RaceRoutePlan
    {
        private RaceRoutePlan() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "track_id", IsRequired = true)] public string TrackId { get; private set; }
        [DataMember(Name = "competition_profile_id", IsRequired = true)] public string CompetitionProfileId { get; private set; }
        [DataMember(Name = "laps", IsRequired = true)] private RaceLapPlan[] Laps { get; set; }
        public int LapCount => Laps.Length;
        public RaceLapPlan GetLap(int index) => Laps[index];
        public static RaceRoutePlan FromJson(string json)
        {
            var result = RouteJson.Read<RaceRoutePlan>(json);
            if (result == null || (result.SchemaVersion != 1 && result.SchemaVersion != 2) || result.Laps == null || result.Laps.Length == 0)
                throw new SerializationException("Invalid route plan schema/laps.");
            TrackValidation.Id(result.Id); TrackValidation.Id(result.TrackId); TrackValidation.Id(result.CompetitionProfileId);
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var lap in result.Laps)
            {
                if (lap == null) throw new ArgumentException("Null lap.");
                lap.Validate(result.SchemaVersion);
                if (!ids.Add(lap.Id)) throw new ArgumentException("Duplicate lap ID.");
            }
            return result;
        }
        public string ToJson() => RouteJson.Write(this);
        public static RaceRoutePlan TimeTrial(string id, string trackId, string loopId, CompetitionRouteRequirements requirements)
        {
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            var laps = new RaceLapPlan[requirements.LapCount];
            for (int i = 0; i < laps.Length; i++) laps[i] = RaceLapPlan.SingleLoop("lap_" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), loopId);
            return FromJson(RouteJson.Write(new RaceRoutePlan { SchemaVersion = 1, Id = id, TrackId = trackId, CompetitionProfileId = requirements.Id, Laps = laps }));
        }
    }
    internal static class RouteJson
    {
        internal static T Read<T>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        internal static string Write<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
