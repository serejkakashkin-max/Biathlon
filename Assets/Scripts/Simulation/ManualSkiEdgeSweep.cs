using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>One inward-facing half-plane of an explicitly admitted convex
    /// corridor cell. Not a global replacement for a curved/branched course.</summary>
    public readonly struct SkiCourseEdgePlane
    {
        public bool IsValid {get;}
        public string Id {get;}
        public double X {get;}
        public double Z {get;}
        public double InwardX {get;}
        public double InwardZ {get;}
        public SkiCourseEdgePlane(string id,double x,double z,double inwardX,double inwardZ)
        {
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Stable edge ID required.",nameof(id));
            X=PhysiologyNumbers.RequireRange(x,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(x));
            Z=PhysiologyNumbers.RequireRange(z,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(z));
            PhysiologyNumbers.RequireRange(inwardX,-1,1,nameof(inwardX));PhysiologyNumbers.RequireRange(inwardZ,-1,1,nameof(inwardZ));
            double length=Math.Sqrt(inwardX*inwardX+inwardZ*inwardZ);
            if(length<PhysiologyNumbers.MinimumPositive)throw new ArgumentException("Nonzero inward edge normal required.");
            Id=id;InwardX=inwardX/length;InwardZ=inwardZ/length;IsValid=true;
        }
    }
    public enum SkiEdgeSweepStatus { Clear, UnresolvedStart, PotentialBoundary, WorkLimit }
    public readonly struct SkiEdgeSweepPreview
    {
        public bool IsValid {get;}
        public SkiEdgeSweepStatus Status {get;}
        public double ClearUntilSeconds {get;}
        public double IntervalEndSeconds {get;}
        public int IntervalChecks {get;}
        internal SkiEdgeSweepPreview(SkiEdgeSweepStatus status,double from,double to,int checks)
        {IsValid=true;Status=status;ClearUntilSeconds=from;IntervalEndSeconds=to;IntervalChecks=checks;}
    }

    /// <summary>Continuous oriented-body test against a single course edge.
    /// Reports an unproved prefix boundary, never clamps position or auto-steers.</summary>
    public sealed class ManualSkiEdgeSweep
    {
        private readonly double halfLength,halfWidth,radius,margin,tolerance;
        private readonly SkiContactSweepDefinition definition;
        public string ContextJson {get;}
        public ManualSkiEdgeSweep(OccupancyDefinition body,SkiContactDefinition contact,SkiContactSweepDefinition definition)
        {
            if(body==null||contact==null||definition==null)throw new ArgumentNullException();
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            radius=Math.Sqrt(halfLength*halfLength+halfWidth*halfWidth);
            margin=body.EdgeClearance;tolerance=contact.ContactTolerance;this.definition=definition;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_convex_edge_sweep_v1",body.ToJson(),contact.ToJson(),definition.ToJson()});
        }
        public SkiEdgeSweepPreview Preview(ManualSkiWorldTrajectory trajectory,SkiCourseEdgePlane edge)
        {
            if(!trajectory.IsValid||!edge.IsValid)throw new ArgumentException("Initialized trajectory and admitted edge required.");
            double reserve=2*(trajectory.PositionErrorBoundMeters+radius*trajectory.HeadingErrorBoundRadians)
                +64*2.2204460492503131e-16*(Math.Abs(edge.X)+Math.Abs(edge.Z)+halfLength+halfWidth+1);
            if(Gap(trajectory.Sample(0),edge)<=reserve-tolerance)
                return new SkiEdgeSweepPreview(SkiEdgeSweepStatus.UnresolvedStart,0,0,0);
            int checks=0;
            return Search(trajectory,edge,0,trajectory.DurationSeconds,0,reserve,ref checks);
        }
        private SkiEdgeSweepPreview Search(ManualSkiWorldTrajectory trajectory,SkiCourseEdgePlane edge,double from,double to,
            int depth,double reserve,ref int checks)
        {
            if(checks>=definition.MaximumIntervalChecks)return new SkiEdgeSweepPreview(SkiEdgeSweepStatus.WorkLimit,from,to,checks);
            checks++;
            double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/trajectory.DurationSeconds;
            var velocity=trajectory.VelocityAt(fraction);
            double translation=Math.Abs(velocity.X*edge.InwardX+velocity.Z*edge.InwardZ)*h+.5*trajectory.PlanarAccelerationBound*h*h;
            double rotation=radius*Math.Min(2,trajectory.PeakYawRate*h);
            if(Gap(trajectory.Sample(fraction),edge)>reserve-tolerance+translation+rotation)
                return new SkiEdgeSweepPreview(SkiEdgeSweepStatus.Clear,to,to,checks);
            if(to-from<=definition.TimeToleranceSeconds)return new SkiEdgeSweepPreview(SkiEdgeSweepStatus.PotentialBoundary,from,to,checks);
            if(depth>=definition.MaximumDepth||mid<=from||mid>=to)return new SkiEdgeSweepPreview(SkiEdgeSweepStatus.WorkLimit,from,to,checks);
            var left=Search(trajectory,edge,from,mid,depth+1,reserve,ref checks);
            if(left.Status!=SkiEdgeSweepStatus.Clear)return left;
            return Search(trajectory,edge,mid,to,depth+1,reserve,ref checks);
        }
        private double Gap(ManualSkiHeadingState pose,SkiCourseEdgePlane edge)
        {
            double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            double support=halfLength*Math.Abs(fx*edge.InwardX+fz*edge.InwardZ)+halfWidth*Math.Abs(fz*edge.InwardX-fx*edge.InwardZ);
            return (pose.X-edge.X)*edge.InwardX+(pose.Z-edge.Z)*edge.InwardZ-support-margin;
        }
    }
}
