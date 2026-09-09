using System;
namespace Biathlon.Domain
{
    public readonly struct SimulationInput
    {
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public int RequestedEffort { get; }
        public SimulationInput(SimulationTick tick, AthleteId athleteId, int requestedEffort)
        {
            if (!athleteId.IsValid) throw new ArgumentException("Invalid athlete ID.", nameof(athleteId));
            AthleteState.ValidateEffort(requestedEffort);
            Tick = tick;
            AthleteId = athleteId;
            RequestedEffort = requestedEffort;
        }
    }
}
