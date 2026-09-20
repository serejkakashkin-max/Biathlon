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
        private readonly Mesh[] meshes=new Mesh[2];private readonly Matrix4x4[][] matrices=new Matrix4x4[2][];private readonly int[] counts=new int[2],triangles=new int[2];
        private readonly Plane[] planes=new Plane[6];private Material material;private MaterialPropertyBlock properties;private Bounds worldBounds;
        private static readonly int Motion=Shader.PropertyToID("_Motion");
        public int Total=>anchors?.Length??0;public int Near=>counts[0];public int Far=>counts[1];public int Visible=>Near+Far;public long SubmittedTriangles {get;private set;}
        public void Initialize(SpectatorSeating source)
        {
            if(settings!=null||source==null)throw new ArgumentException("Initialize once with explicit seating.");var d=SpectatorCrowdSettings.Load();if(source.Count==0||source.Count>d.max_instances)throw new ArgumentException("Seating exceeds crowd budget.");
            settings=d;seating=source;anchors=source.CopyAnchors();ranks=new float[anchors.Length];for(int i=0;i<ranks.Length;i++)ranks[i]=settings.Rank(i);
            material=Resources.Load<Material>("Biathlon/Crowd/Spectator");if(material==null)throw new InvalidOperationException("Generate spectator assets first.");
            for(int i=0;i<2;i++){meshes[i]=Resources.Load<Mesh>("Biathlon/Crowd/SpectatorLOD"+i);if(meshes[i]==null)throw new InvalidOperationException("Missing spectator LOD.");triangles[i]=(int)meshes[i].GetIndexCount(0)/3;matrices[i]=new Matrix4x4[d.max_instances];}properties=new MaterialPropertyBlock();
        }
        public void Prepare(Camera camera,string tier,float time)
        {
            if(settings==null||seating==null||camera==null||!float.IsFinite(time)||Mathf.Abs(time)>1000000)throw new ArgumentException("Initialized crowd, camera and finite bounded time required.");
            if((seating.transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(seating.transform.up,Vector3.up)<.99999f)throw new ArgumentException("Seating supports unit-scale translation/yaw only.");
            float density=settings.Density(tier);counts[0]=counts[1]=0;SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);var matrix=seating.transform.localToWorldMatrix;var eye=camera.transform.position;bool first=true;
            float lodScale=ProjectionAwareLod.Scale(camera);
            for(int i=0;i<anchors.Length;i++)
            {
                if(ranks[i]>=density)continue;var position=matrix.MultiplyPoint3x4(anchors[i]);float distance=Vector3.Distance(position,eye);if(distance>=settings.cull_m)continue;
                var bounds=new Bounds(position+Vector3.up*.8f,new Vector3(1.2f,2,2));if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                int lod=distance*lodScale>=settings.lod_m?1:0;matrices[lod][counts[lod]++]=matrix*Matrix4x4.Translate(anchors[i]);SubmittedTriangles+=triangles[lod];if(first){worldBounds=bounds;first=false;}else worldBounds.Encapsulate(bounds);
            }
            properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,0));
        }
        public void Render(Camera camera,string tier,float time)
        {
            Prepare(camera,tier,time);for(int lod=0;lod<2;lod++)if(counts[lod]>0)
            {
                properties.SetVector(Motion,new Vector4(time,settings.motion_radians,settings.motion_frequency,lod));
                var p=new RenderParams(material){camera=camera,matProps=properties,worldBounds=worldBounds,layer=gameObject.layer,shadowCastingMode=lod==0?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true};
                for(int offset=0;offset<counts[lod];offset+=500)Graphics.RenderMeshInstanced(p,meshes[lod],0,matrices[lod],Math.Min(500,counts[lod]-offset),offset);
            }
        }
    }
}
