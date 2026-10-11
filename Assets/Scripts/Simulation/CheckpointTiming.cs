using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct CheckpointOccurrence
    {
        public string CheckpointId { get; }
        public string LapId { get; }
        public int SectionIndex { get; }
        public long RouteNanometers { get; }
        internal CheckpointOccurrence(string id, RouteSection section, int index, TrackDistance local)
        { CheckpointId = id; LapId = section.LapId; SectionIndex = index;
          RouteNanometers = checked(section.RouteDistanceOf(local).Millimeters * SkiState.NanometersPerMillimeter); }
    }
    public sealed class CheckpointLayout
    {
        private readonly CheckpointOccurrence[] occurrences;
        public string RouteId { get; }
        internal string RoutePlanJson { get; }
        internal string TrackJson { get; }
        public int Count => occurrences.Length;
        public CheckpointOccurrence Get(int ordinal) => occurrences[ordinal];
        public CheckpointLayout(TrackDefinition track, RaceRoute route)
        {
            if (track == null || route == null) throw new ArgumentNullException();
            if (route.TrackSnapshotJson != track.ToJson()) throw new ArgumentException("Checkpoint track/route mismatch.");
            RouteId = route.Id; RoutePlanJson = route.PlanSnapshotJson; TrackJson = track.ToJson(); var list = new List<CheckpointOccurrence>();
            for (int section = 0; section < route.SectionCount; section++)
            {
                var part = route.GetSection(section);
                for (int checkpoint = 0; checkpoint < track.CheckpointCount; checkpoint++)
                {
                    var marker = track.GetCheckpoint(checkpoint);
                    if (marker.Anchor.LoopId == part.LoopId && part.Contains(marker.Anchor.Distance))
                    {
                        // A marker at a split belongs to the following same-loop slice,
                        // avoiding duplicate splits while preserving physical loop-end markers.
                        bool nextOwns=marker.Anchor.Distance.Millimeters==part.LoopEnd.Millimeters&&section+1<route.SectionCount&&
                            route.GetSection(section+1).LoopId==part.LoopId&&route.GetSection(section+1).LoopStart.Millimeters==marker.Anchor.Distance.Millimeters;
                        if(!nextOwns)list.Add(new CheckpointOccurrence(marker.Id, part, section, marker.Anchor.Distance));
                    }
                }
            }
            list.Sort((a, b) => {
                int order = a.RouteNanometers.CompareTo(b.RouteNanometers);
                if (order == 0) order = a.SectionIndex.CompareTo(b.SectionIndex);
                return order == 0 ? StringComparer.Ordinal.Compare(a.CheckpointId, b.CheckpointId) : order;
            });
            occurrences = list.ToArray();
        }
    }
    public readonly struct CheckpointSplit
    {
        internal CheckpointLayout Layout { get; }
        public int Ordinal { get; }
        public AthleteId Athlete { get; }
        public FinishTime GlobalTime { get; }
        public FinishTime ElapsedTime { get; }
        public bool IsValid => Layout != null && Athlete.IsValid && GlobalTime.TickFrequency > 0;
        internal CheckpointSplit(CheckpointLayout layout, int ordinal, AthleteId athlete, FinishTime global, SimulationTick start)
        { Layout = layout; Ordinal = ordinal; Athlete = athlete; GlobalTime = global;
          ElapsedTime = new FinishTime(global.WholeTicks - start.Value, global.FractionParts, global.TickFrequency); }
    }
    /// <summary>Main-route progress only; stationary shooting and penalty loops keep this coordinate unchanged.</summary>
    public sealed class CheckpointTracker
    {
        private readonly CheckpointLayout layout;
        private readonly CheckpointSplit[] splits;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private readonly SimulationTick start;
        private readonly IRaceEventSink events;
        public SimulationTick Tick { get; private set; }
        public long RouteNanometers { get; private set; }
        public int Count { get; private set; }
        public CheckpointSplit Get(int ordinal)
        { if (ordinal < 0 || ordinal >= Count) throw new ArgumentOutOfRangeException(nameof(ordinal)); return splits[ordinal]; }
        public CheckpointTracker(CheckpointLayout layout, AthleteId athlete, int frequency, SimulationTick start, IRaceEventSink events)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (!athlete.IsValid || frequency <= 0) throw new ArgumentException("Invalid checkpoint athlete/clock.");
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.athlete = athlete;
            this.frequency = frequency; this.start = start; Tick = start; splits = new CheckpointSplit[layout.Count];
        }
        public int Observe(SimulationTick tick, long beforeNanometers, long afterNanometers)
        {
            if (tick.Value != checked(Tick.Value + 1) || beforeNanometers != RouteNanometers || afterNanometers < beforeNanometers)
                throw new ArgumentException("Checkpoint progress must be consecutive and forward.");
            int previousCount = Count;
            while (Count < layout.Count && layout.Get(Count).RouteNanometers <= afterNanometers)
            {
                long boundary = layout.Get(Count).RouteNanometers;
                var time = boundary == 0 ? new FinishTime(start.Value, 0, frequency) :
                    FinishTime.AtCrossing(tick, frequency, beforeNanometers, afterNanometers, boundary);
                splits[Count] = new CheckpointSplit(layout, Count, athlete, time, start);
                events.Record(new RaceEvent(tick, frequency, athlete, RaceEventType.CheckpointCrossed, Count + 1));
                Count++;
            }
            Tick = tick; RouteNanometers = afterNanometers; return Count - previousCount;
        }
        public int ObserveMotion(SimulationTick tick, ReadOnlySpan<RaceMotionSegment> segments)
        {
            if (tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Checkpoint ticks must be consecutive.");
            long position = RouteNanometers; double time = 0;
            // Validate the entire trajectory before any state or replay-event mutation.
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                FinishTime.InsideTick(tick, frequency, segment.StartSeconds); FinishTime.InsideTick(tick, frequency, segment.EndSeconds);
                if (segment.RouteBefore != position || segment.RouteAfter < position || segment.StartSeconds < time || segment.EndSeconds <= segment.StartSeconds ||
                    segment.TravelAfter < segment.TravelBefore || ((segment.OnPenaltyLoop || segment.OnStartApproach) && segment.RouteAfter != position) || (segment.OnStartApproach && (segment.OnPenaltyLoop || position != 0)) ||
                    (i > 0 && segments[i - 1].TravelAfter != segment.TravelBefore)) throw new ArgumentException("Invalid continuous timed motion.");
                position = segment.RouteAfter; time = segment.EndSeconds;
            }
            int previous = Count;
            while (Count < layout.Count && layout.Get(Count).RouteNanometers == 0)
                Store(new FinishTime(start.Value, 0, frequency));
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                while (Count < layout.Count && layout.Get(Count).RouteNanometers <= segment.RouteAfter)
                {
                    var crossing = FinishTime.AtSubstepCrossing(tick, frequency, segment.RouteBefore, segment.RouteAfter,
                        layout.Get(Count).RouteNanometers, segment.StartSeconds, segment.EndSeconds);
                    Store(crossing);
                }
            }
            Tick = tick; RouteNanometers = position; return Count - previous;
            void Store(FinishTime crossing)
            {
                splits[Count] = new CheckpointSplit(layout, Count, athlete, crossing, start);
                events.Record(new RaceEvent(tick, frequency, athlete, RaceEventType.CheckpointCrossed, Count + 1)); Count++;
            }
        }
    }
    public enum CheckpointClock { Elapsed = 0, Global = 1 }
    /// <summary>Recomputes a checkpoint leader as interval starters arrive; gaps never freeze against an obsolete leader.</summary>
    public sealed class CheckpointTimingBoard
    {
        private readonly CheckpointLayout layout;
        private readonly Dictionary<AthleteId, int> index;
        private readonly CheckpointSplit[] values;
        private readonly CheckpointSplit[] leaders;
        private readonly int frequency;
        private readonly CheckpointClock clock;
        public CheckpointTimingBoard(CheckpointLayout layout, ReadOnlySpan<AthleteId> roster, int frequency, CheckpointClock clock)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (roster.Length == 0 || frequency <= 0 || (clock != CheckpointClock.Elapsed && clock != CheckpointClock.Global))
                throw new ArgumentException("Invalid timing board configuration.");
            this.frequency = frequency; this.clock = clock;
            index = new Dictionary<AthleteId, int>(roster.Length);
            for (int i = 0; i < roster.Length; i++)
            {
                if (!roster[i].IsValid || index.ContainsKey(roster[i])) throw new ArgumentException("Unique roster IDs required.");
                index.Add(roster[i], i);
            }
            values = new CheckpointSplit[checked(roster.Length * layout.Count)];
            leaders = new CheckpointSplit[layout.Count];
        }
        public void Record(CheckpointSplit split)
        {
            if (!split.IsValid || !ReferenceEquals(split.Layout, layout) || split.GlobalTime.TickFrequency != frequency ||
                !index.TryGetValue(split.Athlete, out int athleteIndex)) throw new ArgumentException("Incompatible split.");
            int slot = checked(athleteIndex * layout.Count + split.Ordinal);
            if (values[slot].IsValid) throw new InvalidOperationException("Checkpoint already recorded for athlete.");
            values[slot] = split;
            // Splits are immutable. Update once on arrival rather than scan
            // the full roster for every AI/HUD query of the same checkpoint.
            var best=leaders[split.Ordinal];
            int order=best.IsValid?Time(split).CompareTo(Time(best)):-1;
            if(order<0||order==0&&StringComparer.Ordinal.Compare(split.Athlete.Value,best.Athlete.Value)<0)
                leaders[split.Ordinal]=split;
        }
        public CheckpointSplit Get(AthleteId athlete, int ordinal)
        {
            ValidateOrdinal(ordinal);
            if (!index.TryGetValue(athlete, out int athleteIndex)) throw new ArgumentException("Unknown timing athlete.");
            var split = values[athleteIndex * layout.Count + ordinal];
            if (!split.IsValid) throw new InvalidOperationException("Athlete has no split at this checkpoint.");
            return split;
        }
        public CheckpointSplit Leader(int ordinal)
        {
            ValidateOrdinal(ordinal); var best=leaders[ordinal];
            if (!best.IsValid) throw new InvalidOperationException("No checkpoint leader yet.");
            return best;
        }
        public double GapSeconds(AthleteId athlete, int ordinal)
        {
            var time = Time(Get(athlete, ordinal)); var leader = Time(Leader(ordinal));
            decimal parts = ((decimal)time.WholeTicks - leader.WholeTicks) * FinishTime.PartsPerTick + time.FractionParts - leader.FractionParts;
            return (double)(parts / FinishTime.PartsPerTick / frequency);
        }
        private FinishTime Time(CheckpointSplit split) => clock == CheckpointClock.Elapsed ? split.ElapsedTime : split.GlobalTime;
        private void ValidateOrdinal(int ordinal) { if (ordinal < 0 || ordinal >= layout.Count) throw new ArgumentOutOfRangeException(nameof(ordinal)); }
    }
}
