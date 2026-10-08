using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
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
    public enum ManualScopedWorldStatus { NotPrepared, Clear, SourceMismatch, BodyOrEdge, Course, Projection, PhaseLimit, IncompleteTick }
    public readonly struct ManualScopedWorldAttempt
    {
        public ManualScopedWorldStatus Status {get;}
        public AthleteId Athlete {get;}
        public SkiJointSweepPreview BodyOrEdge {get;}
        public ManualRouteTubePreview Course {get;}
        public ManualRacePathGatePreview Gate {get;}
        internal ManualScopedWorldAttempt(ManualScopedWorldStatus status,AthleteId athlete=default,
            SkiJointSweepPreview body=default,ManualRouteTubePreview course=default,ManualRacePathGatePreview gate=default)
        {Status=status;Athlete=athlete;BodyOrEdge=body;Course=course;Gate=gate;}
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
    /// Contact continuation and session/lifecycle commit remain separate.</summary>
    public sealed class ManualScopedWorldRoster
    {
        private readonly ManualRacePathCatalog[] catalogs;
        private readonly ManualRacePathProgression[] progress;
        private readonly ManualRouteTubeSweep[] courses;
        private readonly ManualRacePathGateSweep[] gates;
        private readonly ManualSkiJointSweep bodies;
        private readonly ManualRacePathActorFrame[] accepted,staged;
        private readonly ManualRacePathActorFrame[] workingActors;
        private readonly ManualRacePathCursor[] cursors,handoffCursors;
        private readonly ManualRacePathCursor[] workingCursors,proposedCursors;
        private readonly ManualRacePathProgressFrame[] stagedProgress;
        private readonly SkiState[] motion,stagedMotion;
        private readonly SkiState[] workingMotion;
        private readonly double[] workingTravelMeters,stagedTravelMeters;
        private readonly double duration;
        private long revision;
        private bool ready;
        private bool tickActive;
        private long tickRevision;
        private double elapsedSeconds;
        public int Count=>accepted.Length;
        public int TickFrequency {get;}
        public SimulationTick Tick {get;private set;}
        public string ContextJson {get;}
        public string InitialStateContextJson {get;}
        public ManualScopedWorldAttempt LastAttempt {get;private set;}
        public ManualScopedWorldRoster(ManualRacePathCatalog[] catalogs,OccupancyDefinition body,
            SkiContactDefinition contact,SkiContactSweepDefinition work,ManualRacePathCursor[] initialCursors,
            ManualSkiHeadingState[] poses,SkiPlanarVelocity[] velocities,SkiState[] initialMotion,
            int tickFrequency,SimulationTick initialTick=default)
        {
            if(catalogs==null||body==null||contact==null||work==null||initialCursors==null||poses==null||
                velocities==null||initialMotion==null)throw new ArgumentNullException();
            int count=catalogs.Length;
            if(count==0||initialCursors.Length!=count||poses.Length!=count||velocities.Length!=count||
                initialMotion.Length!=count||tickFrequency<=0)throw new ArgumentException("Complete scoped roster and fixed clock required.");
            this.catalogs=(ManualRacePathCatalog[])catalogs.Clone();TickFrequency=tickFrequency;duration=1.0/tickFrequency;Tick=initialTick;
            accepted=new ManualRacePathActorFrame[count];staged=new ManualRacePathActorFrame[count];
            cursors=new ManualRacePathCursor[count];handoffCursors=new ManualRacePathCursor[count];
            stagedProgress=new ManualRacePathProgressFrame[count];motion=(SkiState[])initialMotion.Clone();stagedMotion=new SkiState[count];
            workingActors=new ManualRacePathActorFrame[count];workingCursors=new ManualRacePathCursor[count];
            proposedCursors=new ManualRacePathCursor[count];workingMotion=new SkiState[count];
            workingTravelMeters=new double[count];stagedTravelMeters=new double[count];
            progress=new ManualRacePathProgression[count];courses=new ManualRouteTubeSweep[count];gates=new ManualRacePathGateSweep[count];bodies=new ManualSkiJointSweep(body,contact,work);
            var context=new string[count+6];var initial=new string[count+4];
            context[0]="manual_scoped_full_clear_world_roster_v4";context[1]=body.ToJson();
            context[2]=contact.ToJson();context[3]=work.ToJson();
            context[4]="force_exact_distance;contact_velocity_norm_simpson64;grade_frozen_per_interval;full_duration_tolerance_1e-12;no_implicit_scope_crossing;local_terminal_centre_portals_v1;whole_tube_edge_activation;continuous_acceleration_bound;existing_trajectory_encoding_reserve;staged_full_clock_intervals;exact_remaining_time;round_tick_travel_once;"+ManualSkiWorldTrajectory.SlicePolicy;
            initial[0]="manual_scoped_initial_world_roster_v1";initial[1]=tickFrequency.ToString(CultureInfo.InvariantCulture);
            initial[2]=initialTick.Value.ToString(CultureInfo.InvariantCulture);initial[3]="actor,occurrence,observed_arc,high_arc,route_credit,x,z,heading_nrad,yaw,vx,vz,odometer,speed";
            for(int i=0;i<count;i++)
            {
                var catalog=this.catalogs[i];var cursor=initialCursors[i];var pose=poses[i];var velocity=velocities[i];
                if(catalog==null||!catalog.IsSameRace(this.catalogs[0])||!cursor.IsValid||
                    !ReferenceEquals(cursor.Owner,catalog)||!pose.IsValid||
                    !catalog.TryProject(pose,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,0,out var observation)||
                    observation.PhysicalArcNanometers!=cursor.ObservedArcNanometers)
                    throw new ArgumentException("Each accepted actor must bind its exact race, path and world observation.");
                for(int j=0;j<i;j++)if(cursor.Athlete.Equals(cursors[j].Athlete))throw new ArgumentException("Duplicate scoped world actor.");
                progress[i]=new ManualRacePathProgression(catalog);courses[i]=new ManualRouteTubeSweep(catalog.ProjectionGeometry,body,work);
                gates[i]=new ManualRacePathGateSweep(catalog,work);
                cursors[i]=cursor;accepted[i]=new ManualRacePathActorFrame(cursor.Athlete,pose,velocity,observation);
                context[i+5]=catalog.ContextJson;
                string Long(long value)=>value.ToString(CultureInfo.InvariantCulture);
                string Double(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
                initial[i+4]=ShootingModelDefinitions.EncodeContext(new[]{cursor.Athlete.Value,Long(cursor.OccurrenceIndex),
                    Long(cursor.ObservedArcNanometers),Long(cursor.CreditedArcNanometers),Long(cursor.CreditedRouteNanometers),
                    Double(pose.X),Double(pose.Z),Long(pose.HeadingNanoradians),Double(pose.YawRateRadiansPerSecond),
                    Double(velocity.X),Double(velocity.Z),Long(motion[i].DistanceNanometers),Double(motion[i].SpeedMetersPerSecond)});
            }
            InitialStateContextJson=ShootingModelDefinitions.EncodeContext(initial);context[context.Length-1]=InitialStateContextJson;
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }
        public ManualRacePathActorFrame GetActor(int index)=>accepted[index];
        public ManualRacePathCursor GetCursor(int index)=>cursors[index];
        public SkiState GetPhysicalMotion(int index)=>motion[index];
        public void ValidatePrepared(ManualScopedWorldPreview proposal)
        {
            if(!ready||!ReferenceEquals(proposal.Owner,this)||proposal.Revision!=revision||proposal.Tick.Value!=checked(Tick.Value+1))
                throw new ArgumentException("Missing, foreign, stale or consumed scoped world proposal.");
        }
        public ManualRacePathActorFrame GetProposedActor(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return staged[index];}
        public ManualRacePathCursor GetProposedCursor(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return proposedCursors[index];}
        public SkiState GetProposedPhysicalMotion(ManualScopedWorldPreview proposal,int index)
        {ValidatePrepared(proposal);return stagedMotion[index];}
        private bool Reject(ManualScopedWorldStatus status,AthleteId athlete=default,
            SkiJointSweepPreview joint=default,ManualRouteTubePreview course=default,ManualRacePathGatePreview gate=default)
        {LastAttempt=new ManualScopedWorldAttempt(status,athlete,joint,course,gate);return false;}
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
            ready=false;proposal=default;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);
            if(!PrepareInterval(actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,accepted,cursors,duration,null))return false;
            for(int i=0;i<Count;i++)proposedCursors[i]=stagedProgress[i].Cursor;
            proposal=PublishPrepared();return true;
        }
        private bool PrepareInterval(SkiWorldMotionCandidate[] actors,ManualScopedWorldWindow[] windows,
            SkiReachedContactPair[] reached,int reachedCount,SkiActorCourseEdge[] edges,int edgeCount,
            double[] directionalGrades,ManualRacePathActorFrame[] sources,ManualRacePathCursor[] sourceCursors,
            double seconds,double[] cumulativeTravelMeters)
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
            }
            var joint=bodies.Preview(actors,Count,reached,reachedCount,edges,edgeCount);
            if(joint.Status!=SkiJointSweepStatus.Clear||Math.Abs(joint.ClearUntilSeconds-seconds)>seconds*1e-12)
                return Reject(ManualScopedWorldStatus.BodyOrEdge,joint.First,joint);
            for(int i=0;i<Count;i++)
            {
                var actor=actors[i];var window=windows[i];var catalog=catalogs[i];var row=catalog.Get(sourceCursors[i].OccurrenceIndex);
                if(window.MaximumAdmittedArcNanometers<sourceCursors[i].CreditedArcNanometers||window.MaximumAdmittedArcNanometers>row.LengthNanometers)
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete);
                var gate=gates[i].Preview(actor.Trajectory,row.Index,window.PreviousArcNanometers,
                    window.MaximumArcDisplacementMeters,window.MaximumAdmittedArcNanometers);
                if(gate.Status!=ManualRacePathGateStatus.Clear)
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete,gate:gate);
                var course=courses[i].Preview(actor.Trajectory,row.Index,checked(row.GeometryStartNanometers+window.PreviousArcNanometers),
                    window.MaximumArcDisplacementMeters,false);
                if(course.Status!=ManualRouteTubeStatus.Clear||Math.Abs(course.ClearUntilSeconds-seconds)>seconds*1e-12)
                    return Reject(ManualScopedWorldStatus.Course,actor.Athlete,course:course);
                var pose=actor.Trajectory.Sample(1);
                if(!catalog.TryProject(pose,row.Index,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,out var observation))
                    return Reject(ManualScopedWorldStatus.Projection,actor.Athlete);
                staged[i]=new ManualRacePathActorFrame(actor.Athlete,pose,actor.Trajectory.VelocityAt(1),observation);
                if(!progress[i].TryPreview(staged[i],sourceCursors[i],window.MaximumAdmittedArcNanometers,out stagedProgress[i]))
                    return Reject(ManualScopedWorldStatus.PhaseLimit,actor.Athlete);
                stagedTravelMeters[i]=(cumulativeTravelMeters==null?0:cumulativeTravelMeters[i])+actor.Trajectory.PhysicalTravelDistanceAt(1,directionalGrades[i]);
                long travel=checked((long)Math.Round(stagedTravelMeters[i]*
                    SkiState.NanometersPerMeter,MidpointRounding.ToEven));
                stagedMotion[i]=new SkiState(checked(motion[i].DistanceNanometers+travel),actor.Trajectory.PhysicalSpeedAt(1,directionalGrades[i]));
            }
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }
        private ManualScopedWorldPreview PublishPrepared()
        {
            revision=checked(revision+1);ready=true;
            return new ManualScopedWorldPreview(this,revision,Tick.Next());
        }
        public ManualScopedWorldTick BeginTick()
        {
            if(tickActive)throw new InvalidOperationException("A scoped world tick is already active.");
            long next=checked(tickRevision+1);var target=Tick.Next();ready=false;
            for(int i=0;i<Count;i++)
            {workingActors[i]=accepted[i];workingCursors[i]=cursors[i];workingMotion[i]=motion[i];workingTravelMeters[i]=0;}
            tickRevision=next;elapsedSeconds=0;tickActive=true;
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
        public ManualRacePathActorFrame GetWorkingActor(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingActors[index];}
        public ManualRacePathCursor GetWorkingCursor(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingCursors[index];}
        public SkiState GetWorkingPhysicalMotion(ManualScopedWorldTick tick,int index){ValidateTick(tick);return workingMotion[index];}
        /// <summary>Stage a complete common interval. Rejection retains earlier
        /// staged intervals but changes no accepted state or clock. The caller
        /// must resolve/refine the failing interval, never replace it with a wait.</summary>
        public bool TryAppendInterval(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades)
        {
            ValidateTick(tick);ready=false;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);
            if(actors==null||actors.Length!=Count)throw new ArgumentException("Exact full interval roster required.");
            double seconds=actors[0].Trajectory.DurationSeconds;
            if(!(seconds>0)||seconds>duration-elapsedSeconds)return Reject(ManualScopedWorldStatus.SourceMismatch);
            if(!PrepareInterval(actors,windows,reached,reachedCount,edges,edgeCount,directionalGrades,
                workingActors,workingCursors,seconds,workingTravelMeters))return false;
            double nextElapsed=elapsedSeconds+seconds;
            for(int i=0;i<Count;i++)
            {workingActors[i]=staged[i];workingCursors[i]=stagedProgress[i].Cursor;workingMotion[i]=stagedMotion[i];workingTravelMeters[i]=stagedTravelMeters[i];}
            elapsedSeconds=nextElapsed;return true;
        }
        public bool TryHandoffOrdinary(ManualScopedWorldTick tick,ReadOnlySpan<int> actorIndexes)
        {ValidateTick(tick);return HandoffOrdinary(workingActors,workingCursors,actorIndexes);}
        /// <summary>Only a complete staged fixed clock can become a world proof.
        /// No tolerance rounds up an unpaid interval. Race sessions still validate
        /// and commit their lifecycle/physiology/events before world publication.</summary>
        public bool TryFinalizeTick(ManualScopedWorldTick tick,out ManualScopedWorldPreview proposal)
        {
            ValidateTick(tick);proposal=default;
            if(elapsedSeconds!=duration)return Reject(ManualScopedWorldStatus.IncompleteTick);
            for(int i=0;i<Count;i++){staged[i]=workingActors[i];proposedCursors[i]=workingCursors[i];stagedMotion[i]=workingMotion[i];}
            proposal=PublishPrepared();tickActive=false;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }
        public void AbortTick(ManualScopedWorldTick tick)
        {ValidateTick(tick);tickActive=false;ready=false;elapsedSeconds=0;LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.NotPrepared);}
        /// <summary>World-only commit. The full race must validate all session
        /// lifecycle/physiology proposals before calling this exactly once.</summary>
        public void Commit(ManualScopedWorldPreview proposal)
        {
            ValidatePrepared(proposal);ready=false;
            for(int i=0;i<Count;i++){accepted[i]=staged[i];cursors[i]=proposedCursors[i];motion[i]=stagedMotion[i];}
            Tick=proposal.Tick;
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
    }
}
