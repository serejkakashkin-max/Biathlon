using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Recorded force-law version. V25 and V26 retain the original replay calendars.</summary>
    public enum ManualCourseReactionPolicy { LegacyV25=25, PassiveCurvedV26=26, IndependentCourseV27=27, StableAthleteContactV28=28 }
    public readonly struct ManualScopedWorldWindow
    {
        public AthleteId Athlete {get;}
        public int OccurrenceIndex {get;}
        public long PreviousArcNanometers {get;}
        public double MaximumArcDisplacementMeters {get;}
        public long MaximumAdmittedArcNanometers {get;}
        public ManualScopedWorldWindow(AthleteId athlete,int occurrence,long previousArc,double span,long maximumArc)
        {
            if(!athlete.IsValid||occurrence<0||previousArc<0||maximumArc<0)throw new ArgumentException("Valid actor and scoped arc bounds required.");
            PhysiologyNumbers.RequireRange(span,0,PhysiologyNumbers.MaximumMagnitude,nameof(span));
            Athlete=athlete;OccurrenceIndex=occurrence;PreviousArcNanometers=previousArc;
            MaximumArcDisplacementMeters=span;MaximumAdmittedArcNanometers=maximumArc;
        }
    }
    public enum ManualScopedWorldStatus { NotPrepared, Clear, SourceMismatch, BodyOrEdge, Course, Projection, PhaseLimit, IncompleteTick, PendingContact, ContactResponse, IntervalLimit, ContactSchedule, ContactGeometry, ContactForces, BoundarySchedule, ShootingHold }
    /// <summary>One admitted common clock interval for one roster row. Physical
    /// travel, observed arc and high-water credit remain separate coordinates.</summary>
    public readonly struct ManualScopedWorldMotionPiece
    {
        public int OccurrenceIndex {get;}
        public long ObservedArcBefore {get;} public long ObservedArcAfter {get;}
        public long CreditedArcBefore {get;} public long CreditedArcAfter {get;}
        public long RouteBefore {get;} public long RouteAfter {get;}
        public long TravelBefore {get;} public long TravelAfter {get;}
        public double StartSeconds {get;} public double EndSeconds {get;}
        internal ManualScopedWorldMotionPiece(ManualRacePathCursor before,ManualRacePathCursor after,
            SkiState motionBefore,SkiState motionAfter,double start,double end)
        {
            OccurrenceIndex=before.OccurrenceIndex;ObservedArcBefore=before.ObservedArcNanometers;ObservedArcAfter=after.ObservedArcNanometers;
            CreditedArcBefore=before.CreditedArcNanometers;CreditedArcAfter=after.CreditedArcNanometers;
            RouteBefore=before.CreditedRouteNanometers;RouteAfter=after.CreditedRouteNanometers;
            TravelBefore=motionBefore.DistanceNanometers;TravelAfter=motionAfter.DistanceNanometers;StartSeconds=start;EndSeconds=end;
        }
    }
    public readonly struct ManualScopedWorldAttempt
    {
        public ManualScopedWorldStatus Status {get;}
        public AthleteId Athlete {get;}
        public SkiJointSweepPreview BodyOrEdge {get;}
        public ManualRouteTubePreview Course {get;}
        public ManualRacePathGatePreview Gate {get;}
        public SkiContactGroupPreview Response {get;}
        public SkiEdgeSweepPreview CourseEdge {get;}
        internal ManualScopedWorldAttempt(ManualScopedWorldStatus status,AthleteId athlete=default,
            SkiJointSweepPreview body=default,ManualRouteTubePreview course=default,ManualRacePathGatePreview gate=default,SkiContactGroupPreview response=default,SkiEdgeSweepPreview courseEdge=default)
        {Status=status;Athlete=athlete;BodyOrEdge=body;Course=course;Gate=gate;Response=response;CourseEdge=courseEdge;}
    }
    public readonly struct ManualScopedWorldPreview
    {
        internal ManualScopedWorldRoster Owner {get;}
        internal long Revision {get;}
        public bool IsValid=>Owner!=null;
        public SimulationTick Tick {get;}
        internal ManualScopedWorldPreview(ManualScopedWorldRoster owner,long revision,SimulationTick tick)
        {Owner=owner;Revision=revision;Tick=tick;}
    }
    /// <summary>A provisional full-clock transaction, not accepted race state.</summary>
    public readonly struct ManualScopedWorldTick
    {
        internal ManualScopedWorldRoster Owner {get;}
        internal long Revision {get;}
        public bool IsValid=>Owner!=null;
        public SimulationTick Tick {get;}
        internal ManualScopedWorldTick(ManualScopedWorldRoster owner,long revision,SimulationTick tick)
        {Owner=owner;Revision=revision;Tick=tick;}
    }

    /// <summary>Owned full-clear world proposals for one exact race with
    /// heterogeneous assigned paths. Rejected prefixes never become paid ticks.
    /// Reached contacts respond provisionally before paid continuation;
    /// session/lifecycle commit remains separate.</summary>
    public sealed partial class ManualScopedWorldRoster
    {
        private readonly ManualRacePathCatalog[] catalogs;
        private readonly ManualRacePathProgression[] progress;
        private readonly ManualRouteTubeSweep[] courses;
        private readonly ManualRacePathGateSweep[] gates;
        private readonly ManualRacePathBoundaryScheduler[] boundaries;
        private readonly ManualRacePathBoundaryPreview[] boundaryPreviews;
        private readonly SkiWorldMotionCandidate[] boundaryPrefix;
        private readonly ManualScopedWorldBoundary[] arrivedBoundaries;
        private readonly bool[] heldFiringActors;
        private readonly bool[] physicalParticipants;
        private readonly bool[] contactParticipants,rangeTransitRequested,rangeTransitYield;
        private long[] scheduledPresenceStarts;
        private bool prestartPresence;
        private readonly ManualSkiJointSweep bodies;
        private readonly ManualSkiEdgeSweep courseCells;
        private readonly OccupancyDefinition occupiedBody;
        private readonly ManualNumericalToleranceDefinition numericalTolerance;
        private readonly ManualMatCaptureDefinition matCapture;
        public int MatCaptures {get;private set;}
        private readonly int[] matCaptureCounts;
        public int GetMatCaptures(int actor)=>matCaptureCounts[actor];
        public double MatCaptureDissipatedJoules {get;private set;}
        private readonly ManualSkiContactScheduler scheduler;
        private readonly SkiWorldMotionCandidate[] scheduledPrefix;
        private readonly SkiReachedContactPair[] scheduledArrivals;
        private readonly ManualSkiContactSystem ownedContactGeometry;
        private readonly SkiReachedContactPair[] ownedContacts;
        private readonly int[] ownedContactOrder;
        private readonly double ownedContactTolerance;
        private readonly ManualSkiContactGroup response;
        private readonly SkiImpactObservation[] workingImpacts,acceptedImpacts;
        private long impactTick=-1;
        public SkiImpactObservation GetAcceptedImpact(int index)=>acceptedImpacts[index];
        private readonly ManualSkiContactForces contactForces;
        private readonly SkiForceCoupledTrajectory[] coupledTrajectories;
        private readonly SkiDrivenContactTrajectory[] ownedFreeDrives;
        private readonly SkiForceCoupledTrajectory[] ownedFreePieces;
        private readonly double[] ownedFreeStarts;
        private readonly int[] ownedFreeOccurrences,ownedFreePhases;
        private readonly bool[] ownedFreeValid,ownedContactParticipants;
        private readonly int maximumForceRefinements;
        private readonly SkiWorldMotionCandidate[] coupledCandidates;
        private readonly double[] masses;
        private readonly ManualSkiContactBody[] responseBodies;
        private readonly SkiContactMotion[] responseMotions;
        private readonly int[] responseOrder;
        private readonly ManualRacePathActorFrame[] accepted,staged;
        private readonly ManualRacePathActorFrame[] workingActors;
        private readonly ManualRacePathCursor[] cursors,handoffCursors;
        private readonly ManualRacePathCursor[] workingCursors,proposedCursors;
        private readonly ManualRacePathProgressFrame[] stagedProgress;
        private readonly SkiState[] motion,stagedMotion;
        private readonly SkiState[] workingMotion;
        private readonly double[] workingTravelMeters,stagedTravelMeters;
        private readonly double duration;
        private readonly ManualScopedWorldMotionPiece[] pieces;
        private readonly ManualRouteRibbonSweep[] ribbons;
        private readonly int maximumPieces;
        private int pieceCount;
        private long revision;
        private bool ready;
        private bool tickActive;
        private long tickRevision;
        private double elapsedSeconds;
        private bool pendingContact;
        private PackSprintFieldSession initialFieldBinding;
        internal bool IsVerifiedInitialField(PackSprintFieldSession field)=>Tick.Value==0&&ReferenceEquals(initialFieldBinding,field);
        internal bool IsVerifiedInitialSession(SprintAthleteSession session)
        {
            if(Tick.Value!=0||initialFieldBinding==null)return false;
            for(int i=0;i<Count;i++)if(ReferenceEquals(initialFieldBinding.GetAthlete(i),session))return true;
            return false;
        }
        internal void BindInitialField(PackSprintFieldSession field)
        {
            if(Tick.Value!=0||field==null||field.Tick.Value!=0||field.Count!=Count||initialFieldBinding!=null||ready||tickActive)
                throw new ArgumentException("One unchanged initial field binding required.");
            // Authenticate exact encoded physical poses against every actual
            // grid row before accepting canonical projection coordinates.
            for(int i=0;i<Count;i++)
            {
                var a=field.GetAthlete(i);var c=cursors[i];var row=c.Owner.Get(c.OccurrenceIndex);var point=field.GetPhysicalPoint(i);
                int expectedOccurrence=c.OccurrenceIndex;
                long expectedArc=row.Kind==ManualRacePathKind.StartApproach?row.LengthNanometers-a.RemainingStartApproachNanometers:0;
                if(point.LoopId==field.StartApproach?.LoopId&&a.RemainingStartApproachNanometers==0)
                {expectedOccurrence=c.Owner.StartApproachIndex;expectedArc=c.Owner.Get(expectedOccurrence).LengthNanometers;}
                if(!c.Owner.TrySample(expectedOccurrence,expectedArc,out var expected)||
                    !accepted[i].Athlete.Equals(a.AthleteId)||a.RouteNanometers!=0||motion[i].DistanceNanometers!=a.Motion.DistanceNanometers||
                    accepted[i].Velocity.X!=0||accepted[i].Velocity.Z!=0||accepted[i].Pose.YawRateRadiansPerSecond!=0)
                    throw new ArgumentException("Every initial world row must authenticate its exact stationary grid source.");
                double side=field.GetLateral(i);
                var pose=new ManualSkiHeadingState(expected.X+expected.ForwardZ*side,expected.Z-expected.ForwardX*side,Math.Atan2(expected.ForwardX,expected.ForwardZ));
                if(!SamePose(pose,accepted[i].Pose))throw new ArgumentException("Canonical initial coordinates cannot move or turn a grid body.");
            }
            initialFieldBinding=field;
            scheduledPresenceStarts=new long[Count];prestartPresence=field.StartGrid!=null;
            var starts=new string[Count+2];starts[0]="field_attested_scheduled_presence_v4_first_moving_interval";
            starts[1]=field.StartGrid==null?"scheduled_presence_after_start_event_tick;waiting_pose_static;all_started_body_proofs_retained":"mass_grid_present_before_start";
            for(int i=0;i<Count;i++)
            {
                scheduledPresenceStarts[i]=field.GetAthlete(i).ScheduledStart.Value;
                starts[i+2]=field.GetAthlete(i).AthleteId.Value+":"+scheduledPresenceStarts[i].ToString(CultureInfo.InvariantCulture);
            }
            var extra=ShootingModelDefinitions.EncodeContext(starts);
            ContextJson=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
            BindStartReleaseGate(field);UpdatePhysicalParticipants(Tick.Next());
        }
        public int Count=>accepted.Length;
        private ManualRangeApronSnowRegion rangeApronSnow;
        internal void BindRangeApronSnow(ManualRangeApronSnowRegion region)
        {
            if(region==null||rangeApronSnow!=null||Tick.Value!=0||tickActive||ready)throw new ArgumentException("Bind an apron only to the fresh world.");
            if(region.BodyJson!=BodyContextJson)throw new ArgumentException("Exact apron body required.");
            for(int i=0;i<Count;i++)if(region.TrackJson!=catalogs[i].TrackSnapshotJson||region.VisualJson!=catalogs[i].VisualSnapshotJson||
                region.PathsJson!=catalogs[i].SelectedPath.PathsSnapshotJson)throw new ArgumentException("Exact authored apron geometry required.");
            rangeApronSnow=region;var extra=ShootingModelDefinitions.EncodeContext(new[]{region.ContextJson});
            ContextJson=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
        }
        public int MaximumMotionPieces=>maximumPieces;
        public ManualRouteRibbonDiagnostics GetRibbonDiagnostics(int index)=>ribbons[index].Diagnostics;
        public string WorkContextJson {get;}
        public bool HasContactForcePolicy=>contactForces!=null;
        public int TickFrequency {get;}
        public SimulationTick Tick {get;private set;}
        public string ContextJson {get;private set;}
        public string BodyContextJson {get;}
        public string InitialStateContextJson {get;}
        public ManualCourseReactionPolicy CourseReactionPolicy {get;}
        public ManualScopedWorldAttempt LastAttempt {get;private set;}
        /// <summary>Scratch diagnostics for the current proposal/tick, including
        /// provisional intervals. Reset on the next attempt; never race credit.</summary>
        public int NumericalRestCanonicalizations {get;private set;}
        public double MaximumCanonicalizedRestSpeed {get;private set;}
        public double MaximumCanonicalizedRestYaw {get;private set;}
        public ManualScopedWorldRoster(ManualRacePathCatalog[] catalogs,OccupancyDefinition body,
            SkiContactDefinition contact,SkiContactSweepDefinition work,ManualRacePathCursor[] initialCursors,
            ManualSkiHeadingState[] poses,SkiPlanarVelocity[] velocities,SkiState[] initialMotion,
            int tickFrequency,SimulationTick initialTick=default,SkiContactGroupDefinition responseDefinition=null,double[] actorMasses=null,
            ManualNumericalToleranceDefinition numericalTolerance=null,ManualMatCaptureDefinition matCapture=null,ManualCourseContactDefinition courseContact=null,
            ManualCourseReactionPolicy courseReactionPolicy=ManualCourseReactionPolicy.PassiveCurvedV26)
        {
            if(courseReactionPolicy<ManualCourseReactionPolicy.LegacyV25||courseReactionPolicy>ManualCourseReactionPolicy.StableAthleteContactV28)
                throw new ArgumentOutOfRangeException(nameof(courseReactionPolicy));
            CourseReactionPolicy=courseReactionPolicy;
            if(catalogs==null||body==null||contact==null||work==null||initialCursors==null||poses==null||
                velocities==null||initialMotion==null)throw new ArgumentNullException();
            int count=catalogs.Length;
            numericalTolerance?.Validate(body);
            this.numericalTolerance=numericalTolerance;this.matCapture=matCapture;
            this.courseContact=courseContact;courseNormalsX=new double[count];courseNormalsZ=new double[count];courseLevers=new double[count];
            courseSupports=new bool[count];courseImpactCounts=new int[count];matCaptureCounts=new int[count];
            if(count==0||initialCursors.Length!=count||poses.Length!=count||velocities.Length!=count||
                initialMotion.Length!=count||tickFrequency<=0)throw new ArgumentException("Complete scoped roster and fixed clock required.");
            if((responseDefinition==null)!=(actorMasses==null)||actorMasses!=null&&actorMasses.Length!=count)
                throw new ArgumentException("Contact response requires a complete immutable actor mass roster and group definition.");
            string responseContext="contact_response_unconfigured";
            if(responseDefinition!=null)
            {
                masses=(double[])actorMasses.Clone();var massContext=new string[count+1];
                response=new ManualSkiContactGroup(body,contact,responseDefinition);massContext[0]=response.ContextJson;
                contactForces=new ManualSkiContactForces(body,contact,responseDefinition,count);
                coupledTrajectories=new SkiForceCoupledTrajectory[count];coupledCandidates=new SkiWorldMotionCandidate[count];
                for(int i=0;i<count;i++)
                {
                    PhysiologyNumbers.RequireRange(masses[i],.001,PhysiologyNumbers.MaximumMagnitude,nameof(actorMasses));
                    massContext[i+1]=masses[i].ToString("R",CultureInfo.InvariantCulture);
                }
                responseContext=ShootingModelDefinitions.EncodeContext(massContext);
                responseBodies=new ManualSkiContactBody[count];responseMotions=new SkiContactMotion[count];responseOrder=new int[count];
            }
            this.catalogs=(ManualRacePathCatalog[])catalogs.Clone();TickFrequency=tickFrequency;duration=1.0/tickFrequency;Tick=initialTick;
            accepted=new ManualRacePathActorFrame[count];staged=new ManualRacePathActorFrame[count];
            cursors=new ManualRacePathCursor[count];handoffCursors=new ManualRacePathCursor[count];
            stagedProgress=new ManualRacePathProgressFrame[count];motion=(SkiState[])initialMotion.Clone();stagedMotion=new SkiState[count];
            workingActors=new ManualRacePathActorFrame[count];workingCursors=new ManualRacePathCursor[count];
            ownedFreeDrives=new SkiDrivenContactTrajectory[count];ownedFreePieces=new SkiForceCoupledTrajectory[count];
            ownedFreeStarts=new double[count];ownedFreeOccurrences=new int[count];ownedFreePhases=new int[count];ownedFreeValid=new bool[count];
            ownedContactParticipants=new bool[count];
            ownedContactCachePoses=new ManualSkiHeadingState[count];ownedContactCacheParticipants=new bool[count];
            workingImpacts=new SkiImpactObservation[count];acceptedImpacts=new SkiImpactObservation[count];
            maximumForceRefinements=work.MaximumDepth;
            proposedCursors=new ManualRacePathCursor[count];workingMotion=new SkiState[count];
            workingTravelMeters=new double[count];stagedTravelMeters=new double[count];
            progress=new ManualRacePathProgression[count];courses=new ManualRouteTubeSweep[count];ribbons=new ManualRouteRibbonSweep[count];gates=new ManualRacePathGateSweep[count];bodies=new ManualSkiJointSweep(body,contact,work,separatedResponseBand:true);
            boundaries=new ManualRacePathBoundaryScheduler[count];boundaryPreviews=new ManualRacePathBoundaryPreview[count];
            boundaryPrefix=new SkiWorldMotionCandidate[count];arrivedBoundaries=new ManualScopedWorldBoundary[count];
            heldFiringActors=new bool[count];physicalParticipants=new bool[count];
            contactParticipants=new bool[count];rangeTransitRequested=new bool[count];rangeTransitYield=new bool[count];
            for(int i=0;i<count;i++)physicalParticipants[i]=contactParticipants[i]=true;
            occupiedBody=body;courseCells=new ManualSkiEdgeSweep(body,contact,work);
            scheduler=new ManualSkiContactScheduler(body,contact,work,count);scheduledPrefix=new SkiWorldMotionCandidate[count];
            scheduledArrivals=new SkiReachedContactPair[checked(count*(count-1)/2)];
            ownedContactGeometry=new ManualSkiContactSystem(body,contact);ownedContactTolerance=contact.ContactTolerance;
            ownedContacts=new SkiReachedContactPair[scheduledArrivals.Length];ownedContactOrder=new int[count];
            maximumPieces=work.MaximumIntervalChecks;WorkContextJson=work.ToJson();pieces=new ManualScopedWorldMotionPiece[checked(count*maximumPieces)];
            var context=new string[count+7];var initial=new string[count+4];
            BodyContextJson=body.ToJson();
            context[0] = courseReactionPolicy switch
            {
                ManualCourseReactionPolicy.LegacyV25 => "manual_scoped_full_clear_world_roster_v25_physical_mat_plant_bounds",
                ManualCourseReactionPolicy.PassiveCurvedV26 => "manual_scoped_full_clear_world_roster_v26_passive_curved_snow_support",
                ManualCourseReactionPolicy.IndependentCourseV27 => "manual_scoped_full_clear_world_roster_v27_independent_course_support",
                _ => "manual_scoped_full_clear_world_roster_v28_stable_athlete_contact"
            };
            context[1]=BodyContextJson;
            context[2]=contact.ToJson();context[3]=work.ToJson();
            context[4]="force_exact_distance;contact_velocity_norm_simpson64;grade_frozen_per_interval;full_duration_tolerance_1e-12;no_implicit_scope_crossing;local_terminal_centre_portals_v1;whole_tube_edge_activation;continuous_acceleration_bound;existing_trajectory_encoding_reserve;staged_full_clock_intervals;exact_remaining_time;round_tick_travel_once;owned_motion_pieces;interval_limit_from_sweep_maximum_checks;accepted_phase_endpoints_authorized_by_session_batch;future_penalty_phases_from_paid_interval_high_water;no_handoff_before_pending_contact_response;"+ManualSkiWorldTrajectory.SlicePolicy;
            context[5]=ShootingModelDefinitions.EncodeContext(new[]{responseContext,scheduler.ContextJson,courseCells.ContextJson,ownedContactGeometry.ContextJson,
                "course_tube_or_exact_collinear_cell_or_local_cover_v3;all_four_oriented_body_planes_or_continuous_rectangle_disk_cover;simple_ribbon_or_existing_conservative_tube_union;bounded_cell_refinement_without_body_shrink;exact_authored_endpoint_connectivity;shared_ordinary_bisector_station_portals;no_phase_handoff_from_course_proof;owned_current_pose_proximity_pairs;stable_identity_order;no_penetration_admission;initial_response_without_clock_or_position;separated_breakpoint_or_certified_terminal_stop_field_keeps_immutable_full_free_drives_until_actual_event;ordinary_constant_force_representation_retained;reached_contact_external_drive_calendar_frozen_per_tick_until_occurrence_hold_expiry_or_separation;actual_current_raw_motion_for_reactions;half_interval_force_work_refinement_bounded_by_existing_sweep_depth;no_residual_relaxation;disconnected_certified_neutral_stop_keeps_complete_free_snow_ramp_and_endpoint_without_clipping_foreign_contact_calendar;actual_contact_participants_never_receive_free_override",
                contactForces==null?"contact_forces_unconfigured":contactForces.ContextJson,ManualRacePathBoundaryScheduler.Policy,
                numericalTolerance?.ToJson()??"numerical_tolerance_not_selected",matCapture?.ToJson()??"mat_capture_not_selected",courseContact?.ToJson()??"course_contact_not_selected",
                "terminal_rest_canonicalization_only_after_complete_body_course_gate_proof_and_actual_owned_firing_plane;unchanged_pose_heading_paid_time_and_travel;no_general_velocity_or_contact_relaxation"+
                (courseReactionPolicy==ManualCourseReactionPolicy.LegacyV25?"":";arrested_contact_spin_retains_bounded_curved_snow_support_without_positive_reaction_work_or_unpaid_translation_energy;unchanged_whole_trajectory_admission"),
                "owned_common_contact_and_boundary_prefix;all_roster_sources_before_event_search;stable_boundary_identity_order;zero_clock_owned_endpoint_notification;no_implicit_lifecycle_handoff;"+
                "explicit_stationary_owned_firing_hold_v2_admitted_folded_endpoint_plant;exact_zero_velocity_acceleration_and_peak_yaw;actual_endpoint_only;whole_contact_course_gate_proof_still_required"});
            initial[0]="manual_scoped_initial_world_roster_v1";initial[1]=tickFrequency.ToString(CultureInfo.InvariantCulture);
            initial[2]=initialTick.Value.ToString(CultureInfo.InvariantCulture);initial[3]="actor,occurrence,observed_arc,high_arc,route_credit,x,z,heading_nrad,yaw,vx,vz,odometer,speed";
            for(int i=0;i<count;i++)
            {
                var catalog=this.catalogs[i];var cursor=initialCursors[i];var pose=poses[i];var velocity=velocities[i];
                if(catalog!=null&&matCapture!=null)catalog.ProjectionGeometry.MatPositionTolerance=matCapture.PositionTolerance;
                if(catalog==null||!catalog.IsSameRace(this.catalogs[0])||!cursor.IsValid||
                    !ReferenceEquals(cursor.Owner,catalog)||!pose.IsValid||
                    !catalog.TryProject(pose,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,0,out var observation)||
                    observation.PhysicalArcNanometers!=cursor.ObservedArcNanometers)
                    throw new ArgumentException("Each accepted actor must bind its exact race, path and world observation.");
                for(int j=0;j<i;j++)if(cursor.Athlete.Equals(cursors[j].Athlete))throw new ArgumentException("Duplicate scoped world actor.");
                progress[i]=new ManualRacePathProgression(catalog);courses[i]=new ManualRouteTubeSweep(catalog.ProjectionGeometry,body,work);
                ribbons[i]=new ManualRouteRibbonSweep(catalog.ProjectionGeometry,body,work,numericalTolerance);
                gates[i]=new ManualRacePathGateSweep(catalog,work);
                boundaries[i]=new ManualRacePathBoundaryScheduler(catalog,work);
                cursors[i]=cursor;accepted[i]=new ManualRacePathActorFrame(cursor.Athlete,pose,velocity,observation);
                context[i+6]="catalog_context_sha256:"+ManualContextIdentity.Sha256(catalog.ContextJson);
                string Long(long value)=>value.ToString(CultureInfo.InvariantCulture);
                string Double(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
                initial[i+4]=ShootingModelDefinitions.EncodeContext(new[]{cursor.Athlete.Value,Long(cursor.OccurrenceIndex),
                    Long(cursor.ObservedArcNanometers),Long(cursor.CreditedArcNanometers),Long(cursor.CreditedRouteNanometers),
                    Double(pose.X),Double(pose.Z),Long(pose.HeadingNanoradians),Double(pose.YawRateRadiansPerSecond),
                    Double(velocity.X),Double(velocity.Z),Long(motion[i].DistanceNanometers),Double(motion[i].SpeedMetersPerSecond)});
                int at=i;
                while(at>0&&string.CompareOrdinal(accepted[ownedContactOrder[at-1]].Athlete.Value,cursor.Athlete.Value)>0)
                {ownedContactOrder[at]=ownedContactOrder[at-1];at--;}
                ownedContactOrder[at]=i;
            }
            InitialStateContextJson=ShootingModelDefinitions.EncodeContext(initial);context[context.Length-1]=InitialStateContextJson;
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }
        public ManualRacePathActorFrame GetActor(int index)=>accepted[index];
        public ManualRacePathCatalog GetCatalog(int index)=>catalogs[index];
        public ManualRacePathCursor GetCursor(int index)=>cursors[index];
        public SkiState GetPhysicalMotion(int index)=>motion[index];
        /// <summary>Presence is derived from the attested field schedule; callers
        /// cannot hide a live skier to bypass collision admission.</summary>
        public bool IsPhysicalParticipant(int index,SimulationTick tick)
        {
            if(!HasScheduledPhysicalPresence(index,tick))return false;
            if(!HasStartReleaseGate)return true;
            if(tick.Value==Tick.Value+1){PreviewStartReleases(tick);return pendingStartReleaseTicks[index]>=0;}
            return startReleaseTicks[index]>=0;
        }
        private bool HasScheduledPhysicalPresence(int index,SimulationTick tick)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            var cursor=cursors[index];var catalog=catalogs[index];var finish=catalog.FinishRunoutDefinition;
            if(finish!=null&&finish.ClearAfterRest&&catalog.Get(cursor.OccurrenceIndex).Kind==ManualRacePathKind.FinishRunout&&
                cursor.ObservedArcNanometers>=finish.MinimumClearanceDistance*SkiState.NanometersPerMeter&&
                accepted[index].Velocity.X==0&&accepted[index].Velocity.Z==0&&accepted[index].Pose.YawRateRadiansPerSecond==0)return false;
            // The session publishes its start event at ScheduledStart and
            // first pays movement at the following tick. A held scheduled
            // body cannot receive live traffic impulses during that event.
            return scheduledPresenceStarts==null||prestartPresence||tick.Value>scheduledPresenceStarts[index];
        }
        public void SetRangeTransit(int actor,bool active)
        {
            if(tickActive||actor<0||actor>=Count)throw new InvalidOperationException("Set transit intent before the world tick.");
            rangeTransitRequested[actor]=active;
        }
        public bool IsRangeTransitYielding(int actor)=>rangeTransitYield[actor];
        private void UpdatePhysicalParticipants(SimulationTick tick)
        {
            PreviewStartReleases(tick);
            double clearance=occupiedBody.LongitudinalSeparation*occupiedBody.LongitudinalSeparation+
                occupiedBody.LateralSeparation*occupiedBody.LateralSeparation;
            for(int i=0;i<Count;i++)physicalParticipants[i]=IsPhysicalParticipant(i,tick);
            for(int i=0;i<Count;i++)
            {
                bool soft=rangeTransitRequested[i];
                // Restore ordinary contact only after the departing skier is clear.
                // This avoids introducing penetration at the assistance handoff.
                if(!soft&&rangeTransitYield[i])for(int j=0;j<Count;j++)
                {
                    if(i==j||!physicalParticipants[j])continue;
                    double dx=accepted[i].Pose.X-accepted[j].Pose.X,dz=accepted[i].Pose.Z-accepted[j].Pose.Z;
                    if(dx*dx+dz*dz<=clearance&&ownedContactGeometry.Preview(
                        new ManualSkiContactBody(accepted[i].Athlete,accepted[i].Pose,default,1),
                        new ManualSkiContactBody(accepted[j].Athlete,accepted[j].Pose,default,1)).GapMeters<=ownedContactTolerance)
                    {soft=true;break;}
                }
                rangeTransitYield[i]=soft;contactParticipants[i]=physicalParticipants[i]&&!soft;
            }
        }
        public void ValidatePrepared(ManualScopedWorldPreview proposal)
        {
            if(!ready||!ReferenceEquals(proposal.Owner,this)||proposal.Revision!=revision||proposal.Tick.Value!=checked(Tick.Value+1))
                throw new ArgumentException("Missing, foreign, stale or consumed scoped world proposal.");
            if(HasStartReleaseGate&&startReleasePreviewTick!=proposal.Tick.Value)
                throw new ArgumentException("Missing attested scheduled-start release preview.");
        }
        public ManualRacePathActorFrame GetProposedActor(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return staged[index];}
        public ManualRacePathCursor GetProposedCursor(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return proposedCursors[index];}
        public SkiState GetProposedPhysicalMotion(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return stagedMotion[index];}
        public int GetProposedMotionPieceCount(ManualScopedWorldPreview proposal){ValidatePrepared(proposal);return pieceCount;}
        public ManualScopedWorldMotionPiece GetProposedMotionPiece(ManualScopedWorldPreview proposal,int actor,int piece)
        {ValidatePrepared(proposal);return GetPiece(actor,piece);}
        private ManualScopedWorldMotionPiece GetPiece(int actor,int piece)
        {
            if(actor<0||actor>=Count||piece<0||piece>=pieceCount)throw new ArgumentOutOfRangeException();
            return pieces[checked(actor*maximumPieces+piece)];
        }
        private bool Reject(ManualScopedWorldStatus status,AthleteId athlete=default,
            SkiJointSweepPreview joint=default,ManualRouteTubePreview course=default,ManualRacePathGatePreview gate=default,SkiContactGroupPreview response=default,SkiEdgeSweepPreview courseEdge=default)
        {LastAttempt=new ManualScopedWorldAttempt(status,athlete,joint,course,gate,response,courseEdge);return false;}
        private static bool SamePose(ManualSkiHeadingState a,ManualSkiHeadingState b)
            =>a.IsValid&&b.IsValid&&a.X==b.X&&a.Z==b.Z&&a.HeadingNanoradians==b.HeadingNanoradians&&
                a.YawRateRadiansPerSecond==b.YawRateRadiansPerSecond;
        private static bool SameVelocity(SkiPlanarVelocity a,SkiPlanarVelocity b)=>a.X==b.X&&a.Z==b.Z;

        /// <summary>Prove all body pairs/local supplied planes and each actor's
        /// own physical course for one complete tick. No implicit scope crossing,
        /// mat choice, stationary wait or fallback movement is committed.</summary>
        public bool TryPrepare(SkiWorldMotionCandidate[] actors,ManualScopedWorldWindow[] windows,
            SkiReachedContactPair[] reached,int reachedCount,SkiActorCourseEdge[] edges,int edgeCount,
            double[] directionalGrades,out ManualScopedWorldPreview proposal)
        {
            if(tickActive)throw new InvalidOperationException("Finish or abort the active staged world tick before replacing it.");
            ready=false;proposal=default;pieceCount=0;ClearPathChoices();UpdatePhysicalParticipants(Tick.Next());LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);
            ResetNumericalDiagnostics();
            Array.Clear(workingImpacts,0,Count);impactTick=Tick.Next().Value;
            if(!PrepareInterval(actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,accepted,cursors,duration,null))return false;
            for(int i=0;i<Count;i++)
            {
                proposedCursors[i]=stagedProgress[i].Cursor;
                pieces[i*maximumPieces]=new ManualScopedWorldMotionPiece(cursors[i],proposedCursors[i],motion[i],stagedMotion[i],0,duration);
            }
            pieceCount=1;
            proposal=PublishPrepared();return true;
        }
        private bool PrepareInterval(SkiWorldMotionCandidate[] actors,ManualScopedWorldWindow[] windows,
            SkiReachedContactPair[] reached,int reachedCount,SkiActorCourseEdge[] edges,int edgeCount,
            double[] directionalGrades,ManualRacePathActorFrame[] sources,ManualRacePathCursor[] sourceCursors,
            double seconds,double[] cumulativeTravelMeters,SkiReachedContactPair[] arriving=null,int arrivingCount=0)
        {
            if(actors==null||windows==null||directionalGrades==null||actors.Length!=Count||windows.Length!=Count||directionalGrades.Length!=Count)
                throw new ArgumentException("Exact full world roster, physical windows and grades required.");
            for(int i=0;i<Count;i++)
            {
                var actor=actors[i];var window=windows[i];var cursor=sourceCursors[i];
                if(!actor.Trajectory.IsValid||!actor.Athlete.Equals(cursor.Athlete)||!window.Athlete.Equals(cursor.Athlete)||
                    window.OccurrenceIndex!=cursor.OccurrenceIndex||window.PreviousArcNanometers!=cursor.ObservedArcNanometers||
                    !SamePose(actor.Trajectory.Sample(0),sources[i].Pose)||!SameVelocity(actor.Trajectory.VelocityAt(0),sources[i].Velocity)||
                    Math.Abs(actor.Trajectory.DurationSeconds-seconds)>seconds*1e-12)
                    return Reject(ManualScopedWorldStatus.SourceMismatch,cursor.Athlete);
                PhysiologyNumbers.RequireRange(directionalGrades[i],-1,1,nameof(directionalGrades));
                // A passive drive at rest may retain a conservative nonzero
                // force bound while its exact stop-constrained polynomial is
                // stationary. Authenticate that law, never endpoint equality
                // alone or a fabricated zero-bound replacement trajectory.
                bool stationaryLaw=physicalParticipants[i]||actor.Trajectory.PlanarAccelerationBound==0||
                    actor.Trajectory.TryGetConstantPlanarAcceleration(out var acceleration)&&acceleration.X==0&&acceleration.Z==0;
                if(!physicalParticipants[i]&&(!stationaryLaw||actor.Trajectory.PeakYawRate!=0||
                    sources[i].Velocity.X!=0||sources[i].Velocity.Z!=0||!SamePose(actor.Trajectory.Sample(1),sources[i].Pose)))
                    return Reject(ManualScopedWorldStatus.SourceMismatch,cursor.Athlete);
            }
            var joint=bodies.Preview(actors,Count,reached,reachedCount,edges,edgeCount,arriving??Array.Empty<SkiReachedContactPair>(),arrivingCount,contactParticipants);
            if(joint.Status!=SkiJointSweepStatus.Clear||Math.Abs(joint.ClearUntilSeconds-seconds)>seconds*1e-12)
                return Reject(ManualScopedWorldStatus.BodyOrEdge,joint.First,joint);
            for(int i=0;i<Count;i++)
            {
                var actor=actors[i];var window=windows[i];var catalog=catalogs[i];var row=catalog.Get(sourceCursors[i].OccurrenceIndex);
                if(window.MaximumAdmittedArcNanometers<sourceCursors[i].CreditedArcNanometers||window.MaximumAdmittedArcNanometers>row.LengthNanometers)
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete);
                // An admitted, planted mat body has already paid its complete
                // approach proof. Rebuilding a shorter local ribbon window at
                // rest must not invent a new wall under that unchanged body.
                // Pair contacts and the exact phase plane are still checked.
                bool planted=matCapture!=null&&row.End==ManualRacePathEnd.FiringLine&&sourceCursors[i].ObservedArcNanometers==row.LengthNanometers&&
                    sources[i].Velocity.X==0&&sources[i].Velocity.Z==0&&actor.Trajectory.PeakYawRate==0&&
                    SamePose(actor.Trajectory.Sample(1),sources[i].Pose)&&
                    actor.Trajectory.TryGetConstantPlanarAcceleration(out var plantedAcceleration)&&plantedAcceleration.X==0&&plantedAcceleration.Z==0;
                var gate=planted?new ManualRacePathGatePreview(ManualRacePathGateStatus.Clear,row.LengthNanometers,seconds,seconds,0):
                    gates[i].Preview(actor.Trajectory,row.Index,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,window.MaximumAdmittedArcNanometers);
                if(gate.Status!=ManualRacePathGateStatus.Clear)
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete,gate:gate);
                bool apronClear=rangeApronSnow!=null&&rangeApronSnow.Clears(actor.Trajectory,row.Kind);
                ribbons[i].SetRangeApron(row.Kind==ManualRacePathKind.RangeApproach||row.Kind==ManualRacePathKind.RangeExit?rangeApronSnow:null);
                if(!planted&&!apronClear)
                {
                    // Independent actual occupied-body proofs. Internal visual
                    // knot caps are not exterior course walls. Neither proof
                    // changes body dimensions, pose, velocity or phase authority.
                    bool clear=catalog.ProjectionGeometry.TryGetCollinearCorridor(sources[i].Observation.GeometryProjection,out var cell);
                    SkiEdgeSweepPreview failedEdge=default;
                    for(int k=0;clear&&k<4;k++)
                    {
                        var plane=k==0?cell.Left:k==1?cell.Right:k==2?cell.Entry:cell.Exit;
                        var edge=courseCells.Preview(actor.Trajectory,plane);
                        if(edge.Status!=SkiEdgeSweepStatus.Clear){failedEdge=edge;clear=false;}
                    }
                    if(!clear)
                    {
                        var course=courses[i].Preview(actor.Trajectory,row.Index,checked(row.GeometryStartNanometers+window.PreviousArcNanometers),window.MaximumArcDisplacementMeters,false);
                        if(course.Status!=ManualRouteTubeStatus.Clear||Math.Abs(course.ClearUntilSeconds-seconds)>seconds*1e-12)
                        {
                            var ribbon=ribbons[i].Preview(actor.Trajectory,sources[i].Observation.GeometryProjection);
                            if(ribbon.Status!=ManualRouteTubeStatus.Clear||Math.Abs(ribbon.ClearUntilSeconds-seconds)>seconds*1e-12)
                                return Reject(ManualScopedWorldStatus.Course,actor.Athlete,course:ribbon,courseEdge:failedEdge);
                        }
                    }
                }
                var pose=actor.Trajectory.Sample(1);
                if(!catalog.TryProject(pose,row.Index,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,out var observation))
                    return Reject(ManualScopedWorldStatus.Projection,actor.Athlete);
                var velocity=actor.Trajectory.VelocityAt(1);
                bool numericalRest=planted||row.End==ManualRacePathEnd.FiringLine&&observation.PhysicalArcNanometers==row.LengthNanometers&&
                    (boundaries[i].IsAtBoundary(actor.Trajectory,pose,window)&&(TryCaptureMat(i,catalog,row,ref pose,ref velocity)||TryCanonicalizeRest(ref pose,ref velocity))||
                     IsWithinMatPlantArea(catalog,row,pose)&&TryCaptureMat(i,catalog,row,ref pose,ref velocity));
                var runout=catalog.FinishRunoutDefinition;
                if(row.Kind==ManualRacePathKind.FinishRunout&&runout!=null&&runout.ClearAfterRest&&
                    observation.PhysicalArcNanometers>=runout.MinimumClearanceDistance*SkiState.NanometersPerMeter&&
                    velocity.X*velocity.X+velocity.Z*velocity.Z<=runout.RestSpeed*runout.RestSpeed)
                {pose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,0);velocity=default;numericalRest=true;}
                staged[i]=new ManualRacePathActorFrame(actor.Athlete,pose,velocity,observation);
                if(!progress[i].TryPreview(staged[i],sourceCursors[i],window.MaximumAdmittedArcNanometers,out stagedProgress[i]))
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete);
                stagedTravelMeters[i]=(cumulativeTravelMeters==null?0:cumulativeTravelMeters[i])+actor.Trajectory.PhysicalTravelDistanceAt(1,directionalGrades[i]);
                long travel=checked((long)Math.Round(stagedTravelMeters[i]*
                    SkiState.NanometersPerMeter,MidpointRounding.ToEven));
                stagedMotion[i]=new SkiState(checked(motion[i].DistanceNanometers+travel),numericalRest?0:actor.Trajectory.PhysicalSpeedAt(1,directionalGrades[i]));
            }
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }
        private ManualScopedWorldPreview PublishPrepared()
        {
            revision=checked(revision+1);ready=true;
            return new ManualScopedWorldPreview(this,revision,Tick.Next());
        }
        private void ResetNumericalDiagnostics()
        {NumericalRestCanonicalizations=MatCaptures=CourseEdgeResponses=CurvedCourseContinuations=0;MaximumCanonicalizedRestSpeed=MaximumCanonicalizedRestYaw=MatCaptureDissipatedJoules=0;
            Array.Clear(courseSupports,0,Count);Array.Clear(courseImpactCounts,0,Count);Array.Clear(matCaptureCounts,0,Count);}
        private bool TryCaptureMat(int actor,ManualRacePathCatalog catalog,ManualRacePathOccurrence row,
            ref ManualSkiHeadingState pose,ref SkiPlanarVelocity velocity)
        {
            if(matCapture==null||!IsWithinMatPlantArea(catalog,row,pose))return false;
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            if(speed>matCapture.MaximumSpeed||Math.Abs(pose.YawRateRadiansPerSecond)>matCapture.MaximumYaw)return false;
            // Planting dissipates motion against the snow. Keep the reached
            // world position, heading, paid time and physical odometer.
            MatCaptures++;matCaptureCounts[actor]++;
            double inertia=masses[actor]*(occupiedBody.LongitudinalSeparation*occupiedBody.LongitudinalSeparation+
                occupiedBody.LateralSeparation*occupiedBody.LateralSeparation)/12;
            MatCaptureDissipatedJoules+=.5*masses[actor]*speed*speed+.5*inertia*pose.YawRateRadiansPerSecond*pose.YawRateRadiansPerSecond;
            pose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,0);
            velocity=default;return true;
        }
        private bool IsWithinMatPlantArea(ManualRacePathCatalog catalog,ManualRacePathOccurrence row,ManualSkiHeadingState pose)
        {
            if(matCapture==null||row.End!=ManualRacePathEnd.FiringLine)return false;
            // Called only after the complete body, snow and phase admission.
            // A folded ribbon may observe its admitted terminal station outside
            // the centreline portal's narrow plane band. Plant that reached
            // endpoint under the existing bounded speed/spin/energy policy;
            // do not reject the race or move the body onto another plane.
            if(!catalog.TrySample(row.Index,row.LengthNanometers,out var target))return false;
            double dx=pose.X-target.X,dz=pose.Z-target.Z;
            return dx*dx+dz*dz<=matCapture.PositionTolerance*matCapture.PositionTolerance;
        }
        private bool TryCanonicalizeRest(ref ManualSkiHeadingState pose,ref SkiPlanarVelocity velocity)
        {
            if(numericalTolerance==null)return false;
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z),yaw=Math.Abs(pose.YawRateRadiansPerSecond);
            if(speed>numericalTolerance.TerminalRestSpeed||yaw>numericalTolerance.TerminalRestYaw)return false;
            if(speed!=0||yaw!=0)
            {
                NumericalRestCanonicalizations++;
                MaximumCanonicalizedRestSpeed=Math.Max(MaximumCanonicalizedRestSpeed,speed);
                MaximumCanonicalizedRestYaw=Math.Max(MaximumCanonicalizedRestYaw,yaw);
            }
            // Preserve the exact encoded position and heading. The whole raw
            // trajectory remains responsible for paid time/travel and proof.
            pose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,0);velocity=default;return true;
        }
        public ManualScopedWorldTick BeginTick()
        {
            if(tickActive)throw new InvalidOperationException("A scoped world tick is already active.");
            long next=checked(tickRevision+1);var target=Tick.Next();ready=false;UpdatePhysicalParticipants(target);ClearPathChoices();
            ResetNumericalDiagnostics();
            Array.Clear(workingImpacts,0,Count);impactTick=target.Value;
            for(int i=0;i<Count;i++)
            {workingActors[i]=accepted[i];workingCursors[i]=cursors[i];workingMotion[i]=motion[i];workingTravelMeters[i]=0;ownedFreeValid[i]=false;}
            tickRevision=next;elapsedSeconds=0;pieceCount=0;pendingContact=false;tickActive=true;LastWorldEvent=default;
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);
            return new ManualScopedWorldTick(this,tickRevision,target);
        }
        private void ValidateTick(ManualScopedWorldTick tick)
        {
            if(!tickActive||!ReferenceEquals(tick.Owner,this)||tick.Revision!=tickRevision||tick.Tick.Value!=checked(Tick.Value+1))
                throw new ArgumentException("Missing, foreign, aborted or finalized staged world tick.");
        }
        public double GetElapsedSeconds(ManualScopedWorldTick tick){ValidateTick(tick);return elapsedSeconds;}
        public double GetRemainingSeconds(ManualScopedWorldTick tick){ValidateTick(tick);return duration-elapsedSeconds;}
        public bool HasPendingContact(ManualScopedWorldTick tick){ValidateTick(tick);return pendingContact;}
        public ManualRacePathActorFrame GetWorkingActor(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingActors[index];}
        public ManualRacePathCursor GetWorkingCursor(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingCursors[index];}
        public SkiState GetWorkingPhysicalMotion(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingMotion[index];}
        public int GetWorkingMotionPieceCount(ManualScopedWorldTick tick){ValidateTick(tick);return pieceCount;}
        public ManualScopedWorldMotionPiece GetWorkingMotionPiece(ManualScopedWorldTick tick,int actor,int piece)
        {ValidateTick(tick);return GetPiece(actor,piece);}
        /// <summary>Stage a complete common interval. Rejection retains earlier
        /// staged intervals but changes no accepted state or clock. The caller
        /// must resolve/refine the failing interval, never replace it with a wait.</summary>
        public bool TryAppendInterval(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades)
            =>AppendInterval(tick,actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,Array.Empty<SkiReachedContactPair>(),0);
        public bool TryAppendArrivalInterval(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,SkiReachedContactPair[] arriving,int arrivingCount)
        {
            ValidateTick(tick);
            if(response==null)throw new InvalidOperationException("Contact response policy and athlete masses must be bound before arrival admission.");
            if(arriving==null||arrivingCount<1||arrivingCount>arriving.Length)throw new ArgumentException("At least one exact terminal contact required.");
            return AppendInterval(tick,actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,arriving,arrivingCount);
        }
        /// <summary>Schedule the next actual body arrival and stage its admitted
        /// common prefix, or pay the entire clear interval. Caller must resolve a
        /// pending response and continue the remaining fixed clock; failure is
        /// provisional and cannot be converted into a standing tick.</summary>
        public bool TryAppendToFirstContact(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,out SkiContactSchedulePreview schedule)
        {
            ValidateTick(tick);schedule=default;ready=false;
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(actors==null||actors.Length!=Count)throw new ArgumentException("Exact complete contact interval roster required.");
            double seconds=actors[0].Trajectory.DurationSeconds;
            if(!(seconds>0)||seconds>duration-elapsedSeconds)return Reject(ManualScopedWorldStatus.SourceMismatch);
            schedule=scheduler.Preview(actors,Count,reached,reachedCount,edges,edgeCount,contactParticipants);
            if(schedule.Status==SkiContactScheduleStatus.Clear)
                return TryAppendInterval(tick,actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades);
            if(schedule.Status==SkiContactScheduleStatus.ForceRefresh)
            {
                for(int i=0;i<Count;i++)scheduledPrefix[i]=scheduler.GetPrefix(i);
                return TryAppendInterval(tick,scheduledPrefix,windows,reached,reachedCount,edges,edgeCount,directionalGrades);
            }
            if(schedule.Status!=SkiContactScheduleStatus.Arrival)
                return Reject(ManualScopedWorldStatus.ContactSchedule,schedule.First,schedule.Admission);
            for(int i=0;i<Count;i++)scheduledPrefix[i]=scheduler.GetPrefix(i);
            for(int i=0;i<schedule.ArrivingPairs;i++)scheduledArrivals[i]=scheduler.GetArrival(i);
            return TryAppendArrivalInterval(tick,scheduledPrefix,windows,reached,reachedCount,edges,edgeCount,directionalGrades,scheduledArrivals,schedule.ArrivingPairs);
        }
        private bool AppendInterval(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,SkiReachedContactPair[] arriving,int arrivingCount)
        {
            ValidateTick(tick);ready=false;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(pieceCount>=maximumPieces)return Reject(ManualScopedWorldStatus.IntervalLimit);
            if(actors==null||actors.Length!=Count)throw new ArgumentException("Exact full interval roster required.");
            double seconds=actors[0].Trajectory.DurationSeconds;
            if(!(seconds>0)||seconds>duration-elapsedSeconds)return Reject(ManualScopedWorldStatus.SourceMismatch);
            if(!PrepareInterval(actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,
                workingActors,workingCursors,seconds,workingTravelMeters,arriving,arrivingCount))return false;
            double nextElapsed=elapsedSeconds+seconds;
            for(int i=0;i<Count;i++)
            {
                pieces[i*maximumPieces+pieceCount]=new ManualScopedWorldMotionPiece(workingCursors[i],stagedProgress[i].Cursor,
                    workingMotion[i],stagedMotion[i],elapsedSeconds,nextElapsed);
                workingActors[i]=staged[i];workingCursors[i]=stagedProgress[i].Cursor;workingMotion[i]=stagedMotion[i];workingTravelMeters[i]=stagedTravelMeters[i];
            }
            pieceCount++;
            elapsedSeconds=nextElapsed;pendingContact=arrivingCount>0;
            if(pendingContact)LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.PendingContact);
            return true;
        }
        /// <summary>Resolve reached contacts at unchanged scratch poses. No
        /// position correction, clock, odometer or progress is paid by an impulse.
        /// Whole-race events remain part of the owning session transaction.</summary>
        public bool TryResolveArrivals(ManualScopedWorldTick tick)
        {
            ValidateTick(tick);ready=false;
            if(!pendingContact)throw new InvalidOperationException("No reached terminal contact awaits response.");
            for(int i=0;i<Count;i++)
            {var actor=workingActors[i];responseBodies[i]=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,masses[i],IsGroundedMat(i));}
            var result=response.Preview(responseBodies,Count,responseMotions,responseOrder,contactParticipants);
            if(result.Status!=SkiContactGroupStatus.Resolved)return Reject(ManualScopedWorldStatus.ContactResponse,response:result);
            for(int i=0;i<Count;i++)
            {
                var actor=workingActors[i];var output=responseMotions[i];var v=output.Velocity;
                workingImpacts[i]=workingImpacts[i].Include(v.X-actor.Velocity.X,v.Z-actor.Velocity.Z,output.YawRate-actor.Pose.YawRateRadiansPerSecond);
                var pose=new ManualSkiHeadingState(actor.Pose.X,actor.Pose.Z,actor.Pose.HeadingRadians,output.YawRate);
                staged[i]=new ManualRacePathActorFrame(actor.Athlete,pose,v,actor.Observation);
                double speed=Math.Sqrt(v.X*v.X+v.Z*v.Z);
                double grade=speed==0?0:actor.Observation.GeometricGrade*(v.X*actor.Observation.ForwardX+v.Z*actor.Observation.ForwardZ)/speed;
                stagedMotion[i]=new SkiState(workingMotion[i].DistanceNanometers,speed*Math.Sqrt(1+grade*grade));
            }
            for(int i=0;i<Count;i++){workingActors[i]=staged[i];workingMotion[i]=stagedMotion[i];}
            pendingContact=false;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear,response:result);return true;
        }
        public bool TryHandoffOrdinary(ManualScopedWorldTick tick,ReadOnlySpan<int> actorIndexes)
        {ValidateTick(tick);return !pendingContact&&HandoffOrdinary(workingActors,workingCursors,actorIndexes);}
        /// <summary>Only a complete staged fixed clock can become a world proof.
        /// No tolerance rounds up an unpaid interval. Race sessions still validate
        /// and commit their lifecycle/physiology/events before world publication.</summary>
        public bool TryFinalizeTick(ManualScopedWorldTick tick,out ManualScopedWorldPreview proposal)
        {
            ValidateTick(tick);proposal=default;
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(elapsedSeconds!=duration)return Reject(ManualScopedWorldStatus.IncompleteTick);
            for(int i=0;i<Count;i++){staged[i]=workingActors[i];proposedCursors[i]=workingCursors[i];stagedMotion[i]=workingMotion[i];}
            proposal=PublishPrepared();tickActive=false;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }
        public void AbortTick(ManualScopedWorldTick tick)
        {ValidateTick(tick);tickActive=false;ready=false;pendingContact=false;elapsedSeconds=0;pieceCount=0;ClearPathChoices();LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);}
        public void DiscardPrepared(ManualScopedWorldPreview proposal)
        {ValidatePrepared(proposal);ready=false;pendingContact=false;elapsedSeconds=0;pieceCount=0;ClearPathChoices();LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);}
        /// <summary>World-only commit. The full race must validate all session
        /// lifecycle/physiology proposals before calling this exactly once.</summary>
        public void Commit(ManualScopedWorldPreview proposal)
        {
            ValidatePrepared(proposal);ready=false;
            for(int i=0;i<Count;i++){accepted[i]=staged[i];cursors[i]=proposedCursors[i];motion[i]=stagedMotion[i];}
            for(int i=0;i<Count;i++)acceptedImpacts[i]=impactTick==proposal.Tick.Value?workingImpacts[i]:default;
            CommitStartReleases(proposal.Tick);CommitPathChoices();Tick=proposal.Tick;
        }
        public bool TryHandoffOrdinary(ReadOnlySpan<int> actorIndexes)
        {
            if(ready||tickActive)return false;
            long nextRevision=checked(revision+1);
            if(!HandoffOrdinary(accepted,cursors,actorIndexes))return false;
            revision=nextRevision;return true;
        }
        private bool HandoffOrdinary(ManualRacePathActorFrame[] sources,ManualRacePathCursor[] sourceCursors,ReadOnlySpan<int> actorIndexes)
        {
            if(actorIndexes.Length==0)return false;
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(actorIndexes));
                for(int j=0;j<i;j++)if(actorIndexes[j]==index)throw new ArgumentException("Duplicate scoped actor handoff.");
                if(!progress[index].TryPreviewOrdinaryHandoff(sourceCursors[index],sources[index],out handoffCursors[i]))return false;
            }
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];var next=handoffCursors[i];
                if(!catalogs[index].TryProject(sources[index].Pose,next.OccurrenceIndex,0,0,out var observation))
                    throw new InvalidOperationException("A previously verified scoped endpoint changed.");
                staged[i]=new ManualRacePathActorFrame(next.Athlete,sources[index].Pose,sources[index].Velocity,observation);
            }
            for(int i=0;i<actorIndexes.Length;i++){int index=actorIndexes[i];sources[index]=staged[i];sourceCursors[index]=handoffCursors[i];}
            return true;
        }
        internal bool TryHandoffAcceptedPhases(ReadOnlySpan<int> actorIndexes,ReadOnlySpan<int> targets)
        {
            if(ready||tickActive||actorIndexes.Length==0||actorIndexes.Length!=targets.Length)return false;
            long nextRevision=checked(revision+1);
            if(!HandoffPhases(accepted,cursors,actorIndexes,targets))return false;
            revision=nextRevision;return true;
        }
        internal bool TryHandoffWorkingPhases(ManualScopedWorldTick tick,ReadOnlySpan<int> actorIndexes,ReadOnlySpan<int> targets)
        {
            ValidateTick(tick);
            if(pendingContact||actorIndexes.Length==0||actorIndexes.Length!=targets.Length)return false;
            return HandoffPhases(workingActors,workingCursors,actorIndexes,targets);
        }
        private bool HandoffPhases(ManualRacePathActorFrame[] sources,ManualRacePathCursor[] sourceCursors,
            ReadOnlySpan<int> actorIndexes,ReadOnlySpan<int> targets)
        {
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(actorIndexes));
                for(int j=0;j<i;j++)if(actorIndexes[j]==index)throw new ArgumentException("Duplicate accepted phase handoff.");
                if(!progress[index].TryPreviewPhaseHandoff(sourceCursors[index],sources[index],targets[i],out handoffCursors[i]))return false;
            }
            for(int i=0;i<actorIndexes.Length;i++)
            {
                int index=actorIndexes[i];var next=handoffCursors[i];
                if(!catalogs[index].TryProject(sources[index].Pose,next.OccurrenceIndex,0,0,out var observation))
                    throw new InvalidOperationException("Verified phase endpoint changed.");
                staged[i]=new ManualRacePathActorFrame(next.Athlete,sources[index].Pose,sources[index].Velocity,observation);
            }
            for(int i=0;i<actorIndexes.Length;i++)
            {int index=actorIndexes[i];sources[index]=staged[i];sourceCursors[index]=handoffCursors[i];}
            return true;
        }
    }
}
