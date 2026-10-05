#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Biathlon.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Biathlon.VenueEditorTests
{
    public sealed class ProductionReviewBuildLeaseTests
    {
        [TestCase(false)][TestCase(true)] public void IdleEditorWithoutActivePipelineGetsTemporaryBuildBindingAndRestoresIt(bool reject)
        {
            var priorQuality=QualitySettings.renderPipeline;var priorDefault=GraphicsSettings.defaultRenderPipeline;
            string[] paths={"Assets/Resources/Biathlon/Rendering/high.asset","Assets/Resources/Biathlon/Rendering/balanced.asset","Assets/Resources/Biathlon/Rendering/low.asset"};
            var owners=paths.Select(AssetDatabase.LoadMainAssetAtPath).ToArray();var labels=owners.Select(AssetDatabase.GetLabels).ToArray();
            try
            {
                QualitySettings.renderPipeline=null;GraphicsSettings.defaultRenderPipeline=null;
                Assert.That(GraphicsSettings.currentRenderPipeline,Is.Null);
                Action callback=()=>{Assert.That(GraphicsSettings.currentRenderPipeline,Is.Not.Null);Assert.That(GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>().includeAssetsByLabel,Is.True);if(reject)throw new InvalidOperationException("Expected callback rejection");};
                if(reject)Assert.Throws<InvalidOperationException>(()=>RenderQualityAuthoring.WithBuildProfiles(callback));else RenderQualityAuthoring.WithBuildProfiles(callback);
                Assert.That(QualitySettings.renderPipeline,Is.Null);Assert.That(GraphicsSettings.defaultRenderPipeline,Is.Null);
                for(int i=0;i<owners.Length;i++)CollectionAssert.AreEqual(labels[i],AssetDatabase.GetLabels(owners[i]));
            }
            finally{QualitySettings.renderPipeline=priorQuality;GraphicsSettings.defaultRenderPipeline=priorDefault;}
        }
        [TestCase("Builds/")][TestCase("Assets/Replacement")][TestCase("Builds/../Owner")][TestCase("C:/Owner")]
        public void ReviewOutputRejectsOutsideBuildsBeforeReadingResourcesOrLeasingOwners(string output)
        {
            bool timing=PlayerSettings.enableFrameTimingStats;
            var scenes=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();
            Assert.Throws<ArgumentException>(()=>PlayerBuildPipeline.BuildWindowsProductionReview("missing",output));
            Assert.That(PlayerSettings.enableFrameTimingStats,Is.EqualTo(timing));
            CollectionAssert.AreEqual(scenes,EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray());
        }
        [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)][TestCase(true,true)]
        public void PersistedTimingLeaseRestoresExactBytesAndPreservesIndependentOwnerEdits(bool previous,bool ownerEdit)
        {
            string path=Path.Combine(Path.GetTempPath(),"biathlon-build-lease-"+Guid.NewGuid().ToString("N")+".asset");
            const string inputPreload="  - {fileID: 123, guid: test_input_actions, type: 3}";
            var original=Encoding.UTF8.GetBytes("\uFEFFPlayerSettings:\r\n  preloadedAssets: []\r\n  metroInputSource: 0\r\n  ownerField: original\r\n  enableFrameTimingStats: "+(previous?"1":"0")+"\r\n");
            string temporary=Encoding.UTF8.GetString(original).Replace("\r\n","\n").Replace("enableFrameTimingStats: "+(previous?"1":"0"),"enableFrameTimingStats: "+(previous?"0":"1"));
            temporary=temporary.Replace("  preloadedAssets: []","  preloadedAssets:\n"+inputPreload);
            if(ownerEdit)temporary=temporary.Replace("ownerField: original","ownerField: independently-edited");
            var current=Encoding.UTF8.GetBytes(temporary);
            try
            {
                File.WriteAllBytes(path,current);
                var method=typeof(PlayerBuildPipeline).GetMethod("RestoreProductionTimingFile",BindingFlags.NonPublic|BindingFlags.Static);
                Assert.That(method,Is.Not.Null);
                Assert.That((bool)method.Invoke(null,new object[]{path,original,previous,inputPreload}),Is.EqualTo(!ownerEdit));
                CollectionAssert.AreEqual(ownerEdit?current:original,File.ReadAllBytes(path));
                // An unchanged file is accepted without rewriting it.
                File.WriteAllBytes(path,original);
                Assert.That((bool)method.Invoke(null,new object[]{path,original,previous,inputPreload}),Is.True);
                CollectionAssert.AreEqual(original,File.ReadAllBytes(path));
            }
            finally{if(File.Exists(path))File.Delete(path);}
        }
        [TestCase(false)][TestCase(true)] public void ProductionBuildLeaseRestoresLabelsSettingsAndExistingDirtyOwnersWhenCallbackRejects(bool reject)
        {
            var prior=QualitySettings.renderPipeline;
            try
            {
                // This fixture exercises existing active-URP owner values. The separate
                // null-pipeline fixture checks restoration of an idle unbound Editor.
                if(GraphicsSettings.currentRenderPipeline==null)QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Biathlon/Rendering/high.asset");
                AssertActiveBuildLeaseRestoration(reject);
            }
            finally{QualitySettings.renderPipeline=prior;}
        }
        private static void AssertActiveBuildLeaseRestoration(bool reject)
        {
            var assets=new UnityEngine.Object[]{GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>(),AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Biathlon/Rendering/high.asset"),AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Biathlon/Rendering/balanced.asset"),AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Biathlon/Rendering/low.asset")};
            Assert.That(assets.All(a=>a!=null),Is.True);var dirty=assets.Select(EditorUtility.IsDirty).ToArray();var values=assets.Select(a=>EditorJsonUtility.ToJson(a)).ToArray();var labels=assets.Select(AssetDatabase.GetLabels).ToArray();
            var inclusion=GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();bool include=inclusion.includeAssetsByLabel;string label=inclusion.labelToInclude;
            bool timing=PlayerSettings.enableFrameTimingStats;var scenes=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();
            try
            {
                // Only the test's dirty flags change; no owner field values or assets are saved.
                foreach(var asset in assets)EditorUtility.SetDirty(asset);
                Action callback=()=>{Assert.That(PlayerSettings.enableFrameTimingStats,Is.True);Assert.That(inclusion.includeAssetsByLabel,Is.True);if(reject)throw new InvalidOperationException("Expected build rejection fixture.");};
                if(reject)Assert.Throws<InvalidOperationException>(()=>PlayerBuildPipeline.WithProductionBuildProfiles(callback));else PlayerBuildPipeline.WithProductionBuildProfiles(callback);
                Assert.That(PlayerSettings.enableFrameTimingStats,Is.EqualTo(timing));Assert.That(inclusion.includeAssetsByLabel,Is.EqualTo(include));Assert.That(inclusion.labelToInclude,Is.EqualTo(label));
                CollectionAssert.AreEqual(scenes,EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray());
                for(int i=0;i<assets.Length;i++){Assert.That(EditorUtility.IsDirty(assets[i]),Is.True);Assert.That(EditorJsonUtility.ToJson(assets[i]),Is.EqualTo(values[i]));CollectionAssert.AreEqual(labels[i],AssetDatabase.GetLabels(assets[i]));}
                Assert.Throws<ArgumentNullException>(()=>PlayerBuildPipeline.WithProductionBuildProfiles(null));
            }
            finally{for(int i=0;i<assets.Length;i++){if(dirty[i])EditorUtility.SetDirty(assets[i]);else EditorUtility.ClearDirty(assets[i]);}}
        }
    }
}
#endif
