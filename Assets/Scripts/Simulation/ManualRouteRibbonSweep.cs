using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ManualRouteRibbonDiagnostics
    {
        public int LastVertexCount {get;}
        public double LastStartClearance {get;}
        public double LastMinimumSampledClearance {get;}
        internal ManualRouteRibbonDiagnostics(int vertices,double start,double minimum)
        {LastVertexCount=vertices;LastStartClearance=start;LastMinimumSampledClearance=minimum;}
    }
    /// <summary>Continuous whole-body exclusion inside a local authored ribbon.
    /// Overlapping disks cover the complete occupied rectangle, including its
    /// personal clearance. Every disk must remain inside a simple local polygon
    /// and clear its exterior boundary; internal sampling caps are not walls.
    /// Failure grants no movement, route progress, docking or topology handoff.</summary>
    public sealed class ManualRouteRibbonSweep
    {
        private readonly ManualRouteProjectionMap map;
        private readonly SkiContactSweepDefinition work;
        private readonly double halfLength,coverRadius,bodyRadius,edgeClearance;
        private readonly int disks;
        private readonly double[] x,z;
        private int vertices;
        private bool useTubes;
        private ManualConnectedTubeWindow tubeWindow;
        public int LastVertexCount=>vertices;
        public double LastStartClearance {get;private set;}
        public double LastMinimumSampledClearance {get;private set;}
        public ManualRouteRibbonDiagnostics Diagnostics=>new ManualRouteRibbonDiagnostics(vertices,LastStartClearance,LastMinimumSampledClearance);
        public string ContextJson {get;}
        public ManualRouteRibbonSweep(ManualRouteProjectionMap map,OccupancyDefinition body,SkiContactSweepDefinition work)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));
            this.work=work??throw new ArgumentNullException(nameof(work));
            if(body==null)throw new ArgumentNullException(nameof(body));
            disks=work.MaximumDepth;halfLength=body.LongitudinalSeparation*.5;
            double halfWidth=body.LateralSeparation*.5;
            coverRadius=Math.Sqrt(halfWidth*halfWidth+Math.Pow(halfLength/disks,2));
            bodyRadius=Math.Sqrt(halfWidth*halfWidth+halfLength*halfLength);edgeClearance=body.EdgeClearance;
            x=new double[map.MaximumRibbonVertices];z=new double[x.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_local_authored_ribbon_disk_cover_v3",map.ContextJson,
                body.ToJson(),work.ToJson(),"actual_knot_normals;minimum_window_width;exact_authored_endpoints;simple_polygon_or_union_of_existing_conservative_tubes;unchanged_tube_turn_reserve;disk_count_from_maximum_depth;full_cover_envelope_and_source_offset;continuous_cover_not_corner_samples;no_phase_or_centre_credit"});
        }
        public ManualRouteTubePreview Preview(ManualSkiWorldTrajectory trajectory,ManualRouteProjection source)
        {
            if(!trajectory.IsValid||!source.IsValid)throw new ArgumentException("Owned source and initialized trajectory required.");
            double seconds=trajectory.DurationSeconds;
            var velocity=trajectory.VelocityAt(0);
            // The proof uses an overlapping disk cover, whose outer envelope
            // extends beyond the occupied rectangle. Internal window caps must
            // clear that envelope as well as the source projection offset.
            double reach=source.DistanceFromCentreMeters+halfLength+coverRadius+edgeClearance+seconds*Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z)+
                .5*trajectory.PlanarAccelerationBound*seconds*seconds;
            LastStartClearance=LastMinimumSampledClearance=double.NaN;
            vertices=map.BuildLocalRibbon(source,reach,x,z);
            useTubes=vertices<4||!Simple();
            if(useTubes&&!map.TryBuildConnectedWindow(source,reach,out tubeWindow))
                return new ManualRouteTubePreview(ManualRouteTubeStatus.UnresolvedStart,0,0,0,null);
            double reserve=2*trajectory.PositionErrorBoundMeters+bodyRadius*trajectory.HeadingErrorBoundRadians+
                128*2.2204460492503131e-16*(1+Math.Abs(trajectory.Sample(0).X)+Math.Abs(trajectory.Sample(0).Z));
            LastStartClearance=Clearance(trajectory.Sample(0));LastMinimumSampledClearance=LastStartClearance;
            if(!(LastStartClearance>reserve))return new ManualRouteTubePreview(ManualRouteTubeStatus.UnresolvedStart,0,0,0,null);
            int checks=0;return Search(trajectory,0,seconds,0,reserve,ref checks);
        }
        private ManualRouteTubePreview Search(ManualSkiWorldTrajectory trajectory,double from,double to,int depth,double reserve,ref int checks)
        {
            if(checks>=work.MaximumIntervalChecks)return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,null);
            checks++;double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/trajectory.DurationSeconds;
            double clearance=Clearance(trajectory.Sample(fraction));var v=trajectory.VelocityAt(fraction);
            LastMinimumSampledClearance=Math.Min(LastMinimumSampledClearance,clearance);
            double bound=Math.Sqrt(v.X*v.X+v.Z*v.Z)*h+.5*trajectory.PlanarAccelerationBound*h*h+
                bodyRadius*trajectory.PeakYawRate*h;
            if(clearance>bound+reserve)return new ManualRouteTubePreview(ManualRouteTubeStatus.Clear,to,to,checks,null);
            if(to-from<=work.TimeToleranceSeconds)return new ManualRouteTubePreview(ManualRouteTubeStatus.PotentialBoundary,from,to,checks,null);
            if(depth>=work.MaximumDepth||mid<=from||mid>=to)return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,null);
            var left=Search(trajectory,from,mid,depth+1,reserve,ref checks);
            return left.Status==ManualRouteTubeStatus.Clear?Search(trajectory,mid,to,depth+1,reserve,ref checks):left;
        }
        private double Clearance(ManualSkiHeadingState pose)
        {
            double result=double.PositiveInfinity,fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            for(int k=0;k<disks;k++)
            {
                double offset=halfLength*(2.0*k+1-disks)/disks;
                double px=pose.X+fx*offset,pz=pose.Z+fz*offset;
                if(useTubes)
                {
                    result=Math.Min(result,map.ConnectedTubeDiskClearance(px,pz,tubeWindow,coverRadius+edgeClearance));
                    continue;
                }
                if(!Inside(px,pz))return double.NegativeInfinity;
                double distance=double.PositiveInfinity;
                for(int i=0;i<vertices;i++)
                {
                    int j=(i+1)%vertices;double dx=x[j]-x[i],dz=z[j]-z[i],length=dx*dx+dz*dz;
                    if(length==0)return double.NegativeInfinity;
                    double t=Math.Max(0,Math.Min(1,((px-x[i])*dx+(pz-z[i])*dz)/length));
                    double ax=px-x[i]-t*dx,az=pz-z[i]-t*dz;distance=Math.Min(distance,ax*ax+az*az);
                }
                result=Math.Min(result,Math.Sqrt(distance)-coverRadius-edgeClearance);
            }
            return result;
        }
        private bool Inside(double px,double pz)
        {
            bool inside=false;
            for(int i=0,j=vertices-1;i<vertices;j=i++)
                if((z[i]>pz)!=(z[j]>pz)&&px<(x[j]-x[i])*(pz-z[i])/(z[j]-z[i])+x[i])inside=!inside;
            return inside;
        }
        private bool Simple()
        {
            for(int i=0;i<vertices;i++)
            {
                int ni=(i+1)%vertices;
                if(x[i]==x[ni]&&z[i]==z[ni])return false;
                for(int j=i+1;j<vertices;j++)
                {
                    int nj=(j+1)%vertices;if(j==ni||nj==i)continue;
                    if(Intersect(x[i],z[i],x[ni],z[ni],x[j],z[j],x[nj],z[nj]))return false;
                }
            }
            return true;
        }
        private static bool Intersect(double ax,double az,double bx,double bz,double cx,double cz,double dx,double dz)
        {
            if(Math.Max(ax,bx)<Math.Min(cx,dx)||Math.Max(cx,dx)<Math.Min(ax,bx)||
                Math.Max(az,bz)<Math.Min(cz,dz)||Math.Max(cz,dz)<Math.Min(az,bz))return false;
            double a=(bx-ax)*(cz-az)-(bz-az)*(cx-ax),b=(bx-ax)*(dz-az)-(bz-az)*(dx-ax);
            double c=(dx-cx)*(az-cz)-(dz-cz)*(ax-cx),d=(dx-cx)*(bz-cz)-(dz-cz)*(bx-cx);
            return (a==0||b==0||Math.Sign(a)!=Math.Sign(b))&&(c==0||d==0||Math.Sign(c)!=Math.Sign(d));
        }
    }
}
