using System;
using Biathlon.Career;
using System.Collections.Generic;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        [Serializable] private sealed class ResultsLayout
        {public int schema_version,rows_per_page;public string id;public float row_height,row_padding,heading_height;public int hero_rank_size,hero_title_size,hero_clock_size,shooting_label_size,shooting_mark_size,shooting_detail_size;}
        private ResultsLayout resultsLayout;
        private bool showResultSplits;
        private object pagedResultRecord;private int resultPageSize;
        private static string ResultClock(long milliseconds)
        {
            var time=TimeSpan.FromMilliseconds(milliseconds);
            return time.ToString(milliseconds>=3600000?@"h\:mm\:ss\.fff":@"m\:ss\.fff");
        }
        private void RenderResults(VisualElement actions,VisualElement card)
        {
            var scroll=card as ScrollView;
            if(scroll!=null){scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.contentContainer.style.width=new Length(100,LengthUnit.Percent);}
            card.style.minWidth=0;
            resultsLayout=resultsLayout??JsonUtility.FromJson<ResultsLayout>(ProductionVenueHost.ReadResource("Biathlon/UI/results_layout_v1"));
            if(resultsLayout.schema_version!=1||string.IsNullOrWhiteSpace(resultsLayout.id)||resultsLayout.rows_per_page<10||resultsLayout.rows_per_page>20||resultsLayout.row_height<30||resultsLayout.row_height>44)throw new ArgumentException("Invalid results layout.");
            card.style.paddingTop=card.style.paddingBottom=12;
            var summary=Session.ResultsSummary;var player=summary.Player;var details=player.Finish.Details;
            AddLabel(actions,T("result"),theme.caption_size);
            AddLabel(actions,player.Name,theme.body_size,true);
            var position=AddLabel(actions,F("result_rank",player.Rank,summary.Count),theme.title_size,true);position.name="result_position";
            position.style.color=player.Rank<=3?FrostlineStyle.Gold:FrostlineStyle.Accent;
            AddLabel(actions,ResultClock(player.Finish.ElapsedMilliseconds),theme.section_size,true).name="result_elapsed";
            AddLabel(actions,F("results.points_gain",player.Points,summary.PointsAfter),theme.body_size,true).name="result_points";
            AddLabel(actions,F("results.standing",summary.StandingBefore?.ToString()??"—",summary.StandingAfter,summary.Season),theme.caption_size).name="result_standing";
            AddButton(actions,"career_return",T("career_return"),()=>{Session.ReturnFromResults();showResultSplits=false;LastMessage=null;Render();},true);
            AddButton(actions,"results_protocol",T("results.protocol"),()=>{showResultSplits=false;Render();});
            AddButton(actions,"results_splits",T("results.splits"),()=>{showResultSplits=true;Render();},enabled:details!=null&&details.SplitCount>0);
            AddLabel(actions,F("results.penalties",player.Finish.Penalties),theme.caption_size).name="result_penalties";

            var finishHero=Tile(card,"result_finish_hero",true);finishHero.style.flexDirection=FlexDirection.Row;finishHero.style.alignItems=Align.Center;
            finishHero.style.paddingTop=finishHero.style.paddingBottom=8;finishHero.style.marginBottom=8;finishHero.style.borderLeftWidth=3;finishHero.style.borderLeftColor=player.Rank<=3?FrostlineStyle.Gold:FrostlineStyle.Accent;
            var badge=AddLabel(finishHero,player.Rank.ToString("00"),resultsLayout.hero_rank_size,true);badge.name="result_rank_numeral";badge.style.color=player.Rank<=3?FrostlineStyle.Gold:FrostlineStyle.Accent;badge.style.marginRight=20;badge.style.marginBottom=0;
            var finishIdentity=new VisualElement();finishIdentity.style.flexGrow=1;finishIdentity.style.minWidth=0;finishHero.Add(finishIdentity);
            var header=AddLabel(finishIdentity,T("format."+(int)summary.Record.Format)+" · "+T("woodland"),resultsLayout.hero_title_size,true);header.name="results_title";header.style.marginBottom=4;
            var identity=new VisualElement();identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;finishIdentity.Add(identity);
            identity.Add(new NationFlagGraphic(player.Nation));AddLabel(identity,player.Name,theme.caption_size).style.marginBottom=0;
            var elapsed=AddLabel(finishHero,ResultClock(player.Finish.ElapsedMilliseconds),resultsLayout.hero_clock_size,true);elapsed.style.marginBottom=0;elapsed.style.marginLeft=12;
            if(details!=null)
            {
                var stages=new VisualElement{name="result_shooting"};stages.style.flexDirection=FlexDirection.Row;stages.style.flexWrap=Wrap.Wrap;card.Add(stages);
                for(int b=0;b<details.BoutCount;b++)
                {
                    var stage=new VisualElement();stage.style.flexGrow=1;stage.style.paddingLeft=12;stage.style.borderLeftWidth=2;stage.style.borderLeftColor=FrostlineStyle.Surface;stage.style.marginRight=theme.spacing;stage.style.marginBottom=6;stages.Add(stage);
                    AddLabel(stage,(b+1)+" · "+T("stance."+(int)details.Stance(b)),resultsLayout.shooting_label_size).style.marginBottom=2;
                    var targets=new VisualElement();targets.style.flexDirection=FlexDirection.Row;stage.Add(targets);
                    for(int t=0;t<5;t++)
                    {
                        bool hit=(details.ClosedMask(b)&(1<<t))!=0;
                        var target=new Label(hit?"●":"×"){enableRichText=false,pickingMode=PickingMode.Ignore};target.style.fontSize=resultsLayout.shooting_mark_size*Scale;
                        target.style.color=hit?FrostlineStyle.Accent:FrostlineStyle.Danger;target.style.marginRight=theme.spacing/2;FrostlineStyle.Text(target);targets.Add(target);
                    }
                    var detail=AddLabel(stage,F("results.stage_hits",details.Hits(b),5-details.Hits(b)),resultsLayout.shooting_detail_size);detail.style.color=FrostlineStyle.Muted;detail.style.marginBottom=2;
                }
            }
            if(showResultSplits)RenderResultSplits(card,summary);
            else RenderResultProtocol(card,summary);
        }
        private void RenderResultProtocol(VisualElement card,RaceResultsSummary summary)
        {
            AddLabel(card,T("results.protocol"),theme.body_size,true);
            ResultTableRow(card,"result_table_header",new[]{T("results.rank"),T("results.athlete"),T("results.misses"),T("results.time"),T("results.gap"),T("results.points")},true,false);
            var rows=new VisualElement{name="result_protocol"};card.Add(rows);
            for(int i=0;i<summary.Count;i++)
            {
                var row=summary.Row(i);var d=row.Finish.Details;var misses=new List<string>();
                if(d!=null)for(int b=0;b<d.BoutCount;b++)misses.Add((5-d.Hits(b)).ToString());
                string shooting=d==null?"—":string.Join(" · ",misses);
                ResultTableRow(rows,"result_row_"+i,new[]{row.Rank.ToString(),row.Name,shooting,ResultClock(row.Finish.ElapsedMilliseconds),row.GapMilliseconds==0?"—":"+"+ResultClock(row.GapMilliseconds),row.Points.ToString()},false,row.IsPlayer,row.Nation);
            }
            int pageSize=resultsLayout.rows_per_page;
            if(resultPage<0||!ReferenceEquals(pagedResultRecord,summary.Record)||resultPageSize!=pageSize)
            {resultPage=(summary.Player.Rank-1)/pageSize;pagedResultRecord=summary.Record;resultPageSize=pageSize;}
            PageRows(card,rows,resultPage,pageSize,value=>resultPage=value);
        }
        private void RenderResultSplits(VisualElement card,RaceResultsSummary summary)
        {
            AddLabel(card,T("results.splits"),theme.body_size,true);
            AddLabel(card,T("results.splits_note"),theme.caption_size);
            var rows=new VisualElement{name="result_splits"};card.Add(rows);
            var details=summary.Player.Finish.Details;
            for(int i=0;i<details.SplitCount;i++)
            {
                var split=details.Split(i);long prior=i==0?0:details.Split(i-1).ElapsedMilliseconds;
                var row=new VisualElement{name="result_split_"+i};row.style.paddingTop=theme.spacing;row.style.paddingBottom=theme.spacing;
                row.style.borderBottomWidth=1;row.style.borderBottomColor=FrontEndTheme.Color(theme.focus);rows.Add(row);
                AddLabel(row,F("results.split_number",i+1),theme.body_size,true);
                AddLabel(row,F("results.split_times",ResultClock(split.ElapsedMilliseconds),ResultClock(split.ElapsedMilliseconds-prior)),theme.caption_size);
            }
        }
        private void ResultTableRow(VisualElement parent,string id,string[] values,bool heading,bool player,string nation=null,CareerLeaderBib leader=CareerLeaderBib.None)
        {
            var row=new VisualElement{name=id};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;
            bool resultProtocol=id=="result_table_header"||id.StartsWith("result_row_",StringComparison.Ordinal);
            row.style.paddingTop=row.style.paddingBottom=resultProtocol?(heading?0:resultsLayout.row_padding):heading?8:10;
            if(resultProtocol){row.style.height=heading?resultsLayout.heading_height:resultsLayout.row_height;row.style.flexShrink=0;}row.style.paddingLeft=row.style.paddingRight=8;
            row.style.borderBottomWidth=1;row.style.borderBottomColor=FrostlineStyle.Surface;
            row.style.backgroundColor=heading?FrostlineStyle.Deep:parent.childCount%2==0?new Color(FrostlineStyle.Surface.r,FrostlineStyle.Surface.g,FrostlineStyle.Surface.b,.35f):Color.clear;
            if(player){row.style.backgroundColor=FrostlineStyle.Surface;row.style.borderLeftWidth=4;row.style.borderLeftColor=FrostlineStyle.Accent;}
            parent.Add(row);float[] widths=values.Length==7?new float[]{6,34,10,11,11,14,14}:new float[]{6,32,16,18,18,10};
            for(int i=0;i<values.Length;i++)
            {
                if(i==1&&!heading&&nation!=null)
                {
                    var identity=new VisualElement();identity.style.width=Length.Percent(widths[i]);identity.style.flexDirection=FlexDirection.Row;identity.style.alignItems=Align.Center;identity.style.minWidth=0;row.Add(identity);
                    identity.Add(new NationFlagGraphic(nation));
                    if(leader!=CareerLeaderBib.None)identity.Add(new LeaderBibGraphic(leader,LeaderDescription(leader)));
                    var name=AddLabel(identity,values[i],theme.caption_size,player);
                    name.style.marginBottom=0;name.style.flexGrow=1;name.style.minWidth=0;name.style.whiteSpace=WhiteSpace.NoWrap;name.style.overflow=Overflow.Hidden;name.style.textOverflow=TextOverflow.Ellipsis;name.tooltip=values[i]+" · "+nation;
                    continue;
                }
                var label=AddLabel(row,i==0&&heading?"#":values[i],theme.caption_size,heading||player||i==0);label.style.marginBottom=0;
                if(i!=1)FrostlineStyle.Text(label,!heading);if(heading)label.style.color=FrostlineStyle.Muted;
                if(i==0&&!heading){label.style.color=player?FrostlineStyle.Accent:int.TryParse(values[i],out int rank)&&rank<=3?FrostlineStyle.Gold:FrostlineStyle.Muted;label.style.fontSize=(resultProtocol?theme.caption_size:theme.body_size)*Scale;}
                if(i>1)label.style.unityTextAlign=TextAnchor.MiddleRight;
                label.tooltip=values[i];label.style.width=Length.Percent(widths[i]);label.style.paddingRight=8;label.style.minWidth=0;label.style.flexShrink=1;
                label.style.whiteSpace=WhiteSpace.NoWrap;label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;
            }
        }
    }
}
