using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Read-only estimate from actual individual W' capacity; never advances resources.</summary>
    public readonly struct ShortBurstTelemetry
    {
        public bool Enabled { get; }
        public bool Active { get; }
        public double ReserveFraction { get; }
        public double FullPowerSecondsRemaining { get; }
        public double PowerFraction { get; }

        private ShortBurstTelemetry(bool active, double reserve, double seconds, double fraction)
        { Enabled = true; Active = active; ReserveFraction = reserve; FullPowerSecondsRemaining = seconds; PowerFraction = fraction; }

        public static ShortBurstTelemetry Evaluate(PhysiologyDefinition definition, PhysiologyState state,
            int requestedControl, double form = 1)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.BurstProfile == null) return default;
            var output = EffortSystem.Evaluate(definition, requestedControl, state, form);
            return Evaluate(definition, state, output, requestedControl > definition.BurstProfile.ActivationControl);
        }

        /// <summary>Use accepted power to preserve actual career form and terrain/posture gating.</summary>
        public static ShortBurstTelemetry Evaluate(PhysiologyDefinition definition, PhysiologyState state,
            EffortOutput acceptedPower, bool requested)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var burst = definition.BurstProfile;
            if (burst == null) return default;
            state.Validate(definition);
            double fullDrain = Math.Pow(definition.MaximumRelativeEffort - definition.AerobicThreshold,
                definition.AnaerobicDrainExponent) * definition.AnaerobicDrainWatts / definition.AnaerobicCapacityJoules;
            double usefulReserve = Math.Max(0, state.AnaerobicReserve - burst.FullPowerReserveFraction);
            double seconds = fullDrain > 0 ? usefulReserve / fullDrain : 0;
            double powerFraction = acceptedPower.SustainablePowerWatts > 0 ?
                Math.Min(1, Math.Max(0, (acceptedPower.DeliveredRelativeEffort - definition.AerobicThreshold) /
                    (definition.MaximumRelativeEffort - definition.AerobicThreshold))) : 0;
            return new ShortBurstTelemetry(requested && powerFraction > 0, state.AnaerobicReserve, seconds, powerFraction);
        }
    }
}
