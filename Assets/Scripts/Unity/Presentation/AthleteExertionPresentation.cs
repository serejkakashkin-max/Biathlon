using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public readonly struct ExertionInput
    {
        public readonly float Fatigue, Reserve, HeartRate;
        public readonly bool Finished, Shooting;
        public ExertionInput(float fatigue, float reserve, float heartRate, bool finished = false, bool shooting = false)
        {
            if (!float.IsFinite(fatigue) || fatigue < 0 || fatigue > 1 || !float.IsFinite(reserve) || reserve < 0 || reserve > 1 ||
                !float.IsFinite(heartRate) || heartRate < 0) throw new ArgumentOutOfRangeException();
            Fatigue = fatigue; Reserve = reserve; HeartRate = heartRate; Finished = finished; Shooting = shooting;
        }
        public static ExertionInput From(Biathlon.Simulation.AthletePresentationState a) => new ExertionInput(
            (float)a.Physiology.MuscularFatigue, (float)a.Physiology.AnaerobicReserve, (float)a.Physiology.HeartRateBpm,
            a.Phase == CompetitionPhase.Finished, a.ShootingActive);
    }

    [Serializable]
    public sealed class ExertionPresentationSettings
    {
        public int schema_version;
        public string id;
        public float hr_low, hr_high, fatigue_weight, depletion_weight, hr_weight, response_s;
        public float tired_lean_deg, tired_hip_drop_m, arm_reduction, stride_reduction, breathing_deg, breathing_hz;
        public float finish_lean_deg, finish_hip_drop_m, finish_hand_height_m, finish_hand_forward_m;
        public static ExertionPresentationSettings Load()
        {
            var d = JsonUtility.FromJson<ExertionPresentationSettings>(Resources.Load<TextAsset>("Biathlon/athlete_exertion_v1").text);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) || !(d.hr_low > 0 && d.hr_high > d.hr_low) ||
                !(d.fatigue_weight >= 0 && d.depletion_weight >= 0 && d.hr_weight >= 0) ||
                Mathf.Abs(d.fatigue_weight + d.depletion_weight + d.hr_weight - 1) > .0001f ||
                !(d.response_s > 0 && d.response_s <= 5) || !(d.tired_lean_deg >= 0 && d.tired_lean_deg <= 15) ||
                !(d.tired_hip_drop_m >= 0 && d.tired_hip_drop_m <= .08f) || !(d.arm_reduction >= 0 && d.arm_reduction <= .4f) ||
                !(d.stride_reduction >= 0 && d.stride_reduction <= .3f) || !(d.breathing_deg >= 0 && d.breathing_deg <= 3) ||
                !(d.breathing_hz > 0 && d.breathing_hz < 2) || !(d.finish_lean_deg > 0 && d.finish_lean_deg <= 65) ||
                !(d.finish_hip_drop_m > 0 && d.finish_hip_drop_m < .4f) || !(d.finish_hand_height_m > .5f && d.finish_hand_height_m < 1.2f) ||
                !(d.finish_hand_forward_m > 0 && d.finish_hand_forward_m < .7f)) throw new ArgumentException("Invalid exertion presentation definition.");
            return d;
        }
        public float Intensity(ExertionInput input) => Mathf.Clamp01(input.Fatigue * fatigue_weight +
            (1 - input.Reserve) * depletion_weight + Mathf.InverseLerp(hr_low, hr_high, input.HeartRate) * hr_weight);
    }

    /// <summary>Visual effort blend only; never feeds physiology or the shooting sight.</summary>
    public sealed class AthleteExertionPresentation
    {
        private ExertionPresentationSettings settings;
        public float Intensity { get; private set; }
        public float FinishWeight { get; private set; }
        public void Apply(LocomotionPose pose, ExertionInput input, float tuckWeight, double time, double dt, bool reset)
        {
            if (settings == null) settings = ExertionPresentationSettings.Load();
            float target = settings.Intensity(input);
            float blend = reset ? 1 : 1 - Mathf.Exp(-(float)dt / settings.response_s);
            Intensity = Mathf.Lerp(Intensity, target, blend);
            FinishWeight = Mathf.Lerp(FinishWeight, input.Finished ? 1 : 0, blend);
            // Do not add an independent breathing oscillator to authoritative shooting sway.
            if (input.Shooting) return;
            // Follow the same blend as the visible tuck posture, including interruptions.
            // A selected-state switch must not instantly remove fatigue from the body.
            float activity = (1 - tuckWeight) * Intensity;
            pose.torso_lean_deg += activity * (settings.tired_lean_deg + settings.breathing_deg * Mathf.Sin((float)(time * settings.breathing_hz * Math.PI * 2)));
            pose.hip_drop_m += activity * settings.tired_hip_drop_m;
            pose.arm_swing_m *= 1 - activity * settings.arm_reduction;
            pose.stride_m *= 1 - activity * settings.stride_reduction;
            pose.torso_lean_deg = Mathf.Lerp(pose.torso_lean_deg, settings.finish_lean_deg, FinishWeight);
            pose.hip_drop_m = Mathf.Lerp(pose.hip_drop_m, settings.finish_hip_drop_m, FinishWeight);
            pose.hand_height_m = Mathf.Lerp(pose.hand_height_m, settings.finish_hand_height_m, FinishWeight);
            pose.hand_forward_m = Mathf.Lerp(pose.hand_forward_m, settings.finish_hand_forward_m, FinishWeight);
            pose.cadence_hz *= 1 - FinishWeight;
            pose.arm_swing_m *= 1 - FinishWeight;
            pose.stride_m *= 1 - FinishWeight;
        }
    }
}
