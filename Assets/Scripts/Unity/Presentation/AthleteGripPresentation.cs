using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Late visual hand fit to the already-grounded poles. Never moves a pole contact.</summary>
    public sealed class AthleteGripPresentation : MonoBehaviour
    {
        private AthleteEquipmentBinding equipment;
        private AthleteRiflePresentation rifle;
        public float MaximumResidualMeters { get; private set; }
        public void Render(AthleteRigBinding rig)
        {
            if (equipment == null) { equipment=GetComponent<AthleteEquipmentBinding>(); rifle=GetComponent<AthleteRiflePresentation>(); }
            MaximumResidualMeters=0;
            if (rifle != null && rifle.HandWeight>0) return;
            Fit(false,equipment.LeftPole); Fit(true,equipment.RightPole);
            void Fit(bool right,Transform pole)
            {
                var position=pole.position;var rotation=pole.rotation;
                var socket=rig.Socket(right?AthleteSocket.RightHandGrip:AthleteSocket.LeftHandGrip);
                var hand=rig.Bone(right?AthleteBone.RightHand:AthleteBone.LeftHand);
                var upper=rig.Bone(right?AthleteBone.RightUpperArm:AthleteBone.LeftUpperArm);
                var lower=rig.Bone(right?AthleteBone.RightLowerArm:AthleteBone.LeftLowerArm);
                var handRotation=rotation*Quaternion.Inverse(socket.localRotation);
                var target=position-handRotation*socket.localPosition;
                var bend=lower.position-(upper.position+hand.position)*.5f;
                AthleteLocomotionDriver.SolveTwoBone(upper,lower,hand,target,bend,rig.ModelRoot.rotation);
                hand.rotation=handRotation;
                // Equipment remains parented at its socket; preserve the solved world contact.
                pole.SetPositionAndRotation(position,rotation);
                MaximumResidualMeters=Mathf.Max(MaximumResidualMeters,Vector3.Distance(socket.position,position));
            }
        }
    }
}
