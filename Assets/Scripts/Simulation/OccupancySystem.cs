using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct OccupancyQuery
    {
        public bool Blocked { get; }
        public bool OutsideTrack { get; }
        public AthleteId BlockerId { get; }
        public double FirstContactFraction { get; }
        internal OccupancyQuery(bool blocked, bool outside, AthleteId blocker, double fraction)
        { Blocked = blocked; OutsideTrack = outside; BlockerId = blocker; FirstContactFraction = fraction; }
    }

    public sealed class OccupancySystem
    {
        public OccupancyDefinition Definition { get; }
        public OccupancySystem(OccupancyDefinition definition)
        { Definition = definition ?? throw new ArgumentNullException(nameof(definition)); }

        public bool FitsWidth(double lateralMeters, double widthMeters)
        {
            PhysiologyNumbers.RequireRange(lateralMeters, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(lateralMeters));
            PhysiologyNumbers.RequireRange(widthMeters, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(widthMeters));
            return Math.Abs(lateralMeters) + Definition.EdgeMargin <= widthMeters * 0.5;
        }

        // Validate at roster creation, not in every candidate query. No partial roster mutation.
        public void ValidateRoster(ReadOnlySpan<SkiPlacement> roster)
        {
            for (int i = 0; i < roster.Length; i++)
            {
                Require(roster[i]);
                for (int j = 0; j < i; j++)
                {
                    if (roster[i].AthleteId.Equals(roster[j].AthleteId)) throw new ArgumentException("Duplicate occupancy athlete ID.");
                    if (Overlaps(roster[i], roster[j])) throw new ArgumentException("Initial occupancy overlaps.");
                }
            }
        }

        public bool Overlaps(SkiPlacement a, SkiPlacement b)
        {
            Require(a); Require(b);
            if (!string.Equals(a.CorridorId, b.CorridorId, StringComparison.Ordinal)) return false;
            double longitudinal = (a.Motion.DistanceNanometers - b.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
            return Math.Abs(longitudinal) < Definition.LongitudinalSeparation &&
                Math.Abs(a.LateralMeters - b.LateralMeters) < Definition.LateralSeparation;
        }

        public OccupancyQuery QueryMove(SkiPlacement from, SkiPlacement to, double minimumWidthMeters,
            ReadOnlySpan<SkiPlacement> roster)
        {
            Require(from); Require(to);
            if (!from.AthleteId.Equals(to.AthleteId) || !string.Equals(from.CorridorId, to.CorridorId, StringComparison.Ordinal))
                throw new ArgumentException("A move must preserve athlete/corridor identity.");
            if (!FitsWidth(from.LateralMeters, minimumWidthMeters) || !FitsWidth(to.LateralMeters, minimumWidthMeters))
                return new OccupancyQuery(true, true, default, 0);
            var result = new OccupancyQuery(false, false, default, 1);
            for (int i = 0; i < roster.Length; i++)
            {
                var other = roster[i]; Require(other);
                if (other.AthleteId.Equals(from.AthleteId) || !string.Equals(other.CorridorId, from.CorridorId, StringComparison.Ordinal)) continue;
                double startX = (from.Motion.DistanceNanometers - other.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
                double deltaX = (to.Motion.DistanceNanometers - from.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
                double startY = from.LateralMeters - other.LateralMeters;
                double deltaY = to.LateralMeters - from.LateralMeters;
                double enter = 0, exit = 1;
                if (!IntersectAxis(startX, deltaX, Definition.LongitudinalSeparation, ref enter, ref exit) ||
                    !IntersectAxis(startY, deltaY, Definition.LateralSeparation, ref enter, ref exit) || enter >= exit) continue;
                if (!result.Blocked || enter < result.FirstContactFraction ||
                    (enter == result.FirstContactFraction && string.CompareOrdinal(other.AthleteId.Value, result.BlockerId.Value) < 0))
                    result = new OccupancyQuery(true, false, other.AthleteId, enter);
            }
            return result;
        }
        private static bool IntersectAxis(double start, double delta, double extent, ref double enter, ref double exit)
        {
            if (delta == 0) return Math.Abs(start) < extent;
            double a = (-extent - start) / delta, b = (extent - start) / delta;
            if (a > b) { double swap = a; a = b; b = swap; }
            enter = Math.Max(enter, a); exit = Math.Min(exit, b);
            return enter < exit;
        }
        public OccupancyQuery QueryRelativeMove(SkiPlacement aFrom, SkiPlacement aTo, SkiPlacement bFrom, SkiPlacement bTo)
        {
            Require(aFrom); Require(aTo); Require(bFrom); Require(bTo);
            if (!aFrom.AthleteId.Equals(aTo.AthleteId) || !bFrom.AthleteId.Equals(bTo.AthleteId) ||
                aFrom.CorridorId != aTo.CorridorId || bFrom.CorridorId != bTo.CorridorId)
                throw new ArgumentException("Relative moves must preserve identity and corridor.");
            if (aFrom.CorridorId != bFrom.CorridorId) return new OccupancyQuery(false, false, default, 1);
            double x = (aFrom.Motion.DistanceNanometers - bFrom.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
            double dx = (aTo.Motion.DistanceNanometers - aFrom.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter -
                (bTo.Motion.DistanceNanometers - bFrom.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
            double y = aFrom.LateralMeters - bFrom.LateralMeters;
            double dy = (aTo.LateralMeters - aFrom.LateralMeters) - (bTo.LateralMeters - bFrom.LateralMeters);
            double enter = 0, exit = 1;
            bool hit = IntersectAxis(x, dx, Definition.LongitudinalSeparation, ref enter, ref exit) &&
                IntersectAxis(y, dy, Definition.LateralSeparation, ref enter, ref exit) && enter < exit;
            return new OccupancyQuery(hit, false, hit ? bFrom.AthleteId : default, hit ? enter : 1);
        }
        private static void Require(SkiPlacement placement)
        { if (!placement.IsValid) throw new ArgumentException("Invalid ski placement.", nameof(placement)); }
    }
}
