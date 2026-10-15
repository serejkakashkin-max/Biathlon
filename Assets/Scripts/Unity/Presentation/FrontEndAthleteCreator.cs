using System;
using System.Collections.Generic;
using Biathlon.Career;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private CareerCreationDraft creationDraft;
        private AthleteCreatorStudio creatorStudio;
        private int creatorStep,allocationPage;
        public AthleteCreatorStudio CreatorStudio=>creatorStudio;
        public CareerCreationDraft CreationDraft=>creationDraft;
        private static readonly string[] StatRu={"Выносливость","Анаэробный резерв","Восстановление","Сила","Лыжная техника","Подъёмы","Спуски","Повороты","Дыхание","Стрельба лёжа","Стрельба стоя","Скорость стрельбы","Самообладание"};
        private static readonly string[] StatEn={"Endurance","Anaerobic capacity","Recovery","Strength","Ski technique","Climbing","Downhill","Cornering","Breath control","Prone stability","Standing stability","Shooting speed","Composure"};
        private string TierLabel(string id)=>id=="junior_novice"?L("Юниорская лига","Junior circuit"):id=="senior_debut"?L("Национальная лига","National circuit"):L("Кубок мира","World Cup");
        private string PresetLabel(string id)
        {
            switch(id)
            {
                case "skier":return L("Скоростной лыжник","Fast skier");case "shooter":return L("Меткий стрелок","Sharp shooter");
                case "endurance":return L("Выносливый спортсмен","Endurance athlete");case "sprinter":return L("Взрывной спринтер","Explosive sprinter");
                case "technician":return L("Техничный спортсмен","Technical athlete");default:return L("Универсал","Balanced athlete");
            }
        }
        private string OriginLabel(string id)
        {
            switch(id)
            {
                case "sport_school":return L("Спортивная школа","Sport school");case "former_skier":return L("Бывший лыжник","Former skier");
                case "young_shooter":return L("Юный стрелок","Young shooter");case "late_debut":return L("Поздний дебют","Late debut");
                case "own_path":return L("Собственный путь","Your own path");default:return L("Обычный новичок","Newcomer");
            }
        }
        private void EnsureCreationDraft()
        {
            if(creationDraft!=null)return;
            creationDraft=new CareerCreationDraft(CareerCreationDefinition.FromJson(ReadCore("career_creation_v1.json")),Session.StartProfiles,
                BiathlonRatingDefinition.FromJson(ReadCore("career_ratings_v1.json")),CareerDefinitions.ForNewProductionCareer(ReadCore).Roster,unchecked((ulong)DateTime.UtcNow.Ticks));
            creationDraft.Name=athleteName;if(!string.IsNullOrEmpty(nation))creationDraft.Nation=nation;
            creationDraft.SelectTier(startProfileId);creationDraft.SetAge(startingAge);creatorStep=0;
        }
        private void ReleaseCreatorStudio()
        {if(creatorStudio!=null){creatorStudio.Release();Destroy(creatorStudio);creatorStudio=null;}}
        private void RenderAthleteCreator(VisualElement body,VisualElement left,VisualElement actions,VisualElement card)
        {
            EnsureCreationDraft();var draft=creationDraft;
            left.style.width=Length.Percent(compactLayout?30:25);left.style.paddingRight=16;
            card.style.flexGrow=0;card.style.flexBasis=StyleKeyword.Auto;card.style.width=Length.Percent(compactLayout?27:25);
            card.style.paddingLeft=16;card.style.paddingRight=16;card.style.paddingTop=12;
            var steps=new VisualElement{name="creator_steps"};steps.style.flexDirection=FlexDirection.Row;steps.style.marginBottom=12;actions.Add(steps);
            string[] titles={L("Данные","Identity"),L("Облик","Look"),L("Навыки","Skills")};
            for(int i=0;i<3;i++)
            {
                int step=i;AddButton(steps,"creator_step_"+i,titles[i],()=>{creatorStep=step;LastMessage=null;Render();},creatorStep==i);
                var button=steps.Q<Button>("creator_step_"+i);button.style.flexGrow=1;button.style.flexBasis=0;button.style.minWidth=0;button.style.fontSize=14*Scale;button.style.paddingLeft=6;button.style.paddingRight=3;button.style.height=32;
            }
            if(creatorStep==0)RenderCreatorIdentity(actions);
            else if(creatorStep==1)RenderCreatorAppearance(actions);
            else if(creatorStep==2)RenderCreatorAllocation(actions);
            else
            {
                AddLabel(actions,L("НАЧАЛО ТВОЕЙ ИСТОРИИ","YOUR STORY BEGINS"),theme.caption_size).style.color=FrostlineStyle.Gold;
                AddLabel(actions,draft.Name,theme.section_size,true);
                AddLabel(actions,T("nation."+draft.Nation)+" · "+draft.Age+L(" лет"," years"),theme.body_size);
                AddLabel(actions,TierLabel(draft.StartProfileId),theme.body_size,true);AddLabel(actions,PresetLabel(draft.SpecializationId),theme.body_size);
                AddLabel(actions,OriginLabel(draft.OriginStoryId),theme.caption_size);
                AddLabel(actions,L("Способности и внешность сохранены в этом выборе. Дальше — тренировки и первый сезон.","Your abilities and appearance are ready. Next: training and your first season."),theme.caption_size);
            }
            if(creatorStep==3)AddButton(actions,"confirm_create",L("Начать карьеру","Begin career"),ConfirmCreatedAthlete,true);
            else AddButton(actions,"create",L("Проверить выбор","Review athlete"),ReviewCreatedAthlete,true);
            AddButton(actions,"back",T("back"),Back);

            if(creatorStudio==null){creatorStudio=gameObject.AddComponent<AthleteCreatorStudio>();creatorStudio.Initialize();}
            creatorStudio.ReducedMotion=ReducedMotion;
            var appearance=creatorStudio.Rig.GetComponent<AthleteAppearancePresentation>();appearance.ApplyNation(draft.Nation);appearance.ApplyProfile(draft.Appearance);
            var stage=new VisualElement{name="creator_stage"};stage.style.flexGrow=1;stage.style.flexBasis=0;stage.style.minWidth=0;stage.style.marginRight=16;stage.style.backgroundColor=new Color(.063f,.137f,.188f);
            body.Insert(1,stage);
            var header=AddLabel(stage,L("ТВОЙ БИАТЛОНИСТ","YOUR BIATHLETE"),theme.caption_size,true);header.style.paddingLeft=16;header.style.paddingTop=12;header.style.letterSpacing=1.8f;
            var image=new Image{name="creator_model",image=creatorStudio.Texture,scaleMode=ScaleMode.ScaleToFit,focusable=true};
            image.style.flexGrow=1;image.style.flexBasis=0;image.style.minHeight=0;stage.Add(image);
            float lastX=0;
            image.RegisterCallback<PointerDownEvent>(e=>{if(e.button!=0)return;lastX=e.position.x;image.CapturePointer(e.pointerId);image.Focus();e.StopPropagation();});
            image.RegisterCallback<PointerMoveEvent>(e=>{if(!image.HasPointerCapture(e.pointerId))return;creatorStudio.Rotate((e.position.x-lastX)*.4f);lastX=e.position.x;e.StopPropagation();});
            image.RegisterCallback<PointerUpEvent>(e=>{if(image.HasPointerCapture(e.pointerId))image.ReleasePointer(e.pointerId);});
            image.RegisterCallback<WheelEvent>(e=>{creatorStudio.SetView(creatorStudio.Yaw,creatorStudio.Zoom-e.delta.y*.04f);e.StopPropagation();});
            var views=new VisualElement();views.style.flexDirection=FlexDirection.Row;views.style.flexWrap=Wrap.Wrap;views.style.paddingLeft=10;views.style.paddingRight=10;stage.Add(views);
            View("front",L("Спереди","Front"),0,0);View("side",L("Сбоку","Side"),90,0);View("back",L("Сзади","Back"),180,0);View("face",L("Лицо","Face"),0,1);
            void View(string id,string label,float yaw,float zoom)
            {AddButton(views,"creator_view_"+id,label,()=>creatorStudio.SetView(yaw,zoom));var b=views.Q<Button>("creator_view_"+id);b.style.flexGrow=1;b.style.minWidth=0;b.style.fontSize=13*Scale;b.style.height=30;b.style.paddingLeft=5;b.style.paddingRight=5;}
            var poses=new List<string>{L("Спокойная стойка","Idle"),L("Спортивная стойка","Sport stance"),L("Лыжный ход","Ski skating"),L("Винтовка на спине","Rifle carry")};
            var pose=CreatorChoice(stage,"creator_pose",poses,(int)creatorStudio.Pose,index=>creatorStudio.SetPose((AthletePreviewPose)index));pose.style.marginLeft=10;pose.style.marginRight=10;pose.style.height=32;
            var hint=AddLabel(stage,L("Перетащи для поворота · колесо для приближения","Drag to rotate · scroll to zoom"),12);hint.style.unityTextAlign=TextAnchor.MiddleCenter;hint.style.color=FrostlineStyle.Muted;
            RenderCreationRatings(card);
        }
        private DropdownField CreatorChoice(VisualElement parent,string id,List<string> labels,int index,Action<int> changed)
        {
            var control=new DropdownField(labels,Math.Max(0,index)){name=id};StyleControl(control);parent.Add(control);controls.Add(control);
            control.RegisterValueChangedCallback(e=>changed(control.index));return control;
        }
        private void RenderCreatorIdentity(VisualElement actions)
        {
            var d=creationDraft;var roster=CareerDefinitions.ForNewProductionCareer(ReadCore).Roster;
            AddLabel(actions,T("name"),theme.caption_size);var name=new TextField{name="athlete_name",value=d.Name,maxLength=100};StyleControl(name);actions.Add(name);controls.Add(name);
            name.RegisterValueChangedCallback(e=>{d.Name=e.newValue;athleteName=e.newValue;});
            var nations=new List<string>();var countries=new List<string>();for(int i=0;i<roster.NationCount;i++){nations.Add(roster.Nation(i));countries.Add(T("nation."+roster.Nation(i)));}
            AddLabel(actions,T("nation"),theme.caption_size);CreatorChoice(actions,"athlete_nation",countries,nations.IndexOf(d.Nation),i=>{d.Nation=nation=nations[i];creatorStudio?.Rig.GetComponent<AthleteAppearancePresentation>().ApplyNation(d.Nation);});
            var starts=Session.StartProfiles;var tiers=new List<string>();int selected=0;
            for(int i=0;i<starts.Count;i++){tiers.Add(TierLabel(starts.Get(i).Id));if(starts.Get(i).Id==d.StartProfileId)selected=i;}
            AddLabel(actions,L("Начало карьеры","Career start"),theme.caption_size);
            CreatorChoice(actions,"career_start_profile",tiers,selected,i=>{d.SelectTier(starts.Get(i).Id);startProfileId=d.StartProfileId;startingAge=d.Age;LastMessage=null;Render();});
            var p=starts.Get(d.StartProfileId);AddLabel(actions,L("Возраст на 31 декабря","Age on December 31"),theme.caption_size);
            var age=new SliderInt(p.MinimumAge,p.MaximumAge){name="career_start_age",value=d.Age,showInputField=true};StyleControl(age);actions.Add(age);controls.Add(age);
            age.RegisterValueChangedCallback(e=>{d.SetAge(e.newValue);startingAge=d.Age;});
        }
        private void RenderCreatorAppearance(VisualElement actions)
        {
            var recipe=AthleteFaceCatalog.Load();var p=creationDraft.Appearance;
            int face=0;for(int i=0;i<recipe.profiles.Length;i++)if(recipe.profiles[i].id==p.FaceId)face=i;
            int skin=recipe.SkinIndex(p.SkinId),body=recipe.BodyIndex(p.BodyId);
            var faceLabels=new List<string>{L("Овальное","Oval"),L("Угловатое","Angular"),L("Округлое","Round"),L("Узкое","Narrow"),L("Широкое","Broad"),L("Выраженный подбородок","Defined chin")};
            AddLabel(actions,L("Форма лица","Face"),theme.caption_size);CreatorChoice(actions,"creator_face",faceLabels,face,i=>Change(recipe.profiles[i].id,p.SkinId,p.BodyId));
            var tones=new List<string>{L("Светлый нейтральный","Light neutral"),L("Светлый тёплый","Light warm"),L("Загорелый","Warm tan"),L("Оливковый","Olive"),L("Тёплый коричневый","Warm brown"),L("Глубокий коричневый","Deep brown"),L("Тёмный нейтральный","Dark neutral"),L("Глубокий тёмный","Deep dark")};
            AddLabel(actions,L("Оттенок кожи","Skin tone"),theme.caption_size);CreatorChoice(actions,"creator_skin",tones,skin,i=>Change(p.FaceId,recipe.skin_ids[i],p.BodyId));
            AddLabel(actions,L("Телосложение","Build"),theme.caption_size);CreatorChoice(actions,"creator_body",new List<string>{L("Сухое","Lean"),L("Среднее","Natural"),L("Атлетичное","Athletic")},body,i=>Change(p.FaceId,p.SkinId,recipe.body_ids[i]));
            AddLabel(actions,L("Форма сборной и стартовый номер сохраняют свою роль в гонке.","National kit and race bib keep their role in competition."),theme.caption_size).style.color=FrostlineStyle.Muted;
            void Change(string faceId,string toneId,string bodyId){creationDraft.SetAppearance(AppearanceProfile.Create(faceId,toneId,bodyId));Render();}
        }
        private void RenderCreatorAllocation(VisualElement actions)
        {
            var policy=CareerCreationDefinition.FromJson(ReadCore("career_creation_v1.json"));var labels=new List<string>();int selected=0;
            for(int i=0;i<policy.PresetCount;i++){labels.Add(PresetLabel(policy.Preset(i).Id));if(policy.Preset(i).Id==creationDraft.SpecializationId)selected=i;}
            AddLabel(actions,L("Специализация","Specialization"),theme.caption_size);CreatorChoice(actions,"creator_specialization",labels,selected,i=>{creationDraft.SelectPreset(policy.Preset(i).Id);Render();});
            var origins=new List<string>();var originIds=new List<string>();for(int i=0;i<policy.OriginCount;i++){var origin=policy.Origin(i);if(!origin.Allows(creationDraft.Age))continue;origins.Add(OriginLabel(origin.Id));originIds.Add(origin.Id);}
            AddLabel(actions,L("История спортсмена","Background"),theme.caption_size);CreatorChoice(actions,"creator_origin",origins,originIds.IndexOf(creationDraft.OriginStoryId),i=>{creationDraft.SelectOrigin(originIds[i]);Render();});
            const int perPage=4;allocationPage=Mathf.Clamp(allocationPage,0,3);
            for(int i=allocationPage*perPage;i<Math.Min(13,(allocationPage+1)*perPage);i++)
            {
                int index=i;var stat=(CareerStat)i;var row=new VisualElement{name="creator_stat_"+i};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;actions.Add(row);
                var label=AddLabel(row,Locale=="ru"?StatRu[i]:StatEn[i],13,true);label.style.flexGrow=1;label.style.flexBasis=0;label.style.marginBottom=0;
                AddButton(row,"creator_minus_"+i,"−",()=>Adjust(-1));var minus=row.Q<Button>("creator_minus_"+i);minus.style.width=28;minus.style.height=30;minus.style.paddingLeft=8;
                AddLabel(row,BiathlonRatingEngine.StatRating(creationDraft.Stat(stat)).ToString(),18,true).style.width=28;
                AddButton(row,"creator_plus_"+i,"+",()=>Adjust(1));var plus=row.Q<Button>("creator_plus_"+i);plus.style.width=28;plus.style.height=30;plus.style.paddingLeft=8;
                void Adjust(int delta)
                {
                    try{creationDraft.SetAllocation((CareerStat)index,creationDraft.Allocation((CareerStat)index)+delta);LastMessage=null;}
                    catch(Exception e)when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException){LastMessage=L("Достигнут лимит очков или навыка.","Point or ability limit reached.");}
                    Render();
                }
            }
            var pager=new VisualElement();pager.style.flexDirection=FlexDirection.Row;actions.Add(pager);
            AddButton(pager,"creator_stats_previous","‹",()=>{allocationPage=Math.Max(0,allocationPage-1);Render();},enabled:allocationPage>0);
            AddLabel(pager,(allocationPage+1)+" / 4",theme.caption_size).style.flexGrow=1;
            AddButton(pager,"creator_stats_next","›",()=>{allocationPage=Math.Min(3,allocationPage+1);Render();},enabled:allocationPage<3);
            AddButton(actions,"creator_reset",L("Сбросить распределение","Reset allocation"),()=>{creationDraft.ResetAllocation();Render();});
        }
        private void RenderCreationRatings(VisualElement parent)
        {
            AddLabel(parent,L("ПОСТОЯННЫЕ СПОСОБНОСТИ","PERMANENT ABILITY"),12,true).style.color=FrostlineStyle.Muted;
            var score=AddLabel(parent,creationDraft.Overall.ToString(),80,true);score.name="creator_ovr";score.style.marginBottom=0;score.style.color=FrostlineStyle.Accent;
            AddLabel(parent,"OVR / 99",theme.caption_size).style.letterSpacing=2;
            AddLabel(parent,TierLabel(creationDraft.StartProfileId),theme.body_size,true);AddLabel(parent,PresetLabel(creationDraft.SpecializationId),theme.caption_size).style.color=FrostlineStyle.Gold;
            var stats=new int[13];for(int i=0;i<13;i++)stats[i]=creationDraft.Stat((CareerStat)i);var ratings=BiathlonRatingDefinition.FromJson(ReadCore("career_ratings_v1.json"));
            string[] names={L("Лыжи","Ski"),L("Стрельба","Shooting"),L("Физика","Physical"),L("Техника","Technique"),L("Психология","Mental")};
            for(int i=0;i<names.Length;i++)
            {
                int value=BiathlonRatingEngine.CalculateStats(stats,ratings,(BiathlonRating)(i+1));
                var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;parent.Add(row);
                AddLabel(row,names[i],theme.caption_size);AddLabel(row,value.ToString(),theme.caption_size,true);
                var rail=new VisualElement();rail.style.height=4;rail.style.marginBottom=12;rail.style.backgroundColor=FrostlineStyle.Surface;parent.Add(rail);
                var fill=new VisualElement();fill.style.height=4;fill.style.width=Length.Percent(value/99f*100);fill.style.backgroundColor=FrostlineStyle.Accent;rail.Add(fill);
            }
            AddLabel(parent,L("ОЧКИ СПЕЦИАЛИЗАЦИИ","SPECIALIZATION POINTS"),12,true).style.marginTop=12;
            var budget=AddLabel(parent,creationDraft.BudgetRemaining+" / "+creationDraft.Tier.Budget,26,true);budget.name="creator_budget";budget.style.color=FrostlineStyle.Gold;
            AddLabel(parent,L("Потенциал раскроется по ходу карьеры. Рейтинг не включает текущую форму и усталость.","Potential emerges during your career. Rating excludes current form and fatigue."),12).style.color=FrostlineStyle.Muted;
        }
        private void ReviewCreatedAthlete()
        {
            try{creationDraft.Validate();creatorStep=3;LastMessage=null;Render();}
            catch(Exception e)when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException){ShowError("name_error");}
        }
        private void ConfirmCreatedAthlete()
        {
            try{Session.CreateCareer("career_"+Guid.NewGuid().ToString("N"),creationDraft);creationDraft=null;LastMessage=null;Render();}
            catch(Exception e)when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is System.IO.IOException||e is UnauthorizedAccessException)
            {ShowError(e is System.IO.IOException||e is UnauthorizedAccessException?"save_error":"name_error");}
        }
    }
}
