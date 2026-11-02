using Biathlon.Domain;
using Biathlon.Career;
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
            var athleteTitle=AddLabel(actions,player.Name,theme.body_size,true);athleteTitle.style.whiteSpace=WhiteSpace.NoWrap;athleteTitle.style.overflow=Overflow.Hidden;athleteTitle.style.textOverflow=TextOverflow.Ellipsis;athleteTitle.tooltip=player.Name;
            AddLabel(actions,F("season_week",summary.Season,summary.Week),theme.caption_size);
            AddLabel(actions,"#"+(summary.Standing?.ToString()??"—")+" · "+summary.Points+L(" очков"," pts"),theme.body_size,true).name="career_standing";
            RenderCareerNavigation(actions);
            AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});AddButton(actions,"back",T("back"),Back);

            var identity=new VisualElement{name="career_identity"};identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;identity.style.marginBottom=16;card.Add(identity);
            identity.Add(new NationFlagGraphic(player.Nation));
            var country=AddLabel(identity,T("nation."+player.Nation)+"  /  "+player.Age+L(" лет"," years"),theme.body_size,true);country.style.marginBottom=0;
            var season=AddLabel(identity,L("СЕЗОН ","SEASON ")+summary.Season,theme.caption_size,true);season.style.flexGrow=1;season.style.unityTextAlign=TextAnchor.MiddleRight;season.style.color=FrostlineStyle.Gold;season.style.marginBottom=0;
            RenderCareerNextStep(card);
            RenderCareerRatingStrip(card);
            var heroCard=Tile(card,"next_event_hero",true);heroCard.style.flexDirection=FlexDirection.Row;heroCard.style.alignItems=Align.Center;
            heroCard.style.paddingLeft=heroCard.style.paddingTop=heroCard.style.paddingBottom=0;heroCard.style.paddingRight=18;
            AddVenuePreview(heroCard,compactLayout?126:166);var preview=heroCard.Q<Image>("venue_preview");preview.style.width=Length.Percent(46);preview.style.marginRight=20;preview.style.marginBottom=0;
            var hero=new VisualElement();hero.style.flexGrow=1;hero.style.flexBasis=0;hero.style.minWidth=0;heroCard.Add(hero);
            AddLabel(hero,T("next_event").ToUpperInvariant(),theme.caption_size).style.color=FrostlineStyle.Gold;
            if(summary.Next==null)AddLabel(hero,T("no_event"),theme.section_size,true).style.color=Color.white;
            else
            {
                var next=summary.Next.Event;
                AddLabel(hero,T("format."+(int)next.Format)+"  /  "+T(next.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue"),theme.section_size,true).name="career_next_event";
                AddLabel(hero,next.Week==summary.Week?T("dashboard.race_week"):F("dashboard.weeks_until",next.Week-summary.Week),theme.body_size);
            }
            var condition=TileGrid(card,"career_readiness");
            var form=Tile(condition);form.style.width=Length.Percent(48.5f);form.style.paddingTop=form.style.paddingBottom=10;
            ReadinessMeter(form,L("ФОРМА","FORM"),player.Form/100,FrostlineStyle.Accent);
            var fatigue=Tile(condition);fatigue.style.width=Length.Percent(48.5f);fatigue.style.paddingTop=fatigue.style.paddingBottom=10;
            ReadinessMeter(fatigue,L("УСТАЛОСТЬ","FATIGUE"),player.Fatigue/100,FrostlineStyle.Gold);
            var totals=AddLabel(card,F("dashboard.season_totals",summary.RaceCount,summary.Wins,summary.Podiums),theme.caption_size);totals.name="career_season_totals";totals.style.color=FrostlineStyle.Muted;
            var calendarTitle=AddLabel(card,T("dashboard.calendar").ToUpperInvariant(),theme.caption_size,true);calendarTitle.style.letterSpacing=1.5f;
            var rail=new VisualElement{name="career_season_progress"};rail.style.flexDirection=FlexDirection.Row;rail.style.marginBottom=12;card.Add(rail);
            for(int i=0;i<summary.CalendarCount;i++)
            {
                var entry=summary.Calendar(i);var mark=new VisualElement();mark.style.flexGrow=1;mark.style.height=entry.Current?8:4;mark.style.marginRight=3;
                mark.style.backgroundColor=entry.Completed?FrostlineStyle.Accent:entry.Current?FrostlineStyle.Gold:FrostlineStyle.Surface;
                mark.tooltip=F("event_week",entry.Event.Week,T("format."+(int)entry.Event.Format));rail.Add(mark);
            }
            int pageSize=2;var calendar=TileGrid(card,"career_calendar");
            if(calendarPage<0){calendarPage=0;for(int i=0;i<summary.CalendarCount;i++)if(summary.Calendar(i).Current){calendarPage=i/pageSize;break;}}
            for(int i=0;i<summary.CalendarCount;i++)
            {
                var entry=summary.Calendar(i);var row=Tile(calendar,"career_event_"+entry.Event.Id);row.style.width=Length.Percent(48.5f);row.style.paddingTop=row.style.paddingBottom=10;
                AddVenuePreview(row,compactLayout?36:48);
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
