using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class RenderQualityTests
    {
        [Test] public void DefinitionRejectsInvalidAndAmbiguousProfiles()
        {
            var json = Resources.Load<TextAsset>("Biathlon/render_quality_v1").text;
            var d = RenderQualityDefinition.FromJson(json);
            Assert.That(d.Get(d.default_tier).id, Is.EqualTo("balanced"));
            Assert.That(d.target_frame_rate,Is.EqualTo(100));
            Assert.Throws<ArgumentException>(()=>RenderQualityDefinition.FromJson(json.Replace("\"target_frame_rate\": 100","\"target_frame_rate\": 0")));
            Assert.Throws<ArgumentException>(() => RenderQualityDefinition.FromJson(json.Replace("0.8", "0")));
            Assert.Throws<ArgumentException>(() => RenderQualityDefinition.FromJson(json.Replace("\"high\"", "\"low\"")));
            Assert.Throws<ArgumentException>(() => d.Get("unknown"));
        }
        [Test] public void GeneratedAssetsMatchPolicyAndPreventAimingDistortion()
        {
            foreach (var t in RenderQualityDefinition.Load().tiers)
            {
                var a = Resources.Load<UniversalRenderPipelineAsset>("Biathlon/Rendering/" + t.id);
                Assert.That(a, Is.Not.Null); Assert.That(a.renderScale, Is.EqualTo(t.render_scale));
                Assert.That(a.shadowDistance, Is.EqualTo(t.shadow_distance_m));
                Assert.That(a.mainLightShadowmapResolution, Is.EqualTo(t.shadow_resolution));
                Assert.That(a.shadowCascadeCount, Is.EqualTo(t.shadow_cascades));
                Assert.That(a.supportsSoftShadows, Is.EqualTo(t.soft_shadows));
                Assert.That(a.volumeProfile.TryGet<DepthOfField>(out var dof), Is.True); Assert.That(dof.mode.value, Is.EqualTo(DepthOfFieldMode.Off));
                Assert.That(a.volumeProfile.TryGet<MotionBlur>(out var blur), Is.True); Assert.That(blur.intensity.value, Is.Zero);
                Assert.That(a.volumeProfile.TryGet<LensDistortion>(out var lens), Is.True); Assert.That(lens.intensity.value, Is.Zero);
            }
        }
        [Test] public void SwitchingAndOverlappingOwnersRestorePipelineAndCameraWithoutChangingGeometry()
        {
            var baseline = QualitySettings.renderPipeline;
            int originalRate=Application.targetFrameRate;
            var first = new GameObject("quality first"); var second = new GameObject("quality second");
            try
            {
                var camera = first.AddComponent<Camera>(); camera.fieldOfView = 17; camera.transform.position = new Vector3(1, 2, 3);
                var data = first.AddComponent<UniversalAdditionalCameraData>(); data.volumeLayerMask = 123;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; data.renderPostProcessing = true;
                var a = first.AddComponent<RenderQualityPresentation>(); a.Initialize(camera);
                Assert.That(Application.targetFrameRate,Is.EqualTo(RenderQualityDefinition.Load().target_frame_rate));
                var originalAsset = Resources.Load<UniversalRenderPipelineAsset>("Biathlon/Rendering/low");
                a.Apply("low"); Assert.That(a.Pipeline, Is.Not.SameAs(originalAsset));
                Assert.That(data.volumeLayerMask.value, Is.Zero); Assert.That(data.renderPostProcessing, Is.False);
                Assert.That(camera.fieldOfView, Is.EqualTo(17)); Assert.That(camera.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
                var prior = a.Pipeline; Assert.Throws<ArgumentException>(() => a.Apply("missing")); Assert.That(a.Pipeline, Is.SameAs(prior));
                var b = second.AddComponent<RenderQualityPresentation>(); b.Initialize(second.AddComponent<Camera>()); b.Apply("high");
                a.enabled = false; Assert.That(QualitySettings.renderPipeline, Is.SameAs(b.Pipeline));
                Assert.That(data.volumeLayerMask.value, Is.EqualTo(123)); Assert.That(data.antialiasing, Is.EqualTo(AntialiasingMode.FastApproximateAntialiasing));
                b.enabled = false; Assert.That(QualitySettings.renderPipeline, Is.SameAs(baseline));
                a.enabled = true; Assert.That(QualitySettings.renderPipeline, Is.SameAs(a.Pipeline));
                a.enabled = false; Assert.That(QualitySettings.renderPipeline, Is.SameAs(baseline));
                Assert.That(originalAsset.renderScale, Is.EqualTo(RenderQualityDefinition.Load().Get("low").render_scale));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(baseline));
            Assert.That(Application.targetFrameRate,Is.EqualTo(originalRate));
        }
    }
}
