using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum DraftFeedbackStatus { NoLeader, Sheltered, ProhibitedZone }
    public readonly struct DraftFeedback
    {
        public DraftFeedbackStatus Status { get; }
        public AthleteId LeaderId { get; }
        public double Strength { get; }
        public double GapMeters { get; }
        public double OffsetFromWakeMeters { get; }
        public double DragReduction { get; }
        public double EstimatedMechanicalWattsSaved { get; }
        public DraftFeedback(DraftOutput draft, bool draftAllowed, SkiState motion, SkiForceOutput unshelteredForces)
        {
            if (unshelteredForces.AirDragNewtons < -PhysiologyNumbers.MaximumMagnitude ||
                unshelteredForces.AirDragNewtons > PhysiologyNumbers.MaximumMagnitude) throw new ArgumentOutOfRangeException(nameof(unshelteredForces));
            Status = !draftAllowed ? DraftFeedbackStatus.ProhibitedZone : draft.Strength > 0 ? DraftFeedbackStatus.Sheltered : DraftFeedbackStatus.NoLeader;
            bool active = Status == DraftFeedbackStatus.Sheltered;
            LeaderId = active ? draft.LeaderId : default; Strength = active ? draft.Strength : 0;
            GapMeters = active ? draft.GapMeters : 0; OffsetFromWakeMeters = active ? draft.LateralWakeOffsetMeters : 0;
            DragReduction = active ? draft.DragReduction : 0;
            EstimatedMechanicalWattsSaved = Math.Max(0, unshelteredForces.AirDragNewtons) * DragReduction * motion.SpeedMetersPerSecond;
        }
    }
}
