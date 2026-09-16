#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Biathlon.Unity
{
    public static class RenderQualityAuthoring
    {
        private const string Root = "Assets/Resources/Biathlon/Rendering/";
        /// <summary>URP's shader stripper enumerates quality/graphics assets, not arbitrary Resources.
        /// Temporarily include our tiers by label while preserving the owner's existing settings.</summary>
        public static void WithBuildProfiles(System.Action build)
        {
            var setting = GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();
            if (setting == null) throw new System.InvalidOperationException("URP build inclusion settings unavailable.");
            bool enabled = setting.includeAssetsByLabel; string label = setting.labelToInclude;
            string buildLabel = enabled && !string.IsNullOrWhiteSpace(label) ? label : "BiathlonRenderBuild";
            var assets = new List<UniversalRenderPipelineAsset>(); var labels = new List<string[]>();
            try
            {
                foreach (var tier in RenderQualityDefinition.Load().tiers)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + tier.id + ".asset");
                    if (asset == null) throw new System.InvalidOperationException("Missing render tier " + tier.id);
                    assets.Add(asset); var old = AssetDatabase.GetLabels(asset); labels.Add(old);
                    var next = new List<string>(old); if (!next.Contains(buildLabel)) next.Add(buildLabel);
                    AssetDatabase.SetLabels(asset, next.ToArray());
                }
                setting.labelToInclude = buildLabel; setting.includeAssetsByLabel = true;
                var included = new List<UniversalRenderPipelineAsset>();
                if (!BuildTarget.StandaloneWindows64.TryGetRenderPipelineAssets(included)) throw new System.InvalidOperationException("No URP build assets.");
                foreach (var asset in assets) if (!included.Contains(asset)) throw new System.InvalidOperationException("Render tier omitted from shader build: " + asset.name);
                build();
            }
            finally
            {
                setting.includeAssetsByLabel = enabled; setting.labelToInclude = label;
                for (int i = 0; i < assets.Count; i++) AssetDatabase.SetLabels(assets[i], labels[i]);
            }
        }
        [MenuItem("BIATHLON/Generate Render Quality Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "GameplayRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode = RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer, Root + "GameplayRenderer.asset");
            }
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (renderer.postProcessData == null) throw new System.InvalidOperationException("Installed URP post-processing resources are missing.");
            EditorUtility.SetDirty(renderer);
            var volume = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "GameplayPost.asset");
            if (volume == null)
            {
                volume = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(volume, Root + "GameplayPost.asset");
                volume.Add<DepthOfField>(true).mode.value = DepthOfFieldMode.Off;
                volume.Add<MotionBlur>(true).intensity.value = 0;
                volume.Add<LensDistortion>(true).intensity.value = 0;
                volume.Add<ChromaticAberration>(true).intensity.value = 0;
                volume.Add<FilmGrain>(true).intensity.value = 0;
                volume.Add<Vignette>(true).intensity.value = 0;
                volume.Add<Bloom>(true).intensity.value = 0;
                volume.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
                volume.Add<ColorAdjustments>(true);
                foreach (var component in volume.components) AssetDatabase.AddObjectToAsset(component, volume);
                EditorUtility.SetDirty(volume);
            }
            foreach (var tier in RenderQualityDefinition.Load().tiers)
            {
                var asset = UniversalRenderPipelineAsset.Create(renderer);
                asset.name = tier.id; asset.renderScale = tier.render_scale; asset.shadowDistance = tier.shadow_distance_m;
                asset.mainLightShadowmapResolution = tier.shadow_resolution; asset.shadowCascadeCount = tier.shadow_cascades;
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("m_SoftShadowsSupported").boolValue = tier.soft_shadows;
                serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                asset.supportsHDR = true; asset.msaaSampleCount = 1; asset.useSRPBatcher = true;
                asset.supportsCameraDepthTexture = false; asset.supportsCameraOpaqueTexture = false;
                asset.volumeProfile = volume;
                string path = Root + tier.id + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (existing == null) AssetDatabase.CreateAsset(asset, path);
                else { EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
