namespace Biathlon.Domain
{
    /// <summary>Power telemetry; watts and dimensionless relative effort/efficiency.</summary>
    public readonly struct EffortOutput
    {
        public double SustainablePowerWatts { get; }
        public double RequestedPowerWatts { get; }
        public double DeliveredPowerWatts { get; }
        public double RelativeEffort { get; }
        public double DeliveredRelativeEffort { get; }
        public double PowerEfficiency { get; }
        public EffortOutput(double sustainablePowerWatts, double requestedPowerWatts, double deliveredPowerWatts,
            double relativeEffort, double deliveredRelativeEffort, double powerEfficiency)
        {
            SustainablePowerWatts = PhysiologyNumbers.RequireRange(sustainablePowerWatts, 0, PhysiologyNumbers.MaximumMagnitude, nameof(sustainablePowerWatts));
            RequestedPowerWatts = PhysiologyNumbers.RequireRange(requestedPowerWatts, 0, PhysiologyNumbers.MaximumMagnitude, nameof(requestedPowerWatts));
            DeliveredPowerWatts = PhysiologyNumbers.RequireRange(deliveredPowerWatts, 0, requestedPowerWatts, nameof(deliveredPowerWatts));
            RelativeEffort = PhysiologyNumbers.RequireRange(relativeEffort, 0, PhysiologyNumbers.MaximumMagnitude, nameof(relativeEffort));
            DeliveredRelativeEffort = PhysiologyNumbers.RequireRange(deliveredRelativeEffort, 0, relativeEffort, nameof(deliveredRelativeEffort));
            PowerEfficiency = PhysiologyNumbers.RequireRange(powerEfficiency, 0, 1, nameof(powerEfficiency));
        }
    }
}
