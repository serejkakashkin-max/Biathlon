using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.PlayModeTests;
using Biathlon.Unity;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.VenueEditorTests
{
    public class PhysicalRangeVenueAuthoringTests
    {
        [Test] public void GeneratorWritesIndividualPathMatsAndExplicitShootingDirection()
        {
            var resources=PathPresentationFixture.Resources();string id="venue_path_authoring_test_"+Guid.NewGuid().ToString("N");
            resources["manifest"]=resources["manifest"].Replace("venue_path_presentation_test_v2",id);var plan=PathPresentationFixture.Plan(resources);
            string folder="Assets/Generated/Venues/"+id,track=plan.Track.ToJson(),paths=plan.Paths.ToJson();
            var previousSelection=Selection.objects;var previousActive=Selection.activeObject;
            Selection.activeObject=Resources.Load<TextAsset>("Biathlon/occupancy_provisional_v1");
            try
            {
                var context=new CallerContext();string output=ProductionVenueAuthoring.Generate(plan,"test/explicit_paths");context.AssertUnchanged();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(output);Assert.That(prefab,Is.Not.Null);
                int lanes=0;foreach(var marker in prefab.GetComponentsInChildren<VenueAuthoringMarker>())
                {
                    if(marker.Kind!="firing_lane")continue;int index=lanes++;
                    Assert.That(marker.StableId,Is.EqualTo("firing_lane_"+(index+1)));Assert.That(marker.Reference,Is.EqualTo(plan.LaneId(index)));
                    Assert.That(marker.transform.position,Is.EqualTo(PathPresentationFixture.Position(plan.MatPoint(index))));
                    Assert.That(Vector3.Distance(marker.transform.forward,new Vector3(.6f,0,.8f)),Is.LessThan(1e-6f));
                }
                Assert.That(lanes,Is.EqualTo(plan.LaneCount));Assert.That(lanes,Is.EqualTo(2));
                Assert.That(prefab.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(prefab.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);
                foreach(var camera in prefab.GetComponentsInChildren<Camera>())Assert.That(camera.enabled,Is.False);
                Assert.That(File.ReadAllText(folder+"/manifest.json"),Is.EqualTo(plan.Manifest.ToJson()));
                Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Paths.ToJson(),Is.EqualTo(paths));
            }
            finally
            {
                if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
                Selection.objects=previousSelection;if(Selection.activeObject!=previousActive)Selection.activeObject=previousActive;
            }
        }
        [Test] public void MissingPrefabRejectsBeforeAnyGeneratedOutput()
        {
            var resources=PathPresentationFixture.Resources();string id="venue_path_rejection_test_"+Guid.NewGuid().ToString("N");
            resources["manifest"]=resources["manifest"].Replace("venue_path_presentation_test_v2",id).Replace("\"assets\":[]","\"assets\":[{\"id\":\"missing_reference\",\"resource\":\"test/missing_venue_prefab\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":0},\"offset\":{\"x\":0,\"y\":0,\"z\":0},\"yaw_deg\":0}]");
            var plan=PathPresentationFixture.Plan(resources);string folder="Assets/Generated/Venues/"+id;
            try
            {
                var context=new CallerContext();
                Assert.Throws<FileNotFoundException>(()=>ProductionVenueAuthoring.Generate(plan,"test/rejected_paths"));
                context.AssertUnchanged();
                Assert.That(Directory.Exists(folder),Is.False);Assert.That(AssetDatabase.IsValidFolder(folder),Is.False);
            }
            finally{if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);}
        }
        [Test] public void LegacyGeneratorRetainsLoopReferencesPositionsAndIdentityMarkerRotation()
        {
            string id="venue_legacy_authoring_test_"+Guid.NewGuid().ToString("N"),folder="Assets/Generated/Venues/"+id;
            var manifest=VenueManifest.FromJson(ProductionVenueAuthoring.ReadResource(ProductionVenueAuthoring.Fixture).Replace("venue_pipeline_fixture_v1",id));
            var plan=new VenueAuthoringPlan(manifest,ProductionVenueAuthoring.ReadResource);
            try
            {
                var context=new CallerContext();string output=ProductionVenueAuthoring.Generate(plan,"test/legacy_lanes");context.AssertUnchanged();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(output);Assert.That(prefab,Is.Not.Null);int lanes=0;
                foreach(var marker in prefab.GetComponentsInChildren<VenueAuthoringMarker>())
                {
                    if(marker.Kind!="firing_lane")continue;int index=lanes++;
                    Assert.That(marker.Reference,Is.EqualTo(plan.Lanes.GetLane(index)));Assert.That(marker.transform.position,Is.EqualTo(PathPresentationFixture.Position(plan.MatPoint(index))));
                    Assert.That(marker.transform.rotation,Is.EqualTo(Quaternion.identity));
                }
                Assert.That(lanes,Is.EqualTo(30));Assert.That(lanes,Is.EqualTo(plan.Lanes.Count));
            }
            finally{if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);}
        }
        [TestCase(0,0,0)][TestCase(0,0,2)][TestCase(0,1,1)][TestCase(double.NaN,0,1)]
        public void CorruptedExplicitDirectionRejectsBeforeOutputOrEditorChanges(double x,double y,double z)
        {
            var resources=PathPresentationFixture.Resources();string id="venue_path_heading_test_"+Guid.NewGuid().ToString("N");
            resources["manifest"]=resources["manifest"].Replace("venue_path_presentation_test_v2",id);var plan=PathPresentationFixture.Plan(resources);
            // Domain parsing already rejects invalid input. Simulate a corrupted adapter-bound plan
            // to prove the generator's own boundary rejects it before doing any editor work.
            typeof(VenueVector).GetProperty(nameof(VenueVector.X)).SetValue(plan.Manifest.Range.Forward,x);
            typeof(VenueVector).GetProperty(nameof(VenueVector.Y)).SetValue(plan.Manifest.Range.Forward,y);
            typeof(VenueVector).GetProperty(nameof(VenueVector.Z)).SetValue(plan.Manifest.Range.Forward,z);
            string folder="Assets/Generated/Venues/"+id;var context=new CallerContext();
            try
            {
                Assert.Throws<ArgumentException>(()=>ProductionVenueAuthoring.Generate(plan,"test/rejected_heading"));context.AssertUnchanged();
                Assert.That(Directory.Exists(folder),Is.False);Assert.That(AssetDatabase.IsValidFolder(folder),Is.False);
            }
            finally{if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);}
        }
        private sealed class CallerContext
        {
            private readonly Scene active=SceneManager.GetActiveScene();
            private readonly int sceneCount=SceneManager.sceneCount;
            private readonly int previewCount=EditorSceneManager.previewSceneCount;
            private readonly UnityEngine.Object[] selection=Selection.objects;
            private readonly UnityEngine.Object activeSelection=Selection.activeObject;
            private readonly GameObject[] roots;
            private readonly bool dirty;
            private readonly string path;
            public CallerContext(){roots=active.GetRootGameObjects();dirty=active.isDirty;path=active.path;}
            public void AssertUnchanged()
            {
                Assert.That(SceneManager.GetActiveScene().handle,Is.EqualTo(active.handle));Assert.That(SceneManager.sceneCount,Is.EqualTo(sceneCount));
                Assert.That(EditorSceneManager.previewSceneCount,Is.EqualTo(previewCount));Assert.That(active.path,Is.EqualTo(path));Assert.That(active.isDirty,Is.EqualTo(dirty));
                Assert.That(active.GetRootGameObjects(),Is.EqualTo(roots));Assert.That(Selection.objects,Is.EqualTo(selection));Assert.That(Selection.activeObject,Is.EqualTo(activeSelection));
            }
        }
    }
}
