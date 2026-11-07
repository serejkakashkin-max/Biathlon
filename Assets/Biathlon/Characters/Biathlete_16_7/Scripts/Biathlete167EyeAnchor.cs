using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Real dominant pupil, cached in neutral pose. No mesh baking during playback.</summary>
    internal sealed class Biathlete167EyeAnchor
    {
        private readonly Transform head;
        private readonly Vector3 eyeInHead;
        public Vector3 Position=>head.TransformPoint(eyeInHead);
        public Biathlete167EyeAnchor(Biathlete167Rig rig,SkinnedMeshRenderer skin)
        {
            head=rig.Bone(HumanBodyBones.Head);
            int pupil=Array.FindIndex(skin.sharedMaterials,m=>m.name=="Eye_Pupil");if(pupil<0)throw new InvalidOperationException("Dominant pupil mesh missing.");
            var vertices=skin.sharedMesh.vertices;var indices=skin.sharedMesh.GetTriangles(pupil);var used=new bool[vertices.Length];var sum=Vector3.zero;int count=0;
            foreach(int i in indices)if(!used[i]){used[i]=true;var world=skin.transform.TransformPoint(vertices[i]);if(rig.transform.InverseTransformPoint(world).x>0){sum+=head.InverseTransformPoint(world);count++;}}
            if(count<20)throw new InvalidOperationException("Dominant pupil is not a usable surface.");eyeInHead=sum/count;
        }
    }
}
