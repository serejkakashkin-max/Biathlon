using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum Biathlete167Socket { RifleBack,RifleHand,SkiLeft,SkiRight,PoleLeft,PoleRight }
    public enum Biathlete167HandPoint { Palm,Dorsal,MiddleKnuckle,IndexKnuckle,LittleKnuckle }

    /// <summary>Imported Humanoid visual asset. No race/session or root-motion authority.</summary>
    public sealed class Biathlete167Rig:MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform[] sockets;
        [SerializeField] private Transform rifle;
        [SerializeField] private Renderer[] poleRenderers;
        [SerializeField] private LODGroup lodGroup;
        [SerializeField] private bool naturalAnatomy;
        [SerializeField] private Transform leftGripContact,rightGripContact,leftToeHinge,rightToeHinge;
        [SerializeField] private Transform leftTuckGripContact,rightTuckGripContact;
        [SerializeField] private Transform[] anatomicalHands;
        [SerializeField] private GameObject[] skateGloves,tuckGloves;
        private bool tuckGrip;
        [SerializeField] private TextAsset contactDefinition;
        public TextAsset ContactDefinition=>contactDefinition;
        public bool IsNaturalAnatomy=>naturalAnatomy;
        public Transform HandPoint(bool right,Biathlete167HandPoint point)=>anatomicalHands[(right?5:0)+(int)point];
        public void ConfigureAnatomicalHands(Transform[] frames)
        {
            if(frames==null||frames.Length!=10)throw new InvalidOperationException("Both anatomical hand frames are required.");
            foreach(var frame in frames)if(frame==null||!frame.IsChildOf(transform))throw new InvalidOperationException("Missing anatomical hand point.");
            anatomicalHands=frames;
        }
        public Transform GripContact(bool right)=>tuckGrip?TuckGripContact(right):(right?rightGripContact:leftGripContact);
        public Transform TuckGripContact(bool right)=>right?rightTuckGripContact:leftTuckGripContact;
        public void ConfigureGripVariants(Transform left,Transform right,GameObject[] skating,GameObject[] tucking)
        {leftTuckGripContact=left;rightTuckGripContact=right;skateGloves=skating;tuckGloves=tucking;tuckGrip=true;SetTuckGrip(false);}
        public void SetTuckGrip(bool value)
        {if(tuckGrip==value)return;tuckGrip=value;if(skateGloves!=null)foreach(var glove in skateGloves)glove.SetActive(!value);if(tuckGloves!=null)foreach(var glove in tuckGloves)glove.SetActive(value);}
        public Transform ToeHinge(bool right)=>right?rightToeHinge:leftToeHinge;
        public void ConfigureContacts(Transform leftGrip,Transform rightGrip,Transform leftToe,Transform rightToe,TextAsset definition)
        {leftGripContact=leftGrip;rightGripContact=rightGrip;leftToeHinge=leftToe;rightToeHinge=rightToe;contactDefinition=definition;}
        private Transform[] transforms;
        private Vector3[] bindPositions;
        private Quaternion[] bindRotations;
        private Quaternion rifleCarryRotation;
        private Vector3 rifleCarryPosition;
        public Animator Animator=>animator;
        public Transform Rifle=>rifle;
        public LODGroup Lods=>lodGroup;
        public int PoseIndex {get;private set;}
        public Transform Socket(Biathlete167Socket socket)=>sockets[(int)socket];
        public Transform Bone(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
        public void Configure(Animator human,Transform[] attachments,Transform weapon,Renderer[] poles,LODGroup levels,bool natural=false)
        {animator=human;sockets=attachments;rifle=weapon;poleRenderers=poles;lodGroup=levels;naturalAnatomy=natural;Validate();Remember();}
        public void Validate()
        {
            if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)
                throw new InvalidOperationException("M16.7 requires a validated Humanoid avatar.");
            if(sockets==null||sockets.Length!=6||rifle==null||lodGroup==null)throw new InvalidOperationException("Incomplete M16.7 equipment binding.");
            for(int i=0;i<sockets.Length;i++)
            {if(sockets[i]==null||!sockets[i].IsChildOf(transform))throw new InvalidOperationException("Foreign or missing M16.7 socket.");for(int j=0;j<i;j++)if(sockets[j]==sockets[i])throw new InvalidOperationException("Duplicate M16.7 socket.");}
            animator.applyRootMotion=false;
        }
        private void Awake(){if(animator!=null){Validate();Remember();}}
        private void Remember()
        {
            transforms=animator.GetComponentsInChildren<Transform>();bindPositions=new Vector3[transforms.Length];bindRotations=new Quaternion[transforms.Length];
            for(int i=0;i<transforms.Length;i++){bindPositions[i]=transforms[i].localPosition;bindRotations[i]=transforms[i].localRotation;}
            rifleCarryPosition=Socket(Biathlete167Socket.RifleBack).InverseTransformPoint(rifle.position);
            rifleCarryRotation=Quaternion.Inverse(Socket(Biathlete167Socket.RifleBack).rotation)*rifle.rotation;
        }
        public void SetPose(int pose,bool preserveGrip=false)
        {
            if(pose<0||pose>3)throw new ArgumentOutOfRangeException(nameof(pose));
            if(!preserveGrip)SetTuckGrip(false);
            if(transforms==null)Remember();animator.enabled=false;
            rifle.SetParent(Socket(Biathlete167Socket.RifleBack),false);
            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=bindPositions[i];transforms[i].localRotation=bindRotations[i];}
            rifle.localPosition=rifleCarryPosition;rifle.localRotation=rifleCarryRotation;
            foreach(var r in poleRenderers)r.enabled=pose!=3;
            var leftFoot=Bone(HumanBodyBones.LeftFoot);var rightFoot=Bone(HumanBodyBones.RightFoot);
            var lp=leftFoot.position;var rp=rightFoot.position;var lr=leftFoot.rotation;var rr=rightFoot.rotation;
            if(pose==1)
            {
                Bone(HumanBodyBones.Hips).position-=transform.up*.15f;
                var chest=Bone(HumanBodyBones.Chest);chest.rotation=Quaternion.AngleAxis(16,transform.right)*chest.rotation;
                Limb(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,lp,transform.forward);
                Limb(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,rp,transform.forward);
                leftFoot.rotation=lr;rightFoot.rotation=rr;
            }
            if(pose==2||pose==3)
            {
                foreach(bool right in new[]{false,true})
                {
                    var upper=right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm;
                    var lower=right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm;
                    var hand=right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand;
                    float side=Mathf.Sign(transform.InverseTransformPoint(Bone(hand).position).x);
                    Vector3 target=transform.TransformPoint(pose==2?new Vector3(side*.29f,1.81f,.07f):new Vector3(side*.14f,1.31f,.43f));
                    Limb(upper,lower,hand,target,transform.TransformDirection(new Vector3(side*.6f,-.5f,-.5f)));
                }
            }
            if(pose==3)
            {
                var carry=rifle.rotation;rifle.SetParent(Socket(Biathlete167Socket.RifleHand),true);
                rifle.position=Bone(HumanBodyBones.RightHand).position+transform.forward*.06f;
                rifle.rotation=Quaternion.AngleAxis(90,transform.right)*carry;
            }
            PoseIndex=pose;
        }
        // Shortest-arc solve retains each imported bind axis and both limb lengths.
        private void Limb(HumanBodyBones upperId,HumanBodyBones lowerId,HumanBodyBones endId,Vector3 target,Vector3 hint)
        {
            var upper=Bone(upperId);var lower=Bone(lowerId);var end=Bone(endId);
            var origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
            var delta=target-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(hint,direction).normalized;float along=(a*a-b*b+distance*distance)/(2*distance);
            var elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+direction*distance-lower.position)*lower.rotation;
        }
    }
}
