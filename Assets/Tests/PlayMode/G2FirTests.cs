using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2FirTests
    {
        private const string Root="Biathlon/Vegetation/G2Fir/";
        [Test]
        public void AuditedFirLodsFitExistingPlacementEnvelopeAndRetainCutoutPasses()
        {
            int prior=int.MaxValue;
            for(int lod=0;lod<3;lod++)
            {
                var mesh=Resources.Load<Mesh>(Root+"FirLOD"+lod);Assert.That(mesh,Is.Not.Null);
                Assert.That(mesh.subMeshCount,Is.EqualTo(1));Assert.That(mesh.triangles.Length,Is.LessThan(prior));prior=mesh.triangles.Length;
                Assert.That(mesh.bounds.min.y,Is.InRange(-.001f,.001f));Assert.That(mesh.bounds.max.y,Is.InRange(7.9f,8.1f));
                foreach(var p in mesh.vertices)Assert.That(new Vector2(p.x,p.z).magnitude,Is.LessThan(2.5f));
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.colors.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.tangents.Length,Is.EqualTo(mesh.vertexCount));
            }
            Assert.That(prior/3,Is.InRange(100,700));
            var mat=Resources.Load<Material>(Root+"Fir");Assert.That(mat.shader.isSupported,Is.True);Assert.That(mat.enableInstancing,Is.True);
            Assert.That(mat.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));Assert.That(mat.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
            Assert.That(mat.GetTag("RenderType",false),Is.EqualTo("TransparentCutout"));
            foreach(string name in new[]{"BarkAlbedo","LeafAlbedo","BarkNormal","LeafNormal"})
            {
                var texture=mat.GetTexture("_"+name);Assert.That(texture,Is.Not.Null);Assert.That(Math.Max(texture.width,texture.height),Is.LessThanOrEqualTo(name=="LeafAlbedo"?2048:1024));
            }
        }
        [Test]
        public void SelectedFirChangesOnlySubmittedGeometryAndKeepsZeroAllocationCulling()
        {
            var root=new GameObject("fir placement parity");
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,2,-48);camera.farClipPlane=200;
                var a=new GameObject("legacy").AddComponent<VegetationPresentation>();a.transform.SetParent(root.transform);a.transform.position=Vector3.zero;
                var b=new GameObject("audited").AddComponent<VegetationPresentation>();b.transform.SetParent(root.transform);b.transform.position=Vector3.zero;
                a.Initialize(VegetationPreview.Route);b.Initialize(VegetationPreview.Route,true);
                Assert.That(b.UsesAuditedFir,Is.True);Assert.That(a.UsesAuditedFir,Is.False);
                foreach(string tier in new[]{"low","balanced","high"})
                {
                    a.Prepare(camera,tier,8);b.Prepare(camera,tier,8);
                    Assert.That(b.VisibleInstances,Is.EqualTo(a.VisibleInstances));Assert.That(b.NearCount,Is.EqualTo(a.NearCount));Assert.That(b.FarCount,Is.EqualTo(a.FarCount));
                    long far=Resources.Load<Mesh>(Root+"FirLOD2").GetIndexCount(0)/3;
                    long expected=6633L*b.NearCount+3268L*b.MidCount+far*b.FarCount;Assert.That(b.SubmittedTriangles,Is.EqualTo(expected));
                }
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++)b.Prepare(camera,"high",i);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator FirAlphaLodsAndIntegratedBenchmarkProduceReviewCaptures()
        {
            var root=new GameObject("isolated fir review");Scene scene=default;
            var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.34f,.4f);
                var tree=new GameObject("selected fir");tree.transform.SetParent(root.transform);var mesh=tree.AddComponent<MeshFilter>();tree.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>(Root+"Fir");
                var camera=new GameObject("Hidden fir camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.43f,.52f,.62f);camera.fieldOfView=36;camera.transform.position=new Vector3(8,5,13);camera.transform.LookAt(new Vector3(0,4,0));
                var light=new GameObject("Fir key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(40,-140,0);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                string folder=Path.GetFullPath("artifacts/g2-fir/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1024,1024))
                for(int lod=0;lod<3;lod++)
                {
                    mesh.sharedMesh=Resources.Load<Mesh>(Root+"FirLOD"+lod);
                    for(int frame=0;frame<3;frame++){target.Render(camera);yield return null;}
                    target.SavePng(Path.Combine(folder,"fir-lod"+lod+".png"));
                }
                Object.DestroyImmediate(root);root=null;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;
#if UNITY_EDITOR
                scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
                VisualBenchmarkPresentation benchmark=null;foreach(var go in scene.GetRootGameObjects())if(go.TryGetComponent(out VisualBenchmarkPresentation p))benchmark=p;
                Assert.That(benchmark,Is.Not.Null);Assert.That(benchmark.Ready,Is.True);benchmark.ShowControls=false;
                foreach(var forest in benchmark.Forests)Assert.That(forest.UsesAuditedFir,Is.True);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(string shot in new[]{"sector","athlete","range"})
                foreach(string tier in new[]{"balanced","high"})
                {
                    benchmark.Shot=shot;benchmark.Tier=tier;benchmark.Seek(8);
                    for(int frame=0;frame<3;frame++){benchmark.RenderFrame(8);target.Render(benchmark.View);yield return null;}
                    target.SavePng(Path.Combine(folder,shot+"-"+tier+".png"));
                }
                yield return SceneManager.UnloadSceneAsync(scene);scene=default;
#endif
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
                RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;
            }
        }
    }
}
