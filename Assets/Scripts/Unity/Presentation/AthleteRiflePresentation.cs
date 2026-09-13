using System;
using Biathlon.Simulation;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class RiflePresentationSettings
    {
        public int schema_version;
        public string id;
        public float reach_fraction, release_end_fraction, clearance_side_m, clearance_up_m;
        public Vector3 trigger_grip, support_grip, prone_support_grip, pole_stow_left, pole_stow_right;
        public Vector3 standing_support_bend, standing_trigger_bend, prone_support_bend, prone_trigger_bend;
        public Vector3 cheek_rest, head_from_cheek;
        public float max_head_correction_m;
        public static RiflePresentationSettings Load()
        {
            var d = JsonUtility.FromJson<RiflePresentationSettings>(Resources.Load<TextAsset>("Biathlon/athlete_rifle_v1").text);
            if (d == null || d.schema_version != 1 || string.IsNullOrWhiteSpace(d.id) || !(d.reach_fraction > 0 && d.reach_fraction < d.release_end_fraction && d.release_end_fraction < 1) ||
                !(d.clearance_side_m > 0 && d.clearance_side_m < 1) || !(d.clearance_up_m > 0 && d.clearance_up_m < 1))
                throw new ArgumentException("Invalid rifle presentation definition.");
            if(!(d.max_head_correction_m>0&&d.max_head_correction_m<=.15f))throw new ArgumentException("Invalid head correction envelope.");
            foreach (var v in new[] {d.trigger_grip, d.support_grip, d.prone_support_grip, d.pole_stow_left, d.pole_stow_right,d.standing_support_bend,d.standing_trigger_bend,d.prone_support_bend,d.prone_trigger_bend,d.cheek_rest,d.head_from_cheek})
                if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)) throw new ArgumentException("Invalid prop offset.");
            return d;
        }
    }

    /// <summary>Prop and hands only. Read-only phase timing cannot delay an authoritative shot.</summary>
    public sealed class AthleteRiflePresentation : MonoBehaviour
    {
        private AthleteEquipmentBinding equipment;
        private RiflePresentationSettings settings;
        public float TransferProgress { get; private set; }
        public float HandWeight { get; private set; }

        public static float Progress(ShootingPhase phase, int elapsedTicks, int durationTicks)
        {
            if (elapsedTicks < 0 || durationTicks < 0 || elapsedTicks > durationTicks) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            switch (phase)
            {
                case ShootingPhase.Setup: return durationTicks == 0 ? 1 : (float)elapsedTicks / durationTicks;
                case ShootingPhase.Aiming: case ShootingPhase.BoltCycle: return 1;
                case ShootingPhase.Exiting: return durationTicks == 0 ? 0 : 1 - (float)elapsedTicks / durationTicks;
                default: return 0;
            }
        }

        public void Render(AthleteRigBinding rig, AthleteAnimationFrame frame)
        {
            var a = frame.Athlete;
            Sample(rig, Progress(a.ShootingPhase, a.ShootingPhaseElapsedTicks, a.ShootingPhaseDurationTicks),a.Sight,a.Stance);
        }

        public void Sample(AthleteRigBinding rig, float progress, WeaponAimState sight = default, ShootingStance? stance = null)
        {
            if (!float.IsFinite(progress) || progress < 0 || progress > 1) throw new ArgumentOutOfRangeException(nameof(progress));
            if (equipment == null) { equipment = GetComponent<AthleteEquipmentBinding>(); equipment.Validate(); }
            if (settings == null) settings = RiflePresentationSettings.Load();
            TransferProgress = progress;
            var back = rig.Socket(AthleteSocket.RifleBack); var ready = rig.Socket(AthleteSocket.RifleAim);
            float transfer = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(settings.reach_fraction, settings.release_end_fraction, progress));
            // Swing around the athlete's right side instead of passing the stock through the thorax.
            var clearance = rig.ModelRoot.TransformVector(new Vector3(settings.clearance_side_m, settings.clearance_up_m, 0)) * Mathf.Sin(transfer * Mathf.PI);
            float alignment=Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.release_end_fraction,1,progress));
            var ray=sight.Ray;
            var sightRotation=rig.ModelRoot.rotation*Quaternion.LookRotation(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ),Vector3.up);
            var readyRotation=Quaternion.Slerp(ready.rotation,sightRotation,alignment);
            equipment.Rifle.SetPositionAndRotation(Vector3.Lerp(back.position, ready.position, transfer) + clearance,
                Quaternion.Slerp(back.rotation, readyRotation, transfer));
            if(stance.HasValue&&alignment>0)
            {
                var head=rig.Bone(AthleteBone.Head);
                var desired=equipment.Rifle.TransformPoint(settings.cheek_rest+settings.head_from_cheek);
                head.position+=Vector3.ClampMagnitude(desired-head.position,settings.max_head_correction_m)*alignment;
                head.rotation=Quaternion.Slerp(head.rotation,sightRotation,alignment);
            }
            HandWeight = Mathf.SmoothStep(0, 1, progress / settings.reach_fraction);
            // Props keep their asset parents; restore from typed sockets each sample without accumulation.
            if (HandWeight > 0)
            {
                Hand(false, Vector3.Lerp(settings.support_grip,stance==ShootingStance.Prone?settings.prone_support_grip:settings.support_grip,alignment));
                Hand(true, settings.trigger_grip);
            }
            Pole(equipment.LeftPole, AthleteSocket.LeftHandGrip, settings.pole_stow_left);
            Pole(equipment.RightPole, AthleteSocket.RightHandGrip, settings.pole_stow_right);
            void Hand(bool right, Vector3 grip)
            {
                var upper = rig.Bone(right ? AthleteBone.RightUpperArm : AthleteBone.LeftUpperArm);
                var lower = rig.Bone(right ? AthleteBone.RightLowerArm : AthleteBone.LeftLowerArm);
                var hand = rig.Bone(right ? AthleteBone.RightHand : AthleteBone.LeftHand);
                // Grip offsets address the middle of the original glove, 56 mm below its wrist.
                var target = equipment.Rifle.TransformPoint(grip) + rig.ModelRoot.up * .056f;
                var before = hand.position;
                float weight = right ? HandWeight : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.5f, settings.release_end_fraction, progress));
                Vector3 bend=new Vector3(right?1:-1,-.3f,-.5f);
                if(stance.HasValue)
                {
                    var shootingBend=stance==ShootingStance.Prone?(right?settings.prone_trigger_bend:settings.prone_support_bend):(right?settings.standing_trigger_bend:settings.standing_support_bend);
                    bend=Vector3.Slerp(bend,shootingBend,alignment);
                }
                AthleteLocomotionDriver.SolveTwoBone(upper, lower, hand, Vector3.Lerp(before, target, weight),
                    rig.ModelRoot.TransformDirection(bend), rig.ModelRoot.rotation);
                hand.rotation = Quaternion.Slerp(hand.rotation, rig.ModelRoot.rotation, weight);
            }
            void Pole(Transform pole, AthleteSocket socket, Vector3 stow)
            {
                var held = rig.Socket(socket);
                pole.SetPositionAndRotation(Vector3.Lerp(held.position, rig.ModelRoot.TransformPoint(stow), HandWeight),
                    Quaternion.Slerp(held.rotation * Quaternion.Euler(47, 0, 0), rig.ModelRoot.rotation, HandWeight));
            }
        }
    }
}
