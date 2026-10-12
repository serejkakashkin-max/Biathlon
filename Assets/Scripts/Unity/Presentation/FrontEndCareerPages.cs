using System;
using Biathlon.Career;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private void RenderCareerPage(VisualElement actions,VisualElement card)
        {
            string key="page."+(int)Session.Screen;
            AddLabel(actions,T(key),theme.section_size,true);AddLabel(actions,Session.Player.Name,theme.body_size);
            AddLabel(actions,F("season_week",Session.Career.Season,Session.Career.Week),theme.caption_size);
            AddButton(actions,"back",T(Session.Screen==FrontEndScreen.Training?"training.keep":"back"),Back,true);
            var scroll=card as ScrollView;if(scroll!=null){scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.contentContainer.style.width=new Length(100,LengthUnit.Percent);}
            card.style.minWidth=0;AddLabel(card,T(key),theme.section_size,true);
            if(Session.Screen==FrontEndScreen.Training)RenderTraining(actions,card);
            else if(Session.Screen==FrontEndScreen.Equipment)RenderEquipment(actions,card);
            else if(Session.Screen==FrontEndScreen.Standings)RenderStandings(card);
            else if(Session.Screen==FrontEndScreen.History)RenderHistory(card);
            else if(Session.Screen==FrontEndScreen.Rivalries)RenderRivalries(card);
        }
        private void RenderTraining(VisualElement actions,VisualElement card)
        {
            var plan=Session.TrainingPlan();var preview=Session.TrainingPreview();int available=Session.Definitions.Calendar.TrainingSlots(Session.Career.Week);
            AddLabel(card,F("training.budget",plan.UsedSlots,available),theme.body_size,true).name="training_budget";
            AddLabel(card,F("training.forecast",Session.Player.Form/100,preview.Form/100,Session.Player.Fatigue/100,preview.Fatigue/100),theme.caption_size).name="training_forecast";
            AddLabel(card,T("training.forecast_note"),theme.caption_size);
            AddButton(actions,"training_clear",T("training.clear"),()=>{Session.ClearTraining();Render();},enabled:Session.TrainingCount>0);
            for(int i=0;i<Session.TrainingCount;i++)
            {int index=i;AddButton(actions,"training_remove_"+i,(i+1)+" · "+T("training.kind."+(int)Session.Training(i))+" ×",()=>{Session.RemoveTraining(index);Render();});}
            foreach(CareerTrainingKind kind in Enum.GetValues(typeof(CareerTrainingKind)))
            {
                var session=Session.Definitions.Training.Get(kind);var chosen=kind;
                var row=Section(card);AddLabel(row,T("training.kind."+(int)kind),theme.body_size,true);
                AddLabel(row,T("training.benefit."+(int)kind),theme.caption_size);
                AddLabel(row,F("training.cost",session.TimeSlots,session.FatigueCost/100.0,session.RecoveryGain/100.0,session.HealthRisk/100.0),theme.caption_size);
                AddButton(row,"add_training_"+(int)kind,T("training.add"),()=>{Session.AddTraining(chosen);Render();},enabled:plan.UsedSlots+session.TimeSlots<=available);
            }
        }
        private void RenderEquipment(VisualElement actions,VisualElement card)
        {
            AddLabel(actions,F("equipment.money",Session.Player.Money/100.0),theme.body_size).name="equipment_money";
            AddLabel(card,T("equipment.tradeoff_note"),theme.caption_size);var catalog=Session.EquipmentCatalog;
            for(int i=0;i<catalog.Count;i++)
            {
                var item=catalog.Get(i);string id=item.Id;var compare=CareerInventory.Compare(Session.Player,catalog,Session.Definitions.Economy,id,"packed_neutral");
                var row=Section(card);AddLabel(row,T("equipment."+id),theme.body_size,true);
                AddLabel(row,F("equipment.deltas",compare.GlideDelta*100,compare.GripDelta*100,compare.StabilityDelta*100),theme.caption_size);
                bool selected=Session.Player.SelectedSki==id;
                AddButton(row,"equip_"+id,selected?T("equipment.selected"):compare.Owned?T("equipment.select"):F("equipment.buy",compare.PurchasePrice/100.0),()=>CareerAction(()=>Session.Equip(id)),enabled:!selected&&Session.Player.Money>=compare.PurchasePrice);
            }
        }
        private void RenderStandings(VisualElement card)
        {
            var table=CareerStandings.Table(Session.Career,Session.Definitions.Scoring);
            if(Session.Career.RaceCount==0){AddLabel(card,T("history.no_races"),theme.body_size);return;}
            var rows=new VisualElement{name="standings_rows"};card.Add(rows);
            for(int i=0;i<table.Length;i++)
            {
                var row=table[i];var player=FindAthlete(row.AthleteId);var block=Section(rows);if(player.Id==Session.Career.PlayerId)block.style.backgroundColor=focus;
                AddLabel(block,(i+1)+" · "+player.Name+" · "+player.Nation,theme.body_size,true);
                AddLabel(block,F("history.standing_row",row.Points,row.Wins,row.Podiums,row.Races),theme.caption_size);
            }
        }
        private void RenderHistory(VisualElement card)
        {
            if(Session.Career.RaceCount==0&&Session.Career.HistoryCount==0){AddLabel(card,T("history.no_races"),theme.body_size);return;}
            var rows=new VisualElement{name="history_rows"};card.Add(rows);
            for(int i=Session.Career.RaceCount-1;i>=0;i--)
            {
                var record=Session.Career.Race(i);var row=Section(rows);AddLabel(row,T("format."+(int)record.Format),theme.body_size,true);
                for(int rank=0;rank<record.Count;rank++)if(record.Get(rank).AthleteId==Session.Career.PlayerId)
                {var f=record.Get(rank);AddLabel(row,F("history.race_row",rank+1,ResultClock(f.ElapsedMilliseconds),f.Penalties,Session.Definitions.Scoring.Points(rank+1)),theme.caption_size);}
            }
            for(int i=Session.Career.HistoryCount-1;i>=0;i--)
            {
                var season=Session.Career.History(i);var row=Section(rows);AddLabel(row,F("history.season",season.Season,season.RaceCount),theme.body_size,true);
                for(int rank=0;rank<season.Count;rank++)if(season.Get(rank).AthleteId==Session.Career.PlayerId)
                    AddLabel(row,F("history.season_result",rank+1,season.Get(rank).Points),theme.caption_size);
            }
        }
        private void RenderRivalries(VisualElement card)
        {
            AddLabel(card,T("rivalry.note"),theme.caption_size);var tracked=CareerHistory.Summarize(Session.Career,Session.Definitions.Scoring,Session.Definitions.History);
            int shown=0;var rows=new VisualElement{name="rivalry_rows"};card.Add(rows);
            for(int i=0;i<tracked.Count;i++)
            {
                var rival=tracked.Get(i);if(rival.AthleteId==Session.Career.PlayerId||rival.SharedRaces==0||rival.CloseFinishes==0&&rival.SharedPodiums==0&&!rival.StandingsBattle)continue;
                shown++;var row=Section(rows);AddLabel(row,rival.Name,theme.body_size,true);
                AddLabel(row,F("rivalry.facts",rival.SharedRaces,rival.CloseFinishes,rival.SharedPodiums),theme.caption_size);
                if(rival.StandingsBattle)AddLabel(row,T("rivalry.standings_battle"),theme.caption_size);
            }
            if(shown==0)AddLabel(card,T("rivalry.empty"),theme.body_size);
        }
        private CareerAthlete FindAthlete(string id)
        {for(int i=0;i<Session.Career.AthleteCount;i++)if(Session.Career.Athlete(i).Id==id)return Session.Career.Athlete(i);throw new ArgumentException("Unknown saved athlete.");}
        private VisualElement Section(VisualElement parent)
        {var row=new VisualElement();row.style.paddingTop=theme.spacing;row.style.paddingBottom=theme.spacing;row.style.borderBottomWidth=1;row.style.borderBottomColor=focus;parent.Add(row);return row;}
    }
}
