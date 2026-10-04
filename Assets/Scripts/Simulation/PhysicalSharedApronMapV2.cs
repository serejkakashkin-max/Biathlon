using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalApronMotionV2
    {
        public RaceMotionSegment Segment { get; }
        public PhysicalMotionReceipt Receipt { get; }
        public PhysicalApronMotionV2(RaceMotionSegment segment, PhysicalMotionReceipt receipt) { Segment = segment; Receipt = receipt; }
    }

    /// <summary>Exact selected-path seam geometry. Only the accepting field may establish that
    /// this represents an accepted retained body; constructing it does not grant admission.</summary>
    public readonly struct PhysicalApronTailV2
    {
        internal PhysicalSharedApronMapV2 Owner { get; }
        internal int PathIndex { get; }
        public bool Active => Owner != null;
        public int OccurrenceIndex { get; }
        public int ConnectionIndex { get; }
        public long SeamRouteNanometers { get; }
        public PhysicalTrackPoint Point { get; }
        public double Lateral { get; }
        internal PhysicalApronTailV2(PhysicalSharedApronMapV2 owner, int path, int occurrence, int connection,
            long route, PhysicalTrackPoint point, double lateral)
        { Owner = owner; PathIndex = path; OccurrenceIndex = occurrence; ConnectionIndex = connection; SeamRouteNanometers = route; Point = point; Lateral = lateral; }
    }

    public sealed class PhysicalApronTraceV2
    {
        internal readonly PhysicalSharedApronMapV2 Owner;
        internal int PathIndex;
        internal readonly PhysicalApronMotionV2[] Motions;
        internal readonly RaceMotionSegment[] Timeline;
        internal int Count;
        internal bool Prepared;
        internal bool AllBodyHeadings;
        internal PhysicalTrackPoint Start, End;
        internal long StartRoute, EndRoute;
        internal double LateralStart, LateralEnd;
        internal PhysicalApronTailV2 OldTail, NewTail;
        public AthleteId AthleteId { get; }
        public PhysicalMotionProjection Projection { get; private set; }
        internal PhysicalApronTraceV2(PhysicalSharedApronMapV2 owner, int path, AthleteId id, int capacity,
            PhysicalMotionProjection projection)
        { Owner = owner; PathIndex = path; AthleteId = id; Motions = new PhysicalApronMotionV2[capacity]; Timeline = new RaceMotionSegment[capacity]; Projection = projection; }
        public void RebindPath(int path, PhysicalMotionProjection actualProjection)
        {
            Prepared = false;
            Owner.ValidatePathProjection(path, actualProjection);
            PathIndex = path; Projection = actualProjection;
        }
        public void SetAllBodyHeadings(bool enabled) { Prepared = false; AllBodyHeadings = enabled; }
        public void Prepare(long startRoute, long endRoute, PhysicalTrackPoint start, PhysicalTrackPoint end,
            double lateralStart, double lateralEnd, PhysicalApronTailV2 oldTail, PhysicalApronTailV2 newTail, ReadOnlySpan<PhysicalApronMotionV2> motions)
        {
            Prepared = false;
            if (motions.Length > Motions.Length) throw new ArgumentException("Cold trace capacity exceeded.");
            StartRoute = startRoute; EndRoute = endRoute; Start = start; End = end;
            LateralStart = lateralStart; LateralEnd = lateralEnd; OldTail = oldTail; NewTail = newTail;
            Count = motions.Length; motions.CopyTo(Motions);
            for (int i = 0; i < Count; i++) Timeline[i] = Motions[i].Segment;
            if (!Owner.ValidateTrace(this)) throw new ArgumentException("Incomplete or foreign selected-path trace evidence.");
            Prepared = true;
        }
    }

    /// <summary>Cold reusable scratch; pair queries are sequential and allocate no managed memory.</summary>
    public sealed class PhysicalApronPairScratchV2
    {
        internal readonly long[] Grid;
        public PhysicalApronPairScratchV2(int maximumReceiptsPerTrace)
        {
            if (maximumReceiptsPerTrace < 1) throw new ArgumentOutOfRangeException();
            Grid = new long[checked(2 + maximumReceiptsPerTrace * 4)];
        }
    }

    /// <summary>Draft geometry/provenance kernel only. A false result proves separation for one
    /// named region, never all other regions, reservations, same-loop occupancy or a race commit.</summary>
    public sealed class PhysicalSharedApronMapV2
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalRangeFieldLayout layout;
        private readonly PhysicalSharedApronDefinition definition;
        private readonly PhysicalJunctionDefinition junctions;
        private readonly TrackDefinition track;
        private readonly OccupancyDefinition body;
        private readonly PhysicalApronBodySupportDraft[,] projections;
        private readonly PhysicalApronBodySupportDraft[,] poseProjections;
        private readonly PhysicalApronSeamProjectionDraft[,] seamProjections, seamPoseProjections;
        private readonly PhysicalJunctionMap junctionMap;
        private readonly bool[] eligible;
        private readonly Seam[][] seams;
        private readonly long[] starts, ends;
        private readonly TrackSampler[] samplers;
        private readonly double tickSeconds;
        private readonly long halfBody;
        private readonly struct Seam
        {
            public readonly int Connection;
            public readonly long Route;
            public readonly PhysicalTrackPoint Source, Target;
            public readonly bool ContinuousTangent;
            public Seam(int connection, long route, PhysicalTrackPoint source, PhysicalTrackPoint target, bool continuous)
            { Connection = connection; Route = route; Source = source; Target = target; ContinuousTangent = continuous; }
        }
        private readonly Dictionary<string,PhysicalApronExitClearance[]> exitClearanceByLoop=new Dictionary<string,PhysicalApronExitClearance[]>(StringComparer.Ordinal);
        public string ContextJson { get; }
        public int RegionCount => definition.RegionCount;

        public PhysicalSharedApronMapV2(PhysicalTrackMap map, TrackDefinition track, RaceRoute route,
            CompetitionProfile profile, PhysicalRangeFieldLayout layout, TrackVisualDefinition visual,
            PhysicalJunctionDefinition junctions, PhysicalConnectionDefinition connections, OccupancyDefinition body,
            PhysicalSharedApronDefinition definition, double tickSeconds, long maximumTickTravelNanometers)
        {
            if (map == null || track == null || route == null || profile == null || layout == null || visual == null || junctions == null || connections == null || body == null || definition == null)
                throw new ArgumentNullException();
            if (!map.IsBoundTo(track, route) || !layout.IsBoundTo(track, route) || !ReferenceEquals(layout.GetPath(0).PhysicalMap, map) ||
                definition.TrackSnapshotJson != track.ToJson() || definition.VisualSnapshotJson != visual.ToJson() ||
                definition.RangePathsSnapshotJson != layout.Definition.ToJson() || definition.JunctionSnapshotJson != junctions.ToJson() ||
                definition.ConnectionSnapshotJson != connections.ToJson() || definition.OccupancySnapshotJson != body.ToJson() ||
                route.RequirementsSnapshotJson != profile.ToRouteRequirements().ToJson())
                throw new ArgumentException("Exact apron layout, route, profile, geometry, topology and body binding required.");
            if (maximumTickTravelNanometers < 0) throw new ArgumentOutOfRangeException(nameof(maximumTickTravelNanometers));
            FractionalMotionTimeline.Quantize(0, tickSeconds);
            this.map = map; this.track = track; this.layout = layout; this.junctions = junctions; this.body = body; this.definition = definition; this.tickSeconds = tickSeconds;
            halfBody = (long)Math.Ceiling(body.LongitudinalSeparation * .5 * SkiState.NanometersPerMeter);
            starts = new long[definition.CorridorCount]; ends = new long[starts.Length]; samplers = new TrackSampler[starts.Length];
            projections = new PhysicalApronBodySupportDraft[starts.Length, starts.Length];
            poseProjections = new PhysicalApronBodySupportDraft[starts.Length, starts.Length];
            for (int i = 0; i < starts.Length; i++)
            {
                var c = definition.GetCorridor(i); starts[i] = Nm(c.Start); ends[i] = Nm(c.End); samplers[i] = new TrackSampler(track.GetLoop(c.LoopId));
                for (int j = 0; j <= i; j++)
                {
                    projections[i, j] = projections[j, i] = new PhysicalApronBodySupportDraft(c, definition.GetCorridor(j), body, definition.ValidationPolicy);
                    poseProjections[i, j] = poseProjections[j, i] = new PhysicalApronBodySupportDraft(c, definition.GetCorridor(j), body, definition.ValidationPolicy, true);
                }
            }
            junctionMap = new PhysicalJunctionMap(map, junctions);
            eligible = new bool[RegionCount];
            for (int r = 0; r < RegionCount; r++)
            {
                var region = junctions.Get(definition.GetRegion(r).JunctionIndex); eligible[r] = true;
                for (int p = 0; p < region.PortCount; p++)
                {
                    var port = region.GetPort(p); int c = Corridor(port.Anchor.LoopId);
                    long low = Nm(port.Anchor.Distance.Meters) - Nm(port.GuardBefore.Meters) - halfBody - maximumTickTravelNanometers;
                    long high = Nm(port.Anchor.Distance.Meters) + Nm(port.GuardAfter.Meters) + halfBody + maximumTickTravelNanometers;
                    var loop = track.GetLoop(port.Anchor.LoopId);
                    if (!loop.Closed) { low = Math.Max(0, low); high = Math.Min(Nm(loop.Length.Meters), high); }
                    if (starts[c] > low || ends[c] < high) eligible[r] = false;
                }
            }
            var clearances=new Dictionary<string,List<PhysicalApronExitClearance>>(StringComparer.Ordinal);
            for(int r=0;r<RegionCount;r++)
            {
                var clearance=definition.GetRegion(r).ExitClearance;if(clearance==null)continue;
                if(!clearances.TryGetValue(clearance.PrivateLoopId,out var list))
                {list=new List<PhysicalApronExitClearance>();clearances.Add(clearance.PrivateLoopId,list);}
                list.Add(clearance);
            }
            foreach(var pair in clearances)exitClearanceByLoop.Add(pair.Key,pair.Value.ToArray());
            seams = new Seam[layout.Count][];
            seamProjections = new PhysicalApronSeamProjectionDraft[connections.Count, starts.Length];
            seamPoseProjections = new PhysicalApronSeamProjectionDraft[connections.Count, starts.Length];
            var connectionMap = new PhysicalConnectionMap(map, junctions, connections);
            var requirements = CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            for (int p = 0; p < layout.Count; p++)
            {
                var list = new List<Seam>(); var lane = layout.Definition.GetLane(p); var path = layout.GetPath(p);
                for (int s = 0; s < route.SectionCount; s++)
                {
                    var section = route.GetSection(s);
                    if (!section.HasRange || section.LoopId != layout.Definition.MainLoopId || !requirements.RequiresRange(section.LapIndex)) continue;
                    long origin = checked((section.Start.Millimeters - section.LoopStart.Millimeters + layout.Definition.Entry.Millimeters) * SkiState.NanometersPerMillimeter);
                    for (int k = 1; k <= lane.SliceCount; k++)
                    {
                        long at = checked(origin + (k == lane.SliceCount ? lane.Length.Millimeters : lane.GetSlice(k).Offset.Millimeters) * SkiState.NanometersPerMillimeter);
                        var source = path.MapRoute(at, PhysicalBoundarySide.Incoming); var target = path.MapRoute(at, PhysicalBoundarySide.Outgoing);
                        if (map.SameLoop(source, target)) continue;
                        int connection = connectionMap.FindTransition(source, target);
                        if (connection < 0) throw new ArgumentException("Every apron path occurrence needs an exact directed seam.");
                        list.Add(new Seam(connection, at, source, target, ContinuousSeam(source, target)));
                        if (PhysicalApronSeamProjectionDraft.HasCompleteIncidentWindow(definition, source, true) &&
                            PhysicalApronSeamProjectionDraft.HasCompleteIncidentWindow(definition, target, false) && seamProjections[connection, 0] == null)
                            for (int c = 0; c < starts.Length; c++)
                            {
                                seamProjections[connection, c] = new PhysicalApronSeamProjectionDraft(map, definition, body, source, target, definition.GetCorridor(c).LoopId);
                                seamPoseProjections[connection, c] = new PhysicalApronSeamProjectionDraft(map, definition, body, source, target, definition.GetCorridor(c).LoopId, true);
                            }
                    }
                }
                seams[p] = list.ToArray();
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] { definition.ToJson(), definition.ValidationPolicy.ToJson(), definition.TrackSnapshotJson,
                definition.VisualSnapshotJson, definition.RangePathsSnapshotJson, definition.JunctionSnapshotJson, definition.ConnectionSnapshotJson, definition.OccupancySnapshotJson,
                route.PlanSnapshotJson, route.RequirementsSnapshotJson, profile.ToJson(), tickSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture), maximumTickTravelNanometers.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "shared_apron_geometry_kernel_clock_bound_draft_v2;world_center_right_normal_plus_fz_minus_fx;per_region_all_incident_guards;complete_selected_apron_family;actual_selected_path_receipts;accepted_tail_occurrence_required_by_field;all_fraction_intervals_four_tail_pairs;exact_incident_seam_rotation_support;optional_all_heading_body_envelope_without_lateral_centering;unknown_exclusive;no_admission_or_static_relaxation" });
        }
        public int FindRegionForJunction(int junction)
        { for (int r = 0; r < RegionCount; r++) if (definition.GetRegion(r).JunctionIndex == junction) return r; return -1; }
        public PhysicalApronExitClearance GetExitClearance(int region)=>definition.GetRegion(region).ExitClearance;
        public int ExitClearanceCount(string loop)=>loop!=null&&exitClearanceByLoop.TryGetValue(loop,out var list)?list.Length:0;
        public PhysicalApronExitClearance GetExitClearance(string loop,int index)=>exitClearanceByLoop[loop][index];
        public bool IsEligible(int region) => region >= 0 && region < RegionCount && eligible[region];
        public int SeamCount(int path) => seams[path].Length;
        public long SeamRoute(int path, int occurrence) => seams[path][occurrence].Route;
        public int FindSeamOccurrence(int path, int connection, long route, PhysicalTrackPoint incoming)
        {
            if (path < 0 || path >= seams.Length) return -1;
            for (int i = 0; i < seams[path].Length; i++)
            { var s = seams[path][i]; if (s.Connection == connection && s.Route == route && Same(s.Source, incoming)) return i; }
            return -1;
        }
        public PhysicalApronTailV2 CreateTail(int path, int occurrence, double lateral)
        {
            var seam = seams[path][occurrence]; RequireLateral(lateral);
            return new PhysicalApronTailV2(this, path, occurrence, seam.Connection, seam.Route, seam.Source, lateral);
        }
        public PhysicalApronTraceV2 CreateTrace(int path, AthleteId id, int maximumReceipts)
        {
            if (!id.IsValid || maximumReceipts < 1) throw new ArgumentException("Valid cold trace identity and capacity required.");
            return new PhysicalApronTraceV2(this, path, id, maximumReceipts, new PhysicalMotionProjection(map, tickSeconds, rangePath: layout.GetPath(path)));
        }
        public PhysicalApronTraceV2 CreateTrace(int path, AthleteId id, int maximumReceipts, PhysicalMotionProjection actualProjection)
        {
            if (!id.IsValid || maximumReceipts < 1 || actualProjection == null ||
                !actualProjection.IsBoundTo(map, layout.GetPath(path), tickSeconds)) throw new ArgumentException("Exact selected-path receipt projection and clock required.");
            return new PhysicalApronTraceV2(this, path, id, maximumReceipts, actualProjection);
        }
        internal void ValidatePathProjection(int path, PhysicalMotionProjection projection)
        {
            if (projection == null || !projection.IsBoundTo(map, layout.GetPath(path), tickSeconds))
                throw new ArgumentException("Exact selected-path receipt projection and clock required.");
        }
        public bool Conflicts(int region, PhysicalOccupancySweep a, PhysicalOccupancySweep b)
        {
            if (!IsEligible(region) || a.AthleteId.Equals(b.AthleteId)) return true;
            int ac = Scope(region, a), bc = Scope(region, b);
            return ac < 0 || bc < 0 || projections[ac, bc].Conflicts(a, b);
        }
        public bool Conflicts(int region, PhysicalApronTraceV2 a, PhysicalApronTraceV2 b, PhysicalApronPairScratchV2 scratch)
        {
            if (!IsEligible(region) || a == null || b == null || scratch == null || !a.Prepared || !b.Prepared ||
                !ReferenceEquals(a.Owner, this) || !ReferenceEquals(b.Owner, this) || a.AthleteId.Equals(b.AthleteId) ||
                scratch.Grid.Length < 2 + 2 * (a.Count + b.Count) || !InScope(region, a) || !InScope(region, b) ||
                !Touches(region, a) || !Touches(region, b)) return true;
            int count = 2; scratch.Grid[0] = 0; scratch.Grid[1] = JunctionAdmissionSession.FractionParts;
            for (int i = 0; i < a.Count; i++) { Insert(a.Motions[i].Receipt.StartFraction); Insert(a.Motions[i].Receipt.EndFraction); }
            for (int i = 0; i < b.Count; i++) { Insert(b.Motions[i].Receipt.StartFraction); Insert(b.Motions[i].Receipt.EndFraction); }
            for (int i = 1; i < count; i++)
            {
                var aa = Slice(a, scratch.Grid[i - 1], scratch.Grid[i]); var bb = Slice(b, scratch.Grid[i - 1], scratch.Grid[i]);
                if (Bodies(aa, bb) || Hit(a.OldTail, a.AthleteId, bb) || Hit(a.NewTail, a.AthleteId, bb) || Hit(b.OldTail, b.AthleteId, aa) || Hit(b.NewTail, b.AthleteId, aa)) return true;
            }
            return Pair(a.OldTail, a.AthleteId, b.OldTail, b.AthleteId) || Pair(a.NewTail, a.AthleteId, b.OldTail, b.AthleteId) ||
                Pair(a.OldTail, a.AthleteId, b.NewTail, b.AthleteId) || Pair(a.NewTail, a.AthleteId, b.NewTail, b.AthleteId);
            bool Bodies(PhysicalOccupancySweep x, PhysicalOccupancySweep y)
            {
                int first = Scope(region, x), second = Scope(region, y);
                return first < 0 || second < 0 || (a.AllBodyHeadings || b.AllBodyHeadings
                    ? poseProjections[first, second].Conflicts(x, y) : projections[first, second].Conflicts(x, y));
            }
            bool Hit(PhysicalApronTailV2 tail, AthleteId id, PhysicalOccupancySweep other)
            {
                if (!tail.Active) return false;
                int corridor = Corridor(other.Start.LoopId);
                var cache = a.AllBodyHeadings || b.AllBodyHeadings ? seamPoseProjections : seamProjections;
                var rotation = corridor < 0 ? null : cache[tail.ConnectionIndex, corridor];
                return Bodies(TailSweep(tail, id), other) || rotation == null || rotation.Conflicts(tail.Lateral, tail.Lateral, other);
            }
            bool Pair(PhysicalApronTailV2 x, AthleteId xi, PhysicalApronTailV2 y, AthleteId yi)
            {
                if (!x.Active || !y.Active) return false;
                var cache = a.AllBodyHeadings || b.AllBodyHeadings ? seamPoseProjections : seamProjections;
                var first = cache[x.ConnectionIndex, 0]; var second = cache[y.ConnectionIndex, 0];
                return Hit(x, xi, TailSweep(y, yi)) || Hit(y, yi, TailSweep(x, xi)) || first == null || second == null || first.Conflicts(x.Lateral, x.Lateral, second, y.Lateral, y.Lateral);
            }
            void Insert(long value)
            { int i = 0; while (i < count && scratch.Grid[i] < value) i++; if (i < count && scratch.Grid[i] == value) return; for (int j = count; j > i; j--) scratch.Grid[j] = scratch.Grid[j - 1]; scratch.Grid[i] = value; count++; }
        }
        internal bool ValidateTrace(PhysicalApronTraceV2 t)
        {
            if (t.EndRoute < t.StartRoute || !PointAt(t.PathIndex, t.StartRoute, t.Start) || !PointAt(t.PathIndex, t.EndRoute, t.End)) return false;
            RequireLateral(t.LateralStart); RequireLateral(t.LateralEnd);
            if (!TailMatches(t, t.OldTail, false) || !TailMatches(t, t.NewTail, true) ||
                (t.OldTail.Active || t.NewTail.Active) && t.LateralStart != t.LateralEnd) return false;
            long initialTravel = t.Count == 0 ? 0 : t.Motions[0].Segment.TravelBefore;
            var terminal = FractionalMotionTimeline.Sample(t.Timeline.AsSpan(0, t.Count),
                new RaceMotionPoint(t.StartRoute, initialTravel), JunctionAdmissionSession.FractionParts, tickSeconds);
            if (terminal.RouteNanometers != t.EndRoute) return false;
            long route = t.StartRoute, fraction = 0; var point = t.Start; int transitions = 0;
            for (int i = 0; i < t.Count; i++)
            {
                var m = t.Motions[i]; var s = m.Segment; var r = m.Receipt;
                if (!ReferenceEquals(r.Owner, t.Projection) || s.OnPenaltyLoop || s.OnStartApproach || s.RouteBefore != route || s.RouteAfter < route ||
                    r.StartFraction < fraction || r.EndFraction <= r.StartFraction || r.EndFraction > JunctionAdmissionSession.FractionParts ||
                    r.TravelNanometers != s.RouteAfter - s.RouteBefore || r.TravelNanometers != s.TravelAfter - s.TravelBefore ||
                    r.StartFraction != FractionalMotionTimeline.Quantize(s.StartSeconds, tickSeconds) || r.EndFraction != FractionalMotionTimeline.Quantize(s.EndSeconds, tickSeconds) ||
                    !layout.GetPath(t.PathIndex).IsSinglePhysicalSlice(s.RouteBefore, s.RouteAfter) ||
                    !Same(r.Start, layout.GetPath(t.PathIndex).MapRoute(s.RouteBefore)) ||
                    !Same(r.End, r.TravelNanometers == 0 ? r.Start : layout.GetPath(t.PathIndex).MapRoute(s.RouteAfter, PhysicalBoundarySide.Incoming)) ||
                    !Join(point, r.Start, route, t.NewTail, ref transitions)) return false;
                point = r.End; route = s.RouteAfter; fraction = r.EndFraction;
            }
            if (route != t.EndRoute || !Join(point, t.End, route, t.NewTail, ref transitions) || t.NewTail.Active != (transitions == 1)) return false;
            if (t.Count == 0 && t.StartRoute != t.EndRoute) return false;
            return true;
        }
        private bool Join(PhysicalTrackPoint a, PhysicalTrackPoint b, long route, PhysicalApronTailV2 tail, ref int transitions)
        {
            if (Same(a, b)) return true;
            if (!tail.Active || tail.SeamRouteNanometers != route || !Same(a, tail.Point)) return false;
            var seam = seams[tail.PathIndex][tail.OccurrenceIndex];
            if (!Same(b, seam.Target)) return false;
            return ++transitions <= 1;
        }
        private bool TailMatches(PhysicalApronTraceV2 t, PhysicalApronTailV2 tail, bool newlyCrossed)
        {
            if (!tail.Active) return true;
            if (!ReferenceEquals(tail.Owner, this) || tail.PathIndex != t.PathIndex || tail.OccurrenceIndex < 0 || tail.OccurrenceIndex >= seams[t.PathIndex].Length || tail.Lateral != t.LateralStart) return false;
            var s = seams[t.PathIndex][tail.OccurrenceIndex];
            if (tail.ConnectionIndex != s.Connection || tail.SeamRouteNanometers != s.Route || !Same(tail.Point, s.Source)) return false;
            return newlyCrossed ? s.Route >= t.StartRoute && s.Route <= t.EndRoute : s.Route <= t.StartRoute && t.StartRoute - s.Route <= halfBody;
        }
        private bool InScope(int region, PhysicalApronTraceV2 t)
        {
            if (Scope(region, new PhysicalOccupancySweep(t.AthleteId, t.Start, 0, t.LateralStart, t.LateralStart)) < 0 ||
                Scope(region, new PhysicalOccupancySweep(t.AthleteId, t.End, 0, t.LateralEnd, t.LateralEnd)) < 0 ||
                t.OldTail.Active && Scope(region, TailSweep(t.OldTail, t.AthleteId)) < 0 || t.NewTail.Active && Scope(region, TailSweep(t.NewTail, t.AthleteId)) < 0) return false;
            for (int i = 0; i < t.Count; i++)
            { var r = t.Motions[i].Receipt; if (Scope(region, new PhysicalOccupancySweep(t.AthleteId, r.Start, r.TravelNanometers, t.LateralStart, t.LateralEnd)) < 0) return false; }
            return true;
        }
        private int Scope(int region, PhysicalOccupancySweep s)
        {
            try { map.Validate(s.Start); } catch (ArgumentException) { return -1; }
            if (!s.AthleteId.IsValid) return -1;
            int c = Corridor(s.Start.LoopId);
            if (c < 0 || s.Start.LocalNanometers < starts[c] ||
                s.TravelNanometers > ends[c] - s.Start.LocalNanometers || s.Start.Closed && s.Start.LocalNanometers + s.TravelNanometers >= s.Start.LoopLengthNanometers) return -1;
            // Existing corridor controller remains authoritative; reject impossible lateral evidence here as well.
            long from = s.Start.LocalNanometers, to = from + s.TravelNanometers;
            var loop = track.GetLoop(s.Start.LoopId); double width = samplers[c].Sample(new TrackDistance(from / SkiState.NanometersPerMillimeter)).WidthMeters;
            for (int i = 0; i < loop.SegmentCount; i++)
            { var part = loop.GetSegment(i); long start = part.StartDistance.Millimeters * SkiState.NanometersPerMillimeter; if (start <= to && start + part.Length.Millimeters * SkiState.NanometersPerMillimeter >= from) width = Math.Min(width, part.WidthMeters); }
            double limit = width * .5 - body.EdgeMargin;
            return Math.Abs(s.LateralBeforeMeters) <= limit && Math.Abs(s.LateralAfterMeters) <= limit ? c : -1;
        }
        private PhysicalOccupancySweep Slice(PhysicalApronTraceV2 t, long start, long end)
        {
            double a = t.LateralStart + (t.LateralEnd - t.LateralStart) * start / JunctionAdmissionSession.FractionParts;
            double b = t.LateralStart + (t.LateralEnd - t.LateralStart) * end / JunctionAdmissionSession.FractionParts;
            var point = t.Start;
            for (int i = 0; i < t.Count; i++)
            {
                var r = t.Motions[i].Receipt;
                if (start >= r.StartFraction && end <= r.EndFraction) return t.Projection.Slice(r, t.AthleteId, start, end, a, b);
                if (r.EndFraction <= start) point = i + 1 < t.Count ? t.Motions[i + 1].Receipt.Start : t.End;
            }
            if (t.Count > 0 && start >= t.Motions[t.Count - 1].Receipt.EndFraction) point = t.End;
            return new PhysicalOccupancySweep(t.AthleteId, point, 0, a, b);
        }
        private bool Touches(int region, PhysicalApronTraceV2 t)
        {
            int j = definition.GetRegion(region).JunctionIndex;
            if (junctionMap.TouchesForwardSweep(j, t.Start, 0, halfBody) || junctionMap.TouchesForwardSweep(j, t.End, 0, halfBody) ||
                t.OldTail.Active && junctionMap.TouchesForwardSweep(j, t.OldTail.Point, 0, halfBody) ||
                t.NewTail.Active && junctionMap.TouchesForwardSweep(j, t.NewTail.Point, 0, halfBody)) return true;
            for (int i = 0; i < t.Count; i++)
            { var r = t.Motions[i].Receipt; if (junctionMap.TouchesForwardSweep(j, r.Start, r.TravelNanometers, halfBody)) return true; }
            return false;
        }
        private bool PointAt(int path, long route, PhysicalTrackPoint point)
        {
            try { return Same(point, layout.GetPath(path).MapRoute(route)) || Same(point, layout.GetPath(path).MapRoute(route, PhysicalBoundarySide.Incoming)); }
            catch (ArgumentException) { return false; }
        }
        private bool Same(PhysicalTrackPoint a, PhysicalTrackPoint b)
        { try { return map.SameLoop(a, b) && a.LocalNanometers == b.LocalNanometers; } catch (ArgumentException) { return false; } }
        private int Corridor(string loop) { for (int i = 0; i < starts.Length; i++) if (definition.GetCorridor(i).LoopId == loop) return i; return -1; }
        private bool ContinuousSeam(PhysicalTrackPoint source, PhysicalTrackPoint target)
        {
            int a = Corridor(source.LoopId), b = Corridor(target.LoopId);
            if (a < 0 || b < 0) return false;
            var from = definition.GetCorridor(a); var to = definition.GetCorridor(b);
            double sd = source.LocalNanometers / (double)SkiState.NanometersPerMeter, td = target.LocalNanometers / (double)SkiState.NanometersPerMeter;
            int ai = 1, bi = 1;
            while (ai < from.Count - 1 && from.Get(ai).Distance < sd) ai++;
            while (bi < to.Count - 1 && to.Get(bi).Distance <= td) bi++;
            if (sd < from.Start || sd > from.End || td < to.Start || td > to.End) return false;
            var aa = from.Get(ai - 1); var ab = from.Get(ai); var ba = to.Get(bi - 1); var bb = to.Get(bi);
            double st = (sd - aa.Distance) / (ab.Distance - aa.Distance), tt = (td - ba.Distance) / (bb.Distance - ba.Distance);
            if (aa.X + (ab.X - aa.X) * st != ba.X + (bb.X - ba.X) * tt ||
                aa.Y + (ab.Y - aa.Y) * st != ba.Y + (bb.Y - ba.Y) * tt ||
                aa.Z + (ab.Z - aa.Z) * st != ba.Z + (bb.Z - ba.Z) * tt) return false;
            double ax = ab.X - aa.X, az = ab.Z - aa.Z, bx = bb.X - ba.X, bz = bb.Z - ba.Z;
            return ax * bx + az * bz > 0 && Math.Abs(ax * bz - az * bx) <= 1e-12 * (Math.Abs(ax * bz) + Math.Abs(az * bx));
        }
        private static PhysicalOccupancySweep TailSweep(PhysicalApronTailV2 t, AthleteId id) => new PhysicalOccupancySweep(id, t.Point, 0, t.Lateral, t.Lateral);
        private static long Nm(double wholeMillimetreMeters) => checked(TrackDistance.FromMeters(wholeMillimetreMeters).Millimeters * SkiState.NanometersPerMillimeter);
        private static void RequireLateral(double value) => PhysiologyNumbers.RequireRange(value, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(value));
    }
}
