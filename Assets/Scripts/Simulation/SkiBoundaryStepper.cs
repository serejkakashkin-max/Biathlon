using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiBoundaryStep
    {
        public SkiForceOutput Forces { get; }
        public double ConsumedSeconds { get; }
        public double RemainingSeconds { get; }
        public bool ReachedBoundary { get; }
        internal SkiBoundaryStep(SkiForceOutput forces, double consumed, double remaining, bool reached)
        { Forces = forces; ConsumedSeconds = consumed; RemainingSeconds = remaining; ReachedBoundary = reached; }
    }
    /// <summary>Splits a fixed tick at an exact forward coordinate; caller samples the next surface before consuming remaining time.</summary>
    public sealed class SkiBoundaryStepper
    {
        private readonly SkiForceSystem forces;
        public SkiBoundaryStepper(SkiPhysicsDefinition definition, int frequency) { forces = new SkiForceSystem(definition, frequency); }
        public SkiBoundaryStep Step(SkiState before, double power, TrackSample sample, double remainingSeconds, long boundaryNanometers,
            double headwind = 0, double dragArea = 1, double braking = 0, double snowResistance = 1)
        {
            if (boundaryNanometers <= before.DistanceNanometers) throw new ArgumentException("A forward boundary is required.");
            // Local integration avoids coordinate overflow when the caller only needs motion up to the boundary.
            var local = new SkiState(0, before.SpeedMetersPerSecond);
            var full = forces.StepDuration(local, power, sample, remainingSeconds, headwind, dragArea, braking, snowResistance);
            long required = boundaryNanometers - before.DistanceNanometers;
            if (full.State.DistanceNanometers < required)
                return new SkiBoundaryStep(Translate(full, before.DistanceNanometers + full.State.DistanceNanometers), remainingSeconds, 0, false);
            // Quantized displacement is monotone even after stopping/speed capping. Fixed iterations avoid runtime-dependent convergence exits.
            double low = 0, high = remainingSeconds;
            for (int i = 0; i < 48; i++)
            {
                double middle = (low + high) * 0.5;
                var candidate = forces.StepDuration(local, power, sample, middle, headwind, dragArea, braking, snowResistance);
                if (candidate.State.DistanceNanometers >= required) high = middle; else low = middle;
            }
            var accepted = forces.StepDuration(local, power, sample, high, headwind, dragArea, braking, snowResistance);
            return new SkiBoundaryStep(Translate(accepted, boundaryNanometers), high, remainingSeconds - high, true);
        }
        private static SkiForceOutput Translate(SkiForceOutput f, long distance) => new SkiForceOutput(new SkiState(distance, f.State.SpeedMetersPerSecond),
            f.PropulsiveNewtons, f.GravityNewtons, f.SnowNewtons, f.AirDragNewtons, f.BrakingNewtons, f.AppliedAcceleration);
    }
}
