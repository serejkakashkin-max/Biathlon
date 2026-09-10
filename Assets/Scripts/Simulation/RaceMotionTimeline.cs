using System;
namespace Biathlon.Simulation
{
    public readonly struct RaceMotionPoint
    {
        public long RouteNanometers { get; }
        public long TravelNanometers { get; }
        public RaceMotionPoint(long route, long travel)
        {
            if(route<0 || travel<0) throw new ArgumentOutOfRangeException();
            RouteNanometers=route; TravelNanometers=travel;
        }
    }
    /// <summary>Piecewise-linear timed receipt utilities. All storage is caller-owned. A merged
    /// timeline is a comparison grid, not a substitute for resampling forces or resolving junctions.</summary>
    public static class RaceMotionTimeline
    {
        public static int MergeBoundaries(ReadOnlySpan<RaceMotionSegment> allSegments,double tickSeconds,Span<double> output)
        {
            ValidateDuration(tickSeconds);
            if(output.Length<checked(2+allSegments.Length*2)) throw new ArgumentException("Worst-case boundary capacity required.");
            for(int i=0;i<allSegments.Length;i++) ValidateSegment(allSegments[i],tickSeconds);
            int count=2; output[0]=0; output[1]=tickSeconds;
            for(int i=0;i<allSegments.Length;i++)
            { Insert(allSegments[i].StartSeconds,output,ref count); Insert(allSegments[i].EndSeconds,output,ref count); }
            return count;
        }
        private static void Insert(double time,Span<double> output,ref int count)
        {
            int index=0; while(index<count && output[index]<time) index++;
            if(index<count && output[index]==time) return;
            for(int i=count;i>index;i--) output[i]=output[i-1]; output[index]=time; count++;
        }
        public static RaceMotionPoint Sample(ReadOnlySpan<RaceMotionSegment> trajectory,RaceMotionPoint initial,double time,double tickSeconds)
        {
            ValidateDuration(tickSeconds);
            if(double.IsNaN(time) || time<0 || time>tickSeconds) throw new ArgumentOutOfRangeException(nameof(time));
            var point=initial; double previousTime=0;
            // Validate the full chain, including segments after the requested sample.
            for(int i=0;i<trajectory.Length;i++)
            {
                var segment=trajectory[i]; ValidateSegment(segment,tickSeconds);
                if(segment.RouteBefore!=point.RouteNanometers || segment.TravelBefore!=point.TravelNanometers || segment.StartSeconds<previousTime)
                    throw new ArgumentException("Noncontinuous timed trajectory.");
                point=new RaceMotionPoint(segment.RouteAfter,segment.TravelAfter); previousTime=segment.EndSeconds;
            }
            point=initial;
            for(int i=0;i<trajectory.Length;i++)
            {
                var segment=trajectory[i];
                if(time<=segment.StartSeconds) return point;
                if(time>=segment.EndSeconds) { point=new RaceMotionPoint(segment.RouteAfter,segment.TravelAfter); continue; }
                decimal fraction=(decimal)Math.Max(0,Math.Min(1,(time-segment.StartSeconds)/(segment.EndSeconds-segment.StartSeconds)));
                long distance=(long)((segment.TravelAfter-segment.TravelBefore)*fraction);
                return new RaceMotionPoint((segment.OnPenaltyLoop || segment.OnStartApproach)?segment.RouteBefore:segment.RouteBefore+distance,segment.TravelBefore+distance);
            }
            return point;
        }
        private static void ValidateDuration(double duration)
        { if(double.IsNaN(duration) || double.IsInfinity(duration) || duration<=0) throw new ArgumentOutOfRangeException(nameof(duration)); }
        private static void ValidateSegment(RaceMotionSegment segment,double duration)
        {
            if(double.IsNaN(segment.StartSeconds) || double.IsNaN(segment.EndSeconds) || segment.StartSeconds<0 || segment.EndSeconds>duration ||
                segment.EndSeconds<=segment.StartSeconds || segment.RouteBefore<0 || segment.TravelBefore<0 ||
                segment.RouteAfter<segment.RouteBefore || segment.TravelAfter<segment.TravelBefore ||
                (segment.OnStartApproach && (segment.OnPenaltyLoop || segment.RouteBefore!=0)) ||
                segment.RouteAfter-segment.RouteBefore!=((segment.OnPenaltyLoop || segment.OnStartApproach)?0:segment.TravelAfter-segment.TravelBefore))
                throw new ArgumentException("Invalid timed route/penalty segment.");
        }
    }
}
