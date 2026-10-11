using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Retained menu UI. Rendering/navigation never advances career or race authority.</summary>
    public sealed class FrontEndMenu : MonoBehaviour
    {
        public bool AutoStart=true, PersistPreferences=true;
        public FrontEndSession Session { get; private set; }
        public VisualElement Root => document?.rootVisualElement;
        public string Locale { get; private set; }
        public int TextSizeIndex { get; private set; }
        public string LastMessage { get; private set; }
        private UIDocument document;
        private PanelSettings panel;
        private FrontEndTheme theme;
        private UiTextCatalog text;
        private readonly List<VisualElement> controls=new List<VisualElement>();
        private readonly Dictionary<FrontEndScreen,string> rememberedFocus=new Dictionary<FrontEndScreen,string>();
        private string athleteName="",nation;
        private Label message;
        private Color ink,paper,accent,focus;
        private float Scale => TextSizeIndex==0?1:TextSizeIndex==1?1.15f:1.3f;
        private string T(string key)=>text.Text(key,Locale);
        private string F(string key,params object[] values)=>string.Format(CultureInfo.GetCultureInfo(Locale=="ru"?"ru-RU":"en-US"),T(key),values);
        private static string ReadCore(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private void Start()
        { if(AutoStart&&Session==null)Initialize(new FileCareerProfileStore(Path.Combine(Application.persistentDataPath,"Profiles"))); }
        public void Initialize(ICareerProfileStore profiles)
        {
            if(Session!=null)throw new InvalidOperationException("Initialize front end once.");
            theme=FrontEndTheme.Load();text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            Session=new FrontEndSession(ReadCore,profiles,s=>new SeededRandomSource(s));
            Locale=PersistPreferences?PlayerPrefs.GetString("biathlon.ui.locale","ru"):"ru";
            if(Locale!="ru"&&Locale!="en")Locale="ru";
            TextSizeIndex=PersistPreferences?Mathf.Clamp(PlayerPrefs.GetInt("biathlon.ui.text_size",0),0,2):0;
            ink=FrontEndTheme.Color(theme.ink);paper=FrontEndTheme.Color(theme.paper);accent=FrontEndTheme.Color(theme.accent);focus=FrontEndTheme.Color(theme.focus);
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.name=theme.id;
            gameObject.AddComponent<PanelInputConfiguration>().panelInputRedirection=PanelInputConfiguration.PanelInputRedirection.Never;
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("Biathlon/UI/front_end");
            if(panel.themeStyleSheet==null)throw new FileNotFoundException("Missing front-end theme style sheet.");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(theme.reference_width,theme.reference_height);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            if(Camera.main==null)
            {
                var camera=new GameObject("Front end camera",typeof(Camera));camera.tag="MainCamera";camera.transform.SetParent(transform,false);
                var view=camera.GetComponent<Camera>();view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=FrontEndTheme.Color(theme.snow);view.orthographic=true;
            }
            document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=100;
            Root.RegisterCallback<NavigationMoveEvent>(Navigate);
            Root.RegisterCallback<FocusInEvent>(RememberFocus);
            Root.RegisterCallback<NavigationCancelEvent>(e=>{if(Session.Screen!=FrontEndScreen.Home)Back();e.StopPropagation();},TrickleDown.TrickleDown);
            Render();
        }
        public void Back() { Session.Back();LastMessage=null;Render(); }
        public void ApplyPreferences(string locale,int textSizeIndex)
        {
            if(locale!="ru"&&locale!="en"||textSizeIndex<0||textSizeIndex>2)throw new ArgumentException("Unsupported UI preference.");
            Locale=locale;TextSizeIndex=textSizeIndex;
            if(PersistPreferences){PlayerPrefs.SetString("biathlon.ui.locale",Locale);PlayerPrefs.SetInt("biathlon.ui.text_size",TextSizeIndex);PlayerPrefs.Save();}
            Render();
        }
        private void RememberFocus(FocusInEvent e)
        {
            for(var element=e.target as VisualElement;element!=null&&element!=Root;element=element.parent)
                if(controls.Contains(element)){rememberedFocus[Session.Screen]=element.name;break;}
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
                if(candidate.enabledInHierarchy){candidate.Focus();Root.panel.focusController.IgnoreEvent(e);e.StopPropagation();break;}
            }
        }
        private void Render()
        {
            Root.Clear();controls.Clear();Root.style.flexGrow=1;Root.style.backgroundColor=FrontEndTheme.Color(theme.snow);Root.style.color=ink;
            Root.style.paddingTop=theme.page_padding;Root.style.paddingBottom=theme.page_padding;
            Root.style.paddingLeft=theme.page_padding;Root.style.paddingRight=theme.page_padding;
            var body=new VisualElement();body.style.flexGrow=1;body.style.flexDirection=FlexDirection.Row;Root.Add(body);
            var left=new VisualElement();left.style.width=new Length(44,LengthUnit.Percent);left.style.paddingRight=theme.page_padding;body.Add(left);
            var brand=AddLabel(left,T("brand"),theme.title_size,true);brand.style.fontSize=theme.title_size;brand.style.whiteSpace=WhiteSpace.NoWrap;
            AddLabel(left,T("discipline"),theme.caption_size);
            var actions=new VisualElement();actions.style.marginTop=theme.spacing*2;left.Add(actions);
            var card=new VisualElement();card.style.flexGrow=1;card.style.flexBasis=0;card.style.paddingTop=theme.page_padding;
            card.style.paddingBottom=theme.page_padding;card.style.paddingLeft=theme.page_padding;card.style.paddingRight=theme.page_padding;
            card.style.backgroundColor=paper;body.Add(card);
            if(Session.Screen==FrontEndScreen.Home)
            {
                AddButton(actions,"new_career",T("new_career"),()=>{Session.OpenNewCareer();LastMessage=null;Render();},true);
                AddButton(actions,"continue",T("continue"),Continue,enabled:Session.CanContinue);
                AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});
                AddButton(actions,"quit",T("quit"),()=>Application.Quit());
                AddVenueCard(card);AddLabel(card,T("home_note"),theme.body_size);
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
                AddButton(actions,"create",T("create"),Create,true);AddButton(actions,"back",T("back"),Back);
                AddVenueCard(card);AddLabel(card,T("profile_note"),theme.body_size);
            }
            else if(Session.Screen==FrontEndScreen.Career)
            {
                var saved=Session.Career;var player=Session.Player;
                AddLabel(actions,T("career"),theme.section_size,true);AddLabel(actions,player.Name,theme.section_size,true);
                AddLabel(actions,T("nation."+player.Nation),theme.body_size);AddLabel(actions,F("season_week",saved.Season,saved.Week),theme.body_size);
                AddLabel(actions,F("athlete_state",player.Form/100,player.Fatigue/100),theme.body_size);
                AddButton(actions,"settings",T("settings"),()=>{Session.OpenSettings();Render();});AddButton(actions,"back",T("back"),Back);
                AddLabel(card,T("next_event"),theme.caption_size);var upcoming=Session.NextEvent();
                if(upcoming==null)AddLabel(card,T("no_event"),theme.section_size,true);
                else {AddLabel(card,T(upcoming.VenueId=="woodland_basin_v1"?"woodland":"legacy_venue"),theme.section_size,true);AddLabel(card,F("event_week",upcoming.Week,T("format."+(int)upcoming.Format)),theme.body_size);}
                AddTargets(card);AddLabel(card,F("endurance",player.Stat(CareerStat.Endurance)/CareerUnits.StatScale),theme.body_size);
                AddLabel(card,F("technique",player.Stat(CareerStat.SkiTechnique)/CareerUnits.StatScale),theme.body_size);
            }
            else
            {
                AddLabel(actions,T("settings"),theme.section_size,true);AddLabel(actions,T("language"),theme.caption_size);
                var languages=new DropdownField(new List<string>{"Русский","English"},Locale=="ru"?0:1){name="language"};StyleControl(languages);actions.Add(languages);controls.Add(languages);
                languages.RegisterValueChangedCallback(e=>ApplyPreferences(languages.index==0?"ru":"en",TextSizeIndex));
                AddLabel(actions,T("text_size"),theme.caption_size);
                var sizes=new DropdownField(new List<string>{"100%","115%","130%"},TextSizeIndex){name="text_size"};StyleControl(sizes);actions.Add(sizes);controls.Add(sizes);
                sizes.RegisterValueChangedCallback(e=>ApplyPreferences(Locale,sizes.index));AddButton(actions,"back",T("back"),Back);
                AddVenueCard(card);AddLabel(card,T("settings_note"),theme.body_size);
            }
            message=AddLabel(left,LastMessage??"",theme.caption_size);message.name="message";message.style.color=FrontEndTheme.Color(theme.danger);
            var footer=AddLabel(Root,T("controls"),theme.caption_size);footer.style.marginTop=theme.spacing*2;footer.style.color=FrontEndTheme.Color(theme.muted);
            Root.schedule.Execute(()=>
            {
                if(!isActiveAndEnabled)return;
                if(rememberedFocus.TryGetValue(Session.Screen,out string id))foreach(var control in controls)if(control.name==id&&control.enabledInHierarchy){control.Focus();return;}
                foreach(var control in controls)if(control.enabledInHierarchy){control.Focus();break;}
            });
        }
        private void AddVenueCard(VisualElement card)
        {AddLabel(card,"WOODLAND BASIN",theme.caption_size);AddLabel(card,T("woodland"),theme.section_size,true);AddLabel(card,T("formats"),theme.body_size);AddTargets(card);}
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
            control.style.height=theme.control_height;control.style.fontSize=theme.body_size*Scale;control.style.marginBottom=theme.spacing;control.style.flexShrink=0;
            control.Query<TextElement>().ForEach(element=>element.style.fontSize=theme.body_size*Scale);
        }
        private void AddButton(VisualElement parent,string id,string label,Action action,bool primary=false,bool enabled=true)
        {
            var button=new Button(action){name=id,text=label,enableRichText=false};StyleControl(button);button.SetEnabled(enabled);
            button.style.opacity=enabled?1:.45f;button.style.transitionDuration=new List<TimeValue>{new TimeValue(0)};
            button.style.unityTextAlign=TextAnchor.MiddleLeft;button.style.paddingLeft=theme.spacing;button.style.paddingRight=theme.spacing;
            button.style.borderTopLeftRadius=4;button.style.borderTopRightRadius=4;button.style.borderBottomLeftRadius=4;button.style.borderBottomRightRadius=4;
            void Paint(bool selected)
            {
                button.style.backgroundColor=selected?focus:primary?accent:paper;button.style.color=selected?ink:primary?paper:ink;
                button.style.borderLeftWidth=selected?5:1;button.style.borderRightWidth=1;button.style.borderTopWidth=1;button.style.borderBottomWidth=1;
                button.style.borderLeftColor=accent;button.style.borderRightColor=accent;button.style.borderTopColor=accent;button.style.borderBottomColor=accent;
            }
            Paint(false);button.RegisterCallback<FocusInEvent>(e=>Paint(true));button.RegisterCallback<FocusOutEvent>(e=>Paint(false));
            button.RegisterCallback<MouseEnterEvent>(e=>Paint(true));button.RegisterCallback<MouseLeaveEvent>(e=>Paint(button==Root.panel?.focusController.focusedElement));
            parent.Add(button);controls.Add(button);
        }
        private void Create()
        {
            try { Session.CreateCareer("career_"+Guid.NewGuid().ToString("N"),athleteName,nation,unchecked((ulong)DateTime.UtcNow.Ticks));LastMessage=null;Render(); }
            catch(Exception e) when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is IOException||e is UnauthorizedAccessException)
            {ShowError(e is IOException||e is UnauthorizedAccessException?"save_error":"name_error");}
        }
        private void Continue()
        {
            try {Session.ContinueCareer();LastMessage=null;Render();}
            catch(Exception e) when(e is ArgumentException||e is System.Runtime.Serialization.SerializationException||e is IOException||e is UnauthorizedAccessException)
            {ShowError("load_error");}
        }
        private void ShowError(string key) {LastMessage=T(key);message.text=LastMessage;}
        private void OnDestroy() {if(panel!=null)Destroy(panel);}
    }
}
