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
        internal ManualWorldRosterPreview World { get; }
        public bool ManualWorldRequired { get; }
        internal SprintStepPreview(SprintAthleteSession owner, long revision, SimulationTick tick,
            PhysiologyFrame physiology, RacePathMotionFrame movement, bool moving, bool shooting,
            ManualWorldRosterPreview world=default,bool manualWorldRequired=false)
        { Owner=owner; Revision=revision; Tick=tick; Physiology=physiology; Movement=movement;
          MovementActive=moving; ShootingActive=shooting; World=world;
          ManualWorldRequired=manualWorldRequired; }
    }
    public readonly struct SprintRangePathChoice
    {
        internal SprintAthleteSession Owner { get; }
        internal long SourceRevision { get; }
        internal SimulationTick SourceTick { get; }
        public int BoutIndex { get; }
        public int LaneOrdinal { get; }
        internal SprintRangePathChoice(SprintAthleteSession owner,long revision,SimulationTick tick,int bout,int ordinal)
        {Owner=owner;SourceRevision=revision;SourceTick=tick;BoutIndex=bout;LaneOrdinal=ordinal;}
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
        private PhysicalRangePathMap physicalRangePath;
        private readonly PhysicalRangePathMap[] rangePathChoices;
        private readonly RangePenaltyTraversal[][] rangePathVariants;
        private readonly bool[] pathChosenForBout;
        public PhysicalRangePathMap RangePath => physicalRangePath;
        public string PhysicalRangePathContextJson { get; }
        public int RangePathChoiceCount => rangePathChoices?.Length??0;
        public PhysicalRangePathMap GetRangePathChoice(int index)=>rangePathChoices==null ? throw new InvalidOperationException("No configured path choices.") : rangePathChoices[index];
        public long InitialStartApproachNanometers { get; }
        public long RemainingStartApproachNanometers => Math.Max(0, InitialStartApproachNanometers - Motion.DistanceNanometers);
        public FinishTime StartApproachExitTime { get; private set; }
        public string StartApproachContextJson { get; }
        private readonly RangePenaltyTraversal[] ranges;
        private readonly ShootingSession[] shooting;
        private ShootingLaneAssignment lane;
        public ShootingLaneAssignment AssignedShootingLane => lane;
        internal void SetRangeLane(ShootingLaneAssignment assignment)
        {
            if(assignment.RangeId!=lane.RangeId || assignment.Ordinal<=0 || ActiveShooting!=null)throw new InvalidOperationException("Assign physical lane before reaching the mat.");
            if(physicalRangePath!=null&&(assignment.LaneId!=physicalRangePath.LaneId||assignment.Ordinal!=physicalRangePath.LaneOrdinal))
                throw new InvalidOperationException("Changing a physical path requires a prepared lane/path transaction.");
            lane=assignment;
        }
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
        private PhysicalRangePathMap preparedPath;
        private RangePenaltyTraversal preparedRange;
        private ShootingLaneAssignment preparedLane;
        private bool preparedChoosesPath;
        public SimulationTick Tick => physiology.Clock.Tick;
        public SimulationTick ScheduledStart { get; }
        public AthleteId AthleteId => physiology.AthleteId;
        public int DownhillSkill => skiAthlete.Downhill;
        public PhysiologyState PhysiologyState => physiology.State;
        public PhysiologyDefinition PhysiologyDefinition => physiology.Definition;
        public CompetitionLifecycle Lifecycle { get; }
        public CheckpointTracker Checkpoints { get; }
        public SkiState Motion { get; private set; }
        public long RouteNanometers { get; private set; }
        public int TotalPenaltyLoops { get; private set; }
        public SprintAthleteFrame LastFrame { get; private set; }
        /// <summary>Accepted state from the last committed tick, independent of prepared intent.</summary>
        public bool TuckActive => priorTuck;
        public bool FinalSprintActive => priorSprint;
        public int ActiveBout => Lifecycle.CompletedBouts < profile.BoutCount ? Lifecycle.CompletedBouts : -1;
        public ShootingSession ActiveShooting => ActiveBout >= 0 ? shooting[ActiveBout] : null;
        public int GetBoutClosedMask(int index)
        {
            if(index<0||index>=shooting.Length)throw new ArgumentOutOfRangeException(nameof(index));
            return shooting[index]?.ClosedMask??0;
        }
        public bool HasFinished => Lifecycle.Phase == CompetitionPhase.Finished || Lifecycle.Phase == CompetitionPhase.Classified;
        internal bool IsBoundTo(RaceRoute selectedRoute)=>ReferenceEquals(route,selectedRoute);
        internal int TickFrequency=>config.TickFrequency;
        public RangeMotionPoint? CurrentRangeMotion
        {
            get
            {
                for (int i = 0; i < ranges.Length; i++)
                    if (ranges[i].Phase != RangeTraversalPhase.Complete) return ranges[i].PreviewMotion();
                return null;
            }
        }
        /// <summary>Read-only lookahead on the current assigned range/penalty path.
        /// Does not prepare a tick, choose a mat, grant travel or award progress.</summary>
        public RangeMotionPoint? PreviewCurrentRangeMotion(long advancedNanometers)
        {
            if(advancedNanometers<0)throw new ArgumentOutOfRangeException(nameof(advancedNanometers));
            for(int i=0;i<ranges.Length;i++)if(ranges[i].Phase!=RangeTraversalPhase.Complete)
                return ranges[i].PreviewMotion(advancedNanometers);
            return null;
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
            StartApproachMap startApproach = null, long startBehindLineNanometers = 0, PhysicalRangePathMap physicalRangePath = null,
            PhysicalRangePathMap[] rangePathChoices = null)
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
            if(physicalRangePath!=null)
            {
                if(!physicalRangePath.IsBoundTo(track,route)||lane.LaneId!=physicalRangePath.LaneId||lane.Ordinal!=physicalRangePath.LaneOrdinal)
                    throw new ArgumentException("Physical path and assigned shooting lane must match the exact track/route and lane ordinal.");
                this.physicalRangePath=physicalRangePath;
                PhysicalRangePathContextJson=ShootingModelDefinitions.EncodeContext(new[]{"athlete_fixed_range_path_v1",physicalRangePath.ContextJson,lane.RangeId,
                    lane.LaneId,lane.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)});
            }
            if(rangePathChoices!=null)
            {
                if(physicalRangePath==null||rangePathChoices.Length==0||rangePathChoices.Length>PhysicalRangePathDefinition.MaximumLanes)
                    throw new ArgumentException("Bounded preconfigured physical path choices require an initial path.");
                this.rangePathChoices=(PhysicalRangePathMap[])rangePathChoices.Clone();
                var context=new string[rangePathChoices.Length+2];context[0]="athlete_prepared_range_paths_v1";context[1]=PhysicalRangePathContextJson;
                for(int i=0;i<this.rangePathChoices.Length;i++)
                {
                    var candidate=this.rangePathChoices[i];
                    if(candidate==null||candidate.LaneOrdinal!=i+1||!physicalRangePath.IsSameLayoutFamily(candidate))
                        throw new ArgumentException("Every path choice must share the exact layout family and authored ordinal order.");
                    context[i+2]=candidate.ContextJson;
                }
                if(physicalRangePath.LaneOrdinal>rangePathChoices.Length||!ReferenceEquals(this.rangePathChoices[physicalRangePath.LaneOrdinal-1],physicalRangePath))
                    throw new ArgumentException("Initial path must be the exact configured choice.");
                PhysicalRangePathContextJson=ShootingModelDefinitions.EncodeContext(context);
            }
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
            for (int i = 0; i < ranges.Length; i++) ranges[i] = new RangePenaltyTraversal(profile, track, route, i, AthleteId, config.TickFrequency, rangeEvents,rangePath:physicalRangePath);
            if(this.rangePathChoices!=null)
            {
                rangePathVariants=new RangePenaltyTraversal[ranges.Length][];pathChosenForBout=new bool[ranges.Length];
                for(int b=0;b<ranges.Length;b++)
                {
                    int lap=profile.GetBout(b).AfterLap-1;
                    if(physicalRangePath.FirstForkRouteNanometers(lap)<=ranges[b].RangeEntryNanometers)
                        throw new ArgumentException("Prepared range choices need a common admission line strictly before every fork.");
                    rangePathVariants[b]=new RangePenaltyTraversal[this.rangePathChoices.Length];
                    for(int j=0;j<this.rangePathChoices.Length;j++)
                        rangePathVariants[b][j]=ReferenceEquals(this.rangePathChoices[j],physicalRangePath) ? ranges[b] :
                            new RangePenaltyTraversal(profile,track,route,b,AthleteId,config.TickFrequency,rangeEvents,rangePath:this.rangePathChoices[j]);
                }
            }
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
            int terrainIntervals=physicalRangePath?.TerrainIntervalCount??0;
            if(this.rangePathChoices!=null)for(int i=0;i<this.rangePathChoices.Length;i++)terrainIntervals=Math.Max(terrainIntervals,this.rangePathChoices[i].TerrainIntervalCount);
            segments = new RaceMotionSegment[checked((route.SectionCount + profile.ShotsPerBout + 1) * (segmentCount + 8 + terrainIntervals))];
            receipts = new CompetitionReceipt[profile.LapCount + profile.ShotsPerBout + 4];
        }
        public SprintAthleteFrame Step(SprintAthleteInput input) => CommitStep(PrepareStep(input));

        /// <summary>Event-free preparation into owned scratch. Every attempt replaces the prior handle.
        /// Shooting actions remain deferred until commit; call between field ticks on the simulation thread.</summary>
        public SprintStepPreview PrepareStep(SprintAthleteInput input)
            => PreparePackStep(input, 1, 1, 0);
        /// <summary>Prepare shared physiology/posture for a manual base-route
        /// tick without invoking the legacy auto-following path. The result is
        /// intentionally uncommittable until a full-clear world proposal binds it.</summary>
        public SprintStepPreview PrepareManualBaseStep(SprintAthleteInput input)
            =>PreparePackStep(input,1,1,0,default,true);

        public SprintRangePathChoice CreateRangePathChoice(int laneOrdinal)
        {
            int b=FirstUncompletedRange();
            if(rangePathChoices==null||laneOrdinal<=0||laneOrdinal>rangePathChoices.Length||b<0||b!=ActiveBout||pathChosenForBout[b]||
                ranges[b].Phase!=RangeTraversalPhase.Approach||RouteNanometers>=physicalRangePath.FirstForkRouteNanometers(profile.GetBout(b).AfterLap-1))
                throw new InvalidOperationException("An unassigned current bout and a shared approach before its earliest fork are required.");
            return new SprintRangePathChoice(this,previewRevision,Tick,b,laneOrdinal);
        }
        public SprintStepPreview PrepareRangePathStep(SprintAthleteInput input,SprintRangePathChoice choice)
        {
            if(choice.Owner==null){previewReady=false;throw new ArgumentException("An explicit owned path choice is required.");}
            return PreparePackStep(input,1,1,0,choice);
        }
        public PhysicalRangePathMap GetPreparedRangePath(SprintStepPreview preview)
        {ValidatePreview(preview);return preparedPath;}
        private int FirstUncompletedRange()
        {for(int i=0;i<ranges.Length;i++)if(ranges[i].Phase!=RangeTraversalPhase.Complete)return i;return -1;}

        internal int MotionSegmentCapacity => segments.Length;
        internal SprintStepPreview PreparePackStep(SprintAthleteInput input, double dragMultiplier, double propulsionMultiplier, int extraEffort)
            =>PreparePackStep(input,dragMultiplier,propulsionMultiplier,extraEffort,default);
        internal SprintStepPreview PreparePackStep(SprintAthleteInput input, double dragMultiplier, double propulsionMultiplier, int extraEffort,SprintRangePathChoice choice)
            =>PreparePackStep(input,dragMultiplier,propulsionMultiplier,extraEffort,choice,false);
        private SprintStepPreview PreparePackStep(SprintAthleteInput input, double dragMultiplier, double propulsionMultiplier, int extraEffort,
            SprintRangePathChoice choice,bool manualBase)
        {
            previewReady = false;
            ValidateCommand(input);
            PhysiologyNumbers.RequireRange(dragMultiplier, 0, 1, nameof(dragMultiplier));
            PhysiologyNumbers.RequireRange(propulsionMultiplier, 0, 1, nameof(propulsionMultiplier));
            if (extraEffort < 0 || extraEffort > 1000) throw new ArgumentOutOfRangeException(nameof(extraEffort));
            var tick = input.Ski.Effort.Tick;
            bool started = tick.Value > ScheduledStart.Value;
            int rangeIndex = FirstUncompletedRange();
            var range = rangeIndex >= 0 ? ranges[rangeIndex] : null;
            var candidatePath=physicalRangePath;var candidateLane=lane;bool choosing=choice.Owner!=null;
            if(choosing)
            {
                if(!ReferenceEquals(choice.Owner,this)||choice.SourceRevision!=previewRevision||choice.SourceTick.Value!=Tick.Value||
                    rangePathChoices==null||rangeIndex!=choice.BoutIndex||rangeIndex!=ActiveBout||pathChosenForBout[rangeIndex]||
                    range.Phase!=RangeTraversalPhase.Approach||choice.LaneOrdinal<=0||choice.LaneOrdinal>rangePathChoices.Length)
                    throw new ArgumentException("Foreign, stale, locked or inactive range choice.");
                candidatePath=rangePathChoices[choice.LaneOrdinal-1];
                if(RouteNanometers>=candidatePath.FirstForkRouteNanometers(profile.GetBout(rangeIndex).AfterLap-1))
                    throw new ArgumentException("Range choice cannot change a path after its first fork.");
                candidateLane=new ShootingLaneAssignment(lane.RangeId,candidatePath.LaneId,candidatePath.LaneOrdinal);
            }
            if(rangePathVariants!=null&&range!=null&&!ReferenceEquals(range.RangePath,candidatePath))
            {
                var staged=rangePathVariants[rangeIndex][candidatePath.LaneOrdinal-1];
                staged.StageAcceptedStateFrom(range,RouteNanometers);range=staged;
            }
            bool onMat = started && range != null && range.Phase == RangeTraversalPhase.Shooting;
            if(manualBase&&(!started||onMat||RemainingStartApproachNanometers!=0||choosing||
                range!=null&&(range.Phase!=RangeTraversalPhase.Approach&&range.Phase!=RangeTraversalPhase.Complete||
                              RouteNanometers>=range.RangeEntryNanometers)))
                throw new InvalidOperationException("Manual base-route preview requires an active base section before range entry.");
            var sample = RemainingStartApproachNanometers>0 ? startApproach.Sample(RemainingStartApproachNanometers) : range != null && range.Phase == RangeTraversalPhase.PenaltyLoop ? range.SamplePenalty() :
                candidatePath!=null ? candidatePath.SampleRoute(RouteNanometers) : route.Sample(new TrackDistance(RouteNanometers / SkiState.NanometersPerMillimeter)).Track;
            var sprintOutput = sprint.Evaluate(started && !onMat && input.Ski.SprintRequested, input.Ski.Effort.RequestedEffort, new SkiState(RouteNanometers, Motion.SpeedMetersPerSecond));
            var posture = tuck.Evaluate(started && !onMat && input.Ski.TuckRequested, sprintOutput.EffectiveEffortControl, skiAthlete.Downhill, Motion, sample);
            var work = new PhysiologyInput(new SimulationInput(tick, AthleteId, started && !onMat ? Math.Min(1000, posture.EffectiveEffortControl + extraEffort) : 0), input.Ski.Form, sample.TerrainMuscleFactor);
            var physical = physiology.Preview(work);
            var movement = started && !onMat && !manualBase ? path.Preview(Motion, RouteNanometers, finishNanometers, range,
                physical.Effort.DeliveredPowerWatts * propulsionMultiplier, physiology.State.MuscularFatigue, input.Ski, segments, posture.DragAreaMultiplier * dragMultiplier, RemainingStartApproachNanometers,candidatePath) : default;
            long revision = checked(previewRevision + 1);
            preparedInput = input; preparedWork = work; preparedPosture = posture; preparedSprint = sprintOutput;
            preparedRangeIndex = rangeIndex; preparedSample = sample;
            preparedPath=candidatePath;preparedRange=range;preparedLane=candidateLane;preparedChoosesPath=choosing;
            previewRevision = revision; previewReady = true;
            return new SprintStepPreview(this, revision, tick, physical, movement, started && !onMat, onMat,
                manualWorldRequired:manualBase);
        }
        public RaceMotionSegment GetPreparedSegment(SprintStepPreview preview, int index)
        {
            ValidatePreview(preview);
            if (index < 0 || index >= preview.Movement.SegmentCount) throw new ArgumentOutOfRangeException(nameof(index));
            return segments[index];
        }
        /// <summary>Supply the manual world solver from this athlete's staged
        /// physiology, posture, technique, snow and equipment. No route travel
        /// or race state is committed by asking for the motion input.</summary>
        public ManualSkiMotionInput GetPreparedManualMotionInput(SprintStepPreview preview,
            ManualRouteActorFrame accepted,TrackSample surface,int steeringInput,
            double dragMultiplier=1,double propulsionMultiplier=1,double windX=0,double windZ=0)
        {
            ValidatePreview(preview);
            if(!preview.ManualWorldRequired||!preview.MovementActive||preview.ShootingActive||preview.World.IsValid||
                RemainingStartApproachNanometers!=0||preparedChoosesPath||
                !accepted.IsValid||!accepted.Athlete.Equals(AthleteId)||
                accepted.Projection.RouteNanometers!=RouteNanometers||
                surface.LoopId!=accepted.Projection.LoopId||
                Math.Abs(surface.Distance.Meters-accepted.Projection.LocalMeters)>.001+2*PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Accepted base-route observation and prepared ski tick required.");
            return path.PreviewManualInput(preview.Physiology,physiology.State.MuscularFatigue,
                preparedInput.Ski,preparedPosture,accepted.Pose,accepted.Projection,
                surface,steeringInput,dragMultiplier,propulsionMultiplier,windX,windZ);
        }
        /// <summary>Replace the legacy route-following ski preview only after a
        /// whole manual world roster tick is proven clear. This first bridge is
        /// limited to one base-route section before its range entry; it cannot
        /// authorize a seam, start lane, mat or penalty loop.</summary>
        public SprintStepPreview PrepareManualBaseRouteCommit(SprintStepPreview preview,
            ManualWorldRosterPreview world,int actorIndex)
        {
            ValidatePreview(preview);
            var ledger=world.Owner;
            if(ledger==null||!ledger.HasPhysicalOdometer||ledger.Tick.Value!=Tick.Value||
                world.Tick.Value!=preview.Tick.Value||!preview.ManualWorldRequired||!preview.MovementActive||
                preview.ShootingActive||RemainingStartApproachNanometers!=0||preparedChoosesPath)
                throw new ArgumentException("Active base-route movement and a bound full-world tick required.");
            var before=ledger.GetCursor(actorIndex);
            var after=ledger.GetProposedCursor(world,actorIndex);
            var actor=ledger.GetProposedActor(world,actorIndex);
            var physicalBefore=ledger.GetPhysicalMotion(actorIndex);
            var physicalAfter=ledger.GetProposedPhysicalMotion(world,actorIndex);
            if(!before.Athlete.Equals(AthleteId)||!after.Athlete.Equals(AthleteId)||
                !actor.Athlete.Equals(AthleteId)||before.SectionIndex!=after.SectionIndex||
                before.CreditedStationNanometers!=RouteNanometers||
                physicalBefore.DistanceNanometers!=Motion.DistanceNanometers||
                physicalBefore.SpeedMetersPerSecond!=Motion.SpeedMetersPerSecond||
                after.CreditedStationNanometers<RouteNanometers||
                after.CreditedStationNanometers>finishNanometers||
                physicalAfter.DistanceNanometers<Motion.DistanceNanometers||
                preparedRange!=null&&preparedRange.Phase!=RangeTraversalPhase.Approach&&
                    preparedRange.Phase!=RangeTraversalPhase.Complete||
                preparedRange!=null&&preparedRange.Phase==RangeTraversalPhase.Approach&&
                    after.CreditedStationNanometers>=preparedRange.RangeEntryNanometers)
                throw new ArgumentException("World proposal is stale or crosses an unadmitted race phase.");
            double dt=1.0/config.TickFrequency;
            segments[0]=new RaceMotionSegment(RouteNanometers,
                after.CreditedStationNanometers,Motion.DistanceNanometers,
                physicalAfter.DistanceNanometers,0,dt,false);
            var movement=new RacePathMotionFrame(physicalAfter,after.CreditedStationNanometers,0,
                dt,0,1,false,after.CreditedStationNanometers==finishNanometers);
            long revision=checked(previewRevision+1);
            previewRevision=revision;
            return new SprintStepPreview(this,revision,preview.Tick,preview.Physiology,
                movement,true,false,world);
        }
        /// <summary>Retain a verified forward prefix and stop. Caller must prove continuous safety.
        /// Full prepared physiology is preserved; this is not a collision solver.</summary>
        public SprintStepPreview ConstrainPreparedTravel(SprintStepPreview preview, long maximumTravelNanometers)
        {
            ValidatePreview(preview);
            if(preview.World.IsValid||preview.ManualWorldRequired)
                throw new InvalidOperationException("A proven world tick cannot be clipped by legacy route travel.");
            if (!preview.MovementActive || maximumTravelNanometers < 0)
                throw new ArgumentException("Active movement and nonnegative travel cap required.");
            long available = preview.Movement.Motion.DistanceNanometers - Motion.DistanceNanometers;
            if (maximumTravelNanometers > available) throw new ArgumentException("Cannot extend movement.");
            if (maximumTravelNanometers == available) return preview;
            long revision = checked(previewRevision + 1);
            long remaining = maximumTravelNanometers, main = RouteNanometers, rangeTravel = 0;
            double consumed = 0; int count = 0;
            var range = preparedRange;
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
        public int CurrentRangeRequiredPenaltyLoops
        {get{for(int i=0;i<ranges.Length;i++)if(ranges[i].Phase!=RangeTraversalPhase.Complete)return ranges[i].RequiredLoops;return 0;}}
        public int CurrentRangeRemainingPenaltyLoops
        {get{for(int i=0;i<ranges.Length;i++)if(ranges[i].Phase!=RangeTraversalPhase.Complete)return ranges[i].RequiredLoops-ranges[i].CompletedLoops;return 0;}}
        private void ValidatePreview(SprintStepPreview preview)
        {
            if (!previewReady || !ReferenceEquals(preview.Owner, this) || preview.Revision != previewRevision ||
                preview.Tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Missing, foreign, replaced or consumed Sprint preview.");
            if(preview.World.IsValid)preview.World.Owner.ValidatePrepared(preview.World);
        }
        internal void ValidatePreparedCommit(SprintStepPreview preview)
        { ValidatePreview(preview);if(preview.ManualWorldRequired)throw new InvalidOperationException("Manual world proof required before race commit.");ValidateCommand(preparedInput); }
        internal RangeMotionPoint? PreviewRangeMotion(SprintStepPreview preview, long additionalTravelNanometers = 0)
        {
            ValidatePreview(preview);
            if (additionalTravelNanometers < 0) throw new ArgumentOutOfRangeException(nameof(additionalTravelNanometers));
            return preparedRangeIndex < 0 ? (RangeMotionPoint?)null : preparedRange.PreviewMotion(checked(preview.Movement.RangeTravelNanometers + additionalTravelNanometers));
        }
        public SprintAthleteFrame CommitStep(SprintStepPreview preview)
        {
            ValidatePreview(preview);
            if(preview.ManualWorldRequired)throw new InvalidOperationException("Manual world proof required before race commit.");
            ValidateCommand(preparedInput);
            previewReady = false;
            var input = preparedInput; var tick = preview.Tick;
            bool started = tick.Value > ScheduledStart.Value, onMat = preview.ShootingActive;
            int rangeIndex = preparedRangeIndex;
            bool publishChoice=preparedChoosesPath&&preview.MovementActive&&preview.Movement.RouteNanometers>=preparedRange.RangeEntryNanometers&&
                preview.Movement.Motion.DistanceNanometers>Motion.DistanceNanometers;
            var range = rangeIndex >= 0 ? ranges[rangeIndex] : null;
            if(publishChoice)
            {
                range=ranges[rangeIndex]=preparedRange;physicalRangePath=preparedPath;lane=preparedLane;pathChosenForBout[rangeIndex]=true;
                events.Record(new RaceEvent(preview.Tick,config.TickFrequency,AthleteId,RaceEventType.PhysicalRangePathAssigned,lane.Ordinal));
            }
            else if(!preparedChoosesPath&&rangePathVariants!=null&&range!=null&&!ReferenceEquals(range,preparedRange))
                range=ranges[rangeIndex]=preparedRange;
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

    /// <summary>Prevalidate a full active base-route athlete roster against one
    /// jointly clear world proposal, then publish those sessions and the world
    /// ledger on the same fixed tick. Field timing and phase paths remain with
    /// the eventual full-race orchestrator.</summary>
    public sealed class ManualBaseRosterSessionCommit
    {
        private readonly SprintAthleteSession[] athletes;
        private readonly ManualWorldRosterLedger world;
        private readonly SprintStepPreview[] resolved;

        public ManualBaseRosterSessionCommit(RaceRoute route,
            ReadOnlySpan<SprintAthleteSession> roster,ManualWorldRosterLedger world)
        {
            if(route==null||world==null||!world.HasPhysicalOdometer||
                !world.IsBoundTo(route)||roster.Length==0||roster.Length!=world.Count)
                throw new ArgumentException("A matching physical world and complete race roster are required.");
            this.world=world;
            athletes=roster.ToArray();resolved=new SprintStepPreview[roster.Length];
            for(int i=0;i<athletes.Length;i++)
            {
                var athlete=athletes[i];
                if(athlete==null||!athlete.IsBoundTo(route)||athlete.HasFinished||
                    athlete.TickFrequency!=world.TickFrequency||
                    !athlete.AthleteId.Equals(world.GetCursor(i).Athlete)||
                    athlete.Tick.Value!=world.Tick.Value||
                    athlete.RouteNanometers!=world.GetCursor(i).CreditedStationNanometers||
                    !athlete.Motion.Equals(world.GetPhysicalMotion(i)))
                    throw new ArgumentException("World and race roster order/state must match exactly.");
            }
        }

        public void Commit(ReadOnlySpan<SprintStepPreview> pending,
            ManualWorldRosterPreview proposal)
        {
            if(pending.Length!=athletes.Length||!ReferenceEquals(proposal.Owner,world))
                throw new ArgumentException("One current preview per accepted world actor is required.");
            world.ValidatePrepared(proposal);
            for(int i=0;i<athletes.Length;i++)
            {
                var athlete=athletes[i];
                if(athlete.Tick.Value!=world.Tick.Value||
                    athlete.RouteNanometers!=world.GetCursor(i).CreditedStationNanometers||
                    !athlete.Motion.Equals(world.GetPhysicalMotion(i)))
                    throw new ArgumentException("Accepted world and race state diverged.");
                resolved[i]=athlete.PrepareManualBaseRouteCommit(pending[i],proposal,i);
            }
            for(int i=0;i<athletes.Length;i++)athletes[i].ValidatePreparedCommit(resolved[i]);
            // The production field will also own timing/results and journal
            // publication. This base-section batch assumes ordinary nonthrowing
            // race event sinks, as existing SprintFieldSession commits do.
            for(int i=0;i<athletes.Length;i++)athletes[i].CommitStep(resolved[i]);
            world.Commit(proposal);
        }
    }
}
