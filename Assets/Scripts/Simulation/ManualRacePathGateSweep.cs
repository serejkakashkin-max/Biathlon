using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualRacePathGateStatus { Clear, OutsideStart, PotentialCrossing, WorkLimit }
    public readonly struct ManualRacePathGatePreview
    {
        public bool IsValid {get;}
        public ManualRacePathGateStatus Status {get;}
        public long BoundaryArcNanometers {get;}
        public double ClearUntilSeconds {get;}
        public double IntervalEndSeconds {get;}
        public int IntervalChecks {get;}
        internal ManualRacePathGatePreview(ManualRacePathGateStatus status,long arc,double from,double to,int checks)
        {IsValid=true;Status=status;BoundaryArcNanometers=arc;ClearUntilSeconds=from;IntervalEndSeconds=to;IntervalChecks=checks;}
    }

    /// <summary>Local centre portals, separate from the body course tube. An
    /// endpoint cap in that tube cannot authorize crossing a phase boundary.
    /// Conservative terminal windows may require the caller to narrow/split its
    /// interval; this primitive never snaps a body or commits a waiting tick.</summary>
    public sealed class ManualRacePathGateSweep
    {
        private readonly ManualRacePathCatalog catalog;
        private readonly SkiContactSweepDefinition work;
        public ManualRacePathGateSweep(ManualRacePathCatalog catalog,SkiContactSweepDefinition work)
        {this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));this.work=work??throw new ArgumentNullException(nameof(work));}

        public ManualRacePathGatePreview Preview(ManualSkiWorldTrajectory trajectory,int occurrence,
            long previousArcNanometers,double maximumArcDisplacementMeters,long maximumAdmittedArcNanometers,bool requireStrictEndClearance=false)
        {
            if(!trajectory.IsValid)throw new ArgumentException("Initialized world trajectory required.",nameof(trajectory));
            var row=catalog.Get(occurrence);
            if(previousArcNanometers<0||previousArcNanometers>row.LengthNanometers||
                maximumAdmittedArcNanometers<previousArcNanometers||maximumAdmittedArcNanometers>row.LengthNanometers)
                throw new ArgumentOutOfRangeException(nameof(maximumAdmittedArcNanometers));
            PhysiologyNumbers.RequireRange(maximumArcDisplacementMeters,0,PhysiologyNumbers.MaximumMagnitude,nameof(maximumArcDisplacementMeters));
            double portalSpan=catalog.PortalSearchSpan(trajectory,maximumArcDisplacementMeters);
            int checks=0;
            // Only the terminal part of the selected station window uses this
            // portal. A closed penalty circuit's distant start/end half-planes
            // must not constrain the middle of that same circuit.
            // The practical snow corridor admits physical retreat within the
            // already owned occurrence. Its start plane is a station chart,
            // not a wall. Retreat cannot undo a published race phase or award
            // another lap; the forward endpoint and lifecycle owner remain.
            if(!catalog.ProjectionGeometry.UseRoundSnowCorridor&&catalog.WindowTouchesPortalEdge(occurrence,previousArcNanometers,portalSpan,0))
            {
                var start=AtBoundary(trajectory,occurrence,0,-1,ref checks);
                if(start.Status!=ManualRacePathGateStatus.Clear)return start;
            }
            if(catalog.WindowTouchesPortalEdge(occurrence,previousArcNanometers,portalSpan,maximumAdmittedArcNanometers))
                return AtBoundary(trajectory,occurrence,maximumAdmittedArcNanometers,1,ref checks,requireStrictEndClearance);
            return new ManualRacePathGatePreview(ManualRacePathGateStatus.Clear,-1,trajectory.DurationSeconds,trajectory.DurationSeconds,checks);
        }
        private ManualRacePathGatePreview AtBoundary(ManualSkiWorldTrajectory trajectory,int occurrence,long arc,int sign,ref int checks,bool strict=false)
        {
            if(!catalog.TrySamplePortal(occurrence,arc,out var boundary))throw new InvalidOperationException("An exact scoped portal is required.");
            var pose=trajectory.Sample(0);
            double reserve=2*trajectory.PositionErrorBoundMeters+64*2.2204460492503131e-16*
                (Math.Abs(pose.X)+Math.Abs(pose.Z)+Math.Abs(boundary.X)+Math.Abs(boundary.Z)+1)+catalog.ProjectionGeometry.PortalPositionTolerance;
            if(sign<0)reserve+=catalog.ProjectionGeometry.OrdinaryPortalRetreat;
            if(Extension(pose,boundary,sign)>reserve)
                return new ManualRacePathGatePreview(ManualRacePathGateStatus.OutsideStart,arc,0,0,checks);
            // Existing admission allows an encoded endpoint. An event-free
            // interval needs the stronger proof that the centre never reaches
            // that endpoint, including a tangency followed by retreat.
            return Search(trajectory,boundary,sign,arc,0,trajectory.DurationSeconds,0,strict&&catalog.ProjectionGeometry.PortalPositionTolerance==0?-reserve:reserve,ref checks);
        }
        private static double Extension(ManualSkiHeadingState pose,ManualRacePathTarget boundary,int sign)
            =>sign*((pose.X-boundary.X)*boundary.ForwardX+(pose.Z-boundary.Z)*boundary.ForwardZ);
        private ManualRacePathGatePreview Search(ManualSkiWorldTrajectory trajectory,ManualRacePathTarget boundary,
            int sign,long arc,double from,double to,int depth,double reserve,ref int checks)
        {
            if(checks>=work.MaximumIntervalChecks)
                return new ManualRacePathGatePreview(ManualRacePathGateStatus.WorkLimit,arc,from,to,checks);
            checks++;
            double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/trajectory.DurationSeconds;
            var pose=trajectory.Sample(fraction);var velocity=trajectory.VelocityAt(fraction);
            double normalSpeed=velocity.X*boundary.ForwardX+velocity.Z*boundary.ForwardZ;
            double upper=Extension(pose,boundary,sign)+Math.Abs(normalSpeed)*h+.5*trajectory.PlanarAccelerationBound*h*h;
            // The acceleration norm bounds the remainder of the signed plane
            // distance. This proves the entire interval, including excursions
            // whose two endpoints both project to the admitted scope.
            if(upper<=reserve)
                return new ManualRacePathGatePreview(ManualRacePathGateStatus.Clear,arc,to,to,checks);
            if(to-from<=work.TimeToleranceSeconds)
                return new ManualRacePathGatePreview(ManualRacePathGateStatus.PotentialCrossing,arc,from,to,checks);
            if(depth>=work.MaximumDepth||mid<=from||mid>=to)
                return new ManualRacePathGatePreview(ManualRacePathGateStatus.WorkLimit,arc,from,to,checks);
            var left=Search(trajectory,boundary,sign,arc,from,mid,depth+1,reserve,ref checks);
            if(left.Status!=ManualRacePathGateStatus.Clear)return left;
            return Search(trajectory,boundary,sign,arc,mid,to,depth+1,reserve,ref checks);
        }
    }
}
