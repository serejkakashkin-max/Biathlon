using System;
using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteLocomotionTests
    {
        [Test]
        public void StatesUseAcceptedTuckGradeAndEffortWithExplicitPriority()
        {
            var settings = AthleteLocomotionSettings.Load();
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(0, 0, 0), settings), Is.EqualTo(SkiLocomotion.Idle));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(8, 0, .9f), settings), Is.EqualTo(SkiLocomotion.Flat));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(4, .1f, 1.1f), settings), Is.EqualTo(SkiLocomotion.Climb));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(8, 0, 1.1f), settings), Is.EqualTo(SkiLocomotion.Acceleration));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(8, 0, 1.2f), settings), Is.EqualTo(SkiLocomotion.Sprint));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(8, 0, .9f, sprint: true), settings), Is.EqualTo(SkiLocomotion.Sprint));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(12, -.1f, .7f), settings), Is.EqualTo(SkiLocomotion.TechnicalDescent));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(12, -.1f, .7f, tuck: true), settings), Is.EqualTo(SkiLocomotion.Tuck));
            Assert.That(AthleteLocomotion.Select(new LocomotionInput(8, 0, 1.2f, travelling: false), settings), Is.EqualTo(SkiLocomotion.Idle));
            Assert.Throws<ArgumentException>(() => new LocomotionInput(float.NaN, 0, 0));
            var json = Resources.Load<TextAsset>("Biathlon/athlete_locomotion_v1").text;
            Assert.Throws<ArgumentException>(() => AthleteLocomotionSettings.FromJson(json.Replace("\"transition_s\": 0.35", "\"transition_s\": 0")));
        }

        [UnityTest]
        public IEnumerator MotionBlendsWithoutSnapsOrRootDriftAndPreservesLimbLengths()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            var driver = rig.GetComponent<AthleteLocomotionDriver>();
            Assert.That(driver, Is.Not.Null);
            var rootPosition = rig.transform.position;
            var flat = new LocomotionInput(8, 0, .9f);
            var sprint = new LocomotionInput(9, 0, 1.2f);
            driver.Sample(rig, flat, 0);
            var head = rig.Bone(AthleteBone.Head).position;
            driver.Sample(rig, sprint, 0);
            Assert.That(Vector3.Distance(head, rig.Bone(AthleteBone.Head).position), Is.LessThan(.0001f), "State change starts at current pose.");
            var thigh = rig.Bone(AthleteBone.LeftUpperLeg); var knee = rig.Bone(AthleteBone.LeftLowerLeg); var ankle = rig.Bone(AthleteBone.LeftFoot);
            float upperLength = Vector3.Distance(thigh.position, knee.position), lowerLength = Vector3.Distance(knee.position, ankle.position);
            var startHand = rig.Bone(AthleteBone.LeftHand).position;
            float handTravel = 0;
            for (int i = 1; i <= 90; i++)
            {
                driver.Sample(rig, sprint, i / 60.0);
                Assert.That(Vector3.Distance(thigh.position, knee.position), Is.EqualTo(upperLength).Within(.0001f));
                Assert.That(Vector3.Distance(knee.position, ankle.position), Is.EqualTo(lowerLength).Within(.0001f));
                Assert.That(ankle.position.y, Is.GreaterThan(.09f));
                Assert.That(Vector3.Dot(ankle.up, Vector3.up), Is.GreaterThan(.999f));
                Assert.That(Vector3.Distance(head, rig.Bone(AthleteBone.Head).position), Is.LessThan(.08f));
                head = rig.Bone(AthleteBone.Head).position;
                handTravel = Mathf.Max(handTravel, Vector3.Distance(startHand, rig.Bone(AthleteBone.LeftHand).position));
            }
            Assert.That(handTravel, Is.GreaterThan(.2f));
            Assert.That(driver.BlendProgress, Is.EqualTo(1));
            Assert.That(rig.transform.position, Is.EqualTo(rootPosition));
            driver.Sample(rig, new LocomotionInput(12, -.1f, .7f, tuck: true), 10);
            var tucked = rig.Bone(AthleteBone.Head).position;
            driver.Sample(rig, new LocomotionInput(12, -.1f, .7f, tuck: true), 11);
            Assert.That(Vector3.Distance(tucked, rig.Bone(AthleteBone.Head).position), Is.LessThan(.0001f));
            Object.Destroy(rig.gameObject); yield return null;
        }
    }
}
