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
        [UnityTest] public IEnumerator AcceptedLateralBypassLiftsAndPlacesSkisInsteadOfSlidingInIdle()
        {
            var rig=OriginalAthleteRigBuilder.Create(null);var driver=rig.GetComponent<AthleteLocomotionDriver>();
            var start=rig.transform.position;var left=rig.Bone(AthleteBone.LeftFoot);var right=rig.Bone(AthleteBone.RightFoot);
            float leftLift=0,rightLift=0,yaw=0;Vector3 first=default;float travel=0;
            for(int i=0;i<=120;i++)
            {
                driver.Sample(rig,new LocomotionInput(0,0,.75f,lateralSpeed:.8f),i/60d);
                Assert.That(driver.State,Is.Not.EqualTo(SkiLocomotion.Idle));
                if(i==0)first=left.position;
                leftLift=Mathf.Max(leftLift,left.position.y);rightLift=Mathf.Max(rightLift,right.position.y);
                travel=Mathf.Max(travel,Vector3.Distance(first,left.position));
                yaw=Mathf.Max(yaw,Mathf.Abs(Vector3.SignedAngle(rig.ModelRoot.forward,left.forward,Vector3.up)));
                Assert.That(rig.transform.position,Is.EqualTo(start),"Presentation cannot create lateral movement.");
            }
            Assert.That(driver.SidestepWeight,Is.GreaterThan(.95f));
            Assert.That(leftLift,Is.GreaterThan(.16f));Assert.That(rightLift,Is.GreaterThan(.16f));
            Assert.That(travel,Is.InRange(.2f,.27f),"The configured .24m is the full stroke, not a twice-as-wide crossed-leg swing.");Assert.That(yaw,Is.GreaterThan(15));
            driver.Sample(rig,new LocomotionInput(0,0,0),4);
            Assert.That(driver.State,Is.EqualTo(SkiLocomotion.Idle));
            Assert.That(driver.SidestepWeight,Is.Zero);
            Object.Destroy(rig.gameObject);yield return null;
        }
        [Test] public void SidestepKeepsBootOrderForBothDirections()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var driver=rig.GetComponent<AthleteLocomotionDriver>();
                foreach(float direction in new[]{-.8f,.8f})
                for(int i=0;i<240;i++)
                {
                    driver.Sample(rig,new LocomotionInput(0,0,.75f,lateralSpeed:direction),i/60d);
                    var left=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.LeftFoot).position);
                    var right=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.RightFoot).position);
                    Assert.That(right.x-left.x,Is.GreaterThan(.15f),"Side stepping must not cross the boots or reverse their lateral order.");
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void LateralStepDataRejectsPartialOrUnboundedMotionAndPreservesAbsentLegacySettings()
        {
            var json=Resources.Load<TextAsset>("Biathlon/athlete_locomotion_v1").text;
            foreach(string name in new[]{"sidestep_span_m","sidestep_lift_m","sidestep_max_cadence_hz"})
            {
                string missing=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\""+name+"\"\\s*:\\s*[0-9.]+","");
                Assert.Throws<ArgumentException>(()=>AthleteLocomotionSettings.FromJson(missing),name);
            }
            var absent=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"sidestep_[a-z_]+\"\\s*:\\s*[0-9.]+","");
            Assert.That(AthleteLocomotionSettings.FromJson(absent).sidestep_span_m,Is.Zero);
            Assert.Throws<ArgumentException>(()=>new LocomotionInput(1,0,1,lateralSpeed:float.PositiveInfinity));
            Assert.Throws<ArgumentException>(()=>AthleteLocomotionSettings.FromJson(json.Replace("\"sidestep_span_m\": 0.24","\"sidestep_span_m\": 1")));
        }
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
