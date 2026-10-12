using System;
using System.Collections.Generic;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
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
            var summary=Session.ResultsSummary;var player=summary.Player;var details=player.Finish.Details;
            AddLabel(actions,T("result"),theme.caption_size);
            AddLabel(actions,player.Name,theme.body_size,true);
            var position=AddLabel(actions,F("result_rank",player.Rank,summary.Count),theme.title_size,true);position.name="result_position";
            position.style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
            AddLabel(actions,ResultClock(player.Finish.ElapsedMilliseconds),theme.section_size,true).name="result_elapsed";
            AddLabel(actions,F("results.points_gain",player.Points,summary.PointsAfter),theme.body_size,true).name="result_points";
            AddLabel(actions,F("results.standing",summary.StandingBefore?.ToString()??"—",summary.StandingAfter,summary.Season),theme.caption_size).name="result_standing";
            AddButton(actions,"career_return",T("career_return"),()=>{Session.ReturnFromResults();showResultSplits=false;LastMessage=null;Render();},true);
            AddButton(actions,"results_protocol",T("results.protocol"),()=>{showResultSplits=false;Render();});
            AddButton(actions,"results_splits",T("results.splits"),()=>{showResultSplits=true;Render();},enabled:details!=null&&details.SplitCount>0);
            AddLabel(actions,T("result_saved"),theme.caption_size);

            var header=AddLabel(card,T("format."+(int)summary.Record.Format)+" · "+T("woodland"),theme.section_size,true);header.name="results_title";
            if(details!=null)
            {
                var stages=new VisualElement{name="result_shooting"};stages.style.flexDirection=FlexDirection.Row;stages.style.flexWrap=Wrap.Wrap;card.Add(stages);
                for(int b=0;b<details.BoutCount;b++)
                {
                    var stage=new VisualElement();stage.style.marginRight=theme.spacing*2;stage.style.marginBottom=theme.spacing;stages.Add(stage);
                    AddLabel(stage,(b+1)+" · "+T("stance."+(int)details.Stance(b)),theme.caption_size);
                    var targets=new VisualElement();targets.style.flexDirection=FlexDirection.Row;stage.Add(targets);
                    for(int t=0;t<5;t++)
                    {
                        bool hit=(details.ClosedMask(b)&(1<<t))!=0;
                        var target=new Label(hit?"●":"×"){enableRichText=false,pickingMode=PickingMode.Ignore};target.style.fontSize=theme.section_size*Scale;
                        target.style.color=hit?accent:ink;target.style.marginRight=theme.spacing/2;targets.Add(target);
                    }
                    AddLabel(stage,F("results.stage_hits",details.Hits(b),5-details.Hits(b)),theme.caption_size);
                }
            }
            AddLabel(card,F("results.penalties",player.Finish.Penalties),theme.caption_size).name="result_penalties";
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
                ResultTableRow(rows,"result_row_"+i,new[]{row.Rank.ToString(),row.Name+" · "+row.Nation,shooting,ResultClock(row.Finish.ElapsedMilliseconds),row.GapMilliseconds==0?"—":"+"+ResultClock(row.GapMilliseconds),row.Points.ToString()},false,row.IsPlayer);
            }
            int pageSize=compactLayout?3:5;
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
        private void ResultTableRow(VisualElement parent,string id,string[] values,bool heading,bool player)
        {
            var row=new VisualElement{name=id};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;
            row.style.paddingTop=row.style.paddingBottom=12;row.style.paddingLeft=row.style.paddingRight=8;
            row.style.borderBottomWidth=1;row.style.borderBottomColor=FrontEndTheme.Color(theme.focus);
            if(player){row.style.backgroundColor=focus;row.style.borderLeftWidth=4;row.style.borderLeftColor=accent;}
            parent.Add(row);float[] widths={6,32,16,18,18,10};
            for(int i=0;i<6;i++)
            {
                var label=AddLabel(row,i==0&&heading?"#":values[i],theme.caption_size,heading||player||i==0);label.style.marginBottom=0;
                label.tooltip=values[i];label.style.width=Length.Percent(widths[i]);label.style.paddingRight=8;label.style.minWidth=0;label.style.flexShrink=1;
                label.style.whiteSpace=WhiteSpace.NoWrap;label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;
            }
        }
    }
}
