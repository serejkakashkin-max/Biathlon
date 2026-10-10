using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    /// <summary>One combined mesh of original low V-boards. No collision or penalty authority.</summary>
    public sealed class PenaltyFencePresentation : MonoBehaviour
    {
        [Serializable] private sealed class Settings
        {public int schema_version;public string id;public float spacing_m,length_m,height_m,foot_half_width_m,edge_offset_m,opening_m;}
        private Mesh mesh;private Material material;
        public int BoardCount {get;private set;}
        public void Initialize(TrackDefinition track,TrackVisualDefinition visual)
        {
            var d=JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("Biathlon/penalty_fence_presentation_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.spacing_m>d.length_m&&d.height_m>0&&d.opening_m>=15))throw new ArgumentException("Invalid penalty fence.");
            var loop=track.GetLoop(track.Penalty.LoopId);var ribbon=visual.Get(loop.Id);var sample=new TrackSampler(loop);double length=loop.Length.Meters;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(double at=d.opening_m*.5;at<length-d.opening_m*.5;at+=d.spacing_m)
            {
                var p=ribbon.Sample(at);var f=new Vector3((float)p.ForwardX,0,(float)p.ForwardZ);var r=new Vector3(f.z,0,-f.x);
                float width=(float)sample.Sample(new TrackDistance((long)Math.Round(at*1000))).WidthMeters;
                for(int side=-1;side<=1;side+=2)
                {
                    var centre=new Vector3((float)p.X,(float)p.Y,(float)p.Z)+r*side*(width*.5f+d.edge_offset_m+d.foot_half_width_m);
                    // Keep every connecting road open, including crossings
                    // farther around the oval than its nominal entry seam.
                    if(OverlapsRoad(centre,f,d,track,visual,loop.Id))continue;
                    var top=centre+Vector3.up*d.height_m;
                    Quad(centre-r*d.foot_half_width_m-f*d.length_m*.5f,centre-r*d.foot_half_width_m+f*d.length_m*.5f,top-f*d.length_m*.5f,top+f*d.length_m*.5f);
                    Quad(centre+r*d.foot_half_width_m+f*d.length_m*.5f,centre+r*d.foot_half_width_m-f*d.length_m*.5f,top+f*d.length_m*.5f,top-f*d.length_m*.5f);BoardCount++;
                }
            }
            mesh=new Mesh{name=d.id};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            material=new Material(Resources.Load<Material>("Biathlon/Stadium/white"));material.SetFloat("_Cull",0);
            var child=new GameObject("Penalty V-board fence");child.transform.SetParent(transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 e){int i=vertices.Count;vertices.AddRange(new[]{a,b,c,e});indices.AddRange(new[]{i,i+1,i+2,i+1,i+3,i+2});}
        }
        private static bool OverlapsRoad(Vector3 centre,Vector3 forward,Settings d,TrackDefinition track,TrackVisualDefinition visual,string penalty)
        {
            for(int l=0;l<visual.Count;l++)
            {
                var road=visual.Get(l);if(road.LoopId==penalty)continue;
                var sampler=new TrackSampler(track.GetLoop(road.LoopId));
                for(int k=1;k<road.Count;k++)
                {
                    var a=road.Get(k-1);var b=road.Get(k);double dx=b.X-a.X,dz=b.Z-a.Z,squared=dx*dx+dz*dz;
                    double t=Math.Max(0,Math.Min(1,((centre.x-a.X)*dx+(centre.z-a.Z)*dz)/squared));
                    double x=a.X+t*dx,z=a.Z+t*dz;
                    double width=sampler.Sample(TrackDistance.FromMeters(Math.Round(a.Distance+t*(b.Distance-a.Distance),3))).WidthMeters;
                    double radius=width*.5+Math.Sqrt(d.length_m*d.length_m*.25+d.foot_half_width_m*d.foot_half_width_m)+d.edge_offset_m;
                    if((centre.x-x)*(centre.x-x)+(centre.z-z)*(centre.z-z)<radius*radius)return true;
                }
            }
            return false;
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
