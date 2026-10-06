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
        private readonly double duration;
        private long revision;
        private bool ready;
        public SimulationTick Tick { get; private set; }
        public int Count => accepted.Length;
        public string ContextJson { get; }

        public ManualWorldRosterLedger(RaceRoute route,ManualRouteProjectionMap map,
            ManualRouteJointSweep sweep,ManualRouteProgression progress,
            ManualRouteProgressCursor[] initialCursors,ManualSkiHeadingState[] initialPoses,
            SkiPlanarVelocity[] initialVelocities,int tickFrequency)
        {
            if(route==null||map==null||sweep==null||progress==null||initialCursors==null||
                initialPoses==null||initialVelocities==null)throw new ArgumentNullException();
            if(!map.IsBoundTo(route)||!sweep.IsBoundTo(map)||!progress.IsBoundTo(route)||
                tickFrequency<=0||initialCursors.Length==0||
                initialCursors.Length!=initialPoses.Length||initialCursors.Length!=initialVelocities.Length)
                throw new ArgumentException("Exact route solvers, roster and fixed clock required.");
            this.map=map;this.sweep=sweep;this.progress=progress;duration=1.0/tickFrequency;
            accepted=new ManualRouteActorFrame[initialCursors.Length];staged=new ManualRouteActorFrame[Count];
            cursors=new ManualRouteProgressCursor[Count];stagedProgress=new ManualRouteProgressFrame[Count];
            var context=new string[Count+4];context[0]="manual_full_clear_world_roster_v1";
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
            ContextJson=ShootingModelDefinitions.EncodeContext(context);
        }

        public ManualRouteActorFrame GetActor(int index)=>accepted[index];
        public ManualRouteProgressCursor GetCursor(int index)=>cursors[index];

        /// <summary>Caller supplies every actor in stable roster order. An unresolved
        /// body/course interval or station limit invalidates the entire proposal;
        /// no accepted state or tick changes.</summary>
        public bool TryPrepare(SkiWorldMotionCandidate[] actors,ManualRouteActorWindow[] windows,
            long[] maximumStationsNanometers,SkiReachedContactPair[] reached,int reachedCount,
            SkiActorCourseEdge[] edges,int edgeCount,out ManualWorldRosterPreview proposal)
        {
            ready=false;proposal=default;
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
            { accepted[i]=staged[i];cursors[i]=stagedProgress[i].Cursor; }
            Tick=proposal.Tick;
        }
    }
}
