using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Recipe-owned world art. All gameplay is observed through detached snapshots only.</summary>
    // Explicit-camera culling must observe the final follow/orbit/replay pose.
    [DefaultExecutionOrder(200)]
    public sealed class ProductionVenueEnvironment : MonoBehaviour
    {
        private ProductionEnvironmentDefinition recipe;
        private ProductionTerrainHeights[] heights;
        private GameObject generated;
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        private Mesh[] tileMeshes;
        public RangeApronDrawPlan Apron {get;private set;}
        private VegetationPresentation[] forests;
        private SpectatorCrowd[] crowds;
        private TargetBankArt[] banks;
        public ShotImpactPresentation Impacts {get;private set;}
        private ShootingStance[] bankStances;
        private int[] bankMasks;
        private RacePresentationFeed feed;
        private Camera chase,shooting;
        private WeatherPresentation weather;
        public Func<RaceWeatherFrame?> RaceWeatherObservation {get;set;}
        private WeatherVisualDefinition atmosphericDefinition;
        public string VisualAtmosphereId=>weather?.VisualAtmosphereId;
        private ProductionForestDressing dressing;
        public RangeFlagPresentation RangeFlags {get;private set;}
        public RangeCoachesPresentation Coaches {get;private set;}
        public CourseFloodlightPresentation Floodlights {get;private set;}
        public int CoachClearanceExcludedTrees {get;private set;}
        public int FlagClearanceExcludedTrees {get;private set;}
        /// <summary>Explicit capture hosts may submit one controlled frame themselves.</summary>
        public bool AutomaticRendering {get;set;}=true;
        public ProductionForestDressing Dressing=>dressing;
        private LightingVisualPreset lighting;
        private LightingVisualPreset reviewLighting;
        public void SetLightingForReview(string id)
        {reviewLighting=id==null?null:LightingVisualDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.lighting.resource)).Get(id);}
        private TargetGeometryDefinition geometry;
        private RenderQualityPresentation quality;
        private string defaultTier;
        private long observedTick=-1;
        public bool Initialized=>recipe!=null;
        public string RecipeId=>recipe?.id;
        public string VenueId=>recipe?.venue_id;
        public string WeatherPresetId=>recipe?.lighting.weather_preset;
        public string AtmosphereOverride {get;private set;}
        public void SetAtmosphereForReview(string id)
        {if(id!=null)WeatherVisualDefinition.Load().Get(id);AtmosphereOverride=id;}
        public string AudioSnowProfile=>recipe?.terrain.snow_surface_profile;
        public Vector2 AudioWind=>recipe?.lighting.visual_wind??Vector2.zero;
        public int SubmittedSnowflakes=>weather?.SubmittedFlakes??0;
        public int TerrainTileCount=>tileMeshes?.Length??0;
        public int ForestGroupCount=>forests?.Length??0;
        public int TreeCount {get;private set;}
        public int BankCount=>banks?.Length??0;
        public int CrowdCount=>crowds?.Length??0;
        public float TerrainGridSpacing=>recipe?.terrain.grid_spacing_m??0;
        public Vector2 TerrainGridOrigin
        {
            get
            {
                if(!Initialized)throw new InvalidOperationException("Initialize the environment first.");
                var tile=recipe.terrain.tiles[0];return new Vector2(tile.center.x-tile.size.x*.5f,tile.center.z-tile.size.z*.5f);
            }
        }
        public int VisibleTrees {get;private set;}
        public long SubmittedForestTriangles {get;private set;}
        public Mesh TileMesh(int index)=>tileMeshes[index];
        public VegetationPresentation Forest(int index)=>forests[index];
        public TargetBankArt Bank(int index)=>banks[index];
        public void SetViewCamera(Camera camera)
        {if(!Initialized||camera==null)throw new ArgumentException("Initialized environment and view required.");chase=camera;shooting=null;weather.SetViewCamera(camera);}
        public SpectatorCrowd Crowd(int index)=>crowds[index];
        public void Initialize(VenueAuthoringPlan plan,ProductionEnvironmentDefinition definition,Func<string,string> read,
            RacePresentationFeed source,Camera chaseCamera,Camera shootingCamera=null,RangeApronDrawPlan apron=null)
        {
            if(Initialized||generated!=null)throw new InvalidOperationException("Initialize the production environment once.");
            if(plan==null||definition==null||read==null||source==null||chaseCamera==null)throw new ArgumentNullException();
            if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-6f)throw new ArgumentException("Enabled unit-scale environment root required.");
            if(source.Current!=null&&source.Current.TrackId!=plan.Track.Id)throw new ArgumentException("Presentation feed belongs to another venue.");
            apron?.RequireBinding(plan);
            // Detached copy protects the live presentation from subsequent caller DTO edits.
            var data=ProductionEnvironmentDefinition.FromJson(definition.ToJson());
            var prepared=Prepare(plan,data,read);
            Apron=apron;recipe=data;heights=prepared.Heights;feed=source;chase=chaseCamera;shooting=shootingCamera;lighting=prepared.Lighting;geometry=prepared.Geometry;VenueTrackId=plan.Track.Id;
            atmosphericDefinition=WeatherVisualDefinition.FromJson(Read(read,data.lighting.weather_resource));
            quality=GetComponent<RenderQualityPresentation>();defaultTier=prepared.DefaultTier;
            try
            {
                generated=new GameObject("Production environment "+data.id);generated.transform.SetParent(transform,false);
                generated.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var snow=new Material(prepared.Snow);ownedMaterials.Add(snow);prepared.Blend.Configure(snow);
                if(!string.IsNullOrEmpty(data.terrain.snow_material_preset))
                    SnowMaterialDefinition.FromJson(Read(read,"Biathlon/snow_material_v1")).ConfigureBlended(snow,data.terrain.snow_material_preset,data.terrain.snow_surface_profile);
                tileMeshes=new Mesh[data.terrain.tiles.Length];
                for(int i=0;i<tileMeshes.Length;i++)
                {
                    var tile=data.terrain.tiles[i];var mesh=TerrainBlendMesh.Build(prepared.Corridors,tile.center,new Vector2(tile.size.x,tile.size.z),data.terrain.grid_spacing_m,prepared.Blend,SampleGround);
                    // Keep the original authored world geometry in an absolute vertex buffer.
                    // Zero-pose children share one render matrix, avoiding per-tile float
                    // translations which can open cracks under a rotated host hierarchy.
                    var worldVertices=mesh.vertices;for(int vertex=0;vertex<worldVertices.Length;vertex++)worldVertices[vertex]+=tile.center;
                    if(apron!=null)
                    {
                        var weights=mesh.colors;
                        if(weights.Length!=worldVertices.Length)throw new ArgumentException("Complete native terrain weights required.");
                        for(int vertex=0;vertex<weights.Length;vertex++)weights[vertex]=apron.TerrainWeights(worldVertices[vertex],weights[vertex]);
                        mesh.colors=weights;
                    }
                    mesh.vertices=worldVertices;mesh.RecalculateBounds();ownedMeshes.Add(mesh);tileMeshes[i]=mesh;
                    var go=new GameObject(tile.id);go.transform.SetParent(generated.transform,false);
                    go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=snow;
                }
                RangeFlagDefinition flagDefinition=null;Vector3[] flagAnchors=null;
                if(!string.IsNullOrEmpty(data.range.flags_resource))
                {
                    flagDefinition=RangeFlagDefinition.FromJson(Read(read,data.range.flags_resource));
                    flagAnchors=flagDefinition.Positions(data.range,SampleGround);
                    RangeFlags=generated.AddComponent<RangeFlagPresentation>();RangeFlags.Initialize(flagDefinition,data,SampleGround);
                }
                var flagClearance=flagDefinition==null?null:new RangeFlagClearance(flagDefinition,flagAnchors);
                if(!string.IsNullOrEmpty(data.range.coaches_resource))
                {
                    var coaches=RangeCoachesDefinition.FromJson(Read(read,data.range.coaches_resource));
                    Coaches=generated.AddComponent<RangeCoachesPresentation>();Coaches.Initialize(coaches,data,SampleGround);
                }
                if(!string.IsNullOrEmpty(data.floodlights_resource))
                {
                    Floodlights=generated.AddComponent<CourseFloodlightPresentation>();
                    Floodlights.Initialize(CourseFloodlightDefinition.FromJson(Read(read,data.floodlights_resource)),data,SampleGround,point=>
                    {
                        if(Coaches!=null&&Coaches.Layout.DistanceToFootprint(point)<2)return false;
                        foreach(var group in prepared.Trees)foreach(var tree in group.instances)
                            if(new Vector2(tree.position.x-point.x,tree.position.z-point.z).sqrMagnitude<4)return false;
                        return true;
                    });
                }
                var forestList=new List<VegetationPresentation>(data.forest.groups.Length);
                for(int i=0;i<data.forest.groups.Length;i++)
                {
                    var group=data.forest.groups[i];var trees=prepared.Trees[i].instances;
                    if(flagAnchors!=null||Coaches!=null)
                    {
                        var retained=new List<ProductionTree>();
                        foreach(var tree in trees)
                        {
                            if(flagClearance!=null&&!flagClearance.Clears(tree)){FlagClearanceExcludedTrees++;continue;}
                            if(Coaches!=null&&!Coaches.ClearsTree(tree)){CoachClearanceExcludedTrees++;continue;}
                            retained.Add(tree);
                        }
                        trees=retained.ToArray();
                    }
                    if(trees.Length==0)continue;
                    var placement=new VegetationInstance[trees.Length];var species=new byte[trees.Length];
                    for(int t=0;t<trees.Length;t++){var tree=trees[t];placement[t]=new VegetationInstance(tree.position-group.center,tree.scale,tree.yaw_deg,tree.density_rank);species[t]=(byte)(tree.species=="pine"?1:0);}
                    var go=Child(group.id,group.center,Quaternion.identity);var forest=go.AddComponent<VegetationPresentation>();
                    forest.Initialize(null,true,null,true,true,placement,species,prepared.HighForestFogEndMeters,prepared.Visibility);forestList.Add(forest);TreeCount+=trees.Length;
                }
                forests=forestList.ToArray();
                var crowdList=new List<SpectatorCrowd>();
                var staticRenderers=new List<MeshRenderer>();
                for(int i=0;i<data.assets.Length;i++)
                {
                    var asset=data.assets[i];var go=Instantiate(prepared.Prefabs[i],generated.transform,false);go.name=asset.id;go.transform.SetPositionAndRotation(asset.position,Quaternion.Euler(0,asset.yaw_deg,0));
                    RemoveColliders(go);var seating=go.GetComponentInChildren<SpectatorSeating>();
                    staticRenderers.AddRange(go.GetComponentsInChildren<MeshRenderer>());
                    if(seating!=null&&seating.Pose==SpectatorPose.Standing)
                    {
                        // These are clone-owned roadside feet anchors, not rigid
                        // seating platforms. Each person stands on the actual
                        // captured terrain instead of floating over a slope.
                        var anchors=seating.CopyAnchors();
                        float bottom=Require<Mesh>("Biathlon/Crowd/StandingSpectatorLOD0").bounds.min.y;
                        for(int anchor=0;anchor<anchors.Length;anchor++)
                        {
                            var foot=seating.transform.TransformPoint(anchors[anchor]);
                            foot.y=SampleGround(foot)-bottom;
                            // Project height only. A yaw inverse round-trip can
                            // otherwise perturb the exact authored X/Z float bits.
                            var local=seating.transform.InverseTransformPoint(foot);
                            anchors[anchor]=new Vector3(anchors[anchor].x,local.y,anchors[anchor].z);
                        }
                        seating.Configure(anchors,SpectatorPose.Standing);
                    }
                    if(seating!=null){var crowd=go.AddComponent<SpectatorCrowd>();crowd.Initialize(seating,true,false,prepared.Visibility);crowdList.Add(crowd);}
                }
                crowds=crowdList.ToArray();
                for(int i=0;i<data.mesh_assets.Length;i++)
                {
                    var asset=data.mesh_assets[i];var go=Child(asset.id,asset.position,Quaternion.Euler(0,asset.yaw_deg,0));go.transform.localScale=new Vector3(asset.scale*asset.EffectiveLateralScale,asset.scale,asset.scale);
                    go.AddComponent<MeshFilter>().sharedMesh=prepared.Meshes[i];go.AddComponent<MeshRenderer>().sharedMaterial=prepared.Materials[i];
                    staticRenderers.Add(go.GetComponent<MeshRenderer>());
                }
                if(data.range.backstop!=null)
                {
                    var backstop=data.range.backstop;var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Production backstop";go.transform.SetParent(generated.transform,false);
                    go.transform.SetPositionAndRotation(backstop.position,Quaternion.LookRotation(data.range.forward,Vector3.up));go.transform.localScale=backstop.size;
                    go.GetComponent<MeshRenderer>().sharedMaterial=prepared.Backstop;RemoveColliders(go);
                }
                banks=new TargetBankArt[data.range.banks.Length];bankStances=new ShootingStance[banks.Length];bankMasks=new int[banks.Length];
                for(int i=0;i<banks.Length;i++)
                {
                    var bank=data.range.banks[i];var art=Instantiate(prepared.Bank,generated.transform,false);art.name=bank.id;
                    art.transform.SetPositionAndRotation(bank.position,Quaternion.LookRotation(data.range.forward,Vector3.up));RemoveColliders(art.gameObject);
                    art.Apply(geometry,ShootingStance.Standing,0);banks[i]=art;bankStances[i]=ShootingStance.Standing;
                }
                if(Floodlights!=null)
                {
                    var illuminated=new Dictionary<Material,Material>();
                    var template=Require<Material>("Biathlon/Weather/IlluminatedVenueLit");
                    foreach(var renderer in staticRenderers)
                    {
                        var originals=renderer.sharedMaterials;bool changed=false;
                        for(int i=0;i<originals.Length;i++)
                        {
                            var sourceMaterial=originals[i];if(sourceMaterial==null||sourceMaterial.shader.name!="Universal Render Pipeline/Lit")continue;
                            if(!illuminated.TryGetValue(sourceMaterial,out var replacement))
                            {
                                replacement=new Material(sourceMaterial);replacement.shader=template.shader;
                                replacement.name=sourceMaterial.name+" (venue illumination)";illuminated.Add(sourceMaterial,replacement);ownedMaterials.Add(replacement);
                            }
                            originals[i]=replacement;changed=true;
                        }
                        if(changed)renderer.sharedMaterials=originals;
                    }
                }
                generated.AddComponent<StaticSceneryBatches>().Initialize(staticRenderers);
                var composition=G2ForestComposition.Load();
                if(data.venue_id=="woodland_basin_v1"&&!string.IsNullOrEmpty(composition.dressing_resource))
                {dressing=generated.AddComponent<ProductionForestDressing>();dressing.Initialize(WinterDressingDefinition.Load(composition.dressing_resource),data.route_corridors,SampleGround,(point,radius)=>Coaches==null||Coaches.Layout.DistanceToFootprint(point)>radius+3,prepared.Visibility);}
                var keyObject=Child("Production directional light",Vector3.zero,Quaternion.identity);var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;
                foreach(var forest in forests)forest.ShadowLight=key;foreach(var crowd in crowds)crowd.ShadowLight=key;if(dressing!=null)dressing.ShadowLight=key;
                weather=generated.AddComponent<WeatherPresentation>();weather.Initialize(chase,key,prepared.Sky);
                Impacts=gameObject.AddComponent<ShotImpactPresentation>();Impacts.Initialize(feed,geometry,banks);
                RefreshBanks();
            }
            catch{Shutdown();throw;}
        }
        private sealed class Prepared
        {
            internal ProductionTerrainHeights[] Heights;internal ProductionForestInstances[] Trees;internal TerrainBlendCorridor[] Corridors;
            internal TerrainBlendDefinition Blend;internal LightingVisualPreset Lighting;internal TargetGeometryDefinition Geometry;
            internal string DefaultTier;
            internal float? HighForestFogEndMeters;
            internal ProductionVisibility Visibility;
            internal Material Snow,Sky,Backstop;internal TargetBankArt Bank;internal GameObject[] Prefabs;internal Mesh[] Meshes;internal Material[] Materials;
        }
        private static Prepared Prepare(VenueAuthoringPlan plan,ProductionEnvironmentDefinition data,Func<string,string> read)
        {
            if(!plan.UsesPhysicalPaths||data.venue_id!=plan.Manifest.Id||data.track_resource!=plan.Manifest.TrackResource||data.visual_resource!=plan.Manifest.VisualResource||
                VenueManifest.FromJson(Read(read,data.manifest_resource)).ToJson()!=plan.Manifest.ToJson()||
                TrackDefinition.FromJson(Read(read,data.track_resource)).ToJson()!=plan.Track.ToJson()||
                TrackVisualDefinition.FromJson(Read(read,data.visual_resource),plan.Track).ToJson()!=plan.Visual.ToJson())throw new ArgumentException("Environment must bind the exact physical venue source.");
            if(data.forest.definition_resource!="Biathlon/vegetation_v1"||data.range.geometry_resource!="Biathlon/target_geometry_v1"||data.range.stadium_art_resource!="Biathlon/stadium_art_v1"||data.lighting.weather_resource!="Biathlon/weather_visual_v1")
                throw new ArgumentException("Environment references unsupported core presentation definitions.");
            var p=new Prepared{Heights=new ProductionTerrainHeights[data.terrain.tiles.Length],Trees=new ProductionForestInstances[data.forest.groups.Length],Corridors=new TerrainBlendCorridor[data.route_corridors.Length]};
            p.Blend=TerrainBlendDefinition.FromJson(Read(read,data.terrain.blend_resource));var vegetation=VegetationDefinition.FromJson(Read(read,data.forest.definition_resource));
            Read(read,data.forest.composition_resource);p.DefaultTier=RenderQualityDefinition.FromJson(Read(read,"Biathlon/render_quality_v1")).default_tier;
            if(data.forest.max_instances_per_group>vegetation.max_instances)throw new ArgumentException("Forest exceeds the native instance capacity.");
            for(int i=0;i<p.Heights.Length;i++)p.Heights[i]=ProductionTerrainHeights.Read(Read(read,data.terrain.tiles[i].height_resource),data.terrain.tiles[i],data.venue_id);
            ValidateTiles(data,p.Heights);
            if(data.route_corridors.Length!=plan.Visual.Count)throw new ArgumentException("Every physical visual loop needs its own corridor.");var loopIds=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<p.Corridors.Length;i++)
            {
                var corridor=data.route_corridors[i];if(corridor.source_visual_resource!=data.visual_resource||!loopIds.Add(corridor.loop_id))throw new ArgumentException("Duplicate or foreign terrain corridor.");
                var visual=plan.Visual.Get(corridor.loop_id);var sampler=new TrackSampler(plan.Track.GetLoop(corridor.loop_id));if(visual.Count!=corridor.knots.Length)throw new ArgumentException("Terrain corridor knots must match the venue.");
                var points=new Vector3[visual.Count];
                for(int k=0;k<points.Length;k++)
                {
                    var knot=visual.Get(k);var authored=corridor.knots[k];points[k]=new Vector3((float)knot.X,(float)knot.Y,(float)knot.Z);
                    if(Math.Abs(authored.distance_m-knot.Distance)>.002||(points[k]-authored.Position).sqrMagnitude>1e-6f||
                        Math.Abs(corridor.widths_m[k]-sampler.Sample(new TrackDistance((long)Math.Round(knot.Distance*1000))).WidthMeters)>1e-4)throw new ArgumentException("Terrain corridor differs from the authored physical course.");
                    if(!InsideTerrain(data.terrain,points[k]))throw new ArgumentException("Physical corridor lies outside the terrain tiles.");
                }
                p.Corridors[i]=new TerrainBlendCorridor(points,corridor.widths_m);
            }
            for(int i=0;i<p.Trees.Length;i++)
            {
                var group=data.forest.groups[i];p.Trees[i]=ProductionForestInstances.Read(Read(read,group.instances_resource),group,data.venue_id);
                foreach(var tree in p.Trees[i].instances)
                    if(!InsideTerrain(data.terrain,tree.position)||Mathf.Abs(SampleGround(data,p.Heights,tree.position)-tree.position.y)>.02f)throw new ArgumentException("Tree must use the actual terrain world height.");
            }
            p.Geometry=TargetGeometryDefinition.FromJson(Read(read,data.range.geometry_resource));var stadium=StadiumArtDefinition.FromJson(Read(read,data.range.stadium_art_resource));
            var forward=new Vector3((float)plan.Manifest.Range.Forward.X,0,(float)plan.Manifest.Range.Forward.Z);
            if(data.range.banks.Length!=plan.LaneCount||(data.range.forward-forward).sqrMagnitude>1e-6f||Math.Abs(data.range.target_distance_m-p.Geometry.DistanceMeters)>1e-5f||Math.Abs(data.range.target_height_m-stadium.target_height_m)>1e-5f)
                throw new ArgumentException("Target banks must preserve canonical geometry and explicit range direction.");
            for(int i=0;i<data.range.banks.Length;i++)
            {
                var bank=data.range.banks[i];var lane=plan.Paths.GetLane(i);var mat=plan.MatPoint(i);var position=new Vector3((float)mat.X,(float)mat.Y,(float)mat.Z);
                if(bank.lane_id!=plan.LaneId(i)||bank.mat_anchor.loop_id!=lane.MatAnchor.LoopId||Math.Abs(bank.mat_anchor.distance_m-lane.MatAnchor.Distance.Meters)>.002||
                    (bank.mat_position-position).sqrMagnitude>1e-6f||(bank.position-(position+forward*(float)(p.Geometry.DistanceMeters+plan.FiringDatumDepth)+Vector3.up*data.range.target_height_m)).sqrMagnitude>1e-6f||
                    Vector3.Angle(Quaternion.Euler(0,bank.yaw_deg,0)*Vector3.forward,forward)>.01f)throw new ArgumentException("Target bank does not bind its actual personal mat.");
            }
            p.Lighting=LightingVisualDefinition.FromJson(Read(read,data.lighting.resource)).Get(data.lighting.preset_id);
            var weatherDefinition=WeatherVisualDefinition.FromJson(Read(read,data.lighting.weather_resource));var weatherPreset=weatherDefinition.Get(data.lighting.weather_preset);
            if(data.forest.high_visibility!=null||!string.IsNullOrEmpty(data.visibility_resource))
            {
                float horizon=weatherPreset.fog_end_m;
                if(weatherPreset.id=="snowfall"&&weatherDefinition.snowfall_cycle_s>0)horizon=Mathf.Max(horizon,weatherDefinition.Get("light_snow").fog_end_m);
                if(data.forest.high_visibility!=null)p.HighForestFogEndMeters=horizon*data.lighting.fog_distance_scale;
                if(!string.IsNullOrEmpty(data.visibility_resource))p.Visibility=ProductionVisibilityDefinition.FromJson(Read(read,data.visibility_resource)).Bind(horizon*data.lighting.fog_distance_scale);
            }
            p.Snow=Require<Material>(data.terrain.material_resource);p.Sky=Require<Material>(data.lighting.sky_material_resource);p.Bank=Require<TargetBankArt>(data.range.bank_resource);
            for(int i=0;i<5;i++)if(p.Bank.Aperture(i)==null)throw new ArgumentException("Native target bank has an unbound face.");
            Require<Material>("Biathlon/Stadium/ProneTargetFace");Require<Material>("Biathlon/Snow/Spray");SnowMaterialDefinition.FromJson(Read(read,"Biathlon/snow_material_v1"));
            if(p.Snow.shader.name!="Biathlon/Snow")throw new ArgumentException("Native snow shader required.");
            for(int species=0;species<2;species++)
            {
                string root=species==0?"Biathlon/Vegetation/G2Fir/":"Biathlon/Vegetation/G2Pine/",name=species==0?"Fir":"Pine";
                Require<Material>(root+name);for(int lod=0;lod<3;lod++)Require<Mesh>(root+name+"LOD"+lod);
            }
            var crowd=SpectatorCrowdSettings.FromJson(Read(read,"Biathlon/spectator_crowd_v1"));Require<Material>("Biathlon/Crowd/Spectator");Require<Mesh>("Biathlon/Crowd/SpectatorLOD0");Require<Mesh>("Biathlon/Crowd/SpectatorLOD1");
            if(p.Visibility!=null){Require<Mesh>("Biathlon/Crowd/SpectatorLOD2");Require<Mesh>("Biathlon/Crowd/StandingSpectatorLOD2");}
            p.Prefabs=new GameObject[data.assets.Length];for(int i=0;i<p.Prefabs.Length;i++)
            {
                var prefab=Require<GameObject>(data.assets[i].resource);if(prefab.GetComponentInChildren<SpectatorCrowd>(true)!=null)throw new ArgumentException("Environment owns crowd initialization.");
                var seating=prefab.GetComponentInChildren<SpectatorSeating>();
                if(seating!=null)
                {
                    if(seating.Count<1||seating.Count>crowd.max_instances)throw new ArgumentException("Prefab seating exceeds the native crowd capacity.");
                    if(seating.Pose==SpectatorPose.Standing)
                    {Require<Mesh>("Biathlon/Crowd/StandingSpectatorLOD0");Require<Mesh>("Biathlon/Crowd/StandingSpectatorLOD1");}
                    else if(seating.Pose!=SpectatorPose.Seated)throw new ArgumentException("Unknown prefab spectator pose.");
                }
                p.Prefabs[i]=prefab;
            }
            p.Meshes=new Mesh[data.mesh_assets.Length];p.Materials=new Material[p.Meshes.Length];for(int i=0;i<p.Meshes.Length;i++){p.Meshes[i]=Require<Mesh>(data.mesh_assets[i].mesh_resource);p.Materials[i]=Require<Material>(data.mesh_assets[i].material_resource);}
            if(data.range.backstop!=null)p.Backstop=Require<Material>(data.range.backstop.material_resource);return p;
        }
        private static string Read(Func<string,string> read,string resource){ProductionEnvironmentDefinition.Resource(resource);return read(resource)??throw new FileNotFoundException("Missing environment text: "+resource);}
        private static T Require<T>(string resource)where T:Object{ProductionEnvironmentDefinition.Resource(resource);return Resources.Load<T>(resource)??throw new FileNotFoundException("Missing environment asset: "+resource);}
        private static bool InsideTerrain(ProductionTerrainRecipe terrain,Vector3 point)
        {foreach(var tile in terrain.tiles)if(Mathf.Abs(point.x-tile.center.x)<=tile.size.x*.5f+.001f&&Mathf.Abs(point.z-tile.center.z)<=tile.size.z*.5f+.001f)return true;return false;}
        private static void ValidateTiles(ProductionEnvironmentDefinition data,ProductionTerrainHeights[] heights)
        {
            var tiles=data.terrain.tiles;float half=data.terrain.tile_size_m*.5f;
            for(int i=0;i<tiles.Length;i++)
            {
                var a=tiles[i];if(Mathf.Abs(a.center.x-data.terrain.bounds.center.x)+half>data.terrain.bounds.size.x*.5f+.001f||Mathf.Abs(a.center.z-data.terrain.bounds.center.z)+half>data.terrain.bounds.size.z*.5f+.001f)throw new ArgumentException("Tile exceeds the declared terrain bounds.");
                for(int j=0;j<i;j++)
                {
                    var b=tiles[j];float dx=a.center.x-b.center.x,dz=a.center.z-b.center.z;
                    if(Mathf.Abs(dx)<data.terrain.tile_size_m-.001f&&Mathf.Abs(dz)<data.terrain.tile_size_m-.001f)throw new ArgumentException("Overlapping terrain tiles.");
                    if(Mathf.Abs(dz)<.001f&&Mathf.Abs(Mathf.Abs(dx)-data.terrain.tile_size_m)<.001f)
                        for(int z=0;z<a.rows;z++){int ax=dx>0?0:a.columns-1,bx=dx>0?b.columns-1:0;if(Mathf.Abs(heights[i].heights_m[z*a.columns+ax]-heights[j].heights_m[z*b.columns+bx])>1e-5f)throw new ArgumentException("Terrain X border is discontinuous.");}
                    if(Mathf.Abs(dx)<.001f&&Mathf.Abs(Mathf.Abs(dz)-data.terrain.tile_size_m)<.001f)
                        for(int x=0;x<a.columns;x++){int az=dz>0?0:a.rows-1,bz=dz>0?b.rows-1:0;if(Mathf.Abs(heights[i].heights_m[az*a.columns+x]-heights[j].heights_m[bz*b.columns+x])>1e-5f)throw new ArgumentException("Terrain Z border is discontinuous.");}
                }
            }
        }
        public float SampleGround(Vector3 world)
        {if(!Initialized)throw new InvalidOperationException("Initialize the environment first.");ProductionEnvironmentDefinition.Position(world);return SampleGround(recipe,heights,world);}
        private static float SampleGround(ProductionEnvironmentDefinition data,ProductionTerrainHeights[] heights,Vector3 world)
        {
            int best=0;float nearest=float.PositiveInfinity;
            for(int i=0;i<data.terrain.tiles.Length;i++)
            {
                var tile=data.terrain.tiles[i];float dx=Mathf.Max(0,Mathf.Abs(world.x-tile.center.x)-tile.size.x*.5f),dz=Mathf.Max(0,Mathf.Abs(world.z-tile.center.z)-tile.size.z*.5f),distance=dx*dx+dz*dz;
                if(distance<nearest){nearest=distance;best=i;if(distance==0)break;}
            }
            var chosen=data.terrain.tiles[best];var field=heights[best];float gx=Mathf.Clamp01((world.x-chosen.center.x)/chosen.size.x+.5f)*(chosen.columns-1),gz=Mathf.Clamp01((world.z-chosen.center.z)/chosen.size.z+.5f)*(chosen.rows-1);
            int x=Math.Min((int)gx,chosen.columns-2),z=Math.Min((int)gz,chosen.rows-2),index=z*chosen.columns+x;float fx=gx-x,fz=gz-z;
            float h00=field.heights_m[index],h10=field.heights_m[index+1],h01=field.heights_m[index+chosen.columns],h11=field.heights_m[index+chosen.columns+1];
            return fx+fz<=1?h00+(h10-h00)*fx+(h01-h00)*fz:h11+(h01-h11)*(1-fx)+(h10-h11)*(1-fz);
        }
        private GameObject Child(string name,Vector3 world,Quaternion rotation)
        {var go=new GameObject(name);go.transform.SetParent(generated.transform,false);go.transform.SetPositionAndRotation(world,rotation);return go;}
        private static void RemoveColliders(GameObject go){foreach(var collider in go.GetComponentsInChildren<Collider>(true)){collider.enabled=false;DestroyOwned(collider);}}
        public void RefreshBanks()
        {
            if(!Initialized)throw new InvalidOperationException("Initialize the environment first.");var snapshot=feed.Current;if(snapshot==null||snapshot.Tick==observedTick)return;
            if(snapshot.TrackId!=VenueTrackId)throw new ArgumentException("Presentation snapshot belongs to another venue.");observedTick=snapshot.Tick;
            for(int b=0;b<banks.Length;b++)
            {
                ShootingStance stance=ShootingStance.Standing;int mask=0;
                for(int i=0;i<snapshot.Count;i++){var athlete=snapshot.Get(i);if(athlete.ShootingActive&&athlete.RangeLane==b+1){stance=athlete.Stance;mask=athlete.ClosedTargetsMask;break;}}
                if(bankStances[b]!=stance||bankMasks[b]!=mask){banks[b].Apply(geometry,stance,mask);bankStances[b]=stance;bankMasks[b]=mask;}
            }
        }
        private string VenueTrackId;
        public void Render(Camera camera,string tier,float time)
        {
            using var profile=RuntimeFrameProfile.Environment.Auto();
            if(!Initialized||!isActiveAndEnabled||camera==null||!float.IsFinite(time)||Mathf.Abs(time)>1000000||(tier!="high"&&tier!="balanced"&&tier!="low"))throw new ArgumentException("Initialized environment, explicit camera and render tier/time required.");
            var observed=RaceWeatherObservation?.Invoke();weather.RaceObservation=observed;var observedWind=observed.HasValue?new Vector2((float)observed.Value.Wind.EastMetersPerSecond,(float)observed.Value.Wind.NorthMetersPerSecond):recipe.lighting.visual_wind;
            RangeFlags?.Render(time,observedWind);
            RefreshBanks();VisibleTrees=0;SubmittedForestTriangles=0;
            foreach(var forest in forests){forest.Render(camera,tier,time,observedWind);VisibleTrees+=forest.VisibleInstances;SubmittedForestTriangles+=forest.SubmittedTriangles;}
            foreach(var crowd in crowds)crowd.Render(camera,tier,time);
            dressing?.Render(camera,tier);
            weather.SetViewCamera(camera);
            string atmosphere=recipe.lighting.weather_preset;
            if(atmosphere=="overcast"&&recipe.terrain.snow_material_preset=="wet"&&recipe.lighting.temperature_c>=atmosphericDefinition.wet_haze_temperature_c)
                atmosphere="winter_haze";
            weather.Render(AtmosphereOverride??atmosphere,tier,time,observedWind,SampleGround(camera.transform.position),reviewLighting??lighting,recipe.lighting.fog_distance_scale,
                AtmosphereOverride==null&&atmosphere=="snowfall"?atmosphericDefinition.LightSnowBlend(time):0);
            Floodlights?.Render(camera,(reviewLighting??lighting).floodlights_gain);
        }
        private void LateUpdate(){if(Initialized&&AutomaticRendering)Render(shooting!=null&&shooting.isActiveAndEnabled?shooting:chase,quality!=null?quality.TierId:defaultTier,Time.time);}
        private void OnDisable(){if(generated!=null)generated.SetActive(false);}
        private void OnEnable(){if(generated!=null)generated.SetActive(true);}
        public void Shutdown()
        {
            if(Impacts!=null){Impacts.Shutdown();DestroyOwned(Impacts);Impacts=null;}
            RangeFlags?.Shutdown();RangeFlags=null;FlagClearanceExcludedTrees=0;reviewLighting=null;
            Coaches?.Shutdown();Coaches=null;CoachClearanceExcludedTrees=0;
            Floodlights?.Shutdown();Floodlights=null;
            if(weather!=null)weather.Shutdown();
            if(generated!=null){generated.SetActive(false);generated.transform.SetParent(null);DestroyOwned(generated);generated=null;}
            foreach(var mesh in ownedMeshes)DestroyOwned(mesh);foreach(var material in ownedMaterials)DestroyOwned(material);ownedMeshes.Clear();ownedMaterials.Clear();
            Apron=null;recipe=null;heights=null;feed=null;chase=null;shooting=null;weather=null;lighting=null;geometry=null;quality=null;defaultTier=null;tileMeshes=null;forests=null;crowds=null;banks=null;bankStances=null;bankMasks=null;observedTick=-1;VenueTrackId=null;TreeCount=VisibleTrees=0;SubmittedForestTriangles=0;
        }
        private static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Shutdown();
    }
}

