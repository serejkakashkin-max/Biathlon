#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Biathlon.Unity
{
    public static class PlayerBuildPipeline
    {
        public static void BuildWindows()
        {
            bool previous=PlayerSettings.enableFrameTimingStats;
            try{PlayerSettings.enableFrameTimingStats=true;RenderQualityAuthoring.WithBuildProfiles(BuildWindowsCore);}
            finally{PlayerSettings.enableFrameTimingStats=previous;}
        }
        private static void BuildWindowsCore()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Build requires idle Edit Mode.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows build support is unavailable.");
            Directory.CreateDirectory("Builds/WindowsDevelopment");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/PlayableSprint.unity", "Assets/Scenes/PlayableMassStart.unity", "Assets/Scenes/VisualBenchmark.unity" },
                locationPathName = "Builds/WindowsDevelopment/Biathlon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            File.WriteAllText("Builds/WindowsDevelopment/build-summary.json", JsonUtility.ToJson(new Evidence {
                result = summary.result.ToString(), errors = summary.totalErrors, warnings = summary.totalWarnings,
                bytes = summary.totalSize, seconds = summary.totalTime.TotalSeconds, unity = Application.unityVersion
            }, true));
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + summary.result);
            File.Copy("Assets/Resources/Biathlon/G2ThirdPartyCredits.txt", "Builds/WindowsDevelopment/THIRD_PARTY_CREDITS.txt", true);
        }
        [Serializable] private sealed class Evidence
        {
            public int schema_version = 1;
            public string id = "biathlon_windows_development_build_v1", result, unity;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }
    }
}
#endif
