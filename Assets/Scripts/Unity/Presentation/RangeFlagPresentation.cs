using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded GPU-deformed fabrics. Observes wind; owns no weather or physics.</summary>
    public sealed class RangeFlagPresentation:MonoBehaviour
    {
        private RangeFlagDefinition definition;
        private GameObject generated;
        private Transform[] cloth;
        private Renderer[] renderers;
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private MaterialPropertyBlock properties;
        private static readonly int Wind=Shader.PropertyToID("_FlagWind"),Clock=Shader.PropertyToID("_FlagTime"),Phase=Shader.PropertyToID("_FlagPhase");
        private Vector3[] anchors;
        public int Count=>cloth?.Length??0;
        public Vector3 Position(int i)=>anchors[i];
        public Renderer Fabric(int i)=>renderers[i];
        public Vector2 ObservedWind {get;private set;}
        public void Initialize(RangeFlagDefinition source,ProductionEnvironmentDefinition environment,Func<Vector3,float> ground)
        {
            if(generated!=null||source==null||environment==null)throw new ArgumentException("Fresh flags and explicit venue required.");
            properties=new MaterialPropertyBlock();
            definition=RangeFlagDefinition.FromJson(JsonUtility.ToJson(source));
            anchors=definition.Positions(environment.range,ground);
            foreach(var anchor in anchors)foreach(var corridor in environment.route_corridors)
                for(int i=1;i<corridor.knots.Length;i++)
                {
                    var a=corridor.knots[i-1].Position;var delta=corridor.knots[i].Position-a;delta.y=0;
                    var p=anchor-a;p.y=0;float t=Mathf.Clamp01(Vector3.Dot(p,delta)/delta.sqrMagnitude);
                    float radius=Mathf.Lerp(corridor.widths_m[i-1],corridor.widths_m[i],t)*.5f+definition.flag_height_m*2;
                    if((p-delta*t).sqrMagnitude<radius*radius)throw new ArgumentException("Decorative flag clearance intersects a real sports corridor.");
                }
            var template=Resources.Load<Material>(definition.material_resource);
            if(template==null||template.shader==null||template.shader.name!="Biathlon/RangeFlag")throw new ArgumentException("Range flag material required.");
            generated=new GameObject("National flags behind the actual backstop");generated.transform.SetParent(transform,false);
            generated.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            cloth=new Transform[anchors.Length];renderers=new Renderer[anchors.Length];
            try
            {
                var poleMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));owned.Add(poleMaterial);
                poleMaterial.SetColor("_BaseColor",new Color(.5f,.55f,.59f));poleMaterial.SetFloat("_Metallic",.75f);poleMaterial.SetFloat("_Smoothness",.4f);
                var mesh=BuildCloth(definition.columns,definition.rows);owned.Add(mesh);
                for(int i=0;i<anchors.Length;i++)
                {
                    var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Flag mast "+definition.nations[i].id;
                    pole.transform.SetParent(generated.transform,false);pole.transform.position=anchors[i]+Vector3.up*(definition.pole_height_m*.5f);
                    pole.transform.localScale=new Vector3(definition.pole_radius_m*2,definition.pole_height_m*.5f,definition.pole_radius_m*2);
                    var collider=pole.GetComponent<Collider>();collider.enabled=false;Release(collider);
                    pole.GetComponent<Renderer>().sharedMaterial=poleMaterial;
                    var fabric=new GameObject("National cloth "+definition.nations[i].id);cloth[i]=fabric.transform;fabric.transform.SetParent(generated.transform,false);
                    fabric.transform.position=anchors[i]+Vector3.up*(definition.pole_height_m-.15f);
                    fabric.transform.localScale=new Vector3(definition.flag_height_m*definition.nations[i].aspect,definition.flag_height_m,definition.flag_height_m);
                    fabric.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=fabric.AddComponent<MeshRenderer>();renderers[i]=renderer;
                    var texture=BuildTexture(definition.nations[i],definition.texture_width);owned.Add(texture);
                    var material=new Material(template);owned.Add(material);material.SetTexture("_BaseMap",texture);renderer.sharedMaterial=material;
                    renderer.shadowCastingMode=ShadowCastingMode.TwoSided;renderer.receiveShadows=true;
                    renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                }
                Render(0,environment.lighting.visual_wind);
            }
            catch{Shutdown();throw;}
        }
        public void Render(float time,Vector2 wind)
        {
            if(generated==null||!float.IsFinite(time)||!float.IsFinite(wind.x)||!float.IsFinite(wind.y))throw new ArgumentException("Initialized flags and finite observation required.");
            ObservedWind=wind;float strength=Mathf.Clamp01(wind.magnitude/definition.full_wind_mps);
            var direction=wind.sqrMagnitude>.0001f?new Vector3(wind.x,0,wind.y).normalized:Vector3.right;
            var rotation=Quaternion.LookRotation(Vector3.Cross(direction,Vector3.up),Vector3.up);
            for(int i=0;i<cloth.Length;i++)
            {
                cloth[i].rotation=rotation;
                properties.Clear();properties.SetVector(Wind,new Vector4(strength,definition.maximum_wave_m/definition.flag_height_m,definition.flutter_hz,0));
                properties.SetFloat(Clock,time);properties.SetFloat(Phase,i*1.618034f);renderers[i].SetPropertyBlock(properties);
            }
        }
        public static Mesh BuildCloth(int columns,int rows)
        {
            if(columns<8||columns>48||rows<4||rows>24)throw new ArgumentOutOfRangeException(nameof(columns),"Bounded flag grid required.");
            var p=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[p.Length];var n=new Vector3[p.Length];var t=new int[columns*rows*6];int next=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                int i=y*(columns+1)+x;float u=(float)x/columns,v=(float)y/rows;p[i]=new Vector3(u,-v,0);uv[i]=new Vector2(u,1-v);n[i]=Vector3.forward;
                if(x==columns||y==rows)continue;t[next++]=i;t[next++]=i+columns+1;t[next++]=i+1;t[next++]=i+1;t[next++]=i+columns+1;t[next++]=i+columns+2;
            }
            var mesh=new Mesh{name="Bounded pinned flag grid"};mesh.vertices=p;mesh.uv=uv;mesh.normals=n;mesh.triangles=t;
            mesh.bounds=new Bounds(new Vector3(.5f,-.9f,0),new Vector3(1.2f,2.5f,1.6f));return mesh;
        }
        public static Texture2D BuildTexture(RangeFlagDefinition.Nation nation,int width)
        {
            if(nation==null||!float.IsFinite(nation.aspect)||nation.aspect<1||nation.aspect>2||width<64||width>512)
                throw new ArgumentException("Bounded flag texture required.");
            int height=Mathf.RoundToInt(width/nation.aspect);var pixels=new Color32[width*height];
            Color32 background=RangeFlagDefinition.ColorOf(nation.background);
            for(int i=0;i<pixels.Length;i++)pixels[i]=background;
            // Decode each paint once, never parse HTML colors per texel.
            foreach(var paint in nation.marks)
            {
                Color32 color=RangeFlagDefinition.ColorOf(paint.color);
                int x0=Mathf.Clamp(Mathf.CeilToInt(paint.x*width-.5f),0,width),x1=Mathf.Clamp(Mathf.CeilToInt((paint.x+paint.width)*width-.5f),0,width);
                int y0=Mathf.Clamp(Mathf.CeilToInt(paint.y*height-.5f),0,height),y1=Mathf.Clamp(Mathf.CeilToInt((paint.y+paint.height)*height-.5f),0,height);
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)pixels[y*width+x]=color;
            }
            var result=new Texture2D(width,height,TextureFormat.RGB24,true){name="Original national pattern "+nation.id,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=2};
            result.SetPixels32(pixels);result.Apply(true,true);return result;
        }
        public void Shutdown()
        {
            if(generated!=null){generated.SetActive(false);Release(generated);generated=null;}
            foreach(var item in owned)if(item!=null)Release(item);owned.Clear();cloth=null;renderers=null;anchors=null;definition=null;
        }
        private static void Release(UnityEngine.Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDisable(){if(generated!=null)generated.SetActive(false);}
        private void OnEnable(){if(generated!=null)generated.SetActive(true);}
        private void OnDestroy()=>Shutdown();
    }
}
