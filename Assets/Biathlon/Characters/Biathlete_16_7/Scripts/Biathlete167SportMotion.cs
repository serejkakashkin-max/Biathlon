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
            public string id,provenance;
            public float acceleration_duration_s,range_entry_duration_s,standing_transition_duration_s,prone_transition_duration_s,breathing_cycle_s,breathing_displacement_m,flat_transfer_m,sprint_transfer_m,uphill_transfer_m,start_speed_mps,sprint_speed_mps,uphill_speed_mps,uphill_grade,deceleration_start_speed_mps,rifle_hidden_harness_fraction,loose_harness_sag_m,loose_harness_width_m,loose_harness_thickness_m,maximum_range_wrist_bend_degrees;
            public Vector3 standing_left_elbow_hint,standing_right_elbow_hint,prone_left_elbow_hint,prone_right_elbow_hint,rifle_butt_local,rifle_trigger_local,rifle_support_local,left_pole_stow,right_pole_stow,prone_support_local,loose_harness_upper,loose_harness_lower;
        }
        private static readonly HumanBodyBones[] BoneIds={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        private readonly Biathlete167Rig rig;private readonly Biathlete167MainMotion main;private readonly Definition d;private readonly SkiMotionSettings ski;
        private readonly Transform root;private readonly Transform[] bones=new Transform[17];private readonly Quaternion[] savedRotations=new Quaternion[17];
        private readonly Vector3[] ends=new Vector3[4];private readonly Quaternion[] endRotations=new Quaternion[4];
        private readonly Prop[] poles=new Prop[2];private readonly Prop rifle;
        private readonly Vector3[] handGrip=new Vector3[2];private readonly Quaternion[] gripInHand=new Quaternion[2];
        private readonly Vector3[] forearmInHand=new Vector3[2];
        private readonly SkinnedMeshRenderer[] skins;private readonly Material[][] visibleMaterials,hiddenMaterials;private readonly Material[] hiddenHarness;
        private readonly Biathlete167Outfit outfit;private string materialNation;
        private readonly List<Material> materialScratch=new List<Material>(64);
        private bool hidden,configured;
        private readonly GameObject looseHarness;private readonly Mesh looseMesh;private readonly Vector3[] looseVertices;
        private readonly Vector3[] looseAnchors=new Vector3[4];private const int HarnessSegments=24;
        public Biathlete167Action Action{get;private set;}
        public float Phase{get;private set;}
        public float SpeedMetersPerSecond{get;private set;}
        public float MaximumReachResidualMeters{get;private set;}
        public int WorstReachLimb{get;private set;}
        public float MaximumWristBendDegrees{get;private set;}
        public float RifleTransfer{get;private set;}
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
            rig.SetPose(0);
            for(int i=0;i<bones.Length;i++)bones[i]=rig.Bone(BoneIds[i]);
            rifle=ReadProp(rig.Rifle,null);
            for(int i=0;i<2;i++)
            {
                var hand=bones[i==0?7:10];var frame=rig.GripContact(i==1);handGrip[i]=hand.InverseTransformPoint(frame.position);gripInHand[i]=Quaternion.Inverse(hand.rotation)*frame.rotation;
                forearmInHand[i]=hand.InverseTransformDirection(hand.position-hand.parent.position);
                poles[i]=ReadProp(Longest(hand),frame);
            }
            skins=Array.ConvertAll(rig.Lods.GetLODs(),lod=>Array.Find(lod.renderers,r=>r is SkinnedMeshRenderer)as SkinnedMeshRenderer);
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
            for(int side=0;side<2;side++)for(int end=0;end<2;end++)
            {
                var query=end==0?d.loose_harness_upper:d.loose_harness_lower;query.y+=side==0?-.025f:.025f;float best=float.PositiveInfinity;
                foreach(int index in woodIndices){float distance=(woodVertices[index]-query).sqrMagnitude;if(distance<best){best=distance;looseAnchors[side*2+end]=woodVertices[index];}}
            }
            looseHarness.SetActive(false);
        }
        public void Evaluate(Biathlete167Action action,float normalizedPhase)
        {
            if(action<Biathlete167Action.Skate||action>Biathlete167Action.EnterProne||!float.IsFinite(normalizedPhase)||normalizedPhase<0||normalizedPhase>1)throw new ArgumentOutOfRangeException(nameof(normalizedPhase));
            Action=action;Phase=normalizedPhase;MaximumReachResidualMeters=MaximumWristBendDegrees=0;RifleTransfer=0;RestoreProps();
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
                main.PreviewInput=null;main.State=SkiMotionState.SlowStop;main.Evaluate(0);float settle=Smooth(Mathf.InverseLerp(.45f,.9f,phase));
                bones[0].position=Vector3.Lerp(hip,bones[0].position,settle);for(int i=0;i<bones.Length;i++)bones[i].rotation=Quaternion.Slerp(savedRotations[i],bones[i].rotation,settle);
                StowPoles(Smooth(Mathf.InverseLerp(.72f,1,phase)));
            }
        }
        private void RangePose(bool prone,float progress,float breathPhase)
        {
            // The existing game owns the body pose and the whole setup timing.
            // Only the different mesh pivot and anatomical hand contacts are adapted.
            main.EvaluateShooting(prone?Biathlon.Domain.ShootingStance.Prone:Biathlon.Domain.ShootingStance.Standing,progress);
            var carryButt=rig.Rifle.TransformPoint(d.rifle_butt_local);var carryRotation=rig.Rifle.rotation;
            float transfer=main.ShootingTransfer;RifleTransfer=transfer;
            var authoredRotation=main.ShootingRifleRotation*Quaternion.AngleAxis(90,Vector3.forward);
            var rotation=Quaternion.Slerp(carryRotation,authoredRotation,transfer);
            var sourceButt=main.ShootingRiflePosition+main.ShootingRootOffset;
            float breathing=Mathf.Sin(breathPhase*Mathf.PI*2)*d.breathing_displacement_m*main.ShootingAlignment;
            var butt=Vector3.Lerp(carryButt,sourceButt,transfer)+root.up*breathing;
            rig.Rifle.SetPositionAndRotation(butt-rotation*Vector3.Scale(d.rifle_butt_local,rig.Rifle.lossyScale),rotation);
            CarryVisible(progress<d.rifle_hidden_harness_fraction);
            UpdateLooseHarness(progress>=d.rifle_hidden_harness_fraction);
            SupportContact=rig.Rifle.TransformPoint(prone?d.prone_support_local:d.rifle_support_local);TriggerContact=rig.Rifle.TransformPoint(d.rifle_trigger_local);
            var rightReach=rig.Rifle.TransformPoint(new Vector3(.09f,.04f,.10f));
            Grip(true,Vector3.Lerp(rightReach,TriggerContact,transfer),-root.right,-root.up,prone?d.prone_right_elbow_hint:d.standing_right_elbow_hint,main.ShootingHandWeight);
            Grip(false,SupportContact,root.up,-root.forward,prone?d.prone_left_elbow_hint:d.standing_left_elbow_hint,Smooth(Mathf.InverseLerp(.5f,.75f,progress)));
            // Do this after arm fitting: the laid-down prop must not follow a
            // later wrist rotation. The shipping chain supplies its stow path.
            StowPoles(main.ShootingHandWeight,true);
        }
        private void Grip(bool right,Vector3 point,Vector3 palm,Vector3 axis,Vector3 elbow,float weight)
        {
            int hand=right?10:7,index=right?1:0;var contact=Quaternion.LookRotation(Vector3.Cross(palm,axis),axis);var rotation=contact*Quaternion.Inverse(gripInHand[index]);
            var before=bones[hand].rotation;var start=bones[hand].position;
            for(int pass=0;pass<3;pass++)
            {
                var wrist=point-rotation*Vector3.Scale(handGrip[index],bones[hand].lossyScale);
                var desiredElbow=wrist-rotation*forearmInHand[index].normalized*Vector3.Distance(bones[hand-1].position,bones[hand].position);
                var hint=Vector3.Lerp(root.TransformDirection(elbow),desiredElbow-(bones[hand-2].position+wrist)*.5f,weight);
                Solve(hand-2,Vector3.Lerp(start,wrist,weight),hint);
                var actual=(bones[hand].position-bones[hand-1].position).normalized;var desired=rotation*forearmInHand[index].normalized;
                float bend=Vector3.Angle(desired,actual);
                if(bend>d.maximum_range_wrist_bend_degrees&&weight>.95f)rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(desired,actual),(bend-d.maximum_range_wrist_bend_degrees)/bend)*rotation;
            }
            bones[hand].rotation=Quaternion.Slerp(before,rotation,weight);
            if(weight>.95f)
            {
                // Blending with the inherited reaching pose can reintroduce
                // wrist swing after the IK iteration. Bound the final pose too.
                var actual=bones[hand].position-bones[hand-1].position;var desired=bones[hand].TransformDirection(forearmInHand[index]);float bend=Vector3.Angle(desired,actual);
                if(bend>d.maximum_range_wrist_bend_degrees)bones[hand].rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(desired,actual),(bend-d.maximum_range_wrist_bend_degrees)/bend)*bones[hand].rotation;
                MaximumWristBendDegrees=Mathf.Max(MaximumWristBendDegrees,Vector3.Angle(bones[hand].TransformDirection(forearmInHand[index]),actual));
            }
        }
        private void CaptureEnds()
        {for(int i=0;i<4;i++){int index=i==0?7:i==1?10:i==2?13:16;ends[i]=bones[index].position;endRotations[i]=bones[index].rotation;}}
        private void RestoreEnds()
        {for(int i=0;i<4;i++){int index=i==0?7:i==1?10:i==2?13:16;var hint=i<2?root.right*(i==0?-1:1)-root.up:root.forward;Solve(index-2,ends[i],hint);bones[index].rotation=endRotations[i];}}
        private void Solve(int first,Vector3 destination,Vector3 hint)
        {
            var a=bones[first];var b=bones[first+1];var c=bones[first+2];var origin=a.position;float l1=Vector3.Distance(origin,b.position),l2=Vector3.Distance(b.position,c.position);
            var delta=destination-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.0001f,l1+l2-.0001f);var direction=delta.sqrMagnitude>1e-10f?delta.normalized:root.forward;
            var bend=Vector3.ProjectOnPlane(hint,direction);if(bend.sqrMagnitude<1e-8f)bend=Vector3.ProjectOnPlane(root.right,direction);bend.Normalize();float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            var joint=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));a.rotation=Quaternion.FromToRotation(b.position-origin,joint-origin)*a.rotation;b.rotation=Quaternion.FromToRotation(c.position-b.position,origin+direction*distance-b.position)*b.rotation;
            float residual=Vector3.Distance(c.position,destination);if(residual>MaximumReachResidualMeters){MaximumReachResidualMeters=residual;WorstReachLimb=first;}
        }
        private void StowPoles(float weight,bool production=false)
        {
            for(int i=0;i<2;i++)
            {
                var p=poles[i];var direction=root.TransformDirection(new Vector3(i==0?-.03f:.03f,0,-1));var rotation=Quaternion.FromToRotation(p.Transform.TransformDirection(p.Axis),direction)*p.Transform.rotation;
                var target=production?main.ShootingPolePosition(i==1):root.TransformPoint(i==0?d.left_pole_stow:d.right_pole_stow);var at=target-rotation*Vector3.Scale(p.Grip,p.Transform.lossyScale);
                p.Transform.SetPositionAndRotation(Vector3.Lerp(p.Transform.position,at,weight),Quaternion.Slerp(p.Transform.rotation,rotation,weight));
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
        private void RestoreProps(){Restore(rifle);for(int i=0;i<poles.Length;i++)Restore(poles[i]);}
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
        private sealed class Prop{public Transform Transform,Parent;public Vector3 Position,Grip,Axis;public Quaternion Rotation;}
    }
}
