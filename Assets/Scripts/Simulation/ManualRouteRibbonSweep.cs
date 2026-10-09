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
    /// personal clearance. Every disk must fit a simple local polygon or an
    /// unchanged conservative connected tube. A bounded cell cover refines
    /// oversized end caps; internal sampling caps are not walls.
    /// Failure grants no movement, route progress, docking or topology handoff.</summary>
    public sealed class ManualRouteRibbonSweep
    {
        private readonly ManualRouteProjectionMap map;
        private readonly SkiContactSweepDefinition work;
        private readonly double halfLength,halfWidth,coverRadius,refinedRadius,bodyRadius,edgeClearance;
        private readonly int disks,refinedRows;
        private readonly double[] x,z;
        private int vertices;
        private bool simpleRibbon;
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
            halfWidth=body.LateralSeparation*.5;
            coverRadius=Math.Sqrt(halfWidth*halfWidth+Math.Pow(halfLength/disks,2));
            refinedRows=Math.Max(1,Math.Min(disks,(int)Math.Ceiling(disks*halfWidth/halfLength)));
            refinedRadius=Math.Sqrt(Math.Pow(halfWidth/refinedRows,2)+Math.Pow(halfLength/disks,2));
            bodyRadius=Math.Sqrt(halfWidth*halfWidth+halfLength*halfLength);edgeClearance=body.EdgeClearance;
            x=new double[map.MaximumRibbonVertices];z=new double[x.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_local_authored_ribbon_disk_cover_v5_refined_cells",map.ContextJson,
                body.ToJson(),work.ToJson(),"actual_knot_normals;minimum_window_width;exact_authored_endpoints;simple_polygon_only;per_disk_independent_conservative_tube_cover;unchanged_tube_turn_reserve;disk_count_from_maximum_depth;bounded_rectangular_cell_circumdisks;unchanged_full_body_and_edge_clearance;full_cover_envelope_and_source_offset;continuous_cover_not_corner_samples;no_phase_or_centre_credit"});
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
            simpleRibbon=Simple();
            if(!map.TryBuildConnectedWindow(source,reach,out tubeWindow))
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
                result=Math.Min(result,DiskClearance(px,pz,coverRadius+edgeClearance));
            }
            if(result>0)return result;
            // The one-dimensional cover has unnecessarily large end caps.
            // A bounded rectangular cell grid refines that cover, never the
            // occupied body. Each cell's circumscribed disk plus the same
            // clearance covers its entire share of the rectangle.
            double refined=double.PositiveInfinity;
            for(int k=0;k<disks;k++)for(int j=0;j<refinedRows;j++)
            {
                double along=halfLength*(2.0*k+1-disks)/disks;
                double side=halfWidth*(2.0*j+1-refinedRows)/refinedRows;
                refined=Math.Min(refined,DiskClearance(pose.X+fx*along+fz*side,
                    pose.Z+fz*along-fx*side,refinedRadius+edgeClearance));
            }
            return Math.Max(result,refined);
        }
        private double DiskClearance(double px,double pz,double radius)
        {
                double diskClearance=map.ConnectedTubeDiskClearance(px,pz,tubeWindow,radius);
                if(vertices<4||!simpleRibbon||!Inside(px,pz))return diskClearance;
                double distance=double.PositiveInfinity;
                for(int i=0;i<vertices;i++)
                {
                    int j=(i+1)%vertices;double dx=x[j]-x[i],dz=z[j]-z[i],length=dx*dx+dz*dz;
                    double t=length==0?0:Math.Max(0,Math.Min(1,((px-x[i])*dx+(pz-z[i])*dz)/length));
                    double ax=px-x[i]-t*dx,az=pz-z[i]-t*dz;distance=Math.Min(distance,ax*ax+az*az);
                }
                return Math.Max(diskClearance,Math.Sqrt(distance)-radius);
        }
        private bool Inside(double px,double pz)
        {
            // Called only after the complete local polygon is proved simple.
            int winding=0;
            for(int i=0,j=vertices-1;i<vertices;j=i++)
            {
                double side=(x[i]-x[j])*(pz-z[j])-(px-x[j])*(z[i]-z[j]);
                if(z[j]<=pz){if(z[i]>pz&&side>0)winding++;}
                else if(z[i]<=pz&&side<0)winding--;
            }
            return winding!=0;
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
