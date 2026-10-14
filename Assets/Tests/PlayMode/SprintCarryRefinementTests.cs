using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.PlayModeTests
{
    public class SprintCarryRefinementTests
    {
        [TestCase(9f)]
        [TestCase(12f)]
        [TestCase(15f)]
        public void SprintPoleCyclesStayBelowTheHammeringFrequency(float speed)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                var input=new LocomotionInput(speed,0,1.25f,sprint:true);
                d.Sample(rig,input,0);double prior=d.PoleCycle,cycles=0;
                for(int frame=1;frame<=1200;frame++)
                {
                    d.Sample(rig,input,frame/120.0);
                    cycles+=Mathf.Repeat((float)(d.PoleCycle-prior),1);prior=d.PoleCycle;
                }
                Assert.That(cycles/10,Is.LessThanOrEqualTo(1.8),"Smooth hand paths alone leave too many full pole pushes per second during attack.");
                Assert.That(cycles/10,Is.GreaterThan(1.2),"Attack must retain an active skiing rhythm.");
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [TestCase(false)]
        [TestCase(true)]
        public void CarriedRifleBroadSideFollowsTheBackWithTheMuzzleUpward(bool hero)
        {
            var rig=hero?AthleteRigFactory.Create(null):OriginalAthleteRigBuilder.Create(null);
            try
            {
                rig.transform.rotation=Quaternion.Euler(0,47,0);
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                var rifle=rig.GetComponent<AthleteRiflePresentation>();var weapon=rig.GetComponent<AthleteEquipmentBinding>().Rifle;
                foreach(var input in new[]{new LocomotionInput(6,0,.95f),new LocomotionInput(4,.08f,.95f),new LocomotionInput(9,0,1.25f,sprint:true)})
                {
                    if(d!=null)d.Sample(rig,input,0);else rig.GetComponent<AthleteLocomotionDriver>().Sample(rig,input,0);
                    rifle.Sample(rig,0);var chest=rig.Bone(AthleteBone.Chest);
                    // Rifle mesh is thin on local X, broad in Y/Z; +Z points to muzzle.
                    Assert.That(Mathf.Abs(Vector3.Dot(weapon.right,chest.forward)),Is.GreaterThan(.95f),"The broad stock face must lie parallel to the back, not stand on its edge.");
                    Assert.That(Vector3.Dot(weapon.forward,chest.up),Is.GreaterThan(.95f),"Carry roll must preserve the muzzle-up direction.");
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void AttackDoesNotMakeTheVisiblePoleCycleSlowerThanFastSkating()
        {
            var fast=AthleteRigFactory.Create(null);var attack=AthleteRigFactory.Create(null);
            try
            {
                var a=fast.GetComponent<AthleteHybridLocomotionDriver>();var b=attack.GetComponent<AthleteHybridLocomotionDriver>();
                var ordinary=new LocomotionInput(8,0,.95f);var sprint=new LocomotionInput(9,0,1.25f,sprint:true);
                a.Sample(fast,ordinary,0);b.Sample(attack,sprint,0);double pa=a.PoleCycle,pb=b.PoleCycle,ca=0,cb=0;
                for(int frame=1;frame<=1200;frame++)
                {
                    a.Sample(fast,ordinary,frame/120.0);b.Sample(attack,sprint,frame/120.0);
                    ca+=Mathf.Repeat((float)(a.PoleCycle-pa),1);cb+=Mathf.Repeat((float)(b.PoleCycle-pb),1);pa=a.PoleCycle;pb=b.PoleCycle;
                }
                Assert.That(cb,Is.GreaterThanOrEqualTo(ca-.0001),"Reducing attack frequency must not make ordinary fast skating look busier than attack.");
            }
            finally{Object.DestroyImmediate(fast.gameObject);Object.DestroyImmediate(attack.gameObject);}
        }
        [Test] public void SwitchingAttackAtTheSameObservationTimeDoesNotJumpTheHands()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                var flat=new LocomotionInput(6,0,.95f);var sprint=new LocomotionInput(9,0,1.25f,sprint:true);
                for(int frame=0;frame<=240;frame++)d.Sample(rig,flat,frame/120.0);
                var hand=rig.Bone(AthleteBone.LeftHand).position;d.Sample(rig,sprint,2);
                Assert.That(Vector3.Distance(hand,rig.Bone(AthleteBone.LeftHand).position),Is.LessThan(.0001f));
                for(int frame=241;frame<=480;frame++)d.Sample(rig,sprint,frame/120.0);
                hand=rig.Bone(AthleteBone.LeftHand).position;d.Sample(rig,flat,4);
                Assert.That(Vector3.Distance(hand,rig.Bone(AthleteBone.LeftHand).position),Is.LessThan(.0001f));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
