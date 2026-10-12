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
        public string background,accent,muted,warning;
        public int corner_radius,speed_width;
        public string id;
        public float refresh_seconds;
        public static GameplayHudSettings Load()
        {
            var s=JsonUtility.FromJson<GameplayHudSettings>(ProductionVenueHost.ReadResource("Biathlon/UI/gameplay_hud_v1"));
            if(s.schema_version!=1||string.IsNullOrWhiteSpace(s.id)||!float.IsFinite(s.refresh_seconds)||s.refresh_seconds<.05f||s.refresh_seconds>1||
                s.edge<0||s.panel_padding<0||s.left_width<250||s.right_width<180||s.body_size<16||s.caption_size<12||s.number_size<s.body_size||s.target_size<16||s.spacing<0||s.corner_radius<0||s.speed_width<120)
                throw new ArgumentException("Invalid gameplay HUD settings.");
            return s;
        }
    }
    /// <summary>Retained, non-interactive HUD; refreshes accepted observations at a bounded UI cadence.</summary>
    public sealed partial class GameplayHud : MonoBehaviour
    {
        public VisualElement Root=>document?.rootVisualElement;
        public int RefreshCount {get;private set;}
        public GameplayHudModel Model {get;private set;}
        public string Locale {get;private set;}
        public int TextSizeIndex {get;private set;}
        public bool HighContrast {get;private set;}
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
        private VisualElement reserveFill,fatigueFill,breathFill,shootingPanel,motionPanel,timingPanel,conditionPanel,helpPanel;
        private bool helpVisible;
        public bool HelpVisible=>helpVisible;
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
            foreground=FrontEndTheme.Color(theme.paper);muted=FrontEndTheme.Color(settings.muted);accent=FrontEndTheme.Color(settings.accent);
            panel=FrontEndPanelSettings.Create();panel.name=settings.id;
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
            var line=Row(timingPanel);
            clock=Text(line,"race_clock","",settings.number_size,true);
            clock.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
            lap=Text(line,"lap","",settings.caption_size,true);lap.style.color=accent;
            timing=Text(timingPanel,"timing","",settings.caption_size);
            phase=Text(timingPanel,"phase","",settings.caption_size);phase.style.color=FrontEndTheme.Color(settings.warning);

            conditionPanel=Card("condition_panel",false,false,settings.right_width);
            var pulse=Row(conditionPanel);
            heartbeat=new HeartbeatGraphic(accent);pulse.Add(heartbeat);
            pulseTrace=new PulseTraceGraphic(accent);pulse.Add(pulseTrace);
            Text(pulse,"pulse_caption","BPM",settings.caption_size).style.color=muted;
            heart=Text(pulse,"heart_rate","",settings.body_size,true);
            heart.style.fontSize=22*Scale;heart.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
            reserve=Text(conditionPanel,"reserve","",settings.caption_size);reserveFill=Bar(conditionPanel,"reserve_bar",accent);
            fatigue=Text(conditionPanel,"fatigue","",settings.caption_size);fatigueFill=Bar(conditionPanel,"fatigue_bar",muted);
            wind=Text(conditionPanel,"wind","",settings.caption_size);wind.style.color=muted;

            motionPanel=Card("motion_panel",true,false,settings.speed_width);
            speed=Text(motionPanel,"speed","",settings.number_size,true);speed.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;

            shootingPanel=Card("shooting_panel",true,false,settings.left_width);
            var heading=Row(shootingPanel);
            range=Text(heading,"range","",settings.caption_size,true);range.style.color=accent;
            shots=Text(heading,"shots","",settings.caption_size);shots.style.color=muted;
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.marginTop=4;row.style.marginBottom=8;shootingPanel.Add(row);
            for(int i=0;i<targets.Length;i++)
            {
                var target=Text(row,"target_"+i,"",settings.caption_size,true);targets[i]=target;
                target.style.width=settings.target_size;target.style.height=settings.target_size;target.style.marginRight=10;
                target.style.unityTextAlign=TextAnchor.MiddleCenter;
                target.style.borderTopLeftRadius=target.style.borderTopRightRadius=target.style.borderBottomLeftRadius=target.style.borderBottomRightRadius=settings.target_size/2;
                target.style.borderLeftWidth=target.style.borderRightWidth=target.style.borderTopWidth=target.style.borderBottomWidth=2;
                target.style.borderLeftColor=target.style.borderRightColor=target.style.borderTopColor=target.style.borderBottomColor=foreground;
            }
            weapon=Text(shootingPanel,"weapon","",settings.body_size,true);
            breath=Text(shootingPanel,"breath","",settings.caption_size);breath.style.color=muted;
            breathFill=Bar(shootingPanel,"breath_bar",accent);

            var hint=Card("help_hint",false,true,settings.speed_width);
            Text(hint,"help_key",T("help_key"),settings.caption_size).style.color=muted;
            helpPanel=Card("help_panel",false,true,settings.left_width);
            helpPanel.style.top=settings.edge+48;
            var name=Text(helpPanel,"athlete_name",displayName,settings.body_size,true);
            name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;
            history=Text(helpPanel,"history","",settings.caption_size);
            help=Text(helpPanel,"controls","",settings.caption_size);
            helpPanel.style.display=helpVisible?DisplayStyle.Flex:DisplayStyle.None;
            BuildBroadcast();
            BuildReviewPanels();
            BuildPaceStart();
            Root.Query<VisualElement>().ForEach(v=>{v.pickingMode=PickingMode.Ignore;v.focusable=false;});Root.pickingMode=PickingMode.Ignore;
        }
        public void SetAccessibility(bool contrast)
        {HighContrast=contrast;muted=contrast?Color.white:FrontEndTheme.Color(settings.muted);Build();rendered=null;Refresh();}
        private VisualElement Row(VisualElement parent)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;
            row.style.justifyContent=Justify.SpaceBetween;row.style.alignItems=Align.Center;parent.Add(row);return row;
        }
        private VisualElement Card(string name,bool left,bool top,int width)
        {
            var card=new VisualElement{name=name};card.style.position=Position.Absolute;
            card.style.width=width*Scale;card.style.maxWidth=new Length(40,LengthUnit.Percent);
            if(left)card.style.left=settings.edge;else card.style.right=settings.edge;
            if(top)card.style.top=settings.edge;else card.style.bottom=settings.edge;
            card.style.backgroundColor=HighContrast?Color.black:FrontEndTheme.Color(settings.background);
            card.style.borderTopWidth=1;card.style.borderTopColor=new Color(.55f,.82f,.87f,.38f);
            card.style.paddingTop=card.style.paddingBottom=settings.panel_padding;
            card.style.paddingLeft=card.style.paddingRight=settings.panel_padding+2;
            card.style.borderTopLeftRadius=card.style.borderTopRightRadius=card.style.borderBottomLeftRadius=card.style.borderBottomRightRadius=settings.corner_radius;
            Root.Add(card);return card;
        }
        private Label Text(VisualElement parent,string name,string value,int size,bool bold=false)
        {
            var label=new Label(value){name=name,enableRichText=false};label.style.color=foreground;label.style.fontSize=size*Scale;
            label.style.whiteSpace=WhiteSpace.Normal;label.style.marginTop=0;label.style.marginBottom=settings.spacing;
            label.style.marginLeft=label.style.marginRight=0;label.style.paddingTop=label.style.paddingBottom=0;
            label.style.paddingLeft=label.style.paddingRight=0;label.style.flexShrink=1;
            if(bold)label.style.unityFontStyleAndWeight=FontStyle.Bold;parent.Add(label);return label;
        }
        private VisualElement Bar(VisualElement parent,string name,Color color)
        {
            var track=new VisualElement();track.style.height=3;track.style.backgroundColor=new Color(1,1,1,.12f);
            track.style.marginBottom=settings.spacing+2;parent.Add(track);
            var fill=new VisualElement{name=name};fill.style.height=3;fill.style.backgroundColor=color;track.Add(fill);return fill;
        }
        public void SetHelpVisible(bool visible)
        {helpVisible=visible;if(helpPanel!=null)helpPanel.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;}
        public void Refresh()
        {
            RefreshPaceStart();
            var snapshot=feed?.Current;
            if(snapshot==null){rendered=null;selectedIndex=-1;if(Root!=null)Root.style.display=DisplayStyle.None;return;}
            if(ReferenceEquals(rendered,snapshot))return;
            rendered=snapshot;
            if(selectedIndex<0||selectedIndex>=snapshot.Count||snapshot.Get(selectedIndex).AthleteId!=athleteId)
            {selectedIndex=-1;for(int i=0;i<snapshot.Count;i++)if(snapshot.Get(i).AthleteId==athleteId){selectedIndex=i;break;}}
            if(selectedIndex<0){Root.style.display=DisplayStyle.None;return;}
            Root.style.display=DisplayStyle.Flex;var a=snapshot.Get(selectedIndex);Model=new GameplayHudModel(a,text,Locale);RefreshCount++;
            RefreshShortBurst(a);
            RefreshBroadcast(snapshot);
            RefreshReviewPanels(a);
            clock.text=Model.Clock;lap.text=Model.Lap;timing.text=Model.Timing;phase.text=Model.Phase;history.text=Model.ShootingHistory;
            phase.style.display=a.PendingPenaltyLoops>0||a.Phase==CompetitionPhase.Prestart||a.Phase>=CompetitionPhase.Finished||host?.LastRangeAssistance.Active==true?DisplayStyle.Flex:DisplayStyle.None;
            if(host?.LastRangeAssistance.Active==true)phase.text=T(host.LastRangeAssistance.Mode==PlayerRangeAssistMode.FinalApproach?"assist_in":"assist_out");
            speed.text=Model.Speed;heart.text=Math.Round(a.Physiology.HeartRateBpm).ToString();heart.tooltip=Model.HeartRate;reserve.text=Model.Reserve;fatigue.text=Model.Fatigue;
            heartbeat.Observe(a.Physiology.HeartRateBpm,a.Physiology.HeartRateBpm/readinessPhysiology.MaxHrBpm>.85?FrontEndTheme.Color(settings.warning):accent);
            pulseTrace.Observe(a.Physiology.HeartRateBpm,a.Physiology.HeartRateBpm/readinessPhysiology.MaxHrBpm>.85?FrontEndTheme.Color(settings.warning):accent);
            reserveFill.style.width=Length.Percent(Model.ReservePercent);fatigueFill.style.width=Length.Percent(Model.FatiguePercent);breathFill.style.width=Length.Percent(Model.BreathDebtPercent);
            shootingPanel.style.display=Model.Shooting?DisplayStyle.Flex:DisplayStyle.None;motionPanel.style.display=Model.Shooting?DisplayStyle.None:DisplayStyle.Flex;
            // Review controls remain usable in diagnostic scenes; career races have no review panel.
            float bottom=host?.Review!=null?settings.edge+140:settings.edge;shootingPanel.style.bottom=motionPanel.style.bottom=bottom;
            range.text=Model.Range;weapon.text=Model.Weapon;breath.text=Model.Breath;wind.text=Model.Wind;wind.style.display=Model.Shooting?DisplayStyle.Flex:DisplayStyle.None;shots.text=a.ShotsFired+" / 5";
            help.text=T(Model.Shooting?"shooting_help":"ski_help");
            breath.style.color=a.BreathBlocked||a.BreathHolding&&a.BreathSwayMultiplier>1?FrontEndTheme.Color(settings.warning):muted;
            for(int i=0;i<targets.Length;i++)
            {
                bool closed=(Model.ClosedMask&(1<<i))!=0;targets[i].text=HighContrast?(closed?"✓":"•"):"";
                targets[i].style.backgroundColor=closed?foreground:Color.clear;targets[i].style.color=closed?FrontEndTheme.Color(theme.ink):foreground;
            }
        }
        private void LateUpdate()
        {
            if(feed==null)return;
            RefreshPaceStart();
            if(Keyboard.current?.tabKey.wasPressedThisFrame==true&&!Model.Shooting)SetCheckpointTableVisible(!CheckpointTableVisible);
            if(Keyboard.current?.f2Key.wasPressedThisFrame==true)SetBroadcastVisible(!BroadcastVisible);
            if(Keyboard.current?.f1Key.wasPressedThisFrame==true)SetHelpVisible(!helpVisible);
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
