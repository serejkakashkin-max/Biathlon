using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class AthleteImpactPresentationTests
    {
        private static SkiImpactObservation Impact()
        {
            var body=OccupancyDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/occupancy_provisional_v1"));
            var physics=new ManualSkiContactSystem(body,SkiContactDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/manual_ski_contact_gameplay_v1")));
            var a=new ManualSkiContactBody(new AthleteId("a"),new ManualSkiHeadingState(.45,0,0),new SkiPlanarVelocity(0,10),75);
            var b=new ManualSkiContactBody(new AthleteId("b"),new ManualSkiHeadingState(0,body.LongitudinalSeparation,0),new SkiPlanarVelocity(0,3),75);
            var hit=physics.PreviewAngularContact(a,b);
            Assert.That(Math.Abs(hit.FirstYawRate),Is.GreaterThan(1));
            return (SkiImpactObservation)Activator.CreateInstance(typeof(SkiImpactObservation),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{hit.Contact.FirstVelocity.X-a.Velocity.X,hit.Contact.FirstVelocity.Z-a.Velocity.Z,hit.FirstYawRate},null);
        }
        [Test] public void ActualImpactHasVisibleTwistRecoversAndDoesNotRestartForTheSameTick()
        {
            var settings=AthleteImpactVisualSettings.Load();var pose=new ManualSkiHeadingState(0,0,0);var impact=new AthleteImpactPresentation(settings);
            impact.Observe(default,pose,0,20,true);Assert.That(impact.Angles(.2),Is.EqualTo(Vector3.zero));
            var signal=Impact();impact.Observe(signal,pose,1,20,true);var at=impact.Angles(.2);
            Assert.That(Math.Abs(at.y),Is.GreaterThan(8));Assert.That(Math.Abs(at.x),Is.GreaterThan(8));
            impact.Observe(signal,pose,1,20,true);Assert.That(impact.Angles(.2),Is.EqualTo(at));
            impact.Observe(default,pose,2,20,true);Assert.That(impact.Angles(.2),Is.EqualTo(at));
            Assert.That(impact.Angles(settings.recovery_seconds+.01),Is.EqualTo(Vector3.zero));
            impact.Observe(signal,pose,10,20,true);impact.Observe(default,pose,0,20,true);Assert.That(impact.Angles(.2),Is.EqualTo(Vector3.zero));
            impact.Observe(signal,pose,1,20,false);Assert.That(impact.Angles(.2),Is.EqualTo(Vector3.zero),"Shooting/waiting never acquires a skiing impact pose.");
        }
        [Test] public void AnimationAdapterRetainsImpactThroughGroundAndEquipmentCorrections()
        {
            var root=new GameObject("Integrated impact pose");
            try
            {
                var a=OriginalAthleteRigBuilder.Create(root.transform);var b=OriginalAthleteRigBuilder.Create(root.transform);
                var control=a.gameObject.AddComponent<AthleteAnimationPresentation>();control.Initialize(a);
                var damaged=b.gameObject.AddComponent<AthleteAnimationPresentation>();damaged.Initialize(b);
                var ordinary=State(default);var struck=State(Impact());var position=b.transform.position;var rotation=b.transform.rotation;
                control.Render(new AthleteAnimationFrame(ordinary,1,20,1));damaged.Render(new AthleteAnimationFrame(struck,1,20,1));
                control.Render(new AthleteAnimationFrame(ordinary,6,20,1));damaged.Render(new AthleteAnimationFrame(ordinary,6,20,1));
                Assert.That(Quaternion.Angle(a.Bone(AthleteBone.Chest).localRotation,b.Bone(AthleteBone.Chest).localRotation),Is.GreaterThan(8));
                Assert.That(b.transform.position,Is.EqualTo(position));Assert.That(b.transform.rotation,Is.EqualTo(rotation));
                for(int i=0;i<(int)AthleteBone.Count;i++){var p=b.Bone((AthleteBone)i).position;Assert.That(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),Is.True);}
            }
            finally{Object.DestroyImmediate(root);}
            AthletePresentationState State(SkiImpactObservation impact)
            {
                object boxed=default(AthletePresentationState);
                void Set(string name,object value)=>typeof(AthletePresentationState).GetField("<"+name+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(boxed,value);
                Set("AthleteId","impact_pose_fixture");Set("OnCourse",true);Set("SpeedMetersPerSecond",8d);Set("WorldPose",new ManualSkiHeadingState(0,0,0));Set("Impact",impact);
                return (AthletePresentationState)boxed;
            }
        }
        [UnityTest] public IEnumerator OriginalRigShowsImpulseWithoutMovingPhysicalRootOrChangingShootPose()
        {
            var root=new GameObject("Impact original rig review");BenchmarkRenderTarget target=null;
            try
            {
                var preview=root.AddComponent<AthleteAnimationPreview>();preview.enabled=false;preview.RenderAt(.4);
                var rig=preview.Rig(1);for(int i=0;i<preview.Count;i++)if(i!=1)preview.Rig(i).gameObject.SetActive(false);
                rig.transform.localPosition=Vector3.zero;var position=rig.transform.position;var rotation=rig.transform.rotation;
                var camera=new GameObject("Impact review camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.enabled=false;
                camera.transform.position=new Vector3(3,2.3f,5);camera.transform.LookAt(Vector3.up);camera.fieldOfView=32;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.13f,.18f);
                var light=new GameObject("Impact review light").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(35,-30,0);
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                target=new BenchmarkRenderTarget(800,800);string folder="artifacts/m16-6/traffic-contact/visual";Directory.CreateDirectory(folder);
                for(int i=0;i<12;i++){target.Render(camera);yield return null;}target.SavePng(Path.Combine(folder,"impact-before.png"));
                var chest=rig.Bone(AthleteBone.Chest).localRotation;var effect=new AthleteImpactPresentation(AthleteImpactVisualSettings.Load());effect.Observe(Impact(),new ManualSkiHeadingState(0,0,0),1,20,true);effect.Apply(rig,.2);
                Assert.That(Quaternion.Angle(chest,rig.Bone(AthleteBone.Chest).localRotation),Is.GreaterThan(10));
                Assert.That(rig.transform.position,Is.EqualTo(position));Assert.That(rig.transform.rotation,Is.EqualTo(rotation));
                for(int i=0;i<3;i++){target.Render(camera);yield return null;}target.SavePng(Path.Combine(folder,"impact-reaction.png"));
                rig.ResetPose();preview.RenderAt(.4);rig.transform.localPosition=Vector3.zero;effect.Apply(rig,1);
                Assert.That(Quaternion.Angle(chest,rig.Bone(AthleteBone.Chest).localRotation),Is.LessThan(.01));
                for(int i=0;i<3;i++){target.Render(camera);yield return null;}target.SavePng(Path.Combine(folder,"impact-recovered.png"));
                Assert.That(new FileInfo(Path.Combine(folder,"impact-reaction.png")).Length,Is.GreaterThan(20000),"The native capture must contain the rendered rig, not an unrendered black target.");
            }
            finally{target?.Dispose();Object.DestroyImmediate(root);}
        }
    }
}
