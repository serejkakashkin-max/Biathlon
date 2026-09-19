using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit scene-level weather lease. Rejects simultaneous owners instead of silently
    /// overwriting another host. Release before changing scenes; no authoritative weather mutation.</summary>
    public sealed class WeatherPresentation : MonoBehaviour
    {
        private static WeatherPresentation owner;
        private WeatherVisualDefinition definition;
        private Camera view;private Light key;private Material sky,flakes;private Mesh mesh;
        private Vector3[] vertices;private Vector2[] uv;private Color[] colors;private int[] indices;
        private bool leased,oldFog;private FogMode oldFogMode;private float oldFogStart,oldFogEnd,oldFogDensity,oldIntensity,oldShadowStrength;
        private Color oldFogColor,oldSky,oldEquator,oldGround,oldKeyColor,oldBackground;
        private AmbientMode oldAmbientMode;private Material oldSkybox;private CameraClearFlags oldClear;
        public int SubmittedFlakes {get;private set;}
        public string PresetId {get;private set;}
        public void Initialize(Camera camera,Light directional)
        {
            if(definition!=null||camera==null||directional==null||directional.type!=LightType.Directional||!isActiveAndEnabled)throw new ArgumentException("Enabled weather component, explicit camera and directional light required.");
            if(owner!=null)throw new InvalidOperationException("Release previous weather owner first.");
            definition=WeatherVisualDefinition.Load();view=camera;key=directional;
            var skyTemplate=Resources.Load<Material>("Biathlon/Weather/WinterSky");flakes=Resources.Load<Material>("Biathlon/Snow/Spray");
            if(skyTemplate==null||flakes==null)throw new InvalidOperationException("Generate weather assets.");
            sky=new Material(skyTemplate);mesh=new Mesh{name="Bounded snowfall"};mesh.MarkDynamic();
            vertices=new Vector3[definition.max_particles*4];uv=new Vector2[vertices.Length];colors=new Color[vertices.Length];indices=new int[definition.max_particles*6];
            for(int i=0;i<definition.max_particles;i++){int v=i*4,t=i*6;uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.up;uv[v+3]=Vector2.one;indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v+1;indices[t+4]=v+2;indices[t+5]=v+3;}
            Acquire();
        }
        private void Acquire()
        {
            if(owner!=null&&owner!=this)throw new InvalidOperationException("Another weather owner is active.");owner=this;leased=true;
            oldFog=RenderSettings.fog;oldFogMode=RenderSettings.fogMode;oldFogStart=RenderSettings.fogStartDistance;oldFogEnd=RenderSettings.fogEndDistance;oldFogDensity=RenderSettings.fogDensity;oldFogColor=RenderSettings.fogColor;
            oldAmbientMode=RenderSettings.ambientMode;oldSky=RenderSettings.ambientSkyColor;oldEquator=RenderSettings.ambientEquatorColor;oldGround=RenderSettings.ambientGroundColor;oldSkybox=RenderSettings.skybox;
            oldIntensity=key.intensity;oldShadowStrength=key.shadowStrength;oldKeyColor=key.color;oldClear=view.clearFlags;oldBackground=view.backgroundColor;
        }
        public void Render(string preset,string tier,double time,Vector2 observedWind,float localGroundHeight)
        {
            if(!leased||view==null||key==null||!float.IsFinite(localGroundHeight))throw new InvalidOperationException("Active weather lease required.");
            // Validate observation before mutating any scene state, including for zero-snow presets.
            definition.Sample(0,time,view.transform.position,observedWind);var p=definition.Get(preset);int count=definition.Count(tier,p.snowfall);
            PresetId=preset;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=p.fog_start_m;RenderSettings.fogEndDistance=p.fog_end_m;RenderSettings.fogColor=p.fog_color;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=p.fog_color*p.ambient_intensity;RenderSettings.ambientEquatorColor=p.fog_color*(p.ambient_intensity*.65f);RenderSettings.ambientGroundColor=p.fog_color*(p.ambient_intensity*.35f);
            key.color=p.light_color;key.intensity=p.light_intensity;key.shadowStrength=p.shadow_strength;view.clearFlags=CameraClearFlags.Skybox;view.backgroundColor=p.fog_color;RenderSettings.skybox=sky;
            sky.SetColor("_SkyColor",p.sky_color);sky.SetColor("_HorizonColor",p.fog_color);sky.SetFloat("_CloudCover",p.cloud_cover);sky.SetVector("_Drift",new Vector4(observedWind.x,observedWind.y,(float)time,0));
            int n=0;var center=view.transform.position;var right=view.transform.right*definition.flake_size_m;var up=view.transform.up*definition.flake_size_m;
            for(int i=0;i<count;i++)
            {
                var position=definition.Sample(i,time,center,observedWind);if(position.y<=localGroundHeight)continue;
                float distance=Vector3.Distance(position,center);if(distance<1||distance>definition.volume_width_m*.5f)continue;
                float alpha=definition.flake_opacity*Mathf.Clamp01((distance-1)/2)*(1-Mathf.SmoothStep(0,1,distance/(definition.volume_width_m*.5f)));
                int v=n*4;vertices[v]=position-right-up;vertices[v+1]=position+right-up;vertices[v+2]=position-right+up;vertices[v+3]=position+right+up;
                var color=new Color(.93f,.96f,1,alpha);for(int k=0;k<4;k++)colors[v+k]=color;n++;
            }
            SubmittedFlakes=n;mesh.Clear(false);if(n==0)return;
            mesh.SetVertices(vertices,0,n*4);mesh.SetUVs(0,uv,0,n*4);mesh.SetColors(colors,0,n*4);mesh.SetIndices(indices,0,n*6,MeshTopology.Triangles,0);mesh.RecalculateBounds();
            var render=new RenderParams(flakes){camera=view,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=mesh.bounds,layer=gameObject.layer};Graphics.RenderMesh(render,mesh,0,Matrix4x4.identity);
        }
        private void OnEnable(){if(definition!=null&&!leased)Acquire();}
        private void OnDisable()=>Release();
        private void Release()
        {
            if(!leased)return;leased=false;if(owner==this)owner=null;SubmittedFlakes=0;
            RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogDensity=oldFogDensity;RenderSettings.fogColor=oldFogColor;
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientSkyColor=oldSky;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;RenderSettings.skybox=oldSkybox;
            if(key!=null){key.intensity=oldIntensity;key.shadowStrength=oldShadowStrength;key.color=oldKeyColor;}if(view!=null){view.clearFlags=oldClear;view.backgroundColor=oldBackground;}if(mesh!=null)mesh.Clear(false);
        }
        private void OnDestroy(){Release();if(mesh!=null)Destroy(mesh);if(sky!=null)Destroy(sky);}
    }
}
