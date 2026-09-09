using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct DraftOutput
    {
        public AthleteId LeaderId { get; }
        public double Strength { get; }
        public double DragReduction { get; }
        public double DragAreaMultiplier => 1 - DragReduction;
        public double GapMeters { get; }
        public double LateralWakeOffsetMeters { get; }
        internal DraftOutput(AthleteId leader, double strength, double reduction, double gap, double wakeOffset)
        { LeaderId = leader; Strength = strength; DragReduction = reduction; GapMeters = gap; LateralWakeOffsetMeters = wakeOffset; }
    }
    public sealed class DraftingSystem
    {
        private readonly DraftDefinition definition;
        public DraftingSystem(DraftDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public DraftOutput Evaluate(SkiPlacement follower, ReadOnlySpan<SkiPlacement> participants, TrackSample track,
            double headwindMetersPerSecond = 0, double crosswindMetersPerSecond = 0)
        {
            if (!follower.IsValid) throw new ArgumentException("Invalid follower.", nameof(follower));
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            PhysiologyNumbers.RequireRange(headwindMetersPerSecond, -definition.MaximumWind, definition.MaximumWind, nameof(headwindMetersPerSecond));
            PhysiologyNumbers.RequireRange(crosswindMetersPerSecond, -definition.MaximumWind, definition.MaximumWind, nameof(crosswindMetersPerSecond));
            var best = default(DraftOutput);
            double head = headwindMetersPerSecond * track.WindExposure;
            double cross = crosswindMetersPerSecond * track.WindExposure;
            double windFactor = Math.Min(definition.MaximumWindFactor, Math.Max(definition.MinimumWindFactor,
                1 + definition.HeadwindSensitivity * head / definition.ReferenceSpeed));
            for (int i = 0; i < participants.Length; i++)
            {
                var leader = participants[i];
                if (!leader.IsValid) throw new ArgumentException("Invalid drafting participant.", nameof(participants));
                if (!track.DraftAllowed || leader.AthleteId.Equals(follower.AthleteId) ||
                    !string.Equals(leader.CorridorId, follower.CorridorId, StringComparison.Ordinal)) continue;
                long delta = leader.Motion.DistanceNanometers - follower.Motion.DistanceNanometers;
                if (delta <= 0) continue;
                double gap = delta / (double)SkiState.NanometersPerMeter;
                if (gap < definition.MinimumGap || gap >= definition.MaximumGap) continue;
                double width = definition.WakeHalfWidth + gap * definition.WakeExpansion;
                double wakeShift = cross * gap / Math.Max(leader.Motion.SpeedMetersPerSecond, definition.MinimumFlowSpeed) * definition.CrosswindDisplacementFactor;
                double lateralOffset = follower.LateralMeters - leader.LateralMeters - wakeShift;
                double alignment = Math.Max(0, 1 - Math.Abs(lateralOffset) / width);
                double distance = (definition.MaximumGap - gap) / (definition.MaximumGap - definition.MinimumGap);
                distance = distance * distance * (3 - 2 * distance);
                double speed = Math.Min(1, Math.Min(leader.Motion.SpeedMetersPerSecond, follower.Motion.SpeedMetersPerSecond) / definition.ReferenceSpeed);
                // No aerodynamic saving to refund while net relative air is pushing from behind.
                double strength = follower.Motion.SpeedMetersPerSecond + head <= 0 ? 0 : Math.Min(1, distance * alignment * speed * windFactor);
                if (strength <= 0) continue;
                if (strength > best.Strength || (strength == best.Strength && string.CompareOrdinal(leader.AthleteId.Value, best.LeaderId.Value) < 0))
                    best = new DraftOutput(leader.AthleteId, strength, strength * definition.MaximumDragReduction, gap, lateralOffset);
            }
            return best;
        }
    }
}
