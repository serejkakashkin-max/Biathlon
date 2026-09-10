using System;

namespace Biathlon.Simulation
{
    /// <summary>Explicit pack-only retiming to integer billionths of a tick. Round-to-even
    /// boundaries preserve exact receipt endpoints. Distinct times that collapse are rejected.</summary>
    public static class FractionalMotionTimeline
    {
        public static long Quantize(double seconds, double tickSeconds)
        {
            if (double.IsNaN(tickSeconds) || double.IsInfinity(tickSeconds) || tickSeconds <= 0 ||
                double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > tickSeconds)
                throw new ArgumentOutOfRangeException();
            if (seconds == 0) return 0;
            if (seconds == tickSeconds) return JunctionAdmissionSession.FractionParts;
            return checked((long)Math.Round(seconds / tickSeconds * JunctionAdmissionSession.FractionParts, MidpointRounding.ToEven));
        }

        /// <summary>scratchSeconds is disposable scratch. output remains untouched on any error.</summary>
        public static int MergeBoundaries(ReadOnlySpan<RaceMotionSegment> allSegments, double tickSeconds,
            Span<double> scratchSeconds, Span<long> output)
        {
            if (output.Length < checked(2 + allSegments.Length * 2)) throw new ArgumentException("Worst-case output capacity required.");
            int count = RaceMotionTimeline.MergeBoundaries(allSegments, tickSeconds, scratchSeconds);
            long previous = -1;
            for (int i = 0; i < count; i++)
            {
                long fraction = Quantize(scratchSeconds[i], tickSeconds);
                if (fraction <= previous) throw new InvalidOperationException("Distinct boundaries collapse at pack tick precision; replan rather than erase a segment.");
                previous = fraction;
            }
            for (int i = 0; i < count; i++) output[i] = Quantize(scratchSeconds[i], tickSeconds);
            return count;
        }

        public static RaceMotionPoint Sample(ReadOnlySpan<RaceMotionSegment> trajectory, RaceMotionPoint initial,
            long fraction, double tickSeconds)
        {
            if (fraction < 0 || fraction > JunctionAdmissionSession.FractionParts) throw new ArgumentOutOfRangeException(nameof(fraction));
            // Reuse the authoritative receipt validation, including the chain after the sampled time.
            RaceMotionTimeline.Sample(trajectory, initial, 0, tickSeconds);
            long previousEnd = 0; double previousSeconds = 0;
            for (int i = 0; i < trajectory.Length; i++)
            {
                var segment = trajectory[i];
                long start = Quantize(segment.StartSeconds, tickSeconds), end = Quantize(segment.EndSeconds, tickSeconds);
                if (end <= start || (segment.StartSeconds > previousSeconds && start <= previousEnd))
                    throw new InvalidOperationException("Movement or stationary gap collapses at pack tick precision.");
                previousEnd = end; previousSeconds = segment.EndSeconds;
            }
            if (trajectory.Length > 0 && previousSeconds < tickSeconds && previousEnd == JunctionAdmissionSession.FractionParts)
                throw new InvalidOperationException("Stationary suffix collapses at pack tick precision.");
            var point = initial;
            for (int i = 0; i < trajectory.Length; i++)
            {
                var segment = trajectory[i];
                long start = Quantize(segment.StartSeconds, tickSeconds), end = Quantize(segment.EndSeconds, tickSeconds);
                if (fraction <= start) return point;
                if (fraction >= end) { point = new RaceMotionPoint(segment.RouteAfter, segment.TravelAfter); continue; }
                long travel = (long)((decimal)(segment.TravelAfter - segment.TravelBefore) * (fraction - start) / (end - start));
                return new RaceMotionPoint((segment.OnPenaltyLoop || segment.OnStartApproach) ? segment.RouteBefore : segment.RouteBefore + travel, segment.TravelBefore + travel);
            }
            return point;
        }
    }
}
