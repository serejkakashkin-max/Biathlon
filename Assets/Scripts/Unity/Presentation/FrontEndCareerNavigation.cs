using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        // Reuse the existing guarded session transitions. Browsing never applies a week.
        private void NavigateCareer(FrontEndScreen target)
        {
            if(Session.Screen>=FrontEndScreen.Training&&Session.Screen<=FrontEndScreen.Replays)Session.Back();
            if(target!=FrontEndScreen.Career)Session.OpenCareerPage(target);
            LastMessage=null;Render();
        }

        private void RenderCareerNavigation(VisualElement parent)
        {
            var nav=new VisualElement{name="career_navigation"};parent.Add(nav);
            CareerNavigationItem(nav,FrontEndScreen.Career,"career_overview",L("Обзор сезона","Season overview"));
            NavigationGroup(nav,L("ПОДГОТОВКА","PREPARATION"));
            CareerNavigationItem(nav,FrontEndScreen.Training,"career_training",T("page."+(int)FrontEndScreen.Training));
            CareerNavigationItem(nav,FrontEndScreen.Equipment,"career_equipment",T("page."+(int)FrontEndScreen.Equipment));
            NavigationGroup(nav,L("МОЙ СЕЗОН","MY SEASON"));
            foreach(var page in new[]{FrontEndScreen.Standings,FrontEndScreen.History,FrontEndScreen.Rivalries,FrontEndScreen.Replays})
                CareerNavigationItem(nav,page,"career_"+page.ToString().ToLowerInvariant(),T("page."+(int)page));
        }

        private void NavigationGroup(VisualElement parent,string title)
        {
            var label=AddLabel(parent,title,theme.caption_size);
            label.style.marginTop=14;label.style.marginBottom=6;label.style.letterSpacing=1.5f;
        }

        private void CareerNavigationItem(VisualElement parent,FrontEndScreen page,string id,string title)
        {
            if(Session.Screen==page)
            {
                var active=AddLabel(parent,title,theme.body_size,true);active.name="career_current_page";
                active.AddToClassList("fl-current-page");active.style.backgroundColor=HighContrast?Color.black:FrostlineStyle.Surface;
                active.style.borderLeftWidth=4;active.style.borderLeftColor=HighContrast?Color.white:FrostlineStyle.Gold;
                active.style.paddingLeft=12;active.style.paddingRight=8;active.style.paddingTop=10;active.style.paddingBottom=10;
                active.tooltip=L("Текущий раздел","Current section");
                return;
            }
            AddButton(parent,id,title,()=>NavigateCareer(page));
            var button=parent.Q<Button>(id);button.style.whiteSpace=WhiteSpace.Normal;
            button.style.height=StyleKeyword.Auto;button.style.minHeight=36*Scale;button.style.marginBottom=4;
        }

        private void RenderCareerNextStep(VisualElement card)
        {
            var panel=Tile(card,"career_next_step",true);
            panel.style.borderLeftWidth=4;panel.style.borderLeftColor=FrostlineStyle.Accent;
            var current=Session.Definitions.Calendar.AtWeek(Session.Career.Week);
            AddLabel(panel,L("СЕЙЧАС / НЕДЕЛЯ ","NOW / WEEK ")+Session.Career.Week,theme.caption_size,true).style.color=FrostlineStyle.Gold;
            AddLabel(panel,current==null?L("Подготовьте следующую гонку","Prepare for your next race"):L("Гоночная неделя","Race week"),theme.section_size,true);
            var plan=Session.TrainingPlan();int slots=Session.Definitions.Calendar.TrainingSlots(Session.Career.Week);
            AddLabel(panel,F("training.budget",plan.UsedSlots,slots),theme.body_size).name="career_plan_summary";
            AddLabel(panel,current==null
                ?L("Выберите занятия в разделе «Тренировки». Свободные слоты — отдых. Завершение недели применит ваш план.","Choose sessions in Training. Unused slots mean rest. Ending the week applies your plan.")
                :L("Проверьте план тренировок, экипировку и прогноз перед выходом на старт.","Review your training plan, equipment and forecast before the start."),theme.caption_size);
            if(current==null)
                AddButton(panel,"rest_week",Session.TrainingCount==0?L("Завершить неделю · отдых","End week · rest"):L("Завершить неделю · тренировки","End week · training"),()=>CareerAction(Session.CompleteTrainingWeek),true);
            else
            {
                bool available=current.VenueId=="woodland_basin_v1"&&current.Eligible(Session.Player);
                AddButton(panel,"prepare",T("prepare"),()=>CareerAction(Session.OpenPreRace),true,available);
                if(!available)AddLabel(panel,T("event_unavailable"),theme.caption_size);
            }
        }
    }
}
