using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cached garment/anatomy volumes for studio arm fitting. No runtime mesh baking.</summary>
    internal sealed class Biathlete167BodyClearance
    {
        private readonly Transform root;private readonly Volume[] volumes;
        private readonly float upperarmRadius, forearmRadius, wristRadius;
        private const int SegmentSamples = 9;
        private sealed class Volume
        {
            public Transform Bone;public Vector3 Centre,Radii,WorldCentre,WorldRadii;public Quaternion Frame;public Matrix4x4 ToUnit,FromUnit;public float MinRadius,MaxRadius;
            public readonly Vector3[] UpperExpansion = new Vector3[SegmentSamples], LowerExpansion = new Vector3[SegmentSamples];
            public readonly float[] UpperDepth = new float[SegmentSamples], LowerDepth = new float[SegmentSamples];
            public float UpperBroadPhase, LowerBroadPhase;
        }
        public Biathlete167BodyClearance(Biathlete167Rig rig,SkinnedMeshRenderer skin,float scale,float padding,
            float upperarmRadius, float forearmRadius, float wristRadius)
        {
            this.upperarmRadius = upperarmRadius;
            this.forearmRadius = forearmRadius;
            this.wristRadius = wristRadius;
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
                volume.UpperBroadPhase = PrepareSegment(volume, upperarmRadius, forearmRadius, volume.UpperExpansion, volume.UpperDepth);
                volume.LowerBroadPhase = PrepareSegment(volume, forearmRadius, wristRadius, volume.LowerExpansion, volume.LowerDepth);
            }
        }
        private static float PrepareSegment(Volume volume, float start, float end, Vector3[] expansion, float[] depth)
        {
            var radii = volume.WorldRadii;
            for (int i = 0; i < SegmentSamples; i++)
            {
                float radius = Mathf.Lerp(start, end, i / 8f);
                expansion[i] = new Vector3(radii.x / (radii.x + radius), radii.y / (radii.y + radius), radii.z / (radii.z + radius));
                depth[i] = volume.MinRadius + radius;
            }
            float broadPhase = volume.MaxRadius + Mathf.Max(start, end);
            return broadPhase * broadPhase;
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
        public float Penalty(Vector3 from, Vector3 to, bool connectedShoulder)
        {
            float sum = 0;
            var delta = to - from;
            float inverseLength = delta.sqrMagnitude > 1e-9f ? 1 / delta.sqrMagnitude : 0;
            foreach (var volume in volumes)
            {
                var centreOffset = from - volume.WorldCentre;
                float t = Mathf.Clamp01(-Vector3.Dot(centreOffset, delta) * inverseLength);
                if ((centreOffset + delta * t).sqrMagnitude > (connectedShoulder ? volume.UpperBroadPhase : volume.LowerBroadPhase)) continue;
                var expansion = connectedShoulder ? volume.UpperExpansion : volume.LowerExpansion;
                var depth = connectedShoulder ? volume.UpperDepth : volume.LowerDepth;
                var start = volume.ToUnit.MultiplyPoint3x4(from);
                var step = volume.ToUnit.MultiplyVector(delta) * .125f;
                for (int i = connectedShoulder ? 3 : 0; i < SegmentSamples; i++)
                {
                    float distanceSquared = Vector3.Scale(start + step * i, expansion[i]).sqrMagnitude;
                    if (distanceSquared >= 1) continue;
                    float penetration = (1 - Mathf.Sqrt(distanceSquared)) * depth[i];
                    sum += penetration * penetration;
                }
            }
            return sum;
        }
    }
}
