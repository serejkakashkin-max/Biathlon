using System;
using Biathlon.Simulation;
using Biathlon.Domain;
using UnityEngine;
using Unity.Profiling;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only animation input. No reference to a live race escapes the feed.</summary>
    public readonly struct AthleteAnimationFrame
    {
        public AthletePresentationState Athlete { get; }
        public double TimeSeconds { get; }
        public long Tick { get; }
        public int TickFrequency {get;}
        public double Alpha { get; }
        public float ShootingProgress => AthleteRiflePresentation.Progress(Athlete.ShootingPhase,
            Athlete.ShootingPhaseElapsedTicks,Athlete.ShootingPhaseDurationTicks,Alpha);
        public double PresentationCurvature { get; }
        public double PresentationGrade { get; }
        public double LateralMetersPerSecond { get; }
        public AthleteAnimationFrame(AthletePresentationState athlete, long tick, int frequency, double alpha, double? presentationCurvature = null, double? presentationGrade = null,double lateralMetersPerSecond=0)
        {
            if (tick < 0 || frequency <= 0 || double.IsNaN(alpha) || double.IsInfinity(alpha) || alpha < 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha));
            Athlete = athlete; Tick = tick; Alpha = alpha;
            TickFrequency=frequency;
            PresentationCurvature = presentationCurvature ?? athlete.CourseCurvaturePerMeter;
            PresentationGrade = presentationGrade ?? athlete.CourseGrade;
            if(!double.IsFinite(lateralMetersPerSecond))throw new ArgumentOutOfRangeException(nameof(lateralMetersPerSecond));
            LateralMetersPerSecond=lateralMetersPerSecond;
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
        private static readonly ProfilerMarker PoseMarker=new ProfilerMarker("Biathlon.Presentation.AthletePose");
        private AthleteRigBinding rig;
        private AthletePoseDriver driver;
        private AthleteRiflePresentation rifle;
        private AthleteShootingPose shootingPose;
        private AthleteGroundContact contact;
        private AthleteGripPresentation grip;
        private AthleteDetailPresentation detail;
        private static AthleteImpactVisualSettings impactSettings;
        private AthleteImpactPresentation impact;
        private Biathlete167RacePresentation upgradedVisual;
        public AthleteDetailPresentation Detail => detail;
        public AthleteRigBinding Rig => rig;
        public AthleteAnimationFrame LastFrame { get; private set; }

        public void Initialize(AthleteRigBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            binding.Validate(); rig = binding;
            driver = binding.GetComponent<AthleteHybridLocomotionDriver>();
            if (driver == null) driver = binding.GetComponent<AthletePoseDriver>();
            rifle = binding.GetComponent<AthleteRiflePresentation>();
            shootingPose = binding.GetComponent<AthleteShootingPose>();
            contact = binding.GetComponent<AthleteGroundContact>();
            grip = binding.GetComponent<AthleteGripPresentation>();
            detail = new AthleteDetailPresentation(binding);
            upgradedVisual = binding.GetComponent<Biathlete167RacePresentation>();
            impactSettings??=AthleteImpactVisualSettings.Load();impact=new AthleteImpactPresentation(impactSettings);
        }

        public void Render(AthleteAnimationFrame frame, float cameraDistance = 0, bool visibleShootingPose = true)
        {
            using var marker=PoseMarker.Auto();
            if (rig == null) throw new InvalidOperationException("Initialize athlete rig first.");
            LastFrame = frame;
            impact.Observe(frame.Athlete.Impact,frame.Athlete.WorldPose,frame.Tick,frame.TickFrequency,frame.Athlete.OnCourse&&!frame.Athlete.ShootingActive&&frame.Athlete.Phase!=CompetitionPhase.Finished&&frame.Athlete.Phase!=CompetitionPhase.Classified);
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
            impact.Apply(rig,frame.TimeSeconds);
            if (shootingPose != null) shootingPose.Render(rig, frame);
            if (rifle != null) rifle.Render(rig, frame);
            if (contact != null) contact.Render(rig, frame);
            if (grip != null) grip.Render(rig);
            rig.ModelRoot.SetLocalPositionAndRotation(modelPosition, modelRotation);
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
            upgradedVisual?.Render(frame,detail.Distant,visibleShootingPose);
        }
    }
}
