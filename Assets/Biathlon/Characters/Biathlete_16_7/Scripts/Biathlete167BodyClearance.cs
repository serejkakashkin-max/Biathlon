using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cached garment/anatomy volumes for studio arm fitting. No runtime mesh baking.</summary>
    internal sealed class Biathlete167BodyClearance
    {
        private readonly Transform root;private readonly Volume[] volumes;
        private sealed class Volume
        {
            public Transform Bone;public Vector3 Centre,Radii,WorldCentre,WorldRadii;public Quaternion Frame;public Matrix4x4 ToUnit,FromUnit;public float MinRadius,MaxRadius;
        }
        public Biathlete167BodyClearance(Biathlete167Rig rig,SkinnedMeshRenderer skin,float scale,float padding)
        {
            root=rig.transform;var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var sourceBones=skin.bones;
            var points=new HashSet<int>();var materials=skin.sharedMaterials;
            for(int sub=0;sub<materials.Length;sub++)if(materials[sub].name is "Anatomical_Skin_CC0_2K" or "PULSE_Natural_Tailored_Textile" or "PULSE_Bib_Printed_Textile")foreach(int index in mesh.GetTriangles(sub))points.Add(index);
            var list=new List<Volume>();
            foreach(var id in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head})
            {
                var bone=rig.Bone(id);if(bone==null)continue;int boneIndex=Array.IndexOf(sourceBones,bone);if(boneIndex<0)continue;bool found=false;var bounds=new Bounds();
                foreach(int index in points)
                {
                    var w=weights[index];int dominant=w.boneIndex0;float best=w.weight0;if(w.weight1>best){dominant=w.boneIndex1;best=w.weight1;}if(w.weight2>best){dominant=w.boneIndex2;best=w.weight2;}if(w.weight3>best)dominant=w.boneIndex3;
                    if(dominant!=boneIndex)continue;var p=root.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
                if(!found)continue;
                var radii=bounds.extents*scale+Vector3.one*padding;radii=Vector3.Max(radii,Vector3.one*.025f);
                list.Add(new Volume{Bone=bone,Centre=bone.InverseTransformPoint(root.TransformPoint(bounds.center)),Frame=Quaternion.Inverse(bone.rotation)*root.rotation,Radii=radii});
            }
            volumes=list.ToArray();if(volumes.Length<4)throw new InvalidOperationException("Hero torso/head clearance volumes are incomplete.");Update();
        }
        public void Update()
        {
            foreach(var volume in volumes)
            {
                var scale=Vector3.Scale(volume.Radii,root.lossyScale);volume.WorldRadii=scale;volume.WorldCentre=volume.Bone.TransformPoint(volume.Centre);volume.FromUnit=Matrix4x4.TRS(volume.WorldCentre,volume.Bone.rotation*volume.Frame,scale);volume.ToUnit=volume.FromUnit.inverse;
                volume.MinRadius=Mathf.Min(scale.x,Mathf.Min(scale.y,scale.z));volume.MaxRadius=Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
            }
        }
        public Vector3 Outside(Vector3 point,float side,float radius)
        {
            for(int pass=0;pass<12;pass++)
            {
                bool moved=false;
                foreach(var volume in volumes)
                {
                    var r=volume.WorldRadii;var expansion=new Vector3(r.x/(r.x+radius),r.y/(r.y+radius),r.z/(r.z+radius));
                    var p=Vector3.Scale(volume.ToUnit.MultiplyPoint3x4(point),expansion);float length=p.magnitude;if(length>=1)continue;
                    if(length<1e-6f){p=volume.ToUnit.MultiplyVector(root.right*side).normalized*.0001f;length=.0001f;}
                    var push=p*(1.00001f/length-1);point+=volume.FromUnit.MultiplyVector(new Vector3(push.x/expansion.x,push.y/expansion.y,push.z/expansion.z));moved=true;
                }
                if(!moved)break;
            }
            return point;
        }
        public float Penalty(Vector3 from,Vector3 to,float startRadius,float endRadius,bool connectedShoulder)
        {
            float sum=0;var delta=to-from;float length=delta.sqrMagnitude;
            foreach(var volume in volumes)
            {
                float t=length>1e-9f?Mathf.Clamp01(Vector3.Dot(volume.WorldCentre-from,delta)/length):0;
                if((from+delta*t-volume.WorldCentre).sqrMagnitude>Mathf.Pow(volume.MaxRadius+Mathf.Max(startRadius,endRadius),2))continue;
                for(int i=connectedShoulder?3:0;i<=8;i++)
                {
                    float phase=i/8f,limbRadius=Mathf.Lerp(startRadius,endRadius,phase);var r=volume.WorldRadii;var p=volume.ToUnit.MultiplyPoint3x4(Vector3.Lerp(from,to,phase));
                    var expanded=new Vector3(p.x*r.x/(r.x+limbRadius),p.y*r.y/(r.y+limbRadius),p.z*r.z/(r.z+limbRadius));
                    float penetration=Mathf.Max(0,1-expanded.magnitude)*(volume.MinRadius+limbRadius);sum+=penetration*penetration;
                }
            }
            return sum;
        }
    }
}
