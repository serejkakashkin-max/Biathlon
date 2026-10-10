using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed partial class SprintAthleteSession
    {
        /// <summary>Cold geometry bound to this session's exact route, format,
        /// current mat selection and authored start. No admission or movement.</summary>
        public ManualRacePathCatalog CreateManualPathCatalog(TrackDefinition track,TrackVisualDefinition visual,ManualFinishRunoutDefinition finishRunout=null)
            =>new ManualRacePathCatalog(track,route,profile,visual,physicalRangePath,startApproach,finishRunout);
        internal ManualRacePathCatalog CreateManualPathCatalogChoice(TrackDefinition track,TrackVisualDefinition visual,int laneOrdinal,ManualFinishRunoutDefinition finishRunout)
            =>new ManualRacePathCatalog(track,route,profile,visual,GetRangePathChoice(laneOrdinal-1),startApproach,finishRunout);

        /// <summary>Stage the selected resource path after the entire physical
        /// tick has been proved on its identical common prefix. Physiology and
        /// owned movement receipts are retained, never recomputed by legacy ski.</summary>
        internal void StageManualScopedRangeChoice(SprintStepPreview preview,int laneOrdinal)
        {
            ValidatePreview(preview);
            if(!preview.ScopedWorld.IsValid||preparedRangeIndex<0||preparedRangeIndex!=ActiveBout||
                preparedChoosesPath||pathChosenForBout[preparedRangeIndex]||!preview.MovementActive||
                ranges[preparedRangeIndex].Phase!=RangeTraversalPhase.Approach||
                preview.Movement.RouteNanometers<preparedRange.RangeEntryNanometers||
                preview.Movement.Motion.DistanceNanometers<=Motion.DistanceNanometers)
                throw new ArgumentException("A newly paid common range arrival requires an owned scoped resource choice.");
            var candidate=GetRangePathChoice(laneOrdinal-1);
            if(preview.Movement.RouteNanometers>=candidate.FirstForkRouteNanometers(profile.GetBout(preparedRangeIndex).AfterLap-1))
                throw new ArgumentException("Scoped path selection must precede every physical fork.");
            var stagedRange=rangePathVariants[preparedRangeIndex][laneOrdinal-1];
            if(!ReferenceEquals(stagedRange,ranges[preparedRangeIndex]))stagedRange.StageAcceptedStateFrom(ranges[preparedRangeIndex],RouteNanometers);
            preparedPath=candidate;preparedRange=stagedRange;
            preparedLane=new ShootingLaneAssignment(lane.RangeId,candidate.LaneId,candidate.LaneOrdinal);preparedChoosesPath=true;
        }
        private ManualScopedWorldRoster preparedScopedWorld;
        private ManualScopedWorldRoster acceptedScopedWorld;
        private int preparedScopedIndex=-1;
        private long? manualStartRemaining;

        /// <summary>Prepare physiology/posture at an exact accepted physical
        /// occurrence. Legacy route movement is never invoked. An owning full
        /// world proof is still required before any race state can commit.</summary>
        public SprintStepPreview PrepareManualScopedStep(SprintAthleteInput input,
            ManualScopedWorldRoster world,int actorIndex)
        {
            previewReady=false;
            ValidateScopedSource(world,actorIndex);
            return PreparePackStep(input,1,1,0,default,false,default,world,actorIndex);
        }

        private void ValidateScopedSource(ManualScopedWorldRoster world,int index,bool phaseHandoff=false)
        {
            if(world==null||index<0||index>=world.Count||world.Tick.Value!=Tick.Value||world.TickFrequency!=config.TickFrequency)
                throw new ArgumentException("Exact accepted scoped actor and race clock required.");
            var actor=world.GetActor(index);var cursor=world.GetCursor(index);var catalog=cursor.Owner;
            if(!actor.Athlete.Equals(AthleteId)||!cursor.Athlete.Equals(AthleteId)||
                !catalog.IsBoundTo(route,physicalRangePath,startApproach)||
                !ReferenceEquals(actor.Observation.Owner,catalog)||actor.Observation.OccurrenceIndex!=cursor.OccurrenceIndex||
                actor.Observation.PhysicalArcNanometers!=cursor.ObservedArcNanometers||
                cursor.CreditedRouteNanometers!=RouteNanometers||!SameScopedMotion(world.GetPhysicalMotion(index),Motion))
                throw new ArgumentException("Scoped body, selected path, race credit and physical motion must match this session.");
            if(Tick.Next().Value<=ScheduledStart.Value&&(actor.Velocity.X!=0||actor.Velocity.Z!=0||actor.Pose.YawRateRadiansPerSecond!=0))
                throw new ArgumentException("A scheduled athlete must retain a stationary start body.");
            if(phaseHandoff)return;
            var row=catalog.Get(cursor.OccurrenceIndex);int b=FirstUncompletedRange();var range=b<0?null:ranges[b];
            if(HasFinished)
            {
                if(row.Kind!=ManualRacePathKind.FinishRunout||RouteNanometers!=finishNanometers||range!=null)
                    throw new ArgumentException("A finished scoped athlete must own its post-finish physical runway.");
                return;
            }
            if(RemainingStartApproachNanometers>0)
            {
                long remaining=row.LengthNanometers-cursor.CreditedArcNanometers;
                // A curved offset grid can project onto the adjacent knot edge.
                // Only a complete field-authenticated, unchanged initial pose
                // may establish that canonical initial coordinate. It grants
                // no travel or base-route credit. Later receipts remain exact.
                bool initialProjection=acceptedScopedWorld==null&&world.IsVerifiedInitialSession(this);
                if(row.Kind!=ManualRacePathKind.StartApproach||
                    remaining!=RemainingStartApproachNanometers&&!initialProjection)
                    throw new ArgumentException("Accepted start high water must match remaining start approach.");
                return;
            }
            if(row.Kind==ManualRacePathKind.StartApproach)
                throw new ArgumentException("Completed start approach needs its ordinary handoff.");
            if(range!=null&&range.Phase==RangeTraversalPhase.Shooting)
            {
                int firing=catalog.GetFiringOccurrence(b);
                bool atMat=cursor.OccurrenceIndex==firing&&cursor.ObservedArcNanometers==row.LengthNanometers&&
                    cursor.CreditedArcNanometers==row.LengthNanometers||
                    cursor.OccurrenceIndex==catalog.GetBaseFlowSuccessor(firing)&&cursor.ObservedArcNanometers==0&&cursor.CreditedArcNanometers==0;
                if(!atMat||actor.Velocity.X!=0||actor.Velocity.Z!=0||actor.Pose.YawRateRadiansPerSecond!=0||Motion.SpeedMetersPerSecond!=0)
                    throw new ArgumentException("Shooting requires a stationary body at this exact mat occurrence.");
                return;
            }
            if(row.Kind==ManualRacePathKind.PenaltyLoop)
            {
                if(range==null||b!=row.BoutIndex||range.Phase!=RangeTraversalPhase.PenaltyLoop||
                    range.RequiredLoops-range.CompletedLoops<=0||row.PenaltyOrdinal!=range.CompletedLoops+1||
                    cursor.CreditedArcNanometers!=range.PenaltyNanometers)
                    throw new ArgumentException("Penalty occurrence must match actual bout debt and accepted circuit high water.");
                return;
            }
            if(row.Kind==ManualRacePathKind.RangeApproach&&
                (range==null||b!=row.BoutIndex||range.Phase!=RangeTraversalPhase.Approach||
                    RouteNanometers<range.RangeEntryNanometers&&(range.TravelledNanometers!=0||range.RouteNanometers!=range.RangeEntryNanometers))||
                row.Kind==ManualRacePathKind.RangeExit&&
                (range==null||b!=row.BoutIndex||range.Phase!=RangeTraversalPhase.RangeExit&&range.Phase!=RangeTraversalPhase.PenaltyApproach)||
                row.Kind==ManualRacePathKind.BaseRoute&&range!=null&&RouteNanometers>=range.RangeEntryNanometers&&
                (range.Phase!=RangeTraversalPhase.Rejoin&&range.Phase!=RangeTraversalPhase.Complete||RouteNanometers!=range.RouteNanometers))
                throw new ArgumentException("Physical occurrence must match accepted range lifecycle.");
        }

        /// <summary>Shared technique, snow/equipment, tuck and staged physiology
        /// at the actual scoped surface. Queries cannot grant travel or gates.</summary>
        public ManualSkiMotionInput GetPreparedManualMotionInput(SprintStepPreview preview,
            ManualScopedWorldRoster world,int actorIndex,int steeringInput,
            double dragMultiplier=1,double propulsionMultiplier=1,double windX=0,double windZ=0)
        {
            ValidatePreview(preview);
            if(!ReferenceEquals(preparedScopedWorld,world)||preparedScopedIndex!=actorIndex||
                !preview.ManualWorldRequired||!preview.MovementActive||preview.ShootingActive)
                throw new ArgumentException("Matching prepared moving scoped actor required.");
            ValidateScopedSource(world,actorIndex);
            var actor=world.GetActor(actorIndex);var at=actor.Observation;
            return path.PreviewManualInput(preview.Physiology,physiology.State.MuscularFatigue,
                preparedInput.Ski,preparedPosture,actor.Pose,at.GeometryProjection,
                at.Owner.SampleSurface(at),steeringInput,dragMultiplier,propulsionMultiplier,windX,windZ);
        }

        /// <summary>Resample force metadata at an owned provisional interval
        /// source. Staged physiology and the tick's posture decision are reused.</summary>
        public ManualSkiMotionInput GetPreparedManualMotionInput(SprintStepPreview preview,
            ManualScopedWorldRoster world,ManualScopedWorldTick tick,int actorIndex,int steeringInput,
            double dragMultiplier=1,double propulsionMultiplier=1,double windX=0,double windZ=0)
        {
            ValidatePreview(preview);
            if(!ReferenceEquals(preparedScopedWorld,world)||preparedScopedIndex!=actorIndex||
                !preview.ManualWorldRequired||!preview.MovementActive||preview.ShootingActive||tick.Tick.Value!=preview.Tick.Value)
                throw new ArgumentException("Matching prepared provisional interval required.");
            ValidateScopedSource(world,actorIndex);
            var actor=world.GetWorkingActor(tick,actorIndex);var at=actor.Observation;
            return path.PreviewManualInput(preview.Physiology,physiology.State.MuscularFatigue,
                preparedInput.Ski,preparedPosture,actor.Pose,at.GeometryProjection,
                at.Owner.SampleSurface(at),steeringInput,dragMultiplier,propulsionMultiplier,windX,windZ);
        }

        /// <summary>Bind complete owned world interval receipts to real session
        /// progress. Ordinary seams are supported; lifecycle handoffs remain
        /// separately authorized by the race phase owner.</summary>
        public SprintStepPreview PrepareManualScopedCommit(SprintStepPreview preview,
            ManualScopedWorldPreview world,int actorIndex)
        {
            ValidatePreview(preview);
            var ledger=world.Owner;
            if(!preview.ManualWorldRequired||!ReferenceEquals(ledger,preparedScopedWorld)||actorIndex!=preparedScopedIndex)
                throw new ArgumentException("Matching prepared scoped world required.");
            ValidateScopedSource(ledger,actorIndex);ledger.ValidatePrepared(world);
            var before=ledger.GetCursor(actorIndex);var after=ledger.GetProposedCursor(world,actorIndex);
            var initial=ledger.GetActor(actorIndex);var final=ledger.GetProposedActor(world,actorIndex);
            var nextMotion=ledger.GetProposedPhysicalMotion(world,actorIndex);var row=before.Owner.Get(after.OccurrenceIndex);
            if(world.Tick.Value!=preview.Tick.Value||
                after.CreditedRouteNanometers<RouteNanometers||after.CreditedRouteNanometers>finishNanometers||
                nextMotion.DistanceNanometers<Motion.DistanceNanometers)
                throw new ArgumentException("A complete owned world tick is required.");
            bool moving=preview.MovementActive;
            if(!moving&&(!SameScopedActor(initial,final)||!SameScopedMotion(nextMotion,Motion)||
                after.ObservedArcNanometers!=before.ObservedArcNanometers||after.CreditedArcNanometers!=before.CreditedArcNanometers))
                throw new ArgumentException("Waiting start or shooting must retain the exact stationary body and progress.");
            bool firing=moving&&row.End==ManualRacePathEnd.FiringLine&&after.ObservedArcNanometers==row.LengthNanometers;
            if(firing&&(final.Velocity.X!=0||final.Velocity.Z!=0||final.Pose.YawRateRadiansPerSecond!=0||nextMotion.SpeedMetersPerSecond!=0))
                throw new ArgumentException("Firing arrival must physically brake before session admission; no hidden velocity reset. athlete="+AthleteId+
                    " occurrence="+row.Index+" pose=("+final.Pose.X+","+final.Pose.Z+") heading="+final.Pose.HeadingRadians+
                    " velocity=("+final.Velocity.X+","+final.Velocity.Z+") spin="+final.Pose.YawRateRadiansPerSecond+
                    " arc="+after.ObservedArcNanometers+" length="+row.LengthNanometers+" lateral="+final.Observation.LateralMeters);
            long rangeTravel=0;int count=ledger.GetProposedMotionPieceCount(world);
            if(count<1||count>segments.Length)throw new ArgumentException("World intervals must fit the preallocated race motion receipts.");
            if(moving)for(int i=0;i<count;i++)
            {
                var piece=ledger.GetProposedMotionPiece(world,actorIndex,i);var occurrence=before.Owner.Get(piece.OccurrenceIndex);
                segments[i]=new RaceMotionSegment(piece.RouteBefore,piece.RouteAfter,piece.TravelBefore,piece.TravelAfter,
                    piece.StartSeconds,piece.EndSeconds,occurrence.Kind==ManualRacePathKind.PenaltyLoop,occurrence.Kind==ManualRacePathKind.StartApproach);
                if(preparedRange!=null)rangeTravel=checked(rangeTravel+ScopedRangeIncrement(piece,before.Owner,preparedRange));
            }
            double dt=1.0/config.TickFrequency;
            bool finish=false;
            if(moving&&!HasFinished)for(int i=0;i<count;i++)
            {
                var piece=ledger.GetProposedMotionPiece(world,actorIndex,i);var occurrence=before.Owner.Get(piece.OccurrenceIndex);
                if(occurrence.End==ManualRacePathEnd.Finish&&piece.CreditedArcBefore<occurrence.LengthNanometers&&
                    piece.CreditedArcAfter==occurrence.LengthNanometers)finish=true;
            }
            var movement=new RacePathMotionFrame(nextMotion,after.CreditedRouteNanometers,rangeTravel,dt,0,
                moving?count:0,firing,finish);
            previewRevision=checked(previewRevision+1);
            return new SprintStepPreview(this,previewRevision,preview.Tick,preview.Physiology,movement,moving,
                preview.ShootingActive,scopedWorld:world);
        }
        private static bool SameScopedActor(ManualRacePathActorFrame a,ManualRacePathActorFrame b)
            =>a.Athlete.Equals(b.Athlete)&&a.Pose.X==b.Pose.X&&a.Pose.Z==b.Pose.Z&&
                a.Pose.HeadingNanoradians==b.Pose.HeadingNanoradians&&a.Pose.YawRateRadiansPerSecond==b.Pose.YawRateRadiansPerSecond&&
                a.Velocity.X==b.Velocity.X&&a.Velocity.Z==b.Velocity.Z&&
                ReferenceEquals(a.Observation.Owner,b.Observation.Owner)&&
                a.Observation.OccurrenceIndex==b.Observation.OccurrenceIndex&&
                a.Observation.PhysicalArcNanometers==b.Observation.PhysicalArcNanometers&&
                a.Observation.LateralMeters==b.Observation.LateralMeters&&a.Observation.HeightMeters==b.Observation.HeightMeters;
        private static bool SameScopedMotion(SkiState a,SkiState b)
            =>a.DistanceNanometers==b.DistanceNanometers&&a.SpeedMetersPerSecond==b.SpeedMetersPerSecond;
        private double ScopedFinishSeconds(SprintStepPreview preview)
        {
            var world=preview.ScopedWorld.Owner;var catalog=world.GetCursor(preparedScopedIndex).Owner;
            for(int i=0;i<world.GetProposedMotionPieceCount(preview.ScopedWorld);i++)
            {
                var piece=world.GetProposedMotionPiece(preview.ScopedWorld,preparedScopedIndex,i);var row=catalog.Get(piece.OccurrenceIndex);
                if(row.End==ManualRacePathEnd.Finish&&piece.CreditedArcBefore<row.LengthNanometers&&piece.CreditedArcAfter==row.LengthNanometers)
                    return piece.EndSeconds;
            }
            throw new InvalidOperationException("Finish requires its exact paid incoming world interval.");
        }
        private static long ScopedRangeIncrement(ManualScopedWorldMotionPiece piece,ManualRacePathCatalog catalog,RangePenaltyTraversal range)
            =>catalog.Get(piece.OccurrenceIndex).Kind==ManualRacePathKind.PenaltyLoop?piece.CreditedArcAfter-piece.CreditedArcBefore:
                Math.Max(0,Math.Min(piece.RouteAfter,range.RangeRejoinNanometers)-Math.Max(piece.RouteBefore,range.RangeEntryNanometers));
        internal void RecordScopedPhysicalActions(ManualScopedWorldRoster ledger,int index)
        {
            int contacts=ledger.GetCourseEdgeResponses(index),captures=ledger.GetMatCaptures(index);
            if(contacts>0)events.Record(new RaceEvent(Tick,config.TickFrequency,AthleteId,RaceEventType.CourseBoundaryContact,contacts));
            if(captures>0)events.Record(new RaceEvent(Tick,config.TickFrequency,AthleteId,RaceEventType.ShootingMatCaptured,captures));
        }
        private void CommitScopedStart(SprintStepPreview preview)
        {
            var ledger=preview.ScopedWorld.Owner;var before=ledger.GetCursor(preparedScopedIndex);
            if(before.Owner.Get(before.OccurrenceIndex).Kind!=ManualRacePathKind.StartApproach)return;
            long remaining=RemainingStartApproachNanometers;double end=0;
            int count=ledger.GetProposedMotionPieceCount(preview.ScopedWorld);
            for(int i=0;i<count;i++)
            {
                var piece=ledger.GetProposedMotionPiece(preview.ScopedWorld,preparedScopedIndex,i);
                var row=before.Owner.Get(piece.OccurrenceIndex);if(row.Kind!=ManualRacePathKind.StartApproach)break;
                remaining=row.LengthNanometers-piece.CreditedArcAfter;end=piece.EndSeconds;
            }
            if(RemainingStartApproachNanometers>0&&remaining==0)
            {
                StartApproachExitTime=FinishTime.InsideTick(preview.Tick,config.TickFrequency,end);
                events.Record(new RaceEvent(preview.Tick,config.TickFrequency,AthleteId,RaceEventType.StartApproachCompleted,0));
            }
            manualStartRemaining=remaining;
        }
        internal bool TryPrepareAcceptedScopedPhase(ManualScopedWorldRoster world,int index,out int target)
        {
            target=-1;if(previewReady||HasFinished)return false;
            ValidateScopedSource(world,index,true);
            var c=world.GetCursor(index);var row=c.Owner.Get(c.OccurrenceIndex);var actor=world.GetActor(index);
            int b=row.BoutIndex;if(b<0||b>=ranges.Length)return false;
            var range=ranges[b];
            if(c.ObservedArcNanometers!=row.LengthNanometers||c.CreditedArcNanometers!=row.LengthNanometers||
                range.RouteNanometers!=RouteNanometers)return false;
            switch(row.End)
            {
                case ManualRacePathEnd.FiringLine:
                    if(range.Phase!=RangeTraversalPhase.RangeExit||shooting[b]?.Phase!=ShootingPhase.Complete||
                        Lifecycle.CompletedBouts!=b+1||actor.Velocity.X!=0||actor.Velocity.Z!=0||
                        actor.Pose.YawRateRadiansPerSecond!=0||Motion.SpeedMetersPerSecond!=0)return false;
                    target=c.Owner.GetBaseFlowSuccessor(row.Index);break;
                case ManualRacePathEnd.PenaltyDecision:
                    if(range.Phase==RangeTraversalPhase.PenaltyLoop&&range.PenaltyNanometers==0&&range.CompletedLoops==0&&range.RequiredLoops>0)
                        target=c.Owner.GetPenaltyOccurrence(b,1);
                    else if((range.Phase==RangeTraversalPhase.Rejoin||range.Phase==RangeTraversalPhase.Complete)&&range.RequiredLoops==0)
                        target=c.Owner.GetPenaltyReturnOccurrence(b);
                    else return false;
                    break;
                case ManualRacePathEnd.PenaltyCircuit:
                    if(range.CompletedLoops!=row.PenaltyOrdinal||range.PenaltyNanometers!=0)return false;
                    if(range.Phase==RangeTraversalPhase.PenaltyLoop&&range.CompletedLoops<range.RequiredLoops)
                        target=c.Owner.GetPenaltyOccurrence(b,row.PenaltyOrdinal+1);
                    else if((range.Phase==RangeTraversalPhase.Rejoin||range.Phase==RangeTraversalPhase.Complete)&&range.CompletedLoops==range.RequiredLoops)
                        target=c.Owner.GetPenaltyReturnOccurrence(b);
                    else return false;
                    break;
                default:return false;
            }
            return target>=0;
        }
        internal bool TryPrepareWorkingScopedPhase(SprintStepPreview preview,ManualScopedWorldRoster world,
            ManualScopedWorldTick tick,int index,out int target)
        {
            target=-1;ValidatePreview(preview);
            if(!ReferenceEquals(preparedScopedWorld,world)||preparedScopedIndex!=index||!preview.ManualWorldRequired||
                !preview.MovementActive||preview.ShootingActive||tick.Tick.Value!=preview.Tick.Value)return false;
            ValidateScopedSource(world,index);
            if(world.HasPendingContact(tick))return false;
            var cursor=world.GetWorkingCursor(tick,index);var row=cursor.Owner.Get(cursor.OccurrenceIndex);
            if(row.End==ManualRacePathEnd.Finish)
            {
                if(preparedRange!=null||cursor.Owner.FinishRunoutIndex<0||Lifecycle.Phase!=CompetitionPhase.Skiing||
                    Lifecycle.CompletedBouts!=profile.BoutCount||Lifecycle.CompletedLaps!=profile.LapCount-1||Lifecycle.PendingPenaltyLoops!=0||
                    cursor.ObservedArcNanometers!=row.LengthNanometers||cursor.CreditedArcNanometers!=row.LengthNanometers||
                    cursor.CreditedRouteNanometers!=finishNanometers)return false;
                target=cursor.Owner.FinishRunoutIndex;return true;
            }
            if(preparedRange==null)return false;
            if(row.BoutIndex!=preparedRangeIndex||cursor.ObservedArcNanometers!=row.LengthNanometers||
                cursor.CreditedArcNanometers!=row.LengthNanometers)return false;
            long travel=0;int completed=preparedRange.CompletedLoops;
            for(int i=0;i<world.GetWorkingMotionPieceCount(tick);i++)
            {
                var piece=world.GetWorkingMotionPiece(tick,index,i);var at=cursor.Owner.Get(piece.OccurrenceIndex);
                travel=checked(travel+ScopedRangeIncrement(piece,cursor.Owner,preparedRange));
                if(at.Kind==ManualRacePathKind.PenaltyLoop&&piece.CreditedArcAfter==at.LengthNanometers&&piece.CreditedArcBefore<at.LengthNanometers)completed++;
            }
            var future=preparedRange.PreviewMotion(travel);
            if(future.RouteNanometers!=cursor.CreditedRouteNanometers||future.PenaltyNanometers!=0)return false;
            if(row.End==ManualRacePathEnd.PenaltyDecision)
            {
                if(future.Phase==RangeTraversalPhase.PenaltyLoop&&completed==0&&preparedRange.RequiredLoops>0)
                    target=cursor.Owner.GetPenaltyOccurrence(preparedRangeIndex,1);
                else if((future.Phase==RangeTraversalPhase.Rejoin||future.Phase==RangeTraversalPhase.Complete)&&preparedRange.RequiredLoops==0)
                    target=cursor.Owner.GetPenaltyReturnOccurrence(preparedRangeIndex);
            }
            else if(row.End==ManualRacePathEnd.PenaltyCircuit&&row.PenaltyOrdinal==completed)
            {
                if(future.Phase==RangeTraversalPhase.PenaltyLoop&&completed<preparedRange.RequiredLoops)
                    target=cursor.Owner.GetPenaltyOccurrence(preparedRangeIndex,completed+1);
                else if((future.Phase==RangeTraversalPhase.Rejoin||future.Phase==RangeTraversalPhase.Complete)&&completed==preparedRange.RequiredLoops)
                    target=cursor.Owner.GetPenaltyReturnOccurrence(preparedRangeIndex);
            }
            return target>=0;
        }
    }

    /// <summary>Prevalidate every session's owned motion intervals, then publish
    /// sessions, field timing, Pack side state and the full world on one clock.
    /// Reusable mat ownership and the contact event journal remain separate.</summary>
    public sealed class ManualScopedRosterSessionCommit
    {
        private readonly SprintAthleteSession[] athletes;
        private readonly SprintStepPreview[] resolved;
        private readonly ManualScopedWorldRoster world;
        private readonly int[] phaseTargets;
        public string ContextJson {get;}
        public ManualScopedRosterSessionCommit(ReadOnlySpan<SprintAthleteSession> roster,ManualScopedWorldRoster world,bool compactContext=false)
        {
            if(world==null||roster.Length==0||roster.Length!=world.Count)throw new ArgumentException("Complete scoped race roster required.");
            this.world=world;athletes=roster.ToArray();resolved=new SprintStepPreview[roster.Length];phaseTargets=new int[roster.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{compactContext?"manual_scoped_interval_session_commit_v5_context_reference":"manual_scoped_interval_session_commit_v4",
                compactContext?"world_context_sha256:"+ManualContextIdentity.Sha256(world.ContextJson):world.ContextJson,
                "actual_surface;high_water_range_travel;start_high_water_distinct_from_odometer;full_roster_prevalidation;owned_interval_segments;ordinary_seam_continuation;no_legacy_resumption;accepted_completed_shooting_departure;actual_penalty_debt_and_ordinal;zero_time_same_pose_phase_handoff;event_free_future_penalty_preview;paid_receipt_completed_circuits;full_clock_lifecycle_publication"});
            for(int i=0;i<athletes.Length;i++)
            {
                if(athletes[i]==null||athletes[i].HasFinished)throw new ArgumentException("Active athlete session required.");
                athletes[i].ValidateScopedRosterBinding(world,i);
            }
        }
        public void Commit(ReadOnlySpan<SprintStepPreview> pending,ManualScopedWorldPreview proposal)
        {
            Resolve(pending,proposal);
            // As in the existing session batches, ordinary race event sinks are nonthrowing.
            for(int i=0;i<athletes.Length;i++)athletes[i].CommitStep(resolved[i]);
            world.Commit(proposal);
        }
        private void Resolve(ReadOnlySpan<SprintStepPreview> pending,ManualScopedWorldPreview proposal)
        {
            if(pending.Length!=athletes.Length||!ReferenceEquals(proposal.Owner,world))throw new ArgumentException("Exact full world proof and race previews required.");
            world.ValidatePrepared(proposal);
            for(int i=0;i<athletes.Length;i++)resolved[i]=athletes[i].PrepareManualScopedCommit(pending[i],proposal,i);
            for(int i=0;i<athletes.Length;i++)athletes[i].ValidatePreparedCommit(resolved[i]);
        }
        public void CommitField(SprintFieldSession field,ReadOnlySpan<SprintAthleteInput> inputs,
            ReadOnlySpan<SprintStepPreview> pending,ManualScopedWorldPreview proposal)
        {
            ValidateField(field);field.ValidateCommands(inputs);Resolve(pending,proposal);
            field.PublishManualScopedPrepared(inputs,resolved,proposal);world.Commit(proposal);
        }
        public void CommitPackField(PackSprintFieldSession field,ReadOnlySpan<SprintAthleteInput> inputs,
            ReadOnlySpan<SprintStepPreview> pending,ManualScopedWorldPreview proposal,ReadOnlySpan<DraftOutput> drafts)
        {
            ValidateField(field);field.ValidateManualScopedPublication(proposal,drafts);Resolve(pending,proposal);
            var resource=field.PrepareManualScopedReservations(resolved,proposal);
            field.CommitManualScopedPrepared(inputs,resolved,proposal,drafts,resource);
            for(int i=0;i<athletes.Length;i++)athletes[i].RecordScopedPhysicalActions(world,i);
        }
        private void ValidateField(ISprintFieldView field)
        {
            if(field==null||field.Count!=athletes.Length||field.Tick.Value!=world.Tick.Value)
                throw new ArgumentException("Exact active scoped field clock and roster required.");
            for(int i=0;i<athletes.Length;i++)if(!ReferenceEquals(field.GetAthlete(i),athletes[i]))
                throw new ArgumentException("Scoped world and field must share sessions in exact order.");
        }
        /// <summary>Zero-time phase handoff backed by the actual accepted bout
        /// or penalty traversal. All requested rows validate before any changes.
        /// In-tick future lifecycle authorization is a separate transaction.</summary>
        public bool TryHandoffAcceptedPhases(ReadOnlySpan<int> actorIndexes)
        {
            if(actorIndexes.Length==0||actorIndexes.Length>athletes.Length)return false;
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];if(index<0||index>=athletes.Length)throw new ArgumentOutOfRangeException(nameof(actorIndexes));
                for(int j=0;j<i;j++)if(actorIndexes[j]==index)throw new ArgumentException("Duplicate phase actor.");
                if(!athletes[index].TryPrepareAcceptedScopedPhase(world,index,out phaseTargets[i]))return false;
            }
            return world.TryHandoffAcceptedPhases(actorIndexes,phaseTargets.AsSpan(0,actorIndexes.Length));
        }
        public bool TryHandoffWorkingPhases(ManualScopedWorldTick tick,ReadOnlySpan<SprintStepPreview> pending,ReadOnlySpan<int> actorIndexes)
        {
            if(pending.Length!=athletes.Length||actorIndexes.Length==0||actorIndexes.Length>athletes.Length)return false;
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];if(index<0||index>=athletes.Length)throw new ArgumentOutOfRangeException(nameof(actorIndexes));
                for(int j=0;j<i;j++)if(actorIndexes[j]==index)throw new ArgumentException("Duplicate provisional phase actor.");
                if(!athletes[index].TryPrepareWorkingScopedPhase(pending[index],world,tick,index,out phaseTargets[i]))return false;
            }
            return world.TryHandoffWorkingPhases(tick,actorIndexes,phaseTargets.AsSpan(0,actorIndexes.Length));
        }
    }
    public sealed partial class SprintAthleteSession
    {
        internal void ValidateScopedRosterBinding(ManualScopedWorldRoster world,int index)
        {
            if(!TryPrepareAcceptedScopedPhase(world,index,out _))ValidateScopedSource(world,index);
        }
    }
}
