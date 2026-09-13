using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteGroundContactTests
    {
        [Test]
        public void RenderedGradeUsesGeometryAndRejectsInvalidSampling()
        {
            var track = TrackDefinition.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../tests/fixtures/tracks/synthetic_stadium_v1.json")));
            var visual = TrackVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/stadium_visual_v1").text, track);
            for (int l = 0; l < visual.Count; l++)
            {
                var loop = visual.Get(l);
                for (int i = 1; i < loop.Count; i++)
                {
                    var a = loop.Get(i - 1); var b = loop.Get(i);
                    double x = b.X - a.X, z = b.Z - a.Z;
                    Assert.That(VisualRouteSurface.Grade(loop, (a.Distance + b.Distance) * .5, .01),
                        Is.EqualTo((b.Y - a.Y) / Math.Sqrt(x * x + z * z)).Within(1e-7));
                }
            }
            Assert.Throws<ArgumentException>(() => VisualRouteSurface.Grade(visual.Get(0), 0, double.NaN));
        }

        [Test]
        public void SkiNormalsAndPoleTipsFollowFlatUphillAndDownhillWithoutMovingRoot()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>();
                var rifle = rig.GetComponent<AthleteRiflePresentation>();
                var contact = rig.GetComponent<AthleteGroundContact>();
                var equipment = rig.GetComponent<AthleteEquipmentBinding>();
                var settings = GroundContactSettings.Load();
                rig.transform.SetPositionAndRotation(new Vector3(7, 3, -8), Quaternion.Euler(0, 63, 0));
                var position = rig.transform.position; var rotation = rig.transform.rotation;
                foreach (float grade in new[] { 0, .12f, -.12f })
                {
                    for (int i = 0; i <= 120; i++)
                    {
                        driver.Sample(rig, new LocomotionInput(8, grade, .9f), i / 60.0);
                        rifle.Sample(rig, 0);
                        contact.Sample(rig, grade, i / 120.0, SkiLocomotion.Flat, 0);
                        Assert.That(contact.MaxFootResidualMeters, Is.LessThan(.025f));
                        foreach (var foot in new[] { AthleteBone.LeftFoot, AthleteBone.RightFoot })
                            Assert.That(Vector3.Angle(rig.Bone(foot).up, contact.SurfaceNormal), Is.LessThan(.04f));
                        CheckPole(equipment.LeftPole, contact.LeftPoleTip, Mathf.Max(0, Mathf.Cos(i / 120f * 2 * Mathf.PI)));
                        CheckPole(equipment.RightPole, contact.RightPoleTip, Mathf.Max(0, Mathf.Cos((i / 120f + .5f) * 2 * Mathf.PI)));
                    }
                    void CheckPole(Transform pole, Vector3 tip, float wave)
                    {
                        Assert.That(Vector3.Distance(pole.position, tip), Is.EqualTo(settings.pole_length_m).Within(.00001f));
                        Assert.That(Vector3.Distance(pole.TransformPoint(Vector3.down * settings.pole_length_m), tip), Is.LessThan(.00001f));
                        float clearance = Vector3.Dot(tip - rig.transform.position, contact.SurfaceNormal);
                        Assert.That(clearance, Is.EqualTo(settings.pole_recovery_m * wave * wave).Within(.001f));
                    }
                }
                Assert.That(rig.transform.position, Is.EqualTo(position)); Assert.That(rig.transform.rotation, Is.EqualTo(rotation));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void StagedPolesMeetSurfaceAndShootingAlignmentSurvivesContact()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var pose = rig.GetComponent<AthleteShootingPose>(); var rifle = rig.GetComponent<AthleteRiflePresentation>();
                var contact = rig.GetComponent<AthleteGroundContact>(); var equipment = rig.GetComponent<AthleteEquipmentBinding>();
                foreach (var stance in new[] { ShootingStance.Prone, ShootingStance.Standing })
                {
                    rig.ResetPose(); pose.Sample(rig, stance, 1); rifle.Sample(rig, 1, stance: stance);
                    var aim = equipment.Rifle.rotation; var hand = rig.Bone(AthleteBone.RightHand).position;
                    contact.Sample(rig, 0, 0, SkiLocomotion.Idle, 1);
                    Assert.That(contact.MaxFootResidualMeters, Is.LessThan(.002f));
                    Assert.That(contact.LeftPoleTip.y, Is.EqualTo(0).Within(.0001f));
                    Assert.That(contact.RightPoleTip.y, Is.EqualTo(0).Within(.0001f));
                    Assert.That(equipment.Rifle.rotation, Is.EqualTo(aim));
                    Assert.That(rig.Bone(AthleteBone.RightHand).position, Is.EqualTo(hand));
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}
