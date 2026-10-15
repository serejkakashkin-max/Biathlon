using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        public PlayerSettings Settings {get;private set;}
        private int settingsPage=1,bindingPage;
        private string bindingContext="ski",rebindAction,rebindDevice;
        private int rebindFrame;
        private Label bindingStatus;
        private IVisualElementScheduledItem bindingPoll;
        private FilePlayerSettingsStore reviewSettingsStore;
        public bool Rebinding=>rebindAction!=null;
        public void SetSettingsDirectoryForReview(string directory)
        {
            if(PersistPreferences||Session==null)throw new InvalidOperationException("Only initialized isolated reviews can override settings output.");
            reviewSettingsStore=new FilePlayerSettingsStore(directory,PlayerSettingsPreferences.Catalog);
            var loaded=reviewSettingsStore.Load(Settings);reviewSettingsStore.Save(loaded.Settings);PublishSettings(loaded.Settings);Render();
        }
        private void InitializeSettings()
        {
            PublishSettings(PersistPreferences?PlayerSettingsPreferences.Current:new PlayerSettings{Quality=RenderQualityDefinition.Load().default_tier});
            if(PersistPreferences&&(PlayerSettingsPreferences.LoadStatus==PlayerSettingsLoadStatus.BackupRecovered||PlayerSettingsPreferences.LoadStatus==PlayerSettingsLoadStatus.Invalid||PlayerSettingsPreferences.LoadStatus==PlayerSettingsLoadStatus.Unavailable))
                LastMessage=L("Настройки восстановлены. Проверьте параметры перед гонкой.","Settings recovered. Check your preferences before racing.");
        }
        private bool SaveSettings(PlayerSettings next)
        {
            next.Validate(PlayerSettingsPreferences.Catalog);
            try
            {
                if(PersistPreferences)PlayerSettingsPreferences.Save(next);
                else if(reviewSettingsStore!=null)reviewSettingsStore.Save(next);
                PublishSettings(next);LastMessage=null;return true;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException)
            {
                // Controls have already received their UI change event. Rebuild from
                // the last published record so a rejected value cannot look applied.
                LastMessage=L("Не удалось сохранить настройки. Освободите место и повторите.","Could not save settings. Check free space and retry.");Render();return false;
            }
        }
        private void PublishSettings(PlayerSettings next)
        {
            Settings=next.Clone(PlayerSettingsPreferences.Catalog);
            Locale=Settings.Locale;TextSizeIndex=Settings.TextSize;HighContrast=Settings.HighContrast;ReducedMotion=Settings.ReducedMotion;
            BiathlonAudio.Instance?.SetPreferences(PlayerSettingsPreferences.Audio(Settings),false);
        }
        private bool SaveAudioPreferences(BiathlonAudioPreferences audio)
        {var next=Settings.Clone(PlayerSettingsPreferences.Catalog);PlayerSettingsPreferences.CopyAudio(audio,next);return SaveSettings(next);}
        private void RenderSettings(VisualElement actions,VisualElement card)
        {
            AddLabel(actions,T("settings"),theme.section_size,true);
            string[] ru={"Изображение","Звук","Управление","Доступность","Сохранения"},en={"Display","Audio","Controls","Accessibility","Saves & support"};
            for(int i=0;i<ru.Length;i++)
            {
                int page=i;AddButton(actions,"settings_page_"+i,L(ru[i],en[i]),()=>{CancelRebind(false);settingsPage=page;Render();},settingsPage==i);
            }
            AddButton(actions,"back",T("back"),Back);
            var pages=new VisualElement[5];for(int i=0;i<pages.Length;i++){pages[i]=new VisualElement{name="settings_panel_"+i};pages[i].style.display=settingsPage==i?DisplayStyle.Flex:DisplayStyle.None;card.Add(pages[i]);}
            var display=Tile(pages[0]);AddLabel(display,L("ТВОЙ ВИД НА ТРАССУ","YOUR VIEW OF THE COURSE"),theme.section_size,true);
            AddLabel(display,L("Сохраняется для следующей гонки. Качество Ultra сохраняет полные текстуры и тени.","Applied to your next race. Ultra keeps full textures and shadows."),theme.caption_size);
            AddLabel(display,L("Качество графики","Graphics quality"),theme.caption_size);
            var tiers=new[]{"low","balanced","high"};
            var quality=new DropdownField(new List<string>{"Performance","High","Ultra"},Array.IndexOf(tiers,Settings.Quality)){name="graphics_quality"};StyleControl(quality);display.Add(quality);controls.Add(quality);
            quality.RegisterValueChangedCallback(e=>{var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.Quality=tiers[quality.index];SaveSettings(next);});
            AddLabel(display,L("Запуск — полный экран. Alt + Enter переключает оконный режим.","Launches fullscreen. Alt + Enter switches window mode."),theme.caption_size);
            AddAudioSettings(pages[1]);
            RenderControlSettings(pages[2]);
            var access=Tile(pages[3]);AddLabel(access,L("УДОБНО ЧИТАТЬ. ЛЕГКО ИГРАТЬ.","CLEAR TO READ. EASY TO FOLLOW."),theme.section_size,true);
            AddLabel(access,T("language"),theme.caption_size);
            var languages=new DropdownField(new List<string>{"Русский","English"},Locale=="ru"?0:1){name="language"};StyleControl(languages);access.Add(languages);controls.Add(languages);
            languages.RegisterValueChangedCallback(e=>ApplyPreferences(languages.index==0?"ru":"en",TextSizeIndex));
            AddLabel(access,T("text_size"),theme.caption_size);
            var sizes=new DropdownField(new List<string>{"100%","115%","130%"},TextSizeIndex){name="text_size"};StyleControl(sizes);access.Add(sizes);controls.Add(sizes);
            sizes.RegisterValueChangedCallback(e=>ApplyPreferences(Locale,sizes.index));
            var contrast=new Toggle(T("accessibility.contrast")){name="high_contrast",value=HighContrast};StyleControl(contrast);access.Add(contrast);controls.Add(contrast);contrast.RegisterValueChangedCallback(e=>ApplyAccessibility(e.newValue));
            var motion=new Toggle(L("Уменьшить анимацию","Reduce motion")){name="reduced_motion",value=ReducedMotion};StyleControl(motion);access.Add(motion);controls.Add(motion);
            motion.RegisterValueChangedCallback(e=>{var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.ReducedMotion=e.newValue;if(SaveSettings(next))Render();});
            AddLabel(access,T("accessibility.symbols"),theme.caption_size);AddLabel(access,T("accessibility.assists"),theme.caption_size).name="accessibility_assists";
            AddReliabilitySettings(pages[4]);
        }
        private void RenderControlSettings(VisualElement parent)
        {
            var top=Tile(parent);AddLabel(top,L("ТОЧНОСТЬ В ТВОИХ РУКАХ","CONTROL YOUR RACE"),theme.section_size,true);
            var device=new DropdownField(L("Устройство для гонки","Race input device"),new List<string>{L("Клавиатура и мышь","Keyboard & mouse"),L("Геймпад","Gamepad")},Settings.Device=="gamepad"?1:0){name="input_device"};StyleControl(device);top.Add(device);controls.Add(device);
            device.RegisterValueChangedCallback(e=>{var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.Device=device.index==1?"gamepad":"keyboard";SaveSettings(next);});
            var aiming=new Foldout{text=L("Настроить прицел","Aiming preferences"),name="aim_preferences",value=false};aiming.style.marginTop=8;aiming.style.fontSize=theme.body_size*Scale;FrostlineStyle.Text(aiming);top.Add(aiming);
            controls.Add(aiming.Q<Toggle>());
            var aim=new Slider(L("Чувствительность прицела","Aim sensitivity"),25,200){name="aim_sensitivity",value=Settings.AimSensitivity/10f};StyleControl(aim);aiming.Add(aim);controls.Add(aim);
            var amount=AddLabel(aiming,(Settings.AimSensitivity/10)+"%",theme.caption_size);amount.name="aim_sensitivity_value";
            aim.RegisterValueChangedCallback(e=>{var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.AimSensitivity=Mathf.RoundToInt(e.newValue*10);if(SaveSettings(next))amount.text=Mathf.RoundToInt(e.newValue)+"%";});
            var invert=new Toggle(L("Инвертировать прицел по вертикали","Invert vertical aim")){name="invert_aim",value=Settings.InvertAim};StyleControl(invert);aiming.Add(invert);controls.Add(invert);
            invert.RegisterValueChangedCallback(e=>{var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.InvertAim=e.newValue;SaveSettings(next);});
            var navigation=new VisualElement();navigation.style.flexDirection=FlexDirection.Row;parent.Add(navigation);
            foreach(string context in new[]{"ski","shooting","global"})
            {string chosen=context;AddButton(navigation,"binding_context_"+context,context=="ski"?L("Лыжи","Skiing"):context=="shooting"?L("Стрельба","Shooting"):L("Интерфейс","Interface"),()=>{CancelRebind(false);bindingContext=chosen;bindingPage=0;Render();},bindingContext==context);}
            var table=Tile(parent);table.name="bindings_table";
            var catalog=PlayerSettingsPreferences.Catalog;var rows=new List<ControlBindingDefinition>();for(int i=0;i<catalog.Count;i++)if(catalog.Get(i).Context==bindingContext)rows.Add(catalog.Get(i));
            int pageSize=compactLayout||TextSizeIndex==2?3:5;int pageCount=(rows.Count+pageSize-1)/pageSize;bindingPage=Math.Min(bindingPage,pageCount-1);
            var header=new VisualElement();header.style.flexDirection=FlexDirection.Row;table.Add(header);
            var title=AddLabel(header,L("ДЕЙСТВИЕ","ACTION"),theme.caption_size);title.style.width=Length.Percent(40);
            var keys=AddLabel(header,"KB / M",theme.caption_size);keys.style.width=Length.Percent(30);AddLabel(header,L("ГЕЙМПАД","GAMEPAD"),theme.caption_size);
            for(int i=bindingPage*pageSize;i<Math.Min(rows.Count,(bindingPage+1)*pageSize);i++)
            {
                var row=rows[i];var line=new VisualElement{name="binding_row_"+row.Id};line.style.flexDirection=FlexDirection.Row;line.style.alignItems=Align.Center;line.style.minHeight=44*Scale;table.Add(line);
                var label=AddLabel(line,row.Title(Locale),theme.caption_size);label.style.width=Length.Percent(40);label.style.flexShrink=1;
                foreach(string d in new[]{"keyboard","gamepad"})
                {
                    string action=row.Id,selectedDevice=d;var cell=new VisualElement();cell.style.width=Length.Percent(30);line.Add(cell);
                    AddButton(cell,"bind_"+row.Id+"_"+d,catalog.Path(Settings.Binding(catalog,row.Id,d)).Label,()=>BeginRebind(action,selectedDevice));
                    cell.Q<Button>().style.fontSize=theme.caption_size*Scale;cell.Q<Button>().style.paddingLeft=4;cell.Q<Button>().style.paddingRight=4;
                }
            }
            var footer=new VisualElement();footer.style.flexDirection=FlexDirection.Row;table.Add(footer);
            AddButton(footer,"binding_previous","‹",()=>{bindingPage=Math.Max(0,bindingPage-1);Render();},enabled:bindingPage>0);
            AddLabel(footer,(bindingPage+1)+" / "+pageCount,theme.caption_size);
            AddButton(footer,"binding_next","›",()=>{bindingPage=Math.Min(pageCount-1,bindingPage+1);Render();},enabled:bindingPage+1<pageCount);
            AddButton(footer,"bindings_reset",L("Сбросить кнопки","Reset bindings"),()=>{CancelRebind(false);var next=Settings.Clone(catalog);next.ResetBindings();if(SaveSettings(next))Render();});
            bindingStatus=AddLabel(parent,L("Выбери кнопку. Esc / Start — отмена. Назначение занятых кнопок заблокировано.","Select a binding. Esc / Start cancels. Conflicting controls are blocked."),theme.caption_size);bindingStatus.name="binding_status";
            AddButton(parent,"rebind_cancel",L("Отменить назначение","Cancel rebind"),()=>CancelRebind(),enabled:Rebinding);
        }
        public void BeginRebind(string action,string device)
        {
            var catalog=PlayerSettingsPreferences.Catalog;catalog.Action(action).Default(device);CancelRebind(false);
            rebindAction=action;rebindDevice=device;rebindFrame=Time.frameCount+2;
            if(bindingStatus!=null)bindingStatus.text=L("Нажми новую кнопку для «","Press a new control for “")+catalog.Action(action).Title(Locale)+(Locale=="ru"?"». Esc — отмена.":"”. Esc cancels.");
            Root.Q<Button>("rebind_cancel")?.SetEnabled(true);
            bindingPoll=Root.schedule.Execute(PollRebind).Every(16);
        }
        public bool CompleteRebind(string path)
        {
            if(!Rebinding)throw new InvalidOperationException("No pending rebind.");var catalog=PlayerSettingsPreferences.Catalog;
            try
            {
                catalog.ValidatePath(rebindAction,rebindDevice,path);string conflict=catalog.Conflict(Settings,rebindAction,rebindDevice,path);
                if(conflict!=null){if(bindingStatus!=null)bindingStatus.text=L("Уже назначено: ","Already assigned: ")+catalog.Action(conflict).Title(Locale);return false;}
                var next=Settings.Clone(catalog);next.Rebind(catalog,rebindAction,rebindDevice,path);if(!SaveSettings(next))return false;
                CancelRebind(false);Render();return true;
            }
            catch(ArgumentException){if(bindingStatus!=null)bindingStatus.text=L("Эта кнопка недоступна для выбранного действия.","This control is not available for the selected action.");return false;}
        }
        public void CancelRebind(bool announce=true)
        {
            bindingPoll?.Pause();bindingPoll=null;rebindAction=rebindDevice=null;
            Root?.Q<Button>("rebind_cancel")?.SetEnabled(false);
            if(announce&&bindingStatus!=null)bindingStatus.text=L("Назначение отменено. Кнопки не изменились.","Rebind cancelled. Controls are unchanged.");
        }
        private bool HandleRebindingCancel(NavigationCancelEvent e)
        {
            if(!Rebinding)return false;
            // UI navigation uses B as Back. During capture it is a valid gameplay button;
            // only the explicitly reserved Esc/Start controls cancel the operation.
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true)CancelRebind();
            e.StopImmediatePropagation();return true;
        }
        private void PollRebind()
        {
            if(!Rebinding||Session.Screen!=FrontEndScreen.Settings){CancelRebind(false);return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true){CancelRebind();return;}
            if(Time.frameCount<rebindFrame)return;var catalog=PlayerSettingsPreferences.Catalog;var action=catalog.Action(rebindAction);
            for(int i=0;i<catalog.PathCount;i++)
            {
                var path=catalog.PathAt(i);if(path.Device!=rebindDevice||path.Kind!=action.Kind)continue;
                InputDevice device=path.Path.StartsWith("<Keyboard>/",StringComparison.Ordinal)?(InputDevice)Keyboard.current:path.Path.StartsWith("<Mouse>/",StringComparison.Ordinal)?Mouse.current:Gamepad.current;
                var c=device?.TryGetChildControl<InputControl<float>>(path.Path.Substring(path.Path.IndexOf('/')+1));
                if(c==null)continue;
                bool selected=path.Kind=="button"?(c as ButtonControl)?.wasPressedThisFrame==true:Math.Abs(c.ReadUnprocessedValue())>(path.Path.StartsWith("<Mouse>/",StringComparison.Ordinal)?5:.65);
                if(selected){CompleteRebind(path.Path);return;}
            }
        }
    }
}
