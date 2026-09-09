using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Reusable whole-tick admission workspace. All substeps and events remain private
    /// until Commit. This transaction covers admission only, not physiology or the entire race.</summary>
    public sealed class JunctionAdmissionTickPlan
    {
        private sealed class EventBuffer : IRaceEventSink
        {
            internal readonly RaceEvent[] Values;
            internal int Count;
            internal bool Overflow;
            internal EventBuffer(int capacity) { Values = new RaceEvent[capacity]; }
            public void Record(RaceEvent value)
            {
                // The sink contract forbids exceptions during commit. Report overflow afterward.
                if (Count == Values.Length) { Overflow = true; return; }
                Values[Count++] = value;
            }
        }
        private readonly JunctionAdmissionSession authority, working;
        private readonly EventBuffer buffer;
        private long sourceRevision;
        private SimulationTick tick;
        private bool active;
        public bool IsComplete => active && authority.AuthorityRevision == sourceRevision && working.Tick.Equals(tick) && !working.TickInProgress;
        public long PendingFraction { get { RequireActive(); return working.PendingFraction; } }
        public JunctionAdmissionTickPlan(JunctionAdmissionSession authority, int eventCapacity)
        {
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            if (eventCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(eventCapacity));
            buffer = new EventBuffer(eventCapacity); working = authority.CreatePlanningCopy(buffer);
        }
        public void Begin(SimulationTick nextTick)
        {
            active = false;
            if (authority.TickInProgress || !nextTick.Equals(authority.Tick.Next()))
                throw new ArgumentException("Whole-tick planning must begin at a completed tick.");
            working.CopyAuthorityFrom(authority);
            sourceRevision = authority.AuthorityRevision; tick = nextTick;
            buffer.Count = 0; buffer.Overflow = false; active = true;
        }
        public PhysicalOccupancySweep GetState(int index) { RequireActive(); return working.GetState(index); }
        public AthleteId GetOwner(int junctionIndex) { RequireActive(); return working.GetOwner(junctionIndex); }
        public bool WasAdmitted(int index) { RequireActive(); return working.WasAdmitted(index); }
        public void AddSubstep(long endFraction, bool completesTick, ReadOnlySpan<PhysicalOccupancySweep> proposals,
            ReadOnlySpan<string> connectionIds)
        {
            RequireActive();
            try
            {
                if (IsComplete) throw new InvalidOperationException("Planned tick is already sealed.");
                working.Commit(working.PrepareSubstep(tick, working.PendingFraction, endFraction, completesTick, proposals, connectionIds));
                if (buffer.Overflow) throw new InvalidOperationException("Admission event capacity exceeded; restart with sufficient capacity.");
            }
            catch { active = false; throw; }
        }
        public void Validate()
        {
            RequireActive();
            if (!IsComplete) throw new InvalidOperationException("A complete planned tick is required.");
        }
        public void Commit()
        {
            Validate();
            authority.CopyAuthorityFrom(working);
            active = false;
            authority.PublishEvents(new ReadOnlySpan<RaceEvent>(buffer.Values, 0, buffer.Count));
        }
        public void Cancel() { active = false; }
        private void RequireActive()
        {
            if (!active) throw new InvalidOperationException("No active admission plan.");
            if (authority.AuthorityRevision != sourceRevision)
            { active = false; throw new InvalidOperationException("Authoritative admission changed during planning."); }
        }
    }
}
