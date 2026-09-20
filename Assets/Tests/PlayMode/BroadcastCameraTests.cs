using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class BroadcastCameraTests
    {
        [Test] public void ShotSchemaRejectsUnknownDuplicateAndDegeneratePaths()
        {
            var d=BroadcastShotDefinition.Load();Assert.That(d.shots.Length,Is.EqualTo(6));Assert.Throws<ArgumentException>(()=>d.Get("missing"));
            var rail=d.Get("rail");rail.look_at=(rail.position+rail.end_position)*.5f;Assert.Throws<ArgumentException>(()=>BroadcastShotDefinition.FromJson(JsonUtility.ToJson(d)));
            d=BroadcastShotDefinition.Load();d.shots[1].id=d.shots[0].id;Assert.Throws<ArgumentException>(()=>BroadcastShotDefinition.FromJson(JsonUtility.ToJson(d)));
            d=BroadcastShotDefinition.Load();d.shots[0].fov=float.NaN;Assert.Throws<ArgumentException>(()=>BroadcastShotDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [Test] public void DedicatedCameraLeaseRejectsGameplayOwnershipAndRestoresState()
        {
            var go=new GameObject("camera");var other=new GameObject("director");
            try
            {
                var c=go.AddComponent<Camera>();var a=other.AddComponent<BroadcastCameraDirector>();var b=other.AddComponent<BroadcastCameraDirector>();var d=BroadcastShotDefinition.Load();
                Assert.Throws<ArgumentException>(()=>a.Initialize(c,d));c.enabled=false;var chase=go.AddComponent<AthleteChaseCamera>();Assert.Throws<ArgumentException>(()=>a.Initialize(c,d));Object.DestroyImmediate(chase);
                c.transform.position=new Vector3(2,3,4);c.transform.rotation=Quaternion.Euler(10,20,3);var rotation=c.transform.rotation;c.fieldOfView=67;c.nearClipPlane=.4f;c.farClipPlane=600;c.orthographic=true;
                a.Initialize(c,d);Assert.Throws<ArgumentException>(()=>b.Initialize(c,d));a.Render("sector",0);Assert.That(c.enabled,Is.True);Assert.That(c.orthographic,Is.False);
                a.enabled=false;Assert.That(c.enabled,Is.False);Assert.That(c.transform.position,Is.EqualTo(new Vector3(2,3,4)));Assert.That(c.transform.rotation,Is.EqualTo(rotation));Assert.That(c.fieldOfView,Is.EqualTo(67));Assert.That(c.nearClipPlane,Is.EqualTo(.4f));Assert.That(c.farClipPlane,Is.EqualTo(600));Assert.That(c.orthographic,Is.True);
                b.Initialize(c,d);b.Render("finish",0);b.Release();Assert.Throws<InvalidOperationException>(()=>a.Render("sector",0));
            }
            finally{Object.DestroyImmediate(other);Object.DestroyImmediate(go);}
        }
        [Test] public void TrackingAndRailCutsRewindWithoutMutatingSubjectsOrAllocating()
        {
            var go=new GameObject("camera");var subject=new GameObject("subject");
            try
            {
                var c=go.AddComponent<Camera>();c.enabled=false;var a=go.AddComponent<BroadcastCameraDirector>();var d=BroadcastShotDefinition.Load();a.Initialize(c,d);var original=c.transform.position;
                Assert.Throws<InvalidOperationException>(()=>a.Render("athlete",0));Assert.That(c.transform.position,Is.EqualTo(original));a.Bind("athlete",subject.transform);Assert.Throws<ArgumentException>(()=>a.Bind("athlete",subject.transform));
                subject.transform.position=new Vector3(4,2,10);var p=subject.transform.position;a.Render("athlete",20);Assert.That(c.transform.position,Is.EqualTo(p+d.Get("athlete").position));Assert.That(subject.transform.position,Is.EqualTo(p));
                subject.transform.position+=Vector3.right;a.Render("athlete",20);Assert.That(c.transform.position,Is.EqualTo(p+Vector3.right+d.Get("athlete").position));
                a.Render("rail",2);p=c.transform.position;var q=c.transform.rotation;a.Render("finish",0);a.Render("rail",7);a.Render("rail",2);Assert.That(c.transform.position,Is.EqualTo(p));Assert.That(c.transform.rotation,Is.EqualTo(q));
                Assert.Throws<ArgumentOutOfRangeException>(()=>a.Render("sector",double.NaN));Assert.That(c.transform.position,Is.EqualTo(p));
                for(int i=0;i<20;i++)a.Render("rail",2);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)a.Render("rail",2);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                Object.DestroyImmediate(subject);Assert.Throws<InvalidOperationException>(()=>a.Render("athlete",0));
            }
            finally{Object.DestroyImmediate(go);if(subject!=null)Object.DestroyImmediate(subject);}
        }
        [Test] public void ZoomPromotesVisualDetailButKeepsPhysicalCullingAndDefaultCameras()
        {
            var go=new GameObject("crowd");var cameraGo=new GameObject("camera");
            try
            {
                var s=go.AddComponent<SpectatorSeating>();s.Configure(StadiumArtDefinition.Load().SeatAnchors());var crowd=go.AddComponent<SpectatorCrowd>();crowd.Initialize(s);var c=cameraGo.AddComponent<Camera>();c.enabled=false;c.transform.position=new Vector3(0,3,-50);c.transform.LookAt(new Vector3(0,2,2));c.fieldOfView=15;
                Assert.That(ProjectionAwareLod.Scale(c),Is.EqualTo(1));crowd.Prepare(c,"high",12);Assert.That(crowd.Far,Is.GreaterThan(0));
                var policy=cameraGo.AddComponent<ProjectionAwareLod>();Assert.That(ProjectionAwareLod.Scale(c),Is.LessThan(.25f));crowd.Prepare(c,"high",12);Assert.That(crowd.Near,Is.GreaterThan(0));Assert.That(crowd.Far,Is.Zero);
                c.transform.position=new Vector3(0,3,-130);c.transform.LookAt(new Vector3(0,2,2));crowd.Prepare(c,"high",12);Assert.That(crowd.Visible,Is.Zero);
                policy.enabled=false;Assert.That(ProjectionAwareLod.Scale(c),Is.EqualTo(1));policy.enabled=true;c.fieldOfView=90;Assert.That(ProjectionAwareLod.Scale(c),Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraGo);}
        }
    }
}
