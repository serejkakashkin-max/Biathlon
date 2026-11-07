using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum Biathlete167MotionMode { Inspection, SkateV2, Performance }

    /// <summary>
    /// Asset-studio motion, in metres and normalized left/right cycles. This is authored
    /// procedural animation, not mocap, a race controller or a substitute for ground adaptation.
    /// V2 timing follows Nordiq Canada's One Skate and Team Aker Daehlie's V2 coaching notes:
    /// a double-pole action on each skate, complete weight transfer and a forward whole-body fall.
    /// https://www.teamakerdahlie.com/technique/technique/#v2
    /// https://nordiqcanada.ca/wp-content/uploads/L2T-OS-RM-step-4-Technique-development-skating-Nov-132c-2017.pdf
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Biathlete167Motion : MonoBehaviour
    {
        [Header("Studio playback only")]
        [SerializeField, Min(.4f)] private float cycleSeconds = 1.28f;
        [SerializeField, Range(.1f, 2f)] private float playbackSpeed = 1f;
        [Header("Ski glide, push and recovery")]
        [SerializeField, Range(.51f, .65f)] private float skiContactFraction = .56f;
        [SerializeField] private float skiLandingWidth = .145f;
        [SerializeField] private float skiPushWidth = .32f;
        [SerializeField] private float skiForwardReach = .12f;
        [SerializeField] private float skiBackwardTravel = .24f;
        [SerializeField] private float recoveryLift = .12f;
        [SerializeField] private float skiLandingYaw = 12f;
        [SerializeField] private float skiPushYaw = 19f;
        [SerializeField] private float skiPushEdge = 7f;
        [SerializeField] private float skiRecoveryPitch = 2f;
        [Header("Body alignment")]
        [SerializeField] private float weightTransfer = .16f;
        [SerializeField] private float hipDrop = .095f;
        [SerializeField] private float poleCompression = .035f;
        [SerializeField] private float hipForward = .055f;
        [SerializeField] private float hipPitch = 9f;
        [SerializeField] private float spinePitch = 10f;
        [SerializeField] private float chestPitch = 3f;
        [SerializeField] private float compressionPitch = 5f;
        [SerializeField] private float maximumPelvisReachAdjustment = .08f;
        [Header("Two synchronized poles per skate")]
        [SerializeField, Range(.3f, .65f)] private float poleContactFraction = .48f;
        [SerializeField, Range(0f, .4f)] private float polePlantDelay = .2f;
        [SerializeField] private float polePlantAngle = 28f;
        [SerializeField] private float poleReleaseAngle = 58f;
        [SerializeField] private float poleGripWidth = .275f;
        [SerializeField] private float polePlantWidth = .35f;
        [SerializeField] private float poleGripForward = .58f;
        [SerializeField] private float poleGripBackward = -.11f;
        [SerializeField] private float poleRecoveryLift = .10f;
        [Tooltip("Authored palm centre from the hand bone, in neutral root axes. X is mirrored per side.")]
        [SerializeField] private Vector3 neutralGripOffset = new Vector3(.025f, -.08f, .05f);

        private Biathlete167Rig rig;
        private Biathlete167MainMotion mainReference;
        public Biathlete167MainMotion MainReference=>mainReference;
        private Biathlete167SportMotion performance;
        public Biathlete167SportMotion Performance=>performance;
        private Transform root, hips, spine, chest, neck;
        private Transform[] resetTransforms;
        private Vector3[] bindPositions;
        private Quaternion[] bindRotations;
        private Vector3 hipsRest;
        private Limb leftLeg, rightLeg, leftArm, rightArm;
        private Ski leftSki, rightSki;
        private Pole leftPole, rightPole;
        private float phase;
        private float groundHeight;

        public Biathlete167MotionMode Mode { get; private set; }
        public bool PlaybackEnabled { get; set; }
        public float Phase => phase;
        public float CycleSeconds => Mode==Biathlete167MotionMode.Performance?performance.DurationSeconds:mainReference!=null?mainReference.CycleSeconds:cycleSeconds;
        public float PlaybackSpeed
        {
            get => playbackSpeed;
            set
            {
                if (!float.IsFinite(value) || value < .1f || value > 2f)
                    throw new ArgumentOutOfRangeException(nameof(value));
                playbackSpeed = value;
            }
        }
        public bool LeftSkiGrounded { get; private set; }
        public bool RightSkiGrounded { get; private set; }
        public bool PolesPlanted { get; private set; }
        public float GroundHeight => groundHeight;
        /// <summary>Maximum end-effector reach residual in world metres, for studio QA.</summary>
        public float MaximumReachError { get; private set; }
        public float PelvisReachAdjustment { get; private set; }
        public Vector3 LeftPoleTip => leftPole.Prop.TransformPoint(leftPole.TipInProp);
        public Vector3 RightPoleTip => rightPole.Prop.TransformPoint(rightPole.TipInProp);

        public void Initialize(Biathlete167Rig binding,bool useMainReference=false)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (rig == binding) return;
            if (rig != null) Stop();
            binding.Validate();
            binding.SetPose(0);
            rig = binding;
            root = binding.transform;
            hips = binding.Bone(HumanBodyBones.Hips);
            spine = binding.Bone(HumanBodyBones.Spine);
            chest = binding.Bone(HumanBodyBones.Chest);
            neck = binding.Bone(HumanBodyBones.Neck);
            hipsRest = root.InverseTransformPoint(hips.position);
            leftLeg = ReadLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            rightLeg = ReadLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            leftArm = ReadLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            rightArm = ReadLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            leftSki = ReadSki(leftLeg.End);
            rightSki = ReadSki(rightLeg.End);
            groundHeight = Mathf.Min(leftSki.Ground, rightSki.Ground);
            leftPole = ReadPole(leftArm.End, leftSki.Side);
            rightPole = ReadPole(rightArm.End, rightSki.Side);

            // One-time hierarchy/mesh reads. Evaluation does not allocate, search or rebuild meshes.
            resetTransforms = rig.Animator.GetComponentsInChildren<Transform>(true);
            bindPositions = new Vector3[resetTransforms.Length];
            bindRotations = new Quaternion[resetTransforms.Length];
            for (int i = 0; i < resetTransforms.Length; i++)
            {
                bindPositions[i] = resetTransforms[i].localPosition;
                bindRotations[i] = resetTransforms[i].localRotation;
            }
            Mode = Biathlete167MotionMode.Inspection;
            PlaybackEnabled = false;
            if(useMainReference)
            {
                mainReference=new Biathlete167MainMotion(binding);
                if(binding.PerformanceDefinition!=null)performance=new Biathlete167SportMotion(binding,mainReference);
            }
        }

        public void SetMode(Biathlete167MotionMode mode)
        {
            RequireInitialized();
            if (mode != Biathlete167MotionMode.Inspection && mode != Biathlete167MotionMode.SkateV2)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == Biathlete167MotionMode.Inspection) { Stop(); return; }
            if (Mode != mode) rig.SetPose(0);
            if(Mode==Biathlete167MotionMode.Performance)performance.Release();
            Mode = mode;
            EvaluateCycle(phase);
        }

        /// <summary>Release animation before calling Rig.SetPose for an inspection pose.</summary>
        public void Stop()
        {
            if(Mode==Biathlete167MotionMode.Performance)performance?.Release();
            PlaybackEnabled = false;
            Mode = Biathlete167MotionMode.Inspection;
            if (rig != null) rig.SetPose(0);
            RefreshAppearance();
        }

        private void LateUpdate()
        {
            if(rig==null||!PlaybackEnabled)return;
            float next=phase+Time.unscaledDeltaTime*playbackSpeed/CycleSeconds;
            if(Mode==Biathlete167MotionMode.SkateV2)EvaluateCycle(next);
            else if(Mode==Biathlete167MotionMode.Performance)
            {EvaluateAction(performance.Action,performance.Loops?Mathf.Repeat(next,1):Mathf.Min(1,next));if(!performance.Loops&&next>=1)PlaybackEnabled=false;}
        }

        private void OnDisable() { if (rig != null) Stop(); }
        private void OnDestroy(){performance?.Dispose();mainReference?.Dispose();}
        public void EvaluateAction(Biathlete167Action action,float normalizedPhase)
        {
            RequireInitialized();if(performance==null)throw new InvalidOperationException("Natural hero sports choreography is not configured.");
            performance.Evaluate(action,normalizedPhase);phase=normalizedPhase;Mode=Biathlete167MotionMode.Performance;
            RefreshAppearance();
        }
        public void RefreshAppearance()=>performance?.RefreshPreviewEyewear(Mode==Biathlete167MotionMode.Performance?performance.EyewearLift:0);

        /// <summary>
        /// Apply an exact phase for scrubbing/capture/tests, independently of playback or frame order.
        /// The root stays fixed: skis glide backwards on a studio treadmill, poles have grounded tips
        /// during drive, and the free ski clears the snow on its inward recovery.
        /// </summary>
        public void EvaluateCycle(float normalizedPhase)
        {
            RequireInitialized();
            if (!float.IsFinite(normalizedPhase)) throw new ArgumentOutOfRangeException(nameof(normalizedPhase));
            if(Mode==Biathlete167MotionMode.Performance)performance.Release();
            if(mainReference!=null)
            {
                phase=Mathf.Repeat(normalizedPhase,1);Mode=Biathlete167MotionMode.SkateV2;
                mainReference.Evaluate(phase);MaximumReachError=0;PelvisReachAdjustment=0;
                RefreshAppearance();
                PolesPlanted=mainReference.SourceDriver.PoleDriveActive;return;
            }
            if (Mode != Biathlete167MotionMode.SkateV2) rig.SetPose(0);
            Mode = Biathlete167MotionMode.SkateV2;
            phase = Mathf.Repeat(normalizedPhase, 1f);
            ResetBones();
            MaximumReachError = 0f;

            float polePhase = Mathf.Repeat(phase * 2f - polePlantDelay, 1f);
            float compression = Mathf.Pow(Mathf.Sin(Mathf.PI * polePhase), 2f);
            float weight = Mathf.Sin(phase * Mathf.PI * 2f);
            hips.position = root.TransformPoint(hipsRest + new Vector3(-weightTransfer * weight,
                -hipDrop - poleCompression * compression, hipForward + .02f * compression));
            Rotate(hips, new Vector3(hipPitch, -2f * weight, 1.5f * weight));
            Rotate(spine, new Vector3(spinePitch + compressionPitch * compression, 0, -.5f * weight));
            Rotate(chest, new Vector3(chestPitch, weight, -.5f * weight));
            // Look along the track while the hip and trunk provide the forward fall; no lateral waist fold.
            Rotate(neck, new Vector3(-(hipPitch + spinePitch + chestPitch + compressionPitch * compression) * .7f, 0, 0));

            SkiPose left = SampleSki(leftSki, phase);
            SkiPose right = SampleSki(rightSki, Mathf.Repeat(phase + .5f, 1f));
            // A short pelvis correction respects the imported athlete's measured leg lengths. The
            // push cannot pull the ankle off the snow or lock a knee to reach an impossible target.
            PelvisReachAdjustment = Mathf.Min(maximumPelvisReachAdjustment,
                Mathf.Max(RequiredPelvisDrop(leftLeg, left.Foot), RequiredPelvisDrop(rightLeg, right.Foot)));
            hips.position -= root.TransformVector(Vector3.up * PelvisReachAdjustment);
            ApplySki(ref leftLeg, leftSki, left);
            ApplySki(ref rightLeg, rightSki, right);
            LeftSkiGrounded = left.Grounded;
            RightSkiGrounded = right.Grounded;
            PolesPlanted = polePhase <= poleContactFraction;
            ApplyPole(ref leftArm, leftPole, polePhase, weight);
            ApplyPole(ref rightArm, rightPole, polePhase, weight);
        }

        private SkiPose SampleSki(Ski ski, float skiPhase)
        {
            bool grounded = skiPhase <= skiContactFraction;
            float width, travel, lift, edge, pitch, yaw;
            if (grounded)
            {
                float drive = Smooth(skiPhase / skiContactFraction);
                width = Mathf.Lerp(skiLandingWidth, skiPushWidth, drive);
                travel = Mathf.Lerp(skiForwardReach, -skiBackwardTravel, drive);
                lift = pitch = 0;
                // Edge engages late in the push, and releases before the recovering ski is airborne.
                edge = skiPushEdge * Bell(Mathf.Clamp01((skiPhase / skiContactFraction - .32f) / .68f));
                yaw = Mathf.Lerp(skiLandingYaw, skiPushYaw, drive);
            }
            else
            {
                float recovery = (skiPhase - skiContactFraction) / (1f - skiContactFraction);
                float blend = Smooth(recovery);
                width = Mathf.Lerp(skiPushWidth, skiLandingWidth, blend) - .045f * Bell(recovery);
                travel = Mathf.Lerp(-skiBackwardTravel, skiForwardReach, blend);
                lift = recoveryLift * Bell(recovery);
                pitch = -skiRecoveryPitch * Bell(recovery);
                edge = 0;
                yaw = Mathf.Lerp(skiPushYaw, skiLandingYaw, blend) - 5f * Bell(recovery);
            }
            Quaternion delta = Quaternion.AngleAxis(ski.Side * yaw, Vector3.up) *
                               Quaternion.AngleAxis(-ski.Side * edge, Vector3.forward) *
                               Quaternion.AngleAxis(pitch, Vector3.right);
            Vector3 soleOffset = new Vector3(0, groundHeight - ski.FootRest.y, 0);
            float edgeClearance = ski.HalfWidth * Mathf.Abs(Mathf.Sin(edge * Mathf.Deg2Rad));
            Vector3 sole = new Vector3(ski.Side * width, groundHeight + lift + edgeClearance,
                ski.FootRest.z + travel);
            return new SkiPose { Foot = sole - delta * soleOffset,
                Rotation = delta * ski.FootRotation, Grounded = grounded };
        }

        private void ApplySki(ref Limb leg, Ski ski, SkiPose pose)
        {
            Solve(ref leg, root.TransformPoint(pose.Foot), root.TransformDirection(new Vector3(ski.Side * .12f, 0, 1)));
            leg.End.rotation = root.rotation * pose.Rotation;
        }

        private float RequiredPelvisDrop(Limb leg, Vector3 footInRoot)
        {
            Vector3 upper = root.InverseTransformPoint(leg.Upper.position);
            Vector3 lower = root.InverseTransformPoint(leg.Lower.position);
            Vector3 end = root.InverseTransformPoint(leg.End.position);
            float reach = (Vector3.Distance(upper, lower) + Vector3.Distance(lower, end)) * .99f;
            Vector3 difference = upper - footInRoot;
            float horizontalSquared = difference.x * difference.x + difference.z * difference.z;
            float availableHeight = Mathf.Sqrt(Mathf.Max(0, reach * reach - horizontalSquared));
            return Mathf.Max(0, difference.y - availableHeight);
        }

        private void ApplyPole(ref Limb arm, Pole pole, float polePhase, float weight)
        {
            bool planted = polePhase <= poleContactFraction;
            float progress = planted ? polePhase / poleContactFraction : (polePhase - poleContactFraction) / (1f - poleContactFraction);
            float blend = Smooth(progress);
            float angle = Mathf.Lerp(planted ? polePlantAngle : poleReleaseAngle,
                planted ? poleReleaseAngle : polePlantAngle, blend) * Mathf.Deg2Rad;
            // Keep the locked-arm loading phase in front of the chest. Rearward
            // hand travel accelerates after the hands have descended, preventing
            // the old path from folding the elbow beside the athlete's face.
            float drive=planted?blend:1-blend;
            float gripZ = planted?Mathf.Lerp(poleGripForward,poleGripBackward,blend*blend)
                :Mathf.Lerp(poleGripBackward,poleGripForward,1-Mathf.Pow(1-blend,4));
            float lift = planted ? 0 : poleRecoveryLift * Bell(progress);

            // Each planting keeps its lateral ground coordinate. Only the treadmill's travel axis
            // advances during drive; body weight can cross above the ski without dragging pole tips sideways.
            float strokeStart = phase - polePhase * .5f;
            float plantShift = -weightTransfer * Mathf.Sin(strokeStart * Mathf.PI * 2f) * .5f;
            float nextPlantShift = -plantShift;
            float tipX = pole.Side * polePlantWidth + (planted ? plantShift : Mathf.Lerp(plantShift, nextPlantShift, blend));
            float gripX = pole.Side * poleGripWidth - weightTransfer * weight * .5f;
            float horizontal = pole.Length * Mathf.Sin(angle);
            float dx = gripX - tipX;
            float dz = Mathf.Sqrt(Mathf.Max(0, horizontal * horizontal - dx * dx));
            Vector3 grip = new Vector3(gripX, groundHeight + lift + pole.Length * Mathf.Cos(angle), gripZ);
            Vector3 tip = new Vector3(tipX, groundHeight + lift, gripZ - dz);
            Quaternion correction = Quaternion.FromToRotation(pole.AxisInRoot, (tip - grip).normalized);
            Vector3 wrist = grip - correction * pole.GripFromWristInRoot;
            Solve(ref arm, root.TransformPoint(wrist), root.TransformDirection(new Vector3(pole.Side * .32f, -.65f, Mathf.Lerp(.4f,-.25f,drive))));
            arm.End.rotation = root.rotation * correction * pole.HandRotation;
        }

        private void Rotate(Transform bone, Vector3 rootDegrees)
        {
            bone.rotation = root.rotation * Quaternion.Euler(rootDegrees) * Quaternion.Inverse(root.rotation) * bone.rotation;
        }

        private void ResetBones()
        {
            // Do not reset the animator placement transform, nor the owning root. Both remain caller-owned.
            for (int i = 0; i < resetTransforms.Length; i++)
                if (resetTransforms[i] != rig.Animator.transform && resetTransforms[i] != root)
                    resetTransforms[i].SetLocalPositionAndRotation(bindPositions[i], bindRotations[i]);
        }

        private void Solve(ref Limb limb, Vector3 target, Vector3 hint)
        {
            Vector3 origin = limb.Upper.position;
            float upperLength = Vector3.Distance(origin, limb.Lower.position);
            float lowerLength = Vector3.Distance(limb.Lower.position, limb.End.position);
            Vector3 difference = target - origin;
            float distance = Mathf.Clamp(difference.magnitude, Mathf.Abs(upperLength - lowerLength) + .0001f,
                upperLength + lowerLength - .0001f);
            Vector3 direction = difference.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(hint, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(root.forward, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(root.right, direction);
            bend.Normalize();
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
            Vector3 joint = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            limb.Upper.rotation = Quaternion.FromToRotation(limb.Lower.position - origin, joint - origin) * limb.Upper.rotation;
            limb.Lower.rotation = Quaternion.FromToRotation(limb.End.position - limb.Lower.position,
                origin + direction * distance - limb.Lower.position) * limb.Lower.rotation;
            MaximumReachError = Mathf.Max(MaximumReachError, Vector3.Distance(limb.End.position, target));
        }

        private Limb ReadLimb(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones end) => new Limb
        { Upper = rig.Bone(upper), Lower = rig.Bone(lower), End = rig.Bone(end) };

        private Ski ReadSki(Transform foot)
        {
            MeshFilter filter = EquipmentMesh(foot);
            Vector3 footRest = root.InverseTransformPoint(foot.position);
            float minimum = float.PositiveInfinity, halfWidth = 0;
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 p = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                minimum = Mathf.Min(minimum, p.y);
                if (Mathf.Abs(p.z - footRest.z) < .15f) halfWidth = Mathf.Max(halfWidth, Mathf.Abs(p.x - footRest.x));
            }
            return new Ski { Side = Mathf.Sign(footRest.x), FootRest = footRest, Ground = minimum,
                FootRotation = Quaternion.Inverse(root.rotation) * foot.rotation, HalfWidth = halfWidth };
        }

        private Pole ReadPole(Transform hand, float side)
        {
            MeshFilter filter = EquipmentMesh(hand);
            Vector3 wrist = root.InverseTransformPoint(hand.position);
            Vector3 gripOffset = new Vector3(neutralGripOffset.x * side, neutralGripOffset.y, neutralGripOffset.z);
            Vector3 grip = wrist + gripOffset;
            Vector3[] vertices = filter.sharedMesh.vertices;
            Vector3 farthest = Vector3.zero;
            float greatestDistance = 0;
            // The point furthest from the palm identifies the distal metal tip, not the basket/strap.
            foreach (Vector3 vertex in vertices)
            {
                Vector3 p = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                float distance = (p - grip).sqrMagnitude;
                if (distance > greatestDistance) { greatestDistance = distance; farthest = p; }
            }
            Vector3 axis = (farthest - grip).normalized;
            float endProjection = Vector3.Dot(farthest - grip, axis);
            Vector3 tip = Vector3.zero;
            int count = 0;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 p = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if (Vector3.Dot(p - grip, axis) >= endProjection - .001f) { tip += p; count++; }
            }
            tip /= count;
            return new Pole { Side = side, Prop = filter.transform,
                TipInProp = filter.transform.InverseTransformPoint(root.TransformPoint(tip)),
                AxisInRoot = (tip - grip).normalized, Length = Vector3.Distance(grip, tip),
                GripFromWristInRoot = gripOffset, HandRotation = Quaternion.Inverse(root.rotation) * hand.rotation };
        }

        private static MeshFilter EquipmentMesh(Transform bone)
        {
            // Typed hand/foot binding restricts the search to the relevant rigid attachment.
            // The studio's imported rig has one independent long equipment mesh under each bone.
            MeshFilter result = null;
            float longest = 0;
            foreach (MeshFilter filter in bone.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                float size = filter.sharedMesh.bounds.size.magnitude;
                if (size > longest) { longest = size; result = filter; }
            }
            if (result == null) throw new InvalidOperationException("M16.7 motion requires readable independent ski and pole meshes on the typed limb bindings.");
            return result;
        }

        private void RequireInitialized()
        {
            if (rig == null) throw new InvalidOperationException("Initialize the M16.7 studio motion before evaluating it.");
        }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6f - 15f) + 10f); }
        private static float Bell(float t) { float sine = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)); return sine * sine; }
        private struct Limb { public Transform Upper, Lower, End; }
        private struct Ski { public float Side, Ground, HalfWidth; public Vector3 FootRest; public Quaternion FootRotation; }
        private struct SkiPose { public Vector3 Foot; public Quaternion Rotation; public bool Grounded; }
        private struct Pole
        {
            public float Side, Length;
            public Transform Prop;
            public Vector3 TipInProp, AxisInRoot, GripFromWristInRoot;
            public Quaternion HandRotation;
        }
    }
}
