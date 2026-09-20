using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2SnowTests
    {
        [Test]
        public void AuthoredTrianglesKeepSkiCorridorAndStadiumUnchanged()
        {
            var route=G2BenchmarkSurface.CopyRoute();var mesh=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurface");
            Assert.That(mesh,Is.Not.Null);var vertices=mesh.vertices;var indices=mesh.triangles;int checkedSamples=0;float largestRise=0;
            foreach(var p in vertices)
            {
                Assert.That(p.y,Is.EqualTo(G2BenchmarkSurface.Height(p)).Within(.00001f));
                float rise=p.y-VisualBenchmarkAthletes.Height(p);Assert.That(rise,Is.InRange(-.00001f,.32f));largestRise=Mathf.Max(largestRise,rise);
                if(p.z>=-10)Assert.That(rise,Is.Zero);
            }
            Assert.That(largestRise,Is.GreaterThan(.1f));
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                if(Mathf.Min(VegetationDefinition.DistanceToRoute(a,route),Mathf.Min(VegetationDefinition.DistanceToRoute(b,route),VegetationDefinition.DistanceToRoute(c,route)))>5)continue;
                // Sample the generated triangles, including their edges, not only the analytic height function.
                for(int u=0;u<=4;u++)for(int v=0;v<=4-u;v++)
                {
                    float wa=u*.25f,wb=v*.25f,wc=1-wa-wb;var p=a*wa+b*wb+c*wc;
                    if(VegetationDefinition.DistanceToRoute(p,route)>3.5f)continue;
                    float original=VisualBenchmarkAthletes.Height(a)*wa+VisualBenchmarkAthletes.Height(b)*wb+VisualBenchmarkAthletes.Height(c)*wc;
                    Assert.That(p.y,Is.EqualTo(original).Within(.00001f));checkedSamples++;
                }
            }
            Assert.That(checkedSamples,Is.GreaterThan(1000));
            Assert.Throws<ArgumentException>(()=>G2BenchmarkSurface.Height(new Vector3(float.NaN,0,0)));
            route[0]=Vector3.one;Assert.That(G2BenchmarkSurface.CopyRoute()[0],Is.EqualTo(new Vector3(0,0,-150)));
        }
        [Test]
        public void GroundConformancePreservesTreePoseAndSamplesWorldSpaceOnce()
        {
            var original=new VegetationInstance(new Vector3(2,0,3),.83f,127,.37f);var raised=original.AtHeight(4);
            Assert.That(raised.Position,Is.EqualTo(new Vector3(2,4,3)));Assert.That(raised.Scale,Is.EqualTo(original.Scale));Assert.That(raised.DensityRank,Is.EqualTo(original.DensityRank));
            for(int i=0;i<16;i++)Assert.That(raised.Matrix[i],Is.EqualTo(i==13?4:original.Matrix[i]));
            Assert.Throws<ArgumentException>(()=>original.AtHeight(float.PositiveInfinity));
            var root=new GameObject("ground conformance");
            try
            {
                root.transform.SetPositionAndRotation(new Vector3(12,3,-50),Quaternion.Euler(0,37,0));
                var forest=root.AddComponent<VegetationPresentation>();int samples=0;
                forest.Initialize(VegetationPreview.Route,true,p=>{samples++;return G2BenchmarkSurface.Height(p);});
                Assert.That(samples,Is.EqualTo(forest.TotalInstances));Assert.That(samples,Is.GreaterThan(0));
                for(int i=0;i<forest.TotalInstances;i++){var p=forest.InstanceWorldPosition(i);Assert.That(p.y,Is.EqualTo(G2BenchmarkSurface.Height(p)).Within(.00001f));}
                var camera=root.AddComponent<Camera>();camera.enabled=false;forest.Prepare(camera,"high",0);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)forest.Prepare(camera,"high",i);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(samples,Is.EqualTo(forest.TotalInstances));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void AuditedSnowMapsAreBoundedAndLegacyDetailIsOptIn()
        {
            var mat=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");Assert.That(mat,Is.Not.Null);Assert.That(mat.shader.isSupported,Is.True);
            var settings=G2BenchmarkSurface.Load();Assert.That(mat.GetFloat("_DetailWeight"),Is.EqualTo(settings.snow_detail_weight));
            Assert.That(mat.GetFloat("_DetailMeters"),Is.EqualTo(settings.snow_detail_m));
            foreach(string name in new[]{"Albedo","Normal","Roughness"})
            {
                var texture=mat.GetTexture("_Detail"+name) as Texture2D;Assert.That(texture,Is.Not.Null);
                Assert.That(Mathf.Max(texture.width,texture.height),Is.LessThanOrEqualTo(name=="Roughness"?1024:2048));Assert.That(texture.mipmapCount,Is.GreaterThan(1));
#if UNITY_EDITOR
                var importer=UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(texture)) as UnityEditor.TextureImporter;
                Assert.That(importer.sRGBTexture,Is.EqualTo(name=="Albedo"));
                if(name=="Normal")Assert.That(importer.textureType,Is.EqualTo(UnityEditor.TextureImporterType.NormalMap));
#endif
            }
            var legacy=new Material(mat.shader);try{Assert.That(legacy.GetFloat("_DetailWeight"),Is.Zero);}finally{Object.DestroyImmediate(legacy);}
        }
        [UnityTest]
        public IEnumerator IntegratedSnowAndForestProduceHiddenReviewFrames()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation benchmark=null;foreach(var go in scene.GetRootGameObjects())if(go.TryGetComponent(out VisualBenchmarkPresentation p))benchmark=p;
                Assert.That(benchmark,Is.Not.Null);Assert.That(benchmark.Ready,Is.True);benchmark.ShowControls=false;
                foreach(var forest in benchmark.Forests)for(int i=0;i<forest.TotalInstances;i++){var p=forest.InstanceWorldPosition(i);Assert.That(p.y,Is.EqualTo(G2BenchmarkSurface.Height(p)).Within(.00001f));}
                string folder=Path.GetFullPath("artifacts/g2-snow/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(string shot in new[]{"approach","sector","athlete","range"})
                {
                    benchmark.Shot=shot;benchmark.Tier="high";benchmark.Seek(8);
                    for(int frame=0;frame<3;frame++){benchmark.RenderFrame(8);target.Render(benchmark.View);yield return null;}
                    target.SavePng(Path.Combine(folder,shot+"-high.png"));
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}
