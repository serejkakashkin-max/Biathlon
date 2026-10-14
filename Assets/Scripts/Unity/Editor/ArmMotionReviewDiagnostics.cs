#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Biathlon.Unity.Presentation;
using UnityEngine;

namespace Biathlon.Unity
{
    /// <summary>Explicit batch-editor numeric review of the complete pose/contact/grip chain.</summary>
    public static class ArmMotionReviewDiagnostics
    {
        [Serializable] private sealed class Case
        {
            public string id,body_sha256;
            public float elbow_outward_min_m=float.MaxValue,elbow_outward_max_m,forearm_rate_max_deg_s,forearm_peak_pole_phase,grip_residual_max_m;
            public float leg_cadence_hz,pole_cadence_hz,rifle_broad_back_dot,rifle_muzzle_up_dot;
        }
        [Serializable] private sealed class Receipt
        {
            public int schema_version=1;public string id="arm_motion_review",unity;
            public int frames=360,sampling_hz=120;public Case[] cases;
        }
        public static void Export()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-biathlonArmProbeOutput");
            if(index<0||index+1>=args.Length)throw new ArgumentException("Explicit arm probe output required.");
            ExportTo(args[index+1]);
        }
        public static void ExportTo(string output)
        {
            string path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));
            var receipt=new Receipt{unity=Application.unityVersion,cases=new Case[6]};
            var inputs=new[]{new LocomotionInput(6,0,.95f),new LocomotionInput(4,.08f,.95f),new LocomotionInput(9,0,1.25f,sprint:true),
                new LocomotionInput(3,.15f,.95f),new LocomotionInput(6,0,.95f,curvature:.035f),new LocomotionInput(8,0,.95f)};
            var names=new[]{"flat","uphill","sprint","steep","corner","fast"};
            var bones=new[]{AthleteBone.Hips,AthleteBone.Spine,AthleteBone.Chest,AthleteBone.Neck,
                AthleteBone.LeftUpperLeg,AthleteBone.LeftLowerLeg,AthleteBone.LeftFoot,AthleteBone.RightUpperLeg,AthleteBone.RightLowerLeg,AthleteBone.RightFoot};
            for(int n=0;n<inputs.Length;n++)
            {
                var input=inputs[n];var result=receipt.cases[n]=new Case{id=names[n]};var rig=AthleteRigFactory.Create(null);
                try
                {
                    var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                    var rifle=rig.GetComponent<AthleteRiflePresentation>();var grip=rig.GetComponent<AthleteGripPresentation>();var previous=new Quaternion[2];
                    using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
                    {
                        for(int frame=0;frame<360;frame++)
                        {
                            double time=frame/120.0;rig.transform.position=new Vector3(0,input.Grade*input.Speed*(float)time,input.Speed*(float)time);
                            d.Sample(rig,input,time);rifle.Sample(rig,0);c.SampleMoving(rig,input.Grade,d.PoleCycle,d.ContactState,0,time);d.ApplySkiEdges(rig);grip.Render(rig);
                            result.leg_cadence_hz=d.CadenceHz;result.pole_cadence_hz=d.PoleCadenceHz;
                            var weapon=rig.GetComponent<AthleteEquipmentBinding>().Rifle;var chest=rig.Bone(AthleteBone.Chest);
                            result.rifle_broad_back_dot=Mathf.Abs(Vector3.Dot(weapon.right,chest.forward));result.rifle_muzzle_up_dot=Vector3.Dot(weapon.forward,chest.up);
                            foreach(var bone in bones)
                            {
                                var t=rig.Bone(bone);var p=t.localPosition;var q=t.localRotation;
                                writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(q.x);writer.Write(q.y);writer.Write(q.z);writer.Write(q.w);
                            }
                            for(int side=0;side<2;side++)
                            {
                                var shoulder=rig.Bone(side==0?AthleteBone.LeftUpperArm:AthleteBone.RightUpperArm);
                                var elbow=rig.Bone(side==0?AthleteBone.LeftLowerArm:AthleteBone.RightLowerArm);
                                float outward=(side==0?-1:1)*rig.ModelRoot.InverseTransformDirection(elbow.position-shoulder.position).x;
                                result.elbow_outward_min_m=Mathf.Min(result.elbow_outward_min_m,outward);result.elbow_outward_max_m=Mathf.Max(result.elbow_outward_max_m,outward);
                                if(frame>0)
                                {
                                    float rate=Quaternion.Angle(previous[side],elbow.rotation)*120;
                                    if(rate>result.forearm_rate_max_deg_s){result.forearm_rate_max_deg_s=rate;result.forearm_peak_pole_phase=(float)d.PoleCycle;}
                                }
                                previous[side]=elbow.rotation;
                            }
                            result.grip_residual_max_m=Mathf.Max(result.grip_residual_max_m,grip.MaximumResidualMeters);
                        }
                        using(var hash=SHA256.Create())result.body_sha256=BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(rig.gameObject);}
            }
            File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
        }
    }
}
#endif
