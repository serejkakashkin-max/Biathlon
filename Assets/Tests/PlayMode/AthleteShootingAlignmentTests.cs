using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteShootingAlignmentTests
    {
        [Test]
        public void BothStancesFollowSightAtOneToOneScaleAndKeepHandsOnRifle()
        {
            var rig=OriginalAthleteRigBuilder.Create(null);
            try
            {
                var body=rig.GetComponent<AthleteShootingPose>();var view=rig.GetComponent<AthleteRiflePresentation>();
                var props=rig.GetComponent<AthleteEquipmentBinding>();var settings=RiflePresentationSettings.Load();
                rig.transform.SetPositionAndRotation(new Vector3(1,2,3),Quaternion.Euler(0,53,0));var root=rig.transform.position;
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                foreach(float sign in new[]{-1,0,1})
                {
                    var sight=new WeaponAimState(sign*.08,sign*.05);
                    rig.ResetPose();body.Sample(rig,stance,1);var butt=rig.Socket(AthleteSocket.RifleAim).position;
                    view.Sample(rig,1,sight,stance);
                    var ray=sight.Ray;var expected=rig.transform.TransformDirection(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ));
                    Assert.That(Vector3.Angle(props.Rifle.forward,expected),Is.LessThan(.03f));
                    Assert.That(Vector3.Angle(rig.Bone(AthleteBone.Head).forward,expected),Is.LessThan(.03f));
                    Assert.That(Vector3.Distance(props.Rifle.position,butt),Is.LessThan(.0001f));
                    var support=stance==ShootingStance.Prone?settings.prone_support_grip:settings.support_grip;
                    Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(support)),Is.LessThan(.02f),stance+" support "+sign);
                    Assert.That(Vector3.Distance(rig.Bone(AthleteBone.RightHand).position-rig.transform.up*.056f,props.Rifle.TransformPoint(settings.trigger_grip)),Is.LessThan(.02f));
                    Assert.That(rig.transform.position,Is.EqualTo(root));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
