using System;
using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class BenchmarkProfilingTests
    {
        [UnityTest] public IEnumerator OffscreenCameraRendersAndLeavesTargetAndLoopIntact()
        {
            int leases=CountRenderLeases();
            var go=new GameObject("Offscreen profiling test");var camera=go.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.red;camera.cullingMask=0;
            var previous=new RenderTexture(64,64,24);camera.targetTexture=previous;
            var target=new BenchmarkRenderTarget(96,64);var image=new Texture2D(2,2);
            string path=System.IO.Path.Combine(Application.temporaryCachePath,"biathlon-profile-target-test.png");
            try
            {
                Assert.That(CountRenderLeases(),Is.EqualTo(leases+1));
                Assert.Throws<ArgumentNullException>(()=>target.Render(null));target.Render(camera);target.Render(camera);
                Assert.That(camera.targetTexture,Is.SameAs(previous));
                yield return null;yield return null;yield return null;
                target.SavePng(path);Assert.That(image.LoadImage(System.IO.File.ReadAllBytes(path)),Is.True);
                Assert.That(image.width,Is.EqualTo(target.Width));Assert.That(image.height,Is.EqualTo(target.Height));
                Assert.That(target.Width,Is.EqualTo(96));Assert.That(target.Height,Is.EqualTo(64));
                var pixel=image.GetPixel(32,32);Assert.That(pixel.r,Is.GreaterThan(.8f));Assert.That(pixel.g,Is.LessThan(.1f));
                target.Dispose();Assert.That(camera.targetTexture,Is.SameAs(previous));target.Dispose();
                Assert.That(CountRenderLeases(),Is.EqualTo(leases));
                Assert.Throws<ObjectDisposedException>(()=>target.Render(camera));
                Assert.Throws<ObjectDisposedException>(()=>target.SavePng(path));
            }
            finally
            {
                target.Dispose();UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(previous);UnityEngine.Object.Destroy(image);
                if(System.IO.File.Exists(path))System.IO.File.Delete(path);
            }
        }
        private static int CountRenderLeases()
        {
            int count=0;
            foreach(var phase in UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop().subSystemList)
                if(phase.subSystemList!=null)foreach(var entry in phase.subSystemList)if(entry.type==typeof(BenchmarkRenderTarget))count++;
            return count;
        }
        [Test] public void ProfileHasBoundedSamplingAndRejectsUnknownCasesOrInvalidBudgets()
        {
            var d=BenchmarkProfileDefinition.Load();Assert.That(d.cases.Length,Is.EqualTo(10));Assert.That(d.sample_frames,Is.EqualTo(300));
            string original=JsonUtility.ToJson(d);d.step_seconds=0;Assert.Throws<ArgumentException>(()=>BenchmarkProfileDefinition.FromJson(JsonUtility.ToJson(d)));
            d=JsonUtility.FromJson<BenchmarkProfileDefinition>(original);d.gpu_budget_ms=-1;Assert.Throws<ArgumentException>(()=>BenchmarkProfileDefinition.FromJson(JsonUtility.ToJson(d)));
            d=JsonUtility.FromJson<BenchmarkProfileDefinition>(original);d.cases[1].id=d.cases[0].id;Assert.Throws<ArgumentException>(()=>BenchmarkProfileDefinition.FromJson(JsonUtility.ToJson(d)));
            d=JsonUtility.FromJson<BenchmarkProfileDefinition>(original);d.cases[0].weather="unknown";Assert.Throws<ArgumentException>(()=>BenchmarkProfileDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [UnityTest] public IEnumerator CounterLeaseSamplesWithoutAllocationsAndRejectsUseAfterDispose()
        {
            var counters=new BenchmarkFrameCounters();
            try
            {
                counters.Request();yield return null;counters.Request();yield return null;
                var s=counters.Read(0,8,16);Assert.That(s.draw_calls,Is.GreaterThanOrEqualTo(-1));Assert.That(s.gpu_ms==-1||s.gpu_ms>0,Is.True);
                for(int i=0;i<20;i++)counters.Read(i,8,16);
                long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<60;i++)counters.Read(i,8,16);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                Assert.Throws<ArgumentOutOfRangeException>(()=>counters.Read(-1,8,16));Assert.Throws<ArgumentOutOfRangeException>(()=>counters.Read(0,double.NaN,16));
            }
            finally{counters.Dispose();}
            counters.Dispose();Assert.Throws<ObjectDisposedException>(()=>counters.Read(0,8,16));Assert.Throws<ObjectDisposedException>(()=>counters.Request());
        }
    }
}
