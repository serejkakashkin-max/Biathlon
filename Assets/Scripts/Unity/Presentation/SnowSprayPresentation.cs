using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    public sealed class SnowSprayPresentation : MonoBehaviour
    {
        private SnowSpraySettings settings;
        private SnowTrackSettings trackSettings;
        private SnowSprayBuffer buffer;
        private GameObject drawing;
        private Mesh mesh;
        private MeshRenderer rendererComponent;
        private Vector3[] vertices;
        private Vector2[] uvs;
        private Color[] colors;
        private int[] indices;
        private float surfaceGain=1;
        private RenderQualityPresentation quality;
        public void ConfigureSurface(string profile)
        {if(settings==null)Initialize();surfaceGain=profile=="fresh_soft"?settings.fresh_gain:profile=="icy_fast"?settings.ice_gain:1;}
        public SnowSprayBuffer Buffer=>buffer;
        public int VisibleParticles {get;private set;}
        public void Initialize()
        {
            if(buffer!=null){Clear();return;}
            settings=SnowSpraySettings.Load();trackSettings=SnowTrackSettings.Load();buffer=new SnowSprayBuffer(settings);
            quality=GetComponentInParent<RenderQualityPresentation>();
            vertices=new Vector3[settings.max_particles*4];uvs=new Vector2[vertices.Length];colors=new Color[vertices.Length];indices=new int[settings.max_particles*6];
            for(int i=0;i<settings.max_particles;i++)
            {
                int v=i*4,t=i*6;uvs[v]=new Vector2(0,0);uvs[v+1]=new Vector2(1,0);uvs[v+2]=new Vector2(0,1);uvs[v+3]=new Vector2(1,1);
                indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v+1;indices[t+4]=v+2;indices[t+5]=v+3;
            }
            drawing=new GameObject("Snow contact spray");drawing.transform.SetParent(transform,false);
            mesh=new Mesh{name="Bounded snow spray"};mesh.MarkDynamic();drawing.AddComponent<MeshFilter>().sharedMesh=mesh;
            rendererComponent=drawing.AddComponent<MeshRenderer>();rendererComponent.sharedMaterial=Resources.Load<Material>("Biathlon/Snow/Spray");
            if(rendererComponent.sharedMaterial==null)throw new System.InvalidOperationException("Generate snow spray material first.");
            rendererComponent.shadowCastingMode=ShadowCastingMode.Off;rendererComponent.receiveShadows=false;
        }
        public void Observe(string id,AthleteRigBinding rig,AthleteAnimationFrame frame,float distance)
        {
            var contact=rig.GetComponent<AthleteGroundContact>();if(contact==null||contact.SurfaceNormal.sqrMagnitude<.1f)return;
            var normal=contact.SurfaceNormal;var root=rig.ModelRoot;var state=frame.Athlete;
            bool active=state.OnCourse&&!state.ShootingActive;
            Sample(0,rig.Socket(AthleteSocket.LeftSkiBinding).position,trackSettings.ski_contact_height_m);
            Sample(1,rig.Socket(AthleteSocket.RightSkiBinding).position,trackSettings.ski_contact_height_m);
            Sample(2,contact.LeftPoleTip,trackSettings.pole_contact_height_m);
            Sample(3,contact.RightPoleTip,trackSettings.pole_contact_height_m);
            void Sample(int channel,Vector3 point,float height)
            {
                float clearance=Vector3.Dot(point-root.position,normal);
                var surface=point-normal*clearance+normal*trackSettings.surface_offset_m;
                var hybrid=rig.GetComponent<AthleteHybridLocomotionDriver>();
                float edge=hybrid!=null&&hybrid.Active?Mathf.Abs(channel==0?hybrid.LeftEdgeDegrees:hybrid.RightEdgeDegrees):0;
                float push=channel>=2?1:settings.push_min+edge*settings.edge_gain+Mathf.Max(0,(float)state.Effort.DeliveredRelativeEffort-1)*settings.attack_gain;
                var forward=channel==0?rig.Bone(AthleteBone.LeftFoot).forward:channel==1?rig.Bone(AthleteBone.RightFoot).forward:root.forward;
                buffer.Sample(id,channel,surface,normal,forward,frame.TimeSeconds,active&&clearance>=-.02f&&clearance<=height,(float)state.SpeedMetersPerSecond,distance,state.TuckActive,Mathf.Clamp(push*surfaceGain,0,2));
            }
        }
        public void Render(double time,Camera camera)
        {
            buffer.Advance(time);int visible=0;
            int budget=quality==null||quality.TierId=="high"?settings.ultra_draw_budget:quality.TierId=="balanced"?settings.high_draw_budget:settings.performance_draw_budget;
            var right=camera!=null?camera.transform.right:Vector3.right;var up=camera!=null?camera.transform.up:Vector3.up;
            var observer=camera!=null?camera.transform.position:transform.position;
            for(int i=0;i<buffer.Capacity&&visible<budget;i++)
            {
                var particle=buffer.Get(i);if(particle.Life<=0)continue;
                var p=particle.Position(time,settings.gravity_mps2);float distance=Vector3.Distance(p,observer);if(distance>=settings.draw_distance_m)continue;
                float age=(float)(time-particle.Born)/particle.Life;
                var color=new Color(.94f,.97f,1,settings.opacity*(1-age)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.near_distance_m,settings.draw_distance_m,distance))));
                int v=visible*4;var r=right*particle.Size;var u=up*particle.Size;
                vertices[v]=drawing.transform.InverseTransformPoint(p-r-u);vertices[v+1]=drawing.transform.InverseTransformPoint(p+r-u);
                vertices[v+2]=drawing.transform.InverseTransformPoint(p-r+u);vertices[v+3]=drawing.transform.InverseTransformPoint(p+r+u);
                for(int k=0;k<4;k++)colors[v+k]=color;visible++;
            }
            VisibleParticles=visible;rendererComponent.enabled=visible>0;mesh.Clear(false);if(visible==0)return;
            mesh.SetVertices(vertices,0,visible*4);mesh.SetUVs(0,uvs,0,visible*4);mesh.SetColors(colors,0,visible*4);
            mesh.SetIndices(indices,0,visible*6,MeshTopology.Triangles,0);mesh.RecalculateBounds();
        }
        public void Clear(){buffer?.Clear();VisibleParticles=0;if(mesh!=null)mesh.Clear(false);if(rendererComponent!=null)rendererComponent.enabled=false;}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(drawing!=null)Destroy(drawing);}
    }
}
