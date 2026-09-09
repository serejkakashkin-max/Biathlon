namespace Biathlon.Domain
{
    /// <summary>Normalized resource and actual applied change per second (after saturation/quantization).</summary>
    public readonly struct ResourceOutput
    {
        public double Value { get; }
        public double ChangePerSecond { get; }
        public ResourceOutput(double value, double changePerSecond)
        {
            Value = PhysiologyNumbers.RequireRange(value, 0, 1, nameof(value));
            ChangePerSecond = PhysiologyNumbers.RequireRange(changePerSecond, -PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude, nameof(changePerSecond));
        }
    }
}
