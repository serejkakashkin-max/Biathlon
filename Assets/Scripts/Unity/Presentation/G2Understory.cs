using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded winter branch dressing, independent of authoritative course geometry.</summary>
    public sealed class G2Understory : MonoBehaviour
    {
        [Serializable] public sealed class Plant {public string id;public float x,z,scale,yaw;}
        [Serializable] public sealed class Definition
        {
            public int schema_version;public string id;public float near_m,far_m,cull_m,burial_m;public Plant[] plants;
            public void Validate()
            {
                if(schema_version!=1||string.IsNullOrWhiteSpace(id)||plants==null||plants.Length<1||plants.Length>48)throw new ArgumentException("Invalid understory schema/capacity.");
                Bound(near_m,4,15);Bound(far_m,near_m+1,35);Bound(cull_m,far_m+1,80);Bound(burial_m,0,.2f);
                var ids=new HashSet<string>();var route=G2BenchmarkSurface.CopyRoute();
                foreach(var p in plants)
                {
                    if(p==null||string.IsNullOrWhiteSpace(p.id)||!ids.Add(p.id))throw new ArgumentException("Unique plant IDs required.");
                    Bound(p.x,-45,45);Bound(p.z,-140,-23);Bound(p.scale,.5f,1.5f);Bound(p.yaw,0,360);
                    if(VegetationDefinition.DistanceToRoute(new Vector3(p.x,0,p.z),route)<5.5f+.7f*p.scale)throw new ArgumentException("Understory intrudes on protected route margin.");
                }
            }
            private static void Bound(float value,float min,float max){if(!float.IsFinite(value)||value<min||value>max)throw new ArgumentException("Invalid understory value.");}
        }
        private Definition definition;private Material material;
        private readonly Mesh[] meshes=new Mesh[3];private Matrix4x4[] placements;private Bounds[] bounds;
        private readonly Matrix4x4[][] submitted={new Matrix4x4[48],new Matrix4x4[48],new Matrix4x4[48]};private readonly int[] counts=new int[3];private readonly Plane[] planes=new Plane[6];
        public int Count=>placements?.Length??0;public int Visible=>counts[0]+counts[1]+counts[2];public long SubmittedTriangles{get;private set;}
        public Vector3 Position(int index)=>placements[index].GetColumn(3);
        public static Definition Load(){var d=JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("Biathlon/g2_understory_v1").text);if(d==null)throw new ArgumentException("Missing understory.");d.Validate();return d;}
        public void Initialize()
        {
            if(definition!=null)throw new InvalidOperationException("Understory already initialized.");definition=Load();material=Resources.Load<Material>("Biathlon/Vegetation/G2Shrub/Shrub");
            if(material==null)throw new InvalidOperationException("Generate winter shrub derivatives.");
            for(int i=0;i<3;i++){meshes[i]=Resources.Load<Mesh>("Biathlon/Vegetation/G2Shrub/ShrubLOD"+i);if(meshes[i]==null)throw new InvalidOperationException("Missing winter shrub LOD.");}
            placements=new Matrix4x4[definition.plants.Length];bounds=new Bounds[placements.Length];
            for(int i=0;i<placements.Length;i++)
            {
                var plant=definition.plants[i];var p=new Vector3(plant.x,0,plant.z);p.y=G2BenchmarkSurface.Height(p)-definition.burial_m;
                placements[i]=Matrix4x4.TRS(p,Quaternion.Euler(0,plant.yaw,0),Vector3.one*plant.scale);
                bounds[i]=new Bounds(p+Vector3.up*.5f*plant.scale,new Vector3(1.5f,1.1f,1.5f)*plant.scale);
            }
        }
        public void Prepare(Camera camera)
        {
            if(definition==null||camera==null)throw new ArgumentException("Initialized understory and camera required.");
            Array.Clear(counts,0,counts.Length);SubmittedTriangles=0;GeometryUtility.CalculateFrustumPlanes(camera,planes);float projection=ProjectionAwareLod.Scale(camera);
            for(int i=0;i<placements.Length;i++)
            {
                float distance=Vector3.Distance(camera.transform.position,Position(i));if(distance>=definition.cull_m||!GeometryUtility.TestPlanesAABB(planes,bounds[i]))continue;
                float apparent=distance*projection;int lod=apparent<definition.near_m?0:apparent<definition.far_m?1:2;submitted[lod][counts[lod]++]=placements[i];SubmittedTriangles+=meshes[lod].GetIndexCount(0)/3;
            }
        }
        public void Render(Camera camera)
        {
            Prepare(camera);
            for(int lod=0;lod<3;lod++)if(counts[lod]>0)
            {
                var p=new RenderParams(material){camera=camera,worldBounds=new Bounds(new Vector3(0,4,-80),new Vector3(100,20,150)),layer=gameObject.layer,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true};
                Graphics.RenderMeshInstanced(p,meshes[lod],0,submitted[lod],counts[lod]);
                if(lod==0){p.shadowCastingMode=ShadowCastingMode.ShadowsOnly;p.receiveShadows=false;Graphics.RenderMeshInstanced(p,meshes[1],0,submitted[lod],counts[lod]);}
            }
        }
    }
}
