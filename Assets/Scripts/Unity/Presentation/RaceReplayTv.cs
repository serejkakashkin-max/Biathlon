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
        private FrostlineReplayRow[] tvRows;
        private Comparer<int> tvComparer;
        private RacePresentationSnapshot tvSnapshot;
        private Slider timeline;
        private DropdownField speedChoice,cameraChoice,athleteChoice;
        private bool timelineWrite;
        private UiTextCatalog text;
        private FrontEndTheme theme;
        private readonly List<VisualElement> controls=new List<VisualElement>();
        public VisualElement Root=>document?.rootVisualElement;
        private string T(string id)=>text.Text(id,menu.Locale);
        private string Name(string id){for(int i=0;i<snapshot.AthleteCount;i++)if(snapshot.Athlete(i).Id==id)return snapshot.Athlete(i).Name;return id;}
        private string Nation(string id){for(int i=0;i<snapshot.AthleteCount;i++)if(snapshot.Athlete(i).Id==id)return snapshot.Athlete(i).Nation;return "";}
        private string Clock(long tick){long seconds=tick/Info.TickFrequency;return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
        private float FontScale=>menu.TextSizeIndex==0?1:menu.TextSizeIndex==1?1.15f:1.3f;
        private void BuildControls()
        {
            theme=FrontEndTheme.Load();text=UiTextCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_text_v1"));
            uiRoot=new GameObject("Replay interface");SceneManager.MoveGameObjectToScene(uiRoot,gameObject.scene);uiRoot.AddComponent<PanelInputConfiguration>().panelInputRedirection=PanelInputConfiguration.PanelInputRedirection.Never;
            panel=FrontEndPanelSettings.Create();panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("Biathlon/UI/front_end");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(theme.reference_width,theme.reference_height);panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            document=uiRoot.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=80;
            Root.style.flexGrow=1;Root.pickingMode=PickingMode.Ignore;controls.Clear();tvRows=new FrostlineReplayRow[settings.tv_rows];order=new int[authority.Field.Count];
            tvComparer=Comparer<int>.Create(CompareTvRows);
            Root.RegisterCallback<NavigationSubmitEvent>(e=>{if(Keyboard.current?.spaceKey.wasPressedThisFrame==true)e.StopImmediatePropagation();},TrickleDown.TrickleDown);
            Root.RegisterCallback<NavigationMoveEvent>(e=>
            {
                if(e.direction!=NavigationMoveEvent.Direction.Up&&e.direction!=NavigationMoveEvent.Direction.Down)return;
                int direction=e.direction==NavigationMoveEvent.Direction.Down?1:-1;var selected=Root.panel.focusController.focusedElement as VisualElement;
                while(selected!=null&&!controls.Contains(selected))selected=selected.parent;int index=controls.IndexOf(selected);
                if(controls.Count>0){controls[(index+direction+controls.Count)%controls.Count].Focus();Root.panel.focusController.IgnoreEvent(e);e.StopPropagation();}
            });
            var board=Card("tv_timing",true,true);board.style.width=420*FontScale;board.style.maxWidth=Length.Percent(38);
            var heading=new VisualElement();heading.style.flexDirection=FlexDirection.Row;heading.style.justifyContent=Justify.SpaceBetween;board.Add(heading);
            tvTitle=Label(heading,T("tv.checkpoint"),true);tvTitle.style.color=FrostlineStyle.Gold;tvClock=Label(heading,"00:00",true);FrostlineStyle.Text(tvClock,true);
            for(int i=0;i<tvRows.Length;i++){tvRows[i]=new FrostlineReplayRow(FontScale);board.Add(tvRows[i]);}
            var shots=Card("tv_shooting",false,true);shootingBoard=Label(shots,"",true);finish=Label(shots,"");
            var toolbar=Card("replay_controls",true,false);toolbar.style.right=24;toolbar.style.width=StyleKeyword.Auto;toolbar.style.maxWidth=new Length(100,LengthUnit.Percent);
            status=Label(toolbar,T("replay.controls"));status.name="replay_status";
            timeline=new Slider(0,Info.TickCount/(float)Info.TickFrequency){name="replay_timeline"};toolbar.Add(timeline);controls.Add(timeline);
            timeline.RegisterValueChangedCallback(e=>{if(!timelineWrite)TrySeek((long)(e.newValue*Info.TickFrequency));});
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;row.style.alignItems=Align.Center;toolbar.Add(row);
            Button(row,"replay_pause",T("replay.pause"),TogglePause);Button(row,"replay_back",T("replay.back"),()=>TrySeek(Tick-(long)(settings.seek_seconds*Info.TickFrequency)));
            Button(row,"replay_forward",T("replay.forward"),()=>TrySeek(Tick+(long)(settings.seek_seconds*Info.TickFrequency)));
            var speed=new DropdownField(new List<string>{"1×","2×","4×"},0){name="replay_speed"};Style(speed);row.Add(speed);speed.RegisterValueChangedCallback(e=>SetSpeed(speed.index==0?1:speed.index==1?2:4));
            var modes=new DropdownField(new List<string>{T("replay.auto"),T("replay.track"),T("replay.wide"),T("replay.range"),T("replay.finish")},0){name="replay_camera"};Style(modes);row.Add(modes);
            modes.RegisterValueChangedCallback(e=>SetCamera(new[]{"auto","track","wide","range","finish"}[modes.index]));
            var names=new List<string>();for(int i=0;i<authority.Field.Count;i++)names.Add(Name(authority.Field.GetAthlete(i).AthleteId.Value));
            var athletes=new DropdownField(names,SelectedIndex){name="replay_athlete"};Style(athletes);row.Add(athletes);athletes.RegisterValueChangedCallback(e=>SelectAthlete(athletes.index));
            speedChoice=speed;cameraChoice=modes;athleteChoice=athletes;
            Button(row,"replay_exit",T("back"),Close);FrostlineStyle.Apply(Root,menu.ReducedMotion);Root.EnableInClassList("high-contrast",menu.HighContrast);Root.schedule.Execute(()=>controls[1].Focus());
        }
        private void TrySeek(long tick){try{Seek(tick);}catch(Exception e){LastFailure=e.Message;Paused=true;status.text=T("replay_open_error");}}
        private VisualElement Card(string name,bool left,bool top)
        {
            var card=new VisualElement{name=name};card.style.position=Position.Absolute;
            if(left)card.style.left=24;else card.style.right=24;if(top)card.style.top=24;else card.style.bottom=24;
            card.style.width=330*FontScale;card.style.maxWidth=new Length(30,LengthUnit.Percent);card.style.paddingTop=card.style.paddingBottom=10;card.style.paddingLeft=card.style.paddingRight=14;
            FrostlineStyle.SurfaceCard(card,true);card.style.backgroundColor=menu.HighContrast?Color.black:FrostlineStyle.Deep;card.style.borderTopWidth=2;card.style.borderTopColor=FrostlineStyle.Accent;Root.Add(card);return card;
        }
        private Label Label(VisualElement parent,string value,bool bold=false)
        {var l=new Label(value){enableRichText=false};l.style.fontSize=theme.caption_size*FontScale;l.style.color=FrostlineStyle.Ink;l.style.whiteSpace=WhiteSpace.Normal;if(bold)l.style.unityFontStyleAndWeight=FontStyle.Bold;FrostlineStyle.Text(l);parent.Add(l);return l;}
        private void Style(VisualElement element){element.style.height=36*FontScale;element.style.minWidth=100;element.style.marginRight=8;element.style.marginTop=6;element.style.fontSize=theme.caption_size*FontScale;FrostlineStyle.Text(element);controls.Add(element);}
        private void Button(VisualElement parent,string name,string value,Action action)
        {
            var b=new Button(action){name=name,text=value};Style(b);b.style.backgroundColor=FrostlineStyle.Surface;b.style.color=FrostlineStyle.Ink;
            b.RegisterCallback<FocusInEvent>(e=>{b.style.borderBottomWidth=3;b.style.borderBottomColor=FrontEndTheme.Color(theme.accent);});b.RegisterCallback<FocusOutEvent>(e=>b.style.borderBottomWidth=0);
            parent.Add(b);
        }
        private int[] order;
        private int CompareTvRows(int a,int b)
        {
            var aa=tvSnapshot.Get(a).Hud;var bb=tvSnapshot.Get(b).Hud;
            if(tvSnapshot.Complete)return aa.Rank.CompareTo(bb.Rank);
            int checkpoint=bb.CheckpointOrdinal.CompareTo(aa.CheckpointOrdinal);if(checkpoint!=0)return checkpoint;
            int rank=aa.Rank.CompareTo(bb.Rank);return rank!=0?rank:string.CompareOrdinal(tvSnapshot.Get(a).AthleteId,tvSnapshot.Get(b).AthleteId);
        }
        private void RefreshTv()
        {
            if(document==null||Host?.Feed.Current==null)return;var s=Host.Feed.Current;
            int count=s.Count;for(int i=0;i<count;i++)order[i]=i;
            tvSnapshot=s;Array.Sort(order,0,count,tvComparer);
            tvTitle.text=T(s.Complete?"tv.results":"tv.checkpoint");tvClock.text=Clock(Tick);
            for(int i=0;i<tvRows.Length;i++)
            {
                tvRows[i].style.display=i<count?DisplayStyle.Flex:DisplayStyle.None;if(i>=count)continue;var a=s.Get(order[i]);
                string rank=a.Hud.Rank>0?a.Hud.Rank.ToString(CultureInfo.InvariantCulture):"—";
                string gap=a.Hud.GapSeconds.HasValue?"+"+a.Hud.GapSeconds.Value.ToString("0.0",CultureInfo.InvariantCulture):"—";
                tvRows[i].Bind(rank,Name(a.AthleteId),Nation(a.AthleteId),gap,a.Hud.ShootingResults,order[i]==SelectedIndex);
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

    /// <summary>Retained presentation-only replay row; authority and ordering remain owned by the replay.</summary>
    internal sealed class FrostlineReplayRow:VisualElement
    {
        private readonly Label place,athlete,gap,shooting;
        private readonly NationFlagGraphic flag;
        internal FrostlineReplayRow(float scale)
        {
            style.marginTop=7;style.paddingTop=5;style.paddingBottom=5;style.paddingLeft=6;style.paddingRight=6;
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;Add(row);
            place=Cell(row,26*scale,scale,true);place.style.color=FrostlineStyle.Gold;
            flag=new NationFlagGraphic("");row.Add(flag);athlete=Cell(row,0,scale,false);athlete.style.flexGrow=1;athlete.style.flexBasis=0;
            gap=Cell(row,58*scale,scale,true);gap.style.unityTextAlign=TextAnchor.MiddleRight;
            shooting=Cell(this,0,scale,false);shooting.style.width=Length.Percent(100);shooting.style.marginLeft=26*scale+36;shooting.style.color=FrostlineStyle.Muted;shooting.style.fontSize=11*scale;
            pickingMode=PickingMode.Ignore;
        }
        private static Label Cell(VisualElement parent,float width,float scale,bool numeral)
        {
            var label=new Label{enableRichText=false};label.style.width=width;label.style.minWidth=0;label.style.flexShrink=0;label.style.fontSize=13*scale;
            label.style.color=FrostlineStyle.Ink;label.style.whiteSpace=WhiteSpace.NoWrap;label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;
            label.style.marginTop=label.style.marginBottom=label.style.marginLeft=label.style.marginRight=0;FrostlineStyle.Text(label,numeral);parent.Add(label);return label;
        }
        internal void Bind(string rank,string name,string nation,string interval,string shots,bool selected)
        {
            place.text=rank;athlete.text=name;gap.text=interval;shooting.text=shots;flag.SetNation(nation);tooltip=name+" · "+nation+" · "+interval;
            style.backgroundColor=selected?FrostlineStyle.Surface:Color.clear;style.borderLeftWidth=2;style.borderLeftColor=selected?FrostlineStyle.Accent:Color.clear;
        }
    }
}
