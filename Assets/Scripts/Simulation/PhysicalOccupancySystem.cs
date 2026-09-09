using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalOccupancyConflict
    {
        public bool Blocked { get; }
        public AthleteId FirstAthlete { get; }
        public AthleteId SecondAthlete { get; }
        public double FirstContactFraction { get; }
        internal PhysicalOccupancyConflict(AthleteId first, AthleteId second, double fraction)
        { Blocked = true; FirstAthlete = first; SecondAthlete = second; FirstContactFraction = fraction; }
    }

    /// <summary>A linear, forward, single-branch trajectory over one common time interval.</summary>
    public readonly struct PhysicalOccupancySweep
    {
        public AthleteId AthleteId { get; }
        public PhysicalTrackPoint Start { get; }
        public long TravelNanometers { get; }
        public double LateralBeforeMeters { get; }
        public double LateralAfterMeters { get; }

        public PhysicalOccupancySweep(AthleteId athleteId, PhysicalTrackPoint start, long travelNanometers,
            double lateralBeforeMeters, double lateralAfterMeters)
        {
            if (!athleteId.IsValid || !start.IsValid) throw new ArgumentException("Invalid physical sweep identity.");
            if (travelNanometers < 0 || travelNanometers > start.LoopLengthNanometers ||
                (!start.Closed && travelNanometers > start.LoopLengthNanometers - start.LocalNanometers))
                throw new ArgumentOutOfRangeException(nameof(travelNanometers), "Split at branches and at most one circuit per substep.");
            PhysiologyNumbers.RequireRange(lateralBeforeMeters, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(lateralBeforeMeters));
            PhysiologyNumbers.RequireRange(lateralAfterMeters, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(lateralAfterMeters));
            AthleteId = athleteId; Start = start; TravelNanometers = travelNanometers;
            LateralBeforeMeters = lateralBeforeMeters; LateralAfterMeters = lateralAfterMeters;
        }
    }

    /// <summary>Continuous same-loop body queries. Branch conflicts require junction arbitration separately.</summary>
    public sealed class PhysicalOccupancySystem
    {
        private readonly PhysicalTrackMap map;
        private readonly OccupancySystem occupancy;
        public PhysicalOccupancySystem(PhysicalTrackMap map, OccupancyDefinition definition)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            occupancy = new OccupancySystem(definition);
        }

        public OccupancyQuery QueryRelativeMove(PhysicalOccupancySweep a, PhysicalOccupancySweep b)
        {
            Validate(a); Validate(b);
            if (a.AthleteId.Equals(b.AthleteId)) throw new ArgumentException("Pair queries require distinct athletes.");
            if (!map.SameLoop(a.Start, b.Start)) return new OccupancyQuery(false, false, default, 1);
            long length = a.Start.LoopLengthNanometers;
            // Translation keeps all SkiState coordinates nonnegative without changing relative geometry.
            long origin = a.Start.Closed ? checked(3 * length) : 0;
            var aFrom = Placement(a, origin, false); var aTo = Placement(a, origin, true);
            var result = new OccupancyQuery(false, false, default, 1);
            // Each travel <= L, initial relative distance in (-L,L), so relative endpoints
            // lie in (-2L,2L). With body separation < L, exactly these five images suffice.
            int radius = a.Start.Closed ? 2 : 0;
            for (int image = -radius; image <= radius; image++)
            {
                long offset = checked(origin + image * length);
                var query = occupancy.QueryRelativeMove(aFrom, aTo, Placement(b, offset, false), Placement(b, offset, true));
                if (query.Blocked && (!result.Blocked || query.FirstContactFraction < result.FirstContactFraction)) result = query;
            }
            return result;
        }

        /// <summary>Checks an entire common time interval, including stationary athletes. A caller
        /// shortening a trajectory must rebuild and recheck every affected interval and suffix.
        /// This reports conflicts; it neither grants junction admission nor resolves them.</summary>
        public PhysicalOccupancyConflict QueryBatch(ReadOnlySpan<PhysicalOccupancySweep> sweeps)
        {
            // Validate the entire batch even if its first pair already overlaps.
            for (int i = 0; i < sweeps.Length; i++)
            {
                Validate(sweeps[i]);
                for (int j = 0; j < i; j++)
                    if (sweeps[i].AthleteId.Equals(sweeps[j].AthleteId)) throw new ArgumentException("Duplicate physical athlete.");
            }
            PhysicalOccupancyConflict result = default;
            for (int i = 0; i < sweeps.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    var query = QueryRelativeMove(sweeps[i], sweeps[j]);
                    if (!query.Blocked) continue;
                    var first = sweeps[i].AthleteId; var second = sweeps[j].AthleteId;
                    if (string.CompareOrdinal(first.Value, second.Value) > 0) { var swap = first; first = second; second = swap; }
                    int order = string.CompareOrdinal(first.Value, result.FirstAthlete.Value);
                    if (order == 0) order = string.CompareOrdinal(second.Value, result.SecondAthlete.Value);
                    if (!result.Blocked || query.FirstContactFraction < result.FirstContactFraction ||
                        (query.FirstContactFraction == result.FirstContactFraction && order < 0))
                        result = new PhysicalOccupancyConflict(first, second, query.FirstContactFraction);
                }
            return result;
        }

        private void Validate(PhysicalOccupancySweep sweep)
        {
            map.Validate(sweep.Start);
            if (!sweep.AthleteId.IsValid) throw new ArgumentException("Invalid physical sweep.");
            if (sweep.Start.Closed && occupancy.Definition.LongitudinalSeparation >= sweep.Start.LoopLengthNanometers / (double)SkiState.NanometersPerMeter)
                throw new ArgumentException("Body separation must be smaller than the closed loop.");
        }

        private static SkiPlacement Placement(PhysicalOccupancySweep sweep, long offset, bool end) =>
            new SkiPlacement(sweep.AthleteId, sweep.Start.LoopId,
                new SkiState(checked(offset + sweep.Start.LocalNanometers + (end ? sweep.TravelNanometers : 0)), 0),
                end ? sweep.LateralAfterMeters : sweep.LateralBeforeMeters);
    }
}
