using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualFieldStepStatus
    {
        NotStarted, Complete, WorldRejected, PhaseRequired, MovingFiringArrival, FinishedRosterUnsupported, MotionPieceLimit
    }
    public readonly struct ManualFieldStepResult
    {
        public ManualFieldStepStatus Status {get;}
        public SimulationTick Tick {get;}
        public AthleteId Athlete {get;}
        public ManualScopedWorldAttempt WorldAttempt {get;}
        public double ProvisionalSeconds {get;}
        public int Intervals {get;}
        internal ManualFieldStepResult(ManualFieldStepStatus status,SimulationTick tick,AthleteId athlete,
            ManualScopedWorldAttempt attempt,double seconds,int intervals)
        {Status=status;Tick=tick;Athlete=athlete;WorldAttempt=attempt;ProvisionalSeconds=seconds;Intervals=intervals;}
    }
    /// <summary>Composes staged physiology, signed yaw, shared ski/contact forces,
    /// owned first events and session publication on one fixed field clock.
    /// Failure aborts the provisional clock; it never pays a stationary queue.
    /// This composition deliberately rejects missing lifecycle/resource authority.
    /// It is not selected by the production host until complete-race qualification.</summary>
    public sealed class ManualScopedFieldStepper
    {
        private readonly PackSprintFieldSession field;
        private readonly ManualScopedWorldRoster world;
        private readonly ManualScopedRosterSessionCommit commit;
        private readonly ManualSkiControlledDriveSystem drive;
        private readonly ManualSkiContactDriveSystem stationary;
        private readonly SkiPhysicsDefinition physics;
        public SkiPhysicsDefinition PhysicsDefinition=>physics;
        public CorneringDefinition CorneringDefinition {get;}
        public OccupancyDefinition BodyDefinition {get;}
        private readonly SprintStepPreview[] pending;
        private readonly SkiDrivenContactTrajectory[] drives;
        private readonly SkiWorldMotionCandidate[] candidates;
        private readonly ManualScopedWorldWindow[] windows;
        private readonly double[] grades;
        private readonly double[] matStops;
        private readonly ManualMatBrakingDefinition matBraking;
        private readonly DraftOutput[] drafts;
        private readonly ManualSkiContactBody[] draftBodies;
        private readonly int[] ordinary,phases,held;
        private readonly int maximumIntervals;
        public string SteeringDefinitionJson {get;}
        public double BodyEdgeMargin {get;}
        public double BodyLongitudinalSeparation {get;}
        public double BodyCourseRadius {get;}
        public string ContextJson {get;}
        public ManualFieldStepResult LastStep {get;private set;}
        public ManualRacePathActorFrame LastRejectedWorkingActor {get;private set;}
        public ManualScopedWorldRoster World=>world;
        public PackSprintFieldSession Field=>field;

        /// <summary>Bind an unchanged initial field, including every outer grid
        /// row. No paid legacy setup tick, inward shift or body shrink is used.
        /// Later adoption requires an explicit phase/high-water reconstitution.</summary>
        public static ManualScopedWorldRoster CreateInitialWorld(PackSprintFieldSession field,
            TrackDefinition track,TrackVisualDefinition visual,OccupancyDefinition body,
            SkiContactDefinition contact,SkiContactSweepDefinition work,SkiContactGroupDefinition group,
            SkiPhysicsDefinition physics,ManualFinishRunoutDefinition finishRunout=null)
        {
            if(field==null||track==null||visual==null||body==null||contact==null||work==null||group==null||physics==null||
                field.Tick.Value!=0||field.UsesReusableRangeMats)
                throw new ArgumentException("Exact initial non-reusable field and owned policies required.");
            int count=field.Count;
            var catalogs=new ManualRacePathCatalog[count];var cursors=new ManualRacePathCursor[count];
            var poses=new ManualSkiHeadingState[count];var velocities=new SkiPlanarVelocity[count];
            var motions=new SkiState[count];var masses=new double[count];
            for(int i=0;i<count;i++)
            {
                var a=field.GetAthlete(i);var c=catalogs[i]=a.CreateManualPathCatalog(track,visual,finishRunout);
                bool inStart=c.StartApproachIndex>=0&&a.RemainingStartApproachNanometers>0;
                int occurrence=inStart?c.StartApproachIndex:c.StartApproachIndex>=0?c.GetBaseFlowSuccessor(c.StartApproachIndex):0;
                long arc=inStart?c.Get(occurrence).LengthNanometers-a.RemainingStartApproachNanometers:0;
                if(arc<0||!c.TrySample(occurrence,arc,out var p)||a.RouteNanometers!=0||a.Motion.SpeedMetersPerSecond!=0)
                    throw new ArgumentException("The actual initial grid must lie in its authored physical start occurrence.");
                double lateral=field.GetLateral(i);
                poses[i]=new ManualSkiHeadingState(p.X+p.ForwardZ*lateral,p.Z-p.ForwardX*lateral,Math.Atan2(p.ForwardX,p.ForwardZ));
                if(!c.TryProject(poses[i],occurrence,arc,Math.Abs(lateral)+body.LongitudinalSeparation,out var observation))
                    throw new ArgumentException("Encoded initial body must project onto its owned start occurrence.");
                long high=inStart?observation.PhysicalArcNanometers:arc;
                cursors[i]=new ManualRacePathProgression(c).Reconstitute(a.AthleteId,occurrence,observation.PhysicalArcNanometers,high,0);
                motions[i]=a.Motion;masses[i]=physics.MassKilograms;
            }
            var result=new ManualScopedWorldRoster(catalogs,body,contact,work,cursors,poses,velocities,motions,
                field.TickFrequency,field.Tick,group,masses);
            result.BindInitialField(field);return result;
        }

        public ManualScopedFieldStepper(PackSprintFieldSession field,ManualScopedWorldRoster world,
            SkiModelDefinitions models,OccupancyDefinition body,ManualSkiSteeringDefinition steering,
            SkiContactSweepDefinition work,ManualMatBrakingDefinition matBraking=null)
        {
            this.field=field??throw new ArgumentNullException(nameof(field));
            this.world=world??throw new ArgumentNullException(nameof(world));
            if(models==null||body==null||steering==null||work==null||field.Count!=world.Count||
                field.TickFrequency!=world.TickFrequency||field.Tick.Value!=world.Tick.Value||
                world.BodyContextJson!=body.ToJson()||world.WorkContextJson!=work.ToJson()||!world.HasContactForcePolicy)
                throw new ArgumentException("Exact field/world/model composition required.");
            if(field.UsesReusableRangeMats)
                throw new NotSupportedException("Reusable mat transactions must be integrated before selecting scoped movement.");
            var sessions=new SprintAthleteSession[field.Count];
            for(int i=0;i<sessions.Length;i++)sessions[i]=field.GetAthlete(i);
            commit=new ManualScopedRosterSessionCommit(sessions,world,compactContext:true);
            physics=models.Physics;maximumIntervals=world.MaximumMotionPieces;
            CorneringDefinition=models.Cornering;BodyDefinition=body;
            SteeringDefinitionJson=steering.ToJson();BodyEdgeMargin=body.LateralSeparation*.5+body.EdgeClearance;
            BodyLongitudinalSeparation=body.LongitudinalSeparation;
            BodyCourseRadius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)+body.EdgeClearance;
            this.matBraking=matBraking;
            drive=new ManualSkiControlledDriveSystem(physics,body,steering,models.Cornering,field.TickFrequency);
            stationary=new ManualSkiContactDriveSystem(physics,body,field.TickFrequency);
            int count=field.Count;
            pending=new SprintStepPreview[count];drives=new SkiDrivenContactTrajectory[count];
            candidates=new SkiWorldMotionCandidate[count];windows=new ManualScopedWorldWindow[count];
            grades=new double[count];matStops=new double[count];drafts=new DraftOutput[count];
            draftBodies=new ManualSkiContactBody[count];
            ordinary=new int[count];phases=new int[count];held=new int[count];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_scoped_field_stepper_v5_explicit_finish_runout","world_context_sha256:"+ManualContextIdentity.Sha256(world.ContextJson),
                commit.ContextJson,drive.ContextJson,work.ToJson(),matBraking?.ToJson()??"terminal_mat_braking_not_selected",
                "all_signed_intents;bounded_brake_fractions_validated_before_preparation;one_fixed_clock;owned_force_pieces;actual_contact_response;ordinary_and_paid_penalty_handoffs;stationary_firing_hold;explicit_terminal_mat_inverse_shared_force;prepared_physiology_paid_without_propulsion_bonus;abort_not_queue;no_resource_fallback;selected_finish_runout_shared_braking_and_clock;world_drafting_actual_positions_and_leader_velocity_axis;unchanged_pack_draft_definition;world_wind_from_observed_route_and_scalar_headwind;no_station_or_desired_line_wake"});
        }

        public bool TryStep(ReadOnlySpan<SprintAthleteInput> inputs,ReadOnlySpan<int> steeringInputs)
            =>TryStep(inputs,steeringInputs,ReadOnlySpan<double>.Empty);

        public bool TryStep(ReadOnlySpan<SprintAthleteInput> inputs,ReadOnlySpan<int> steeringInputs,
            ReadOnlySpan<double> brakeFractions)
        {
            if(inputs.Length!=field.Count||steeringInputs.Length!=field.Count||
                brakeFractions.Length!=0&&brakeFractions.Length!=field.Count||field.Tick.Value!=world.Tick.Value)
                throw new ArgumentException("Complete signed roster and exact clock required.");
            field.ValidateManualScopedCommands(inputs);
            // Validate even the last row before preparing any physiology or a
            // zero-time accepted phase transition.
            for(int i=0;i<field.Count;i++)
            {
                if(brakeFractions.Length!=0)PhysiologyNumbers.RequireRange(brakeFractions[i],0,1,nameof(brakeFractions));
                if(field.GetAthlete(i).HasFinished&&world.GetCatalog(i).Get(world.GetCursor(i).OccurrenceIndex).Kind!=ManualRacePathKind.FinishRunout)
                    return Report(ManualFieldStepStatus.FinishedRosterUnsupported,default,field.GetAthlete(i).AthleteId);
                if(steeringInputs[i]<-ManualSkiCommand.SteeringUnits||steeringInputs[i]>ManualSkiCommand.SteeringUnits||
                    inputs[i].Ski.Effort.Tick.Value!=field.Tick.Next().Value||
                    !inputs[i].Ski.Effort.AthleteId.Equals(field.GetAthlete(i).AthleteId))
                    throw new ArgumentException("Every row requires next-tick effort and bounded signed yaw for its actual athlete.");
            }
            int acceptedPhases=0;
            for(int i=0;i<field.Count;i++)
            {
                var c=world.GetCursor(i);var row=c.Owner.Get(c.OccurrenceIndex);
                if(c.ObservedArcNanometers==row.LengthNanometers&&row.End!=ManualRacePathEnd.Continue&&
                    field.GetAthlete(i).TryPrepareAcceptedScopedPhase(world,i,out _))phases[acceptedPhases++]=i;
            }
            if(acceptedPhases>0&&!commit.TryHandoffAcceptedPhases(phases.AsSpan(0,acceptedPhases)))
                return Report(ManualFieldStepStatus.PhaseRequired,default);
            for(int i=0;i<field.Count;i++)pending[i]=field.GetAthlete(i).PrepareManualScopedStep(inputs[i],world,i);
            var tick=world.BeginTick();bool published=false;ManualScopedWorldPreview proof=default;
            try
            {
                if(!world.TryResolveOwnedCurrentContacts(tick))return Report(ManualFieldStepStatus.WorldRejected,tick);
                while(world.GetRemainingSeconds(tick)>0)
                {
                    if(!world.TryResolveOwnedCurrentContacts(tick))return Report(ManualFieldStepStatus.WorldRejected,tick);
                    if(world.GetWorkingMotionPieceCount(tick)>=maximumIntervals)
                        return Report(ManualFieldStepStatus.MotionPieceLimit,tick);
                    double seconds=world.GetRemainingSeconds(tick);int heldCount=0;
                    int draftCount=0;
                    for(int i=0;i<field.Count;i++)
                    {
                        var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);
                        var row=cursor.Owner.Get(cursor.OccurrenceIndex);
                        if(pending[i].MovementActive&&!(row.End==ManualRacePathEnd.FiringLine&&cursor.ObservedArcNanometers==row.LengthNanometers))
                            draftBodies[draftCount++]=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,physics.MassKilograms);
                    }
                    for(int i=0;i<field.Count;i++)
                    {
                        drafts[i]=default;if(!pending[i].MovementActive)continue;
                        var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);var row=cursor.Owner.Get(cursor.OccurrenceIndex);
                        if(row.End==ManualRacePathEnd.FiringLine&&cursor.ObservedArcNanometers==row.LengthNanometers)continue;
                        var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,physics.MassKilograms);
                        double wind=inputs[i].Ski.HeadwindMetersPerSecond;
                        drafts[i]=field.EvaluateScopedWorldDraft(body,draftBodies.AsSpan(0,draftCount),cursor.Owner.SampleSurface(actor.Observation),
                            -wind*actor.Observation.ForwardX,-wind*actor.Observation.ForwardZ);
                    }
                    // This explicitly selected last strip may request neutral
                    // braking on the athlete's owned mat only. It removes no
                    // velocity or physiology cost and grants no course/phase
                    // admission. Outside it the supplied signed intent remains.
                    for(int i=0;i<field.Count;i++)
                    {
                        matStops[i]=0;if(matBraking==null||!pending[i].MovementActive)continue;
                        var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);
                        var row=cursor.Owner.Get(cursor.OccurrenceIndex);
                        if(row.End!=ManualRacePathEnd.FiringLine||cursor.ObservedArcNanometers==row.LengthNanometers||
                            !cursor.Owner.TrySamplePortal(row.Index,row.LengthNanometers,out var goal))continue;
                        double distance=(goal.X-actor.Pose.X)*goal.ForwardX+(goal.Z-actor.Pose.Z)*goal.ForwardZ;
                        double speed=actor.Velocity.X*goal.ForwardX+actor.Velocity.Z*goal.ForwardZ;
                        if(distance<=0||distance>matBraking.TerminalDistanceMeters||speed<=0)continue;
                        double after=2*distance/speed;
                        if(after<PhysiologyNumbers.MinimumPositive||after>PhysiologyNumbers.MaximumMagnitude)continue;
                        var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,physics.MassKilograms);
                        var motion=new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond);
                        double wind=inputs[i].Ski.HeadwindMetersPerSecond;
                        var input=Neutral(field.GetAthlete(i).GetPreparedManualMotionInput(pending[i],world,tick,i,0,drafts[i].DragAreaMultiplier,
                            windX:-wind*actor.Observation.ForwardX,windZ:-wind*actor.Observation.ForwardZ));
                        var preview=drive.PreviewStopping(body,motion,actor.Observation,cursor.Owner.SampleSurface(actor.Observation),input,
                            Math.Min(seconds,after),after);
                        if(!preview.WithinBrakingBudget)continue;
                        matStops[i]=after;seconds=Math.Min(seconds,after);
                    }
                    for(int i=0;i<field.Count;i++)
                    {
                        var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);
                        var catalog=cursor.Owner;var row=catalog.Get(cursor.OccurrenceIndex);
                        var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,physics.MassKilograms);
                        var motion=new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond);
                        bool firing=row.End==ManualRacePathEnd.FiringLine&&cursor.ObservedArcNanometers==row.LengthNanometers;
                        if(firing&&(motion.Velocity.X!=0||motion.Velocity.Z!=0||motion.YawRate!=0))
                            return Report(ManualFieldStepStatus.MovingFiringArrival,tick,actor.Athlete);
                        if(firing)held[heldCount++]=i;
                        var surface=catalog.SampleSurface(actor.Observation);
                        if(pending[i].MovementActive&&!firing)
                        {
                            double wind=inputs[i].Ski.HeadwindMetersPerSecond;
                            var input=field.GetAthlete(i).GetPreparedManualMotionInput(pending[i],world,tick,i,steeringInputs[i],drafts[i].DragAreaMultiplier,
                                windX:-wind*actor.Observation.ForwardX,windZ:-wind*actor.Observation.ForwardZ);
                            if(row.Kind==ManualRacePathKind.FinishRunout)
                                drives[i]=drive.Preview(body,motion,actor.Observation,surface,Neutral(input).WithBrakeFraction(catalog.FinishRunoutDefinition.BrakeFraction),seconds).Drive;
                            else if(matStops[i]>0)drives[i]=drive.PreviewStopping(body,motion,actor.Observation,surface,Neutral(input),seconds,matStops[i]).Frame.Drive;
                            else
                            {
                                if(brakeFractions.Length!=0)input=input.WithBrakeFraction(brakeFractions[i]);
                                drives[i]=drive.Preview(body,motion,actor.Observation,surface,input,seconds).Drive;
                            }
                        }
                        else
                        {
                            // Already stationary race phases have no propulsion.
                            // The actual common force/contact solver still owns
                            // their motion; a contact-induced push fails the
                            // stationary phase proof rather than becoming a clamp.
                            drives[i]=stationary.Preview(body,motion,actor.Observation,surface,0,0,0,seconds);
                        }
                        grades[i]=actor.Observation.GradeAlong(actor.Pose);
                        double span=physics.MaximumSpeedMetersPerSecond*seconds*actor.Observation.StationMetersPerPlanarMeter;
                        windows[i]=new ManualScopedWorldWindow(actor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,
                            span,row.LengthNanometers);
                    }
                    if(!world.TryPreviewContactDrivenMotion(tick,drives,candidates)||
                        !world.TryAppendToOwnedFirstEvent(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,grades,
                            out var next,held.AsSpan(0,heldCount)))return Report(ManualFieldStepStatus.WorldRejected,tick);
                    int ordinaryCount=0,phaseCount=0;
                    for(int b=0;b<next.BoundaryCount;b++)
                    {
                        var boundary=world.GetLastBoundary(tick,b);
                        if(!boundary.IsOccurrenceEnd)return Report(ManualFieldStepStatus.PhaseRequired,tick,boundary.Athlete);
                        if(boundary.End==ManualRacePathEnd.Continue)ordinary[ordinaryCount++]=boundary.ActorIndex;
                        else if(boundary.End==ManualRacePathEnd.PenaltyDecision||boundary.End==ManualRacePathEnd.PenaltyCircuit||
                            boundary.End==ManualRacePathEnd.Finish&&world.GetCatalog(boundary.ActorIndex).FinishRunoutIndex>=0)
                            phases[phaseCount++]=boundary.ActorIndex;
                        else if(boundary.End==ManualRacePathEnd.Finish)
                            return Report(ManualFieldStepStatus.PhaseRequired,tick,boundary.Athlete);
                    }
                    if(world.HasPendingContact(tick)&&!world.TryResolveArrivals(tick))
                        return Report(ManualFieldStepStatus.WorldRejected,tick);
                    if(ordinaryCount>0&&!world.TryHandoffOrdinary(tick,ordinary.AsSpan(0,ordinaryCount)))
                        return Report(ManualFieldStepStatus.PhaseRequired,tick);
                    if(phaseCount>0&&!commit.TryHandoffWorkingPhases(tick,pending,phases.AsSpan(0,phaseCount)))
                        return Report(ManualFieldStepStatus.PhaseRequired,tick);
                    // A zero-time firing notification is consumed on the next
                    // pass by an actual stationary-body hold, never a paid wait.
                }
                if(!world.TryFinalizeTick(tick,out proof))return Report(ManualFieldStepStatus.WorldRejected,tick);
                // Publish the wake used by the last owned force interval.
                // Physiology and delivered propulsion receive no refund/bonus.
                int pieces=world.GetProposedMotionPieceCount(proof);
                commit.CommitPackField(field,inputs,pending,proof,drafts);
                published=true;
                LastRejectedWorkingActor=default;
                LastStep=new ManualFieldStepResult(ManualFieldStepStatus.Complete,field.Tick,default,world.LastAttempt,
                    1.0/field.TickFrequency,pieces);
                return true;
            }
            finally {if(!published){if(proof.IsValid)world.DiscardPrepared(proof);else world.AbortTick(tick);}}
        }
        private static ManualSkiMotionInput Neutral(ManualSkiMotionInput input)
            =>new ManualSkiMotionInput(0,0,input.CorneringSkill,input.DownhillSkill,input.LineQuality,input.MuscularFatigue,
                input.GripMultiplier,input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.SteeringResponse,input.WindX,input.WindZ);
        private bool Report(ManualFieldStepStatus status,ManualScopedWorldTick tick,AthleteId athlete=default)
        {
            double seconds=tick.IsValid?world.GetElapsedSeconds(tick):0;
            int count=tick.IsValid?world.GetWorkingMotionPieceCount(tick):0;
            LastRejectedWorkingActor=default;
            var failed=athlete.IsValid?athlete:world.LastAttempt.Athlete;
            if(tick.IsValid)for(int i=0;i<world.Count;i++)
            {
                var actor=world.GetWorkingActor(tick,i);
                if(actor.Athlete.Equals(failed)){LastRejectedWorkingActor=actor;break;}
            }
            LastStep=new ManualFieldStepResult(status,field.Tick.Next(),athlete,world.LastAttempt,seconds,count);return false;
        }
    }
}
