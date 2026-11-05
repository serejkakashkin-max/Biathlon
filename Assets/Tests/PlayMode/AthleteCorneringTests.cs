using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteCorneringTests
    {
        [Test]
        public void VisualRouteUsesRenderedTurnDirectionAndClosedSeam()
        {
            var track = Biathlon.Domain.TrackDefinition.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../tests/fixtures/tracks/synthetic_stadium_v1.json")));
            var visual = Biathlon.Domain.TrackVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/stadium_visual_v1").text, track);
            var loop = visual.Get("main_short");
            Assert.That(VisualRouteCurvature.Sample(loop, 100, 6), Is.Zero);
            Assert.That(VisualRouteCurvature.Sample(loop, 200, 6), Is.LessThan(0), "Visible +X to +Z is a left turn.");
            Assert.That(VisualRouteCurvature.Sample(loop, 0, 6), Is.EqualTo(VisualRouteCurvature.Sample(loop, loop.Get(loop.Count - 1).Distance, 6)).Within(1e-10));
            Assert.Throws<ArgumentException>(() => VisualRouteCurvature.Sample(loop, 100, 0));
        }
        [Test]
        public void SignedCornerLeanIsBoundedAndStopsWithTheAthlete()
        {
            var s = AthleteLocomotionSettings.Load();
            float Lean(float speed, float curvature, bool travelling = true) => AthleteLocomotion.CornerLean(
                new LocomotionInput(speed, 0, .9f, travelling: travelling, curvature: curvature), s);
            Assert.That(Lean(8, .02f), Is.GreaterThan(Lean(4, .02f)));
            Assert.That(Lean(8, -.02f), Is.EqualTo(-Lean(8, .02f)).Within(.0001f));
            Assert.That(Lean(100, 1), Is.EqualTo(s.corner_max_lean_deg));
            Assert.That(Lean(100, -1), Is.EqualTo(-s.corner_max_lean_deg));
            Assert.That(Lean(0, 1), Is.Zero); Assert.That(Lean(8, 0), Is.Zero);
            Assert.That(Lean(8, .02f, false), Is.Zero);
            Assert.Throws<ArgumentException>(() => new LocomotionInput(8, 0, .9f, curvature: float.NaN));
        }

        [Test]
        public void CornerReversalBlendsWhileFeetAndPlacementRemainAligned()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                rig.transform.SetPositionAndRotation(new Vector3(4, 3, -2), Quaternion.Euler(0, 73, 0));
                var driver = rig.GetComponent<AthleteLocomotionDriver>();
                var right = new LocomotionInput(12, -.06f, .7f, curvature: .06f);
                var left = new LocomotionInput(12, -.06f, .7f, curvature: -.06f);
                driver.Sample(rig, right, 0);
                var head = rig.Bone(AthleteBone.Head); var foot = rig.Bone(AthleteBone.LeftFoot);
                var prior = head.position; var placement = rig.transform.position; var rotation = rig.transform.rotation;
                // Owner-reviewed pose now shares the bank across pelvis/spine;
                // the old minimum required the excessive whole28deg hip roll.
                var turn=AthleteCornerPoseSettings.Load();
                Assert.That(Vector3.Dot(rig.Bone(AthleteBone.Hips).up, rig.transform.right),
                    Is.EqualTo(Mathf.Sin(turn.maximum_bank_degrees*turn.hips_bank_fraction*Mathf.Deg2Rad)).Within(.002f));
                driver.Sample(rig, left, 0);
                Assert.That(Vector3.Distance(head.position, prior), Is.LessThan(.0001f));
                for (int i = 1; i <= 120; i++)
                {
                    driver.Sample(rig, left, i / 60.0);
                    Assert.That(Vector3.Distance(head.position, prior), Is.LessThan(.06f));
                    Assert.That(Mathf.Abs(driver.CornerLeanDegrees), Is.LessThanOrEqualTo(28));
                    Assert.That(Vector3.Dot(foot.up, Vector3.up), Is.GreaterThan(.999f));
                    Assert.That(rig.transform.InverseTransformPoint(foot.position).y, Is.GreaterThan(.09f));
                    prior = head.position;
                }
                Assert.That(driver.CornerLeanDegrees, Is.LessThan(-27));
                Assert.That(rig.transform.position, Is.EqualTo(placement)); Assert.That(rig.transform.rotation, Is.EqualTo(rotation));
                for (int i = 121; i <= 240; i++) driver.Sample(rig, new LocomotionInput(0, 0, 0), i / 60.0);
                Assert.That(Mathf.Abs(driver.CornerLeanDegrees), Is.LessThan(.05f));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}
