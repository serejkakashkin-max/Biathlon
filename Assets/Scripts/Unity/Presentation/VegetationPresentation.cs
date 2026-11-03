using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Bounded, explicit-camera visual forest. No colliders or authoritative wind/terrain.</summary>
    public sealed class VegetationPresentation : MonoBehaviour
    {
        private StableSceneryShadows stableShadows;
        public Light ShadowLight{get;set;}
        public int ShadowCasters=>stableShadows?.Count??0;
        public long StableShadowTriangles=>stableShadows?.Triangles??0;
        private VegetationDefinition definition;
        private float? productionHighFogEndMeters;
        private ProductionVisibility visibility;
        private VegetationInstance[] instances;
        private Vector3[] worldPositions;
        private Bounds[] worldCanopies;
        private Matrix4x4[] worldMatrices;
        private Matrix4x4 cachedWorld;
        private bool geometryCached;
        public int GeometryBuildCount {get;private set;}
        private readonly Matrix4x4[][] matrices=new Matrix4x4[6][];
        private readonly Mesh[] meshes=new Mesh[6];
        private readonly int[] counts=new int[6];
        private readonly int[] primaryCounts=new int[6];
        private readonly Matrix4x4[][] pinnedMatrices=new Matrix4x4[2][];
        private readonly int[] pinnedCounts=new int[2];
        public int TransitionInstances{get;private set;}
        public int RenderedInstances{get;private set;}
        public bool BlendsForest=>visibility?.BlendsForest==true;
        private byte[] species;
        private readonly float[] canopyRadius=new float[2],canopyMinY=new float[2],canopyMaxY=new float[2];
        private Bounds preparedWorldBounds;
        private int groups=3;
        private Bounds localForestBounds;
        private readonly Plane[] planes=new Plane[6];
        private readonly Material[] materials=new Material[2];
        private MaterialPropertyBlock properties;
        private static readonly int Wind=Shader.PropertyToID("_Wind"), WindDirection=Shader.PropertyToID("_WindDirection"), Snow=Shader.PropertyToID("_SnowCoverage"),
            LodRanges=Shader.PropertyToID("_ForestLodRanges"),LodCamera=Shader.PropertyToID("_ForestLodCamera"),LodLevel=Shader.PropertyToID("_ForestLodLevel"),LodEnabled=Shader.PropertyToID("_ForestLodEnabled");
        public int TotalInstances => instances?.Length??0;
        public int VisibleInstances => NearCount+MidCount+FarCount;
        public int NearCount => primaryCounts[0]+primaryCounts[3]; public int MidCount => primaryCounts[1]+primaryCounts[4]; public int FarCount => primaryCounts[2]+primaryCounts[5];
        public int VisiblePines=>primaryCounts[3]+primaryCounts[4]+primaryCounts[5];
        public long SubmittedTriangles { get; private set; }
        public long ShadowSourceTriangles {get;private set;}
        private readonly int[] triangles=new int[6];
        public bool UsesAuditedFir { get; private set; }
        public bool UsesAuditedPine { get; private set; }
        public bool UsesSimplifiedShadows {get;private set;}
        public bool SimplifyingCurrentShadows {get;private set;}
        public bool UsesExplicitSpecies {get;private set;}
        public float? ProductionHighFogEndMeters=>productionHighFogEndMeters;
        public float? CompleteVisibilityFogEndMeters=>visibility?.FogEndMeters;
        public Bounds PreparedWorldBounds=>preparedWorldBounds;
        public int InstanceSpecies(int index)=>species[index];
        public Vector3 InstanceWorldPosition(int index)=>transform.TransformPoint(instances[index].Position);
        public void Initialize(Vector3[] route,bool useAuditedFir=false,Func<Vector3,float> groundHeight=null,bool useAuditedPine=false,bool useSimplifiedShadows=false,VegetationInstance[] authoredInstances=null,byte[] authoredSpecies=null,float? productionHighFogEndMeters=null,ProductionVisibility completeVisibility=null)
        {
            if(definition!=null)throw new InvalidOperationException("Forest already initialized.");
            if(productionHighFogEndMeters.HasValue&&(!float.IsFinite(productionHighFogEndMeters.Value)||productionHighFogEndMeters.Value<=0||authoredInstances==null||authoredSpecies==null||!useAuditedFir||!useAuditedPine))
                throw new ArgumentException("Production High fog visibility requires a finite positive horizon and explicit audited trees.");
            if(useAuditedPine&&!useAuditedFir)throw new ArgumentException("Pine composition requires the audited forest.");
            if(completeVisibility!=null&&(authoredInstances==null||authoredSpecies==null||!useAuditedFir||!useAuditedPine))throw new ArgumentException("Complete visibility requires explicit native tree instances and bounds.");
            if(authoredSpecies!=null)
            {
                if(authoredInstances==null||authoredSpecies.Length!=authoredInstances.Length||!useAuditedFir||!useAuditedPine)throw new ArgumentException("Explicit species require matching authored audited fir/pine instances.");
                foreach(byte type in authoredSpecies)if(type>1)throw new ArgumentException("Unknown authored forest species.");
            }
            var loaded=VegetationDefinition.Load();
            if(completeVisibility?.BlendsForest==true&&loaded.near_lod_m+completeVisibility.ForestNearBlendMeters>=loaded.far_lod_m)
                throw new ArgumentException("Forest transitions must not overlap.");
            definition=loaded;
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
                if(completeVisibility?.BlendsForest==true)pinnedMatrices[type]=new Matrix4x4[definition.max_instances];
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
            visibility=completeVisibility;
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
            if(visibility!=null){var shadowMeshes=new Mesh[groups/3];for(int i=0;i<shadowMeshes.Length;i++)shadowMeshes[i]=meshes[i*3+1];stableShadows=new StableSceneryShadows(shadowMeshes,materials,definition.max_instances);}
            properties=new MaterialPropertyBlock();
            worldPositions=new Vector3[instances.Length];worldCanopies=new Bounds[instances.Length];worldMatrices=new Matrix4x4[instances.Length];
        }
        private void PrepareWorldGeometry(Matrix4x4 world)
        {
            if(geometryCached&&cachedWorld.Equals(world))return;
            for(int index=0;index<instances.Length;index++)
            {
                var tree=instances[index];var position=world.MultiplyPoint3x4(tree.Position);int type=species[index];
                float radius=canopyRadius[type]*tree.Scale+definition.wind_amplitude_m;
                worldPositions[index]=position;worldMatrices[index]=world*tree.Matrix;
                worldCanopies[index]=UsesExplicitSpecies?new Bounds(position+Vector3.up*((canopyMinY[type]+canopyMaxY[type])*.5f*tree.Scale),
                    new Vector3(radius*2,(canopyMaxY[type]-canopyMinY[type])*tree.Scale,radius*2)):
                    new Bounds(position+Vector3.up*4*tree.Scale,new Vector3(6,9,6)*tree.Scale);
            }
            cachedWorld=world;geometryCached=true;GeometryBuildCount++;
        }
        public void Prepare(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            if(definition==null||camera==null||!float.IsFinite(presentationTime))throw new ArgumentException("Initialized forest, camera and finite time required.");
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Vector3.Dot(transform.up,Vector3.up)<.99999f)throw new ArgumentException("Forest root supports translation/yaw only; placement owns tree scale.");
            if(observedWind.HasValue&&(!float.IsFinite(observedWind.Value.x)||!float.IsFinite(observedWind.Value.y)))throw new ArgumentException("Finite visual wind required.");
            SimplifyingCurrentShadows=UsesSimplifiedShadows&&tier!="high";
            float density=definition.Density(tier);for(int i=0;i<counts.Length;i++){counts[i]=0;primaryCounts[i]=0;}
            pinnedCounts[0]=pinnedCounts[1]=0;TransitionInstances=RenderedInstances=0;SubmittedTriangles=0;ShadowSourceTriangles=0;
            preparedWorldBounds=new Bounds(transform.position,Vector3.zero);
            stableShadows?.Begin(camera,tier,ShadowLight);
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            var world=transform.localToWorldMatrix;var eye=camera.transform.position;float lodScale=ProjectionAwareLod.Scale(camera);bool firstBounds=true;
            // Whole-grove rejection precedes the per-tree LOD/frustum loop.
            // The sphere encloses every species, LOD and wind excursion.
            Vector3 forestCentre=world.MultiplyPoint3x4(localForestBounds.center);
            float forestRadius=localForestBounds.extents.magnitude;
            if(stableShadows==null)for(int p=0;p<planes.Length;p++)if(planes[p].GetDistanceToPoint(forestCentre)<-forestRadius)return;
            PrepareWorldGeometry(world);
            bool fogBounded=visibility!=null||productionHighFogEndMeters.HasValue&&tier=="high";
            // The shader's clip-Z fog remap is certified for the standard perspective
            // matrix. Unsupported orthographic/physical/oblique projections retain
            // their complete frustum rather than discarding potentially unfogged trees.
            var fogFrame=fogBounded?new ProductionVisibilityFrame(camera,visibility?.FogEndMeters??productionHighFogEndMeters.Value):default;
            preparedWorldBounds=new Bounds(transform.position,Vector3.zero);
            for(int index=0;index<instances.Length;index++)
            {
                var tree=instances[index];
                stableShadows?.Add(species[index],worldMatrices[index],worldCanopies[index]);
                if(visibility==null&&tree.DensityRank>=density)continue;
                var position=worldPositions[index];float distance=Vector3.Distance(position,eye);
                if(!fogBounded&&definition.Lod(distance)<0)continue;
                float lodDistance=distance*lodScale;
                int lod=fogBounded?(lodDistance>=definition.far_lod_m?2:lodDistance>=definition.near_lod_m?1:0):definition.Lod(lodDistance);
                // Lower tiers reduce detailed work rather than punching holes in the forest.
                if(visibility!=null&&tree.DensityRank>=density)lod=2;
                // Includes canopy, height and maximum wind excursion. Intended sector root is unit scale.
                var bounds=worldCanopies[index];
                // URP linear fog uses camera depth. Keep any canopy/wind portion in front
                // of the bound fog plane, including the conservative near-clip margin.
                if(fogBounded&&fogFrame.Hidden(bounds))continue;
                if(!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                if(firstBounds){preparedWorldBounds=bounds;firstBounds=false;}else preparedWorldBounds.Encapsulate(bounds);
                int type=species[index],group=type*3+lod;
                bool pinned=visibility!=null&&tree.DensityRank>=density;
                if(BlendsForest&&!pinned)
                {
                    float near=Mathf.SmoothStep(0,1,Mathf.InverseLerp(definition.near_lod_m,definition.near_lod_m+visibility.ForestNearBlendMeters,lodDistance));
                    float far=Mathf.SmoothStep(0,1,Mathf.InverseLerp(definition.far_lod_m,definition.far_lod_m+visibility.ForestFarBlendMeters,lodDistance));
                    // Exactly two adjacent meshes at most. The shader assigns complementary
                    // screen samples, so overlapping branches cannot double their opacity.
                    int primary=near<.5f?0:far<.5f?1:2;primaryCounts[type*3+primary]++;
                    int submissions=0;
                    if(near<1){Add(type*3,worldMatrices[index]);submissions++;}
                    if(near>0&&far<1){Add(type*3+1,worldMatrices[index]);submissions++;}
                    if(far>0){Add(type*3+2,worldMatrices[index]);submissions++;}
                    if(submissions>1)TransitionInstances++;
                }
                else
                {
                    primaryCounts[group]++;
                    if(BlendsForest&&pinned){pinnedMatrices[type][pinnedCounts[type]++]=worldMatrices[index];SubmittedTriangles+=triangles[group];RenderedInstances++;}
                    else Add(group,worldMatrices[index]);
                }
            }
            if(stableShadows!=null)ShadowSourceTriangles=stableShadows.Triangles;
            var direction=observedWind.HasValue?observedWind.Value.normalized:new Vector2(1,.35f);
            float strength=observedWind.HasValue?Mathf.Clamp01(observedWind.Value.magnitude/5):1;
            properties.SetVector(Wind,new Vector4(presentationTime,definition.wind_amplitude_m*strength,definition.wind_frequency,0));
            properties.SetVector(WindDirection,new Vector4(direction.x,direction.y,0,0));
            properties.SetFloat(Snow,definition.snow_coverage);
            if(BlendsForest)
            {
                properties.SetVector(LodRanges,new Vector4(definition.near_lod_m,visibility.ForestNearBlendMeters,definition.far_lod_m,visibility.ForestFarBlendMeters));
                properties.SetVector(LodCamera,new Vector4(eye.x,eye.y,eye.z,lodScale));
            }
        }
        private void Add(int group,Matrix4x4 matrix)
        {
            matrices[group][counts[group]++]=matrix;SubmittedTriangles+=triangles[group];RenderedInstances++;
            if(group%3<2)ShadowSourceTriangles+=triangles[SimplifyingCurrentShadows?group+1:group];
        }
        public void Render(Camera camera,string tier,float presentationTime,Vector2? observedWind=null)
        {
            Prepare(camera,tier,presentationTime,observedWind);
            for(int i=0;i<groups;i++)if(counts[i]>0)
            {
                properties.SetFloat(LodEnabled,BlendsForest?1:0);properties.SetFloat(LodLevel,i%3);
                var p=new RenderParams(materials[i/3]){camera=camera,matProps=properties,shadowCastingMode=stableShadows==null&&!SimplifyingCurrentShadows&&i%3<2?ShadowCastingMode.On:ShadowCastingMode.Off,receiveShadows=true,
                    worldBounds=UsesExplicitSpecies?preparedWorldBounds:new Bounds(transform.position,new Vector3(500,50,500)),layer=gameObject.layer};
                // Default instancing includes inverse matrices; stay below the 511-instance limit
                // without relying on platform-dependent uniform-scaling shader optimizations.
                for(int offset=0;offset<counts[i];offset+=500)
                    Graphics.RenderMeshInstanced(p,meshes[i],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
                if(stableShadows==null&&SimplifyingCurrentShadows&&i%3<2)
                {
                    p.shadowCastingMode=ShadowCastingMode.ShadowsOnly;p.receiveShadows=false;
                    for(int offset=0;offset<counts[i];offset+=500)
                        Graphics.RenderMeshInstanced(p,meshes[i+1],0,matrices[i],Math.Min(500,counts[i]-offset),offset);
                }
            }
            if(stableShadows!=null){properties.SetFloat(LodEnabled,0);stableShadows.Render(camera,properties,gameObject.layer);}
            if(BlendsForest)for(int type=0;type<groups/3;type++)if(pinnedCounts[type]>0)
            {
                properties.SetFloat(LodEnabled,0);
                var p=new RenderParams(materials[type]){camera=camera,matProps=properties,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,worldBounds=preparedWorldBounds,layer=gameObject.layer};
                for(int offset=0;offset<pinnedCounts[type];offset+=500)
                    Graphics.RenderMeshInstanced(p,meshes[type*3+2],0,pinnedMatrices[type],Math.Min(500,pinnedCounts[type]-offset),offset);
            }
        }
    }
}
