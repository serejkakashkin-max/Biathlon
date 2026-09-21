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
    public class G2UnderstoryTests
    {
        private const string Root="Biathlon/Vegetation/G2Shrub/";
        [Test]
        public void WinterSkeletonRetainsBranchUvsAndBoundedDecreasingLods()
        {
            int prior=2500;
            for(int i=0;i<3;i++)
            {
                var m=Resources.Load<Mesh>(Root+"ShrubLOD"+i);Assert.That(m,Is.Not.Null);int triangles=(int)m.GetIndexCount(0)/3;
                Assert.That(triangles,Is.GreaterThan(200).And.LessThan(prior));prior=triangles;
                Assert.That(m.bounds.min.y,Is.InRange(-.002f,.02f));Assert.That(m.bounds.max.y,Is.InRange(.85f,.95f));
                Assert.That(m.normals.Length,Is.EqualTo(m.vertexCount));Assert.That(m.tangents.Length,Is.EqualTo(m.vertexCount));
                foreach(var uv in m.uv)Assert.That(uv.x,Is.LessThan(.2f));
                foreach(var v in m.vertices)Assert.That(new Vector2(v.x,v.z).magnitude,Is.LessThan(.7f));
            }
            var mat=Resources.Load<Material>(Root+"Shrub");Assert.That(mat.shader.isSupported,Is.True);Assert.That(mat.enableInstancing,Is.True);
            foreach(string channel in new[]{"_LeafAlbedo","_LeafNormal"}){var t=mat.GetTexture(channel);Assert.That(t,Is.Not.Null);Assert.That(Math.Max(t.width,t.height),Is.LessThanOrEqualTo(1024));}
            Assert.That(mat.GetTexture("_BarkAlbedo"),Is.SameAs(mat.GetTexture("_LeafAlbedo")));
        }
        [Test]
        public void PlacementRespectsBothRouteLegsAndRendersWithoutAllocations()
        {
            var d=G2Understory.Load();Assert.That(d.plants.Length,Is.EqualTo(24));var route=G2BenchmarkSurface.CopyRoute();
            foreach(var plant in d.plants)Assert.That(VegetationDefinition.DistanceToRoute(new Vector3(plant.x,0,plant.z),route),Is.GreaterThanOrEqualTo(5.5f+.7f*plant.scale));
            d.plants[0].x=0;Assert.Throws<ArgumentException>(d.Validate);d=G2Understory.Load();d.plants[0].id=d.plants[1].id;Assert.Throws<ArgumentException>(d.Validate);
            d=G2Understory.Load();d.cull_m=float.NaN;Assert.Throws<ArgumentException>(d.Validate);
            var root=new GameObject("winter understory");
            try
            {
                var p=root.AddComponent<G2Understory>();p.Initialize();var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(16,7,-74);camera.transform.LookAt(new Vector3(0,1,-25));
                d=G2Understory.Load();for(int i=0;i<p.Count;i++)Assert.That(p.Position(i).y,Is.EqualTo(G2BenchmarkSurface.Height(p.Position(i))-d.burial_m).Within(.0001));
                for(int i=0;i<10;i++)p.Render(camera);Assert.That(p.Visible,Is.GreaterThan(0));long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)p.Render(camera);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                camera.transform.position=new Vector3(0,100,400);p.Prepare(camera);Assert.That(p.Visible,Is.Zero);Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator WinterBranchLodsAndIntegratedViewsProduceReviewFrames()
        {
            var root=new GameObject("shrub review");Scene scene=default;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.39f,.44f);
                var shrub=new GameObject("winter branches");shrub.transform.SetParent(root.transform);var filter=shrub.AddComponent<MeshFilter>();shrub.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>(Root+"Shrub");
                var eye=new GameObject("hidden camera");eye.transform.SetParent(root.transform);var camera=eye.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.58f,.66f,.72f);camera.transform.position=new Vector3(1.4f,.8f,1.4f);camera.transform.LookAt(new Vector3(0,.45f,0));
                var light=new GameObject("winter key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(40,-140,0);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                string folder=Path.GetFullPath("artifacts/g2-shrub/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1024,1024))for(int lod=0;lod<3;lod++)
                {
                    filter.sharedMesh=Resources.Load<Mesh>(Root+"ShrubLOD"+lod);for(int i=0;i<3;i++){target.Render(camera);yield return null;}target.SavePng(Path.Combine(folder,"shrub-lod"+lod+".png"));
                }
                Object.DestroyImmediate(root);root=null;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;
#if UNITY_EDITOR
                scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
                VisualBenchmarkPresentation p=null;foreach(var go in scene.GetRootGameObjects())if(go.TryGetComponent(out VisualBenchmarkPresentation found))p=found;
                Assert.That(p,Is.Not.Null);Assert.That(p.Understory.Count,Is.EqualTo(24));p.ShowControls=false;p.enabled=false;
                using(var target=new BenchmarkRenderTarget(1920,1080))foreach(string shot in new[]{"athlete","approach","range"})
                {
                    p.Shot=shot;p.Tier="high";p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,shot+"-high.png"));
                }
#endif
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;}
        }
    }
}
