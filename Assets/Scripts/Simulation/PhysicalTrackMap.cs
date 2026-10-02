using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public enum PhysicalBoundarySide { Outgoing=0, Incoming=1 }
    public readonly struct PhysicalTrackPoint
    {
        internal PhysicalTrackMap Owner { get; }
        internal int LoopIndex { get; }
        public string LoopId { get; }
        public long LocalNanometers { get; }
        public long LoopLengthNanometers { get; }
        public bool Closed { get; }
        public bool IsValid=>Owner!=null;
        internal PhysicalTrackPoint(PhysicalTrackMap owner,int index,TrackLoopDefinition loop,long distance)
        { Owner=owner;LoopIndex=index;LoopId=loop.Id;LoopLengthNanometers=checked(loop.Length.Millimeters*SkiState.NanometersPerMillimeter);
          Closed=loop.Closed;LocalNanometers=Closed && distance==LoopLengthNanometers?0:distance; }
    }
    public sealed class PhysicalTrackMap
    {
        private readonly TrackDefinition track;
        private readonly RaceRoute route;
        private readonly int[] sectionLoops;
        private readonly long routeLength;
        public string TrackSnapshotJson { get; }
        public string RoutePlanSnapshotJson => route.PlanSnapshotJson;
        public string RouteRequirementsSnapshotJson => route.RequirementsSnapshotJson;
        public PhysicalTrackMap(TrackDefinition track,RaceRoute route)
        {
            if(track==null || route==null) throw new ArgumentNullException();
            TrackSnapshotJson=track.ToJson();
            if(route.TrackSnapshotJson!=TrackSnapshotJson) throw new ArgumentException("Physical mapping requires the route's exact authored track.");
            this.track=track;this.route=route;routeLength=checked(route.Length.Millimeters*SkiState.NanometersPerMillimeter);
            sectionLoops=new int[route.SectionCount];
            for(int i=0;i<sectionLoops.Length;i++) sectionLoops[i]=LoopIndex(route.GetSection(i).LoopId);
        }
        public PhysicalTrackPoint MapRoute(long routeNanometers,PhysicalBoundarySide side=PhysicalBoundarySide.Outgoing)
        {
            if(routeNanometers<0 || routeNanometers>routeLength || (side!=PhysicalBoundarySide.Outgoing && side!=PhysicalBoundarySide.Incoming))
                throw new ArgumentOutOfRangeException();
            int index=route.Sample(new TrackDistance(routeNanometers/SkiState.NanometersPerMillimeter)).SectionIndex;
            if(side==PhysicalBoundarySide.Incoming && index>0 && routeNanometers==route.GetSection(index).Start.Millimeters*SkiState.NanometersPerMillimeter) index--;
            var section=route.GetSection(index);
            long local=routeNanometers-(section.Start.Millimeters-section.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter;
            return Point(sectionLoops[index],local);
        }
        public PhysicalTrackPoint MapPenalty(long penaltyNanometers)=>MapLoop(track.Penalty.LoopId,penaltyNanometers);
        public PhysicalTrackPoint MapLoop(string loopId,long localNanometers)=>Point(LoopIndex(loopId),localNanometers);
        private PhysicalTrackPoint Point(int index,long local)
        {
            var loop=track.GetLoop(index);long length=checked(loop.Length.Millimeters*SkiState.NanometersPerMillimeter);
            if(local<0 || local>length) throw new ArgumentOutOfRangeException(nameof(local));
            return new PhysicalTrackPoint(this,index,loop,local);
        }
        private int LoopIndex(string id)
        { for(int i=0;i<track.LoopCount;i++) if(track.GetLoop(i).Id==id) return i;throw new ArgumentException("Unknown physical loop."); }
        public bool SameLoop(PhysicalTrackPoint a,PhysicalTrackPoint b)
        { Validate(a);Validate(b);return a.LoopIndex==b.LoopIndex; }
        public long SignedShortestGap(PhysicalTrackPoint from,PhysicalTrackPoint to)
        {
            if(!SameLoop(from,to)) throw new ArgumentException("Different loops have no shared longitudinal gap.");
            long delta=to.LocalNanometers-from.LocalNanometers;
            if(from.Closed)
            {
                if(delta>from.LoopLengthNanometers/2) delta-=from.LoopLengthNanometers;
                else if(delta<-(from.LoopLengthNanometers/2)) delta+=from.LoopLengthNanometers;
            }
            return delta;
        }
        public long ForwardDistance(PhysicalTrackPoint from,PhysicalTrackPoint to)
        {
            if(!SameLoop(from,to)) throw new ArgumentException("Different physical loops.");
            long delta=to.LocalNanometers-from.LocalNanometers;
            if(delta<0){if(!from.Closed) throw new ArgumentException("Open loop cannot wrap.");delta+=from.LoopLengthNanometers;}
            return delta;
        }
        internal bool IsBoundTo(TrackDefinition authoredTrack, RaceRoute authoredRoute)
            => ReferenceEquals(track, authoredTrack) && ReferenceEquals(route, authoredRoute);
        internal void Validate(PhysicalTrackPoint point)
        { if(!ReferenceEquals(point.Owner,this)) throw new ArgumentException("Foreign/default physical point."); }
        internal bool IsSingleRouteSection(long start, long end)
        {
            int first = route.Sample(new TrackDistance(start / SkiState.NanometersPerMillimeter)).SectionIndex;
            int last = route.Sample(new TrackDistance(end / SkiState.NanometersPerMillimeter)).SectionIndex;
            if (end > start && last > 0 && end == route.GetSection(last).Start.Millimeters * SkiState.NanometersPerMillimeter) last--;
            return first == last;
        }
    }
}
