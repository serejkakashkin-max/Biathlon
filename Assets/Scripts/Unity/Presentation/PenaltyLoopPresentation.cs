using System;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Physical penalty-route guide and authoritative tick clock; never grants loop credit.</summary>
    public sealed class PenaltyLoopPresentation : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private string athleteId,loopId;
        private double length;
        private LineRenderer guide;
        private Material material;
        private RacePresentationSnapshot rendered;
        public bool Active { get; private set; }
        public string DisplayText { get; private set; }="";
        public int RemainingLoops { get; private set; }
        public double LoopProgress { get; private set; }
        public double RaceSeconds { get; private set; }
        public void Initialize(RacePresentationFeed source,string selected,TrackDefinition track,TrackVisualDefinition visual)
        {
            if(source==null||track==null||visual==null)throw new ArgumentNullException();
            if(string.IsNullOrWhiteSpace(selected)||visual.TrackId!=track.Id)throw new ArgumentException("Matching track and athlete ID required.");
            if(guide!=null)Destroy(guide.gameObject);if(material!=null)Destroy(material);
            feed=source;athleteId=selected;loopId=track.Penalty.LoopId;length=track.GetLoop(loopId).Length.Millimeters/1000.0;rendered=null;
            var route=visual.Get(loopId);var child=new GameObject("Penalty route guide");child.transform.SetParent(transform,false);guide=child.AddComponent<LineRenderer>();
            material=new Material(Resources.Load<Material>("Biathlon/GreyboxUnlit")){color=new Color(1,.6f,.1f)};guide.sharedMaterial=material;
            guide.useWorldSpace=false;guide.widthMultiplier=.25f;int steps=(int)Math.Ceiling(length/visual.SampleSpacing);guide.positionCount=steps+1;
            for(int i=0;i<=steps;i++){var p=route.Sample(length*i/steps);guide.SetPosition(i,new Vector3((float)p.X,(float)p.Y+.2f,(float)p.Z));}
            guide.enabled=false;Refresh();
        }
        public void Refresh()
        {
            var snapshot=feed==null?null:feed.Current;
            if(snapshot==null){rendered=null;SetInactive();RaceSeconds=0;return;}
            if(ReferenceEquals(rendered,snapshot))return;rendered=snapshot;SetInactive();RaceSeconds=snapshot.Tick/(double)snapshot.TickFrequency;
            for(int i=0;i<snapshot.Count;i++)
            {
                var a=snapshot.Get(i);if(a.AthleteId!=athleteId)continue;
                Active=a.OnCourse&&a.LoopId==loopId;guide.enabled=Active;if(!Active)return;
                RemainingLoops=a.PendingPenaltyLoops;LoopProgress=Math.Max(0,Math.Min(1,a.LocalNanometers/1e9/length));
                DisplayText=string.Format(CultureInfo.InvariantCulture,"PENALTY  |  {0} loops remaining\nThis loop {1:F1} / {2:F0} m  |  Race clock {3:F2} s",RemainingLoops,LoopProgress*length,length,RaceSeconds);return;
            }
        }
        private void SetInactive(){Active=false;RemainingLoops=0;LoopProgress=0;DisplayText="";if(guide!=null)guide.enabled=false;}
        private void LateUpdate()=>Refresh();
        private void OnGUI(){if(Active)GUI.Box(new Rect(Screen.width/2f-260,Screen.height-90,520,60),DisplayText);}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
