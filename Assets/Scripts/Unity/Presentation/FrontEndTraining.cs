using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Career;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private void RenderTraining(VisualElement actions,VisualElement card)
        {
            var plan=Session.TrainingPlan();var preview=Session.TrainingPreview();int available=Session.Definitions.Calendar.TrainingSlots(Session.Career.Week);var culture=CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US");
            AddLabel(card,L("ПОСТРОЙТЕ СВОЮ НЕДЕЛЮ","BUILD YOUR TRAINING WEEK"),theme.section_size,true);
            AddLabel(card,F("training.budget",plan.UsedSlots,available),theme.body_size,true).name="training_budget";
            var slots=new VisualElement{name="training_slots"};slots.style.flexDirection=FlexDirection.Row;slots.style.marginBottom=14;card.Add(slots);
            for(int i=0;i<available;i++){var cell=new VisualElement();cell.style.flexGrow=1;cell.style.height=8;cell.style.marginRight=5;cell.style.backgroundColor=i<plan.UsedSlots?FrostlineStyle.Accent:FrostlineStyle.Surface;slots.Add(cell);}
            var forecast=TileGrid(card,"training_forecast");
            ForecastInstrument(forecast,L("ФОРМА К СТАРТУ","FORM AT START"),Session.Player.Form/100,preview.Form/100,FrostlineStyle.Accent);
            ForecastInstrument(forecast,L("УСТАЛОСТЬ К СТАРТУ","FATIGUE AT START"),Session.Player.Fatigue/100,preview.Fatigue/100,FrostlineStyle.Gold);
            RenderTrainingAbilityForecast(card,preview);
            AddLabel(card,L("Здоровье ","Health ")+(Session.Player.Health/100.0).ToString("0.#",culture)+" → "+(preview.Health/100.0).ToString("0.#",culture)+"% · "+L("Риск ","Risk ")+(Session.Player.HealthRisk/100.0).ToString("0.#",culture)+" → "+(preview.HealthRisk/100.0).ToString("0.#",culture)+"%",theme.caption_size).name="training_health_forecast";
            var changes=new List<string>();string[] names=Locale=="ru"?StatRu:StatEn;
            for(int i=0;i<CareerUnits.StatCount;i++){int delta=preview.Stat((CareerStat)i)-Session.Player.Stat((CareerStat)i);if(delta!=0)changes.Add(names[i]+" "+(delta/1000.0).ToString("+0.000;-0.000",culture));}
            AddLabel(card,changes.Count==0?L("Навыки: без изменений в этом прогнозе","Abilities: no change in this forecast"):string.Join(" · ",changes),theme.caption_size).name="training_stat_changes";
            var weekPlan=Tile(card,"training_week_plan");
            AddLabel(weekPlan,L("ПЛАН НЕДЕЛИ","WEEK PLAN"),theme.body_size,true);
            if(Session.TrainingCount==0)AddLabel(weekPlan,L("Добавьте занятия ниже. Свободные слоты остаются для отдыха.","Choose sessions below. Unused slots remain available for rest."),theme.caption_size);
            for(int i=0;i<Session.TrainingCount;i++)
            {
                int index=i;AddButton(weekPlan,"training_remove_"+i,(i+1)+" · "+T("training.kind."+(int)Session.Training(i))+L(" · убрать"," · remove"),()=>{Session.RemoveTraining(index);Render();});
                var remove=weekPlan.Q<Button>("training_remove_"+i);remove.style.whiteSpace=WhiteSpace.Normal;remove.style.height=StyleKeyword.Auto;remove.style.minHeight=52*Scale;
            }
            AddButton(weekPlan,"training_clear",T("training.clear"),()=>{Session.ClearTraining();Render();},enabled:Session.TrainingCount>0);
            var tiles=TileGrid(card,"training_tiles");
            foreach(CareerTrainingKind kind in Enum.GetValues(typeof(CareerTrainingKind)))
            {
                var definition=Session.Definitions.Training.Get(kind);var chosen=kind;
                int selected=0;for(int i=0;i<Session.TrainingCount;i++)if(Session.Training(i)==kind)selected++;
                var tile=ArtCard(tiles,"training_card_"+(int)kind,"training_"+(int)kind,"training_art_"+(int)kind,compactLayout?100:48.5f,selected>0);
                AddLabel(tile,T("training.kind."+(int)kind),theme.section_size,true);AddLabel(tile,T("training.benefit."+(int)kind),theme.caption_size);
                AddLabel(tile,L("В плане: ","In plan: ")+selected+" · "+definition.TimeSlots+L(" слот(а)"," slot(s)"),theme.caption_size).name="training_selected_count_"+(int)kind;
                AddLabel(tile,F("training.cost",definition.TimeSlots,definition.FatigueCost/100.0,definition.RecoveryGain/100.0,definition.HealthRisk/100.0),theme.caption_size).style.color=FrostlineStyle.Muted;
                bool fits=plan.UsedSlots+definition.TimeSlots<=available;
                AddButton(tile,"add_training_"+(int)kind,T("training.add"),()=>{Session.AddTraining(chosen);Render();},true,fits);
                tile.Q<Button>("add_training_"+(int)kind).style.marginTop=StyleKeyword.Auto;
                if(!fits){var button=tile.Q<Button>("add_training_"+(int)kind);button.style.opacity=1;button.style.backgroundColor=HighContrast?Color.black:FrostlineStyle.Surface;button.style.color=HighContrast?Color.white:FrostlineStyle.Muted;}
                if(!fits)AddLabel(tile,L("Недостаточно свободных слотов","Not enough available slots"),theme.caption_size).style.color=FrostlineStyle.Gold;
            }
            AddLabel(card,L("Прогноз рассчитан по вашему плану. Неделя применяется после её завершения; рост навыков зависит от спортсмена и нагрузки.","Forecast uses your plan. Training applies when the week ends; improvement depends on the athlete and workload."),theme.caption_size).tooltip=T("training.forecast_note");
        }
    }
}
