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
        public int LastOwnedContactNarrowphasePairs {get;private set;}
        public int LastOwnedContactExcludedPairs {get;private set;}
        public int OwnedContactGeometryBuilds {get;private set;}
        private readonly ManualSkiHeadingState[] ownedContactCachePoses;
        private readonly bool[] ownedContactCacheParticipants;
        private bool ownedContactCacheValid;
        public SkiContactForcePreview LastContactForces {get;private set;}
        public SkiContactForceReaction GetLastContactForceReaction(int index)
        {
            if(contactForces==null||!LastContactForces.IsValid)throw new InvalidOperationException("No current contact force preview.");
            if(index<0||index>=LastContactForces.SupportPoints)throw new ArgumentOutOfRangeException(nameof(index));
            return contactForces.GetReaction(index);
        }
        /// <summary>Couple current raw drive forces through actual owned bodies.
        /// Reached contacts cover the next common constant-force piece. A field
        /// with no reached pairs keeps each full immutable free drive, including
        /// its physical stop/cap, until a real contact or route event. This preview
        /// pays no time; the normal scheduler, course and phase proofs still
        /// must admit it. Failure never writes a partial caller candidate array.</summary>
        public bool TryPreviewContactDrivenMotion(ManualScopedWorldTick tick,SkiDrivenContactTrajectory[] drives,SkiWorldMotionCandidate[] output)
            =>TryPreviewContactDrivenMotion(tick,drives,output,allowIndependentCourse:true);

        private bool TryPreviewContactDrivenMotion(ManualScopedWorldTick tick,SkiDrivenContactTrajectory[] drives,
            SkiWorldMotionCandidate[] output,bool allowIndependentCourse)
        {
            ValidateTick(tick);ready=false;LastContactForces=default;independentCoursePreview=false;
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
                responseBodies[i]=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,masses[i],IsGroundedMat(i));
                hasBreakpoint|=frame.ConstantAccelerationSeconds<frame.DurationSeconds;
                hasBreakpoint|=frame.Snow.HasExactNeutralConstraint&&frame.FirstDrive.InitialState.SpeedMetersPerSecond>0&&frame.FirstDrive.Acceleration<0&&
                    frame.MotionAt(1).Velocity.X==0&&frame.MotionAt(1).Velocity.Z==0;
            }
            if(!CollectOwnedContacts())return false;
            if(allowIndependentCourse&&LastOwnedContacts.Count==0&&CourseEdgeResponses>0&&
                CourseReactionPolicy==ManualCourseReactionPolicy.IndependentCourseV27&&TryPreviewIndependentCourseMotion(drives,output))
                return true;
            if(LastOwnedContacts.Count==0&&CourseEdgeResponses==0)Array.Clear(ownedFreeValid,0,Count);
            if(LastOwnedContacts.Count==0&&hasBreakpoint&&CourseEdgeResponses==0)
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
            if(LastOwnedContacts.Count==0)LastContactForces=contactForces.Preview(responseBodies,drives,Count,coupledTrajectories,contactParticipants);
            else
            {
                // Contact reactions may refresh repeatedly inside one fixed
                // field tick. They must not restart a snow ramp or a free stop
                // from an externally pushed, almost sideways source each time.
                // Preserve the original external force calendar; reactions still
                // start from actual current raw velocity/spin and never snap it.
                double seconds=drives[0].DurationSeconds;
                for(int i=0;i<Count;i++)
                {
                    var cursor=workingCursors[i];var row=catalogs[i].Get(cursor.OccurrenceIndex);
                    if(!ownedContactParticipants[i]&&!courseSupports[i]&&NeutralStop(drives[i]))
                    {
                        // This disconnected stop keeps its complete free law,
                        // including the snow ramp after its forward-speed stop.
                        // Its internal breakpoint cannot clip the other pair's
                        // force clock or replace its own exact endpoint.
                        ownedFreeDrives[i]=drives[i];ownedFreeStarts[i]=elapsedSeconds;
                        ownedFreeOccurrences[i]=cursor.OccurrenceIndex;ownedFreePhases[i]=0;ownedFreeValid[i]=false;
                        continue;
                    }
                    bool hold=row.End==ManualRacePathEnd.FiringLine&&cursor.ObservedArcNanometers==row.LengthNanometers;
                    if(!ownedFreeValid[i]||ownedFreeOccurrences[i]!=cursor.OccurrenceIndex||
                        elapsedSeconds>=ownedFreeStarts[i]+ownedFreeDrives[i].DurationSeconds||hold)
                    {
                        ownedFreeDrives[i]=drives[i];ownedFreeStarts[i]=elapsedSeconds;
                        ownedFreeOccurrences[i]=cursor.OccurrenceIndex;ownedFreePhases[i]=0;ownedFreeValid[i]=true;
                    }
                    var original=ownedFreeDrives[i];int phase=ownedFreePhases[i];
                    while(original.TryGetAccelerationPiece(phase,out _,out double end,out _)&&
                        ownedFreeStarts[i]+end<=elapsedSeconds)phase++;
                    ownedFreePhases[i]=phase;
                    if(!original.TryGetAccelerationPiece(phase,out _,out double to,out _))
                        return Reject(ManualScopedWorldStatus.ContactForces);
                    seconds=Math.Min(seconds,ownedFreeStarts[i]+to-elapsedSeconds);
                }
                for(int i=0;i<Count;i++)
                {
                    ownedFreeDrives[i].TryGetAccelerationPiece(ownedFreePhases[i],out _,out _,out var acceleration);
                    var body=responseBodies[i];
                    ownedFreePieces[i]=new SkiForceCoupledTrajectory(body.Pose,new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond),acceleration,seconds);
                }
                LastContactForces=seconds<contactForces.MinimumPieceSeconds
                    ?contactForces.Preview(responseBodies,drives,Count,coupledTrajectories,contactParticipants)
                    :contactForces.PreviewRefined(responseBodies,ownedFreePieces,Count,coupledTrajectories,maximumForceRefinements,contactParticipants);
            }
            if(LastContactForces.Status!=SkiContactForceStatus.Resolved)return Reject(ManualScopedWorldStatus.ContactForces);
            for(int i=0;i<Count;i++)
            {
                // An unrelated pair's reaction calendar must not replace a
                // separated skier's own exact free stop/snow law. Future contact
                // is still admitted by the common continuous scheduler.
                coupledTrajectories[i]=ApplyCourseReaction(i,coupledTrajectories[i]);
                var trajectory=LastOwnedContacts.Count>0&&!ownedContactParticipants[i]&&!courseSupports[i]&&NeutralStop(drives[i])
                    ?new ManualSkiWorldTrajectory(drives[i]).Slice(0,LastContactForces.DurationSeconds)
                    :new ManualSkiWorldTrajectory(coupledTrajectories[i]);
                coupledCandidates[i]=new SkiWorldMotionCandidate(workingActors[i].Athlete,trajectory);
            }
            for(int i=0;i<Count;i++)output[i]=coupledCandidates[i];
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
            bool NeutralStop(SkiDrivenContactTrajectory free)
            {
                var end=free.MotionAt(1);
                return free.Snow.HasExactNeutralConstraint&&free.FirstDrive.InitialState.SpeedMetersPerSecond>0&&
                    free.FirstDrive.Acceleration<0&&end.Velocity.X==0&&end.Velocity.Z==0;
            }
        }
        private bool independentCoursePreview;

        private bool TryPreviewIndependentCourseMotion(SkiDrivenContactTrajectory[] drives,SkiWorldMotionCandidate[] output)
        {
            // A snow-edge reaction belongs to its supported skier. An unrelated
            // free stop must neither restart that reaction hundreds of times nor
            // replace the separated skier's exact nonlinear free law. All future
            // body arrivals, course edges and route events still use the common
            // continuous scheduler before any time can be paid.
            double duration=drives[0].DurationSeconds,seconds=duration,freeSeconds=duration;
            for(int i=0;i<Count;i++)
            {
                if(courseSupports[i])seconds=Math.Min(seconds,drives[i].ConstantAccelerationSeconds);
                else freeSeconds=Math.Min(freeSeconds,drives[i].ConstantAccelerationSeconds);
            }
            seconds=Math.Min(duration,Math.Max(seconds,contactForces.MinimumPieceSeconds));
            if(Math.Max(freeSeconds,contactForces.MinimumPieceSeconds)>=seconds)return false;
            for(int i=0;i<Count;i++)
            {
                ManualSkiWorldTrajectory trajectory;
                if(!courseSupports[i])trajectory=new ManualSkiWorldTrajectory(drives[i]).Slice(0,seconds);
                else
                {
                    var initial=drives[i].InitialMotion;var end=drives[i].MotionAt(seconds/duration);
                    var acceleration=new SkiContactAcceleration((end.Velocity.X-initial.Velocity.X)/seconds,
                        (end.Velocity.Z-initial.Velocity.Z)/seconds,(end.YawRate-initial.YawRate)/seconds);
                    var free=new SkiForceCoupledTrajectory(responseBodies[i].Pose,initial,acceleration,seconds);
                    trajectory=new ManualSkiWorldTrajectory(ApplyCourseReaction(i,free));
                }
                coupledCandidates[i]=new SkiWorldMotionCandidate(workingActors[i].Athlete,trajectory);
            }
            for(int i=0;i<Count;i++)output[i]=coupledCandidates[i];
            independentCoursePreview=true;
            LastContactForces=new SkiContactForcePreview(SkiContactForceStatus.Resolved,seconds,0);
            LastAttempt=new ManualScopedWorldAttempt(ManualScopedWorldStatus.Clear);return true;
        }

        internal bool TryAppendContactDrivenFirstEvent(ManualScopedWorldTick tick,SkiDrivenContactTrajectory[] drives,
            SkiWorldMotionCandidate[] candidates,ManualScopedWorldWindow[] windows,double[] grades,
            out ManualScopedWorldEventPreview result,ReadOnlySpan<int> held)
        {
            if(TryAppendToOwnedFirstEvent(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,grades,out result,held))
                return true;
            if(!independentCoursePreview||LastAttempt.Status!=ManualScopedWorldStatus.Course)return false;
            for(int i=0;i<Count;i++)
            {
                // A longer supported piece can fail the original course proof.
                // Retry from the same unpaid source on the historical cadence
                // before registering another impulse at this same position.
                if(!courseSupports[i]||!workingActors[i].Athlete.Equals(LastAttempt.Athlete))continue;
                return TryPreviewContactDrivenMotion(tick,drives,candidates,allowIndependentCourse:false)&&
                    TryAppendToOwnedFirstEvent(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,grades,out result,held);
            }
            return false;
        }
        public SkiReachedContactPair GetLastOwnedContactPair(int index)
        {
            if(!LastOwnedContacts.IsValid||index<0||index>=LastOwnedContacts.Count)throw new ArgumentOutOfRangeException(nameof(index));
            return ownedContacts[index];
        }
        private bool CollectOwnedContacts()
        {
            // This group depends on current body poses and physical presence,
            // never on velocity or the next proposed force law. Several callers
            // ask for it before paying any time. Retain all pair proofs while
            // reusing an exact unchanged group, including its stable pair order.
            if(ownedContactCacheValid)
            {
                bool same=true;
                for(int i=0;i<Count;i++)if(!SamePose(workingActors[i].Pose,ownedContactCachePoses[i])||contactParticipants[i]!=ownedContactCacheParticipants[i]){same=false;break;}
                if(same)return true;
            }
            ownedContactCacheValid=false;
            OwnedContactGeometryBuilds++;
            LastOwnedContacts=default;int count=0;Array.Clear(ownedContactParticipants,0,Count);
            LastOwnedContactNarrowphasePairs=LastOwnedContactExcludedPairs=0;
            // The accepted proximity band is measured on separating axes. Inflate
            // BOTH rectangle half axes by half the band before using a containing
            // circle, so diagonal corner proximity remains a narrow-phase query.
            double band=ownedContactTolerance+4*PhysiologyNumbers.MinimumPositive;
            double length=occupiedBody.LongitudinalSeparation*.5+band*.5;
            double width=occupiedBody.LateralSeparation*.5+band*.5;
            double reach=2*Math.Sqrt(length*length+width*width);
            for(int i=0;i<Count;i++)for(int j=i+1;j<Count;j++)
            {
                int ai=ownedContactOrder[i],bi=ownedContactOrder[j];var a=workingActors[ai];var b=workingActors[bi];
                if(!contactParticipants[ai]||!contactParticipants[bi])continue;
                double dx=a.Pose.X-b.Pose.X,dz=a.Pose.Z-b.Pose.Z;
                double reserve=128*2.2204460492503131e-16*(1+reach+Math.Abs(a.Pose.X)+Math.Abs(a.Pose.Z)+Math.Abs(b.Pose.X)+Math.Abs(b.Pose.Z));
                double bound=reach+reserve;
                if(dx*dx+dz*dz>bound*bound){LastOwnedContactExcludedPairs++;continue;}
                LastOwnedContactNarrowphasePairs++;
                var gap=ownedContactGeometry.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,default,1),
                    new ManualSkiContactBody(b.Athlete,b.Pose,default,1));
                if(gap.Status==SkiContactPreviewStatus.Penetrating||gap.GapMeters< -ownedContactTolerance)
                {
                    LastOwnedContacts=new ManualScopedContactSetPreview(false,0,new SkiReachedContactPair(a.Athlete,b.Athlete),gap);
                    return Reject(ManualScopedWorldStatus.ContactGeometry,a.Athlete);
                }
                if(gap.GapMeters<=ownedContactTolerance+4*PhysiologyNumbers.MinimumPositive)
                {ownedContacts[count++]=new SkiReachedContactPair(a.Athlete,b.Athlete);ownedContactParticipants[ai]=ownedContactParticipants[bi]=true;}
            }
            LastOwnedContacts=new ManualScopedContactSetPreview(true,count);
            for(int i=0;i<Count;i++){ownedContactCachePoses[i]=workingActors[i].Pose;ownedContactCacheParticipants[i]=contactParticipants[i];}
            ownedContactCacheValid=true;return true;
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
