#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Biathlon.Unity
{
    public static partial class PlayerBuildPipeline
    {
        public static void BuildWindows()
            => BuildWindows("Builds/WindowsDevelopment", false);
        public static void BuildWindowsVisualReview()
            => BuildWindows("Builds/VisualReviewWindows", true);
        private static void BuildWindows(string directory, bool visualReview)
        {
            bool previous=PlayerSettings.enableFrameTimingStats;
            try{PlayerSettings.enableFrameTimingStats=true;RenderQualityAuthoring.WithBuildProfiles(()=>BuildWindowsCore(directory,visualReview));}
            finally{PlayerSettings.enableFrameTimingStats=previous;}
        }
        private static void BuildWindowsCore(string directory, bool visualReview)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Build requires idle Edit Mode.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows build support is unavailable.");
            Directory.CreateDirectory(directory);
            var scenes = visualReview
                ? new[] { "Assets/Scenes/VisualBenchmark.unity", "Assets/Scenes/PlayableSprint.unity", "Assets/Scenes/PlayableMassStart.unity" }
                : new[] { "Assets/Scenes/PlayableSprint.unity", "Assets/Scenes/PlayableMassStart.unity", "Assets/Scenes/VisualBenchmark.unity" };
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = Path.Combine(directory,"Biathlon.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(directory,"build-summary.json"), JsonUtility.ToJson(new Evidence {
                result = summary.result.ToString(), errors = summary.totalErrors, warnings = summary.totalWarnings,
                bytes = summary.totalSize, seconds = summary.totalTime.TotalSeconds, unity = Application.unityVersion,
                startup_scene = scenes[0]
            }, true));
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + summary.result);
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt", Path.Combine(directory,"THIRD_PARTY_CREDITS.txt"), true);
            if(visualReview)
            {
                File.Copy("docs/quality/g2/VISUAL_REVIEW_BUILD_README_RU.txt",Path.Combine(directory,"READ_ME_RU.txt"),true);
                File.WriteAllText(Path.Combine(directory,"START_VISUAL_REVIEW.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Biathlon Visual Review\" \"%~dp0Biathlon.exe\" -force-d3d11 -screen-fullscreen 1 -window-mode borderless\r\n");
            }
        }
        [Serializable] private sealed class Evidence
        {
            public int schema_version = 1;
            public string id = "biathlon_windows_development_build_v1", result, unity, startup_scene;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }
    }
}
#endif
