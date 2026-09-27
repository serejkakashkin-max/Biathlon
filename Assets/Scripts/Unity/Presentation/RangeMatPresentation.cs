using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only venue dressing at the same physical route samples used by athlete proxies.</summary>
    public sealed class RangeMatPresentation : MonoBehaviour
    {
        private Transform[] anchors;
        public int Count=>anchors?.Length??0;
        // Observations expose one-based logical lane numbers, like the range signage.
        public Transform Anchor(int lane)=>lane>=1&&lane<=Count?anchors[lane-1]:throw new ArgumentOutOfRangeException(nameof(lane));
        public void Initialize(TrackDefinition track,TrackVisualDefinition visual,PhysicalRangeLaneDefinition lanes)
        {
            if(anchors!=null||track==null||visual==null||lanes==null||track.Id!=visual.TrackId||track.Id!=lanes.TrackId)
                throw new ArgumentException("Fresh range presentation and matching definitions required.");
            var art=StadiumArtDefinition.Load();
            double stop=track.Range.FiringLine.Distance.Meters-lanes.Entry.Meters;
            anchors=new Transform[lanes.Count];
            for(int lane=0;lane<lanes.Count;lane++)
            {
                var point=visual.Get(lanes.GetLane(lane)).Sample(stop);
                var go=new GameObject("Physical shooting lane "+(lane+1));var anchor=go.transform;
                anchor.SetParent(transform,false);anchor.localPosition=new Vector3((float)point.X,(float)point.Y,(float)point.Z);
                anchor.localRotation=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ));anchors[lane]=anchor;
                var mat=GameObject.CreatePrimitive(PrimitiveType.Cube);mat.name="Shooting mat";mat.transform.SetParent(anchor,false);
                // Flush surface: never lift the athlete or move its authoritative stop point.
                mat.transform.localPosition=new Vector3(0,-.022f,0);mat.transform.localScale=new Vector3(art.mat_width_m,.05f,art.mat_length_m);
                mat.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Biathlon/Stadium/rubber");
                Destroy(mat.GetComponent<Collider>());
            }
        }
    }
}
