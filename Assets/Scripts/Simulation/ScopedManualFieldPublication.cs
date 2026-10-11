using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed partial class SprintFieldSession
    {
        private ManualScopedWorldRoster manualScopedWorld;
        internal void PublishManualScopedPrepared(ReadOnlySpan<SprintAthleteInput> inputs,
            ReadOnlySpan<SprintStepPreview> prepared,ManualScopedWorldPreview proof)
        {
            var world=proof.Owner;
            if(world==null||prepared.Length!=Count||world.Count!=Count||world.Tick.Value!=Tick.Value||
                world.TickFrequency!=frequency||manualScopedWorld!=null&&!ReferenceEquals(manualScopedWorld,world))
                throw new ArgumentException("Exact scoped world and complete active field required.");
            ValidateCommands(inputs);world.ValidatePrepared(proof);
            for(int i=0;i<Count;i++)
            {
                if(!ReferenceEquals(prepared[i].ScopedWorld.Owner,world)||prepared[i].ScopedWorld.Revision!=proof.Revision)
                    throw new ArgumentException("Every active field row requires the same scoped world proof.");
                GetAthlete(i).ValidatePreparedCommit(prepared[i]);
            }
            string next=ContextJson;
            if(manualScopedWorld==null)
            {
                string extra=ShootingModelDefinitions.EncodeContext("manual_scoped_field_clock_publication_v2_context_reference","world_context_sha256:"+ManualContextIdentity.Sha256(world.ContextJson),world.InitialStateContextJson);
                next=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
            }
            CommitPrepared(inputs,prepared,advanceScopedFinishers:true);manualScopedWorld=world;ContextJson=next;
        }
    }

    public sealed partial class PackSprintFieldSession
    {
        private ManualScopedWorldRoster manualScopedWorld;
        private readonly ManualRacePathActorFrame[] manualScopedFrames;
        public bool HasManualScopedWorldState=>manualScopedWorld!=null;
        public bool IsRangeTransitYielding(int index)=>manualScopedWorld!=null&&manualScopedWorld.IsRangeTransitYielding(index);
        internal DraftOutput EvaluateScopedWorldDraft(ManualSkiContactBody follower,ReadOnlySpan<ManualSkiContactBody> roster,
            TrackSample surface,double windX,double windZ)
            =>drafting.EvaluateWorld(follower,roster,surface,windX,windZ);
        internal void ValidateManualScopedCommands(ReadOnlySpan<SprintAthleteInput> inputs)=>field.ValidateCommands(inputs);
        public ManualRacePathActorFrame GetManualScopedWorldActor(int index)
        {
            if(manualScopedWorld==null)throw new InvalidOperationException("No accepted scoped manual world state.");
            return index>=0&&index<Count?manualScopedFrames[index]:throw new ArgumentOutOfRangeException(nameof(index));
        }
        internal void ValidateManualScopedPublication(ManualScopedWorldPreview proof,ReadOnlySpan<DraftOutput> nextDrafts)
        {
            var world=proof.Owner;
            if(world==null||world.Count!=Count||world.TickFrequency!=frequency||world.Tick.Value!=Tick.Value||
                world.BodyContextJson!=manualBodyContext||nextDrafts.Length!=Count||manualWorld!=null||
                manualScopedWorld!=null&&!ReferenceEquals(manualScopedWorld,world))
                throw new ArgumentException("Exact complete scoped Pack world/body/clock/draft roster required.");
            world.ValidatePrepared(proof);
            // Reusable mat reservation/path changes still need their own owned
            // transaction. This bridge cannot bypass that resource lifecycle.
            if(reservations!=null&&world.PathChoiceCount!=reservations.LaneCount)throw new NotSupportedException("Scoped reusable mat ownership requires its attested cold family.");
            for(int i=0;i<Count;i++)
            {
                var a=GetAthlete(i);var source=world.GetActor(i);var target=world.GetProposedActor(proof,i);
                if(!source.Athlete.Equals(a.AthleteId))throw new ArgumentException("Exact active scoped roster required.");
                a.ValidateScopedRosterBinding(world,i);
                if(manualScopedWorld==null)
                {
                    var point=CurrentPoint(i);var at=source.Observation;
                    long local=(long)Math.Round(at.LocalMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven);
                    if(point.Closed&&local==point.LoopLengthNanometers)local=0;
                    // The unstarted front grid row is recorded on the incoming
                    // authored endpoint by legacy Pack. Its exact selected main
                    // successor has the same physical pose. Recognize only this
                    // already validated directed start seam; no arbitrary loop
                    // alias or presentation coordinate correction is allowed.
                    bool initialStartSeam=world.Tick.Value==0&&startOverlay!=null&&startOverlay.UsesAuthoredBranch&&
                        a.RemainingStartApproachNanometers==0&&a.RouteNanometers==0&&
                        point.LoopId==StartApproach.LoopId&&point.LocalNanometers==StartApproach.LengthNanometers&&
                        at.LoopId==startOverlay.MainLoopId&&local==0;
                    if(!world.IsVerifiedInitialField(this)&&(!initialStartSeam&&(point.LoopId!=at.LoopId||Math.Abs(point.LocalNanometers-local)>2)||
                        Math.Abs(lateral[i]-at.LateralMeters)>2*PhysiologyNumbers.MinimumPositive))
                        throw new ArgumentException("Initial scoped coordinates must match the accepted physical Pack position: "+
                            a.AthleteId+"; pack="+point.LoopId+":"+point.LocalNanometers+":"+lateral[i].ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
                            "; scoped="+at.LoopId+":"+local+":"+at.LateralMeters.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                }
                else if(!SameScopedPoseAndVelocity(source,manualScopedFrames[i]))
                    throw new ArgumentException("Scoped accepted Pack body changed outside its owning world publication.");
                PhysiologyNumbers.RequireRange(nextDrafts[i].Strength,0,1,nameof(nextDrafts));
                PhysiologyNumbers.RequireRange(nextDrafts[i].DragReduction,0,1,nameof(nextDrafts));
                var observation=target.Observation;var row=world.GetProposedCursor(proof,i).Owner.Get(observation.OccurrenceIndex);
                preparedManualPoints[i]=map.MapLoop(row.LoopId,checked(row.LocalStartNanometers+observation.PhysicalArcNanometers));
            }
        }
        private static bool SameScopedPoseAndVelocity(ManualRacePathActorFrame a,ManualRacePathActorFrame b)
            =>a.Athlete.Equals(b.Athlete)&&a.Pose.X==b.Pose.X&&a.Pose.Z==b.Pose.Z&&a.Pose.HeadingNanoradians==b.Pose.HeadingNanoradians&&
                a.Pose.YawRateRadiansPerSecond==b.Pose.YawRateRadiansPerSecond&&a.Velocity.X==b.Velocity.X&&a.Velocity.Z==b.Velocity.Z;
        internal void CommitManualScopedPrepared(ReadOnlySpan<SprintAthleteInput> inputs,ReadOnlySpan<SprintStepPreview> prepared,
            ManualScopedWorldPreview proof,ReadOnlySpan<DraftOutput> nextDrafts,PhysicalRangeReservationPreview resource=default,PhysicalRangeArrivalPreview rangeOrder=default)
        {
            ValidateManualScopedPublication(proof,nextDrafts);field.ValidateCommands(inputs);
            if(reservations!=null)reservations.ValidatePreview(resource);
            if(pathArrivalOrder!=null)pathArrivalOrder.ValidatePreview(rangeOrder);
            string next=ContextJson;
            if(manualScopedWorld==null)
            {
                string extra=ShootingModelDefinitions.EncodeContext("manual_scoped_pack_publication_v2_context_reference","world_context_sha256:"+ManualContextIdentity.Sha256(proof.Owner.ContextJson),proof.Owner.InitialStateContextJson);
                next=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
            }
            field.PublishManualScopedPrepared(inputs,prepared,proof);
            for(int i=0;i<Count;i++)
            {
                manualScopedFrames[i]=proof.Owner.GetProposedActor(proof,i);manualPoints[i]=preparedManualPoints[i];
                lateral[i]=manualScopedFrames[i].Observation.LateralMeters;committedDrafts[i]=nextDrafts[i];
                initialBlocks[i]=default;junctionWaitSince[i]=0;tails[i]=default;
                onCourse[i]=StartGrid!=null||field.Tick.Value>=GetAthlete(i).ScheduledStart.Value;
            }
            proof.Owner.Commit(proof);CommitManualScopedReservations(resource);
            CommitManualScopedRangeOrder(rangeOrder);
            for(int i=0;i<Count;i++)manualScopedFrames[i]=proof.Owner.GetActor(i);
            manualScopedWorld=proof.Owner;ContextJson=next;
        }
    }
}
