using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiWorldMotionCandidate
    {
        public AthleteId Athlete {get;}
        public ManualSkiWorldTrajectory Trajectory {get;}
        public SkiWorldMotionCandidate(AthleteId athlete,ManualSkiWorldTrajectory trajectory)
        {
            if(!athlete.IsValid||!trajectory.IsValid)throw new ArgumentException("Initialized actor trajectory required.");
            Athlete=athlete;Trajectory=trajectory;
        }
    }
    public readonly struct SkiReachedContactPair
    {
        public AthleteId First {get;}
        public AthleteId Second {get;}
        public SkiReachedContactPair(AthleteId first,AthleteId second)
        {
            if(!first.IsValid||!second.IsValid||first.Equals(second))throw new ArgumentException("Distinct reached-contact actors required.");
            bool swap=string.CompareOrdinal(first.Value,second.Value)>0;First=swap?second:first;Second=swap?first:second;
        }
    }
    public readonly struct SkiActorCourseEdge
    {
        public AthleteId Athlete {get;}
        public SkiCourseEdgePlane Edge {get;}
        public SkiActorCourseEdge(AthleteId athlete,SkiCourseEdgePlane edge)
        {
            if(!athlete.IsValid||!edge.IsValid)throw new ArgumentException("Initialized actor and edge required.");
            Athlete=athlete;Edge=edge;
        }
    }
    public enum SkiJointSweepStatus { Clear, PairInterval, EdgeInterval }
    public readonly struct SkiJointSweepPreview
    {
        public bool IsValid {get;}
        public SkiJointSweepStatus Status {get;}
        public double ClearUntilSeconds {get;}
        public AthleteId First {get;}
        public AthleteId Second {get;}
        public string EdgeId {get;}
        public SkiContactSweepPreview Pair {get;}
        public SkiEdgeSweepPreview Edge {get;}
        internal SkiJointSweepPreview(SkiJointSweepStatus status,double clear,AthleteId first=default,AthleteId second=default,
            string edgeId=null,SkiContactSweepPreview pair=default,SkiEdgeSweepPreview edge=default)
        {IsValid=true;Status=status;ClearUntilSeconds=clear;First=first;Second=second;EdgeId=edgeId;Pair=pair;Edge=edge;}
    }

    /// <summary>One common continuous prefix for all proposed trajectories and their
    /// explicitly admitted convex-cell edges. Does not commit poses or race progress.</summary>
    public sealed class ManualSkiJointSweep
    {
        private readonly ManualSkiContactSweep pairs;
        private readonly ManualSkiEdgeSweep edges;
        public string ContextJson {get;}
        public ManualSkiJointSweep(OccupancyDefinition body,SkiContactDefinition contact,SkiContactSweepDefinition definition)
        {
            pairs=new ManualSkiContactSweep(body,contact,definition);edges=new ManualSkiEdgeSweep(body,contact,definition);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_joint_world_prefix_v1",pairs.SpatialContextJson,edges.ContextJson});
        }
        public SkiJointSweepPreview Preview(SkiWorldMotionCandidate[] actors,int count,
            SkiReachedContactPair[] reached,int reachedCount,SkiActorCourseEdge[] boundaries,int boundaryCount)
        {
            if(actors==null||reached==null||boundaries==null||count<1||count>actors.Length||reachedCount<0||reachedCount>reached.Length||boundaryCount<0||boundaryCount>boundaries.Length)
                throw new ArgumentException("Valid motion/contact/edge array slices required.");
            double duration=actors[0].Trajectory.DurationSeconds;
            for(int i=0;i<count;i++)
            {
                if(!actors[i].Athlete.IsValid||!actors[i].Trajectory.IsValid||actors[i].Trajectory.DurationSeconds!=duration)
                    throw new ArgumentException("All candidates require the same valid time interval.");
                for(int j=0;j<i;j++)if(actors[i].Athlete.Equals(actors[j].Athlete))throw new ArgumentException("Duplicate motion actor.");
            }
            for(int i=0;i<reachedCount;i++)
            {
                if(Find(reached[i].First)<0||Find(reached[i].Second)<0||reached[i].First.Equals(reached[i].Second))throw new ArgumentException("Foreign reached contact.");
                for(int j=0;j<i;j++)if(reached[i].First.Equals(reached[j].First)&&reached[i].Second.Equals(reached[j].Second))throw new ArgumentException("Duplicate reached contact.");
            }
            for(int i=0;i<boundaryCount;i++)
            {
                if(Find(boundaries[i].Athlete)<0||!boundaries[i].Edge.IsValid)throw new ArgumentException("Foreign or invalid course edge.");
                for(int j=0;j<i;j++)if(boundaries[i].Athlete.Equals(boundaries[j].Athlete)&&boundaries[i].Edge.Id==boundaries[j].Edge.Id)
                    throw new ArgumentException("Duplicate actor edge identity.");
            }
            var earliest=new SkiJointSweepPreview(SkiJointSweepStatus.Clear,duration);
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
            {
                var a=actors[i];var b=actors[j];
                if(string.CompareOrdinal(a.Athlete.Value,b.Athlete.Value)>0){var swap=a;a=b;b=swap;}
                bool continuing=false;
                for(int k=0;k<reachedCount;k++)if(reached[k].First.Equals(a.Athlete)&&reached[k].Second.Equals(b.Athlete)){continuing=true;break;}
                var result=continuing?pairs.PreviewExistingContact(a.Athlete,a.Trajectory,b.Athlete,b.Trajectory):pairs.Preview(a.Athlete,a.Trajectory,b.Athlete,b.Trajectory);
                if(result.Status!=SkiContactSweepStatus.Clear)
                    Consider(new SkiJointSweepPreview(SkiJointSweepStatus.PairInterval,result.ClearUntilSeconds,a.Athlete,b.Athlete,pair:result));
            }
            for(int i=0;i<boundaryCount;i++)
            {
                var edge=boundaries[i];var result=edges.Preview(actors[Find(edge.Athlete)].Trajectory,edge.Edge);
                if(result.Status!=SkiEdgeSweepStatus.Clear)
                    Consider(new SkiJointSweepPreview(SkiJointSweepStatus.EdgeInterval,result.ClearUntilSeconds,edge.Athlete,edgeId:edge.Edge.Id,edge:result));
            }
            return earliest;

            int Find(AthleteId id)
            {for(int i=0;i<count;i++)if(actors[i].Athlete.Equals(id))return i;return -1;}
            void Consider(SkiJointSweepPreview candidate)
            {
                if(candidate.ClearUntilSeconds>earliest.ClearUntilSeconds)return;
                if(candidate.ClearUntilSeconds==earliest.ClearUntilSeconds&&earliest.Status!=SkiJointSweepStatus.Clear)
                {
                    // Fixed tie order: event kind, first/second stable ID, edge ID.
                    // The result is independent of actor/edge input enumeration.
                    int compare=(int)candidate.Status-(int)earliest.Status;
                    if(compare==0)compare=string.CompareOrdinal(candidate.First.Value,earliest.First.Value);
                    if(compare==0)compare=string.CompareOrdinal(candidate.Second.Value,earliest.Second.Value);
                    if(compare==0)compare=string.CompareOrdinal(candidate.EdgeId,earliest.EdgeId);
                    if(compare>=0)return;
                }
                earliest=candidate;
            }
        }
    }
}
