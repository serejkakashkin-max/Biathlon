using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public readonly struct OvertakeOpportunity
    {
        public SkiPlacement Target { get; }
        public double PassingLaneMeters { get; }
        public bool Available { get; }
        public bool Blocked => Target.IsValid && !Available;
        public long ClearanceDistanceNanometers { get; }
        internal OvertakeOpportunity(SkiPlacement target, double lane, bool available, long clearance)
        { Target = target; PassingLaneMeters = lane; Available = available; ClearanceDistanceNanometers = clearance; }
    }
    /// <summary>Static occupancy query shared by controller and AI. Caller supplies the minimum path width.
    /// It does not reserve a lane or replace simultaneous movement resolution.</summary>
    public static class OvertakeOpportunityQuery
    {
        public static OvertakeOpportunity Evaluate(OvertakeDefinition definition, OccupancySystem occupancy,
            SkiPlacement current, double minimumWidthMeters, bool allowed, ReadOnlySpan<SkiPlacement> roster)
        {
            if (definition == null || occupancy == null) throw new ArgumentNullException();
            if (!current.IsValid || !occupancy.FitsWidth(current.LateralMeters, minimumWidthMeters))
                throw new ArgumentException("Current placement must fit the path width.");
            for (int i = 0; i < roster.Length; i++) if (!roster[i].IsValid) throw new ArgumentException("Invalid roster.");
            if (!allowed) return default;
            long bestGap = long.MaxValue; var target = default(SkiPlacement);
            for (int i = 0; i < roster.Length; i++)
            {
                var candidate = roster[i];
                if (candidate.AthleteId.Equals(current.AthleteId) || candidate.CorridorId != current.CorridorId) continue;
                if (Math.Abs(candidate.LateralMeters - current.LateralMeters) >= occupancy.Definition.LateralSeparation) continue;
                long gap = candidate.Motion.DistanceNanometers - current.Motion.DistanceNanometers;
                if (gap <= 0 || gap / (double)SkiState.NanometersPerMeter > definition.InitiationGap) continue;
                if (gap < bestGap || (gap == bestGap && string.CompareOrdinal(candidate.AthleteId.Value, target.AthleteId.Value) < 0))
                { bestGap = gap; target = candidate; }
            }
            if (!target.IsValid) return default;
            long clearance = checked((long)Math.Ceiling((occupancy.Definition.LongitudinalSeparation + definition.PassingClearance) * SkiState.NanometersPerMeter));
            long passDistance = checked(target.Motion.DistanceNanometers + clearance);
            for (int side = -1; side <= 1; side += 2)
            {
                double lane = target.LateralMeters + side * (occupancy.Definition.LateralSeparation + definition.PassingClearance);
                if (!occupancy.FitsWidth(lane, minimumWidthMeters)) continue;
                var outPosition = At(current, current.Motion.DistanceNanometers, lane);
                var aheadPosition = At(current, passDistance, lane);
                if (occupancy.QueryMove(current, outPosition, minimumWidthMeters, roster).Blocked ||
                    occupancy.QueryMove(outPosition, aheadPosition, minimumWidthMeters, roster).Blocked) continue;
                return new OvertakeOpportunity(target, lane, true, clearance);
            }
            return new OvertakeOpportunity(target, 0, false, clearance);
        }
        private static SkiPlacement At(SkiPlacement source, long distance, double lateral) =>
            new SkiPlacement(source.AthleteId, source.CorridorId, new SkiState(distance, source.Motion.SpeedMetersPerSecond), lateral);
    }
}
