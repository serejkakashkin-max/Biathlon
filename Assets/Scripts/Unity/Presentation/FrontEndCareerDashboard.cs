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
            AddLabel(actions,player.Name,theme.section_size,true);
            AddLabel(actions,L("Неделя ","Week ")+summary.Week+" · "+T("nation."+player.Nation),theme.caption_size);
            AddLabel(actions,"#"+(summary.Standing?.ToString()??"—")+" · "+summary.Points+L(" очков"," pts"),theme.body_size,true).name="career_standing";
            AddLabel(actions,F("athlete_state",player.Form/100,player.Fatigue/100),theme.caption_size).name="career_readiness";actions.Q<Label>("career_readiness").style.display=DisplayStyle.None;
            var current=Session.Definitions.Calendar.AtWeek(summary.Week);
            if(current==null)AddButton(actions,"rest_week",T(Session.TrainingCount==0?"rest_week":"training.advance"),()=>CareerAction(Session.CompleteTrainingWeek),true);
            else
            {
                bool available=current.VenueId=="woodland_basin_v1"&&current.Eligible(player);
                AddButton(actions,"prepare",T("prepare"),()=>CareerAction(Session.OpenPreRace),true,available);
                if(!available)AddLabel(actions,T("event_unavailable"),theme.caption_size);
            }
            foreach(var page in new[]{FrontEndScreen.Training,FrontEndScreen.Equipment,FrontEndScreen.Standings,FrontEndScreen.History,FrontEndScreen.Rivalries,FrontEndScreen.Replays})
            {var chosen=page;AddButton(actions,"career_"+page.ToString().ToLowerInvariant(),T("page."+(int)page),()=>{Session.OpenCareerPage(chosen);Render();});}
            AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});AddButton(actions,"back",T("back"),Back);

            var heroCard=Tile(card,"next_event_hero",true);heroCard.style.flexDirection=FlexDirection.Row;heroCard.style.alignItems=Align.Center;
            AddVenuePreview(heroCard,compactLayout?130:160);var preview=heroCard.Q<Image>("venue_preview");preview.style.width=Length.Percent(44);preview.style.marginRight=20;preview.style.marginBottom=0;
            var hero=new VisualElement();hero.style.flexGrow=1;hero.style.flexBasis=0;hero.style.minWidth=0;heroCard.Add(hero);
            AddLabel(hero,T("next_event").ToUpperInvariant(),theme.caption_size).style.color=focus;
            if(summary.Next==null)AddLabel(hero,T("no_event"),theme.section_size,true).style.color=Color.white;
            else
            {
                var next=summary.Next.Event;
                AddLabel(hero,T("format."+(int)next.Format)+"  /  "+T(next.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue"),theme.section_size,true).name="career_next_event";
                AddLabel(hero,next.Week==summary.Week?T("dashboard.race_week"):F("dashboard.weeks_until",next.Week-summary.Week),theme.body_size);
            }
            hero.Query<Label>().ForEach(label=>label.style.color=Color.white);
            MetricStrip(card,summary.RaceCount.ToString(),L("Гонки","Races"),summary.Wins.ToString(),L("Победы","Wins"),summary.Podiums.ToString(),L("Подиумы","Podiums"));
            AddLabel(card,F("dashboard.season_totals",summary.RaceCount,summary.Wins,summary.Podiums),theme.caption_size).name="career_season_totals";card.Q<Label>("career_season_totals").style.display=DisplayStyle.None;
            AddLabel(card,T("dashboard.calendar"),theme.body_size,true);
            int pageSize=2;var calendar=TileGrid(card,"career_calendar");
            if(calendarPage<0){calendarPage=0;for(int i=0;i<summary.CalendarCount;i++)if(summary.Calendar(i).Current){calendarPage=i/pageSize;break;}}
            for(int i=0;i<summary.CalendarCount;i++)
            {
                var entry=summary.Calendar(i);var row=Tile(calendar,"career_event_"+entry.Event.Id);row.style.width=Length.Percent(48.5f);row.style.paddingTop=row.style.paddingBottom=10;
                AddVenuePreview(row,compactLayout?56:64);
                if(entry.Current){row.style.borderLeftWidth=4;row.style.borderLeftColor=accent;}
                AddLabel(row,F("event_week",entry.Event.Week,T("format."+(int)entry.Event.Format)),theme.body_size,entry.Current);
                string status=entry.Completed?(entry.PlayerRank.HasValue?F("dashboard.event_result",entry.PlayerRank,entry.PlayerPoints):T("dashboard.completed")):
                    entry.Past?T("dashboard.past"):!entry.Eligible?T("dashboard.ineligible"):entry.Current?T("dashboard.current"):T("dashboard.upcoming");
                AddLabel(row,T(entry.Event.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue")+" · "+status,theme.caption_size).style.color=FrontEndTheme.Color(theme.muted);
            }
            PageRows(card,calendar,calendarPage,pageSize,value=>calendarPage=value);
        }
    }
}
