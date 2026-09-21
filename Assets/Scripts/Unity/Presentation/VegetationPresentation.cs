using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded, explicit-camera visual forest. No colliders or authoritative wind/terrain.</summary>
    public sealed class VegetationPresentation : MonoBehaviour
    {
        private VegetationDefinition definition;
        private VegetationInstance[] instances;
        private readonly Matrix4x4[][] matrices=new Matrix4x4[6][];
        private readonly Mesh[] meshes=new Mesh[6];
        private readonly int[] counts=new int[6];
        private byte[] species;
        private int groups=3;
        private readonly Plane[] planes=new Plane[6];
        private readonly Material[] materials=new Material[2];
        private MaterialPropertyBlock properties;
        private static readonly int Wind=Shader.PropertyToID("_Wind"), WindDirection=Shader.PropertyToID("_WindDirection"), Snow=Shader.PropertyToID("_SnowCoverage");
        public int TotalInstances => instances?.Length??0;
        public int VisibleInstances => NearCount+MidCount+FarCount;
        public int NearCount => counts[0]+counts[3]; public int MidCount => counts[1]+counts[4]; public int FarCount => counts[2]+counts[5];
        public int VisiblePines=>counts[3]+counts[4]+counts[5];
        public long SubmittedTriangles { get; private set; }
        private readonly int[] triangles=new int[6];
        public bool UsesAuditedFir { get; private set; }
        public bool UsesAuditedPine { get; private set; }
        public int InstanceSpecies(int index)=>species[index];
        public Vector3 InstanceWorldPosition(int index)=>transform.TransformPoint(instances[index].Position);
        public void Initialize(Vector3[] route,bool useAuditedFir=false,Func<Vector3,float> groundHeight=null,bool useAuditedPine=false)
        {
            if(definition!=null)throw new InvalidOperationException("Forest already initialized.");
            if(useAuditedPine&&!useAuditedFir)throw new ArgumentException("Pine composition requires the audited forest.");
            definition=VegetationDefinition.Load();instances=definition.Place(route);
            species=new byte[instances.Length];groups=useAuditedPine?6:3;
            if(useAuditedPine)
            {
                var composition=G2ForestComposition.Load();
                for(int i=0;i<instances.Length;i++)species[i]=(byte)composition.Species(transform.TransformPoint(instances[i].Position));
            }
            if(groundHeight!=null)
                for(int i=0;i<instances.Length;i++)
                {
                    var world=transform.TransformPoint(instances[i].Position);world.y=groundHeight(world);
                    if(!float.IsFinite(world.y))throw new ArgumentException("Nonfinite forest surface sample.");
                    instances[i]=instances[i].AtHeight(transform.InverseTransformPoint(world).y);
                }
            for(int type=0;type<groups/3;type++)
            {
                string root=type==1?"Biathlon/Vegetation/G2Pine/":useAuditedFir?"Biathlon/Vegetation/G2Fir/":"Biathlon/Vegetation/";
                string name=type==1?"Pine":useAuditedFir?"Fir":"Conifer";
                materials[type]=Resources.Load<Material>(root+name);
                if(materials[type]==null)throw new InvalidOperationException("Generate vegetation assets.");
                for(int lod=0;lod<3;lod++){int i=type*3+lod;meshes[i]=Resources.Load<Mesh>(root+name+"LOD"+lod);if(meshes[i]==null)throw new InvalidOperationException("Missing vegetation LOD.");matrices[i]=new Matrix4x4[definition.max_instances];triangles[i]=(int)meshes[i].GetIndexCount(0)/3;}
            }
            UsesAuditedFir=useAuditedFir;UsesAuditedPine=useAuditedPine;
            properties=new MaterialPropertyBlock();
        }
        public void Prepare(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            if(definition==null||camera==null||!float.IsFinite(presentationTime))throw new ArgumentException("Initialized forest, camera and finite time required.");
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(transform.up,Vector3.up)<.99999f)throw new ArgumentException("Forest root supports translation/yaw only; placement owns tree scale.");
            if(observedWind.HasValue&&(!float.IsFinite(observedWind.Value.x)||!float.IsFinite(observedWind.Value.y)))throw new ArgumentException("Finite visual wind required.");
            float density=definition.Density(tier);for(int i=0;i<counts.Length;i++)counts[i]=0;SubmittedTriangles=0;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            var world=transform.localToWorldMatrix;var eye=camera.transform.position;float lodScale=ProjectionAwareLod.Scale(camera);
            for(int index=0;index<instances.Length;index++)
            {
                var tree=instances[index];
                if(tree.DensityRank>=density)continue;
                var position=world.MultiplyPoint3x4(tree.Position);float distance=Vector3.Distance(position,eye);if(definition.Lod(distance)<0)continue;int lod=definition.Lod(distance*lodScale);
                // Includes canopy, height and maximum wind excursion. Intended sector root is unit scale.
                var bounds=new Bounds(position+Vector3.up*4*tree.Scale,new Vector3(6,9,6)*tree.Scale);
                if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                int group=species[index]*3+lod;
                matrices[group][counts[group]++]=world*tree.Matrix;SubmittedTriangles+=triangles[group];
            }
            var direction=observedWind.HasValue?observedWind.Value.normalized:new Vector2(1,.35f);
            float strength=observedWind.HasValue?Mathf.Clamp01(observedWind.Value.magnitude/5):1;
            properties.SetVector(Wind,new Vector4(presentationTime,definition.wind_amplitude_m*strength,definition.wind_frequency,0));
            properties.SetVector(WindDirection,new Vector4(direction.x,direction.y,0,0));
            properties.SetFloat(Snow,definition.snow_coverage);
        }
        public void Render(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            Prepare(camera,tier,presentationTime,observedWind);
            for(int i=0;i<groups;i++)if(counts[i]>0)
            {
                var p=new RenderParams(materials[i/3]){camera=camera,matProps=properties,shadowCastingMode=i%3<2?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,
                    worldBounds=new Bounds(transform.position,new Vector3(500,50,500)),layer=gameObject.layer};
                // Default instancing includes inverse matrices; stay below the 511-instance limit
                // without relying on platform-dependent uniform-scaling shader optimizations.
                for(int offset=0;offset<counts[i];offset+=500)
                    Graphics.RenderMeshInstanced(p,meshes[i],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
            }
        }
    }
}
