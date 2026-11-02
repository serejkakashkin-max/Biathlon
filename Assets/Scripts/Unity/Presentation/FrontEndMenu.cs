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
        public bool ReducedMotion {get;private set;}
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
        private IVisualElementScheduledItem backdropMotion;
        private float Scale => TextSizeIndex==0?1:TextSizeIndex==1?1.15f:1.3f;
        private string T(string key)=>text.Text(key,Locale);
        private string F(string key,params object[] values)=>string.Format(CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US"),T(key),values);
        private static string ReadCore(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private void Start()
        { if(AutoStart&&Session==null)Initialize(new FileCareerProfileStore(Path.Combine(Application.persistentDataPath,"Profiles"))); }
        public void Initialize(ICareerProfileStore profiles,RaceReplayLibrary replayLibrary=null)
        {
            if(Session!=null)throw new InvalidOperationException("Initialize front end once.");
            BiathlonAudio.Ensure(PersistPreferences).SetMenu(true);
            theme=FrontEndTheme.Load();text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            backdrop=Resources.Load<Texture2D>("Biathlon/UI/menu_background");
            Session=new FrontEndSession(ReadCore,profiles,s=>new SeededRandomSource(s));
            InitializeReliability(profiles);
            ReplayLibrary=replayLibrary??new RaceReplayLibrary(Path.Combine(Application.persistentDataPath,"Replays"));
            Replays=gameObject.AddComponent<RaceReplayPresentation>();
            InitializeSettings();
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
            // ScrollView consumes directional events when its scroller is visible.
            // Keep button navigation on the same explicit route; leave fields to native handlers.
            Root.RegisterCallback<NavigationMoveEvent>(e=>{if(e.target is Button)Navigate(e);},TrickleDown.TrickleDown);
            Root.RegisterCallback<NavigationSubmitEvent>(e=>{if(Rebinding)e.StopImmediatePropagation();},TrickleDown.TrickleDown);
            Root.RegisterCallback<GeometryChangedEvent>(e=>{bool compact=e.newRect.width<1250;if(compactLayout==compact)return;compactLayout=compact;Root.schedule.Execute(Render);});
            Root.RegisterCallback<FocusInEvent>(RememberFocus);
            Root.RegisterCallback<NavigationCancelEvent>(e=>{if(HandleRebindingCancel(e))return;if(Session.Screen!=FrontEndScreen.Home)Back();e.StopPropagation();},TrickleDown.TrickleDown);
            Render();
        }
        public void Back()
        {
            if(Rebinding){CancelRebind();return;}
            BiathlonAudio.Instance?.Ui(AudioCue.Back);
            if(Race.Loading)return;
            if(Replays.Loading)return;
            if(Replays.Active){Replays.Close();return;}
            if(Session.Screen==FrontEndScreen.Race){if(Race.Paused&&!Race.AwaitingSave)Race.Resume();return;}
            if(careerCardAthlete!=null){careerCardAthlete=null;LastMessage=null;Render();return;}
            if(Session.Screen==FrontEndScreen.Rivalries&&careerRosterArchive){careerRosterArchive=false;Render();return;}
            if(Session.Screen==FrontEndScreen.NewCareer&&creatorMode==CareerCreationMode.ExistingAthlete&&catalogReview){catalogReview=false;LastMessage=null;Render();return;}
            if(Session.Screen==FrontEndScreen.NewCareer&&creatorMode==CareerCreationMode.CreateOwn&&creatorStep>0){creatorStep=0;LastMessage=null;Render();return;}
            if(Session.Screen==FrontEndScreen.NewCareer&&creatorMode.HasValue){creatorMode=null;catalogReview=false;LastMessage=null;Render();return;}
            if(Session.Screen==FrontEndScreen.Results)Session.ReturnFromResults();else Session.Back();
            LastMessage=null;Render();
        }
        public void RefreshFrontEnd()=>Render();
        public void ShowFrontEndError(string key)=>ShowError(key);
        public void ShowRaceFailure(){SetRaceVisible(false);ShowError("race_fault");Render();}
        public void SetRaceVisible(bool visible)
        {
            BiathlonAudio.Instance?.SetMenu(!visible);
            Root.style.display=visible?DisplayStyle.None:DisplayStyle.Flex;menuCamera.enabled=!visible;
            if(visible){Root.panel?.focusController.focusedElement?.Blur();LastMessage=null;}
        }
        public void ApplyPreferences(string locale,int textSizeIndex)
        {
            if(locale!="ru"&&locale!="en"||textSizeIndex<0||textSizeIndex>2)throw new ArgumentException("Unsupported UI preference.");
            var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.Locale=locale;next.TextSize=textSizeIndex;
            if(SaveSettings(next))Render();
        }
        public void ApplyAccessibility(bool contrast)
        {var next=Settings.Clone(PlayerSettingsPreferences.Catalog);next.HighContrast=contrast;if(SaveSettings(next))Render();}
        private bool IsShown(VisualElement element)
        {if(!element.enabledInHierarchy)return false;for(var e=element;e!=null&&e!=Root;e=e.parent)if(e.resolvedStyle.display==DisplayStyle.None)return false;return true;}
        private void RememberFocus(FocusInEvent e)
        {
            for(var element=e.target as VisualElement;element!=null&&element!=Root;element=element.parent)
                if(controls.Contains(element)){rememberedFocus[Session.Screen]=element.name;element.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(element);break;}
        }
        private void Navigate(NavigationMoveEvent e)
        {
            if(Rebinding){e.StopImmediatePropagation();return;}
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
            CancelRebind(false);
            if(careerCardAthlete!=null&&Session.Screen!=FrontEndScreen.Career&&Session.Screen<FrontEndScreen.Training)careerCardAthlete=null;
            if(Session.Screen!=FrontEndScreen.NewCareer&&careerCardAthlete==null)ReleaseCreatorStudio();
            backdropMotion?.Pause();backdropMotion=null;
            Root.Clear();controls.Clear();paper=HighContrast?Color.black:FrontEndTheme.Color(theme.paper);ink=HighContrast?Color.white:FrontEndTheme.Color(theme.ink);
            FrostlineStyle.Apply(Root,ReducedMotion);Root.EnableInClassList("high-contrast",HighContrast);
            Root.style.flexGrow=1;Root.style.backgroundColor=FrontEndTheme.Color(theme.snow);Root.style.color=ink;
            if(Race.Loading){RenderRaceLoading();return;}
            bool home=Session.Screen==FrontEndScreen.Home;
            var background=new Image{image=backdrop,scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore,name="menu_backdrop"};
            background.style.position=Position.Absolute;background.style.left=0;background.style.right=0;background.style.top=0;background.style.bottom=0;Root.Add(background);
            if(home&&!ReducedMotion)
            {
                background.style.left=background.style.right=-12;background.style.top=background.style.bottom=-8;
                backdropMotion=background.schedule.Execute(()=>background.style.translate=new Translate((float)Math.Sin(Time.unscaledTimeAsDouble*.12)*6,(float)Math.Cos(Time.unscaledTimeAsDouble*.1)*3)).Every(33);
            }
            var shade=new VisualElement{pickingMode=PickingMode.Ignore};shade.style.position=Position.Absolute;shade.style.left=0;shade.style.top=0;shade.style.bottom=0;
            shade.style.width=new Length(home?44:100,LengthUnit.Percent);shade.style.backgroundColor=FrontEndTheme.Color(home?theme.home_shade:theme.menu_panel);Root.Add(shade);
            Root.style.paddingTop=theme.page_padding;Root.style.paddingBottom=theme.page_padding;
            Root.style.paddingLeft=theme.page_padding;Root.style.paddingRight=theme.page_padding;
            var body=new VisualElement{name="menu_body"};body.style.flexGrow=1;body.style.flexDirection=FlexDirection.Row;Root.Add(body);
            var left=new VisualElement{name="menu_navigation"};left.style.width=new Length(home?38:23,LengthUnit.Percent);left.style.paddingRight=theme.page_padding;body.Add(left);
            var brand=AddLabel(left,T("brand"),theme.title_size,true);brand.style.fontSize=home?theme.title_size:theme.section_size;brand.style.whiteSpace=WhiteSpace.NoWrap;
            brand.style.unityFontStyleAndWeight=FontStyle.Normal;brand.style.letterSpacing=2;
            AddLabel(left,"F R O S T L I N E",theme.caption_size).style.color=accent;
            AddLabel(left,home?T("discipline"):Session.Career==null?L("КАРЬЕРА","CAREER"):Session.Definitions.Calendar.Circuit==CareerCircuit.Junior?L("ЮНИОРСКАЯ ЛИГА","JUNIOR CIRCUIT"):Session.Definitions.Calendar.Circuit==CareerCircuit.Development?L("ПУТЬ В КУБОК МИРА","ROAD TO WORLD CUP"):L("КУБОК МИРА","WORLD CUP"),theme.caption_size);
            var actions=new ScrollView(ScrollViewMode.Vertical){name="menu_actions",verticalScrollerVisibility=ScrollerVisibility.Auto,horizontalScrollerVisibility=ScrollerVisibility.Hidden};actions.style.marginTop=theme.spacing;actions.style.flexShrink=1;actions.style.minHeight=0;left.Add(actions);
            var card=new ScrollView(ScrollViewMode.Vertical){name="page_content",verticalScrollerVisibility=ScrollerVisibility.Auto,horizontalScrollerVisibility=ScrollerVisibility.Hidden};card.style.flexGrow=1;card.style.flexBasis=0;card.style.paddingTop=theme.page_padding;
            card.style.paddingBottom=theme.page_padding;card.style.paddingLeft=theme.page_padding;card.style.paddingRight=theme.page_padding;
            card.style.backgroundColor=paper;card.style.minWidth=0;card.contentContainer.style.width=Length.Percent(100);
            card.style.borderTopLeftRadius=card.style.borderTopRightRadius=card.style.borderBottomLeftRadius=card.style.borderBottomRightRadius=2;body.Add(card);
            if(Session.Screen==FrontEndScreen.Home)
            {
                shade.style.width=Length.Percent(38);brand.style.fontSize=46;
                var title=AddLabel(actions,L("ТВОЙ СЕЗОН.\nТВОЙ СЛЕД.","YOUR SEASON.\nYOUR TRACK."),52,true);title.name="home_statement";title.style.marginTop=18;title.style.marginBottom=24;
                if(Session.CanContinue)AddButton(actions,"continue",T("continue"),Continue,true);
                AddButton(actions,"new_career",T("new_career"),()=>{Session.OpenNewCareer();LastMessage=null;Render();},!Session.CanContinue);
                if(!Session.CanContinue)
                {
                    AddButton(actions,"continue",T("continue"),Continue,enabled:false);
                    AddLabel(actions,L("Создайте спортсмена, чтобы начать свой сезон.","Create an athlete to begin your season."),theme.caption_size);
                }
                AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});
                AddButton(actions,"quit",T("quit"),()=>Application.Quit());
                card.style.backgroundColor=Color.clear;card.style.justifyContent=Justify.FlexEnd;card.pickingMode=PickingMode.Ignore;
                var caption=AddLabel(left,T("home_note"),theme.caption_size);caption.style.marginTop=theme.spacing*2;
                var signature=Tile(Root,"home_venue_signature",true);signature.style.position=Position.Absolute;signature.style.left=Length.Percent(43);signature.style.right=32;signature.style.bottom=100;signature.style.borderLeftWidth=4;signature.style.borderLeftColor=FrostlineStyle.Gold;signature.style.backgroundColor=new Color(.04f,.10f,.16f,.78f);
                AddLabel(signature,L("ЛЫЖИ / СТРЕЛЬБА / КАРЬЕРА","SKI / SHOOT / CAREER"),theme.caption_size).style.color=FrostlineStyle.Gold;
                AddLabel(signature,"WOODLAND BASIN",36,true);AddLabel(signature,T("formats"),theme.caption_size);
            }
            else if(Session.Screen==FrontEndScreen.NewCareer)
            {
                RenderCareerCreation(body,left,actions,card);
            }
            else if(careerCardAthlete!=null)
            {
                RenderCareerAthleteCard(body,left,actions,card);
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
                var forecast=new Biathlon.AI.CareerRaceProvider(ReadCore,Session.Definitions,ProductionVenueHost.ReadResource)
                    .Forecast(CareerSave.FromJson(Session.PreparedWeek.RaceSnapshotJson),e);
                var snowConditions=RaceSnowConditions.FromJson(ReadCore("race_snow_neutral_v1.json"));
                var summary=new PreRaceSummary(Session.PreparedWeek,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+e.VenueId+"/"+format+"_profile")),
                    forecast==null?snowConditions:forecast.SnowConditions(snowConditions),Session.EquipmentCatalog);
                AddLabel(actions,T("pre_race"),theme.section_size,true);AddLabel(actions,T("format."+(int)e.Format),theme.section_size,true);
                AddLabel(actions,summary.Player.Name,theme.body_size);
                AddLabel(actions,F("athlete_state",summary.Player.Form/100,summary.Player.Fatigue/100),theme.body_size);
                AddButton(actions,"start_race",T(Race.Loading?"loading":"start_race"),()=>Race.Launch(this),true,!Race.Loading);
                AddButton(actions,"back",T("back"),Back,enabled:!Race.Loading);
                AddLabel(card,"WOODLAND BASIN",theme.caption_size).style.color=accent;
                AddLabel(card,T("format."+(int)e.Format)+" · "+T("woodland"),theme.section_size,true);
                AddVenuePreview(card,compactLayout?105:150);
                MetricStrip(card,(RaceDistanceBinding.Create(summary.Profile).TotalMeters/1000).ToString("0.##",CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US"))+L(" км"," km"),L("Дистанция","Distance"),summary.Profile.LapCount.ToString(),L("Круги","Laps"),summary.EntrantCount.ToString(),L("На старте","Entrants"));
                var facts=Tile(card);
                AddPreRaceKit(facts,e,summary.Player);
                var stances=new List<string>();for(int i=0;i<summary.Profile.BoutCount;i++)stances.Add(T("stance."+(int)summary.Profile.GetBout(i).Stance));
                InfoRow(facts,L("Рубежи","Shooting"),string.Join(" → ",stances));
                InfoRow(facts,L("Старт","Start"),T("start."+(int)summary.Profile.StartType)+(summary.Profile.StartIntervalSeconds>0?F("start_interval",summary.Profile.StartIntervalSeconds):""));
                InfoRow(facts,T("equipment"),EquipmentName(summary.Skis.Id));
                var weather=CareerWeatherPresentation.Bind(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+e.VenueId+"/environment")),forecast);
                AddLabel(facts,(weather.lighting.preset_id=="evening"||weather.lighting.preset_id=="dawn"||weather.lighting.preset_id=="late_day"?T("time."+weather.lighting.preset_id)+" · ":"")+T("weather."+weather.lighting.weather_preset)+" · "+T("snow."+summary.SnowProfileId)+
                    " · "+weather.lighting.temperature_c.ToString("0.#",CultureInfo.InvariantCulture)+"°C"+
                    " · "+L("ветер ","wind ")+weather.lighting.visual_wind.magnitude.ToString("0.#",CultureInfo.InvariantCulture)+L(" м/с"," m/s"),theme.caption_size).name="pre_race_weather";
                RenderRaceWeatherPlan(card);
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
                RenderSettings(actions,card);
            }
            message=AddLabel(left,LastMessage??"",theme.caption_size);message.name="message";message.style.color=FrontEndTheme.Color(theme.danger);
            // Recovery/errors must remain above the scrolling navigation on compact screens.
            left.Insert(3,message);
            left.Query<Label>().ForEach(label=>{if(label!=message)label.style.color=Color.white;});
            if(HighContrast)
            {
                Root.Query<VisualElement>().ForEach(v=>{if(v.ClassListContains("fl-surface")||v.ClassListContains("fl-hero"))v.style.backgroundColor=Color.black;});
                Root.Query<Label>().ForEach(v=>{if(v!=message)v.style.color=Color.white;});
            }
            var help=AddLabel(Root,L("↑ ↓  Выбор    ·    Enter / A  Открыть    ·    Esc / B  Назад","↑ ↓  Navigate    ·    Enter / A  Select    ·    Esc / B  Back"),theme.caption_size);
            help.name="menu_control_hint";help.style.marginTop=10;help.style.marginBottom=0;help.style.color=HighContrast?Color.white:FrostlineStyle.Muted;
            body.AddToClassList("fl-screen");
            if(!ReducedMotion){body.style.opacity=0;body.schedule.Execute(()=>body.style.opacity=1).StartingIn(16);}
            Root.schedule.Execute(()=>
            {
                if(!isActiveAndEnabled||Root.style.display.value==DisplayStyle.None)return;
                if(rememberedFocus.TryGetValue(Session.Screen,out string id))foreach(var control in controls)if(control.name==id&&IsShown(control)){control.Focus();return;}
                foreach(var control in controls)if(IsShown(control)){control.Focus();break;}
            });
        }
        private void AddVenueCard(VisualElement card)
        {
            AddLabel(card,"WOODLAND BASIN",theme.caption_size).style.color=FrostlineStyle.Gold;
            AddLabel(card,T("woodland"),theme.section_size,true);AddVenuePreview(card,160);
            AddLabel(card,T("formats"),theme.body_size);AddCourseMap(card);
            card.Q("venue_map").style.height=120;
        }
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
            FrostlineStyle.Text(label,size>=theme.section_size);if(bold&&size<theme.section_size)label.style.unityFontStyleAndWeight=FontStyle.Bold;parent.Add(label);return label;
        }
        private void StyleControl(VisualElement control)
        {
            control.style.height=theme.control_height;control.style.fontSize=theme.body_size*Scale;control.style.marginBottom=4;control.style.flexShrink=0;
            control.AddToClassList("fl-control");FrostlineStyle.Text(control);
            // Default-theme editable fields supply a FontDefinition of their own.
            // Override that definition together with the Font so TextCore does not
            // combine a default field atlas/material with the authored face.
            control.style.unityFontDefinition=FontDefinition.FromFont(FrostlineStyle.BodyFont);
            control.Query<TextElement>().ForEach(element=>
            {
                element.style.fontSize=theme.body_size*Scale;element.style.color=ink;
                // TextField and IntegerField contain a separate editable TextElement.
                // Its default-theme fill must not cover the authored input surface.
                element.style.backgroundImage=StyleKeyword.None;element.style.backgroundColor=Color.clear;
                FrostlineStyle.Text(element);
                element.style.unityFontDefinition=FontDefinition.FromFont(FrostlineStyle.BodyFont);
            });
            control.Query<VisualElement>().ForEach(element=>
            {
                bool input=element.ClassListContains("unity-base-field__input")||element.ClassListContains("unity-base-popup-field__input")||
                    element.ClassListContains("unity-text-field__input")||element.ClassListContains("unity-integer-field__input")||element.name=="unity-text-input";
                if(!input)return;
                element.style.backgroundImage=StyleKeyword.None;
                element.style.backgroundColor=HighContrast?Color.black:FrostlineStyle.Surface;element.style.color=ink;
                element.style.borderTopLeftRadius=element.style.borderTopRightRadius=element.style.borderBottomLeftRadius=element.style.borderBottomRightRadius=2;
                element.style.borderLeftWidth=element.style.borderRightWidth=element.style.borderTopWidth=element.style.borderBottomWidth=1;
                element.style.borderLeftColor=element.style.borderRightColor=element.style.borderTopColor=element.style.borderBottomColor=FrostlineStyle.Accent;
                element.style.paddingLeft=10;
            });
            if(control is Toggle)
            {
                control.style.height=StyleKeyword.Auto;control.style.minHeight=theme.control_height;
                var label=control.Q<Label>(className:"unity-base-field__label");
                if(label!=null){label.style.minWidth=0;label.style.flexBasis=0;label.style.flexGrow=1;label.style.flexShrink=1;label.style.whiteSpace=WhiteSpace.Normal;}
                var input=control.Q(className:"unity-base-field__input");
                if(input!=null){input.style.flexGrow=0;input.style.flexShrink=0;input.style.width=28;input.style.minWidth=28;input.style.paddingLeft=0;}
            }
        }
        private void AddButton(VisualElement parent,string id,string label,Action action,bool primary=false,bool enabled=true)
        {
            var button=new Button(()=>{BiathlonAudio.Instance?.Ui(AudioCue.Confirm);action();}){name=id,text=label,enableRichText=false};StyleControl(button);button.SetEnabled(enabled);
            button.style.opacity=enabled?1:.45f;button.AddToClassList("fl-button");
            button.style.unityTextAlign=TextAnchor.MiddleLeft;button.style.paddingLeft=theme.spacing;button.style.paddingRight=theme.spacing;
            button.style.borderTopLeftRadius=2;button.style.borderTopRightRadius=2;button.style.borderBottomLeftRadius=2;button.style.borderBottomRightRadius=2;
            void Paint(bool selected)
            {
                button.style.backgroundColor=selected?focus:primary?accent:Color.clear;button.style.color=primary&&!selected?FrostlineStyle.Deep:ink;
                button.style.borderLeftWidth=selected?5:0;button.style.borderRightWidth=0;button.style.borderTopWidth=0;button.style.borderBottomWidth=0;
                button.style.borderLeftColor=accent;button.style.borderRightColor=accent;button.style.borderTopColor=accent;button.style.borderBottomColor=accent;
                if(!selected)button.style.borderLeftColor=Color.clear;
            }
            Paint(false);button.RegisterCallback<FocusInEvent>(e=>{Paint(true);BiathlonAudio.Instance?.Ui(AudioCue.Focus);});button.RegisterCallback<FocusOutEvent>(e=>Paint(false));
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
            ContinueReliably();
        }
        private void CareerAction(Action action)
        {
            try{action();LastMessage=null;Render();}
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){ShowError("save_error");}
        }
        private void ShowError(string key)
        {
            if(key=="save_error"||key=="result_save_error")RecordIssue(DiagnosticIssue.SaveWriteFailed);
            else if(key=="load_error")RecordIssue(DiagnosticIssue.SaveReadFailed);
            else if(key=="race_load_error"||key=="event_unavailable")RecordIssue(DiagnosticIssue.MissingContent);
            else if(key=="race_fault")RecordIssue(DiagnosticIssue.SimulationFault);
            LastMessage=T(key);if(message!=null)message.text=LastMessage;
        }
        private void OnDestroy() {CancelRebind(false);ReleaseReliability();ReleaseCreatorStudio();if(panel!=null)Destroy(panel);}
    }
}
