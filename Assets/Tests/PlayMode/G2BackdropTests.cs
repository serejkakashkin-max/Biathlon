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
    public class G2BackdropTests
    {
        [Test] public void RootHeightMatchesRenderedTrianglePlanesAcrossTheEntireBasin()
        {
            var settings=BenchmarkBackdropSettings.Load();var mesh=G2BenchmarkBackdrop.BuildLandscape(settings);
            try
            {
                var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var p=vertices[indices[i]]*.23f+vertices[indices[i+1]]*.31f+vertices[indices[i+2]]*.46f;
                    Assert.That(G2BenchmarkBackdrop.SurfaceHeight(settings,p),Is.EqualTo(p.y).Within(.001f),"Root sample must match the actual rendered triangle.");
                }
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void DistantLandscapeIsBoundedUpwardAndOutsideEveryRouteSegment()
        {
            var settings=BenchmarkBackdropSettings.Load();var mesh=G2BenchmarkBackdrop.BuildLandscape(settings);
            try
            {
                Assert.That(mesh.GetIndexCount(0)/3,Is.LessThan(10000));var vertices=mesh.vertices;var indices=mesh.triangles;
                foreach(var p in vertices){Assert.That(float.IsFinite(p.sqrMagnitude),Is.True);Assert.That(VegetationDefinition.DistanceToRoute(p,G2BenchmarkSurface.CopyRoute()),Is.GreaterThan(7));Assert.That(p.y,Is.InRange(-1.001f,100));}
                for(int i=0;i<indices.Length;i+=3)Assert.That(Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).y,Is.GreaterThan(0));
                for(int r=0;r<=settings.rings;r++)Assert.That(Vector3.Distance(vertices[r*(settings.segments+1)],vertices[r*(settings.segments+1)+settings.segments]),Is.LessThan(.0001f));
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [UnityTest] public IEnumerator IntegratedBackdropKeepsWeatherStableAndHasNoGameplayColliders()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent(out VisualBenchmarkPresentation found))p=found;
                Assert.That(p.Backdrop,Is.Not.Null);Assert.That(p.Backdrop.TriangleCount,Is.LessThan(5000));Assert.That(p.Backdrop.GetComponentsInChildren<Collider>(),Is.Empty);
                p.enabled=false;p.ShowControls=false;p.Tier="high";var folder=Path.GetFullPath("artifacts/g2-composition/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                {
                foreach(string weather in new[]{"clear","overcast","snowfall"})
                {
                    p.WeatherPreset=weather;p.Shot="approach";p.Seek(8);float fog=RenderSettings.fogEndDistance;
                    Assert.That(p.Backdrop.VisibleTrees,Is.InRange(1,p.Backdrop.TreeCount));
                    for(int i=0;i<10;i++)p.Backdrop.RenderTrees(p.View);
                    long bytes=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<30;i++)p.Backdrop.RenderTrees(p.View);
                    Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                    for(int i=0;i<3;i++){p.RenderFrame(8);Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(fog));target.Render(p.View);yield return null;}
                    target.SavePng(Path.Combine(folder,weather+"-approach.png"));
                }
                p.WeatherPreset="clear";p.Shot="range";p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"clear-range.png"));
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}
