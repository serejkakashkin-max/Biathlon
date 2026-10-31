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
        private ProductionVisibility visibility;
        private Vector3[] worldPositions;private Matrix4x4[] worldMatrices;private Bounds[] spectatorBounds;
        private Matrix4x4 cachedWorld;private bool geometryCached;
        public int GeometryBuildCount{get;private set;}
        public bool PreservesAllSpectators=>visibility!=null;
        private readonly Mesh[] meshes=new Mesh[30];private readonly Matrix4x4[][] matrices=new Matrix4x4[30][];private readonly int[] counts=new int[30],triangles=new int[30];
        private readonly Plane[] planes=new Plane[6];private readonly Material[] materials=new Material[10];private MaterialPropertyBlock properties;private Bounds worldBounds;private int groups=3;
        private static readonly int Motion=Shader.PropertyToID("_Motion");
        public int Total=>anchors?.Length??0;public int Near=>CountLod(0);public int Far=>CountLod(1)+CountLod(2);public int Silhouettes=>CountLod(2);
        private int CountLod(int lod){int total=0;for(int i=lod;i<groups;i+=3)total+=counts[i];return total;}public int Visible=>Near+Far;public long SubmittedTriangles {get;private set;}
        public bool UsesSimplifiedShadows{get;private set;}
        public bool SimplifyingCurrentShadows{get;private set;}
        public bool UsesAuditedCrowd{get;private set;}
        public SpectatorPose Pose{get;private set;}
        public int VisibleVariant(int variant)=>variant>=0&&variant<groups/3?counts[variant*3]+counts[variant*3+1]+counts[variant*3+2]:throw new ArgumentOutOfRangeException(nameof(variant));
        public long ShadowSourceTriangles {get{long total=0;for(int i=0;i<groups;i+=3)total+=counts[i]*triangles[SimplifyingCurrentShadows?i+1:i];return total;}}
        public void Initialize(SpectatorSeating source,bool useSimplifiedShadows=false,bool useAuditedCrowd=false,ProductionVisibility completeVisibility=null)
        {
            if(settings!=null||source==null)throw new ArgumentException("Initialize once with explicit seating.");var d=SpectatorCrowdSettings.Load();if(source.Count==0||source.Count>d.max_instances)throw new ArgumentException("Seating exceeds crowd budget.");
            if(source.Pose!=SpectatorPose.Seated&&source.Pose!=SpectatorPose.Standing)throw new ArgumentException("Unknown spectator source pose.");
            if(useAuditedCrowd&&source.Pose==SpectatorPose.Standing)throw new ArgumentException("Audited seated crowd cannot represent standing spectators.");
            Pose=source.Pose;
            settings=d;seating=source;UsesSimplifiedShadows=useSimplifiedShadows;anchors=source.CopyAnchors();ranks=new float[anchors.Length];for(int i=0;i<ranks.Length;i++)ranks[i]=settings.Rank(i);
            visibility=completeVisibility;worldPositions=new Vector3[anchors.Length];worldMatrices=new Matrix4x4[anchors.Length];spectatorBounds=new Bounds[anchors.Length];
            UsesAuditedCrowd=useAuditedCrowd;groups=useAuditedCrowd?30:3;
            for(int variant=0;variant<groups/3;variant++)
            {
                materials[variant]=Resources.Load<Material>(useAuditedCrowd?"Biathlon/RestrictedG2Crowd/Spectator"+variant:"Biathlon/Crowd/Spectator");
                if(materials[variant]==null)throw new InvalidOperationException("Missing spectator assets. G2 requires local licensed inputs: run scripts/Build-G2Crowd.ps1; never publish restricted derivatives.");
                for(int lod=0;lod<3;lod++){int i=variant*3+lod;int nativeLod=useAuditedCrowd||visibility==null?Math.Min(lod,1):lod;meshes[i]=Resources.Load<Mesh>(useAuditedCrowd?"Biathlon/RestrictedG2Crowd/Crowd"+variant+"LOD"+nativeLod:"Biathlon/Crowd/"+(Pose==SpectatorPose.Standing?"StandingSpectatorLOD":"SpectatorLOD")+nativeLod);if(meshes[i]==null)throw new InvalidOperationException("Missing spectator LOD; regenerate local crowd assets.");triangles[i]=(int)meshes[i].GetIndexCount(0)/3;matrices[i]=new Matrix4x4[d.max_instances];}
            }
            properties=new MaterialPropertyBlock();
        }
        private void PrepareGeometry(Matrix4x4 matrix)
        {
            if(geometryCached&&matrix.Equals(cachedWorld))return;
            for(int i=0;i<anchors.Length;i++)
            {
                var position=matrix.MultiplyPoint3x4(anchors[i]);worldPositions[i]=position;worldMatrices[i]=matrix*Matrix4x4.Translate(anchors[i]);
                spectatorBounds[i]=Pose==SpectatorPose.Standing?new Bounds(position+Vector3.up,new Vector3(1.2f,2.1f,2)):new Bounds(position+Vector3.up*.8f,new Vector3(1.2f,2,2));
            }
            cachedWorld=matrix;geometryCached=true;GeometryBuildCount++;
        }
        public void Prepare(Camera camera,string tier,float time)
        {
            if(settings==null||seating==null||camera==null||!float.IsFinite(time)||Mathf.Abs(time)>1000000)throw new ArgumentException("Initialized crowd, camera and finite bounded time required.");
            if((seating.transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(seating.transform.up,Vector3.up)<.99999f)throw new ArgumentException("Seating supports unit-scale translation/yaw only.");
            SimplifyingCurrentShadows=UsesSimplifiedShadows&&tier!="high";float density=settings.Density(tier);Array.Clear(counts,0,counts.Length);SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);var matrix=seating.transform.localToWorldMatrix;var eye=camera.transform.position;bool first=true;
            float lodScale=ProjectionAwareLod.Scale(camera);
            PrepareGeometry(matrix);var fogFrame=visibility!=null?visibility.Frame(camera):default;
            for(int i=0;i<anchors.Length;i++)
            {
                if(visibility==null&&ranks[i]>=density)continue;var position=worldPositions[i];float distance=Vector3.Distance(position,eye);if(visibility==null&&distance>=settings.cull_m)continue;
                var bounds=spectatorBounds[i];if(visibility!=null&&fogFrame.Hidden(bounds)||!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                float lodDistance=distance*lodScale;
                int lod=visibility!=null&&lodDistance>=settings.cull_m?2:lodDistance>=settings.lod_m||visibility!=null&&ranks[i]>=density?1:0;int variant=UsesAuditedCrowd?(int)(VegetationDefinition.Mix(unchecked((uint)(i+settings.seed)))%(uint)(groups/3)):0;int group=variant*3+lod;
                matrices[group][counts[group]++]=worldMatrices[i];SubmittedTriangles+=triangles[group];if(first){worldBounds=bounds;first=false;}else worldBounds.Encapsulate(bounds);
            }
            properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,0));
        }
        public void Render(Camera camera,string tier,float time)
        {
            Prepare(camera,tier,time);for(int group=0;group<groups;group++)if(counts[group]>0)
            {
                int lod=group%3;
                properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,Math.Min(lod,1)));
                var p=new RenderParams(materials[group/3]){camera=camera,matProps=properties,worldBounds=worldBounds,layer=gameObject.layer,shadowCastingMode=lod==0&&!SimplifyingCurrentShadows?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true};
                for(int offset=0;offset<counts[group];offset+=500)Graphics.RenderMeshInstanced(p,meshes[group],0,matrices[group],Math.Min(500,counts[group]-offset),offset);
                if(lod==0&&SimplifyingCurrentShadows)
                {
                    p.shadowCastingMode=ShadowCastingMode.ShadowsOnly;p.receiveShadows=false;
                    for(int offset=0;offset<counts[group];offset+=500)Graphics.RenderMeshInstanced(p,meshes[group+1],0,matrices[group],Math.Min(500,counts[group]-offset),offset);
                }
            }
        }
    }
}
