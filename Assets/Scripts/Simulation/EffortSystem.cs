using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Shared physical capability mapping. Stateless, no random source or controller identity.</summary>
    public sealed class EffortSystem
    {
        private readonly PhysiologyDefinition definition;
        public EffortSystem(PhysiologyDefinition definition)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            PhysiologyNumbers.RequireRange(definition.SustainablePowerWatts * 1.10 * definition.MaximumRelativeEffort,
                PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, "maximum possible requested watts");
        }

        public EffortOutput Evaluate(int requestedControl, PhysiologyState state, double form)
        {
            AthleteState.ValidateEffort(requestedControl);
            state.Validate(definition);
            // Form's domain is a GDD state contract, not a hidden athlete/difficulty multiplier.
            PhysiologyNumbers.RequireRange(form, 0, 1.10, nameof(form));
            double efficiency = 1 - state.MuscularFatigue * (1 - definition.MinimumPowerEfficiency);
            double sustainable = definition.SustainablePowerWatts * form * efficiency;
            double effort = sustainable == 0 ? 0 : requestedControl / (double)AthleteState.EffortUnits * definition.MaximumRelativeEffort;
            double peak = definition.AerobicThreshold + (definition.MaximumRelativeEffort - definition.AerobicThreshold) * state.AnaerobicReserve;
            double deliveredEffort = Math.Min(effort, peak);
            return new EffortOutput(sustainable, sustainable * effort, sustainable * deliveredEffort,
                effort, deliveredEffort, efficiency);
        }
    }
}
