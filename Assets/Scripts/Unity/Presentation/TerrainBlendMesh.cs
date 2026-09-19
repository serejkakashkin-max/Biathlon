using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public static class TerrainBlendMesh
    {
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
