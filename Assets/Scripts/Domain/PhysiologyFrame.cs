namespace Biathlon.Domain
{
    /// <summary>End-tick state; effort describes power delivered during the just-completed interval.</summary>
    public readonly struct PhysiologyFrame
    {
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public PhysiologyState State { get; }
        public EffortOutput Effort { get; }
        public HeartRateOutput HeartRate { get; }
        public double DriftBpm { get; }
        public double ReserveChangePerSecond { get; }
        public double FatigueChangePerSecond { get; }
        public PhysiologyFrame(SimulationTick tick, AthleteId athleteId, PhysiologyState state, EffortOutput effort,
            HeartRateOutput heartRate, double driftBpm, double reserveChangePerSecond, double fatigueChangePerSecond)
        {
            Tick = tick; AthleteId = athleteId; State = state; Effort = effort; HeartRate = heartRate;
            DriftBpm = driftBpm; ReserveChangePerSecond = reserveChangePerSecond; FatigueChangePerSecond = fatigueChangePerSecond;
        }
    }
}
