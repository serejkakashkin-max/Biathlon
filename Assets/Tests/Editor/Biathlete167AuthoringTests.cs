using System;
using System.Linq;
using System.Reflection;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Biathlon.EditorTests
{
    public sealed class Biathlete167AuthoringTests
    {
        private const string Preview="Assets/Biathlon/Characters/Biathlete_16_7/Preview/Biathlete_16_7_Preview.unity";
        private SceneSetup[] setup;
        private MethodInfo generate;
        [SetUp] public void OpenCleanPreview()
        {
            Assert.That(Enumerable.Range(0,SceneManager.sceneCount).All(i=>!SceneManager.GetSceneAt(i).isDirty),Is.True,"Save existing scenes before this authoring test.");
            setup=EditorSceneManager.GetSceneManagerSetup();
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Biathlon.Unity.Biathlete167Authoring")).First(t=>t!=null);
            generate=type.GetMethod("GenerateScene",BindingFlags.Static|BindingFlags.NonPublic);
            EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
        }
        [TearDown] public void RestoreSceneSetup()
        {
            if(setup==null)return;
            // The dirty-scene test deliberately adds an unsaved sentinel.
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            for(int i=SceneManager.sceneCount-1;i>=0;i--){var scene=SceneManager.GetSceneAt(i);if(scene!=scratch)EditorSceneManager.CloseScene(scene,true);}
            // EditMode's runner can start with no scene setup at all.
            if(setup.Length==0||!setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        [Test] public void RegeneratingAnOpenedPreviewKeepsOneSavedStudio()
        {
            for(int repeat=0;repeat<2;repeat++)
            {
                generate.Invoke(null,new object[]{"Assets/Biathlon/Characters/Biathlete_16_7/Prefabs/Biathlete_16_7_Natural.prefab"});
                var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
                Assert.That(scenes.Count(s=>s.path==Preview),Is.EqualTo(1));
                var preview=scenes.Single(s=>s.path==Preview);Assert.That(preview.isDirty,Is.False);
                Assert.That(preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Biathlete167Studio>(true)).Count(),Is.EqualTo(1));
                Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(Preview));
            }
        }
        [Test] public void RegenerationPreservesAnUnsavedPreview()
        {
            var sentinel=new GameObject("Unsaved authoring sentinel");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            var error=Assert.Throws<TargetInvocationException>(()=>generate.Invoke(null,new object[]{"unused.prefab"}));
            Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(error.InnerException.Message,Does.Contain("dirty character preview"));
            Assert.That(sentinel!=null,Is.True);Assert.That(SceneManager.GetActiveScene().isDirty,Is.True);
            Assert.That(SceneManager.sceneCount,Is.EqualTo(1));
        }
        [Test] public void RepeatedNaturalAuthoringPersistsTheLoadedImporter()
        {
            const string model="Assets/Biathlon/Characters/Biathlete_16_7/Models/Biathlete_16_7_Natural.fbx";
            string guid=AssetDatabase.AssetPathToGUID(model);
            var full=generate.DeclaringType.GetMethod("GenerateNatural",BindingFlags.Static|BindingFlags.Public);
            for(int repeat=0;repeat<2;repeat++)
            {
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(model),Is.Not.Empty);
                full.Invoke(null,null);
                var importer=(ModelImporter)AssetImporter.GetAtPath(model);
                Assert.That(importer.animationType,Is.EqualTo(ModelImporterAnimationType.Human));
                Assert.That(AssetDatabase.AssetPathToGUID(model),Is.EqualTo(guid));
                var remaps=importer.GetExternalObjectMap();
                foreach(string glove in new[]{"Race_Glove_Softshell","Race_Glove_Palm_Grip"})
                    Assert.That(remaps.Any(pair=>pair.Key.name==glove&&pair.Value is Material material&&material.shader.name=="Universal Render Pipeline/Lit"),Is.True);
                Assert.That(SceneManager.GetSceneByPath(Preview).isDirty,Is.False);
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
