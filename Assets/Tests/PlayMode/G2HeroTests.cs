using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class G2HeroTests
    {
        private const string Root = "Biathlon/Athletes/G2Hero/";
        [Test]
        public void ClosedGlovesFollowPoleOrientationWithoutMovingContactsOrAllocating()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var motion=rig.GetComponent<AthleteLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                var contact=rig.GetComponent<AthleteGroundContact>();var grip=rig.GetComponent<AthleteGripPresentation>();var equipment=rig.GetComponent<AthleteEquipmentBinding>();
                for(int i=0;i<90;i++)
                {
                    motion.Sample(rig,new LocomotionInput(8,0,.9f),i/60.0);rifle.Sample(rig,0);contact.Sample(rig,0,motion.Cycle,motion.State,0);
                    var left=equipment.LeftPole.position;var right=equipment.RightPole.position;var rotation=equipment.LeftPole.rotation;
                    grip.Render(rig);
                    Assert.That(Vector3.Distance(equipment.LeftPole.position,left),Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(equipment.RightPole.position,right),Is.LessThan(.00001f));
                    Assert.That(Quaternion.Angle(equipment.LeftPole.rotation,rotation),Is.LessThan(.001f));
                    Assert.That(grip.MaximumResidualMeters,Is.LessThan(.015f));
                    Assert.That(Quaternion.Angle(rig.Socket(AthleteSocket.LeftHandGrip).rotation,rotation),Is.LessThan(.001f));
                }
                long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)grip.Render(rig);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.That(rig.transform.position,Is.EqualTo(Vector3.zero));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test]
        public void BibNumbersArePerInstanceAndDoNotMutateSharedMaterial()
        {
            var a=AthleteRigFactory.Create(null);var b=AthleteRigFactory.Create(null);
            try
            {
                var first=a.GetComponent<AthleteBibPresentation>();var second=b.GetComponent<AthleteBibPresentation>();
                var ar=a.GetComponentInChildren<SkinnedMeshRenderer>();var br=b.GetComponentInChildren<SkinnedMeshRenderer>();
                var shared=ar.sharedMaterials[2];float baseline=shared.GetFloat("_BibNumber");
                first.SetNumber(12);second.SetNumber(60);
                Assert.That(br.sharedMaterials[2],Is.SameAs(shared));Assert.That(shared.GetFloat("_BibNumber"),Is.EqualTo(baseline));
                var properties=new MaterialPropertyBlock();ar.GetPropertyBlock(properties,2);Assert.That(properties.GetFloat("_BibNumber"),Is.EqualTo(12));
                br.GetPropertyBlock(properties,2);Assert.That(properties.GetFloat("_BibNumber"),Is.EqualTo(60));
                long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)first.SetNumber(12);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>first.SetNumber(0));Assert.Throws<System.ArgumentOutOfRangeException>(()=>first.SetNumber(61));
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test]
        public void HeroFactoryAndSkinLodsRespectHysteresisShootingAndZeroAllocation()
        {
            var rig = AthleteRigFactory.Create(null);
            try
            {
                var detail = rig.GetComponent<AthleteSkinDetailBinding>(); Assert.That(detail, Is.Not.Null);
                var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>(); detail.Validate();
                detail.Prepare(80,false,true); Assert.That(detail.Level, Is.EqualTo(2));
                detail.Prepare(44,false,false); Assert.That(detail.Level, Is.EqualTo(2));
                detail.Prepare(42,false,false); Assert.That(detail.Level, Is.EqualTo(1));
                detail.Prepare(11,false,false); Assert.That(detail.Level, Is.EqualTo(1));
                detail.Prepare(9,false,false); Assert.That(detail.Level, Is.Zero);
                detail.Prepare(80,true,false); Assert.That(detail.Level, Is.Zero, "Shooting retains close skin detail.");
                detail.Prepare(80,false,true);
                Assert.That(skin.sharedMesh, Is.SameAs(Resources.Load<Mesh>(Root+"Meshes/HeroLOD2")));
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 600; i++) detail.Prepare(70,false,false);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before, Is.Zero);
                Assert.Throws<System.ArgumentOutOfRangeException>(() => detail.Prepare(float.NaN,false,false));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
        [Test]
        public void ContinuousSkinLodsHaveNormalizedM11WeightsAndRealGeometryReduction()
        {
            int previous = int.MaxValue;
            var rig = Object.Instantiate(Resources.Load<AthleteRigBinding>(Root + "HeroAthlete"));
            try
            {
                rig.Validate();
                Assert.That(rig.GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(rig.GetComponentInChildren<SkinnedMeshRenderer>().quality, Is.EqualTo(SkinQuality.Bone4));
                for (int lod = 0; lod < 3; lod++)
                {
                    var mesh = Resources.Load<Mesh>(Root + "Meshes/HeroLOD" + lod);
                    Assert.That(mesh, Is.Not.Null);
                    Assert.That(mesh.triangles.Length, Is.LessThan(previous)); previous = mesh.triangles.Length;
                    Assert.That(mesh.bindposes.Length, Is.EqualTo((int)AthleteBone.Count));
                    Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
                    int blended = 0;
                    foreach (var w in mesh.boneWeights)
                    {
                        Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1).Within(.0001));
                        Assert.That(w.boneIndex0, Is.InRange(0, 16)); Assert.That(w.boneIndex1, Is.InRange(0, 16));
                        Assert.That(w.boneIndex2, Is.InRange(0, 16)); Assert.That(w.boneIndex3, Is.InRange(0, 16));
                        if (w.weight1 > .01f) blended++;
                    }
                    Assert.That(blended, Is.GreaterThan(mesh.vertexCount / 4), "Continuous skin needs blended joints, not rigid segment weights.");
                }
                Assert.That(previous / 3, Is.LessThan(4000));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [UnityTest]
        public IEnumerator ExistingMotionContractDeformsHeroAndCapturesReviewFrames()
        {
            var root = new GameObject("G2 hero validation");
            var rig = Object.Instantiate(Resources.Load<AthleteRigBinding>(Root + "HeroAthlete"), root.transform);
            rig.GetComponent<AthleteBibPresentation>().SetNumber(12);
            var camera = new GameObject("G2 isolated review camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f,.21f,.25f);
            camera.cullingMask = 1 << 30; camera.fieldOfView = 32; camera.nearClipPlane = .05f;
            var light = new GameObject("G2 review key").AddComponent<Light>(); light.transform.SetParent(root.transform);
            light.type = LightType.Directional; light.intensity = 1.7f; light.cullingMask = 1 << 30; light.transform.rotation = Quaternion.Euler(35,-145,0);
            var fill = new GameObject("G2 review fill").AddComponent<Light>(); fill.transform.SetParent(root.transform);
            fill.type = LightType.Directional; fill.intensity = .5f; fill.cullingMask = 1 << 30; fill.transform.rotation = Quaternion.Euler(20,30,0);
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;
            var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>(); var bake = new Mesh();
            string output = Path.GetFullPath("artifacts/g2-hero/review"); Directory.CreateDirectory(output);
            using (var target = new BenchmarkRenderTarget(1024,1024))
            {
                for (int pose = 0; pose < 9; pose++)
                {
                    rig.ResetPose();
                    if (pose >= 2 && pose <= 4) rig.GetComponent<AthleteLocomotionDriver>().Sample(rig, new LocomotionInput(8,0,.9f), 2.25);
                    if (pose == 3) rig.GetComponent<AthleteShootingPose>().Sample(rig,ShootingStance.Prone,1);
                    if (pose == 4) rig.GetComponent<AthleteShootingPose>().Sample(rig,ShootingStance.Standing,1);
                    if (pose >= 7) rig.Rotate(AthleteBone.LeftUpperArm,Quaternion.Euler(-45,0,0));
                    var stance = pose == 3 ? ShootingStance.Prone : ShootingStance.Standing;
                    rig.GetComponent<AthleteRiflePresentation>().Sample(rig,pose == 3 || pose == 4 ? 1 : 0,stance:stance);
                    if (pose >= 2 && pose <= 4) rig.GetComponent<AthleteGroundContact>().Sample(rig,0,.25,SkiLocomotion.Flat,pose == 2 ? 0 : 1);
                    rig.GetComponent<AthleteGripPresentation>().Render(rig);
                    Vector3 at = pose == 3 ? new Vector3(0,.65f,.25f) : new Vector3(0,1.05f,0);
                    camera.transform.position = at + (pose == 0 ? new Vector3(1,.4f,4.1f) : pose == 1 ? new Vector3(-1,.4f,-4.1f) : new Vector3(3,1,3.5f));
                    camera.transform.LookAt(at);
                    if (pose == 5 || pose == 6)
                    {
                        at = new Vector3(0,1.68f,0);
                        camera.transform.position = at + (pose == 5 ? new Vector3(.3f,.08f,.92f) : new Vector3(-.65f,.1f,-.75f));
                        camera.transform.LookAt(at);
                    }
                    if (pose >= 7)
                    {
                        at=rig.Bone(AthleteBone.LeftHand).position+new Vector3(0,-.06f,.02f);
                        camera.transform.position=at+(pose==7?new Vector3(-.30f,.12f,.32f):new Vector3(-.45f,.1f,-.12f));camera.transform.LookAt(at);
                    }
                    for (int frame = 0; frame < 3; frame++) { target.Render(camera); yield return null; }
                    skin.BakeMesh(bake);
                    Assert.That(bake.vertexCount, Is.EqualTo(skin.sharedMesh.vertexCount));
                    Assert.That(bake.bounds.size.magnitude, Is.InRange(1,4));
                    foreach (var v in bake.vertices) Assert.That(float.IsFinite(v.sqrMagnitude), Is.True);
                    Assert.That(rig.transform.localPosition, Is.EqualTo(Vector3.zero), "Pose may not move placement root.");
                    target.SavePng(Path.Combine(output,"hero-pose-"+pose+".png"));
                }
            }
            Object.Destroy(bake); Object.Destroy(root); yield return null;
        }
    }
}
