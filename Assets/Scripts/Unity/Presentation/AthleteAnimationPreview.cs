using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum AthletePreviewSet { Locomotion, ProneSequence, StandingSequence, ExertionAndCorners }
    /// <summary>Repeatable visual fixture; it has no race host and never emits gameplay commands.</summary>
    public sealed class AthleteAnimationPreview : MonoBehaviour
    {
        public bool Frozen;
        public double SampleTime;
        public AthletePreviewSet StateSet;
        private AthletePreviewSet renderedSet;
        private bool hasRendered;
        private AthleteRigBinding[] rigs;
        private AthleteLocomotionDriver[] drivers;
        private AthleteShootingPose[] shooting;
        private AthleteRiflePresentation[] rifles;
        private AthleteGroundContact[] contacts;
        public int Count => rigs == null ? 0 : rigs.Length;
        public AthleteRigBinding Rig(int index) => rigs[index];
        public static LocomotionInput Input(int index)
        {
            switch ((SkiLocomotion)index)
            {
                case SkiLocomotion.Idle: return new LocomotionInput(0, 0, 0);
                case SkiLocomotion.Flat: return new LocomotionInput(8, 0, .9f);
                case SkiLocomotion.Climb: return new LocomotionInput(4, .1f, 1.0f);
                case SkiLocomotion.Acceleration: return new LocomotionInput(8, 0, 1.1f);
                case SkiLocomotion.Sprint: return new LocomotionInput(10, 0, 1.2f);
                case SkiLocomotion.Tuck: return new LocomotionInput(12, -.1f, .7f, tuck: true);
                default: return new LocomotionInput(12, -.1f, .7f);
            }
        }
        public void Initialize()
        {
            if (rigs != null) return;
            rigs = new AthleteRigBinding[7]; drivers = new AthleteLocomotionDriver[7];
            shooting = new AthleteShootingPose[7]; rifles = new AthleteRiflePresentation[7]; contacts = new AthleteGroundContact[7];
            for (int i = 0; i < rigs.Length; i++)
            {
                rigs[i] = OriginalAthleteRigBuilder.Create(transform);
                rigs[i].transform.localPosition = new Vector3((i - 3) * 2, 0, 0);
                drivers[i] = rigs[i].GetComponent<AthleteLocomotionDriver>();
                shooting[i] = rigs[i].GetComponent<AthleteShootingPose>(); rifles[i] = rigs[i].GetComponent<AthleteRiflePresentation>(); contacts[i] = rigs[i].GetComponent<AthleteGroundContact>();
            }
        }
        public void RenderAt(double time)
        {
            Initialize(); SampleTime = time;
            bool reset = !hasRendered || renderedSet != StateSet;
            for (int i = 0; i < rigs.Length; i++)
            {
                bool atRange = StateSet == AthletePreviewSet.ProneSequence || StateSet == AthletePreviewSet.StandingSequence;
                var stance = StateSet == AthletePreviewSet.StandingSequence ? ShootingStance.Standing : ShootingStance.Prone;
                var input = atRange ? new LocomotionInput(0, 0, 0, travelling: false) : Input(i);
                var exertion = new ExertionInput(0, 1, 60, shooting: atRange);
                if (StateSet == AthletePreviewSet.ExertionAndCorners)
                {
                    input = new LocomotionInput(i == 6 ? 0 : 8, 0, i == 5 ? 1.2f : .9f, travelling: i != 6,
                        curvature: i == 0 ? -.04f : i == 1 ? .04f : 0);
                    exertion = new ExertionInput(i >= 3 ? 1 : 0, i >= 3 ? 0 : 1, i >= 3 ? 190 : 60, finished: i == 6);
                }
                if (reset) drivers[i].Sample(rigs[i], input, time + 10, exertion);
                drivers[i].Sample(rigs[i], input, time, exertion);
                float progress = atRange ? SequenceProgress(i) : 0;
                shooting[i].Sample(rigs[i], stance, progress);
                rifles[i].Sample(rigs[i], progress, atRange && i == 4 ? new WeaponAimState(.025, .012) : default, atRange ? stance : (ShootingStance?)null);
                // This fixture's ground is flat; slope-specific evidence is the WP148 fixture.
                contacts[i].Sample(rigs[i], 0, drivers[i].Cycle, drivers[i].State, rifles[i].HandWeight);
            }
            renderedSet = StateSet; hasRendered = true;
        }
        public static float SequenceProgress(int index) => index == 1 ? .2f : index == 2 || index == 5 ? .6f : index == 3 || index == 4 ? 1 : 0;
        public string Label(int index)
        {
            if (StateSet == AthletePreviewSet.Locomotion) return ((SkiLocomotion)index).ToString();
            if (StateSet == AthletePreviewSet.ExertionAndCorners)
                return index == 0 ? "Left turn" : index == 1 ? "Right turn" : index == 2 ? "Fresh" : index == 3 ? "Tired" : index == 4 ? "Tired repeat" : index == 5 ? "Tired sprint" : "Finish fixture";
            return index == 0 ? "Carry" : index == 1 ? "Draw" : index == 2 ? "Lower" : index == 3 ? "Aim" : index == 4 ? "Bolt / sight offset" : index == 5 ? "Exit" : "Carry restored";
        }
        private void Update() { RenderAt(Frozen ? SampleTime : Time.timeAsDouble); }
        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 740, 44), "M11 " + StateSet + " | original development rig | t=" + SampleTime.ToString("F2"));
            var camera = Camera.main;
            if (camera == null || rigs == null) return;
            for (int i = 0; i < rigs.Length; i++)
            {
                var p = camera.WorldToScreenPoint(rigs[i].transform.position + Vector3.up * 2.1f);
                if (p.z > 0) GUI.Box(new Rect(p.x - 70, Screen.height - p.y, 160, 24), Label(i));
            }
        }
    }
}
