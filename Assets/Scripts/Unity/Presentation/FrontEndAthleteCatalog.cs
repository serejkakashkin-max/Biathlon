using System;
using System.Collections.Generic;
using Biathlon.Career;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private CareerCreationMode? creatorMode;
        private CareerAthleteCatalog athleteCatalog;
        private readonly CareerAthleteFilter athleteFilter=new CareerAthleteFilter();
        private string selectedCatalogAthlete;
        private int catalogPage,catalogAbilityPage;
        private bool catalogAdvanced,catalogDetails,catalogReview;
        public CareerAthleteCatalog AthleteCatalog=>athleteCatalog;
        public string SelectedCatalogAthlete=>selectedCatalogAthlete;
        public void ChooseCreationMode(CareerCreationMode mode)
        {
            if(Session.Screen!=Biathlon.Presentation.FrontEndScreen.NewCareer)throw new InvalidOperationException("Open new career before choosing its mode.");
            if(mode<CareerCreationMode.CreateOwn||mode>CareerCreationMode.ExistingAthlete)throw new ArgumentOutOfRangeException(nameof(mode));
            EnsureCreationDraft();creatorMode=mode;catalogReview=false;
            if(mode==CareerCreationMode.ExistingAthlete&&athleteCatalog==null)
                athleteCatalog=new CareerAthleteCatalog(ReadCore,"career_"+Guid.NewGuid().ToString("N"),creationDraft.DraftSeed,s=>new Biathlon.Simulation.SeededRandomSource(s));
            LastMessage=null;Render();
        }
        private void RenderCareerCreation(VisualElement body,VisualElement left,VisualElement actions,VisualElement card)
        {
            if(!creatorMode.HasValue)
            {
                ReleaseCreatorStudio();AddLabel(actions,T("new_career"),theme.section_size,true);
                AddLabel(actions,L("ТВОЙ ПУТЬ К СТАРТУ","YOUR PATH TO THE START"),theme.caption_size).style.color=FrostlineStyle.Gold;
                AddButton(actions,"creator_mode_own",L("Создать спортсмена","Create an athlete"),()=>ChooseCreationMode(CareerCreationMode.CreateOwn),true);
                AddLabel(actions,L("Выбери внешность и сильные стороны. Начни свой путь с юниоров, национальной лиги или Кубка мира.","Choose your look and strengths. Begin in juniors, the national circuit or the World Cup."),theme.body_size);
                AddButton(actions,"creator_mode_existing",L("Выбрать биатлониста","Choose an athlete"),()=>ChooseCreationMode(CareerCreationMode.ExistingAthlete));
                AddLabel(actions,L("Прими готовые способности, возраст и историю спортсмена. Следующий сезон зависит от тебя.","Take on an athlete's existing ability, age and identity. Their next season is yours."),theme.body_size);
                AddButton(actions,"back",T("back"),Back);
                AddVenueCard(card);var mark=AddLabel(card,L("ОДИН СПОРТСМЕН.\nТВОЯ КАРЬЕРА.","ONE ATHLETE.\nYOUR CAREER."),44,true);mark.style.marginTop=24;
                AddLabel(card,L("Тренировки, форма и решения на дистанции. Рейтинг показывает подготовку, но результат решается в гонке.","Training, readiness and decisions on the course. Rating reflects ability; the race decides the result."),theme.body_size);
                return;
            }
            if(creatorMode==CareerCreationMode.CreateOwn)RenderAthleteCreator(body,left,actions,card);
            else RenderAthleteCatalog(body,left,actions,card);
        }
        private void RenderAthleteCatalog(VisualElement body,VisualElement left,VisualElement actions,VisualElement card)
        {
            left.style.width=Length.Percent(compactLayout?34:30);left.style.paddingRight=16;
            card.style.flexGrow=0;card.style.flexBasis=StyleKeyword.Auto;card.style.width=Length.Percent(compactLayout?27:25);card.style.paddingLeft=16;card.style.paddingRight=16;card.style.paddingTop=12;
            var rows=athleteCatalog.Search(athleteFilter);
            if(rows.Length>0&&!Array.Exists(rows,a=>a.Id==selectedCatalogAthlete))selectedCatalogAthlete=rows[0].Id;
            var selected=rows.Length==0?null:athleteCatalog.Get(selectedCatalogAthlete);
            if(catalogReview&&selected!=null)
            {
                AddLabel(actions,L("ТВОЙ СЛЕДУЮЩИЙ СЕЗОН","YOUR NEXT SEASON"),theme.caption_size).style.color=FrostlineStyle.Gold;
                AddLabel(actions,selected.Name,theme.section_size,true);AddLabel(actions,T("nation."+selected.Nation)+" · "+selected.Age+L(" лет"," years"),theme.body_size);
                AddLabel(actions,TierLabel(selected.StartProfileId),theme.body_size,true);
                AddLabel(actions,L("Возраст, национальность и способности сохраняются. Ты управляешь этим спортсменом — второй копии в составе не будет.","Age, nationality and abilities stay intact. You manage this athlete; the field contains a single copy."),theme.body_size);
                AddButton(actions,"catalog_confirm",L("Начать карьеру","Begin career"),ConfirmCatalogAthlete,true);
                AddButton(actions,"catalog_review_back",T("back"),()=>{catalogReview=false;Render();});
            }
            else
            {
                AddLabel(actions,L("ВЫБЕРИ БИАТЛОНИСТА","CHOOSE YOUR ATHLETE"),theme.caption_size,true);
                var search=new TextField{name="catalog_search",value=athleteFilter.Name,maxLength=100};StyleControl(search);search.style.height=34;search.tooltip=L("Поиск по имени","Search by name");actions.Add(search);controls.Add(search);
                StyleCatalogFilter(search);
                search.RegisterValueChangedCallback(e=>{athleteFilter.Name=e.newValue;catalogPage=0;Render();});
                var filterRow=new VisualElement();filterRow.style.flexDirection=FlexDirection.Row;actions.Add(filterRow);
                var circuits=new List<string>{L("Все лиги","All circuits"),L("Юниоры","Juniors"),L("Национальная","National"),L("Кубок мира","World Cup")};
                var circuit=CreatorChoice(filterRow,"catalog_circuit",circuits,athleteFilter.Circuit.HasValue?(int)athleteFilter.Circuit.Value:0,i=>{athleteFilter.Circuit=i==0?(CareerCircuit?)null:(CareerCircuit)i;catalogPage=0;Render();});circuit.style.width=Length.Percent(52);circuit.style.height=34;
                var roster=CareerDefinitions.ForNewProductionCareer(ReadCore).Roster;var nations=new List<string>{null};var countries=new List<string>{L("Все страны","All nations")};
                for(int i=0;i<roster.NationCount;i++){nations.Add(roster.Nation(i));countries.Add(roster.Nation(i));}
                var nationChoice=CreatorChoice(filterRow,"catalog_nation",countries,nations.IndexOf(athleteFilter.Nation),i=>{athleteFilter.Nation=nations[i];catalogPage=0;Render();});nationChoice.style.width=Length.Percent(48);nationChoice.style.height=34;
                StyleCatalogFilter(circuit);StyleCatalogFilter(nationChoice);
                var specs=new List<string>{L("Любая специализация","Any specialty"),L("Лыжник","Skier"),L("Стрелок","Shooter"),L("Универсал","Balanced")};var specIds=new[]{(string)null,"skier","shooter","balanced"};
                var spec=CreatorChoice(actions,"catalog_specialization",specs,Array.IndexOf(specIds,athleteFilter.SpecializationId),i=>{athleteFilter.SpecializationId=specIds[i];catalogPage=0;Render();});spec.style.height=34;
                StyleCatalogFilter(spec);
                AddButton(actions,"catalog_advanced",L("Возраст / рейтинг","Age / rating")+(catalogAdvanced?" −":" +"),()=>{catalogAdvanced=!catalogAdvanced;Render();});actions.Q<Button>("catalog_advanced").style.height=28;
                if(catalogAdvanced)
                {
                    FilterBounds(actions,L("Возраст","Age"),"age",athleteFilter.MinimumAge,athleteFilter.MaximumAge,16,70,(min,max)=>{athleteFilter.MinimumAge=min;athleteFilter.MaximumAge=max;});
                    FilterBounds(actions,"OVR","rating",athleteFilter.MinimumRating,athleteFilter.MaximumRating,1,99,(min,max)=>{athleteFilter.MinimumRating=min;athleteFilter.MaximumRating=max;});
                }
                int pageSize=catalogAdvanced?1:3;catalogPage=Mathf.Clamp(catalogPage,0,Math.Max(0,(rows.Length-1)/pageSize));
                AddLabel(actions,rows.Length+L(" спортсменов"," athletes"),12).style.color=FrostlineStyle.Muted;
                for(int i=catalogPage*pageSize;i<Math.Min(rows.Length,(catalogPage+1)*pageSize);i++)
                {
                    var entry=rows[i];string id=entry.Id;AddButton(actions,"catalog_athlete_"+id,"",()=>{selectedCatalogAthlete=id;Render();});
                    var button=actions.Q<Button>("catalog_athlete_"+id);button.style.height=58;button.style.flexDirection=FlexDirection.Row;button.style.alignItems=Align.Center;
                    if(id==selectedCatalogAthlete){button.style.backgroundColor=FrostlineStyle.Surface;button.style.borderLeftWidth=3;button.style.borderLeftColor=FrostlineStyle.Accent;}
                    var flag=new NationFlagGraphic(entry.Nation);flag.style.width=24;flag.style.height=16;flag.style.flexShrink=0;flag.style.marginRight=8;button.Add(flag);
                    var textColumn=new VisualElement();textColumn.style.flexGrow=1;textColumn.style.flexBasis=0;textColumn.style.minWidth=0;button.Add(textColumn);
                    var name=AddLabel(textColumn,entry.Name,14,true);name.style.marginBottom=0;name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;
                    AddLabel(textColumn,entry.Age+L(" лет · "," years · ")+PresetLabel(entry.SpecializationId),11).style.marginBottom=0;
                    var ovr=AddLabel(button,entry.Ratings.Overall.ToString(),26,true);ovr.style.marginBottom=0;ovr.style.marginLeft=6;
                }
                if(rows.Length==0)AddLabel(actions,L("Нет совпадений. Измени фильтры.","No matches. Adjust the filters."),theme.caption_size);
                var pages=new VisualElement();pages.style.flexDirection=FlexDirection.Row;actions.Add(pages);
                AddButton(pages,"catalog_previous","‹",()=>{catalogPage--;Render();},enabled:catalogPage>0);AddLabel(pages,(rows.Length==0?0:catalogPage+1)+" / "+Math.Max(1,(rows.Length+pageSize-1)/pageSize),13).style.flexGrow=1;
                AddButton(pages,"catalog_next","›",()=>{catalogPage++;Render();},enabled:(catalogPage+1)*pageSize<rows.Length);
                AddButton(actions,"catalog_review",L("Выбрать спортсмена","Choose athlete"),()=>{catalogReview=true;Render();},true,selected!=null);
                AddButton(actions,"back",T("back"),Back);
            }
            if(selected==null)
            {if(creatorStudio!=null){creatorStudio.Rig.gameObject.SetActive(false);creatorStudio.PreviewCamera.enabled=false;}AddLabel(card,L("Измени поиск, чтобы увидеть спортсменов.","Adjust your search to see athletes."),theme.body_size);return;}
            RenderCreatorStage(body,selected.Appearance,selected.Nation);RenderCatalogRatings(card,selected);
        }
        private void FilterBounds(VisualElement parent,string caption,string id,int minimum,int maximum,int floor,int ceiling,Action<int,int> set)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;parent.Add(row);AddLabel(row,caption,12).style.width=Length.Percent(36);
            var min=new IntegerField{name="catalog_"+id+"_min",value=minimum};var max=new IntegerField{name="catalog_"+id+"_max",value=maximum};
            StyleControl(min);StyleControl(max);min.style.width=max.style.width=Length.Percent(30);min.style.height=max.style.height=30;row.Add(min);row.Add(max);controls.Add(min);controls.Add(max);
            min.RegisterValueChangedCallback(e=>{set(Mathf.Clamp(e.newValue,floor,maximum),maximum);catalogPage=0;Render();});
            max.RegisterValueChangedCallback(e=>{set(minimum,Mathf.Clamp(e.newValue,minimum,ceiling));catalogPage=0;Render();});
        }
        private void StyleCatalogFilter(VisualElement control)
        {control.style.height=32*Scale;control.style.fontSize=14*Scale;control.Query<TextElement>().ForEach(t=>t.style.fontSize=14*Scale);}
        private void RenderCatalogRatings(VisualElement card,CareerCatalogAthlete entry)
        {
            AddLabel(card,"OVR / 99",theme.caption_size).style.color=FrostlineStyle.Muted;
            AddLabel(card,entry.Ratings.Overall.ToString(),compactLayout?64:76,true).style.color=FrostlineStyle.Accent;
            AddLabel(card,entry.Name,compactLayout?22:theme.section_size,true);AddLabel(card,T("nation."+entry.Nation)+" · "+entry.Age+L(" лет"," years"),theme.caption_size);
            AddLabel(card,TierLabel(entry.StartProfileId),theme.body_size,true);AddLabel(card,PresetLabel(entry.SpecializationId),theme.caption_size).style.color=FrostlineStyle.Gold;
            var statNames=Locale=="ru"?StatRu:StatEn;
            if(!catalogDetails){AddLabel(card,L("Сильная сторона: ","Strength: ")+statNames[(int)entry.StrongestStat],12);AddLabel(card,L("Можно улучшить: ","Room to improve: ")+statNames[(int)entry.WeakestStat],12);}
            AddButton(card,"catalog_details",catalogDetails?L("Обзор","Overview"):L("Все характеристики","All abilities"),()=>{catalogDetails=!catalogDetails;catalogAbilityPage=0;Render();});
            if(catalogDetails)
            {
                for(int i=catalogAbilityPage*7;i<Math.Min(13,(catalogAbilityPage+1)*7);i++){var row=new VisualElement{name="catalog_stat_"+i};row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;card.Add(row);AddLabel(row,statNames[i],12).style.marginBottom=4;AddLabel(row,BiathlonRatingEngine.StatRating(entry.Stat((CareerStat)i)).ToString(),13,true).style.marginBottom=4;}
                var pages=new VisualElement();pages.style.flexDirection=FlexDirection.Row;card.Add(pages);
                AddButton(pages,"catalog_details_previous","‹",()=>{catalogAbilityPage=0;Render();},enabled:catalogAbilityPage>0);AddLabel(pages,(catalogAbilityPage+1)+" / 2",12).style.flexGrow=1;
                AddButton(pages,"catalog_details_next","›",()=>{catalogAbilityPage=1;Render();},enabled:catalogAbilityPage==0);
            }
            else
            {
                InfoRow(card,L("Лыжи","Ski"),entry.Ratings.Ski.ToString());InfoRow(card,L("Стрельба","Shooting"),entry.Ratings.Shooting.ToString());
                InfoRow(card,L("Физика","Physical"),entry.Ratings.Physical.ToString());InfoRow(card,L("Техника","Technique"),entry.Ratings.Technique.ToString());InfoRow(card,L("Психология","Mental"),entry.Ratings.Mental.ToString());
                AddLabel(card,L("Потенциал скрыт. Способности, возраст и страна спортсмена сохраняются.","Potential is hidden. The athlete's ability, age and nationality stay intact."),12).style.color=FrostlineStyle.Muted;
            }
        }
        private void ConfirmCatalogAthlete()
        {
            try{Session.CreateCareer(athleteCatalog,selectedCatalogAthlete);athleteCatalog=null;selectedCatalogAthlete=null;creatorMode=null;creationDraft=null;LastMessage=null;Render();}
            catch(Exception e)when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is System.IO.IOException||e is UnauthorizedAccessException)
            {ShowError(e is System.IO.IOException||e is UnauthorizedAccessException?"save_error":"name_error");}
        }
    }
}
