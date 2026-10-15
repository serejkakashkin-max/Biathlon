using System;
using Biathlon.Career;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private string careerCardAthlete;
        private int careerCardTab,careerAbilityPage;
        private int careerAchievementPage;
        private CareerAchievementCatalog careerAchievements;
        public string ViewedCareerAthleteId=>careerCardAthlete;
        public void OpenAthleteCard(string athleteId)
        {
            if(Session.Career==null||(Session.Screen!=FrontEndScreen.Career&&Session.Screen<FrontEndScreen.Training))throw new InvalidOperationException("Athlete cards require a career menu.");
            if(Session.Career.FindWorldAthlete(athleteId)==null)throw new ArgumentException("Unknown living athlete.",nameof(athleteId));
            careerCardAthlete=athleteId;careerCardTab=0;careerAbilityPage=0;careerAchievementPage=0;LastMessage=null;Render();
        }
        private string CareerCircuitLabel(CareerCircuit circuit)=>circuit==CareerCircuit.Junior?L("ЮНИОРЫ","JUNIORS"):circuit==CareerCircuit.Development?L("НАЦИОНАЛЬНАЯ ЛИГА","NATIONAL CIRCUIT"):circuit==CareerCircuit.WorldCup?L("КУБОК МИРА","WORLD CUP"):L("ОТКРЫТАЯ ЛИГА","OPEN CIRCUIT");
        private static string CareerDelta(int delta)=>delta>0?"+"+delta:delta<0?"−"+Math.Abs(delta):"±0";
        private void RenderCareerAthleteCard(VisualElement body,VisualElement left,VisualElement actions,VisualElement card)
        {
            var summary=new AthleteCareerSummary(Session.Career,Session.Definitions,careerCardAthlete);var a=summary.Athlete;
            left.style.width=Length.Percent(compactLayout?27:23);card.style.flexGrow=0;card.style.flexBasis=StyleKeyword.Auto;card.style.width=Length.Percent(compactLayout?36:35);card.style.paddingLeft=card.style.paddingRight=compactLayout?14:24;
            AddLabel(actions,a.Id==Session.Career.PlayerId?L("МОЙ СПОРТСМЕН","MY ATHLETE"):L("СПОРТИВНОЕ ДОСЬЕ","ATHLETE DOSSIER"),13,true).style.color=FrostlineStyle.Gold;
            var identity=new VisualElement();identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;identity.style.marginBottom=10;actions.Add(identity);identity.Add(new NationFlagGraphic(a.Nation));
            AddLabel(identity,T("nation."+a.Nation),theme.caption_size).style.marginBottom=0;
            var name=AddLabel(actions,a.Name,compactLayout?24:30,true);name.name="athlete_card_name";name.style.marginBottom=4;
            AddLabel(actions,a.Age+L(" лет · "," years · ")+CareerCircuitLabel(summary.Circuit),13);
            AddLabel(actions,PresetLabel(summary.Ratings.Ski>summary.Ratings.Shooting+3?"skier":summary.Ratings.Shooting>summary.Ratings.Ski+3?"shooter":"balanced"),13).style.color=FrostlineStyle.Gold;
            string[] tabs={L("Профиль","Profile"),L("13 способностей","13 abilities"),L("Развитие","Development"),L("Достижения","Milestones")};
            for(int i=0;i<tabs.Length;i++){int tab=i;AddButton(actions,"athlete_card_tab_"+i,tabs[i],()=>{careerCardTab=tab;careerAbilityPage=0;Render();},careerCardTab==i);}
            AddButton(actions,"back",T("back"),Back);
            AddLabel(actions,L("Рейтинг — постоянные способности. Форма и усталость влияют на готовность отдельно.","Rating reflects permanent ability. Form and fatigue affect readiness separately."),12).style.marginTop=12;
            RenderCreatorStage(body,a.Appearance,a.Nation);
            var preview=body.Q("creator_stage");preview.Q<Label>().text=L("СПОРТСМЕН / 3D","ATHLETE / 3D");
            var skis=creatorStudio.Rig.GetComponent<CareerSkiAppearance>()??creatorStudio.Rig.gameObject.AddComponent<CareerSkiAppearance>();skis.Apply(creatorStudio.Rig,a.SelectedSki);
            var scoreRow=new VisualElement{name="athlete_card_rating"};scoreRow.style.flexDirection=FlexDirection.Row;scoreRow.style.alignItems=Align.Center;card.Add(scoreRow);
            var score=AddLabel(scoreRow,summary.Ratings.Overall.ToString(),compactLayout?64:80,true);score.name="athlete_card_ovr";score.style.color=FrostlineStyle.Accent;score.style.marginBottom=0;
            var description=new VisualElement();description.style.marginLeft=12;scoreRow.Add(description);
            AddLabel(description,"OVR / 99",13,true).style.marginBottom=2;
            var delta=AddLabel(description,CareerDelta(summary.OverallDelta),22,true);delta.name="athlete_card_delta";delta.style.color=summary.OverallDelta<0?FrostlineStyle.Danger:FrostlineStyle.Gold;delta.style.marginBottom=0;
            AddLabel(description,L("за сезон","this season"),11).style.marginBottom=0;
            if(careerCardTab==0)RenderAthleteOverview(card,summary);
            else if(careerCardTab==1)RenderAthleteAbilities(card,summary);
            else if(careerCardTab==2)RenderAthleteDevelopment(card,summary);
            else RenderAthleteAchievements(card,summary);
        }
        private void CareerAbilityRail(VisualElement parent,string name,int value,int delta,string id,bool compact=false)
        {
            var item=new VisualElement{name=id};item.style.marginBottom=compact?7:10;parent.Add(item);
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;item.Add(row);
            var title=AddLabel(row,name,compact?12:13);title.style.flexGrow=1;title.style.flexBasis=0;title.style.marginBottom=3;
            var score=AddLabel(row,value.ToString(),compact?17:20,true);score.style.marginBottom=0;score.style.marginLeft=8;
            var change=AddLabel(row,CareerDelta(delta),11,true);change.style.marginBottom=0;change.style.width=34;change.style.unityTextAlign=TextAnchor.MiddleRight;change.style.color=delta<0?FrostlineStyle.Danger:delta>0?FrostlineStyle.Gold:FrostlineStyle.Muted;
            var track=new VisualElement();track.style.height=4;track.style.backgroundColor=FrostlineStyle.Deep;item.Add(track);
            var fill=new VisualElement();fill.style.height=4;fill.style.width=Length.Percent(value*100f/99);fill.style.backgroundColor=FrostlineStyle.Accent;track.Add(fill);
            if(delta!=0){var segment=new VisualElement();segment.style.height=4;segment.style.position=Position.Absolute;segment.style.left=Length.Percent(Math.Max(0,Math.Min(value,value-delta))*100f/99);segment.style.width=Length.Percent(Math.Abs(delta)*100f/99);segment.style.backgroundColor=delta>0?FrostlineStyle.Gold:FrostlineStyle.Danger;track.Add(segment);}
        }
        private void RenderAthleteOverview(VisualElement card,AthleteCareerSummary summary)
        {
            string[] labels={L("Лыжи","Ski"),L("Стрельба","Shooting"),L("Физика","Physical"),L("Техника","Technique"),L("Психология","Mental")};
            for(int i=0;i<5;i++){var kind=(BiathlonRating)(i+1);CareerAbilityRail(card,labels[i],BiathlonRatingEngine.Calculate(summary.Athlete,Session.Definitions.Ratings,kind),summary.GroupDelta(kind),"athlete_group_"+i,compactLayout);}
            var readiness=Tile(card,"athlete_card_readiness",true);readiness.style.paddingTop=readiness.style.paddingBottom=10;readiness.style.paddingLeft=readiness.style.paddingRight=12;
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;readiness.Add(row);
            AddLabel(row,L("Форма ","Form ")+summary.Athlete.Form/100+"%",13,true).name="athlete_card_form";
            AddLabel(row,L("Усталость ","Fatigue ")+summary.Athlete.Fatigue/100+"%",13,true).name="athlete_card_fatigue";
            var note=AddLabel(readiness,summary.SeasonRank.HasValue?"#"+summary.SeasonRank+" · "+summary.SeasonPoints+L(" очков в сезоне"," season points"):summary.InActiveField?L("В стартовом составе · зачёт ещё не открыт","Active field · standings not yet established"):L("Развивается в своём дивизионе","Developing in their own circuit"),11);note.style.color=FrostlineStyle.Muted;note.style.marginBottom=0;
            string assessment=summary.Trend==ObservedDevelopmentTrend.Improving?L("Развитие: рост наблюдается","Development: growth observed"):summary.Trend==ObservedDevelopmentTrend.Stable?L("Развитие: стабильный рейтинг","Development: stable rating"):summary.Trend==ObservedDevelopmentTrend.Declining?L("Развитие: рейтинг снижается","Development: rating declining"):L("Потенциал: оценка уточняется","Potential: assessment in progress");
            AddLabel(card,assessment,12,true).name="athlete_potential_assessment";
            AddLabel(card,L("Тренировки и возраст раскрывают возможности. Максимальный рейтинг не гарантирован.","Training and age reveal possibilities. No peak rating is guaranteed."),11).style.color=FrostlineStyle.Muted;
        }
        private void RenderAthleteAbilities(VisualElement card,AthleteCareerSummary summary)
        {
            AddLabel(card,L("СПОСОБНОСТИ / ИЗМЕНЕНИЕ ЗА СЕЗОН","ABILITY / SEASON CHANGE"),11,true).style.color=FrostlineStyle.Gold;
            int first=careerAbilityPage*7,last=Math.Min(13,first+7);string[] names=Locale=="ru"?StatRu:StatEn;
            for(int i=first;i<last;i++)CareerAbilityRail(card,names[i],summary.Ability((CareerStat)i),summary.AbilityDelta((CareerStat)i),"athlete_ability_"+i,true);
            var pager=new VisualElement();pager.style.flexDirection=FlexDirection.Row;pager.style.justifyContent=Justify.SpaceBetween;pager.style.alignItems=Align.Center;card.Add(pager);
            AddButton(pager,"athlete_abilities_previous","‹",()=>{careerAbilityPage=0;Render();},enabled:careerAbilityPage>0);
            AddLabel(pager,(careerAbilityPage+1)+" / 2",13,true).style.marginBottom=0;
            AddButton(pager,"athlete_abilities_next","›",()=>{careerAbilityPage=1;Render();},enabled:careerAbilityPage==0);
            AddLabel(card,L("Никакого дополнительного бонуса от OVR: результат определяют эти способности и действия в гонке.","OVR grants no extra bonus. These abilities and race decisions determine the result."),11).style.color=FrostlineStyle.Muted;
        }
        private void RenderAthleteDevelopment(VisualElement card,AthleteCareerSummary summary)
        {
            AddLabel(card,L("СЛЕД КАРЬЕРЫ / OVR","CAREER TRACK / OVR"),12,true).style.color=FrostlineStyle.Gold;
            card.Add(new CareerDevelopmentGraphic(summary,Session.Definitions.Calendar.WeeksPerSeason));
            var ends=new VisualElement();ends.style.flexDirection=FlexDirection.Row;ends.style.justifyContent=Justify.SpaceBetween;card.Add(ends);
            var first=summary.Observation(0);AddLabel(ends,L("Сезон ","Season ")+first.Season+" · "+first.Overall,11);
            AddLabel(ends,L("Сейчас ","Now ")+summary.Ratings.Overall,11,true);
            var peak=Tile(card,"athlete_career_peak",true);peak.style.paddingTop=peak.style.paddingBottom=10;
            var value=AddLabel(peak,summary.PeakOverall+"  /  "+L("ЛИЧНЫЙ ПИК","CAREER PEAK"),22,true);value.style.color=FrostlineStyle.Gold;value.style.marginBottom=3;
            AddLabel(peak,summary.PeakAge+L(" лет · сезон "," years · season ")+summary.PeakSeason,12).style.marginBottom=0;
            if(summary.StrongestSeason.HasValue)AddLabel(card,L("Лучший сезон по OVR: ","Strongest OVR season: ")+summary.StrongestSeason,12);
            AddLabel(card,summary.Athlete.Races+L(" гонок  ·  "," races  ·  ")+summary.Athlete.Wins+L(" побед"," wins"),13,true).name="athlete_lifetime_results";
            AddLabel(card,L("Заработано за карьеру: ","Career earnings: ")+(summary.Athlete.LifetimeEarnings/100.0).ToString("N0"),12);
            AddLabel(card,summary.ObservationCount<2?L("История начинается с первого наблюдения. Пройди сезон, чтобы увидеть изменение.","History begins with the first observation. Complete a season to see the change."):L("Точки — сохранённые наблюдения. Золотая отметка показывает зафиксированный максимум.","Dots are saved observations. Gold marks the observed peak."),11).style.color=FrostlineStyle.Muted;
        }
        private void RenderCareerRatingStrip(VisualElement card)
        {
            var summary=new AthleteCareerSummary(Session.Career,Session.Definitions,Session.Career.PlayerId);
            var strip=Tile(card,"career_rating_strip",true);strip.style.flexDirection=FlexDirection.Row;strip.style.alignItems=Align.Center;strip.style.paddingTop=strip.style.paddingBottom=8;strip.style.marginBottom=12;
            var rating=AddLabel(strip,summary.Ratings.Overall.ToString(),34,true);rating.name="career_player_ovr";rating.style.color=FrostlineStyle.Accent;rating.style.marginBottom=0;rating.style.marginRight=14;
            var textColumn=new VisualElement();textColumn.style.flexGrow=1;textColumn.style.flexBasis=0;strip.Add(textColumn);
            AddLabel(textColumn,L("ПОДГОТОВКА / OVR","PERMANENT ABILITY / OVR"),12,true).style.marginBottom=2;
            AddLabel(textColumn,CareerDelta(summary.OverallDelta)+L(" за сезон · форма отдельно"," this season · form is separate"),11).style.marginBottom=0;
            AddButton(strip,"career_athlete_card",L("Мой спортсмен","My athlete"),()=>OpenAthleteCard(Session.Career.PlayerId));
            var button=strip.Q<Button>("career_athlete_card");button.style.height=34;button.style.fontSize=13*Scale;button.style.marginBottom=0;
        }
        private void RenderTrainingAbilityForecast(VisualElement card,CareerAthlete preview)
        {
            int before=BiathlonRatingEngine.Calculate(Session.Player,Session.Definitions.Ratings).Overall,after=BiathlonRatingEngine.Calculate(preview,Session.Definitions.Ratings).Overall;
            var strip=new VisualElement{name="training_ability_forecast"};strip.style.flexDirection=FlexDirection.Row;strip.style.alignItems=Align.Center;strip.style.marginBottom=10;card.Add(strip);
            var score=AddLabel(strip,"OVR "+before+" → "+after,16,true);score.style.color=FrostlineStyle.Accent;score.style.marginRight=16;score.style.marginBottom=0;
            string[] names=Locale=="ru"?StatRu:StatEn;int largest=-1,second=-1;
            int Gain(int stat)=>preview.Stat((CareerStat)stat)-Session.Player.Stat((CareerStat)stat);
            for(int i=0;i<13;i++)if(Gain(i)>0){if(largest<0||Gain(i)>Gain(largest)){second=largest;largest=i;}else if(second<0||Gain(i)>Gain(second))second=i;}
            var detail=AddLabel(strip,largest<0?L("Способности не меняются","No ability change"):names[largest]+(second>=0?" / "+names[second]:"")+L(" · прогресс навыков"," · ability progress"),11);detail.style.marginBottom=0;detail.style.flexGrow=1;detail.style.flexBasis=0;
            strip.tooltip=L("Прогноз рассчитан по текущему плану. Малые изменения навыков могут не изменить округлённый OVR; рост не гарантирован.","Forecast uses the current plan. Small ability changes may leave rounded OVR unchanged; improvement is not guaranteed.");
        }
        private void RenderAthleteAchievements(VisualElement card,AthleteCareerSummary summary)
        {
            if(careerAchievements==null)careerAchievements=CareerAchievementCatalog.FromJson(ReadCore("career_achievements_v1.json"));
            AddLabel(card,L("КАРЬЕРНЫЕ ДОСТИЖЕНИЯ","CAREER MILESTONES"),12,true).style.color=FrostlineStyle.Gold;
            var rows=new VisualElement{name="athlete_achievements"};card.Add(rows);
            for(int i=0;i<careerAchievements.Count;i++)
            {
                var a=careerAchievements.Get(i);int value=a.Value(summary.Athlete,Session.Definitions.Ratings);bool earned=value>=a.Target;
                var row=Tile(rows,"achievement_"+a.Id,true);row.style.paddingTop=row.style.paddingBottom=10;row.style.paddingLeft=row.style.paddingRight=12;
                var identity=new VisualElement();identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;row.Add(identity);identity.Add(new CareerMilestoneMedal(earned));
                var title=AddLabel(identity,a.Title(Locale),14,true);title.style.marginBottom=0;title.style.flexGrow=1;title.style.flexBasis=0;title.style.color=earned?FrostlineStyle.Gold:FrostlineStyle.Ink;
                string metric=a.Metric==CareerAchievementMetric.Races?L("финишей","finishes"):a.Metric==CareerAchievementMetric.Wins?L("побед","wins"):L("личный пик OVR","career peak OVR");
                AddLabel(row,Math.Min(value,a.Target)+" / "+a.Target+" · "+metric,11).style.marginBottom=4;
                var rail=new VisualElement();rail.style.height=4;rail.style.backgroundColor=FrostlineStyle.Surface;row.Add(rail);
                var fill=new VisualElement();fill.style.height=4;fill.style.width=Length.Percent(Math.Min(100,value*100f/a.Target));fill.style.backgroundColor=earned?FrostlineStyle.Gold:FrostlineStyle.Accent;rail.Add(fill);
            }
            PageRows(card,rows,careerAchievementPage,3,p=>careerAchievementPage=p);
            AddLabel(card,L("По сохранённым финишам, победам и личному пику. Достижения не дают бонусов к способностям.","Based on saved finishes, wins and career peak. Milestones grant no ability bonuses."),11).style.color=FrostlineStyle.Muted;
        }
    }
}
