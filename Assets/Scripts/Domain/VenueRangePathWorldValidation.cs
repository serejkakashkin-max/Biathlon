using System;
using System.Collections.Generic;
using System.Globalization;

namespace Biathlon.Domain
{
    /// <summary>Cold static checks for schema 2 physical paths. Corridor overlap needs a shared
    /// local guard; this is not a substitute for accepted-motion/body admission tests.</summary>
    public static class VenueRangePathWorldValidation
    {
        private readonly struct Point
        {
            public readonly double X,Z;
            public Point(double x,double z){X=x;Z=z;}
        }
        private readonly struct Edge
        {
            public readonly string Loop;
            public readonly int Index;
            public readonly TrackVisualKnot A,B;
            public readonly double Width;
            public Edge(string loop,int index,TrackVisualKnot a,TrackVisualKnot b,double width)
            {Loop=loop;Index=index;A=a;B=b;Width=width;}
            public double Parameter(Point p)
            {double x=B.X-A.X,z=B.Z-A.Z;return ((p.X-A.X)*x+(p.Z-A.Z)*z)/(x*x+z*z);}
            public double Distance(double t)=>A.Distance+(B.Distance-A.Distance)*t;
            public double Height(double t)=>A.Y+(B.Y-A.Y)*t;
        }
        public static VenueValidationReport Validate(VenueAuthoringPlan plan,VenueValidationPolicy policy,Func<string,bool> assetExists)
        {
            if(plan==null||policy==null||assetExists==null)throw new ArgumentNullException();
            if(!plan.UsesPhysicalPaths)throw new ArgumentException("Explicit physical path binding required.");
            var issues=new List<VenueValidationIssue>();var track=plan.Track;var paths=plan.Paths;var layout=plan.Manifest.Range;
            if(plan.StartContinuation!=null&&!plan.StartContinuation.MatchesPresentation(plan.Visual))
                issues.Add(new VenueValidationIssue("start_continuation.presentation_mismatch","start_continuation",
                    "Rendering geometry differs from the authoritative start continuation corridors."));
            var usage=new Dictionary<string,int>(StringComparer.Ordinal);
            for(int lane=0;lane<paths.Count;lane++)
            {
                var used=new HashSet<string>(StringComparer.Ordinal);var path=paths.GetLane(lane);
                for(int s=0;s<path.SliceCount;s++)used.Add(path.GetSlice(s).LoopId);
                foreach(string id in used){usage.TryGetValue(id,out int count);usage[id]=count+1;}
            }
            var edges=new List<Edge>();double firingHalf=paths.Count*layout.LanePitch*.5;
            for(int i=0;i<track.LoopCount;i++)
            {
                var loop=track.GetLoop(i);var visual=plan.Visual.Get(loop.Id);string path="loops/"+loop.Id;
                bool used=usage.TryGetValue(loop.Id,out int uses);
                double minimum=loop.Kind==TrackLoopKind.Penalty?policy.PenaltyWidth:loop.Id!=paths.MainLoopId&&used&&uses==1?policy.LaneWidth:policy.MainWidth;
                bool startApproach=plan.StartApproach!=null&&loop.Id==plan.StartApproach.LoopId;
                if(!loop.Closed&&!used&&!startApproach&&(plan.Formats==null||!plan.Formats.UsesLoop(loop.Id)))
                    Add("loop.open",path,"Open race loops require physical range paths, a bound start approach or a validated directed format route.");
                for(int j=0;j<loop.SegmentCount;j++)
                {
                    var segment=loop.GetSegment(j);
                    if(segment.WidthMeters<minimum)Add("width.minimum",path+"/"+segment.Id,"Authored width "+F(segment.WidthMeters)+" < "+F(minimum));
                    if(segment.WidthMeters<plan.Occupancy.EdgeMargin*2)Add("width.body",path+"/"+segment.Id,"Authored corridor cannot contain the configured body and edge clearance.");
                    if(Math.Max(Math.Abs(segment.StartGrade),Math.Abs(segment.EndGrade))>policy.MaxGrade)Add("grade.limit",path+"/"+segment.Id,"Authoritative grade exceeds production policy.");
                }
                double total=0;
                for(int k=1;k<visual.Count;k++)
                {
                    var a=visual.Get(k-1);var b=visual.Get(k);double dx=b.X-a.X,dy=b.Y-a.Y,dz=b.Z-a.Z;
                    double horizontal=Math.Sqrt(dx*dx+dz*dz),world=Math.Sqrt(horizontal*horizontal+dy*dy),declared=b.Distance-a.Distance;
                    string edgePath=path+"/edges/"+(k-1);total+=world;
                    if(Math.Abs(world-declared)>policy.DistanceAbsolute+policy.DistanceRelative*declared)Add("distance.world",edgePath,"World "+F(world)+"m vs authored "+F(declared)+"m.");
                    double grade=dy/horizontal,width=0;
                    if(Math.Abs(grade)>policy.MaxGrade)Add("grade.world_limit",edgePath,"World grade exceeds production policy.");
                    for(int j=0;j<loop.SegmentCount;j++)
                    {
                        var segment=loop.GetSegment(j);double low=Math.Max(a.Distance,segment.StartDistance.Meters),high=Math.Min(b.Distance,new TrackDistance(checked(segment.StartDistance.Millimeters+segment.Length.Millimeters)).Meters);
                        if(high<=low)continue;width=Math.Max(width,segment.WidthMeters);
                        double first=segment.StartGrade+(segment.EndGrade-segment.StartGrade)*(low-segment.StartDistance.Meters)/segment.Length.Meters;
                        double last=segment.StartGrade+(segment.EndGrade-segment.StartGrade)*(high-segment.StartDistance.Meters)/segment.Length.Meters;
                        if(Math.Max(Math.Abs(first-grade),Math.Abs(last-grade))>policy.GradeTolerance)Add("grade.world_mismatch",edgePath+"/"+segment.Id,"World grade differs from the authoritative interval.");
                    }
                    Project(a.X,a.Y,a.Z,out double ax,out double az,out _);Project(b.X,b.Y,b.Z,out double bx,out double bz,out _);
                    if(CrossesRectangle(ax,az,bx,bz,-firingHalf,firingHalf,policy.RangeTolerance,policy.FiringDepth))
                        Add(loop.Kind==TrackLoopKind.Penalty?"penalty.firing_corridor":"range.route_in_firing_corridor",edgePath,"Route centreline enters the firing corridor.");
                    edges.Add(new Edge(loop.Id,k-1,a,b,width));
                }
                if(loop.Kind==TrackLoopKind.Penalty&&Math.Abs(total-policy.PenaltyLength)>policy.DistanceAbsolute+policy.DistanceRelative*policy.PenaltyLength)Add("penalty.length",path,"Penalty world distance differs from the configured length.");
            }
            if(paths.Count<policy.RequiredLanes)Add("range.capacity","range","Lane count below required field capacity.");
            if(layout.Entry.LoopId!=paths.MainLoopId||!layout.Entry.Distance.Equals(paths.Entry)||layout.Exit.LoopId!=paths.MainLoopId||!layout.Exit.Distance.Equals(paths.Exit))
                Add("range.layout_anchor_mismatch","range","Layout anchors must bind the replaced physical path interval.");
            CheckSide("entry",plan.At(layout.Entry));CheckSide("exit",plan.At(layout.Exit));
            for(int i=0;i<paths.Count;i++)
            {
                var lane=paths.GetLane(i);var mat=plan.At(lane.MatAnchor);Project(mat.X,mat.Y,mat.Z,out double lateral,out double depth,out double height);
                double expected=(paths.Count*.5-.5-i)*layout.LanePitch;
                if(Math.Abs(lateral-expected)>policy.RangeTolerance||Math.Abs(depth)>policy.RangeTolerance||Math.Abs(height)>policy.RangeTolerance)
                    Add("range.mat_alignment","range/lanes/"+lane.Id,"Authored mat anchor does not match its right-to-left firing-line slot.");
                var end=lane.GetSlice(lane.SliceCount-1);
                if(!Connected(end.LoopId,end.End,track.Penalty.LoopId,new TrackDistance(0))&&
                    !Connected(track.Penalty.Entry.LoopId,track.Penalty.Entry.Distance,track.Penalty.LoopId,new TrackDistance(0)))
                    Add("penalty.disconnected","penalty/lanes/"+lane.Id,"Accepted lane exit has no directed penalty access.");
            }
            if(!Connected(track.Penalty.LoopId,new TrackDistance(0),track.Penalty.Rejoin.LoopId,track.Penalty.Rejoin.Distance))
                Add("penalty.disconnected","penalty/rejoin","Penalty circuit needs a directed rejoin seam.");
            if(plan.StartApproach!=null)
            {
                var approach=track.GetLoop(plan.StartApproach.LoopId);
                if(approach.Closed||approach.Kind!=TrackLoopKind.Race)
                    Add("start_approach.loop","start_approach","The bound start approach must be a distinct open race corridor.");
                for(int lane=0;lane<paths.Count;lane++)for(int s=0;s<paths.GetLane(lane).SliceCount;s++)
                    if(paths.GetLane(lane).GetSlice(s).LoopId==approach.Id)
                        Add("start_approach.range_overlap","start_approach","Start approach cannot also be counted range-path travel.");
                if(!Connected(approach.Id,approach.Length,plan.StartApproach.RouteEntryLoopId,new TrackDistance(0)))
                    Add("start_approach.disconnected","start_approach","An explicit directed end-to-route-start seam is required.");
                var a=plan.Visual.Get(approach.Id).Sample(approach.Length.Meters);var b=plan.Visual.Get(plan.StartApproach.RouteEntryLoopId).Sample(0);
                double x=a.X-b.X,y=a.Y-b.Y,z=a.Z-b.Z;
                if(Math.Sqrt(x*x+y*y+z*z)>policy.SeamTolerance)
                    Add("start_approach.world_gap","start_approach","Approach endpoint and main route start must coincide in world space.");
            }
            for(int i=0;i<plan.Connections.Count;i++)
            {
                var connection=plan.Connections.Get(i);var from=Port(connection.FromPortId);var to=Port(connection.ToPortId);var a=plan.At(from.Anchor);var b=plan.At(to.Anchor);
                double x=a.X-b.X,y=a.Y-b.Y,z=a.Z-b.Z;
                if(Math.Sqrt(x*x+y*y+z*z)>policy.SeamTolerance)Add("connection.world_gap","connections/"+connection.Id,"Coincident seam has a world-space gap.");
            }
            // Region membership follows guard intervals, not just exact port anchors. An extra
            // broad main/private guard can otherwise serialize several individually authored forks.
            for(int i=0;i<plan.Junctions.Count;i++)
            {
                var junction=plan.Junctions.Get(i);var forks=new HashSet<(string Main,long Fork,string Private,long Start)>();
                for(int lane=0;lane<paths.Count;lane++)
                {
                    var a=paths.GetLane(lane).GetSlice(0);var b=paths.GetLane(lane).GetSlice(1);
                    if(Covers(junction,a.LoopId,a.End.Meters,a.End.Meters,track.GetLoop(a.LoopId))||
                        Covers(junction,b.LoopId,b.Start.Meters,b.Start.Meters,track.GetLoop(b.LoopId)))
                        forks.Add((a.LoopId,a.End.Millimeters,b.LoopId,b.Start.Millimeters));
                }
                if(forks.Count>1)Add("range.shared_entry_gate","junctions/"+junction.Id,"One exclusive guard region covers multiple distinct mat forks; local independent access is not certified.");
            }
            for(int i=0;i<edges.Count;i++)for(int j=i+1;j<edges.Count;j++)
            {
                var a=edges[i];var b=edges[j];
                if(a.Loop==b.Loop)
                {
                    var loop=track.GetLoop(a.Loop);int last=plan.Visual.Get(a.Loop).Count-2;
                    if(Math.Abs(a.Index-b.Index)<=1||loop.Closed&&a.Index==0&&b.Index==last)
                    {
                        if(Retraces(a,b))Add("loop.adjacent_retrace","loops/"+a.Loop+"/edges/"+a.Index+"/"+b.Index,
                            "Joined centreline edges retrace a positive horizontal interval.");
                        continue;
                    }
                }
                double clearance=plan.Occupancy.PersonalClearance*.5;
                if(!BoundsOverlap(a,b,clearance))continue;
                var polygon=Overlap(a,b,clearance);if(polygon.Count==0)continue;
                double alo=double.MaxValue,ahi=double.MinValue,blo=double.MaxValue,bhi=double.MinValue,minHeight=double.MaxValue,maxHeight=double.MinValue;
                foreach(var point in polygon)
                {
                    double at=Clamp(a.Parameter(point)),bt=Clamp(b.Parameter(point));alo=Math.Min(alo,a.Distance(at));ahi=Math.Max(ahi,a.Distance(at));blo=Math.Min(blo,b.Distance(bt));bhi=Math.Max(bhi,b.Distance(bt));
                    double gap=a.Height(at)-b.Height(bt);minHeight=Math.Min(minHeight,gap);maxHeight=Math.Max(maxHeight,gap);
                }
                double separation=minHeight<=0&&maxHeight>=0?0:Math.Min(Math.Abs(minHeight),Math.Abs(maxHeight));if(separation>=policy.IntersectionClearance)continue;
                double margin=plan.Occupancy.BodyLength*.5+plan.Occupancy.PersonalClearance;
                alo=Math.Max(0,alo-margin);ahi=Math.Min(track.GetLoop(a.Loop).Length.Meters,ahi+margin);blo=Math.Max(0,blo-margin);bhi=Math.Min(track.GetLoop(b.Loop).Length.Meters,bhi+margin);
                bool common=false,covered=false;
                for(int region=0;region<plan.Junctions.Count;region++)
                {
                    var junction=plan.Junctions.Get(region);bool hasA=false,hasB=false;
                    for(int p=0;p<junction.PortCount;p++)
                    {
                        var port=junction.GetPort(p);
                        if(port.Anchor.LoopId==a.Loop)hasA=true;
                        if(port.Anchor.LoopId==b.Loop)hasB=true;
                    }
                    common|=hasA&&hasB;covered|=Covers(junction,a.Loop,alo,ahi,track.GetLoop(a.Loop))&&Covers(junction,b.Loop,blo,bhi,track.GetLoop(b.Loop));
                }
                if(!covered)Add(common?"junction.guard_extent":"junction.unprotected_crossing","crossings/"+a.Loop+"/"+a.Index+"/"+b.Loop+"/"+b.Index,
                    "World corridor overlap requires one shared region covering "+a.Loop+" "+F(alo)+".."+F(ahi)+"m and "+b.Loop+" "+F(blo)+".."+F(bhi)+"m, including configured body clearance.");
            }
            for(int i=0;i<plan.Manifest.AssetCount;i++){var asset=plan.Manifest.Asset(i);if(!assetExists(asset.Resource))Add("asset.missing","assets/"+asset.Id,asset.Resource);}
            return new VenueValidationReport(plan.Manifest.Id,policy.Id,issues);
            void Add(string code,string path,string detail)=>issues.Add(new VenueValidationIssue(code,path,detail));
            void Project(double x,double y,double z,out double lateral,out double depth,out double height)
            {double dx=x-layout.Origin.X,dz=z-layout.Origin.Z;lateral=dx*layout.Forward.Z-dz*layout.Forward.X;depth=dx*layout.Forward.X+dz*layout.Forward.Z;height=y-layout.Origin.Y;}
            void CheckSide(string id,TrackVisualPoint point)
            {Project(point.X,point.Y,point.Z,out double lateral,out double depth,out _);if(Math.Abs(lateral)<firingHalf-policy.RangeTolerance||depth>policy.RangeTolerance||depth< -layout.ApronDepth-policy.RangeTolerance)Add("range.side_approach","range/"+id,"Entry/exit must lie beside the firing line inside the rear apron.");}
            PhysicalJunctionPort Port(string id)
            {for(int r=0;r<plan.Junctions.Count;r++){var junction=plan.Junctions.Get(r);for(int p=0;p<junction.PortCount;p++)if(junction.GetPort(p).Id==id)return junction.GetPort(p);}throw new ArgumentException("Unknown validated port.");}
            bool Connected(string fromLoop,TrackDistance from,string toLoop,TrackDistance to)
            {for(int i=0;i<plan.Connections.Count;i++){var c=plan.Connections.Get(i);if(Matches(Port(c.FromPortId),fromLoop,from)&&Matches(Port(c.ToPortId),toLoop,to))return true;}return false;}
        }
        private static bool Matches(PhysicalJunctionPort port,string loop,TrackDistance distance)=>port.Anchor.LoopId==loop&&port.Anchor.Distance.Equals(distance);
        private static bool Retraces(Edge a,Edge b)
        {
            // Arithmetic tolerance only. Ordinary ribbon overlap at a bend is permitted;
            // a collinear reversal has positive centreline overlap away from the shared knot.
            double ax=a.B.X-a.A.X,az=a.B.Z-a.A.Z,bx=b.B.X-b.A.X,bz=b.B.Z-b.A.Z;
            double cross=ax*bz-az*bx;
            return ax*bx+az*bz<0&&Math.Abs(cross)<=1e-12*(Math.Abs(ax*bz)+Math.Abs(az*bx));
        }
        private static bool Covers(PhysicalJunction junction,string loopId,double low,double high,TrackLoopDefinition loop)
        {
            var intervals=new List<(double Low,double High)>();
            for(int p=0;p<junction.PortCount;p++)
            {
                var port=junction.GetPort(p);if(port.Anchor.LoopId!=loopId)continue;
                for(int shift=loop.Closed?-1:0;shift<=(loop.Closed?1:0);shift++)
                {double at=port.Anchor.Distance.Meters+shift*loop.Length.Meters;intervals.Add((at-port.GuardBefore.Meters,at+port.GuardAfter.Meters));}
            }
            intervals.Sort((a,b)=>a.Low.CompareTo(b.Low));double reached=low;
            foreach(var interval in intervals)
            {if(interval.High<reached)continue;if(interval.Low>reached+1e-9)return false;reached=Math.Max(reached,interval.High);if(reached>=high-1e-9)return true;}
            return false;
        }
        private static List<Point> Overlap(Edge a,Edge b,double clearance)
        {
            var result=Rectangle(a,clearance);var clip=Rectangle(b,clearance);
            for(int edge=0;edge<4&&result.Count>0;edge++)
            {
                var start=clip[edge];var end=clip[(edge+1)%4];var input=result;result=new List<Point>();var previous=input[input.Count-1];double previousSide=Side(start,end,previous);
                foreach(var point in input)
                {
                    double side=Side(start,end,point);
                    if((side>=0)!=(previousSide>=0))
                    {double t=previousSide/(previousSide-side);result.Add(new Point(previous.X+(point.X-previous.X)*t,previous.Z+(point.Z-previous.Z)*t));}
                    if(side>=0)result.Add(point);previous=point;previousSide=side;
                }
            }
            return result;
        }
        private static bool BoundsOverlap(Edge a,Edge b,double clearance)
        {
            double ah=a.Width*.5+clearance,bh=b.Width*.5+clearance;
            return Math.Max(a.A.X,a.B.X)+ah>=Math.Min(b.A.X,b.B.X)-bh&&Math.Max(b.A.X,b.B.X)+bh>=Math.Min(a.A.X,a.B.X)-ah&&
                Math.Max(a.A.Z,a.B.Z)+ah>=Math.Min(b.A.Z,b.B.Z)-bh&&Math.Max(b.A.Z,b.B.Z)+bh>=Math.Min(a.A.Z,a.B.Z)-ah;
        }
        private static List<Point> Rectangle(Edge edge,double clearance)
        {
            double dx=edge.B.X-edge.A.X,dz=edge.B.Z-edge.A.Z,n=Math.Sqrt(dx*dx+dz*dz),half=edge.Width*.5+clearance;
            double x=-dz/n*half,z=dx/n*half;
            return new List<Point>{new Point(edge.A.X+x,edge.A.Z+z),new Point(edge.A.X-x,edge.A.Z-z),new Point(edge.B.X-x,edge.B.Z-z),new Point(edge.B.X+x,edge.B.Z+z)};
        }
        private static double Side(Point a,Point b,Point p)=>(b.X-a.X)*(p.Z-a.Z)-(b.Z-a.Z)*(p.X-a.X);
        private static double Clamp(double value)=>Math.Max(0,Math.Min(1,value));
        private static bool CrossesRectangle(double ax,double az,double bx,double bz,double minX,double maxX,double minZ,double maxZ)
        {
            double low=0,high=1;return Clip(ax,bx-ax,minX,maxX)&&Clip(az,bz-az,minZ,maxZ)&&low<high;
            bool Clip(double start,double delta,double min,double max)
            {if(delta==0)return start>min&&start<max;double a=(min-start)/delta,b=(max-start)/delta;if(a>b){double t=a;a=b;b=t;}low=Math.Max(low,a);high=Math.Min(high,b);return low<high;}
        }
        private static string F(double value)=>value.ToString("0.######",CultureInfo.InvariantCulture);
    }
}
