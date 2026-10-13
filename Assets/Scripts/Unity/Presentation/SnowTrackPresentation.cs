using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Receives completed poses, writes only a bounded transparent mesh.</summary>
    public sealed class SnowTrackPresentation : MonoBehaviour
    {
        private SnowTrackSettings settings;
        private SnowTrackBuffer buffer;
        private Mesh mesh;
        private GameObject drawing;
        private MeshRenderer rendererComponent;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector4[] tangents;
        private Vector2[] uvs;
        private Color[] colors;
        private int[] indices;
        public SnowTrackBuffer Buffer=>buffer;
        public int VisibleMarks {get;private set;}
        public Mesh Mesh=>mesh;
        public void Initialize()
        {
            if(buffer!=null){Clear();return;}
            settings=SnowTrackSettings.Load();buffer=new SnowTrackBuffer(settings);
            vertices=new Vector3[settings.max_marks*4];uvs=new Vector2[vertices.Length];colors=new Color[vertices.Length];indices=new int[settings.max_marks*6];
            normals=new Vector3[vertices.Length];tangents=new Vector4[vertices.Length];
            for(int i=0;i<settings.max_marks;i++)
            {
                int v=i*4,t=i*6;uvs[v]=new Vector2(0,0);uvs[v+1]=new Vector2(1,0);uvs[v+2]=new Vector2(0,1);uvs[v+3]=new Vector2(1,1);
                indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v+1;indices[t+4]=v+2;indices[t+5]=v+3;
            }
            drawing=new GameObject("Snow contact marks");drawing.transform.SetParent(transform,false);
            mesh=new Mesh{name="Bounded snow contact marks"};mesh.MarkDynamic();drawing.AddComponent<MeshFilter>().sharedMesh=mesh;
            rendererComponent=drawing.AddComponent<MeshRenderer>();rendererComponent.sharedMaterial=Resources.Load<Material>("Biathlon/Snow/ContactMarks");
            if(rendererComponent.sharedMaterial==null)throw new System.InvalidOperationException("Generate snow contact material first.");
            rendererComponent.shadowCastingMode=ShadowCastingMode.Off;rendererComponent.receiveShadows=false;
        }
        public void Observe(string id,AthleteRigBinding rig,AthleteAnimationFrame frame,float distance)
        {
            var state=frame.Athlete;var root=rig.ModelRoot;
            var contact=rig.GetComponent<AthleteGroundContact>();
            if(contact==null||contact.SurfaceNormal.sqrMagnitude<.1f)return;
            var normal=contact.SurfaceNormal;
            bool skiing=state.OnCourse&&!state.ShootingActive&&state.SpeedMetersPerSecond>.5&&distance<settings.draw_distance_m;
            Sample(0,rig.Socket(AthleteSocket.LeftSkiBinding).position,settings.ski_contact_height_m,false);
            Sample(1,rig.Socket(AthleteSocket.RightSkiBinding).position,settings.ski_contact_height_m,false);
            Sample(2,contact.LeftPoleTip,settings.pole_contact_height_m,true);
            Sample(3,contact.RightPoleTip,settings.pole_contact_height_m,true);
            void Sample(int channel,Vector3 point,float height,bool pole)
            {
                float clearance=Vector3.Dot(point-root.position,normal);
                var surface=point-normal*clearance+normal*settings.surface_offset_m;
                buffer.Sample(id,channel,surface,normal,frame.TimeSeconds,skiing&&clearance>=-.02f&&clearance<=height,pole);
            }
        }
        public void Render(double time,Vector3 observer)
        {
            buffer.Advance(time);int visible=0;
            for(int i=0;i<buffer.Count;i++)
            {
                var mark=buffer.Get(i);float distance=Vector3.Distance(observer,(mark.A+mark.B)*.5f);
                if(distance>=settings.draw_distance_m)continue;
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.fade_start_m,settings.draw_distance_m,distance));
                fade*=1-(float)((time-mark.Time)/settings.lifetime_s);
                var side=Vector3.Cross(mark.Normal,mark.B-mark.A).normalized*(mark.Width*.5f);int v=visible*4;
                vertices[v]=drawing.transform.InverseTransformPoint(mark.A-side);vertices[v+1]=drawing.transform.InverseTransformPoint(mark.A+side);
                vertices[v+2]=drawing.transform.InverseTransformPoint(mark.B-side);vertices[v+3]=drawing.transform.InverseTransformPoint(mark.B+side);
                var color=settings.color;color.a*=fade;
                var n=drawing.transform.InverseTransformDirection(mark.Normal);
                var tangent=drawing.transform.InverseTransformDirection(side.normalized);
                for(int k=0;k<4;k++){colors[v+k]=color;normals[v+k]=n;tangents[v+k]=new Vector4(tangent.x,tangent.y,tangent.z,1);}visible++;
            }
            VisibleMarks=visible;rendererComponent.enabled=visible>0;
            mesh.Clear(false);if(visible==0)return;
            mesh.SetVertices(vertices,0,visible*4);mesh.SetUVs(0,uvs,0,visible*4);mesh.SetColors(colors,0,visible*4);
            mesh.SetNormals(normals,0,visible*4);mesh.SetTangents(tangents,0,visible*4);
            mesh.SetIndices(indices,0,visible*6,MeshTopology.Triangles,0);mesh.RecalculateBounds();
        }
        public void Clear(){buffer?.Clear();VisibleMarks=0;if(mesh!=null)mesh.Clear(false);if(rendererComponent!=null)rendererComponent.enabled=false;}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(drawing!=null)Destroy(drawing);}
    }
}
