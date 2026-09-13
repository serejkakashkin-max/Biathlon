using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteStandingTests
    {
        [Test]
        public void StandingTurnsTheBodyAndPlantsFeetWhileKeepingHeadRifleAndRootStable()
        {
            var rig=OriginalAthleteRigBuilder.Create(null);
            try
            {
                var pose=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var props=rig.GetComponent<AthleteEquipmentBinding>();var settings=RiflePresentationSettings.Load();
                rig.transform.SetPositionAndRotation(new Vector3(-2,1,3),Quaternion.Euler(0,-38,0));
                var initial=rig.Bone(AthleteBone.Head).position;var prior=initial;var root=rig.transform.position;
                for(int i=0;i<=200;i++)
                {
                    Sample(i/200f);
                    Assert.That(Vector3.Distance(prior,rig.Bone(AthleteBone.Head).position),Is.LessThan(.02f));prior=rig.Bone(AthleteBone.Head).position;
                    Assert.That(rig.transform.InverseTransformPoint(rig.Bone(AthleteBone.LeftFoot).position).y,Is.GreaterThan(.08f));
                }
                Assert.That(Vector3.Dot(rig.Bone(AthleteBone.Head).forward,rig.transform.forward),Is.GreaterThan(.999f));
                Assert.That(Vector3.Dot(props.Rifle.forward,rig.transform.forward),Is.GreaterThan(.999f));
                Assert.That(Vector3.Distance(props.Rifle.position,rig.Bone(AthleteBone.RightUpperArm).position),Is.LessThan(.15f));
                Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(settings.support_grip)),Is.LessThan(.015f));
                Assert.That(Vector3.Distance(rig.Bone(AthleteBone.RightHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(settings.trigger_grip)),Is.LessThan(.015f));
                for(int i=199;i>=0;i--)Sample(i/200f);
                Assert.That(Vector3.Distance(initial,rig.Bone(AthleteBone.Head).position),Is.LessThan(.0001f));Assert.That(rig.transform.position,Is.EqualTo(root));
                void Sample(float p){rig.ResetPose();pose.Sample(rig,ShootingStance.Standing,p);rifle.Sample(rig,p);}
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
