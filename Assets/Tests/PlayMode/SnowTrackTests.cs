using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class SnowTrackTests
    {
        [Test] public void TrackHistoryBreaksOnContactGapsTeleportsAndRewindAndExpires()
        {
            var settings=SnowTrackSettings.Load();var b=new SnowTrackBuffer(settings);
            b.Sample("a",0,Vector3.zero,Vector3.up,0,true);
            b.Sample("a",0,Vector3.forward*.2f,Vector3.up,.11,true);Assert.That(b.Count,Is.EqualTo(1));
            b.Sample("a",0,Vector3.forward*.4f,Vector3.up,.22,false);
            b.Sample("a",0,Vector3.forward*.6f,Vector3.up,.33,true);Assert.That(b.Count,Is.EqualTo(1));
            b.Sample("a",0,Vector3.forward*10,Vector3.up,.44,true);Assert.That(b.Count,Is.EqualTo(1));
            b.Sample("a",0,Vector3.forward*10.2f,Vector3.up,1,true);Assert.That(b.Count,Is.EqualTo(1));
            b.Sample("a",2,Vector3.zero,Vector3.up,1,true,true);Assert.That(b.Count,Is.EqualTo(2));
            b.Sample("a",2,Vector3.zero,Vector3.up,1.11,true,true);Assert.That(b.Count,Is.EqualTo(2),"A planted pole stamps once.");
            b.Advance(settings.lifetime_s+2);Assert.That(b.Count,Is.Zero);
            b.Advance(0);Assert.That(b.EmitterCount,Is.Zero);
            Assert.Throws<ArgumentException>(()=>b.Sample("a",0,new Vector3(float.NaN,0,0),Vector3.up,0,true));
        }
        [Test] public void TrackBudgetAndWarmAllocationStayBounded()
        {
            var settings=SnowTrackSettings.Load();settings.max_marks=16;var b=new SnowTrackBuffer(settings);
            for(int i=0;i<200;i++)b.Sample("a",0,Vector3.forward*i*.1f,Vector3.up,i*.11,true);
            Assert.That(b.Count,Is.EqualTo(16));Assert.That(b.Overwritten,Is.GreaterThan(0));
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            for(int i=200;i<400;i++)b.Sample("a",0,Vector3.forward*i*.1f,Vector3.up,i*.11,true);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocated,Is.Zero);
            for(int i=0;i<settings.max_emitters+20;i++)b.Sample("extra"+i,0,Vector3.zero,Vector3.up,44,true);
            Assert.That(b.EmitterCount,Is.EqualTo(settings.max_emitters));
        }
        [Test] public void MeshCullsDistantMarksAndReusesBuffers()
        {
            var go=new GameObject("snow track test");
            try
            {
                var p=go.AddComponent<SnowTrackPresentation>();p.Initialize();
                for(int i=0;i<50;i++)p.Buffer.Sample("a",0,new Vector3(0,0,i*.1f),Vector3.up,i*.11,true);
                p.Render(5.4,Vector3.zero);Assert.That(p.VisibleMarks,Is.GreaterThan(0));Assert.That(p.Mesh.vertexCount,Is.EqualTo(p.VisibleMarks*4));
                p.Render(5.4,new Vector3(1000,0,0));Assert.That(p.VisibleMarks,Is.Zero);
                p.Render(5.4,Vector3.zero);
                long allocated=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<60;i++)p.Render(5.4,Vector3.zero);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocated,Is.Zero);
                p.Render(0,Vector3.zero);Assert.That(p.VisibleMarks,Is.Zero);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ActualHostContactMarksDoNotAdvanceOrMutateRaceSnapshot()
        {
            var go=new GameObject("snow integration");
            try
            {
                var host=go.AddComponent<PlayableSprintHost>();host.AutoStart=false;host.enabled=false;host.Initialize(2);
                var proxy=host.GetComponent<AthleteProxyPresentation>();var id=host.Runner.Field.GetAthlete(0).AthleteId;
                var chase=host.GetComponentInChildren<AthleteChaseCamera>();chase.Render(1000);
                for(int i=0;i<400;i++)
                {
                    var t=host.Runner.Field.Tick.Next();host.Step(new Biathlon.Domain.PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new Biathlon.Domain.WeaponAimCommand(t,id,0,0),false,false));
                    if(i%2==0){proxy.Render(1);chase.Render(1000);}
                }
                var snapshot=host.Feed.Current;long tick=host.Runner.Field.Tick.Value;
                proxy.Render(1);Assert.That(host.Feed.Current,Is.SameAs(snapshot));Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));
                Assert.That(go.GetComponent<SnowTrackPresentation>().Buffer.Count,Is.GreaterThan(0));
                Assert.That(go.GetComponent<SnowSprayPresentation>().Buffer.TotalEmitted,Is.GreaterThan(0));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
