using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Exact registered-gap starts. Unsupported clock resolutions fail before emission;
    /// gaps are never rounded. Tied qualifiers are emitted together in canonical source order.</summary>
    public sealed class PursuitStartScheduler
    {
        private readonly ScheduledStart[] starts;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private int next;
        public SimulationTick Tick { get; private set; }
        public int Count => starts.Length;
        public int EmittedCount => next;
        public string QualificationJson { get; }
        public ScheduledStart Get(int index) => starts[index];

        public PursuitStartScheduler(PursuitQualificationSnapshot qualification, int frequency,
            SimulationTick firstStart, IRaceEventSink events)
        {
            if (qualification == null) throw new ArgumentNullException(nameof(qualification));
            if (frequency <= 0 || firstStart.Value <= 0) throw new ArgumentException("Positive start clock required.");
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.frequency = frequency;
            QualificationJson = qualification.ToJson();
            starts = new ScheduledStart[qualification.Count];
            for (int i = 0; i < starts.Length; i++)
            {
                var row = qualification.Get(i);
                // Decimal avoids overflowing an intermediate Int64 product for otherwise valid offsets.
                decimal ticks = (decimal)row.StartGapUnits * frequency / qualification.GapUnitsPerSecond;
                if (ticks != decimal.Truncate(ticks) || ticks > long.MaxValue - firstStart.Value)
                    throw new ArgumentException("Source gap must be exactly representable in the selected race clock.");
                starts[i] = new ScheduledStart(new AthleteId(row.AthleteId), row.StartOrdinal,
                    new SimulationTick(checked(firstStart.Value + (long)ticks)));
            }
        }

        public int Step(SimulationTick target, Span<ScheduledStart> due)
        {
            if (target.Value != checked(Tick.Value + 1)) throw new ArgumentException("Start scheduler ticks must be consecutive.");
            int count = 0;
            while (next + count < starts.Length && starts[next + count].Tick.Value == target.Value) count++;
            if (due.Length < count) throw new ArgumentException("Output capacity for all tied starts required before emission.");
            Tick = target;
            for (int i = 0; i < count; i++)
            {
                var start = starts[next++];
                due[i] = start;
                events.Record(new RaceEvent(target, frequency, start.AthleteId, RaceEventType.ScheduledStartDue, start.StartOrdinal));
            }
            return count;
        }
    }
}
