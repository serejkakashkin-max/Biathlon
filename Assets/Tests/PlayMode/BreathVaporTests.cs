using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class BreathVaporTests
    {
        [Test] public void ColdLoadEmissionRespectsHoldPhaseWarmthAndRewind()
        {
            var d=BreathVaporSettings.Load();Assert.That(d.Intensity(10,1),Is.Zero);Assert.That(d.Intensity(-15,1),Is.GreaterThan(d.Intensity(-15,0)));
            var b=new BreathVaporBuffer(d);var cold=new BreathEnvironment(-15,Vector2.zero);var warm=new BreathEnvironment(10,Vector2.zero);
            b.Observe("a",0,Vector3.zero,Vector3.forward,1,warm,true,false);Assert.That(b.TotalEmitted,Is.Zero);
            b.Observe("b",0,Vector3.zero,Vector3.forward,1,cold,true,true,0);Assert.That(b.TotalEmitted,Is.Zero);
            b.Observe("b",.1,Vector3.zero,Vector3.forward,1,cold,true,false,.9);Assert.That(b.TotalEmitted,Is.Zero);
            b.Observe("b",.2,Vector3.zero,Vector3.forward,1,cold,true,false,.05);Assert.That(b.TotalEmitted,Is.EqualTo(d.puffs_per_exhale));
            b.Observe("b",.2,Vector3.zero,Vector3.forward,1,cold,true,false,.05);Assert.That(b.TotalEmitted,Is.EqualTo(d.puffs_per_exhale));
            b.Clear();b.Observe("ski",0,Vector3.one,Vector3.forward,1,cold,true,false);var first=b.Get(0);b.Advance(2);Assert.That(b.Get(0).Strength,Is.Zero);
            b.Observe("ski",0,Vector3.one,Vector3.forward,1,cold,true,false);Assert.That(b.Get(0).Origin,Is.EqualTo(first.Origin));Assert.That(b.Get(0).Velocity,Is.EqualTo(first.Velocity));
            Assert.Throws<ArgumentException>(()=>new BreathEnvironment(float.NaN,Vector2.zero));Assert.Throws<ArgumentException>(()=>b.Observe("x",0,Vector3.zero,Vector3.zero,1,cold,true,false));
        }
        [Test] public void FixedBudgetsAndWarmedEmissionDoNotAllocate()
        {
            var d=BreathVaporSettings.Load();d.max_puffs=5;d.max_sources=2;var b=new BreathVaporBuffer(d);var cold=new BreathEnvironment(-15,Vector2.right);
            b.Observe("a",0,Vector3.zero,Vector3.forward,1,cold,true,false);b.Observe("b",0,Vector3.zero,Vector3.forward,1,cold,true,false);Assert.That(b.Overwritten,Is.EqualTo(1));b.Observe("c",0,Vector3.zero,Vector3.forward,1,cold,true,false);Assert.That(b.TotalEmitted,Is.EqualTo(6));
            for(int i=1;i<30;i++)b.Observe("a",i*.1,Vector3.zero,Vector3.forward,1,cold,true,false);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=30;i<300;i++)b.Observe("a",i*.1,Vector3.zero,Vector3.forward,1,cold,true,false);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void CameraCullUnsetEnvironmentAndWarmMeshRenderingAreBounded()
        {
            var go=new GameObject("vapor test");var cameraObject=new GameObject("vapor camera");
            try{var p=go.AddComponent<BreathVaporPresentation>();p.Initialize();var c=cameraObject.AddComponent<Camera>();c.enabled=false;c.transform.position=new Vector3(0,1,-2);var env=new BreathEnvironment(-15,Vector2.zero);p.Buffer.Observe("a",0,Vector3.up,Vector3.forward,1,env,true,false);p.Render(.2,c);Assert.That(p.SubmittedPuffs,Is.Zero);p.SetEnvironment(env);p.Render(.2,c);Assert.That(p.SubmittedPuffs,Is.EqualTo(3));for(int i=0;i<20;i++)p.Render(.2,c);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)p.Render(.2,c);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);c.transform.position=Vector3.one*100;p.Render(.2,c);Assert.That(p.SubmittedPuffs,Is.Zero);p.SetEnvironment(null);Assert.That(p.Buffer.TotalEmitted,Is.Zero);}
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraObject);}
        }
        [Test] public void ActualHostVaporOnlyReadsDetachedRaceObservations()
        {
            var go=new GameObject("vapor integration");
            try{var host=go.AddComponent<PlayableSprintHost>();host.AutoStart=false;host.enabled=false;host.Initialize(2);var proxy=host.GetComponent<AthleteProxyPresentation>();var vapor=go.GetComponent<BreathVaporPresentation>();proxy.Render(1);Assert.That(vapor.Buffer.TotalEmitted,Is.Zero);proxy.SetVisualBreathEnvironment(new BreathEnvironment(-15,Vector2.zero));var id=host.Runner.Field.GetAthlete(0).AthleteId;var chase=host.GetComponentInChildren<AthleteChaseCamera>();for(int i=0;i<400;i++){var t=host.Runner.Field.Tick.Next();host.Step(new Biathlon.Domain.PlayerCommand(t,id,800,0),new ShootingDeviceCommand(new Biathlon.Domain.WeaponAimCommand(t,id,0,0),false,false));if(i%2==0){chase.Render(1000);proxy.Render(1);}}var snapshot=host.Feed.Current;long tick=host.Runner.Field.Tick.Value;proxy.Render(1);Assert.That(host.Feed.Current,Is.SameAs(snapshot));Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));Assert.That(vapor.Buffer.TotalEmitted,Is.GreaterThan(0));}
            finally{Object.DestroyImmediate(go);}
        }
    }
}
