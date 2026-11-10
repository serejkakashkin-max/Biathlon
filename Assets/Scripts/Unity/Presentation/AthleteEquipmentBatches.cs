using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Identical rigid equipment uses the original meshes/materials through instancing.
    /// Source enabled state, animated transforms and near/far shadow modes remain authoritative.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class AthleteEquipmentBatches : MonoBehaviour
    {
        private sealed class Source
        {
            public MeshRenderer Renderer; public Mesh Mesh; public Material[] Materials; public bool WasForcedOff;
            public Batch[] ActiveBatches;
            public ShadowCastingMode Mode; public int Layer; public uint RenderingLayers; public bool Receive;
        }
        private sealed class Batch
        {
            public Mesh Mesh; public Material Material; public int Submesh, Layer;
            public uint RenderingLayers; public ShadowCastingMode Shadows; public bool Receive;
            public readonly Matrix4x4[] Matrices = new Matrix4x4[500];
            public int Count; public Bounds Bounds;
        }
        private readonly List<Source> sources = new List<Source>();
        private readonly List<Batch> batches = new List<Batch>();
        private readonly Plane[] viewPlanes = new Plane[6];
        private readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private readonly Dictionary<(Mesh, Material, int, int, uint, ShadowCastingMode, bool), Batch> lookup =
            new Dictionary<(Mesh, Material, int, int, uint, ShadowCastingMode, bool), Batch>();
        public Camera ViewCamera {get;set;}
        public Camera RangeCamera {get;set;}
        public int SourceCount => sources.Count;
        public int SubmittedBatches { get; private set; }
        public int SubmittedInstances { get; private set; }
        public int CulledSources { get; private set; }
        public void Register(AthleteRigBinding rig)
            => RegisterVisual(rig == null ? null : rig.transform);
        public void RegisterVisual(Transform rig)
        {
            if (rig == null) throw new ArgumentNullException(nameof(rig));
            if (!SystemInfo.supportsInstancing) return;
            foreach (var renderer in rig.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<TextMesh>() != null || renderer.HasPropertyBlock() || renderer.forceRenderingOff) continue;
                var filter = renderer.GetComponent<MeshFilter>(); var mesh = filter == null ? null : filter.sharedMesh;
                var original = renderer.sharedMaterials;
                if (mesh == null || original.Length != mesh.subMeshCount) continue;
                bool valid = true; foreach (var material in original) valid &= material != null && material.shader.isSupported;
                if (!valid) continue;
                var source = new Source { Renderer = renderer, Mesh = mesh, Materials = new Material[original.Length], WasForcedOff = renderer.forceRenderingOff };
                for (int i = 0; i < original.Length; i++)
                {
                    if (!materials.TryGetValue(original[i], out var material))
                    { material = new Material(original[i]) { enableInstancing = true }; materials.Add(original[i], material); }
                    source.Materials[i] = material;
                    // Allocate both existing LOD shadow variants during registration, never during animation.
                    GetBatch(source, i, ShadowCastingMode.On); GetBatch(source, i, ShadowCastingMode.Off);
                }
                source.ActiveBatches = new Batch[original.Length]; RefreshBatches(source);
                sources.Add(source); renderer.forceRenderingOff = isActiveAndEnabled;
            }
        }
        private Batch GetBatch(Source source, int submesh, ShadowCastingMode shadows)
        {
            var renderer = source.Renderer;
            var key = (source.Mesh, source.Materials[submesh], submesh, renderer.gameObject.layer, renderer.renderingLayerMask, shadows, renderer.receiveShadows);
            if (!lookup.TryGetValue(key, out var batch))
            {
                batch = new Batch { Mesh = source.Mesh, Material = source.Materials[submesh], Submesh = submesh, Layer = key.Item4,
                    RenderingLayers = key.Item5, Shadows = shadows, Receive = renderer.receiveShadows };
                lookup.Add(key, batch); batches.Add(batch);
            }
            return batch;
        }
        private void RefreshBatches(Source source)
        {
            var renderer = source.Renderer;
            source.Mode = renderer.shadowCastingMode; source.Layer = renderer.gameObject.layer;
            source.RenderingLayers = renderer.renderingLayerMask; source.Receive = renderer.receiveShadows;
            for (int i = 0; i < source.ActiveBatches.Length; i++) source.ActiveBatches[i] = GetBatch(source, i, source.Mode);
        }
        public void Render()
        {
            using var marker = RuntimeFrameProfile.Equipment.Auto();
            foreach (var batch in batches) batch.Count = 0;
            SubmittedBatches = SubmittedInstances = CulledSources = 0;
            var camera = RangeCamera != null && RangeCamera.isActiveAndEnabled && ViewCamera != null && !ViewCamera.enabled ? RangeCamera : ViewCamera;
            bool cull = camera != null;
            if (cull) GeometryUtility.CalculateFrustumPlanes(camera, viewPlanes);
            var sun = RenderSettings.sun;
            var ray = sun != null ? sun.transform.forward : Vector3.zero;
            float shadowDistance = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset pipeline
                ? pipeline.shadowDistance : QualitySettings.shadowDistance;
            foreach (var source in sources)
            {
                var renderer = source.Renderer;
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var bounds = renderer.bounds;
                if (cull && !GeometryUtility.TestPlanesAABB(viewPlanes, bounds) &&
                    (renderer.shadowCastingMode == ShadowCastingMode.Off || !CastsNearView(bounds, camera.transform.position, ray, shadowDistance)))
                { CulledSources++; continue; }
                if (source.Mode != renderer.shadowCastingMode || source.Layer != renderer.gameObject.layer ||
                    source.RenderingLayers != renderer.renderingLayerMask || source.Receive != renderer.receiveShadows) RefreshBatches(source);
                var matrix = renderer.localToWorldMatrix;
                for (int submesh = 0; submesh < source.Materials.Length; submesh++)
                {
                    var batch = source.ActiveBatches[submesh];
                    if (batch.Count == batch.Matrices.Length) Submit(batch);
                    if (batch.Count == 0) batch.Bounds = bounds; else batch.Bounds.Encapsulate(bounds);
                    batch.Matrices[batch.Count++] = matrix; SubmittedInstances++;
                }
            }
            foreach (var batch in batches) if (batch.Count > 0) Submit(batch);
        }
        private static bool CastsNearView(Bounds caster, Vector3 eye, Vector3 ray, float distance)
        {
            // Keep offscreen casters throughout the shadow volume, including their projected shadow.
            if (ray.y >= -.001f) return true;
            var influence = caster;
            float drop = caster.size.y + caster.extents.y + Mathf.Abs(eye.y - caster.min.y);
            influence.Encapsulate(new Bounds(caster.center + ray * (drop / -ray.y), caster.size));
            return influence.SqrDistance(eye) <= distance * distance;
        }
        private void Submit(Batch batch)
        {
            var p = new RenderParams(batch.Material) { worldBounds = batch.Bounds, layer = batch.Layer, renderingLayerMask = batch.RenderingLayers,
                shadowCastingMode = batch.Shadows, receiveShadows = batch.Receive, lightProbeUsage = LightProbeUsage.Off };
            Graphics.RenderMeshInstanced(p, batch.Mesh, batch.Submesh, batch.Matrices, batch.Count);
            SubmittedBatches++; batch.Count = 0;
        }
        private void LateUpdate() => Render();
        private void OnEnable() { foreach (var source in sources) if (source.Renderer != null) source.Renderer.forceRenderingOff = true; }
        private void OnDisable() { foreach (var source in sources) if (source.Renderer != null) source.Renderer.forceRenderingOff = source.WasForcedOff; }
        public void Clear()
        {
            OnDisable(); foreach (var material in materials.Values) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            sources.Clear(); materials.Clear(); batches.Clear(); lookup.Clear();
        }
        private void OnDestroy() => Clear();
    }
}
