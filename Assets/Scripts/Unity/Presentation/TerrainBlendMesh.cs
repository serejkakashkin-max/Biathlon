using System;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public static class TerrainBlendMesh
    {
        private readonly struct CorridorEdge
        {
            internal readonly Vector3 Start,Delta;internal readonly float LengthSquared,StartWidth,EndWidth;
            internal CorridorEdge(Vector3 start,Vector3 end,float startWidth,float endWidth)
            {start.y=end.y=0;Start=start;Delta=end-start;LengthSquared=Delta.sqrMagnitude;StartWidth=startWidth;EndWidth=endWidth;}
        }
        /// <summary>Cold tiled surface over independent world corridors. Never joins unrelated loops.</summary>
        public static Mesh Build(TerrainBlendCorridor[] corridors,Vector3 center,Vector2 size,float spacing,TerrainBlendDefinition definition,Func<Vector3,float> worldHeight,Func<Vector3,float> ice=null)
        {
            if(definition==null||corridors==null||corridors.Length<1||worldHeight==null||!float.IsFinite(center.x)||!float.IsFinite(center.y)||!float.IsFinite(center.z)||
                !float.IsFinite(size.x)||!float.IsFinite(size.y)||size.x<=0||size.y<=0||!float.IsFinite(spacing)||spacing<.1f)throw new ArgumentException("Invalid tiled terrain input.");
            foreach(var corridor in corridors){if(corridor==null)throw new ArgumentException("Null terrain corridor.");for(int i=0;i<corridor.Count;i++)definition.Weights(0,corridor.Width(i)*.5f,0);}
            var edgeList=new List<CorridorEdge>();
            foreach(var corridor in corridors)for(int r=1;r<corridor.Count;r++)
            {
                var a=corridor.Point(r-1);var b=corridor.Point(r);float reach=Mathf.Max(corridor.Width(r-1),corridor.Width(r))*.5f+definition.edge_width_m;
                // Edges outside the tile cannot contribute a packed/edge layer; retain each independent segment only.
                if(Mathf.Max(a.x,b.x)+reach<center.x-size.x*.5f||Mathf.Min(a.x,b.x)-reach>center.x+size.x*.5f||
                    Mathf.Max(a.z,b.z)+reach<center.z-size.y*.5f||Mathf.Min(a.z,b.z)-reach>center.z+size.y*.5f)continue;
                edgeList.Add(new CorridorEdge(a,b,corridor.Width(r-1)*.5f,corridor.Width(r)*.5f));
            }
            var edges=edgeList.ToArray();
            double nx=Math.Ceiling(size.x/spacing),nz=Math.Ceiling(size.y/spacing);if((nx+1)*(nz+1)>100000)throw new ArgumentException("Terrain authoring vertex budget exceeded.");
            int columns=(int)nx,rows=(int)nz;var vertices=new Vector3[(columns+1)*(rows+1)];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];var normals=new Vector3[vertices.Length];var indices=new int[columns*rows*6];int k=0;
            for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++)
            {
                var local=new Vector3((float)x/columns*size.x-size.x*.5f,0,(float)z/rows*size.y-size.y*.5f);var p=center+local;
                float bestEdge=float.PositiveInfinity,bestDistance=0,bestWidth=0,signed=0;
                for(int r=0;r<edges.Length;r++)
                {
                    var segment=edges[r];var horizontal=new Vector3(p.x,0,p.z);float t=Mathf.Clamp01(Vector3.Dot(horizontal-segment.Start,segment.Delta)/segment.LengthSquared);
                    var delta=horizontal-segment.Start-segment.Delta*t;float distance=delta.magnitude,halfWidth=Mathf.Lerp(segment.StartWidth,segment.EndWidth,t);
                    float edge=distance-halfWidth;if(edge<bestEdge){bestEdge=edge;bestDistance=distance;bestWidth=halfWidth;signed=Vector3.Cross(segment.Delta,delta).y<0?-distance:distance;}
                }
                float h=worldHeight(p);if(!float.IsFinite(h))throw new ArgumentException("Non-finite world terrain height.");int i=z*(columns+1)+x;
                float iceMask=ice==null?0:ice(p);
                colors[i]=edges.Length==0?definition.Weights(1000000,definition.edge_width_m,iceMask):definition.Weights(bestDistance,bestWidth,iceMask);
                local.y=h-center.y;vertices[i]=local;uv[i]=new Vector2(signed,p.z);
                // The same world samples on both sides of a tile edge produce identical normals.
                float left=worldHeight(p-Vector3.right*spacing),right=worldHeight(p+Vector3.right*spacing),back=worldHeight(p-Vector3.forward*spacing),front=worldHeight(p+Vector3.forward*spacing);
                if(!float.IsFinite(left)||!float.IsFinite(right)||!float.IsFinite(back)||!float.IsFinite(front))throw new ArgumentException("Non-finite terrain normal sample.");
                normals[i]=new Vector3(left-right,2*spacing,back-front).normalized;
                if(x==columns||z==rows)continue;int a1=i,b1=i+columns+1;indices[k++]=a1;indices[k++]=b1;indices[k++]=a1+1;indices[k++]=a1+1;indices[k++]=b1;indices[k++]=b1+1;
            }
            var mesh=new Mesh{name="Production tiled winter surface",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=indices;mesh.normals=normals;mesh.RecalculateBounds();return mesh;
        }
        /// <summary>Authoring-only shared surface, no colliders or authoritative surface mutation.
        /// Caller supplies readonly route points, height sampling and optional explicit visual ice mask.</summary>
        public static Mesh Build(Vector3[] route,float halfWidth,Vector2 size,float spacing,TerrainBlendDefinition definition,Func<Vector3,float> height=null,Func<Vector3,float> ice=null)
        {
            if(definition==null||route==null||route.Length<2||!float.IsFinite(size.x)||!float.IsFinite(size.y)||size.x<=0||size.y<=0||!float.IsFinite(spacing)||spacing<.1f)throw new ArgumentException("Invalid terrain mesh input.");
            foreach(var p in route)if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new ArgumentException("Non-finite route.");
            definition.Weights(0,halfWidth,0);
            double nx=Math.Ceiling(size.x/spacing),nz=Math.Ceiling(size.y/spacing);if((nx+1)*(nz+1)>100000)throw new ArgumentException("Terrain authoring vertex budget exceeded.");
            int columns=(int)nx,rows=(int)nz;var vertices=new Vector3[(columns+1)*(rows+1)];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];var indices=new int[columns*rows*6];int k=0;
            for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++)
            {
                var p=new Vector3((float)x/columns*size.x-size.x*.5f,0,(float)z/rows*size.y-size.y*.5f);float best=float.PositiveInfinity,signed=0;
                for(int r=1;r<route.Length;r++){var a=route[r-1];var b=route[r];a.y=b.y=0;var v=b-a;float t=v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude):0;var delta=p-a-v*t;float d=delta.magnitude;if(d<best){best=d;signed=Vector3.Cross(v,delta).y<0?-d:d;}}
                float h=height==null?0:height(p);if(!float.IsFinite(h))throw new ArgumentException("Non-finite terrain height.");
                int i=z*(columns+1)+x;colors[i]=definition.Weights(best,halfWidth,ice==null?0:ice(p));p.y=h;vertices[i]=p;uv[i]=new Vector2(signed,p.z);
                if(x==columns||z==rows)continue;int a1=i,b1=i+columns+1;indices[k++]=a1;indices[k++]=b1;indices[k++]=a1+1;indices[k++]=a1+1;indices[k++]=b1;indices[k++]=b1+1;
            }
            var mesh=new Mesh{name="Authored winter blend surface",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
