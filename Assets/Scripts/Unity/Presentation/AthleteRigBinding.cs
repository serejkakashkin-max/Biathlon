using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum AthleteBone
    {
        Hips, Spine, Chest, Neck, Head,
        LeftUpperArm, LeftLowerArm, LeftHand, RightUpperArm, RightLowerArm, RightHand,
        LeftUpperLeg, LeftLowerLeg, LeftFoot, RightUpperLeg, RightLowerLeg, RightFoot, Count
    }

    public enum AthleteSocket
    {
        LeftHandGrip, RightHandGrip, LeftSkiBinding, RightSkiBinding,
        RifleBack, RifleAim, CameraTarget, Count
    }

    /// <summary>Asset-side binding only. Names are authoring labels, never athlete identity.
    /// Bind convention: metres, +Y up, +Z forward; limb local axes match the original rig.
    /// An imported rig can supply correction transforms at these bindings.</summary>
    public sealed class AthleteRigBinding : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform[] bones;
        [SerializeField] private Transform[] sockets;
        [SerializeField] private Animator animator;
        private Vector3[] bindPositions;
        private Quaternion[] bindRotations;
        public Transform ModelRoot => modelRoot;
        public Animator Animator => animator;
        public Transform Bone(AthleteBone bone) => bones[(int)bone];
        public Transform Socket(AthleteSocket socket) => sockets[(int)socket];

        public void Configure(Transform root, Transform[] boneBindings, Transform[] socketBindings, Animator animation = null)
        {
            modelRoot = root;
            bones = boneBindings == null ? null : (Transform[])boneBindings.Clone();
            sockets = socketBindings == null ? null : (Transform[])socketBindings.Clone();
            animator = animation;
            Validate();
            RememberBindPose();
        }

        public void Validate()
        {
            if (modelRoot == null || (modelRoot != transform && !modelRoot.IsChildOf(transform)))
                throw new InvalidOperationException("Athlete model root must belong to its rig.");
            ValidateBindings(bones, (int)AthleteBone.Count, "bones");
            ValidateBindings(sockets, (int)AthleteSocket.Count, "sockets");
            if (animator != null)
            {
                if (!animator.transform.IsChildOf(modelRoot) && animator.transform != modelRoot)
                    throw new InvalidOperationException("Animator must be inside the visual model.");
                animator.applyRootMotion = false;
            }
        }

        private void ValidateBindings(Transform[] bindings, int count, string category)
        {
            if (bindings == null || bindings.Length != count)
                throw new InvalidOperationException("Incomplete athlete " + category + ".");
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i] == null || !bindings[i].IsChildOf(modelRoot))
                    throw new InvalidOperationException("Athlete " + category + " must belong to the model.");
                for (int j = 0; j < i; j++)
                    if (bindings[j] == bindings[i]) throw new InvalidOperationException("Duplicate athlete " + category + ".");
            }
        }

        private void Awake()
        {
            // Runtime factories configure after AddComponent. Serialized prefabs already have bindings.
            if (modelRoot != null) { Validate(); RememberBindPose(); }
        }

        private void RememberBindPose()
        {
            bindPositions = new Vector3[bones.Length];
            bindRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            { bindPositions[i] = bones[i].localPosition; bindRotations[i] = bones[i].localRotation; }
        }

        public void ResetPose()
        {
            if (bindPositions == null) RememberBindPose();
            for (int i = 0; i < bones.Length; i++)
            { bones[i].localPosition = bindPositions[i]; bones[i].localRotation = bindRotations[i]; }
        }

        public void Rotate(AthleteBone bone, Quaternion delta)
        {
            if (bindRotations == null) RememberBindPose();
            bones[(int)bone].localRotation = bindRotations[(int)bone] * delta;
        }

        public void Offset(AthleteBone bone, Vector3 delta)
        {
            if (bindPositions == null) RememberBindPose();
            bones[(int)bone].localPosition = bindPositions[(int)bone] + delta;
        }
    }
}
