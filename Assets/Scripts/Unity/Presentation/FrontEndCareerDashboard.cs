using Biathlon.Domain;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private void RenderCareerDashboard(VisualElement actions,VisualElement card)
        {
            var summary=new CareerDashboardSummary(Session.Career,Session.Definitions);var player=summary.Player;
            var scroll=card as ScrollView;
            if(scroll!=null){scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.contentContainer.style.width=new Length(100,LengthUnit.Percent);}
            card.style.minWidth=0;
            AddLabel(actions,T("career"),theme.caption_size);AddLabel(actions,player.Name,theme.section_size,true);
            AddLabel(actions,T("nation."+player.Nation),theme.caption_size);AddLabel(actions,F("season_week",summary.Season,summary.Week),theme.caption_size);
            AddLabel(actions,F("dashboard.standing",summary.Standing?.ToString()??"—",summary.Points),theme.body_size,true).name="career_standing";
            AddLabel(actions,F("athlete_state",player.Form/100,player.Fatigue/100),theme.caption_size).name="career_readiness";
            var current=Session.Definitions.Calendar.AtWeek(summary.Week);
            if(current==null)AddButton(actions,"rest_week",T(Session.TrainingCount==0?"rest_week":"training.advance"),()=>CareerAction(Session.CompleteTrainingWeek),true);
            else
            {
                bool available=current.VenueId=="woodland_basin_v1"&&current.Eligible(player);
                AddButton(actions,"prepare",T("prepare"),()=>CareerAction(Session.OpenPreRace),true,available);
                if(!available)AddLabel(actions,T("event_unavailable"),theme.caption_size);
            }
            foreach(var page in new[]{FrontEndScreen.Training,FrontEndScreen.Equipment,FrontEndScreen.Standings,FrontEndScreen.History,FrontEndScreen.Rivalries})
            {var chosen=page;AddButton(actions,"career_"+page.ToString().ToLowerInvariant(),T("page."+(int)page),()=>{Session.OpenCareerPage(chosen);Render();});}
            AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});AddButton(actions,"back",T("back"),Back);

            AddLabel(card,T("next_event"),theme.caption_size);
            if(summary.Next==null)AddLabel(card,T("no_event"),theme.section_size,true);
            else
            {
                var next=summary.Next.Event;
                AddLabel(card,T("format."+(int)next.Format)+" · "+T(next.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue"),theme.section_size,true).name="career_next_event";
                AddLabel(card,next.Week==summary.Week?T("dashboard.race_week"):F("dashboard.weeks_until",next.Week-summary.Week),theme.body_size);
            }
            AddLabel(card,F("dashboard.season_totals",summary.RaceCount,summary.Wins,summary.Podiums),theme.caption_size).name="career_season_totals";
            var heading=AddLabel(card,T("dashboard.calendar"),theme.body_size,true);heading.style.marginTop=theme.spacing;
            var calendar=new VisualElement{name="career_calendar"};card.Add(calendar);
            for(int i=0;i<summary.CalendarCount;i++)
            {
                var entry=summary.Calendar(i);var row=new VisualElement{name="career_event_"+entry.Event.Id};
                row.style.paddingTop=theme.spacing;row.style.paddingBottom=theme.spacing;row.style.paddingLeft=theme.spacing;
                row.style.borderBottomWidth=1;row.style.borderBottomColor=FrontEndTheme.Color(theme.focus);
                if(entry.Current){row.style.backgroundColor=FrontEndTheme.Color(theme.focus);row.style.borderLeftWidth=4;row.style.borderLeftColor=accent;}
                calendar.Add(row);
                var label=AddLabel(row,F("event_week",entry.Event.Week,T("format."+(int)entry.Event.Format)),theme.body_size,entry.Current);label.style.marginBottom=theme.spacing/2;
                string status=entry.Completed?(entry.PlayerRank.HasValue?F("dashboard.event_result",entry.PlayerRank,entry.PlayerPoints):T("dashboard.completed")):
                    entry.Past?T("dashboard.past"):!entry.Eligible?T("dashboard.ineligible"):entry.Current?T("dashboard.current"):T("dashboard.upcoming");
                var caption=AddLabel(row,T(entry.Event.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue")+" · "+status,theme.caption_size);
                caption.style.marginBottom=0;caption.style.color=FrontEndTheme.Color(theme.muted);
            }
        }
    }
}
