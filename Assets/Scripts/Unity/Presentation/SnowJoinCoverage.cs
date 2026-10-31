using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold coverage of connected snow joins: complete same-plane, same-facing
    /// curtain coverage, and complete two-sided roof coverage of interior curtains.
    /// Partial/exposed joins remain; no boundary is inferred from names.</summary>
    public sealed class SnowJoinCoverage
    {
        private readonly Dictionary<(int,float,bool,int),List<Face>> cells=new Dictionary<(int,float,bool,int),List<Face>>();
        private readonly Dictionary<(long,long),List<RoofEdge>> roofs=new Dictionary<(long,long),List<RoofEdge>>();
        private double horizontalRoundoff;
        private readonly struct RoofEdge
        {
            internal readonly Vector2 A,B;
            internal readonly double First,Last,Side;
            internal RoofEdge(Vector2 a,Vector2 b,double first,double last,double side){A=a;B=b;First=first;Last=last;Side=side;}
        }
        /// <summary>Index generated snow roofs once, before filtering their shared internal
        /// curtains. Projected endpoints match at the native float precision of the
        /// generated venue extent; complete endpoint height inequalities
        /// certify both sides; an offset, partial, lower or single-sided roof cannot hide
        /// an exposed wall. No approximate position welding or roof modification.</summary>
        public void CaptureRoofs(IEnumerable<Mesh> meshes)
        {
            roofs.Clear();var pending=new List<RoofEdge>();float extent=0;foreach(var mesh in meshes)
            {
                if(mesh==null)continue;var p=mesh.vertices;var faces=mesh.triangles;
                foreach(var point in p)extent=Mathf.Max(extent,Mathf.Max(Mathf.Abs(point.x),Mathf.Abs(point.z)));
                for(int i=0;i<faces.Length;i+=3)
                {
                    var a=p[faces[i]];var b=p[faces[i+1]];var c=p[faces[i+2]];
                    if(Vector3.Cross(b-a,c-a).y<=1e-12f)continue;
                    Add(a,b,c);Add(b,c,a);Add(c,a,b);
                }
            }
            horizontalRoundoff=(double)BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(extent)+1)-extent;
            foreach(var edge in pending){Index(edge.A,edge);Index(edge.B,edge);}
            void Add(Vector3 a,Vector3 b,Vector3 third)
            {
                var first=new Vector2(a.x,a.z);var last=new Vector2(b.x,b.z);
                if(first.Equals(last))return;
                if(After(first,last)){var swap=first;first=last;last=swap;var point=a;a=b;b=point;}
                double side=((double)last.x-first.x)*(third.z-first.y)-((double)last.y-first.y)*(third.x-first.x);
                pending.Add(new RoofEdge(first,last,a.y,b.y,side));
            }
            void Index(Vector2 point,RoofEdge edge)
            {var key=RoofCell(point);if(!roofs.TryGetValue(key,out var list)){list=new List<RoofEdge>();roofs.Add(key,list);}list.Add(edge);}
        }
        public Mesh RetainExteriorCurtains(Mesh source,out int removed)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var p=source.vertices;var faces=source.triangles;var kept=new List<int>(faces.Length);removed=0;
            for(int i=0;i<faces.Length;i+=3)
            {
                var a=p[faces[i]];var b=p[faces[i+1]];var c=p[faces[i+2]];
                if(Interior(a,b,c)){removed++;continue;}
                kept.Add(faces[i]);kept.Add(faces[i+1]);kept.Add(faces[i+2]);
            }
            if(removed==0)return source;
            var result=new Mesh{name=source.name+" exterior curtains",indexFormat=source.indexFormat,
                vertices=p,normals=source.normals,uv=source.uv,colors=source.colors,triangles=kept.ToArray()};
            result.RecalculateBounds();return result;
            bool Interior(Vector3 a,Vector3 b,Vector3 c)
            {
                // Curtain triangles have exactly two distinct projected positions.
                // The complete vertical face lies below the two endpoint maxima.
                var pa=new Vector2(a.x,a.z);var pb=new Vector2(b.x,b.z);var pc=new Vector2(c.x,c.z);
                Vector2 first,last;
                if(pa.Equals(pb)){first=pa;last=pc;}else if(pa.Equals(pc)||pb.Equals(pc)){first=pa;last=pb;}else return false;
                if(first.Equals(last))return false;if(After(first,last)){var swap=first;first=last;last=swap;}
                if(horizontalRoundoff<=0)return false;
                double firstHeight=double.NegativeInfinity,lastHeight=double.NegativeInfinity;
                Height(pa,a.y);Height(pb,b.y);Height(pc,c.y);
                bool left=false,right=false;
                // Independently clipped float roof faces can differ by the last
                // representable height bit at an otherwise exact XZ seam. Admit
                // one downward float step, never a geometric clearance epsilon.
                var cell=RoofCell(first);
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                if(roofs.TryGetValue((cell.Item1+x,cell.Item2+z),out var candidates))foreach(var roof in candidates)
                {
                    bool forward=Near(first,roof.A)&&Near(last,roof.B),backward=Near(first,roof.B)&&Near(last,roof.A);
                    if(!forward&&!backward)continue;
                    double firstY=forward?roof.First:roof.Last,lastY=forward?roof.Last:roof.First,side=forward?roof.Side:-roof.Side;
                    if(firstY<PreviousFloat(firstHeight)||lastY<PreviousFloat(lastHeight))continue;
                    left|=side>0;right|=side<0;if(left&&right)return true;
                }
                return false;
                void Height(Vector2 point,double height){if(point.Equals(first))firstHeight=System.Math.Max(firstHeight,height);else lastHeight=System.Math.Max(lastHeight,height);}
            }
        }
        private (long,long) RoofCell(Vector2 point)=>((long)Math.Floor(point.x/horizontalRoundoff),(long)Math.Floor(point.y/horizontalRoundoff));
        private bool Near(Vector2 a,Vector2 b)=>Math.Abs((double)a.x-b.x)<=horizontalRoundoff&&Math.Abs((double)a.y-b.y)<=horizontalRoundoff;
        private static double PreviousFloat(double height)
        {
            float value=(float)height;if(value==0)return -float.Epsilon;
            return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value)+(value>0?-1:1));
        }
        private static bool After(Vector2 a,Vector2 b)=>a.x>b.x||a.x==b.x&&a.y>b.y;
        private readonly struct P
        {
            internal readonly double X,Y;
            internal P(double x,double y){X=x;Y=y;}
            internal static P Lerp(P a,P b,double t)=>new P(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        }
        private sealed class Face
        {
            internal P[] Points;internal int Axis;internal float Plane;internal bool Positive;internal double Low,High;
        }
        public Mesh RetainUncovered(Mesh source,out int removed)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var p=source.vertices;var indices=source.triangles;var kept=new List<int>(indices.Length);removed=0;
            for(int i=0;i<indices.Length;i+=3)
            {
                var face=Project(p[indices[i]],p[indices[i+1]],p[indices[i+2]]);
                bool covered=face!=null&&Covered(face);
                if(covered){removed++;continue;}
                kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);
                if(face!=null)for(int cell=Cell(face.Low);cell<=Cell(face.High);cell++)
                {var key=(face.Axis,face.Plane,face.Positive,cell);if(!cells.TryGetValue(key,out var list)){list=new List<Face>();cells.Add(key,list);}list.Add(face);}
            }
            if(removed==0)return source;
            var result=new Mesh{name=source.name+" certified curtain coverage",indexFormat=source.indexFormat,
                vertices=p,normals=source.normals,uv=source.uv,colors=source.colors,triangles=kept.ToArray()};
            result.RecalculateBounds();return result;
        }
        private bool Covered(Face face)
        {
            var remaining=new List<List<P>>{new List<P>(face.Points)};var seen=new HashSet<Face>();
            double original=Area(remaining[0]);
            for(int cell=Cell(face.Low);cell<=Cell(face.High);cell++)
            if(cells.TryGetValue((face.Axis,face.Plane,face.Positive,cell),out var list))foreach(var other in list)
            {
                if(!seen.Add(other)||other.High<face.Low||other.Low>face.High)continue;
                var next=new List<List<P>>();double sign=Cross(other.Points[0],other.Points[1],other.Points[2])>0?1:-1;
                foreach(var piece in remaining)
                {
                    var inside=piece;
                    for(int edge=0;edge<3&&inside.Count>0;edge++)
                    {
                        var a=other.Points[edge];var b=other.Points[(edge+1)%3];
                        var outside=Clip(inside,a,b,-sign);if(Area(outside)>0)next.Add(outside);
                        inside=Clip(inside,a,b,sign);
                    }
                }
                remaining=next;double uncovered=0;foreach(var piece in remaining)uncovered+=Area(piece);
                // Double-precision clipping roundoff only; below the rendered float
                // precision, and much stricter than the independent 0.1mm contact bound.
                if(uncovered<=Math.Max(1e-12,original*1e-10))return true;
            }
            return false;
        }
        private static int Cell(double value)=>(int)Math.Floor(value/8);
        private static Face Project(Vector3 a,Vector3 b,Vector3 c)
        {
            int axis=a.x==b.x&&a.x==c.x?0:a.z==b.z&&a.z==c.z?2:-1;if(axis<0)return null;
            var normal=Vector3.Cross(b-a,c-a);if(normal.sqrMagnitude<1e-16f)return null;
            var p=new[]{new P(axis==0?a.z:a.x,a.y),new P(axis==0?b.z:b.x,b.y),new P(axis==0?c.z:c.x,c.y)};
            return new Face{Points=p,Axis=axis,Plane=axis==0?a.x:a.z,Positive=axis==0?normal.x>0:normal.z>0,
                Low=Math.Min(p[0].X,Math.Min(p[1].X,p[2].X)),High=Math.Max(p[0].X,Math.Max(p[1].X,p[2].X))};
        }
        private static double Cross(P a,P b,P c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
        private static double Area(List<P> p){double area=0;for(int i=1;i+1<p.Count;i++)area+=Math.Abs(Cross(p[0],p[i],p[i+1]));return area;}
        private static List<P> Clip(List<P> p,P a,P b,double sign)
        {
            var result=new List<P>();if(p.Count==0)return result;var prior=p[p.Count-1];double previous=sign*Cross(a,b,prior);
            foreach(var point in p){double value=sign*Cross(a,b,point);if((previous>=0)!=(value>=0))result.Add(P.Lerp(prior,point,previous/(previous-value)));if(value>=0)result.Add(point);prior=point;previous=value;}
            return result;
        }
    }
}
