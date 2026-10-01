using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class VenueValidationPolicy
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="distance_abs_tolerance_m",IsRequired=true)] public double DistanceAbsolute {get;private set;}
        [DataMember(Name="distance_relative_tolerance",IsRequired=true)] public double DistanceRelative {get;private set;}
        [DataMember(Name="seam_tolerance_m",IsRequired=true)] public double SeamTolerance {get;private set;}
        [DataMember(Name="grade_tolerance",IsRequired=true)] public double GradeTolerance {get;private set;}
        [DataMember(Name="max_abs_grade",IsRequired=true)] public double MaxGrade {get;private set;}
        [DataMember(Name="min_main_width_m",IsRequired=true)] public double MainWidth {get;private set;}
        [DataMember(Name="min_lane_width_m",IsRequired=true)] public double LaneWidth {get;private set;}
        [DataMember(Name="min_penalty_width_m",IsRequired=true)] public double PenaltyWidth {get;private set;}
        [DataMember(Name="range_position_tolerance_m",IsRequired=true)] public double RangeTolerance {get;private set;}
        [DataMember(Name="penalty_length_m",IsRequired=true)] public double PenaltyLength {get;private set;}
        [DataMember(Name="required_lanes",IsRequired=true)] public int RequiredLanes {get;private set;}
        [DataMember(Name="firing_corridor_depth_m",IsRequired=true)] public double FiringDepth {get;private set;}
        [DataMember(Name="intersection_clearance_m",IsRequired=true)] public double IntersectionClearance {get;private set;}
        public static VenueValidationPolicy FromJson(string json)
        {
            var p=RouteJson.Read<VenueValidationPolicy>(json);if(p==null||p.SchemaVersion!=1)throw new SerializationException("Invalid venue policy schema.");TrackValidation.Id(p.Id);
            TrackValidation.Finite(p.DistanceAbsolute,0,1,"distance tolerance");TrackValidation.Finite(p.DistanceRelative,0,.05,"relative tolerance");
            TrackValidation.Finite(p.SeamTolerance,0,1,"seam tolerance");TrackValidation.Finite(p.GradeTolerance,0,.1,"grade tolerance");
            TrackValidation.Finite(p.MaxGrade,.01,1,"max grade");TrackValidation.Finite(p.MainWidth,1,20,"main width");TrackValidation.Finite(p.LaneWidth,1,10,"lane width");TrackValidation.Finite(p.PenaltyWidth,1,10,"penalty width");
            TrackValidation.Finite(p.RangeTolerance,0,1,"range tolerance");TrackValidation.Finite(p.PenaltyLength,1,1000,"penalty length");TrackValidation.Finite(p.FiringDepth,1,200,"firing corridor depth");TrackValidation.Finite(p.IntersectionClearance,.1,20,"intersection clearance");if(p.RequiredLanes<1||p.RequiredLanes>60)throw new ArgumentException("Invalid required lane count.");return p;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
    [DataContract] public sealed class VenueValidationIssue
    {
        [DataMember(Name="code")] public string Code {get;private set;}
        [DataMember(Name="path")] public string Path {get;private set;}
        [DataMember(Name="detail")] public string Detail {get;private set;}
        internal VenueValidationIssue(string code,string path,string detail){Code=code;Path=path;Detail=detail;}
    }
    [DataContract] public sealed class VenueValidationReport
    {
        [DataMember(Name="schema_version")] public int SchemaVersion {get;private set;}
        [DataMember(Name="id")] public string Id {get;private set;}
        [DataMember(Name="policy_id")] public string PolicyId {get;private set;}
        [DataMember(Name="static_checks_pass")] public bool Passed {get;private set;}
        [DataMember(Name="issues")] private VenueValidationIssue[] issues;
        public int Count=>issues.Length;public VenueValidationIssue Get(int i)=>issues[i];
        internal VenueValidationReport(string id,string policy,List<VenueValidationIssue> list)
        {SchemaVersion=1;Id=id;PolicyId=policy;list.Sort((a,b)=>{int c=StringComparer.Ordinal.Compare(a.Path,b.Path);return c!=0?c:StringComparer.Ordinal.Compare(a.Code,b.Code);});issues=list.ToArray();Passed=issues.Length==0;}
        public string ToJson()=>RouteJson.Write(this);
    }
    /// <summary>Offline diagnostics. Never relaxes, edits or advances authoritative race data.</summary>
    public static class VenueValidator
    {
        public static VenueValidationReport Validate(VenueAuthoringPlan plan,VenueValidationPolicy policy,Func<string,bool> assetExists)
        {
            if(plan==null||policy==null||assetExists==null)throw new ArgumentNullException();
            var issues=new List<VenueValidationIssue>();var track=plan.Track;var layout=plan.Manifest.Range;
            var lanes=new HashSet<string>(StringComparer.Ordinal);for(int i=0;i<plan.Lanes.Count;i++)lanes.Add(plan.Lanes.GetLane(i));
            for(int i=0;i<track.LoopCount;i++)
            {
                var loop=track.GetLoop(i);var visual=plan.Visual.Get(loop.Id);string path="loops/"+loop.Id;
                VenuePathIntersections.FindSelfIntersections(visual,policy.IntersectionClearance,Add);
                double total=0,minWidth=loop.Kind==TrackLoopKind.Penalty?policy.PenaltyWidth:lanes.Contains(loop.Id)?policy.LaneWidth:policy.MainWidth;
                for(int j=0;j<loop.SegmentCount;j++)
                {
                    var s=loop.GetSegment(j);
                    if(s.WidthMeters<minWidth)Add("width.minimum",path+"/"+s.Id,"Authored width "+F(s.WidthMeters)+" < "+F(minWidth));
                    if(Math.Max(Math.Abs(s.StartGrade),Math.Abs(s.EndGrade))>policy.MaxGrade)Add("grade.limit",path+"/"+s.Id,"Authoritative grade exceeds production policy.");
                }
                for(int k=1;k<visual.Count;k++)
                {
                    var a=visual.Get(k-1);var b=visual.Get(k);double dx=b.X-a.X,dy=b.Y-a.Y,dz=b.Z-a.Z,horizontal=Math.Sqrt(dx*dx+dz*dz),world=Math.Sqrt(horizontal*horizontal+dy*dy),declared=b.Distance-a.Distance;
                    total+=world;string edge=path+"/edges/"+(k-1);
                    Project(a.X,a.Y,a.Z,out double ax,out double az,out _);Project(b.X,b.Y,b.Z,out double bx,out double bz,out _);
                    double firingHalf=plan.Lanes.Count*layout.LanePitch*.5;
                    if(CrossesRectangle(ax,az,bx,bz,-firingHalf,firingHalf,policy.RangeTolerance,policy.FiringDepth))
                        Add(loop.Kind==TrackLoopKind.Penalty?"penalty.firing_corridor":"range.route_in_firing_corridor",edge,"Route centreline enters the firing corridor.");
                    if(Math.Abs(world-declared)>policy.DistanceAbsolute+policy.DistanceRelative*declared)Add("distance.world",edge,"World "+F(world)+"m vs authored "+F(declared)+"m.");
                    double grade=dy/horizontal;
                    if(Math.Abs(grade)>policy.MaxGrade)Add("grade.world_limit",edge,"World grade "+F(grade)+" exceeds policy.");
                    for(int j=0;j<loop.SegmentCount;j++)
                    {
                        var s=loop.GetSegment(j);double lo=Math.Max(a.Distance,s.StartDistance.Meters),hi=Math.Min(b.Distance,s.StartDistance.Meters+s.Length.Meters);if(hi<=lo)continue;
                        double lowGrade=s.StartGrade+(s.EndGrade-s.StartGrade)*(lo-s.StartDistance.Meters)/s.Length.Meters;
                        double highGrade=s.StartGrade+(s.EndGrade-s.StartGrade)*(hi-s.StartDistance.Meters)/s.Length.Meters;
                        if(Math.Max(Math.Abs(lowGrade-grade),Math.Abs(highGrade-grade))>policy.GradeTolerance)Add("grade.world_mismatch",edge+"/"+s.Id,"World grade "+F(grade)+" vs authoritative interval "+F(lowGrade)+".."+F(highGrade));
                    }
                }
                if(loop.Kind==TrackLoopKind.Penalty&&Math.Abs(total-policy.PenaltyLength)>policy.DistanceAbsolute+policy.DistanceRelative*policy.PenaltyLength)Add("penalty.length",path,"Penalty world distance is "+F(total)+"m.");
                if(!loop.Closed&&!lanes.Contains(loop.Id))Add("loop.open",path,"Production race/penalty circuits must close; explicit range branches are exempt.");
            }
            var entered=new HashSet<string>(StringComparer.Ordinal);var exited=new HashSet<string>(StringComparer.Ordinal);var penaltyLanes=new HashSet<string>(StringComparer.Ordinal);bool mainPenaltyIn=false,penaltyOut=false;
            for(int i=0;i<plan.Connections.Count;i++)
            {
                var c=plan.Connections.Get(i);PhysicalJunction junction=null;
                for(int j=0;j<plan.Junctions.Count;j++)if(plan.Junctions.Get(j).Id==c.JunctionId)junction=plan.Junctions.Get(j);
                TrackAnchor from=null,to=null;for(int j=0;j<junction.PortCount;j++){var p=junction.GetPort(j);if(p.Id==c.FromPortId)from=p.Anchor;if(p.Id==c.ToPortId)to=p.Anchor;}
                var a=plan.At(from);var b=plan.At(to);double distance=Distance(a,b);
                if(distance>policy.SeamTolerance)Add("connection.world_gap","connections/"+c.Id,"Coincident seam is separated by "+F(distance)+"m.");
                if(lanes.Contains(to.LoopId)&&to.Distance.Millimeters==0&&from.LoopId==plan.Lanes.MainLoopId&&from.Distance.Equals(plan.Lanes.Entry))entered.Add(to.LoopId);
                if(lanes.Contains(from.LoopId)&&from.Distance.Equals(track.GetLoop(from.LoopId).Length)&&to.LoopId==plan.Lanes.MainLoopId&&to.Distance.Equals(plan.Lanes.Exit))exited.Add(from.LoopId);
                if(to.LoopId==track.Penalty.LoopId&&to.Distance.Millimeters==0)
                {
                    if(from.LoopId==track.Penalty.Entry.LoopId&&from.Distance.Equals(track.Penalty.Entry.Distance))mainPenaltyIn=true;
                    if(lanes.Contains(from.LoopId)&&from.Distance.Equals(track.GetLoop(from.LoopId).Length))penaltyLanes.Add(from.LoopId);
                }
                if(from.LoopId==track.Penalty.LoopId&&from.Distance.Millimeters==0&&to.LoopId==track.Penalty.Rejoin.LoopId&&to.Distance.Equals(track.Penalty.Rejoin.Distance))penaltyOut=true;
            }
            foreach(string lane in lanes)if(!entered.Contains(lane)||!exited.Contains(lane))Add("range.lane_disconnected","range/lanes/"+lane,"Lane needs directed main entrance and rejoin seams at its authored endpoints.");
            foreach(string lane in lanes)if(!penaltyLanes.Contains(lane)&&!(exited.Contains(lane)&&mainPenaltyIn))Add("penalty.disconnected","penalty/lanes/"+lane,"Lane exit has no directed penalty access.");
            if(!penaltyOut)Add("penalty.disconnected","penalty/rejoin","Penalty loop needs a directed rejoin seam.");
            if(plan.Lanes.Count<policy.RequiredLanes)Add("range.capacity","range","Lane count below required field capacity.");
            double halfWidth=plan.Lanes.Count*layout.LanePitch*.5;
            if(layout.Entry.LoopId!=plan.Lanes.MainLoopId||!layout.Entry.Distance.Equals(plan.Lanes.Entry)||layout.Exit.LoopId!=plan.Lanes.MainLoopId||!layout.Exit.Distance.Equals(plan.Lanes.Exit))
                Add("range.layout_anchor_mismatch","range","Layout entrance/exit must bind the actual physical fork/rejoin.");
            CheckSide("entry",plan.At(layout.Entry));CheckSide("exit",plan.At(layout.Exit));
            double stop=track.Range.FiringLine.Distance.Meters-plan.Lanes.Entry.Meters;
            for(int i=0;i<plan.Lanes.Count;i++)
            {
                string id=plan.Lanes.GetLane(i);var p=plan.Visual.Get(id).Sample(stop);Project(p.X,p.Y,p.Z,out double lateral,out double depth,out double height);
                double expected=(plan.Lanes.Count*.5-.5-i)*layout.LanePitch;
                if(Math.Abs(lateral-expected)>policy.RangeTolerance||Math.Abs(depth)>policy.RangeTolerance||Math.Abs(height)>policy.RangeTolerance)
                    Add("range.mat_alignment","range/lanes/"+id,"Stop does not match its right-to-left firing-line slot.");
            }
            // A common exclusive entry region cannot certify independent access to free lanes.
            int entryRegions=0;
            for(int i=0;i<plan.Junctions.Count;i++)
            {
                var j=plan.Junctions.Get(i);int entryLanes=0;
                for(int k=0;k<j.PortCount;k++){var p=j.GetPort(k);if(lanes.Contains(p.Anchor.LoopId)&&p.Anchor.Distance.Millimeters==0)entryLanes++;}
                if(entryLanes>0)entryRegions++;
                if(entryLanes>1)Add("range.shared_entry_gate","junctions/"+j.Id,"Multiple lane entrances share an exclusive admission region; dense-flow proof or topology revision required.");
            }
            if(entryRegions>1)Add("range.split_entry_regions","range/entry","Equal-length lane starts share the same physical fork across multiple conflict-region IDs; independent access is not certified.");
            for(int i=0;i<plan.Manifest.AssetCount;i++){var asset=plan.Manifest.Asset(i);if(!assetExists(asset.Resource))Add("asset.missing","assets/"+asset.Id,asset.Resource);}
            return new VenueValidationReport(plan.Manifest.Id,policy.Id,issues);
            void Add(string code,string path,string detail)=>issues.Add(new VenueValidationIssue(code,path,detail));
            void Project(double x,double y,double z,out double lateral,out double depth,out double height)
            {double dx=x-layout.Origin.X,dz=z-layout.Origin.Z;lateral=dx*layout.Forward.Z-dz*layout.Forward.X;depth=dx*layout.Forward.X+dz*layout.Forward.Z;height=y-layout.Origin.Y;}
            void CheckSide(string id,TrackVisualPoint p)
            {
                Project(p.X,p.Y,p.Z,out double lateral,out double depth,out _);
                if(Math.Abs(lateral)<halfWidth-policy.RangeTolerance||depth>policy.RangeTolerance||depth< -layout.ApronDepth-policy.RangeTolerance)
                    Add("range.side_approach","range/"+id,"Entry/exit must be beside the firing line, within the rear apron depth.");
            }
        }
        private static double Distance(TrackVisualPoint a,TrackVisualPoint b){double x=a.X-b.X,y=a.Y-b.Y,z=a.Z-b.Z;return Math.Sqrt(x*x+y*y+z*z);}
        private static bool CrossesRectangle(double ax,double az,double bx,double bz,double minX,double maxX,double minZ,double maxZ)
        {
            double lo=0,hi=1;
            return Clip(ax,bx-ax,minX,maxX)&&Clip(az,bz-az,minZ,maxZ)&&lo<hi;
            bool Clip(double start,double delta,double min,double max)
            {
                if(delta==0)return start>min&&start<max;
                double a=(min-start)/delta,b=(max-start)/delta;if(a>b){double t=a;a=b;b=t;}
                lo=Math.Max(lo,a);hi=Math.Min(hi,b);return lo<hi;
            }
        }
        private static string F(double value)=>value.ToString("0.######",CultureInfo.InvariantCulture);
    }
}
