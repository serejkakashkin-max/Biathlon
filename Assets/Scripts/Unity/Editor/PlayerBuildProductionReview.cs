#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace Biathlon.Unity
{
    // Integration needs only 'partial' on the existing PlayerBuildPipeline declaration.
    public static partial class PlayerBuildPipeline
    {
        public const string ProductionReviewResource="Biathlon/woodland_review_v1";
        public static void BuildWindowsProductionReview()
        {BuildWindowsProductionReview(ProductionReviewResource,"Builds/WoodlandReviewWindows");}
        public static void BuildWindowsProductionReview(string reviewResource,string directory)
        {
            ValidateProductionBuildDirectory(directory);
            Biathlon.Domain.VenueManifest.ResourceId(reviewResource);
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Build requires idle Edit Mode.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Windows build support is unavailable.");
            var definition=ProductionReviewDefinition.FromJson(ProductionVenueHost.ReadResource(reviewResource));
            foreach(var entry in definition.entries)
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.scene_path)==null)throw new ArgumentException("Missing production review scene: "+entry.scene_path);
                var scene=EditorSceneManager.OpenPreviewScene(entry.scene_path);
                try
                {
                    var roots=scene.GetRootGameObjects();var host=roots.Length==1?roots[0].GetComponent<ProductionVenueHost>():null;
                    if(host==null||!host.AutoStart||host.ReviewDefinitionResource!=reviewResource||host.FieldCount!=entry.field_count||string.IsNullOrEmpty(host.EnvironmentResource))
                        throw new ArgumentException("Production review requires exact generated bootstrap/resource bindings: "+entry.id);
                }
                finally{EditorSceneManager.ClosePreviewScene(scene);}
            }
            WithProductionBuildProfiles(()=>BuildProductionReviewCore(definition,directory));
        }
        private static void ValidateProductionBuildDirectory(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory)||!directory.StartsWith("Builds/",StringComparison.Ordinal)||directory.Contains("..")||directory.Contains('\\')||Path.IsPathRooted(directory)||directory.Length<=7)
                throw new ArgumentException("An explicit project directory inside Builds/ is required.");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Builds"))+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Build output must stay inside the project Builds/ directory.");
        }
        /// <summary>No SaveAssets or global build scene writes. Existing dirty owner values stay owned.</summary>
        public static void WithProductionBuildProfiles(Action build)
        {
            if(build==null)throw new ArgumentNullException(nameof(build));
            var owners=new List<ProductionOwnerState>();
            var global=GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
            if(global!=null)owners.Add(new ProductionOwnerState(global));
            foreach(var tier in RenderQualityDefinition.Load().tiers)
            {
                var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/Biathlon/Rendering/"+tier.id+".asset");
                if(asset!=null)owners.Add(new ProductionOwnerState(asset));
            }
            bool previous=PlayerSettings.enableFrameTimingStats;
            string settingsPath=Path.GetFullPath(Path.Combine(Application.dataPath,"..","ProjectSettings","ProjectSettings.asset"));
            byte[] previousFile=File.ReadAllBytes(settingsPath);
            string inputPreload=null;
            if(EditorBuildSettings.TryGetConfigObject<UnityEngine.Object>("com.unity.input.settings.actions",out var actions)&&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actions,out string guid,out long fileId))
                inputPreload="  - {fileID: "+fileId+", guid: "+guid+", type: 3}";
            try{PlayerSettings.enableFrameTimingStats=true;RenderQualityAuthoring.WithBuildProfiles(build);}
            finally
            {
                PlayerSettings.enableFrameTimingStats=previous;
                foreach(var owner in owners)owner.RestoreDirtyFlag();
                RestoreProductionTimingFile(settingsPath,previousFile,previous,inputPreload);
            }
        }
        // BuildPipeline may persist the temporary timing flag/Input System preload and normalize line endings.
        // Restore only when those transformations fully explain the file change. Preserve any
        // independent owner edit rather than replacing it with the captured snapshot.
        private static bool RestoreProductionTimingFile(string path,byte[] original,bool previous,string inputPreload)
        {
            var current=File.ReadAllBytes(path);
            if(ProductionBytesEqual(current,original))return true;
            var text=System.Text.Encoding.UTF8.GetString(current);
            string marker="  enableFrameTimingStats: ";
            int at=text.IndexOf(marker,StringComparison.Ordinal);
            if(at<0||text.IndexOf(marker,at+marker.Length,StringComparison.Ordinal)>=0)return false;
            int value=at+marker.Length;
            if(value>=text.Length||(text[value]!='0'&&text[value]!='1'))return false;
            text=text.Substring(0,value)+(previous?"1":"0")+text.Substring(value+1);
            string originalText=System.Text.Encoding.UTF8.GetString(original);
            if(inputPreload!=null&&!originalText.Contains(inputPreload))
            {
                text=text.Replace(inputPreload+"\r\n","").Replace(inputPreload+"\n","");
                text=text.Replace("  preloadedAssets:\r\n  metroInputSource:","  preloadedAssets: []\r\n  metroInputSource:")
                    .Replace("  preloadedAssets:\n  metroInputSource:","  preloadedAssets: []\n  metroInputSource:");
            }
            foreach(var candidate in new[]{text,text.Replace("\r\n","\n"),text.Replace("\r\n","\n").Replace("\n","\r\n")})
                if(ProductionBytesEqual(System.Text.Encoding.UTF8.GetBytes(candidate),original))
                {
                    // Unity may retain a mapped input-settings reader after BuildPipeline.
                    // Truncating its file fails with Win32 1224. Replace the fully proven
                    // document atomically, preserving the existing reader's old mapping.
                    string temporary=path+".biathlon-restore-"+Guid.NewGuid().ToString("N");
                    try
                    {
                        File.WriteAllBytes(temporary,original);
                        if(!ProductionBytesEqual(File.ReadAllBytes(path),current))return false;
                        File.Replace(temporary,path,null);return true;
                    }
                    finally{if(File.Exists(temporary))File.Delete(temporary);}
                }
            return false;
        }
        private static bool ProductionBytesEqual(byte[] a,byte[] b)
        {if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
        private sealed class ProductionOwnerState
        {
            private readonly UnityEngine.Object asset;private readonly bool dirty;private readonly string json;
            internal ProductionOwnerState(UnityEngine.Object value){asset=value;dirty=EditorUtility.IsDirty(value);json=EditorJsonUtility.ToJson(value);}
            internal void RestoreDirtyFlag()
            {
                if(asset==null)return;
                if(dirty)EditorUtility.SetDirty(asset);
                // Clear only a lease-created dirty bit when every serialized owner value is exact.
                // A callback's independent changed values remain dirty and are never overwritten.
                else if(EditorJsonUtility.ToJson(asset)==json)EditorUtility.ClearDirty(asset);
            }
        }
        private static void BuildProductionReviewCore(ProductionReviewDefinition definition,string directory)
        {
            Directory.CreateDirectory(directory);
            var scenes=new[]{definition.Get(definition.initial_entry_id).scene_path,definition.entries[0].id==definition.initial_entry_id?definition.entries[1].scene_path:definition.entries[0].scene_path};
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(directory,"Biathlon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            var summary=report.summary;
            File.WriteAllText(Path.Combine(directory,"build-summary.json"),JsonUtility.ToJson(new ProductionBuildEvidence{id=definition.venue_id+"_windows_review_build_v1",result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,unity=Application.unityVersion,startup_scene=scenes[0],review_definition_id=definition.id,initial_quality=definition.initial_quality_tier},true));
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Production Windows review build failed: "+summary.result);
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt",Path.Combine(directory,"THIRD_PARTY_CREDITS.txt"),true);
            File.WriteAllText(Path.Combine(directory,"review-definition.json"),definition.ToJson());
            if(definition.venue_id=="woodland_basin_v1")File.WriteAllText(Path.Combine(directory,"START_WOODLAND_REVIEW.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Woodland review\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080\r\n");
            File.WriteAllText(Path.Combine(directory,"START_PRODUCTION_REVIEW.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Production review\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080\r\n");
            File.WriteAllText(Path.Combine(directory,"READ_ME.txt"),definition.venue_id+" production review\r\nNew race: Mass Start30 or Sprint30. Initial quality and scene come from review-definition.json. Select High/Balanced/Low in the review menu.\r\nSKI: W/S effort, A/D steering (gentle course return after release), Space tuck (steering keeps the low posture). RANGE: ride onto cyan arrows for automatic mat approach; control returns after the exit turn. Mouse aim, LMB fire, RMB fine, Space breath.\r\nHidden smoke verifies initialized content and paid controls. Windows may suspend rendering in hidden windows; unavailable frames/counters are explicitly reported. Normal Game View captures supply the separate visual review. No sustained FPS/GPU measurement is claimed.\r\n");
        }
        [Serializable] private sealed class ProductionBuildEvidence
        {
            public int schema_version=1;public string id="woodland_windows_review_build_v1",result,unity,startup_scene,review_definition_id,initial_quality;
            public int errors,warnings;public ulong bytes;public double seconds;
        }
    }
}
#endif
