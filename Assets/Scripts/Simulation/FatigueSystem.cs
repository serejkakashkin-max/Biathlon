using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class FatigueSystem
    {
        private readonly PhysiologyDefinition definition;
        private readonly int frequency;
        public FatigueSystem(PhysiologyDefinition definition, int tickFrequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            frequency = tickFrequency;
            PhysiologyNumbers.RequireRange(Gain(definition.MaximumRelativeEffort, definition.MaximumTerrainMuscleFactor),
                0, PhysiologyNumbers.MaximumMagnitude, "maximum fatigue gain");
            PhysiologyNumbers.RequireRange(definition.FatigueRecoveryPerSecond * definition.RecoveryModifier,
                0, PhysiologyNumbers.MaximumMagnitude, "maximum fatigue recovery");
        }
        private double Gain(double effort, double terrain) => definition.FatigueGainPerSecond == 0 ? 0 :
            Math.Pow(effort, definition.FatigueExponent) * terrain * definition.FatigueGainPerSecond;
        public ResourceOutput Step(double fatigue, double deliveredRelativeEffort, double terrainMuscleFactor)
        {
            PhysiologyNumbers.RequireRange(fatigue, 0, 1, nameof(fatigue));
            PhysiologyNumbers.RequireRange(deliveredRelativeEffort, 0, definition.MaximumRelativeEffort, nameof(deliveredRelativeEffort));
            PhysiologyNumbers.RequireRange(terrainMuscleFactor, 0, definition.MaximumTerrainMuscleFactor, nameof(terrainMuscleFactor));
            double recoveryFraction = Math.Max(0, (definition.AerobicThreshold - deliveredRelativeEffort) / definition.AerobicThreshold);
            double recovery = recoveryFraction * definition.FatigueRecoveryPerSecond * definition.RecoveryModifier *
                (1 - fatigue * definition.FatigueRecoveryPenalty);
            double next = PhysiologyNumbers.Quantize(Math.Min(1, Math.Max(0,
                fatigue + (Gain(deliveredRelativeEffort, terrainMuscleFactor) - recovery) / frequency)));
            return new ResourceOutput(next, (next - fatigue) * frequency);
        }
    }
}
