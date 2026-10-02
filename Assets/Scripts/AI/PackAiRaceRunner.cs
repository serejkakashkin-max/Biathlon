using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Single-threaded AI composition. Observe every actor before one authoritative field
    /// step; publish telemetry/events only after that step succeeds. All physics lives in Field.</summary>
    public sealed class PackAiRaceRunner
    {
        private readonly AiRacePolicyDefinition policy;
        private readonly AiDifficultyDefinition difficulty;
        private readonly ShootingModelDefinitions shootingModel;
        private readonly ShootingAiDefinition aimDefinition;
        private readonly PaceControllerDefinition paceDefinition;
        private readonly PreRangeRecoveryDefinition recoveryDefinition;
        private readonly AiPersonalityPolicy[] personalities;
        private readonly TacticalDecisionClock[] clocks;
        private readonly PaceController[] pace;
        private readonly DraftTargetSelector[] draft;
        private readonly OvertakeController[] lanes;
        private readonly ShootingAiController[] shooters;
        private readonly ShootingSession[] previousShooting;
        private readonly TacticalIntentDecision[] intents, publishedIntents;
        private readonly ShootingStrategyPlan[] strategies, publishedStrategies;
        private readonly AiUtilitySnapshot[] utilities;
        private readonly DraftSelectionFrame[] draftFrames;
        private readonly OvertakeDecisionFrame[] overtakeFrames;
        private readonly PreRangeRecoveryPlan[] recoveryFrames;
        private readonly PaceControllerFrame[] paceFrames;
        private readonly ShootingAiFrame[] shootingFrames;
        private readonly bool[] tacticalDue, shootingDue;
        private readonly SprintAthleteInput[] inputs;
        private readonly double[] lateral;
        private readonly SkiPlacement[] placements;
        private readonly AiRouteNeighbor[] neighbors;
        private readonly TrackDistance[] offsets;
        private readonly AiRouteLookahead[] ahead;
        private readonly double[] features=new double[8], preferences=new double[8];
        private readonly TacticalCandidate[] candidates=new TacticalCandidate[8];
        private readonly TacticalUtilityScore[] scores=new TacticalUtilityScore[8];
        private readonly SprintPerceptionSource raceSource;
        private readonly PackPerceptionSource physicalSource;
        private readonly TacticalUtilityEvaluator utility;
        private readonly TerrainPacePlanner terrain;
        private readonly PreRangeRecoveryPlanner recovery;
        private readonly ShootingStrategyPlanner shootingStrategy;
        private readonly OvertakeDecisionEvaluator overtake;
        private readonly IAiDecisionSink telemetry;
        private readonly EventJournal journal;
        private readonly IRaceEventSink events;
        private readonly ulong seed;
        private readonly int frequency;
        private readonly double[] raceForms; private readonly ShootingPressureContext[] racePressures;
        private readonly PursuitTacticalPlanner pursuit;
        private readonly PursuitTacticalPlan[] pursuitPlans, publishedPursuitPlans;
        public PursuitTacticalPlan GetLastPursuitPlan(int index)=>publishedPursuitPlans[index];
        public PackSprintFieldSession Field { get; }
        public string ContextJson { get; }
        public long TacticalDecisions { get; private set; }
        public long ShootingDecisions { get; private set; }
        public bool IsFaulted { get; private set; }
        public SprintAthleteInput GetLastInput(int index)=>inputs[index];
        public TacticalIntentDecision GetLastIntent(int index)=>intents[index];
        public double GetLastLateral(int index)=>lateral[index];
        private sealed class EventJournal : IRaceEventSink
        {
            private readonly RaceEvent[] entries; private int count;
            internal EventJournal(int capacity) { entries=new RaceEvent[capacity]; }
            internal void Clear()=>count=0;
            public void Record(RaceEvent value)
            { if(count==entries.Length) throw new InvalidOperationException("AI event journal capacity exceeded."); entries[count++]=value; }
            internal void Flush(IRaceEventSink sink) { for(int i=0;i<count;i++) sink.Record(entries[i]); count=0; }
        }
        public PackAiRaceRunner(PackSprintFieldSession field,Func<string,string> read,
            ReadOnlySpan<AiPersonalityDefinition> profiles,AiDifficultyDefinition difficulty,ulong seed,
            IRaceEventSink events,IAiDecisionSink telemetry,double[] raceForms=null,ShootingPressureContext[] racePressures=null)
        {
            Field=field??throw new ArgumentNullException(nameof(field));
            if(read==null || difficulty==null || events==null || telemetry==null) throw new ArgumentNullException();
            if(field.Tick.Value!=0 || profiles.Length!=field.Count) throw new ArgumentException("Fresh field and complete personality roster required.");
            for(int i=0;i<profiles.Length;i++) if(profiles[i]==null) throw new ArgumentException("Null personality.");
            if(raceForms!=null&&raceForms.Length!=field.Count||racePressures!=null&&racePressures.Length!=field.Count)throw new ArgumentException("Complete readiness roster required.");
            this.raceForms=raceForms==null?null:(double[])raceForms.Clone();this.racePressures=racePressures==null?null:(ShootingPressureContext[])racePressures.Clone();
            if(this.raceForms!=null)foreach(double value in this.raceForms)PhysiologyNumbers.RequireRange(value,0,1.1,"race form");
            this.difficulty=difficulty; this.seed=seed; this.events=events; this.telemetry=telemetry; frequency=field.TickFrequency;
            policy=AiRacePolicyDefinition.FromJson(read("ai_race_policy_provisional_v1.json"));
            for(int i=0;i<field.Track.LoopCount;i++)
                if(field.Track.GetLoop(i).Closed && policy.PhysicalRadius*2>field.Track.GetLoop(i).Length.Meters) throw new ArgumentException("Physical preview exceeds half a periodic loop.");
            var cadence=difficulty.Cadence(); paceDefinition=difficulty.Apply(PaceControllerDefinition.FromJson(read("pace_controller_provisional_v1.json")));
            shootingModel=new ShootingModelDefinitions(read); aimDefinition=difficulty.Apply(ShootingAiDefinition.FromJson(read("shooting_ai_v1.json")));
            var utilityDefinition=TacticalUtilityDefinition.FromJson(read("tactical_utility_provisional_v1.json"));
            var terrainDefinition=TerrainPlanningDefinition.FromJson(read("terrain_planning_provisional_v1.json"));
            recoveryDefinition=PreRangeRecoveryDefinition.FromJson(read("pre_range_recovery_provisional_v1.json"));
            var strategyDefinition=ShootingStrategyDefinition.FromJson(read("shooting_strategy_provisional_v1.json"));
            var draftDefinition=DraftDefinition.FromJson(read("draft_provisional_v1.json"));
            var selectionDefinition=DraftSelectionDefinition.FromJson(read("draft_selection_provisional_v1.json"));
            var laneDefinition=OvertakeDefinition.FromJson(read("overtake_provisional_v1.json"));
            var overtakeDefinition=OvertakeDecisionDefinition.FromJson(read("overtake_decision_provisional_v1.json"));
            var occupancyDefinition=OccupancyDefinition.FromJson(read("occupancy_provisional_v1.json"));
            var occupancy=new OccupancySystem(occupancyDefinition);
            utility=new TacticalUtilityEvaluator(utilityDefinition); terrain=new TerrainPacePlanner(terrainDefinition);
            recovery=new PreRangeRecoveryPlanner(recoveryDefinition); shootingStrategy=new ShootingStrategyPlanner(strategyDefinition);
            overtake=new OvertakeDecisionEvaluator(overtakeDefinition,laneDefinition,occupancy);
            raceSource=new SprintPerceptionSource(field); physicalSource=new PackPerceptionSource(field);
            int n=field.Count; journal=new EventJournal(n*4);
            pursuitPlans=new PursuitTacticalPlan[n];publishedPursuitPlans=new PursuitTacticalPlan[n];
            personalities=new AiPersonalityPolicy[n];clocks=new TacticalDecisionClock[n];pace=new PaceController[n];draft=new DraftTargetSelector[n];lanes=new OvertakeController[n];
            shooters=new ShootingAiController[n];previousShooting=new ShootingSession[n];intents=new TacticalIntentDecision[n];publishedIntents=new TacticalIntentDecision[n];
            strategies=new ShootingStrategyPlan[n];publishedStrategies=new ShootingStrategyPlan[n];utilities=new AiUtilitySnapshot[n];draftFrames=new DraftSelectionFrame[n];
            overtakeFrames=new OvertakeDecisionFrame[n];recoveryFrames=new PreRangeRecoveryPlan[n];paceFrames=new PaceControllerFrame[n];shootingFrames=new ShootingAiFrame[n];
            tacticalDue=new bool[n];shootingDue=new bool[n];inputs=new SprintAthleteInput[n];lateral=new double[n];placements=new SkiPlacement[n];neighbors=new AiRouteNeighbor[n-1];
            offsets=new TrackDistance[policy.PreviewSamples];ahead=new AiRouteLookahead[offsets.Length];
            for(int i=0;i<offsets.Length;i++) offsets[i]=TrackDistance.FromMeters(i*policy.PreviewStride);
            var context=new List<string>{"pack_ai_runner_v1",field.ContextJson,seed.ToString(System.Globalization.CultureInfo.InvariantCulture),policy.ToJson(),difficulty.ToJson(),
                cadence.ToJson(),paceDefinition.ToJson(),shootingModel.ContextJson,aimDefinition.ToJson(),utilityDefinition.ToJson(),terrainDefinition.ToJson(),
                recoveryDefinition.ToJson(),strategyDefinition.ToJson(),draftDefinition.ToJson(),selectionDefinition.ToJson(),laneDefinition.ToJson(),overtakeDefinition.ToJson(),occupancyDefinition.ToJson()};
            for(int i=0;i<n;i++)
            {
                var id=field.GetAthlete(i).AthleteId; personalities[i]=new AiPersonalityPolicy(id,profiles[i]);context.Add(personalities[i].ContextJson);
                clocks[i]=new TacticalDecisionClock(cadence,frequency);pace[i]=new PaceController(paceDefinition,id,frequency,new SeededRandomSource(ActorSeed(id,0)));
                draft[i]=new DraftTargetSelector(selectionDefinition,draftDefinition,id,frequency);lanes[i]=new OvertakeController(laneDefinition,occupancy,id,frequency,journal);
            }
            if(field.Profile.Format==CompetitionFormat.Pursuit)
            {
                var definition=PursuitTacticsDefinition.FromJson(read("pursuit_tactics_provisional_v1.json"));
                if(definition.DefenseRadius>policy.PhysicalRadius) throw new ArgumentException("Pursuit defense must fit the observed neighborhood.");
                pursuit=new PursuitTacticalPlanner(definition);context.Add(definition.ToJson());
                context.Add("pursuit_global_checkpoint_or_source_gap;bounded_preferences;last_range_risk_v1");
            }
            if(this.raceForms!=null)foreach(double value in this.raceForms)context.Add("readiness_form:"+value.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            if(this.racePressures!=null)foreach(var value in this.racePressures)context.Add("readiness_pressure:"+value.RaceImportance.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+value.PositionImportance.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+value.RivalProximity.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+value.Confidence.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+value.FinalStanding+","+value.FirstPodium);
            using(var stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,context.ToArray());ContextJson=Encoding.UTF8.GetString(stream.ToArray()); }
        }
        private ulong ActorSeed(AthleteId id,int stream)
        { unchecked { ulong h=14695981039346656037UL^seed;foreach(char c in id.Value) h=(h^c)*1099511628211UL;return h^(ulong)stream; } }
        private int boundPlayerIndex=-1;
        public void Step()
        {
            if(IsFaulted) throw new InvalidOperationException("Failed AI planning must be reconstructed from its recorded context/commands.");
            if(boundPlayerIndex>=0)throw new InvalidOperationException("Player-controlled race requires a player command every tick.");
            try { StepCore(-1,default,0); } catch { IsFaulted=true;throw; }
        }
        /// <summary>Replace one actor's intent before shared physics; other AI observes the same prior tick.</summary>
        public void StepWithPlayer(int index,SprintAthleteInput input,double desiredLateralMeters)
        {
            if(index<0||index>=Field.Count)throw new ArgumentOutOfRangeException(nameof(index));
            if(double.IsNaN(desiredLateralMeters)||double.IsInfinity(desiredLateralMeters))throw new ArgumentException("Finite lateral intent required.");
            if(input.Ski.Effort.Tick.Value!=Field.Tick.Value+1||!input.Ski.Effort.AthleteId.Equals(Field.GetAthlete(index).AthleteId))throw new ArgumentException("Player tick/identity mismatch.");
            if(IsFaulted)throw new InvalidOperationException("Faulted runner must be reconstructed.");
            if(boundPlayerIndex<0&&Field.Tick.Value!=0)throw new InvalidOperationException("Select the player before the race starts.");
            if(boundPlayerIndex>=0&&boundPlayerIndex!=index)throw new InvalidOperationException("Player identity cannot change during a race.");
            boundPlayerIndex=index;
            try { StepCore(index,input,desiredLateralMeters); } catch { IsFaulted=true;throw; }
        }
        private void StepCore(int playerIndex,SprintAthleteInput playerInput,double playerLateral)
        {
            if(Field.IsComplete) throw new InvalidOperationException("AI race is complete.");
            journal.Clear(); var tick=Field.Tick.Next();
            for(int i=0;i<Field.Count;i++)
            {
                var actor=Field.GetAthlete(i); var id=actor.AthleteId; tacticalDue[i]=shootingDue[i]=false;
                if(actor.HasFinished) { inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,0),1,1,0),new WeaponAimCommand(tick,id,0,0));continue; }
                if(i==playerIndex){inputs[i]=playerInput;lateral[i]=playerLateral;continue;}
                var observation=raceSource.Capture(i,TrackDistance.FromMeters(policy.PhysicalRadius),offsets,ahead,neighbors);
                int count=physicalSource.CapturePlacements(i,TrackDistance.FromMeters(policy.PhysicalRadius),placements);
                var point=Field.GetPhysicalPoint(i); var self=count>0?placements[0]:new SkiPlacement(id,point.LoopId,new SkiState(point.LoopLengthNanometers,actor.Motion.SpeedMetersPerSecond),Field.GetLateral(i));
                var roster=placements.AsSpan(0,count); bool skiing=count>0 && observation.Phase==CompetitionPhase.Skiing;
                var window=clocks[i].Step(tick); TacticalIntentDecision? chosen=null; int adjustment=0;bool pass=false;
                if(window.DecisionDue)
                {
                    tacticalDue[i]=true;
                    draftFrames[i]=count>0?draft[i].Evaluate(tick,0,roster,observation.CurrentTrack):default;
                    recoveryFrames[i]=recovery.Evaluate(observation);
                    var segment=Field.Track.GetLoop(observation.CurrentTrack.LoopId).GetSegment(observation.CurrentTrack.SegmentIndex);
                    long clear=segment.StartDistance.Millimeters+segment.Length.Millimeters-observation.CurrentTrack.Distance.Millimeters;
                    if(observation.NextRangeEntryRouteDistanceNanometers.HasValue) clear=Math.Min(clear,observation.NextRangeEntryRouteDistanceNanometers.Value/SkiState.NanometersPerMillimeter);
                    overtakeFrames[i]=skiing?overtake.Evaluate(self,roster,observation.Physiology,1,observation.CurrentTrack.WidthMeters,
                        observation.CurrentTrack.OvertakeAllowed,Math.Max(Math.Abs(segment.StartGrade),Math.Abs(segment.EndGrade)),Math.Abs(segment.CurvaturePerMeter),new TrackDistance(clear)):new OvertakeDecisionFrame(OvertakeDecisionReason.Terrain);
                    bool canPass=skiing && overtakeFrames[i].Request && overtakeFrames[i].Opportunity.Available;
                    var selected=draftFrames[i].Selected; bool canDraft=skiing && selected.Strength>0;
                    bool final=actor.Lifecycle.CompletedLaps==Field.Profile.LapCount-1;
                    bool finish=skiing && final && observation.RemainingBaseRouteNanometers/1e9<=policy.FinalSprintDistance;
                    var plan=skiing?terrain.Evaluate(observation.SpeedMetersPerSecond,clocks[i].PlanningHorizonTicks/(double)frequency,observation.Physiology,ahead):default;
                    features[0]=1-observation.Physiology.AnaerobicReserve;features[1]=observation.Physiology.AnaerobicReserve;features[2]=observation.Physiology.MuscularFatigue;
                    features[3]=recoveryFrames[i].Urgency;features[4]=canDraft?selected.Strength:0;features[5]=canPass?1:0;
                    features[6]=skiing?Math.Max(policy.AttackOpportunity,plan.ClimbDemand):0;
                    features[7]=finish?Math.Max(0,1-observation.RemainingBaseRouteNanometers/1e9/policy.FinalSprintDistance):0;
                    candidates[0]=new TacticalCandidate(TacticalIntentKind.Recover,skiing);candidates[1]=new TacticalCandidate(TacticalIntentKind.Hold,true);
                    candidates[2]=new TacticalCandidate(TacticalIntentKind.Follow,canDraft,canDraft?selected.LeaderId:default);
                    candidates[3]=new TacticalCandidate(TacticalIntentKind.Draft,canDraft,canDraft?selected.LeaderId:default);
                    candidates[4]=new TacticalCandidate(TacticalIntentKind.Overtake,canPass,canPass?overtakeFrames[i].Opportunity.Target.AthleteId:default);
                    candidates[5]=new TacticalCandidate(TacticalIntentKind.Attack,skiing && observation.Physiology.AnaerobicReserve>=policy.AttackMinimumReserve);
                    candidates[6]=new TacticalCandidate(TacticalIntentKind.FinalSprint,finish);candidates[7]=new TacticalCandidate(TacticalIntentKind.PreRangeRecovery,skiing && recoveryFrames[i].Requested);
                    personalities[i].CopyPreferences(final,preferences);
                    if(pursuit!=null)
                    {
                        double gap=observation.CheckpointGapToLeaderSeconds ?? (actor.ScheduledStart.Value-Field.GetAthlete(0).ScheduledStart.Value)/(double)frequency;
                        int? place=observation.ObservedCheckpointPlace ?? (gap==0?(int?)1:null);
                        double? behind=null;
                        for(int j=0;j<observation.NeighborCount;j++)
                            if(neighbors[j].RouteGapNanometers<0 && neighbors[j].Phase==CompetitionPhase.Skiing)
                            {double distance=-neighbors[j].RouteGapNanometers/1e9;if(!behind.HasValue || distance<behind.Value)behind=distance;}
                        bool lastRange=actor.ActiveBout==Field.Profile.BoutCount-1;
                        pursuitPlans[i]=pursuit.Evaluate(gap,place,behind,canDraft?selected.Strength:0,lastRange,policy.ShootingTimeUrgency);
                        pursuit.ApplyPreferences(pursuitPlans[i],preferences);
                    }
                    chosen=utility.Evaluate(id,tick,window.NextDecisionTick,features,preferences,candidates,scores);intents[i]=chosen.Value;utilities[i]=new AiUtilitySnapshot(scores);
                    adjustment=plan.EffortAdjustment;
                    if(chosen.Value.Kind==TacticalIntentKind.PreRangeRecovery)
                        adjustment+=paceDefinition.Effort(TacticalIntentKind.Hold)-recoveryFrames[i].EffortReduction-paceDefinition.Effort(TacticalIntentKind.PreRangeRecovery);
                    pass=chosen.Value.Kind==TacticalIntentKind.Overtake;
                    double need=Math.Max(0,Math.Min(1,(observation.Physiology.HeartRateBpm-recoveryDefinition.StartHeartRate)/(recoveryDefinition.FullHeartRate-recoveryDefinition.StartHeartRate)));
                    strategies[i]=personalities[i].PlanShooting(shootingStrategy,pursuit!=null?pursuitPlans[i].ShootingUrgency:policy.ShootingTimeUrgency,need,0);
                }
                paceFrames[i]=pace[i].Step(tick,chosen,adjustment);
                var lane=lanes[i].Step(paceFrames[i].Input,self,observation.CurrentTrack,roster,pass,!skiing);
                lateral[i]=lane.DesiredLateralMeters;
                if(lane.Phase==OvertakePhase.Idle && skiing && (intents[i].Kind==TacticalIntentKind.Draft || intents[i].Kind==TacticalIntentKind.Follow))
                    for(int j=0;j<count;j++) if(placements[j].AthleteId.Equals(intents[i].TargetId)) lateral[i]=placements[j].LateralMeters;
                var aim=new WeaponAimCommand(tick,id,0,0);bool trigger=false,hold=false;var shooting=actor.ActiveShooting;
                if(shooting!=null)
                {
                    if(!ReferenceEquals(previousShooting[i],shooting))
                    { shooters[i]=new ShootingAiController(aimDefinition,shootingModel.Targets,shootingModel.Aim,shootingModel.Stances.Get(Field.Profile.GetBout(actor.ActiveBout).Stance),id,frequency,new SeededRandomSource(ActorSeed(id,actor.ActiveBout+1)),Field.Tick);previousShooting[i]=shooting; }
                    var sight=shooting.Sight;
                    shootingFrames[i]=shooters[i].Step(tick,new ShootingAiObservation(shooting.Phase,shooting.ClosedMask,shooting.ShotsFired,sight.YawRadians,sight.PitchRadians,
                        shooting.Tick.Equals(shooting.InitialTick)?1:shooting.LastFrame.Pressure.DecisionDelayMultiplier),strategies[i]);
                    aim=shootingFrames[i].Aim;trigger=shootingFrames[i].Trigger;hold=shootingFrames[i].HoldBreath;shootingDue[i]=shootingFrames[i].DecisionMade;
                }
                else shootingFrames[i]=default;
                inputs[i]=new SprintAthleteInput(new TimeTrialInput(paceFrames[i].Input,raceForms==null?1:raceForms[i],1,0,sprintRequested:intents[i].Kind==TacticalIntentKind.FinalSprint),aim,trigger,hold,pressure:racePressures==null?default:racePressures[i]);
            }
            Field.Step(inputs,lateral);journal.Flush(events);
            for(int i=0;i<Field.Count;i++)
            {
                if(tacticalDue[i])
                {
                    TacticalDecisions++;
                    if(pursuit!=null)
                    {
                        if(pursuitPlans[i].Reason!=publishedPursuitPlans[i].Reason)
                            events.Record(new RaceEvent(tick,frequency,Field.GetAthlete(i).AthleteId,RaceEventType.AiPursuitTacticsChanged,(int)pursuitPlans[i].Reason));
                        publishedPursuitPlans[i]=pursuitPlans[i];
                    }
                    if(!publishedIntents[i].AthleteId.IsValid || publishedIntents[i].Kind!=intents[i].Kind || !publishedIntents[i].TargetId.Equals(intents[i].TargetId))
                        events.Record(new RaceEvent(tick,frequency,Field.GetAthlete(i).AthleteId,RaceEventType.AiIntentChanged,(int)intents[i].Kind));
                    if(publishedStrategies[i].DefinitionId==null || publishedStrategies[i].Risk!=strategies[i].Risk)
                        events.Record(new RaceEvent(tick,frequency,Field.GetAthlete(i).AthleteId,RaceEventType.AiShootingPolicyChanged,(int)Math.Round(strategies[i].Risk*1000)));
                    publishedIntents[i]=intents[i];publishedStrategies[i]=strategies[i];Publish(i,tick,AiTraceKind.Tactical);
                }
                if(shootingDue[i]) { ShootingDecisions++;Publish(i,tick,AiTraceKind.Shooting); }
            }
        }
        private void Publish(int i,SimulationTick tick,AiTraceKind kind)=>telemetry.Record(new AiDecisionTrace(tick,Field.GetAthlete(i).AthleteId,kind,intents[i],utilities[i],draftFrames[i],overtakeFrames[i],recoveryFrames[i],strategies[i],paceFrames[i],shootingFrames[i],lateral[i]));
    }
}
