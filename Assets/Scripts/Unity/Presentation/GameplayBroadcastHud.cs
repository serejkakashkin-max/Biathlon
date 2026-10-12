using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        public bool BroadcastVisible {get;private set;}
        private VisualElement broadcastPanel;
        private Label broadcastTitle;
        private Label[] broadcastRows;
        private int[] broadcastOrder;
        private readonly Dictionary<string,string> broadcastNames=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> broadcastNations=new Dictionary<string,string>(StringComparer.Ordinal);
        public void SetBroadcastRoster(CareerSave save)
        {broadcastNames.Clear();broadcastNations.Clear();for(int i=0;i<save.AthleteCount;i++){var a=save.Athlete(i);broadcastNames.Add(a.Id,a.Name);broadcastNations.Add(a.Id,a.Nation);}rendered=null;Refresh();}
        public void SetBroadcastVisible(bool value)
        {BroadcastVisible=value;if(broadcastPanel!=null)broadcastPanel.style.display=value?DisplayStyle.Flex:DisplayStyle.None;rendered=null;Refresh();}
        private void BuildBroadcast()
        {
            var policy=ReplayPresentationSettings.Load();broadcastRows=new Label[policy.tv_rows];
            broadcastPanel=Card("live_tv",true,true,settings.left_width);broadcastPanel.style.top=settings.edge+120*Scale;
            broadcastTitle=Text(broadcastPanel,"live_tv_title","",settings.caption_size,true);
            for(int i=0;i<broadcastRows.Length;i++)broadcastRows[i]=Text(broadcastPanel,"live_tv_row_"+i,"",settings.caption_size);
            broadcastPanel.style.display=BroadcastVisible?DisplayStyle.Flex:DisplayStyle.None;
        }
        private void RefreshBroadcast(RacePresentationSnapshot s)
        {
            if(!BroadcastVisible)return;
            if(broadcastOrder==null||broadcastOrder.Length!=s.Count)broadcastOrder=new int[s.Count];
            for(int i=0;i<s.Count;i++)broadcastOrder[i]=i;
            Array.Sort(broadcastOrder,(a,b)=>
            {
                var x=s.Get(a).Hud;var y=s.Get(b).Hud;
                int c=s.Complete?0:y.CheckpointOrdinal.CompareTo(x.CheckpointOrdinal);
                return c!=0?c:x.Rank!=y.Rank?x.Rank.CompareTo(y.Rank):string.CompareOrdinal(s.Get(a).AthleteId,s.Get(b).AthleteId);
            });
            broadcastTitle.text=text.Text(s.Complete?"tv.results":"tv.checkpoint",Locale);
            for(int i=0;i<broadcastRows.Length;i++)
            {
                broadcastRows[i].style.display=i<s.Count?DisplayStyle.Flex:DisplayStyle.None;if(i>=s.Count)continue;
                var a=s.Get(broadcastOrder[i]);string name=broadcastNames.TryGetValue(a.AthleteId,out var n)?n:a.AthleteId;
                string rank=a.Hud.Rank>0?a.Hud.Rank.ToString(CultureInfo.InvariantCulture):"—";
                string gap=a.Hud.GapSeconds.HasValue?"+"+a.Hud.GapSeconds.Value.ToString("0.0",CultureInfo.InvariantCulture):"—";
                string shots=a.Hud.ShootingResults;
                if(a.ShootingActive){shots="";for(int j=0;j<5;j++)shots+=(a.ClosedTargetsMask&(1<<j))!=0?"●":"○";}
                string penalty=a.PendingPenaltyLoops>0?" · "+string.Format(text.Text("tv.penalties",Locale),a.PendingPenaltyLoops):"";
                broadcastRows[i].text=rank+"  "+name+"  "+gap+"\n"+shots+penalty;
            }
        }
    }
}
