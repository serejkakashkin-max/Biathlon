using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct AiRouteNeighbor
    {
        public AthleteId AthleteId { get; }
        public CompetitionPhase Phase { get; }
        // Positive is ahead on the base competition route, not a physical corridor gap.
        public long RouteGapNanometers { get; }
        public double SpeedMetersPerSecond { get; }
        internal AiRouteNeighbor(SprintAthleteSession athlete, long origin)
        { AthleteId = athlete.AthleteId; Phase = athlete.Lifecycle.Phase;
          RouteGapNanometers = athlete.RouteNanometers - origin; SpeedMetersPerSecond = athlete.Motion.SpeedMetersPerSecond; }
    }
    public readonly struct AiRouteLookahead
    {
        public TrackDistance Offset { get; }
        public RaceRouteSample Sample { get; }
        public AiRouteLookahead(TrackDistance offset, RaceRouteSample sample)
        {
            if (string.IsNullOrEmpty(sample.Track.SegmentId) || offset.Millimeters > sample.RouteDistance.Millimeters)
                throw new ArgumentException("Valid route sample and relative offset required.");
            Offset = offset; Sample = sample;
        }
    }
    public readonly struct AiSprintPerception
    {
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public CompetitionFormat Format { get; }
        public CheckpointClock TimingClock => Format == CompetitionFormat.Sprint ? CheckpointClock.Elapsed : CheckpointClock.Global;
        public CompetitionPhase Phase { get; }
        public PhysiologyState Physiology { get; }
        public double SpeedMetersPerSecond { get; }
        public long RouteNanometers { get; }
        public long RemainingBaseRouteNanometers { get; }
        public long? NextRangeEntryRouteDistanceNanometers { get; }
        public int RemainingPenaltyLoops { get; }
        public int CompletedLaps { get; }
        public int CompletedBouts { get; }
        public ShootingPhase? ShootingPhase { get; }
        public TrackSample CurrentTrack { get; }
        public int LatestCheckpointOrdinal { get; }
        public double? CheckpointGapToLeaderSeconds { get; }
        public double? ElapsedGapToCheckpointLeaderSeconds => TimingClock == CheckpointClock.Elapsed ? CheckpointGapToLeaderSeconds : null;
        public int? ObservedCheckpointPlace { get; }
        public int CheckpointCompetitorsObserved { get; }
        public int NeighborCount { get; }
        public int LookaheadCount { get; }
        // M5 has no physical corridor placement. Unavailable is not a measured zero wake.
        public bool PhysicalDraftAvailable => false;
        internal AiSprintPerception(SimulationTick tick, SprintAthleteSession self, long remaining,
            long? range, TrackSample track, int checkpoint, double? gap, int? place, int observed, int neighbors, int lookahead, CompetitionFormat format)
        {
            Tick = tick; Format = format; AthleteId = self.AthleteId; Phase = self.Lifecycle.Phase; Physiology = self.PhysiologyState;
            SpeedMetersPerSecond = self.Motion.SpeedMetersPerSecond; RouteNanometers = self.RouteNanometers;
            RemainingBaseRouteNanometers = remaining; NextRangeEntryRouteDistanceNanometers = range;
            RemainingPenaltyLoops = self.Lifecycle.PendingPenaltyLoops; CompletedLaps = self.Lifecycle.CompletedLaps;
            CompletedBouts = self.Lifecycle.CompletedBouts; ShootingPhase = self.ActiveShooting?.Phase;
            CurrentTrack = track; LatestCheckpointOrdinal = checkpoint; CheckpointGapToLeaderSeconds = gap;
            ObservedCheckpointPlace = place; CheckpointCompetitorsObserved = observed; NeighborCount = neighbors; LookaheadCount = lookahead;
        }
    }
    /// <summary>Capture between field ticks on its owning simulation thread. Buffers belong to the caller;
    /// retain their contents until that caller deliberately overwrites them. Capture never advances authority.</summary>
    public sealed class SprintPerceptionSource
    {
        private readonly ISprintFieldView field;
        private readonly TrackSampler penalty;
        private readonly long[] rangeEntries;
        public SprintPerceptionSource(ISprintFieldView field)
        {
            this.field = field ?? throw new ArgumentNullException(nameof(field));
            penalty = new TrackSampler(field.Track.GetLoop(field.Track.Penalty.LoopId));
            rangeEntries = new long[field.Profile.BoutCount];
            for (int bout = 0; bout < rangeEntries.Length; bout++)
            {
                int matches = 0;
                for (int i = 0; i < field.Route.SectionCount; i++)
                {
                    var section = field.Route.GetSection(i);
                    if (section.LapIndex + 1 != field.Profile.GetBout(bout).AfterLap || !section.HasRange) continue;
                    rangeEntries[bout] = checked(section.RouteDistanceOf(field.Track.Range.Entry.Distance).Millimeters * SkiState.NanometersPerMillimeter);
                    matches++;
                }
                if (matches != 1) throw new ArgumentException("Perception requires an unambiguous range occurrence.");
            }
        }
        public AiSprintPerception Capture(int athleteIndex, TrackDistance neighborRadius, ReadOnlySpan<TrackDistance> offsets,
            Span<AiRouteLookahead> lookahead, Span<AiRouteNeighbor> neighbors)
        {
            if (athleteIndex < 0 || athleteIndex >= field.Count || lookahead.Length < offsets.Length || neighbors.Length < field.Count - 1)
                throw new ArgumentException("Valid athlete and worst-case caller buffer capacity required.");
            for (int i = 1; i < offsets.Length; i++)
                if (offsets[i].Millimeters <= offsets[i - 1].Millimeters) throw new ArgumentException("Lookahead offsets must increase strictly.");
            var self = field.GetAthlete(athleteIndex);
            for (int i = 0; i < field.Count; i++)
                if (!field.GetAthlete(i).HasFinished && field.GetAthlete(i).Tick.Value != field.Tick.Value)
                    throw new InvalidOperationException("Perception requires a coherent completed field tick.");
            long originMm = self.RouteNanometers / SkiState.NanometersPerMillimeter;
            long remainingMm = field.Route.Length.Millimeters - originMm;
            long radius = checked(neighborRadius.Millimeters * SkiState.NanometersPerMillimeter);
            for (int i = 0; i < offsets.Length; i++)
            {
                long offset = Math.Min(offsets[i].Millimeters, remainingMm);
                var distance = new TrackDistance(originMm + offset);
                var sample = self.RangePath == null ? field.Route.Sample(distance) :
                    self.RangePath.SampleRaceRoute(checked(distance.Millimeters * SkiState.NanometersPerMillimeter));
                lookahead[i] = new AiRouteLookahead(new TrackDistance(offset), sample);
            }
            int count = 0;
            for (int i = 0; i < field.Count; i++)
            {
                var other = field.GetAthlete(i);
                if (i == athleteIndex || other.Lifecycle.Phase == CompetitionPhase.Prestart || other.HasFinished) continue;
                long delta = other.RouteNanometers - self.RouteNanometers;
                if (delta < -radius || delta > radius) continue;
                neighbors[count++] = new AiRouteNeighbor(other, self.RouteNanometers);
            }
            // Stable roster order is preserved; no ranking by distance is presented as Sprint classification.
            int checkpoint = self.Checkpoints.Count - 1, observed = 0; int? place = null; double? gap = null;
            if (checkpoint >= 0)
            {
                var own = ClockTime(self.Checkpoints.Get(checkpoint)); int ahead = 0;
                for (int i = 0; i < field.Count; i++)
                {
                    var other = field.GetAthlete(i);
                    if (other.Checkpoints.Count <= checkpoint) continue;
                    observed++; if (ClockTime(other.Checkpoints.Get(checkpoint)).CompareTo(own) < 0) ahead++;
                }
                place = ahead + 1; gap = field.Timing.GapSeconds(self.AthleteId, checkpoint);
            }
            long? range = self.ActiveBout < 0 ? (long?)null : Math.Max(0, rangeEntries[self.ActiveBout] - self.RouteNanometers);
            var track = self.RangePath == null ? field.Route.Sample(new TrackDistance(originMm)).Track :
                self.RangePath.SampleRoute(self.RouteNanometers);
            var motion = self.CurrentRangeMotion;
            if (motion.HasValue && motion.Value.Phase == RangeTraversalPhase.PenaltyLoop)
                track = penalty.Sample(new TrackDistance(motion.Value.PenaltyNanometers / SkiState.NanometersPerMillimeter));
            return new AiSprintPerception(field.Tick, self,
                field.Route.Length.Millimeters * SkiState.NanometersPerMillimeter - self.RouteNanometers,
                range, track, checkpoint, gap, place, observed, count, offsets.Length, field.Profile.Format);
        }
        private FinishTime ClockTime(CheckpointSplit split) => field.Profile.Format == CompetitionFormat.Sprint ? split.ElapsedTime : split.GlobalTime;
    }
}

