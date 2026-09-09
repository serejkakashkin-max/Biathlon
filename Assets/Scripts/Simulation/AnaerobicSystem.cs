using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class AnaerobicSystem
    {
        private readonly PhysiologyDefinition definition;
        private readonly int frequency;
        public AnaerobicSystem(PhysiologyDefinition definition, int tickFrequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            frequency = tickFrequency;
            PhysiologyNumbers.RequireRange(Drain(definition.MaximumRelativeEffort), 0, PhysiologyNumbers.MaximumMagnitude, "maximum drain rate");
            PhysiologyNumbers.RequireRange(definition.AerobicThreshold * definition.RecoveryModifier * definition.AnaerobicRecoveryWatts / definition.AnaerobicCapacityJoules,
                0, PhysiologyNumbers.MaximumMagnitude, "maximum recovery rate");
        }
        private double Drain(double effort) => definition.AnaerobicDrainWatts == 0 ? 0 :
            Math.Pow(effort - definition.AerobicThreshold, definition.AnaerobicDrainExponent) * definition.AnaerobicDrainWatts / definition.AnaerobicCapacityJoules;
        public ResourceOutput Step(double reserve, double deliveredRelativeEffort, double muscularFatigue)
        {
            PhysiologyNumbers.RequireRange(reserve, 0, 1, nameof(reserve));
            PhysiologyNumbers.RequireRange(deliveredRelativeEffort, 0, definition.MaximumRelativeEffort, nameof(deliveredRelativeEffort));
            PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue));
            double rate = deliveredRelativeEffort > definition.AerobicThreshold ? -Drain(deliveredRelativeEffort) :
                (definition.AerobicThreshold - deliveredRelativeEffort) * definition.RecoveryModifier *
                (1 - muscularFatigue * definition.FatigueRecoveryPenalty) * definition.AnaerobicRecoveryWatts / definition.AnaerobicCapacityJoules;
            double next = PhysiologyNumbers.Quantize(Math.Min(1, Math.Max(0, reserve + rate / frequency)));
            return new ResourceOutput(next, (next - reserve) * frequency);
        }
    }
}
