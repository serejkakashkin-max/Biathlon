using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>A common-line crossing derived from a complete accepted timed trajectory.
    /// This receipt is an ordering fact, not proof of collision-safe admission.</summary>
    public readonly struct PhysicalRangeArrival
    {
        public AthleteId AthleteId { get; }
        public int BoutIndex { get; }
        public SimulationTick Tick { get; }
        public int TickFrequency { get; }
        public long AdmissionRouteNanometers { get; }
        public long Fraction { get; }
        public bool IsValid => AthleteId.IsValid && Tick.Value > 0 && TickFrequency > 0;

        private PhysicalRangeArrival(AthleteId athlete, int bout, SimulationTick tick, int frequency, long line, long fraction)
        {
            AthleteId = athlete; BoutIndex = bout; Tick = tick; TickFrequency = frequency;
            AdmissionRouteNanometers = line; Fraction = fraction;
        }

        /// <summary>Find the first integer tick fraction whose retimed, floor-interpolated
        /// route point reaches the line. Validate the entire chain, even its suffix after arrival.
        /// Call only with the final accepted prefix; a proposed trajectory cannot reserve a lane.</summary>
        public static bool TryFromAcceptedMotion(AthleteId athlete, int boutIndex, SimulationTick tick,
            int tickFrequency, long admissionRouteNanometers, RaceMotionPoint initial,
            ReadOnlySpan<RaceMotionSegment> segments, out PhysicalRangeArrival arrival)
        {
            arrival = default;
            if (!athlete.IsValid || boutIndex < 0 || tick.Value <= 0 || tickFrequency <= 0 ||
                admissionRouteNanometers < initial.RouteNanometers)
                throw new ArgumentException("A valid race identity/clock and an unpassed admission line are required.");
            double tickSeconds = 1.0 / tickFrequency;
            FractionalMotionTimeline.Sample(segments, initial, 0, tickSeconds);
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.OnPenaltyLoop || segment.OnStartApproach || segment.RouteAfter <= segment.RouteBefore ||
                    segment.RouteBefore > admissionRouteNanometers || segment.RouteAfter < admissionRouteNanometers)
                    continue;
                long start = FractionalMotionTimeline.Quantize(segment.StartSeconds, tickSeconds);
                long end = FractionalMotionTimeline.Quantize(segment.EndSeconds, tickSeconds);
                // decimal holds a long route delta times one billion without overflow. Ceiling
                // matches Sample's floor of travelled nanometres and never grants an early arrival.
                long fraction = start + (long)Math.Ceiling((decimal)(admissionRouteNanometers - segment.RouteBefore) *
                    (end - start) / (segment.RouteAfter - segment.RouteBefore));
                arrival = new PhysicalRangeArrival(athlete, boutIndex, tick, tickFrequency, admissionRouteNanometers, fraction);
                return true;
            }
            return false;
        }
    }
}
