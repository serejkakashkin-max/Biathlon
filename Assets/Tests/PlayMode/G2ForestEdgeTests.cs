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
    public class G2ForestEdgeTests
    {
        [Test]
        public void AuthoredGrovesHaveStableDensityRanksAndClearBothSkiLanes()
        {
            var d=G2ForestEdge.Load();var trees=d.Place();var route=G2BenchmarkSurface.CopyRoute();var settings=VegetationDefinition.Load();
            Assert.That(trees.Length,Is.InRange(18,32));int low=0,balanced=0;
            for(int i=0;i<trees.Length;i++)
            {
                var t=trees[i];Assert.That(VegetationDefinition.DistanceToRoute(t.Position,route),Is.GreaterThanOrEqualTo(settings.route_half_width_m+settings.canopy_radius_m*t.Scale+settings.wind_amplitude_m));
                Assert.That(t.Position.z+settings.canopy_radius_m*t.Scale,Is.LessThan(-22));
                if(t.DensityRank<settings.low_density)low++;if(t.DensityRank<settings.balanced_density)balanced++;
            }
            Assert.That(low,Is.GreaterThan(0).And.LessThan(balanced));Assert.That(balanced,Is.LessThan(trees.Length));
            Array.Reverse(d.trees);var reversed=d.Place();for(int i=0;i<trees.Length;i++)Assert.That(reversed[trees.Length-1-i].Matrix,Is.EqualTo(trees[i].Matrix));
            for(int i=0;i<trees.Length;i++)Assert.That(reversed[trees.Length-1-i].DensityRank,Is.EqualTo(trees[i].DensityRank));
            d=G2ForestEdge.Load();d.trees[0].x=0;Assert.Throws<ArgumentException>(d.Validate);
            d=G2ForestEdge.Load();d.trees[0].z=-5;Assert.Throws<ArgumentException>(d.Validate);
            d=G2ForestEdge.Load();d.trees[0].id=d.trees[1].id;Assert.Throws<ArgumentException>(d.Validate);
            d=G2ForestEdge.Load();d.trees[0].scale=float.NaN;Assert.Throws<ArgumentException>(d.Validate);
            d=G2ForestEdge.Load();d.trees[10].x=15.3f;Assert.Throws<ArgumentException>(d.Validate);
        }
        [Test]
        public void AuthoredPlacementIsCopiedGroundedAndAllocationFreeDuringRendering()
        {
            var root=new GameObject("edge grove test");
            try
            {
                var f=root.AddComponent<VegetationPresentation>();var trees=G2ForestEdge.Load().Place();var original=trees[0];
                f.Initialize(G2BenchmarkSurface.CopyRoute(),true,G2BenchmarkSurface.Height,true,true,trees);trees[0]=default;
                Assert.That(f.TotalInstances,Is.EqualTo(24));Assert.That(f.InstanceWorldPosition(0).x,Is.EqualTo(original.Position.x));
                for(int i=0;i<f.TotalInstances;i++){var p=f.InstanceWorldPosition(i);Assert.That(p.y,Is.EqualTo(G2BenchmarkSurface.Height(p)).Within(.0001));}
                var eye=new GameObject("edge observer");eye.transform.SetParent(root.transform);var camera=eye.AddComponent<Camera>();camera.enabled=false;
                camera.transform.position=new Vector3(16,7,-74);camera.transform.LookAt(new Vector3(0,1,-25));
                for(int i=0;i<10;i++)f.Render(camera,"high",8);Assert.That(f.VisibleInstances,Is.GreaterThan(0));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)f.Render(camera,"high",8);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator BenchmarkGrovesPreserveRouteAndRenderAllReviewAngles()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;foreach(var go in scene.GetRootGameObjects())if(go.TryGetComponent(out VisualBenchmarkPresentation found))p=found;
                Assert.That(p,Is.Not.Null);Assert.That(p.ForestEdge,Is.Not.Null);Assert.That(p.Forests.Length,Is.EqualTo(4));
                var route=G2BenchmarkSurface.CopyRoute();foreach(var forest in p.Forests)for(int i=0;i<forest.TotalInstances;i++)Assert.That(VegetationDefinition.DistanceToRoute(forest.InstanceWorldPosition(i),route),Is.GreaterThan(5));
                p.ShowControls=false;p.enabled=false;string folder=Path.GetFullPath("artifacts/g2-forest-edge/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(string shot in new[]{"approach","athlete","finish","sector","range"})
                {
                    p.Shot=shot;p.Tier="high";p.Seek(8);
                    for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}
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
