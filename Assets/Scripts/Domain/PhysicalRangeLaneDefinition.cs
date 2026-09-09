using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Equal-length physical alternatives for the range portion of a bout lap.
    /// The first version requires one constant terrain segment; it cannot disguise a
    /// different slope/surface/width behind the base-route physics.</summary>
    [DataContract]
    public sealed class PhysicalRangeLaneDefinition
    {
        private PhysicalRangeLaneDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "track_id", IsRequired = true)] public string TrackId { get; private set; }
        [DataMember(Name = "main_loop_id", IsRequired = true)] public string MainLoopId { get; private set; }
        [DataMember(Name = "entry_distance_m", IsRequired = true)] private double EntryMeters { get; set; }
        [DataMember(Name = "exit_distance_m", IsRequired = true)] private double ExitMeters { get; set; }
        [DataMember(Name = "lane_loop_ids", IsRequired = true)] private string[] Lanes { get; set; }
        public TrackDistance Entry { get; private set; }
        public TrackDistance Exit { get; private set; }
        public int Count => Lanes.Length;
        public string GetLane(int index) => Lanes[index];
        public string TrackSnapshotJson { get; private set; }
        public static PhysicalRangeLaneDefinition FromJson(string json, TrackDefinition track)
        {
            if (track == null) throw new ArgumentNullException(nameof(track));
            var v = RouteJson.Read<PhysicalRangeLaneDefinition>(json);
            if (v == null || v.SchemaVersion != 1 || v.Lanes == null || v.Lanes.Length == 0)
                throw new SerializationException("Invalid range lane schema.");
            TrackValidation.Id(v.Id); TrackValidation.Id(v.TrackId); TrackValidation.Id(v.MainLoopId);
            v.Entry = TrackDistance.FromMeters(v.EntryMeters); v.Exit = TrackDistance.FromMeters(v.ExitMeters);
            if (v.TrackId != track.Id || v.MainLoopId != track.Range.Entry.LoopId || v.MainLoopId != track.Penalty.Entry.LoopId ||
                v.Entry.Millimeters >= track.Range.Entry.Distance.Millimeters || v.Exit.Millimeters != track.Penalty.Entry.Distance.Millimeters ||
                v.Exit.Millimeters <= track.Range.FiringLine.Distance.Millimeters)
                throw new ArgumentException("Range alternatives must fork before range entry and join at penalty access.");
            var main = track.GetLoop(v.MainLoopId); TrackSegmentDefinition source = null;
            for (int i = 0; i < main.SegmentCount; i++) if (main.GetSegment(i).StartDistance.Equals(v.Entry)) source = main.GetSegment(i);
            if (source == null || source.StartGrade != source.EndGrade || v.Exit.Millimeters > source.StartDistance.Millimeters + source.Length.Millimeters)
                throw new ArgumentException("Range fork must begin at a constant terrain segment boundary and remain within it.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in v.Lanes)
            {
                TrackValidation.Id(id); var lane = track.GetLoop(id);
                if (!ids.Add(id) || id == v.MainLoopId || lane.Kind != TrackLoopKind.Race || lane.Closed || lane.SegmentCount != 1 ||
                    lane.Length.Millimeters != v.Exit.Millimeters - v.Entry.Millimeters)
                    throw new ArgumentException("Distinct open equal-length range lanes required.");
                var s = lane.GetSegment(0);
                if (s.StartGrade != source.StartGrade || s.EndGrade != source.EndGrade || s.CurvaturePerMeter != source.CurvaturePerMeter ||
                    s.WidthMeters != source.WidthMeters || s.SnowProfileId != source.SnowProfileId || s.WindExposure != source.WindExposure ||
                    s.TerrainMuscleFactor != source.TerrainMuscleFactor || s.DraftAllowed != source.DraftAllowed ||
                    s.OvertakeAllowed != source.OvertakeAllowed || s.RiskLevel != source.RiskLevel || s.SemanticZone != source.SemanticZone)
                    throw new ArgumentException("Range lane physics must match the replaced base-route segment.");
            }
            v.TrackSnapshotJson = track.ToJson(); return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
