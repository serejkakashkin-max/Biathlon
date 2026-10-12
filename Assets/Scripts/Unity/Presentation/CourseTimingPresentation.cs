using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Small timing posts outside the ski corridor; no collider or Update loop.</summary>
    public sealed class CourseTimingPresentation:MonoBehaviour
    {
        private Material material;
        public int TimingLines {get;private set;}
        public void Initialize(VenueAuthoringPlan venue)
        {
            var d=RaceReviewSettings.Load();material=new Material(Resources.Load<Material>("Biathlon/Stadium/white"));material.SetColor("_BaseColor",new Color(.05f,.43f,.47f));
            for(int i=0;i<venue.Track.CheckpointCount;i++)
            {
                var marker=venue.Track.GetCheckpoint(i);var point=venue.Visual.Get(marker.Anchor.LoopId).Sample(marker.Anchor.Distance.Meters);
                float half=(float)new TrackSampler(venue.Track.GetLoop(marker.Anchor.LoopId)).Sample(marker.Anchor.Distance).WidthMeters*.5f+d.checkpoint_post_offset_m;
                var center=new Vector3((float)point.X,(float)point.Y,(float)point.Z);var right=new Vector3((float)point.ForwardZ,0,(float)-point.ForwardX);
                for(int side=-1;side<=1;side+=2)
                {
                    var pole=GameObject.CreatePrimitive(PrimitiveType.Cube);pole.name="Timing post";pole.transform.SetParent(transform,false);
                    pole.transform.position=center+right*half*side+Vector3.up*d.checkpoint_post_height_m*.5f;pole.transform.localScale=new Vector3(.12f,d.checkpoint_post_height_m,.12f);
                    Destroy(pole.GetComponent<Collider>());pole.GetComponent<Renderer>().sharedMaterial=material;
                    var sign=new GameObject("Timing line sign",typeof(TextMesh));sign.transform.SetParent(transform,false);sign.transform.position=pole.transform.position+Vector3.up*.65f;
                    sign.transform.rotation=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ));
                    var text=sign.GetComponent<TextMesh>();text.text="SPLIT";text.fontSize=32;text.characterSize=.065f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
                }
                TimingLines++;
            }
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
