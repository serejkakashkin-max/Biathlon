using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Owned, event-free all-actor world/route proposal. Not a complete
    /// race step: physiology, odometer, range and lifecycle are still separate.</summary>
    public readonly struct ManualWorldRosterPreview
    {
        internal ManualWorldRosterLedger Owner { get; }
        internal long Revision { get; }
        public bool IsValid => Owner!=null;
        public SimulationTick Tick { get; }
        public ManualRouteJointPreview Joint { get; }
        internal ManualWorldRosterPreview(ManualWorldRosterLedger owner,long revision,
            SimulationTick tick,ManualRouteJointPreview joint)
        { Owner=owner;Revision=revision;Tick=tick;Joint=joint; }
    }

    /// <summary>Accepts only full, jointly clear fixed ticks for every actor.
    /// A partial contact/course prefix cannot become a standing, paid tick.</summary>
    public sealed class ManualWorldRosterLedger
    {
        private readonly ManualRouteProjectionMap map;
        private readonly ManualRouteJointSweep sweep;
        private readonly ManualRouteProgression progress;
        private readonly ManualRouteActorFrame[] accepted,staged;
        private readonly ManualRouteProgressCursor[] cursors;
        private readonly ManualRouteProgressFrame[] stagedProgress;
        private readonly SkiState[] acceptedMotion,stagedMotion;
        private readonly double duration;
        private long revision;
        private bool ready;
        public SimulationTick Tick { get; private set; }
        public int Count => accepted.Length;
        public bool HasPhysicalOdometer => acceptedMotion!=null;
        public string ContextJson { get; }

        public ManualWorldRosterLedger(RaceRoute route,ManualRouteProjectionMap map,
            ManualRouteJointSweep sweep,ManualRouteProgression progress,
            ManualRouteProgressCursor[] initialCursors,ManualSkiHeadingState[] initialPoses,
            SkiPlanarVelocity[] initialVelocities,int tickFrequency,SkiState[] initialMotion=null)
        {
            if(route==null||map==null||sweep==null||progress==null||initialCursors==null||
                initialPoses==null||initialVelocities==null)throw new ArgumentNullException();
            if(!map.IsBoundTo(route)||!sweep.IsBoundTo(map)||!progress.IsBoundTo(route)||
                tickFrequency<=0||initialCursors.Length==0||
                initialCursors.Length!=initialPoses.Length||initialCursors.Length!=initialVelocities.Length||
                initialMotion!=null&&initialMotion.Length!=initialCursors.Length)
                throw new ArgumentException("Exact route solvers, roster and fixed clock required.");
            this.map=map;this.sweep=sweep;this.progress=progress;duration=1.0/tickFrequency;
            accepted=new ManualRouteActorFrame[initialCursors.Length];staged=new ManualRouteActorFrame[Count];
            cursors=new ManualRouteProgressCursor[Count];stagedProgress=new ManualRouteProgressFrame[Count];
            if(initialMotion!=null)
            {acceptedMotion=(SkiState[])initialMotion.Clone();stagedMotion=new SkiState[Count];}
            var context=new string[Count+4+(initialMotion==null?0:Count+1)];
            context[0]=initialMotion==null?"manual_full_clear_world_roster_v1":
                "manual_full_clear_world_odometer_roster_v2";
            context[1]=sweep.ContextJson;context[2]=progress.ContextJson;
            context[3]=tickFrequency.ToString(System.Globalization.CultureInfo.InvariantCulture);
            for(int i=0;i<Count;i++)
            {
                var cursor=initialCursors[i];var pose=initialPoses[i];
                if(!cursor.IsValid||!pose.IsValid||
                    !map.TryProject(pose,cursor.SectionIndex,cursor.ObservedStationNanometers,0,false,
                        out var observation)||observation.RouteNanometers!=cursor.ObservedStationNanometers)
                    throw new ArgumentException("Accepted initial world pose must match its selected route cursor.");
                for(int j=0;j<i;j++)if(cursor.Athlete.Equals(cursors[j].Athlete))
                    throw new ArgumentException("Duplicate athlete in accepted world roster.");
                accepted[i]=new ManualRouteActorFrame(cursor.Athlete,pose,initialVelocities[i],observation);
                cursors[i]=cursor;context[i+4]=cursor.Athlete.Value;
            }
            if(initialMotion!=null)
            {
                context[Count+4]="force_slope_distance;contact_linear_velocity_simpson_64;grade_frozen_per_tick";
                for(int i=0;i<Count;i++)context[Count+5+i]=
                    initialMotion[i].DistanceNanometers.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+
                    initialMotion[i].SpeedMetersPerSecond.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            }
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }

        public ManualRouteActorFrame GetActor(int index)=>accepted[index];
        public ManualRouteProgressCursor GetCursor(int index)=>cursors[index];
        public SkiState GetPhysicalMotion(int index)=>acceptedMotion==null
            ?throw new InvalidOperationException("Physical odometer was not bound to this roster.")
            :acceptedMotion[index];

        /// <summary>Caller supplies every actor in stable roster order. An unresolved
        /// body/course interval or station limit invalidates the entire proposal;
        /// no accepted state or tick changes.</summary>
        public bool TryPrepare(SkiWorldMotionCandidate[] actors,ManualRouteActorWindow[] windows,
            long[] maximumStationsNanometers,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,out ManualWorldRosterPreview proposal)
            =>TryPrepareCore(actors,windows,maximumStationsNanometers,reached,reachedCount,
                edges,edgeCount,null,out proposal);

        /// <summary>Bind physical ski distance to the same full-clear world tick.
        /// Grades are the observed directional grades for post-contact motion;
        /// force-driven motion retains its own exact force-trajectory distance.</summary>
        public bool TryPrepareWithOdometer(SkiWorldMotionCandidate[] actors,ManualRouteActorWindow[] windows,
            long[] maximumStationsNanometers,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,
            out ManualWorldRosterPreview proposal)
        {
            if(acceptedMotion==null)throw new InvalidOperationException("Bind initial physical motion first.");
            if(directionalGrades==null||directionalGrades.Length!=Count)
                throw new ArgumentException("Every actor needs an observed directional grade.");
            return TryPrepareCore(actors,windows,maximumStationsNanometers,reached,reachedCount,
                edges,edgeCount,directionalGrades,out proposal);
        }

        private bool TryPrepareCore(SkiWorldMotionCandidate[] actors,ManualRouteActorWindow[] windows,
            long[] maximumStationsNanometers,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,
            out ManualWorldRosterPreview proposal)
        {
            ready=false;proposal=default;
            if(acceptedMotion!=null&&directionalGrades==null)
                throw new InvalidOperationException("Physical roster requires an odometer proposal on every tick.");
            if(actors==null||windows==null||maximumStationsNanometers==null||
                actors.Length!=Count||windows.Length!=Count||maximumStationsNanometers.Length!=Count)
                throw new ArgumentException("Exact complete world roster and station limits required.");
            for(int i=0;i<Count;i++)
            {
                var actor=actors[i];var window=windows[i];var cursor=cursors[i];
                if(!actor.Trajectory.IsValid||!actor.Athlete.Equals(cursor.Athlete)||
                    !window.Athlete.Equals(cursor.Athlete)||
                    window.SectionIndex!=cursor.SectionIndex||
                    window.PreviousStationNanometers!=cursor.ObservedStationNanometers||
                    !SamePose(actor.Trajectory.Sample(0),accepted[i].Pose)||
                    !SameVelocity(actor.Trajectory.VelocityAt(0),accepted[i].Velocity)||
                    Math.Abs(actor.Trajectory.DurationSeconds-duration)>duration*1e-12)
                    return false;
            }
            if(!sweep.TryPreviewCommonPrefixFrames(actors,Count,reached,reachedCount,windows,
                edges,edgeCount,staged,out var joint)||
                joint.Status!=ManualRouteJointStatus.Clear||
                Math.Abs(joint.ClearUntilSeconds-duration)>duration*1e-12||
                !progress.TryPreviewGroup(staged,cursors,maximumStationsNanometers,Count,stagedProgress))
                return false;
            if(acceptedMotion!=null)
            {
                for(int i=0;i<Count;i++)
                {
                    double grade=directionalGrades[i];
                    var trajectory=actors[i].Trajectory;
                    double travel=trajectory.PhysicalTravelDistanceAt(1,grade);
                    double speed=trajectory.PhysicalSpeedAt(1,grade);
                    long increment=checked((long)Math.Round(travel*SkiState.NanometersPerMeter,
                        MidpointRounding.ToEven));
                    stagedMotion[i]=new SkiState(checked(acceptedMotion[i].DistanceNanometers+increment),speed);
                }
            }
            revision=checked(revision+1);ready=true;
            proposal=new ManualWorldRosterPreview(this,revision,Tick.Next(),joint);
            return true;
        }

        private static bool SamePose(ManualSkiHeadingState a,ManualSkiHeadingState b)
            =>a.IsValid&&b.IsValid&&a.X==b.X&&a.Z==b.Z&&
              a.HeadingNanoradians==b.HeadingNanoradians&&
              a.YawRateRadiansPerSecond==b.YawRateRadiansPerSecond;
        private static bool SameVelocity(SkiPlanarVelocity a,SkiPlanarVelocity b)
            =>a.X==b.X&&a.Z==b.Z;

        /// <summary>Commit only the owner-held complete proposal once. No race
        /// events or physical ski odometer are inferred from projected station.</summary>
        public void Commit(ManualWorldRosterPreview proposal)
        {
            if(!ready||!ReferenceEquals(proposal.Owner,this)||proposal.Revision!=revision||
                proposal.Tick.Value!=Tick.Value+1)
                throw new ArgumentException("Missing, foreign, replaced or consumed world roster proposal.");
            ready=false;
            for(int i=0;i<Count;i++)
            { accepted[i]=staged[i];cursors[i]=stagedProgress[i].Cursor;
              if(acceptedMotion!=null)acceptedMotion[i]=stagedMotion[i]; }
            Tick=proposal.Tick;
        }
    }
}
