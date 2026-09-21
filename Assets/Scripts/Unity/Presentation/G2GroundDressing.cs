using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Small explicit off-route decoration set. No colliders or simulation references.</summary>
    public sealed class G2GroundDressing : MonoBehaviour
    {
        [Serializable] public sealed class Rock {public string id;public float x,z,scale,yaw;}
        [Serializable] public sealed class Definition
        {
            public int schema_version;public string id;public float near_distance_m,cull_distance_m,burial_fraction;public Rock[] rocks;
            public void Validate()
            {
                if(schema_version!=1||string.IsNullOrWhiteSpace(id)||rocks==null||rocks.Length<1||rocks.Length>32)throw new ArgumentException("Invalid ground dressing schema/capacity.");
                Check(near_distance_m,5,40);Check(cull_distance_m,near_distance_m+1,120);Check(burial_fraction,0,.25f);
                var ids=new HashSet<string>();var route=G2BenchmarkSurface.CopyRoute();
                foreach(var rock in rocks)
                {
                    if(rock==null||string.IsNullOrWhiteSpace(rock.id)||!ids.Add(rock.id))throw new ArgumentException("Unique rock IDs required.");
                    Check(rock.x,-120,120);Check(rock.z,-140,-15);Check(rock.scale,.3f,1.5f);Check(rock.yaw,0,360);
                    if(VegetationDefinition.DistanceToRoute(new Vector3(rock.x,0,rock.z),route)<3.5f+1.15f*rock.scale+2)throw new ArgumentException("Rock intrudes on protected route margin.");
                }
            }
            private static void Check(float x,float min,float max){if(!float.IsFinite(x)||x<min||x>max)throw new ArgumentException("Invalid ground dressing value.");}
        }
        private Definition definition;
        private readonly Mesh[] meshes=new Mesh[2];private Material material;
        private Matrix4x4[] placements;private Bounds[] bounds;private readonly Matrix4x4[][] submitted={new Matrix4x4[32],new Matrix4x4[32]};
        private readonly Plane[] planes=new Plane[6];
        public int Count=>placements?.Length??0;
        public int NearCount{get;private set;}public int FarCount{get;private set;}
        public long SubmittedTriangles{get;private set;}
        public Vector3 Position(int index)=>placements[index].GetColumn(3);
        public static Definition Load()
        {
            var d=JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("Biathlon/g2_ground_dressing_v1").text);if(d==null)throw new ArgumentException("Missing ground dressing.");d.Validate();return d;
        }
        public void Initialize()
        {
            if(definition!=null)throw new InvalidOperationException("Ground dressing already initialized.");definition=Load();
            material=Resources.Load<Material>("Biathlon/Terrain/G2Rock/SnowyRock");
            for(int i=0;i<2;i++){meshes[i]=Resources.Load<Mesh>("Biathlon/Terrain/G2Rock/RockLOD"+i);if(meshes[i]==null)throw new InvalidOperationException("Generate rock assets.");}
            if(material==null)throw new InvalidOperationException("Generate rock material.");
            placements=new Matrix4x4[definition.rocks.Length];bounds=new Bounds[placements.Length];
            for(int i=0;i<placements.Length;i++)
            {
                var rock=definition.rocks[i];var p=new Vector3(rock.x,0,rock.z);p.y=G2BenchmarkSurface.Height(p)-definition.burial_fraction*rock.scale;
                placements[i]=Matrix4x4.TRS(p,Quaternion.Euler(0,rock.yaw,0),Vector3.one*rock.scale);
                bounds[i]=new Bounds(p+Vector3.up*.55f*rock.scale,new Vector3(2.3f,1.2f,2.3f)*rock.scale);
            }
        }
        public void Prepare(Camera camera)
        {
            if(definition==null||camera==null)throw new ArgumentException("Initialized dressing and camera required.");
            NearCount=FarCount=0;SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);float projection=ProjectionAwareLod.Scale(camera);
            for(int i=0;i<placements.Length;i++)
            {
                float distance=Vector3.Distance(camera.transform.position,Position(i));
                if(distance>=definition.cull_distance_m||!GeometryUtility.TestPlanesAABB(planes,bounds[i]))continue;
                int lod=distance*projection<definition.near_distance_m?0:1;submitted[lod][lod==0?NearCount++:FarCount++]=placements[i];
                SubmittedTriangles+=meshes[lod].GetIndexCount(0)/3;
            }
        }
        public void Render(Camera camera)
        {
            Prepare(camera);
            for(int i=0;i<2;i++)
            {
                int count=i==0?NearCount:FarCount;if(count==0)continue;
                var p=new RenderParams(material){camera=camera,shadowCastingMode=i==0?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,worldBounds=new Bounds(new Vector3(0,3,-70),new Vector3(250,20,160)),layer=gameObject.layer};
                Graphics.RenderMeshInstanced(p,meshes[i],0,submitted[i],count);
            }
        }
    }
}
