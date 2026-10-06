using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ManualRouteTubeStatus { Clear, UnresolvedStart, PotentialBoundary, WorkLimit }
    public readonly struct ManualRouteTubePreview
    {
        public bool IsValid {get;}
        public ManualRouteTubeStatus Status {get;}
        public double ClearUntilSeconds {get;}
        public double IntervalEndSeconds {get;}
        public int IntervalChecks {get;}
        public string SourceEdgeId {get;}
        internal ManualRouteTubePreview(ManualRouteTubeStatus status,double from,double to,int checks,string source)
        {IsValid=true;Status=status;ClearUntilSeconds=from;IntervalEndSeconds=to;IntervalChecks=checks;SourceEdgeId=source;}
    }

    /// <summary>Continuous conservative body-centre tube across many short visual
    /// edges of one selected route occurrence. A circumscribed body radius plus
    /// edge clearance and the authored turn reserve are removed from each width.
    /// This is spatial exclusion only: it cannot admit a route branch or award progress.</summary>
    public sealed class ManualRouteTubeSweep
    {
        private readonly ManualRouteProjectionMap route;
        private readonly OccupancyDefinition body;
        private readonly SkiContactSweepDefinition work;
        public string ContextJson {get;}
        public ManualRouteTubeSweep(ManualRouteProjectionMap route,OccupancyDefinition body,SkiContactSweepDefinition work)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            this.body=body??throw new ArgumentNullException(nameof(body));
            this.work=work??throw new ArgumentNullException(nameof(work));
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_selected_route_inner_tube_sweep_v1",
                route.CorridorContextJson,body.ToJson(),work.ToJson(),
                "circumscribed_body;minimum_authored_width;maximum_visual_turn_reserve;local_station_window"});
        }

        public ManualRouteTubePreview Preview(ManualSkiWorldTrajectory trajectory,int currentSection,
            long previousStationNanometers,double maximumStationDisplacementMeters,bool admitNextSection)
        {
            if(!trajectory.IsValid)throw new ArgumentException("Initialized world trajectory required.",nameof(trajectory));
            var window=route.BuildTubeWindow(currentSection,previousStationNanometers,
                maximumStationDisplacementMeters,admitNextSection,body);
            double reserve=2*trajectory.PositionErrorBoundMeters+
                64*2.2204460492503131e-16*(Math.Abs(trajectory.Sample(0).X)+Math.Abs(trajectory.Sample(0).Z)+1);
            var start=trajectory.Sample(0);
            double startSlack=route.TubeClearanceAt(start.X,start.Z,window,out var startEdge);
            if(!(startSlack>reserve))return new ManualRouteTubePreview(ManualRouteTubeStatus.UnresolvedStart,0,0,0,startEdge);
            int checks=0;
            return Search(trajectory,window,0,trajectory.DurationSeconds,0,reserve,ref checks);
        }

        private ManualRouteTubePreview Search(ManualSkiWorldTrajectory trajectory,ManualRouteTubeWindow window,
            double from,double to,int depth,double reserve,ref int checks)
        {
            if(checks>=work.MaximumIntervalChecks)
                return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,null);
            checks++;
            double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/trajectory.DurationSeconds;
            var pose=trajectory.Sample(fraction);
            double slack=route.TubeClearanceAt(pose.X,pose.Z,window,out var edgeId);
            var velocity=trajectory.VelocityAt(fraction);
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double travelBound=speed*h+.5*trajectory.PlanarAccelerationBound*h*h;
            // Distance to a fixed centreline segment is 1-Lipschitz. If one
            // segment's clearance covers the whole motion/error ball, every
            // oriented body pose in this time interval remains inside its tube.
            if(slack>travelBound+reserve)
                return new ManualRouteTubePreview(ManualRouteTubeStatus.Clear,to,to,checks,edgeId);
            if(to-from<=work.TimeToleranceSeconds)
                return new ManualRouteTubePreview(ManualRouteTubeStatus.PotentialBoundary,from,to,checks,edgeId);
            if(depth>=work.MaximumDepth||mid<=from||mid>=to)
                return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,edgeId);
            var left=Search(trajectory,window,from,mid,depth+1,reserve,ref checks);
            if(left.Status!=ManualRouteTubeStatus.Clear)return left;
            return Search(trajectory,window,mid,to,depth+1,reserve,ref checks);
        }
    }
}
