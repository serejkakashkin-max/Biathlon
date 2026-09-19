using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    public sealed class BreathVaporPresentation : MonoBehaviour
    {
        private BreathVaporSettings settings;private ExertionPresentationSettings exertion;private BreathEnvironment? environment;
        private BreathVaporBuffer buffer;private Mesh mesh;private Material material;
        private Vector3[] vertices;private Vector2[] uv;private Color[] colors;private int[] indices;
        public BreathVaporBuffer Buffer=>buffer;public int SubmittedPuffs {get;private set;}
        public void Initialize()
        {
            if(buffer!=null){Clear();environment=null;return;}settings=BreathVaporSettings.Load();exertion=ExertionPresentationSettings.Load();buffer=new BreathVaporBuffer(settings);
            material=Resources.Load<Material>("Biathlon/Snow/Spray");if(material==null)throw new InvalidOperationException("Missing original vapor mask material.");
            mesh=new Mesh{name="Bounded athlete breath vapor"};mesh.MarkDynamic();vertices=new Vector3[settings.max_puffs*4];uv=new Vector2[vertices.Length];colors=new Color[vertices.Length];indices=new int[settings.max_puffs*6];
            for(int i=0;i<settings.max_puffs;i++){int v=i*4,t=i*6;uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.up;uv[v+3]=Vector2.one;indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v+1;indices[t+4]=v+2;indices[t+5]=v+3;}
        }
        public void SetEnvironment(BreathEnvironment? value){environment=value;if(!value.HasValue)Clear();}
        public void Observe(string id,AthleteRigBinding rig,AthleteAnimationFrame frame,float distance)
        {
            if(!environment.HasValue)return;var a=frame.Athlete;var head=rig.Bone(AthleteBone.Head);
            buffer.Observe(id,frame.TimeSeconds,head.TransformPoint(settings.mouth_local),head.forward,exertion.Intensity(ExertionInput.From(a)),environment.Value,a.OnCourse&&distance<settings.cull_m,a.BreathHolding,a.ShootingActive?a.BreathingPhaseCycles:(double?)null);
        }
        public void Render(double time,Camera camera)
        {
            buffer.Advance(time);SubmittedPuffs=0;mesh.Clear(false);if(!environment.HasValue||camera==null)return;
            int n=0;var center=camera.transform.position;
            for(int i=0;i<buffer.Capacity;i++)
            {
                var p=buffer.Get(i);if(p.Strength<=0)continue;float age=(float)(time-p.Born);var position=p.Origin+p.Velocity*age;float distance=Vector3.Distance(position,center);if(distance>=settings.cull_m||distance<.3f)continue;
                float size=settings.size_start_m+settings.growth_mps*age;var right=camera.transform.right*size;var up=camera.transform.up*size;
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.fade_start_m,settings.cull_m,distance));float alpha=settings.opacity*p.Strength*Mathf.Sin(Mathf.PI*age/settings.lifetime_s)*fade;
                int v=n*4;vertices[v]=position-right-up;vertices[v+1]=position+right-up;vertices[v+2]=position-right+up;vertices[v+3]=position+right+up;var color=new Color(.92f,.96f,1,alpha);for(int k=0;k<4;k++)colors[v+k]=color;n++;
            }
            SubmittedPuffs=n;if(n==0)return;mesh.SetVertices(vertices,0,n*4);mesh.SetUVs(0,uv,0,n*4);mesh.SetColors(colors,0,n*4);mesh.SetIndices(indices,0,n*6,MeshTopology.Triangles,0);mesh.RecalculateBounds();
            Graphics.RenderMesh(new RenderParams(material){camera=camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=mesh.bounds,layer=gameObject.layer},mesh,0,Matrix4x4.identity);
        }
        public void Clear(){buffer?.Clear();SubmittedPuffs=0;if(mesh!=null)mesh.Clear(false);}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
