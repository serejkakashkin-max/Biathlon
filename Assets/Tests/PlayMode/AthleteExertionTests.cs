using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteExertionTests
    {
        [Test]
        public void ExertionIsBoundedMonotonicAndRecoveryHasNoImmediatePoseSnap()
        {
            var s = ExertionPresentationSettings.Load();
            Assert.That(s.Intensity(new ExertionInput(0, 1, 60)), Is.Zero);
            Assert.That(s.Intensity(new ExertionInput(1, 0, 220)), Is.EqualTo(1));
            float previous = 0;
            for (int i = 0; i <= 100; i++)
            {
                float value = s.Intensity(new ExertionInput(i / 100f, 1 - i / 100f, 60 + i * 1.5f));
                Assert.That(value, Is.InRange(previous, 1)); previous = value;
            }
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>(); var input = new LocomotionInput(8, 0, .9f);
                driver.Sample(rig, input, 0, new ExertionInput(1, 0, 190));
                var head = rig.Bone(AthleteBone.Head).position;
                driver.Sample(rig, input, 0, new ExertionInput(0, 1, 60));
                Assert.That(rig.Bone(AthleteBone.Head).position, Is.EqualTo(head));
                for (int i = 1; i <= 300; i++)
                {
                    driver.Sample(rig, input, i / 60.0, new ExertionInput(0, 1, 60));
                    Assert.That(Vector3.Distance(head, rig.Bone(AthleteBone.Head).position), Is.LessThan(.03f));
                    head = rig.Bone(AthleteBone.Head).position;
                }
                Assert.That(driver.ExertionIntensity, Is.LessThan(.003f));
                Assert.That(rig.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void TiredAndFinishPosesAreReadableWhileShootingSightRemainsUnmodified()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            var freshRig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>(); var rifle = rig.GetComponent<AthleteRiflePresentation>();
                var pose = rig.GetComponent<AthleteShootingPose>(); var contact = rig.GetComponent<AthleteGroundContact>();
                var motion = new LocomotionInput(8, 0, .9f);
                var freshDriver = freshRig.GetComponent<AthleteLocomotionDriver>();
                float fresh = 0;
                // Compare matched phases throughout multiple cycles, not two unrelated
                // timestamps whose poling compression can hide the exertion difference.
                for (int i = 0; i <= 240; i++)
                {
                    freshDriver.Sample(freshRig, motion, i / 60.0, new ExertionInput(0, 1, 60));
                    driver.Sample(rig, motion, i / 60.0, new ExertionInput(1, 0, 190));
                    fresh = freshRig.Bone(AthleteBone.Head).position.y;
                    Assert.That(rig.Bone(AthleteBone.Head).position.y, Is.LessThan(fresh - .06f));
                }
                driver.Sample(rig, new LocomotionInput(0, 0, 0, travelling: false), 2, new ExertionInput(1, 0, 190, finished: true));
                rifle.Sample(rig, 0); contact.Sample(rig, 0, driver.Cycle, SkiLocomotion.Idle, 0);
                Assert.That(driver.FinishWeight, Is.EqualTo(1));
                Assert.That(rig.Bone(AthleteBone.Head).position.y, Is.LessThan(fresh - .2f), "Recovery posture must visibly lower the head relative to fresh skiing.");
                Assert.That(contact.MaxFootResidualMeters, Is.LessThan(.001f));
                foreach (var stance in new[] { ShootingStance.Prone, ShootingStance.Standing })
                {
                    driver.Sample(rig, new LocomotionInput(0, 0, 0, travelling: false), 0, new ExertionInput(1, 0, 190, shooting: true));
                    pose.Sample(rig, stance, 1); rifle.Sample(rig, 1, stance: stance);
                    var weapon = rig.GetComponent<AthleteEquipmentBinding>().Rifle;
                    Assert.That(Vector3.Angle(weapon.forward, rig.transform.forward), Is.LessThan(.03f));
                    Assert.That(Vector3.Distance(rig.Bone(AthleteBone.RightHand).position - rig.transform.up * .056f,
                        weapon.TransformPoint(RiflePresentationSettings.Load().trigger_grip)), Is.LessThan(.02f));
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); Object.DestroyImmediate(freshRig.gameObject); }
        }
    }
}
