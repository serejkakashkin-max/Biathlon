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
        public SkiContactForcePreview LastContactForces {get;private set;}
        public SkiContactForceReaction GetLastContactForceReaction(int index)
        {
            if(contactForces==null||!LastContactForces.IsValid)throw new InvalidOperationException("No current contact force preview.");
            return contactForces.GetReaction(index);
        }
        /// <summary>Couple current raw drive forces through actual owned bodies.
        /// Reached contacts cover the next common constant-force piece. A field
        /// with no reached pairs keeps each full immutable free drive, including
        /// its physical stop/cap, until a real contact or route event. This preview
        /// pays no time; the normal scheduler, course and phase proofs still
        /// must admit it. Failure never writes a partial caller candidate array.</summary>
        public bool TryPreviewContactDrivenMotion(ManualScopedWorldTick tick,SkiDrivenContactTrajectory[] drives,SkiWorldMotionCandidate[] output)
        {
            ValidateTick(tick);ready=false;LastContactForces=default;
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(contactForces==null)throw new InvalidOperationException("Contact force policy and exact masses must be bound.");
            if(drives==null||output==null||drives.Length!=Count||output.Length!=Count)
                throw new ArgumentException("Complete current world drive and output roster required.");
            double remaining=GetRemainingSeconds(tick);bool hasBreakpoint=false;
            for(int i=0;i<Count;i++)
            {
                var actor=workingActors[i];var frame=drives[i];
                if(!frame.IsValid||frame.DurationSeconds>remaining||frame.DurationSeconds!=drives[0].DurationSeconds||
                    !SamePose(frame.InitialPose,actor.Pose)||!SameVelocity(frame.InitialMotion.Velocity,actor.Velocity)||
                    frame.InitialMotion.YawRate!=actor.Pose.YawRateRadiansPerSecond||frame.MassKilograms!=masses[i])
                    throw new ArgumentException("Exact current physical source, mass and equal-duration remaining drive required.");
                responseBodies[i]=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,masses[i]);
                hasBreakpoint|=frame.ConstantAccelerationSeconds<frame.DurationSeconds;
                hasBreakpoint|=frame.Snow.HasExactNeutralConstraint&&frame.FirstDrive.InitialState.SpeedMetersPerSecond>0&&frame.FirstDrive.Acceleration<0&&
                    frame.MotionAt(1).Velocity.X==0&&frame.MotionAt(1).Velocity.Z==0;
            }
            if(!CollectOwnedContacts())return false;
            if(LastOwnedContacts.Count==0&&hasBreakpoint)
            {
                // Recomputing every separated skier at another skier's tiny
                // stop breakpoint repeatedly restarts a partly paid snow ramp.
                // Keep the exact free law. Future arrivals still go through the
                // continuous scheduler and all body/course/phase admission.
                for(int i=0;i<Count;i++)coupledCandidates[i]=new SkiWorldMotionCandidate(workingActors[i].Athlete,new ManualSkiWorldTrajectory(drives[i]));
                for(int i=0;i<Count;i++)output[i]=coupledCandidates[i];
                LastContactForces=new SkiContactForcePreview(SkiContactForceStatus.Resolved,drives[0].DurationSeconds,0);
                LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
            }
            LastContactForces=contactForces.Preview(responseBodies,drives,Count,coupledTrajectories);
            if(LastContactForces.Status!=SkiContactForceStatus.Resolved)return Reject(ManualScopedWorldStatus.ContactForces);
            for(int i=0;i<Count;i++)coupledCandidates[i]=new SkiWorldMotionCandidate(workingActors[i].Athlete,new ManualSkiWorldTrajectory(coupledTrajectories[i]));
            for(int i=0;i<Count;i++)output[i]=coupledCandidates[i];
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }
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
