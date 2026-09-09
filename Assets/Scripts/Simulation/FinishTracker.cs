using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class FinishTracker
    {
        private readonly long finishNanometers;
        private readonly int frequency;
        private readonly AthleteId athleteId;
        private readonly IRaceEventSink events;
        private long lastDistance;
        public SimulationTick Tick { get; private set; }
        public bool HasFinished { get; private set; }
        public FinishTime Time { get; private set; }
        public FinishTracker(TrackDistance finish, AthleteId athleteId, int tickFrequency, IRaceEventSink events, long initialDistanceNanometers = 0)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            finishNanometers = checked(finish.Millimeters * SkiState.NanometersPerMillimeter);
            if (finishNanometers <= 0 || !athleteId.IsValid || tickFrequency <= 0 || initialDistanceNanometers < 0 || initialDistanceNanometers >= finishNanometers)
                throw new ArgumentException("Invalid finish tracker initial configuration.");
            this.athleteId = athleteId; frequency = tickFrequency; lastDistance = initialDistanceNanometers;
        }
        public bool Observe(SimulationTick tick, SkiState before, SkiState after)
        {
            if (!tick.Equals(Tick.Next()) || before.DistanceNanometers != lastDistance || after.DistanceNanometers < before.DistanceNanometers)
                throw new ArgumentException("Finish observation must be consecutive and forward.");
            bool crossed = !HasFinished && after.DistanceNanometers >= finishNanometers;
            var time = Time;
            if (crossed)
            {
                time = FinishTime.AtCrossing(tick, frequency, before.DistanceNanometers, after.DistanceNanometers, finishNanometers);
            }
            Tick = tick; lastDistance = after.DistanceNanometers;
            if (crossed)
            {
                Time = time; HasFinished = true;
                events.Record(new RaceEvent(tick, frequency, athleteId, RaceEventType.RaceFinished, 0));
            }
            return crossed;
        }
    }
}
