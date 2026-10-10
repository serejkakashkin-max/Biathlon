using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ManualRouteRibbonDiagnostics
    {
        public int LastVertexCount {get;}
        public int LastClearanceSamples {get;}
        public int LastDiskQueries {get;}
        public double LastStartClearance {get;}
        public double LastMinimumSampledClearance {get;}
        internal ManualRouteRibbonDiagnostics(int vertices,double start,double minimum,int samples,int disks)
        {LastVertexCount=vertices;LastStartClearance=start;LastMinimumSampledClearance=minimum;LastClearanceSamples=samples;LastDiskQueries=disks;}
    }
    /// <summary>Continuous whole-body exclusion inside a local authored ribbon.
    /// A single convex capsule first certifies the complete occupied rectangle.
    /// When it cannot, overlapping disks cover the rectangle, including its
    /// personal clearance. Every disk must fit a simple local polygon or an
    /// unchanged conservative connected tube. An exact rectangle/polygon
    /// separation proof avoids oversized disk caps on simple ribbons; a
    /// bounded cell cover also handles folded connected tubes.
    /// Failure grants no movement, route progress, docking or topology handoff.</summary>
    public sealed class ManualRouteRibbonSweep
    {
        private readonly ManualRouteProjectionMap map;
        private readonly SkiContactSweepDefinition work;
        private readonly double halfLength,halfWidth,coverRadius,refinedRadius,bodyRadius,edgeClearance;
        private readonly int disks,refinedRows;
        private readonly double sweepTolerance,softSnowShoulder;
        private readonly double[] x,z;
        private readonly double[] edgeX,edgeZ,edgeSquared,minX,maxX,minZ,maxZ;
        private int vertices;
        private bool simpleRibbon;
        private ManualConnectedTubeWindow tubeWindow;
        private ManualRangeApronSnowRegion rangeApron;
        internal void SetRangeApron(ManualRangeApronSnowRegion region)=>rangeApron=region;
        public int LastVertexCount=>vertices;
        private int clearanceSamples,diskQueries;
        public double LastStartClearance {get;private set;}
        public double LastMinimumSampledClearance {get;private set;}
        internal ManualSkiHeadingState LastThreatenedPose {get;private set;}
        public ManualRouteRibbonDiagnostics Diagnostics=>new ManualRouteRibbonDiagnostics(vertices,LastStartClearance,LastMinimumSampledClearance,clearanceSamples,diskQueries);
        public string ContextJson {get;}
        public ManualRouteRibbonSweep(ManualRouteProjectionMap map,OccupancyDefinition body,SkiContactSweepDefinition work,
            ManualNumericalToleranceDefinition tolerance=null)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));
            this.work=work??throw new ArgumentNullException(nameof(work));
            if(body==null)throw new ArgumentNullException(nameof(body));
            tolerance?.Validate(body);sweepTolerance=tolerance?.CourseSweepTolerance??0;
            softSnowShoulder=tolerance?.SoftSnowShoulder??0;
            disks=work.MaximumDepth;halfLength=body.LongitudinalSeparation*.5;
            halfWidth=body.LateralSeparation*.5;
            coverRadius=Math.Sqrt(halfWidth*halfWidth+Math.Pow(halfLength/disks,2));
            refinedRows=Math.Max(1,Math.Min(disks,(int)Math.Ceiling(disks*halfWidth/halfLength)));
            refinedRadius=Math.Sqrt(Math.Pow(halfWidth/refinedRows,2)+Math.Pow(halfLength/disks,2));
            bodyRadius=Math.Sqrt(halfWidth*halfWidth+halfLength*halfLength);
            edgeClearance=body.EdgeClearance-(tolerance?.CourseClearanceAllowance??0);
            x=new double[map.MaximumRibbonVertices];z=new double[x.Length];
            edgeX=new double[x.Length];edgeZ=new double[x.Length];edgeSquared=new double[x.Length];
            minX=new double[x.Length];maxX=new double[x.Length];minZ=new double[x.Length];maxZ=new double[x.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_local_authored_ribbon_v13_hierarchical_cover","projection_context_sha256:"+ManualContextIdentity.Sha256(map.ContextJson),
                body.ToJson(),work.ToJson(),tolerance?.ToJson()??"numerical_tolerance_not_selected",
                "all_rectangle_corners_in_one_owned_convex_capsule;certificate_threshold_includes_continuous_bound_and_existing_tolerance;actual_knot_normals;minimum_window_width;exact_authored_endpoints;simple_polygon_only;all_rectangle_corners_inside_and_all_edges_disjoint;minimum_segment_separation_minus_selected_edge_clearance;bounded_explicit_soft_snow_shoulder;per_disk_independent_conservative_tube_cover;unchanged_tube_turn_reserve;disk_count_from_maximum_depth;bounded_rectangular_cell_circumdisks;unchanged_full_body;full_cover_envelope_and_source_offset;continuous_whole_body_bound;no_phase_or_centre_credit"});
        }
        public ManualRouteTubePreview Preview(ManualSkiWorldTrajectory trajectory,ManualRouteProjection source)
        {
            if(!trajectory.IsValid||!source.IsValid)throw new ArgumentException("Owned source and initialized trajectory required.");
            double seconds=trajectory.DurationSeconds;
            clearanceSamples=diskQueries=0;
            var velocity=trajectory.VelocityAt(0);
            // The proof uses an overlapping disk cover, whose outer envelope
            // extends beyond the occupied rectangle. Internal window caps must
            // clear that envelope as well as the source projection offset.
            double reach=source.DistanceFromCentreMeters+halfLength+coverRadius+edgeClearance+softSnowShoulder+seconds*Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z)+
                .5*trajectory.PlanarAccelerationBound*seconds*seconds;
            reach*=map.SnowWindowMultiplier;
            LastStartClearance=LastMinimumSampledClearance=double.NaN;
            vertices=map.BuildLocalRibbon(source,reach,x,z);
            for(int i=0;i<vertices;i++)
            {
                int j=(i+1)%vertices;edgeX[i]=x[j]-x[i];edgeZ[i]=z[j]-z[i];edgeSquared[i]=edgeX[i]*edgeX[i]+edgeZ[i]*edgeZ[i];
                minX[i]=Math.Min(x[i],x[j]);maxX[i]=Math.Max(x[i],x[j]);minZ[i]=Math.Min(z[i],z[j]);maxZ[i]=Math.Max(z[i],z[j]);
            }
            simpleRibbon=Simple();
            if(!map.TryBuildConnectedWindow(source,reach,out tubeWindow))
                return new ManualRouteTubePreview(ManualRouteTubeStatus.UnresolvedStart,0,0,0,null);
            double reserve=2*trajectory.PositionErrorBoundMeters+bodyRadius*trajectory.HeadingErrorBoundRadians+
                128*2.2204460492503131e-16*(1+Math.Abs(trajectory.Sample(0).X)+Math.Abs(trajectory.Sample(0).Z))-sweepTolerance;
            LastThreatenedPose=trajectory.Sample(0);
            LastStartClearance=Clearance(LastThreatenedPose,certificateThreshold:reserve);LastMinimumSampledClearance=LastStartClearance;
            if(!(LastStartClearance>reserve))return new ManualRouteTubePreview(ManualRouteTubeStatus.UnresolvedStart,0,0,0,null);
            int checks=0;return Search(trajectory,0,seconds,0,reserve,ref checks);
        }
        private ManualRouteTubePreview Search(ManualSkiWorldTrajectory trajectory,double from,double to,int depth,double reserve,ref int checks)
        {
            if(checks>=work.MaximumIntervalChecks)return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,null);
            checks++;double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/trajectory.DurationSeconds;
            var v=trajectory.VelocityAt(fraction);
            double bound=Math.Sqrt(v.X*v.X+v.Z*v.Z)*h+.5*trajectory.PlanarAccelerationBound*h*h+
                bodyRadius*trajectory.PeakYawRate*h;
            double clearance=Clearance(trajectory.Sample(fraction),certificateThreshold:bound+reserve);
            if(clearance<LastMinimumSampledClearance){LastMinimumSampledClearance=clearance;LastThreatenedPose=trajectory.Sample(fraction);}
            if(clearance>bound+reserve)return new ManualRouteTubePreview(ManualRouteTubeStatus.Clear,to,to,checks,null);
            if(to-from<=work.TimeToleranceSeconds)return new ManualRouteTubePreview(ManualRouteTubeStatus.PotentialBoundary,from,to,checks,null);
            if(depth>=work.MaximumDepth||mid<=from||mid>=to)return new ManualRouteTubePreview(ManualRouteTubeStatus.WorkLimit,from,to,checks,null);
            var left=Search(trajectory,from,mid,depth+1,reserve,ref checks);
            return left.Status==ManualRouteTubeStatus.Clear?Search(trajectory,mid,to,depth+1,reserve,ref checks):left;
        }
        // A contact normal must compare the same full clearance field on both
        // sides of its probe. Certificate early-outs are valid for admission,
        // but switching certificates at zero introduces a false normal.
        internal double CurrentRibbonClearance(ManualSkiHeadingState pose)=>Clearance(pose,certificateThreshold:double.PositiveInfinity);
        internal double CurrentCoverClearance(ManualSkiHeadingState pose)=>Clearance(pose,true);
        private double Clearance(ManualSkiHeadingState pose,bool coverOnly=false,double certificateThreshold=0)
        {
            clearanceSamples++;
            double capsule=rangeApron?.Clearance(pose)??double.NegativeInfinity;
            if(capsule>certificateThreshold)return capsule;
            if(!coverOnly)
            {
                capsule=Math.Max(capsule,map.ConnectedTubeRectangleClearance(pose,tubeWindow,halfLength,halfWidth,edgeClearance)+softSnowShoulder);
                if(capsule>certificateThreshold)return capsule;
                if(vertices>=4&&simpleRibbon)
                {
                    capsule=Math.Max(capsule,RectangleClearance(pose,halfLength,halfWidth,edgeClearance,x,z,vertices));
                    if(capsule>certificateThreshold)return capsule;
                }
            }
            double result=double.PositiveInfinity,fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            for(int k=0;k<disks;k++)
            {
                double offset=halfLength*(2.0*k+1-disks)/disks;
                double px=pose.X+fx*offset,pz=pose.Z+fz*offset;
                result=Math.Min(result,DiskClearance(px,pz,coverRadius+edgeClearance));
                // A minimum cannot recover once it loses to the already known
                // capsule certificate. Preserve the exact maximum; skip work.
                if(result<=capsule)break;
            }
            result=Math.Max(result,capsule);
            if(result>0&&certificateThreshold!=double.PositiveInfinity)return result;
            if(!coverOnly&&vertices>=4&&simpleRibbon)
            {
                result=Math.Max(result,RectangleClearance(pose,halfLength,halfWidth,edgeClearance,x,z,vertices));
                if(result>0&&certificateThreshold!=double.PositiveInfinity)return result;
            }
            // The one-dimensional cover has unnecessarily large end caps.
            // A bounded rectangular cell grid refines that cover, never the
            // occupied body. Each cell's circumscribed disk plus the same
            // clearance covers its entire share of the rectangle.
            double refined=RefinedGridMinimum(pose,fx,fz,0,disks-1,0,refinedRows-1,double.PositiveInfinity,result);
            return Math.Max(result,refined);
        }
        // Bound an entire group of the SAME fine-grid disk centres before
        // evaluating its leaves. A containing rectangle certified inside one
        // tube or the simple polygon gives a lower bound for every leaf disk.
        // Once that bound cannot improve the current minimum, the group is
        // irrelevant. No cell radius, occupied body or admission margin changes.
        private double RefinedGridMinimum(ManualSkiHeadingState pose,double fx,double fz,int k0,int k1,int j0,int j1,double minimum,double floor)
        {
            if(minimum<=floor)return minimum;
            if(k0==k1&&j0==j1)
            {
                double along=halfLength*(2.0*k0+1-disks)/disks,side=halfWidth*(2.0*j0+1-refinedRows)/refinedRows;
                return Math.Min(minimum,DiskClearance(pose.X+fx*along+fz*side,pose.Z+fz*along-fx*side,refinedRadius+edgeClearance));
            }
            double a=halfLength*(k0+k1+1.0-disks)/disks,s=halfWidth*(j0+j1+1.0-refinedRows)/refinedRows;
            var centre=new ManualSkiHeadingState(pose.X+fx*a+fz*s,pose.Z+fz*a-fx*s,pose.HeadingRadians,pose.YawRateRadiansPerSecond);
            double length=halfLength*(k1-k0)/disks,width=halfWidth*(j1-j0)/refinedRows,radius=refinedRadius+edgeClearance;
            double extent=Math.Sqrt(length*length+width*width);
            // Distance to a union of tubes is 1-Lipschitz. Polygon distance
            // supplies the same bound only when the entire centre group stays
            // inside it; otherwise use tubes alone. This avoids four-corner
            // polygon intersection work at every internal tree node.
            double lower=map.ConnectedTubeDiskClearance(centre.X,centre.Z,tubeWindow,radius)+softSnowShoulder-extent;
            if(vertices>=4&&simpleRibbon&&Inside(centre.X,centre.Z))
            {
                double distance=PolygonBoundaryDistance(centre.X,centre.Z);
                if(distance>extent)lower=Math.Max(lower,distance-extent-radius);
            }
            double reserve=256*2.2204460492503131e-16*(1+Math.Abs(pose.X)+Math.Abs(pose.Z)+Math.Abs(lower)+Math.Abs(minimum));
            if(lower-minimum>reserve)return minimum;
            if((k1-k0)*halfLength/disks>=(j1-j0)*halfWidth/refinedRows&&k0<k1)
            {
                int mid=(k0+k1)/2;
                minimum=RefinedGridMinimum(pose,fx,fz,k0,mid,j0,j1,minimum,floor);
                return RefinedGridMinimum(pose,fx,fz,mid+1,k1,j0,j1,minimum,floor);
            }
            else
            {
                int mid=(j0+j1)/2;
                minimum=RefinedGridMinimum(pose,fx,fz,k0,k1,j0,mid,minimum,floor);
                return RefinedGridMinimum(pose,fx,fz,k0,k1,mid+1,j1,minimum,floor);
            }
        }
        private double DiskClearance(double px,double pz,double radius)
        {
                diskQueries++;
                double diskClearance=map.ConnectedTubeDiskClearance(px,pz,tubeWindow,radius)+softSnowShoulder;
                if(vertices<4||!simpleRibbon||!Inside(px,pz))return diskClearance;
                return Math.Max(diskClearance,PolygonBoundaryDistance(px,pz)-radius);
        }
        private double PolygonBoundaryDistance(double px,double pz)
        {
                double distance=double.PositiveInfinity;
                for(int i=0;i<vertices;i++)
                {
                    // A segment cannot beat the nearest distance if even its
                    // enclosing box is farther away. Values are cached once
                    // per owned ribbon, not rebuilt for every cell/probe.
                    double bx=Math.Max(0,Math.Max(minX[i]-px,px-maxX[i])),bz=Math.Max(0,Math.Max(minZ[i]-pz,pz-maxZ[i]));
                    if(bx*bx+bz*bz>distance)continue;
                    double dx=edgeX[i],dz=edgeZ[i],length=edgeSquared[i];
                    double t=length==0?0:Math.Max(0,Math.Min(1,((px-x[i])*dx+(pz-z[i])*dz)/length));
                    double ax=px-x[i]-t*dx,az=pz-z[i]-t*dz;distance=Math.Min(distance,ax*ax+az*az);
                }
                return Math.Sqrt(distance);
        }
        private bool Inside(double px,double pz)
            =>Inside(px,pz,x,z,vertices);
        private static bool Inside(double px,double pz,double[] x,double[] z,int vertices)
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
        // For a simple polygon, inside corners plus disjoint complete edges
        // prove the entire convex rectangle is inside. Corner tests alone do
        // not exclude a concave notch. Segment separation is a conservative
        // continuous clearance: translation/rotation obey the same Search
        // bound as the disk cover, including the unchanged numerical reserve.
        internal static double RectangleClearance(ManualSkiHeadingState pose,double halfLength,double halfWidth,
            double edgeClearance,double[] x,double[] z,int vertices)
        {
            double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians),minimum=double.PositiveInfinity;
            for(int k=0;k<4;k++)
            {
                RectangleCorner(k,pose,fx,fz,halfLength,halfWidth,out var ax,out var az);
                if(!Inside(ax,az,x,z,vertices))return double.NegativeInfinity;
                RectangleCorner((k+1)%4,pose,fx,fz,halfLength,halfWidth,out var bx,out var bz);
                for(int i=0;i<vertices;i++)
                {
                    int j=(i+1)%vertices;
                    if(Intersect(ax,az,bx,bz,x[i],z[i],x[j],z[j]))return -edgeClearance;
                    minimum=Math.Min(minimum,PointSegmentSquared(ax,az,x[i],z[i],x[j],z[j]));
                    minimum=Math.Min(minimum,PointSegmentSquared(bx,bz,x[i],z[i],x[j],z[j]));
                    minimum=Math.Min(minimum,PointSegmentSquared(x[i],z[i],ax,az,bx,bz));
                    minimum=Math.Min(minimum,PointSegmentSquared(x[j],z[j],ax,az,bx,bz));
                }
            }
            return Math.Sqrt(minimum)-edgeClearance;
        }
        private static void RectangleCorner(int corner,ManualSkiHeadingState pose,double fx,double fz,
            double halfLength,double halfWidth,out double px,out double pz)
        {
            double along=corner<2?halfLength:-halfLength,side=corner==0||corner==3?halfWidth:-halfWidth;
            px=pose.X+fx*along+fz*side;pz=pose.Z+fz*along-fx*side;
        }
        private static double PointSegmentSquared(double px,double pz,double ax,double az,double bx,double bz)
        {
            double dx=bx-ax,dz=bz-az,length=dx*dx+dz*dz;
            double t=length==0?0:Math.Max(0,Math.Min(1,((px-ax)*dx+(pz-az)*dz)/length));
            dx=px-ax-t*dx;dz=pz-az-t*dz;return dx*dx+dz*dz;
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
