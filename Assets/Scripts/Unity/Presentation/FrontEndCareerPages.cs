using System;
using Biathlon.Career;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private int historyPage,rivalryPage;
        private void RenderCareerPage(VisualElement actions,VisualElement card)
        {
            string key="page."+(int)Session.Screen;
            AddLabel(actions,T(key),theme.section_size,true);AddLabel(actions,Session.Player.Name,theme.body_size);
            AddLabel(actions,F("season_week",Session.Career.Season,Session.Career.Week),theme.caption_size);
            if(Session.Screen==FrontEndScreen.History)AddButton(actions,"career_development",L("Мой прогресс","My development"),()=>{OpenAthleteCard(Session.Career.PlayerId);careerCardTab=2;Render();});
            AddButton(actions,"back",T(Session.Screen==FrontEndScreen.Training?"training.keep":"back"),Back,true);
            var scroll=card as ScrollView;if(scroll!=null){scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.contentContainer.style.width=new Length(100,LengthUnit.Percent);}
            card.style.minWidth=0;
            var pageTitle=AddLabel(card,T(key).ToUpperInvariant(),theme.caption_size,true);pageTitle.style.letterSpacing=2;pageTitle.style.color=FrostlineStyle.Gold;
            if(Session.Screen==FrontEndScreen.Training)RenderTraining(actions,card);
            else if(Session.Screen==FrontEndScreen.Equipment)RenderEquipment(actions,card);
            else if(Session.Screen==FrontEndScreen.Standings)RenderStandings(card);
            else if(Session.Screen==FrontEndScreen.History)RenderHistory(card);
            else if(Session.Screen==FrontEndScreen.Rivalries)RenderRivalries(card);
            else if(Session.Screen==FrontEndScreen.Replays)RenderReplays(card);
        }
        private void RenderTraining(VisualElement actions,VisualElement card)
        {
            var plan=Session.TrainingPlan();var preview=Session.TrainingPreview();int available=Session.Definitions.Calendar.TrainingSlots(Session.Career.Week);
            AddLabel(card,F("training.budget",plan.UsedSlots,available),theme.body_size,true).name="training_budget";
            var slots=new VisualElement{name="training_slots"};slots.style.flexDirection=FlexDirection.Row;slots.style.marginBottom=14;card.Add(slots);
            for(int slot=0;slot<available;slot++)
            {
                var cell=new VisualElement();cell.style.flexGrow=1;cell.style.height=8;cell.style.marginRight=5;
                cell.style.backgroundColor=slot<plan.UsedSlots?FrostlineStyle.Accent:FrostlineStyle.Surface;slots.Add(cell);
            }
            var forecast=TileGrid(card,"training_forecast");
            ForecastInstrument(forecast,L("ФОРМА К СТАРТУ","FORM AT START"),Session.Player.Form/100,preview.Form/100,FrostlineStyle.Accent);
            ForecastInstrument(forecast,L("УСТАЛОСТЬ К СТАРТУ","FATIGUE AT START"),Session.Player.Fatigue/100,preview.Fatigue/100,FrostlineStyle.Gold);
            RenderTrainingAbilityForecast(card,preview);
            AddButton(actions,"training_clear",T("training.clear"),()=>{Session.ClearTraining();Render();},enabled:Session.TrainingCount>0);
            for(int i=0;i<Session.TrainingCount;i++)
            {int index=i;AddButton(actions,"training_remove_"+i,(i+1)+" · "+T("training.kind."+(int)Session.Training(i))+" ×",()=>{Session.RemoveTraining(index);Render();});}
            var trainingTiles=TileGrid(card,"training_tiles");
            foreach(CareerTrainingKind kind in Enum.GetValues(typeof(CareerTrainingKind)))
            {
                var session=Session.Definitions.Training.Get(kind);var chosen=kind;
                var row=Tile(trainingTiles);row.style.paddingTop=row.style.paddingBottom=12;row.style.width=Length.Percent(48.5f);
                var mark=new VisualElement();mark.style.width=32;mark.style.height=3;mark.style.backgroundColor=kind==CareerTrainingKind.Recovery?FrostlineStyle.Gold:FrostlineStyle.Accent;mark.style.marginBottom=10;row.Add(mark);
                AddLabel(row,T("training.kind."+(int)kind),theme.body_size,true);
                AddLabel(row,T("training.benefit."+(int)kind),theme.caption_size);
                AddLabel(row,F("training.cost",session.TimeSlots,session.FatigueCost/100.0,session.RecoveryGain/100.0,session.HealthRisk/100.0),theme.caption_size).style.color=FrostlineStyle.Muted;
                AddButton(row,"add_training_"+(int)kind,T("training.add"),()=>{Session.AddTraining(chosen);Render();},primary:true,enabled:plan.UsedSlots+session.TimeSlots<=available);
            }
            PageRows(card,trainingTiles,trainingPage,2,value=>trainingPage=value);
            AddLabel(card,L("План применяется при завершении недели","Plan applies when you complete the week"),theme.caption_size).tooltip=T("training.forecast_note");
        }
        private void RenderStandings(VisualElement card)
        {
            var table=CareerStandings.Table(Session.Career,Session.Definitions.Scoring);
            AddNationalKitLegend(card);var leaders=NextStartLeaders();
            if(Session.Career.RaceCount==0){AddLabel(card,T("history.no_races"),theme.body_size);return;}
            ResultTableRow(card,"standings_header",new[]{"#",L("Спортсмен","Athlete"),"OVR",L("Гонки","Races"),L("Победы","Wins"),L("Подиумы","Podiums"),L("Очки","Points")},true,false);
            var rows=new VisualElement{name="standings_rows"};card.Add(rows);
            for(int i=0;i<table.Length;i++)
            {
                var row=table[i];var player=FindAthlete(row.AthleteId);
                ResultTableRow(rows,"standing_"+i,new[]{(i+1).ToString(),player.Name,BiathlonRatingEngine.Calculate(player,Session.Definitions.Ratings).Overall.ToString(),row.Races.ToString(),row.Wins.ToString(),row.Podiums.ToString(),row.Points.ToString()},false,player.Id==Session.Career.PlayerId,player.Nation,leaders?.ForAthlete(player.Id)??CareerLeaderBib.None);
                var visual=rows[rows.childCount-1];visual.focusable=true;visual.tooltip=L("Открыть досье: ","Open dossier: ")+player.Name;controls.Add(visual);
                visual.RegisterCallback<FocusInEvent>(e=>{visual.style.borderBottomColor=FrostlineStyle.Accent;visual.style.borderBottomWidth=3;});
                visual.RegisterCallback<FocusOutEvent>(e=>{visual.style.borderBottomColor=FrostlineStyle.Surface;visual.style.borderBottomWidth=1;});
                string athleteId=player.Id;visual.RegisterCallback<ClickEvent>(e=>OpenAthleteCard(athleteId));visual.RegisterCallback<NavigationSubmitEvent>(e=>{OpenAthleteCard(athleteId);e.StopPropagation();});
            }
            PageRows(card,rows,standingsPage,compactLayout?5:9,value=>standingsPage=value);
        }

        private void RenderHistory(VisualElement card)
        {
            if(Session.Career.RaceCount==0&&Session.Career.HistoryCount==0){AddLabel(card,T("history.no_races"),theme.body_size);return;}
            var rows=TileGrid(card,"history_rows");
            for(int i=Session.Career.RaceCount-1;i>=0;i--)
            {
                var record=Session.Career.Race(i);var row=Tile(rows);row.style.width=Length.Percent(48.5f);AddLabel(row,T("format."+(int)record.Format),theme.body_size,true);
                for(int rank=0;rank<record.Count;rank++)if(record.Get(rank).AthleteId==Session.Career.PlayerId)
                {var f=record.Get(rank);AddLabel(row,"#"+(rank+1),theme.section_size,true).style.color=rank<3?FrostlineStyle.Gold:FrostlineStyle.Accent;AddLabel(row,F("history.race_row",rank+1,ResultClock(f.ElapsedMilliseconds),f.Penalties,Session.Definitions.Scoring.Points(rank+1)),theme.caption_size);}
            }
            for(int i=Session.Career.HistoryCount-1;i>=0;i--)
            {
                var season=Session.Career.History(i);var row=Tile(rows);row.style.width=Length.Percent(48.5f);AddLabel(row,F("history.season",season.Season,season.RaceCount),theme.body_size,true);
                for(int rank=0;rank<season.Count;rank++)if(season.Get(rank).AthleteId==Session.Career.PlayerId)
                    AddLabel(row,F("history.season_result",rank+1,season.Get(rank).Points),theme.caption_size);
            }
            PageRows(card,rows,historyPage,compactLayout?2:4,value=>historyPage=value);
        }
        private void RenderRivalries(VisualElement card)
        {
            RenderCareerRoster(card);
        }
        private CareerAthlete FindAthlete(string id)
        {return Session.Career.FindWorldAthlete(id)??throw new ArgumentException("Unknown saved athlete.");}
        private void RenderReplays(VisualElement card)
        {
            AddLabel(card,T("replay.note"),theme.caption_size);int shown=0;
            foreach(var info in ReplayLibrary.List())
            {
                if(info.PlayerId!=Session.Career.PlayerId)continue;shown++;var row=Section(card);
                row.style.borderLeftWidth=3;row.style.borderLeftColor=FrostlineStyle.Gold;
                AddLabel(row,T("format."+info.Format)+" · "+info.PlayerName,theme.body_size,true);
                AddLabel(row,DateTimeOffset.Parse(info.RecordedUtc).ToLocalTime().ToString("g")+" · "+ResultClock(info.TickCount*1000/info.TickFrequency),theme.caption_size);
                AddButton(row,"replay_"+info.Id,T(Replays.Loading?"loading":"replay.watch"),()=>Replays.Launch(this,info),enabled:!Replays.Loading);
                var moments=new Foldout{text=T("replay.moments"),value=false};moments.AddToClassList("frostline-highlights");row.Add(moments);
                int count=0;foreach(var moment in info.Highlights)
                {
                    count++;var chosen=moment;
                    AddButton(moments,"highlight_"+info.Id+"_"+count,T("highlight."+(int)moment.Kind)+" · "+ResultClock(moment.Tick*1000/info.TickFrequency),()=>Replays.Launch(this,info,chosen.Tick),enabled:!Replays.Loading);
                }
            }
            if(shown==0)AddLabel(card,T("replay.empty"),theme.body_size);
        }
        private VisualElement Section(VisualElement parent)
        {return Tile(parent);}
        private void ForecastInstrument(VisualElement parent,string label,int before,int after,Color color)
        {
            var tile=Tile(parent);tile.style.width=Length.Percent(48.5f);tile.style.paddingTop=tile.style.paddingBottom=10;
            AddLabel(tile,label,theme.caption_size).style.color=FrostlineStyle.Muted;
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;tile.Add(row);
            var from=AddLabel(row,before+"%",theme.body_size);from.style.color=FrostlineStyle.Muted;from.style.marginRight=10;
            AddLabel(row,"→",theme.body_size).style.marginRight=10;
            AddLabel(row,after+"%",theme.section_size,true).style.color=color;
            var axis=new VisualElement();axis.style.height=5;axis.style.backgroundColor=FrostlineStyle.Deep;axis.style.marginBottom=4;tile.Add(axis);
            var fill=new VisualElement();fill.style.width=Length.Percent(Mathf.Clamp(after,0,100));fill.style.height=5;fill.style.backgroundColor=color;axis.Add(fill);
            var baseline=new VisualElement();baseline.style.position=Position.Absolute;baseline.style.left=Length.Percent(Mathf.Clamp(before,0,100));baseline.style.width=2;baseline.style.top=-3;baseline.style.height=11;baseline.style.backgroundColor=FrostlineStyle.Ink;axis.Add(baseline);
            axis.tooltip=L("Риска — текущее состояние; полоса — прогноз плана","Marker: current state; bar: plan forecast");
        }
    }
}
