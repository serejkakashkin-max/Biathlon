using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum RangeTraversalPhase { Approach = 0, Shooting = 1, RangeExit = 2, PenaltyApproach = 3, PenaltyLoop = 4, Rejoin = 5, Complete = 6 }
    public readonly struct RangeMotionPoint
    {
        public RangeTraversalPhase Phase { get; }
        public long RouteNanometers { get; }
        public long PenaltyNanometers { get; }
        public long DistanceToBoundaryNanometers { get; }
        internal RangeMotionPoint(RangeTraversalPhase phase, long route, long penalty, long remaining)
        { Phase = phase; RouteNanometers = route; PenaltyNanometers = penalty; DistanceToBoundaryNanometers = remaining; }
    }
    public readonly struct RangeTraversalFrame
    {
        public SimulationTick Tick { get; }
        public RangeTraversalPhase Phase { get; }
        public long RouteNanometers { get; }
        public long PenaltyNanometers { get; }
        public long TravelledNanometers { get; }
        public long ConsumedNanometers { get; }
        public long UnconsumedNanometers { get; }
        public int RequiredLoops { get; }
        public int CompletedLoops { get; }
        public int RemainingLoops => RequiredLoops - CompletedLoops;
        internal RangeTraversalFrame(SimulationTick tick, RangeTraversalPhase phase, long route, long penalty,
            long travelled, long consumed, long unconsumed, int required, int completed)
        { Tick = tick; Phase = phase; RouteNanometers = route; PenaltyNanometers = penalty; TravelledNanometers = travelled;
          ConsumedNanometers = consumed; UnconsumedNanometers = unconsumed; RequiredLoops = required; CompletedLoops = completed; }
    }
    /// <summary>One bout's spatial detour. Displacement comes from authoritative ski physics, never player intent.</summary>
    public sealed class RangePenaltyTraversal
    {
        private const long NmPerMm = 1000000;
        private readonly long firing, exit, entry, rejoin, penaltyLength;
        private readonly TrackSampler penaltySampler;
        private readonly CompetitionProfile profile;
        private readonly TrackDefinition track;
        private readonly RaceRoute route;
        private readonly AthleteId athlete;
        private readonly int frequency, shots;
        private readonly IRaceEventSink events;
        public string RangeId { get; }
        public int BoutIndex { get; }
        public long RangeEntryNanometers { get; }
        public long RangeRejoinNanometers => rejoin;
        /// <summary>The exact optional physical path that supplied this bout's immutable mat stop.</summary>
        public PhysicalRangePathMap RangePath { get; }
        public long FiringRouteNanometers => firing;
        public SimulationTick Tick { get; private set; }
        public RangeTraversalPhase Phase { get; private set; }
        public long RouteNanometers { get; private set; }
        public long PenaltyNanometers { get; private set; }
        public long TravelledNanometers { get; private set; }
        public int RequiredLoops { get; private set; }
        public int CompletedLoops { get; private set; }
        public RangeTraversalFrame LastFrame { get; private set; }
        public TrackSample SamplePenalty() => penaltySampler.Sample(new TrackDistance(PenaltyNanometers / NmPerMm));
        /// <summary>Read-only path lookahead for resampling physics within a tick; grants no authoritative movement or penalty credit.</summary>
        public RangeMotionPoint PreviewMotion(long advancedNanometers = 0)
        {
            if (advancedNanometers < 0) throw new ArgumentOutOfRangeException(nameof(advancedNanometers));
            var phase = Phase; long route = RouteNanometers, penalty = PenaltyNanometers; int completed = CompletedLoops;
            while (phase != RangeTraversalPhase.Shooting && phase != RangeTraversalPhase.Complete)
            {
                bool inPenalty = phase == RangeTraversalPhase.PenaltyLoop;
                long end = phase == RangeTraversalPhase.Approach ? firing : phase == RangeTraversalPhase.RangeExit ? exit :
                    phase == RangeTraversalPhase.PenaltyApproach ? entry : inPenalty ? penaltyLength : rejoin;
                long distance = end - (inPenalty ? penalty : route);
                if (advancedNanometers < distance)
                    return new RangeMotionPoint(phase, inPenalty ? route : route + advancedNanometers,
                        inPenalty ? penalty + advancedNanometers : penalty, distance - advancedNanometers);
                advancedNanometers -= distance;
                if (inPenalty) penalty = 0; else route = end;
                switch (phase)
                {
                    case RangeTraversalPhase.Approach: phase = RangeTraversalPhase.Shooting; break;
                    case RangeTraversalPhase.RangeExit: phase = RangeTraversalPhase.PenaltyApproach; break;
                    case RangeTraversalPhase.PenaltyApproach: phase = RequiredLoops > 0 ? RangeTraversalPhase.PenaltyLoop : RangeTraversalPhase.Rejoin; break;
                    case RangeTraversalPhase.PenaltyLoop: if (++completed == RequiredLoops) phase = RangeTraversalPhase.Rejoin; break;
                    case RangeTraversalPhase.Rejoin: phase = RangeTraversalPhase.Complete; break;
                }
            }
            return new RangeMotionPoint(phase, route, penalty, 0);
        }

        public RangePenaltyTraversal(CompetitionProfile profile, TrackDefinition track, RaceRoute route, int boutIndex,
            AthleteId athlete, int frequency, IRaceEventSink events, SimulationTick initialTick = default,
            PhysicalRangePathMap rangePath = null)
        {
            if (profile == null || track == null || route == null) throw new ArgumentNullException();
            this.profile = profile; this.track = track; this.route = route;
            if (!athlete.IsValid || frequency <= 0 || boutIndex < 0 || boutIndex >= profile.BoutCount)
                throw new ArgumentException("Invalid traversal identity, clock or bout.");
            if (route.TrackId != track.Id || route.CompetitionProfileId != profile.Id || route.Length.Millimeters != profile.Distance.Millimeters || route.LapCount != profile.LapCount)
                throw new ArgumentException("Traversal route/profile mismatch.");
            var penalty = track.GetLoop(track.Penalty.LoopId);
            if (penalty.Length.Millimeters != profile.PenaltyLoopDistance.Millimeters || track.Penalty.Entry.LoopId != track.Range.Entry.LoopId ||
                track.Penalty.Rejoin.LoopId != track.Range.Entry.LoopId ||
                track.Penalty.Entry.Distance.Millimeters < track.Range.Exit.Distance.Millimeters ||
                track.Penalty.Rejoin.Distance.Millimeters < track.Penalty.Entry.Distance.Millimeters)
                throw new ArgumentException("Ordered range/penalty anchors and matching penalty length required.");
            int matches = 0; long start = 0; int lap = profile.GetBout(boutIndex).AfterLap - 1;
            for (int i = 0; i < route.SectionCount; i++)
            {
                var section = route.GetSection(i);
                if (section.LapIndex == lap && section.HasRange && section.LoopId == track.Range.Entry.LoopId)
                { matches++; start = section.Start.Millimeters-section.LoopStart.Millimeters; }
            }
            if (matches != 1) throw new ArgumentException("Bout requires exactly one unambiguous range section.");
            RangeEntryNanometers = checked((start + track.Range.Entry.Distance.Millimeters) * NmPerMm);
            if (rangePath != null && !rangePath.IsBoundTo(track, route))
                throw new ArgumentException("The personal mat path must bind this exact traversal track and route.", nameof(rangePath));
            firing = rangePath == null ? checked((start + track.Range.FiringLine.Distance.Millimeters) * NmPerMm) :
                rangePath.FiringRouteNanometers(lap);
            exit = checked((start + track.Range.Exit.Distance.Millimeters) * NmPerMm);
            entry = checked((start + track.Penalty.Entry.Distance.Millimeters) * NmPerMm);
            rejoin = checked((start + track.Penalty.Rejoin.Distance.Millimeters) * NmPerMm);
            if (rangePath != null && (firing <= RangeEntryNanometers || firing >= exit))
                throw new ArgumentException("The personal mat stop must lie strictly between this bout's range entry and exit.", nameof(rangePath));
            RangePath = rangePath;
            penaltyLength = checked(penalty.Length.Millimeters * NmPerMm);
            penaltySampler = new TrackSampler(penalty);
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.athlete = athlete;
            this.frequency = frequency; shots = profile.ShotsPerBout; RangeId = track.Range.Id; BoutIndex = boutIndex;
            Tick = initialTick; RouteNanometers = RangeEntryNanometers;
            LastFrame = new RangeTraversalFrame(Tick, Phase, RouteNanometers, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>Copies accepted state into an owner-selected preallocated scratch traversal only.
        /// The owning session must keep this destination outside its accepted traversal array until commit.</summary>
        internal void StageAcceptedStateFrom(RangePenaltyTraversal accepted, long actualRouteNanometers)
        {
            if (accepted == null) throw new ArgumentNullException(nameof(accepted));
            if (ReferenceEquals(this, accepted) || !ReferenceEquals(profile, accepted.profile) || !ReferenceEquals(track, accepted.track) ||
                !ReferenceEquals(route, accepted.route) || !athlete.Equals(accepted.athlete) || BoutIndex != accepted.BoutIndex ||
                frequency != accepted.frequency || shots != accepted.shots || !ReferenceEquals(events, accepted.events) ||
                RangeId != accepted.RangeId || RangeEntryNanometers != accepted.RangeEntryNanometers || exit != accepted.exit ||
                entry != accepted.entry || rejoin != accepted.rejoin || penaltyLength != accepted.penaltyLength)
                throw new ArgumentException("Scratch and accepted traversals must share exact owner, bout and immutable race context.");
            if (RangePath == null || accepted.RangePath == null || !RangePath.IsSameLayoutFamily(accepted.RangePath))
                throw new ArgumentException("Scratch and accepted mat paths must belong to the same exact layout family.");
            int lap = profile.GetBout(BoutIndex).AfterLap - 1;
            if (accepted.Phase != RangeTraversalPhase.Approach || actualRouteNanometers < 0 ||
                actualRouteNanometers >= RangePath.FirstForkRouteNanometers(lap))
                throw new ArgumentException("Mat path staging requires an accepted approach strictly before every physical fork.");
            if (actualRouteNanometers < accepted.RangeEntryNanometers ?
                accepted.TravelledNanometers != 0 || accepted.RouteNanometers != accepted.RangeEntryNanometers :
                accepted.RouteNanometers != actualRouteNanometers)
                throw new ArgumentException("Accepted traversal state must agree with the actual body coordinate or its untouched pre-entry origin.");
            var ordinary = RangePath.PhysicalMap.MapRoute(actualRouteNanometers);
            var sourcePoint = accepted.RangePath.MapRoute(actualRouteNanometers);
            var targetPoint = RangePath.MapRoute(actualRouteNanometers);
            if (!RangePath.PhysicalMap.SameLoop(ordinary, sourcePoint) || ordinary.LocalNanometers != sourcePoint.LocalNanometers ||
                !RangePath.PhysicalMap.SameLoop(ordinary, targetPoint) || ordinary.LocalNanometers != targetPoint.LocalNanometers)
                throw new ArgumentException("Mat path staging cannot reposition a body already on a private physical path.");
            Tick = accepted.Tick; Phase = accepted.Phase; RouteNanometers = accepted.RouteNanometers;
            PenaltyNanometers = accepted.PenaltyNanometers; TravelledNanometers = accepted.TravelledNanometers;
            RequiredLoops = accepted.RequiredLoops; CompletedLoops = accepted.CompletedLoops; LastFrame = accepted.LastFrame;
        }

        public RangeTraversalFrame Step(SimulationTick tick, long displacementNanometers, ShotResolutionFrame? completedBout = null)
        {
            if (tick.Value != checked(Tick.Value + 1) || displacementNanometers < 0)
                throw new ArgumentException("Consecutive tick and nonnegative displacement required.");
            int required = 0;
            if (completedBout.HasValue)
            {
                var bout = completedBout.Value;
                if (Phase != RangeTraversalPhase.Shooting || bout.Lifecycle.Phase != ShootingPhase.Complete ||
                    bout.Lifecycle.Tick.Value != tick.Value || bout.Lifecycle.ShotsFired != shots || bout.ClosedMask < 0 || bout.ClosedMask >= (1 << TargetGeometryDefinition.TargetCount) ||
                    displacementNanometers != 0)
                    throw new ArgumentException("Current completed five-shot bout required at the stationary firing line.");
                required = shots;
                for (int i = 0; i < TargetGeometryDefinition.TargetCount; i++) if ((bout.ClosedMask & (1 << i)) != 0) required--;
            }
            Tick = tick;
            if (completedBout.HasValue) { RequiredLoops = required; Change(RangeTraversalPhase.RangeExit); }
            long remaining = displacementNanometers;
            // At most five penalty circuits plus the fixed anchor transitions; no per-tick allocations.
            while (Phase != RangeTraversalPhase.Shooting && Phase != RangeTraversalPhase.Complete)
            {
                bool inPenalty = Phase == RangeTraversalPhase.PenaltyLoop;
                long end = Phase == RangeTraversalPhase.Approach ? firing : Phase == RangeTraversalPhase.RangeExit ? exit :
                    Phase == RangeTraversalPhase.PenaltyApproach ? entry : inPenalty ? penaltyLength : rejoin;
                long current = inPenalty ? PenaltyNanometers : RouteNanometers;
                long accepted = Math.Min(remaining, end - current);
                if (inPenalty) PenaltyNanometers += accepted; else RouteNanometers += accepted;
                TravelledNanometers += accepted; remaining -= accepted;
                if (current + accepted < end) break;
                switch (Phase)
                {
                    case RangeTraversalPhase.Approach: Change(RangeTraversalPhase.Shooting); break;
                    case RangeTraversalPhase.RangeExit: Change(RangeTraversalPhase.PenaltyApproach); break;
                    case RangeTraversalPhase.PenaltyApproach:
                        Change(RequiredLoops > 0 ? RangeTraversalPhase.PenaltyLoop : RangeTraversalPhase.Rejoin); break;
                    case RangeTraversalPhase.PenaltyLoop:
                        CompletedLoops++; PenaltyNanometers = 0;
                        events.Record(new RaceEvent(tick, frequency, athlete, RaceEventType.PenaltyLoopCompleted, RequiredLoops - CompletedLoops));
                        if (CompletedLoops == RequiredLoops) Change(RangeTraversalPhase.Rejoin); break;
                    case RangeTraversalPhase.Rejoin: Change(RangeTraversalPhase.Complete); break;
                }
                if (remaining == 0 && Phase == RangeTraversalPhase.PenaltyLoop) break;
            }
            LastFrame = new RangeTraversalFrame(tick, Phase, RouteNanometers, PenaltyNanometers, TravelledNanometers,
                displacementNanometers - remaining, remaining, RequiredLoops, CompletedLoops);
            return LastFrame;
        }
        private void Change(RangeTraversalPhase phase)
        { Phase = phase; events.Record(new RaceEvent(Tick, frequency, athlete, RaceEventType.RangeTraversalPhaseChanged, (int)phase)); }
    }
}
