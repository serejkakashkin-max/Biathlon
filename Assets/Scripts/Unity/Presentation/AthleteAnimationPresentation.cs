using System;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only animation input. No reference to a live race escapes the feed.</summary>
    public readonly struct AthleteAnimationFrame
    {
        public AthletePresentationState Athlete { get; }
        public double TimeSeconds { get; }
        public long Tick { get; }
        public double PresentationCurvature { get; }
        public double PresentationGrade { get; }
        public AthleteAnimationFrame(AthletePresentationState athlete, long tick, int frequency, double alpha, double? presentationCurvature = null, double? presentationGrade = null)
        {
            if (tick < 0 || frequency <= 0 || double.IsNaN(alpha) || double.IsInfinity(alpha) || alpha < 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha));
            Athlete = athlete; Tick = tick;
            PresentationCurvature = presentationCurvature ?? athlete.CourseCurvaturePerMeter;
            PresentationGrade = presentationGrade ?? athlete.CourseGrade;
            if (!double.IsFinite(PresentationGrade)) throw new ArgumentOutOfRangeException(nameof(presentationGrade));
            if (double.IsNaN(PresentationCurvature) || double.IsInfinity(PresentationCurvature)) throw new ArgumentOutOfRangeException(nameof(presentationCurvature));
            TimeSeconds = Math.Max(0, tick - 1 + alpha) / frequency;
        }
    }

    /// <summary>A swappable pose consumer. Animation output cannot advance the race or move its root.</summary>
    public abstract class AthletePoseDriver : MonoBehaviour
    {
        public abstract void Apply(AthleteRigBinding rig, AthleteAnimationFrame frame);
    }

    public sealed class AthleteAnimationPresentation : MonoBehaviour
    {
        private AthleteRigBinding rig;
        private AthletePoseDriver driver;
        private AthleteRiflePresentation rifle;
        private AthleteShootingPose shootingPose;
        private AthleteGroundContact contact;
        private AthleteDetailPresentation detail;
        public AthleteDetailPresentation Detail => detail;
        public AthleteRigBinding Rig => rig;
        public AthleteAnimationFrame LastFrame { get; private set; }

        public void Initialize(AthleteRigBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            binding.Validate(); rig = binding;
            driver = binding.GetComponent<AthletePoseDriver>();
            rifle = binding.GetComponent<AthleteRiflePresentation>();
            shootingPose = binding.GetComponent<AthleteShootingPose>();
            contact = binding.GetComponent<AthleteGroundContact>();
            detail = new AthleteDetailPresentation(binding);
        }

        public void Render(AthleteAnimationFrame frame, float cameraDistance = 0)
        {
            if (rig == null) throw new InvalidOperationException("Initialize athlete rig first.");
            LastFrame = frame;
            if (rig.Animator != null) rig.Animator.applyRootMotion = false;
            if (!detail.Prepare(cameraDistance, frame)) return;
            // Protect the presentation placement from asset root-motion/pose code as well.
            var position = transform.localPosition;
            var rotation = transform.localRotation;
            var scale = transform.localScale;
            var modelPosition = rig.ModelRoot.localPosition;
            var modelRotation = rig.ModelRoot.localRotation;
            if (rig.Animator == null) rig.ResetPose();
            if (driver != null) driver.Apply(rig, frame);
            if (shootingPose != null) shootingPose.Render(rig, frame);
            if (rifle != null) rifle.Render(rig, frame);
            if (contact != null) contact.Render(rig, frame);
            rig.ModelRoot.SetLocalPositionAndRotation(modelPosition, modelRotation);
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }
    }
}
