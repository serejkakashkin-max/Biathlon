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
        private readonly Matrix4x4[][] matrices=new Matrix4x4[3][];
        private readonly Mesh[] meshes=new Mesh[3];
        private readonly int[] counts=new int[3];
        private readonly Plane[] planes=new Plane[6];
        private Material material;
        private MaterialPropertyBlock properties;
        private static readonly int Wind=Shader.PropertyToID("_Wind"), WindDirection=Shader.PropertyToID("_WindDirection"), Snow=Shader.PropertyToID("_SnowCoverage");
        public int TotalInstances => instances?.Length??0;
        public int VisibleInstances => counts[0]+counts[1]+counts[2];
        public int NearCount => counts[0]; public int MidCount => counts[1]; public int FarCount => counts[2];
        public long SubmittedTriangles { get; private set; }
        private readonly int[] triangles=new int[3];
        public void Initialize(Vector3[] route)
        {
            if(definition!=null)throw new InvalidOperationException("Forest already initialized.");
            definition=VegetationDefinition.Load();instances=definition.Place(route);
            material=Resources.Load<Material>("Biathlon/Vegetation/Conifer");
            if(material==null)throw new InvalidOperationException("Generate vegetation assets.");
            for(int i=0;i<3;i++){meshes[i]=Resources.Load<Mesh>("Biathlon/Vegetation/ConiferLOD"+i);if(meshes[i]==null)throw new InvalidOperationException("Missing vegetation LOD.");matrices[i]=new Matrix4x4[definition.max_instances];triangles[i]=(int)meshes[i].GetIndexCount(0)/3;}
            properties=new MaterialPropertyBlock();
        }
        public void Prepare(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            if(definition==null||camera==null||!float.IsFinite(presentationTime))throw new ArgumentException("Initialized forest, camera and finite time required.");
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(transform.up,Vector3.up)<.99999f)throw new ArgumentException("Forest root supports translation/yaw only; placement owns tree scale.");
            if(observedWind.HasValue&&(!float.IsFinite(observedWind.Value.x)||!float.IsFinite(observedWind.Value.y)))throw new ArgumentException("Finite visual wind required.");
            float density=definition.Density(tier);counts[0]=counts[1]=counts[2]=0;SubmittedTriangles=0;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            var world=transform.localToWorldMatrix;var eye=camera.transform.position;float lodScale=ProjectionAwareLod.Scale(camera);
            foreach(var tree in instances)
            {
                if(tree.DensityRank>=density)continue;
                var position=world.MultiplyPoint3x4(tree.Position);float distance=Vector3.Distance(position,eye);if(definition.Lod(distance)<0)continue;int lod=definition.Lod(distance*lodScale);
                // Includes canopy, height and maximum wind excursion. Intended sector root is unit scale.
                var bounds=new Bounds(position+Vector3.up*4*tree.Scale,new Vector3(6,9,6)*tree.Scale);
                if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                matrices[lod][counts[lod]++]=world*tree.Matrix;SubmittedTriangles+=triangles[lod];
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
            for(int i=0;i<3;i++)if(counts[i]>0)
            {
                var p=new RenderParams(material){camera=camera,matProps=properties,shadowCastingMode=i<2?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,
                    worldBounds=new Bounds(transform.position,new Vector3(500,50,500)),layer=gameObject.layer};
                // Default instancing includes inverse matrices; stay below the 511-instance limit
                // without relying on platform-dependent uniform-scaling shader optimizations.
                for(int offset=0;offset<counts[i];offset+=500)
                    Graphics.RenderMeshInstanced(p,meshes[i],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
            }
        }
    }
}
