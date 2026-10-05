using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    // New optional apron uses the accepted visible right-normal line convention. Existing
    // start/directed passage projectors, contexts and arithmetic remain untouched.
    // Any axis separating complete rotating-body envelopes proves safety; no axis blocks.
    internal sealed class PhysicalApronBodySupportDraft
    {
        private readonly PhysicalPassageCorridor source,target;
        private readonly double halfLength,halfWidth,radius,margin;
        private readonly bool indexedBounds;
        private const double DoubleUnitRoundoff=2.2204460492503131e-16;
        public double ProjectionMarginMeters=>margin;
        public PhysicalApronBodySupportDraft(PhysicalPassageCorridor source,PhysicalPassageCorridor target,
            OccupancyDefinition body,VenueValidationPolicy policy, bool allBodyHeadings = false, bool indexedBounds = false)
        {
            this.source=source;this.target=target;
            this.indexedBounds=indexedBounds;
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            radius=Math.Sqrt(halfLength*halfLength+halfWidth*halfWidth);
            // An enclosing square in every path heading contains the unchanged rectangular
            // body's circumcircle, hence every setup/prone/standing/recovery orientation.
            // It is conservative and leaves the actual accepted centre/lateral offset intact.
            if(allBodyHeadings) halfLength=halfWidth=radius;
            double maximumScale=0,maximumCoordinate=0;
            Reserve(source);Reserve(target);
            margin=Math.Max(policy.SeamTolerance,maximumScale/SkiState.NanometersPerMeter+
                32*DoubleUnitRoundoff*(maximumCoordinate+radius+1));
            void Reserve(PhysicalPassageCorridor corridor)
            {
                for(int i=0;i<corridor.Count;i++)
                {
                    var p=corridor.Get(i);maximumCoordinate=Math.Max(maximumCoordinate,Math.Max(Math.Abs(p.X),Math.Abs(p.Z)));
                    if(i==0)continue;var prior=corridor.Get(i-1);double dx=p.X-prior.X,dz=p.Z-prior.Z;
                    Direction(dx,dz,out _,out _,out double horizontal);
                    maximumScale=Math.Max(maximumScale,horizontal/(p.Distance-prior.Distance));
                }
            }
        }
        public bool Conflicts(PhysicalOccupancySweep a,PhysicalOccupancySweep b)
        {
            if(Separated(a,b,1,0)||Separated(a,b,0,1))return false;
            if(SeparatedOnEdges(source,a,b)||SeparatedOnEdges(target,a,b))return false;
            return true;
        }
        private bool SeparatedOnEdges(PhysicalPassageCorridor corridor,PhysicalOccupancySweep a,PhysicalOccupancySweep b)
        {
            for(int i=1;i<corridor.Count;i++)
            {
                var p=corridor.Get(i-1);var q=corridor.Get(i);Direction(q.X-p.X,q.Z-p.Z,out double x,out double z,out _);
                if(Separated(a,b,x,z)||Separated(a,b,-z,x))return true;
            }
            return false;
        }
        private bool Separated(PhysicalOccupancySweep a,PhysicalOccupancySweep b,double x,double z)
        {
            Bounds(a,x,z,out double alow,out double ahigh);Bounds(b,x,z,out double blow,out double bhigh);
            double lateralMagnitude=Math.Max(Math.Max(Math.Abs(a.LateralBeforeMeters),Math.Abs(a.LateralAfterMeters)),
                Math.Max(Math.Abs(b.LateralBeforeMeters),Math.Abs(b.LateralAfterMeters)));
            double reserve=margin+32*DoubleUnitRoundoff*lateralMagnitude;
            return ahigh+reserve<blow-reserve||bhigh+reserve<alow-reserve;
        }
        internal void Bounds(PhysicalOccupancySweep sweep,double x,double z,out double low,out double high)
        {
            var corridor=sweep.Start.LoopId==source.LoopId?source:target;
            double start=sweep.Start.LocalNanometers/(double)SkiState.NanometersPerMeter;
            double travel=sweep.TravelNanometers/(double)SkiState.NanometersPerMeter,end=start+travel;
            double minimum=double.MaxValue,maximum=double.MinValue;
            for(int i=indexedBounds?FirstIntersectingEdge(corridor,start):1;i<corridor.Count;i++)
            {
                var a=corridor.Get(i-1);var b=corridor.Get(i);
                if(indexedBounds&&a.Distance>end)break;
                double from=Math.Max(start,a.Distance),to=Math.Min(end,b.Distance);
                if(from>to)continue;
                double dx=b.X-a.X,dz=b.Z-a.Z;Direction(dx,dz,out double fx,out double fz,out _);
                double extent=halfLength*Math.Abs(fx*x+fz*z)+halfWidth*Math.Abs(-fz*x+fx*z);
                Add(from);Add(to);
                // The centre and lateral offset vary linearly on each edge, so endpoint
                // projections enclose its entire swept rectangle. At a tangent change the
                // corner support includes every intermediate body rotation and offset normal.
                if(i<corridor.Count-1&&b.Distance>=start&&b.Distance<=end)
                {
                    double t=travel==0?0:(b.Distance-start)/travel;
                    double lateral=sweep.LateralBeforeMeters+(sweep.LateralAfterMeters-sweep.LateralBeforeMeters)*t;
                    var next=corridor.Get(i+1);Direction(next.X-b.X,next.Z-b.Z,out double nx,out double nz,out _);
                    double sine=fx*nz-fz*nx,cosine=fx*nx+fz*nz;
                    double turn=Math.Atan2(sine,cosine);
                    Rotation(lateral);
                    if(travel==0)Rotation(sweep.LateralAfterMeters);
                    void Rotation(double line)
                    {
                        if(sine==0&&cosine<0)
                        {
                            // An exact half turn has two equally short arcs. Without an
                            // authoritative direction both remain possible and blocking.
                            double centre=(b.X+fz*line)*x+(b.Z-fx*line)*z;
                            double rotation=radius+2*Math.Abs(line);
                            minimum=Math.Min(minimum,centre-rotation);maximum=Math.Max(maximum,centre+rotation);return;
                        }
                        double origin=b.X*x+b.Z*z;
                        double forward=fx*x+fz*z,normal=fz*x-fx*z;
                        double nextForward=nx*x+nz*z,nextNormal=nz*x-nx*z;
                        for(int along=-1;along<=1;along+=2)for(int across=-1;across<=1;across+=2)
                        {
                            double longitudinal=along*halfLength,offset=line+across*halfWidth;
                            // Each corner projects as A*cos(theta)+B*sin(theta).
                            // Endpoints and any enclosed max/min critical angle are the
                            // complete support of the shortest signed rotation arc.
                            double a=longitudinal*forward+offset*normal;
                            double c=offset*forward-longitudinal*normal;
                            AddCorner(a);AddCorner(longitudinal*nextForward+offset*nextNormal);
                            double peak=Math.Atan2(c,a),trough=Math.Atan2(-c,-a);
                            Direction(a,c,out _,out _,out double reach);
                            if(InTurn(peak))AddCorner(reach);
                            if(InTurn(trough))AddCorner(-reach);
                        }
                        bool InTurn(double angle)=>turn>=0?angle>=0&&angle<=turn:angle<=0&&angle>=turn;
                        void AddCorner(double projection)
                        {minimum=Math.Min(minimum,origin+projection);maximum=Math.Max(maximum,origin+projection);}
                    }
                }
                void Add(double distance)
                {
                    double t=(distance-a.Distance)/(b.Distance-a.Distance);
                    double time=travel==0?0:(distance-start)/travel;
                    double lateral=sweep.LateralBeforeMeters+(sweep.LateralAfterMeters-sweep.LateralBeforeMeters)*time;
                    Project(lateral);
                    if(travel==0)Project(sweep.LateralAfterMeters);
                    void Project(double line)
                    {
                        double centre=(a.X+dx*t+fz*line)*x+(a.Z+dz*t-fx*line)*z;
                        minimum=Math.Min(minimum,centre-extent);maximum=Math.Max(maximum,centre+extent);
                    }
                }
            }
            if(minimum==double.MaxValue)throw new InvalidOperationException("Authoritative corridor did not cover the sweep.");
            low=minimum;high=maximum;
        }
        private static int FirstIntersectingEdge(PhysicalPassageCorridor corridor,double start)
        {
            // Include the incoming edge at an exact knot, as the legacy exhaustive scan
            // does. Only disjoint edges are skipped; projection arithmetic/order is identical.
            int low=1,high=corridor.Count-1;
            while(low<high){int middle=low+(high-low)/2;if(corridor.Get(middle).Distance<start)low=middle+1;else high=middle;}
            return low;
        }
        private static void Direction(double x,double z,out double fx,out double fz,out double length)
        {
            double scale=Math.Max(Math.Abs(x),Math.Abs(z));x/=scale;z/=scale;
            double unitLength=Math.Sqrt(x*x+z*z);fx=x/unitLength;fz=z/unitLength;length=scale*unitLength;
        }
    }
}
