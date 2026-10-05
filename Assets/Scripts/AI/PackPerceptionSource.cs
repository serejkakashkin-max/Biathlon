using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Local periodic placements from one completed authoritative pack tick.
    /// Self is row zero. Never use these local coordinates as competition distance.</summary>
    public sealed class PackPerceptionSource
    {
        private readonly PackSprintFieldSession field;
        public PackPerceptionSource(PackSprintFieldSession field)
        { this.field = field ?? throw new ArgumentNullException(nameof(field)); }

        public int CapturePlacements(int athleteIndex, TrackDistance radius, Span<SkiPlacement> destination,bool includeStartContinuation=false)
        {
            if (athleteIndex < 0 || athleteIndex >= field.Count || destination.Length < field.Count)
                throw new ArgumentException("Valid athlete and worst-case field buffer required.");
            var self = field.GetPhysicalPoint(athleteIndex);
            long limit = checked(radius.Millimeters * SkiState.NanometersPerMillimeter);
            if (self.Closed && limit > self.LoopLengthNanometers / 2)
                throw new ArgumentException("Local pack perception radius must not exceed half the physical loop.");
            for (int i = 0; i < field.Count; i++)
                if (!field.GetAthlete(i).HasFinished && field.GetAthlete(i).Tick.Value != field.Tick.Value)
                    throw new InvalidOperationException("Pack observations require a coherent completed field tick.");
            if (!field.IsOnCourse(athleteIndex)) return 0;
            bool continuation=includeStartContinuation&&field.TryGetStartContinuationObservation(athleteIndex,out _);
            long selfStation=self.LocalNanometers;
            if(continuation)field.TryGetStartContinuationObservation(athleteIndex,out selfStation);
            string corridor=continuation?field.StartApproach.LoopId:self.LoopId;
            long origin = self.LoopLengthNanometers;
            destination[0] = Placement(athleteIndex, corridor, origin);
            int count = 1;
            for (int i = 0; i < field.Count; i++) if (i != athleteIndex && field.IsOnCourse(i))
            {
                var other = field.GetPhysicalPoint(i);long otherStation=other.LocalNanometers;
                if(continuation){if(!field.TryGetStartContinuationObservation(i,out otherStation))continue;}
                else if (other.LoopId != self.LoopId) continue;
                long gap = otherStation - selfStation;
                if (self.Closed&&!continuation)
                {
                    if (gap > self.LoopLengthNanometers / 2) gap -= self.LoopLengthNanometers;
                    else if (gap < -self.LoopLengthNanometers / 2) gap += self.LoopLengthNanometers;
                }
                if (gap < -limit || gap > limit) continue;
                destination[count++] = Placement(i, corridor, checked(origin + gap));
            }
            return count;
        }
        private SkiPlacement Placement(int index, string loop, long local) => new SkiPlacement(
            field.GetAthlete(index).AthleteId, loop, new SkiState(local, field.GetAthlete(index).Motion.SpeedMetersPerSecond), field.GetLateral(index));
    }
}
