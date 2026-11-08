#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167SkateQualityTests
    {
        private Biathlete167Rig rig;
        private Biathlete167Motion motion;

        [SetUp] public void Create()
        {
            rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Biathlete167Rig>(
                "Assets/Biathlon/Characters/Biathlete_16_7/Prefabs/Biathlete_16_7_Natural.prefab"));
            motion = rig.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(rig, true);
            motion.Stop();
        }

        [TearDown] public void Cleanup() { if (rig != null) Object.DestroyImmediate(rig.gameObject); }

        [TestCase(Biathlete167Action.Skate)]
        [TestCase(Biathlete167Action.Acceleration)]
        [TestCase(Biathlete167Action.Uphill)]
        public void SkatingTracksTheKneesWithoutExaggeratedPelvisSway(Biathlete167Action action)
        {
            float leftmost = float.PositiveInfinity, rightmost = float.NegativeInfinity;
            float bow = 0, elbowPeak = 0, footLeft = float.PositiveInfinity, footRight = float.NegativeInfinity;
            var records = new List<string> { "phase,side,hip_x_m,knee_outward_m,elbow_deg" };
            for (int frame = 0; frame <= 512; frame++)
            {
                motion.EvaluateAction(action, frame / 512f);
                float hipX = rig.transform.InverseTransformPoint(rig.Bone(HumanBodyBones.Hips).position).x;
                leftmost = Mathf.Min(leftmost, hipX); rightmost = Mathf.Max(rightmost, hipX);
                for (int side = 0; side < 2; side++)
                {
                    var hip = rig.Bone(side == 0 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg).position;
                    var knee = rig.Bone(side == 0 ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg).position;
                    var foot = rig.Bone(side == 0 ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot).position;
                    float footX = rig.transform.InverseTransformPoint(foot).x;
                    footLeft = Mathf.Min(footLeft, footX); footRight = Mathf.Max(footRight, footX);
                    var shoulder = rig.Bone(side == 0 ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm).position;
                    var elbow = rig.Bone(side == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm).position;
                    var hand = rig.Bone(side == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand).position;
                    float outward = Vector3.Dot(Vector3.ProjectOnPlane(knee - hip, foot - hip), rig.transform.right) * (side == 0 ? -1 : 1);
                    float angle = Vector3.Angle(shoulder - elbow, hand - elbow);
                    bow = Mathf.Max(bow, outward); elbowPeak = Mathf.Max(elbowPeak, angle);
                    records.Add(FormattableString.Invariant($"{frame / 512f},{side},{hipX},{outward},{angle}"));
                }
            }
            const string folder = "artifacts/m16-7-skate-tracking";
            Directory.CreateDirectory(folder);
            File.WriteAllLines(folder + "/" + action + ".csv", records);
            TestContext.WriteLine($"{action}: pelvis span {rightmost-leftmost:F3} m, outward knee {bow:F3} m, elbow {elbowPeak:F1} degrees");
            var defects = new List<string>();
            // Owner requests a quiet body; the skate push must remain active.
            if (footRight-footLeft < .25f) defects.Add("The legs lost their skating push");
            if (rightmost-leftmost > .001f) defects.Add($"Periodic lateral pelvis sway: {rightmost-leftmost:F3} m");
            if (bow > .06f) defects.Add($"Outward-bowed knee: {bow:F3} m");
            float elbowLimit = action == Biathlete167Action.Uphill ? 145 : action == Biathlete167Action.Skate ? 155 : 165;
            if (elbowPeak > elbowLimit) defects.Add($"Overextended arm in {action}: {elbowPeak:F1} degrees");
            Assert.That(defects, Is.Empty, string.Join("\n", defects));
        }

        [TestCase(Biathlete167Action.Skate)]
        [TestCase(Biathlete167Action.Acceleration)]
        [TestCase(Biathlete167Action.Uphill)]
        public void SkatingKeepsSoftKneesLowFinishingElbowsAndAnEvenHandReturn(Biathlete167Action action)
        {
            var shoulders = new[] { rig.Bone(HumanBodyBones.LeftUpperArm), rig.Bone(HumanBodyBones.RightUpperArm) };
            var elbows = new[] { rig.Bone(HumanBodyBones.LeftLowerArm), rig.Bone(HumanBodyBones.RightLowerArm) };
            var thighs = new[] { rig.Bone(HumanBodyBones.LeftUpperLeg), rig.Bone(HumanBodyBones.RightUpperLeg) };
            var knees = new[] { rig.Bone(HumanBodyBones.LeftLowerLeg), rig.Bone(HumanBodyBones.RightLowerLeg) };
            var feet = new[] { rig.Bone(HumanBodyBones.LeftFoot), rig.Bone(HumanBodyBones.RightFoot) };
            var previous = new Vector3[2];
            var highestGrip = new[] { float.NegativeInfinity, float.NegativeInfinity };
            var records = new List<string> { "phase,side,knee_deg,elbow_below_shoulder_m,elbow_outset_m,grip_height_m,grip_speed_mps" };
            float highestKnee = 0, highestLowElbow = float.NegativeInfinity, peakSpeed = 0;
            for (int frame = 0; frame <= 512; frame++)
            {
                motion.EvaluateAction(action, frame / 512f);
                float dt = motion.CycleSeconds / 512f;
                for (int side = 0; side < 2; side++)
                {
                    var grip = rig.GripContact(side == 1).position;
                    float knee = Vector3.Angle(thighs[side].position - knees[side].position, feet[side].position - knees[side].position);
                    float gripHeight = Vector3.Dot(grip - shoulders[side].position, rig.transform.up);
                    var upper = elbows[side].position - shoulders[side].position;
                    float elbowHeight = Vector3.Dot(upper, rig.transform.up);
                    float outset = Vector3.Dot(upper, rig.transform.right) * (side == 0 ? -1 : 1);
                    float speed = frame == 0 ? 0 : Vector3.Distance(grip, previous[side]) / dt;
                    highestKnee = Mathf.Max(highestKnee, knee);
                    highestGrip[side] = Mathf.Max(highestGrip[side], gripHeight);
                    if (gripHeight < -.22f) highestLowElbow = Mathf.Max(highestLowElbow, elbowHeight);
                    peakSpeed = Mathf.Max(peakSpeed, speed);
                    records.Add(FormattableString.Invariant($"{frame / 512f},{side},{knee},{elbowHeight},{outset},{gripHeight},{speed}"));
                    previous[side] = grip;
                }
            }
            const string folder = "artifacts/m16-7-skate-rhythm";
            Directory.CreateDirectory(folder);
            File.WriteAllLines(folder + "/" + action + ".csv", records);
            var defects = new List<string>();
            if (highestKnee >= 155f) defects.Add($"Locked-looking leg {highestKnee:F1} degrees");
            if (highestLowElbow >= -.11f) defects.Add($"High elbow at low finish {highestLowElbow:F3} m");
            if (peakSpeed >= 4.2f) defects.Add($"Abrupt hand return {peakSpeed:F2} m/s at normal playback speed");
            if (action == Biathlete167Action.Uphill)
                Assert.That(highestGrip[1] - highestGrip[0], Is.InRange(.04f, .15f), "V1 retains its offset plant.");
            Assert.That(defects, Is.Empty, string.Join("\n", defects));
        }
    }
}
#endif
