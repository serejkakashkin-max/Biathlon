using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Conservative whole-interval world body envelopes for one explicitly bound start
    /// continuation. An axis separating complete envelopes is sufficient proof of no contact;
    /// failure to find an axis conservatively retains the conflict.</summary>
    public sealed class PhysicalStartContinuationMap
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalStartContinuationDefinition definition;
        private readonly double halfLength,halfWidth,radius,margin;
        private const double DoubleUnitRoundoff=2.2204460492503131e-16;
        public int JunctionIndex=>definition.JunctionIndex;
        public string SourceLoopId=>definition.Source.LoopId;
        public string MainLoopId=>definition.Main.LoopId;
        public long MainPrefixNanometers { get; }
        public double ProjectionMarginMeters=>margin;
        public string ContextJson { get; }
        public PhysicalStartContinuationMap(PhysicalTrackMap map,TrackDefinition track,RaceRoute route,
            StartApproachMap approach,PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,
            OccupancyDefinition body,PhysicalStartContinuationDefinition definition)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));this.definition=definition??throw new ArgumentNullException(nameof(definition));
            if(track==null||route==null||approach==null||junctions==null||connections==null||body==null)throw new ArgumentNullException();
            if(!map.IsBoundTo(track,route)||definition.TrackSnapshotJson!=map.TrackSnapshotJson||
                definition.JunctionSnapshotJson!=junctions.ToJson()||definition.ConnectionSnapshotJson!=connections.ToJson()||
                definition.OccupancySnapshotJson!=body.ToJson()||approach.LoopId!=SourceLoopId||route.GetSection(0).LoopId!=MainLoopId||
                map.MapRoute(0).LocalNanometers!=0||
                definition.ApproachSnapshotJson!=approach.DefinitionSnapshotJson||
                approach.TrackJson!=map.TrackSnapshotJson||approach.RoutePlanJson!=map.RoutePlanSnapshotJson||
                approach.RouteRequirementsJson!=map.RouteRequirementsSnapshotJson)
                throw new ArgumentException("Exact authoritative continuation context required.");
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            radius=Math.Sqrt(halfLength*halfLength+halfWidth*halfWidth);
            // Reuse the captured canonical world policy. This also bounds permitted rendering
            // disagreement and supplies an outward arithmetic margin for the separating proof.
            double maximumScale=0,maximumCoordinate=0;
            Reserve(definition.Source);Reserve(definition.Main);
            // A sliced receipt floors each endpoint by less than one logical nanometre.
            // Transform that reserve into world distance, then include outward IEEE arithmetic
            // error for the captured coordinates/body and query-specific lateral offsets.
            margin=Math.Max(definition.ValidationPolicy.SeamTolerance,maximumScale/SkiState.NanometersPerMeter+
                32*DoubleUnitRoundoff*(maximumCoordinate+radius+1));
            MainPrefixNanometers=checked((long)Math.Floor(definition.Main.Length*SkiState.NanometersPerMeter));
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{definition.ToJson(),definition.ValidationPolicy.ToJson(),
                definition.TrackSnapshotJson,definition.ApproachSnapshotJson,definition.JunctionSnapshotJson,
                definition.ConnectionSnapshotJson,definition.OccupancySnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson,
                "start_continuation_world_swept_rectangles_v1;whole_common_interval;all_knots_both_tangents;shortest_knot_rotation_corner_support_v2;ambiguous_half_turn_radial_fallback;incoming_tails;first_lap_only;terminal_and_other_ports_exclusive;whole_mm_knots;outward_scaled_nm_and_ieee_reserve_v1"});
            void Reserve(PhysicalStartCorridor corridor)
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
        public bool Contains(PhysicalTrackPoint point)
        {
            map.Validate(point);
            return point.LoopId==SourceLoopId||point.LoopId==MainLoopId&&point.LocalNanometers<=MainPrefixNanometers;
        }
        public bool Contains(PhysicalOccupancySweep sweep)
            =>Contains(sweep.Start)&&(!sweep.Start.Closed||sweep.Start.LocalNanometers+sweep.TravelNanometers<sweep.Start.LoopLengthNanometers)&&
                (sweep.Start.LoopId!=MainLoopId||sweep.Start.LocalNanometers+sweep.TravelNanometers<=MainPrefixNanometers);

        public bool Conflicts(PhysicalOccupancySweep a,PhysicalOccupancySweep b)
        {
            if(!Contains(a)||!Contains(b))throw new ArgumentException("Swept continuation proof cannot include another branch or wrapped main-terminal traffic.");
            if(a.AthleteId.Equals(b.AthleteId))throw new ArgumentException("Distinct continuation bodies required.");
            if(Separated(a,b,1,0)||Separated(a,b,0,1))return false;
            // Every available edge axis is useful; any missing separating axis only overblocks.
            // Axes from full captured corridors avoid allocations and include both sides of knots.
            if(SeparatedOnEdges(definition.Source,a,b)||SeparatedOnEdges(definition.Main,a,b))return false;
            return true;
        }
        private bool SeparatedOnEdges(PhysicalStartCorridor corridor,PhysicalOccupancySweep a,PhysicalOccupancySweep b)
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
        private void Bounds(PhysicalOccupancySweep sweep,double x,double z,out double low,out double high)
        {
            var corridor=sweep.Start.LoopId==SourceLoopId?definition.Source:definition.Main;
            double start=sweep.Start.LocalNanometers/(double)SkiState.NanometersPerMeter;
            double travel=sweep.TravelNanometers/(double)SkiState.NanometersPerMeter,end=start+travel;
            double minimum=double.MaxValue,maximum=double.MinValue;
            for(int i=1;i<corridor.Count;i++)
            {
                var a=corridor.Get(i-1);var b=corridor.Get(i);
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
                            double centre=(b.X-fz*line)*x+(b.Z+fx*line)*z;
                            double rotation=radius+2*Math.Abs(line);
                            minimum=Math.Min(minimum,centre-rotation);maximum=Math.Max(maximum,centre+rotation);return;
                        }
                        double origin=b.X*x+b.Z*z;
                        double forward=fx*x+fz*z,normal=-fz*x+fx*z;
                        double nextForward=nx*x+nz*z,nextNormal=-nz*x+nx*z;
                        for(int along=-1;along<=1;along+=2)for(int across=-1;across<=1;across+=2)
                        {
                            double longitudinal=along*halfLength,offset=line+across*halfWidth;
                            // Each corner projects as A*cos(theta)+B*sin(theta).
                            // Endpoints and any enclosed max/min critical angle are the
                            // complete support of the shortest signed rotation arc.
                            double a=longitudinal*forward+offset*normal;
                            double c=longitudinal*normal-offset*forward;
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
                        double centre=(a.X+dx*t-fz*line)*x+(a.Z+dz*t+fx*line)*z;
                        minimum=Math.Min(minimum,centre-extent);maximum=Math.Max(maximum,centre+extent);
                    }
                }
            }
            if(minimum==double.MaxValue)throw new InvalidOperationException("Authoritative corridor did not cover the sweep.");
            low=minimum;high=maximum;
        }
        private static void Direction(double x,double z,out double fx,out double fz,out double length)
        {
            double scale=Math.Max(Math.Abs(x),Math.Abs(z));x/=scale;z/=scale;
            double unitLength=Math.Sqrt(x*x+z*z);fx=x/unitLength;fz=z/unitLength;length=scale*unitLength;
        }
    }
}
