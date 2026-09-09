namespace Biathlon.Domain
{
    /// <summary>Value snapshot. HR in bpm, resources in [0,1], high-load exposure in seconds.</summary>
    public readonly struct PhysiologyState
    {
        public double HeartRateBpm { get; }
        public double AnaerobicReserve { get; }
        public double MuscularFatigue { get; }
        public double HighLoadSeconds { get; }
        public PhysiologyState(double heartRateBpm, double anaerobicReserve, double muscularFatigue, double highLoadSeconds)
        {
            HeartRateBpm = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(heartRateBpm, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(heartRateBpm)));
            AnaerobicReserve = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(anaerobicReserve, 0, 1, nameof(anaerobicReserve)));
            MuscularFatigue = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue)));
            HighLoadSeconds = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(highLoadSeconds, 0, PhysiologyNumbers.MaximumMagnitude, nameof(highLoadSeconds)));
        }
        public void Validate(PhysiologyDefinition definition)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            PhysiologyNumbers.RequireRange(HeartRateBpm, definition.RestingHrBpm, definition.HrSafetyCeilingBpm, nameof(HeartRateBpm));
            PhysiologyNumbers.RequireRange(AnaerobicReserve, 0, 1, nameof(AnaerobicReserve));
            PhysiologyNumbers.RequireRange(MuscularFatigue, 0, 1, nameof(MuscularFatigue));
            PhysiologyNumbers.RequireRange(HighLoadSeconds, 0, definition.DriftSaturationSeconds, nameof(HighLoadSeconds));
        }
        public static PhysiologyState Rested(PhysiologyDefinition definition)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            return new PhysiologyState(definition.RestingHrBpm, 1, 0, 0);
        }
    }
}
