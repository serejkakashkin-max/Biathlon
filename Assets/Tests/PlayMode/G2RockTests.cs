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
    public class G2RockTests
    {
        private const string Root="Biathlon/Terrain/G2Rock/";
        [Test]
        public void RockGeometryAndMapsFitBoundedDressingBudget()
        {
            for(int lod=0;lod<2;lod++)
            {
                var mesh=Resources.Load<Mesh>(Root+"RockLOD"+lod);Assert.That(mesh,Is.Not.Null);Assert.That(mesh.triangles.Length/3,Is.LessThanOrEqualTo(lod==0?2000:600));
                Assert.That(mesh.bounds.min.y,Is.InRange(-.001f,.001f));Assert.That(mesh.bounds.max.y,Is.InRange(.95f,1.1f));
                foreach(var p in mesh.vertices)Assert.That(new Vector2(p.x,p.z).magnitude,Is.LessThan(1.15f));
                Assert.That(mesh.subMeshCount,Is.EqualTo(1));Assert.That(mesh.tangents.Length,Is.EqualTo(mesh.vertexCount));
            }
            var mat=Resources.Load<Material>(Root+"SnowyRock");Assert.That(mat.shader.isSupported,Is.True);Assert.That(mat.enableInstancing,Is.True);
            Assert.That(mat.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));Assert.That(mat.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
            foreach(string name in new[]{"_BaseMap","_BumpMap","_RoughnessMap"})
            {
                var tex=mat.GetTexture(name);Assert.That(tex,Is.Not.Null);Assert.That(Mathf.Max(tex.width,tex.height),Is.LessThanOrEqualTo(name=="_BaseMap"?2048:1024));
#if UNITY_EDITOR
                var importer=UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(tex)) as UnityEditor.TextureImporter;Assert.That(importer.sRGBTexture,Is.EqualTo(name=="_BaseMap"));
#endif
            }
        }
        [Test]
        public void DressingRejectsDuplicateIdsAndProtectedRouteIntrusions()
        {
            var d=G2GroundDressing.Load();d.rocks[1].id=d.rocks[0].id;Assert.Throws<ArgumentException>(()=>d.Validate());
            d=G2GroundDressing.Load();d.rocks[0].x=0;Assert.Throws<ArgumentException>(()=>d.Validate());
            d=G2GroundDressing.Load();d.rocks[0].scale=float.NaN;Assert.Throws<ArgumentException>(()=>d.Validate());
        }
        [Test]
        public void GroundDressingConformsAndCullsWithoutAllocationsOrColliders()
        {
            var root=new GameObject("bounded dressing");
            try
            {
                var dressing=root.AddComponent<G2GroundDressing>();dressing.Initialize();var d=G2GroundDressing.Load();Assert.That(dressing.Count,Is.EqualTo(16));
                for(int i=0;i<dressing.Count;i++){var p=dressing.Position(i);Assert.That(p.y,Is.EqualTo(G2BenchmarkSurface.Height(p)-d.burial_fraction*d.rocks[i].scale).Within(.00001f));}
                Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,4,-100);camera.transform.LookAt(new Vector3(0,0,-35));camera.farClipPlane=130;
                dressing.Prepare(camera);Assert.That(dressing.NearCount+dressing.FarCount,Is.GreaterThan(0));Assert.That(dressing.SubmittedTriangles,Is.LessThanOrEqualTo(16*2000));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++)dressing.Prepare(camera);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                camera.transform.position=Vector3.one*1000;dressing.Prepare(camera);Assert.That(dressing.NearCount+dressing.FarCount,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator IsolatedAndIntegratedRocksProduceHiddenReviewFrames()
        {
            var root=new GameObject("rock review");Scene scene=default;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.34f,.4f);
                var rock=new GameObject("audited boulder");rock.transform.SetParent(root.transform);var mesh=rock.AddComponent<MeshFilter>();rock.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>(Root+"SnowyRock");
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.SetParent(root.transform);floor.transform.localScale=Vector3.one*.5f;floor.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");
                var camera=new GameObject("hidden rock camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.43f,.52f,.62f);camera.fieldOfView=38;camera.transform.position=new Vector3(2.6f,1.9f,2.8f);camera.transform.LookAt(new Vector3(0,.4f,0));
                var light=new GameObject("rock key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(40,-140,0);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                string folder=Path.GetFullPath("artifacts/g2-rock/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1024,1024))
                for(int lod=0;lod<2;lod++)
                {
                    mesh.sharedMesh=Resources.Load<Mesh>(Root+"RockLOD"+lod);for(int frame=0;frame<3;frame++){target.Render(camera);yield return null;}target.SavePng(Path.Combine(folder,"rock-lod"+lod+".png"));
                }
                Object.DestroyImmediate(root);root=null;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;
#if UNITY_EDITOR
                scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
                VisualBenchmarkPresentation benchmark=null;foreach(var go in scene.GetRootGameObjects())if(go.TryGetComponent(out VisualBenchmarkPresentation p))benchmark=p;
                Assert.That(benchmark,Is.Not.Null);Assert.That(benchmark.Ready,Is.True);benchmark.ShowControls=false;benchmark.enabled=false;
                Assert.That(benchmark.GroundDressing.Count,Is.EqualTo(16));
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(string shot in new[]{"approach","athlete","sector"})
                {
                    benchmark.Shot=shot;benchmark.Tier="high";benchmark.Seek(8);
                    for(int frame=0;frame<3;frame++){benchmark.RenderFrame(8);target.Render(benchmark.View);yield return null;}
                    target.SavePng(Path.Combine(folder,shot+"-high.png"));
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
