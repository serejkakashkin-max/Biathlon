using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ScheduledStart
    {
        public AthleteId AthleteId { get; } public int StartOrdinal { get; } public SimulationTick Tick { get; }
        internal ScheduledStart(AthleteId athlete, int ordinal, SimulationTick tick) { AthleteId = athlete; StartOrdinal = ordinal; Tick = tick; }
    }
    public sealed class IntervalStartScheduler
    {
        private readonly ScheduledStart[] starts;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private int next;
        public SimulationTick Tick { get; private set; }
        public int Count => starts.Length;
        public int EmittedCount => next;
        public ScheduledStart Get(int index) => starts[index];
        public IntervalStartScheduler(CompetitionProfile profile, ReadOnlySpan<AthleteId> roster, int frequency, SimulationTick firstStart, IRaceEventSink events)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.StartType != CompetitionStartType.Interval) throw new ArgumentException("Interval scheduler requires an interval profile.");
            if (frequency <= 0 || firstStart.Value <= 0 || roster.Length == 0 || roster.Length > profile.FieldCapacity) throw new ArgumentException("Invalid start clock/roster size.");
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.frequency = frequency;
            decimal scaled = (decimal)profile.StartIntervalSeconds * frequency;
            if (scaled != decimal.Truncate(scaled) || scaled < 1 || scaled > long.MaxValue) throw new ArgumentException("Start interval must be exactly representable in whole ticks.");
            long intervalTicks = (long)scaled;
            _ = checked(firstStart.Value + intervalTicks * (roster.Length - 1L));
            starts = new ScheduledStart[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                if (!roster[i].IsValid) throw new ArgumentException("Invalid athlete in start roster.");
                for (int j = 0; j < i; j++) if (roster[i].Equals(roster[j])) throw new ArgumentException("Duplicate athlete in start roster.");
                starts[i] = new ScheduledStart(roster[i], i + 1, new SimulationTick(firstStart.Value + intervalTicks * i));
            }
        }
        public int Step(SimulationTick target, Span<ScheduledStart> due)
        {
            if (target.Value != checked(Tick.Value + 1)) throw new ArgumentException("Start scheduler ticks must be consecutive.");
            bool emit = next < starts.Length && starts[next].Tick.Value == target.Value;
            if (emit && due.Length == 0) throw new ArgumentException("Output capacity required before emitting start.");
            Tick = target;
            if (!emit) return 0;
            var start = starts[next++]; due[0] = start;
            events.Record(new RaceEvent(target, frequency, start.AthleteId, RaceEventType.ScheduledStartDue, start.StartOrdinal));
            return 1;
        }
    }
}
