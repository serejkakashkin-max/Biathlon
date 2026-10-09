using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Read-only physical slice projection. It grants no travel, lane assignment or admission.
    /// Callers must split motion at every returned boundary and integrate the actual sample.</summary>
    public sealed class PhysicalRangePathMap
    {
        private readonly PhysicalTrackMap map;
        private readonly TrackDefinition track;
        private readonly RaceRoute route;
        private readonly PhysicalRangePathDefinition definition;
        private readonly PhysicalRangePathLane lane;
        private readonly bool[] usesRange;
        private readonly TrackSampler[] samplers;
        private readonly long entry, exit, routeEnd, firstFork;
        public PhysicalTrackMap PhysicalMap => map;
        public TrackAnchor MatAnchor => lane.MatAnchor;
        public string LaneId => lane.Id;
        public int LaneOrdinal { get; }
        internal int SliceCount => lane.SliceCount;
        internal PhysicalRangePathSlice GetSlice(int index)=>lane.GetSlice(index);
        internal int FiringSliceIndex=>lane.FiringSliceIndex;
        /// <summary>Cold physical-occurrence metadata, not lane or branch admission.</summary>
        internal bool TryGetOverlayBounds(int sectionIndex,out long entryNanometers,out long exitNanometers)
        {
            if(sectionIndex<0||sectionIndex>=usesRange.Length)throw new ArgumentOutOfRangeException(nameof(sectionIndex));
            entryNanometers=exitNanometers=0;
            if(!usesRange[sectionIndex])return false;
            var s=route.GetSection(sectionIndex);
            long origin=checked((s.Start.Millimeters-s.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter);
            entryNanometers=checked(origin+entry);exitNanometers=checked(origin+exit);return true;
        }
        /// <summary>Cold upper bound for physical terrain receipts across this lane, counting every slice occurrence.</summary>
        internal int TerrainIntervalCount { get; }
        public string ContextJson { get; }
        internal bool IsBoundTo(TrackDefinition authoredTrack,RaceRoute authoredRoute)=>ReferenceEquals(track,authoredTrack)&&ReferenceEquals(route,authoredRoute);
        internal bool IsSameFamily(PhysicalRangePathMap other)=>other!=null&&ReferenceEquals(track,other.track)&&ReferenceEquals(route,other.route)&&ReferenceEquals(definition,other.definition);

        public PhysicalRangePathMap(PhysicalTrackMap map, TrackDefinition track, RaceRoute route,
            PhysicalRangePathDefinition definition, int laneIndex)
        {
            if(map==null||track==null||route==null||definition==null)throw new ArgumentNullException();
            if(map.TrackSnapshotJson!=definition.TrackSnapshotJson||track.ToJson()!=definition.TrackSnapshotJson||
                route.TrackSnapshotJson!=definition.TrackSnapshotJson||map.RoutePlanSnapshotJson!=route.PlanSnapshotJson||
                map.RouteRequirementsSnapshotJson!=route.RequirementsSnapshotJson||laneIndex<0||laneIndex>=definition.Count)
                throw new ArgumentException("Exact physical track/route/path snapshots and lane index required.");
            this.map=map;this.track=track;this.route=route;this.definition=definition;lane=definition.GetLane(laneIndex);LaneOrdinal=laneIndex+1;
            long firstForkMillimeters=long.MaxValue;
            for(int i=0;i<definition.Count;i++)firstForkMillimeters=Math.Min(firstForkMillimeters,definition.GetLane(i).Fork.Millimeters);
            firstFork=checked(firstForkMillimeters*SkiState.NanometersPerMillimeter);
            entry=definition.Entry.Millimeters*SkiState.NanometersPerMillimeter;
            exit=definition.Exit.Millimeters*SkiState.NanometersPerMillimeter;
            routeEnd=route.Length.Millimeters*SkiState.NanometersPerMillimeter;
            var requirements=CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            usesRange=new bool[route.SectionCount];var occurrences=new int[route.LapCount];
            for(int i=0;i<route.SectionCount;i++)
            {
                var section=route.GetSection(i);usesRange[i]=requirements.RequiresRange(section.LapIndex)&&section.HasRange&&section.LoopId==definition.MainLoopId;
                if(usesRange[i]&&(section.LoopStart.Millimeters>definition.Entry.Millimeters||section.LoopEnd.Millimeters<definition.Exit.Millimeters))
                    throw new ArgumentException("A physical range overlay must be wholly contained by its route slice.");
                if(usesRange[i])occurrences[section.LapIndex]++;
            }
            for(int i=0;i<occurrences.Length;i++)if(requirements.RequiresRange(i)&&occurrences[i]!=1)
                throw new ArgumentException("Exactly one path overlay per required shooting lap expected.");
            int terrainIntervals=0;
            for(int i=0;i<lane.SliceCount;i++)
            {
                var slice=lane.GetSlice(i);var physicalLoop=track.GetLoop(slice.LoopId);
                for(int j=0;j<physicalLoop.SegmentCount;j++)
                {
                    var segment=physicalLoop.GetSegment(j);
                    long end=checked(segment.StartDistance.Millimeters+segment.Length.Millimeters);
                    if(segment.StartDistance.Millimeters<slice.End.Millimeters&&end>slice.Start.Millimeters)
                        terrainIntervals=checked(terrainIntervals+1);
                }
            }
            TerrainIntervalCount=terrainIntervals;
            samplers=new TrackSampler[track.LoopCount];for(int i=0;i<samplers.Length;i++)samplers[i]=new TrackSampler(track.GetLoop(i));
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{definition.ToJson(),definition.TrackSnapshotJson,definition.JunctionSnapshotJson,definition.ConnectionSnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson,
                lane.Id,"range_physical_slices_v1;actual_path_sample;individual_mat_offset;no_admission_grant"});
        }

        public PhysicalTrackPoint MapRoute(long distance, PhysicalBoundarySide side=PhysicalBoundarySide.Outgoing)
        {
            var ordinary=map.MapRoute(distance,side);
            if(!Locate(distance,side,out _,out var slice,out long local))return ordinary;
            return map.MapLoop(slice.LoopId,local);
        }

        public TrackSample SampleRoute(long distance)
        {
            map.MapRoute(distance); // validate nanometre bounds before the millimetre sampler conversion
            if(!Locate(distance,PhysicalBoundarySide.Outgoing,out _,out var slice,out long local))
                return route.Sample(new TrackDistance(distance/SkiState.NanometersPerMillimeter)).Track;
            return samplers[LoopIndex(slice.LoopId)].Sample(new TrackDistance(local/SkiState.NanometersPerMillimeter));
        }

        /// <summary>Keep canonical competition lap/section/distance for planning while observing
        /// the selected physical path's actual terrain. This sample grants no path admission.</summary>
        public RaceRouteSample SampleRaceRoute(long distance)
        {
            var actual = SampleRoute(distance);
            var canonical = route.Sample(new TrackDistance(distance / SkiState.NanometersPerMillimeter));
            return new RaceRouteSample(canonical.LapIndex, canonical.SectionIndex, canonical.RouteDistance, actual);
        }

        public long DistanceToNextBoundary(long distance)
        {
            map.MapRoute(distance);
            if(distance==routeEnd)return 0;
            if(Locate(distance,PhysicalBoundarySide.Outgoing,out int sectionIndex,out var slice,out long local))
            {
                var sample=samplers[LoopIndex(slice.LoopId)].Sample(new TrackDistance(local/SkiState.NanometersPerMillimeter));
                var segment=track.GetLoop(slice.LoopId).GetSegment(sample.SegmentIndex);
                long terrainEnd=(segment.StartDistance.Millimeters+segment.Length.Millimeters)*SkiState.NanometersPerMillimeter;
                var section=route.GetSection(sectionIndex);
                long sliceEnd=(section.Start.Millimeters-section.LoopStart.Millimeters+definition.Entry.Millimeters+slice.Offset.Millimeters+slice.Length.Millimeters)*SkiState.NanometersPerMillimeter;
                return Math.Min(terrainEnd-local,sliceEnd-distance);
            }
            int index=route.Sample(new TrackDistance(distance/SkiState.NanometersPerMillimeter)).SectionIndex;
            var current=route.GetSection(index);var ordinary=SampleRoute(distance);
            var s=track.GetLoop(current.LoopId).GetSegment(ordinary.SegmentIndex);
            long origin=(current.Start.Millimeters-current.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter;
            long boundary=origin+(s.StartDistance.Millimeters+s.Length.Millimeters)*SkiState.NanometersPerMillimeter;
            if(usesRange[index]&&distance<origin+entry)boundary=Math.Min(boundary,origin+entry);
            long sectionEnd=(current.Start.Millimeters+current.Length.Millimeters)*SkiState.NanometersPerMillimeter;
            return Math.Min(boundary,sectionEnd)-distance;
        }

        public long FiringRouteNanometers(int lapIndex)
        {
            if(lapIndex<0||lapIndex>=route.LapCount)throw new ArgumentOutOfRangeException(nameof(lapIndex));
            for(int i=0;i<usesRange.Length;i++)if(usesRange[i]&&route.GetSection(i).LapIndex==lapIndex)
                return (route.GetSection(i).Start.Millimeters-route.GetSection(i).LoopStart.Millimeters+definition.Entry.Millimeters+lane.FiringOffset.Millimeters)*SkiState.NanometersPerMillimeter;
            throw new ArgumentException("Lap does not require shooting.");
        }

        /// <summary>The first authored fork of any lane on this shooting lap; it bounds safe shared-prefix staging.</summary>
        public long FirstForkRouteNanometers(int lapIndex)
        {
            if(lapIndex<0||lapIndex>=route.LapCount)throw new ArgumentOutOfRangeException(nameof(lapIndex));
            for(int i=0;i<usesRange.Length;i++)if(usesRange[i]&&route.GetSection(i).LapIndex==lapIndex)
                return checked((route.GetSection(i).Start.Millimeters-route.GetSection(i).LoopStart.Millimeters)*SkiState.NanometersPerMillimeter+firstFork);
            throw new ArgumentException("Lap does not require shooting.");
        }

        /// <summary>The selected lane's own physical turn on the main loop.
        /// Unlike FirstForkRouteNanometers this is distinct for each mat.</summary>
        public long LaneForkRouteNanometers(int lapIndex)
        {
            if(lapIndex<0||lapIndex>=route.LapCount)throw new ArgumentOutOfRangeException(nameof(lapIndex));
            for(int i=0;i<usesRange.Length;i++)if(usesRange[i]&&route.GetSection(i).LapIndex==lapIndex)
                return checked((route.GetSection(i).Start.Millimeters-route.GetSection(i).LoopStart.Millimeters)*
                    SkiState.NanometersPerMillimeter+lane.Fork.Millimeters*SkiState.NanometersPerMillimeter);
            throw new ArgumentException("Lap does not require shooting.");
        }

        internal bool IsSameLayoutFamily(PhysicalRangePathMap other)=>other!=null&&ReferenceEquals(map,other.map)&&
            ReferenceEquals(track,other.track)&&ReferenceEquals(route,other.route)&&ReferenceEquals(definition,other.definition);

        internal bool IsSinglePhysicalSlice(long before,long after)
        {
            map.MapRoute(before);map.MapRoute(after);
            if(after<before)throw new ArgumentException("Ordered receipt coordinates required.");
            if(before==after)return true;
            bool a=Locate(before,PhysicalBoundarySide.Outgoing,out int first,out var from,out _);
            bool b=Locate(after,PhysicalBoundarySide.Incoming,out int last,out var to,out _);
            if(a||b)return a&&b&&first==last&&ReferenceEquals(from,to);
            for(int i=0;i<usesRange.Length;i++)if(usesRange[i])
            {
                long start=(route.GetSection(i).Start.Millimeters-route.GetSection(i).LoopStart.Millimeters)*SkiState.NanometersPerMillimeter;
                if(before<start+exit&&after>start+entry)return false;
            }
            return map.IsSingleRouteSection(before,after);
        }

        private bool Locate(long distance,PhysicalBoundarySide side,out int sectionIndex,out PhysicalRangePathSlice slice,out long local)
        {
            sectionIndex=route.Sample(new TrackDistance(distance/SkiState.NanometersPerMillimeter)).SectionIndex;
            if(side==PhysicalBoundarySide.Incoming&&sectionIndex>0&&distance==route.GetSection(sectionIndex).Start.Millimeters*SkiState.NanometersPerMillimeter)sectionIndex--;
            var section=route.GetSection(sectionIndex);
            long at=distance-(section.Start.Millimeters-section.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter;
            slice=null;local=0;
            if(!usesRange[sectionIndex]||at<entry||at>exit||at==entry&&side==PhysicalBoundarySide.Incoming||at==exit&&side==PhysicalBoundarySide.Outgoing)return false;
            long offset=at-entry;
            for(int i=0;i<lane.SliceCount;i++)
            {
                var candidate=lane.GetSlice(i);long end=(candidate.Offset.Millimeters+candidate.Length.Millimeters)*SkiState.NanometersPerMillimeter;
                if(offset<end||offset==end&&side==PhysicalBoundarySide.Incoming)
                {slice=candidate;local=candidate.Start.Millimeters*SkiState.NanometersPerMillimeter+offset-candidate.Offset.Millimeters*SkiState.NanometersPerMillimeter;return true;}
            }
            throw new InvalidOperationException("Validated range path did not cover its interval.");
        }
        private int LoopIndex(string id){for(int i=0;i<track.LoopCount;i++)if(track.GetLoop(i).Id==id)return i;throw new ArgumentException("Unknown physical loop.");}
    }
}
