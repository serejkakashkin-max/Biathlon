using System;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Chronological accepted-shot capsules. A cross encodes miss without color alone.</summary>
    public sealed class RangeTargetStrip:VisualElement
    {
        public int ClosedMask {get;private set;} public int Misses {get;private set;} public int Marks {get;private set;}
        private int pulse=-1;private float strength;
        public RangeTargetStrip(){pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
        public void Observe(RangeBroadcastRow row,int recentShot,float intensity)
        {if(row.ShotMarks==Marks&&row.ClosedMask==ClosedMask&&row.Misses==Misses&&recentShot==pulse&&Mathf.Abs(strength-intensity)<.02f)return;Marks=row.ShotMarks;ClosedMask=row.ClosedMask;Misses=row.Misses;pulse=recentShot;strength=intensity;MarkDirtyRepaint();}
        private void Draw(MeshGenerationContext c)
        {
            var p=c.painter2D;float w=contentRect.width,h=contentRect.height;
            for(int i=0;i<5;i++)
            {
                var mark=(RangeShotMark)((Marks>>(i*2))&3);float x=w*(.1f+i*.2f),y=h*.5f;
                float radius=Mathf.Min(h*.31f,w*.075f)*(1+(i==pulse?.15f*strength:0));
                Color color=mark==RangeShotMark.Miss?FrostlineStyle.Danger:mark==RangeShotMark.Hit?FrostlineStyle.Ink:FrostlineStyle.Muted;
                p.fillColor=mark==RangeShotMark.Hit?color:mark==RangeShotMark.Miss?new Color(color.r,color.g,color.b,.19f):FrostlineStyle.Deep;
                p.strokeColor=color;p.lineWidth=mark==RangeShotMark.Pending?1:1.5f;p.BeginPath();p.Arc(new Vector2(x,y),radius,0,360);p.ClosePath();p.Fill();p.Stroke();
                if(mark==RangeShotMark.Miss)
                {float d=radius*.42f;p.BeginPath();p.MoveTo(new Vector2(x-d,y-d));p.LineTo(new Vector2(x+d,y+d));p.MoveTo(new Vector2(x-d,y+d));p.LineTo(new Vector2(x+d,y-d));p.Stroke();}
                if(mark==RangeShotMark.Unobserved)
                {p.BeginPath();p.MoveTo(new Vector2(x-radius*.45f,y));p.LineTo(new Vector2(x+radius*.45f,y));p.Stroke();}
            }
        }
    }

    /// <summary>Range status marks are drawn with the panel, without a fallback font.</summary>
    public sealed class RangeStatusIcon : VisualElement
    {
        public bool Complete { get; private set; }
        private Color tint;

        public RangeStatusIcon(float size, Color tint)
        {
            this.tint = tint;
            pickingMode = PickingMode.Ignore;
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        public void SetState(bool complete, Color color)
        {
            if (Complete == complete && tint == color) return;
            Complete = complete;
            tint = color;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            float size = Mathf.Min(contentRect.width, contentRect.height);
            if (size <= 0) return;
            var center = contentRect.center;
            var painter = context.painter2D;
            painter.BeginPath();
            if (!Complete)
            {
                painter.fillColor = tint;
                painter.Arc(center, size * .16f, 0, 360);
                painter.ClosePath();
                painter.Fill();
                return;
            }
            painter.strokeColor = tint;
            painter.lineWidth = size * .085f;
            painter.MoveTo(center + size * new Vector2(-.25f, .01f));
            painter.LineTo(center + size * new Vector2(-.07f, .18f));
            painter.LineTo(center + size * new Vector2(.25f, -.18f));
            painter.Stroke();
        }
    }

    public sealed partial class GameplayHud
    {
        public bool RangeBroadcastVisible {get;private set;}
        public int RangeBroadcastRows {get;private set;}
        public int RangeBroadcastCapacity {get;private set;}
        public RangeBroadcastModel RangeBroadcast {get;private set;}
        private RangeBroadcastDefinition rangeBroadcastPolicy;
        private VisualElement rangeBoard;
        private Label rangeBoardTitle,rangeBoardEmpty,rangeBoardMore,rangeBoardVisit;
        private VisualElement[] rangeBoardCards;
        private Label[] rangeBoardNames,rangeBoardLanes,rangeBoardBibs,rangeBoardScores;
        private RangeStatusIcon[] rangeBoardStates;
        private NationFlagGraphic[] rangeBoardFlags;
        private RangeTargetStrip[] rangeBoardTargets;
        private RangeBroadcastRow[] paintedRangeRows;
        private readonly string[] rangeNumbers=new string[61];
        private int paintedRangeWindow=-1,rangeOverflow=-1,rangeActive=-1;
        private float rangeLayoutWidth,rangeLayoutHeight;
        private static readonly string[] ShotScores={"0/5","1/5","2/5","3/5","4/5","5/5"};
        private string[] rangeVisitLabels;
        private void InitializeRangeBroadcast()
        {
            rangeBroadcastPolicy=RangeBroadcastDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/range_broadcast_v1"));
            for(int i=0;i<rangeNumbers.Length;i++)rangeNumbers[i]=i==0?"—":i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if(host?.Runner==null)return;
            RangeBroadcast=new RangeBroadcastModel(rangeBroadcastPolicy,host.Runner.Field.Track,host.Runner.Field.Route,host.Runner.Field.Profile,host.Runner.Field.Count);
            if(isActiveAndEnabled)feed.Published+=ObserveRangeBroadcast;
            if(feed.Current!=null)RangeBroadcast.Observe(feed.Current);
        }
        private void ObserveRangeBroadcast(RacePresentationSnapshot s)
        {
            if(!isActiveAndEnabled)return;
            RangeBroadcast?.Observe(s);
            // Text and composition update on accepted publication, without waiting for
            // the slower general telemetry HUD cadence. No extra snapshots are created.
            if(rangeBoard!=null&&selectedIndex>=0&&selectedIndex<s.Count)RefreshRangeBroadcast(s,selectedIndex);
        }
        private void BuildRangeBroadcast()
        {
            paintedRangeWindow=-1;rangeOverflow=rangeActive=-1;rangeLayoutWidth=rangeLayoutHeight=0;RangeBroadcastVisible=false;
            int bouts=host?.Runner?.Field.Profile.BoutCount??0;rangeVisitLabels=new string[bouts];
            for(int i=0;i<bouts;i++)
            {
                bool prone=host.Runner.Field.Profile.GetBout(i).Stance==Biathlon.Domain.ShootingStance.Prone;
                rangeVisitLabels[i]=L("РУБЕЖ ","RANGE ")+(i+1)+"  /  "+L(prone?"ЛЁЖА":"СТОЯ",prone?"PRONE":"STANDING");
            }
            rangeBoard=Card("range_broadcast",false,true,rangeBroadcastPolicy.PanelWidth);
            rangeBoard.style.maxWidth=Length.Percent(35);rangeBoard.style.top=settings.edge;
            rangeBoard.style.paddingTop=8;rangeBoard.style.paddingBottom=7;rangeBoard.style.paddingLeft=rangeBoard.style.paddingRight=9;
            rangeBoard.style.backgroundColor=HighContrast?Color.black:new Color(.043f,.098f,.157f,.87f);
            rangeBoard.style.borderLeftWidth=0;rangeBoard.style.borderTopWidth=2;rangeBoard.style.borderTopColor=accent;
            var heading=Row(rangeBoard);heading.style.height=23*Scale;
            rangeBoardTitle=Text(heading,"range_broadcast_title",L("СТРЕЛЬБИЩЕ","LIVE SHOOTING"),18,true);rangeBoardTitle.style.color=accent;rangeBoardTitle.style.marginBottom=0;
            var liveBadge=Row(heading);
            liveBadge.Add(new RangeStatusIcon(11*Scale,accent));
            var live=Text(liveBadge,"range_broadcast_live","LIVE",10,true);live.style.color=accent;live.style.marginBottom=0;live.style.flexShrink=0;
            rangeBoardVisit=Text(rangeBoard,"range_broadcast_visit",bouts>0?rangeVisitLabels[0]:"",11);rangeBoardVisit.style.color=muted;rangeBoardVisit.style.marginBottom=6;
            var columns=Row(rangeBoard);columns.style.height=18*Scale;columns.style.marginBottom=3;
            var captions=Text(columns,"range_broadcast_columns",L("№     КОВР.     СПОРТСМЕН","BIB   MAT        ATHLETE"),9);captions.style.color=muted;captions.style.marginBottom=0;
            var captionShots=Text(columns,"range_broadcast_shots_label",L("ВЫСТРЕЛЫ        ИТОГ","SHOTS           SCORE"),9);captionShots.style.color=muted;captionShots.style.marginBottom=0;
            rangeBoardEmpty=Text(rangeBoard,"range_broadcast_empty",L("Ожидаем прибытия спортсменов","Awaiting athletes"),settings.caption_size);rangeBoardEmpty.style.color=muted;
            int count=rangeBroadcastPolicy.MaximumRows;
            rangeBoardCards=new VisualElement[count];rangeBoardNames=new Label[count];rangeBoardLanes=new Label[count];rangeBoardBibs=new Label[count];rangeBoardScores=new Label[count];rangeBoardStates=new RangeStatusIcon[count];
            rangeBoardFlags=new NationFlagGraphic[count];rangeBoardTargets=new RangeTargetStrip[count];paintedRangeRows=new RangeBroadcastRow[count];
            for(int i=0;i<count;i++)
            {
                var card=Row(rangeBoard);card.name="range_rival_"+i;card.style.height=rangeBroadcastPolicy.RowHeight*Scale;
                card.style.marginBottom=1;card.style.paddingLeft=4;card.style.paddingRight=3;card.style.flexShrink=0;
                card.style.borderLeftWidth=2;card.style.borderLeftColor=Color.clear;
                card.style.borderBottomWidth=1;card.style.borderBottomColor=new Color(1,1,1,.045f);rangeBoardCards[i]=card;
                Label Fixed(string id,float width,Color color)
                {var l=Text(card,id,"",12,true);l.style.width=width*Scale;l.style.flexShrink=0;l.style.marginBottom=0;l.style.color=color;l.style.unityTextAlign=TextAnchor.MiddleCenter;return l;}
                rangeBoardBibs[i]=Fixed("range_rival_bib_"+i,23,foreground);
                rangeBoardLanes[i]=Fixed("range_rival_lane_"+i,23,muted);
                var flag=new NationFlagGraphic("");flag.style.width=21*Scale;flag.style.height=13*Scale;flag.style.marginLeft=3;flag.style.marginRight=7;flag.style.flexShrink=0;card.Add(flag);rangeBoardFlags[i]=flag;
                var name=Text(card,"range_rival_name_"+i,"",13,true);name.style.flexGrow=1;name.style.flexBasis=0;name.style.minWidth=0;
                name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;name.style.marginBottom=0;rangeBoardNames[i]=name;
                var targets=new RangeTargetStrip{name="range_rival_targets_"+i};targets.tooltip=L("По порядку выстрелов: круг — попадание, крест — промах. Черта — порядок неизвестен после перемотки.","Shot order: filled circle = hit, cross = miss. Dash = order unavailable after seek.");targets.style.width=100*Scale;targets.style.height=24*Scale;targets.style.flexShrink=0;card.Add(targets);rangeBoardTargets[i]=targets;
                rangeBoardScores[i]=Fixed("range_rival_score_"+i,29,foreground);
                var status=new RangeStatusIcon(15*Scale,accent){name="range_rival_state_"+i};
                rangeBoardStates[i]=status;card.Add(status);
                card.style.display=DisplayStyle.None;
            }
            rangeBoardMore=Text(rangeBoard,"range_broadcast_more",L("СТРЕЛЯЮТ 0","SHOOTING 0"),11);rangeBoardMore.style.color=muted;rangeBoardMore.style.marginTop=5;rangeBoardMore.style.marginBottom=0;
            // Keep this absolute overlay measured while unseen. Display.None
            // deferred first text/layout work until the player reached the range.
            rangeBoard.style.visibility=Visibility.Hidden;
            rangeBoard.RegisterCallback<GeometryChangedEvent>(_=>
            {
                if(!isActiveAndEnabled||Root==null||panel==null)return;
                int capacity=RangeBroadcastCapacity;LayoutRangeBroadcast();
                // A resize can change the readable rail count without another
                // accepted tick. Recompose only for that actual capacity change.
                if(capacity!=RangeBroadcastCapacity&&feed?.Current!=null&&selectedIndex>=0)
                    RefreshRangeBroadcast(feed.Current,selectedIndex);
            });
        }
        private void LayoutRangeBroadcast()
        {
            float height=Root.resolvedStyle.height,width=Root.resolvedStyle.width;
            if(!float.IsFinite(height)||height<100){height=theme.reference_height;width=theme.reference_width;}
            // UI Toolkit reference scaling alone would keep20tiny rows at720p. Use
            // the actual target/display pixel budget for minimum readable row height.
            float pixels=panel.targetTexture!=null?panel.targetTexture.height:Screen.height;
            float factor=Mathf.Min(1,pixels/1080f);
            float top=settings.edge,bottom=height-170*Scale-settings.edge;
            if(conditionPanel!=null&&conditionPanel.worldBound.height>0)bottom=conditionPanel.worldBound.yMin-Root.worldBound.yMin-12;
            float rowHeight=Mathf.Max(rangeBroadcastPolicy.RowHeight*Scale,rangeBroadcastPolicy.RowHeight*Scale/Mathf.Max(.5f,factor));
            RangeBroadcastCapacity=Mathf.Clamp(Mathf.FloorToInt((bottom-top-103*Scale)/(rowHeight+1)),1,rangeBroadcastPolicy.MaximumRows);
            if(Mathf.Abs(rangeLayoutWidth-width)<.1f&&Mathf.Abs(rangeLayoutHeight-rowHeight)<.1f)return;
            rangeLayoutWidth=width;rangeLayoutHeight=rowHeight;
            rangeBoard.style.top=top;
            // Small HUD dimensions preserve a legible name and still leave >=65%
            // of the view unobscured; body text already follows accessibility Scale.
            float panelWidth=Mathf.Min(rangeBroadcastPolicy.PanelWidth*Scale,width*.35f);
            rangeBoard.style.width=panelWidth;
            float legibility=Mathf.Max(1,theme.reference_height/Mathf.Max(480,pixels));
            rangeBoardTitle.style.fontSize=18*Scale*legibility;rangeBoardVisit.style.fontSize=11*Scale*legibility;rangeBoardMore.style.fontSize=11*Scale*legibility;
            for(int i=0;i<rangeBoardCards.Length;i++)
            {
                rangeBoardCards[i].style.height=rowHeight;rangeBoardNames[i].style.fontSize=13*Scale*legibility;
                rangeBoardBibs[i].style.fontSize=rangeBoardLanes[i].style.fontSize=rangeBoardScores[i].style.fontSize=12*Scale*legibility;
            }
        }
        private void RefreshRangeBroadcast(RacePresentationSnapshot snapshot,int player)
        {
            if(!isActiveAndEnabled||Root==null||panel==null)return;
            LayoutRangeBroadcast();
            int window=RangeBroadcast==null?-1:RangeBroadcast.Window(snapshot.Get(player).RouteMeters,snapshot.Get(player).OnCourse,snapshot.Get(player).Phase);
            bool visible=window>=0&&!CheckpointTableVisible;
            // During live range coverage, place the existing readiness card beside
            // the bottom pulse card. The board then has its own uninterrupted rail.
            if(readinessPanel!=null){readinessPanel.style.right=visible?settings.edge+rangeBroadcastPolicy.PanelWidth*Scale+12:settings.edge;readinessPanel.style.bottom=visible?settings.edge:settings.edge+168*Scale;}
            if(visible!=RangeBroadcastVisible){RangeBroadcastVisible=visible;rangeBoard.style.visibility=visible?Visibility.Visible:Visibility.Hidden;}
            if(!visible){RangeBroadcastRows=0;return;}
            if(window!=paintedRangeWindow)
            {
                paintedRangeWindow=window;rangeBoardVisit.text=rangeVisitLabels[window];
            }
            RangeBroadcastRows=RangeBroadcast.Compose(window,RangeBroadcastCapacity);
            rangeBoardEmpty.style.display=RangeBroadcastRows==0?DisplayStyle.Flex:DisplayStyle.None;
            if(rangeOverflow!=RangeBroadcast.OverflowCount||rangeActive!=RangeBroadcast.ActiveCount)
            {rangeOverflow=RangeBroadcast.OverflowCount;rangeActive=RangeBroadcast.ActiveCount;rangeBoardMore.text=L("СТРЕЛЯЮТ ","SHOOTING ")+rangeActive+(rangeOverflow>0?L("  ·  ЕЩЁ В ОЧЕРЕДИ ","  ·  QUEUED ")+rangeOverflow:"");}
            for(int row=0;row<rangeBoardCards.Length;row++)
            {
                rangeBoardCards[row].style.display=row<RangeBroadcastRows?DisplayStyle.Flex:DisplayStyle.None;if(row>=RangeBroadcastRows)continue;
                var r=RangeBroadcast.Get(RangeBroadcast.Displayed(row),window);var old=paintedRangeRows[row];bool identity=r.AthleteId!=old.AthleteId||r.Bout!=old.Bout;
                bool own=r.AthleteId==athleteId;
                if(identity)
                {
                    string name=broadcastNames.TryGetValue(r.AthleteId,out var n)?n:r.AthleteId;
                    rangeBoardNames[row].text=name;rangeBoardNames[row].tooltip=name;
                    rangeBoardFlags[row].SetNation(broadcastNations.TryGetValue(r.AthleteId,out var nation)?nation:"");
                    var rig=host.Proxies.GetRig(r.AthleteId);int bib=rig!=null?rig.GetComponent<AthleteBibPresentation>()?.Number??0:0;
                    rangeBoardBibs[row].text=rangeNumbers[Mathf.Clamp(bib,0,60)];
                    rangeBoardCards[row].style.borderLeftColor=own&&rangeBroadcastPolicy.PlayerMarkerEnabled?accent:Color.clear;
                }
                if(identity||r.Lane!=old.Lane)rangeBoardLanes[row].text=rangeNumbers[Mathf.Clamp(r.Lane,0,60)];
                if(identity||r.Shots!=old.Shots||r.ClosedMask!=old.ClosedMask)rangeBoardScores[row].text=ShotScores[RangeBroadcastRow.Hits(r.ClosedMask)];
                if(identity||r.Active!=old.Active)
                    rangeBoardStates[row].SetState(!r.Active,r.Active?accent:foreground);
                paintedRangeRows[row]=r;
            }
            AnimateRangeBroadcast(snapshot.Tick);
        }
        private void AnimateRangeBroadcast(double tick)
        {
            if(!RangeBroadcastVisible||RangeBroadcast==null||paintedRangeWindow<0)return;
            int frequency=RangeBroadcast.Frequency;
            for(int i=0;i<RangeBroadcastRows;i++)
            {
                var r=paintedRangeRows[i];bool own=r.AthleteId==athleteId;
                float opacity=(float)r.Opacity(tick,frequency,rangeBroadcastPolicy);
                if(reducedMotion)opacity=r.Phase((long)tick,frequency,rangeBroadcastPolicy)==RangeRowPhase.Departed?0:1;
                rangeBoardCards[i].style.opacity=opacity;
                rangeBoardCards[i].style.translate=new Translate(reducedMotion?0:(1-opacity)*8,0);
                double missAge=(tick-r.LastMissTick)/frequency,hitAge=(tick-r.LastHitTick)/frequency,completionAge=(tick-r.ResultStartTick)/frequency;
                bool miss=r.LastMissTick>=0&&missAge>=0&&missAge<rangeBroadcastPolicy.MissFlashSeconds;
                bool completed=r.CompletionObserved&&r.CompletedTick>=0&&r.AdmissionTick>=0&&completionAge>=0&&completionAge<rangeBroadcastPolicy.CompletionFlashSeconds;
                var color=miss?FrostlineStyle.Danger:completed?accent:own&&rangeBroadcastPolicy.PlayerMarkerEnabled?accent:Color.white;
                float alpha=miss?.27f*(float)(1-missAge/rangeBroadcastPolicy.MissFlashSeconds):completed?.20f*(float)(1-completionAge/rangeBroadcastPolicy.CompletionFlashSeconds):own&&rangeBroadcastPolicy.PlayerMarkerEnabled?.055f:.025f;
                rangeBoardCards[i].style.backgroundColor=new Color(color.r,color.g,color.b,alpha);
                bool hit=r.LastHitTick>=0&&hitAge>=0&&hitAge<rangeBroadcastPolicy.HitFlashSeconds;
                rangeBoardTargets[i].Observe(r,miss||hit?r.LastShot:-1,reducedMotion?0:miss?(float)(1-missAge/rangeBroadcastPolicy.MissFlashSeconds):hit?(float)(1-hitAge/rangeBroadcastPolicy.HitFlashSeconds):0);
            }
        }
    }
}
