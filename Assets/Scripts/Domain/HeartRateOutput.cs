namespace Biathlon.Domain
{
    public readonly struct HeartRateOutput
    {
        public double HeartRateBpm { get; }
        public double TargetBpm { get; }
        public double RelativeLoad { get; }
        public HeartRateOutput(double heartRateBpm, double targetBpm, double relativeLoad)
        {
            HeartRateBpm = PhysiologyNumbers.Quantize(heartRateBpm);
            TargetBpm = PhysiologyNumbers.RequireRange(targetBpm, 0, PhysiologyNumbers.MaximumMagnitude, nameof(targetBpm));
            RelativeLoad = PhysiologyNumbers.RequireRange(relativeLoad, 0, PhysiologyNumbers.MaximumMagnitude, nameof(relativeLoad));
        }
    }
}
