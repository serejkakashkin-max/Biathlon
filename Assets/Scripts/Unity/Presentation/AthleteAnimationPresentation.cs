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
        public AthleteAnimationFrame(AthletePresentationState athlete, long tick, int frequency, double alpha)
        {
            if (tick < 0 || frequency <= 0 || double.IsNaN(alpha) || double.IsInfinity(alpha) || alpha < 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha));
            Athlete = athlete; Tick = tick;
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
        public AthleteRigBinding Rig => rig;
        public AthleteAnimationFrame LastFrame { get; private set; }

        public void Initialize(AthleteRigBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            binding.Validate(); rig = binding;
            driver = binding.GetComponent<AthletePoseDriver>();
        }

        public void Render(AthleteAnimationFrame frame)
        {
            if (rig == null) throw new InvalidOperationException("Initialize athlete rig first.");
            LastFrame = frame;
            if (rig.Animator != null) rig.Animator.applyRootMotion = false;
            // Protect the presentation placement from asset root-motion/pose code as well.
            var position = transform.localPosition;
            var rotation = transform.localRotation;
            var scale = transform.localScale;
            var modelPosition = rig.ModelRoot.localPosition;
            var modelRotation = rig.ModelRoot.localRotation;
            if (rig.Animator == null) rig.ResetPose();
            if (driver != null) driver.Apply(rig, frame);
            rig.ModelRoot.SetLocalPositionAndRotation(modelPosition, modelRotation);
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }
    }
}
