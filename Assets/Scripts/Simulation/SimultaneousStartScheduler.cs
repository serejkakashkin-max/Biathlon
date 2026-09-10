using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>One common start gun for an immutable grid. This schedules the race clock;
    /// motion and start-area occupancy remain the launch coordinator's responsibility.</summary>
    public sealed class SimultaneousStartScheduler
    {
        private readonly MassStartGrid grid;
        private readonly IRaceEventSink events;
        private readonly int frequency;
        private readonly SimulationTick start;
        public SimulationTick Tick { get; private set; }
        public int EmittedCount { get; private set; }
        public int Count=>grid.Count;
        public string ContextJson { get; }
        public ScheduledStart Get(int index)
        { var slot=grid.Get(index);return new ScheduledStart(slot.AthleteId,slot.StartOrdinal,start); }
        public SimultaneousStartScheduler(MassStartGrid grid,int frequency,SimulationTick start,IRaceEventSink events)
        {
            this.grid=grid??throw new ArgumentNullException(nameof(grid));this.events=events??throw new ArgumentNullException(nameof(events));
            if(frequency<=0 || start.Value<=0)throw new ArgumentException("Positive common start clock required.");
            this.frequency=frequency;this.start=start;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{grid.ContextJson,
                frequency.ToString(System.Globalization.CultureInfo.InvariantCulture),start.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),"simultaneous_start_v1"});
        }
        public int Step(SimulationTick target,Span<ScheduledStart> due)
        {
            if(target.Value!=checked(Tick.Value+1))throw new ArgumentException("Consecutive start ticks required.");
            bool emit=target.Equals(start);
            if(emit && due.Length<Count)throw new ArgumentException("Capacity for the entire simultaneous field required.");
            Tick=target;
            if(!emit)return 0;
            for(int i=0;i<Count;i++)
            {
                var entry=Get(i);due[i]=entry;
                events.Record(new RaceEvent(target,frequency,entry.AthleteId,RaceEventType.ScheduledStartDue,entry.StartOrdinal));
            }
            EmittedCount=Count;return Count;
        }
    }
}
