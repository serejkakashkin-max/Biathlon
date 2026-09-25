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
    public class G2ShadowGeometryTests
    {
        [Test]
        public void ForestShadowLodsReduceGeometryAndPreserveVisibleSubmission()
        {
            var root=new GameObject("forest shadow parity");
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,2,-48);camera.farClipPlane=200;
                var a=root.AddComponent<VegetationPresentation>();var b=root.AddComponent<VegetationPresentation>();
                a.Initialize(VegetationPreview.Route,true,useAuditedPine:true);b.Initialize(VegetationPreview.Route,true,useAuditedPine:true,useSimplifiedShadows:true);
                foreach(string tier in new[]{"low","balanced","high"})
                {
                    a.Prepare(camera,tier,8);b.Prepare(camera,tier,8);Assert.That(b.VisibleInstances,Is.EqualTo(a.VisibleInstances));Assert.That(b.SubmittedTriangles,Is.EqualTo(a.SubmittedTriangles));
                    if(tier=="high")Assert.That(b.ShadowSourceTriangles,Is.EqualTo(a.ShadowSourceTriangles));
                    else Assert.That(b.ShadowSourceTriangles,Is.GreaterThan(0).And.LessThan(a.ShadowSourceTriangles));
                    for(int i=0;i<a.TotalInstances;i++)Assert.That(b.InstanceWorldPosition(i),Is.EqualTo(a.InstanceWorldPosition(i)));
                }
                for(int i=0;i<10;i++)b.Render(camera,"high",8);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)b.Render(camera,"high",8);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void CrowdShadowLodPreservesPopulationAndVisibleMesh()
        {
            var root=new GameObject("crowd shadow parity");var observer=new GameObject("observer");
            try
            {
                var seats=root.AddComponent<SpectatorSeating>();seats.Configure(StadiumArtDefinition.Load().SeatAnchors());
                var a=root.AddComponent<SpectatorCrowd>();var b=root.AddComponent<SpectatorCrowd>();a.Initialize(seats);b.Initialize(seats,true);
                var camera=observer.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,3,-18);camera.transform.LookAt(new Vector3(0,2,2));
                foreach(string tier in new[]{"low","balanced","high"})
                {
                    a.Prepare(camera,tier,8);b.Prepare(camera,tier,8);Assert.That(b.Visible,Is.EqualTo(a.Visible));Assert.That(b.Near,Is.EqualTo(a.Near));Assert.That(b.SubmittedTriangles,Is.EqualTo(a.SubmittedTriangles));
                    if(tier=="high")Assert.That(b.ShadowSourceTriangles,Is.EqualTo(a.ShadowSourceTriangles));
                    else Assert.That(b.ShadowSourceTriangles,Is.GreaterThan(0).And.LessThan(a.ShadowSourceTriangles*.25));
                }
                for(int i=0;i<10;i++)b.Render(camera,"high",8);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)b.Render(camera,"high",8);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(observer);}
        }
        [Test]
        public void TerrainShadowMeshRetainsHeightWithinEightCentimetres()
        {
            var surface=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurface");var shadow=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurfaceShadow");
            Assert.That(shadow,Is.Not.Null);Assert.That(shadow.GetIndexCount(0),Is.LessThan(surface.GetIndexCount(0)*.4));Assert.That(shadow.bounds.size.x,Is.EqualTo(surface.bounds.size.x));Assert.That(shadow.bounds.size.z,Is.EqualTo(surface.bounds.size.z));
            var vertices=shadow.vertices;int columns=1;while(columns<vertices.Length&&vertices[columns].z==vertices[0].z)columns++;int rows=vertices.Length/columns;
            var xs=new float[columns];var zs=new float[rows];for(int i=0;i<columns;i++)xs[i]=vertices[i].x;for(int i=0;i<rows;i++)zs[i]=vertices[i*columns].z;
            foreach(var p in surface.vertices)
            {
                int ix=Array.BinarySearch(xs,p.x),iz=Array.BinarySearch(zs,p.z);if(ix<0)ix=~ix-1;if(iz<0)iz=~iz-1;ix=Mathf.Min(ix,columns-2);iz=Mathf.Min(iz,rows-2);
                float u=(p.x-xs[ix])/(xs[ix+1]-xs[ix]),v=(p.z-zs[iz])/(zs[iz+1]-zs[iz]);
                int a=iz*columns+ix;float h=u+v<=1?vertices[a].y*(1-u-v)+vertices[a+1].y*u+vertices[a+columns].y*v:
                    vertices[a+1].y*(1-v)+vertices[a+columns].y*(1-u)+vertices[a+columns+1].y*(u+v-1);
                Assert.That(Mathf.Abs(h-p.y),Is.LessThanOrEqualTo(.08f));
            }
        }
        [UnityTest]
        public IEnumerator IntegratedProxyShadowsKeepVisibleSurfaceAndProduceReviewFrames()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;int surfaceCount=0,shadowCount=0;
                var visible=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurface");var shadow=Resources.Load<Mesh>("Biathlon/Benchmark/SectorSurfaceShadow");
                foreach(var go in scene.GetRootGameObjects())
                {
                    if(go.TryGetComponent(out VisualBenchmarkPresentation found))p=found;
                    foreach(var f in go.GetComponentsInChildren<MeshFilter>())
                    {
                        if(f.sharedMesh==visible){Assert.That(f.GetComponent<Renderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));surfaceCount++;}
                        if(f.sharedMesh==shadow){Assert.That(f.GetComponent<Renderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.ShadowsOnly));shadowCount++;}
                    }
                }
                Assert.That(surfaceCount,Is.EqualTo(1));Assert.That(shadowCount,Is.EqualTo(1));Assert.That(p,Is.Not.Null);
                foreach(var f in p.Forests)Assert.That(f.UsesSimplifiedShadows,Is.True);Assert.That(p.GetComponent<SpectatorCrowd>().UsesSimplifiedShadows,Is.True);
                p.ShowControls=false;p.enabled=false;string folder=Path.GetFullPath("artifacts/g2-shadow/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(string shot in new[]{"athlete","range","crowd_zoom"})
                {
                    p.Shot=shot;p.Tier="high";p.Seek(8);
                    for(int frame=0;frame<3;frame++){p.RenderFrame(8);target.Render(p.View);yield return null;}
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
