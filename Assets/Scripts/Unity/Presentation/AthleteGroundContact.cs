using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class GroundContactSettings
    {
        public int schema_version;
        public string id;
        public float sample_half_span_m, max_grade, pole_length_m, pole_recovery_m, pole_tuck_clearance_m;
        public static GroundContactSettings Load()
        {
            var d = JsonUtility.FromJson<GroundContactSettings>(Resources.Load<TextAsset>("Biathlon/athlete_ground_contact_v1").text);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) ||
                !(d.sample_half_span_m > 0 && d.sample_half_span_m <= 5) || !(d.max_grade > 0 && d.max_grade <= .5f) ||
                !(d.pole_length_m > 1 && d.pole_length_m < 2) || !(d.pole_recovery_m > 0 && d.pole_recovery_m < .5f) ||
                !(d.pole_tuck_clearance_m > 0 && d.pole_tuck_clearance_m < .5f))
                throw new ArgumentException("Invalid ground contact definition.");
            return d;
        }
    }

    /// <summary>Bounded limb/prop corrections against a local presentation plane. No colliders,
    /// race movement, aim adjustment or claims of world-locked pole friction.</summary>
    public sealed class AthleteGroundContact : MonoBehaviour
    {
        private GroundContactSettings settings;
        private AthleteLocomotionSettings motion;
        private AthleteEquipmentBinding equipment;
        private AthleteLocomotionDriver driver;
        private AthleteRiflePresentation rifle;
        public Vector3 SurfaceNormal { get; private set; }
        public float MaxFootResidualMeters { get; private set; }
        public Vector3 LeftPoleTip { get; private set; }
        public Vector3 RightPoleTip { get; private set; }

        public void Render(AthleteRigBinding rig, AthleteAnimationFrame frame)
        {
            if (driver == null) driver = GetComponent<AthleteLocomotionDriver>();
            if (rifle == null) rifle = GetComponent<AthleteRiflePresentation>();
            Sample(rig, (float)frame.PresentationGrade, driver != null ? driver.Cycle : 0,
                driver != null ? driver.State : SkiLocomotion.Idle, rifle != null ? rifle.HandWeight : 0);
        }

        public void Sample(AthleteRigBinding rig, float grade, double cycle, SkiLocomotion state, float stowWeight)
        {
            if (!float.IsFinite(grade) || !double.IsFinite(cycle) || !float.IsFinite(stowWeight) || stowWeight < 0 || stowWeight > 1) throw new ArgumentOutOfRangeException();
            if (settings == null) { settings = GroundContactSettings.Load(); motion = AthleteLocomotionSettings.Load(); equipment = GetComponent<AthleteEquipmentBinding>(); equipment.Validate(); }
            var root = rig.ModelRoot;
            grade = Mathf.Clamp(grade, -settings.max_grade, settings.max_grade);
            var normal = new Vector3(0, 1, -grade).normalized;
            SurfaceNormal = root.TransformDirection(normal);
            MaxFootResidualMeters = 0;
            Foot(false); Foot(true);
            LeftPoleTip = Pole(equipment.LeftPole, 0);
            RightPoleTip = Pole(equipment.RightPole, .5f);

            void Foot(bool right)
            {
                var upper = rig.Bone(right ? AthleteBone.RightUpperLeg : AthleteBone.LeftUpperLeg);
                var lower = rig.Bone(right ? AthleteBone.RightLowerLeg : AthleteBone.LeftLowerLeg);
                var foot = rig.Bone(right ? AthleteBone.RightFoot : AthleteBone.LeftFoot);
                var local = root.InverseTransformPoint(foot.position);
                float lift = Mathf.Max(0, local.y - motion.ankle_height_m);
                var target = new Vector3(local.x, grade * local.z, local.z) + normal * (motion.ankle_height_m + lift);
                // Retain the existing knee bend, especially prone knees; do not switch IK branch.
                var hint = lower.position - (upper.position + foot.position) * .5f;
                var direction = Vector3.ProjectOnPlane(foot.forward, SurfaceNormal).normalized;
                AthleteLocomotionDriver.SolveTwoBone(upper, lower, foot, root.TransformPoint(target), hint, root.rotation);
                foot.rotation = Quaternion.LookRotation(direction, SurfaceNormal);
                MaxFootResidualMeters = Mathf.Max(MaxFootResidualMeters, Vector3.Distance(foot.position, root.TransformPoint(target)));
            }
            Vector3 Pole(Transform pole, float offset)
            {
                var grip = root.InverseTransformPoint(pole.position);
                // Staged poles stand on this plane too, including a sloped validation fixture.
                var onPlane = grip - normal * Vector3.Dot(grip, normal);
                grip = Vector3.Lerp(grip, onPlane + normal * settings.pole_length_m, stowWeight);
                float wave = Mathf.Max(0, Mathf.Cos((float)((cycle + offset) * Math.PI * 2)));
                float lift = state == SkiLocomotion.Tuck || state == SkiLocomotion.TechnicalDescent ? settings.pole_tuck_clearance_m :
                    state == SkiLocomotion.Idle ? 0 : settings.pole_recovery_m * wave * wave;
                lift *= 1 - stowWeight;
                float height = Mathf.Clamp(Vector3.Dot(grip, normal) - lift, 0, settings.pole_length_m);
                var rear = Vector3.ProjectOnPlane(Vector3.back, normal).normalized;
                var tip = grip - normal * height + rear * Mathf.Sqrt(Mathf.Max(0, settings.pole_length_m * settings.pole_length_m - height * height));
                pole.SetPositionAndRotation(root.TransformPoint(grip), Quaternion.FromToRotation(Vector3.down, root.TransformDirection(tip - grip)));
                return root.TransformPoint(tip);
            }
        }
    }
}
