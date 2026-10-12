using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Replay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Retained menu UI. Rendering/navigation never advances career or race authority.</summary>
    public sealed partial class FrontEndMenu : MonoBehaviour
    {
        public bool AutoStart=true, PersistPreferences=true;
        public FrontEndSession Session { get; private set; }
        public VisualElement Root => document?.rootVisualElement;
        public string Locale { get; private set; }
        public int TextSizeIndex { get; private set; }
        public bool HighContrast {get;private set;}
        public string LastMessage { get; private set; }
        public CareerRacePresentation Race { get; private set; }
        public RaceReplayLibrary ReplayLibrary {get;private set;}
        public RaceReplayPresentation Replays {get;private set;}
        private Camera menuCamera;
        private Texture2D backdrop;
        private UIDocument document;
        private PanelSettings panel;
        private FrontEndTheme theme;
        private UiTextCatalog text;
        private readonly List<VisualElement> controls=new List<VisualElement>();
        private readonly Dictionary<FrontEndScreen,string> rememberedFocus=new Dictionary<FrontEndScreen,string>();
        private string athleteName="",nation;
        private string startProfileId="junior_novice";
        private int startingAge=18;
        private Label message;
        private Color ink,paper,accent,focus;
        private bool compactLayout;
        private float Scale => TextSizeIndex==0?1:TextSizeIndex==1?1.15f:1.3f;
        private string T(string key)=>text.Text(key,Locale);
        private string F(string key,params object[] values)=>string.Format(CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US"),T(key),values);
        private static string ReadCore(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private void Start()
        { if(AutoStart&&Session==null)Initialize(new FileCareerProfileStore(Path.Combine(Application.persistentDataPath,"Profiles"))); }
        public void Initialize(ICareerProfileStore profiles,RaceReplayLibrary replayLibrary=null)
        {
            if(Session!=null)throw new InvalidOperationException("Initialize front end once.");
            theme=FrontEndTheme.Load();text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            backdrop=Resources.Load<Texture2D>("Biathlon/UI/menu_background");
            Session=new FrontEndSession(ReadCore,profiles,s=>new SeededRandomSource(s));
            ReplayLibrary=replayLibrary??new RaceReplayLibrary(Path.Combine(Application.persistentDataPath,"Replays"));
            Replays=gameObject.AddComponent<RaceReplayPresentation>();
            Locale=PersistPreferences?PlayerPrefs.GetString("biathlon.ui.locale","ru"):"ru";
            if(Locale!="ru"&&Locale!="en")Locale="ru";
            TextSizeIndex=PersistPreferences?Mathf.Clamp(PlayerPrefs.GetInt("biathlon.ui.text_size",0),0,2):0;
            HighContrast=PersistPreferences&&PlayerPrefs.GetInt("biathlon.ui.high_contrast",0)==1;
            ink=FrontEndTheme.Color(theme.ink);paper=FrontEndTheme.Color(theme.paper);accent=FrontEndTheme.Color(theme.accent);focus=FrontEndTheme.Color(theme.focus);
            panel=FrontEndPanelSettings.Create();panel.name=theme.id;
            gameObject.AddComponent<PanelInputConfiguration>().panelInputRedirection=PanelInputConfiguration.PanelInputRedirection.Never;
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("Biathlon/UI/front_end");
            if(panel.themeStyleSheet==null)throw new FileNotFoundException("Missing front-end theme style sheet.");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(theme.reference_width,theme.reference_height);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            {
                var camera=new GameObject("Front end camera",typeof(Camera));camera.tag="MainCamera";camera.transform.SetParent(transform,false);
                menuCamera=camera.GetComponent<Camera>();menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=FrontEndTheme.Color(theme.snow);menuCamera.orthographic=true;menuCamera.cullingMask=0;
            }
            Race=gameObject.AddComponent<CareerRacePresentation>();
            document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=100;
            Root.RegisterCallback<NavigationMoveEvent>(Navigate);
            Root.RegisterCallback<GeometryChangedEvent>(e=>{bool compact=e.newRect.width<1250;if(compactLayout==compact)return;compactLayout=compact;Root.schedule.Execute(Render);});
            Root.RegisterCallback<FocusInEvent>(RememberFocus);
            Root.RegisterCallback<NavigationCancelEvent>(e=>{if(Session.Screen!=FrontEndScreen.Home)Back();e.StopPropagation();},TrickleDown.TrickleDown);
            Render();
        }
        public void Back()
        {
            if(Race.Loading)return;
            if(Replays.Loading)return;
            if(Replays.Active){Replays.Close();return;}
            if(Session.Screen==FrontEndScreen.Race){if(Race.Paused&&!Race.AwaitingSave)Race.Resume();return;}
            if(Session.Screen==FrontEndScreen.Results)Session.ReturnFromResults();else Session.Back();
            LastMessage=null;Render();
        }
        public void RefreshFrontEnd()=>Render();
        public void ShowFrontEndError(string key)=>ShowError(key);
        public void ShowRaceFailure(){SetRaceVisible(false);ShowError("race_fault");Render();}
        public void SetRaceVisible(bool visible)
        {
            Root.style.display=visible?DisplayStyle.None:DisplayStyle.Flex;menuCamera.enabled=!visible;
            if(visible){Root.panel?.focusController.focusedElement?.Blur();LastMessage=null;}
        }
        public void ApplyPreferences(string locale,int textSizeIndex)
        {
            if(locale!="ru"&&locale!="en"||textSizeIndex<0||textSizeIndex>2)throw new ArgumentException("Unsupported UI preference.");
            Locale=locale;TextSizeIndex=textSizeIndex;
            if(PersistPreferences){PlayerPrefs.SetString("biathlon.ui.locale",Locale);PlayerPrefs.SetInt("biathlon.ui.text_size",TextSizeIndex);PlayerPrefs.Save();}
            Render();
        }
        public void ApplyAccessibility(bool contrast)
        {HighContrast=contrast;if(PersistPreferences){PlayerPrefs.SetInt("biathlon.ui.high_contrast",contrast?1:0);PlayerPrefs.Save();}Render();}
        private bool IsShown(VisualElement element)
        {if(!element.enabledInHierarchy)return false;for(var e=element;e!=null&&e!=Root;e=e.parent)if(e.resolvedStyle.display==DisplayStyle.None)return false;return true;}
        private void RememberFocus(FocusInEvent e)
        {
            for(var element=e.target as VisualElement;element!=null&&element!=Root;element=element.parent)
                if(controls.Contains(element)){rememberedFocus[Session.Screen]=element.name;element.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(element);break;}
        }
        private void Navigate(NavigationMoveEvent e)
        {
            int step=e.direction==NavigationMoveEvent.Direction.Down?1:e.direction==NavigationMoveEvent.Direction.Up?-1:0;
            if(step==0||controls.Count==0)return;
            var selected=Root.panel?.focusController.focusedElement as VisualElement;
            while(selected!=null&&!controls.Contains(selected))selected=selected.parent;
            int start=controls.IndexOf(selected);
            for(int i=1;i<=controls.Count;i++)
            {
                var candidate=controls[(start+step*i+controls.Count*2)%controls.Count];
                if(IsShown(candidate)){candidate.Focus();Root.panel.focusController.IgnoreEvent(e);e.StopPropagation();break;}
            }
        }
        private void Render()
        {
            Root.Clear();controls.Clear();paper=HighContrast?Color.white:FrontEndTheme.Color(theme.paper);ink=HighContrast?Color.black:FrontEndTheme.Color(theme.ink);
            Root.style.flexGrow=1;Root.style.backgroundColor=FrontEndTheme.Color(theme.snow);Root.style.color=ink;
            bool home=Session.Screen==FrontEndScreen.Home;
            var background=new Image{image=backdrop,scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore,name="menu_backdrop"};
            background.style.position=Position.Absolute;background.style.left=0;background.style.right=0;background.style.top=0;background.style.bottom=0;Root.Add(background);
            var shade=new VisualElement{pickingMode=PickingMode.Ignore};shade.style.position=Position.Absolute;shade.style.left=0;shade.style.top=0;shade.style.bottom=0;
            shade.style.width=new Length(home?44:100,LengthUnit.Percent);shade.style.backgroundColor=FrontEndTheme.Color(home?theme.home_shade:theme.menu_panel);Root.Add(shade);
            Root.style.paddingTop=theme.page_padding;Root.style.paddingBottom=theme.page_padding;
            Root.style.paddingLeft=theme.page_padding;Root.style.paddingRight=theme.page_padding;
            var body=new VisualElement();body.style.flexGrow=1;body.style.flexDirection=FlexDirection.Row;Root.Add(body);
            var left=new VisualElement();left.style.width=new Length(home?44:23,LengthUnit.Percent);left.style.paddingRight=theme.page_padding;body.Add(left);
            var brand=AddLabel(left,T("brand"),theme.title_size,true);brand.style.fontSize=home?theme.title_size:theme.section_size;brand.style.whiteSpace=WhiteSpace.NoWrap;
            brand.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;brand.style.letterSpacing=2;
            AddLabel(left,home?T("discipline"):Session.Career==null?L("КАРЬЕРА","CAREER"):Session.Definitions.Calendar.Circuit==CareerCircuit.Junior?L("ЮНИОРСКАЯ ЛИГА","JUNIOR CIRCUIT"):Session.Definitions.Calendar.Circuit==CareerCircuit.Development?L("ПУТЬ В КУБОК МИРА","ROAD TO WORLD CUP"):L("КУБОК МИРА","WORLD CUP"),theme.caption_size);
            var actions=new ScrollView(ScrollViewMode.Vertical){verticalScrollerVisibility=ScrollerVisibility.Hidden,horizontalScrollerVisibility=ScrollerVisibility.Hidden};actions.style.marginTop=theme.spacing*2;actions.style.flexShrink=1;actions.style.minHeight=0;left.Add(actions);
            var card=new ScrollView(ScrollViewMode.Vertical){name="page_content",verticalScrollerVisibility=ScrollerVisibility.Hidden,horizontalScrollerVisibility=ScrollerVisibility.Hidden};card.style.flexGrow=1;card.style.flexBasis=0;card.style.paddingTop=theme.page_padding;
            card.style.paddingBottom=theme.page_padding;card.style.paddingLeft=theme.page_padding;card.style.paddingRight=theme.page_padding;
            card.style.backgroundColor=paper;card.style.minWidth=0;card.contentContainer.style.width=Length.Percent(100);
            card.style.borderTopLeftRadius=card.style.borderTopRightRadius=card.style.borderBottomLeftRadius=card.style.borderBottomRightRadius=18;body.Add(card);
            if(Session.Screen==FrontEndScreen.Home)
            {
                AddButton(actions,"new_career",T("new_career"),()=>{Session.OpenNewCareer();LastMessage=null;Render();},true);
                AddButton(actions,"continue",T("continue"),Continue,enabled:Session.CanContinue);
                AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});
                AddButton(actions,"quit",T("quit"),()=>Application.Quit());
                card.style.backgroundColor=Color.clear;card.style.justifyContent=Justify.FlexEnd;card.pickingMode=PickingMode.Ignore;
                var caption=AddLabel(left,T("home_note"),theme.caption_size);caption.style.marginTop=theme.spacing*2;
            }
            else if(Session.Screen==FrontEndScreen.NewCareer)
            {
                AddLabel(actions,T("new_career"),theme.section_size,true);AddLabel(actions,T("name"),theme.caption_size);
                var name=new TextField{name="athlete_name",value=athleteName};name.maxLength=100;StyleControl(name);actions.Add(name);controls.Add(name);
                name.RegisterValueChangedCallback(e=>athleteName=e.newValue);
                AddLabel(actions,T("nation"),theme.caption_size);
                var roster=CareerDefinitions.ForNewProductionCareer(ReadCore).Roster;
                var nations=new List<string>();var labels=new List<string>();
                for(int i=0;i<roster.NationCount;i++){nations.Add(roster.Nation(i));labels.Add(T("nation."+roster.Nation(i)));}
                int selected=Math.Max(0,nations.IndexOf(nation));nation=nations[selected];
                var country=new DropdownField(labels,selected){name="athlete_nation"};StyleControl(country);actions.Add(country);controls.Add(country);
                country.RegisterValueChangedCallback(e=>nation=nations[country.index]);
                AddLabel(actions,L("Начало карьеры","Career start"),theme.caption_size);
                var profiles=Session.StartProfiles;
                var profileLabels=new List<string>();int profileIndex=0;
                for(int i=0;i<profiles.Count;i++){var p=profiles.Get(i);if(p.Id==startProfileId)profileIndex=i;profileLabels.Add(p.Id=="junior_novice"?L("Юниор","Junior"):L("Взрослый","Senior"));}
                var profileChoice=new DropdownField(profileLabels,profileIndex){name="career_start_profile"};StyleControl(profileChoice);actions.Add(profileChoice);controls.Add(profileChoice);
                var selectedProfile=profiles.Get(profileIndex);startingAge=Mathf.Clamp(startingAge,selectedProfile.MinimumAge,selectedProfile.MaximumAge);
                AddLabel(actions,L("Возраст на 31 декабря","Age on December 31"),theme.caption_size);
                var age=new SliderInt(selectedProfile.MinimumAge,selectedProfile.MaximumAge){name="career_start_age",value=startingAge,showInputField=true};StyleControl(age);actions.Add(age);controls.Add(age);
                age.RegisterValueChangedCallback(e=>startingAge=e.newValue);
                profileChoice.RegisterValueChangedCallback(e=>{var p=profiles.Get(profileChoice.index);startProfileId=p.Id;startingAge=p.DefaultAge;Render();});
                AddButton(actions,"create",T("create"),Create,true);AddButton(actions,"back",T("back"),Back);
                AddVenueCard(card);
                AddLabel(card,startProfileId=="junior_novice"?L("ТВОЙ ПЕРВЫЙ СЕЗОН","YOUR FIRST SEASON"):L("НОВАЯ ЦЕЛЬ","A NEW CHALLENGE"),theme.caption_size).style.color=accent;
                AddLabel(card,startProfileId=="junior_novice"?L("Юниорская лига","Junior circuit"):L("Путь в Кубок мира","Road to the World Cup"),theme.section_size,true);
                AddLabel(card,L("Соперники разного уровня. Общая физика. Тренировки, форма и тактика решают исход гонки.","Varied rivals. Shared physics. Training, readiness and tactics decide the race."),theme.body_size);
            }
            else if(Session.Screen==FrontEndScreen.Career)
            {
                left.style.width=new Length(23,LengthUnit.Percent);shade.style.width=new Length(100,LengthUnit.Percent);
                brand.style.fontSize=theme.section_size;RenderCareerDashboard(actions,card);
            }
            else if(Session.Screen==FrontEndScreen.PreRace)
            {
                var e=Session.PreparedWeek.Event;
                string format=e.Format==CompetitionFormat.Sprint?"sprint":e.Format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
                var summary=new PreRaceSummary(Session.PreparedWeek,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+e.VenueId+"/"+format+"_profile")),
                    RaceSnowConditions.FromJson(ReadCore("race_snow_neutral_v1.json")),SkiEquipmentCatalog.FromJson(ReadCore("ski_equipment_provisional_v1.json")));
                AddLabel(actions,T("pre_race"),theme.section_size,true);AddLabel(actions,T("format."+(int)e.Format),theme.section_size,true);
                AddLabel(actions,summary.Player.Name,theme.body_size);
                AddLabel(actions,F("athlete_state",summary.Player.Form/100,summary.Player.Fatigue/100),theme.body_size);
                AddButton(actions,"start_race",T(Race.Loading?"loading":"start_race"),()=>Race.Launch(this),true,!Race.Loading);
                AddButton(actions,"back",T("back"),Back,enabled:!Race.Loading);
                AddLabel(card,"WOODLAND BASIN",theme.caption_size).style.color=accent;
                AddLabel(card,T("format."+(int)e.Format)+" · "+T("woodland"),theme.section_size,true);
                AddVenuePreview(card,compactLayout?105:150);
                MetricStrip(card,(summary.Profile.DistanceMeters/1000).ToString("0.00")+" km",L("Дистанция","Distance"),summary.Profile.LapCount.ToString(),L("Круги","Laps"),summary.EntrantCount.ToString(),L("На старте","Entrants"));
                var facts=Tile(card);
                AddPreRaceKit(facts,e,summary.Player);
                var stances=new List<string>();for(int i=0;i<summary.Profile.BoutCount;i++)stances.Add(T("stance."+(int)summary.Profile.GetBout(i).Stance));
                InfoRow(facts,L("Рубежи","Shooting"),string.Join(" → ",stances));
                InfoRow(facts,L("Старт","Start"),T("start."+(int)summary.Profile.StartType)+(summary.Profile.StartIntervalSeconds>0?F("start_interval",summary.Profile.StartIntervalSeconds):""));
                InfoRow(facts,T("equipment"),T("equipment."+summary.Skis.Id));
                var weather=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+e.VenueId+"/environment"));
                AddLabel(facts,T("weather."+weather.lighting.weather_preset)+" · "+T("snow."+summary.SnowProfileId),theme.caption_size).name="pre_race_weather";
                var raceKeys=AddLabel(actions,T("race_controls"),theme.caption_size);raceKeys.name="pre_race_controls";raceKeys.style.marginTop=16;
            }
            else if(Session.Screen==FrontEndScreen.Race)
            {
                AddLabel(actions,T(Race.AwaitingSave?"race_stopped":"paused"),theme.section_size,true);
                if(Race.AwaitingSave&&Race.Host.Runner.Field.IsComplete)AddButton(actions,"retry_save",T("retry_save"),Race.SaveResult,true);
                if(!Race.AwaitingSave)AddButton(actions,"resume",T("resume"),Race.Resume,true);
                AddButton(actions,"abandon",T("abandon"),Race.Abandon);
                AddVenueCard(card);AddLabel(card,T("abandon_note"),theme.body_size);
            }
            else if(Session.Screen==FrontEndScreen.Results)
            {
                left.style.width=new Length(23,LengthUnit.Percent);shade.style.width=new Length(100,LengthUnit.Percent);
                brand.style.fontSize=theme.section_size;RenderResults(actions,card);
            }
            else if(Session.Screen>=FrontEndScreen.Training)
            {
                left.style.width=new Length(23,LengthUnit.Percent);shade.style.width=new Length(100,LengthUnit.Percent);
                brand.style.fontSize=theme.section_size;RenderCareerPage(actions,card);
            }
            else if(Session.Screen==FrontEndScreen.Settings)
            {
                AddLabel(actions,T("settings"),theme.section_size,true);AddLabel(actions,T("language"),theme.caption_size);
                var languages=new DropdownField(new List<string>{"Русский","English"},Locale=="ru"?0:1){name="language"};StyleControl(languages);actions.Add(languages);controls.Add(languages);
                languages.RegisterValueChangedCallback(e=>ApplyPreferences(languages.index==0?"ru":"en",TextSizeIndex));
                AddLabel(actions,T("text_size"),theme.caption_size);
                var sizes=new DropdownField(new List<string>{"100%","115%","130%"},TextSizeIndex){name="text_size"};StyleControl(sizes);actions.Add(sizes);controls.Add(sizes);
                sizes.RegisterValueChangedCallback(e=>ApplyPreferences(Locale,sizes.index));AddButton(actions,"back",T("back"),Back);
                var contrast=new Toggle(T("accessibility.contrast")){name="high_contrast",value=HighContrast};StyleControl(contrast);actions.Add(contrast);controls.Add(contrast);
                contrast.RegisterValueChangedCallback(e=>ApplyAccessibility(e.newValue));
                AddLabel(card,T("settings"),theme.section_size,true);
                var controlsCard=Tile(card);AddLabel(controlsCard,L("НА ТРАССЕ","ON THE COURSE"),theme.caption_size);
                InfoRow(controlsCard,"W / S",L("Темп / тормоз","Pace / brake"));InfoRow(controlsCard,"A / D",L("Ручное руление","Manual steering"));InfoRow(controlsCard,"Shift / Ctrl",L("Атака / восстановление","Attack / recover"));InfoRow(controlsCard,"Space",L("Низкая стойка","Tuck"));InfoRow(controlsCard,"Tab",L("Последняя отсечка","Latest split"));
                var shootingCard=Tile(card);AddLabel(shootingCard,L("НА РУБЕЖЕ","ON THE RANGE"),theme.caption_size);
                InfoRow(shootingCard,L("ПКМ / ЛКМ","RMB / LMB"),L("Затвор / выстрел","Bolt / fire"));InfoRow(shootingCard,"Space / F",L("Дыхание / точный прицел","Breath / fine aim"));
                AddLabel(card,T("accessibility.symbols"),theme.caption_size);AddLabel(card,T("accessibility.assists"),theme.caption_size).name="accessibility_assists";
            }
            message=AddLabel(left,LastMessage??"",theme.caption_size);message.name="message";message.style.color=FrontEndTheme.Color(theme.danger);
            left.Query<Label>().ForEach(label=>{if(label!=message)label.style.color=Color.white;});
            var footer=AddLabel(Root,T("controls"),theme.caption_size);footer.style.marginTop=theme.spacing*2;footer.style.color=Color.white;
            footer.style.backgroundColor=FrontEndTheme.Color(theme.menu_panel);footer.style.paddingLeft=theme.spacing;footer.style.paddingTop=8;footer.style.paddingBottom=8;
            Root.schedule.Execute(()=>
            {
                if(!isActiveAndEnabled||Root.style.display.value==DisplayStyle.None)return;
                if(rememberedFocus.TryGetValue(Session.Screen,out string id))foreach(var control in controls)if(control.name==id&&IsShown(control)){control.Focus();return;}
                foreach(var control in controls)if(IsShown(control)){control.Focus();break;}
            });
        }
        private void AddVenueCard(VisualElement card)
        {AddLabel(card,"WOODLAND BASIN",theme.caption_size);AddLabel(card,T("woodland"),theme.section_size,true);AddVenuePreview(card,190);AddLabel(card,T("formats"),theme.body_size);AddCourseMap(card);AddTargets(card);}
        private void AddTargets(VisualElement card)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.marginTop=theme.spacing*3;row.style.marginBottom=theme.spacing*3;card.Add(row);
            for(int i=0;i<5;i++)
            {
                var target=new VisualElement();target.style.width=42;target.style.height=42;target.style.flexShrink=0;target.style.marginRight=theme.spacing;
                target.style.backgroundColor=i==4?accent:ink;target.style.borderTopLeftRadius=21;target.style.borderTopRightRadius=21;target.style.borderBottomLeftRadius=21;target.style.borderBottomRightRadius=21;row.Add(target);
            }
        }
        private Label AddLabel(VisualElement parent,string value,int size,bool bold=false)
        {
            var label=new Label(value){enableRichText=false};label.style.fontSize=size*Scale;label.style.whiteSpace=WhiteSpace.Normal;
            label.style.marginBottom=theme.spacing;label.style.flexShrink=0;label.style.color=ink;
            if(bold)label.style.unityFontStyleAndWeight=FontStyle.Bold;parent.Add(label);return label;
        }
        private void StyleControl(VisualElement control)
        {
            control.style.height=theme.control_height;control.style.fontSize=theme.body_size*Scale;control.style.marginBottom=6;control.style.flexShrink=0;
            control.Query<TextElement>().ForEach(element=>element.style.fontSize=theme.body_size*Scale);
            control.Query<VisualElement>(className:"unity-base-field__input").ForEach(element=>
            {
                element.style.backgroundColor=paper;element.style.color=FrontEndTheme.Color(theme.ink);
                element.style.borderTopLeftRadius=element.style.borderTopRightRadius=element.style.borderBottomLeftRadius=element.style.borderBottomRightRadius=8;
                element.style.borderLeftWidth=element.style.borderRightWidth=element.style.borderTopWidth=element.style.borderBottomWidth=0;
                element.style.paddingLeft=10;
            });
        }
        private void AddButton(VisualElement parent,string id,string label,Action action,bool primary=false,bool enabled=true)
        {
            var button=new Button(action){name=id,text=label,enableRichText=false};StyleControl(button);button.SetEnabled(enabled);
            button.style.opacity=enabled?1:.45f;button.style.transitionDuration=new List<TimeValue>{new TimeValue(0)};
            button.style.unityTextAlign=TextAnchor.MiddleLeft;button.style.paddingLeft=theme.spacing;button.style.paddingRight=theme.spacing;
            button.style.borderTopLeftRadius=8;button.style.borderTopRightRadius=8;button.style.borderBottomLeftRadius=8;button.style.borderBottomRightRadius=8;
            void Paint(bool selected)
            {
                button.style.backgroundColor=selected?focus:primary?accent:FrontEndTheme.Color(theme.menu_panel);button.style.color=selected?ink:paper;
                button.style.borderLeftWidth=selected?5:0;button.style.borderRightWidth=0;button.style.borderTopWidth=0;button.style.borderBottomWidth=0;
                button.style.borderLeftColor=accent;button.style.borderRightColor=accent;button.style.borderTopColor=accent;button.style.borderBottomColor=accent;
            }
            Paint(false);button.RegisterCallback<FocusInEvent>(e=>Paint(true));button.RegisterCallback<FocusOutEvent>(e=>Paint(false));
            button.RegisterCallback<MouseEnterEvent>(e=>Paint(true));button.RegisterCallback<MouseLeaveEvent>(e=>Paint(button==Root.panel?.focusController.focusedElement));
            parent.Add(button);controls.Add(button);
        }
        private void Create()
        {
            try { Session.CreateCareer("career_"+Guid.NewGuid().ToString("N"),athleteName,nation,unchecked((ulong)DateTime.UtcNow.Ticks),startProfileId,startingAge);LastMessage=null;Render(); }
            catch(Exception e) when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is IOException||e is UnauthorizedAccessException)
            {ShowError(e is IOException||e is UnauthorizedAccessException?"save_error":"name_error");}
        }
        private void Continue()
        {
            try {Session.ContinueCareer();LastMessage=null;Render();}
            catch(Exception e) when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is IOException||e is UnauthorizedAccessException)
            {ShowError("load_error");}
        }
        private void CareerAction(Action action)
        {
            try{action();LastMessage=null;Render();}
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){ShowError("save_error");}
        }
        private void ShowError(string key) {LastMessage=T(key);if(message!=null)message.text=LastMessage;}
        private void OnDestroy() {if(panel!=null)Destroy(panel);}
    }
}
