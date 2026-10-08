using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualScopedWorldEventStatus { Clear, Contact, Boundary, ContactAndBoundary }
    public readonly struct ManualScopedWorldEventPreview
    {
        public bool IsValid {get;}
        public ManualScopedWorldEventStatus Status {get;}
        public double PaidSeconds {get;}
        public int BoundaryCount {get;}
        public SkiContactSchedulePreview ContactSchedule {get;}
        internal ManualScopedWorldEventPreview(ManualScopedWorldEventStatus status,double seconds,int count,SkiContactSchedulePreview contact=default)
        {IsValid=true;Status=status;PaidSeconds=seconds;BoundaryCount=count;ContactSchedule=contact;}
    }
    /// <summary>An actual owned centre at a local cap. End identifies the
    /// lifecycle gate only when IsOccurrenceEnd is true. This notification
    /// itself does not authorize a handoff, shooting or finish publication.</summary>
    public readonly struct ManualScopedWorldBoundary
    {
        public int ActorIndex {get;}
        public AthleteId Athlete {get;}
        public int OccurrenceIndex {get;}
        public long ArcNanometers {get;}
        public bool IsOccurrenceEnd {get;}
        public ManualRacePathEnd End {get;}
        internal ManualScopedWorldBoundary(int actor,ManualRacePathCursor cursor,ManualRacePathOccurrence row,long arc)
        {ActorIndex=actor;Athlete=cursor.Athlete;OccurrenceIndex=row.Index;ArcNanometers=arc;IsOccurrenceEnd=arc==row.LengthNanometers;End=row.End;}
    }
    public sealed partial class ManualScopedWorldRoster
    {
        private double eventElapsedSeconds;
        private int eventPieceCount;
        public ManualScopedWorldEventPreview LastWorldEvent {get;private set;}
        public ManualScopedWorldBoundary GetLastBoundary(ManualScopedWorldTick tick,int index)
        {
            ValidateTick(tick);
            if(!LastWorldEvent.IsValid||elapsedSeconds!=eventElapsedSeconds||pieceCount!=eventPieceCount||
                index<0||index>=LastWorldEvent.BoundaryCount)throw new ArgumentOutOfRangeException(nameof(index));
            return arrivedBoundaries[index];
        }
        /// <summary>Pay only the next common body/contact or physical scope
        /// interval. The caller resolves any response, authorizes the reported
        /// lifecycle/ordinary handoffs and continues the exact remaining clock.
        /// A zero-time owned boundary reports an event without inventing a wait.
        /// Frozen polynomial pieces are supported; other unresolved centre laws
        /// fail explicitly rather than being approximated by an endpoint chord.</summary>
        public bool TryAppendToOwnedFirstEvent(ManualScopedWorldTick tick,SkiWorldMotionCandidate[] actors,
            ManualScopedWorldWindow[] windows,SkiActorCourseEdge[] edges,int edgeCount,double[] directionalGrades,
            out ManualScopedWorldEventPreview result)
        {
            ValidateTick(tick);ready=false;LastWorldEvent=default;result=default;
            if(pendingContact)return Reject(ManualScopedWorldStatus.PendingContact);
            if(actors==null||windows==null||directionalGrades==null||actors.Length!=Count||windows.Length!=Count||directionalGrades.Length!=Count)
                throw new ArgumentException("Exact complete event roster, scoped windows and grades required.");
            double seconds=actors[0].Trajectory.DurationSeconds,remaining=GetRemainingSeconds(tick);
            if(!(seconds>0)||seconds>remaining)return Reject(ManualScopedWorldStatus.SourceMismatch);
            // Validate even the last row before publishing any provisional
            // common interval or a zero-time endpoint notification.
            for(int i=0;i<Count;i++)
            {
                var actor=actors[i];var cursor=workingCursors[i];var window=windows[i];
                if(!actor.Trajectory.IsValid||!actor.Athlete.Equals(cursor.Athlete)||!window.Athlete.Equals(cursor.Athlete)||
                    window.OccurrenceIndex!=cursor.OccurrenceIndex||window.PreviousArcNanometers!=cursor.ObservedArcNanometers||
                    !SamePose(actor.Trajectory.Sample(0),workingActors[i].Pose)||!SameVelocity(actor.Trajectory.VelocityAt(0),workingActors[i].Velocity)||
                    actor.Trajectory.DurationSeconds!=seconds)
                    return Reject(ManualScopedWorldStatus.SourceMismatch,cursor.Athlete);
                var row=catalogs[i].Get(cursor.OccurrenceIndex);
                if(window.MaximumAdmittedArcNanometers<cursor.CreditedArcNanometers||window.MaximumAdmittedArcNanometers>row.LengthNanometers)
                    return Reject(ManualScopedWorldStatus.PhaseLimit,cursor.Athlete);
                PhysiologyNumbers.RequireRange(directionalGrades[i],-1,1,nameof(directionalGrades));
            }
            if(!CollectOwnedContacts())return false;
            double earliest=seconds;
            for(int i=0;i<Count;i++)
            {
                var preview=boundaries[i].Preview(actors[i].Trajectory,windows[i]);boundaryPreviews[i]=preview;
                if(preview.Status==ManualRacePathBoundaryStatus.Boundary&&preview.Seconds<earliest)earliest=preview.Seconds;
            }
            if(earliest==0)
            {
                int count=CollectArrivedBoundaries(actors,windows);
                if(count==0)return Reject(ManualScopedWorldStatus.BoundarySchedule);
                result=PublishWorldEvent(ManualScopedWorldEventStatus.Boundary,0,count);return true;
            }
            for(int i=0;i<Count;i++)boundaryPrefix[i]=earliest==seconds?actors[i]:
                new SkiWorldMotionCandidate(actors[i].Athlete,actors[i].Trajectory.Slice(0,earliest));
            // First select the actual contact prefix. Portal uncertainty after
            // that contact must not discard its earlier safe interval.
            var contact=scheduler.Preview(boundaryPrefix,Count,ownedContacts,LastOwnedContacts.Count,edges,edgeCount);
            bool collision=contact.Status==SkiContactScheduleStatus.Arrival;
            if(contact.Status!=SkiContactScheduleStatus.Clear&&!collision)
                return Reject(ManualScopedWorldStatus.ContactSchedule,contact.First,contact.Admission);
            if(collision)
            {
                for(int i=0;i<Count;i++)boundaryPrefix[i]=scheduler.GetPrefix(i);
                for(int i=0;i<contact.ArrivingPairs;i++)scheduledArrivals[i]=scheduler.GetArrival(i);
            }
            for(int i=0;i<Count;i++)
            {
                var preview=boundaries[i].Preview(boundaryPrefix[i].Trajectory,windows[i]);
                if(preview.Status==ManualRacePathBoundaryStatus.Blocked||preview.Status==ManualRacePathBoundaryStatus.UnsupportedTrajectory)
                    return Reject(ManualScopedWorldStatus.BoundarySchedule,actors[i].Athlete,gate:preview.Admission);
            }
            double before=GetElapsedSeconds(tick);
            bool appended=collision?TryAppendArrivalInterval(tick,boundaryPrefix,windows,ownedContacts,LastOwnedContacts.Count,edges,edgeCount,
                directionalGrades,scheduledArrivals,contact.ArrivingPairs):
                TryAppendInterval(tick,boundaryPrefix,windows,ownedContacts,LastOwnedContacts.Count,edges,edgeCount,directionalGrades);
            if(!appended)return false;
            int reached=CollectArrivedBoundaries(actors,windows);
            var status=collision?(reached>0?ManualScopedWorldEventStatus.ContactAndBoundary:ManualScopedWorldEventStatus.Contact):
                reached>0?ManualScopedWorldEventStatus.Boundary:ManualScopedWorldEventStatus.Clear;
            result=PublishWorldEvent(status,GetElapsedSeconds(tick)-before,reached,contact);return true;
        }
        private ManualScopedWorldEventPreview PublishWorldEvent(ManualScopedWorldEventStatus status,double seconds,int count,SkiContactSchedulePreview contact=default)
        {eventElapsedSeconds=elapsedSeconds;eventPieceCount=pieceCount;return LastWorldEvent=new ManualScopedWorldEventPreview(status,seconds,count,contact);}
        private int CollectArrivedBoundaries(SkiWorldMotionCandidate[] sources,ManualScopedWorldWindow[] windows)
        {
            int count=0;
            for(int k=0;k<Count;k++)
            {
                int i=ownedContactOrder[k];var cursor=workingCursors[i];long cap=windows[i].MaximumAdmittedArcNanometers;
                if(boundaryPreviews[i].Status==ManualRacePathBoundaryStatus.Boundary&&cursor.ObservedArcNanometers==cap&&
                    cursor.CreditedArcNanometers==cap&&boundaries[i].IsAtBoundary(sources[i].Trajectory,workingActors[i].Pose,windows[i]))
                    arrivedBoundaries[count++]=new ManualScopedWorldBoundary(i,cursor,catalogs[i].Get(cursor.OccurrenceIndex),cap);
            }
            return count;
        }
    }
}
