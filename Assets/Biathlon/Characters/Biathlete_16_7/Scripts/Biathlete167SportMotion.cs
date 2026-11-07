using System;
using System.Collections.Generic;
using Biathlon.Simulation;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    public enum Biathlete167Action { Skate,Acceleration,Uphill,RangeEntry,Standing,Prone,EnterStanding,EnterProne }

    /// <summary>Isolated sports choreography. Explicit time, fixed root, no race or shot authority.</summary>
    public sealed class Biathlete167SportMotion:IDisposable
    {
        [Serializable] private sealed class Definition
        {
            public int schema_version;
            public int hand_fit_iterations,arm_clearance_angles;
            public string id,provenance;
            public float acceleration_duration_s,range_entry_duration_s,standing_transition_duration_s,prone_transition_duration_s,breathing_cycle_s,breathing_displacement_m,flat_transfer_m,sprint_transfer_m,uphill_transfer_m,start_speed_mps,sprint_speed_mps,uphill_speed_mps,uphill_grade,deceleration_start_speed_mps,rifle_hidden_harness_fraction,loose_harness_sag_m,loose_harness_width_m,loose_harness_thickness_m,maximum_range_wrist_bend_degrees,rifle_transfer_side_m,rifle_transfer_up_m,rifle_transfer_forward_m;
            public float eyewear_raise_start,eyewear_raise_end,eyewear_touch_start,eyewear_touch_peak,eyewear_release_start,eyewear_release_end,poles_drop_start,poles_drop_end;
            public float rifle_turn_start,rifle_turn_end,support_hand_start,support_hand_end;
            public float forearm_radius_m,wrist_radius_m,upperarm_radius_m;
            public float setup_pose_blend_end;
            public float body_volume_scale,body_volume_padding_m;
            public float free_hand_minimum_height_m;
            public float rifle_transfer_first_side_m;
            public float rifle_reach_wrist_side_m,arm_guide_bias,arm_free_guide_bias;
            public float maximum_forearm_pronation_degrees,maximum_wrist_twist_degrees;
            public Vector3 left_pole_lay_direction,right_pole_lay_direction;
            public Vector3 resting_elbow_hint;
            public Vector3 coast_wrist_offset;
            public float rifle_reach_lift_m,rifle_reach_front_m,rifle_reach_settle_weight;
            public float rifle_hand_reach_end,rifle_removal_start,rifle_removal_end;
            public float reaching_elbow_roll_degrees,shooting_elbow_roll_degrees;
            public Vector3[] standing_right_elbow_curve,prone_right_elbow_curve;
            public Vector3 left_free_hand_from_shoulder,right_free_hand_from_shoulder,rifle_reach_elbow_hint;
            public Biathlete167SkiStance standing_ski_stance,prone_ski_stance;
            public Vector3 rear_aperture_local;
            public float sight_height_settle_start,sight_height_settle_end;
            public Vector3 prone_neck_settle_degrees,prone_shoulder_settle_m;
            public Vector3 standing_left_elbow_hint,standing_right_elbow_hint,prone_left_elbow_hint,rifle_butt_local,rifle_trigger_local,rifle_support_local,left_pole_stow,right_pole_stow,prone_support_local,loose_harness_upper,loose_harness_lower,standing_shoulder_pocket,prone_shoulder_pocket,rifle_reach_local,right_grip_hint_root,right_grip_hint_rifle;
            public Vector3 eyewear_pivot,glasses_raise_offset,mask_raise_offset,glasses_raise_euler,mask_raise_euler,eyewear_touch_point;
            private static bool Span(float a,float b)=>float.IsFinite(a)&&float.IsFinite(b)&&a>=0&&a<b&&b<=1;
            public void ValidateRange()
            {
                if(hand_fit_iterations<2||hand_fit_iterations>64||arm_clearance_angles<8||arm_clearance_angles>96||!Span(rifle_turn_start,rifle_turn_end)||!Span(support_hand_start,support_hand_end)||!Span(eyewear_raise_start,eyewear_raise_end)||!Span(eyewear_touch_start,eyewear_touch_peak)||!Span(eyewear_release_start,eyewear_release_end)||!Span(poles_drop_start,poles_drop_end)||!float.IsFinite(setup_pose_blend_end)||setup_pose_blend_end<=0||setup_pose_blend_end>.3f||!(wrist_radius_m>0&&wrist_radius_m<=forearm_radius_m&&forearm_radius_m<=upperarm_radius_m&&upperarm_radius_m<.15f))throw new ArgumentException("Invalid hero range fitting/timing definition.");
                if(standing_ski_stance==null||prone_ski_stance==null)throw new ArgumentException("Shooting ski stances missing.");standing_ski_stance.Validate();prone_ski_stance.Validate();
                if(!Span(0,rifle_hand_reach_end)||!Span(rifle_removal_start,rifle_removal_end)||rifle_hand_reach_end>rifle_removal_start||!(body_volume_scale>=1&&body_volume_scale<=1.5f)||!(body_volume_padding_m>=0&&body_volume_padding_m<.1f)||!(arm_guide_bias>0&&arm_guide_bias<=.1f)||!(arm_free_guide_bias>0&&arm_free_guide_bias<=.1f)||!(free_hand_minimum_height_m>=0&&free_hand_minimum_height_m<.3f))throw new ArgumentException("Invalid anatomical fitting or handover definition.");
                foreach(var v in new[]{left_free_hand_from_shoulder,right_free_hand_from_shoulder,rifle_reach_elbow_hint})if(!float.IsFinite(v.sqrMagnitude))throw new ArgumentException("Non-finite arm guide.");
                if(!(maximum_forearm_pronation_degrees>0&&maximum_forearm_pronation_degrees<=180&&maximum_wrist_twist_degrees>=0&&maximum_wrist_twist_degrees<=20&&180-maximum_forearm_pronation_degrees<=maximum_wrist_twist_degrees))throw new ArgumentException("Invalid forearm/wrist rotation distribution.");
                foreach(var v in new[]{left_pole_lay_direction,right_pole_lay_direction})if(!float.IsFinite(v.sqrMagnitude)||v.sqrMagnitude<.5f||v.y>0)throw new ArgumentException("Invalid downward pole lay direction.");
                if(!float.IsFinite(resting_elbow_hint.sqrMagnitude)||resting_elbow_hint.sqrMagnitude<.1f||resting_elbow_hint.x<0||resting_elbow_hint.y>=0)throw new ArgumentException("Invalid resting elbow direction.");
                if(!float.IsFinite(coast_wrist_offset.sqrMagnitude)||coast_wrist_offset.sqrMagnitude>.09f)throw new ArgumentException("Invalid coast wrist offset.");
                if(!float.IsFinite(rifle_reach_lift_m)||rifle_reach_lift_m<0||rifle_reach_lift_m>.7f||!float.IsFinite(rifle_reach_front_m)||rifle_reach_front_m<0||rifle_reach_front_m>.4f)throw new ArgumentException("Invalid overhead rifle reach arc.");
                if(!float.IsFinite(rifle_reach_settle_weight)||rifle_reach_settle_weight<.8f||rifle_reach_settle_weight>=1)throw new ArgumentException("Invalid rifle reach contact settling.");
                if(!Span(sight_height_settle_start,sight_height_settle_end)||!float.IsFinite(rear_aperture_local.sqrMagnitude)||rear_aperture_local.sqrMagnitude<.01f)throw new ArgumentException("Invalid pupil / aperture height fitting definition.");
                if (!float.IsFinite(prone_neck_settle_degrees.sqrMagnitude) || prone_neck_settle_degrees.sqrMagnitude > 30f * 30f ||
                    !float.IsFinite(prone_shoulder_settle_m.sqrMagnitude) || prone_shoulder_settle_m.sqrMagnitude > .05f * .05f)
                    throw new ArgumentException("Invalid prone aim settling pose.");
                foreach(var curve in new[]{standing_right_elbow_curve,prone_right_elbow_curve})
                {if(curve==null||curve.Length<2||curve.Length>4097)throw new ArgumentException("Baked right elbow guide missing.");foreach(var v in curve)if(!float.IsFinite(v.sqrMagnitude)||v.sqrMagnitude<.0001f||v.sqrMagnitude>1)throw new ArgumentException("Invalid baked right elbow guide.");}
            }
        }
        private static readonly HumanBodyBones[] BoneIds={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        private readonly Biathlete167Rig rig;private readonly Biathlete167MainMotion main;private readonly Definition d;private readonly SkiMotionSettings ski;
        private readonly Transform root;private readonly Transform[] bones=new Transform[17];private readonly Quaternion[] savedRotations=new Quaternion[17];
        private readonly Vector3[] ends=new Vector3[4];private readonly Quaternion[] endRotations=new Quaternion[4];
        private readonly Prop[] poles=new Prop[2];private readonly Prop rifle;
        private readonly Prop[] eyewear=new Prop[2];private readonly Quaternion headNeutral;
        private readonly Vector3[] handGrip=new Vector3[2];private readonly Quaternion[] gripInHand=new Quaternion[2];
        private readonly Vector3[] forearmInHand=new Vector3[2];
        private readonly Quaternion[] handInForearm=new Quaternion[2];
        private readonly Vector3[] poleReleasePositions=new Vector3[2];private readonly Quaternion[] poleReleaseRotations=new Quaternion[2];
        private readonly SkinnedMeshRenderer[] skins;private readonly Material[][] visibleMaterials,hiddenMaterials;private readonly Material[] hiddenHarness;
        private readonly Biathlete167Outfit outfit;private string materialNation;
        private readonly List<Material> materialScratch=new List<Material>(64);
        private bool hidden,configured;
        private readonly GameObject looseHarness;private readonly Mesh looseMesh;private readonly Vector3[] looseVertices;
        private readonly Vector3[] looseAnchors=new Vector3[4];private const int HarnessSegments=24;
        private readonly Bounds stockBounds,barrelBounds;private Matrix4x4 rifleInverse;
        private readonly Biathlete167BodyClearance bodyClearance;
        private readonly Biathlete167ShootingSkis shootingSkis;
        private readonly Biathlete167EyeAnchor eyeAnchor;
        // The offline studio baker sets this by reflection. Playback samples
        // the authored curves, so scrubbing never depends on a previous frame.
        private Vector3? authoringRightElbowGuide=null,rightElbowGuide;
        public float MaximumShootingBindingResidualMeters=>shootingSkis.MaximumBindingResidualMeters;
        private readonly Quaternion[][] setupStart={new Quaternion[17],new Quaternion[17]};private readonly Vector3[] setupHips=new Vector3[2];
        public Biathlete167Action Action{get;private set;}
        public float Phase{get;private set;}
        public float SpeedMetersPerSecond{get;private set;}
        public float MaximumReachResidualMeters{get;private set;}
        public int WorstReachLimb{get;private set;}
        public float MaximumWristBendDegrees{get;private set;}
        public float MaximumWristTwistDegrees{get;private set;}
        public float MaximumForearmPronationDegrees{get;private set;}
        public float MaximumPalmFitResidualMeters{get;private set;}
        public float RifleTransfer{get;private set;}
        public float EyewearLift{get;private set;}
        public Vector3 SupportContact{get;private set;}
        public Vector3 TriggerContact{get;private set;}
        public float DurationSeconds=>Action==Biathlete167Action.Acceleration?d.acceleration_duration_s:Action==Biathlete167Action.RangeEntry?d.range_entry_duration_s:Action==Biathlete167Action.EnterStanding?d.standing_transition_duration_s:Action==Biathlete167Action.EnterProne?d.prone_transition_duration_s:Action==Biathlete167Action.Standing||Action==Biathlete167Action.Prone?d.breathing_cycle_s:main.CycleSeconds;
        public bool Loops=>Action==Biathlete167Action.Skate||Action==Biathlete167Action.Uphill||Action==Biathlete167Action.Standing||Action==Biathlete167Action.Prone;
        public Biathlete167SportMotion(Biathlete167Rig target,Biathlete167MainMotion reference)
        {
            rig=target??throw new ArgumentNullException(nameof(target));main=reference??throw new ArgumentNullException(nameof(reference));root=rig.transform;
            if(rig.PerformanceDefinition==null||rig.PerformanceLocomotion==null)throw new InvalidOperationException("Authored sport motion definitions missing.");
            d=JsonUtility.FromJson<Definition>(rig.PerformanceDefinition.text);ski=SkiMotionSettings.FromJson(rig.PerformanceLocomotion.text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.acceleration_duration_s<=0||d.prone_transition_duration_s<=0||d.maximum_range_wrist_bend_degrees<=0||d.maximum_range_wrist_bend_degrees>35||d.loose_harness_sag_m<=0)throw new ArgumentException("Invalid sports choreography.");
            d.ValidateRange();
            rig.SetPose(0);
            for(int i=0;i<bones.Length;i++)bones[i]=rig.Bone(BoneIds[i]);
            shootingSkis=new Biathlete167ShootingSkis(rig);
            headNeutral=Quaternion.Inverse(root.rotation)*bones[4].rotation;
            for(int i=0;i<2;i++)eyewear[i]=ReadProp(rig.EyewearRoot((Biathlete167Eyewear)i),null);
            rifle=ReadProp(rig.Rifle,null);
            for(int i=0;i<2;i++)
            {
                var hand=bones[i==0?7:10];var frame=rig.GripContact(i==1);handGrip[i]=hand.InverseTransformPoint(frame.position);gripInHand[i]=Quaternion.Inverse(hand.rotation)*frame.rotation;
                forearmInHand[i]=hand.InverseTransformDirection(hand.position-hand.parent.position);
                handInForearm[i]=Quaternion.Inverse(hand.parent.rotation)*hand.rotation;
                poles[i]=ReadProp(Longest(hand),frame);
                poles[i].RootRotation=Quaternion.Inverse(root.rotation)*poles[i].Transform.rotation;
            }
            skins=Array.ConvertAll(rig.Lods.GetLODs(),lod=>Array.Find(lod.renderers,r=>r is SkinnedMeshRenderer)as SkinnedMeshRenderer);
            eyeAnchor=new Biathlete167EyeAnchor(rig,skins[0]);
            bodyClearance=new Biathlete167BodyClearance(rig,skins[0],d.body_volume_scale,d.body_volume_padding_m);
            outfit=rig.GetComponent<Biathlete167Outfit>();materialNation=outfit!=null?outfit.Nation:null;
            visibleMaterials=new Material[skins.Length][];hiddenMaterials=new Material[skins.Length][];hiddenHarness=new Material[2];
            string[] names={"PULSE_Rifle_Carry_Webbing","PULSE_Rifle_Harness_Suspension"};
            for(int i=0;i<skins.Length;i++)
            {
                visibleMaterials[i]=skins[i].sharedMaterials;hiddenMaterials[i]=(Material[])visibleMaterials[i].Clone();
                for(int j=0;j<hiddenMaterials[i].Length;j++)for(int k=0;k<names.Length;k++)if(hiddenMaterials[i][j].name==names[k])
                {
                    if(hiddenHarness[k]==null){hiddenHarness[k]=new Material(hiddenMaterials[i][j]){name=names[k]+"_OffShoulder",hideFlags=HideFlags.DontSave};hiddenHarness[k].SetFloat("_AlphaClip",1);hiddenHarness[k].EnableKeyword("_ALPHATEST_ON");hiddenHarness[k].SetColor("_BaseColor",Color.clear);}
                    hiddenMaterials[i][j]=hiddenHarness[k];
                }
            }
            looseHarness=new GameObject("Removed rifle carrying harness");looseHarness.transform.SetParent(rig.Rifle,false);looseHarness.hideFlags=HideFlags.DontSave;
            looseMesh=new Mesh{name="Authored loose carrying tapes",hideFlags=HideFlags.DontSave};looseMesh.MarkDynamic();looseVertices=new Vector3[2*(HarnessSegments+1)*4];
            var uv=new Vector2[looseVertices.Length];var triangles=new int[2*(HarnessSegments*24+12)];int cursor=0;
            for(int side=0;side<2;side++)
            {
                int start=side*(HarnessSegments+1)*4;
                for(int row=0;row<=HarnessSegments;row++)
                {
                    for(int corner=0;corner<4;corner++)uv[start+row*4+corner]=new Vector2(corner==1||corner==2?1:0,row/(float)HarnessSegments);
                    if(row<HarnessSegments)for(int edge=0;edge<4;edge++)
                    {int a=start+row*4+edge,b=start+row*4+(edge+1)%4,c=b+4,e=a+4;triangles[cursor++]=a;triangles[cursor++]=b;triangles[cursor++]=c;triangles[cursor++]=a;triangles[cursor++]=c;triangles[cursor++]=e;}
                }
                foreach(int i in new[]{0,2,1,0,3,2,HarnessSegments*4,HarnessSegments*4+1,HarnessSegments*4+2,HarnessSegments*4,HarnessSegments*4+2,HarnessSegments*4+3})triangles[cursor++]=start+i;
            }
            looseMesh.vertices=looseVertices;looseMesh.uv=uv;looseMesh.triangles=triangles;
            looseHarness.AddComponent<MeshFilter>().sharedMesh=looseMesh;var renderer=looseHarness.AddComponent<MeshRenderer>();renderer.sharedMaterial=Array.Find(visibleMaterials[0],m=>m.name=="PULSE_Rifle_Carry_Webbing");
            var rifleMesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;int wood=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="PULSE_Natural_Rifle_Walnut");var woodIndices=rifleMesh.GetTriangles(wood);var woodVertices=rifleMesh.vertices;
            stockBounds=new Bounds(woodVertices[woodIndices[0]],Vector3.zero);foreach(int index in woodIndices)stockBounds.Encapsulate(woodVertices[index]);
            int barrel=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="PULSE_Rifle_Barrel_Satin");var barrelIndices=rifleMesh.GetTriangles(barrel);barrelBounds=new Bounds(woodVertices[barrelIndices[0]],Vector3.zero);foreach(int index in barrelIndices)barrelBounds.Encapsulate(woodVertices[index]);
            for(int side=0;side<2;side++)for(int end=0;end<2;end++)
            {
                var query=end==0?d.loose_harness_upper:d.loose_harness_lower;query.y+=side==0?-.025f:.025f;float best=float.PositiveInfinity;
                foreach(int index in woodIndices){float distance=(woodVertices[index]-query).sqrMagnitude;if(distance<best){best=distance;looseAnchors[side*2+end]=woodVertices[index];}}
            }
            looseHarness.SetActive(false);
            for(int stance=0;stance<2;stance++)
            {main.EvaluateShooting(stance==0?Biathlon.Domain.ShootingStance.Standing:Biathlon.Domain.ShootingStance.Prone,0);setupHips[stance]=root.InverseTransformPoint(bones[0].position);for(int i=0;i<bones.Length;i++)setupStart[stance][i]=Quaternion.Inverse(root.rotation)*bones[i].rotation;}
            rig.SetPose(0);
        }
        public void Evaluate(Biathlete167Action action,float normalizedPhase)
        {
            if(action<Biathlete167Action.Skate||action>Biathlete167Action.EnterProne||!float.IsFinite(normalizedPhase)||normalizedPhase<0||normalizedPhase>1)throw new ArgumentOutOfRangeException(nameof(normalizedPhase));
            Action=action;Phase=normalizedPhase;MaximumReachResidualMeters=MaximumWristBendDegrees=MaximumWristTwistDegrees=MaximumForearmPronationDegrees=MaximumPalmFitResidualMeters=0;RifleTransfer=EyewearLift=0;RestoreProps();
            if(!configured){main.SourceDriver.Configure(ski);configured=true;}
            if(action==Biathlete167Action.Skate||action==Biathlete167Action.Acceleration||action==Biathlete167Action.Uphill)
            {CarryVisible(true);Travel(action,normalizedPhase);return;}
            if(action==Biathlete167Action.RangeEntry){CarryVisible(true);Approach(normalizedPhase);return;}
            main.PreviewInput=null;main.State=SkiMotionState.SlowStop;main.Evaluate(0);SpeedMetersPerSecond=0;
            bool prone=action==Biathlete167Action.Prone||action==Biathlete167Action.EnterProne;
            float progress=action==Biathlete167Action.Standing||action==Biathlete167Action.Prone?1:normalizedPhase;
            RangePose(prone,progress,normalizedPhase);
        }
        private void Travel(Biathlete167Action action,float phase)
        {
            bool sprint=action==Biathlete167Action.Acceleration,uphill=action==Biathlete167Action.Uphill;
            SpeedMetersPerSecond=sprint?Mathf.Lerp(d.start_speed_mps,d.sprint_speed_mps,Smooth(phase)):uphill?d.uphill_speed_mps:6;
            main.State=sprint?SkiMotionState.MaxSprint:uphill?SkiMotionState.Uphill:SkiMotionState.Flat;
            main.PreviewInput=new LocomotionInput(SpeedMetersPerSecond,uphill?d.uphill_grade:0,sprint?1.2f:.9f,sprint:sprint);
            float cycle=sprint?d.acceleration_duration_s*(phase*.8f+phase*phase*.22f):phase;
            main.Evaluate(cycle);
            CaptureEnds();
            float amplitude=sprint?Mathf.Lerp(d.flat_transfer_m,d.sprint_transfer_m,phase):uphill?d.uphill_transfer_m:d.flat_transfer_m;
            float transfer=-Mathf.Sin((float)main.SourceDriver.Cycle*Mathf.PI*2)*amplitude;
            bones[0].position+=root.right*transfer;
            RestoreEnds();
        }
        private void Approach(float phase)
        {
            SpeedMetersPerSecond=Mathf.Lerp(d.deceleration_start_speed_mps,0,Smooth(Mathf.InverseLerp(0,.72f,phase)));
            main.State=SkiMotionState.NoPole;main.PreviewInput=new LocomotionInput(SpeedMetersPerSecond,0,.15f,travelling:SpeedMetersPerSecond>.1f);main.Evaluate(phase*1.3f);
            if(phase>.45f)
            {
                var hip=bones[0].position;for(int i=0;i<bones.Length;i++)savedRotations[i]=bones[i].rotation;
                main.PreviewInput=null;main.EvaluateShooting(Biathlon.Domain.ShootingStance.Standing,0);float settle=Smooth(Mathf.InverseLerp(.45f,.9f,phase));
                bones[0].position=Vector3.Lerp(hip,bones[0].position,settle);for(int i=0;i<bones.Length;i++)bones[i].rotation=Quaternion.Slerp(savedRotations[i],bones[i].rotation,settle);
            }
            float release=Smooth(Mathf.InverseLerp(d.poles_drop_start,d.poles_drop_end,phase));
            bodyClearance.Update();rifleInverse=rig.Rifle.worldToLocalMatrix;PrepareHeldArms(1-release);
            for(int i=0;i<2;i++){poleReleasePositions[i]=poles[i].Transform.position;poleReleaseRotations[i]=poles[i].Transform.rotation;}
            PrepareFreeArms(release);
            ApplyEyewear(Smooth(Mathf.InverseLerp(d.eyewear_raise_start,d.eyewear_raise_end,phase)));
            float gesture=Smooth(Mathf.InverseLerp(d.eyewear_touch_start,d.eyewear_touch_peak,phase))*(1-Smooth(Mathf.InverseLerp(d.eyewear_release_start,d.eyewear_release_end,phase)));
            if(gesture>0)Grip(true,EyewearPoint(d.eyewear_touch_point),-root.forward,-root.up,d.standing_right_elbow_hint,gesture);
            StowPoles(release,true);
        }
        private void RangePose(bool prone,float progress,float breathPhase)
        {
            var curve=prone?d.prone_right_elbow_curve:d.standing_right_elbow_curve;rightElbowGuide=authoringRightElbowGuide;
            if(!rightElbowGuide.HasValue&&curve!=null&&curve.Length>=2){float at=progress*(curve.Length-1);int i=Mathf.Min((int)at,curve.Length-2);rightElbowGuide=Vector3.Lerp(curve[i],curve[i+1],at-i);}
            // Keep the shipping body choreography. Adapt the imported prop,
            // initial contact handover and anatomical equipment clearances.
            main.EvaluateShooting(prone?Biathlon.Domain.ShootingStance.Prone:Biathlon.Domain.ShootingStance.Standing,progress);
            if(progress<d.setup_pose_blend_end)
            {
                int stance=prone?1:0;float blend=Smooth(progress/d.setup_pose_blend_end);for(int i=0;i<bones.Length;i++)savedRotations[i]=bones[i].rotation;
                bones[0].position=Vector3.Lerp(root.TransformPoint(setupHips[stance]),bones[0].position,blend);
                for(int i=0;i<bones.Length;i++)bones[i].rotation=Quaternion.Slerp(root.rotation*setupStart[stance][i],savedRotations[i],blend);
            }
            shootingSkis.Apply(prone?d.prone_ski_stance:d.standing_ski_stance,progress,prone);
            float sightWeight=Smooth(Mathf.InverseLerp(d.sight_height_settle_start,d.sight_height_settle_end,progress));
            if (prone)
            {
                // Let the neck bring the head forward while the eyes stay on the lane.
                // Rotating the joint keeps both neck and head bone lengths fixed.
                var gaze = bones[4].rotation;
                var settle = root.rotation * Quaternion.Euler(d.prone_neck_settle_degrees * sightWeight) * Quaternion.Inverse(root.rotation);
                bones[3].rotation = settle * bones[3].rotation;
                bones[4].rotation = gaze;
            }
            bodyClearance.Update();
            ApplyEyewear(1);
            var buttPoint=d.rifle_butt_local;
            var carryButt=rig.Rifle.TransformPoint(buttPoint);var carryRotation=rig.Rifle.rotation;
            float transfer=Smooth(Mathf.InverseLerp(d.rifle_removal_start,d.rifle_removal_end,progress));RifleTransfer=transfer;
            var authoredRotation=main.ShootingReadyRotation*Quaternion.AngleAxis(90,Vector3.forward);
            var rotation=Quaternion.Slerp(carryRotation,authoredRotation,Smooth(Mathf.InverseLerp(d.rifle_turn_start,d.rifle_turn_end,transfer)));
            var sourceButt=bones[8].position+root.TransformVector(prone?d.prone_shoulder_pocket:d.standing_shoulder_pocket);
            if (prone)
            {
                sourceButt += root.TransformVector(d.prone_shoulder_settle_m * sightWeight);
                var aperture = sourceButt + authoredRotation * Vector3.Scale(d.rear_aperture_local - buttPoint, rig.Rifle.lossyScale);
                sourceButt += root.up * (Vector3.Dot(eyeAnchor.Position - aperture, root.up) * sightWeight);
            }
            float breathing=Mathf.Sin(breathPhase*Mathf.PI*2)*d.breathing_displacement_m*main.ShootingAlignment;
            var first=carryButt+root.TransformVector(new Vector3(d.rifle_transfer_first_side_m,d.rifle_transfer_up_m,0));
            var second=sourceButt+root.TransformVector(new Vector3(d.rifle_transfer_side_m,d.rifle_transfer_up_m,d.rifle_transfer_forward_m));
            float t=transfer,u=1-t;var butt=carryButt*(u*u*u)+first*(3*u*u*t)+second*(3*u*t*t)+sourceButt*(t*t*t)+root.up*breathing;
            rig.Rifle.SetPositionAndRotation(butt-rotation*Vector3.Scale(buttPoint,rig.Rifle.lossyScale),rotation);
            rifleInverse=rig.Rifle.worldToLocalMatrix;
            PrepareFreeArms();
            CarryVisible(progress<d.rifle_hidden_harness_fraction);
            UpdateLooseHarness(progress>=d.rifle_hidden_harness_fraction);
            SupportContact=rig.Rifle.TransformPoint(prone?d.prone_support_local:d.rifle_support_local);TriggerContact=rig.Rifle.TransformPoint(d.rifle_trigger_local);
            var rightReach=rig.Rifle.TransformPoint(d.rifle_reach_local);
            Grip(true,Vector3.Lerp(rightReach,TriggerContact,transfer),rig.Rifle.up,-rig.Rifle.right,Vector3.zero,Smooth(Mathf.InverseLerp(0,d.rifle_hand_reach_end,progress)),true);
            Grip(false,SupportContact,root.up,-root.forward,prone?d.prone_left_elbow_hint:d.standing_left_elbow_hint,Smooth(Mathf.InverseLerp(d.support_hand_start,d.support_hand_end,progress)));
            // Do this after arm fitting: the laid-down prop must not follow a
            // later wrist rotation. Both poles stay beside the mat thereafter.
            StowPoles(1);
        }
        private void ApplyEyewear(float amount)
        {
            EyewearLift=amount;var frame=bones[4].rotation*Quaternion.Inverse(headNeutral);var pivot=bones[4].position+frame*d.eyewear_pivot;
            for(int i=0;i<eyewear.Length;i++)
            {
                var prop=eyewear[i];var turn=frame*Quaternion.Euler((i==0?d.glasses_raise_euler:d.mask_raise_euler)*amount)*Quaternion.Inverse(frame);
                var position=prop.Parent.TransformPoint(prop.Position);var rotation=prop.Parent.rotation*prop.Rotation;
                prop.Transform.SetPositionAndRotation(pivot+turn*(position-pivot)+frame*((i==0?d.glasses_raise_offset:d.mask_raise_offset)*amount),turn*rotation);
            }
        }
        private Vector3 EyewearPoint(Vector3 point)
        {
            var frame=bones[4].rotation*Quaternion.Inverse(headNeutral);var pivot=bones[4].position+frame*d.eyewear_pivot;bool mask=rig.Eyewear==Biathlete167Eyewear.Mask;
            var turn=frame*Quaternion.Euler((mask?d.mask_raise_euler:d.glasses_raise_euler)*EyewearLift)*Quaternion.Inverse(frame);
            return pivot+turn*(bones[4].position+frame*point-pivot)+frame*((mask?d.mask_raise_offset:d.glasses_raise_offset)*EyewearLift);
        }
        private void Grip(bool right,Vector3 point,Vector3 palm,Vector3 axis,Vector3 elbow,float weight,bool weapon=false)
        {
            if(weight<=0)return;
            int hand=right?10:7,index=right?1:0;var contact=Quaternion.LookRotation(Vector3.Cross(palm,axis),axis);
            if(right&&weapon)contact=Quaternion.Slerp(Quaternion.LookRotation(Vector3.Cross(-rig.Rifle.right,rig.Rifle.forward),rig.Rifle.forward),contact,Smooth(RifleTransfer));
            var rotation=contact*Quaternion.Inverse(gripInHand[index]);
            var start=bones[hand].position;
            var inheritedHint=bones[hand-1].position-(bones[hand-2].position+start)*.5f;
            float constraintWeight=Smooth(Mathf.InverseLerp(.5f,.95f,weight));
            for(int pass=0;pass<d.hand_fit_iterations;pass++)
            {
                var wrist=point-rotation*Vector3.Scale(handGrip[index],bones[hand].lossyScale);
                var hint=right&&weapon?Vector3.Slerp(root.TransformDirection(d.rifle_reach_elbow_hint),rig.Rifle.TransformDirection(d.right_grip_hint_rifle)+root.TransformDirection(d.right_grip_hint_root),Smooth(RifleTransfer)):root.TransformDirection(elbow);
                hint=Vector3.Slerp(inheritedHint.normalized,hint.normalized,weight);
                var destination=Vector3.Lerp(start,wrist,weight);
                if(right&&weapon){float arc=weight>=1?0:Mathf.Sin(weight*Mathf.PI)*(1-Smooth(Mathf.InverseLerp(d.rifle_reach_settle_weight,1,weight)));destination+=root.TransformVector(new Vector3(d.rifle_reach_wrist_side_m,d.rifle_reach_lift_m,d.rifle_reach_front_m))*arc;}
                if(weight<.999f)destination=bodyClearance.Outside(destination,right?1:-1,d.wrist_radius_m);
                Solve(hand-2,destination,hint,right&&weapon,true,right&&weapon?weight:0);
                var actual=(bones[hand].position-bones[hand-1].position).normalized;var desired=rotation*forearmInHand[index].normalized;
                float bend=Vector3.Angle(desired,actual);
                var previous=rotation;
                // Damping is needed only for the wrapped right-hand removal;
                // the support hand converges directly without extra iterations.
                if(bend>d.maximum_range_wrist_bend_degrees)rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(desired,actual),(right&&weapon?.5f:1f)*constraintWeight*(bend-d.maximum_range_wrist_bend_degrees)/bend)*rotation;
                if(pass>0&&Quaternion.Angle(previous,rotation)<.01f)break;
            }
            // The reaching forearm has moved since PrepareFreeArms. Blend
            // from its current neutral frame, never the stale world-space
            // hand frame (which can choose the other side of a 180° arc).
            var neutral=bones[hand-1].rotation*handInForearm[index];
            bones[hand].rotation=right?Quaternion.Slerp(neutral,rotation,constraintWeight):BlendSupportHand(neutral,rotation,(bones[hand].position-bones[hand-1].position).normalized,constraintWeight);
            {
                // Blending with the inherited reaching pose can reintroduce
                // wrist swing after the IK iteration. Bound the final pose too.
                var actual=bones[hand].position-bones[hand-1].position;var desired=bones[hand].TransformDirection(forearmInHand[index]);float bend=Vector3.Angle(desired,actual);
                if(bend>d.maximum_range_wrist_bend_degrees)bones[hand].rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(desired,actual),(bend-d.maximum_range_wrist_bend_degrees)/bend)*bones[hand].rotation;
                MaximumWristBendDegrees=Mathf.Max(MaximumWristBendDegrees,Vector3.Angle(bones[hand].TransformDirection(forearmInHand[index]),actual));
            }
            OrientWristAndForearm(hand,index,bones[hand].rotation);
            if(weight>.999f)MaximumPalmFitResidualMeters=Mathf.Max(MaximumPalmFitResidualMeters,Vector3.Distance(point,rig.GripContact(right).position));
        }
        private void OrientWristAndForearm(int hand,int side,Quaternion desired)
        {
            var lower=bones[hand-1];var axis=(bones[hand].position-lower.position).normalized;var modelAxis=desired*forearmInHand[side];float bend=Vector3.Angle(modelAxis,axis);
            if(bend>d.maximum_range_wrist_bend_degrees)desired=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(modelAxis,axis),(bend-d.maximum_range_wrist_bend_degrees)/bend)*desired;
            MaximumWristBendDegrees=Mathf.Max(MaximumWristBendDegrees,Vector3.Angle(desired*forearmInHand[side],axis));
            var neutral=lower.rotation*handInForearm[side];
            var delta=desired*Quaternion.Inverse(neutral);float sine=Vector3.Dot(new Vector3(delta.x,delta.y,delta.z),axis);float twist=Mathf.DeltaAngle(0,2*Mathf.Atan2(sine,delta.w)*Mathf.Rad2Deg);
            float roll=Mathf.Clamp(twist,-d.maximum_forearm_pronation_degrees,d.maximum_forearm_pronation_degrees);lower.rotation=Quaternion.AngleAxis(roll,axis)*lower.rotation;bones[hand].rotation=desired;
            MaximumForearmPronationDegrees=Mathf.Max(MaximumForearmPronationDegrees,Mathf.Abs(roll));MaximumWristTwistDegrees=Mathf.Max(MaximumWristTwistDegrees,Mathf.Abs(twist-roll));
        }
        private static Quaternion BlendSupportHand(Quaternion neutral,Quaternion goal,Vector3 axis,float weight)
        {
            // The left support palm pronates through the positive turn. A
            // shortest-arc quaternion blend changes branches at 180 degrees
            // and flips the wrist's swing while contact is still blending.
            // Interpolate swing and that continuous pronation separately.
            var delta=goal*Quaternion.Inverse(neutral);float sine=Vector3.Dot(new Vector3(delta.x,delta.y,delta.z),axis);float norm=Mathf.Sqrt(sine*sine+delta.w*delta.w);
            var twist=norm>1e-8f?new Quaternion(axis.x*sine/norm,axis.y*sine/norm,axis.z*sine/norm,delta.w/norm):Quaternion.identity;
            float angle=norm>1e-8f?Mathf.Repeat(2*Mathf.Atan2(sine,delta.w)*Mathf.Rad2Deg,360):0;
            return Quaternion.Slerp(Quaternion.identity,delta*Quaternion.Inverse(twist),weight)*Quaternion.AngleAxis(angle*weight,axis)*neutral;
        }
        private void PrepareFreeArms(float weight=1)
        {
            if(weight<=0)return;
            for(int side=0;side<2;side++)
            {
                int hand=side==0?7:10;var before=bones[hand].rotation;var start=bones[hand].position;var at=bones[hand-2].position+root.TransformVector(side==0?d.left_free_hand_from_shoulder:d.right_free_hand_from_shoulder);
                var local=root.InverseTransformPoint(at);local.y=Mathf.Max(local.y,d.free_hand_minimum_height_m);at=bodyClearance.Outside(root.TransformPoint(local),side==0?-1:1,d.wrist_radius_m);
                Solve(hand-2,Vector3.Lerp(start,at,weight),RestingElbowHint(side),side==1,true);OrientWristAndForearm(hand,side,Quaternion.Slerp(before,bones[hand-1].rotation*handInForearm[side],weight));
            }
        }
        private void PrepareHeldArms(float weight)
        {
            if(weight<=0)return;
            for(int side=0;side<2;side++)
            {
                int hand=side==0?7:10;var rotation=bones[hand].rotation;var poleAxis=poles[side].Transform.TransformDirection(poles[side].Axis).normalized;
                // Adapt the thicker imported sleeves around the inherited G2
                // grip, preserving the hand and held-pole frame in world space.
                var offset=d.coast_wrist_offset;offset.x*=side==0?-1:1;
                Solve(hand-2,bones[hand].position+root.TransformVector(offset)*weight,RestingElbowHint(side),side==1,true);
                // Rotate the grip around the shaft before changing its pitch:
                // this preserves the down/back pole direction while adapting
                // the palm to the lower, closer elbow.
                var actual=bones[hand].position-bones[hand-1].position;var expected=rotation*forearmInHand[side];
                float turn=Vector3.SignedAngle(Vector3.ProjectOnPlane(expected,poleAxis),Vector3.ProjectOnPlane(actual,poleAxis),poleAxis);
                OrientWristAndForearm(hand,side,Quaternion.AngleAxis(turn*weight,poleAxis)*rotation);
            }
        }
        private Vector3 RestingElbowHint(int side)=>root.TransformDirection(new Vector3(d.resting_elbow_hint.x*(side==0?-1:1),d.resting_elbow_hint.y,d.resting_elbow_hint.z));
        private void CaptureEnds()
        {for(int i=0;i<4;i++){int index=i==0?7:i==1?10:i==2?13:16;ends[i]=bones[index].position;endRotations[i]=bones[index].rotation;}}
        private void RestoreEnds()
        {for(int i=0;i<4;i++){int index=i==0?7:i==1?10:i==2?13:16;var hint=i<2?root.right*(i==0?-1:1)-root.up:root.forward;Solve(index-2,ends[i],hint);bones[index].rotation=endRotations[i];}}
        private void Solve(int first,Vector3 destination,Vector3 hint,bool avoidStock=false,bool avoidBody=false,float anatomicalGripWeight=0)
        {
            var a=bones[first];var b=bones[first+1];var c=bones[first+2];var origin=a.position;float l1=Vector3.Distance(origin,b.position),l2=Vector3.Distance(b.position,c.position);
            var delta=destination-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.0001f,l1+l2-.0001f);var direction=delta.sqrMagnitude>1e-10f?delta.normalized:root.forward;
            var bend=Vector3.ProjectOnPlane(hint,direction);if(bend.sqrMagnitude<1e-8f)bend=Vector3.ProjectOnPlane(root.right,direction);bend.Normalize();
            if(anatomicalGripWeight>0)
            {
                // Transport an elbow plane with the reaching arm. Projecting a
                // fixed hint onto the arm axis flips when both become parallel.
                float roll=Mathf.Lerp(d.reaching_elbow_roll_degrees,d.shooting_elbow_roll_degrees,Smooth(RifleTransfer));
                var stable=Quaternion.FromToRotation(root.right,direction)*(Quaternion.AngleAxis(roll,root.right)*root.up);
                bend=Vector3.Slerp(bend,stable,anatomicalGripWeight).normalized;
                if(rightElbowGuide.HasValue){var guide=Vector3.ProjectOnPlane(root.TransformVector(rightElbowGuide.Value),direction);if(guide.sqrMagnitude>1e-8f)bend=guide.normalized;}
            }
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            float radius=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));var centre=origin+direction*along;var joint=centre+bend*radius;
            if((avoidStock||avoidBody)&&ArmPenalty(origin,joint,origin+direction*distance,avoidStock,avoidBody)>1e-10f)
            {
                var other=Vector3.Cross(direction,bend);var end=origin+direction*distance;float best=float.PositiveInfinity,selectedAngle=0;
                var guided=anatomicalGripWeight>0&&rightElbowGuide.HasValue;var guidePoint=guided?origin+root.TransformVector(rightElbowGuide.Value):joint;
                float guideBias=guided?d.arm_guide_bias:d.arm_free_guide_bias;
                for(int i=0;i<d.arm_clearance_angles;i++)
                {
                    float angle=(i==0?0:(i+1)/2*(i%2==1?1:-1))*Mathf.PI*2/d.arm_clearance_angles;
                    var candidate=centre+(bend*Mathf.Cos(angle)+other*Mathf.Sin(angle))*radius;
                    float score=ArmPenalty(origin,candidate,end,avoidStock,avoidBody)+(guided?(candidate-guidePoint).sqrMagnitude:angle*angle)*guideBias;
                    if(score<best){best=score;selectedAngle=angle;}
                }
                float step=Mathf.PI*2/d.arm_clearance_angles,lo=selectedAngle-step,hi=selectedAngle+step;
                for(int i=0;i<12;i++)
                {
                    float left=(2*lo+hi)/3,right=(lo+2*hi)/3;
                    var p=centre+(bend*Mathf.Cos(left)+other*Mathf.Sin(left))*radius;var q=centre+(bend*Mathf.Cos(right)+other*Mathf.Sin(right))*radius;
                    if(ArmPenalty(origin,p,end,avoidStock,avoidBody)+(guided?(p-guidePoint).sqrMagnitude:left*left)*guideBias<ArmPenalty(origin,q,end,avoidStock,avoidBody)+(guided?(q-guidePoint).sqrMagnitude:right*right)*guideBias)hi=right;else lo=left;
                }
                float selected=(lo+hi)*.5f;joint=centre+(bend*Mathf.Cos(selected)+other*Mathf.Sin(selected))*radius;
            }
            a.rotation=Quaternion.FromToRotation(b.position-origin,joint-origin)*a.rotation;b.rotation=Quaternion.FromToRotation(c.position-b.position,origin+direction*distance-b.position)*b.rotation;
            float residual=Vector3.Distance(c.position,destination);if(residual>MaximumReachResidualMeters){MaximumReachResidualMeters=residual;WorstReachLimb=first;}
        }
        private float ArmPenalty(Vector3 shoulder,Vector3 elbow,Vector3 wrist,bool stock,bool body)
        {
            float result=stock?StockPenalty(shoulder,elbow,d.upperarm_radius_m,d.forearm_radius_m,true)+StockPenalty(elbow,wrist,d.forearm_radius_m,d.wrist_radius_m):0;
            if(body)result+=bodyClearance.Penalty(shoulder,elbow,d.upperarm_radius_m,d.forearm_radius_m,true)+bodyClearance.Penalty(elbow,wrist,d.forearm_radius_m,d.wrist_radius_m,false);
            return result;
        }
        private float StockPenalty(Vector3 from,Vector3 to,float firstRadius,float lastRadius,bool shoulderConnection=false)
        {
            from=rifleInverse.MultiplyPoint3x4(from);to=rifleInverse.MultiplyPoint3x4(to);float sum=0;
            for(int i=shoulderConnection?3:0;i<=8;i++){float t=i/8f;var point=Vector3.Lerp(from,to,t);float gap=Mathf.Min(Vector3.Distance(point,stockBounds.ClosestPoint(point)),Vector3.Distance(point,barrelBounds.ClosestPoint(point)));float penetration=Mathf.Max(0,Mathf.Lerp(firstRadius,lastRadius,t)-gap);sum+=penetration*penetration;}
            return sum;
        }
        private void StowPoles(float weight,bool releaseSnapshot=false)
        {
            for(int i=0;i<2;i++)
            {
                var p=poles[i];var direction=root.TransformDirection(i==0?d.left_pole_lay_direction:d.right_pole_lay_direction);var baseline=root.rotation*p.RootRotation;var rotation=Quaternion.FromToRotation(baseline*p.Axis,direction)*baseline;
                var target=root.TransformPoint(i==0?d.left_pole_stow:d.right_pole_stow);var at=target-rotation*Vector3.Scale(p.Grip,p.Transform.lossyScale);
                p.Transform.SetPositionAndRotation(Vector3.Lerp(releaseSnapshot?poleReleasePositions[i]:p.Transform.position,at,weight),Quaternion.Slerp(releaseSnapshot?poleReleaseRotations[i]:p.Transform.rotation,rotation,weight));
            }
        }
        private void CarryVisible(bool visible)
        {
            if(visible&&looseHarness!=null)looseHarness.SetActive(false);
            bool changed=outfit!=null&&materialNation!=outfit.Nation;
            if(changed)
            {
                materialNation=outfit.Nation;
                for(int i=0;i<skins.Length;i++)
                {
                    skins[i].GetSharedMaterials(materialScratch);
                    for(int j=0;j<materialScratch.Count;j++)
                        if(hiddenMaterials[i][j]!=hiddenHarness[0]&&hiddenMaterials[i][j]!=hiddenHarness[1])
                            visibleMaterials[i][j]=hiddenMaterials[i][j]=materialScratch[j];
                }
            }
            if(hidden==!visible&&!changed)return;
            hidden=!visible;for(int i=0;i<skins.Length;i++)skins[i].sharedMaterials=visible?visibleMaterials[i]:hiddenMaterials[i];
        }
        private void UpdateLooseHarness(bool show)
        {
            looseHarness.SetActive(show);if(!show)return;
            var down=rig.Rifle.InverseTransformDirection(-root.up);var outward=rig.Rifle.InverseTransformDirection(root.right);
            for(int side=0;side<2;side++)for(int row=0;row<=HarnessSegments;row++)
            {
                float t=row/(float)HarnessSegments;var a=looseAnchors[side*2];var b=looseAnchors[side*2+1];var sag=down*d.loose_harness_sag_m+outward*(side==0?-.035f:.035f);
                var centre=Vector3.Lerp(a,b,t)+sag*Mathf.Sin(t*Mathf.PI);var tangent=(b-a+sag*Mathf.PI*Mathf.Cos(t*Mathf.PI)).normalized;
                var across=Vector3.ProjectOnPlane(outward,tangent).normalized;if(across.sqrMagnitude<.01f)across=Vector3.Cross(tangent,Vector3.up).normalized;var normal=Vector3.Cross(tangent,across).normalized;
                for(int corner=0;corner<4;corner++)looseVertices[(side*(HarnessSegments+1)+row)*4+corner]=centre+across*((corner==0||corner==3?-1:1)*d.loose_harness_width_m*.5f)+normal*((corner<2?-1:1)*d.loose_harness_thickness_m*.5f);
            }
            looseMesh.vertices=looseVertices;looseMesh.RecalculateNormals();looseMesh.RecalculateBounds();
        }
        private void RestoreProps(){Restore(rifle);for(int i=0;i<poles.Length;i++)Restore(poles[i]);for(int i=0;i<eyewear.Length;i++)Restore(eyewear[i]);}
        private static void Restore(Prop p){p.Transform.SetParent(p.Parent,false);p.Transform.SetLocalPositionAndRotation(p.Position,p.Rotation);}
        public void Release()
        {RestoreProps();CarryVisible(true);main.PreviewInput=null;if(configured){main.SourceDriver.Configure(SkiMotionSettings.Load());configured=false;}rig.SetPose(0);}
        public void Dispose(){Release();foreach(var material in hiddenHarness)if(material!=null){if(Application.isPlaying)Object.Destroy(material);else Object.DestroyImmediate(material);}if(Application.isPlaying){Object.Destroy(looseHarness);Object.Destroy(looseMesh);}else{Object.DestroyImmediate(looseHarness);Object.DestroyImmediate(looseMesh);}}
        private static Prop ReadProp(Transform transform,Transform frame)
        {
            var result=new Prop{Transform=transform,Parent=transform.parent,Position=transform.localPosition,Rotation=transform.localRotation};
            if(frame!=null)
            {
                result.Grip=transform.InverseTransformPoint(frame.position);var mesh=transform.GetComponent<MeshFilter>().sharedMesh;float longest=0;
                foreach(var vertex in mesh.vertices){float distance=(vertex-result.Grip).sqrMagnitude;if(distance>longest){longest=distance;result.Axis=(vertex-result.Grip).normalized;}}
            }
            return result;
        }
        private static Transform Longest(Transform hand)
        {MeshFilter result=null;float largest=0;foreach(var mesh in hand.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh!=null&&mesh.sharedMesh.bounds.size.magnitude>largest){result=mesh;largest=mesh.sharedMesh.bounds.size.magnitude;}return result!=null?result.transform:throw new InvalidOperationException("Missing typed hand prop.");}
        private static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
        private sealed class Prop{public Transform Transform,Parent;public Vector3 Position,Grip,Axis;public Quaternion Rotation,RootRotation;}
    }
}
