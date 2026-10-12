using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        public bool CheckpointTableVisible {get;private set;}
        private VisualElement checkpointPanel,readinessPanel,readinessPointer,checkpointNotice;
        private Label checkpointHeading,checkpointStatus,readinessTitle,readinessHint,noticeText;
        private readonly Label[] checkpointCells=new Label[30];
        private RaceReviewSettings reviewSettings;private PhysiologyDefinition readinessPhysiology;private BreathingDefinition readinessBreathing;
        private int noticedCheckpoint=-1;private double noticeUntil;
        private string L(string ru,string en)=>Locale=="ru"?ru:en;
        private void BuildReviewPanels()
        {
            reviewSettings=reviewSettings??RaceReviewSettings.Load();
            readinessPhysiology=readinessPhysiology??PhysiologyDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/physiology_gameplay_v2"));
            readinessBreathing=readinessBreathing??BreathingDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/breathing_gameplay_v2"));
            checkpointPanel=Card("checkpoint_table",true,true,900);checkpointPanel.style.maxWidth=Length.Percent(86);checkpointPanel.style.left=Length.Percent(7);checkpointPanel.style.top=130;
            checkpointHeading=Text(checkpointPanel,"checkpoint_heading",L("ПРОМЕЖУТОЧНЫЕ РЕЗУЛЬТАТЫ","INTERMEDIATE RESULTS"),settings.body_size,true);
            checkpointStatus=Text(checkpointPanel,"checkpoint_status","",settings.caption_size);
            var columns=Row(checkpointPanel);
            for(int c=0;c<2;c++)
            {
                var column=new VisualElement();column.style.width=Length.Percent(48);columns.Add(column);
                for(int r=0;r<15;r++){int i=c*15+r;checkpointCells[i]=Text(column,"checkpoint_row_"+i,"",settings.caption_size);checkpointCells[i].style.height=27*Scale;checkpointCells[i].style.whiteSpace=WhiteSpace.NoWrap;checkpointCells[i].style.overflow=Overflow.Hidden;checkpointCells[i].style.textOverflow=TextOverflow.Ellipsis;}
            }
            checkpointPanel.style.display=CheckpointTableVisible?DisplayStyle.Flex:DisplayStyle.None;
            checkpointNotice=Card("checkpoint_notice",true,true,380);checkpointNotice.style.left=Length.Percent(38);checkpointNotice.style.top=settings.edge;
            noticeText=Text(checkpointNotice,"checkpoint_notice_text","",settings.body_size,true);checkpointNotice.style.display=DisplayStyle.None;
            readinessPanel=Card("shooting_readiness",true,false,380);readinessPanel.style.left=Length.Percent(38);readinessPanel.style.bottom=settings.edge;
            readinessTitle=Text(readinessPanel,"readiness_title","",settings.body_size,true);
            var band=new VisualElement();band.style.height=8;band.style.flexDirection=FlexDirection.Row;band.style.marginTop=4;band.style.marginBottom=12;readinessPanel.Add(band);
            float[] widths={reviewSettings.calm_hr_load,reviewSettings.high_hr_load-reviewSettings.calm_hr_load,1-reviewSettings.high_hr_load};
            string[] colors={"#66D3AD","#F4CE76","#F59889"};
            for(int i=0;i<3;i++){var segment=new VisualElement();segment.style.width=Length.Percent(widths[i]*100);segment.style.height=8;segment.style.backgroundColor=FrontEndTheme.Color(colors[i]);band.Add(segment);}
            readinessPointer=new VisualElement{name="readiness_pointer"};readinessPointer.style.position=Position.Absolute;readinessPointer.style.top=-4;readinessPointer.style.height=16;readinessPointer.style.width=3;readinessPointer.style.backgroundColor=Color.white;band.Add(readinessPointer);
            readinessHint=Text(readinessPanel,"readiness_hint","",settings.caption_size);readinessPanel.style.display=DisplayStyle.None;
        }
        public void SetCheckpointTableVisible(bool visible)
        {CheckpointTableVisible=visible;if(checkpointPanel!=null)checkpointPanel.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;rendered=null;Refresh();}
        private void RefreshReviewPanels(AthletePresentationState a)
        {
            if(a.ShootingActive&&CheckpointTableVisible)
            {CheckpointTableVisible=false;checkpointPanel.style.display=DisplayStyle.None;}
            if(a.Hud.CheckpointOrdinal>noticedCheckpoint)
            {noticedCheckpoint=a.Hud.CheckpointOrdinal;noticeUntil=Time.unscaledTimeAsDouble+reviewSettings.checkpoint_notice_seconds;}
            checkpointNotice.style.display=a.Hud.Rank>0&&Time.unscaledTimeAsDouble<noticeUntil&&!CheckpointTableVisible?DisplayStyle.Flex:DisplayStyle.None;
            noticeText.text=L("ОТСЕЧКА  ","SPLIT  ")+a.Hud.Rank+" / "+a.Hud.ComparedAthletes+"    +"+(a.Hud.GapSeconds??0).ToString("0.0")+L(" с"," s");
            if(CheckpointTableVisible)
            {
                var table=host==null?null:CheckpointStandings.Capture(host.Runner.Field,a.Hud.CheckpointOrdinal);
                checkpointStatus.text=table==null||table.Count==0?L("Ожидаем первую отсечку · Tab закрыть","Waiting for the first split · Tab close"):
                    L("Отсечка ","Split ")+(table.Ordinal+1)+" · "+table.Count+" / "+host.Runner.Field.Count+L(" прошли · результаты обновляются · Tab закрыть"," through · provisional · Tab close");
                for(int i=0;i<checkpointCells.Length;i++)
                {
                    var label=checkpointCells[i];label.style.display=table!=null&&i<table.Count?DisplayStyle.Flex:DisplayStyle.None;if(table==null||i>=table.Count)continue;
                    var row=table.Get(i);string name=broadcastNames.TryGetValue(row.AthleteId,out var value)?value:row.AthleteId;
                    label.text=row.Rank.ToString("00")+"   "+name+"    "+GameplayHudModel.FormatClock(row.Seconds)+"   +"+row.GapSeconds.ToString("0.0");
                    label.tooltip=label.text;
                    label.style.color=row.AthleteId==athleteId?accent:foreground;
                }
            }
            bool near=a.ShootingActive;
            if(host!=null&&!near&&a.OnCourse&&host.IsShootingLap(a.Hud.Lap-1))
            {
                var entry=host.Venue.Track.Range.Entry;
                near=a.LoopId==entry.LoopId&&entry.Distance.Meters-a.LocalNanometers/(double)SkiState.NanometersPerMeter>=0&&
                    entry.Distance.Meters-a.LocalNanometers/(double)SkiState.NanometersPerMeter<=reviewSettings.range_guidance_m;
            }
            readinessPanel.style.display=near&&!CheckpointTableVisible?DisplayStyle.Flex:DisplayStyle.None;
            if(near)
            {
                var ready=new ShootingReadiness(a.Physiology.HeartRateBpm,a.Physiology.MuscularFatigue,readinessPhysiology,readinessBreathing,reviewSettings.calm_hr_load,reviewSettings.high_hr_load);
                string[] ru={"СПОКОЙНЫЙ ПУЛЬС","ПОВЫШЕННЫЙ ПУЛЬС","ВЫСОКИЙ ПУЛЬС"},en={"CALM HEART RATE","ELEVATED HEART RATE","HIGH HEART RATE"};
                readinessTitle.text=L(ru[ready.Zone],en[ready.Zone])+" · "+Math.Round(a.Physiology.HeartRateBpm);
                readinessTitle.style.color=FrontEndTheme.Color(ready.Zone==0?"#66D3AD":ready.Zone==1?"#F4CE76":"#F59889");readinessPointer.style.left=Length.Percent((float)ready.Load*100);
                readinessHint.text=L(ready.Zone==0?"Прицел спокойнее · Space — дыхание":ready.Zone==1?"Снизьте темп перед рубежом · Ctrl": "Сильнее колебания прицела · Ctrl — восстановление",
                    ready.Zone==0?"Calmer aim · Space to hold breath":ready.Zone==1?"Ease your pace before the range · Ctrl":"Stronger aim sway · Ctrl to recover");
            }
        }
    }
}
