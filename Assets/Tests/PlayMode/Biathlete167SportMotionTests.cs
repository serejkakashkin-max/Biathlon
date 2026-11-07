#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167SportMotionTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence="artifacts/m16-7-sport-motion-20261106";
        private Biathlete167Rig rig;private Biathlete167Motion motion;
        private static readonly HumanBodyBones[] Limbs={HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        [SetUp] public void Create(){rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);Assert.That(motion.Performance,Is.Not.Null);}
        [TearDown] public void Cleanup(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}
        [TestCase(Biathlete167Action.Skate),TestCase(Biathlete167Action.Acceleration),TestCase(Biathlete167Action.Uphill),TestCase(Biathlete167Action.RangeEntry),TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone),TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void AuthoredActionsPreserveAnatomyAndRoot(Biathlete167Action action)
        {
            var root=rig.transform.position;var rotation=rig.transform.rotation;var lengths=new float[8];
            for(int limb=0;limb<4;limb++)for(int segment=0;segment<2;segment++)lengths[limb*2+segment]=Vector3.Distance(rig.Bone(Limbs[limb*3+segment]).position,rig.Bone(Limbs[limb*3+segment+1]).position);
            float maxResidual=0;
            for(int sample=0;sample<=128;sample++)
            {
                motion.EvaluateAction(action,sample/128f);maxResidual=Mathf.Max(maxResidual,motion.Performance.MaximumReachResidualMeters);
                Assert.That(motion.Performance.MaximumWristBendDegrees,Is.LessThanOrEqualTo(25.1f),action+" final blended wrist");
                foreach(var bone in Limbs){var p=rig.Bone(bone).position;Assert.That(float.IsFinite(p.sqrMagnitude),Is.True);}
                for(int limb=0;limb<4;limb++)for(int segment=0;segment<2;segment++)Assert.That(Vector3.Distance(rig.Bone(Limbs[limb*3+segment]).position,rig.Bone(Limbs[limb*3+segment+1]).position),Is.EqualTo(lengths[limb*2+segment]).Within(.00003f));
                Assert.That(rig.transform.position,Is.EqualTo(root));Assert.That(Quaternion.Angle(rotation,rig.transform.rotation),Is.LessThan(.001f));
            }
            Assert.That(maxResidual,Is.LessThan(.025f),action+" has an unreachable hand/foot target; fix choreography rather than stretching anatomy.");
        }
        [Test] public void TechniqueTimingAndAccelerationAreDistinct()
        {
            motion.EvaluateAction(Biathlete167Action.Skate,.35f);Assert.That(motion.MainReference.SourceDriver.PoleCadenceHz/motion.MainReference.SourceDriver.CadenceHz,Is.EqualTo(2).Within(.0001f));
            motion.EvaluateAction(Biathlete167Action.Uphill,.35f);Assert.That(motion.MainReference.SourceDriver.PoleCadenceHz/motion.MainReference.SourceDriver.CadenceHz,Is.EqualTo(1).Within(.0001f));
            motion.EvaluateAction(Biathlete167Action.Acceleration,.1f);float start=motion.Performance.SpeedMetersPerSecond;
            motion.EvaluateAction(Biathlete167Action.Acceleration,.9f);Assert.That(motion.Performance.SpeedMetersPerSecond,Is.GreaterThan(start+4));
            motion.EvaluateAction(Biathlete167Action.RangeEntry,1);Assert.That(motion.Performance.SpeedMetersPerSecond,Is.Zero);Assert.That(motion.Performance.RifleTransfer,Is.Zero);
        }
        [TestCase(Biathlete167Action.Skate, 0f)]
        [TestCase(Biathlete167Action.Acceleration, 0f)]
        [TestCase(Biathlete167Action.Uphill, .115f)]
        public void TravelKeepsActualSkisAndPolesOnTheAuthoredSurface(Biathlete167Action action, float grade)
        {
            Transform root = rig.transform;
            var normal = new Vector3(0, 1, -grade).normalized;
            MeshFilter Longest(HumanBodyBones bone) => rig.Bone(bone).GetComponentsInChildren<MeshFilter>(true)
                .OrderByDescending(mesh => mesh.sharedMesh.bounds.size.sqrMagnitude).First();
            var skis = new[] { Longest(HumanBodyBones.LeftFoot), Longest(HumanBodyBones.RightFoot) };
            var poles = new[] { Longest(HumanBodyBones.LeftHand), Longest(HumanBodyBones.RightHand) };
            var skiVertices = skis.Select(s => s.sharedMesh.vertices).ToArray();
            var toeInSki = skis.Select((s, i) => s.transform.InverseTransformPoint(rig.ToeHinge(i == 1).position)).ToArray();
            var gripInPole = poles.Select((p, i) => p.transform.InverseTransformPoint(rig.GripContact(i == 1).position)).ToArray();
            var tipInPole = poles.Select((p, i) => p.sharedMesh.vertices.OrderByDescending(v => (v - gripInPole[i]).sqrMagnitude).First()).ToArray();
            var lastTips = new Vector3[2];
            var highestSki = new float[2];
            var highestPole = new float[2];
            var contacts = new int[2];
            for (int frame = 0; frame <= 512; frame++)
            {
                motion.EvaluateAction(action, frame / 512f);
                float support = float.PositiveInfinity;
                for (int side = 0; side < 2; side++)
                {
                    var ski = skis[side].transform;
                    var pole = poles[side].transform;
                    float bottom = skiVertices[side].Min(v => Vector3.Dot(root.InverseTransformPoint(ski.TransformPoint(v)), normal));
                    support = Mathf.Min(support, bottom);
                    highestSki[side] = Mathf.Max(highestSki[side], bottom);
                    Assert.That(bottom, Is.GreaterThanOrEqualTo(-.002f), $"{action} ski {side}, frame {frame}");
                    Assert.That(Vector3.Distance(ski.TransformPoint(toeInSki[side]), rig.ToeHinge(side == 1).position), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(pole.TransformPoint(gripInPole[side]), rig.GripContact(side == 1).position), Is.LessThan(.001f));

                    var tip = root.InverseTransformPoint(pole.TransformPoint(tipInPole[side]));
                    float height = Vector3.Dot(tip, normal);
                    Assert.That(height, Is.InRange(-.006f, .32f), $"{action} pole {side}, frame {frame}");
                    if (frame > 0) Assert.That(Vector3.Distance(tip, lastTips[side]), Is.LessThan(.085f), $"{action} pole jump at frame {frame}");
                    lastTips[side] = tip;
                    highestPole[side] = Mathf.Max(highestPole[side], height);
                    if (Mathf.Abs(height) < .012f) contacts[side]++;
                }
                Assert.That(support, Is.LessThan(.007f), $"{action} floats above the snow at frame {frame}");
            }
            for (int side = 0; side < 2; side++)
            {
                Assert.That(highestSki[side], Is.GreaterThan(.04f));
                Assert.That(highestPole[side], Is.GreaterThan(.09f));
                Assert.That(contacts[side], Is.GreaterThan(120), "Both poles need a usable snow contact phase.");
            }
        }

        [Test] public void AccelerationActuallyIncreasesStrokeFrequency()
        {
            double CyclesBetween(int start, int end)
            {
                motion.EvaluateAction(Biathlete167Action.Acceleration, start / 128f);
                double previous = motion.MainReference.SourceDriver.Cycle;
                double cycles = 0;
                for (int i = start + 1; i <= end; i++)
                {
                    motion.EvaluateAction(Biathlete167Action.Acceleration, i / 128f);
                    double current = motion.MainReference.SourceDriver.Cycle;
                    double delta = current - previous;
                    cycles += delta >= 0 ? delta : delta + 1;
                    previous = current;
                }
                return cycles;
            }
            double early = CyclesBetween(0, 32);
            double late = CyclesBetween(96, 128);
            Assert.That(late, Is.GreaterThan(early * 1.4));
        }
        [Test] public void RangeBodyRetargetsTheActualShippingShootingChain()
        {
            var reference=motion.MainReference;var ids=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head};
            reference.SourceRig.ResetPose();rig.SetPose(0);
            var sourceBind=Enumerable.Range(0,5).Select(i=>reference.SourceRig.Bone((AthleteBone)i).rotation).ToArray();var targetBind=ids.Select(i=>rig.Bone(i).rotation).ToArray();
            foreach(var stance in new[]{Biathlon.Domain.ShootingStance.Standing,Biathlon.Domain.ShootingStance.Prone})foreach(float phase in new[]{0,.2f,.6f,1})
            {
                reference.EvaluateShooting(stance,phase);
                for(int i=0;i<5;i++)
                {
                    var expected=reference.SourceRig.Bone((AthleteBone)i).rotation*Quaternion.Inverse(sourceBind[i])*targetBind[i];
                    Assert.That(Quaternion.Angle(expected,rig.Bone(ids[i]).rotation),Is.LessThan(.06f),"Use the shipping body chain before authoring hero-specific contact improvements: "+stance+"/"+phase+"/"+ids[i]);
                }
            }
        }
        [Test] public void RangeScrubbingIsOrderIndependentAndStopRestoresCarriedEquipment()
        {
            var bones=rig.Animator.GetComponentsInChildren<Transform>();var positions=new Vector3[bones.Length];var rotations=new Quaternion[bones.Length];
            motion.EvaluateAction(Biathlete167Action.EnterProne,.8f);for(int i=0;i<bones.Length;i++){positions[i]=bones[i].position;rotations[i]=bones[i].rotation;}
            motion.EvaluateAction(Biathlete167Action.Standing,.2f);motion.EvaluateAction(Biathlete167Action.Skate,.5f);motion.EvaluateAction(Biathlete167Action.EnterProne,.8f);
            for(int i=0;i<bones.Length;i++){Assert.That(Vector3.Distance(positions[i],bones[i].position),Is.LessThan(.00003f),bones[i].name);Assert.That(Quaternion.Angle(rotations[i],bones[i].rotation),Is.LessThan(.08f),bones[i].name);}
            motion.Stop();Assert.That(motion.Mode,Is.EqualTo(Biathlete167MotionMode.Inspection));Assert.That(rig.Rifle.parent,Is.SameAs(rig.Socket(Biathlete167Socket.RifleBack)));
        }
        [Test] public void SettledShootingKeepsBothPalmContactsAndLimitsWristBend()
        {
            foreach(var action in new[]{Biathlete167Action.Standing,Biathlete167Action.Prone})for(int i=0;i<=64;i++)
            {
                motion.EvaluateAction(action,i/64f);
                Assert.That(Vector3.Distance(rig.GripContact(false).position,motion.Performance.SupportContact),Is.LessThan(.001f),action+" support palm");
                Assert.That(Vector3.Distance(rig.GripContact(true).position,motion.Performance.TriggerContact),Is.LessThan(.001f),action+" trigger palm");
                Assert.That(motion.Performance.MaximumWristBendDegrees,Is.LessThanOrEqualTo(25.1f),action+" wrist bend");
            }
        }
        [Test] public void ShootingAndStopPreserveSelectedNationalTextiles()
        {
            var outfit=rig.GetComponent<Biathlete167Outfit>();var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().First();
            foreach(var code in new[]{"RUS","FIN","FRA"})
            {
                outfit.ApplyNation(code);var expected=skin.sharedMaterials.Where(m=>m.name=="PULSE_Natural_Tailored_Textile"||m.name=="PULSE_Race_Red"||m.name=="PULSE_Scarlet_Accent").ToArray();
                motion.EvaluateAction(Biathlete167Action.Prone,.5f);foreach(var material in expected)Assert.That(skin.sharedMaterials,Does.Contain(material));
                motion.EvaluateAction(Biathlete167Action.Skate,.5f);foreach(var material in expected)Assert.That(skin.sharedMaterials,Does.Contain(material));
                motion.EvaluateAction(Biathlete167Action.Standing,.5f);motion.Stop();foreach(var material in expected)Assert.That(skin.sharedMaterials,Does.Contain(material));
            }
        }
        [Test] public void WarmSportsSamplingDoesNotAllocateManagedMemory()
        {
            foreach(var action in new[]{Biathlete167Action.Skate,Biathlete167Action.Uphill,Biathlete167Action.Acceleration,Biathlete167Action.Standing,Biathlete167Action.Prone})
            {
                for(int i=0;i<16;i++)motion.EvaluateAction(action,i/16f);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<128;i++)motion.EvaluateAction(action,i/128f);
                long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero,action+" warmed sampling");
            }
        }
        [Serializable] private sealed class Cycle { public string state;public float cycle_seconds;public int frames; }
        [Serializable] private sealed class Capture { public int schema_version=1;public string id="m167_shipping_motion_adaptation";public int width=1200,height=720;public string view="right side, actual Unity render";public Cycle[] cycles; }
        [UnityTest] public IEnumerator CaptureActualSportsMotionCycles()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();studio.Rig.gameObject.SetActive(false);
            var go=new GameObject("Sport cycle evidence camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;
            var receipt=new Capture();var cycles=new System.Collections.Generic.List<Cycle>();int frame=0;string folder=Evidence+"/motion";Directory.CreateDirectory(folder);
            try{using(var target=new BenchmarkRenderTarget(receipt.width,receipt.height))
            {
                foreach(Biathlete167Action action in Enum.GetValues(typeof(Biathlete167Action)))
                {
                    motion.EvaluateAction(action,0);float duration=motion.CycleSeconds;int count=Mathf.CeilToInt(duration*24);cycles.Add(new Cycle{state=action.ToString(),cycle_seconds=duration,frames=count});
                    for(int i=0;i<count;i++)
                    {
                        float phase=motion.Performance.Loops?i/(float)count:i/(float)(count-1);motion.EvaluateAction(action,phase);
                        studio.SetPreviewGrade(motion.Performance.SurfaceGrade);
                        float low=action==Biathlete167Action.Prone?1:action==Biathlete167Action.EnterProne?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,phase)):0;
                        var focus=new Vector3(0,Mathf.Lerp(.96f,.35f,low),0);camera.transform.position=rig.transform.TransformPoint(focus+new Vector3(4.7f,.15f,0));camera.transform.LookAt(rig.transform.TransformPoint(focus));
                        yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(folder+"/frame-"+(frame++).ToString("D4")+".png");
                    }
                }
                receipt.cycles=cycles.ToArray();File.WriteAllText(folder+"/complete.json",JsonUtility.ToJson(receipt,true)+"\n");
            }}finally{studio.SetPreviewGrade(0);studio.Rig.gameObject.SetActive(true);Object.Destroy(go);motion.Stop();}
        }
        [UnityTest] public IEnumerator CaptureActualSportsAndRangePoses()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();
            // The local fixture is used so all screenshots correspond to the tested instance.
            var cameraObject=new GameObject("Sport motion evidence camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;
            studio.Rig.gameObject.SetActive(false);Directory.CreateDirectory(Evidence+"/native");
            try{using(var target=new BenchmarkRenderTarget(1200,1000))
            {
                foreach(Biathlete167Action action in Enum.GetValues(typeof(Biathlete167Action)))
                {
                    float[] phases=action==Biathlete167Action.EnterProne||action==Biathlete167Action.EnterStanding?new[]{.2f,.45f,.7f,1f}:new[]{.25f,.75f};
                    foreach(float phase in phases)
                    {
                        motion.EvaluateAction(action,phase);studio.SetPreviewGrade(motion.Performance.SurfaceGrade);bool low=action==Biathlete167Action.Prone||(action==Biathlete167Action.EnterProne&&phase>.65f);var focus=new Vector3(0,low?.35f:.96f,low?.05f:0);
                        foreach(var item in new[]{("side",new Vector3(3.6f,.12f,0)),("front",new Vector3(.15f,.12f,3.8f))})
                        {camera.transform.position=rig.transform.TransformPoint(focus+item.Item2);camera.transform.LookAt(rig.transform.TransformPoint(focus));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+item.Item1+".png");}
                    }
                }
                foreach(var action in new[]{Biathlete167Action.Skate,Biathlete167Action.Uphill,Biathlete167Action.Standing,Biathlete167Action.Prone})foreach(float phase in new[]{.25f,.75f})foreach(bool right in new[]{false,true})
                {
                    motion.EvaluateAction(action,phase);var hand=rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position;
                    camera.transform.position=hand+new Vector3(right?-.38f:.38f,.15f,.55f);camera.transform.LookAt(hand);yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();
                    target.SavePng(Evidence+"/native/hand-"+action+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+(right?"right":"left")+".png");
                }
            }}finally{studio.SetPreviewGrade(0);studio.Rig.gameObject.SetActive(true);Object.Destroy(cameraObject);motion.Stop();}
        }
    }
}
#endif
