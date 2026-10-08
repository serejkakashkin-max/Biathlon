using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed partial class SprintAthleteSession
    {
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

        private void ValidateScopedSource(ManualScopedWorldRoster world,int index)
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
            var row=catalog.Get(cursor.OccurrenceIndex);int b=FirstUncompletedRange();var range=b<0?null:ranges[b];
            if(RemainingStartApproachNanometers>0)
            {
                if(row.Kind!=ManualRacePathKind.StartApproach||
                    row.LengthNanometers-cursor.CreditedArcNanometers!=RemainingStartApproachNanometers)
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
                (range==null||b!=row.BoutIndex||range.Phase!=RangeTraversalPhase.Approach||RouteNanometers<range.RangeEntryNanometers)||
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

        /// <summary>Bind one complete world proof within the accepted physical
        /// occurrence to real session progress. Cross-occurrence segments and
        /// lifecycle handoffs require the subsequent full phase transaction.</summary>
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
            var nextMotion=ledger.GetProposedPhysicalMotion(world,actorIndex);var row=before.Owner.Get(before.OccurrenceIndex);
            if(world.Tick.Value!=preview.Tick.Value||after.OccurrenceIndex!=before.OccurrenceIndex||
                after.CreditedRouteNanometers<RouteNanometers||after.CreditedRouteNanometers>finishNanometers||
                nextMotion.DistanceNanometers<Motion.DistanceNanometers)
                throw new ArgumentException("A complete same-occurrence world tick is required.");
            bool moving=preview.MovementActive;
            if(!moving&&(!SameScopedActor(initial,final)||!SameScopedMotion(nextMotion,Motion)||
                after.ObservedArcNanometers!=before.ObservedArcNanometers||after.CreditedArcNanometers!=before.CreditedArcNanometers))
                throw new ArgumentException("Waiting start or shooting must retain the exact stationary body and progress.");
            bool firing=moving&&row.End==ManualRacePathEnd.FiringLine&&after.ObservedArcNanometers==row.LengthNanometers;
            if(firing&&(final.Velocity.X!=0||final.Velocity.Z!=0||final.Pose.YawRateRadiansPerSecond!=0||nextMotion.SpeedMetersPerSecond!=0))
                throw new ArgumentException("Firing arrival must physically brake before session admission; no hidden velocity reset.");
            long rangeTravel=0;
            if(moving&&preparedRange!=null)
                rangeTravel=row.Kind==ManualRacePathKind.PenaltyLoop?after.CreditedArcNanometers-before.CreditedArcNanometers:
                    Math.Max(0,Math.Min(after.CreditedRouteNanometers,preparedRange.RangeRejoinNanometers)-
                        Math.Max(RouteNanometers,preparedRange.RangeEntryNanometers));
            double dt=1.0/config.TickFrequency;
            if(moving)segments[0]=new RaceMotionSegment(RouteNanometers,after.CreditedRouteNanometers,
                Motion.DistanceNanometers,nextMotion.DistanceNanometers,0,dt,
                row.Kind==ManualRacePathKind.PenaltyLoop,row.Kind==ManualRacePathKind.StartApproach);
            var movement=new RacePathMotionFrame(nextMotion,after.CreditedRouteNanometers,rangeTravel,dt,0,
                moving?1:0,firing,moving&&row.End==ManualRacePathEnd.Finish&&after.CreditedRouteNanometers==finishNanometers);
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
        private void CommitScopedStart(SprintStepPreview preview)
        {
            var ledger=preview.ScopedWorld.Owner;var before=ledger.GetCursor(preparedScopedIndex);
            if(before.Owner.Get(before.OccurrenceIndex).Kind!=ManualRacePathKind.StartApproach)return;
            var after=ledger.GetProposedCursor(preview.ScopedWorld,preparedScopedIndex);
            long remaining=before.Owner.Get(before.OccurrenceIndex).LengthNanometers-after.CreditedArcNanometers;
            if(RemainingStartApproachNanometers>0&&remaining==0)
            {
                StartApproachExitTime=FinishTime.InsideTick(preview.Tick,config.TickFrequency,preview.Movement.ConsumedSeconds);
                events.Record(new RaceEvent(preview.Tick,config.TickFrequency,AthleteId,RaceEventType.StartApproachCompleted,0));
            }
            manualStartRemaining=remaining;
        }
    }

    /// <summary>Prevalidate every same-occurrence session row, then publish
    /// sessions and the full world on one clock. Pack side state, field timing,
    /// contact event journal and cross-phase movement remain separate.</summary>
    public sealed class ManualScopedRosterSessionCommit
    {
        private readonly SprintAthleteSession[] athletes;
        private readonly SprintStepPreview[] resolved;
        private readonly ManualScopedWorldRoster world;
        public string ContextJson {get;}
        public ManualScopedRosterSessionCommit(ReadOnlySpan<SprintAthleteSession> roster,ManualScopedWorldRoster world)
        {
            if(world==null||roster.Length==0||roster.Length!=world.Count)throw new ArgumentException("Complete scoped race roster required.");
            this.world=world;athletes=roster.ToArray();resolved=new SprintStepPreview[roster.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_scoped_same_occurrence_session_commit_v1",world.ContextJson,
                "actual_surface;high_water_range_travel;start_high_water_distinct_from_odometer;full_roster_prevalidation;no_cross_occurrence_segments;no_legacy_resumption"});
            for(int i=0;i<athletes.Length;i++)
            {
                if(athletes[i]==null||athletes[i].HasFinished)throw new ArgumentException("Active athlete session required.");
                athletes[i].ValidateScopedRosterBinding(world,i);
            }
        }
        public void Commit(ReadOnlySpan<SprintStepPreview> pending,ManualScopedWorldPreview proposal)
        {
            if(pending.Length!=athletes.Length||!ReferenceEquals(proposal.Owner,world))throw new ArgumentException("Exact full world proof and race previews required.");
            world.ValidatePrepared(proposal);
            for(int i=0;i<athletes.Length;i++)resolved[i]=athletes[i].PrepareManualScopedCommit(pending[i],proposal,i);
            for(int i=0;i<athletes.Length;i++)athletes[i].ValidatePreparedCommit(resolved[i]);
            // As in the existing session batches, ordinary race event sinks are nonthrowing.
            for(int i=0;i<athletes.Length;i++)athletes[i].CommitStep(resolved[i]);
            world.Commit(proposal);
        }
    }
    public sealed partial class SprintAthleteSession
    {
        internal void ValidateScopedRosterBinding(ManualScopedWorldRoster world,int index)=>ValidateScopedSource(world,index);
    }
}
