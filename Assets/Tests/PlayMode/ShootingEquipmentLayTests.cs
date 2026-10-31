using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class ShootingEquipmentLayTests
    {
        [TestCase(ShootingStance.Prone)][TestCase(ShootingStance.Standing)]
        public void LayAndPickupAreContinuousWithinTheExistingProgressClock(ShootingStance stance)
        {
            var rig=OriginalAthleteRigBuilder.Create(null);
            try
            {
                var body=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();var contact=rig.GetComponent<AthleteGroundContact>();var equipment=rig.GetComponent<AthleteEquipmentBinding>();
                var samples=new Vector3[201];
                for(int i=0;i<=200;i++)
                {
                    Sample(i/200f);samples[i]=equipment.LeftPole.position;
                    if(i>0)Assert.That(Vector3.Distance(samples[i],samples[i-1]),Is.LessThan(.07f));
                    Assert.That(contact.LeftPoleTip.y,Is.GreaterThanOrEqualTo(-.0001f));
                }
                for(int i=200;i>=0;i--){Sample(i/200f);Assert.That(Vector3.Distance(equipment.LeftPole.position,samples[i]),Is.LessThan(.0001f));}
                void Sample(float p){rig.ResetPose();body.Sample(rig,stance,p);rifle.Sample(rig,p,stance:stance);contact.Sample(rig,0,0,SkiLocomotion.Idle,rifle.HandWeight);}
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [TestCase(ShootingStance.Prone,0f)]
        [TestCase(ShootingStance.Standing,0f)]
        [TestCase(ShootingStance.Prone,.12f)]
        [TestCase(ShootingStance.Standing,-.12f)]
        public void ReadyPolesLieOnTheSurfaceAndSkisStayGrounded(ShootingStance stance,float grade)
        {
            var rig=OriginalAthleteRigBuilder.Create(null);
            try
            {
                rig.transform.SetPositionAndRotation(new Vector3(3,2,1),Quaternion.Euler(0,57,0));
                var body=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var contact=rig.GetComponent<AthleteGroundContact>();var equipment=rig.GetComponent<AthleteEquipmentBinding>();
                var settings=GroundContactSettings.Load();var rootPosition=rig.transform.position;
                rig.ResetPose();body.Sample(rig,stance,1);rifle.Sample(rig,1,stance:stance);
                contact.Sample(rig,grade,0,SkiLocomotion.Idle,rifle.HandWeight);
                foreach(var pole in new[]{equipment.LeftPole,equipment.RightPole})
                {
                    var tip=pole.TransformPoint(Vector3.down*settings.pole_length_m);
                    Assert.That(Mathf.Abs(Vector3.Dot((tip-pole.position).normalized,contact.SurfaceNormal)),Is.LessThan(.001f),"Stowed shaft must lie parallel to snow/mat, not stand vertically.");
                    float gripHeight=Vector3.Dot(pole.position-rootPosition,contact.SurfaceNormal),tipHeight=Vector3.Dot(tip-rootPosition,contact.SurfaceNormal);
                    Assert.That(gripHeight,Is.InRange(.015f,.065f));Assert.That(tipHeight,Is.InRange(.015f,.065f));
                    float x=rig.transform.InverseTransformPoint(pole.position).x;
                    if(stance==ShootingStance.Prone)Assert.That(x,Is.GreaterThan(.4f),"Prone poles belong alongside the athlete.");
                    else Assert.That(Mathf.Abs(x),Is.LessThan(.12f),"Standing poles rest between the skis.");
                }
                foreach(var foot in new[]{AthleteBone.LeftFoot,AthleteBone.RightFoot})
                    Assert.That(Vector3.Angle(rig.Bone(foot).up,contact.SurfaceNormal),Is.LessThan(.05f));
                foreach(var socket in new[]{AthleteSocket.LeftSkiBinding,AthleteSocket.RightSkiBinding})
                    Assert.That(Mathf.Abs(Vector3.Dot(rig.Socket(socket).forward,contact.SurfaceNormal)),Is.LessThan(.001f),"Skis keep their length on the snow/mat plane during shooting.");
                Assert.That(rig.transform.position,Is.EqualTo(rootPosition));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
