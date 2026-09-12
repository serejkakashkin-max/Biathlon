using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class TrackVisualKnot
    {
        [DataMember(Name="distance_m",IsRequired=true)] public double Distance { get; private set; }
        [DataMember(Name="x",IsRequired=true)] public double X { get; private set; }
        [DataMember(Name="y",IsRequired=true)] public double Y { get; private set; }
        [DataMember(Name="z",IsRequired=true)] public double Z { get; private set; }
    }
    public readonly struct TrackVisualPoint
    {
        public double X { get; } public double Y { get; } public double Z { get; }
        public double ForwardX { get; } public double ForwardZ { get; }
        internal TrackVisualPoint(TrackVisualKnot a,TrackVisualKnot b,double fraction)
        {X=a.X+(b.X-a.X)*fraction;Y=a.Y+(b.Y-a.Y)*fraction;Z=a.Z+(b.Z-a.Z)*fraction;double dx=b.X-a.X,dz=b.Z-a.Z,n=Math.Sqrt(dx*dx+dz*dz);ForwardX=dx/n;ForwardZ=dz/n;}
    }
    [DataContract] public sealed class TrackVisualLoop
    {
        [DataMember(Name="loop_id",IsRequired=true)] public string LoopId { get; private set; }
        [DataMember(Name="knots",IsRequired=true)] private TrackVisualKnot[] knots { get; set; }
        public int Count=>knots.Length;public TrackVisualKnot Get(int index)=>knots[index];
        public TrackVisualPoint Sample(double distance)
        {
            if(double.IsNaN(distance)||double.IsInfinity(distance)||distance<0||distance>knots[knots.Length-1].Distance)throw new ArgumentOutOfRangeException(nameof(distance));
            int i=1;while(i<knots.Length-1&&knots[i].Distance<=distance)i++;return new TrackVisualPoint(knots[i-1],knots[i],(distance-knots[i-1].Distance)/(knots[i].Distance-knots[i-1].Distance));
        }
        internal void Validate(TrackDefinition track)
        {
            var loop=track.GetLoop(LoopId);if(knots==null||knots.Length<2||knots.Length>10000)throw new SerializationException("Invalid visual knots.");
            double previous=-1;TrackVisualKnot prior=null;
            foreach(var knot in knots)
            {
                if(knot==null)throw new SerializationException("Null visual knot.");TrackValidation.Finite(knot.Distance,0,loop.Length.Meters,"visual distance");
                TrackValidation.Finite(knot.X,-1000000,1000000,"x");TrackValidation.Finite(knot.Y,-1000000,1000000,"y");TrackValidation.Finite(knot.Z,-1000000,1000000,"z");
                if(knot.Distance<=previous||(prior!=null&&knot.X==prior.X&&knot.Z==prior.Z))throw new SerializationException("Visual knots must advance and have a horizontal tangent.");previous=knot.Distance;prior=knot;
            }
            if(knots[0].Distance!=0||previous!=loop.Length.Meters)throw new SerializationException("Visual loop must span the authoritative loop distance.");
            var first=knots[0];var last=knots[knots.Length-1];if(loop.Closed&&(first.X!=last.X||first.Y!=last.Y||first.Z!=last.Z))throw new SerializationException("Closed visual loop has an open seam.");
        }
    }
    [DataContract] public sealed class TrackVisualDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId { get; private set; }
        [DataMember(Name="sample_spacing_m",IsRequired=true)] public double SampleSpacing { get; private set; }
        [DataMember(Name="loops",IsRequired=true)] private TrackVisualLoop[] loops { get; set; }
        public int Count=>loops.Length;public TrackVisualLoop Get(int index)=>loops[index];
        public TrackVisualLoop Get(string id){foreach(var loop in loops)if(loop.LoopId==id)return loop;throw new ArgumentException("Missing visual loop.");}
        public static TrackVisualDefinition FromJson(string json,TrackDefinition track)
        {
            if(track==null)throw new ArgumentNullException(nameof(track));var d=RouteJson.Read<TrackVisualDefinition>(json);
            if(d==null||d.SchemaVersion!=1||d.TrackId!=track.Id||d.loops==null||d.loops.Length<1||d.loops.Length>track.LoopCount)throw new SerializationException("Visual track mismatch.");TrackValidation.Id(d.Id);TrackValidation.Finite(d.SampleSpacing,0.1,100,"sample spacing");
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var loop in d.loops){if(loop==null||!ids.Add(loop.LoopId))throw new SerializationException("Duplicate visual loop.");loop.Validate(track);}return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}
