using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteProneTests
    {
        [Test]
        public void ProneLowersContinuouslyAndReturnsWithoutChangingStandingOrPlacement()
        {
            var rig=OriginalAthleteRigBuilder.Create(null);
            try
            {
                var body=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                rig.transform.SetPositionAndRotation(new Vector3(3,2,1),Quaternion.Euler(0,67,0));
                var head=rig.Bone(AthleteBone.Head);var root=rig.transform.position;
                var standing=head.position;
                Sample(ShootingStance.Standing,1);
                Assert.That(rig.transform.InverseTransformPoint(head.position).y,Is.GreaterThan(1.2f));
                Assert.That(body.ProneWeight,Is.Zero);
                Sample(ShootingStance.Standing,0);
                var prior=head.position;
                for(int i=0;i<=200;i++)
                {
                    Sample(ShootingStance.Prone,i/200f);
                    Assert.That(Vector3.Distance(head.position,prior),Is.LessThan(.04f));prior=head.position;
                    Assert.That(rig.transform.InverseTransformPoint(rig.Bone(AthleteBone.LeftFoot).position).y,Is.GreaterThan(.05f));
                    Assert.That(rig.transform.InverseTransformPoint(rig.Bone(AthleteBone.LeftLowerLeg).position).y,Is.GreaterThan(-.02f));
                    if(i>=150) Assert.That(Vector3.Dot(rig.GetComponent<AthleteEquipmentBinding>().Rifle.forward,rig.transform.forward),Is.GreaterThan(.999f),"Ready rifle must not pitch into the ground with the torso.");
                }
                Assert.That(rig.transform.InverseTransformPoint(head.position).y,Is.LessThan(.5f));
                Assert.That(rig.transform.InverseTransformPoint(head.position).z,Is.GreaterThan(.5f));
                Assert.That(body.ProneWeight,Is.EqualTo(1));
                Assert.That(Vector3.Dot(rig.GetComponent<AthleteEquipmentBinding>().Rifle.forward,rig.transform.forward),Is.GreaterThan(.999f));
                for(int i=199;i>=0;i--)Sample(ShootingStance.Prone,i/200f);
                Assert.That(Vector3.Distance(head.position,standing),Is.LessThan(.0001f));
                Assert.That(rig.transform.position,Is.EqualTo(root));
                void Sample(ShootingStance stance,float progress){rig.ResetPose();body.Sample(rig,stance,progress);rifle.Sample(rig,progress);}
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
