using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class RaceReplayPresentation
    {
        private PanelSettings panel;
        private UIDocument document;
        private Label status,tvTitle,tvClock,shootingBoard,finish;
        private Label[] tvRows;
        private Slider timeline;
        private DropdownField speedChoice,cameraChoice,athleteChoice;
        private bool timelineWrite;
        private UiTextCatalog text;
        private FrontEndTheme theme;
        private readonly List<VisualElement> controls=new List<VisualElement>();
        public VisualElement Root=>document?.rootVisualElement;
        private string T(string id)=>text.Text(id,menu.Locale);
        private string Name(string id){for(int i=0;i<snapshot.AthleteCount;i++)if(snapshot.Athlete(i).Id==id)return snapshot.Athlete(i).Name;return id;}
        private string Clock(long tick){long seconds=tick/Info.TickFrequency;return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
        private float FontScale=>menu.TextSizeIndex==0?1:menu.TextSizeIndex==1?1.15f:1.3f;
        private void BuildControls()
        {
            theme=FrontEndTheme.Load();text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            uiRoot=new GameObject("Replay interface");SceneManager.MoveGameObjectToScene(uiRoot,gameObject.scene);uiRoot.AddComponent<PanelInputConfiguration>().panelInputRedirection=PanelInputConfiguration.PanelInputRedirection.Never;
            panel=FrontEndPanelSettings.Create();panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("Biathlon/UI/front_end");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(theme.reference_width,theme.reference_height);panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            document=uiRoot.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=80;
            Root.style.flexGrow=1;Root.pickingMode=PickingMode.Ignore;controls.Clear();tvRows=new Label[settings.tv_rows];order=new int[authority.Field.Count];
            Root.RegisterCallback<NavigationSubmitEvent>(e=>{if(Keyboard.current?.spaceKey.wasPressedThisFrame==true)e.StopImmediatePropagation();},TrickleDown.TrickleDown);
            Root.RegisterCallback<NavigationMoveEvent>(e=>
            {
                if(e.direction!=NavigationMoveEvent.Direction.Up&&e.direction!=NavigationMoveEvent.Direction.Down)return;
                int direction=e.direction==NavigationMoveEvent.Direction.Down?1:-1;var selected=Root.panel.focusController.focusedElement as VisualElement;
                while(selected!=null&&!controls.Contains(selected))selected=selected.parent;int index=controls.IndexOf(selected);
                if(controls.Count>0){controls[(index+direction+controls.Count)%controls.Count].Focus();Root.panel.focusController.IgnoreEvent(e);e.StopPropagation();}
            });
            var board=Card("tv_timing",true,true);tvTitle=Label(board,T("tv.checkpoint"),true);tvClock=Label(board,"00:00",true);
            for(int i=0;i<tvRows.Length;i++)tvRows[i]=Label(board,"—");
            var shots=Card("tv_shooting",false,true);shootingBoard=Label(shots,"",true);finish=Label(shots,"");
            var toolbar=Card("replay_controls",true,false);toolbar.style.right=24;toolbar.style.width=StyleKeyword.Auto;toolbar.style.maxWidth=new Length(100,LengthUnit.Percent);
            status=Label(toolbar,T("replay.controls"));status.name="replay_status";
            timeline=new Slider(0,Info.TickCount/(float)Info.TickFrequency){name="replay_timeline"};toolbar.Add(timeline);controls.Add(timeline);
            timeline.RegisterValueChangedCallback(e=>{if(!timelineWrite)TrySeek((long)(e.newValue*Info.TickFrequency));});
            var row=new ScrollView(ScrollViewMode.Horizontal);row.style.flexDirection=FlexDirection.Row;row.contentContainer.style.flexDirection=FlexDirection.Row;toolbar.Add(row);
            Button(row,"replay_pause",T("replay.pause"),TogglePause);Button(row,"replay_back",T("replay.back"),()=>TrySeek(Tick-(long)(settings.seek_seconds*Info.TickFrequency)));
            Button(row,"replay_forward",T("replay.forward"),()=>TrySeek(Tick+(long)(settings.seek_seconds*Info.TickFrequency)));
            var speed=new DropdownField(new List<string>{"1×","2×","4×"},0){name="replay_speed"};Style(speed);row.Add(speed);speed.RegisterValueChangedCallback(e=>SetSpeed(speed.index==0?1:speed.index==1?2:4));
            var modes=new DropdownField(new List<string>{T("replay.auto"),T("replay.track"),T("replay.wide"),T("replay.range"),T("replay.finish")},0){name="replay_camera"};Style(modes);row.Add(modes);
            modes.RegisterValueChangedCallback(e=>SetCamera(new[]{"auto","track","wide","range","finish"}[modes.index]));
            var names=new List<string>();for(int i=0;i<authority.Field.Count;i++)names.Add(Name(authority.Field.GetAthlete(i).AthleteId.Value));
            var athletes=new DropdownField(names,SelectedIndex){name="replay_athlete"};Style(athletes);row.Add(athletes);athletes.RegisterValueChangedCallback(e=>SelectAthlete(athletes.index));
            speedChoice=speed;cameraChoice=modes;athleteChoice=athletes;
            Button(row,"replay_exit",T("back"),Close);Root.schedule.Execute(()=>controls[1].Focus());
        }
        private void TrySeek(long tick){try{Seek(tick);}catch(Exception e){LastFailure=e.Message;Paused=true;status.text=T("replay_open_error");}}
        private VisualElement Card(string name,bool left,bool top)
        {
            var card=new VisualElement{name=name};card.style.position=Position.Absolute;
            if(left)card.style.left=24;else card.style.right=24;if(top)card.style.top=24;else card.style.bottom=24;
            card.style.width=330*FontScale;card.style.maxWidth=new Length(30,LengthUnit.Percent);card.style.paddingTop=card.style.paddingBottom=10;card.style.paddingLeft=card.style.paddingRight=14;
            card.style.backgroundColor=menu.HighContrast?Color.black:new Color(.02f,.08f,.1f,.94f);card.style.borderTopWidth=3;card.style.borderTopColor=FrontEndTheme.Color(theme.accent);Root.Add(card);return card;
        }
        private Label Label(VisualElement parent,string value,bool bold=false)
        {var l=new Label(value){enableRichText=false};l.style.fontSize=theme.caption_size*FontScale;l.style.color=Color.white;l.style.whiteSpace=WhiteSpace.Normal;if(bold)l.style.unityFontStyleAndWeight=FontStyle.Bold;parent.Add(l);return l;}
        private void Style(VisualElement element){element.style.height=40*FontScale;element.style.minWidth=100;element.style.marginRight=8;element.style.fontSize=theme.caption_size*FontScale;controls.Add(element);}
        private void Button(VisualElement parent,string name,string value,Action action)
        {
            var b=new Button(action){name=name,text=value};Style(b);b.style.backgroundColor=FrontEndTheme.Color(theme.paper);b.style.color=FrontEndTheme.Color(theme.ink);
            b.RegisterCallback<FocusInEvent>(e=>{b.style.borderBottomWidth=3;b.style.borderBottomColor=FrontEndTheme.Color(theme.accent);});b.RegisterCallback<FocusOutEvent>(e=>b.style.borderBottomWidth=0);
            parent.Add(b);
        }
        private int[] order;
        private void RefreshTv()
        {
            if(document==null||Host?.Feed.Current==null)return;var s=Host.Feed.Current;
            int count=s.Count;for(int i=0;i<count;i++)order[i]=i;
            Array.Sort(order,0,count,Comparer<int>.Create((a,b)=>
            {
                var aa=s.Get(a).Hud;var bb=s.Get(b).Hud;
                if(s.Complete)return aa.Rank.CompareTo(bb.Rank);
                int checkpoint=bb.CheckpointOrdinal.CompareTo(aa.CheckpointOrdinal);if(checkpoint!=0)return checkpoint;
                int rank=aa.Rank.CompareTo(bb.Rank);return rank!=0?rank:string.CompareOrdinal(s.Get(a).AthleteId,s.Get(b).AthleteId);
            }));
            tvTitle.text=T(s.Complete?"tv.results":"tv.checkpoint");tvClock.text=Clock(Tick);
            for(int i=0;i<tvRows.Length;i++)
            {
                tvRows[i].style.display=i<count?DisplayStyle.Flex:DisplayStyle.None;if(i>=count)continue;var a=s.Get(order[i]);
                string rank=a.Hud.Rank>0?a.Hud.Rank.ToString(CultureInfo.InvariantCulture):"—";
                string gap=a.Hud.GapSeconds.HasValue?"+"+a.Hud.GapSeconds.Value.ToString("0.0",CultureInfo.InvariantCulture):"—";
                tvRows[i].text=rank+"  "+Name(a.AthleteId)+"  "+gap+"  "+a.Hud.ShootingResults;
            }
            var selected=s.Get(SelectedIndex);string targets="";for(int i=0;i<5;i++)targets+=(selected.ClosedTargetsMask&(1<<i))!=0?"● ":"○ ";
            shootingBoard.text=Name(selected.AthleteId)+"\n"+(selected.ShootingActive?targets+" · "+T("stance."+(int)selected.Stance):selected.Hud.ShootingResults);
            finish.text=selected.PendingPenaltyLoops>0?string.Format(T("tv.penalties"),selected.PendingPenaltyLoops):selected.Phase==CompetitionPhase.Finished?T("tv.finished"):"";
            timelineWrite=true;timeline.SetValueWithoutNotify(Tick/(float)Info.TickFrequency);timelineWrite=false;
            speedChoice.SetValueWithoutNotify(Speed+"×");
            cameraChoice.SetValueWithoutNotify(cameraChoice.choices[CameraMode=="auto"?0:CameraMode=="track"?1:CameraMode=="wide"?2:CameraMode=="range"?3:4]);
            athleteChoice.SetValueWithoutNotify(athleteChoice.choices[SelectedIndex]);
            if(LastFailure==null)status.text=Seeking?T("replay.seeking")+" "+Math.Round(100*Tick/(double)Math.Max(1,seekTarget))+"%":Clock(Tick)+" / "+Clock(Info.TickCount)+" · "+(Paused?T("replay.paused"):Speed+"×")+" · "+T("replay.controls");
        }
    }
}
