using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class WinterDressingDefinition
    {
        [Serializable] public sealed class Cluster {public string id;public float distance_m,side,lateral_m;}
        public int schema_version,max_instances;public string id,loop_id;
        public float near_m,cull_m,protected_margin_m,burial_m;
        public Cluster[] clusters;
        public static WinterDressingDefinition Load(string resource)=>FromJson(Resources.Load<TextAsset>(resource).text);
        public static WinterDressingDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<WinterDressingDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||string.IsNullOrWhiteSpace(d.loop_id)||d.max_instances<6||d.max_instances>128||d.clusters==null||d.clusters.Length*6>d.max_instances||
                !(d.near_m>5&&d.cull_m>d.near_m&&d.cull_m<=150&&d.protected_margin_m>=2&&d.burial_m>=0&&d.burial_m<=.3f))throw new ArgumentException("Invalid winter dressing definition.");
            var ids=new HashSet<string>();foreach(var c in d.clusters)if(c==null||!ids.Add(c.id)||string.IsNullOrWhiteSpace(c.id)||!float.IsFinite(c.distance_m)||c.distance_m<0||Mathf.Abs(c.side)!=1||!float.IsFinite(c.lateral_m)||c.lateral_m<8||c.lateral_m>25)throw new ArgumentException("Invalid winter cluster.");
            return d;
        }
    }
    /// <summary>Grounded winter clusters, bounded instance batches and frustum/distance LOD.
    /// Geometry and placement are presentation only; no collider or race-path mutation.</summary>
    public sealed class ProductionForestDressing:MonoBehaviour
    {
        private WinterDressingDefinition definition;
        private readonly Mesh[] near=new Mesh[6],far=new Mesh[6];private readonly Material[] materials=new Material[6];
        private Matrix4x4[] placements;private Bounds[] bounds;private int[] types;
        private Matrix4x4[][] submitted;private readonly int[] counts=new int[12];private readonly Plane[] planes=new Plane[6];
        public int Count=>placements?.Length??0;public int Visible{get;private set;}public long SubmittedTriangles{get;private set;}
        public Vector3 Position(int i)=>placements[i].GetColumn(3);
        public static bool ClearOfCourses(Vector3 p,float radius,ProductionRouteCorridor[] corridors,float margin)
        {
            foreach(var route in corridors)for(int i=1;i<route.knots.Length;i++)
            {
                var a=route.knots[i-1].Position;var b=route.knots[i].Position;a.y=b.y=p.y=0;var delta=b-a;
                float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,delta)/delta.sqrMagnitude):0;
                if(Vector3.Distance(p,a+delta*t)<Mathf.Lerp(route.widths_m[i-1],route.widths_m[i],t)*.5f+margin+radius)return false;
            }
            return true;
        }
        public void Initialize(WinterDressingDefinition d,ProductionRouteCorridor[] routes,Func<Vector3,float> height,Func<Vector3,float,bool> keep=null)
        {
            if(definition!=null||d==null||routes==null||height==null)throw new ArgumentException("Fresh dressing and explicit terrain required.");
            definition=WinterDressingDefinition.FromJson(JsonUtility.ToJson(d));
            ProductionRouteCorridor path=null;foreach(var route in routes)if(route.loop_id==d.loop_id)path=route;
            if(path==null)throw new ArgumentException("Dressing course missing.");
            near[0]=Resources.Load<Mesh>("Biathlon/Vegetation/G2Shrub/ShrubLOD0");far[0]=Resources.Load<Mesh>("Biathlon/Vegetation/G2Shrub/ShrubLOD2");materials[0]=Resources.Load<Material>("Biathlon/Vegetation/G2Shrub/Shrub");
            near[1]=Resources.Load<Mesh>("Biathlon/Terrain/G2Rock/RockLOD0");far[1]=Resources.Load<Mesh>("Biathlon/Terrain/G2Rock/RockLOD1");materials[1]=Resources.Load<Material>("Biathlon/Terrain/G2Rock/SnowyRock");
            near[2]=Resources.Load<Mesh>("Biathlon/Vegetation/G2Fir/FirLOD0");far[2]=Resources.Load<Mesh>("Biathlon/Vegetation/G2Fir/FirLOD2");materials[2]=Resources.Load<Material>("Biathlon/Vegetation/G2Fir/Fir");
            foreach(int type in new[]{3,4,5}){near[type]=Resources.Load<Mesh>("Biathlon/Terrain/WinterDressing/"+(type==3?"FallenLog":type==4?"Stump":"SnowDrift"));far[type]=near[type];materials[type]=Resources.Load<Material>(type==5?"Biathlon/Terrain/WinterDressing/SnowDrift":"Biathlon/Terrain/WinterDressing/Wood");}
            for(int i=0;i<6;i++)if(near[i]==null||far[i]==null||materials[i]==null)throw new InvalidOperationException("Generate winter dressing assets.");
            var matrixList=new List<Matrix4x4>();var boundList=new List<Bounds>();var typeList=new List<int>();
            foreach(var cluster in d.clusters)
            {
                var knots=path.knots;int segment=1;while(segment<knots.Length-1&&knots[segment].distance_m<cluster.distance_m)segment++;
                float t=Mathf.InverseLerp(knots[segment-1].distance_m,knots[segment].distance_m,cluster.distance_m);
                var center=Vector3.Lerp(knots[segment-1].Position,knots[segment].Position,t);
                var forward=(knots[segment].Position-knots[segment-1].Position).normalized;var side=Vector3.Cross(Vector3.up,forward).normalized*cluster.side;
                center+=side*cluster.lateral_m;
                for(int type=0;type<6;type++)
                {
                    float radius=type==2?1.4f:type==3?1.8f:type==5?2.4f:.9f;
                    var point=center+forward*(type%3-1)*2.6f+side*(type/3)*2.1f;
                    if(!ClearOfCourses(point,radius,routes,d.protected_margin_m))continue;
                    if(keep!=null&&!keep(point,radius))continue;
                    point.y=height(point)-d.burial_m;
                    float scale=type==2?.35f:type==1?.65f:type==0?.8f:1;
                    float yaw=(VegetationDefinition.Mix((uint)(matrixList.Count+71))%360);
                    var matrix=Matrix4x4.TRS(point,Quaternion.Euler(0,yaw,0),Vector3.one*scale);
                    matrixList.Add(matrix);typeList.Add(type);boundList.Add(new Bounds(point+Vector3.up*radius,new Vector3(radius*2,radius*3,radius*2)));
                }
            }
            placements=matrixList.ToArray();types=typeList.ToArray();bounds=boundList.ToArray();submitted=new Matrix4x4[12][];
            for(int i=0;i<12;i++)submitted[i]=new Matrix4x4[d.max_instances];
        }
        public void Render(Camera camera,string tier)
        {
            if(definition==null||camera==null)throw new ArgumentException("Initialized dressing and view required.");
            Array.Clear(counts,0,counts.Length);Visible=0;SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);
            float limit=definition.cull_m*(tier=="low"?.65f:tier=="balanced"?.85f:1);
            var worldBounds=new Bounds(camera.transform.position,Vector3.one*limit*2);
            for(int i=0;i<placements.Length;i++)
            {
                float distance=Vector3.Distance(camera.transform.position,Position(i));if(distance>limit||!GeometryUtility.TestPlanesAABB(planes,bounds[i]))continue;
                int lod=distance*ProjectionAwareLod.Scale(camera)<definition.near_m?0:1,group=types[i]*2+lod;
                submitted[group][counts[group]++]=placements[i];Visible++;SubmittedTriangles+=(lod==0?near[types[i]]:far[types[i]]).GetIndexCount(0)/3;
            }
            for(int group=0;group<12;group++)if(counts[group]>0)
            {
                int type=group/2;var parameters=new RenderParams(materials[type]){camera=camera,worldBounds=worldBounds,shadowCastingMode=group%2==0?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,layer=gameObject.layer};
                Graphics.RenderMeshInstanced(parameters,group%2==0?near[type]:far[type],0,submitted[group],counts[group]);
            }
        }
    }
}
