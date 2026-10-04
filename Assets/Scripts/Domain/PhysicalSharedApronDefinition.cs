using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class PhysicalSharedApronRegion
    {
        [DataMember(Name = "junction_id", IsRequired = true)] public string JunctionId { get; private set; }
        [DataMember(Name = "post_shoot_exit_clearance", EmitDefaultValue = false)] public PhysicalApronExitClearance ExitClearance { get; private set; }
        public int JunctionIndex { get; internal set; }
        internal int FirstCorridor { get; set; }
        internal int SecondCorridor { get; set; }
    }

    [DataContract] public sealed class PhysicalApronExitClearance
    {
        [DataMember(Name = "private_loop_id", IsRequired = true)] public string PrivateLoopId { get; private set; }
        [DataMember(Name = "through_loop_id", IsRequired = true)] public string ThroughLoopId { get; private set; }
        public long ClaimAfterStopMillimeters { get; internal set; }
        public long ReleaseAfterMillimeters { get; internal set; }
        public long ThroughGuardStartMillimeters { get; internal set; }
        public long ThroughGuardEndMillimeters { get; internal set; }
    }

    /// <summary>Optional bounds for complete pairwise physical apron admission. Mat assignment,
    /// guards and body dimensions remain authoritative in their existing definitions.</summary>
    [DataContract]
    public sealed class PhysicalSharedApronDefinition
    {
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "track_id", IsRequired = true)] public string TrackId { get; private set; }
        [DataMember(Name = "range_paths_id", IsRequired = true)] public string RangePathsId { get; private set; }
        [DataMember(Name = "occupancy_definition_id", IsRequired = true)] public string OccupancyId { get; private set; }
        [DataMember(Name = "validation_policy_resource", IsRequired = true)] public string ValidationPolicyResource { get; private set; }
        [DataMember(Name = "corridors", IsRequired = true)] private PhysicalPassageCorridor[] corridors { get; set; }
        [DataMember(Name = "regions", IsRequired = true)] private PhysicalSharedApronRegion[] regions { get; set; }
        public int CorridorCount => corridors.Length;
        public int RegionCount => regions.Length;
        public PhysicalPassageCorridor GetCorridor(int i) => corridors[i];
        public PhysicalSharedApronRegion GetRegion(int i) => regions[i];
        public bool ContainsRegion(int junctionIndex)
        {
            for(int i=0;i<RegionCount;i++)if(regions[i].JunctionIndex==junctionIndex)return true;
            return false;
        }
        public VenueValidationPolicy ValidationPolicy { get; private set; }
        public string TrackSnapshotJson { get; private set; }
        public string VisualSnapshotJson { get; private set; }
        public string RangePathsSnapshotJson { get; private set; }
        public string JunctionSnapshotJson { get; private set; }
        public string ConnectionSnapshotJson { get; private set; }
        public string OccupancySnapshotJson { get; private set; }

        public static PhysicalSharedApronDefinition FromJson(string json, TrackDefinition track,
            TrackVisualDefinition visual, PhysicalRangePathDefinition paths, PhysicalJunctionDefinition junctions,
            PhysicalConnectionDefinition connections, OccupancyDefinition body, Func<string, string> readResource)
        {
            if (track == null || visual == null || paths == null || junctions == null || connections == null || body == null || readResource == null)
                throw new ArgumentNullException();
            var value = RouteJson.Read<PhysicalSharedApronDefinition>(json);
            if (value == null || (value.SchemaVersion != 1 && value.SchemaVersion != 2) || value.corridors == null || value.regions == null ||
                value.CorridorCount < 2 || value.CorridorCount > PhysicalRangePathDefinition.MaximumLanes + 2 ||
                value.RegionCount < 1 || value.RegionCount > 512)
                throw new SerializationException("Unsupported bounded shared apron schema.");
            TrackValidation.Id(value.Id); TrackValidation.Id(value.TrackId); TrackValidation.Id(value.RangePathsId); TrackValidation.Id(value.OccupancyId);
            VenueManifest.ResourceId(value.ValidationPolicyResource);
            string trackJson = track.ToJson(), junctionJson = junctions.ToJson(), connectionJson = connections.ToJson();
            if (value.TrackId != track.Id || value.RangePathsId != paths.Id || value.OccupancyId != body.Id || visual.TrackId != track.Id ||
                paths.TrackSnapshotJson != trackJson || junctions.TrackSnapshotJson != trackJson || connections.TrackSnapshotJson != trackJson ||
                paths.JunctionSnapshotJson != junctionJson || paths.ConnectionSnapshotJson != connectionJson || connections.JunctionSnapshotJson != junctionJson)
                throw new ArgumentException("Exact apron track, range paths, topology and body binding required.");
            value.ValidationPolicy = VenueValidationPolicy.FromJson(readResource(value.ValidationPolicyResource));
            var pathLoops = new HashSet<string>(StringComparer.Ordinal);
            for (int l = 0; l < paths.Count; l++)
                for (int s = 0; s < paths.GetLane(l).SliceCount; s++) pathLoops.Add(paths.GetLane(l).GetSlice(s).LoopId);
            var corridorIds = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < value.CorridorCount; i++)
            {
                var c = value.GetCorridor(i);
                if (c == null) throw new ArgumentException("Null apron corridor.");
                c.Validate(track, value.ValidationPolicy);
                if (!pathLoops.Contains(c.LoopId) || track.GetLoop(c.LoopId).Kind != TrackLoopKind.Race || corridorIds.ContainsKey(c.LoopId))
                    throw new ArgumentException("One bounded corridor per reachable range-path race loop required.");
                if (track.GetLoop(c.LoopId).Closed && (c.Start == 0 || c.End == track.GetLoop(c.LoopId).Length.Meters))
                    throw new ArgumentException("A shared apron cannot cover a closed terminal or wrapped window.");
                corridorIds.Add(c.LoopId, i);
            }
            bool hasExitClearance=false;
            var regionIds = new HashSet<string>(StringComparer.Ordinal); var used = new bool[value.CorridorCount];
            for (int i = 0; i < value.RegionCount; i++)
            {
                var r = value.GetRegion(i);
                if (r == null) throw new ArgumentException("Null apron region.");
                TrackValidation.Id(r.JunctionId);
                if (!regionIds.Add(r.JunctionId)) throw new ArgumentException("Duplicate apron region.");
                r.JunctionIndex = -1;
                for (int j = 0; j < junctions.Count; j++) if (junctions.Get(j).Id == r.JunctionId) r.JunctionIndex = j;
                if (r.JunctionIndex < 0) throw new ArgumentException("Unknown apron region.");
                r.FirstCorridor = r.SecondCorridor = -1;
                var region = junctions.Get(r.JunctionIndex);
                for (int p = 0; p < region.PortCount; p++)
                {
                    var port = region.GetPort(p);
                    if (!corridorIds.TryGetValue(port.Anchor.LoopId, out int c)) throw new ArgumentException("Every incident guard needs a bound apron corridor.");
                    if (r.FirstCorridor < 0) r.FirstCorridor = c;
                    else if (c != r.FirstCorridor)
                    {
                        if (r.SecondCorridor >= 0 && r.SecondCorridor != c) throw new ArgumentException("Only two distinct incident loops are supported per apron region.");
                        r.SecondCorridor = c;
                    }
                    used[c] = true;
                    // Multiple ports on one loop are legitimate: each complete interval is checked.
                    double from = port.Anchor.Distance.Meters - port.GuardBefore.Meters - body.LongitudinalSeparation * .5;
                    double to = port.Anchor.Distance.Meters + port.GuardAfter.Meters + body.LongitudinalSeparation * .5;
                    var loop = track.GetLoop(port.Anchor.LoopId);
                    if (!loop.Closed) { from = Math.Max(0, from); to = Math.Min(loop.Length.Meters, to); }
                    if (value.GetCorridor(c).Start > from || value.GetCorridor(c).End < to)
                        throw new ArgumentException("Apron corridors must cover every complete incident guard and half-body.");
                }
                if (r.SecondCorridor < 0) throw new ArgumentException("Same-loop self regions retain existing occupancy and exclusive semantics.");
                if(r.ExitClearance!=null)
                {
                    if(value.SchemaVersion!=2)throw new ArgumentException("Post-shoot exit-clearance authority requires schema2.");
                    hasExitClearance=true;
                    var clearance=r.ExitClearance;TrackValidation.Id(clearance.PrivateLoopId);TrackValidation.Id(clearance.ThroughLoopId);
                    PhysicalRangePathLane lane=null;
                    for(int l=0;l<paths.Count;l++)if(paths.GetLane(l).MatAnchor.LoopId==clearance.PrivateLoopId)
                    {if(lane!=null)throw new ArgumentException("One private mat path per exit-clearance region required.");lane=paths.GetLane(l);}
                    if(lane==null||clearance.PrivateLoopId==paths.MainLoopId||clearance.ThroughLoopId==clearance.PrivateLoopId||region.PortCount!=2)
                        throw new ArgumentException("A local post-shoot crossing requires exactly one private mat and one through port.");
                    PhysicalJunctionPort privatePort=null,mainPort=null;
                    for(int p=0;p<region.PortCount;p++)
                    {
                        var port=region.GetPort(p);
                        if(port.Anchor.LoopId==clearance.PrivateLoopId)privatePort=port;
                        else if(port.Anchor.LoopId==clearance.ThroughLoopId)mainPort=port;
                        else throw new ArgumentException("Exit clearance cannot bind another loop.");
                    }
                    if(privatePort==null||mainPort==null||privatePort.Anchor.Distance.Millimeters<=lane.MatAnchor.Distance.Millimeters)
                        throw new ArgumentException("Exit clearance must bind the actual downstream private crossing.");
                    long privateEnd=track.GetLoop(clearance.PrivateLoopId).Length.Millimeters;
                    long release=checked(privatePort.Anchor.Distance.Millimeters+privatePort.GuardAfter.Millimeters+
                        (long)Math.Ceiling(body.LongitudinalSeparation*.5*1000));
                    if(clearance.ThroughLoopId==paths.MainLoopId)
                    {if(release>=privateEnd)throw new ArgumentException("Main crossing must clear before the private-to-exit seam.");}
                    else
                    {
                        if(privatePort.Anchor.Distance.Millimeters!=privateEnd||privatePort.GuardAfter.Millimeters!=0||
                            lane.GetSlice(lane.SliceCount-1).LoopId!=clearance.ThroughLoopId)
                            throw new ArgumentException("Exit merge must bind the terminal private-to-exit slice.");
                        release=privateEnd;
                    }
                    clearance.ClaimAfterStopMillimeters=lane.MatAnchor.Distance.Millimeters;clearance.ReleaseAfterMillimeters=release;
                    long half=(long)Math.Ceiling(body.LongitudinalSeparation*.5*1000);
                    clearance.ThroughGuardStartMillimeters=checked(mainPort.Anchor.Distance.Millimeters-mainPort.GuardBefore.Millimeters-half);
                    clearance.ThroughGuardEndMillimeters=checked(mainPort.Anchor.Distance.Millimeters+mainPort.GuardAfter.Millimeters+half);
                }
            }
            if(value.SchemaVersion==2&&!hasExitClearance)throw new ArgumentException("Schema2 requires an explicit local exit-clearance policy.");
            for (int c = 0; c < used.Length; c++) if (!used[c]) throw new ArgumentException("An unrelated corridor cannot enter the apron authority.");
            if (!value.MatchesPresentation(visual)) throw new ArgumentException("Every captured and intervening visual knot must match apron authority.");
            value.TrackSnapshotJson = trackJson; value.VisualSnapshotJson = visual.ToJson(); value.RangePathsSnapshotJson = paths.ToJson();
            value.JunctionSnapshotJson = junctionJson; value.ConnectionSnapshotJson = connectionJson; value.OccupancySnapshotJson = body.ToJson();
            return value;
        }

        public bool MatchesPresentation(TrackVisualDefinition visual)
        {
            if (visual == null || visual.TrackId != TrackId) return false;
            for (int c = 0; c < CorridorCount; c++)
            {
                var corridor = GetCorridor(c); TrackVisualLoop loop;
                try { loop = visual.Get(corridor.LoopId); } catch (ArgumentException) { return false; }
                if (loop.Get(loop.Count - 1).Distance < corridor.End) return false;
                for (int i = 0; i < corridor.Count; i++)
                {
                    var p = corridor.Get(i); var q = loop.Sample(p.Distance);
                    if (!Near(p.X, p.Y, p.Z, q.X, q.Y, q.Z)) return false;
                }
                for (int i = 0; i < loop.Count; i++)
                {
                    var p = loop.Get(i); if (p.Distance < corridor.Start) continue; if (p.Distance > corridor.End) break;
                    int e = 1; while (e < corridor.Count - 1 && corridor.Get(e).Distance < p.Distance) e++;
                    var a = corridor.Get(e - 1); var b = corridor.Get(e); double t = (p.Distance - a.Distance) / (b.Distance - a.Distance);
                    if (!Near(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t, p.X, p.Y, p.Z)) return false;
                }
            }
            return true;
        }
        private bool Near(double ax, double ay, double az, double bx, double by, double bz)
        { double x = ax - bx, y = ay - by, z = az - bz; return Math.Sqrt(x * x + y * y + z * z) <= ValidationPolicy.SeamTolerance; }
        public string ToJson() => RouteJson.Write(this);
    }
}
