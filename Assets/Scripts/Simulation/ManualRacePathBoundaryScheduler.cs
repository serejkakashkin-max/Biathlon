using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualRacePathBoundaryStatus { Clear, Boundary, Blocked, UnsupportedTrajectory }
    public readonly struct ManualRacePathBoundaryPreview
    {
        public ManualRacePathBoundaryStatus Status {get;}
        public double Seconds {get;}
        public long BoundaryArcNanometers {get;}
        public ManualRacePathGatePreview Admission {get;}
        internal ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus status,double seconds,long arc,ManualRacePathGatePreview admission=default)
        {Status=status;Seconds=seconds;BoundaryArcNanometers=arc;Admission=admission;}
    }
    /// <summary>Locate the first real centre-plane arrival in one admitted
    /// scope. Exact quadratic roots preserve the frozen force interval. A
    /// clamped route projection alone never authorizes an arrival or movement.</summary>
    public sealed class ManualRacePathBoundaryScheduler
    {
        private readonly ManualRacePathCatalog catalog;
        private readonly ManualRacePathGateSweep gates;
        public const string Policy="scoped_boundary_quadratic_root_v2_shared_portals;actual_centre_plane;shared_ordinary_bisector_chart;race_phase_gate_planes_unchanged;exact_frozen_prefix;no_endpoint_chord_fit;whole_prefix_portal_proof;strict_no_touch_clearance_for_event_free_intervals;actual_nanorounded_station;no_clock_roundup";
        public ManualRacePathBoundaryScheduler(ManualRacePathCatalog catalog,SkiContactSweepDefinition work)
        {this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));gates=new ManualRacePathGateSweep(catalog,work);}
        public ManualRacePathBoundaryPreview Preview(ManualSkiWorldTrajectory trajectory,ManualScopedWorldWindow window)
        {
            if(!trajectory.IsValid)throw new ArgumentException("Initialized world trajectory required.");
            var row=catalog.Get(window.OccurrenceIndex);long arc=window.MaximumAdmittedArcNanometers;
            if(window.PreviousArcNanometers>arc||arc>row.LengthNanometers)throw new ArgumentException("Admitted local station required.");
            double seconds=trajectory.DurationSeconds;
            if(!catalog.WindowTouchesPortalEdge(row.Index,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,arc))
                return Whole(trajectory,window);
            if(!catalog.TrySamplePortal(row.Index,arc,out var boundary))throw new InvalidOperationException("Exact scoped boundary required.");
            var pose=trajectory.Sample(0);double offset=Extension(pose,boundary);
            double reserve=Reserve(trajectory,pose,boundary);
            // This is an already owned endpoint event, not a fabricated zero
            // duration movement. The lifecycle owner must consume or block it.
            if(window.PreviousArcNanometers==arc&&Math.Abs(offset)<=reserve&&AtStation(pose,window,arc))
                return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Boundary,0,arc);
            if(offset>reserve)return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Blocked,0,arc);
            if(!trajectory.TryGetConstantPlanarAcceleration(out var acceleration))
            {
                var whole=Whole(trajectory,window,true);
                return whole.Status==ManualRacePathBoundaryStatus.Clear?whole:
                    new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.UnsupportedTrajectory,whole.Seconds,arc,whole.Admission);
            }
            var velocity=trajectory.VelocityAt(0);
            double b=velocity.X*boundary.ForwardX+velocity.Z*boundary.ForwardZ;
            double a=.5*(acceleration.X*boundary.ForwardX+acceleration.Z*boundary.ForwardZ);
            if(!TryRoot(a,b,offset,seconds,out double root))
            {
                var whole=Whole(trajectory,window);
                var end=trajectory.Sample(1);var endVelocity=trajectory.VelocityAt(1);
                // The encoded actual endpoint can coincide with the interval's
                // end although its mathematical root rounds just outside it.
                // Recognize only that already paid endpoint, never increase time.
                if(whole.Status==ManualRacePathBoundaryStatus.Clear&&IsAtBoundary(trajectory,end,window)&&
                    endVelocity.X*boundary.ForwardX+endVelocity.Z*boundary.ForwardZ>=0)
                    return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Boundary,seconds,arc,whole.Admission);
                return Whole(trajectory,window,true);
            }
            if(root==0)return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Blocked,0,arc);
            var prefix=root==seconds?trajectory:trajectory.Slice(0,root);
            var admission=gates.Preview(prefix,row.Index,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,arc);
            if(admission.Status!=ManualRacePathGateStatus.Clear||!AtStation(prefix.Sample(1),window,arc))
                return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Blocked,root,arc,admission);
            return new ManualRacePathBoundaryPreview(ManualRacePathBoundaryStatus.Boundary,root,arc,admission);
        }
        private ManualRacePathBoundaryPreview Whole(ManualSkiWorldTrajectory trajectory,ManualScopedWorldWindow window,bool strict=false)
        {
            var admission=gates.Preview(trajectory,window.OccurrenceIndex,window.PreviousArcNanometers,
                window.MaximumArcDisplacementMeters,window.MaximumAdmittedArcNanometers,strict);
            return new ManualRacePathBoundaryPreview(admission.Status==ManualRacePathGateStatus.Clear?
                ManualRacePathBoundaryStatus.Clear:ManualRacePathBoundaryStatus.Blocked,
                admission.ClearUntilSeconds,window.MaximumAdmittedArcNanometers,admission);
        }
        internal bool IsAtBoundary(ManualSkiWorldTrajectory source,ManualSkiHeadingState pose,ManualScopedWorldWindow window)
        {
            if(!catalog.TrySamplePortal(window.OccurrenceIndex,window.MaximumAdmittedArcNanometers,out var boundary))return false;
            return Math.Abs(Extension(pose,boundary))<=Reserve(source,pose,boundary)&&AtStation(pose,window,window.MaximumAdmittedArcNanometers);
        }
        private bool AtStation(ManualSkiHeadingState pose,ManualScopedWorldWindow window,long arc)
            =>catalog.TryProject(pose,window.OccurrenceIndex,window.PreviousArcNanometers,window.MaximumArcDisplacementMeters,out var at)&&at.PhysicalArcNanometers==arc;
        private static double Extension(ManualSkiHeadingState pose,ManualRacePathTarget boundary)
            =>(pose.X-boundary.X)*boundary.ForwardX+(pose.Z-boundary.Z)*boundary.ForwardZ;
        private static double Reserve(ManualSkiWorldTrajectory trajectory,ManualSkiHeadingState pose,ManualRacePathTarget boundary)
            =>2*trajectory.PositionErrorBoundMeters+64*2.2204460492503131e-16*(Math.Abs(pose.X)+Math.Abs(pose.Z)+Math.Abs(boundary.X)+Math.Abs(boundary.Z)+1);
        private static bool TryRoot(double a,double b,double c,double duration,out double root)
        {
            double result=double.PositiveInfinity;
            if(a==0)
            {
                if(b>0)Accept(-c/b);
            }
            else
            {
                double discriminant=b*b-4*a*c;
                if(discriminant>=0)
                {
                    // Stable quadratic formula also retains a short first
                    // crossing when the other root is many seconds away.
                    double q=-.5*(b+(b<0?-1:1)*Math.Sqrt(discriminant));
                    if(q!=0){Accept(q/a);Accept(c/q);}
                    else Accept(-b/(2*a));
                }
            }
            root=result;return result<=duration;
            void Accept(double at)
            {if(at>=0&&at<=duration&&b+2*a*at>=0&&at<result)result=at;}
        }
    }
}
