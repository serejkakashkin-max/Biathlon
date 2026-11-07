#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167SupportTests
    {
        private const string Root = "Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence = "artifacts/m16-7-support-motion";
        private Biathlete167Rig rig;
        private Biathlete167Motion motion;

        [SetUp] public void Create()
        {
            rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Biathlete167Rig>(Root + "/Prefabs/Biathlete_16_7_Natural.prefab"));
            motion = rig.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(rig, true);
            motion.Stop();
            Directory.CreateDirectory(Evidence);
        }
        [TearDown] public void Cleanup() { if (rig != null) Object.DestroyImmediate(rig.gameObject); }

        [TestCase(Biathlete167Action.Skate)]
        [TestCase(Biathlete167Action.Acceleration)]
        [TestCase(Biathlete167Action.Uphill)]
        public void TravelHasSeatedHeelsAHighGlideAndACompletePush(Biathlete167Action action)
        {
            var feet = new[] { rig.Bone(HumanBodyBones.LeftFoot), rig.Bone(HumanBodyBones.RightFoot) };
            var skis = feet.Select(foot => foot.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(mesh => mesh.sharedMesh.bounds.size.sqrMagnitude).First()).ToArray();
            var sole = feet.Select(foot => foot.InverseTransformDirection(rig.transform.forward)).ToArray();
            var skiForward = skis.Select(ski => ski.transform.InverseTransformDirection(rig.transform.forward)).ToArray();
            var skiUp = skis.Select(ski => ski.transform.InverseTransformDirection(rig.transform.up)).ToArray();
            var vertices = skis.Select(ski => ski.sharedMesh.vertices).ToArray();
            float seated = 0, kneeHigh = 0, kneeLow = 180, heelPeak = 0, followThrough = float.PositiveInfinity;
            int loadedFrames = 0;
            var rows = new List<string> { "phase,side,heel_deg,knee_deg,clearance_m,hand_from_hip_m" };
            for (int frame = 0; frame < 128; frame++)
            {
                float phase = frame / 128f;
                motion.EvaluateAction(action, phase);
                var normal = new Vector3(0, 1, -motion.Performance.SurfaceGrade).normalized;
                for (int side = 0; side < 2; side++)
                {
                    var foot = feet[side]; var ski = skis[side].transform;
                    var forward = ski.TransformDirection(skiForward[side]);
                    float heel = Vector3.SignedAngle(forward, foot.TransformDirection(sole[side]), Vector3.Cross(ski.TransformDirection(skiUp[side]), forward));
                    float knee = Vector3.Angle(foot.parent.parent.position - foot.parent.position, foot.position - foot.parent.position);
                    float clearance = vertices[side].Min(v => Vector3.Dot(rig.transform.InverseTransformPoint(ski.TransformPoint(v)), normal));
                    float hand = Vector3.Dot(rig.GripContact(side == 1).position - rig.Bone(HumanBodyBones.Hips).position, rig.transform.forward);
                    rows.Add(FormattableString.Invariant($"{phase},{side},{heel},{knee},{clearance},{hand}"));
                    followThrough = Mathf.Min(followThrough, hand);
                    heelPeak = Mathf.Max(heelPeak, heel);
                    if (clearance > .007f) continue;
                    loadedFrames++;
                    if (Mathf.Abs(heel) < 4) seated++;
                    kneeHigh = Mathf.Max(kneeHigh, knee);
                    kneeLow = Mathf.Min(kneeLow, knee);
                }
            }
            File.WriteAllLines(Evidence + "/" + action + ".csv", rows);
            TestContext.WriteLine($"{action}: seated {seated}/{loadedFrames}, heel {heelPeak:F1}, knee {kneeLow:F1}–{kneeHigh:F1}, hands {followThrough:F3}");
            Assert.That(seated / loadedFrames, Is.GreaterThan(.68f), "A loaded ski needs a seated boot during the majority of glide.");
            // Owner review rejected the earlier 162-degree rise. Keep a visible
            // extension during glide while retaining a soft supporting knee.
            Assert.That(kneeHigh, Is.InRange(137f, 150f), "Extend through glide without locking the supporting knee.");
            Assert.That(kneeHigh-kneeLow, Is.GreaterThan(12), "Retain loading and extension without an exaggerated rise.");
            Assert.That(followThrough, Is.LessThan(-.035f), "Finish the pole stroke behind the hips.");
        }

        [Test] public void ApproachCarriesTheGripsNearTheHipsUntilRelease()
        {
            float furthest = 0;
            for (int frame = 0; frame <= 64; frame++)
            {
                motion.EvaluateAction(Biathlete167Action.RangeEntry, frame/64f*.3f);
                foreach (bool right in new[] { false, true })
                {
                    var hip = rig.Bone(right ? HumanBodyBones.RightUpperLeg : HumanBodyBones.LeftUpperLeg).position;
                    furthest = Mathf.Max(furthest, Vector3.Dot(rig.GripContact(right).position-hip, rig.transform.forward));
                }
            }
            File.WriteAllText(Evidence+"/approach.txt",FormattableString.Invariant($"{furthest}"));
            Assert.That(furthest, Is.LessThan(.22f), "Glide with soft elbows and the grips near the hips.");
        }

        [Test] public void ProneSleevesActuallyRestOnTheMat()
        {
            var skin = rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
            var baked = new Mesh();
            try
            {
                skin.BakeMesh(baked);
                var rest = baked.vertices;
                var elbows = new[] { rig.Bone(HumanBodyBones.LeftLowerArm), rig.Bone(HumanBodyBones.RightLowerArm) };
                var indices = elbows.Select(elbow => Enumerable.Range(0, rest.Length).Where(i =>
                    Vector3.Distance(skin.transform.TransformPoint(rest[i]), elbow.position) < .077f).ToArray()).ToArray();
                Assert.That(indices.All(list => list.Length > 25));
                var rows = new List<string> { "phase,left_sleeve_m,right_sleeve_m,left_joint_m,right_joint_m" };
                float min = float.MaxValue, max = float.MinValue;
                for (int frame = 0; frame <= 24; frame++)
                {
                    motion.EvaluateAction(Biathlete167Action.Prone, frame/24f);
                    skin.BakeMesh(baked);
                    var posed = baked.vertices;
                    float Gap(int side) => indices[side].Min(i => rig.transform.InverseTransformPoint(skin.transform.TransformPoint(posed[i])).y);
                    float left = Gap(0), right = Gap(1);
                    min = Mathf.Min(min, left, right); max = Mathf.Max(max, left, right);
                    rows.Add(FormattableString.Invariant($"{frame/24f},{left},{right},{elbows[0].position.y},{elbows[1].position.y}"));
                }
                File.WriteAllLines(Evidence+"/prone.csv",rows);
                Assert.That(min, Is.GreaterThan(-.007f), "Sleeve goes through the mat.");
                Assert.That(max, Is.LessThan(.012f), "Both elbows must meet the actual mat plane.");
            }
            finally { Object.DestroyImmediate(baked); }
        }
    }
}
#endif
