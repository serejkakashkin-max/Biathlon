using System;

namespace Biathlon.Domain
{
    /// <summary>Directed corridor with shared longitudinal origin; positive lateral offset is track-right.</summary>
    public readonly struct SkiPlacement
    {
        public AthleteId AthleteId { get; }
        public string CorridorId { get; }
        public SkiState Motion { get; }
        public double LateralMeters { get; }
        public SkiPlacement(AthleteId athleteId, string corridorId, SkiState motion, double lateralMeters)
        {
            if (!athleteId.IsValid) throw new ArgumentException("Invalid ski athlete.", nameof(athleteId));
            TrackValidation.Id(corridorId);
            PhysiologyNumbers.RequireRange(lateralMeters, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(lateralMeters));
            AthleteId = athleteId; CorridorId = corridorId; Motion = motion; LateralMeters = lateralMeters;
        }
        public bool IsValid => AthleteId.IsValid && !string.IsNullOrWhiteSpace(CorridorId);
    }
}
