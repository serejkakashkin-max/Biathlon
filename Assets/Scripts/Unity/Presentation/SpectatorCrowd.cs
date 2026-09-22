using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class SpectatorCrowdSettings
    {
        public int schema_version,max_instances,seed;public string id;public float low_density,balanced_density,lod_m,cull_m,motion_radians,motion_frequency;
        public static SpectatorCrowdSettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/spectator_crowd_v1").text);
        public static SpectatorCrowdSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<SpectatorCrowdSettings>(json);if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.max_instances<1||d.max_instances>512)throw new ArgumentException("Invalid spectator schema/budget.");
            Bound(d.low_density,.05f,1);Bound(d.balanced_density,d.low_density,1);Bound(d.lod_m,10,60);Bound(d.cull_m,d.lod_m+1,200);Bound(d.motion_radians,0,1);Bound(d.motion_frequency,0,5);return d;
        }
        private static void Bound(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid spectator setting.");}
        public float Density(string tier)=>tier=="low"?low_density:tier=="balanced"?balanced_density:tier=="high"?1:throw new ArgumentException("Unknown crowd tier.");
        public float Rank(int index)=>(VegetationDefinition.Mix(unchecked((uint)(index*73856093+seed)))&0xffffff)/16777216f;
    }
    public sealed class SpectatorCrowd : MonoBehaviour
    {
        private SpectatorCrowdSettings settings;private SpectatorSeating seating;private Vector3[] anchors;private float[] ranks;
        private readonly Mesh[] meshes=new Mesh[4];private readonly Matrix4x4[][] matrices=new Matrix4x4[4][];private readonly int[] counts=new int[4],triangles=new int[4];
        private readonly Plane[] planes=new Plane[6];private readonly Material[] materials=new Material[2];private MaterialPropertyBlock properties;private Bounds worldBounds;private int groups=2;
        private static readonly int Motion=Shader.PropertyToID("_Motion");
        public int Total=>anchors?.Length??0;public int Near=>counts[0]+counts[2];public int Far=>counts[1]+counts[3];public int Visible=>Near+Far;public long SubmittedTriangles {get;private set;}
        public bool UsesSimplifiedShadows{get;private set;}
        public bool UsesAuditedCrowd{get;private set;}
        public int VisibleVariant(int variant)=>variant>=0&&variant<2?counts[variant*2]+counts[variant*2+1]:throw new ArgumentOutOfRangeException(nameof(variant));
        public long ShadowSourceTriangles=>counts[0]*triangles[UsesSimplifiedShadows?1:0]+counts[2]*triangles[UsesSimplifiedShadows?3:2];
        public void Initialize(SpectatorSeating source,bool useSimplifiedShadows=false,bool useAuditedCrowd=false)
        {
            if(settings!=null||source==null)throw new ArgumentException("Initialize once with explicit seating.");var d=SpectatorCrowdSettings.Load();if(source.Count==0||source.Count>d.max_instances)throw new ArgumentException("Seating exceeds crowd budget.");
            settings=d;seating=source;UsesSimplifiedShadows=useSimplifiedShadows;anchors=source.CopyAnchors();ranks=new float[anchors.Length];for(int i=0;i<ranks.Length;i++)ranks[i]=settings.Rank(i);
            UsesAuditedCrowd=useAuditedCrowd;groups=useAuditedCrowd?4:2;
            for(int variant=0;variant<groups/2;variant++)
            {
                materials[variant]=Resources.Load<Material>(useAuditedCrowd?"Biathlon/RestrictedG2Crowd/Spectator"+variant:"Biathlon/Crowd/Spectator");
                if(materials[variant]==null)throw new InvalidOperationException("Missing spectator assets. G2 requires local licensed inputs: run scripts/Build-G2Crowd.ps1; never publish restricted derivatives.");
                for(int lod=0;lod<2;lod++){int i=variant*2+lod;meshes[i]=Resources.Load<Mesh>(useAuditedCrowd?"Biathlon/RestrictedG2Crowd/Crowd"+variant+"LOD"+lod:"Biathlon/Crowd/SpectatorLOD"+lod);if(meshes[i]==null)throw new InvalidOperationException("Missing spectator LOD; regenerate local crowd assets.");triangles[i]=(int)meshes[i].GetIndexCount(0)/3;matrices[i]=new Matrix4x4[d.max_instances];}
            }
            properties=new MaterialPropertyBlock();
        }
        public void Prepare(Camera camera,string tier,float time)
        {
            if(settings==null||seating==null||camera==null||!float.IsFinite(time)||Mathf.Abs(time)>1000000)throw new ArgumentException("Initialized crowd, camera and finite bounded time required.");
            if((seating.transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(seating.transform.up,Vector3.up)<.99999f)throw new ArgumentException("Seating supports unit-scale translation/yaw only.");
            float density=settings.Density(tier);Array.Clear(counts,0,counts.Length);SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);var matrix=seating.transform.localToWorldMatrix;var eye=camera.transform.position;bool first=true;
            float lodScale=ProjectionAwareLod.Scale(camera);
            for(int i=0;i<anchors.Length;i++)
            {
                if(ranks[i]>=density)continue;var position=matrix.MultiplyPoint3x4(anchors[i]);float distance=Vector3.Distance(position,eye);if(distance>=settings.cull_m)continue;
                var bounds=new Bounds(position+Vector3.up*.8f,new Vector3(1.2f,2,2));if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                int lod=distance*lodScale>=settings.lod_m?1:0;int variant=UsesAuditedCrowd?(int)(VegetationDefinition.Mix(unchecked((uint)(i+settings.seed)))&1):0;int group=variant*2+lod;
                matrices[group][counts[group]++]=matrix*Matrix4x4.Translate(anchors[i]);SubmittedTriangles+=triangles[group];if(first){worldBounds=bounds;first=false;}else worldBounds.Encapsulate(bounds);
            }
            properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,0));
        }
        public void Render(Camera camera,string tier,float time)
        {
            Prepare(camera,tier,time);for(int group=0;group<groups;group++)if(counts[group]>0)
            {
                int lod=group%2;
                properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,lod));
                var p=new RenderParams(materials[group/2]){camera=camera,matProps=properties,worldBounds=worldBounds,layer=gameObject.layer,shadowCastingMode=lod==0&&!UsesSimplifiedShadows?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true};
                for(int offset=0;offset<counts[group];offset+=500)Graphics.RenderMeshInstanced(p,meshes[group],0,matrices[group],Math.Min(500,counts[group]-offset),offset);
                if(lod==0&&UsesSimplifiedShadows)
                {
                    p.shadowCastingMode=ShadowCastingMode.ShadowsOnly;p.receiveShadows=false;
                    for(int offset=0;offset<counts[group];offset+=500)Graphics.RenderMeshInstanced(p,meshes[group+1],0,matrices[group],Math.Min(500,counts[group]-offset),offset);
                }
            }
        }
    }
}
