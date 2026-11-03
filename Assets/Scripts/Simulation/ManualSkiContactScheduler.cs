using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum SkiContactScheduleStatus { Clear, Arrival, InitialProximity, Unresolved, ForceRefresh }
    public readonly struct SkiContactSchedulePreview
    {
        public bool IsValid {get;}
        public SkiContactScheduleStatus Status {get;}
        public double Seconds {get;}
        public int ArrivingPairs {get;}
        public AthleteId First {get;} public AthleteId Second {get;}
        public SkiJointSweepPreview Admission {get;}
        internal SkiContactSchedulePreview(SkiContactScheduleStatus status,double seconds,int count=0,
            AthleteId first=default,AthleteId second=default,SkiJointSweepPreview admission=default)
        {IsValid=true;Status=status;Seconds=seconds;ArrivingPairs=count;First=first;Second=second;Admission=admission;}
    }
    /// <summary>Find an actual common arrival for a complete proposed roster.
    /// Each pair's conservative bracket is refined independently. Every pair and
    /// explicit edge must then admit the same prefix. No world state is advanced.
    /// Existing contacts are explicit and retain their full continuous checks.</summary>
    public sealed class ManualSkiContactScheduler
    {
        private readonly ManualSkiContactSweep pairSweep;
        private readonly ManualSkiContactSystem geometry;
        private readonly ManualSkiJointSweep joint;
        private readonly SkiWorldMotionCandidate[] prefix;
        private readonly SkiReachedContactPair[] arrivals;
        private readonly ManualSkiContactBody[] endpointBodies;
        private readonly double tolerance;
        private int arrivalCount;
        public int EndpointPoseSamples {get;private set;}
        public string ContextJson {get;}
        public ManualSkiContactScheduler(OccupancyDefinition body,SkiContactDefinition contact,SkiContactSweepDefinition work,int maximumActors)
        {
            if(maximumActors<1)throw new ArgumentOutOfRangeException(nameof(maximumActors));
            pairSweep=new ManualSkiContactSweep(body,contact,work);geometry=new ManualSkiContactSystem(body,contact);
            joint=new ManualSkiJointSweep(body,contact,work,separatedResponseBand:true);tolerance=contact.ContactTolerance;
            prefix=new SkiWorldMotionCandidate[maximumActors];arrivals=new SkiReachedContactPair[checked(maximumActors*(maximumActors-1)/2)];
            endpointBodies=new ManualSkiContactBody[maximumActors];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_common_contact_schedule_v2",pairSweep.FirstContactContextJson,joint.ContextJson,
                "all_pairs_actual_endpoint_group;stable_pair_order;common_prefix_all_body_and_explicit_edges;existing_contact_proved_half_prefix_force_refresh;no_mutation_or_wait"});
        }
        public SkiReachedContactPair GetArrival(int index)
        {if(index<0||index>=arrivalCount)throw new ArgumentOutOfRangeException(nameof(index));return arrivals[index];}
        public SkiWorldMotionCandidate GetPrefix(int index)
        {if(index<0||index>=prefixCount)throw new ArgumentOutOfRangeException(nameof(index));return prefix[index];}
        private int prefixCount;
        public SkiContactSchedulePreview Preview(SkiWorldMotionCandidate[] actors,int count,SkiReachedContactPair[] continuing,int continuingCount,
            SkiActorCourseEdge[] edges,int edgeCount)
            =>Preview(actors,count,continuing,continuingCount,edges,edgeCount,null);
        internal SkiContactSchedulePreview Preview(SkiWorldMotionCandidate[] actors,int count,SkiReachedContactPair[] continuing,int continuingCount,
            SkiActorCourseEdge[] edges,int edgeCount,bool[] participants)
        {
            arrivalCount=prefixCount=EndpointPoseSamples=0;
            if(actors==null||count<1||count>actors.Length||count>prefix.Length)throw new ArgumentException("Complete bounded roster required.");
            // Existing joint validation covers identities, durations, duplicate or
            // foreign contacts and edges even when an earlier pair is unresolved.
            var whole=joint.Preview(actors,count,continuing,continuingCount,edges,edgeCount,Array.Empty<SkiReachedContactPair>(),0,participants);
            double duration=actors[0].Trajectory.DurationSeconds;
            if(whole.Status==SkiJointSweepStatus.Clear)
            {
                Array.Copy(actors,prefix,count);prefixCount=count;
                return new SkiContactSchedulePreview(SkiContactScheduleStatus.Clear,duration,admission:whole);
            }
            double earliest=duration;bool found=false;AthleteId initialFirst=default,initialSecond=default;
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
            {
                if(participants!=null&&(!participants[i]||!participants[j]))continue;
                var a=actors[i];var b=actors[j];if(IsContinuing(a.Athlete,b.Athlete))continue;
                var hit=pairSweep.PreviewFirstContact(a.Athlete,a.Trajectory,b.Athlete,b.Trajectory);
                if(hit.Status==SkiFirstContactStatus.InitialProximity)
                {
                    var first=CanonicalFirst(a.Athlete,b.Athlete);var second=CanonicalSecond(a.Athlete,b.Athlete);
                    if(!initialFirst.IsValid||Compare(new SkiReachedContactPair(first,second),new SkiReachedContactPair(initialFirst,initialSecond))<0)
                    {initialFirst=first;initialSecond=second;}
                }
                if(hit.Status==SkiFirstContactStatus.Reached&&(!found||hit.ContactSeconds<earliest))
                {earliest=hit.ContactSeconds;found=true;}
            }
            if(initialFirst.IsValid)return new SkiContactSchedulePreview(SkiContactScheduleStatus.InitialProximity,0,first:initialFirst,second:initialSecond);
            // Frozen support reactions cannot remain accurate indefinitely while
            // two touching boxes rotate. Re-evaluate forces at a continuously
            // proved interior prefix, before numerical contact depth is spent.
            // This grants neither an arrival impulse nor any unpaid clock. An
            // The re-proof includes every pair/edge; zero or unproved prefixes fail.
            bool coupledRoster=true;
            for(int i=0;i<count;i++)coupledRoster&=actors[i].Trajectory.TryGetConstantAngularAcceleration(out _);
            if(coupledRoster&&whole.Status==SkiJointSweepStatus.PairInterval&&
                whole.ClearUntilSeconds>0&&(!found||whole.ClearUntilSeconds<earliest))
            {
                double refresh=whole.ClearUntilSeconds*.5;
                for(int i=0;i<count;i++)prefix[i]=new SkiWorldMotionCandidate(actors[i].Athlete,actors[i].Trajectory.Slice(0,refresh));
                var admitted=joint.Preview(prefix,count,continuing,continuingCount,edges,edgeCount,Array.Empty<SkiReachedContactPair>(),0,participants);
                if(admitted.Status==SkiJointSweepStatus.Clear)
                {
                    prefixCount=count;
                    return new SkiContactSchedulePreview(SkiContactScheduleStatus.ForceRefresh,refresh,first:whole.First,second:whole.Second,admission:admitted);
                }
            }
            if(!found)return new SkiContactSchedulePreview(SkiContactScheduleStatus.Unresolved,whole.ClearUntilSeconds,first:whole.First,second:whole.Second,admission:whole);
            for(int i=0;i<count;i++)prefix[i]=new SkiWorldMotionCandidate(actors[i].Athlete,actors[i].Trajectory.Slice(0,earliest));
            // Every pair observes the same immutable prefix endpoint. Sampling
            // it once avoids O(n²) repeated force integration/pose encoding.
            // Rebuild for every query: a new earliest time changes the prefix.
            for(int i=0;i<count;i++)if(participants==null||participants[i])
            {endpointBodies[i]=new ManualSkiContactBody(prefix[i].Athlete,prefix[i].Trajectory.Sample(1),default,1);EndpointPoseSamples++;}
            // Collect actual simultaneous endpoint proximity, not guessed time ties.
            // This also includes a pair whose full root query exhausted its work
            // after this earlier time, provided the joint prefix now proves it.
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
            {
                if(participants!=null&&(!participants[i]||!participants[j]))continue;
                var a=prefix[i];var b=prefix[j];if(IsContinuing(a.Athlete,b.Athlete))continue;
                var gap=geometry.Preview(endpointBodies[i],endpointBodies[j]);
                if(gap.GapMeters<=tolerance&&gap.GapMeters> -tolerance)arrivals[arrivalCount++]=new SkiReachedContactPair(a.Athlete,b.Athlete);
            }
            // Stable insertion order avoids an allocating comparer or dependence
            // on caller enumeration. Array lengths were allocated at construction.
            for(int i=1;i<arrivalCount;i++)
            {
                var value=arrivals[i];int j=i-1;
                while(j>=0&&Compare(arrivals[j],value)>0){arrivals[j+1]=arrivals[j];j--;}
                arrivals[j+1]=value;
            }
            var proof=joint.Preview(prefix,count,continuing,continuingCount,edges,edgeCount,arrivals,arrivalCount,participants);
            if(proof.Status!=SkiJointSweepStatus.Clear||arrivalCount==0)
            {
                arrivalCount=0;
                return new SkiContactSchedulePreview(SkiContactScheduleStatus.Unresolved,proof.ClearUntilSeconds,first:proof.First,second:proof.Second,admission:proof);
            }
            prefixCount=count;
            return new SkiContactSchedulePreview(SkiContactScheduleStatus.Arrival,earliest,arrivalCount,admission:proof);

            bool IsContinuing(AthleteId a,AthleteId b)
            {
                var first=CanonicalFirst(a,b);var second=CanonicalSecond(a,b);
                for(int i=0;i<continuingCount;i++)if(continuing[i].First.Equals(first)&&continuing[i].Second.Equals(second))return true;
                return false;
            }
        }
        private static AthleteId CanonicalFirst(AthleteId a,AthleteId b)=>string.CompareOrdinal(a.Value,b.Value)<0?a:b;
        private static AthleteId CanonicalSecond(AthleteId a,AthleteId b)=>string.CompareOrdinal(a.Value,b.Value)<0?b:a;
        private static int Compare(SkiReachedContactPair a,SkiReachedContactPair b)
        {int result=string.CompareOrdinal(a.First.Value,b.First.Value);return result!=0?result:string.CompareOrdinal(a.Second.Value,b.Second.Value);}
    }
}
