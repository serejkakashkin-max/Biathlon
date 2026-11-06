#if UNITY_EDITOR
using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167MotionTests
    {
        private const string Prefab = "Assets/Biathlon/Characters/Biathlete_16_7/Prefabs/Biathlete_16_7.prefab";
        private static readonly HumanBodyBones[] Limbs =
        {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };

        private Biathlete167Rig rig;
        private Biathlete167Motion motion;

        [SetUp]
        public void CreateActualImportedRig()
        {
            rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)).GetComponent<Biathlete167Rig>();
            rig.transform.SetPositionAndRotation(new Vector3(4, .3f, -8), Quaternion.Euler(0, 37, 0));
            motion = rig.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(rig);
        }

        [TearDown]
        public void DestroyRig() { if (rig != null) Object.DestroyImmediate(rig.gameObject); }

        [Test]
        public void CycleTransfersWeightRecoversEachSkiAndPlantsBothPolesWithoutRootDrift()
        {
            Vector3 rootPosition = rig.transform.position;
            Quaternion rootRotation = rig.transform.rotation;
            Vector3 animatorPosition = rig.Animator.transform.localPosition;
            Quaternion animatorRotation = rig.Animator.transform.localRotation;
            var lengths = new float[8];
            for (int limb = 0; limb < 4; limb++)
                for (int segment = 0; segment < 2; segment++)
                    lengths[limb * 2 + segment] = Vector3.Distance(rig.Bone(Limbs[limb * 3 + segment]).position,
                        rig.Bone(Limbs[limb * 3 + segment + 1]).position);

            int leftRecovery = 0, rightRecovery = 0, poleContact = 0, poleRecovery = 0;
            float smallestHipX = float.PositiveInfinity, largestHipX = float.NegativeInfinity;
            for (int sample = 0; sample < 120; sample++)
            {
                motion.EvaluateCycle(sample / 120f);
                Assert.That(motion.LeftSkiGrounded || motion.RightSkiGrounded, Is.True, "The reference V2 cycle never becomes a two-foot jump.");
                if (!motion.LeftSkiGrounded) leftRecovery++;
                if (!motion.RightSkiGrounded) rightRecovery++;
                float hipX = rig.transform.InverseTransformPoint(rig.Bone(HumanBodyBones.Hips).position).x;
                smallestHipX = Mathf.Min(smallestHipX, hipX);
                largestHipX = Mathf.Max(largestHipX, hipX);
                Assert.That(motion.MaximumReachError, Is.LessThan(.003f), "Unreachable limb target at phase " + motion.Phase);
                Assert.That(motion.PelvisReachAdjustment, Is.LessThan(.08f), "Pelvis adaptation reached its guard at phase " + motion.Phase);
                Vector3 leftTip = rig.transform.InverseTransformPoint(motion.LeftPoleTip);
                Vector3 rightTip = rig.transform.InverseTransformPoint(motion.RightPoleTip);
                Assert.That(leftTip.y, Is.GreaterThanOrEqualTo(motion.GroundHeight - .003f));
                Assert.That(rightTip.y, Is.GreaterThanOrEqualTo(motion.GroundHeight - .003f));
                if (motion.PolesPlanted)
                {
                    poleContact++;
                    Assert.That(leftTip.y, Is.EqualTo(motion.GroundHeight).Within(.003f), "Left metal tip must touch the snow during drive.");
                    Assert.That(rightTip.y, Is.EqualTo(motion.GroundHeight).Within(.003f), "Right metal tip must touch the snow during drive.");
                }
                else if (leftTip.y > motion.GroundHeight + .04f && rightTip.y > motion.GroundHeight + .04f) poleRecovery++;

                for (int limb = 0; limb < 4; limb++)
                    for (int segment = 0; segment < 2; segment++)
                        Assert.That(Vector3.Distance(rig.Bone(Limbs[limb * 3 + segment]).position,
                            rig.Bone(Limbs[limb * 3 + segment + 1]).position),
                            Is.EqualTo(lengths[limb * 2 + segment]).Within(.00002f), "IK must rotate joints without stretching the source anatomy.");
                Assert.That(rig.transform.position, Is.EqualTo(rootPosition));
                Assert.That(Quaternion.Angle(rig.transform.rotation, rootRotation), Is.LessThan(.001f));
                Assert.That(rig.Animator.transform.localPosition, Is.EqualTo(animatorPosition));
                Assert.That(Quaternion.Angle(rig.Animator.transform.localRotation, animatorRotation), Is.LessThan(.001f));
                Assert.That(rig.Rifle.parent, Is.SameAs(rig.Socket(Biathlete167Socket.RifleBack)));
            }
            Assert.That(leftRecovery, Is.GreaterThan(30));
            Assert.That(rightRecovery, Is.GreaterThan(30));
            Assert.That(poleContact, Is.InRange(50, 65));
            Assert.That(poleRecovery, Is.GreaterThan(15));
            Assert.That(largestHipX - smallestHipX, Is.GreaterThan(.28f), "Weight must travel onto alternating supporting skis.");
        }

        [Test]
        public void ScrubbingIsPeriodicOrderIndependentAndDoesNotLeaveTheRifleInAnAimingSocket()
        {
            rig.SetPose(3);
            motion.EvaluateCycle(.375f);
            Transform[] bones = rig.Animator.GetComponentsInChildren<Transform>(true);
            Vector3[] positions = new Vector3[bones.Length];
            Quaternion[] rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) { positions[i] = bones[i].position; rotations[i] = bones[i].rotation; }
            motion.EvaluateCycle(.96f);
            motion.EvaluateCycle(.08f);
            motion.EvaluateCycle(7.375f);
            for (int i = 0; i < bones.Length; i++)
            {
                Assert.That(Vector3.Distance(positions[i], bones[i].position), Is.LessThan(.00002f), bones[i].name);
                Assert.That(Quaternion.Angle(rotations[i], bones[i].rotation), Is.LessThan(.06f), bones[i].name);
            }
            Assert.That(motion.Phase, Is.EqualTo(.375f));
            Assert.That(rig.Rifle.parent, Is.SameAs(rig.Socket(Biathlete167Socket.RifleBack)));
            motion.PlaybackEnabled = true;
            motion.Stop();
            Assert.That(motion.PlaybackEnabled, Is.False);
            Assert.That(motion.Mode, Is.EqualTo(Biathlete167MotionMode.Inspection));
            Assert.That(rig.PoseIndex, Is.Zero);
            rig.SetPose(3);
            Assert.That(rig.Rifle.parent, Is.SameAs(rig.Socket(Biathlete167Socket.RifleHand)));
        }

        [TestCase(0f)]
        [TestCase(.1f)]
        [TestCase(.34f)]
        [TestCase(.5f)]
        [TestCase(.56f)]
        public void PhaseBoundariesDoNotTeleportFeetOrPoleTips(float boundary)
        {
            motion.EvaluateCycle(boundary - .0001f);
            Vector3 leftFoot = rig.Bone(HumanBodyBones.LeftFoot).position;
            Vector3 rightFoot = rig.Bone(HumanBodyBones.RightFoot).position;
            Vector3 leftTip = motion.LeftPoleTip, rightTip = motion.RightPoleTip;
            motion.EvaluateCycle(boundary + .0001f);
            Assert.That(Vector3.Distance(leftFoot, rig.Bone(HumanBodyBones.LeftFoot).position), Is.LessThan(.003f));
            Assert.That(Vector3.Distance(rightFoot, rig.Bone(HumanBodyBones.RightFoot).position), Is.LessThan(.003f));
            Assert.That(Vector3.Distance(leftTip, motion.LeftPoleTip), Is.LessThan(.003f));
            Assert.That(Vector3.Distance(rightTip, motion.RightPoleTip), Is.LessThan(.003f));
        }

        [Test]
        public void CycleDeformsTheActualSkinnedMeshAndKeepsAllVerticesFinite()
        {
            SkinnedMeshRenderer skin = rig.Lods.GetLODs()[0].renderers[0] as SkinnedMeshRenderer;
            if (skin == null)
                foreach (Renderer renderer in rig.Lods.GetLODs()[0].renderers)
                    if (renderer is SkinnedMeshRenderer candidate) { skin = candidate; break; }
            Assert.That(skin, Is.Not.Null);
            var mesh = new Mesh();
            try
            {
                motion.EvaluateCycle(.15f);
                skin.BakeMesh(mesh);
                Vector3[] first = mesh.vertices;
                motion.EvaluateCycle(.65f);
                skin.BakeMesh(mesh);
                Vector3[] second = mesh.vertices;
                float largestMovement = 0;
                for (int i = 0; i < second.Length; i++)
                {
                    Assert.That(float.IsFinite(second[i].x) && float.IsFinite(second[i].y) && float.IsFinite(second[i].z), Is.True);
                    Assert.That(second[i].magnitude, Is.LessThan(4f));
                    largestMovement = Mathf.Max(largestMovement, Vector3.Distance(first[i], second[i]));
                }
                Assert.That(largestMovement, Is.GreaterThan(.15f), "The actual Humanoid skin, not only equipment, must follow weight transfer.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void HandsClearTheFaceAndElbowsStayAnatomicallyOpenAcrossTheStroke()
        {
            for(int sample=0;sample<120;sample++)
            {
                motion.EvaluateCycle(sample/120f);
                var head=rig.Bone(HumanBodyBones.Head);
                var face=head.position+rig.transform.up*.105f+rig.transform.forward*.045f;
                foreach(bool right in new[]{false,true})
                {
                    var shoulder=rig.Bone(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                    var elbow=rig.Bone(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
                    var hand=rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                    Assert.That(Vector3.Distance(hand.position,face),Is.GreaterThan(.20f),"Hand intersects the face at phase "+motion.Phase);
                    float angle=Vector3.Angle(shoulder.position-elbow.position,hand.position-elbow.position);
                    Assert.That(angle,Is.InRange(32f,177f),"Elbow folded or locked at phase "+motion.Phase);
                }
            }
        }

        [Test]
        public void WarmedCycleEvaluationDoesNotAllocateManagedMemory()
        {
            for(int i=0;i<120;i++)motion.EvaluateCycle(i/120f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<480;i++)motion.EvaluateCycle(i/120f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero,"Animation must reuse its cached transforms and mesh measurements.");
        }

        [Test]
        public void InvalidScrubAndPlaybackInputsCannotPoisonTheRig()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.EvaluateCycle(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.EvaluateCycle(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.PlaybackSpeed = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.PlaybackSpeed = float.NaN);
            motion.EvaluateCycle(-.25f);
            Assert.That(motion.Phase, Is.EqualTo(.75f).Within(.000001f));
        }
    }
}
#endif
