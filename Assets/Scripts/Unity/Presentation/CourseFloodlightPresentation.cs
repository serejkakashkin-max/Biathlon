using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Static course illumination baked at load; a small fixed shadowless
    /// target fill keeps ordinary range materials readable. No camera-relative pools.</summary>
    public sealed class CourseFloodlightPresentation : MonoBehaviour
    {
        private static CourseFloodlightPresentation owner;
        private static readonly int Map=Shader.PropertyToID("_VenueLightMap"),Bounds=Shader.PropertyToID("_VenueLightBounds"),Gain=Shader.PropertyToID("_VenueLightGain"),Falloff=Shader.PropertyToID("_VenueLightHeight");
        private CourseFloodlightDefinition definition;
        private Mesh metalMesh,lensMesh;
        private Material metal,lens;
        private Texture2D map;
        private Matrix4x4[] matrices;
        private Vector4 bounds;
        private Texture oldMap;
        private Vector4 oldBounds;
        private float oldGain,oldHeight;
        private float appliedGain=float.NaN;
        private bool leased;
        private Light[] targetFill;
        private GameObject targetRoot;
        public CourseFloodlightAnchor[] Anchors {get;private set;}
        public int MastCount=>Anchors?.Length??0;
        public bool LampsOn {get;private set;}
        public int RealtimeLightCount=>targetFill?.Length??0;
        public Texture2D LightMap=>map;
        public Vector4 LightMapBounds=>bounds;
        public void Initialize(CourseFloodlightDefinition d,ProductionEnvironmentDefinition venue,Func<Vector3,float> ground,Func<Vector3,bool> clear=null)
        {
            if(definition!=null||d==null||venue==null||ground==null||owner!=null)throw new ArgumentException("One fresh venue floodlight owner required.");
            definition=CourseFloodlightDefinition.FromJson(JsonUtility.ToJson(d));
            try
            {
                Anchors=CourseFloodlightLayout.Build(definition,venue,ground,clear);matrices=new Matrix4x4[MastCount];
                for(int i=0;i<MastCount;i++){var aim=Anchors[i].Focus-Anchors[i].Foot;aim.y=0;matrices[i]=Matrix4x4.TRS(Anchors[i].Foot,Quaternion.LookRotation(aim.sqrMagnitude>.001f?aim:Vector3.forward),Vector3.one);}
                var structure=new RangeCoachMesh();var glass=new RangeCoachMesh();var steel=new Color(.26f,.31f,.34f);float h=d.height_m;
                structure.Box(new Vector3(0,.18f,0),new Vector3(.8f,.36f,.8f),new Color(.5f,.54f,.56f));
                structure.Tube(new Vector3(0,.25f,0),new Vector3(0,h,0),.11f,steel);
                structure.Tube(new Vector3(-1.25f,h,0),new Vector3(1.25f,h,0),.075f,steel);
                for(int i=0;i<2;i++)
                {
                    float x=i==0?-.68f:.68f;
                    structure.Box(new Vector3(x,h-.16f,.18f),new Vector3(.92f,.24f,.82f),new Color(.09f,.12f,.14f));
                    // Downward-facing LED panels are visible from below, never aimed into a skier's eyes.
                    glass.Box(new Vector3(x,h-.286f,.2f),new Vector3(.79f,.012f,.67f),Color.white);
                    glass.Box(new Vector3(x,h-.2f,.598f),new Vector3(.79f,.14f,.014f),Color.white);
                    for(int bar=0;bar<4;bar++)structure.Box(new Vector3(x-.34f+bar*.225f,h-.294f,.2f),new Vector3(.025f,.016f,.68f),steel);
                }
                metalMesh=structure.Build("Shared course floodlight mast");lensMesh=glass.Build("Shared LED panels");
                metal=new Material(Resources.Load<Material>(d.metal_material));lens=new Material(Resources.Load<Material>(d.lens_material));metal.enableInstancing=lens.enableInstancing=true;
                var b=venue.terrain.bounds;bounds=new Vector4(b.center.x-b.size.x*.5f,b.center.z-b.size.z*.5f,1/b.size.x,1/b.size.z);
                int n=d.map_resolution;var pixels=new Color[n*n];
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=new Vector3(bounds.x+(x+.5f)/n/bounds.z,0,bounds.y+(z+.5f)/n/bounds.w);p.y=ground(p);
                    var light=Illumination(p);pixels[z*n+x]=new Color(light.r,light.g,light.b,p.y);
                }
                map=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Static winter venue illumination",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};map.SetPixels(pixels);map.Apply(false,true);
                var first=venue.range.banks[0].position;var last=venue.range.banks[venue.range.banks.Length-1].position;
                int intervals=Math.Max(1,Mathf.CeilToInt(Vector3.Distance(first,last)/d.target_spacing_m));
                if(intervals+1>8)throw new ArgumentException("Target fill exceeds its fixed8light budget.");
                targetRoot=new GameObject("Shadowless target illumination");targetRoot.transform.SetParent(transform,false);targetFill=new Light[intervals+1];
                for(int i=0;i<targetFill.Length;i++)
                {
                    var go=new GameObject("Target fill "+i);go.transform.SetParent(targetRoot.transform,false);
                    go.transform.position=Vector3.Lerp(first,last,(float)i/intervals)-venue.range.forward*d.target_setback_m+Vector3.up*d.target_height_m;
                    var light=go.AddComponent<Light>();light.type=LightType.Point;light.shadows=LightShadows.None;light.range=d.target_range_m;light.color=d.light_color;light.enabled=false;targetFill[i]=light;
                }
                Acquire();Apply(0);
            }
            catch{Shutdown();throw;}
        }
        public Color Illumination(Vector3 point)
        {
            if(Anchors==null)throw new InvalidOperationException();float sum=0;
            for(int i=0;i<Anchors.Length;i++)
            {
                var a=Anchors[i];float radius=a.Range?definition.range_pool_radius_m:definition.pool_radius_m;
                var delta=new Vector2(point.x-a.Focus.x,point.z-a.Focus.z);float t=Mathf.Clamp01(1-delta.sqrMagnitude/(radius*radius));
                sum+=t*t;
            }
            return definition.light_color*Mathf.Min(1.6f,sum)*definition.illumination_gain;
        }
        public void Render(Camera camera,float gain)
        {
            if(camera==null||!float.IsFinite(gain)||gain<0||gain>2||definition==null)throw new ArgumentException("Bound floodlights and explicit view required.");
            Apply(gain);
            var a=new RenderParams(metal){camera=camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,layer=gameObject.layer};
            var b=new RenderParams(lens){camera=camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,layer=gameObject.layer};
            Graphics.RenderMeshInstanced(a,metalMesh,0,matrices);Graphics.RenderMeshInstanced(b,lensMesh,0,matrices);
        }
        private void Apply(float gain)
        {
            if(!leased||appliedGain==gain)return;appliedGain=gain;LampsOn=gain>0;Shader.SetGlobalTexture(Map,map);Shader.SetGlobalVector(Bounds,bounds);Shader.SetGlobalFloat(Gain,gain);Shader.SetGlobalFloat(Falloff,definition.height_falloff_m);
            lens.SetColor("_BaseColor",LampsOn?definition.light_color*definition.lens_gain*gain:new Color(.22f,.25f,.27f));
            foreach(var light in targetFill){light.enabled=LampsOn;light.intensity=definition.target_intensity*gain;}
        }
        private void Acquire()
        {
            if(leased||definition==null)return;if(owner!=null&&owner!=this)throw new InvalidOperationException("Release previous floodlights first.");
            oldMap=Shader.GetGlobalTexture(Map);oldBounds=Shader.GetGlobalVector(Bounds);oldGain=Shader.GetGlobalFloat(Gain);oldHeight=Shader.GetGlobalFloat(Falloff);owner=this;leased=true;appliedGain=float.NaN;
        }
        private void Release()
        {
            if(!leased)return;leased=false;LampsOn=false;if(owner==this)owner=null;Shader.SetGlobalTexture(Map,oldMap);Shader.SetGlobalVector(Bounds,oldBounds);Shader.SetGlobalFloat(Gain,oldGain);Shader.SetGlobalFloat(Falloff,oldHeight);
            if(targetFill!=null)foreach(var light in targetFill)if(light!=null)light.enabled=false;
        }
        private void OnDisable()=>Release();
        private void OnEnable()=>Acquire();
        public void Shutdown()
        {
            Release();Free(targetRoot);targetRoot=null;targetFill=null;Free(map);Free(metal);Free(lens);Free(metalMesh);Free(lensMesh);definition=null;Anchors=null;matrices=null;map=null;metal=null;lens=null;metalMesh=lensMesh=null;
        }
        private static void Free(UnityEngine.Object o){if(o!=null){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}}
        private void OnDestroy()=>Shutdown();
    }
}
