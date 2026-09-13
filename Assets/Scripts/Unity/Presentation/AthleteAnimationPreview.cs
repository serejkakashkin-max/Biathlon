using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Repeatable visual fixture; it has no race host and never emits gameplay commands.</summary>
    public sealed class AthleteAnimationPreview : MonoBehaviour
    {
        public bool Frozen;
        public double SampleTime;
        private AthleteRigBinding[] rigs;
        private AthleteLocomotionDriver[] drivers;
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
            for (int i = 0; i < rigs.Length; i++)
            {
                rigs[i] = OriginalAthleteRigBuilder.Create(transform);
                rigs[i].transform.localPosition = new Vector3((i - 3) * 2, 0, 0);
                drivers[i] = rigs[i].GetComponent<AthleteLocomotionDriver>();
            }
        }
        public void RenderAt(double time)
        {
            Initialize(); SampleTime = time;
            for (int i = 0; i < rigs.Length; i++) drivers[i].Sample(rigs[i], Input(i), time);
        }
        private void Update() { RenderAt(Frozen ? SampleTime : Time.timeAsDouble); }
        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 650, 44), "M11 locomotion validation | original development rig | t=" + SampleTime.ToString("F2"));
            var camera = Camera.main;
            if (camera == null || rigs == null) return;
            for (int i = 0; i < rigs.Length; i++)
            {
                var p = camera.WorldToScreenPoint(rigs[i].transform.position + Vector3.up * 2.1f);
                if (p.z > 0) GUI.Box(new Rect(p.x - 70, Screen.height - p.y, 160, 24), ((SkiLocomotion)i).ToString());
            }
        }
    }
}
