#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity
{
    /// <summary>Original bevelled architectural mesh authoring, no runtime geometry churn.</summary>
    public sealed class StadiumArtMesh
    {
        private readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        private readonly List<int> triangles=new List<int>();
        public void Box(Vector3 center,Vector3 size,float bevel=.015f,Quaternion? orientation=null)
        {
            var q=orientation??Quaternion.identity;var e=size*.5f;float b=Mathf.Min(bevel,Mathf.Min(e.x,Mathf.Min(e.y,e.z))*.8f);
            Face(Vector3.right,Vector3.back,Vector3.up);Face(Vector3.left,Vector3.forward,Vector3.up);
            Face(Vector3.up,Vector3.right,Vector3.back);Face(Vector3.down,Vector3.right,Vector3.forward);
            Face(Vector3.forward,Vector3.right,Vector3.up);Face(Vector3.back,Vector3.left,Vector3.up);
            void Face(Vector3 normal,Vector3 u,Vector3 v)
            {
                int start=vertices.Count;float eu=Vector3.Scale(u,e).magnitude,ev=Vector3.Scale(v,e).magnitude;
                float[] us={-eu,-eu+b,eu-b,eu},vs={-ev,-ev+b,ev-b,ev};
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    var p=Vector3.Scale(normal,e)+u*us[x]+v*vs[y];var core=new Vector3(Mathf.Clamp(p.x,-e.x+b,e.x-b),Mathf.Clamp(p.y,-e.y+b,e.y-b),Mathf.Clamp(p.z,-e.z+b,e.z-b));
                    var n=(p-core).normalized;vertices.Add(center+q*(core+n*b));normals.Add(q*n);
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=start+y*4+x;triangles.Add(a);triangles.Add(a+1);triangles.Add(a+4);triangles.Add(a+1);triangles.Add(a+5);triangles.Add(a+4);}
            }
        }
        public void Beam(Vector3 from,Vector3 to,float width){var d=to-from;Box((from+to)*.5f,new Vector3(width,width,d.magnitude),width*.16f,Quaternion.LookRotation(d));}
        public Mesh Build(string name){var m=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(vertices);m.SetNormals(normals);m.SetTriangles(triangles,0);m.RecalculateBounds();return m;}
        public static Mesh Disk()
        {
            const int sides=96;var v=new Vector3[sides+1];var n=new Vector3[v.Length];var t=new int[sides*3];
            for(int i=0;i<sides;i++){float a=2*Mathf.PI*i/sides;v[i+1]=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,0);t[i*3]=0;t[i*3+1]=(i+1)%sides+1;t[i*3+2]=i+1;}
            for(int i=0;i<n.Length;i++)n[i]=Vector3.back;var m=new Mesh{name="Target aperture unit diameter"};m.vertices=v;m.normals=n;m.triangles=t;m.RecalculateBounds();return m;
        }
    }
}
#endif
