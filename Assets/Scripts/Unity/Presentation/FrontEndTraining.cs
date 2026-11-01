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
            AddLabel(actions,L("ПЛАН НЕДЕЛИ","WEEK PLAN"),theme.body_size,true);
            if(Session.TrainingCount==0)AddLabel(actions,L("Добавьте занятия справа. Свободные слоты остаются для отдыха.","Choose sessions on the right. Unused slots remain available for rest."),theme.caption_size);
            for(int i=0;i<Session.TrainingCount;i++)
            {
                int index=i;AddButton(actions,"training_remove_"+i,(i+1)+" · "+T("training.kind."+(int)Session.Training(i))+L(" · убрать"," · remove"),()=>{Session.RemoveTraining(index);Render();});
                var remove=actions.Q<Button>("training_remove_"+i);remove.style.whiteSpace=WhiteSpace.Normal;remove.style.height=StyleKeyword.Auto;remove.style.minHeight=52*Scale;
            }
            AddButton(actions,"training_clear",T("training.clear"),()=>{Session.ClearTraining();Render();},enabled:Session.TrainingCount>0);
            var tiles=TileGrid(card,"training_tiles");
            foreach(CareerTrainingKind kind in Enum.GetValues(typeof(CareerTrainingKind)))
            {
                var definition=Session.Definitions.Training.Get(kind);var chosen=kind;var tile=Tile(tiles);tile.name="training_card_"+(int)kind;tile.style.width=Length.Percent(48.5f);tile.style.paddingTop=12;
                tile.Add(new TrainingSessionGraphic(kind));
                AddLabel(tile,T("training.kind."+(int)kind),theme.body_size,true);AddLabel(tile,T("training.benefit."+(int)kind),theme.caption_size);
                int selected=0;for(int i=0;i<Session.TrainingCount;i++)if(Session.Training(i)==kind)selected++;
                AddLabel(tile,L("В плане: ","In plan: ")+selected+" · "+definition.TimeSlots+L(" слот(а)"," slot(s)"),theme.caption_size).name="training_selected_count_"+(int)kind;
                AddLabel(tile,F("training.cost",definition.TimeSlots,definition.FatigueCost/100.0,definition.RecoveryGain/100.0,definition.HealthRisk/100.0),theme.caption_size).style.color=FrostlineStyle.Muted;
                bool fits=plan.UsedSlots+definition.TimeSlots<=available;
                AddButton(tile,"add_training_"+(int)kind,T("training.add"),()=>{Session.AddTraining(chosen);Render();},true,fits);
                if(!fits){var button=tile.Q<Button>("add_training_"+(int)kind);button.style.opacity=1;button.style.backgroundColor=HighContrast?Color.black:FrostlineStyle.Surface;button.style.color=HighContrast?Color.white:FrostlineStyle.Muted;}
                if(!fits)AddLabel(tile,L("Недостаточно свободных слотов","Not enough available slots"),theme.caption_size).style.color=FrostlineStyle.Gold;
            }
            AddLabel(card,L("Прогноз рассчитан по вашему плану. Неделя применяется после её завершения; рост навыков зависит от спортсмена и нагрузки.","Forecast uses your plan. Training applies when the week ends; improvement depends on the athlete and workload."),theme.caption_size).tooltip=T("training.forecast_note");
        }
    }
    /// <summary>Six original retained sport illustrations, with no font/icon dependency.</summary>
    internal sealed class TrainingSessionGraphic:VisualElement
    {
        internal TrainingSessionGraphic(CareerTrainingKind kind)
        {
            name="training_art_"+(int)kind;style.height=112;style.marginBottom=12;style.flexShrink=0;pickingMode=PickingMode.Ignore;
            generateVisualContent+=context=>
            {
                var p=context.painter2D;float sx=contentRect.width/300,sy=contentRect.height/120;
                Vector2 V(float x,float y)=>new Vector2(x*sx,y*sy);
                void Line(Color color,float width,params Vector2[] points){p.strokeColor=color;p.lineWidth=width;p.lineCap=LineCap.Round;p.lineJoin=LineJoin.Round;p.BeginPath();for(int i=0;i<points.Length;i++){var v=V(points[i].x,points[i].y);if(i==0)p.MoveTo(v);else p.LineTo(v);}p.Stroke();}
                void Poly(Color color,params Vector2[] points){p.fillColor=color;p.BeginPath();for(int i=0;i<points.Length;i++){var v=V(points[i].x,points[i].y);if(i==0)p.MoveTo(v);else p.LineTo(v);}p.ClosePath();p.Fill();}
                void Head(float x,float y){p.fillColor=FrostlineStyle.Ink;p.BeginPath();p.Arc(V(x,y),7*sy,Angle.Degrees(0),Angle.Degrees(360));p.Fill();Line(FrostlineStyle.Gold,3,new Vector2(x-6,y-3),new Vector2(x+7,y-3));}
                var suit=FrostlineStyle.Accent;var snow=new Color(.42f,.58f,.67f);var dark=FrostlineStyle.Deep;var gold=FrostlineStyle.Gold;
                Poly(dark,new Vector2(0,0),new Vector2(300,0),new Vector2(300,120),new Vector2(0,120));
                Poly(new Color(.12f,.23f,.3f),new Vector2(0,84),new Vector2(43,53),new Vector2(84,74),new Vector2(133,38),new Vector2(195,76),new Vector2(255,49),new Vector2(300,82),new Vector2(300,120),new Vector2(0,120));
                Line(snow,2,new Vector2(10,106),new Vector2(290,106));
                if(kind==CareerTrainingKind.Prone||kind==CareerTrainingKind.Standing)
                {
                    float targetY=kind==CareerTrainingKind.Prone?69:48;for(int i=0;i<3;i++){p.strokeColor=i==0?gold:snow;p.lineWidth=2;p.BeginPath();p.Arc(V(271,targetY),(13-i*4)*sy,Angle.Degrees(0),Angle.Degrees(360));p.Stroke();}
                    Line(snow,2,new Vector2(271,targetY+14),new Vector2(271,106));
                    if(kind==CareerTrainingKind.Prone){Poly(suit,new Vector2(87,70),new Vector2(145,61),new Vector2(170,68),new Vector2(159,81),new Vector2(91,82));Head(177,65);Line(suit,8,new Vector2(100,78),new Vector2(52,85),new Vector2(25,88));Line(gold,6,new Vector2(156,72),new Vector2(181,85),new Vector2(202,72));Line(FrostlineStyle.Ink,4,new Vector2(161,70),new Vector2(239,70));}
                    else{Head(143,27);Poly(suit,new Vector2(131,38),new Vector2(150,38),new Vector2(156,70),new Vector2(131,75));Line(suit,9,new Vector2(138,72),new Vector2(120,102));Line(suit,9,new Vector2(148,71),new Vector2(170,102));Line(gold,6,new Vector2(143,43),new Vector2(166,63),new Vector2(185,50));Line(FrostlineStyle.Ink,4,new Vector2(149,49),new Vector2(237,49));}
                }
                else if(kind==CareerTrainingKind.Recovery)
                {
                    Poly(gold,new Vector2(65,100),new Vector2(224,100),new Vector2(239,111),new Vector2(51,111));Head(119,35);Line(suit,16,new Vector2(120,50),new Vector2(140,80));Line(suit,9,new Vector2(139,82),new Vector2(190,95));Line(suit,9,new Vector2(139,82),new Vector2(105,96));Line(FrostlineStyle.Ink,6,new Vector2(121,54),new Vector2(154,69),new Vector2(182,87));
                }
                else
                {
                    bool sprint=kind==CareerTrainingKind.Intensity,turn=kind==CareerTrainingKind.Technique;
                    float hip=turn?131:120,head=sprint?161:turn?155:140;Head(head,sprint?42:29);
                    Poly(suit,new Vector2(hip-6,72),new Vector2(head-15,sprint?48:39),new Vector2(head+1,sprint?52:42),new Vector2(hip+9,77));
                    Line(suit,8,new Vector2(hip,73),new Vector2(95,91),new Vector2(turn?77:58,102));Line(suit,8,new Vector2(hip+5,75),new Vector2(155,87),new Vector2(turn?193:169,102));
                    Line(gold,5,new Vector2(head-11,sprint?52:43),new Vector2(180,65),new Vector2(191,83));Line(gold,5,new Vector2(head-11,sprint?52:43),new Vector2(115,58),new Vector2(91,76));
                    Line(snow,2,new Vector2(91,76),new Vector2(55,107));Line(snow,2,new Vector2(191,83),new Vector2(227,107));
                    Line(gold,3,new Vector2(26,109),new Vector2(124,109));Line(gold,3,new Vector2(140,111),new Vector2(240,111));
                    if(sprint){Line(snow,2,new Vector2(22,46),new Vector2(65,46));Line(snow,2,new Vector2(11,59),new Vector2(72,59));}
                    if(turn){Line(gold,3,new Vector2(251,91),new Vector2(251,50));Poly(gold,new Vector2(251,50),new Vector2(277,56),new Vector2(251,64));}
                }
            };
        }
    }
}
