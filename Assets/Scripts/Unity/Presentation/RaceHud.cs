using System;
using System.Globalization;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class RaceHud : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private string athleteId;
        private RacePresentationSnapshot rendered;
        private GUIStyle style;
        public string DisplayText { get; private set; }="Waiting for race";
        public void Initialize(RacePresentationFeed source,string selected)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));if(string.IsNullOrWhiteSpace(selected))throw new ArgumentException("Stable athlete ID required.");
            feed=source;athleteId=selected;rendered=null;Refresh();
        }
        public void Refresh()
        {
            var snapshot=feed==null?null:feed.Current;
            if(snapshot==null){rendered=null;DisplayText="Waiting for race";return;}
            if(ReferenceEquals(rendered,snapshot))return;rendered=snapshot;DisplayText="Athlete unavailable";
            for(int i=0;i<snapshot.Count;i++)
            {
                var a=snapshot.Get(i);if(a.AthleteId!=athleteId)continue;var h=a.Hud;
                string position=h.Rank==0?"Timing pending":string.Format(CultureInfo.InvariantCulture,"{0} rank {1} / {2}  |  +{3:F2} s",h.Final?"FINAL":"Split "+(h.CheckpointOrdinal+1),h.Rank,h.ComparedAthletes,h.GapSeconds);
                DisplayText=string.Format(CultureInfo.InvariantCulture,"{0}  |  {1}\nLap {2} / {3}  |  {4}\n{5}\nShooting {6}",a.AthleteId,a.Phase,h.Lap,h.LapCount,h.NextRangeAfterLap>0?"Next range after lap "+h.NextRangeAfterLap:"All ranges complete",position,h.ShootingResults);return;
            }
        }
        private void LateUpdate()=>Refresh();
        private void OnGUI()
        {
            if(style==null)style=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,normal={textColor=Color.white}};
            float width=Mathf.Min(580,Screen.width-24),height=style.CalcHeight(new GUIContent(DisplayText),width-24)+24,x=Screen.width-width-12;
            GUI.Box(new Rect(x,12,width,height),GUIContent.none);GUI.Label(new Rect(x+12,24,width-24,height-24),DisplayText,style);
        }
    }
}
