using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Presentation-only quality lease. Supports overlapping hosts during scene/race transitions.
    /// Assets are immutable templates; runtime instances and camera state are restored on release.</summary>
    public sealed class RenderQualityPresentation : MonoBehaviour
    {
        private static readonly List<RenderQualityPresentation> owners = new List<RenderQualityPresentation>();
        private static RenderPipelineAsset original;
        private static int originalTargetFrameRate;
        private RenderQualityDefinition definition;
        private UniversalRenderPipelineAsset pipeline;
        private CameraState[] cameras;
        public string TierId { get; private set; }
        public UniversalRenderPipelineAsset Pipeline => pipeline;
        private struct CameraState
        {
            public UniversalAdditionalCameraData Data;
            public bool Post;
            public LayerMask VolumeMask;
            public AntialiasingMode AA;
        }
        public void Initialize(params Camera[] targets)
        {
            if (definition != null) throw new InvalidOperationException("Quality adapter already initialized.");
            if (targets == null || targets.Length == 0) throw new ArgumentException("Explicit gameplay cameras required.");
            foreach (var c in targets) if (c == null) throw new ArgumentException("Missing camera.");
            definition = RenderQualityDefinition.Load(); cameras = new CameraState[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                var data = targets[i].GetComponent<UniversalAdditionalCameraData>(); bool created = data == null;
                if (created) data = targets[i].gameObject.AddComponent<UniversalAdditionalCameraData>();
                cameras[i] = new CameraState { Data = data, Post = data.renderPostProcessing,
                    VolumeMask = data.volumeLayerMask, AA = data.antialiasing };
            }
            Apply(Application.isPlaying?RenderQualityPreferences.Read(definition.default_tier):definition.default_tier);
        }
        public void Apply(string id)
        {
            if (definition == null) throw new InvalidOperationException("Initialize first.");
            if (!isActiveAndEnabled) throw new InvalidOperationException("Enable quality adapter before applying a profile.");
            var tier = definition.Get(id);
            var template = Resources.Load<UniversalRenderPipelineAsset>("Biathlon/Rendering/" + id);
            if (template == null) throw new InvalidOperationException("Generate production render assets first.");
            var next = Instantiate(template); next.name = "BIATHLON runtime " + id;
            var previous = pipeline; pipeline = next; TierId = id;
            if (!owners.Contains(this)) { if (owners.Count == 0) {original = QualitySettings.renderPipeline;originalTargetFrameRate=Application.targetFrameRate;} owners.Add(this); }
            Application.targetFrameRate=definition.target_frame_rate;
            if (owners[owners.Count - 1] == this) QualitySettings.renderPipeline = pipeline;
            foreach (var c in cameras)
            {
                if(c.Data==null)continue;
                // Only the owned pipeline's safe default Volume profile applies. Scene volumes
                // cannot silently introduce blur, lens distortion or exposure changes to aiming.
                c.Data.volumeLayerMask = 0; c.Data.renderPostProcessing = tier.post_processing;
                c.Data.antialiasing = tier.post_processing ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            }
            if (previous != null) Destroy(previous);
        }
        private void OnEnable() { if (definition != null) Apply(TierId); }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private void Release()
        {
            if (!owners.Remove(this)) return;
            QualitySettings.renderPipeline = owners.Count == 0 ? original : owners[owners.Count - 1].pipeline;
            if (owners.Count == 0) {original = null;Application.targetFrameRate=originalTargetFrameRate;}
            foreach (var c in cameras) if (c.Data != null)
            { c.Data.renderPostProcessing = c.Post; c.Data.volumeLayerMask = c.VolumeMask; c.Data.antialiasing = c.AA; }
            if (pipeline != null) Destroy(pipeline); pipeline = null;
        }
    }
}
