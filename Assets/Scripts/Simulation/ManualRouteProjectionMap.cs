using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Course observation, never an instruction to move or turn the athlete.</summary>
    public readonly struct ManualRouteProjection
    {
        public int SectionIndex { get; }
        public string LoopId { get; }
        public long RouteNanometers { get; }
        public double LocalMeters { get; }
        public double LateralMeters { get; }
        public double HeightMeters { get; }
        public double DistanceFromCentreMeters { get; }
        public double ForwardX { get; }
        public double ForwardZ { get; }
        public double GeometricGrade { get; }
        public double StationMetersPerPlanarMeter { get; }
        public bool IsValid => LoopId != null;
        internal ManualRouteProjection(int section, string loop, long route, double local, double lateral,
            double height, double distance, double fx, double fz, double grade, double stationScale)
        {
            SectionIndex=section;LoopId=loop;RouteNanometers=route;LocalMeters=local;LateralMeters=lateral;
            HeightMeters=height;DistanceFromCentreMeters=distance;ForwardX=fx;ForwardZ=fz;
            GeometricGrade=grade;StationMetersPerPlanarMeter=stationScale;
        }
        /// <summary>Geometric rise/run along actual world heading. Does not change the pose.</summary>
        public double GradeAlong(ManualSkiHeadingState pose)
        {
            if(!IsValid||!pose.IsValid)throw new ArgumentException("Initialized course observation and pose required.");
            return GeometricGrade*(ForwardX*Math.Sin(pose.HeadingRadians)+ForwardZ*Math.Cos(pose.HeadingRadians));
        }
    }

    /// <summary>A course-space observation for an AI controller. Sampling a target
    /// does not turn, move, admit a branch, or award route distance to an athlete.</summary>
    public readonly struct ManualRouteTarget
    {
        public bool IsValid => LoopId != null;
        public int SectionIndex { get; }
        public string LoopId { get; }
        public long RouteNanometers { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double ForwardX { get; }
        public double ForwardZ { get; }
        internal ManualRouteTarget(int section,string loop,long route,double x,double y,double z,double forwardX,double forwardZ)
        {SectionIndex=section;LoopId=loop;RouteNanometers=route;X=x;Y=y;Z=z;ForwardX=forwardX;ForwardZ=forwardZ;}
    }

    /// <summary>Four inward planes of one straight authored corridor cell. A
    /// transition to another cell needs separate topology admission.</summary>
    public readonly struct ManualRouteCorridorCell
    {
        public bool IsValid {get;}
        public int SectionIndex {get;}
        public double MinimumWidthMeters {get;}
        public SkiCourseEdgePlane Left {get;}
        public SkiCourseEdgePlane Right {get;}
        public SkiCourseEdgePlane Entry {get;}
        public SkiCourseEdgePlane Exit {get;}
        internal ManualRouteCorridorCell(int section,double width,SkiCourseEdgePlane left,SkiCourseEdgePlane right,
            SkiCourseEdgePlane entry,SkiCourseEdgePlane exit)
        {IsValid=true;SectionIndex=section;MinimumWidthMeters=width;Left=left;Right=right;Entry=entry;Exit=exit;}
    }

    internal readonly struct ManualRouteTubeWindow
    {
        internal readonly int FirstSection,LastSection;
        internal readonly double LowStationMeters,HighStationMeters,BodyRadiusMeters;
        internal ManualRouteTubeWindow(int first,int last,double low,double high,double bodyRadius)
        {FirstSection=first;LastSection=last;LowStationMeters=low;HighStationMeters=high;BodyRadiusMeters=bodyRadius;}
    }
    internal readonly struct ManualConnectedTubeWindow
    {
        internal readonly int First,Last,IncomingFirst,IncomingLast;
        internal ManualConnectedTubeWindow(int first,int last,int incomingFirst=-1,int incomingLast=-1)
        {First=first;Last=last;IncomingFirst=incomingFirst;IncomingLast=incomingLast;}
    }

    // A geometry namespace, never a competition-distance receipt. The catalog
    // keeps the mapping back to counted base distance outside this primitive.
    internal readonly struct ManualProjectionScope
    {
        internal readonly string LoopId;
        internal readonly long LocalStartNanometers,LocalEndNanometers,StationStartNanometers;
        internal ManualProjectionScope(string loop,long from,long to,long station)
        {LoopId=loop;LocalStartNanometers=from;LocalEndNanometers=to;StationStartNanometers=station;}
    }

    /// <summary>Local projection onto an explicitly selected base-race route occurrence.
    /// This geometry primitive does not award distance, choose a branch, admit a body,
    /// follow range/start/penalty paths, or connect the manual kernel to the live host.</summary>
    public sealed class ManualRouteProjectionMap
    {
        private readonly RaceRoute route;
        private readonly bool baseRouteGeometry;
        private readonly Edge[] edges;
        private readonly ManualRouteCorridorCell[] collinearCells;
        private readonly int[] firstEdges, edgeEnds;
        private int[] phaseIncomingEdges;
        private readonly long[] starts, ends;
        private readonly long length;
        public string ContextJson { get; private set; }
        internal double PortalPositionTolerance {get;private set;}
        internal bool UseRoundSnowCorridor {get;private set;}
        internal double SnowWindowMultiplier {get;private set;}=1;
        internal double OrdinaryPortalRetreat {get;private set;}
        internal double OrdinaryHandoffWindow {get;private set;}
        internal double MatPositionTolerance {get;set;}
        private bool[] matDepartures;
        private bool[] ordinaryStarts,ordinaryEnds;
        internal double OrdinaryPortalBand=>UseRoundSnowCorridor?Math.Max(PortalPositionTolerance,OrdinaryPortalRetreat):PortalPositionTolerance;
        internal bool IsOrdinaryEnd(int section)=>UseRoundSnowCorridor&&ordinaryEnds!=null&&ordinaryEnds[section];
        internal void PreserveStartPortal(int section)
        {
            if(section<0)return;
            ordinaryEnds[section]=false;
            if(section+1<ordinaryStarts.Length)ordinaryStarts[section+1]=false;
        }
        internal void BindPortalTolerance(double meters,bool roundSnowCorridor=false,double snowWindowMultiplier=1,double ordinaryPortalRetreat=0,double ordinaryHandoffWindow=0)
        {
            PhysiologyNumbers.RequireRange(meters,0,.01,nameof(meters));
            PortalPositionTolerance=meters;UseRoundSnowCorridor=roundSnowCorridor;SnowWindowMultiplier=snowWindowMultiplier;
            OrdinaryPortalRetreat=ordinaryPortalRetreat;
            OrdinaryHandoffWindow=ordinaryHandoffWindow;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{ContextJson,"owned_portal_band_m:"+meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture),"round_snow_corridor:"+roundSnowCorridor,"snow_window_multiplier:"+snowWindowMultiplier.ToString("R",System.Globalization.CultureInfo.InvariantCulture)});
        }
        public string CorridorContextJson { get; }
        private readonly struct Edge
        {
            internal readonly int Section;
            internal readonly string Loop;
            internal readonly double LocalStart,LocalEnd,StationStart,StationEnd,X,Z,Y,DX,DZ,DY,PlanarLength,MinimumWidth,TubeTurnReserve;
            internal readonly string TubeId;
            internal readonly ManualRouteCorridorCell Corridor;
            internal readonly double RibbonStartX,RibbonStartZ,RibbonEndX,RibbonEndZ;
            internal readonly double EndX,EndY,EndZ;
            internal readonly double EntryX,EntryZ,ExitX,ExitZ;
            internal readonly bool SharedPortal;
            internal Edge(int section,int ordinal,string loop,double localStart,double localEnd,double stationStart,
                TrackVisualPoint a,TrackVisualPoint b,double minimumWidth,double turnReserveFraction)
            {
                Section=section;Loop=loop;LocalStart=localStart;LocalEnd=localEnd;
                StationStart=stationStart;StationEnd=stationStart+localEnd-localStart;
                X=a.X;Y=a.Y;Z=a.Z;DX=b.X-a.X;DY=b.Y-a.Y;DZ=b.Z-a.Z;
                EndX=b.X;EndY=b.Y;EndZ=b.Z;
                PlanarLength=Math.Sqrt(DX*DX+DZ*DZ);
                MinimumWidth=minimumWidth;
                RibbonStartX=a.ForwardZ;RibbonStartZ=-a.ForwardX;
                RibbonEndX=b.ForwardZ;RibbonEndZ=-b.ForwardX;
                TubeTurnReserve=minimumWidth*.5*turnReserveFraction;
                if(!(PlanarLength>0))throw new ArgumentException("Projection edges need horizontal extent.");
                double fx=DX/PlanarLength,fz=DZ/PlanarLength,rx=fz,rz=-fx,half=minimumWidth*.5;
                EntryX=ExitX=fx;EntryZ=ExitZ=fz;SharedPortal=false;
                string id=section.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+
                    ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)+":";
                TubeId=id+"tube";
                Corridor=new ManualRouteCorridorCell(section,minimumWidth,
                    new SkiCourseEdgePlane(id+"left",X-rx*half,Z-rz*half,rx,rz),
                    new SkiCourseEdgePlane(id+"right",X+rx*half,Z+rz*half,-rx,-rz),
                    new SkiCourseEdgePlane(id+"entry",X,Z,fx,fz),
                    new SkiCourseEdgePlane(id+"exit",EndX,EndZ,-fx,-fz));
            }
            internal Edge(Edge source,double entryX,double entryZ,double exitX,double exitZ)
            {
                this=source;EntryX=entryX;EntryZ=entryZ;ExitX=exitX;ExitZ=exitZ;SharedPortal=true;
                Corridor=new ManualRouteCorridorCell(Section,MinimumWidth,source.Corridor.Left,source.Corridor.Right,
                    new SkiCourseEdgePlane(source.TubeId+":shared-entry",X,Z,entryX,entryZ),
                    new SkiCourseEdgePlane(source.TubeId+":shared-exit",EndX,EndZ,-exitX,-exitZ));
            }
            internal double ProjectionFraction(double x,double z)
            {
                if(!SharedPortal)return ((x-X)*DX+(z-Z)*DZ)/(PlanarLength*PlanarLength);
                double entry=((x-X)*EntryX+(z-Z)*EntryZ)/(DX*EntryX+DZ*EntryZ);
                double exit=((EndX-x)*ExitX+(EndZ-z)*ExitZ)/(DX*ExitX+DZ*ExitZ);
                // A folded chart has no unambiguous local station. Never
                // manufacture one by clamping a negative denominator.
                return entry+exit>0?entry/(entry+exit):double.NaN;
            }
        }

        public ManualRouteProjectionMap(RaceRoute route,TrackVisualDefinition geometry)
            :this(route,geometry,BaseScopes(route),true,null) { }

        internal static ManualRouteProjectionMap ForPhysicalScopes(RaceRoute route,TrackVisualDefinition geometry,
            ManualProjectionScope[] scopes,string scopeContext,bool[] ordinaryEntries,bool[] firingDepartures,
            int[] phaseEntries=null,int[] phaseExits=null)
            =>new ManualRouteProjectionMap(route,geometry,scopes,false,scopeContext??throw new ArgumentNullException(nameof(scopeContext)),ordinaryEntries,firingDepartures,phaseEntries,phaseExits);

        private static ManualProjectionScope[] BaseScopes(RaceRoute route)
        {
            if(route==null)throw new ArgumentNullException(nameof(route));
            var scopes=new ManualProjectionScope[route.SectionCount];
            for(int i=0;i<scopes.Length;i++)
            {
                var s=route.GetSection(i);scopes[i]=new ManualProjectionScope(s.LoopId,
                    checked(s.LoopStart.Millimeters*SkiState.NanometersPerMillimeter),
                    checked(s.LoopEnd.Millimeters*SkiState.NanometersPerMillimeter),
                    checked(s.Start.Millimeters*SkiState.NanometersPerMillimeter));
            }
            return scopes;
        }

        private ManualRouteProjectionMap(RaceRoute route,TrackVisualDefinition geometry,ManualProjectionScope[] scopes,
            bool baseRouteGeometry,string scopeContext,bool[] ordinaryEntries=null,bool[] firingDepartures=null,
            int[] phaseEntries=null,int[] phaseExits=null)
        {
            if(route==null||geometry==null||scopes==null)throw new ArgumentNullException();
            if(route.TrackId!=geometry.TrackId)throw new ArgumentException("Projection route and geometry must match.");
            if(scopes.Length==0)throw new ArgumentException("Positive physical occurrences required.");
            this.route=route;this.baseRouteGeometry=baseRouteGeometry;
            var track=TrackDefinition.FromJson(route.TrackSnapshotJson);
            int count=scopes.Length;firstEdges=new int[count];edgeEnds=new int[count];
            starts=new long[count];ends=new long[count];var list=new List<Edge>();
            for(int sectionIndex=0;sectionIndex<count;sectionIndex++)
            {
                var scope=scopes[sectionIndex];var loop=geometry.Get(scope.LoopId);
                var physical=track.GetLoop(scope.LoopId);
                if(scope.LocalStartNanometers<0||scope.LocalEndNanometers<=scope.LocalStartNanometers||
                    scope.LocalEndNanometers>checked(physical.Length.Millimeters*SkiState.NanometersPerMillimeter)||
                    scope.StationStartNanometers!=(sectionIndex==0?0:ends[sectionIndex-1]))
                    throw new ArgumentException("Contiguous geometry namespace and positive bounded physical slices required.");
                double localStart=scope.LocalStartNanometers/(double)SkiState.NanometersPerMeter;
                double localEnd=scope.LocalEndNanometers/(double)SkiState.NanometersPerMeter;
                double stationStart=scope.StationStartNanometers/(double)SkiState.NanometersPerMeter;
                double maximumTurn=0;
                for(int k=1;k+1<loop.Count;k++)
                {
                    var p=loop.Get(k-1);var q=loop.Get(k);var r=loop.Get(k+1);
                    double ax=q.X-p.X,az=q.Z-p.Z,bx=r.X-q.X,bz=r.Z-q.Z;
                    double angle=Math.Abs(Math.Atan2(ax*bz-az*bx,ax*bx+az*bz));
                    maximumTurn=Math.Max(maximumTurn,angle);
                }
                double turnReserveFraction=maximumTurn>=Math.PI*.5?1:Math.Min(1,Math.Tan(maximumTurn*.5));
                starts[sectionIndex]=scope.StationStartNanometers;
                ends[sectionIndex]=checked(scope.StationStartNanometers+scope.LocalEndNanometers-scope.LocalStartNanometers);
                firstEdges[sectionIndex]=list.Count;
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(localStart,loop.Get(k-1).Distance);
                    double to=Math.Min(localEnd,loop.Get(k).Distance);
                    if(to<=from)continue;
                    double minimumWidth=double.PositiveInfinity;
                    for(int p=0;p<physical.SegmentCount;p++)
                    {
                        var segment=physical.GetSegment(p);
                        double end=segment.StartDistance.Meters+segment.Length.Meters;
                        if(segment.StartDistance.Meters<to&&end>from)
                            minimumWidth=Math.Min(minimumWidth,segment.WidthMeters);
                    }
                    if(double.IsPositiveInfinity(minimumWidth))throw new ArgumentException("Visual edge lacks physical track width.");
                    list.Add(new Edge(sectionIndex,list.Count,loop.LoopId,from,to,stationStart+from-localStart,
                        loop.Sample(from),loop.Sample(to),minimumWidth,turnReserveFraction));
                }
                edgeEnds[sectionIndex]=list.Count;
                if(firstEdges[sectionIndex]==edgeEnds[sectionIndex])throw new ArgumentException("Empty projection occurrence.");
            }
            length=ends[count-1];edges=list.ToArray();
            if(ordinaryEntries!=null)
            {
                if(baseRouteGeometry||ordinaryEntries.Length!=count||firingDepartures==null||firingDepartures.Length!=count)
                    throw new ArgumentException("Exact physical-scope entry topology required.");
                matDepartures=(bool[])firingDepartures.Clone();
                ordinaryStarts=(bool[])ordinaryEntries.Clone();ordinaryEnds=new bool[count];
                for(int s=1;s<count;s++)if(ordinaryStarts[s])ordinaryEnds[s-1]=true;
                for(int s=1;s<count;s++)
                {
                    if(!ordinaryEntries[s]&&!firingDepartures[s])continue;
                    int previous=edgeEnds[s-1]-1,next=firstEdges[s];var a=edges[previous];var b=edges[next];
                    if(!Touching(a,b))throw new ArgumentException("An ordinary portal must join exactly touching authored endpoints.");
                    if(firingDepartures[s])
                    {
                        if(ordinaryEntries[s]||a.Loop!=b.Loop||b.DX*a.ExitX+b.DZ*a.ExitZ<=0)
                            throw new ArgumentException("Firing departure must enter the unchanged incoming plane on the same selected mat path.");
                        // Keep the closed firing endpoint exactly as paid.
                        // Its authorized departure reuses that plane; two
                        // adjacent polyline tangents must not create a wedge
                        // that rejects an unchanged off-centre shooting body.
                        edges[next]=new Edge(b,a.ExitX,a.ExitZ,b.ExitX,b.ExitZ);
                        continue;
                    }
                    double fx=a.ExitX+b.EntryX,fz=a.ExitZ+b.EntryZ,norm=Math.Sqrt(fx*fx+fz*fz);
                    if(norm<=PhysiologyNumbers.MinimumPositive)throw new ArgumentException("A reversing seam requires an explicit junction, not an ordinary chart.");
                    fx/=norm;fz/=norm;
                    edges[previous]=new Edge(a,a.EntryX,a.EntryZ,fx,fz);
                    edges[next]=new Edge(b,fx,fz,b.ExitX,b.ExitZ);
                }
            }
            if(phaseEntries!=null||phaseExits!=null)
            {
                if(baseRouteGeometry||phaseEntries==null||phaseExits==null||phaseEntries.Length!=count||phaseExits.Length!=count)
                    throw new ArgumentException("Exact owned phase portal topology required.");
                phaseIncomingEdges=new int[count];for(int s=0;s<count;s++)phaseIncomingEdges[s]=-1;
                for(int s=0;s<count;s++)
                {
                    int source=phaseEntries[s];
                    if(source>=0)
                    {
                        if(source>=count)throw new ArgumentException("Unknown phase portal source.");
                        int next=firstEdges[s];var a=edges[edgeEnds[source]-1];var b=edges[next];
                        if(!Touching(a,b)||b.DX*a.ExitX+b.DZ*a.ExitZ<=0)
                            throw new ArgumentException("Phase entry must reuse a touching forward incoming plane.");
                        edges[next]=new Edge(b,a.ExitX,a.ExitZ,b.ExitX,b.ExitZ);
                        phaseIncomingEdges[s]=edgeEnds[source]-1;
                    }
                    source=phaseExits[s];
                    if(source>=0)
                    {
                        if(source>=count)throw new ArgumentException("Unknown phase portal source.");
                        int last=edgeEnds[s]-1;var a=edges[edgeEnds[source]-1];var b=edges[last];
                        if(a.EndX!=b.EndX||a.EndY!=b.EndY||a.EndZ!=b.EndZ||b.DX*a.ExitX+b.DZ*a.ExitZ<=0)
                            throw new ArgumentException("Phase exit must reuse the same forward authored gate endpoint.");
                        edges[last]=new Edge(b,b.EntryX,b.EntryZ,a.ExitX,a.ExitZ);
                    }
                }
            }
            ContextJson=baseRouteGeometry?ShootingModelDefinitions.EncodeContext(new[]{"manual_base_route_local_projection_v1",
                route.TrackSnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson,geometry.ToJson(),
                "local_clamped_knot_observation_v2"}):ShootingModelDefinitions.EncodeContext(new[]{
                    "manual_physical_scope_local_projection_v2_context_reference","scope_context_sha256:"+ManualContextIdentity.Sha256(scopeContext),geometry.ToJson(),
                    "geometry_namespace_not_race_credit;local_clamped_knot_observation_v2;scoped_endpoint_encoding_snap_v1;ordinary_shared_bisector_portal_chart_v1;firing_departure_reuses_unchanged_paid_incoming_plane_v1;phase_authority_separate;folded_charts_reject"});
            collinearCells=BuildCollinearCells();
            CorridorContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_selected_route_convex_cell_v1",
                "projection_context_sha256:"+ManualContextIdentity.Sha256(ContextJson),"minimum_authored_width_over_visual_edge;four_inward_planes;full_body_interior_only"});
        }

        internal bool IsBoundTo(RaceRoute selectedRoute)=>baseRouteGeometry&&ReferenceEquals(route,selectedRoute);

        /// <summary>Search only the current occurrence and, if explicitly admitted, its
        /// immediate successor. The supplied station window must come from the movement
        /// solver, not a globally nearest-point search. Failure grants no movement.
        /// The returned station can move backward; the caller must not count that as
        /// positive odometer travel or automatically snap the world pose to the centre.</summary>
        public bool TryProject(ManualSkiHeadingState pose,int currentSection,long previousStationNanometers,
            double maximumStationDisplacementMeters,bool admitNextSection,out ManualRouteProjection result)
        {
            if(!pose.IsValid)throw new ArgumentException("Initialized world pose required.");
            if(currentSection<0||currentSection>=starts.Length)throw new ArgumentOutOfRangeException(nameof(currentSection));
            if(previousStationNanometers<starts[currentSection]||previousStationNanometers>ends[currentSection])
                throw new ArgumentOutOfRangeException(nameof(previousStationNanometers));
            PhysiologyNumbers.RequireRange(maximumStationDisplacementMeters,0,PhysiologyNumbers.MaximumMagnitude,nameof(maximumStationDisplacementMeters));
            long span=checked((long)Math.Round(maximumStationDisplacementMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
            // An authorized departure can start anywhere inside its planted
            // entry band. Observe that already paid position continuously on
            // the next moving piece instead of holding station zero and then
            // rejecting the first piece outside the band as a distance jump.
            if(!baseRouteGeometry&&span>0&&previousStationNanometers==starts[currentSection])
            {
                double entryBand=matDepartures!=null&&matDepartures[currentSection]?MatPositionTolerance:
                    ordinaryStarts!=null&&ordinaryStarts[currentSection]?Math.Max(OrdinaryPortalBand,OrdinaryHandoffWindow):0;
                span=checked(span+(long)Math.Round(entryBand*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
            }
            long low=Math.Max(starts[currentSection],previousStationNanometers-span);
            long high=Math.Min(length,checked(previousStationNanometers+span));
            int last=admitNextSection?Math.Min(currentSection+1,starts.Length-1):currentSection;
            double lower=low/(double)SkiState.NanometersPerMeter,upper=high/(double)SkiState.NanometersPerMeter;
            double seekReserve=baseRouteGeometry?0:2*PhysiologyNumbers.MinimumPositive;
            double best=double.PositiveInfinity;result=default;
            for(int section=currentSection;section<=last;section++)
            {
                int begin=firstEdges[section],end=edgeEnds[section];
                // Binary seek preserves warm query cost on dense authored geometry.
                int a=begin,b=end;
                while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<lower-seekReserve)a=m+1;else b=m;}
                for(int i=a;i<end;i++)
                {
                    var edge=edges[i];if(edge.StationStart>upper+seekReserve)break;
                    double fraction=edge.ProjectionFraction(pose.X,pose.Z);
                    if(double.IsNaN(fraction))
                    {
                        if(!UseRoundSnowCorridor)continue;
                        // A ski body can occupy the outside of a tight bend.
                        // Observe its nearest local authored edge without moving
                        // it or selecting a different route occurrence.
                        fraction=((pose.X-edge.X)*edge.DX+(pose.Z-edge.Z)*edge.DZ)/(edge.PlanarLength*edge.PlanarLength);
                    }
                    // World pose is rounded to nanometres while authored knot
                    // coordinates are doubles. Reserve two encoding units at an
                    // endpoint; this is numerical tolerance, not course-edge tuning.
                    double endpointReserve=Math.Max(2*PhysiologyNumbers.MinimumPositive,PortalPositionTolerance)/edge.PlanarLength;
                    double entryReserve=i==begin?endpointReserve:2*PhysiologyNumbers.MinimumPositive/edge.PlanarLength;
                    double exitReserve=i==end-1?endpointReserve:2*PhysiologyNumbers.MinimumPositive/edge.PlanarLength;
                    if(UseRoundSnowCorridor&&i==begin&&ordinaryStarts!=null&&ordinaryStarts[section])
                        entryReserve=Math.Max(entryReserve,Math.Max(OrdinaryPortalBand,Math.Min(OrdinaryHandoffWindow,edge.MinimumWidth*.5))/edge.PlanarLength);
                    if(UseRoundSnowCorridor&&i==end-1&&ordinaryEnds!=null&&ordinaryEnds[section])
                        exitReserve=Math.Max(exitReserve,OrdinaryPortalBand/edge.PlanarLength);
                    if(MatPositionTolerance>0&&i==begin&&matDepartures!=null&&matDepartures[section])
                        entryReserve=Math.Max(entryReserve,MatPositionTolerance/edge.PlanarLength);
                    // A rounded world path can lie in the exterior Voronoi wedge
                    // of a sharp authored knot. Its nearest point is the knot,
                    // not an unbounded projection onto either adjacent segment.
                    // Limit this endpoint fallback to the physical corridor so a
                    // distant pose never snaps to a route occurrence.
                    bool beyondEndpoint=fraction< -entryReserve||fraction>1+exitReserve;
                    fraction=Math.Max(0,Math.Min(1,fraction));
                    // A scoped seam has an exact physical-arc coordinate. A
                    // quantized pose within the existing two-nanometre endpoint
                    // reserve observes that endpoint; its world pose is unchanged.
                    // Keep the existing base-route observation contract intact.
                    if(!baseRouteGeometry)
                    {
                        // The wider ordinary band admits an already reached
                        // seam; it must not award progress before the centre
                        // reaches that seam. Only mat departure reuses its
                        // separately authorized planted-position band.
                        double entryBand=MatPositionTolerance>0&&i==begin&&matDepartures!=null&&matDepartures[section]
                            ?MatPositionTolerance:i==begin&&ordinaryStarts!=null&&ordinaryStarts[section]?Math.Max(OrdinaryPortalBand,Math.Min(OrdinaryHandoffWindow,edge.MinimumWidth*.5)):PortalPositionTolerance;
                        double entryOffset=(pose.X-edge.X)*edge.EntryX+(pose.Z-edge.Z)*edge.EntryZ;
                        bool plantedDeparture=i==begin&&(MatPositionTolerance>0&&matDepartures!=null&&matDepartures[section]||
                            UseRoundSnowCorridor&&ordinaryStarts!=null&&ordinaryStarts[section]);
                        bool atEntry=fraction<=endpointReserve||i==begin&&Math.Abs(entryOffset)<=entryBand&&
                            (!plantedDeparture||maximumStationDisplacementMeters==0&&previousStationNanometers==starts[section]||entryOffset<=0);
                        bool atExit=1-fraction<=endpointReserve;
                        if(atEntry&&i==begin&&maximumStationDisplacementMeters==0&&previousStationNanometers==starts[section])fraction=0;
                        else if(atEntry||atExit)fraction=atEntry&&atExit?(fraction<=.5?0:1):atEntry?0:1;
                    }
                    double station=edge.StationStart+fraction*(edge.StationEnd-edge.StationStart);
                    long encoded=checked((long)Math.Round(station*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
                    if(!baseRouteGeometry&&i==begin&&fraction==0)encoded=starts[section];
                    if(!baseRouteGeometry&&i==end-1&&fraction==1)encoded=ends[section];
                    if(encoded<low||encoded>high)continue;
                    double dx=pose.X-edge.X-edge.DX*fraction,dz=pose.Z-edge.Z-edge.DZ*fraction;
                    double distanceSquared=dx*dx+dz*dz;
                    if(!UseRoundSnowCorridor&&beyondEndpoint&&distanceSquared>edge.MinimumWidth*edge.MinimumWidth*.25)continue;
                    // Stable ties keep the current edge/occurrence. Branch admission is
                    // explicit and may never advance a lap merely because coordinates repeat.
                    if(distanceSquared>=best)continue;
                    best=distanceSquared;double fx=edge.DX/edge.PlanarLength,fz=edge.DZ/edge.PlanarLength;
                    result=new ManualRouteProjection(edge.Section,edge.Loop,encoded,
                        edge.LocalStart+fraction*(edge.LocalEnd-edge.LocalStart),dx*fz-dz*fx,
                        edge.Y+edge.DY*fraction,Math.Sqrt(distanceSquared),fx,fz,edge.DY/edge.PlanarLength,
                        (edge.StationEnd-edge.StationStart)/edge.PlanarLength);
                }
            }
            return result.IsValid;
        }

        // Portal normals belong to station charts. AI targets and surface
        // tangents still use the actual route edge, never this transverse plane.
        internal bool TrySamplePortal(int section,long station,out ManualRouteTarget target)
        {
            if(!TrySampleAhead(section,station,0,false,out target))return false;
            if(station!=starts[section]&&station!=ends[section])return true;
            var edge=edges[station==starts[section]?firstEdges[section]:edgeEnds[section]-1];
            bool entry=station==starts[section];
            target=new ManualRouteTarget(section,target.LoopId,station,target.X,target.Y,target.Z,
                entry?edge.EntryX:edge.ExitX,entry?edge.EntryZ:edge.ExitZ);return true;
        }

        /// <summary>Sample a bounded forward target on the current route occurrence.
        /// A successor may be sampled only when its admission is explicitly supplied;
        /// the query never skips another occurrence or chooses a spatially close lap.</summary>
        public bool TrySampleAhead(int currentSection,long currentStationNanometers,double lookaheadMeters,
            bool admitNextSection,out ManualRouteTarget target)
        {
            if(currentSection<0||currentSection>=starts.Length)throw new ArgumentOutOfRangeException(nameof(currentSection));
            if(currentStationNanometers<starts[currentSection]||currentStationNanometers>ends[currentSection])
                throw new ArgumentOutOfRangeException(nameof(currentStationNanometers));
            PhysiologyNumbers.RequireRange(lookaheadMeters,0,PhysiologyNumbers.MaximumMagnitude,nameof(lookaheadMeters));
            long ahead=checked((long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
            long station=checked(currentStationNanometers+ahead);
            int section=currentSection;
            if(station>ends[section])
            {
                if(!admitNextSection||section+1>=starts.Length||station>ends[section+1])
                {target=default;return false;}
                section++;
            }
            double metres=station/(double)SkiState.NanometersPerMeter;
            int a=firstEdges[section],b=edgeEnds[section];
            double seekReserve=baseRouteGeometry?0:2*PhysiologyNumbers.MinimumPositive;
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<metres-seekReserve)a=m+1;else b=m;}
            if(a==edgeEnds[section]){target=default;return false;}
            var edge=edges[a];
            // Nanometre state encoding and authored doubles may differ at an endpoint.
            // This reserve is numerical only and cannot extend the selected section.
            if(metres<edge.StationStart-2*PhysiologyNumbers.MinimumPositive||
                metres>edge.StationEnd+2*PhysiologyNumbers.MinimumPositive)
            {target=default;return false;}
            double fraction=Math.Max(0,Math.Min(1,(metres-edge.StationStart)/(edge.StationEnd-edge.StationStart)));
            target=new ManualRouteTarget(section,edge.Loop,station,edge.X+edge.DX*fraction,
                edge.Y+edge.DY*fraction,edge.Z+edge.DZ*fraction,
                edge.DX/edge.PlanarLength,edge.DZ/edge.PlanarLength);
            return true;
        }

        /// <summary>Admit only a body wholly inside one straight visual/physical
        /// cell. Its four planes can be passed to the joint continuous sweep; a
        /// failed query means the caller must handle a seam or unsafe start, not
        /// skip edge checking. No route progress or transition is awarded.</summary>
        public bool TryGetInteriorCorridor(ManualRouteProjection observation,OccupancyDefinition body,
            out ManualRouteCorridorCell cell)
        {
            if(!observation.IsValid||body==null)throw new ArgumentException("Initialized route observation and occupied body required.");
            int section=observation.SectionIndex;
            if(section<0||section>=starts.Length||observation.LoopId!=edges[firstEdges[section]].Loop||
                observation.RouteNanometers<starts[section]||observation.RouteNanometers>ends[section])
                throw new ArgumentException("Projection does not belong to this route occurrence.");
            double metres=observation.RouteNanometers/(double)SkiState.NanometersPerMeter;
            int a=firstEdges[section],b=edgeEnds[section];
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<metres)a=m+1;else b=m;}
            cell=default;
            if(a==edgeEnds[section])return false;
            var edge=edges[a];
            if(metres<edge.StationStart||metres>edge.StationEnd)return false;
            double fraction=(metres-edge.StationStart)/(edge.StationEnd-edge.StationStart);
            double radius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+
                body.LateralSeparation*body.LateralSeparation);
            double guard=radius+body.EdgeClearance;
            if(fraction*edge.PlanarLength<=guard||(1-fraction)*edge.PlanarLength<=guard||
                Math.Abs(observation.LateralMeters)+guard>=edge.MinimumWidth*.5)return false;
            cell=edge.Corridor;
            return true;
        }

        // A union of exactly touching, collinear authored rectangles contains
        // the narrower rectangle spanning their full length. Its four inward
        // planes therefore prove actual oriented-body occupancy across internal
        // sampling seams. This does not merge any route/phase coordinate.
        private ManualRouteCorridorCell[] BuildCollinearCells()
        {
            var result=new ManualRouteCorridorCell[edges.Length];int first=0;
            while(first<edges.Length)
            {
                int last=first;double width=edges[first].MinimumWidth;
                while(last+1<edges.Length&&TouchingCollinear(edges[last],edges[last+1]))
                {last++;width=Math.Min(width,edges[last].MinimumWidth);}
                var a=edges[first];var b=edges[last];
                double fx=a.DX/a.PlanarLength,fz=a.DZ/a.PlanarLength,half=width*.5;
                string id="collinear:"+first.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+
                    last.ToString(System.Globalization.CultureInfo.InvariantCulture)+":";
                for(int i=first;i<=last;i++)result[i]=new ManualRouteCorridorCell(edges[i].Section,width,
                    new SkiCourseEdgePlane(id+"left",a.X-fz*half,a.Z+fx*half,fz,-fx),
                    new SkiCourseEdgePlane(id+"right",a.X+fz*half,a.Z-fx*half,-fz,fx),
                    new SkiCourseEdgePlane(id+"entry",a.X,a.Z,fx,fz),
                    new SkiCourseEdgePlane(id+"exit",b.EndX,b.EndZ,-fx,-fz));
                first=last+1;
            }
            return result;
        }
        private static bool TouchingCollinear(Edge a,Edge b)
            =>a.EndX==b.X&&a.EndZ==b.Z&&a.EndY==b.Y&&
                a.DX*b.DZ-a.DZ*b.DX==0&&a.DX*b.DX+a.DZ*b.DZ>0;

        /// <summary>Only an actual source edge's exact collinear cell union.
        /// Callers must continuously sweep the complete body against all four
        /// returned planes. This query grants no boundary or branch handoff.</summary>
        public bool TryGetCollinearCorridor(ManualRouteProjection observation,out ManualRouteCorridorCell cell)
        {
            if(!observation.IsValid)throw new ArgumentException("Initialized owned route observation required.");
            int section=observation.SectionIndex;
            if(section<0||section>=starts.Length||observation.LoopId!=edges[firstEdges[section]].Loop||
                observation.RouteNanometers<starts[section]||observation.RouteNanometers>ends[section])
                throw new ArgumentException("Projection does not belong to this route occurrence.");
            double station=observation.RouteNanometers/(double)SkiState.NanometersPerMeter;
            int a=firstEdges[section],b=edgeEnds[section];
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<station)a=m+1;else b=m;}
            cell=default;
            if(a>=edgeEnds[section]||station<edges[a].StationStart||station>edges[a].StationEnd)return false;
            cell=collinearCells[a];return true;
        }

        internal int MaximumRibbonVertices=>checked(edges.Length*2+2);
        // Local, exactly connected geometry only. The polygon uses the same
        // authored knot normals as TrackRibbonBuilder, with the minimum physical
        // width over its window. It is an inscribed exclusion domain, never a
        // route/phase admission or a global nearest-path query.
        internal int BuildLocalRibbon(ManualRouteProjection source,double reach,double[] x,double[] z)
        {
            if(!TryBuildConnectedWindow(source,reach,out var window))return 0;
            int first=window.First,last=window.Last;
            int nodes=last-first+2,n=checked(nodes*2);if(x.Length<n||z.Length<n)throw new ArgumentException("Cold ribbon buffers required.");
            for(int k=0;k<nodes;k++)
            {
                var edge=edges[k==nodes-1?last:first+k];
                // Preserve each authored section width. A future narrow mat
                // must not retroactively shrink the common arrival aisle.
                double half=edge.MinimumWidth*.5;
                double px=k==nodes-1?edge.EndX:edge.X,pz=k==nodes-1?edge.EndZ:edge.Z;
                double rx=k==nodes-1?edge.RibbonEndX:edge.RibbonStartX,rz=k==nodes-1?edge.RibbonEndZ:edge.RibbonStartZ;
                x[k]=px-rx*half;z[k]=pz-rz*half;
                x[n-1-k]=px+rx*half;z[n-1-k]=pz+rz*half;
            }
            return n;
        }
        internal bool TryBuildConnectedWindow(ManualRouteProjection source,double reach,out ManualConnectedTubeWindow window)
        {
            window=default;
            int section=source.SectionIndex,a=firstEdges[section],b=edgeEnds[section];
            double station=source.RouteNanometers/(double)SkiState.NanometersPerMeter;
            // The cursor is encoded in integer nanometers; authored edge stations
            // are doubles. Keep an encoded knot attached to its connected snow.
            double reserve=2*PhysiologyNumbers.MinimumPositive;
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<station-reserve)a=m+1;else b=m;}
            if(a>=edgeEnds[section]||station<edges[a].StationStart-reserve||station>edges[a].StationEnd+reserve)return false;
            int first=a,last=a;double before=0,after=0;
            while(first>0&&before<reach&&Touching(edges[first-1],edges[first]))
            {first--;before+=edges[first].PlanarLength;}
            while(last+1<edges.Length&&after<reach&&Touching(edges[last],edges[last+1]))
            {last++;after+=edges[last].PlanarLength;}
            int incomingFirst=-1,incomingLast=-1;
            // An owned penalty/finish transition retains the actual incoming
            // snow under the rear of the body. Catalog order is not path order:
            // alternative penalty occurrences are stored after the base flow.
            if(UseRoundSnowCorridor&&phaseIncomingEdges!=null&&phaseIncomingEdges[section]>=0&&
                station-starts[section]/(double)SkiState.NanometersPerMeter<reach)
            {
                incomingFirst=incomingLast=phaseIncomingEdges[section];double covered=edges[incomingFirst].PlanarLength;
                while(incomingFirst>0&&covered<reach&&Touching(edges[incomingFirst-1],edges[incomingFirst]))
                {incomingFirst--;covered+=edges[incomingFirst].PlanarLength;}
            }
            window=new ManualConnectedTubeWindow(first,last,incomingFirst,incomingLast);return true;
        }
        // Every disk must fit in at least one of the pre-existing conservative
        // edge tubes. Their unchanged turn reserve and actual width are retained;
        // no convex hull, bridging of disconnected paths or enlarged snow domain.
        internal double ConnectedTubeDiskClearance(double x,double z,ManualConnectedTubeWindow window,double radius)
        {
            double result=double.NegativeInfinity;
            for(int i=window.First;i<=window.Last;i++)
            {
                result=Math.Max(result,Clearance(i));
            }
            if(window.IncomingFirst>=0)for(int i=window.IncomingFirst;i<=window.IncomingLast;i++)result=Math.Max(result,Clearance(i));
            return result;
            double Clearance(int i)
            {
                var edge=edges[i];double cap=edge.MinimumWidth*.5-(UseRoundSnowCorridor?0:edge.TubeTurnReserve)-radius;
                double needed=cap-result;
                if(needed<=0)return double.NegativeInfinity;
                double bx=Math.Max(0,Math.Max(Math.Min(edge.X,edge.EndX)-x,x-Math.Max(edge.X,edge.EndX)));
                double bz=Math.Max(0,Math.Max(Math.Min(edge.Z,edge.EndZ)-z,z-Math.Max(edge.Z,edge.EndZ)));
                double box=bx*bx+bz*bz,limit=needed*needed;
                if(box>limit+64*2.2204460492503131e-16*(1+box+limit))return double.NegativeInfinity;
                double t=Math.Max(0,Math.Min(1,((x-edge.X)*edge.DX+(z-edge.Z)*edge.DZ)/(edge.PlanarLength*edge.PlanarLength)));
                double dx=x-edge.X-t*edge.DX,dz=z-edge.Z-t*edge.DZ;
                return cap-Math.Sqrt(dx*dx+dz*dz);
            }
        }
        // A capsule is convex. If every rectangle corner fits the same owned
        // edge capsule, the complete rectangle and its clearance fit as well.
        // Do not combine corners from different capsules across a folded path.
        internal double ConnectedTubeRectangleClearance(ManualSkiHeadingState pose,ManualConnectedTubeWindow window,
            double halfLength,double halfWidth,double clearance)
        {
            double result=double.NegativeInfinity,fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            for(int i=window.First;i<=window.Last;i++)result=Math.Max(result,At(i));
            if(window.IncomingFirst>=0)for(int i=window.IncomingFirst;i<=window.IncomingLast;i++)result=Math.Max(result,At(i));
            return result;
            double At(int i)
            {
                var edge=edges[i];double maximumSquared=0;
                for(int corner=0;corner<4;corner++)
                {
                    double along=corner<2?halfLength:-halfLength,side=corner==0||corner==3?halfWidth:-halfWidth;
                    double px=pose.X+fx*along+fz*side,pz=pose.Z+fz*along-fx*side;
                    double t=Math.Max(0,Math.Min(1,((px-edge.X)*edge.DX+(pz-edge.Z)*edge.DZ)/(edge.PlanarLength*edge.PlanarLength)));
                    double dx=px-edge.X-t*edge.DX,dz=pz-edge.Z-t*edge.DZ;
                    maximumSquared=Math.Max(maximumSquared,dx*dx+dz*dz);
                }
                return edge.MinimumWidth*.5-(UseRoundSnowCorridor?0:edge.TubeTurnReserve)-clearance-Math.Sqrt(maximumSquared);
            }
        }
        private bool Touching(Edge a,Edge b)=>a.EndX==b.X&&a.EndZ==b.Z&&a.EndY==b.Y||
            PortalPositionTolerance>0&&Math.Abs(a.EndX-b.X)<=PortalPositionTolerance&&
            Math.Abs(a.EndZ-b.Z)<=PortalPositionTolerance&&Math.Abs(a.EndY-b.Y)<=PortalPositionTolerance;

        internal ManualRouteTubeWindow BuildTubeWindow(int currentSection,long currentStationNanometers,
            double maximumStationDisplacementMeters,bool admitNextSection,OccupancyDefinition body)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(currentSection<0||currentSection>=starts.Length)throw new ArgumentOutOfRangeException(nameof(currentSection));
            if(currentStationNanometers<starts[currentSection]||currentStationNanometers>ends[currentSection])
                throw new ArgumentOutOfRangeException(nameof(currentStationNanometers));
            PhysiologyNumbers.RequireRange(maximumStationDisplacementMeters,0,PhysiologyNumbers.MaximumMagnitude,
                nameof(maximumStationDisplacementMeters));
            long span=checked((long)Math.Round(maximumStationDisplacementMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
            long low=Math.Max(starts[currentSection],checked(currentStationNanometers-span));
            long high=Math.Min(length,checked(currentStationNanometers+span));
            double radius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+
                body.LateralSeparation*body.LateralSeparation)+body.EdgeClearance;
            return new ManualRouteTubeWindow(currentSection,
                admitNextSection?Math.Min(currentSection+1,starts.Length-1):currentSection,
                low/(double)SkiState.NanometersPerMeter,high/(double)SkiState.NanometersPerMeter,radius);
        }

        internal double TubeClearanceAt(double x,double z,ManualRouteTubeWindow window,out string sourceEdgeId)
        {
            double best=double.NegativeInfinity;sourceEdgeId=null;
            double seekReserve=baseRouteGeometry?0:2*PhysiologyNumbers.MinimumPositive;
            for(int section=window.FirstSection;section<=window.LastSection;section++)
            {
                int a=firstEdges[section],b=edgeEnds[section];
                while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<window.LowStationMeters-seekReserve)a=m+1;else b=m;}
                for(int i=a;i<edgeEnds[section];i++)
                {
                    var edge=edges[i];if(edge.StationStart>window.HighStationMeters+seekReserve)break;
                    double fraction=Math.Max(0,Math.Min(1,((x-edge.X)*edge.DX+(z-edge.Z)*edge.DZ)/
                        (edge.PlanarLength*edge.PlanarLength)));
                    double dx=x-edge.X-fraction*edge.DX,dz=z-edge.Z-fraction*edge.DZ;
                    double allowance=edge.MinimumWidth*.5-window.BodyRadiusMeters-edge.TubeTurnReserve;
                    double slack=allowance-Math.Sqrt(dx*dx+dz*dz);
                    if(slack>best){best=slack;sourceEdgeId=edge.TubeId;}
                }
            }
            return best;
        }

        // A tube window includes whole visual edges, including their round caps.
        // A portal inside one of those edges must be checked even if its station
        // lies beyond the query's narrower observation window.
        internal bool WindowTouchesPortalEdge(int section,long stationNanometers,long previousNanometers,double spanMeters)
        {
            double station=stationNanometers/(double)SkiState.NanometersPerMeter;
            double previous=previousNanometers/(double)SkiState.NanometersPerMeter;
            double reserve=2*PhysiologyNumbers.MinimumPositive;
            int a=firstEdges[section],b=edgeEnds[section];
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<station-reserve)a=m+1;else b=m;}
            if(a>=edgeEnds[section])return false;
            var edge=edges[a];
            return edge.StationStart<=previous+spanMeters+reserve&&edge.StationEnd>=previous-spanMeters-reserve;
        }
    }
}
