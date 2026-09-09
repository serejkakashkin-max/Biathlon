using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct FinalSprintOutput
    {
        public bool Active { get; }
        public int EffectiveEffortControl { get; }
        public double RemainingMeters { get; }
        internal FinalSprintOutput(bool active, int effort, double remaining)
        { Active = active; EffectiveEffortControl = effort; RemainingMeters = remaining; }
    }
    public sealed class FinalSprintSystem
    {
        private readonly FinalSprintDefinition definition;
        private readonly long finishNanometers;
        public FinalSprintSystem(FinalSprintDefinition definition, TrackDistance finish)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (finish.Millimeters <= 0) throw new ArgumentOutOfRangeException(nameof(finish));
            finishNanometers = checked(finish.Millimeters * SkiState.NanometersPerMillimeter);
        }
        public FinalSprintOutput Evaluate(bool requested, int effortControl, SkiState current)
        {
            AthleteState.ValidateEffort(effortControl);
            double remaining = Math.Max(0, finishNanometers - current.DistanceNanometers) / (double)SkiState.NanometersPerMeter;
            bool active = requested && remaining > 0 && remaining <= definition.ActivationDistanceMeters;
            return new FinalSprintOutput(active, active ? Math.Max(effortControl, definition.EffortControl) : effortControl, remaining);
        }
    }
}
