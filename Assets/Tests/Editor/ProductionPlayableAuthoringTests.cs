#if UNITY_EDITOR
using System;
using Biathlon.Unity;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.VenueEditorTests
{
    public class ProductionPlayableAuthoringTests
    {
        private const string Output="Assets/Scenes/Production/ProductionAuthoringContractTest.unity";
        [TestCase("mass_start",30)] [TestCase("sprint",60)]
        public void GeneratedSceneBindsActualCatalogEnvironmentAndPreservesCallerSceneAndSelection(string format,int count)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Output),Is.Null,"The test must not overwrite an existing scene.");
            var active=SceneManager.GetActiveScene();var selection=Selection.objects;var selected=Selection.activeObject;int opened=SceneManager.sceneCount;
            try
            {
                Assert.That(ProductionPlayableAuthoring.Generate(ProductionPlayableAuthoring.Venue01+"manifest",ProductionPlayableAuthoring.Venue01+"environment",format,count,Output),Is.EqualTo(Output));
                Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(active));Assert.That(SceneManager.sceneCount,Is.EqualTo(opened));Assert.That(Selection.objects,Is.EqualTo(selection));Assert.That(Selection.activeObject,Is.EqualTo(selected));
                var saved=EditorSceneManager.OpenPreviewScene(Output);
                try
                {
                    var roots=saved.GetRootGameObjects();Assert.That(roots.Length,Is.EqualTo(1));var host=roots[0].GetComponent<ProductionVenueHost>();
                    Assert.That(host,Is.Not.Null);Assert.That(host.AutoStart,Is.True);Assert.That(host.FieldCount,Is.EqualTo(count));
                    Assert.That(host.VenueResource,Is.EqualTo(ProductionPlayableAuthoring.Venue01+"manifest"));
                    Assert.That(host.ProfileResource,Is.EqualTo(ProductionPlayableAuthoring.Venue01+format+"_profile"));
                    Assert.That(host.RouteResource,Is.EqualTo(ProductionPlayableAuthoring.Venue01+format+"_route"));
                    Assert.That(host.EnvironmentResource,Is.EqualTo(ProductionPlayableAuthoring.Venue01+"environment"));
                    Assert.That(host.Runner,Is.Null);Assert.That(roots[0].GetComponentsInChildren<Collider>(),Is.Empty);
                }
                finally{EditorSceneManager.ClosePreviewScene(saved);}
            }
            finally{AssetDatabase.DeleteAsset(Output);Selection.objects=selection;Selection.activeObject=selected;}
        }
        [Test] public void InvalidOutputUncoveredFieldAndFreshPursuitRejectBeforeWritingScenes()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Output),Is.Null);var active=SceneManager.GetActiveScene();int count=SceneManager.sceneCount;
            Assert.Throws<ArgumentException>(()=>ProductionPlayableAuthoring.Generate(null,null,"sprint",30,"../outside.unity"));
            foreach(var format in new[]{"missing","pursuit"})Assert.Throws<ArgumentException>(()=>ProductionPlayableAuthoring.Generate(ProductionPlayableAuthoring.Venue01+"manifest",ProductionPlayableAuthoring.Venue01+"environment",format,30,Output));
            Assert.Throws<ArgumentException>(()=>ProductionPlayableAuthoring.Generate(ProductionPlayableAuthoring.Venue01+"manifest",ProductionPlayableAuthoring.Venue01+"environment","mass_start",31,Output));
            Assert.Throws<ArgumentException>(()=>ProductionPlayableAuthoring.Generate(ProductionPlayableAuthoring.Venue01+"manifest",ProductionPlayableAuthoring.Venue01+"environment","sprint",61,Output));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Output),Is.Null);Assert.That(SceneManager.sceneCount,Is.EqualTo(count));Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(active));
        }
    }
}
#endif
