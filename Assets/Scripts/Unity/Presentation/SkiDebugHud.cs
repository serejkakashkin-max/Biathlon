using System;
using System.Globalization;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Read-only greybox telemetry. Physical position is not an elapsed-time race rank.</summary>
    public sealed class SkiDebugHud : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private string athleteId;
        private RacePresentationSnapshot rendered;
        private GUIStyle style;
        public string DisplayText { get; private set; } = "Waiting for race snapshot";
        public void Initialize(RacePresentationFeed source, string selectedAthleteId)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            if(string.IsNullOrWhiteSpace(selectedAthleteId))throw new ArgumentException("Stable athlete ID required.");
            feed=source;athleteId=selectedAthleteId;rendered=null;Refresh();
        }
        public void Refresh()
        {
            var snapshot=feed==null?null:feed.Current;
            if(snapshot==null){DisplayText="Waiting for race snapshot";rendered=null;return;}
            if(ReferenceEquals(snapshot,rendered))return;
            rendered=snapshot;DisplayText="Selected athlete unavailable: "+athleteId;
            for(int i=0;i<snapshot.Count;i++)
            {
                var a=snapshot.Get(i);if(a.AthleteId!=athleteId)continue;
                DisplayText=string.Format(CultureInfo.InvariantCulture,
                    "SKI TELEMETRY  |  {0}  |  tick {1}\nSpeed   {2:F1} km/h\nEffort  {3:F0}% sustainable  |  {4:F0} W\nHR      {5:F0} bpm\nReserve {6:F0}%  |  Fatigue {7:F0}%\nDraft   {8:F0}% drag reduction  |  {9}\nPosition {10}  {11:F1} m  |  lateral {12:F1} m\nPhase   {13}",
                    a.AthleteId,snapshot.Tick,a.SpeedMetersPerSecond*3.6,a.Effort.DeliveredRelativeEffort*100,
                    a.Effort.DeliveredPowerWatts,a.Physiology.HeartRateBpm,a.Physiology.AnaerobicReserve*100,
                    a.Physiology.MuscularFatigue*100,a.Draft.DragReduction*100,a.Draft.LeaderId.IsValid?a.Draft.LeaderId.Value:"no shelter",
                    a.LoopId,a.LocalNanometers/(double)Biathlon.Domain.SkiState.NanometersPerMeter,a.LateralMeters,a.Phase);
                return;
            }
        }
        private void LateUpdate()=>Refresh();
        private void OnGUI()
        {
            if(style==null)style=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,normal={textColor=Color.white}};
            float width=Mathf.Min(600,Screen.width-24);
            float height=style.CalcHeight(new GUIContent(DisplayText),width-24)+24;
            GUI.Box(new Rect(12,12,width,height),GUIContent.none);
            GUI.Label(new Rect(24,24,width-24,height-24),DisplayText,style);
        }
    }
}
