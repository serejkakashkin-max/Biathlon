using System;
using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private string careerRosterNation,careerRosterSearch="";
        private bool careerRosterWorld,careerRosterEncounters;
        private CareerRosterSort careerRosterSort;
        private CareerCircuit? careerRosterCircuit;
        private void RenderCareerRoster(VisualElement card)
        {
            if(careerRosterArchive&&Session.Career.RetiredCount>0)
            {
                AddButton(card,"career_roster_archive",L("К действующим спортсменам","Back to current athletes"),()=>{careerRosterArchive=false;Render();});
                RenderCareerRetired(card);return;
            }
            var filters=new VisualElement{name="career_roster_filters"};filters.style.flexDirection=FlexDirection.Row;filters.style.flexWrap=Wrap.Wrap;card.Add(filters);
            var scope=new List<string>{L("Этот стартовый состав","This active field"),L("Весь карьерный мир","Whole career world"),L("Наши соперничества","Our encounters")};
            RosterChoice("career_roster_scope",scope,careerRosterEncounters?2:careerRosterWorld?1:0,i=>{careerRosterWorld=i==1;careerRosterEncounters=i==2;rivalryPage=0;Render();});
            var nations=new List<string>{null};var labels=new List<string>{L("Все страны","All nations")};
            for(int i=0;i<Session.Definitions.Roster.NationCount;i++){string id=Session.Definitions.Roster.Nation(i);nations.Add(id);labels.Add(T("nation."+id));}
            RosterChoice("career_roster_nation",labels,Math.Max(0,nations.IndexOf(careerRosterNation)),i=>{careerRosterNation=nations[i];rivalryPage=0;Render();});
            RosterChoice("career_roster_sort",new List<string>{L("Рейтинг OVR","OVR rating"),L("Очки сезона","Season points"),L("Возраст","Age"),L("Имя","Name")},(int)careerRosterSort,i=>{careerRosterSort=(CareerRosterSort)i;rivalryPage=0;Render();});
            void RosterChoice(string id,List<string> choices,int value,Action<int> changed)
            {
                var field=new DropdownField(choices,value){name=id};StyleControl(field);field.style.width=Length.Percent(32);field.style.marginRight=Length.Percent(1);
                field.style.height=34*Scale;field.Query<TextElement>().ForEach(t=>t.style.fontSize=12*Scale);filters.Add(field);controls.Add(field);field.RegisterValueChangedCallback(e=>changed(field.index));
            }
            var searchRow=new VisualElement();searchRow.style.flexDirection=FlexDirection.Row;searchRow.style.alignItems=Align.Center;card.Add(searchRow);
            var searchTitle=AddLabel(searchRow,L("Имя","Name"),12);searchTitle.style.width=Length.Percent(6);searchTitle.style.marginBottom=0;
            var search=new TextField{name="career_roster_search",value=careerRosterSearch,maxLength=100};StyleControl(search);search.tooltip=L("Поиск по имени","Search by name");search.style.height=32*Scale;search.style.width=Length.Percent(61);search.style.marginRight=Length.Percent(1);search.Query<TextElement>().ForEach(t=>t.style.fontSize=13*Scale);searchRow.Add(search);controls.Add(search);
            var circuits=new List<string>{L("Все лиги","All circuits"),L("Юниоры","Juniors"),L("Национальная лига","National circuit"),L("Кубок мира","World Cup")};
            var circuit=new DropdownField(circuits,careerRosterCircuit.HasValue?(int)careerRosterCircuit.Value:0){name="career_roster_circuit"};StyleControl(circuit);circuit.style.width=Length.Percent(32);circuit.style.height=32*Scale;circuit.Query<TextElement>().ForEach(t=>t.style.fontSize=12*Scale);searchRow.Add(circuit);controls.Add(circuit);
            circuit.RegisterValueChangedCallback(e=>{careerRosterCircuit=circuit.index==0?(CareerCircuit?)null:(CareerCircuit)circuit.index;rivalryPage=0;Render();});
            search.RegisterCallback<FocusOutEvent>(e=>{if(search.value==careerRosterSearch)return;careerRosterSearch=search.value;rivalryPage=0;Render();});
            search.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode!=KeyCode.Return&&e.keyCode!=KeyCode.KeypadEnter)return;careerRosterSearch=search.value;rivalryPage=0;Render();e.StopPropagation();});
            var entries=AthleteCareerSummary.Search(Session.Career,Session.Definitions,!careerRosterWorld&&!careerRosterEncounters,careerRosterNation,careerRosterSearch,careerRosterSort,careerRosterCircuit);
            var encounters=new Dictionary<string,EncounterFacts>(StringComparer.Ordinal);
            void AddFacts(CareerSeasonHistory season)
            {
                for(int i=0;i<season.Count;i++)
                {
                    var row=season.Get(i);if(row.AthleteId==Session.Career.PlayerId)continue;
                    if(!encounters.TryGetValue(row.AthleteId,out var sum))sum=new EncounterFacts();
                    sum.Races+=row.SharedRaces;sum.Close+=row.CloseFinishes;sum.Podiums+=row.SharedPodiums;sum.Standings|=row.StandingsBattle;encounters[row.AthleteId]=sum;
                }
            }
            AddFacts(CareerHistory.Summarize(Session.Career,Session.Definitions.Scoring,Session.Definitions.History));for(int i=0;i<Session.Career.HistoryCount;i++)AddFacts(Session.Career.History(i));
            var rows=TileGrid(card,"rivalry_rows");int count=0;
            foreach(var summary in entries)
            {
                var a=summary.Athlete;encounters.TryGetValue(a.Id,out var facts);
                if(careerRosterEncounters&&(facts==null||facts.Races==0||facts.Close==0&&facts.Podiums==0&&!facts.Standings))continue;
                count++;var row=Tile(rows,"rival_card_"+a.Id);row.style.width=Length.Percent(48.5f);row.style.paddingTop=row.style.paddingBottom=12;row.style.paddingLeft=row.style.paddingRight=14;
                if(a.Id==Session.Career.PlayerId){row.style.borderLeftWidth=3;row.style.borderLeftColor=FrostlineStyle.Accent;}
                var identity=new VisualElement();identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;row.Add(identity);identity.Add(new NationFlagGraphic(a.Nation));
                var label=AddLabel(identity,a.Name,14,true);label.style.flexGrow=1;label.style.flexBasis=0;label.style.marginBottom=0;label.style.whiteSpace=WhiteSpace.NoWrap;label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;label.tooltip=a.Name;
                var rating=AddLabel(identity,summary.Ratings.Overall.ToString(),30,true);rating.name="rival_ovr_"+a.Id;rating.style.color=FrostlineStyle.Accent;rating.style.marginBottom=0;rating.tooltip="OVR / 99";
                AddLabel(row,a.Age+L(" лет · "," years · ")+CareerCircuitLabel(summary.Circuit),11).style.color=FrostlineStyle.Muted;
                AddLabel(row,L("Лыжи ","Ski ")+summary.Ratings.Ski+"  /  "+L("Стрельба ","Shooting ")+summary.Ratings.Shooting,12,true);
                if(summary.SeasonPoints.HasValue)AddLabel(row,"#"+summary.SeasonRank+" · "+summary.SeasonPoints+L(" очков в сезоне"," season points"),11);
                else AddLabel(row,L("Зачёт сезона: нет результата","Season standings: no result"),11).style.color=FrostlineStyle.Muted;
                if(facts!=null&&facts.Races>0)AddLabel(row,F("rivalry.facts",facts.Races,facts.Close,facts.Podiums),11).tooltip=L("Совместные подиумы — гонки, где вы оба были в тройке. Это не личный счёт подиумов.","Shared podiums count races where both finished in the top three, not an athlete's lifetime podium count.");
                AddButton(row,"open_athlete_"+a.Id,L("Досье спортсмена","Athlete dossier"),()=>OpenAthleteCard(a.Id));var button=row.Q<Button>("open_athlete_"+a.Id);button.style.height=32;button.style.fontSize=12*Scale;button.style.marginBottom=0;
            }
            if(count==0)AddLabel(card,careerRosterEncounters?T("rivalry.empty"):L("Совпадений нет. Измени страну или поиск.","No matches. Adjust nation or search."),theme.body_size);
            else PageRows(card,rows,rivalryPage,compactLayout?2:4,value=>rivalryPage=value);
            AddLabel(card,L("OVR — способности. Очки — результаты текущего сезона. Фоновое развитие не создаёт вымышленные результаты гонок.","OVR measures ability. Points are current-season race results. Background development creates no fictional race results."),11).style.color=FrostlineStyle.Muted;
            if(Session.Career.RetiredCount>0)
            {
                AddButton(card,"career_roster_archive",L("Архив спортсменов · ","Athlete archive · ")+Session.Career.RetiredCount,()=>{careerRosterArchive=!careerRosterArchive;Render();});
            }
        }
        private bool careerRosterArchive;
        private int careerRetiredPage;
        private sealed class EncounterFacts {internal int Races,Close,Podiums;internal bool Standings;}
        private void RenderCareerRetired(VisualElement card)
        {
            var rows=TileGrid(card,"retired_athletes");
            for(int i=Session.Career.RetiredCount-1;i>=0;i--)
            {
                var a=Session.Career.Retired(i);var row=Tile(rows,"retired_"+a.Id);row.style.width=Length.Percent(48.5f);
                var identity=new VisualElement();identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;row.Add(identity);identity.Add(new NationFlagGraphic(a.Nation));AddLabel(identity,a.Name,14,true);
                AddLabel(row,a.Reason=="retired"?L("Завершил карьеру · сезон ","Retired · season ")+a.RetiredSeason:L("Вне активного карьерного пула · сезон ","Outside active career pool · season ")+a.RetiredSeason,11);
                AddLabel(row,L("Пик ","Peak ")+a.PeakOverall+" · "+a.PeakAge+L(" лет"," years"),18,true).style.color=FrostlineStyle.Gold;
                AddLabel(row,a.Races+L(" гонок · "," races · ")+a.Wins+L(" побед"," wins"),12);
            }
            PageRows(card,rows,careerRetiredPage,2,value=>careerRetiredPage=value);
        }
    }
}
