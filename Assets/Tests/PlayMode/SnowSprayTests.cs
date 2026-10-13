using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class SnowSprayTests
    {
        [Test] public void ContactDistanceTuckAndPoleOnsetControlBoundedEmission()
        {
            var s=SnowSpraySettings.Load();var b=new SnowSprayBuffer(s);
            b.Sample("a",0,Vector3.zero,Vector3.up,Vector3.forward,0,false,8,1);Assert.That(b.ActiveCount,Is.Zero);
            b.Sample("a",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,100);Assert.That(b.ActiveCount,Is.Zero);
            b.Sample("a",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,1);Assert.That(b.TotalEmitted,Is.EqualTo(s.ski_count));
            b.Sample("a",0,Vector3.zero,Vector3.up,Vector3.forward,.01,true,8,1);Assert.That(b.TotalEmitted,Is.EqualTo(s.ski_count));
            b.Sample("a",2,Vector3.zero,Vector3.up,Vector3.forward,.01,true,8,1);int count=b.TotalEmitted;
            b.Sample("a",2,Vector3.zero,Vector3.up,Vector3.forward,.2,true,8,1);Assert.That(b.TotalEmitted,Is.EqualTo(count));
            b.Clear();b.Sample("a",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,8,1,true);Assert.That(b.TotalEmitted,Is.EqualTo(1));
            b.Advance(3);Assert.That(b.ActiveCount,Is.Zero);b.Advance(0);Assert.That(b.TotalEmitted,Is.Zero);
        }
        [Test] public void RepeatAndRewindProduceSameVisualScatterWithoutGrowingMemory()
        {
            var s=SnowSpraySettings.Load();s.max_particles=16;s.performance_draw_budget=8;s.high_draw_budget=12;s.ultra_draw_budget=16;var a=new SnowSprayBuffer(s);var b=new SnowSprayBuffer(s);
            for(int i=0;i<30;i++){a.Sample("racer",i%4,Vector3.zero,Vector3.up,Vector3.forward,i*.01,true,10,0);b.Sample("racer",i%4,Vector3.zero,Vector3.up,Vector3.forward,i*.01,true,10,0);}
            Assert.That(a.ActiveCount,Is.LessThanOrEqualTo(16));Assert.That(a.Overwritten,Is.GreaterThan(0));
            for(int i=0;i<a.Capacity;i++){Assert.That(a.Get(i).Velocity,Is.EqualTo(b.Get(i).Velocity));Assert.That(a.Get(i).Origin,Is.EqualTo(b.Get(i).Origin));}
            long allocation=GC.GetAllocatedBytesForCurrentThread();
            for(int i=30;i<200;i++)a.Sample("racer",i%4,Vector3.zero,Vector3.up,Vector3.forward,i*.01,true,10,0);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocation,Is.Zero);
            a.Clear();b.Clear();a.Sample("racer",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,10,0);b.Sample("racer",0,Vector3.zero,Vector3.up,Vector3.forward,0,true,10,0);
            Assert.That(a.Get(0).Velocity,Is.EqualTo(b.Get(0).Velocity));
            Assert.Throws<ArgumentException>(()=>SnowSpraySettings.FromJson(JsonUtility.ToJson(s).Replace("\"max_particles\":16","\"max_particles\":0")));
        }
        [Test] public void BillboardMeshCullsExpiresAndAllocatesNothingAfterWarmup()
        {
            var go=new GameObject("spray test");
            try
            {
                var p=go.AddComponent<SnowSprayPresentation>();p.Initialize();var camera=go.AddComponent<Camera>();
                p.Buffer.Sample("a",0,Vector3.forward,Vector3.up,Vector3.forward,0,true,8,0);p.Render(.1,camera);Assert.That(p.VisibleParticles,Is.GreaterThan(0));
                long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<60;i++)p.Render(.1,camera);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocation,Is.Zero);
                camera.transform.position=Vector3.one*1000;p.Render(.1,camera);Assert.That(p.VisibleParticles,Is.Zero);
                p.Render(3,camera);Assert.That(p.Buffer.ActiveCount,Is.Zero);
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
