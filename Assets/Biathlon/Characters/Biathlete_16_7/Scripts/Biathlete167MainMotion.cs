using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Retargets the actual production G2 pose pipeline to the imported athlete.
    /// The donor is invisible and receives synthetic preview inputs only. No race host exists.
    /// Bone directions/angles come from the game's drivers; this class authors no skating cycle.</summary>
    public sealed class Biathlete167MainMotion:IDisposable
    {
        private static readonly HumanBodyBones[] Human={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        private readonly Biathlete167Rig target;
        private readonly Transform root;
        private readonly Transform[] to=new Transform[17],from=new Transform[17];
        private readonly Quaternion[] targetBind=new Quaternion[17],sourceBind=new Quaternion[17];
        private readonly Vector3 hipBind,sourceHipBind;
        private readonly AthleteRiflePresentation rifle;
        private readonly AthleteShootingPose shooting;
        private readonly RiflePresentationSettings rifleSettings;
        private readonly AthleteGroundContact contact;
        private readonly AthleteGripPresentation grip;
        private readonly AthleteEquipmentBinding equipment;
        private readonly Prop leftPole,rightPole;
        private readonly FootContact leftFoot,rightFoot;
        private readonly ContactSettings settings;
        public bool ContactAdaptationEnabled{get;set;}=true;
        public float MaximumWristBendDegrees{get;private set;}
        public float MaximumWristTwistDegrees{get;private set;}
        public float MaximumForearmRollDegrees{get;private set;}
        public float PelvisClearanceLiftMeters{get;private set;}
        public float MaximumBindingResidualMeters{get;private set;}
        public float MaximumHeelRiseDegrees{get;private set;}
        private readonly Renderer[] hiddenRenderers;
        private readonly bool[] referenceRendererStates;
        private bool referenceVisible;
        public AthleteRigBinding SourceRig{get;}
        public AthleteHybridLocomotionDriver SourceDriver{get;}
        public SkiMotionState State{get;set;}=SkiMotionState.Flat;
        public LocomotionInput? PreviewInput{get;set;}
        public float CycleSeconds{get;private set;}
        public float Phase{get;private set;}

        public Biathlete167MainMotion(Biathlete167Rig binding)
        {
            target=binding??throw new ArgumentNullException(nameof(binding));target.Validate();target.SetPose(0);root=target.transform;
            SourceRig=AthleteRigFactory.Create(root.parent);SourceRig.name="Main athlete motion reference";
            SourceRig.transform.SetPositionAndRotation(root.position,root.rotation);
            SourceRig.gameObject.hideFlags=HideFlags.DontSave;
            hiddenRenderers=SourceRig.GetComponentsInChildren<Renderer>(true);
            referenceRendererStates=new bool[hiddenRenderers.Length];
            for(int i=0;i<hiddenRenderers.Length;i++){referenceRendererStates[i]=hiddenRenderers[i].enabled;hiddenRenderers[i].enabled=false;}
            // The factory's runtime components remain callable as ordinary C# objects;
            // no independent Update/LateUpdate is allowed to alter the reference pose.
            foreach(var component in SourceRig.GetComponentsInChildren<MonoBehaviour>(true))component.enabled=false;
            SourceDriver=SourceRig.GetComponent<AthleteHybridLocomotionDriver>();SourceDriver.UseOriginalMotion=false;
            rifle=SourceRig.GetComponent<AthleteRiflePresentation>();contact=SourceRig.GetComponent<AthleteGroundContact>();
            shooting=SourceRig.GetComponent<AthleteShootingPose>();
            rifleSettings=RiflePresentationSettings.Load();
            grip=SourceRig.GetComponent<AthleteGripPresentation>();equipment=SourceRig.GetComponent<AthleteEquipmentBinding>();
            SourceRig.ResetPose();
            for(int i=0;i<17;i++)
            {
                to[i]=target.Bone(Human[i]);from[i]=SourceRig.Bone((AthleteBone)i);
                targetBind[i]=Quaternion.Inverse(root.rotation)*to[i].rotation;
                sourceBind[i]=Quaternion.Inverse(root.rotation)*from[i].rotation;
            }
            hipBind=root.InverseTransformPoint(to[0].position);sourceHipBind=root.InverseTransformPoint(from[0].position);
            settings=JsonUtility.FromJson<ContactSettings>(target.ContactDefinition.text);settings.Validate();
            leftPole=ReadPole(to[7],target.GripContact(false),target.TuckGripContact(false));rightPole=ReadPole(to[10],target.GripContact(true),target.TuckGripContact(true));
            leftFoot=ReadFoot(false,13);rightFoot=ReadFoot(true,16);
            SampleReference(0);CycleSeconds=SourceDriver.CadenceHz>.0001f?1/SourceDriver.CadenceHz:1;target.SetPose(0);
        }

        public void Evaluate(float phase)
        {
            if(!float.IsFinite(phase))throw new ArgumentOutOfRangeException(nameof(phase));
            Phase=Mathf.Repeat(phase,1);target.SetPose(0,true);
            target.SetTuckGrip(ContactAdaptationEnabled&&State==SkiMotionState.Tuck);
            SourceRig.transform.SetPositionAndRotation(root.position,root.rotation);
            var input=PreviewInput??SkiMotionComparison.Input(State);SourceDriver.Sample(SourceRig,input,100,ReferenceExertion());
            CycleSeconds=SourceDriver.CadenceHz>.0001f?1/SourceDriver.CadenceHz:1;SampleReference(Phase*CycleSeconds);
            // Retarget in world directions. The imported bind axes differ from G2;
            // copying local Euler angles would twist the arms and reverse the knees.
            to[0].position=root.TransformPoint(hipBind+root.InverseTransformPoint(from[0].position)-sourceHipBind);
            for(int i=0;i<5;i++)CopyRotation(i);
            CopyLimb(5);CopyLimb(8);CopyLimb(11);CopyLimb(14);
            MaximumWristBendDegrees=MaximumWristTwistDegrees=MaximumForearmRollDegrees=MaximumBindingResidualMeters=MaximumHeelRiseDegrees=PelvisClearanceLiftMeters=0;
            if(ContactAdaptationEnabled)
            {
                if(State==SkiMotionState.Tuck){LiftTuckPelvis();ApplyTuckHand(leftPole,6,7);ApplyTuckHand(rightPole,9,10);}
                else{ApplyPole(leftPole,equipment.LeftPole,6,7);ApplyPole(rightPole,equipment.RightPole,9,10);}
                ApplyFoot(leftFoot);ApplyFoot(rightFoot);
            }
        }

        private void SampleReference(double time)
        {
            for(int i=0;i<hiddenRenderers.Length;i++)hiddenRenderers[i].enabled=referenceRendererStates[i];
            var input=PreviewInput??SkiMotionComparison.Input(State);var exertion=ReferenceExertion();
            // Explicit rewind reset gives reproducible exact-phase studio scrubbing.
            SourceDriver.Sample(SourceRig,input,time+10,exertion);SourceDriver.Sample(SourceRig,input,time,exertion);
            rifle.Sample(SourceRig,0);
            contact.SampleMoving(SourceRig,input.Grade,SourceDriver.PoleCycle,SourceDriver.ContactState,0,time+10);
            contact.SampleMoving(SourceRig,input.Grade,SourceDriver.PoleCycle,SourceDriver.ContactState,0,time);
            SourceDriver.ApplySkiEdges(SourceRig);grip.Render(SourceRig);
            for(int i=0;i<hiddenRenderers.Length;i++){referenceRendererStates[i]=hiddenRenderers[i].enabled;hiddenRenderers[i].enabled=referenceVisible&&referenceRendererStates[i];}
        }

        private ExertionInput ReferenceExertion()=>State==SkiMotionState.Fatigued?new ExertionInput(.7f,.2f,185):new ExertionInput(0,1,140);

        public Vector3 ShootingRiflePosition=>equipment.Rifle.position;
        public Quaternion ShootingRifleRotation=>equipment.Rifle.rotation;
        public Vector3 ShootingRootOffset=>to[0].position-from[0].position;
        public float ShootingTransfer=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(rifleSettings.reach_fraction,rifleSettings.release_end_fraction,rifle.TransferProgress));
        public float ShootingHandWeight=>rifle.HandWeight;
        public float ShootingAlignment=>rifle.ReadyAlignment;
        public Vector3 ShootingPolePosition(bool right)=>(right?equipment.RightPole:equipment.LeftPole).position;
        /// <summary>Use the shipping locomotion/shooting/rifle/contact/grip chain,
        /// then retarget its actual joint directions onto the anatomical hero.</summary>
        public void EvaluateShooting(ShootingStance stance,float progress)
        {
            if(!float.IsFinite(progress)||progress<0||progress>1)throw new ArgumentOutOfRangeException(nameof(progress));
            target.SetPose(0);SourceRig.transform.SetPositionAndRotation(root.position,root.rotation);
            for(int i=0;i<hiddenRenderers.Length;i++)hiddenRenderers[i].enabled=referenceRendererStates[i];
            var input=new LocomotionInput(0,0,0,travelling:false);var exertion=new ExertionInput(0,1,60,shooting:true);
            SourceDriver.Sample(SourceRig,input,10,exertion);SourceDriver.Sample(SourceRig,input,0,exertion);
            shooting.Sample(SourceRig,stance,progress);rifle.Sample(SourceRig,progress,default,stance);
            contact.SampleMoving(SourceRig,0,SourceDriver.PoleCycle,SourceDriver.ContactState,rifle.HandWeight,10);
            contact.SampleMoving(SourceRig,0,SourceDriver.PoleCycle,SourceDriver.ContactState,rifle.HandWeight,0);grip.Render(SourceRig);
            to[0].position=root.TransformPoint(hipBind+root.InverseTransformPoint(from[0].position)-sourceHipBind);
            for(int i=0;i<5;i++)CopyRotation(i);CopyLimb(5);CopyLimb(8);CopyLimb(11);CopyLimb(14);
            MaximumBindingResidualMeters=MaximumHeelRiseDegrees=0;
            ApplyFoot(leftFoot);ApplyFoot(rightFoot);
            for(int i=0;i<hiddenRenderers.Length;i++)hiddenRenderers[i].enabled=referenceVisible&&referenceRendererStates[i];
        }

        private void LiftTuckPelvis()
        {
            // G2's shorter proportions fold the anatomical torso into the
            // thighs. Lift the pelvis while preserving both planted feet.
            var leftPosition=to[13].position;var rightPosition=to[16].position;
            var leftRotation=to[13].rotation;var rightRotation=to[16].rotation;
            var leftHint=to[12].position-(to[11].position+leftPosition)*.5f;
            var rightHint=to[15].position-(to[14].position+rightPosition)*.5f;
            to[0].position+=root.up*settings.tuck_pelvis_clearance_lift_m;
            SolveImportedLimb(to[11],to[12],to[13],leftPosition,leftHint);to[13].rotation=leftRotation;
            SolveImportedLimb(to[14],to[15],to[16],rightPosition,rightHint);to[16].rotation=rightRotation;
            PelvisClearanceLiftMeters=settings.tuck_pelvis_clearance_lift_m;
        }

        public void SetReferenceVisible(bool value)
        {
            referenceVisible=value;
            for(int i=0;i<hiddenRenderers.Length;i++)hiddenRenderers[i].enabled=value&&referenceRendererStates[i];
        }

        private void CopyRotation(int i)
        {
            var source=Quaternion.Inverse(root.rotation)*from[i].rotation;
            to[i].rotation=root.rotation*source*Quaternion.Inverse(sourceBind[i])*targetBind[i];
        }

        private void CopyLimb(int first)
        {
            var upper=to[first];var lower=to[first+1];var end=to[first+2];
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,from[first+1].position-from[first].position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,from[first+2].position-from[first+1].position)*lower.rotation;
            CopyRotation(first+2);
        }

        private Prop ReadPole(Transform hand,Transform contactFrame,Transform tuckFrame)
        {
            var selected=LongestProp(hand);
            return new Prop{Transform=selected,Hand=hand,Contact=contactFrame,TuckContact=tuckFrame,
                TuckGripInHand=hand.InverseTransformPoint(tuckFrame.position),TuckContactInHand=Quaternion.Inverse(hand.rotation)*tuckFrame.rotation,
                ModelOriginFromGrip=contactFrame.InverseTransformPoint(selected.position),ModelRotationFromGrip=Quaternion.Inverse(contactFrame.rotation)*selected.rotation,
                Axis=selected.InverseTransformDirection(contactFrame.up),GripInHand=hand.InverseTransformPoint(contactFrame.position),ContactInHand=Quaternion.Inverse(hand.rotation)*contactFrame.rotation,
                Side=Mathf.Sign(root.InverseTransformPoint(hand.position).x),ForearmAxis=hand.InverseTransformDirection(hand.position-hand.parent.position),
                HandInForearm=Quaternion.Inverse(hand.parent.rotation)*hand.rotation};
        }

        private static Transform LongestProp(Transform parent)
        {
            MeshFilter selected=null;float longest=0;
            foreach(var filter in parent.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null&&filter.sharedMesh.bounds.size.magnitude>longest)
                {selected=filter;longest=filter.sharedMesh.bounds.size.magnitude;}
            if(selected==null)throw new InvalidOperationException("Independent equipment mesh is missing.");
            return selected.transform;
        }

        private void ApplyTuckHand(Prop pole,int lowerIndex,int handIndex)
        {
            // A full anatomical hand frame is required: aligning only the shaft
            // leaves the palm free to roll outward around that shaft.
            float pitch=settings.tuck_pole_elevation_degrees*Mathf.Deg2Rad;
            float splay=settings.tuck_pole_splay_degrees*Mathf.Deg2Rad;
            var direction=root.TransformDirection(new Vector3(pole.Side*Mathf.Sin(splay)*Mathf.Cos(pitch),Mathf.Sin(pitch),-Mathf.Cos(splay)*Mathf.Cos(pitch)));
            float palmRoll=settings.tuck_palm_up_roll_degrees*Mathf.Deg2Rad;
            var palm=pole.Side*root.right*Mathf.Cos(palmRoll)+root.up*Mathf.Sin(palmRoll);
            palm=Vector3.ProjectOnPlane(palm,direction).normalized;
            var contactRotation=Quaternion.LookRotation(Vector3.Cross(palm,direction),direction);
            var handRotation=contactRotation*Quaternion.Inverse(pole.TuckContactInHand);
            var upper=to[lowerIndex-1];var lower=to[lowerIndex];var hand=to[handIndex];
            var shoulder=root.InverseTransformPoint(upper.position);var head=root.InverseTransformPoint(to[4].position);
            var desiredGrip=root.TransformPoint(new Vector3(pole.Side*settings.tuck_grip_half_width_m,shoulder.y-settings.tuck_grip_below_shoulder_m,Mathf.Max(head.z+settings.tuck_grip_forward_of_head_m,shoulder.z+settings.tuck_minimum_reach_forward_m)));
            // The hand anchor is at the upper end of the grip. Contact frames
            // remain at the prop centre so existing mesh/shaft checks stay exact.
            desiredGrip+=direction*settings.tuck_upper_grip_offset_m;
            var desiredWrist=desiredGrip-handRotation*Vector3.Scale(pole.TuckGripInHand,hand.lossyScale);
            // Solve both segment directions to the wrist. Projecting a desired
            // elbow onto a sphere and then forcing a preselected forearm rotation
            // moved the wrist sideways and drove the poles through the torso.
            // Keep the elbow under the body. The authored contact definition
            // is qualified for wrist swing, axial twist and actual shaft/body
            // clearance together; rotating only the hand would move the pole.
            var hint=root.TransformDirection(new Vector3(pole.Side*settings.tuck_elbow_hint_outward,-1,settings.tuck_elbow_hint_forward));
            SolveImportedLimb(upper,lower,hand,desiredWrist,hint);
            // Swing alone cannot detect a hand turned backwards. Carry axial
            // rotation on the forearm, preserving the solved wrist position.
            var forearm=(hand.position-lower.position).normalized;
            var neutral=lower.rotation*pole.HandInForearm;
            float desiredRoll=WristTwist(handRotation,neutral,forearm);
            float roll=Mathf.Clamp(desiredRoll,-settings.maximum_forearm_roll_degrees,settings.maximum_forearm_roll_degrees);
            lower.rotation=Quaternion.AngleAxis(roll,forearm)*lower.rotation;
            hand.rotation=handRotation;
            MaximumForearmRollDegrees=Mathf.Max(MaximumForearmRollDegrees,Mathf.Abs(roll));
            MaximumWristTwistDegrees=Mathf.Max(MaximumWristTwistDegrees,Mathf.Abs(WristTwist(hand.rotation,lower.rotation*pole.HandInForearm,forearm)));
            pole.Transform.SetPositionAndRotation(pole.TuckContact.TransformPoint(pole.ModelOriginFromGrip),pole.TuckContact.rotation*pole.ModelRotationFromGrip);
            MaximumWristBendDegrees=Mathf.Max(MaximumWristBendDegrees,Vector3.Angle(hand.TransformDirection(pole.ForearmAxis),hand.position-lower.position));
        }

        private static float WristTwist(Quaternion hand,Quaternion neutral,Vector3 forearm)
        {
            var delta=hand*Quaternion.Inverse(neutral);
            return Mathf.DeltaAngle(0,2*Mathf.Atan2(Vector3.Dot(new Vector3(delta.x,delta.y,delta.z),forearm),delta.w)*Mathf.Rad2Deg);
        }

        private void ApplyPole(Prop pole,Transform sourcePole,int lowerIndex,int handIndex)
        {
            for(int pass=0;pass<3;pass++)
            {
                OrientHand(pole,sourcePole,lowerIndex,handIndex);
                var grip=root.InverseTransformPoint(pole.Contact.position);var desired=grip;
                float side=pole.Side;
                desired.x=side*Mathf.Max(settings.minimum_grip_half_width_m,grip.x*side);
                var shift=root.TransformVector(desired-grip);
                if(shift.sqrMagnitude<.00000001f)break;
                var upper=to[lowerIndex-1];var lower=to[lowerIndex];var hand=to[handIndex];
                var hint=lower.position-(upper.position+hand.position)*.5f;
                SolveImportedLimb(upper,lower,hand,hand.position+shift,hint);
            }
            OrientHand(pole,sourcePole,lowerIndex,handIndex);
        }

        private void OrientHand(Prop pole,Transform sourcePole,int lowerIndex,int handIndex)
        {
            var forearm=(pole.Hand.position-to[lowerIndex].position).normalized;
            var neutral=to[lowerIndex].rotation*Quaternion.Inverse(root.rotation*targetBind[lowerIndex])*(root.rotation*targetBind[handIndex]);
            pole.Hand.rotation=neutral;
            var current=pole.Transform.TransformDirection(pole.Axis);
            var desired=-sourcePole.up;
            var a=Vector3.ProjectOnPlane(current,forearm);var b=Vector3.ProjectOnPlane(desired,forearm);
            float roll=a.sqrMagnitude>.00001f&&b.sqrMagnitude>.00001f?Vector3.SignedAngle(a,b,forearm):0;
            roll=Mathf.Clamp(roll,-settings.maximum_forearm_roll_degrees,settings.maximum_forearm_roll_degrees);
            // Pronation belongs to the forearm; twisting only the rigid glove
            // would put the entire axial rotation into the wrist seam.
            to[lowerIndex].rotation=Quaternion.AngleAxis(roll,forearm)*to[lowerIndex].rotation;
            neutral=Quaternion.AngleAxis(roll,forearm)*neutral;pole.Hand.rotation=neutral;
            var full=Quaternion.FromToRotation(pole.Transform.TransformDirection(pole.Axis),desired)*neutral;
            pole.Hand.rotation=Quaternion.RotateTowards(neutral,full,settings.maximum_wrist_bend_degrees);
            MaximumForearmRollDegrees=Mathf.Max(MaximumForearmRollDegrees,Mathf.Abs(roll));
            MaximumWristTwistDegrees=Mathf.Max(MaximumWristTwistDegrees,Mathf.Abs(WristTwist(pole.Hand.rotation,to[lowerIndex].rotation*pole.HandInForearm,forearm)));
            MaximumWristBendDegrees=Mathf.Max(MaximumWristBendDegrees,Vector3.Angle(pole.Hand.TransformDirection(pole.ForearmAxis),forearm));
        }

        private FootContact ReadFoot(bool right,int index)
        {
            var foot=to[index];var ski=LongestProp(foot);var toe=target.ToeHinge(right);
            return new FootContact{Index=index,Foot=foot,Ski=ski,Toe=toe,ToeLocal=foot.InverseTransformPoint(toe.position),
                ForwardLocal=ski.InverseTransformDirection(root.forward),UpLocal=ski.InverseTransformDirection(root.up)};
        }

        private void ApplyFoot(FootContact contact)
        {
            var skiPosition=contact.Ski.position;var skiRotation=contact.Ski.rotation;var toe=contact.Toe.position;
            var normal=contact.Ski.TransformDirection(contact.UpLocal);var forward=contact.Ski.TransformDirection(contact.ForwardLocal);
            var lateral=Vector3.Cross(normal,forward).normalized;
            var shin=(to[contact.Index-1].position-contact.Foot.position).normalized;
            float lean=Vector3.SignedAngle(normal,Vector3.ProjectOnPlane(shin,lateral),lateral);
            float rise=Mathf.Clamp(lean-settings.maximum_forward_ankle_degrees,0,settings.maximum_heel_rise_degrees);
            var rotation=Quaternion.AngleAxis(rise,lateral)*contact.Foot.rotation;
            var ankle=toe-rotation*Vector3.Scale(contact.ToeLocal,contact.Foot.lossyScale);
            var upper=to[contact.Index-2];var lower=to[contact.Index-1];var hint=lower.position-(upper.position+contact.Foot.position)*.5f;
            SolveImportedLimb(upper,lower,contact.Foot,ankle,hint);
            contact.Foot.rotation=rotation;
            // A Nordic ski is hinged at the toe, not welded to the ankle.
            contact.Ski.SetPositionAndRotation(skiPosition,skiRotation);
            MaximumBindingResidualMeters=Mathf.Max(MaximumBindingResidualMeters,Vector3.Distance(contact.Toe.position,toe));
            MaximumHeelRiseDegrees=Mathf.Max(MaximumHeelRiseDegrees,rise);
        }

        private static void SolveImportedLimb(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint)
        {
            var origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
            var delta=target-origin;float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.00001f,a+b-.00001f);var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(hint,direction).normalized;
            float along=(a*a-b*b+length*length)/(2*length);
            var joint=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,joint-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+direction*length-lower.position)*lower.rotation;
        }

        public void Stop()=>target.SetPose(0);
        public void Dispose(){if(SourceRig!=null){if(Application.isPlaying)Object.Destroy(SourceRig.gameObject);else Object.DestroyImmediate(SourceRig.gameObject);}}
        private sealed class Prop{public Transform Transform,Hand,Contact,TuckContact;public Vector3 Axis,ForearmAxis,GripInHand,TuckGripInHand,ModelOriginFromGrip;public Quaternion ContactInHand,TuckContactInHand,ModelRotationFromGrip,HandInForearm;public float Side;}
        private sealed class FootContact{public int Index;public Transform Foot,Ski,Toe;public Vector3 ToeLocal,ForwardLocal,UpLocal;}
        [Serializable] private sealed class ContactSettings
        {
            public int schema_version;public string id;
            public float maximum_wrist_bend_degrees,maximum_forearm_roll_degrees,maximum_forward_ankle_degrees,maximum_heel_rise_degrees;
            public float minimum_grip_half_width_m,tuck_grip_forward_of_head_m;
            public float tuck_elbow_hint_outward,tuck_elbow_hint_forward;
            public float tuck_grip_palmar_offset_m;
            public float tuck_pelvis_clearance_lift_m;
            public float tuck_power_grip_obliquity_degrees,tuck_upper_grip_offset_m,tuck_pole_splay_degrees,tuck_grip_half_width_m,tuck_grip_below_shoulder_m,tuck_pole_elevation_degrees,tuck_palm_up_roll_degrees,maximum_tuck_wrist_bend_degrees,tuck_minimum_reach_forward_m;
            public void Validate()
            {
                if(!(tuck_pelvis_clearance_lift_m>=0&&tuck_pelvis_clearance_lift_m<=.1f))throw new InvalidOperationException("Invalid anatomical tuck clearance.");
                ValidateContactFrames();
            }
            private void ValidateContactFrames()
            {if(!(tuck_grip_palmar_offset_m>=.02f&&tuck_grip_palmar_offset_m<=.06f)||!(tuck_elbow_hint_outward>=0&&tuck_elbow_hint_outward<=.3f)||!(tuck_elbow_hint_forward>=-1&&tuck_elbow_hint_forward<=.3f)||!(tuck_power_grip_obliquity_degrees>=5&&tuck_power_grip_obliquity_degrees<=80)||!(tuck_upper_grip_offset_m>=.045f&&tuck_upper_grip_offset_m<=.075f)||!(tuck_pole_splay_degrees>=0&&tuck_pole_splay_degrees<=20)||!(maximum_tuck_wrist_bend_degrees>0&&maximum_tuck_wrist_bend_degrees<=45)||!(tuck_minimum_reach_forward_m>=.12f&&tuck_minimum_reach_forward_m<=.3f)||!(tuck_palm_up_roll_degrees>=0&&tuck_palm_up_roll_degrees<=180)||!(tuck_grip_half_width_m>=.04f&&tuck_grip_half_width_m<=.2f)||!(tuck_grip_below_shoulder_m>=-.08f&&tuck_grip_below_shoulder_m<=.22f)||!(tuck_pole_elevation_degrees>=-5&&tuck_pole_elevation_degrees<=30)||schema_version!=1||string.IsNullOrWhiteSpace(id)||!(minimum_grip_half_width_m>=.1f&&minimum_grip_half_width_m<=.2f)||!(tuck_grip_forward_of_head_m>=.04f&&tuck_grip_forward_of_head_m<=.25f)||!(maximum_wrist_bend_degrees>0&&maximum_wrist_bend_degrees<=30)||!(maximum_forearm_roll_degrees>0&&maximum_forearm_roll_degrees<=90)||!(maximum_forward_ankle_degrees>0&&maximum_forward_ankle_degrees<=25)||!(maximum_heel_rise_degrees>0&&maximum_heel_rise_degrees<=45))throw new InvalidOperationException("Invalid M16.7 contact adaptation definition.");}
        }
    }
}
