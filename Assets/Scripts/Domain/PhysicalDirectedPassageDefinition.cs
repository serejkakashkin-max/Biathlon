using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Bounded authoritative centreline window in native physical-loop coordinates.</summary>
    [DataContract] public sealed class PhysicalPassageCorridor
    {
        [DataMember(Name="loop_id",IsRequired=true)] public string LoopId { get; private set; }
        [DataMember(Name="knots",IsRequired=true)] private PhysicalCorridorKnot[] knots { get; set; }
        public int Count=>knots.Length;
        public PhysicalCorridorKnot Get(int index)=>knots[index];
        public double Start=>knots[0].Distance;
        public double End=>knots[knots.Length-1].Distance;
        public static PhysicalPassageCorridor CaptureFullLoop(TrackVisualLoop visual,TrackDefinition track,VenueValidationPolicy policy)
        {
            if(visual==null||track==null||policy==null)throw new ArgumentNullException();
            var c=new PhysicalPassageCorridor{LoopId=visual.LoopId,knots=new PhysicalCorridorKnot[visual.Count]};
            for(int i=0;i<c.Count;i++)c.knots[i]=PhysicalCorridorKnot.Capture(visual.Get(i));
            c.Validate(track,policy);
            if(c.Start!=0||c.End!=track.GetLoop(c.LoopId).Length.Meters)throw new ArgumentException("Complete paid-loop geometry required.");
            return c;
        }
        internal void Validate(TrackDefinition track,VenueValidationPolicy policy)
        {
            TrackValidation.Id(LoopId);var loop=track.GetLoop(LoopId);
            if(knots==null||knots.Length<2||knots.Length>10000)throw new ArgumentException("A bounded passage corridor is required.");
            long previous=-1;
            for(int i=0;i<Count;i++)
            {
                var b=Get(i);if(b==null)throw new ArgumentException("Null passage knot.");
                TrackValidation.Finite(b.Distance,0,loop.Length.Meters,"passage distance");
                var coordinate=TrackDistance.FromMeters(b.Distance);
                if(coordinate.Millimeters<=previous)throw new ArgumentException("Passage knots must strictly advance whole-millimetre coordinates.");
                b.BindDistance(coordinate);
                TrackValidation.Finite(b.X,-1000000,1000000,"passage x");TrackValidation.Finite(b.Y,-1000000,1000000,"passage y");TrackValidation.Finite(b.Z,-1000000,1000000,"passage z");
                if(i>0)
                {
                    var a=Get(i-1);double x=b.X-a.X,y=b.Y-a.Y,z=b.Z-a.Z;
                    double horizontal=Magnitude(x,0,z),world=Magnitude(x,y,z),declared=b.Distance-a.Distance;
                    if(horizontal==0||Math.Abs(world-declared)>policy.DistanceAbsolute+policy.DistanceRelative*declared)
                        throw new ArgumentException("Passage world distance must match authoritative travel.");
                    if(Math.Abs(y/horizontal)>policy.MaxGrade)throw new ArgumentException("Passage world grade exceeds policy.");
                }
                previous=coordinate.Millimeters;
            }
        }
        private static double Magnitude(double x,double y,double z)
        {double scale=Math.Max(Math.Abs(x),Math.Max(Math.Abs(y),Math.Abs(z)));if(scale==0)return 0;x/=scale;y/=scale;z/=scale;return scale*Math.Sqrt(x*x+y*y+z*z);}
    }

    [DataContract] public sealed class PhysicalDirectedPassage
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="connection_id",IsRequired=true)] public string ConnectionId { get; private set; }
        [DataMember(Name="source",IsRequired=true)] public PhysicalPassageCorridor Source { get; private set; }
        [DataMember(Name="target",IsRequired=true)] public PhysicalPassageCorridor Target { get; private set; }
        public int JunctionIndex { get; internal set; }
        public int ConnectionIndex { get; internal set; }
    }

    /// <summary>Optional directed-passage authority. Membership alone never grants compatibility.
    /// Exact route occurrences and complete swept-body/tail proofs remain required by the field.</summary>
    [DataContract] public sealed class PhysicalDirectedPassageDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId { get; private set; }
        [DataMember(Name="route_id",IsRequired=true)] public string RouteId { get; private set; }
        [DataMember(Name="occupancy_definition_id",IsRequired=true)] public string OccupancyId { get; private set; }
        [DataMember(Name="validation_policy_resource",IsRequired=true)] public string ValidationPolicyResource { get; private set; }
        [DataMember(Name="passages",IsRequired=true)] private PhysicalDirectedPassage[] passages { get; set; }
        public int Count=>passages.Length;
        public PhysicalDirectedPassage Get(int index)=>passages[index];
        public VenueValidationPolicy ValidationPolicy { get; private set; }
        public string TrackSnapshotJson { get; private set; }
        public string RoutePlanSnapshotJson { get; private set; }
        public string JunctionSnapshotJson { get; private set; }
        public string ConnectionSnapshotJson { get; private set; }
        public string OccupancySnapshotJson { get; private set; }

        public static PhysicalDirectedPassageDefinition FromJson(string json,TrackDefinition track,RaceRoutePlan route,
            PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,OccupancyDefinition body,Func<string,string> readResource)
        {
            if(track==null||route==null||junctions==null||connections==null||body==null||readResource==null)throw new ArgumentNullException();
            var value=RouteJson.Read<PhysicalDirectedPassageDefinition>(json);
            // The initial policy has exactly the explicit format departure and return.
            if(value==null||value.SchemaVersion!=1||value.passages==null||value.Count!=2)
                throw new SerializationException("Exactly two explicit directed format passages are required.");
            TrackValidation.Id(value.Id);TrackValidation.Id(value.TrackId);TrackValidation.Id(value.RouteId);TrackValidation.Id(value.OccupancyId);VenueManifest.ResourceId(value.ValidationPolicyResource);
            string snapshot=track.ToJson();
            if(route.SchemaVersion!=2||value.TrackId!=track.Id||route.TrackId!=track.Id||value.RouteId!=route.Id||value.OccupancyId!=body.Id||
                junctions.TrackSnapshotJson!=snapshot||connections.TrackSnapshotJson!=snapshot||connections.JunctionSnapshotJson!=junctions.ToJson())
                throw new ArgumentException("Exact directed passage route, track, topology and body binding required.");
            value.ValidationPolicy=VenueValidationPolicy.FromJson(readResource(value.ValidationPolicyResource));
            var ids=new HashSet<string>(StringComparer.Ordinal);var regions=new HashSet<int>();
            foreach(var passage in value.passages)
            {
                if(passage==null||passage.Source==null||passage.Target==null)throw new ArgumentException("Null passage corridor.");
                TrackValidation.Id(passage.Id);TrackValidation.Id(passage.ConnectionId);
                if(!ids.Add(passage.Id))throw new ArgumentException("Duplicate passage identity.");
                passage.Source.Validate(track,value.ValidationPolicy);passage.Target.Validate(track,value.ValidationPolicy);
                if(passage.Source.LoopId==passage.Target.LoopId||track.GetLoop(passage.Source.LoopId).Kind!=TrackLoopKind.Race||track.GetLoop(passage.Target.LoopId).Kind!=TrackLoopKind.Race)
                    throw new ArgumentException("A directed passage requires distinct race-loop corridors; self crossings remain exclusive.");
                passage.ConnectionIndex=-1;
                for(int c=0;c<connections.Count;c++)if(connections.Get(c).Id==passage.ConnectionId)passage.ConnectionIndex=c;
                if(passage.ConnectionIndex<0)throw new ArgumentException("Unknown passage connection.");
                var connection=connections.Get(passage.ConnectionIndex);passage.JunctionIndex=-1;
                for(int j=0;j<junctions.Count;j++)if(junctions.Get(j).Id==connection.JunctionId)passage.JunctionIndex=j;
                if(passage.JunctionIndex<0||!regions.Add(passage.JunctionIndex))throw new ArgumentException("One distinct region is required per passage.");
                var region=junctions.Get(passage.JunctionIndex);
                if(region.PortCount!=2)throw new ArgumentException("Only an exact two-port directed passage may share a world proof.");
                PhysicalJunctionPort from=null,to=null;
                for(int p=0;p<region.PortCount;p++){var port=region.GetPort(p);if(port.Id==connection.FromPortId)from=port;if(port.Id==connection.ToPortId)to=port;}
                if(from==null||to==null||from.Anchor.LoopId!=passage.Source.LoopId||to.Anchor.LoopId!=passage.Target.LoopId||
                    from.Anchor.Distance.Meters!=passage.Source.End||to.Anchor.Distance.Meters!=passage.Target.Start)
                    throw new ArgumentException("Corridors must end/start at the exact directed seam anchors.");
                double half=body.LongitudinalSeparation*.5;
                if(passage.Source.Start>from.Anchor.Distance.Meters-from.GuardBefore.Meters-half||passage.Target.End<to.Anchor.Distance.Meters+to.GuardAfter.Meters+half)
                    throw new ArgumentException("Corridors must cover complete incident guards and body extent.");
                // Closed terminal images and open wrapping cannot enter this bounded proof.
                if(track.GetLoop(passage.Source.LoopId).Closed&&passage.Source.Start==0||track.GetLoop(passage.Target.LoopId).Closed&&passage.Target.End==track.GetLoop(passage.Target.LoopId).Length.Meters)
                    throw new ArgumentException("Closed terminal seams remain outside directed passage windows.");
                var a=passage.Source.Get(passage.Source.Count-1);var b=passage.Target.Get(0);
                if(a.X!=b.X||a.Y!=b.Y||a.Z!=b.Z)throw new ArgumentException("A passage seam must coincide exactly.");
                var prior=passage.Source.Get(passage.Source.Count-2);var next=passage.Target.Get(1);
                double ax=a.X-prior.X,az=a.Z-prior.Z,bx=next.X-b.X,bz=next.Z-b.Z;
                if(ax*bx+az*bz<=0||Math.Abs(ax*bz-az*bx)>1e-12*(Math.Abs(ax*bz)+Math.Abs(az*bx)))
                    throw new ArgumentException("A directed seam must preserve exact lateral direction.");
                ValidateSimplePassage(passage);
                bool used=false;
                for(int l=0;l<route.LapCount;l++)
                {
                    var lap=route.GetLap(l);
                    for(int s=1;s<lap.LoopCount;s++)
                    {
                        var incoming=lap.GetSlice(s-1);var outgoing=lap.GetSlice(s);
                        if(outgoing.EnterConnectionId!=passage.ConnectionId)continue;
                        if(incoming.LoopId!=passage.Source.LoopId||outgoing.LoopId!=passage.Target.LoopId||incoming.End.Meters!=passage.Source.End||outgoing.Start.Meters!=passage.Target.Start||
                            incoming.Start.Meters>passage.Source.Start||outgoing.End.Meters<passage.Target.End)
                            throw new ArgumentException("Every named route occurrence must cover the exact passage windows and direction.");
                        used=true;
                    }
                }
                if(!used)throw new ArgumentException("The exact route must use each passage connection.");
            }
            if(value.Get(0).Source.LoopId!=value.Get(1).Target.LoopId||value.Get(0).Target.LoopId!=value.Get(1).Source.LoopId)
                throw new ArgumentException("The initial pair must explicitly depart from and return to the same two physical race loops.");
            value.TrackSnapshotJson=snapshot;value.RoutePlanSnapshotJson=route.ToJson();value.JunctionSnapshotJson=junctions.ToJson();
            value.ConnectionSnapshotJson=connections.ToJson();value.OccupancySnapshotJson=body.ToJson();return value;
        }
        public string ToJson()=>RouteJson.Write(this);
        private static void ValidateSimplePassage(PhysicalDirectedPassage passage)
        {
            CheckSelf(passage.Source);CheckSelf(passage.Target);
            for(int a=1;a<passage.Source.Count;a++)for(int b=1;b<passage.Target.Count;b++)
            {
                // The exact directed seam was checked above; its two incident edges are
                // collinear, forward and meet only at their shared endpoint.
                if(a==passage.Source.Count-1&&b==1)continue;
                if(Intersects(passage.Source.Get(a-1),passage.Source.Get(a),passage.Target.Get(b-1),passage.Target.Get(b)))
                    throw new ArgumentException("Nonincident passage corridors must not cross or touch.");
            }
            void CheckSelf(PhysicalPassageCorridor corridor)
            {
                for(int a=1;a<corridor.Count;a++)for(int b=a+1;b<corridor.Count;b++)
                {
                    var p=corridor.Get(a-1);var q=corridor.Get(a);var r=corridor.Get(b-1);var s=corridor.Get(b);
                    if(b==a+1)
                    {
                        if(Orientation(p,q,s)==0&&(q.X-p.X)*(s.X-q.X)+(q.Z-p.Z)*(s.Z-q.Z)<=0)
                            throw new ArgumentException("Adjacent passage edges must not double back.");
                        continue;
                    }
                    if(Intersects(p,q,r,s))throw new ArgumentException("A passage corridor must not cross or touch itself.");
                }
            }
        }
        private static bool Intersects(PhysicalCorridorKnot a,PhysicalCorridorKnot b,PhysicalCorridorKnot c,PhysicalCorridorKnot d)
        {
            double scale=Math.Max(Math.Max(Math.Max(Math.Abs(a.X),Math.Abs(a.Z)),Math.Max(Math.Abs(b.X),Math.Abs(b.Z))),
                Math.Max(Math.Max(Math.Abs(c.X),Math.Abs(c.Z)),Math.Max(Math.Abs(d.X),Math.Abs(d.Z))));
            double reserve=32*2.2204460492503131e-16*(scale+1);
            if(Math.Max(a.X,b.X)+reserve<Math.Min(c.X,d.X)-reserve||Math.Max(c.X,d.X)+reserve<Math.Min(a.X,b.X)-reserve||
                Math.Max(a.Z,b.Z)+reserve<Math.Min(c.Z,d.Z)-reserve||Math.Max(c.Z,d.Z)+reserve<Math.Min(a.Z,b.Z)-reserve)return false;
            int abC=Orientation(a,b,c),abD=Orientation(a,b,d),cdA=Orientation(c,d,a),cdB=Orientation(c,d,b);
            return abC*abD<=0&&cdA*cdB<=0;
        }
        private static int Orientation(PhysicalCorridorKnot a,PhysicalCorridorKnot b,PhysicalCorridorKnot c)
        {
            double first=(b.X-a.X)*(c.Z-a.Z),second=(b.Z-a.Z)*(c.X-a.X),cross=first-second;
            double reserve=32*2.2204460492503131e-16*(Math.Abs(first)+Math.Abs(second)+1);
            return cross>reserve?1:cross< -reserve?-1:0;
        }
        public bool MatchesPresentation(TrackVisualDefinition visual)
        {
            if(visual==null||visual.TrackId!=TrackId)return false;
            foreach(var passage in passages)if(!Matches(passage.Source)||!Matches(passage.Target))return false;
            return true;
            bool Matches(PhysicalPassageCorridor corridor)
            {
                TrackVisualLoop loop;try{loop=visual.Get(corridor.LoopId);}catch(ArgumentException){return false;}
                if(loop.Get(loop.Count-1).Distance<corridor.End)return false;
                for(int i=0;i<corridor.Count;i++){var p=corridor.Get(i);var q=loop.Sample(p.Distance);if(!Near(p.X,p.Y,p.Z,q.X,q.Y,q.Z))return false;}
                for(int i=0;i<loop.Count;i++)
                {
                    var p=loop.Get(i);if(p.Distance<corridor.Start)continue;if(p.Distance>corridor.End)break;
                    int e=1;while(e<corridor.Count-1&&corridor.Get(e).Distance<p.Distance)e++;
                    var a=corridor.Get(e-1);var b=corridor.Get(e);double t=(p.Distance-a.Distance)/(b.Distance-a.Distance);
                    if(!Near(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,p.X,p.Y,p.Z))return false;
                }
                return true;
            }
        }
        private bool Near(double ax,double ay,double az,double bx,double by,double bz)
        {double x=ax-bx,y=ay-by,z=az-bz;return Math.Sqrt(x*x+y*y+z*z)<=ValidationPolicy.SeamTolerance;}
    }
}
