using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        public bool CheckpointTableVisible {get;private set;}
        private readonly CheckpointHistorySelection checkpointHistory=new CheckpointHistorySelection();
        public int SelectedCheckpointOrdinal=>checkpointHistory.Selected;
        public bool FollowingLatestCheckpoint=>checkpointHistory.FollowsLatest;
        private CheckpointLayout checkpointHistoryLayout;
        private VisualElement checkpointPanel,readinessPanel,readinessPointer,checkpointNotice;
        private Label checkpointHeading,checkpointStatus,checkpointPrevious,checkpointNext,checkpointMode,readinessTitle,readinessHint;
        private Label liveSplitHeading,liveSplitTime,liveSplitDifference,liveSplitReference;
        private readonly TimingAthleteRow[] checkpointCells=new TimingAthleteRow[30];
        private RaceReviewSettings reviewSettings;private PhysiologyDefinition readinessPhysiology;private BreathingDefinition readinessBreathing;
        private static readonly string[] ReadinessRu={"СПОКОЙНЫЙ ПУЛЬС","ПОВЫШЕННЫЙ ПУЛЬС","ВЫСОКИЙ ПУЛЬС"},
            ReadinessEn={"CALM HEART RATE","ELEVATED HEART RATE","HIGH HEART RATE"};
        private string L(string ru,string en)=>Locale=="ru"?ru:en;
        private void BuildReviewPanels()
        {
            reviewSettings=reviewSettings??RaceReviewSettings.Load();
            readinessPhysiology=readinessPhysiology??PhysiologyDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/physiology_gameplay_v2"));
            readinessBreathing=readinessBreathing??BreathingDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/breathing_gameplay_v2"));
            // Prepare occurrence labels with the HUD during loading, not on the first TAB press.
            if(checkpointHistoryLayout==null&&host?.Runner!=null)checkpointHistoryLayout=new CheckpointLayout(host.Runner.Field.Track,host.Runner.Field.Route);
            checkpointPanel=Card("checkpoint_table",true,true,1320);checkpointPanel.style.width=Length.Percent(86);checkpointPanel.style.maxWidth=Length.Percent(86);checkpointPanel.style.left=Length.Percent(7);checkpointPanel.style.top=108;
            checkpointPanel.style.backgroundColor=HighContrast?Color.black:FrostlineStyle.Deep;
            checkpointPanel.style.paddingTop=checkpointPanel.style.paddingBottom=10;checkpointPanel.style.paddingLeft=checkpointPanel.style.paddingRight=20;
            var navigation=Row(checkpointPanel);navigation.name="checkpoint_navigation";
            checkpointPrevious=Text(navigation,"checkpoint_previous","‹",settings.body_size,true);checkpointPrevious.style.flexShrink=0;
            checkpointHeading=Text(navigation,"checkpoint_heading","",settings.body_size,true);
            checkpointHeading.style.letterSpacing=1;checkpointHeading.style.color=accent;
            checkpointHeading.style.unityTextAlign=TextAnchor.MiddleCenter;checkpointHeading.style.flexGrow=1;
            checkpointNext=Text(navigation,"checkpoint_next","›",settings.body_size,true);checkpointNext.style.flexShrink=0;
            checkpointMode=Text(checkpointPanel,"checkpoint_mode","",settings.caption_size,true);checkpointMode.style.color=accent;
            checkpointStatus=Text(checkpointPanel,"checkpoint_status","",settings.caption_size);
            var columns=Row(checkpointPanel);
            columns.style.alignItems=Align.FlexStart;
            for(int c=0;c<2;c++)
            {
                var column=new VisualElement();column.style.width=Length.Percent(48);columns.Add(column);
                var header=Row(column);header.name="checkpoint_columns_"+c;header.style.paddingLeft=9;header.style.paddingRight=8;header.style.marginBottom=6;
                var who=Text(header,"split_athlete_header_"+c,L("МЕСТО / СПОРТСМЕН","RANK / ATHLETE"),settings.caption_size);who.style.color=muted;who.style.flexGrow=1;
                var elapsed=Text(header,"split_time_header_"+c,L("ВРЕМЯ","TIME"),settings.caption_size);elapsed.style.width=66*Scale;elapsed.style.color=muted;elapsed.style.unityTextAlign=TextAnchor.MiddleRight;
                var gap=Text(header,"split_gap_header_"+c,"+",settings.caption_size);gap.style.width=62*Scale;gap.style.color=muted;gap.style.unityTextAlign=TextAnchor.MiddleRight;
                for(int r=0;r<15;r++){int i=c*15+r;checkpointCells[i]=new TimingAthleteRow(Scale){name="checkpoint_row_"+i};checkpointCells[i].style.height=29*Scale;checkpointCells[i].style.marginBottom=1;FrostlineStyle.Text(checkpointCells[i]);column.Add(checkpointCells[i]);}
            }
            checkpointPanel.style.display=CheckpointTableVisible?DisplayStyle.Flex:DisplayStyle.None;
            checkpointNotice=Card("checkpoint_notice",true,true,380);
            checkpointNotice.style.left=Length.Percent(35);checkpointNotice.style.width=Length.Percent(30);
            checkpointNotice.style.display=DisplayStyle.None;
            liveSplitHeading=Text(checkpointNotice,"live_split_heading","",settings.caption_size,true);liveSplitHeading.style.color=accent;
            var splitLine=Row(checkpointNotice);
            liveSplitTime=Text(splitLine,"live_split_time","",settings.number_size,true);
            liveSplitDifference=Text(splitLine,"live_split_difference","",settings.body_size,true);
            liveSplitReference=Text(checkpointNotice,"live_split_reference","",settings.caption_size);
            liveSplitReference.style.color=muted;liveSplitReference.style.whiteSpace=WhiteSpace.NoWrap;
            liveSplitReference.style.overflow=Overflow.Hidden;liveSplitReference.style.textOverflow=TextOverflow.Ellipsis;
            readinessPanel=Card("shooting_readiness",false,false,settings.right_width);readinessPanel.style.bottom=settings.edge+168*Scale;
            readinessTitle=Text(readinessPanel,"readiness_title","",settings.caption_size,true);
            var band=new VisualElement();band.style.height=8;band.style.flexDirection=FlexDirection.Row;band.style.marginTop=4;band.style.marginBottom=12;readinessPanel.Add(band);
            float[] widths={reviewSettings.calm_hr_load,reviewSettings.high_hr_load-reviewSettings.calm_hr_load,1-reviewSettings.high_hr_load};
            string[] colors={"#66D3AD","#F4CE76","#F59889"};
            for(int i=0;i<3;i++){var segment=new VisualElement();segment.style.width=Length.Percent(widths[i]*100);segment.style.height=8;segment.style.backgroundColor=FrontEndTheme.Color(colors[i]);band.Add(segment);}
            readinessPointer=new VisualElement{name="readiness_pointer"};readinessPointer.style.position=Position.Absolute;readinessPointer.style.top=-4;readinessPointer.style.height=16;readinessPointer.style.width=3;readinessPointer.style.backgroundColor=Color.white;band.Add(readinessPointer);
            readinessHint=Text(readinessPanel,"readiness_hint","",settings.caption_size);readinessPanel.style.display=DisplayStyle.None;
        }
        public void SetCheckpointTableVisible(bool visible)
        {if(visible&&!CheckpointTableVisible)checkpointHistory.FollowLatest();CheckpointTableVisible=visible;if(checkpointPanel!=null)checkpointPanel.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;rendered=null;Refresh();}
        public void MoveCheckpointHistory(int direction)
        {if(!CheckpointTableVisible)return;if(checkpointHistory.Move(direction)){rendered=null;Refresh();}}
        public void FollowLatestCheckpoint()
        {if(!CheckpointTableVisible)return;checkpointHistory.FollowLatest();rendered=null;Refresh();}
        public void CaptureCheckpointHistoryInput(Keyboard keyboard,Gamepad gamepad)
        {
            if(!isActiveAndEnabled||!CheckpointTableVisible||Model.Shooting)return;
            if(keyboard?.pageUpKey.wasPressedThisFrame==true||gamepad?.rightStick.left.wasPressedThisFrame==true)MoveCheckpointHistory(-1);
            else if(keyboard?.pageDownKey.wasPressedThisFrame==true||gamepad?.rightStick.right.wasPressedThisFrame==true)MoveCheckpointHistory(1);
            else if(keyboard?.endKey.wasPressedThisFrame==true||gamepad?.rightStickButton.wasPressedThisFrame==true)FollowLatestCheckpoint();
        }
        private void RefreshReviewPanels(AthletePresentationState a)
        {
            checkpointHistory.Observe(feed.Current.Tick,a.Hud.CheckpointOrdinal);
            if(a.ShootingActive&&CheckpointTableVisible)
            {CheckpointTableVisible=false;checkpointPanel.style.display=DisplayStyle.None;}
            motionPanel.style.visibility=conditionPanel.style.visibility=CheckpointTableVisible?Visibility.Hidden:Visibility.Visible;
            RefreshLiveSplit(a);
            if(CheckpointTableVisible)
            {
                var table=host==null?null:CheckpointStandings.Capture(host.Runner.Field,checkpointHistory.Selected);
                checkpointHeading.text=checkpointHistory.Selected<0?L("ОТСЕЧКИ","SPLITS"):L("ОТСЕЧКА ","SPLIT ")+(checkpointHistory.Selected+1)+" / "+(checkpointHistory.Latest+1);
                checkpointPrevious.style.opacity=checkpointHistory.Selected>0?1:.3f;checkpointNext.style.opacity=checkpointHistory.Selected<checkpointHistory.Latest?1:.3f;
                checkpointMode.text=checkpointHistory.FollowsLatest?L("ПОСЛЕДНЯЯ ОТСЕЧКА","LATEST SPLIT"):L("ИСТОРИЯ ОТСЕЧЕК","SPLIT HISTORY");checkpointMode.style.color=checkpointHistory.FollowsLatest?accent:FrostlineStyle.Gold;
                string location="";
                if(host!=null&&checkpointHistory.Selected>=0)
                {
                    var occurrence=checkpointHistoryLayout.Get(checkpointHistory.Selected);int lap=host.Runner.Field.Route.GetSection(occurrence.SectionIndex).LapIndex+1;
                    location=L("КРУГ ","LAP ")+lap+" · "+host.DistanceView.Kilometers(occurrence.RouteNanometers/(double)SkiState.NanometersPerMeter,System.Globalization.CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US"))+L(" КМ · "," KM · ");
                }
                checkpointStatus.text=table==null||table.Count==0?L("Ожидаем первую отсечку","Waiting for the first split"):
                    location+table.Count+" / "+host.Runner.Field.Count+L(" прошли · результаты обновляются"," through · provisional");
                for(int i=0;i<checkpointCells.Length;i++)
                {
                    var label=checkpointCells[i];label.style.display=table!=null&&i<table.Count?DisplayStyle.Flex:DisplayStyle.None;if(table==null||i>=table.Count)continue;
                    var row=table.Get(i);string name=broadcastNames.TryGetValue(row.AthleteId,out var value)?value:row.AthleteId;
                    string nation=broadcastNations.TryGetValue(row.AthleteId,out var code)?code:"";
                    var role=host.Proxies?.LeaderRole(row.AthleteId)??Biathlon.Career.CareerLeaderBib.None;
                    string description=role==(Biathlon.Career.CareerLeaderBib.Total|Biathlon.Career.CareerLeaderBib.Discipline)?L("Лидер общего зачёта и дисциплины","Overall and discipline leader"):
                        role==Biathlon.Career.CareerLeaderBib.Total?L("Лидер общего зачёта","Overall leader"):role==Biathlon.Career.CareerLeaderBib.Discipline?L("Лидер дисциплины","Discipline leader"):"";
                    label.BindCheckpoint(row.AthleteId,row.Rank,name,nation,row.Seconds,row.GapSeconds,Locale,row.AthleteId==athleteId,role,description);
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
                readinessTitle.text=L(ReadinessRu[ready.Zone],ReadinessEn[ready.Zone])+" · "+Math.Round(a.Physiology.HeartRateBpm);
                readinessTitle.style.color=FrontEndTheme.Color(ready.Zone==0?"#66D3AD":ready.Zone==1?"#F4CE76":"#F59889");readinessPointer.style.left=Length.Percent((float)ready.Load*100);
                readinessHint.text=L(ready.Zone==0?"Прицел устойчив":ready.Zone==1?"Умеренная тряска":"Сильная тряска",
                    ready.Zone==0?"Steady aim":ready.Zone==1?"Moderate sway":"Strong sway");
            }
        }

        private void RefreshLiveSplit(AthletePresentationState athlete)
        {
            var split=host==null||CheckpointTableVisible||helpVisible?default:
                LiveSplit.Capture(host.Runner.Field,checkpointHistoryLayout,selectedIndex,athlete,
                    reviewSettings.checkpoint_approach_m,reviewSettings.checkpoint_notice_seconds);
            checkpointNotice.style.display=split.Visible?DisplayStyle.Flex:DisplayStyle.None;
            if(!split.Visible)return;

            var culture=System.Globalization.CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US");
            string distance=host.DistanceView.Kilometers(split.RouteMeters,culture);
            liveSplitHeading.text=L("ОТСЕЧКА ","SPLIT ")+distance+L(" КМ"," KM")+(split.Crossed
                ?L(" · МЕСТО "," · RANK ")+split.Rank+" / "+split.Arrivals
                :L(" · ПОДЪЕЗД"," · APPROACH"));
            liveSplitTime.text=GameplayHudModel.FormatClock(split.Seconds);
            double? difference=split.DifferenceSeconds;
            string gap=difference.HasValue?GameplayHudModel.FormatGap(Math.Abs(difference.Value),culture):"";
            liveSplitDifference.text=difference<0?"−"+gap.Substring(1):gap;
            liveSplitDifference.style.color=difference>0?(HighContrast?Color.white:FrostlineStyle.Danger):accent;
            if(split.LeaderSeconds.HasValue)
            {
                string name=broadcastNames.TryGetValue(split.LeaderId,out var leader)?leader:split.LeaderId==athleteId?L("ВЫ","YOU"):null;
                liveSplitReference.text=L("ЛИДЕР ","LEADER ")+GameplayHudModel.FormatClock(split.LeaderSeconds.Value)+
                    (string.IsNullOrEmpty(name)?"":" · "+name);
            }
            else liveSplitReference.text=L("Ещё нет времени лидера","No time to beat yet");
        }
    }
}
