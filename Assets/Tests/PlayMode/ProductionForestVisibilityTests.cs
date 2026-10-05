using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class ProductionForestVisibilityTests
    {
        private static ProductionForestHighVisibility Policy()=>new ProductionForestHighVisibility{
            schema_version=1,id="test_high_fog_visibility_v1",mode="fog_frustum_v1"};
        private static Camera View(GameObject root)
        {
            var camera=new GameObject("Explicit visibility camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);
            camera.enabled=false;camera.transform.position=new Vector3(0,4,0);camera.fieldOfView=60;camera.aspect=16f/9;
            camera.nearClipPlane=.1f;camera.farClipPlane=400;return camera;
        }
        private static VegetationPresentation Forest(GameObject root,VegetationInstance[] trees,byte[] species,float? fog=null)
        {
            var forest=new GameObject("Authored visibility forest").AddComponent<VegetationPresentation>();forest.transform.SetParent(root.transform,false);
            forest.Initialize(null,true,null,true,true,trees,species,fog);return forest;
        }
        private static VegetationInstance Tree(float x,float z,float scale=1,float rank=.1f)=>new VegetationInstance(new Vector3(x,0,z),scale,73,rank);
        private static long Triangles(string species,int lod)=>Resources.Load<Mesh>("Biathlon/Vegetation/G2"+species+"/"+species+"LOD"+lod).GetIndexCount(0)/3;

        [Test] public void VersionedPolicyRoundTripsAndOmissionPreservesTheLegacyContract()
        {
            var fixture=new ProductionEnvironmentFixture();string legacy=fixture.Recipe.ToJson();
            Assert.That(legacy,Does.Contain("\"high_visibility\":null"));
            Assert.That(ProductionEnvironmentDefinition.FromJson(legacy).forest.high_visibility,Is.Null);
            string omitted=legacy.Replace(",\"high_visibility\":null","");Assert.That(omitted,Does.Not.Contain("high_visibility"));
            Assert.That(ProductionEnvironmentDefinition.FromJson(omitted).forest.high_visibility,Is.Null);
            string note=omitted.Substring(0,omitted.Length-1)+",\"authoring_note\":\"escaped \\\"high_visibility\\\":{} and \\\\ path\"}";
            Assert.That(ProductionEnvironmentDefinition.FromJson(note).forest.high_visibility,Is.Null,"Unrelated escaped authoring text cannot acquire policy presence.");
            Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(legacy.Replace("\"high_visibility\":null","\"high_visibility\":{}")));
            fixture.Recipe.forest.high_visibility=new ProductionForestHighVisibility();
            Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(fixture.Recipe.ToJson()),"Explicitly empty DTO must not acquire omitted-policy semantics.");
            fixture.Recipe.forest.high_visibility=Policy();var copied=ProductionEnvironmentDefinition.FromJson(fixture.Recipe.ToJson());
            fixture.Recipe.forest.high_visibility.mode="changed_after_copy";
            Assert.That(copied.forest.high_visibility.mode,Is.EqualTo("fog_frustum_v1"));
            Assert.That(copied.forest.high_visibility.id,Is.EqualTo("test_high_fog_visibility_v1"));
            copied.forest.high_visibility=null;Assert.That(ProductionEnvironmentDefinition.FromJson(copied.ToJson()).forest.high_visibility,Is.Null);
            copied.forest.high_visibility=Policy();
            foreach(int version in new[]{0,2}){copied.forest.high_visibility.schema_version=version;Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(copied.ToJson()));}
            copied.forest.high_visibility=Policy();copied.forest.high_visibility.mode="radius";
            Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(copied.ToJson()));
            copied.forest.high_visibility=Policy();copied.forest.high_visibility.id="../foreign";
            Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(copied.ToJson()));
            Assert.That(VegetationDefinition.Load().cull_m,Is.EqualTo(120));
        }

        [Test] public void HighRetainsFarAndPartialCanopiesWhileAllOtherTiersKeepExactLegacyLodsAndShadows()
        {
            var root=new GameObject("High horizon test");
            try
            {
                var camera=View(root);float fog=WeatherVisualDefinition.Load().Get("clear").fog_end_m;
                var trees=new[]{Tree(0,12),Tree(0,40),Tree(0,100),Tree(0,160),Tree(0,fog+2),Tree(0,fog+8),Tree(400,150),Tree(0,700)};
                var species=new byte[trees.Length];for(int i=0;i<species.Length;i++)species[i]=1;
                var legacy=Forest(root,trees,species);var extended=Forest(root,trees,species,fog);
                foreach(string tier in new[]{"low","balanced"})
                {
                    legacy.Prepare(camera,tier,12);extended.Prepare(camera,tier,12);
                    Assert.That(extended.VisibleInstances,Is.EqualTo(legacy.VisibleInstances));Assert.That(extended.NearCount,Is.EqualTo(legacy.NearCount));
                    Assert.That(extended.MidCount,Is.EqualTo(legacy.MidCount));Assert.That(extended.FarCount,Is.EqualTo(legacy.FarCount));
                    Assert.That(extended.SubmittedTriangles,Is.EqualTo(legacy.SubmittedTriangles));Assert.That(extended.ShadowSourceTriangles,Is.EqualTo(legacy.ShadowSourceTriangles));
                    Assert.That(extended.PreparedWorldBounds,Is.EqualTo(legacy.PreparedWorldBounds));
                }
                legacy.Prepare(camera,"high",12);extended.Prepare(camera,"high",12);
                Assert.That(legacy.VisibleInstances,Is.EqualTo(3));Assert.That(extended.VisibleInstances,Is.EqualTo(5));
                Assert.That(extended.NearCount,Is.EqualTo(1));Assert.That(extended.MidCount,Is.EqualTo(1));Assert.That(extended.FarCount,Is.EqualTo(3));
                Assert.That(extended.SubmittedTriangles,Is.EqualTo(Triangles("Pine",0)+Triangles("Pine",1)+3*Triangles("Pine",2)));
                Assert.That(extended.ShadowSourceTriangles,Is.EqualTo(legacy.ShadowSourceTriangles));Assert.That(extended.SimplifyingCurrentShadows,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void WholeNativeCanopyAndWindSupportProtectsTheFogPlaneAndPreparedBounds()
        {
            var root=new GameObject("Whole canopy horizon test");
            try
            {
                var camera=View(root);float scale=1.4f,fog=WeatherVisualDefinition.Load().Get("clear").fog_end_m;
                float radius=0,minY=0,maxY=0;
                for(int lod=0;lod<3;lod++)
                {
                    var b=Resources.Load<Mesh>("Biathlon/Vegetation/G2Fir/FirLOD"+lod).bounds;
                    float x=Mathf.Max(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x)),z=Mathf.Max(Mathf.Abs(b.min.z),Mathf.Abs(b.max.z));
                    radius=Mathf.Max(radius,Mathf.Sqrt(x*x+z*z));minY=Mathf.Min(minY,b.min.y);maxY=Mathf.Max(maxY,b.max.y);
                }
                radius=radius*scale+VegetationDefinition.Load().wind_amplitude_m;
                var partial=Tree(0,fog+radius+camera.nearClipPlane*.5f,scale);
                var hidden=Tree(0,fog+radius+camera.nearClipPlane*2,scale);
                var forest=Forest(root,new[]{partial,hidden},new byte[]{0,0},fog);forest.Prepare(camera,"high",0);
                Assert.That(forest.VisibleInstances,Is.EqualTo(1),"A pivot beyond fog is retained if any canopy support intersects the conservative fog plane.");
                var expected=new Bounds(partial.Position+Vector3.up*((minY+maxY)*.5f*scale),new Vector3(radius*2,(maxY-minY)*scale,radius*2));
                Assert.That(forest.PreparedWorldBounds,Is.EqualTo(expected));Assert.That(forest.FarCount,Is.EqualTo(1));Assert.That(forest.ShadowSourceTriangles,Is.Zero);
                camera.farClipPlane=150;forest.Prepare(camera,"high",0);Assert.That(forest.VisibleInstances,Is.Zero,"Camera far clipping is independent of the fog horizon.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void CameraDepthRatherThanPivotRadiusWorksForWideAndRotatedViewsAndRewindsWithoutAllocation()
        {
            var root=new GameObject("Wide forward-plane test");
            try
            {
                var camera=View(root);camera.fieldOfView=100;float fog=WeatherVisualDefinition.Load().Get("clear").fog_end_m;
                var trees=new[]{Tree(210,150),Tree(0,160),Tree(0,280)};var forest=Forest(root,trees,new byte[]{1,0,1},fog);
                forest.Prepare(camera,"high",12);int visible=forest.VisibleInstances;long triangles=forest.SubmittedTriangles;var bounds=forest.PreparedWorldBounds;
                Assert.That(visible,Is.EqualTo(2));Assert.That(Vector3.Distance(trees[0].Position,camera.transform.position),Is.GreaterThan(fog));
                camera.transform.rotation=Quaternion.Euler(0,180,0);forest.Prepare(camera,"high",20);Assert.That(forest.VisibleInstances,Is.Zero);
                camera.transform.rotation=Quaternion.identity;forest.Prepare(camera,"high",12);
                Assert.That(forest.VisibleInstances,Is.EqualTo(visible));Assert.That(forest.SubmittedTriangles,Is.EqualTo(triangles));Assert.That(forest.PreparedWorldBounds,Is.EqualTo(bounds));
                for(int i=0;i<20;i++)forest.Prepare(camera,"high",i);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)forest.Prepare(camera,"high",i);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                root.transform.rotation=Quaternion.Euler(0,37,0);camera.transform.localRotation=Quaternion.identity;
                forest.Prepare(camera,"high",12);Assert.That(forest.VisibleInstances,Is.EqualTo(visible),"The depth plane follows the explicit camera forward axis.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void InvalidHorizonRejectsBeforeForestMutationAndCanRetry()
        {
            foreach(float horizon in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0,-1})
            {
                var root=new GameObject("Rejected horizon");
                try
                {
                    var forest=root.AddComponent<VegetationPresentation>();var trees=new[]{Tree(0,160)};
                    Assert.Throws<ArgumentException>(()=>forest.Initialize(null,true,null,true,true,trees,new byte[]{1},horizon));
                    Assert.That(forest.TotalInstances,Is.Zero);Assert.That(forest.ProductionHighFogEndMeters,Is.Null);
                    forest.Initialize(null,true,null,true,true,trees,new byte[]{1},220);Assert.That(forest.TotalInstances,Is.EqualTo(1));
                }
                finally{Object.DestroyImmediate(root);}
            }
            var other=new GameObject("Missing authored policy binding");
            try{var forest=other.AddComponent<VegetationPresentation>();Assert.Throws<ArgumentException>(()=>forest.Initialize(VegetationPreview.Route,productionHighFogEndMeters:220));Assert.That(forest.TotalInstances,Is.Zero);forest.Initialize(VegetationPreview.Route);Assert.That(forest.ProductionHighFogEndMeters,Is.Null);}
            finally{Object.DestroyImmediate(other);}
        }

        [Test] public void UnsupportedProjectionRetainsTheWholeFrustumAndZoomKeepsExistingLodAndShadowDecisions()
        {
            var root=new GameObject("Projection-specific visibility test");
            try
            {
                var camera=View(root);camera.farClipPlane=1000;var forest=Forest(root,new[]{Tree(0,600)},new byte[]{1},220);
                forest.Prepare(camera,"high",0);Assert.That(forest.VisibleInstances,Is.Zero);
                camera.orthographic=true;camera.orthographicSize=10;forest.Prepare(camera,"high",0);
                Assert.That(forest.VisibleInstances,Is.EqualTo(1),"An uncertified orthographic fog remap must not lose potentially unfogged canopy.");
                camera.orthographic=false;camera.ResetProjectionMatrix();var oblique=camera.CalculateObliqueMatrix(new Vector4(0,0,-1,-2));camera.projectionMatrix=oblique;
                Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),new Bounds(new Vector3(0,4,600),new Vector3(6,8,6))),Is.True,"The oblique fixture itself must contain the far witness.");
                forest.Prepare(camera,"high",0);Assert.That(forest.VisibleInstances,Is.EqualTo(1),"Custom oblique depth cannot use the ordinary fog halfspace.");
                camera.ResetProjectionMatrix();camera.fieldOfView=12;camera.gameObject.AddComponent<ProjectionAwareLod>();
                var zoom=Forest(root,new[]{Tree(0,200)},new byte[]{1},220);zoom.Prepare(camera,"high",0);
                Assert.That(zoom.VisibleInstances,Is.EqualTo(1));Assert.That(zoom.MidCount,Is.EqualTo(1));Assert.That(zoom.FarCount,Is.Zero);
                Assert.That(zoom.ShadowSourceTriangles,Is.EqualTo(Triangles("Pine",1)),"The opted-in horizon preserves projection-aware LOD and its existing shadow policy.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [UnityTest] public IEnumerator ProductionColdPolicyUsesCopiedBoundWeatherAndLeavesSourcesAndPlacementImmutable()
        {
            var fixture=new ProductionEnvironmentFixture();fixture.Recipe.forest.high_visibility=Policy();fixture.Recipe.lighting.weather_preset="overcast";fixture.Recipe.lighting.fog_distance_scale=2;
            var treeFile=JsonUtility.FromJson<ProductionForestInstances>(fixture.Texts["forest_instances"]);
            treeFile.instances[0].position=new Vector3(18,-.03f,100);treeFile.instances[1].position=new Vector3(23,-.03f,105);fixture.Texts["forest_instances"]=JsonUtility.ToJson(treeFile);
            string track=fixture.Plan.Track.ToJson(),visual=fixture.Plan.Visual.ToJson(),paths=fixture.Plan.Paths.ToJson(),trees=fixture.Texts["forest_instances"];
            var root=new GameObject("Cold production horizon");var oldSky=RenderSettings.skybox;bool oldFog=RenderSettings.fog;
            try
            {
                var camera=View(root);camera.transform.position=new Vector3(18,4,-60);var environment=root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,root.AddComponent<RacePresentationFeed>(),camera);
                Assert.That(environment.Forest(0).ProductionHighFogEndMeters,Is.EqualTo(WeatherVisualDefinition.Load().Get("overcast").fog_end_m*2));
                fixture.Recipe.forest.high_visibility.mode="caller_mutation";fixture.Recipe.lighting.weather_preset="clear";fixture.Recipe.lighting.fog_distance_scale=3;
                environment.Render(camera,"high",12);Assert.That(environment.VisibleTrees,Is.EqualTo(2));Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(280));
                environment.Render(camera,"balanced",20);Assert.That(environment.VisibleTrees,Is.Zero);environment.Render(camera,"low",20);Assert.That(environment.VisibleTrees,Is.Zero);
                environment.Render(camera,"high",12);Assert.That(environment.VisibleTrees,Is.EqualTo(2));
                Assert.That(environment.Forest(0).InstanceWorldPosition(0),Is.EqualTo(treeFile.instances[0].position-fixture.Recipe.forest.groups[0].center+fixture.Recipe.forest.groups[0].center));
                Assert.That(fixture.Plan.Track.ToJson(),Is.EqualTo(track));Assert.That(fixture.Plan.Visual.ToJson(),Is.EqualTo(visual));Assert.That(fixture.Plan.Paths.ToJson(),Is.EqualTo(paths));Assert.That(fixture.Texts["forest_instances"],Is.EqualTo(trees));
                foreach(var collider in root.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False,"Presentation disables collision immediately before deferred destruction.");
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);environment.Shutdown();
                Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}
        }

        [Test] public void InvalidPolicyOrBoundWeatherRejectsTheWholeEnvironmentBeforeChildrenOrWeatherLease()
        {
            for(int failure=0;failure<4;failure++)
            {
                var fixture=new ProductionEnvironmentFixture();fixture.Recipe.forest.high_visibility=Policy();var root=new GameObject("Rejected production visibility");var oldSky=RenderSettings.skybox;bool oldFog=RenderSettings.fog;
                try
                {
                    var camera=View(root);var feed=root.AddComponent<RacePresentationFeed>();var environment=root.AddComponent<ProductionVenueEnvironment>();int children=root.transform.childCount;
                    if(failure==0)fixture.Recipe.forest.high_visibility.schema_version=2;
                    if(failure==1)fixture.Recipe.forest.high_visibility.mode="radius";
                    if(failure==2)fixture.Recipe.lighting.fog_distance_scale=float.NaN;
                    if(failure==3)fixture.Texts["Biathlon/weather_visual_v1"]=Resources.Load<TextAsset>("Biathlon/weather_visual_v1").text.Replace("\"fog_end_m\":220","\"fog_end_m\":0");
                    Assert.Throws<ArgumentException>(()=>environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera));
                    Assert.That(environment.Initialized,Is.False);Assert.That(root.transform.childCount,Is.EqualTo(children));Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));
                    fixture.Recipe.forest.high_visibility=Policy();fixture.Recipe.lighting.fog_distance_scale=1;fixture.Texts.Remove("Biathlon/weather_visual_v1");
                    environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);Assert.That(environment.Initialized,Is.True);environment.Shutdown();
                }
                finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}
            }
        }

        [UnityTest] public IEnumerator ActualProductionStadiumPolicyPreservesAllAuthoredContentAndExactLegacyCounts()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>Resources.Load<TextAsset>(id)?.text??throw new FileNotFoundException(id);
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(prefix+"manifest")),Read);var recipe=ProductionEnvironmentDefinition.FromJson(Read(prefix+"environment"));
            recipe.forest.high_visibility=Policy();string track=plan.Track.ToJson(),visual=plan.Visual.ToJson(),paths=plan.Paths.ToJson();var root=new GameObject("Actual forest horizon proof");
            try
            {
                var camera=View(root);camera.transform.position=new Vector3(110,35,-72);camera.transform.LookAt(new Vector3(0,1,8));camera.fieldOfView=64;camera.nearClipPlane=.05f;camera.farClipPlane=700;
                var environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(plan,recipe,Read,root.AddComponent<RacePresentationFeed>(),camera);
                // Bind the current inventory; retain exact per-instance and native
                // Low/Balanced comparisons instead of freezing a historical count.
                Assert.That(environment.ForestGroupCount,Is.EqualTo(recipe.forest.groups.Length));int declared=0;
                foreach(var group in recipe.forest.groups)declared+=group.count;
                Assert.That(environment.TreeCount,Is.EqualTo(declared));int high=0,expectedHigh=0;long shadows=0;
                float fog=WeatherVisualDefinition.Load().Get(recipe.lighting.weather_preset).fog_end_m*recipe.lighting.fog_distance_scale;
                var frustum=GeometryUtility.CalculateFrustumPlanes(camera);
                for(int i=0;i<environment.ForestGroupCount;i++)
                {
                    var forest=environment.Forest(i);Assert.That(forest.ProductionHighFogEndMeters,Is.EqualTo(fog));forest.Prepare(camera,"high",12);high+=forest.VisibleInstances;shadows+=forest.ShadowSourceTriangles;
                    var group=recipe.forest.groups[i];var authored=JsonUtility.FromJson<ProductionForestInstances>(Read(group.instances_resource));
                    var legacyRoot=new GameObject("Exact default group comparison");
                    try
                    {
                        legacyRoot.transform.position=group.center;var placement=new VegetationInstance[authored.instances.Length];var species=new byte[placement.Length];
                        for(int tree=0;tree<placement.Length;tree++){var value=authored.instances[tree];placement[tree]=new VegetationInstance(value.position-group.center,value.scale,value.yaw_deg,value.density_rank);species[tree]=(byte)(value.species=="pine"?1:0);}
                        var legacy=legacyRoot.AddComponent<VegetationPresentation>();legacy.Initialize(null,true,null,true,true,placement,species);
                        for(int tree=0;tree<placement.Length;tree++)
                        {
                            var pivot=forest.InstanceWorldPosition(tree);
                            Assert.That(pivot,Is.EqualTo(legacy.InstanceWorldPosition(tree)),"Published float placement must remain exactly equal to the existing adapter.");
                            float radius=0,minY=0,maxY=0;
                            for(int lod=0;lod<3;lod++)
                            {
                                var b=Resources.Load<Mesh>("Biathlon/Vegetation/G2"+(species[tree]==1?"Pine":"Fir")+"/"+(species[tree]==1?"Pine":"Fir")+"LOD"+lod).bounds;
                                float x=Mathf.Max(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x)),z=Mathf.Max(Mathf.Abs(b.min.z),Mathf.Abs(b.max.z));
                                radius=Mathf.Max(radius,Mathf.Sqrt(x*x+z*z));minY=Mathf.Min(minY,b.min.y);maxY=Mathf.Max(maxY,b.max.y);
                            }
                            float scale=placement[tree].Scale;radius=radius*scale+VegetationDefinition.Load().wind_amplitude_m;
                            var canopy=new Bounds(pivot+Vector3.up*((minY+maxY)*.5f*scale),new Vector3(radius*2,(maxY-minY)*scale,radius*2));
                            var forward=camera.transform.forward;float support=Vector3.Dot(canopy.extents,new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z)));
                            if(GeometryUtility.TestPlanesAABB(frustum,canopy)&&Vector3.Dot(canopy.center-camera.transform.position,forward)-support<fog+camera.nearClipPlane)expectedHigh++;
                        }
                        foreach(string tier in new[]{"low","balanced"}){forest.Prepare(camera,tier,12);legacy.Prepare(camera,tier,12);Assert.That(forest.VisibleInstances,Is.EqualTo(legacy.VisibleInstances));Assert.That(forest.SubmittedTriangles,Is.EqualTo(legacy.SubmittedTriangles));Assert.That(forest.ShadowSourceTriangles,Is.EqualTo(legacy.ShadowSourceTriangles));Assert.That(forest.PreparedWorldBounds,Is.EqualTo(legacy.PreparedWorldBounds));}
                    }
                    finally{Object.DestroyImmediate(legacyRoot);}
                }
                Assert.That(high,Is.EqualTo(expectedHigh),"Every current source canopy must match the native full-bound frustum/fog witness, independent of historical inventory counts.");Assert.That(high,Is.GreaterThan(0));Assert.That(shadows,Is.Zero);
                Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Visual.ToJson(),Is.EqualTo(visual));Assert.That(plan.Paths.ToJson(),Is.EqualTo(paths));
                foreach(var collider in root.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False,"No gameplay collision while native destruction is pending.");
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}
        }
    }
}
