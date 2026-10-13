using System;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Five physical targets plus separate miss markers; no hidden shot resolution.</summary>
    public sealed class RangeTargetStrip:VisualElement
    {
        public int ClosedMask {get;private set;} public int Misses {get;private set;}
        public RangeTargetStrip(){pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
        public void Observe(int mask,int misses)
        {if(mask==ClosedMask&&misses==Misses)return;ClosedMask=mask;Misses=misses;MarkDirtyRepaint();}
        private void Draw(MeshGenerationContext c)
        {
            var p=c.painter2D;float w=contentRect.width,h=contentRect.height;
            void Circle(float x,float radius,Color fill,Color stroke)
            {p.fillColor=fill;p.strokeColor=stroke;p.lineWidth=1.2f;p.BeginPath();p.Arc(new Vector2(x,h*.5f),radius,0,360);p.ClosePath();p.Fill();p.Stroke();}
            for(int i=0;i<Misses;i++)Circle(w*(.025f+i*.04f),h*.075f,FrostlineStyle.Danger,FrostlineStyle.Danger);
            for(int i=0;i<5;i++)
            {bool hit=(ClosedMask&(1<<i))!=0;Circle(w*(.30f+i*.15f),Mathf.Min(h*.28f,w*.06f),hit?FrostlineStyle.Ink:FrostlineStyle.Deep,hit?FrostlineStyle.Ink:FrostlineStyle.Muted);}
        }
    }
    public sealed partial class GameplayHud
    {
        public bool RangeBroadcastVisible {get;private set;}
        public int RangeBroadcastRows {get;private set;}
        public RangeBroadcastModel RangeBroadcast {get;private set;}
        private RangeBroadcastDefinition rangeBroadcastPolicy;
        private VisualElement rangeBoard;
        private Label rangeBoardTitle,rangeBoardEmpty,rangeBoardMore;
        private VisualElement[] rangeBoardCards;
        private Label[] rangeBoardNames,rangeBoardLanes;
        private NationFlagGraphic[] rangeBoardFlags;
        private RangeTargetStrip[] rangeBoardTargets;
        private int paintedRangeWindow=-1;
        private void InitializeRangeBroadcast()
        {
            rangeBroadcastPolicy=RangeBroadcastDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/range_broadcast_v1"));
            if(host?.Runner==null)return;
            RangeBroadcast=new RangeBroadcastModel(rangeBroadcastPolicy,host.Runner.Field.Track,host.Runner.Field.Route,host.Runner.Field.Profile,host.Runner.Field.Count);
            feed.Published+=ObserveRangeBroadcast;
            if(feed.Current!=null)RangeBroadcast.Observe(feed.Current);
        }
        private void ObserveRangeBroadcast(RacePresentationSnapshot s)=>RangeBroadcast?.Observe(s);
        private void BuildRangeBroadcast()
        {
            paintedRangeWindow=-1;RangeBroadcastVisible=false;
            rangeBoard=Card("range_broadcast",false,true,rangeBroadcastPolicy.PanelWidth);
            rangeBoard.style.maxWidth=Length.Percent(32);rangeBoard.style.top=settings.edge+64*Scale;
            rangeBoard.style.borderLeftColor=accent;
            rangeBoardTitle=Text(rangeBoard,"range_broadcast_title","",settings.body_size,true);rangeBoardTitle.style.color=accent;
            rangeBoardEmpty=Text(rangeBoard,"range_broadcast_empty",L("Соперники подходят к рубежу","Rivals are approaching the range"),settings.caption_size);
            rangeBoardEmpty.style.color=muted;
            int capacity=rangeBroadcastPolicy.MaximumRows;
            rangeBoardCards=new VisualElement[capacity];rangeBoardNames=new Label[capacity];rangeBoardLanes=new Label[capacity];
            rangeBoardFlags=new NationFlagGraphic[capacity];rangeBoardTargets=new RangeTargetStrip[capacity];
            for(int i=0;i<capacity;i++)
            {
                var card=Row(rangeBoard);card.name="range_rival_"+i;card.style.height=rangeBroadcastPolicy.RowHeight*Scale;
                card.style.marginBottom=3;card.style.paddingLeft=card.style.paddingRight=4;
                card.style.borderTopLeftRadius=card.style.borderTopRightRadius=card.style.borderBottomLeftRadius=card.style.borderBottomRightRadius=3;
                rangeBoardCards[i]=card;
                var lane=Text(card,"range_rival_lane_"+i,"",settings.caption_size,true);lane.style.width=19*Scale;lane.style.flexShrink=0;lane.style.marginBottom=0;lane.style.color=muted;rangeBoardLanes[i]=lane;
                var flag=new NationFlagGraphic("");flag.style.width=20*Scale;flag.style.height=13*Scale;flag.style.marginRight=6;flag.style.flexShrink=0;card.Add(flag);rangeBoardFlags[i]=flag;
                var name=Text(card,"range_rival_name_"+i,"",settings.caption_size,true);name.style.flexGrow=1;name.style.flexBasis=0;name.style.minWidth=0;
                name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;name.style.marginBottom=0;rangeBoardNames[i]=name;
                var targets=new RangeTargetStrip{name="range_rival_targets_"+i};targets.style.width=115*Scale;targets.style.height=25*Scale;targets.style.flexShrink=0;card.Add(targets);rangeBoardTargets[i]=targets;
            }
            rangeBoardMore=Text(rangeBoard,"range_broadcast_more","",settings.caption_size);rangeBoardMore.style.color=muted;
            var legend=Text(rangeBoard,"range_broadcast_legend",L("Точки слева — промахи","Dots on the left — misses"),settings.caption_size);legend.style.color=muted;legend.style.marginBottom=0;
            rangeBoard.style.display=DisplayStyle.None;
        }
        private void RefreshRangeBroadcast(RacePresentationSnapshot snapshot,int player)
        {
            int window=RangeBroadcast==null?-1:RangeBroadcast.Window(snapshot.Get(player).RouteMeters,snapshot.Get(player).OnCourse,snapshot.Get(player).Phase);
            bool visible=window>=0&&!CheckpointTableVisible;
            if(visible!=RangeBroadcastVisible){RangeBroadcastVisible=visible;rangeBoard.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;}
            if(!visible){RangeBroadcastRows=0;return;}
            if(window!=paintedRangeWindow)
            {
                paintedRangeWindow=window;var stance=host.Runner.Field.Profile.GetBout(window).Stance;
                rangeBoardTitle.text=L("РУБЕЖ ","RANGE ")+(window+1)+"  ·  "+L(stance==Biathlon.Domain.ShootingStance.Prone?"ЛЁЖА":"СТОЯ",stance==Biathlon.Domain.ShootingStance.Prone?"PRONE":"STANDING");
            }
            int total=RangeBroadcast.Select(window,player);RangeBroadcastRows=RangeBroadcast.SelectVisible(total,player,snapshot.Tick,snapshot.TickFrequency,rangeBoardCards.Length);
            rangeBoardEmpty.style.display=total==0?DisplayStyle.Flex:DisplayStyle.None;
            rangeBoardMore.text=total>rangeBoardCards.Length?L("НА РУБЕЖЕ: ","AT RANGE: ")+total+"  ·  "+(RangeBroadcast.Page+1)+" / "+RangeBroadcast.Pages:"";
            rangeBoardMore.style.display=total>rangeBoardCards.Length?DisplayStyle.Flex:DisplayStyle.None;
            for(int row=0;row<rangeBoardCards.Length;row++)
            {
                rangeBoardCards[row].style.display=row<RangeBroadcastRows?DisplayStyle.Flex:DisplayStyle.None;if(row>=RangeBroadcastRows)continue;
                var r=RangeBroadcast.Get(RangeBroadcast.Displayed(row));bool own=r.AthleteId==athleteId;
                string name=broadcastNames.TryGetValue(r.AthleteId,out var n)?n:r.AthleteId;
                rangeBoardNames[row].text=name;rangeBoardNames[row].tooltip=name;rangeBoardNames[row].style.color=own?accent:foreground;
                rangeBoardLanes[row].text=r.Active&&r.Lane>0?r.Lane.ToString():r.Shots==5?"✓":"—";
                rangeBoardFlags[row].SetNation(broadcastNations.TryGetValue(r.AthleteId,out var nation)?nation:"");
                rangeBoardTargets[row].Observe(r.ClosedMask,r.Misses);
                rangeBoardTargets[row].tooltip=L("Выстрелы: ","Shots: ")+r.Shots+" / 5 · "+L("Промахи: ","Misses: ")+r.Misses;
                bool flashing=r.Flashing(snapshot.Tick,snapshot.TickFrequency,rangeBroadcastPolicy.MissFlashSeconds);
                float pulse=reducedMotion?1:(snapshot.Tick-r.LastMissTick)/(double)snapshot.TickFrequency% .5<.25?1:.25f;
                rangeBoardCards[row].style.backgroundColor=flashing?new Color(FrostlineStyle.Danger.r*.65f,FrostlineStyle.Danger.g*.2f,FrostlineStyle.Danger.b*.2f,.55f*pulse):
                    own?new Color(accent.r,accent.g,accent.b,.12f):new Color(1,1,1,.035f);
            }
        }
    }
}
