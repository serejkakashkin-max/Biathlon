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
        private readonly struct Edge
        {
            internal readonly int Section;
            internal readonly string Loop;
            internal readonly double LocalStart,LocalEnd,StationStart,StationEnd,X,Z,Y,DX,DZ,DY,PlanarLength;
            internal Edge(int section,string loop,double localStart,double localEnd,double stationStart,
                TrackVisualPoint a,TrackVisualPoint b)
            {
                Section=section;Loop=loop;LocalStart=localStart;LocalEnd=localEnd;
                StationStart=stationStart;StationEnd=stationStart+localEnd-localStart;
                X=a.X;Y=a.Y;Z=a.Z;DX=b.X-a.X;DY=b.Y-a.Y;DZ=b.Z-a.Z;
                PlanarLength=Math.Sqrt(DX*DX+DZ*DZ);
                if(!(PlanarLength>0))throw new ArgumentException("Projection edges need horizontal extent.");
            }
        }

        public ManualRouteProjectionMap(RaceRoute route,TrackVisualDefinition geometry)
        {
            if(route==null||geometry==null)throw new ArgumentNullException();
            if(route.TrackId!=geometry.TrackId)throw new ArgumentException("Projection route and geometry must match.");
            int count=route.SectionCount;firstEdges=new int[count];edgeEnds=new int[count];
            starts=new long[count];ends=new long[count];var list=new List<Edge>();
            for(int sectionIndex=0;sectionIndex<count;sectionIndex++)
            {
                var section=route.GetSection(sectionIndex);var loop=geometry.Get(section.LoopId);
                starts[sectionIndex]=checked(section.Start.Millimeters*SkiState.NanometersPerMillimeter);
                ends[sectionIndex]=checked((section.Start.Millimeters+section.Length.Millimeters)*SkiState.NanometersPerMillimeter);
                firstEdges[sectionIndex]=list.Count;
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(section.LoopStart.Meters,loop.Get(k-1).Distance);
                    double to=Math.Min(section.LoopEnd.Meters,loop.Get(k).Distance);
                    if(to<=from)continue;
                    list.Add(new Edge(sectionIndex,loop.LoopId,from,to,section.Start.Meters+from-section.LoopStart.Meters,
                        loop.Sample(from),loop.Sample(to)));
                }
                edgeEnds[sectionIndex]=list.Count;
                if(firstEdges[sectionIndex]==edgeEnds[sectionIndex])throw new ArgumentException("Empty projection occurrence.");
            }
            length=ends[count-1];edges=list.ToArray();
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_base_route_local_projection_v1",
                route.TrackSnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson,geometry.ToJson()});
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
    }
}
