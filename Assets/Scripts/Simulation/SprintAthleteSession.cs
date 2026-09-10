using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SprintAthleteInput
    {
        public TimeTrialInput Ski { get; } public WeaponAimCommand Aim { get; }
        public bool Trigger { get; } public bool HoldBreath { get; }
        public WindVector Wind { get; } public ShootingPressureContext Pressure { get; }
        public SprintAthleteInput(TimeTrialInput ski, WeaponAimCommand aim, bool trigger = false, bool holdBreath = false,
            WindVector wind = default, ShootingPressureContext pressure = default)
        {
            if (!ski.Effort.AthleteId.Equals(aim.AthleteId) || ski.Effort.Tick.Value != aim.Tick.Value || !aim.AthleteId.IsValid)
                throw new ArgumentException("Ski/aim intent must share athlete and tick.");
            Ski = ski; Aim = aim; Trigger = trigger; HoldBreath = holdBreath; Wind = wind; Pressure = pressure;
        }
    }
    public readonly struct SprintAthleteFrame
    {
        public SimulationTick Tick { get; } public CompetitionFrame Competition { get; }
        public PhysiologyFrame Physiology { get; } public SkiState Motion { get; }
        public long RouteNanometers { get; } public ShootingSessionFrame Shooting { get; }
        public bool ShootingActive { get; } public int TotalPenaltyLoops { get; }
        internal SprintAthleteFrame(SimulationTick tick, CompetitionFrame competition, PhysiologyFrame physiology, SkiState motion,
            long route, ShootingSessionFrame shooting, bool active, int penalties)
        { Tick = tick; Competition = competition; Physiology = physiology; Motion = motion; RouteNanometers = route;
          Shooting = shooting; ShootingActive = active; TotalPenaltyLoops = penalties; }
    }
    public readonly struct SprintStepPreview
    {
        internal SprintAthleteSession Owner { get; }
        internal long Revision { get; }
        public SimulationTick Tick { get; }
        public PhysiologyFrame Physiology { get; }
        public RacePathMotionFrame Movement { get; }
        public bool MovementActive { get; }
        public bool ShootingActive { get; }
        internal SprintStepPreview(SprintAthleteSession owner, long revision, SimulationTick tick,
            PhysiologyFrame physiology, RacePathMotionFrame movement, bool moving, bool shooting)
        { Owner=owner; Revision=revision; Tick=tick; Physiology=physiology; Movement=movement; MovementActive=moving; ShootingActive=shooting; }
    }
    public sealed class SprintAthleteSession
    {
        private readonly SimulationConfig config;
        private readonly CompetitionProfile profile;
        private readonly TimeTrialAthlete skiAthlete;
        private readonly ShootingAthleteDefinition shootingAthlete;
        private readonly ShootingModelDefinitions shootingModel;
        private readonly PhysiologySession physiology;
        private readonly RacePathMotion path;
        private readonly RaceRoute route;
        private readonly StartApproachMap startApproach;
        public long InitialStartApproachNanometers { get; }
        public long RemainingStartApproachNanometers => Math.Max(0, InitialStartApproachNanometers - Motion.DistanceNanometers);
        public FinishTime StartApproachExitTime { get; private set; }
        public string StartApproachContextJson { get; }
        private readonly RangePenaltyTraversal[] ranges;
        private readonly ShootingSession[] shooting;
        private readonly ShootingLaneAssignment lane;
        private readonly ShootingWindSystem windValidation;
        private readonly TuckSystem tuck;
        private readonly FinalSprintSystem sprint;
        private readonly IRaceEventSink events;
        private readonly RaceMotionSegment[] segments;
        private readonly CompetitionReceipt[] receipts;
        private readonly long finishNanometers;
        private readonly double maximumHeadwind;
        private readonly SkiTechniqueDefinition techniqueDefinition;
        private bool priorTuck, priorSprint;
        private long previewRevision;
        private bool previewReady;
        private SprintAthleteInput preparedInput;
        private PhysiologyInput preparedWork;
        private TuckOutput preparedPosture;
        private FinalSprintOutput preparedSprint;
        private int preparedRangeIndex;
        private TrackSample preparedSample;
        public SimulationTick Tick => physiology.Clock.Tick;
        public SimulationTick ScheduledStart { get; }
        public AthleteId AthleteId => physiology.AthleteId;
        public PhysiologyState PhysiologyState => physiology.State;
        public PhysiologyDefinition PhysiologyDefinition => physiology.Definition;
        public CompetitionLifecycle Lifecycle { get; }
        public CheckpointTracker Checkpoints { get; }
        public SkiState Motion { get; private set; }
        public long RouteNanometers { get; private set; }
        public int TotalPenaltyLoops { get; private set; }
        public SprintAthleteFrame LastFrame { get; private set; }
        public int ActiveBout => Lifecycle.CompletedBouts < profile.BoutCount ? Lifecycle.CompletedBouts : -1;
        public ShootingSession ActiveShooting => ActiveBout >= 0 ? shooting[ActiveBout] : null;
        public bool HasFinished => Lifecycle.Phase == CompetitionPhase.Finished || Lifecycle.Phase == CompetitionPhase.Classified;
        public RangeMotionPoint? CurrentRangeMotion
        {
            get
            {
                for (int i = 0; i < ranges.Length; i++)
                    if (ranges[i].Phase != RangeTraversalPhase.Complete) return ranges[i].PreviewMotion();
                return null;
            }
        }
        private sealed class RangeEvents : IRaceEventSink
        {
            private readonly IRaceEventSink target;
            public RangeEvents(IRaceEventSink target) { this.target = target; }
            public void Record(RaceEvent e) { if (e.EventType != RaceEventType.PenaltyLoopCompleted) target.Record(e); }
        }
        public SprintAthleteSession(SimulationConfig config, CompetitionProfile profile, SkiModelDefinitions skiModel,
            ShootingModelDefinitions shootingModel, TimeTrialAthlete skiAthlete, ShootingAthleteDefinition shootingAthlete,
            TrackDefinition track, RaceRoute route, CheckpointLayout checkpoints, RaceSnowConditions conditions, string skiId,
            SimulationTick scheduledStart, ShootingLaneAssignment lane, IRaceEventSink events,
            StartApproachMap startApproach = null, long startBehindLineNanometers = 0)
        {
            if (config == null || profile == null || skiModel == null || shootingModel == null || skiAthlete == null || shootingAthlete == null ||
                track == null || route == null || checkpoints == null || events == null) throw new ArgumentNullException();
            if ((profile.Format != CompetitionFormat.Sprint && profile.Format != CompetitionFormat.Pursuit && profile.Format != CompetitionFormat.MassStart) || scheduledStart.Value <= 0 || skiAthlete.Id != shootingAthlete.Id ||
                route.CompetitionProfileId != profile.Id || checkpoints.RouteId != route.Id || checkpoints.RoutePlanJson != route.PlanSnapshotJson ||
                checkpoints.TrackJson != track.ToJson() || lane.RangeId != track.Range.Id || lane.Ordinal <= 0)
                throw new ArgumentException("Invalid supported race format, identities, start or lane.");
            if(startBehindLineNanometers<0 || (profile.Format==CompetitionFormat.MassStart && startApproach==null) ||
                (startApproach!=null && profile.Format!=CompetitionFormat.MassStart) ||
                (startBehindLineNanometers>0 && (startApproach==null || startBehindLineNanometers>startApproach.LengthNanometers)))
                throw new ArgumentException("Explicit Mass Start approach and bounded rear-row position required.");
            this.startApproach=startApproach;InitialStartApproachNanometers=startBehindLineNanometers;
            if(startApproach!=null)StartApproachContextJson=ShootingModelDefinitions.EncodeContext(new[]{startApproach.ContextJson,
                startBehindLineNanometers.ToString(System.Globalization.CultureInfo.InvariantCulture)});
            this.config = config; this.profile = profile; this.skiAthlete = skiAthlete; this.shootingAthlete = shootingAthlete;
            this.shootingModel = shootingModel; this.route = route; this.events = events; this.lane = lane; ScheduledStart = scheduledStart;
            maximumHeadwind = skiModel.Physics.MaximumHeadwindMetersPerSecond;
            techniqueDefinition = skiModel.Technique;
            var physical = skiModel.StatCurves.Apply(skiModel.Physiology, skiAthlete.PhysiologyStats);
            PhysiologyNumbers.RequireRange(physical.SustainablePowerWatts * 1.10 * physical.MaximumRelativeEffort,
                0, skiModel.Physics.MaximumPowerWatts, "athlete power exceeds ski model domain");
            physiology = new PhysiologySession(config, physical, new AthleteId(skiAthlete.Id), PhysiologyState.Rested(physical), events);
            Lifecycle = new CompetitionLifecycle(profile, AthleteId, config.TickFrequency, events);
            Checkpoints = new CheckpointTracker(checkpoints, AthleteId, config.TickFrequency, scheduledStart, events);
            path = new RacePathMotion(skiModel, skiAthlete, track, route, conditions, skiId, config.TickFrequency, startApproach);
            tuck = new TuckSystem(skiModel.Tuck); sprint = new FinalSprintSystem(skiModel.Sprint, route.Length);
            windValidation = new ShootingWindSystem(shootingModel.Wind);
            ranges = new RangePenaltyTraversal[profile.BoutCount]; shooting = new ShootingSession[profile.BoutCount];
            var rangeEvents = new RangeEvents(events);
            for (int i = 0; i < ranges.Length; i++) ranges[i] = new RangePenaltyTraversal(profile, track, route, i, AthleteId, config.TickFrequency, rangeEvents);
            // Validate the full shooting composition at construction, not only when an athlete reaches the first mat.
            for (int i = 0; i < ranges.Length; i++)
                _ = new ShootingSession(config, shootingModel, shootingAthlete, profile.GetBout(i).Stance, lane.RangeId, events, physiologyDefinition: physical);
            finishNanometers = checked(route.Length.Millimeters * SkiState.NanometersPerMillimeter);
            // Reject a synthetic topology that can reach two scheduled bouts within one tick; production-sized Sprint loops are well above this guard.
            for (int i = 1; i < ranges.Length; i++)
                if (ranges[i].RangeEntryNanometers - ranges[i - 1].RangeRejoinNanometers <=
                    skiModel.Physics.MaximumSpeedMetersPerSecond / config.TickFrequency * SkiState.NanometersPerMeter)
                    throw new ArgumentException("Shooting bouts must be separated by more than one maximum-speed tick.");
            int segmentCount = 0;
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i); segmentCount += loop.SegmentCount;
                for (int j = 0; j < loop.SegmentCount; j++)
                    PhysiologyNumbers.RequireRange(loop.GetSegment(j).TerrainMuscleFactor, 0, physical.MaximumTerrainMuscleFactor, nameof(track));
            }
            segments = new RaceMotionSegment[checked((route.SectionCount + profile.ShotsPerBout + 1) * (segmentCount + 8))];
            receipts = new CompetitionReceipt[profile.LapCount + profile.ShotsPerBout + 4];
        }
        public SprintAthleteFrame Step(SprintAthleteInput input) => CommitStep(PrepareStep(input));

        /// <summary>Event-free preparation into owned scratch. Every attempt replaces the prior handle.
        /// Shooting actions remain deferred until commit; call between field ticks on the simulation thread.</summary>
        public SprintStepPreview PrepareStep(SprintAthleteInput input)
            => PreparePackStep(input, 1, 1, 0);

        internal int MotionSegmentCapacity => segments.Length;
        internal SprintStepPreview PreparePackStep(SprintAthleteInput input, double dragMultiplier, double propulsionMultiplier, int extraEffort)
        {
            previewReady = false;
            ValidateCommand(input);
            PhysiologyNumbers.RequireRange(dragMultiplier, 0, 1, nameof(dragMultiplier));
            PhysiologyNumbers.RequireRange(propulsionMultiplier, 0, 1, nameof(propulsionMultiplier));
            if (extraEffort < 0 || extraEffort > 1000) throw new ArgumentOutOfRangeException(nameof(extraEffort));
            var tick = input.Ski.Effort.Tick;
            bool started = tick.Value > ScheduledStart.Value;
            int rangeIndex = -1;
            for (int i = 0; i < ranges.Length; i++) if (ranges[i].Phase != RangeTraversalPhase.Complete) { rangeIndex = i; break; }
            var range = rangeIndex >= 0 ? ranges[rangeIndex] : null;
            bool onMat = started && range != null && range.Phase == RangeTraversalPhase.Shooting;
            var sample = RemainingStartApproachNanometers>0 ? startApproach.Sample(RemainingStartApproachNanometers) : range != null && range.Phase == RangeTraversalPhase.PenaltyLoop ? range.SamplePenalty() :
                route.Sample(new TrackDistance(RouteNanometers / SkiState.NanometersPerMillimeter)).Track;
            var sprintOutput = sprint.Evaluate(started && !onMat && input.Ski.SprintRequested, input.Ski.Effort.RequestedEffort, new SkiState(RouteNanometers, Motion.SpeedMetersPerSecond));
            var posture = tuck.Evaluate(started && !onMat && input.Ski.TuckRequested, sprintOutput.EffectiveEffortControl, skiAthlete.Downhill, Motion, sample);
            var work = new PhysiologyInput(new SimulationInput(tick, AthleteId, started && !onMat ? Math.Min(1000, posture.EffectiveEffortControl + extraEffort) : 0), input.Ski.Form, sample.TerrainMuscleFactor);
            var physical = physiology.Preview(work);
            var movement = started && !onMat ? path.Preview(Motion, RouteNanometers, finishNanometers, range,
                physical.Effort.DeliveredPowerWatts * propulsionMultiplier, physiology.State.MuscularFatigue, input.Ski, segments, posture.DragAreaMultiplier * dragMultiplier, RemainingStartApproachNanometers) : default;
            long revision = checked(previewRevision + 1);
            preparedInput = input; preparedWork = work; preparedPosture = posture; preparedSprint = sprintOutput;
            preparedRangeIndex = rangeIndex; preparedSample = sample;
            previewRevision = revision; previewReady = true;
            return new SprintStepPreview(this, revision, tick, physical, movement, started && !onMat, onMat);
        }
        public RaceMotionSegment GetPreparedSegment(SprintStepPreview preview, int index)
        {
            ValidatePreview(preview);
            if (index < 0 || index >= preview.Movement.SegmentCount) throw new ArgumentOutOfRangeException(nameof(index));
            return segments[index];
        }
        /// <summary>Retain a verified forward prefix and stop. Caller must prove continuous safety.
        /// Full prepared physiology is preserved; this is not a collision solver.</summary>
        public SprintStepPreview ConstrainPreparedTravel(SprintStepPreview preview, long maximumTravelNanometers)
        {
            ValidatePreview(preview);
            if (!preview.MovementActive || maximumTravelNanometers < 0)
                throw new ArgumentException("Active movement and nonnegative travel cap required.");
            long available = preview.Movement.Motion.DistanceNanometers - Motion.DistanceNanometers;
            if (maximumTravelNanometers > available) throw new ArgumentException("Cannot extend movement.");
            if (maximumTravelNanometers == available) return preview;
            long revision = checked(previewRevision + 1);
            long remaining = maximumTravelNanometers, main = RouteNanometers, rangeTravel = 0;
            double consumed = 0; int count = 0;
            var range = preparedRangeIndex >= 0 ? ranges[preparedRangeIndex] : null;
            for (int i = 0; i < preview.Movement.SegmentCount && remaining > 0; i++)
            {
                var segment = segments[i]; long distance = segment.TravelAfter - segment.TravelBefore;
                long accepted = Math.Min(remaining, distance);
                double end = accepted == distance ? segment.EndSeconds :
                    segment.StartSeconds + (double)((decimal)accepted / distance) * (segment.EndSeconds - segment.StartSeconds);
                long nextMain = segment.OnPenaltyLoop || segment.OnStartApproach ? segment.RouteBefore : segment.RouteBefore + accepted;
                segments[count++] = new RaceMotionSegment(segment.RouteBefore, nextMain, segment.TravelBefore,
                    segment.TravelBefore + accepted, segment.StartSeconds, end, segment.OnPenaltyLoop, segment.OnStartApproach);
                if (range != null && (segment.OnPenaltyLoop || (segment.RouteBefore >= range.RangeEntryNanometers &&
                    segment.RouteBefore < range.RangeRejoinNanometers))) rangeTravel += accepted;
                main = nextMain; consumed = end; remaining -= accepted;
            }
            var movement = new RacePathMotionFrame(new SkiState(Motion.DistanceNanometers + maximumTravelNanometers, 0),
                main, rangeTravel, consumed, 1.0 / config.TickFrequency - consumed, count, false, false);
            previewRevision = revision;
            return new SprintStepPreview(this, revision, preview.Tick, preview.Physiology, movement, true, false);
        }
        private void ValidatePreview(SprintStepPreview preview)
        {
            if (!previewReady || !ReferenceEquals(preview.Owner, this) || preview.Revision != previewRevision ||
                preview.Tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Missing, foreign, replaced or consumed Sprint preview.");
        }
        internal void ValidatePreparedCommit(SprintStepPreview preview) { ValidatePreview(preview); ValidateCommand(preparedInput); }
        internal RangeMotionPoint? PreviewRangeMotion(SprintStepPreview preview)
        {
            ValidatePreview(preview);
            return preparedRangeIndex < 0 ? (RangeMotionPoint?)null : ranges[preparedRangeIndex].PreviewMotion(preview.Movement.RangeTravelNanometers);
        }
        public SprintAthleteFrame CommitStep(SprintStepPreview preview)
        {
            ValidatePreview(preview);
            ValidateCommand(preparedInput);
            previewReady = false;
            var input = preparedInput; var tick = preview.Tick;
            bool started = tick.Value > ScheduledStart.Value, onMat = preview.ShootingActive;
            int rangeIndex = preparedRangeIndex; var range = rangeIndex >= 0 ? ranges[rangeIndex] : null;
            var physical = preview.Physiology; var work = preparedWork; var posture = preparedPosture;
            var sprintOutput = preparedSprint; var sample = preparedSample; var movement = preview.Movement;
            int receiptCount = 0; ShootingSessionFrame shotFrame = default;
            ShotResolutionFrame? completedBout = null;
            if (tick.Value == ScheduledStart.Value) receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.Start);
            if (onMat)
            {
                var session = shooting[rangeIndex];
                var action = session.Phase == ShootingPhase.Outside ? ShootingAction.EnterRange : session.Phase == ShootingPhase.AwaitingLane ? ShootingAction.AssignLane :
                    input.Trigger ? ShootingAction.Trigger : ShootingAction.None;
                shotFrame = session.Step(new ShootingSessionInput(input.Aim, action, action == ShootingAction.AssignLane ? lane : default,
                    input.HoldBreath, physical.State.HeartRateBpm, physical.State.MuscularFatigue, physical.State.MuscularFatigue, input.Wind, 0, sample.WindExposure, input.Pressure));
                if (session.Phase == ShootingPhase.Complete)
                { completedBout = shotFrame.Shot; receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.CompleteBout, session.ClosedMask, session.ShotsFired); }
            }
            else if (started)
            {
                if (range != null && RouteNanometers <= range.RangeEntryNanometers && movement.RouteNanometers >= range.RangeEntryNanometers &&
                    Lifecycle.CompletedBouts == rangeIndex && Lifecycle.Phase == CompetitionPhase.Skiing)
                    receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.EnterRange);
                if (movement.AtFiringLine && shooting[rangeIndex] == null)
                {
                    // One initialization per bout, outside the movement hot loop; no gameplay state is reset.
                    var boutConfig = new SimulationConfig(1, config.Id, config.TickFrequency, unchecked(config.Seed + (ulong)rangeIndex));
                    shooting[rangeIndex] = new ShootingSession(boutConfig, shootingModel, shootingAthlete, profile.GetBout(rangeIndex).Stance,
                        lane.RangeId, events, tick, physiology.Definition);
                }
            }
            physiology.CommitPreview(work, physical);
            int previousLoops = range?.CompletedLoops ?? 0;
            for (int i = 0; i < ranges.Length; i++) ranges[i].Step(tick, i == rangeIndex && started && !onMat ? movement.RangeTravelNanometers : 0,
                i == rangeIndex ? completedBout : null);
            if (completedBout.HasValue) TotalPenaltyLoops += range.RequiredLoops;
            if (range != null)
                for (int i = previousLoops; i < range.CompletedLoops; i++) receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.CompletePenaltyLoop);
            if (started && !onMat)
            {
                if(RemainingStartApproachNanometers>0 && movement.Motion.DistanceNanometers>=InitialStartApproachNanometers)
                    for(int i=0;i<movement.SegmentCount;i++)
                        if(segments[i].OnStartApproach && segments[i].TravelAfter==InitialStartApproachNanometers)
                        {
                            StartApproachExitTime=FinishTime.InsideTick(tick,config.TickFrequency,segments[i].EndSeconds);
                            events.Record(new RaceEvent(tick,config.TickFrequency,AthleteId,RaceEventType.StartApproachCompleted,0));
                            break;
                        }
                RouteNanometers = movement.RouteNanometers;
                Motion = movement.AtFiringLine ? new SkiState(movement.Motion.DistanceNanometers, 0) : movement.Motion;
                for (int lap = Lifecycle.CompletedLaps; lap < profile.LapCount - 1; lap++)
                {
                    long boundary = checked((route.GetLapStart(lap).Millimeters + route.GetLapLength(lap).Millimeters) * SkiState.NanometersPerMillimeter);
                    if (RouteNanometers < boundary) break;
                    receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.ReachLapBoundary);
                }
                if (movement.AtRouteStop) receipts[receiptCount++] = new CompetitionReceipt(CompetitionAction.Finish,
                    finish: FinishTime.InsideTick(tick, config.TickFrequency, movement.ConsumedSeconds));
            }
            if (started) Checkpoints.ObserveMotion(tick, started && !onMat ? segments.AsSpan(0, movement.SegmentCount) : ReadOnlySpan<RaceMotionSegment>.Empty);
            var competition = Lifecycle.StepBatch(tick, receipts.AsSpan(0, receiptCount));
            Transition(posture.Active, ref priorTuck, RaceEventType.TuckStarted, RaceEventType.TuckEnded);
            Transition(sprintOutput.Active, ref priorSprint, RaceEventType.FinalSprintStarted, RaceEventType.FinalSprintEnded);
            LastFrame = new SprintAthleteFrame(tick, competition, physical, Motion, RouteNanometers, shotFrame, onMat, TotalPenaltyLoops); return LastFrame;
        }
        public void ValidateCommand(SprintAthleteInput input)
        {
            var tick = input.Ski.Effort.Tick;
            if (HasFinished || tick.Value != checked(Tick.Value + 1) || !input.Ski.Effort.AthleteId.Equals(AthleteId) ||
                input.Aim.Tick.Value != tick.Value || !input.Aim.AthleteId.Equals(AthleteId)) throw new ArgumentException("Invalid Sprint command/terminal state.");
            windValidation.Evaluate(input.Wind, 0, 1, shootingModel.Targets.DistanceMeters);
            PhysiologyNumbers.RequireRange(input.Ski.HeadwindMetersPerSecond, -maximumHeadwind, maximumHeadwind, nameof(input.Ski.HeadwindMetersPerSecond));
            if (input.Ski.TechniqueId != null)
            {
                bool found = false;
                for (int i = 0; i < techniqueDefinition.ProfileCount; i++) if (techniqueDefinition.GetProfile(i).Id == input.Ski.TechniqueId) { found = true; break; }
                if (!found) throw new ArgumentException("Unknown ski technique.");
            }
        }
        private void Transition(bool next, ref bool previous, RaceEventType start, RaceEventType end)
        { if (next != previous) events.Record(new RaceEvent(Tick, config.TickFrequency, AthleteId, next ? start : end, 0)); previous = next; }
    }
}


