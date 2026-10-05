using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public enum SpectatorPose { Seated = 0, Standing = 1 }
    public sealed class SpectatorSeating : MonoBehaviour
    {
        [SerializeField] private Vector3[] anchors=Array.Empty<Vector3>();
        [SerializeField] private SpectatorPose pose=SpectatorPose.Seated;
        public int Count=>anchors.Length;
        public SpectatorPose Pose=>pose;
        public Vector3[] CopyAnchors()=>(Vector3[])anchors.Clone();
        public void Configure(Vector3[] value)=>Configure(value,SpectatorPose.Seated);
        public void Configure(Vector3[] value,SpectatorPose sourcePose)
        {
            if(sourcePose!=SpectatorPose.Seated&&sourcePose!=SpectatorPose.Standing)throw new ArgumentException("Unknown spectator source pose.");
            if(value==null||value.Length==0||value.Length>512)throw new ArgumentException("Bounded explicit seating required.");
            for(int i=0;i<value.Length;i++){var p=value[i];if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)||p.sqrMagnitude>1000000)throw new ArgumentException("Invalid seat anchor.");for(int j=0;j<i;j++)if((value[j]-p).sqrMagnitude<.25f)throw new ArgumentException("Overlapping spectator seats.");}
            anchors=(Vector3[])value.Clone();
            pose=sourcePose;
        }
    }
}
