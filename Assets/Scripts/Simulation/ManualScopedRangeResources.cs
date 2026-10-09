using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed partial class PackSprintFieldSession
    {
        internal PhysicalRangeReservationPreview PrepareManualScopedReservations(ReadOnlySpan<SprintStepPreview> prepared,ManualScopedWorldPreview proof)
        {
            if(reservations==null)return default;
            var world=proof.Owner;world.ValidatePrepared(proof);
            if(world.PathChoiceCount!=reservations.LaneCount||prepared.Length!=Count)
                throw new NotSupportedException("An attested scoped reusable path family is required.");
            int arrivalsCount=0,releasesCount=0;
            for(int i=0;i<Count;i++)
            {
                var actor=GetAthlete(i);var preview=prepared[i];int lane=reservations.GetOwnedLane(i),bout=reservations.GetOwnedBout(i);
                if(lane>0&&IsScopedPrivateBodyClear(world,proof,i,lane,bout))
                    reservationReleases[releasesCount++]=PhysicalRangeRelease.FromScopedClearEndpoint(actor.AthleteId,bout,lane,proof.Tick,frequency,reservations.ClearanceRouteNanometers(lane,bout));
                bout=actor.ActiveBout;
                if(bout<0||actor.HasFinished||!preview.MovementActive||bout!=reservations.GetLastAssignedBout(i)+1)continue;
                if(actor.RouteNanometers>reservations.AdmissionRouteNanometers(bout))
                    throw new InvalidOperationException("An unowned scoped range admission was already crossed.");
                if(PhysicalRangeArrival.TryFromScopedMotion(proof,i,bout,reservations.AdmissionRouteNanometers(bout),out var arrival))
                    pathArrivals[arrivalsCount++]=arrival;
            }
            var reservation=reservations.Prepare(proof.Tick,reservationReleases.AsSpan(0,releasesCount),pathArrivals.AsSpan(0,arrivalsCount));
            for(int i=0;i<Count;i++)if(reservations.WasPreparedDenied(reservation,i))
                throw new InvalidOperationException("Scoped range capacity is exhausted; no paid queue fallback is authorized.");
            for(int r=0;r<arrivalsCount;r++)
            {
                var arrival=pathArrivals[r];int i=0;while(!GetAthlete(i).AthleteId.Equals(arrival.AthleteId))i++;
                int selected=reservations.GetPreparedRecord(reservation,i,arrival.BoutIndex).LaneOrdinal;
                world.StagePaidPathChoice(proof,i,selected,arrival.BoutIndex);
                GetAthlete(i).StageManualScopedRangeChoice(prepared[i],selected);
            }
            return reservation;
        }

        private bool IsScopedPrivateBodyClear(ManualScopedWorldRoster world,ManualScopedWorldPreview proof,int index,int lane,int bout)
        {
            var cursor=world.GetProposedCursor(proof,index);var actor=world.GetProposedActor(proof,index);var catalog=cursor.Owner;
            if(cursor.CreditedRouteNanometers<reservations.ClearanceRouteNanometers(lane,bout)||
                actor.Observation.LoopId==reservations.PrivateLoopId(lane))return false;
            int exit=catalog.GetBaseFlowSuccessor(catalog.GetFiringOccurrence(bout));var row=catalog.Get(exit);
            if(row.LoopId!=reservations.PrivateLoopId(lane)||!catalog.TrySamplePortal(exit,row.LengthNanometers,out var portal))
                throw new InvalidOperationException("The owned private corridor requires its exact departure portal.");
            double fx=Math.Sin(actor.Pose.HeadingRadians),fz=Math.Cos(actor.Pose.HeadingRadians);
            double forward=fx*portal.ForwardX+fz*portal.ForwardZ;
            double right=fz*portal.ForwardX-fx*portal.ForwardZ;
            double extent=widths.Definition.LongitudinalSeparation*.5*Math.Abs(forward)+widths.Definition.LateralSeparation*.5*Math.Abs(right);
            double signed=(actor.Pose.X-portal.X)*portal.ForwardX+(actor.Pose.Z-portal.Z)*portal.ForwardZ;
            return signed>extent+PhysiologyNumbers.MinimumPositive;
        }
        private void CommitManualScopedReservations(PhysicalRangeReservationPreview preview)
        {
            if(reservations==null)return;
            reservations.Commit(preview);
            for(int i=0;i<reservations.LastChangeCount;i++)
            {
                var change=reservations.GetLastChange(i);
                events.Record(new RaceEvent(preview.Tick,frequency,change.AthleteId,change.IsRelease?RaceEventType.PhysicalRangeReservationReleased:RaceEventType.PhysicalRangeReservationAcquired,change.LaneOrdinal));
            }
        }
    }
}
