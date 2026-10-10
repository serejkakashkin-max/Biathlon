using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Painted finish lanes. Actual finish time and runout belong to the race.</summary>
    public sealed class FinishCorridorPresentation : MonoBehaviour
    {
        [Serializable] private sealed class Settings
        {
            public int schema_version,lane_count;
            public string id;
            public float straight_marking_m,marker_width_m,marker_height_m,marker_spacing_m,finish_line_width_m,surface_offset_m;
            public Color lane_color,finish_color;
        }
        private Mesh mesh;
        private Material lanes,finish;
        private GameObject generated;
        public Vector3 FinishPoint {get;private set;}
        public int LaneCount {get;private set;}
        public int MarkerCount {get;private set;}
        public void Initialize(VenueAuthoringPlan venue,RaceRoute route)
        {
            if(generated!=null||venue==null||route==null)throw new ArgumentException("Fresh finish presentation and authored route required.");
            var last=route.GetSection(route.SectionCount-1);
            if(venue.Track.GetLoop(last.LoopId).Closed)return;
            var data=JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("Biathlon/finish_presentation_v1").text);
            if(data==null||data.schema_version!=1||string.IsNullOrWhiteSpace(data.id)||data.lane_count<1||
                !(data.straight_marking_m>0&&data.straight_marking_m<=last.LoopEnd.Meters)||
                !(data.marker_width_m>0&&data.marker_height_m>0&&data.marker_spacing_m>0&&data.finish_line_width_m>0&&data.surface_offset_m>0))
                throw new ArgumentException("Invalid finish paint definition.");
            var visual=venue.Visual.Get(last.LoopId);double end=last.LoopEnd.Meters;
            var a=visual.Sample(end-data.straight_marking_m);var b=visual.Sample(end);
            float width=(float)new TrackSampler(venue.Track.GetLoop(last.LoopId)).Sample(last.LoopEnd).WidthMeters;
            FinishPoint=new Vector3((float)b.X,(float)b.Y,(float)b.Z);LaneCount=data.lane_count;
            int rows=(int)Math.Ceiling(data.straight_marking_m/data.marker_spacing_m);MarkerCount=rows*(data.lane_count+1);
            var vertices=new Vector3[(MarkerCount+1)*4];var white=new int[MarkerCount*6];var red=new int[6];
            var right=new Vector3((float)b.ForwardZ,0,(float)-b.ForwardX);
            var start=new Vector3((float)a.X,(float)a.Y+data.surface_offset_m,(float)a.Z);
            var stop=FinishPoint+Vector3.up*data.surface_offset_m;
            for(int i=0;i<=data.lane_count;i++)for(int row=0;row<rows;row++)
            {
                float side=-width*.5f+width*i/data.lane_count;
                var sample=visual.Sample(end-data.straight_marking_m+row*data.marker_spacing_m);
                var foot=new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z)+right*side;
                int index=i*rows+row;Quad(index,foot,foot+Vector3.up*data.marker_height_m,right*data.marker_width_m*.5f,white,index*6);
            }
            var forward=new Vector3((float)b.ForwardX,0,(float)b.ForwardZ);
            Quad(MarkerCount,stop-right*width*.5f,stop+right*width*.5f,forward*data.finish_line_width_m*.5f,red,0);
            mesh=new Mesh{name=data.id};mesh.vertices=vertices;mesh.subMeshCount=2;mesh.SetTriangles(white,0);mesh.SetTriangles(red,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var template=Resources.Load<Material>("Biathlon/Stadium/white");
            lanes=new Material(template);finish=new Material(template);lanes.SetColor("_BaseColor",data.lane_color);finish.SetColor("_BaseColor",data.finish_color);lanes.SetFloat("_Cull",0);
            generated=new GameObject("Finish lanes");generated.transform.SetParent(transform,false);generated.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            generated.AddComponent<MeshFilter>().sharedMesh=mesh;generated.AddComponent<MeshRenderer>().sharedMaterials=new[]{lanes,finish};
            void Quad(int index,Vector3 from,Vector3 to,Vector3 half,int[] triangles,int offset)
            {
                int v=index*4;vertices[v]=from-half;vertices[v+1]=from+half;vertices[v+2]=to-half;vertices[v+3]=to+half;
                // Select winding from the supplied direction, for upward faces.
                bool up=Vector3.Cross(half,to-from).y>=0;
                int x=up?1:2,y=up?2:1;
                triangles[offset]=v;triangles[offset+1]=v+x;triangles[offset+2]=v+y;
                triangles[offset+3]=v+1;triangles[offset+4]=v+3;triangles[offset+5]=v+2;
                if(!up){triangles[offset+3]=v+2;triangles[offset+5]=v+1;}
            }
        }
        private void OnDestroy()
        {
            Release(generated);Release(mesh);Release(lanes);Release(finish);
        }
        private static void Release(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
