using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>A forward interval on an existing physical loop; repeated/shared loops retain their identity.</summary>
    [DataContract]
    public sealed class PhysicalRangePathSlice
    {
        private PhysicalRangePathSlice() { }
        [DataMember(Name = "loop_id", IsRequired = true)] public string LoopId { get; private set; }
        [DataMember(Name = "start_m", IsRequired = true)] private double StartMeters { get; set; }
        [DataMember(Name = "end_m", IsRequired = true)] private double EndMeters { get; set; }
        public TrackDistance Start { get; private set; }
        public TrackDistance End { get; private set; }
        public TrackDistance Length { get; private set; }
        public TrackDistance Offset { get; private set; }

        internal void Validate(TrackDefinition track, long offset)
        {
            TrackValidation.Id(LoopId);
            var loop = track.GetLoop(LoopId);
            Start = TrackDistance.FromMeters(StartMeters); End = TrackDistance.FromMeters(EndMeters);
            if (loop.Kind != TrackLoopKind.Race || End.Millimeters <= Start.Millimeters || End.Millimeters > loop.Length.Millimeters)
                throw new ArgumentException("Range slices require positive, forward race-loop intervals without wrapping.");
            Length = new TrackDistance(End.Millimeters - Start.Millimeters);
            Offset = new TrackDistance(offset);
        }
    }

    [DataContract]
    public sealed class PhysicalRangePathLane
    {
        private PhysicalRangePathLane() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "slices", IsRequired = true)] private PhysicalRangePathSlice[] Slices { get; set; }
        [DataMember(Name = "mat_anchor", IsRequired = true)] public TrackAnchor MatAnchor { get; private set; }
        public int SliceCount => Slices.Length;
        public PhysicalRangePathSlice GetSlice(int index) => Slices[index];
        public TrackDistance Length { get; private set; }
        /// <summary>Distance from the replaced interval's beginning, derived only from slices and the mat anchor.</summary>
        public TrackDistance FiringOffset { get; private set; }
        public int FiringSliceIndex { get; private set; }
        public TrackDistance Fork => Slices[0].End;

        internal void Validate(TrackDefinition track, string mainLoop, TrackDistance entry, TrackDistance exit,
            IReadOnlyDictionary<string, TrackAnchor> ports, PhysicalConnectionDefinition connections)
        {
            TrackValidation.Id(Id);
            if (Slices == null || Slices.Length < 2 || Slices.Length > PhysicalRangePathDefinition.MaximumSlicesPerLane)
                throw new ArgumentException("Each range lane requires 2..32 bounded physical slices.");
            if (Slices[1] != null && Slices[1].LoopId == mainLoop)
                throw new ArgumentException("The first shared main slice must end at the lane's actual fork.");
            if (MatAnchor == null) throw new ArgumentException("A physical mat anchor is required.");
            MatAnchor.Validate(track);
            if (MatAnchor.LoopId == mainLoop) throw new ArgumentException("A mat must be on a physical alternative to the main entry aisle.");
            long length = 0; int matOccurrences = 0;
            for (int i = 0; i < Slices.Length; i++)
            {
                var slice = Slices[i];
                if (slice == null) throw new ArgumentException("Null physical range slice.");
                slice.Validate(track, length);
                if (i == 0 && (slice.LoopId != mainLoop || !slice.Start.Equals(entry)))
                    throw new ArgumentException("Every lane must start on the shared main entry aisle at the replaced interval's beginning.");
                if (i > 0) RequireAdjacency(Slices[i - 1].LoopId, Slices[i - 1].End,
                    slice.LoopId, slice.Start, ports, connections);
                if (slice.LoopId == MatAnchor.LoopId && MatAnchor.Distance.Millimeters > slice.Start.Millimeters &&
                    MatAnchor.Distance.Millimeters < slice.End.Millimeters)
                {
                    matOccurrences++; FiringSliceIndex = i;
                    FiringOffset = new TrackDistance(checked(length + MatAnchor.Distance.Millimeters - slice.Start.Millimeters));
                }
                length = checked(length + slice.Length.Millimeters);
            }
            if (matOccurrences != 1) throw new ArgumentException("The mat must occur strictly inside exactly one positive-distance slice.");
            Length = new TrackDistance(length);
            if (length != exit.Millimeters - entry.Millimeters)
                throw new ArgumentException("Range physical travel must exactly equal the replaced main interval in whole millimetres.");
            var last = Slices[Slices.Length - 1];
            RequireAdjacency(last.LoopId, last.End, mainLoop, exit, ports, connections);
        }

        private static void RequireAdjacency(string fromLoop, TrackDistance from, string toLoop, TrackDistance to,
            IReadOnlyDictionary<string, TrackAnchor> ports, PhysicalConnectionDefinition connections)
        {
            if (fromLoop == toLoop)
            {
                if (!from.Equals(to)) throw new ArgumentException("Adjacent slices on the same loop must share the exact coordinate.");
                return;
            }
            for (int i = 0; i < connections.Count; i++)
            {
                var connection = connections.Get(i);
                var a = ports[connection.FromPortId]; var b = ports[connection.ToPortId];
                if (a.LoopId == fromLoop && a.Distance.Equals(from) && b.LoopId == toLoop && b.Distance.Equals(to)) return;
            }
            throw new ArgumentException("Every loop-changing range seam requires a matching directed physical connection.");
        }
    }

    /// <summary>Versioned range path contract only. Runtime sampling, lane admission, world geometry and
    /// occupancy support require separate integration; accepting this data does not certify a production venue.</summary>
    [DataContract]
    public sealed class PhysicalRangePathDefinition
    {
        public const int MaximumLanes = 60;
        public const int MaximumSlicesPerLane = 32;
        private PhysicalRangePathDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "track_id", IsRequired = true)] public string TrackId { get; private set; }
        [DataMember(Name = "main_loop_id", IsRequired = true)] public string MainLoopId { get; private set; }
        [DataMember(Name = "entry_distance_m", IsRequired = true)] private double EntryMeters { get; set; }
        [DataMember(Name = "exit_distance_m", IsRequired = true)] private double ExitMeters { get; set; }
        [DataMember(Name = "lanes", IsRequired = true)] private PhysicalRangePathLane[] Lanes { get; set; }
        public TrackDistance Entry { get; private set; }
        public TrackDistance Exit { get; private set; }
        public int Count => Lanes.Length;
        /// <summary>Authored order is retained; callers must not infer ordinals from names or sort IDs.</summary>
        public PhysicalRangePathLane GetLane(int index) => Lanes[index];
        public string TrackSnapshotJson { get; private set; }
        public string JunctionSnapshotJson { get; private set; }
        public string ConnectionSnapshotJson { get; private set; }

        public static PhysicalRangePathDefinition FromJson(string json, TrackDefinition track,
            PhysicalJunctionDefinition junctions, PhysicalConnectionDefinition connections)
        {
            if (track == null || junctions == null || connections == null) throw new ArgumentNullException();
            string snapshot = track.ToJson(); string junctionSnapshot = junctions.ToJson();
            if (junctions.TrackSnapshotJson != snapshot || connections.TrackSnapshotJson != snapshot ||
                connections.JunctionSnapshotJson != junctionSnapshot)
                throw new ArgumentException("Range paths require exact matching track, junction and connection snapshots.");
            var value = RouteJson.Read<PhysicalRangePathDefinition>(json);
            if (value == null || value.SchemaVersion != 1 || value.Lanes == null || value.Lanes.Length == 0 || value.Lanes.Length > MaximumLanes)
                throw new SerializationException("Invalid physical range path schema or lane count; at most 60 lanes are supported by the contract.");
            TrackValidation.Id(value.Id); TrackValidation.Id(value.TrackId); TrackValidation.Id(value.MainLoopId);
            value.Entry = TrackDistance.FromMeters(value.EntryMeters); value.Exit = TrackDistance.FromMeters(value.ExitMeters);
            var main = track.GetLoop(value.MainLoopId);
            if (value.TrackId != track.Id || main.Kind != TrackLoopKind.Race ||
                value.MainLoopId != track.Range.Entry.LoopId || value.MainLoopId != track.Penalty.Entry.LoopId ||
                value.Entry.Millimeters > track.Range.Entry.Distance.Millimeters || value.Exit.Millimeters != track.Penalty.Entry.Distance.Millimeters ||
                value.Exit.Millimeters > main.Length.Millimeters || value.Entry.Millimeters >= value.Exit.Millimeters)
                throw new ArgumentException("A matching main range interval ending at penalty access is required.");
            var ports = new Dictionary<string, TrackAnchor>(StringComparer.Ordinal);
            for (int j = 0; j < junctions.Count; j++)
                for (int p = 0; p < junctions.Get(j).PortCount; p++)
                {
                    var port = junctions.Get(j).GetPort(p); ports.Add(port.Id, port.Anchor);
                }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var mats = new Dictionary<string, HashSet<long>>(StringComparer.Ordinal);
            foreach (var lane in value.Lanes)
            {
                if (lane == null) throw new ArgumentException("Null physical range lane.");
                lane.Validate(track, value.MainLoopId, value.Entry, value.Exit, ports, connections);
                long firing=checked(value.Entry.Millimeters+lane.FiringOffset.Millimeters);
                if(firing<=track.Range.Entry.Distance.Millimeters||firing>=track.Range.Exit.Distance.Millimeters)
                    throw new ArgumentException("Each physical mat stop must lie between the logical range entry and exit.");
                if (!ids.Add(lane.Id)) throw new ArgumentException("Unique ordered physical lane IDs are required.");
                if (!mats.TryGetValue(lane.MatAnchor.LoopId, out var coordinates))
                { coordinates = new HashSet<long>(); mats.Add(lane.MatAnchor.LoopId, coordinates); }
                if (!coordinates.Add(lane.MatAnchor.Distance.Millimeters)) throw new ArgumentException("Each logical lane requires a distinct physical mat anchor.");
            }
            value.TrackSnapshotJson = snapshot; value.JunctionSnapshotJson = junctionSnapshot;
            value.ConnectionSnapshotJson = connections.ToJson(); return value;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
