using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ManualRouteActorWindow
    {
        public AthleteId Athlete {get;}
        public int SectionIndex {get;}
        public long PreviousStationNanometers {get;}
        public double MaximumStationDisplacementMeters {get;}
        public bool AdmitNextSection {get;}
        public ManualRouteActorWindow(AthleteId athlete,int section,long previousStationNanometers,
            double maximumStationDisplacementMeters,bool admitNextSection)
        {
            if(!athlete.IsValid||section<0||previousStationNanometers<0)
                throw new ArgumentException("Valid actor and route cursor required.");
            PhysiologyNumbers.RequireRange(maximumStationDisplacementMeters,0,PhysiologyNumbers.MaximumMagnitude,
                nameof(maximumStationDisplacementMeters));
            Athlete=athlete;SectionIndex=section;PreviousStationNanometers=previousStationNanometers;
            MaximumStationDisplacementMeters=maximumStationDisplacementMeters;AdmitNextSection=admitNextSection;
        }
    }
    public enum ManualRouteJointStatus { Clear, BodyOrEdge, RouteTube }
    public readonly struct ManualRouteJointPreview
    {
        public bool IsValid {get;}
        public ManualRouteJointStatus Status {get;}
        public double ClearUntilSeconds {get;}
        public AthleteId Athlete {get;}
        public SkiJointSweepPreview BodyOrEdge {get;}
        public ManualRouteTubePreview Tube {get;}
        internal ManualRouteJointPreview(ManualRouteJointStatus status,double clear,AthleteId athlete=default,
            SkiJointSweepPreview bodyOrEdge=default,ManualRouteTubePreview tube=default)
        {IsValid=true;Status=status;ClearUntilSeconds=clear;Athlete=athlete;BodyOrEdge=bodyOrEdge;Tube=tube;}
    }

    /// <summary>One common non-committing prefix for the actual world bodies,
    /// explicit local edges, and selected-route inner tubes. The caller retains
    /// ownership of contact response, topology admission and race events.</summary>
    public sealed class ManualRouteJointSweep
    {
        private readonly ManualRouteProjectionMap route;
        private readonly ManualSkiJointSweep bodies;
        private readonly ManualRouteTubeSweep course;
        public string ContextJson {get;}
        public ManualRouteJointSweep(ManualRouteProjectionMap route,OccupancyDefinition body,
            SkiContactDefinition contact,SkiContactSweepDefinition work)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            bodies=new ManualSkiJointSweep(body,contact,work);
            course=new ManualRouteTubeSweep(route,body,work);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_route_body_tube_common_prefix_v1",
                bodies.ContextJson,course.ContextJson});
        }
        public ManualRouteJointPreview Preview(SkiWorldMotionCandidate[] actors,int count,
            SkiReachedContactPair[] reached,int reachedCount,ManualRouteActorWindow[] routeWindows,
            SkiActorCourseEdge[] additionalEdges,int additionalEdgeCount)
        {
            if(actors==null||routeWindows==null||additionalEdges==null||count<1||count>actors.Length||
                routeWindows.Length!=count)throw new ArgumentException("Every motion actor needs one route window.");
            // Validate the exact stable-ID bijection before asking either solver.
            // Never silently omit a moving athlete from a body or course check.
            for(int i=0;i<count;i++)
            {
                if(!routeWindows[i].Athlete.IsValid)throw new ArgumentException("Invalid route actor.");
                int found=0;
                for(int j=0;j<count;j++)if(routeWindows[i].Athlete.Equals(actors[j].Athlete))found++;
                if(found!=1)throw new ArgumentException("Foreign or duplicate route actor.");
                for(int j=0;j<i;j++)if(routeWindows[i].Athlete.Equals(routeWindows[j].Athlete))
                    throw new ArgumentException("Duplicate route window.");
            }
            var joint=bodies.Preview(actors,count,reached,reachedCount,additionalEdges,additionalEdgeCount);
            var best=joint.Status==SkiJointSweepStatus.Clear
                ?new ManualRouteJointPreview(ManualRouteJointStatus.Clear,joint.ClearUntilSeconds)
                :new ManualRouteJointPreview(ManualRouteJointStatus.BodyOrEdge,joint.ClearUntilSeconds,
                    joint.First,bodyOrEdge:joint);
            for(int i=0;i<count;i++)
            {
                var actor=actors[i];ManualRouteActorWindow scope=default;
                for(int j=0;j<count;j++)if(routeWindows[j].Athlete.Equals(actor.Athlete))
                {scope=routeWindows[j];break;}
                var tube=course.Preview(actor.Trajectory,scope.SectionIndex,scope.PreviousStationNanometers,
                    scope.MaximumStationDisplacementMeters,scope.AdmitNextSection);
                if(tube.Status==ManualRouteTubeStatus.Clear||tube.ClearUntilSeconds>best.ClearUntilSeconds)continue;
                if(tube.ClearUntilSeconds==best.ClearUntilSeconds&&best.Status!=ManualRouteJointStatus.Clear&&
                    (best.Status==ManualRouteJointStatus.BodyOrEdge||
                        string.CompareOrdinal(actor.Athlete.Value,best.Athlete.Value)>=0))continue;
                best=new ManualRouteJointPreview(ManualRouteJointStatus.RouteTube,tube.ClearUntilSeconds,
                    actor.Athlete,tube:tube);
            }
            return best;
        }

        /// <summary>Observe every candidate at the common proven prefix. This
        /// does not commit the poses, grant odometer distance, choose a branch,
        /// or emit race events. A failed local projection leaves output untouched.</summary>
        public bool TryProjectCommonPrefix(SkiWorldMotionCandidate[] actors,int count,
            SkiReachedContactPair[] reached,int reachedCount,ManualRouteActorWindow[] routeWindows,
            SkiActorCourseEdge[] additionalEdges,int additionalEdgeCount,
            ManualRouteProjection[] projections,out ManualRouteJointPreview joint)
        {
            if(projections==null||count<1||projections.Length<count)
                throw new ArgumentException("One projection slot per actor required.",nameof(projections));
            joint=Preview(actors,count,reached,reachedCount,routeWindows,additionalEdges,additionalEdgeCount);
            // Validate the entire roster before writing a slot. In particular,
            // one athlete outside its selected station window cannot leave a
            // partial commit-looking array for the caller to consume.
            for(int i=0;i<count;i++)
                if(!TryProjectActor(actors[i],routeWindows,count,joint.ClearUntilSeconds,out _))return false;
            for(int i=0;i<count;i++)
            {
                TryProjectActor(actors[i],routeWindows,count,joint.ClearUntilSeconds,out var observation);
                projections[i]=observation;
            }
            return true;
        }

        private bool TryProjectActor(SkiWorldMotionCandidate actor,ManualRouteActorWindow[] windows,int count,
            double safeSeconds,out ManualRouteProjection observation)
        {
            ManualRouteActorWindow window=default;
            for(int j=0;j<count;j++)if(windows[j].Athlete.Equals(actor.Athlete))
            {window=windows[j];break;}
            double fraction=safeSeconds/actor.Trajectory.DurationSeconds;
            return route.TryProject(actor.Trajectory.Sample(fraction),window.SectionIndex,
                window.PreviousStationNanometers,window.MaximumStationDisplacementMeters,
                window.AdmitNextSection,out observation);
        }
    }
}
