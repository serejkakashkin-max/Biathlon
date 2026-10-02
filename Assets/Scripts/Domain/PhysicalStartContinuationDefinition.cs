using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class PhysicalCorridorKnot
    {
        [DataMember(Name="distance_m",IsRequired=true)] public double Distance { get; private set; }
        [DataMember(Name="x",IsRequired=true)] public double X { get; private set; }
        [DataMember(Name="y",IsRequired=true)] public double Y { get; private set; }
        [DataMember(Name="z",IsRequired=true)] public double Z { get; private set; }
        internal void BindDistance(TrackDistance distance)=>Distance=distance.Meters;
    }
    /// <summary>Authoritative piecewise-linear world centreline, independent of presentation assets.</summary>
    [DataContract] public sealed class PhysicalStartCorridor
    {
        [DataMember(Name="loop_id",IsRequired=true)] public string LoopId { get; private set; }
        [DataMember(Name="knots",IsRequired=true)] private PhysicalCorridorKnot[] knots { get; set; }
        public int Count=>knots.Length;
        public PhysicalCorridorKnot Get(int index)=>knots[index];
        public double Length=>knots[knots.Length-1].Distance;
        internal void Validate(TrackDefinition track,VenueValidationPolicy policy)
        {
            TrackValidation.Id(LoopId);var loop=track.GetLoop(LoopId);
            if(knots==null||knots.Length<2||knots.Length>10000)throw new ArgumentException("A bounded authoritative corridor is required.");
            long previous=-1;
            for(int i=0;i<Count;i++)
            {
                var b=Get(i);if(b==null)throw new ArgumentException("Null corridor knot.");
                TrackValidation.Finite(b.Distance,0,loop.Length.Meters,"corridor distance");
                var coordinate=TrackDistance.FromMeters(b.Distance);
                if(coordinate.Millimeters<=previous)throw new ArgumentException("Authoritative corridor knots require strictly advancing whole-millimetre coordinates.");
                b.BindDistance(coordinate);
                TrackValidation.Finite(b.X,-1000000,1000000,"corridor x");
                TrackValidation.Finite(b.Y,-1000000,1000000,"corridor y");
                TrackValidation.Finite(b.Z,-1000000,1000000,"corridor z");
                if(i>0)
                {
                    var a=Get(i-1);double x=b.X-a.X,y=b.Y-a.Y,z=b.Z-a.Z;
                    double horizontal=Magnitude(x,0,z),world=Magnitude(x,y,z),declared=b.Distance-a.Distance;
                    if(horizontal==0||Math.Abs(world-declared)>policy.DistanceAbsolute+policy.DistanceRelative*declared)
                        throw new ArgumentException("Corridor world distance must match authoritative travel.");
                    if(Math.Abs(y/horizontal)>policy.MaxGrade)throw new ArgumentException("Corridor world grade exceeds policy.");
                }
                previous=coordinate.Millimeters;
            }
            if(Get(0).Distance!=0)throw new ArgumentException("Corridor must start at local zero.");
        }
        private static double Magnitude(double x,double y,double z)
        {
            double scale=Math.Max(Math.Abs(x),Math.Max(Math.Abs(y),Math.Abs(z)));if(scale==0)return 0;
            x/=scale;y/=scale;z/=scale;return scale*Math.Sqrt(x*x+y*y+z*z);
        }
    }

    /// <summary>Opt-in shared start continuation. The captured geometry is gameplay authority;
    /// rendering must be checked against it. Other junctions and main-terminal traffic remain exclusive.</summary>
    [DataContract] public sealed class PhysicalStartContinuationDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId { get; private set; }
        [DataMember(Name="start_approach_id",IsRequired=true)] public string StartApproachId { get; private set; }
        [DataMember(Name="connection_id",IsRequired=true)] public string ConnectionId { get; private set; }
        [DataMember(Name="occupancy_definition_id",IsRequired=true)] public string OccupancyId { get; private set; }
        [DataMember(Name="validation_policy_resource",IsRequired=true)] public string ValidationPolicyResource { get; private set; }
        [DataMember(Name="corridors",IsRequired=true)] private PhysicalStartCorridor[] corridors { get; set; }
        public PhysicalStartCorridor Source { get; private set; }
        public PhysicalStartCorridor Main { get; private set; }
        public VenueValidationPolicy ValidationPolicy { get; private set; }
        public int JunctionIndex { get; private set; }
        public string TrackSnapshotJson { get; private set; }
        public string ApproachSnapshotJson { get; private set; }
        public string JunctionSnapshotJson { get; private set; }
        public string ConnectionSnapshotJson { get; private set; }
        public string OccupancySnapshotJson { get; private set; }

        public static PhysicalStartContinuationDefinition FromJson(string json,TrackDefinition track,
            StartApproachDefinition approach,PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,
            OccupancyDefinition body,Func<string,string> readResource)
        {
            if(track==null||approach==null||junctions==null||connections==null||body==null||readResource==null)throw new ArgumentNullException();
            var value=RouteJson.Read<PhysicalStartContinuationDefinition>(json);
            if(value==null||value.SchemaVersion!=1||value.corridors==null||value.corridors.Length!=2)
                throw new SerializationException("Two authoritative start continuation corridors are required.");
            TrackValidation.Id(value.Id);TrackValidation.Id(value.TrackId);TrackValidation.Id(value.StartApproachId);
            TrackValidation.Id(value.ConnectionId);TrackValidation.Id(value.OccupancyId);VenueManifest.ResourceId(value.ValidationPolicyResource);
            string snapshot=track.ToJson();
            if(value.TrackId!=track.Id||value.StartApproachId!=approach.Id||value.OccupancyId!=body.Id||approach.TrackId!=track.Id||
                junctions.TrackSnapshotJson!=snapshot||connections.TrackSnapshotJson!=snapshot||connections.JunctionSnapshotJson!=junctions.ToJson())
                throw new ArgumentException("Exact start continuation track, topology, approach and body binding required.");
            var sourceLoop=track.GetLoop(approach.LoopId);var mainLoop=track.GetLoop(approach.RouteEntryLoopId);
            if(sourceLoop.Closed||sourceLoop.Kind!=TrackLoopKind.Race||mainLoop.Kind!=TrackLoopKind.Race||sourceLoop.Id==mainLoop.Id)
                throw new ArgumentException("A distinct open start corridor and main race corridor are required.");
            value.ValidationPolicy=VenueValidationPolicy.FromJson(readResource(value.ValidationPolicyResource));
            foreach(var corridor in value.corridors)
            {
                if(corridor==null)throw new ArgumentException("Null continuation corridor.");corridor.Validate(track,value.ValidationPolicy);
                if(corridor.LoopId==sourceLoop.Id&&value.Source==null)value.Source=corridor;
                else if(corridor.LoopId==mainLoop.Id&&value.Main==null)value.Main=corridor;
                else throw new ArgumentException("Only the exact source and main prefix corridors may share this policy.");
            }
            if(value.Source==null||value.Main==null||value.Source.Length!=sourceLoop.Length.Meters)
                throw new ArgumentException("The complete actual source corridor is required.");
            PhysicalConnection connection=null;
            for(int i=0;i<connections.Count;i++)if(connections.Get(i).Id==value.ConnectionId)connection=connections.Get(i);
            if(connection==null)throw new ArgumentException("Unknown start continuation connection.");
            PhysicalJunction region=null;value.JunctionIndex=-1;
            for(int i=0;i<junctions.Count;i++)if(junctions.Get(i).Id==connection.JunctionId){region=junctions.Get(i);value.JunctionIndex=i;}
            if(region==null||region.PortCount!=2)throw new ArgumentException("Start continuation requires exactly its two incident ports.");
            PhysicalJunctionPort from=null,to=null;
            for(int p=0;p<region.PortCount;p++)
            {var port=region.GetPort(p);if(port.Id==connection.FromPortId)from=port;if(port.Id==connection.ToPortId)to=port;}
            if(from==null||to==null||from.Anchor.LoopId!=sourceLoop.Id||from.Anchor.Distance.Millimeters!=sourceLoop.Length.Millimeters||
                to.Anchor.LoopId!=mainLoop.Id||to.Anchor.Distance.Millimeters!=0||from.GuardAfter.Millimeters!=0)
                throw new ArgumentException("Start continuation must bind the directed source endpoint to main zero.");
            if(value.Main.Length<to.GuardAfter.Meters+body.LongitudinalSeparation*.5)
                throw new ArgumentException("Outgoing authoritative geometry must cover the complete guarded body envelope.");
            if(mainLoop.Closed&&value.Main.Length+body.LongitudinalSeparation*.5>=mainLoop.Length.Meters-to.GuardBefore.Meters)
                throw new ArgumentException("The outgoing continuation prefix must end before the closed incoming terminal guard and its body envelope.");
            var a=value.Source.Get(value.Source.Count-1);var b=value.Main.Get(0);
            if(a.X!=b.X||a.Y!=b.Y||a.Z!=b.Z)throw new ArgumentException("The authoritative seam must coincide exactly; no free displacement is permitted.");
            var prior=value.Source.Get(value.Source.Count-2);var next=value.Main.Get(1);
            double ax=a.X-prior.X,az=a.Z-prior.Z,bx=next.X-b.X,bz=next.Z-b.Z;
            if(ax*bx+az*bz<=0||Math.Abs(ax*bz-az*bx)>1e-12*(Math.Abs(ax*bz)+Math.Abs(az*bx)))
                throw new ArgumentException("The start seam must preserve the exact lateral direction.");
            value.TrackSnapshotJson=snapshot;value.ApproachSnapshotJson=approach.ToJson();
            value.JunctionSnapshotJson=junctions.ToJson();value.ConnectionSnapshotJson=connections.ToJson();value.OccupancySnapshotJson=body.ToJson();
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);

        /// <summary>Checks every authored and rendering knot. Equal endpoint positions alone cannot hide a rendering shortcut.</summary>
        public bool MatchesPresentation(TrackVisualDefinition visual)
        {
            if(visual==null||visual.TrackId!=TrackId)return false;
            foreach(var corridor in corridors)
            {
                TrackVisualLoop loop;
                try{loop=visual.Get(corridor.LoopId);}catch(ArgumentException){return false;}
                if(loop.Get(loop.Count-1).Distance<corridor.Length)return false;
                for(int i=0;i<corridor.Count;i++)
                {
                    var point=corridor.Get(i);var rendered=loop.Sample(point.Distance);
                    if(!Near(point.X,point.Y,point.Z,rendered.X,rendered.Y,rendered.Z))return false;
                }
                for(int i=0;i<loop.Count;i++)
                {
                    var point=loop.Get(i);if(point.Distance>corridor.Length)break;
                    int e=1;while(e<corridor.Count-1&&corridor.Get(e).Distance<point.Distance)e++;
                    var a=corridor.Get(e-1);var b=corridor.Get(e);double t=(point.Distance-a.Distance)/(b.Distance-a.Distance);
                    if(!Near(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,point.X,point.Y,point.Z))return false;
                }
            }
            return true;
        }
        private bool Near(double ax,double ay,double az,double bx,double by,double bz)
        {double x=ax-bx,y=ay-by,z=az-bz;return Math.Sqrt(x*x+y*y+z*z)<=ValidationPolicy.SeamTolerance;}
    }
}
