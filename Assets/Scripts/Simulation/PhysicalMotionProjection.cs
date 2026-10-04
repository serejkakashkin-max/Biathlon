using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalMotionReceipt
    {
        internal PhysicalMotionProjection Owner { get; }
        public PhysicalTrackPoint Start { get; }
        public PhysicalTrackPoint End { get; }
        public long TravelNanometers { get; }
        public long StartFraction { get; }
        public long EndFraction { get; }
        internal PhysicalMotionReceipt(PhysicalMotionProjection owner, PhysicalTrackPoint start, PhysicalTrackPoint end,
            long travel, long startFraction, long endFraction)
        { Owner = owner; Start = start; End = end; TravelNanometers = travel; StartFraction = startFraction; EndFraction = endFraction; }
    }

    /// <summary>Projects one validated timed receipt onto one physical branch. Explicit penalty
    /// start is supplied from traversal state; route progress cannot identify penalty position.</summary>
    public sealed class PhysicalMotionProjection
    {
        private readonly PhysicalTrackMap map;
        private readonly double tickSeconds;
        private readonly PhysicalRangeLaneMap rangeLane;
        private readonly PhysicalRangePathMap rangePath;
        private readonly StartApproachMap approach;
        private readonly long behind;
        private readonly PhysicalStartApproachMap startOverlay;
        internal bool IsBoundTo(PhysicalTrackMap exactMap, PhysicalRangePathMap exactPath, double exactTickSeconds)
            => ReferenceEquals(map, exactMap) && ReferenceEquals(rangePath, exactPath) && tickSeconds == exactTickSeconds;
        public PhysicalMotionProjection(PhysicalTrackMap map, double tickSeconds, PhysicalRangeLaneMap rangeLane = null,
            StartApproachMap approach = null, long startBehindLineNanometers = 0, PhysicalStartApproachMap startOverlay = null,
            PhysicalRangePathMap rangePath = null)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            if (rangeLane != null && !ReferenceEquals(rangeLane.PhysicalMap, map)) throw new ArgumentException("Foreign range lane map.");
            if(rangePath!=null&&(!ReferenceEquals(rangePath.PhysicalMap,map)||rangeLane!=null))
                throw new ArgumentException("One physical range layout bound to this map is required.");
            this.rangeLane = rangeLane;
            this.rangePath = rangePath;
            if(startBehindLineNanometers<0 || (startBehindLineNanometers>0 && approach==null) ||
                (approach!=null && (approach.TrackJson!=map.TrackSnapshotJson || approach.RoutePlanJson!=map.RoutePlanSnapshotJson ||
                approach.RouteRequirementsJson!=map.RouteRequirementsSnapshotJson || startBehindLineNanometers>approach.LengthNanometers)))
                throw new ArgumentException("Exact physical approach context and grid position required.");
            if(startOverlay!=null && (!ReferenceEquals(startOverlay.PhysicalMap,map) || !ReferenceEquals(startOverlay.Approach,approach))) throw new ArgumentException("Foreign start overlay.");
            this.startOverlay=startOverlay;this.approach=approach;behind=startBehindLineNanometers;
            FractionalMotionTimeline.Quantize(0, tickSeconds); this.tickSeconds = tickSeconds;
        }
        public PhysicalMotionReceipt Project(RaceMotionSegment segment, PhysicalTrackPoint physicalStart)
        {
            map.Validate(physicalStart);
            Span<RaceMotionSegment> one = stackalloc RaceMotionSegment[1]; one[0] = segment;
            var origin = new RaceMotionPoint(segment.RouteBefore, segment.TravelBefore);
            FractionalMotionTimeline.Sample(one, origin, 0, tickSeconds);
            long travel = segment.TravelAfter - segment.TravelBefore;
            if (travel > physicalStart.LoopLengthNanometers) throw new ArgumentException("Split receipts at circuit boundaries.");
            PhysicalTrackPoint end;
            if(segment.OnStartApproach)
            {
                if(approach==null || segment.TravelAfter>behind)throw new ArgumentException("Split approach receipts at the physical route entry.");
                var expected=startOverlay==null ? map.MapLoop(approach.LoopId,checked(approach.LengthNanometers-behind+segment.TravelBefore)) : startOverlay.MapRemaining(behind-segment.TravelBefore);
                if(!map.SameLoop(expected,physicalStart) || expected.LocalNanometers!=physicalStart.LocalNanometers)
                    throw new ArgumentException("Physical approach origin disagrees with grid travel.");
                end=Advance(physicalStart,travel);
            }
            else if (segment.OnPenaltyLoop)
            {
                if (!map.SameLoop(physicalStart, map.MapPenalty(0))) throw new ArgumentException("Penalty receipt needs an explicit penalty-loop origin.");
                end = Advance(physicalStart, travel);
            }
            else
            {
                var expected = MapRoute(segment.RouteBefore);
                if (!map.SameLoop(expected, physicalStart) || expected.LocalNanometers != physicalStart.LocalNanometers)
                    throw new ArgumentException("Physical start disagrees with route receipt.");
                end = rangePath!=null&&travel==0 ? physicalStart : MapRoute(segment.RouteAfter, PhysicalBoundarySide.Incoming);
                bool single=rangePath==null ? map.IsSingleRouteSection(segment.RouteBefore,segment.RouteAfter) : rangePath.IsSinglePhysicalSlice(segment.RouteBefore,segment.RouteAfter);
                if (!single || !map.SameLoop(physicalStart, end) ||
                    Advance(physicalStart, travel).LocalNanometers != end.LocalNanometers)
                    throw new ArgumentException("Split route receipts at section/branch transitions.");
            }
            return new PhysicalMotionReceipt(this, physicalStart, end, travel,
                FractionalMotionTimeline.Quantize(segment.StartSeconds, tickSeconds), FractionalMotionTimeline.Quantize(segment.EndSeconds, tickSeconds));
        }
        private PhysicalTrackPoint MapRoute(long distance, PhysicalBoundarySide side = PhysicalBoundarySide.Outgoing)
            => rangePath!=null ? rangePath.MapRoute(distance,side) : rangeLane == null ? map.MapRoute(distance, side) : rangeLane.MapRoute(distance, side);
        public PhysicalOccupancySweep Slice(PhysicalMotionReceipt receipt, AthleteId athlete, long startFraction, long endFraction,
            double lateralBeforeMeters, double lateralAfterMeters)
        {
            if (!ReferenceEquals(receipt.Owner, this)) throw new ArgumentException("Foreign/default projected receipt.");
            if (startFraction < receipt.StartFraction || endFraction > receipt.EndFraction || endFraction < startFraction)
                throw new ArgumentOutOfRangeException();
            long start = Distance(receipt, startFraction), end = Distance(receipt, endFraction);
            return new PhysicalOccupancySweep(athlete, Advance(receipt.Start, start), end - start, lateralBeforeMeters, lateralAfterMeters);
        }
        private static long Distance(PhysicalMotionReceipt receipt, long fraction) =>
            (long)((decimal)receipt.TravelNanometers * (fraction - receipt.StartFraction) / (receipt.EndFraction - receipt.StartFraction));
        private PhysicalTrackPoint Advance(PhysicalTrackPoint start, long travel)
        {
            long local = checked(start.LocalNanometers + travel);
            if (start.Closed && local >= start.LoopLengthNanometers) local -= start.LoopLengthNanometers;
            return map.MapLoop(start.LoopId, local);
        }
    }
}
