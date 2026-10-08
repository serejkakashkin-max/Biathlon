using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Telemetry of current owned body proximity. This is not a future
    /// movement proof and cannot register a foreign or merely predicted pair.</summary>
    public readonly struct ManualScopedContactSetPreview
    {
        public bool IsValid {get;}
        public int Count {get;}
        public SkiReachedContactPair Limit {get;}
        public SkiContactPreview Geometry {get;}
        internal ManualScopedContactSetPreview(bool valid,int count,SkiReachedContactPair limit=default,SkiContactPreview geometry=default)
        {IsValid=valid;Count=count;Limit=limit;Geometry=geometry;}
    }
    public sealed partial class ManualScopedWorldRoster
    {
        public ManualScopedContactSetPreview LastOwnedContacts {get;private set;}
        public SkiReachedContactPair GetLastOwnedContactPair(int index)
        {
            if(!LastOwnedContacts.IsValid||index<0||index>=LastOwnedContacts.Count)throw new ArgumentOutOfRangeException(nameof(index));
            return ownedContacts[index];
        }
        private bool CollectOwnedContacts()
        {
            LastOwnedContacts=default;int count=0;
            for(int i=0;i<Count;i++)for(int j=i+1;j<Count;j++)
            {
                int ai=ownedContactOrder[i],bi=ownedContactOrder[j];var a=workingActors[ai];var b=workingActors[bi];
                var gap=ownedContactGeometry.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,default,1),
                    new ManualSkiContactBody(b.Athlete,b.Pose,default,1));
                if(gap.Status==SkiContactPreviewStatus.Penetrating||gap.GapMeters< -ownedContactTolerance)
                {
                    LastOwnedContacts=new ManualScopedContactSetPreview(false,0,new SkiReachedContactPair(a.Athlete,b.Athlete),gap);
                    return Reject(ManualScopedWorldStatus.ContactGeometry,a.Athlete);
                }
                if(gap.GapMeters<=ownedContactTolerance)ownedContacts[count++]=new SkiReachedContactPair(a.Athlete,b.Athlete);
            }
            LastOwnedContacts=new ManualScopedContactSetPreview(true,count);return true;
        }
        /// <summary>Use only current owned endpoint proximity for continuing
        /// contacts, then prove the full roster's next common arrival or clear
        /// interval. Separated pairs automatically return to ordinary scheduling.</summary>
        public bool TryAppendToOwnedFirstContact(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,
            out SkiContactSchedulePreview schedule)
        {
            ValidateTick(tick);ready=false;schedule=default;
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(!CollectOwnedContacts())return false;
            return TryAppendToFirstContact(tick,actors,windows,ownedContacts,LastOwnedContacts.Count,edges,edgeCount,directionalGrades,out schedule);
        }
        /// <summary>Respond at an actual current contact, including accepted
        /// initial proximity. No time, odometer, route credit or pose correction.
        /// All outputs remain provisional; unresolved response blocks continuation.</summary>
        public bool TryResolveOwnedCurrentContacts(ManualScopedWorldTick tick)
        {
            ValidateTick(tick);ready=false;
            if(!CollectOwnedContacts())return false;
            if(LastOwnedContacts.Count==0)
            {
                if(pendingContact)return Reject(ManualScopedWorldStatus.ContactGeometry);
                LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
            }
            if(response==null)throw new InvalidOperationException("Owned contact response requires bound group policy and exact actor masses.");
            pendingContact=true;return TryResolveArrivals(tick);
        }
    }
}
