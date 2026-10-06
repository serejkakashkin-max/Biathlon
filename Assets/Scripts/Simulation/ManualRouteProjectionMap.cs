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

    /// <summary>Local projection onto an explicitly selected base-race route occurrence.
    /// This geometry primitive does not award distance, choose a branch, admit a body,
    /// follow range/start/penalty paths, or connect the manual kernel to the live host.</summary>
    public sealed class ManualRouteProjectionMap
    {
        private readonly Edge[] edges;
        private readonly int[] firstEdges, edgeEnds;
        private readonly long[] starts, ends;
        private readonly long length;
        public string ContextJson { get; }
        public string CorridorContextJson { get; }
        private readonly struct Edge
        {
            internal readonly int Section;
            internal readonly string Loop;
            internal readonly double LocalStart,LocalEnd,StationStart,StationEnd,X,Z,Y,DX,DZ,DY,PlanarLength,MinimumWidth,TubeTurnReserve;
            internal readonly string TubeId;
            internal readonly ManualRouteCorridorCell Corridor;
            internal Edge(int section,int ordinal,string loop,double localStart,double localEnd,double stationStart,
                TrackVisualPoint a,TrackVisualPoint b,double minimumWidth,double turnReserveFraction)
            {
                Section=section;Loop=loop;LocalStart=localStart;LocalEnd=localEnd;
                StationStart=stationStart;StationEnd=stationStart+localEnd-localStart;
                X=a.X;Y=a.Y;Z=a.Z;DX=b.X-a.X;DY=b.Y-a.Y;DZ=b.Z-a.Z;
                PlanarLength=Math.Sqrt(DX*DX+DZ*DZ);
                MinimumWidth=minimumWidth;
                TubeTurnReserve=minimumWidth*.5*turnReserveFraction;
                if(!(PlanarLength>0))throw new ArgumentException("Projection edges need horizontal extent.");
                double fx=DX/PlanarLength,fz=DZ/PlanarLength,rx=fz,rz=-fx,half=minimumWidth*.5;
                string id=section.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+
                    ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)+":";
                TubeId=id+"tube";
                Corridor=new ManualRouteCorridorCell(section,minimumWidth,
                    new SkiCourseEdgePlane(id+"left",X-rx*half,Z-rz*half,rx,rz),
                    new SkiCourseEdgePlane(id+"right",X+rx*half,Z+rz*half,-rx,-rz),
                    new SkiCourseEdgePlane(id+"entry",X,Z,fx,fz),
                    new SkiCourseEdgePlane(id+"exit",X+DX,Z+DZ,-fx,-fz));
            }
        }

        public ManualRouteProjectionMap(RaceRoute route,TrackVisualDefinition geometry)
        {
            if(route==null||geometry==null)throw new ArgumentNullException();
            if(route.TrackId!=geometry.TrackId)throw new ArgumentException("Projection route and geometry must match.");
            var track=TrackDefinition.FromJson(route.TrackSnapshotJson);
            int count=route.SectionCount;firstEdges=new int[count];edgeEnds=new int[count];
            starts=new long[count];ends=new long[count];var list=new List<Edge>();
            for(int sectionIndex=0;sectionIndex<count;sectionIndex++)
            {
                var section=route.GetSection(sectionIndex);var loop=geometry.Get(section.LoopId);
                var physical=track.GetLoop(section.LoopId);
                double maximumTurn=0;
                for(int k=1;k+1<loop.Count;k++)
                {
                    var p=loop.Get(k-1);var q=loop.Get(k);var r=loop.Get(k+1);
                    double ax=q.X-p.X,az=q.Z-p.Z,bx=r.X-q.X,bz=r.Z-q.Z;
                    double angle=Math.Abs(Math.Atan2(ax*bz-az*bx,ax*bx+az*bz));
                    maximumTurn=Math.Max(maximumTurn,angle);
                }
                double turnReserveFraction=maximumTurn>=Math.PI*.5?1:Math.Min(1,Math.Tan(maximumTurn*.5));
                starts[sectionIndex]=checked(section.Start.Millimeters*SkiState.NanometersPerMillimeter);
                ends[sectionIndex]=checked((section.Start.Millimeters+section.Length.Millimeters)*SkiState.NanometersPerMillimeter);
                firstEdges[sectionIndex]=list.Count;
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(section.LoopStart.Meters,loop.Get(k-1).Distance);
                    double to=Math.Min(section.LoopEnd.Meters,loop.Get(k).Distance);
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
                    list.Add(new Edge(sectionIndex,list.Count,loop.LoopId,from,to,section.Start.Meters+from-section.LoopStart.Meters,
                        loop.Sample(from),loop.Sample(to),minimumWidth,turnReserveFraction));
                }
                edgeEnds[sectionIndex]=list.Count;
                if(firstEdges[sectionIndex]==edgeEnds[sectionIndex])throw new ArgumentException("Empty projection occurrence.");
            }
            length=ends[count-1];edges=list.ToArray();
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_base_route_local_projection_v1",
                route.TrackSnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson,geometry.ToJson()});
            CorridorContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_selected_route_convex_cell_v1",
                ContextJson,"minimum_authored_width_over_visual_edge;four_inward_planes;full_body_interior_only"});
        }

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
            long low=Math.Max(starts[currentSection],previousStationNanometers-span);
            long high=Math.Min(length,checked(previousStationNanometers+span));
            int last=admitNextSection?Math.Min(currentSection+1,starts.Length-1):currentSection;
            double lower=low/(double)SkiState.NanometersPerMeter,upper=high/(double)SkiState.NanometersPerMeter;
            double best=double.PositiveInfinity;result=default;
            for(int section=currentSection;section<=last;section++)
            {
                int begin=firstEdges[section],end=edgeEnds[section];
                // Binary seek preserves warm query cost on dense authored geometry.
                int a=begin,b=end;
                while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<lower)a=m+1;else b=m;}
                for(int i=a;i<end;i++)
                {
                    var edge=edges[i];if(edge.StationStart>upper)break;
                    double fraction=((pose.X-edge.X)*edge.DX+(pose.Z-edge.Z)*edge.DZ)/(edge.PlanarLength*edge.PlanarLength);
                    // World pose is rounded to nanometres while authored knot
                    // coordinates are doubles. Reserve two encoding units at an
                    // endpoint; this is numerical tolerance, not course-edge tuning.
                    double endpointReserve=2*PhysiologyNumbers.MinimumPositive/edge.PlanarLength;
                    if(fraction< -endpointReserve||fraction>1+endpointReserve)continue;
                    fraction=Math.Max(0,Math.Min(1,fraction));
                    double station=edge.StationStart+fraction*(edge.StationEnd-edge.StationStart);
                    long encoded=checked((long)Math.Round(station*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
                    if(encoded<low||encoded>high)continue;
                    double dx=pose.X-edge.X-edge.DX*fraction,dz=pose.Z-edge.Z-edge.DZ*fraction;
                    double distanceSquared=dx*dx+dz*dz;
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
            while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<metres)a=m+1;else b=m;}
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
            for(int section=window.FirstSection;section<=window.LastSection;section++)
            {
                int a=firstEdges[section],b=edgeEnds[section];
                while(a<b){int m=a+(b-a)/2;if(edges[m].StationEnd<window.LowStationMeters)a=m+1;else b=m;}
                for(int i=a;i<edgeEnds[section];i++)
                {
                    var edge=edges[i];if(edge.StationStart>window.HighStationMeters)break;
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
    }
}
