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
            => Evaluate(definition, requestedControl, state, form);

        internal static EffortOutput Evaluate(PhysiologyDefinition definition, int requestedControl,
            PhysiologyState state, double form)
        {
            AthleteState.ValidateEffort(requestedControl);
            state.Validate(definition);
            // Form's domain is a GDD state contract, not a hidden athlete/difficulty multiplier.
            PhysiologyNumbers.RequireRange(form, 0, 1.10, nameof(form));
            double efficiency = 1 - state.MuscularFatigue * (1 - definition.MinimumPowerEfficiency);
            double sustainable = definition.SustainablePowerWatts * form * efficiency;
            double effort = sustainable == 0 ? 0 : RequestedRelativeEffort(definition, requestedControl);
            double reservePower = definition.BurstProfile == null ? state.AnaerobicReserve :
                Math.Min(1, state.AnaerobicReserve / definition.BurstProfile.FullPowerReserveFraction);
            double peak = definition.AerobicThreshold + (definition.MaximumRelativeEffort - definition.AerobicThreshold) * reservePower;
            double deliveredEffort = Math.Min(effort, peak);
            return new EffortOutput(sustainable, sustainable * effort, sustainable * deliveredEffort,
                effort, deliveredEffort, efficiency);
        }

        private static double RequestedRelativeEffort(PhysiologyDefinition definition, int control)
        {
            var burst = definition.BurstProfile;
            if (burst == null) return control / (double)AthleteState.EffortUnits * definition.MaximumRelativeEffort;
            if (control <= burst.ActivationControl)
                return control / (double)AthleteState.EffortUnits * burst.NormalMaximumRelativeEffort;
            double normalAtBoundary = burst.ActivationControl / (double)AthleteState.EffortUnits * burst.NormalMaximumRelativeEffort;
            double fraction = (control - burst.ActivationControl) / (double)(AthleteState.EffortUnits - burst.ActivationControl);
            return normalAtBoundary + fraction * (definition.MaximumRelativeEffort - normalAtBoundary);
        }
    }
}
