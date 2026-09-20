using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2MotionTests
    {
        [Test]
        public void HeroStanceTransfersAreContinuousAndReturnToCarry()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var driver=rig.GetComponent<AthleteLocomotionDriver>();var body=rig.GetComponent<AthleteShootingPose>();
                var rifle=rig.GetComponent<AthleteRiflePresentation>();var grip=rig.GetComponent<AthleteGripPresentation>();
                var contact=rig.GetComponent<AthleteGroundContact>();var props=rig.GetComponent<AthleteEquipmentBinding>();
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                {
                    Sample(0);var initial=props.Rifle.position;var initialRotation=props.Rifle.rotation;
                    var previousLeft=rig.Bone(AthleteBone.LeftHand).position;var previousRight=rig.Bone(AthleteBone.RightHand).position;
                    for(int i=1;i<=400;i++)
                    {
                        Sample(i<=200?i/200f:(400-i)/200f);
                        var left=rig.Bone(AthleteBone.LeftHand).position;var right=rig.Bone(AthleteBone.RightHand).position;
                        Assert.That(Vector3.Distance(previousLeft,left),Is.LessThan(.06f),stance+" left step "+i);
                        Assert.That(Vector3.Distance(previousRight,right),Is.LessThan(.06f),stance+" right step "+i);
                        previousLeft=left;previousRight=right;
                    }
                    Assert.That(Vector3.Distance(props.Rifle.position,initial),Is.LessThan(.00001f));
                    Assert.That(Quaternion.Angle(props.Rifle.rotation,initialRotation),Is.LessThan(.001f));
                    void Sample(float progress)
                    {
                        driver.Sample(rig,new LocomotionInput(0,0,0,travelling:false),0);
                        body.Sample(rig,stance,progress);rifle.Sample(rig,progress,stance:stance);
                        contact.Sample(rig,0,0,SkiLocomotion.Idle,rifle.HandWeight);grip.Render(rig);
                    }
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test]
        public void HeroRifleContactsPreserveSightAndStowedPolesInBothStances()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                rig.transform.SetPositionAndRotation(new Vector3(1,2,3),Quaternion.Euler(0,53,0));
                var body=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var grip=rig.GetComponent<AthleteGripPresentation>();var props=rig.GetComponent<AthleteEquipmentBinding>();
                var ground=rig.GetComponent<AthleteGroundContact>();
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                foreach(float sign in new[]{-1,0,1})
                {
                    rig.ResetPose();body.Sample(rig,stance,1);var sight=new WeaponAimState(sign*.08,sign*.05);
                    rifle.Sample(rig,1,sight,stance);ground.Sample(rig,0,0,SkiLocomotion.Idle,1);
                    var position=props.Rifle.position;var rotation=props.Rifle.rotation;
                    var left=props.LeftPole.position;var right=props.RightPole.position;
                    grip.Render(rig);
                    Assert.That(Vector3.Distance(rig.Socket(AthleteSocket.LeftHandGrip).position,rifle.SupportGripWorld),Is.LessThan(.02f),stance+" support "+sign);
                    Assert.That(Vector3.Distance(rig.Socket(AthleteSocket.RightHandGrip).position,rifle.TriggerGripWorld),Is.LessThan(.02f),stance+" trigger "+sign);
                    Assert.That(props.Rifle.position,Is.EqualTo(position));Assert.That(props.Rifle.rotation,Is.EqualTo(rotation));
                    Assert.That(Vector3.Distance(left,props.LeftPole.position),Is.LessThan(.00001f));Assert.That(Vector3.Distance(right,props.RightPole.position),Is.LessThan(.00001f));
                    var ray=sight.Ray;var expected=rig.transform.TransformDirection(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ));
                    Assert.That(Vector3.Angle(props.Rifle.forward,expected),Is.LessThan(.03f));
                }
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)grip.Render(rig);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.That(rig.transform.position,Is.EqualTo(new Vector3(1,2,3)));
                var mesh=props.Rifle.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.name,Is.EqualTo("HeroRifle"));Assert.That(mesh.bounds.size.z,Is.InRange(1.0f,1.1f));
                Assert.That(mesh.triangles.Length/3,Is.LessThan(10000));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }

        [UnityTest]
        public IEnumerator HeroPreviewMatrixHasFiniteSkinAndRepeatableTransitions()
        {
            var root=new GameObject("G2 motion matrix");var bake=new Mesh();
            try
            {
                var preview=root.AddComponent<AthleteAnimationPreview>();preview.enabled=false;preview.UseHero=true;preview.Initialize();
                var camera=new GameObject("Hidden motion camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.21f,.25f);camera.cullingMask=1<<30;camera.fieldOfView=25;
                camera.transform.position=new Vector3(0,2.5f,9);camera.transform.LookAt(new Vector3(0,.9f,0));
                var key=new GameObject("Motion key").AddComponent<Light>();key.transform.SetParent(root.transform);key.type=LightType.Directional;key.intensity=1.7f;key.cullingMask=1<<30;key.transform.rotation=Quaternion.Euler(35,-145,0);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                string folder=Path.GetFullPath("artifacts/g2-motion");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,512))
                foreach(AthletePreviewSet set in Enum.GetValues(typeof(AthletePreviewSet)))
                {
                    preview.StateSet=set;
                    for(int frame=0;frame<4;frame++)
                    {
                        double time=.4+frame*.16;preview.RenderAt(time);
                        for(int i=0;i<preview.Count;i++)
                        {
                            var rig=preview.Rig(i);var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(bake);
                            foreach(var p in bake.vertices)Assert.That(float.IsFinite(p.sqrMagnitude),Is.True);
                            Assert.That(bake.bounds.size.y,Is.InRange(.1f,3f));
                            var hand=rig.Bone(AthleteBone.RightHand).position;preview.RenderAt(time);
                            Assert.That(Vector3.Distance(hand,rig.Bone(AthleteBone.RightHand).position),Is.LessThan(.0001f));
                        }
                        for(int settle=0;settle<3;settle++){target.Render(camera);yield return null;}
                        target.SavePng(Path.Combine(folder,set+"-"+frame+".png"));
                    }
                }
            }
            finally{Object.DestroyImmediate(bake);Object.DestroyImmediate(root);}
        }
    }
}
