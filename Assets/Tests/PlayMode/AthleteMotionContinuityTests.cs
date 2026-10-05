using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteMotionContinuityTests
    {
        // Native Hero rig measurements, not a reimplementation of its pose curve. The owner
        // requested minimal sideways body motion on 2026-10-05: continuity alone is insufficient
        // when a smooth sprint still moves the pelvis almost 38 cm from side to side.
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(240)]
        public void SupportTransferRemainsMinimalWithBoundedVelocityAccelerationAndJerk(int frequency)
        {
            var rig = AthleteRigFactory.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>();
                var inputs = new[] { new LocomotionInput(8, 0, .9f), new LocomotionInput(4, .08f, .9f),
                    new LocomotionInput(8, 0, 1.1f), new LocomotionInput(9, 0, 1.2f),
                    new LocomotionInput(12, -.1f, .7f), new LocomotionInput(12, -.1f, .7f, tuck: true) };
                foreach (var input in inputs)
                {
                    double prior = 0, velocity = 0, acceleration = 0, peakVelocity = 0, peakAcceleration = 0, peakJerk = 0;
                    float minimum = 10, maximum = -10, minimumChest = 10, maximumChest = -10;
                    for (int i = 0; i <= frequency * 8; i++)
                    {
                        driver.Sample(rig, input, (double)i / frequency);
                        float position = rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Hips).position).x;
                        float chestPosition = rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Chest).position).x;
                        double nextVelocity = (position - prior) * frequency;
                        double nextAcceleration = (nextVelocity - velocity) * frequency;
                        double jerk = (nextAcceleration - acceleration) * frequency;
                        if (i > 3)
                        {
                            peakVelocity = Math.Max(peakVelocity, Math.Abs(nextVelocity));
                            peakAcceleration = Math.Max(peakAcceleration, Math.Abs(nextAcceleration));
                            peakJerk = Math.Max(peakJerk, Math.Abs(jerk));
                            minimum = Mathf.Min(minimum, position); maximum = Mathf.Max(maximum, position);
                            minimumChest = Mathf.Min(minimumChest, chestPosition); maximumChest = Mathf.Max(maximumChest, chestPosition);
                        }
                        prior = position; velocity = nextVelocity; acceleration = nextAcceleration;
                    }
                    string condition = driver.State + " at " + frequency + " Hz";
                    Assert.That(maximum - minimum, Is.LessThan(.031f), condition + " minimal pelvis displacement, at most 3.1 cm peak-to-peak");
                    Assert.That(maximumChest - minimumChest, Is.LessThan(.031f), condition + " torso must not amplify the small pelvis motion");
                    Assert.That(peakVelocity, Is.LessThan(.16), condition + " pelvis lateral speed");
                    Assert.That(peakAcceleration, Is.LessThan(1.5), condition + " pelvis acceleration");
                    Assert.That(peakJerk, Is.LessThan(20), condition + " pelvis jerk / support-boundary continuity");
                    if (driver.State == SkiLocomotion.Tuck)
                        Assert.That(maximum - minimum, Is.LessThan(.00001f), condition + " tuck must remain still");
                    Assert.That(rig.transform.position, Is.EqualTo(Vector3.zero));
                    Assert.That(rig.transform.rotation, Is.EqualTo(Quaternion.identity));
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TiredTuckAndInterruptedTransitionsHaveNoZeroTimePoseJump(bool enterTuck)
        {
            var rig = AthleteRigFactory.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>();
                var tired = new ExertionInput(1, 0, 190);
                var current = new LocomotionInput(12, -.1f, .7f, tuck: !enterTuck);
                var next = new LocomotionInput(12, -.1f, .7f, tuck: enterTuck);
                for (int i = 0; i <= 180; i++) driver.Sample(rig, current, i / 60.0, tired);
                SwitchWithoutElapsedTime(next, 3);
                for (int i = 1; i <= 8; i++) driver.Sample(rig, next, 3 + i / 60.0, tired);
                SwitchWithoutElapsedTime(current, 3 + 8 / 60.0);
                SwitchWithoutElapsedTime(next, 3 + 8 / 60.0);
                for (int i = 9; i <= 120; i++) driver.Sample(rig, next, 3 + i / 60.0, tired);
                Assert.That(driver.BlendProgress, Is.EqualTo(1));
                Assert.That(rig.transform.position, Is.EqualTo(Vector3.zero));

                void SwitchWithoutElapsedTime(LocomotionInput target, double time)
                {
                    var hip = rig.Bone(AthleteBone.Hips).position;
                    var chest = rig.Bone(AthleteBone.Chest).rotation;
                    var head = rig.Bone(AthleteBone.Head).position;
                    driver.Sample(rig, target, time, tired);
                    Assert.That(Vector3.Distance(hip, rig.Bone(AthleteBone.Hips).position), Is.LessThan(.00001f), "Tuck must blend fatigue hip drop.");
                    Assert.That(Quaternion.Angle(chest, rig.Bone(AthleteBone.Chest).rotation), Is.LessThan(.03f), "Tuck must blend fatigue torso lean.");
                    Assert.That(Vector3.Distance(head, rig.Bone(AthleteBone.Head).position), Is.LessThan(.0001f), "Interrupted transitions start from the visible pose.");
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void RewindsAndLongSamplingGapsResetBothPoseAndTuckBlend()
        {
            var rig = AthleteRigFactory.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>();
                var tired = new ExertionInput(1, 0, 190);
                var tucked = new LocomotionInput(12, -.1f, .7f, tuck: true);
                var descent = new LocomotionInput(12, -.1f, .7f);
                driver.Sample(rig, tucked, 3, tired);
                driver.Sample(rig, descent, 3.1, tired);
                foreach (double time in new[] { 2.0, 12.0 })
                {
                    var fresh = AthleteRigFactory.Create(null);
                    try
                    {
                        var input = time < 3 ? descent : tucked;
                        fresh.GetComponent<AthleteLocomotionDriver>().Sample(fresh, input, time, tired);
                        driver.Sample(rig, input, time, tired);
                        for (int i = 0; i < 3; i++)
                        {
                            foreach (var bone in new[] { AthleteBone.Hips, AthleteBone.Head, AthleteBone.LeftHand })
                                Assert.That(Vector3.Distance(rig.Bone(bone).position, fresh.Bone(bone).position), Is.LessThan(.0001f), "Reset and repeated same-time sampling must not inherit a stale tuck blend.");
                            driver.Sample(rig, input, time, tired);
                        }
                    }
                    finally { Object.DestroyImmediate(fresh.gameObject); }
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}
