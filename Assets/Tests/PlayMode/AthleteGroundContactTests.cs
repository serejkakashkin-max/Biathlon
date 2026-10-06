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
        [Test] public void TuckPoleShaftsClearTheActualHeroSkinAfterTheHandGrip()
        {
            var rig=AthleteRigFactory.Create(null);var baked=new Mesh();
            try
            {
                var driver=rig.GetComponent<AthleteLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var contact=rig.GetComponent<AthleteGroundContact>();var grip=rig.GetComponent<AthleteGripPresentation>();
                var equipment=rig.GetComponent<AthleteEquipmentBinding>();
                foreach(float grade in new[]{0f,-.08f,-.15f})
                {
                    driver.Sample(rig,new LocomotionInput(12,grade,.1f,tuck:true),10);
                    rifle.Sample(rig,0);contact.Sample(rig,grade,0,SkiLocomotion.Tuck,0);grip.Render(rig);
                    foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        renderer.BakeMesh(baked);var vertices=baked.vertices;var triangles=baked.triangles;
                        for(int i=0;i<vertices.Length;i++)vertices[i]=renderer.transform.TransformPoint(vertices[i]);
                        foreach(var pole in new[]{equipment.LeftPole,equipment.RightPole})
                        {
                            var origin=pole.position;var direction=-pole.up;
                            for(int i=0;i<triangles.Length;i+=3)
                            {
                                var a=vertices[triangles[i]];var ab=vertices[triangles[i+1]]-a;var ac=vertices[triangles[i+2]]-a;
                                var p=Vector3.Cross(direction,ac);float determinant=Vector3.Dot(ab,p);if(Mathf.Abs(determinant)<1e-8f)continue;
                                var from=origin-a;float u=Vector3.Dot(from,p)/determinant;if(u<0||u>1)continue;
                                var q=Vector3.Cross(from,ab);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)continue;
                                float distance=Vector3.Dot(ac,q)/determinant;
                                Assert.That(distance>.1f&&distance<1.36f,Is.False,$"Pole intersects hero skin {renderer.name} at {distance:F3}m, grade={grade}.");
                            }
                        }
                    }
                }
            }
            finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        [Test]
        public void HeroTuckUsesHipHingeCompactHandsAndTrailingAirbornePoles()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var driver=rig.GetComponent<AthleteLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var contact=rig.GetComponent<AthleteGroundContact>();var grip=rig.GetComponent<AthleteGripPresentation>();
                Assert.That(grip,Is.Not.Null,"Exercise the shipped hero including its late glove fit.");
                var equipment=rig.GetComponent<AthleteEquipmentBinding>();var origin=rig.transform.position;var rotation=rig.transform.rotation;
                foreach(float grade in new[]{0f,-.08f,-.15f})
                {
                    driver.Sample(rig,new LocomotionInput(12,grade,.1f,tuck:true),10);
                    rifle.Sample(rig,0);contact.Sample(rig,grade,0,SkiLocomotion.Tuck,0);grip.Render(rig);
                    Vector3 Local(AthleteBone bone)=>rig.ModelRoot.InverseTransformPoint(rig.Bone(bone).position);
                    var hip=Local(AthleteBone.Hips);var foot=Local(AthleteBone.LeftFoot);var shoulder=Local(AthleteBone.LeftUpperArm);
                    Assert.That(hip.z,Is.LessThan(foot.z-.09f),"The crouch hinges at hips behind the boots.");
                    Assert.That(hip.y,Is.InRange(.68f,.9f));
                    Assert.That(shoulder.y-hip.y,Is.LessThan(.25f),"Torso is low and forward, not an upright curved back.");
                    Assert.That(Vector3.Distance(Local(AthleteBone.LeftHand),Local(AthleteBone.RightHand)),Is.LessThan(.15f));
                    var elbow=Local(AthleteBone.LeftLowerArm);var hand=Local(AthleteBone.LeftHand);
                    Assert.That(Vector3.Angle(elbow-shoulder,hand-elbow),Is.GreaterThan(50),"Elbows bend instead of reaching straight forward.");
                    foreach(var pole in new[]{equipment.LeftPole,equipment.RightPole})
                        Assert.That(Vector3.Dot(-pole.up,-rig.ModelRoot.forward),Is.GreaterThan(.85f),"Poles trail in the air, splayed around the torso.");
                    Assert.That(Vector3.Dot(contact.LeftPoleTip-rig.ModelRoot.position,contact.SurfaceNormal),Is.GreaterThan(.6f));
                    Assert.That(contact.MaxFootResidualMeters,Is.LessThan(.025f));Assert.That(grip.MaximumResidualMeters,Is.LessThan(.02f));
                    Assert.That(rig.transform.position,Is.EqualTo(origin));Assert.That(rig.transform.rotation,Is.EqualTo(rotation));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
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
