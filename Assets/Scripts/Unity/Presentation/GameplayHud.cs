using System;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class GameplayHudSettings
    {
        public int schema_version,edge,panel_padding,left_width,right_width,body_size,caption_size,number_size,target_size,spacing;
        public string id;
        public float refresh_seconds;
        public static GameplayHudSettings Load()
        {
            var s=JsonUtility.FromJson<GameplayHudSettings>(ProductionVenueHost.ReadResource("Biathlon/UI/gameplay_hud_v1"));
            if(s.schema_version!=1||string.IsNullOrWhiteSpace(s.id)||!float.IsFinite(s.refresh_seconds)||s.refresh_seconds<.05f||s.refresh_seconds>1||
                s.edge<0||s.panel_padding<0||s.left_width<250||s.right_width<250||s.body_size<16||s.caption_size<14||s.number_size<s.body_size||s.target_size<24||s.spacing<0)
                throw new ArgumentException("Invalid gameplay HUD settings.");
            return s;
        }
    }
    /// <summary>Retained, non-interactive HUD; refreshes accepted observations at a bounded UI cadence.</summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        public VisualElement Root=>document?.rootVisualElement;
        public int RefreshCount {get;private set;}
        public GameplayHudModel Model {get;private set;}
        public string Locale {get;private set;}
        public int TextSizeIndex {get;private set;}
        private RacePresentationFeed feed;
        private string athleteId,displayName="";
        private int selectedIndex=-1;
        private RacePresentationSnapshot rendered;
        private FrontEndTheme theme;
        private GameplayHudSettings settings;
        private UiTextCatalog text;
        private PanelSettings panel;
        private UIDocument document;
        private GameObject documentObject;
        private ProductionVenueHost host;
        private SkiDebugHud debug;
        private Label clock,lap,timing,phase,history,speed,heart,reserve,fatigue,range,weapon,breath,wind,shots,help;
        private VisualElement reserveFill,fatigueFill,breathFill,shootingPanel,motionPanel,timingPanel,conditionPanel;
        private readonly Label[] targets=new Label[5];
        private double nextRefresh;
        private Color foreground,muted,accent;
        private float Scale=>TextSizeIndex==0?1:TextSizeIndex==1?1.15f:1.3f;
        private string T(string key)=>text.Text("hud."+key,Locale);

        public void Initialize(RacePresentationFeed source,string selected,ProductionVenueHost owner=null)
        {
            if(feed!=null)throw new InvalidOperationException("Initialize HUD once.");
            if(source==null||string.IsNullOrWhiteSpace(selected))throw new ArgumentException("Feed and stable athlete ID required.");
            feed=source;athleteId=selected;host=owner;debug=owner?.GetComponent<SkiDebugHud>();
            theme=FrontEndTheme.Load();settings=GameplayHudSettings.Load();
            text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            foreground=FrontEndTheme.Color(theme.paper);muted=FrontEndTheme.Color(theme.focus);accent=FrontEndTheme.Color(theme.accent);
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.name=settings.id;
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("Biathlon/UI/front_end");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(theme.reference_width,theme.reference_height);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            // A separate owned UI root must not inherit the hidden front-end UIDocument.
            documentObject=new GameObject("Gameplay interface");SceneManager.MoveGameObjectToScene(documentObject,gameObject.scene);
            document=documentObject.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=20;
            string locale=PlayerPrefs.GetString("biathlon.ui.locale","ru");
            SetPreferences(locale=="en"?"en":"ru",Mathf.Clamp(PlayerPrefs.GetInt("biathlon.ui.text_size",0),0,2));
        }
        public void SetPreferences(string locale,int textSizeIndex,string athleteName="")
        {
            if(locale!="ru"&&locale!="en"||textSizeIndex<0||textSizeIndex>2)throw new ArgumentException("Invalid HUD preferences.");
            Locale=locale;TextSizeIndex=textSizeIndex;displayName=athleteName??"";Build();rendered=null;Refresh();
        }
        private void Build()
        {
            Root.Clear();Root.style.flexGrow=1;
            timingPanel=Card("timing_panel",true,true,settings.left_width);
            if(displayName.Length>0)
            {
                var name=Text(timingPanel,"athlete_name",displayName,settings.caption_size);
                name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;
            }
            clock=Text(timingPanel,"race_clock","",settings.number_size,true);
            lap=Text(timingPanel,"lap","",settings.body_size);
            timing=Text(timingPanel,"timing","",settings.caption_size);
            phase=Text(timingPanel,"phase","",settings.caption_size);
            history=Text(timingPanel,"history","",settings.caption_size);
            conditionPanel=Card("condition_panel",false,true,settings.right_width);
            heart=Text(conditionPanel,"heart_rate","",settings.number_size,true);
            reserve=Text(conditionPanel,"reserve","",settings.caption_size);reserveFill=Bar(conditionPanel,"reserve_bar",foreground);
            fatigue=Text(conditionPanel,"fatigue","",settings.caption_size);fatigueFill=Bar(conditionPanel,"fatigue_bar",muted);
            motionPanel=Card("motion_panel",true,false,settings.left_width);
            speed=Text(motionPanel,"speed","",settings.number_size,true);help=Text(motionPanel,"ski_help",T("ski_help"),settings.caption_size);
            shootingPanel=Card("shooting_panel",true,false,settings.left_width);
            range=Text(shootingPanel,"range","",settings.body_size,true);
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.marginBottom=settings.spacing;shootingPanel.Add(row);
            for(int i=0;i<targets.Length;i++)
            {
                var target=Text(row,"target_"+i,"",settings.caption_size,true);targets[i]=target;
                target.style.width=settings.target_size;target.style.height=settings.target_size;target.style.marginRight=settings.spacing;
                target.style.unityTextAlign=TextAnchor.MiddleCenter;target.style.borderTopLeftRadius=target.style.borderTopRightRadius=target.style.borderBottomLeftRadius=target.style.borderBottomRightRadius=settings.target_size/2;
                target.style.borderLeftWidth=target.style.borderRightWidth=target.style.borderTopWidth=target.style.borderBottomWidth=2;
                target.style.borderLeftColor=target.style.borderRightColor=target.style.borderTopColor=target.style.borderBottomColor=foreground;
            }
            shots=Text(shootingPanel,"shots","",settings.caption_size);
            weapon=Text(shootingPanel,"weapon","",settings.body_size,true);
            breath=Text(shootingPanel,"breath","",settings.caption_size);
            Text(shootingPanel,"breath_debt_label",T("breath_debt"),settings.caption_size);breathFill=Bar(shootingPanel,"breath_bar",muted);
            wind=Text(shootingPanel,"wind","",settings.caption_size);
            Text(shootingPanel,"shooting_help",T("shooting_help"),settings.caption_size);
            // Every element ignores picking: aiming and UI navigation remain owned by their existing input adapters.
            Root.Query<VisualElement>().ForEach(v=>{v.pickingMode=PickingMode.Ignore;v.focusable=false;});Root.pickingMode=PickingMode.Ignore;
        }
        private VisualElement Card(string name,bool left,bool top,int width)
        {
            var card=new VisualElement{name=name};card.style.position=Position.Absolute;card.style.width=width;
            card.style.maxWidth=new Length(40,LengthUnit.Percent);
            if(left)card.style.left=settings.edge;else card.style.right=settings.edge;
            if(top)card.style.top=settings.edge;else card.style.bottom=settings.edge;
            card.style.backgroundColor=FrontEndTheme.Color(theme.menu_panel);
            card.style.paddingTop=card.style.paddingBottom=card.style.paddingLeft=card.style.paddingRight=settings.panel_padding;
            card.style.borderLeftWidth=3;card.style.borderLeftColor=accent;Root.Add(card);return card;
        }
        private Label Text(VisualElement parent,string name,string value,int size,bool bold=false)
        {
            var label=new Label(value){name=name,enableRichText=false};label.style.color=foreground;label.style.fontSize=size*Scale;
            label.style.whiteSpace=WhiteSpace.Normal;label.style.marginBottom=settings.spacing;label.style.flexShrink=0;
            if(bold)label.style.unityFontStyleAndWeight=FontStyle.Bold;parent.Add(label);return label;
        }
        private VisualElement Bar(VisualElement parent,string name,Color color)
        {
            var track=new VisualElement();track.style.height=5;track.style.backgroundColor=accent;track.style.marginBottom=settings.spacing;parent.Add(track);
            var fill=new VisualElement{name=name};fill.style.height=5;fill.style.backgroundColor=color;track.Add(fill);return fill;
        }
        public void Refresh()
        {
            var snapshot=feed?.Current;
            if(snapshot==null){rendered=null;selectedIndex=-1;if(Root!=null)Root.style.display=DisplayStyle.None;return;}
            if(ReferenceEquals(rendered,snapshot))return;
            rendered=snapshot;
            if(selectedIndex<0||selectedIndex>=snapshot.Count||snapshot.Get(selectedIndex).AthleteId!=athleteId)
            {selectedIndex=-1;for(int i=0;i<snapshot.Count;i++)if(snapshot.Get(i).AthleteId==athleteId){selectedIndex=i;break;}}
            if(selectedIndex<0){Root.style.display=DisplayStyle.None;return;}
            Root.style.display=DisplayStyle.Flex;var a=snapshot.Get(selectedIndex);Model=new GameplayHudModel(a,text,Locale);RefreshCount++;
            clock.text=Model.Clock;lap.text=Model.Lap;timing.text=Model.Timing;phase.text=Model.Phase;history.text=Model.ShootingHistory;
            if(host?.LastRangeAssistance.Active==true)phase.text=T(host.LastRangeAssistance.Mode==PlayerRangeAssistMode.FinalApproach?"assist_in":"assist_out");
            speed.text=Model.Speed;heart.text=Model.HeartRate;reserve.text=Model.Reserve;fatigue.text=Model.Fatigue;
            reserveFill.style.width=Length.Percent(Model.ReservePercent);fatigueFill.style.width=Length.Percent(Model.FatiguePercent);breathFill.style.width=Length.Percent(Model.BreathDebtPercent);
            shootingPanel.style.display=Model.Shooting?DisplayStyle.Flex:DisplayStyle.None;motionPanel.style.display=Model.Shooting?DisplayStyle.None:DisplayStyle.Flex;
            // Review controls remain usable in diagnostic scenes; career races have no review panel.
            float bottom=host?.Review!=null?settings.edge+140:settings.edge;shootingPanel.style.bottom=motionPanel.style.bottom=bottom;
            range.text=Model.Range;weapon.text=Model.Weapon;breath.text=Model.Breath;wind.text=Model.Wind;shots.text=Model.Shots;
            for(int i=0;i<targets.Length;i++)
            {
                bool closed=(Model.ClosedMask&(1<<i))!=0;targets[i].text=closed?"+":(i+1).ToString();
                targets[i].style.backgroundColor=closed?foreground:Color.clear;targets[i].style.color=closed?FrontEndTheme.Color(theme.ink):foreground;
            }
        }
        private void LateUpdate()
        {
            if(feed==null)return;
            if(debug!=null&&Keyboard.current?.f3Key.wasPressedThisFrame==true)
            {debug.enabled=!debug.enabled;timingPanel.style.visibility=debug.enabled?Visibility.Hidden:Visibility.Visible;}
            if(Time.unscaledTimeAsDouble<nextRefresh)return;
            nextRefresh=Time.unscaledTimeAsDouble+settings.refresh_seconds;Refresh();
        }
        private void OnEnable()
        {
            if(documentObject==null)return;
            documentObject.SetActive(true);Build();rendered=null;Refresh();
        }
        private void OnDisable(){if(documentObject!=null)documentObject.SetActive(false);}
        private void OnDestroy()
        {if(documentObject!=null){documentObject.SetActive(false);Destroy(documentObject);}if(panel!=null)Destroy(panel);}
    }
}
