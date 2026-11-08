using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only height of the actual drawn snow, including decorative banks.
    /// Built once; queries use cached triangles and never enter gameplay physics.</summary>
    public sealed class RenderedSnowSurface
    {
        private readonly struct Triangle
        {
            private readonly Vector3 a,b,c;
            private readonly float inverse;
            public Triangle(Vector3 a,Vector3 b,Vector3 c,float area){this.a=a;this.b=b;this.c=c;inverse=1/area;}
            public bool Height(Vector3 p,out float height)
            {
                float x=p.x-a.x,z=p.z-a.z;
                float u=(x*(c.z-a.z)-z*(c.x-a.x))*inverse;
                float v=((b.x-a.x)*z-(b.z-a.z)*x)*inverse;
                const float precision=1e-5f;
                height=a.y+u*(b.y-a.y)+v*(c.y-a.y);
                return u>=-precision&&v>=-precision&&u+v<=1+precision;
            }
        }
        private readonly Dictionary<long,Triangle[]> cells=new Dictionary<long,Triangle[]>();
        private readonly float cellSize;
        private readonly Func<Vector3,float> fallback;
        public int TriangleCount {get;private set;}
        public RenderedSnowSurface(IEnumerable<MeshFilter> filters,float cellSize,Func<Vector3,float> fallback)
        {
            if(filters==null||fallback==null||!float.IsFinite(cellSize)||cellSize<=0)throw new ArgumentException("Snow meshes, grid spacing and terrain fallback required.");
            this.cellSize=cellSize;this.fallback=fallback;
            var buckets=new Dictionary<long,List<Triangle>>();
            foreach(var filter in filters)
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
                var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;var matrix=filter.transform.localToWorldMatrix;
                for(int index=0;index<indices.Length;index+=3)
                {
                    var a=matrix.MultiplyPoint3x4(vertices[indices[index]]);var b=matrix.MultiplyPoint3x4(vertices[indices[index+1]]);var c=matrix.MultiplyPoint3x4(vertices[indices[index+2]]);
                    float area=(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
                    if(Mathf.Abs(area)<1e-10f)continue;
                    var triangle=new Triangle(a,b,c,area);TriangleCount++;
                    int minX=Cell(Mathf.Min(a.x,Mathf.Min(b.x,c.x))),maxX=Cell(Mathf.Max(a.x,Mathf.Max(b.x,c.x)));
                    int minZ=Cell(Mathf.Min(a.z,Mathf.Min(b.z,c.z))),maxZ=Cell(Mathf.Max(a.z,Mathf.Max(b.z,c.z)));
                    for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
                    {
                        long key=Key(x,z);if(!buckets.TryGetValue(key,out var list)){list=new List<Triangle>();buckets.Add(key,list);}list.Add(triangle);
                    }
                }
            }
            foreach(var pair in buckets)cells.Add(pair.Key,pair.Value.ToArray());
        }
        private int Cell(float value)=>Mathf.FloorToInt(value/cellSize);
        private static long Key(int x,int z)=>((long)x<<32)|(uint)z;
        public float Height(Vector3 point)
        {
            float height=fallback(point);
            if(cells.TryGetValue(Key(Cell(point.x),Cell(point.z)),out var triangles))
                foreach(var triangle in triangles)if(triangle.Height(point,out float y))height=Mathf.Max(height,y);
            return height;
        }
    }
}
