using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded, explicit-camera visual forest. No colliders or authoritative wind/terrain.</summary>
    public sealed class VegetationPresentation : MonoBehaviour
    {
        private VegetationDefinition definition;
        private float? productionHighFogEndMeters;
        private VegetationInstance[] instances;
        private readonly Matrix4x4[][] matrices=new Matrix4x4[6][];
        private readonly Mesh[] meshes=new Mesh[6];
        private readonly int[] counts=new int[6];
        private byte[] species;
        private readonly float[] canopyRadius=new float[2],canopyMinY=new float[2],canopyMaxY=new float[2];
        private Bounds preparedWorldBounds;
        private int groups=3;
        private Bounds localForestBounds;
        private readonly Plane[] planes=new Plane[6];
        private readonly Material[] materials=new Material[2];
        private MaterialPropertyBlock properties;
        private static readonly int Wind=Shader.PropertyToID("_Wind"), WindDirection=Shader.PropertyToID("_WindDirection"), Snow=Shader.PropertyToID("_SnowCoverage");
        public int TotalInstances => instances?.Length??0;
        public int VisibleInstances => NearCount+MidCount+FarCount;
        public int NearCount => counts[0]+counts[3]; public int MidCount => counts[1]+counts[4]; public int FarCount => counts[2]+counts[5];
        public int VisiblePines=>counts[3]+counts[4]+counts[5];
        public long SubmittedTriangles { get; private set; }
        public long ShadowSourceTriangles {get;private set;}
        private readonly int[] triangles=new int[6];
        public bool UsesAuditedFir { get; private set; }
        public bool UsesAuditedPine { get; private set; }
        public bool UsesSimplifiedShadows {get;private set;}
        public bool SimplifyingCurrentShadows {get;private set;}
        public bool UsesExplicitSpecies {get;private set;}
        public float? ProductionHighFogEndMeters=>productionHighFogEndMeters;
        public Bounds PreparedWorldBounds=>preparedWorldBounds;
        public int InstanceSpecies(int index)=>species[index];
        public Vector3 InstanceWorldPosition(int index)=>transform.TransformPoint(instances[index].Position);
        public void Initialize(Vector3[] route,bool useAuditedFir=false,Func<Vector3,float> groundHeight=null,bool useAuditedPine=false,bool useSimplifiedShadows=false,VegetationInstance[] authoredInstances=null,byte[] authoredSpecies=null,float? productionHighFogEndMeters=null)
        {
            if(definition!=null)throw new InvalidOperationException("Forest already initialized.");
            if(productionHighFogEndMeters.HasValue&&(!float.IsFinite(productionHighFogEndMeters.Value)||productionHighFogEndMeters.Value<=0||authoredInstances==null||authoredSpecies==null||!useAuditedFir||!useAuditedPine))
                throw new ArgumentException("Production High fog visibility requires a finite positive horizon and explicit audited trees.");
            if(useAuditedPine&&!useAuditedFir)throw new ArgumentException("Pine composition requires the audited forest.");
            if(authoredSpecies!=null)
            {
                if(authoredInstances==null||authoredSpecies.Length!=authoredInstances.Length||!useAuditedFir||!useAuditedPine)throw new ArgumentException("Explicit species require matching authored audited fir/pine instances.");
                foreach(byte type in authoredSpecies)if(type>1)throw new ArgumentException("Unknown authored forest species.");
            }
            definition=VegetationDefinition.Load();
            if(authoredInstances!=null)
            {
                if(authoredInstances.Length<1||authoredInstances.Length>definition.max_instances)throw new ArgumentException("Authored forest exceeds capacity.");
                foreach(var tree in authoredInstances)
                    if(!float.IsFinite(tree.Position.x)||!float.IsFinite(tree.Position.y)||!float.IsFinite(tree.Position.z)||!float.IsFinite(tree.Scale)||tree.Scale<.5f||tree.Scale>1.5f||!float.IsFinite(tree.DensityRank)||tree.DensityRank<0||tree.DensityRank>=1)throw new ArgumentException("Invalid authored tree.");
                instances=(VegetationInstance[])authoredInstances.Clone();
            }
            else instances=definition.Place(route);
            species=authoredSpecies==null?new byte[instances.Length]:(byte[])authoredSpecies.Clone();groups=useAuditedPine?6:3;
            if(useAuditedPine&&authoredSpecies==null)
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
                for(int lod=0;lod<3;lod++)
                {
                    int i=type*3+lod;meshes[i]=Resources.Load<Mesh>(root+name+"LOD"+lod);if(meshes[i]==null)throw new InvalidOperationException("Missing vegetation LOD.");matrices[i]=new Matrix4x4[definition.max_instances];triangles[i]=(int)meshes[i].GetIndexCount(0)/3;
                    var bounds=meshes[i].bounds;float x=Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)),z=Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z));
                    canopyRadius[type]=Mathf.Max(canopyRadius[type],Mathf.Sqrt(x*x+z*z));canopyMinY[type]=Mathf.Min(canopyMinY[type],bounds.min.y);canopyMaxY[type]=Mathf.Max(canopyMaxY[type],bounds.max.y);
                }
            }
            UsesAuditedFir=useAuditedFir;UsesAuditedPine=useAuditedPine;
            UsesSimplifiedShadows=useSimplifiedShadows;
            UsesExplicitSpecies=authoredSpecies!=null;
            this.productionHighFogEndMeters=productionHighFogEndMeters;
            localForestBounds=new Bounds(instances.Length==0?Vector3.zero:instances[0].Position,Vector3.zero);
            foreach(var tree in instances)
            {
                float reach=Mathf.Max(canopyRadius[0],canopyRadius[1])*tree.Scale+definition.wind_amplitude_m;
                float low=Mathf.Min(0,Mathf.Min(canopyMinY[0],canopyMinY[1]))*tree.Scale;
                float high=Mathf.Max(canopyMaxY[0],canopyMaxY[1])*tree.Scale;
                localForestBounds.Encapsulate(UsesExplicitSpecies?
                    new Bounds(tree.Position+Vector3.up*((low+high)*.5f),new Vector3(reach*2,high-low,reach*2)):
                    new Bounds(tree.Position+Vector3.up*4*tree.Scale,new Vector3(6,9,6)*tree.Scale));
            }
            properties=new MaterialPropertyBlock();
        }
        public void Prepare(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            if(definition==null||camera==null||!float.IsFinite(presentationTime))throw new ArgumentException("Initialized forest, camera and finite time required.");
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(transform.up,Vector3.up)<.99999f)throw new ArgumentException("Forest root supports translation/yaw only; placement owns tree scale.");
            if(observedWind.HasValue&&(!float.IsFinite(observedWind.Value.x)||!float.IsFinite(observedWind.Value.y)))throw new ArgumentException("Finite visual wind required.");
            SimplifyingCurrentShadows=UsesSimplifiedShadows&&tier!="high";
            float density=definition.Density(tier);for(int i=0;i<counts.Length;i++)counts[i]=0;SubmittedTriangles=0;ShadowSourceTriangles=0;
            preparedWorldBounds=new Bounds(transform.position,Vector3.zero);
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            var world=transform.localToWorldMatrix;var eye=camera.transform.position;float lodScale=ProjectionAwareLod.Scale(camera);bool firstBounds=true;
            // Whole-grove rejection precedes the per-tree LOD/frustum loop.
            // The sphere encloses every species, LOD and wind excursion.
            Vector3 forestCentre=world.MultiplyPoint3x4(localForestBounds.center);
            float forestRadius=localForestBounds.extents.magnitude;
            for(int p=0;p<planes.Length;p++)if(planes[p].GetDistanceToPoint(forestCentre)<-forestRadius)return;
            bool fogBoundedHigh=productionHighFogEndMeters.HasValue&&tier=="high";
            // The shader's clip-Z fog remap is certified for the standard perspective
            // matrix. Unsupported orthographic/physical/oblique projections retain
            // their complete frustum rather than discarding potentially unfogged trees.
            bool fogDepthSupported=fogBoundedHigh&&!camera.orthographic&&!camera.usePhysicalProperties&&
                SameProjection(camera.projectionMatrix,Matrix4x4.Perspective(camera.fieldOfView,camera.aspect,camera.nearClipPlane,camera.farClipPlane));
            var forward=camera.transform.forward;var absoluteForward=new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z));
            float fogDepth=fogBoundedHigh?productionHighFogEndMeters.Value+camera.nearClipPlane:0;
            preparedWorldBounds=new Bounds(transform.position,Vector3.zero);
            for(int index=0;index<instances.Length;index++)
            {
                var tree=instances[index];
                if(tree.DensityRank>=density)continue;
                var position=world.MultiplyPoint3x4(tree.Position);float distance=Vector3.Distance(position,eye);
                if(!fogBoundedHigh&&definition.Lod(distance)<0)continue;
                float lodDistance=distance*lodScale;
                int lod=fogBoundedHigh?(lodDistance>=definition.far_lod_m?2:lodDistance>=definition.near_lod_m?1:0):definition.Lod(lodDistance);
                // Includes canopy, height and maximum wind excursion. Intended sector root is unit scale.
                int type=species[index];float radius=canopyRadius[type]*tree.Scale+definition.wind_amplitude_m;
                var bounds=UsesExplicitSpecies?new Bounds(position+Vector3.up*((canopyMinY[type]+canopyMaxY[type])*.5f*tree.Scale),
                    new Vector3(radius*2,(canopyMaxY[type]-canopyMinY[type])*tree.Scale,radius*2)):new Bounds(position+Vector3.up*4*tree.Scale,new Vector3(6,9,6)*tree.Scale);
                // URP linear fog uses camera depth. Keep any canopy/wind portion in front
                // of the bound fog plane, including the conservative near-clip margin.
                if(fogDepthSupported&&Vector3.Dot(bounds.center-eye,forward)-Vector3.Dot(bounds.extents,absoluteForward)>=fogDepth)continue;
                if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                if(firstBounds){preparedWorldBounds=bounds;firstBounds=false;}else preparedWorldBounds.Encapsulate(bounds);
                int group=species[index]*3+lod;
                matrices[group][counts[group]++]=world*tree.Matrix;SubmittedTriangles+=triangles[group];
                if(lod<2)ShadowSourceTriangles+=triangles[SimplifyingCurrentShadows?group+1:group];
            }
            var direction=observedWind.HasValue?observedWind.Value.normalized:new Vector2(1,.35f);
            float strength=observedWind.HasValue?Mathf.Clamp01(observedWind.Value.magnitude/5):1;
            properties.SetVector(Wind,new Vector4(presentationTime,definition.wind_amplitude_m*strength,definition.wind_frequency,0));
            properties.SetVector(WindDirection,new Vector4(direction.x,direction.y,0,0));
            properties.SetFloat(Snow,definition.snow_coverage);
        }
        private static bool SameProjection(Matrix4x4 actual,Matrix4x4 expected)
        {for(int i=0;i<16;i++)if(actual[i]!=expected[i])return false;return true;}
        public void Render(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            Prepare(camera,tier,presentationTime,observedWind);
            for(int i=0;i<groups;i++)if(counts[i]>0)
            {
                var p=new RenderParams(materials[i/3]){camera=camera,matProps=properties,shadowCastingMode=!SimplifyingCurrentShadows&&i%3<2?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,
                    worldBounds=UsesExplicitSpecies?preparedWorldBounds:new Bounds(transform.position,new Vector3(500,50,500)),layer=gameObject.layer};
                // Default instancing includes inverse matrices; stay below the 511-instance limit
                // without relying on platform-dependent uniform-scaling shader optimizations.
                for(int offset=0;offset<counts[i];offset+=500)
                    Graphics.RenderMeshInstanced(p,meshes[i],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
                if(SimplifyingCurrentShadows&&i%3<2)
                {
                    p.shadowCastingMode=ShadowCastingMode.ShadowsOnly;p.receiveShadows=false;
                    for(int offset=0;offset<counts[i];offset+=500)
                        Graphics.RenderMeshInstanced(p,meshes[i+1],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
                }
            }
        }
    }
}
