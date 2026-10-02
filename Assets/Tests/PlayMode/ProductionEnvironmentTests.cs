using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Small adapter fixture; the original path fixture is not production world/body proof.</summary>
    public sealed class ProductionEnvironmentFixture
    {
        public readonly Dictionary<string,string> Texts;
        public readonly VenueAuthoringPlan Plan;
        public readonly ProductionEnvironmentDefinition Recipe;
        public string Read(string resource)
        {if(Texts.TryGetValue(resource,out var text))return text;var asset=Resources.Load<TextAsset>(resource);return asset!=null?asset.text:throw new FileNotFoundException(resource);}
        public ProductionEnvironmentFixture(Func<Vector3,float> ground=null)
        {
            ground=ground??(p=>-.03f);Texts=PathPresentationFixture.Resources();Texts["visual"]=Texts["visual"].Replace("\"y\":1","\"y\":0").Replace("\"y\":3","\"y\":0");Plan=PathPresentationFixture.Plan(Texts);
            Recipe=new ProductionEnvironmentDefinition
            {
                schema_version=1,id="environment_adapter_test_v1",venue_id=Plan.Manifest.Id,manifest_resource="manifest",track_resource="track",visual_resource="visual",
                terrain=new ProductionTerrainRecipe{bounds=new ProductionBounds{center=Vector3.zero,size=new Vector3(256,0,256)},tile_size_m=128,grid_spacing_m=8,blend_resource="Biathlon/terrain_blend_v1",material_resource="Biathlon/Terrain/G2Snow/WinterBlend",tiles=new ProductionTerrainTile[4]},
                route_corridors=new ProductionRouteCorridor[Plan.Visual.Count],
                forest=new ProductionForestRecipe{definition_resource="Biathlon/vegetation_v1",composition_resource="Biathlon/g2_forest_composition_v1",instance_space="world",max_instances_per_group=768,groups=new[]{new ProductionForestGroup{id="authored_forest",center=new Vector3(20,70,-30),size=new Vector3(128,50,128),count=2,instances_resource="forest_instances"}}},
                range=new ProductionRangeRecipe{geometry_resource="Biathlon/target_geometry_v1",stadium_art_resource="Biathlon/stadium_art_v1",bank_resource="Biathlon/Stadium/TargetBank",forward=new Vector3(.6f,0,.8f),origin=Vector3.zero,target_distance_m=50,target_height_m=.85f,banks=new ProductionTargetBank[2],
                    backstop=new ProductionBackstop{position=new Vector3(45,2,70),size=new Vector3(40,4,.5f),material_resource="Biathlon/Stadium/G2Event/concrete",generate_original_mesh=true}},
                assets=new[]{new ProductionPrefabPlacement{id="test_grandstand",resource="Biathlon/Stadium/G2Grandstand/Grandstand",position=new Vector3(20,0,-30),yaw_deg=180,scale=1}},
                mesh_assets=new[]{new ProductionMeshPlacement{id="native_event",mesh_resource="Biathlon/Stadium/G2Event/ScreenFrame",material_resource="Biathlon/Stadium/G2Event/Charcoal",position=new Vector3(50,0,-35),yaw_deg=30,scale=1}},
                crowd=new ProductionCrowdRecipe{mode="owner_approved_simplified",resource_root="Biathlon/Crowd",restricted_resources_allowed=false},
                lighting=new ProductionLightingRecipe{resource="Biathlon/g2_lighting_visual_v1",preset_id="clear_cold",weather_resource="Biathlon/weather_visual_v1",weather_preset="clear",sky_material_resource="Biathlon/Weather/G2Lighting/BenchmarkSky",visual_wind=new Vector2(.75f,.2f),temperature_c=-8}
            };
            for(int i=0;i<4;i++)
            {
                var tile=new ProductionTerrainTile{id="test_tile_"+i,center=new Vector3((i%2==0?-1:1)*64,0,(i/2==0?-1:1)*64),size=new Vector3(128,0,128),columns=17,rows=17,height_resource="tile_heights_"+i};Recipe.terrain.tiles[i]=tile;
                var heights=new ProductionTerrainHeights{schema_version=1,id="test_heights_"+i,venue_id=Plan.Manifest.Id,tile_id=tile.id,columns=17,rows=17,heights_m=new float[17*17]};
                for(int z=0;z<17;z++)for(int x=0;x<17;x++)heights.heights_m[z*17+x]=ground(tile.center+new Vector3(x*8-64,0,z*8-64));Texts[tile.height_resource]=JsonUtility.ToJson(heights);
            }
            for(int i=0;i<Plan.Visual.Count;i++)
            {
                var loop=Plan.Visual.Get(i);var corridor=new ProductionRouteCorridor{loop_id=loop.LoopId,source_visual_resource="visual",knots=new ProductionRouteKnot[loop.Count],widths_m=new float[loop.Count]};
                var sampler=new TrackSampler(Plan.Track.GetLoop(loop.LoopId));
                for(int k=0;k<loop.Count;k++){var knot=loop.Get(k);corridor.knots[k]=new ProductionRouteKnot{distance_m=(float)knot.Distance,x=(float)knot.X,y=(float)knot.Y,z=(float)knot.Z};corridor.widths_m[k]=(float)sampler.Sample(new TrackDistance((long)Math.Round(knot.Distance*1000))).WidthMeters;}Recipe.route_corridors[i]=corridor;
            }
            for(int i=0;i<2;i++)
            {
                var lane=Plan.Paths.GetLane(i);var point=Plan.MatPoint(i);var position=PathPresentationFixture.Position(point);
                Recipe.range.banks[i]=new ProductionTargetBank{id="world_bank_"+i,lane_id=lane.Id,mat_anchor=new ProductionAnchor{loop_id=lane.MatAnchor.LoopId,distance_m=(float)lane.MatAnchor.Distance.Meters},mat_position=position,position=position+Recipe.range.forward*50+Vector3.up*.85f,yaw_deg=Mathf.Atan2(.6f,.8f)*Mathf.Rad2Deg,scale=1,initial_stance="Standing"};
            }
            var trees=new ProductionForestInstances{schema_version=1,id="test_authored_trees",venue_id=Plan.Manifest.Id,group_id="authored_forest",instances=new[]{
                new ProductionTree{id="explicit_pine",species="pine",position=new Vector3(18,ground(new Vector3(18,0,-25)),-25),scale=1,yaw_deg=11,density_rank=.1f},
                new ProductionTree{id="explicit_fir",species="fir",position=new Vector3(23,ground(new Vector3(23,0,-20)),-20),scale=1.2f,yaw_deg=210,density_rank=.2f}}};Texts["forest_instances"]=JsonUtility.ToJson(trees);
        }
        public PackSprintFieldSession Field(RaceEventLog events)
        {
            string Core(string file)=>Read("Biathlon/"+Path.GetFileNameWithoutExtension(file));
            var profile=CompetitionProfile.FromJson(Core("sprint_test_v1.json").Replace("\"distance_m\":3000","\"distance_m\":900"));var requirements=profile.ToRouteRequirements();
            var route=new RaceRoute(Plan.Track,RaceRoutePlan.TimeTrial("environment_sprint",Plan.Track.Id,"main",requirements),requirements);
            var layout=new PhysicalRangeFieldLayout(new PhysicalTrackMap(Plan.Track,route),Plan.Track,route,Plan.Paths);
            var ski=TimeTrialAthlete.FromJson(Core("time_trial_athlete_v1.json"));var shooting=ShootingAthleteDefinition.FromJson(Core("shooting_athlete_v1.json"));shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            return new PackSprintFieldSession(new SimulationConfig(1,"environment_sim",20,123),profile,new SkiModelDefinitions(Core),new ShootingModelDefinitions(Core),Plan.Track,route,
                RaceSnowConditions.FromJson(Core("race_snow_neutral_v1.json").Replace("snow_test_packed","packed")),ResultTimingDefinition.FromJson(Core("result_timing_v1.json")),new[]{new SprintEntrant(ski,shooting,"ski_neutral")},
                new SimulationTick(1),"environment_result",OccupancyDefinition.FromJson(Core("occupancy_provisional_v1.json")),DraftDefinition.FromJson(Core("draft_provisional_v1.json")),OvertakeDefinition.FromJson(Core("overtake_provisional_v1.json")),Plan.Junctions,Plan.Connections,events,rangeLayout:layout);
        }
    }
    public class ProductionEnvironmentTests
    {
        private static Camera Camera(GameObject root,Vector3 position)
        {var go=new GameObject("Explicit environment test camera");go.transform.SetParent(root.transform,false);var camera=go.AddComponent<Camera>();camera.enabled=false;camera.transform.position=position;camera.farClipPlane=300;return camera;}
        [Test] public void SeparateCorridorsKeepLooseGapVariableWidthsAndContinuousTiledNormals()
        {
            var definition=TerrainBlendDefinition.Load();var points=new[]{new Vector3(395,0,490),new Vector3(395,0,510)};var widths=new[]{2f,6f};
            var first=new TerrainBlendCorridor(points,widths);points[0]=Vector3.zero;widths[0]=100;
            var second=new TerrainBlendCorridor(new[]{new Vector3(405,0,490),new Vector3(405,0,510)},new[]{2f,2f});var corridors=new[]{first,second};
            float Height(Vector3 p)=>70+.1f*(p.x-400)+.2f*(p.z-500);
            var left=TerrainBlendMesh.Build(corridors,new Vector3(395,30,500),new Vector2(10,20),1,definition,Height);
            var right=TerrainBlendMesh.Build(corridors,new Vector3(405,0,500),new Vector2(10,20),1,definition,Height);
            try
            {
                Assert.That(left.colors[10*11+10],Is.EqualTo(new Color(1,0,0,0)),"No artificial segment may join independent corridor endpoints through the loose gap.");
                Assert.That(first.Point(0),Is.EqualTo(new Vector3(395,0,490)));Assert.That(first.Width(0),Is.EqualTo(2));
                Assert.That(left.colors[1*11+7].g,Is.LessThan(.1f));Assert.That(left.colors[19*11+7].g,Is.GreaterThan(.9f),"The same lateral sample lies outside the narrow end and inside the wide end.");
                for(int z=0;z<=20;z++)
                {
                    var a=left.vertices[z*11+10]+new Vector3(395,30,500);var b=right.vertices[z*11]+new Vector3(405,0,500);
                    Assert.That(a,Is.EqualTo(b));Assert.That(left.colors[z*11+10],Is.EqualTo(right.colors[z*11]));Assert.That(left.normals[z*11+10],Is.EqualTo(right.normals[z*11]));Assert.That(a.y,Is.EqualTo(Height(a)).Within(1e-5));
                }
                foreach(var normal in left.normals)Assert.That(normal.y,Is.GreaterThan(.95f));
            }
            finally{Object.DestroyImmediate(left);Object.DestroyImmediate(right);}
        }
        [Test] public void ExplicitSpeciesRetainsWorldPlacementNativeBoundsAndWarmZeroAllocation()
        {
            var root=new GameObject("Highland explicit forest test");var cameraRoot=new GameObject("Highland camera test");
            try
            {
                root.transform.position=new Vector3(500,0,600);var camera=Camera(cameraRoot,new Vector3(500,78,580));camera.transform.LookAt(new Vector3(500,82,605));
                var placements=new[]{new VegetationInstance(new Vector3(0,78,0),1,10,.1f),new VegetationInstance(new Vector3(4,79,3),1.2f,80,.2f)};var species=new byte[]{1,0};
                var forest=root.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,placements,species);species[0]=0;
                Assert.That(forest.UsesExplicitSpecies,Is.True);Assert.That(forest.InstanceSpecies(0),Is.EqualTo(1));Assert.That(forest.InstanceSpecies(1),Is.Zero);Assert.That(forest.InstanceWorldPosition(0),Is.EqualTo(new Vector3(500,78,600)));
                forest.Prepare(camera,"high",0);Assert.That(forest.VisibleInstances,Is.EqualTo(2));Assert.That(forest.VisiblePines,Is.EqualTo(1));Assert.That(forest.PreparedWorldBounds.min.y,Is.GreaterThan(70));
                Assert.That(forest.PreparedWorldBounds.Contains(new Vector3(500,78,600)),Is.True);Assert.That(forest.PreparedWorldBounds.max.y,Is.GreaterThan(87));
                for(int i=0;i<20;i++)forest.Prepare(camera,"high",i);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)forest.Prepare(camera,"high",i);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                var rejected=new GameObject("Rejected species");try{var other=rejected.AddComponent<VegetationPresentation>();Assert.Throws<ArgumentException>(()=>other.Initialize(null,true,null,true,true,placements,new byte[]{2,0}));other.Initialize(null,true,null,true,true,placements,new byte[]{0,1});Assert.That(other.TotalInstances,Is.EqualTo(2));}finally{Object.DestroyImmediate(rejected);}
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(cameraRoot);}
        }
        [UnityTest] public IEnumerator ConsumerCreatesActualWorldTerrainTreesBanksAndPublicCrowdThenReleasesOwnedResources()
        {
            var fixture=new ProductionEnvironmentFixture(p=>70+.02f*p.x+.04f*p.z);var root=new GameObject("Production environment lifecycle test");var oldSky=RenderSettings.skybox;bool oldFog=RenderSettings.fog;
            try
            {
                var camera=Camera(root,new Vector3(20,72,-40));camera.transform.LookAt(new Vector3(20,76,-20));var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<ProductionVenueEnvironment>();
                string track=fixture.Plan.Track.ToJson(),paths=fixture.Plan.Paths.ToJson(),visual=fixture.Plan.Visual.ToJson();view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);
                Assert.That(view.TerrainTileCount,Is.EqualTo(4));Assert.That(view.ForestGroupCount,Is.EqualTo(1));Assert.That(view.TreeCount,Is.EqualTo(2));Assert.That(view.BankCount,Is.EqualTo(2));Assert.That(view.CrowdCount,Is.EqualTo(1));Assert.That(view.Crowd(0).UsesAuditedCrowd,Is.False);
                Assert.That(view.SampleGround(new Vector3(1.25f,0,-3.5f)),Is.EqualTo(70+.02f*1.25f+.04f*-3.5f).Within(1e-5f));
                Assert.That(Vector3.Distance(view.Forest(0).InstanceWorldPosition(0),new Vector3(18,69.36f,-25)),Is.LessThan(1e-5f));
                for(int i=0;i<2;i++){var bank=fixture.Recipe.range.banks[i];Assert.That(view.Bank(i).transform.position,Is.EqualTo(bank.position));Assert.That(Vector3.Angle(view.Bank(i).transform.forward,fixture.Recipe.range.forward),Is.LessThan(.01));}
                var mesh=view.TileMesh(0);var material=root.GetComponentInChildren<MeshRenderer>().sharedMaterial;Assert.That(material,Is.Not.SameAs(Resources.Load<Material>(fixture.Recipe.terrain.material_resource)));
                fixture.Recipe.range.banks[0].position=Vector3.zero;fixture.Recipe.terrain.tiles[0].center=Vector3.zero;Assert.That(view.Bank(0).transform.position,Is.Not.EqualTo(Vector3.zero));
                view.Render(camera,"high",12);Assert.That(RenderSettings.skybox,Is.Not.SameAs(oldSky));Assert.That(fixture.Plan.Track.ToJson(),Is.EqualTo(track));Assert.That(fixture.Plan.Paths.ToJson(),Is.EqualTo(paths));Assert.That(fixture.Plan.Visual.ToJson(),Is.EqualTo(visual));
                Assert.Throws<ArgumentException>(()=>view.Render(camera,"unknown",12));Assert.Throws<InvalidOperationException>(()=>view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera));
                view.Shutdown();Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));Assert.That(view.Initialized,Is.False);
                yield return null;Assert.That(mesh==null,Is.True);Assert.That(material==null,Is.True);Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.Destroy(root);}
        }
        [Test] public void MissingFinalAssetAndForeignBindingsRejectBeforeObjectsOrWeatherLease()
        {
            foreach(int failure in new[]{0,1,2,3,4,5,6,7,8})
            {
                var fixture=new ProductionEnvironmentFixture();var root=new GameObject("Rejected environment test");var oldSky=RenderSettings.skybox;var oldFog=RenderSettings.fog;
                try
                {
                    var camera=Camera(root,Vector3.zero);var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<ProductionVenueEnvironment>();int initialChildren=root.transform.childCount;
                    if(failure==0)fixture.Recipe.mesh_assets[0].material_resource="Biathlon/missing_final_environment_material";
                    if(failure==1)fixture.Recipe.venue_id="foreign_venue";
                    if(failure==2)fixture.Recipe.range.banks[1].position+=Vector3.right;
                    if(failure==3)fixture.Recipe.route_corridors[0].widths_m[0]+=1;
                    if(failure==4){var h=JsonUtility.FromJson<ProductionTerrainHeights>(fixture.Texts["tile_heights_1"]);h.heights_m[0]+=1;fixture.Texts["tile_heights_1"]=JsonUtility.ToJson(h);}
                    if(failure==5)fixture.Recipe.crowd.restricted_resources_allowed=true;
                    if(failure==6)fixture.Texts["visual"]=fixture.Texts["visual"].Replace("\"x\":100","\"x\":101");
                    if(failure==7)fixture.Texts.Remove("tile_heights_3");
                    if(failure==8){var h=JsonUtility.FromJson<ProductionTerrainHeights>(fixture.Texts["tile_heights_3"]);h.heights_m=new float[1];fixture.Texts["tile_heights_3"]=JsonUtility.ToJson(h);}
                    if(failure==0||failure==7)Assert.Throws<FileNotFoundException>(()=>view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera));
                    else Assert.Throws<ArgumentException>(()=>view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera));
                    Assert.That(view.Initialized,Is.False);Assert.That(root.transform.childCount,Is.EqualTo(initialChildren));Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
        [UnityTest] public IEnumerator ConcurrentWeatherOwnerRollsBackCreatedEnvironmentAndDisableRestoresTheFirstLease()
        {
            var fixture=new ProductionEnvironmentFixture();var first=new GameObject("First environment owner");var second=new GameObject("Rejected environment owner");var oldSky=RenderSettings.skybox;bool oldFog=RenderSettings.fog;
            try
            {
                var a=first.AddComponent<ProductionVenueEnvironment>();var b=second.AddComponent<ProductionVenueEnvironment>();
                var cameraA=Camera(first,new Vector3(0,3,-20));var cameraB=Camera(second,new Vector3(0,3,-20));
                a.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,first.AddComponent<RacePresentationFeed>(),cameraA);a.Render(cameraA,"high",0);var ownedSky=RenderSettings.skybox;int initialChildren=second.transform.childCount;
                Assert.Throws<InvalidOperationException>(()=>b.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,second.AddComponent<RacePresentationFeed>(),cameraB));
                Assert.That(b.Initialized,Is.False);Assert.That(second.transform.childCount,Is.EqualTo(initialChildren));Assert.That(RenderSettings.skybox,Is.SameAs(ownedSky));
                a.enabled=false;Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));
                a.enabled=true;a.Render(cameraA,"high",1);Assert.That(RenderSettings.skybox,Is.SameAs(ownedSky));
                yield return null;Assert.That(second.GetComponentsInChildren<ProductionVenueEnvironment>().Length,Is.EqualTo(1));Assert.That(second.GetComponentsInChildren<MeshRenderer>(),Is.Empty);
            }
            finally{first.GetComponent<ProductionVenueEnvironment>()?.Shutdown();second.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.Destroy(first);Object.Destroy(second);}
        }
        [UnityTest] public IEnumerator RealPersonalMatSnapshotsUpdateWorldBankWithoutSimulationMutation()
        {
            var fixture=new ProductionEnvironmentFixture();var events=new RaceEventLog(6000);var field=fixture.Field(events);var root=new GameObject("Environment bank snapshot test");
            try
            {
                var camera=Camera(root,new Vector3(0,5,-30));var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<ProductionVenueEnvironment>();view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);
                var inputs=new SprintAthleteInput[1];bool prone=false,standing=false,closed=false,returned=false;int initialMask=0;
                while(field.Tick.Value<12000&&!field.IsComplete)
                {
                    var actor=field.GetAthlete(0);var tick=field.Tick.Next();bool trigger=actor.ActiveShooting?.Phase==ShootingPhase.Aiming;
                    inputs[0]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,800),1,1,0),new WeaponAimCommand(tick,actor.AthleteId,0,0),trigger);field.Step(inputs,new[]{0.0});
                    var snapshot=RacePresentationSnapshot.Capture(field);feed.Publish(snapshot);var state=snapshot.Get(0);long beforeTick=field.Tick.Value,beforeRoute=actor.RouteNanometers,beforeMotion=actor.Motion.DistanceNanometers;int beforeEvents=events.Count;var physical=field.GetPhysicalPoint(0);var sight=actor.ActiveShooting?.Sight??default;
                    view.RefreshBanks();Assert.That(feed.Current,Is.SameAs(snapshot));Assert.That(field.Tick.Value,Is.EqualTo(beforeTick));Assert.That(actor.RouteNanometers,Is.EqualTo(beforeRoute));Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo(beforeMotion));Assert.That(events.Count,Is.EqualTo(beforeEvents));Assert.That(field.GetPhysicalPoint(0),Is.EqualTo(physical));Assert.That(actor.ActiveShooting?.Sight??default,Is.EqualTo(sight));
                    if(state.ShootingActive)
                    {
                        Assert.That(state.RangeLane,Is.EqualTo(1));Assert.That(state.LoopId,Is.EqualTo("mat_1"));
                        if(state.Stance==ShootingStance.Prone){prone=true;if(state.ClosedTargetsMask==0)Assert.That(view.Bank(0).Aperture(2).sharedMaterial,Is.SameAs(Resources.Load<Material>("Biathlon/Stadium/ProneTargetFace")));}
                        else standing=true;
                        if(state.ClosedTargetsMask!=initialMask){closed=true;initialMask=state.ClosedTargetsMask;for(int i=0;i<5;i++)if((state.ClosedTargetsMask&(1<<i))!=0)Assert.That(view.Bank(0).Aperture(i).sharedMaterial,Is.Not.SameAs(Resources.Load<Material>("Biathlon/Stadium/ProneTargetFace")));}
                    }
                    else if(prone&&state.CompletedBouts>0){returned=true;Assert.That(view.Bank(0).Aperture(2).sharedMaterial,Is.Not.SameAs(Resources.Load<Material>("Biathlon/Stadium/ProneTargetFace")));}
                    if(standing&&state.CompletedBouts>=2&&!state.ShootingActive)break;
                }
                Assert.That(prone&&standing&&closed&&returned,Is.True);yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ImportedProductionRecipeCreatesThirtyCheckedBanksAndActualHighlandGround()
        {
            const string rootResource="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id){var asset=Resources.Load<TextAsset>(id);return asset!=null?asset.text:throw new FileNotFoundException(id);}
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(rootResource+"manifest")),Read);var recipe=ProductionEnvironmentDefinition.FromJson(Read(rootResource+"environment"));var root=new GameObject("Imported production environment acceptance test");
            try
            {
                var camera=Camera(root,new Vector3(0,5,-35));var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<ProductionVenueEnvironment>();view.Initialize(plan,recipe,Read,feed,camera);
                Assert.That(view.BankCount,Is.EqualTo(30));Assert.That(view.TerrainTileCount,Is.EqualTo(recipe.terrain.tiles.Length));Assert.That(view.ForestGroupCount,Is.EqualTo(recipe.forest.groups.Length));Assert.That(view.TreeCount,Is.GreaterThan(20000));
                // The exact groomed ribbon carries the skis. Terrain now stays below its whole
                // width, checked separately by exact triangle overlay. Knot contact alone cannot
                // certify the ribbon edges; lowering affected cells may deepen the base gap.
                foreach(var loop in recipe.route_corridors)foreach(var knot in loop.knots)
                    Assert.That(knot.y-view.SampleGround(knot.Position),Is.GreaterThanOrEqualTo(.025f),"Terrain must remain beneath the exact groomed course, including climbing/highland loops.");
                for(int i=0;i<30;i++){var mat=PathPresentationFixture.Position(plan.MatPoint(i));Assert.That(view.Bank(i).transform.position,Is.EqualTo(mat+recipe.range.forward*50+Vector3.up*.85f));}
                view.Render(camera,"high",0);for(int i=0;i<view.CrowdCount;i++)Assert.That(view.Crowd(i).UsesAuditedCrowd,Is.False);
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.Destroy(root);}
        }
    }
}
