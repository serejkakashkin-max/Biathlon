using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2SkiTechniqueTests
    {
        [Test] public void SkateCycleKeepsSupportDuringLiftedRecoveryAndReturnsContinuously()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var driver=rig.GetComponent<AthleteLocomotionDriver>();int leftLifted=0,rightLifted=0,bothGrounded=0;
                Vector3 previousLeft=default,previousRight=default;
                for(int i=0;i<360;i++)
                {
                    driver.Sample(rig,new LocomotionInput(7,0,.9f),i/120.0);
                    var left=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.LeftFoot).position);
                    var right=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.RightFoot).position);
                    bool l=left.y>.125f,r=right.y>.125f;if(l)leftLifted++;if(r)rightLifted++;if(!l&&!r)bothGrounded++;
                    Assert.That(l&&r,Is.False,"Skating cycle must retain one grounded support ski.");
                    if(i>0){Assert.That(Vector3.Distance(left,previousLeft),Is.LessThan(.03f));Assert.That(Vector3.Distance(right,previousRight),Is.LessThan(.03f));}
                    previousLeft=left;previousRight=right;
                }
                Assert.That(leftLifted,Is.GreaterThan(30));Assert.That(rightLifted,Is.GreaterThan(30));Assert.That(bothGrounded,Is.GreaterThan(30));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void BothHandsPushTogetherAndHipsTransferWithoutRootMovement()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteLocomotionDriver>();float lo=10,hi=-10;
                for(int i=0;i<180;i++)
                {
                    d.Sample(rig,new LocomotionInput(7,0,.9f),i/60.0);
                    var l=rig.Bone(AthleteBone.LeftHand).position;var right=rig.Bone(AthleteBone.RightHand).position;
                    Assert.That(Mathf.Abs(l.z-right.z),Is.LessThan(.02f),"Skating poling must not alternate like walking.");
                    float hip=rig.Bone(AthleteBone.Hips).position.x;lo=Mathf.Min(lo,hip);hi=Mathf.Max(hi,hip);
                    Assert.That(rig.transform.position,Is.EqualTo(Vector3.zero));
                }
                Assert.That(hi-lo,Is.GreaterThan(.17f));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=180;i<300;i++)d.Sample(rig,new LocomotionInput(7,0,.9f),i/60.0);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void MovingPlantStaysInWorldAndReleasesForRecoveryTeleportAndShooting()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();var c=rig.GetComponent<AthleteGroundContact>();
                var grip=rig.GetComponent<AthleteGripPresentation>();var e=rig.GetComponent<AthleteEquipmentBinding>();
                int locked=0;bool was=false;Vector3 anchor=default;
                for(int i=0;i<180;i++)
                {
                    double t=i/120.0;rig.transform.position=new Vector3(0,0,(float)t*2);
                    d.Sample(rig,new LocomotionInput(2,0,.9f),t);rifle.Sample(rig,0);c.SampleMoving(rig,0,d.PoleCycle,d.State,0,t);grip.Render(rig);
                    if(c.LeftPlanted&&was){Assert.That(Vector3.Distance(anchor,c.LeftPoleTip),Is.LessThan(.00001f));locked++;}
                    was=c.LeftPlanted;anchor=c.LeftPoleTip;
                    Assert.That(Vector3.Distance(e.LeftPole.TransformPoint(Vector3.down*1.36f),c.LeftPoleTip),Is.LessThan(.0001f));
                    Assert.That(grip.MaximumResidualMeters,Is.LessThan(.025f));
                }
                Assert.That(locked,Is.GreaterThan(4));
                rig.transform.position+=Vector3.forward*20;d.Sample(rig,new LocomotionInput(2,0,.9f),3);rifle.Sample(rig,0);c.SampleMoving(rig,0,.5,SkiLocomotion.Flat,0,3);
                Assert.That(Vector3.Distance(rig.transform.position,c.LeftPoleTip),Is.LessThan(3));
                c.SampleMoving(rig,0,.5,SkiLocomotion.Idle,1,3.01);Assert.That(c.LeftPlanted||c.RightPlanted,Is.False);
                c.SampleMoving(rig,0,0,SkiLocomotion.Flat,0,0);Assert.That(c.LeftPlanted||c.RightPlanted,Is.False);
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void MovingContactHasNoWarmAllocationsAndRejectsInvalidConfiguration()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var c=rig.GetComponent<AthleteGroundContact>();var d=rig.GetComponent<AthleteLocomotionDriver>();var r=rig.GetComponent<AthleteRiflePresentation>();
                void Sample(int i){d.Sample(rig,new LocomotionInput(4,0,.9f),i/120.0);r.Sample(rig,0);c.SampleMoving(rig,0,d.PoleCycle,d.State,0,i/120.0);}
                for(int i=0;i<30;i++)Sample(i);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=30;i<200;i++)Sample(i);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                var settings=SkiTechniqueVisualSettings.Load();settings.contact_max_grip_correction_m=10;
                Assert.Throws<ArgumentException>(()=>SkiTechniqueVisualSettings.FromJson(JsonUtility.ToJson(settings)));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
