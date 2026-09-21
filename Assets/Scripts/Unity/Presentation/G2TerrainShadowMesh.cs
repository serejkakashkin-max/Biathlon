using System;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Authoring-only regular-grid reduction; fine rows/columns preserve route-bank shadows.</summary>
    public static class G2TerrainShadowMesh
    {
        public static Mesh Build(Mesh visible,Vector3[] route)
        {
            if(visible==null||route==null||route.Length<2)throw new ArgumentException("Visible surface and route required.");
            var source=visible.vertices;var normals=visible.normals;var colors=visible.colors;var uv=visible.uv;
            int columns=1;while(columns<source.Length&&source[columns].z==source[0].z)columns++;
            if(columns<2||source.Length%columns!=0)throw new ArgumentException("Regular surface grid required.");int rows=source.Length/columns;
            var xs=new List<int>();var zs=new List<int>();
            for(int x=0;x<columns;x++)
            {
                bool fine=false;foreach(var p in route)if(Mathf.Abs(source[x].x-p.x)<=9)fine=true;
                if(x%2==0||x==columns-1||fine)xs.Add(x);
            }
            for(int z=0;z<rows;z++)
            {
                float position=source[z*columns].z;bool fine=position>=-20&&position<=-10;
                foreach(var p in route)if(Mathf.Abs(position-p.z)<=9)fine=true;
                if(z%2==0||z==rows-1||fine)zs.Add(z);
            }
            int count=xs.Count*zs.Count;var vertices=new Vector3[count];var outNormals=new Vector3[count];var outColors=new Color[count];var outUv=new Vector2[count];
            var indices=new int[(xs.Count-1)*(zs.Count-1)*6];int k=0;
            for(int z=0;z<zs.Count;z++)for(int x=0;x<xs.Count;x++)
            {
                int sourceIndex=zs[z]*columns+xs[x],i=z*xs.Count+x;vertices[i]=source[sourceIndex];outNormals[i]=normals[sourceIndex];outColors[i]=colors[sourceIndex];outUv[i]=uv[sourceIndex];
                if(x==xs.Count-1||z==zs.Count-1)continue;int b=i+xs.Count;
                indices[k++]=i;indices[k++]=b;indices[k++]=i+1;indices[k++]=i+1;indices[k++]=b;indices[k++]=b+1;
            }
            var result=new Mesh{name="Adaptive terrain shadow surface",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=vertices,normals=outNormals,colors=outColors,uv=outUv,triangles=indices};result.RecalculateBounds();return result;
        }
    }
}
