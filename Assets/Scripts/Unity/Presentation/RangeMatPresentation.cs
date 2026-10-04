using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only venue dressing at the same physical route samples used by athlete proxies.</summary>
    public sealed class RangeMatPresentation : MonoBehaviour
    {
        private Transform[] anchors;
        private string[] laneIds;
        private Material physicalMaterial;
        public int Count=>anchors?.Length??0;
        // Observations expose one-based logical lane numbers, like the range signage.
        public Transform Anchor(int lane)=>lane>=1&&lane<=Count?anchors[lane-1]:throw new ArgumentOutOfRangeException(nameof(lane));
        public string LaneId(int lane)=>lane>=1&&lane<=Count?laneIds[lane-1]:throw new ArgumentOutOfRangeException(nameof(lane));
        public void Initialize(VenueAuthoringPlan plan)
        {
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            if(plan.UsesPhysicalPaths)Initialize(plan.Track,plan.Visual,plan.Paths,plan.Manifest.Range.Forward,plan.Manifest.Range.MatDefinition);
            else Initialize(plan.Track,plan.Visual,plan.Lanes);
        }
        public void Initialize(TrackDefinition track,TrackVisualDefinition visual,PhysicalRangeLaneDefinition lanes)
        {
            if(anchors!=null||track==null||visual==null||lanes==null||track.Id!=visual.TrackId||track.Id!=lanes.TrackId)
                throw new ArgumentException("Fresh range presentation and matching definitions required.");
            double stop=track.Range.FiringLine.Distance.Meters-lanes.Entry.Meters;
            var points=new TrackVisualPoint[lanes.Count];var rotations=new Quaternion[lanes.Count];var ids=new string[lanes.Count];
            for(int lane=0;lane<lanes.Count;lane++)
            {
                ids[lane]=lanes.GetLane(lane);points[lane]=visual.Get(ids[lane]).Sample(stop);
                rotations[lane]=Quaternion.LookRotation(new Vector3((float)points[lane].ForwardX,0,(float)points[lane].ForwardZ));
            }
            Create(points,rotations,ids);
        }
        public void Initialize(TrackDefinition track,TrackVisualDefinition visual,PhysicalRangePathDefinition paths,VenueVector rangeForward,VenueRangeMatDefinition matDefinition=null)
        {
            if(anchors!=null||track==null||visual==null||paths==null||rangeForward==null||
                track.Id!=visual.TrackId||track.Id!=paths.TrackId||track.ToJson()!=paths.TrackSnapshotJson)
                throw new ArgumentException("Fresh range presentation and matching physical path definitions required.");
            double x=rangeForward.X,y=rangeForward.Y,z=rangeForward.Z;
            if(!double.IsFinite(x)||!double.IsFinite(y)||!double.IsFinite(z)||Math.Abs(y)>1e-9||Math.Abs(x*x+z*z-1)>1e-9)
                throw new ArgumentException("A finite horizontal unit shooting direction is required.",nameof(rangeForward));
            if(matDefinition!=null)VenueRangeMatDefinition.FromJson(matDefinition.ToJson());
            var rotation=Quaternion.LookRotation(new Vector3((float)x,0,(float)z).normalized);
            var points=new TrackVisualPoint[paths.Count];var rotations=new Quaternion[paths.Count];var ids=new string[paths.Count];
            // Resolve every mat before adding any children; a missing final loop cannot leave a partial venue.
            for(int lane=0;lane<paths.Count;lane++)
            {
                var path=paths.GetLane(lane);ids[lane]=path.Id;
                points[lane]=visual.Get(path.MatAnchor.LoopId).Sample(path.MatAnchor.Distance.Meters);rotations[lane]=rotation;
            }
            Create(points,rotations,ids,matDefinition);
        }
        private void Create(TrackVisualPoint[] points,Quaternion[] rotations,string[] ids,VenueRangeMatDefinition matDefinition=null)
        {
            var art=StadiumArtDefinition.Load();var material=Resources.Load<Material>("Biathlon/Stadium/rubber");
            if(matDefinition!=null)
            {
                var shader=Resources.Load<Shader>("Biathlon/Stadium/PhysicalRangeMat");
                if(shader==null||material==null)throw new InvalidOperationException("Physical mat shader and rubber material required.");
                physicalMaterial=new Material(shader){name="Physical range rubber"};
                physicalMaterial.SetColor("_BaseColor",material.GetColor("_BaseColor"));material=physicalMaterial;
            }
            anchors=new Transform[points.Length];laneIds=ids;
            for(int lane=0;lane<points.Length;lane++)
            {
                var point=points[lane];
                var go=new GameObject("Physical shooting lane "+(lane+1));var anchor=go.transform;
                anchor.SetParent(transform,false);anchor.localPosition=new Vector3((float)point.X,(float)point.Y,(float)point.Z);
                anchor.localRotation=rotations[lane];anchors[lane]=anchor;
                var mat=GameObject.CreatePrimitive(PrimitiveType.Cube);mat.name="Shooting mat";mat.transform.SetParent(anchor,false);
                // Flush surface: never lift the athlete or move its authoritative stop point.
                mat.transform.localPosition=matDefinition==null?new Vector3(0,-.022f,0):
                    new Vector3(0,(float)(-matDefinition.Thickness*.5),(float)matDefinition.CentreForwardFromStop);
                mat.transform.localScale=matDefinition==null?new Vector3(art.mat_width_m,.05f,art.mat_length_m):
                    new Vector3((float)matDefinition.Width,(float)matDefinition.Thickness,(float)matDefinition.Length);
                mat.GetComponent<Renderer>().sharedMaterial=material;
                var collider=mat.GetComponent<Collider>();
                if(collider!=null){collider.enabled=false;Destroy(collider);}
            }
        }
        private void OnDestroy(){if(physicalMaterial!=null)Destroy(physicalMaterial);}
    }
}
