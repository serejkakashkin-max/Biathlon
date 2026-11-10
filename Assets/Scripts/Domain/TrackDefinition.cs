using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    public enum TrackLoopKind { Race, Penalty }
    public enum TrackSemanticZone { Flat, ClimbLong, ClimbSteep, Rolling, DescentFast, DescentTechnical, Stadium, RangeApproach, PenaltyLoop }

    [DataContract]
    public sealed class TrackAuthoringLimits
    {
        private TrackAuthoringLimits() { }
        [DataMember(Name = "max_abs_grade", IsRequired = true)] public double MaxAbsGrade { get; private set; }
        [DataMember(Name = "max_abs_curvature_per_m", IsRequired = true)] public double MaxAbsCurvaturePerMeter { get; private set; }
        [DataMember(Name = "min_width_m", IsRequired = true)] public double MinWidthMeters { get; private set; }
        [DataMember(Name = "max_width_m", IsRequired = true)] public double MaxWidthMeters { get; private set; }
        internal void Validate()
        {
            TrackValidation.Finite(MaxAbsGrade, 0, 1000000, nameof(MaxAbsGrade));
            TrackValidation.Finite(MaxAbsCurvaturePerMeter, 0, 1000000, nameof(MaxAbsCurvaturePerMeter));
            TrackValidation.Finite(MinWidthMeters, 0.001, 1000000, nameof(MinWidthMeters));
            TrackValidation.Finite(MaxWidthMeters, MinWidthMeters, 1000000, nameof(MaxWidthMeters));
        }
    }
    [DataContract]
    public sealed class TrackSegmentDefinition
    {
        private TrackSegmentDefinition() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "length_m", IsRequired = true)] public double LengthMeters { get; private set; }
        [DataMember(Name = "start_grade", IsRequired = true)] public double StartGrade { get; private set; }
        [DataMember(Name = "end_grade", IsRequired = true)] public double EndGrade { get; private set; }
        [DataMember(Name = "curvature_per_m", IsRequired = true)] public double CurvaturePerMeter { get; private set; }
        [DataMember(Name = "width_m", IsRequired = true)] public double WidthMeters { get; private set; }
        [DataMember(Name = "snow_profile_id", IsRequired = true)] public string SnowProfileId { get; private set; }
        [DataMember(Name = "wind_exposure", IsRequired = true)] public double WindExposure { get; private set; }
        [DataMember(Name = "terrain_muscle_factor", IsRequired = true)] public double TerrainMuscleFactor { get; private set; }
        [DataMember(Name = "draft_allowed", IsRequired = true)] public bool DraftAllowed { get; private set; }
        [DataMember(Name = "overtake_allowed", IsRequired = true)] public bool OvertakeAllowed { get; private set; }
        [DataMember(Name = "risk_level", IsRequired = true)] public double RiskLevel { get; private set; }
        [DataMember(Name = "semantic_zone", IsRequired = true)] private string SemanticZoneName { get; set; }
        public TrackSemanticZone SemanticZone { get; private set; }
        public TrackDistance Length { get; private set; }
        public TrackDistance StartDistance { get; private set; }
        internal void Validate(TrackAuthoringLimits limits, long startMillimeters)
        {
            TrackValidation.Id(Id); TrackValidation.Id(SnowProfileId);
            Length = TrackDistance.FromMeters(LengthMeters);
            if (Length.Millimeters == 0) throw new ArgumentException("Segments require positive length.");
            StartDistance = new TrackDistance(startMillimeters);
            TrackValidation.Finite(StartGrade, -limits.MaxAbsGrade, limits.MaxAbsGrade, nameof(StartGrade));
            TrackValidation.Finite(EndGrade, -limits.MaxAbsGrade, limits.MaxAbsGrade, nameof(EndGrade));
            TrackValidation.Finite(CurvaturePerMeter, -limits.MaxAbsCurvaturePerMeter, limits.MaxAbsCurvaturePerMeter, nameof(CurvaturePerMeter));
            TrackValidation.Finite(WidthMeters, limits.MinWidthMeters, limits.MaxWidthMeters, nameof(WidthMeters));
            TrackValidation.Finite(WindExposure, 0, 1, nameof(WindExposure));
            TrackValidation.Finite(TerrainMuscleFactor, 0, 1000000, nameof(TerrainMuscleFactor));
            TrackValidation.Finite(RiskLevel, 0, 1, nameof(RiskLevel));
            SemanticZone = SemanticZoneName switch {
                "flat" => TrackSemanticZone.Flat, "climb_long" => TrackSemanticZone.ClimbLong,
                "climb_steep" => TrackSemanticZone.ClimbSteep, "rolling" => TrackSemanticZone.Rolling,
                "descent_fast" => TrackSemanticZone.DescentFast, "descent_technical" => TrackSemanticZone.DescentTechnical,
                "stadium" => TrackSemanticZone.Stadium, "range_approach" => TrackSemanticZone.RangeApproach,
                "penalty_loop" => TrackSemanticZone.PenaltyLoop, _ => throw new ArgumentException("Unknown semantic zone.")
            };
        }
    }
    [DataContract]
    public sealed class TrackLoopDefinition
    {
        private TrackLoopDefinition() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "kind", IsRequired = true)] private string KindName { get; set; }
        [DataMember(Name = "closed", IsRequired = true)] public bool Closed { get; private set; }
        [DataMember(Name = "segments", IsRequired = true)] private TrackSegmentDefinition[] Segments { get; set; }
        public TrackLoopKind Kind { get; private set; }
        public TrackDistance Length { get; private set; }
        public int SegmentCount => Segments.Length;
        public TrackSegmentDefinition GetSegment(int index) => Segments[index];
        internal void Validate(TrackAuthoringLimits limits)
        {
            TrackValidation.Id(Id);
            Kind = KindName switch { "race" => TrackLoopKind.Race, "penalty" => TrackLoopKind.Penalty, _ => throw new ArgumentException("Unknown loop kind.") };
            if (Segments == null || Segments.Length == 0) throw new ArgumentException("Loop segments required.");
            long length = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in Segments)
            {
                if (segment == null) throw new ArgumentException("Null segment.");
                segment.Validate(limits, length);
                if (!ids.Add(segment.Id)) throw new ArgumentException("Duplicate segment ID within loop.");
                length = checked(length + segment.Length.Millimeters);
                if (length > 1000000000) throw new ArgumentException("Loop exceeds numeric coordinate domain.");
            }
            Length = new TrackDistance(length);
        }
    }
    [DataContract]
    public sealed class TrackAnchor
    {
        private TrackAnchor() { }
        [DataMember(Name = "loop_id", IsRequired = true)] public string LoopId { get; private set; }
        [DataMember(Name = "distance_m", IsRequired = true)] public double DistanceMeters { get; private set; }
        public TrackDistance Distance { get; private set; }
        internal TrackLoopDefinition Validate(TrackDefinition track)
        {
            TrackValidation.Id(LoopId);
            var loop = track.GetLoop(LoopId);
            Distance = TrackDistance.FromMeters(DistanceMeters);
            if (Distance.Millimeters > loop.Length.Millimeters) throw new ArgumentException("Anchor lies outside loop.");
            return loop;
        }
    }
    [DataContract]
    public sealed class TrackCheckpointDefinition
    {
        private TrackCheckpointDefinition() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "anchor", IsRequired = true)] public TrackAnchor Anchor { get; private set; }
        [DataMember(Name = "broadcast_only", EmitDefaultValue = false)] public bool BroadcastOnly { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);
            if (Anchor == null) throw new ArgumentException("Checkpoint anchor required.");
            Anchor.Validate(track);
        }
    }
    [DataContract]
    public sealed class TrackRangeDefinition
    {
        private TrackRangeDefinition() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "entry", IsRequired = true)] public TrackAnchor Entry { get; private set; }
        [DataMember(Name = "firing_line", IsRequired = true)] public TrackAnchor FiringLine { get; private set; }
        [DataMember(Name = "exit", IsRequired = true)] public TrackAnchor Exit { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);
            if (Entry == null || FiringLine == null || Exit == null) throw new ArgumentException("Range anchors required.");
            var loop = Entry.Validate(track); FiringLine.Validate(track); Exit.Validate(track);
            if (loop.Kind != TrackLoopKind.Race || Entry.LoopId != FiringLine.LoopId || Entry.LoopId != Exit.LoopId ||
                Entry.Distance.Millimeters >= FiringLine.Distance.Millimeters || FiringLine.Distance.Millimeters >= Exit.Distance.Millimeters)
                throw new ArgumentException("Range anchors must be ordered on one race loop.");
        }
    }
    [DataContract]
    public sealed class TrackPenaltyDefinition
    {
        private TrackPenaltyDefinition() { }
        [DataMember(Name = "loop_id", IsRequired = true)] public string LoopId { get; private set; }
        [DataMember(Name = "entry", IsRequired = true)] public TrackAnchor Entry { get; private set; }
        [DataMember(Name = "rejoin", IsRequired = true)] public TrackAnchor Rejoin { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            var loop = track.GetLoop(LoopId);
            if (loop.Kind != TrackLoopKind.Penalty || !loop.Closed) throw new ArgumentException("Penalty requires a closed penalty loop.");
            if (Entry == null || Rejoin == null) throw new ArgumentException("Penalty access anchors required.");
            Entry.Validate(track); Rejoin.Validate(track);
            if (Entry.LoopId != track.Range.Exit.LoopId || Rejoin.LoopId != Entry.LoopId ||
                Entry.Distance.Millimeters < track.Range.Exit.Distance.Millimeters || Rejoin.Distance.Millimeters < Entry.Distance.Millimeters)
                throw new ArgumentException("Penalty access must follow the range exit on the same loop.");
        }
    }
    [DataContract]
    public sealed class TrackDefinition
    {
        [DataContract]
        private sealed class CheckpointSupplement
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
            [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
            [DataMember(Name = "track_id", IsRequired = true)] public string TrackId { get; private set; }
            [DataMember(Name = "checkpoints", IsRequired = true)] public TrackCheckpointDefinition[] Checkpoints { get; private set; }
        }
        private TrackDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "authoring_limits", IsRequired = true)] public TrackAuthoringLimits AuthoringLimits { get; private set; }
        [DataMember(Name = "loops", IsRequired = true)] private TrackLoopDefinition[] Loops { get; set; }
        [DataMember(Name = "checkpoints", IsRequired = true)] private TrackCheckpointDefinition[] Checkpoints { get; set; }
        [DataMember(Name = "range", IsRequired = true)] public TrackRangeDefinition Range { get; private set; }
        [DataMember(Name = "penalty", IsRequired = true)] public TrackPenaltyDefinition Penalty { get; private set; }
        private Dictionary<string, TrackLoopDefinition> loopIndex;
        public int LoopCount => Loops.Length;
        public int CheckpointCount => Checkpoints.Length;
        public TrackLoopDefinition GetLoop(int index) => Loops[index];
        public TrackCheckpointDefinition GetCheckpoint(int index) => Checkpoints[index];
        public TrackDefinition WithBroadcastCheckpoints(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var supplement = (CheckpointSupplement)new DataContractJsonSerializer(typeof(CheckpointSupplement)).ReadObject(stream);
            if (supplement == null || supplement.SchemaVersion != 1 || supplement.TrackId != Id ||
                supplement.Checkpoints == null || supplement.Checkpoints.Length == 0)
                throw new SerializationException("Broadcast checkpoints must bind the authored track.");
            TrackValidation.Id(supplement.Id);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var anchors = new HashSet<(string, long)>();
            foreach (var checkpoint in Checkpoints)
            {
                ids.Add(checkpoint.Id);
                anchors.Add((checkpoint.Anchor.LoopId, checkpoint.Anchor.Distance.Millimeters));
            }
            foreach (var checkpoint in supplement.Checkpoints)
            {
                if (checkpoint == null || !checkpoint.BroadcastOnly)
                    throw new SerializationException("Supplementary checkpoints must be marked for broadcast.");
                checkpoint.Validate(this);
                var anchor = checkpoint.Anchor;
                var loop = GetLoop(anchor.LoopId);
                if (loop.Kind != TrackLoopKind.Race || anchor.Distance.Millimeters == 0 ||
                    anchor.Distance.Millimeters >= loop.Length.Millimeters || !ids.Add(checkpoint.Id) ||
                    !anchors.Add((anchor.LoopId, anchor.Distance.Millimeters)))
                    throw new SerializationException("Supplementary timing lines must be distinct interior race anchors.");
            }
            var copy = (TrackDefinition)MemberwiseClone();
            copy.Checkpoints = new TrackCheckpointDefinition[Checkpoints.Length + supplement.Checkpoints.Length];
            Checkpoints.CopyTo(copy.Checkpoints, 0);
            supplement.Checkpoints.CopyTo(copy.Checkpoints, Checkpoints.Length);
            return copy;
        }
        public TrackLoopDefinition GetLoop(string id)
        {
            if (id == null || !loopIndex.TryGetValue(id, out var loop)) throw new ArgumentException("Unknown track loop ID.", nameof(id));
            return loop;
        }
        public static TrackDefinition FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var track = (TrackDefinition)new DataContractJsonSerializer(typeof(TrackDefinition)).ReadObject(stream);
                if (track == null || track.SchemaVersion != 1) throw new SerializationException("Unsupported track schema.");
                TrackValidation.Id(track.Id);
                if (track.AuthoringLimits == null || track.Loops == null || track.Loops.Length == 0 ||
                    track.Checkpoints == null || track.Range == null || track.Penalty == null)
                    throw new SerializationException("Track sections are required.");
                track.AuthoringLimits.Validate();
                track.loopIndex = new Dictionary<string, TrackLoopDefinition>(StringComparer.Ordinal);
                int raceLoops = 0;
                foreach (var loop in track.Loops)
                {
                    if (loop == null) throw new SerializationException("Null loop.");
                    loop.Validate(track.AuthoringLimits);
                    if (track.loopIndex.ContainsKey(loop.Id)) throw new SerializationException("Duplicate loop ID.");
                    track.loopIndex.Add(loop.Id, loop);
                    if (loop.Kind == TrackLoopKind.Race) raceLoops++;
                }
                if (raceLoops == 0) throw new SerializationException("A race loop is required.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var checkpoint in track.Checkpoints)
                {
                    if (checkpoint == null) throw new SerializationException("Null checkpoint.");
                    checkpoint.Validate(track);
                    if (!ids.Add(checkpoint.Id)) throw new SerializationException("Duplicate checkpoint ID.");
                }
                track.Range.Validate(track); track.Penalty.Validate(track);
                return track;
            }
        }
        public string ToJson()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(TrackDefinition)).WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
