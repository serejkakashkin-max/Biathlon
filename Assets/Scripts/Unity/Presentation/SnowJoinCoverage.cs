using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold exact-plane coverage of coincident axis-aligned curtains at connected
    /// route cuts. Removes a face only when earlier same-facing faces cover its whole area.
    /// Partial, opposite-facing and offset faces remain; no boundary is inferred from names.</summary>
    public sealed class SnowJoinCoverage
    {
        private readonly Dictionary<(int,float,bool,int),List<Face>> cells=new Dictionary<(int,float,bool,int),List<Face>>();
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
