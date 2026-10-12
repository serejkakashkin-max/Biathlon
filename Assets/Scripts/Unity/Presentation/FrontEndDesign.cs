using System;
using System.Collections.Generic;
using Biathlon.Domain;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private int calendarPage=-1,resultPage=-1,standingsPage,trainingPage,equipmentPage;
        private string L(string ru,string en)=>Locale=="ru"?ru:en;
        private VisualElement TileGrid(VisualElement parent,string id="")
        {var grid=new VisualElement{name=id};grid.style.flexDirection=FlexDirection.Row;grid.style.flexWrap=Wrap.Wrap;grid.style.justifyContent=Justify.SpaceBetween;parent.Add(grid);return grid;}
        private VisualElement Tile(VisualElement parent,string id="",bool dark=false)
        {
            var tile=new VisualElement{name=id};tile.style.backgroundColor=dark?FrontEndTheme.Color(theme.ink):Color.white;
            tile.style.paddingTop=tile.style.paddingBottom=18;tile.style.paddingLeft=tile.style.paddingRight=20;
            tile.style.marginBottom=14;tile.style.borderTopLeftRadius=tile.style.borderTopRightRadius=tile.style.borderBottomLeftRadius=tile.style.borderBottomRightRadius=12;
            parent.Add(tile);return tile;
        }
        private void MetricStrip(VisualElement parent,params string[] pairs)
        {
            var strip=TileGrid(parent);strip.style.marginTop=12;strip.style.marginBottom=8;
            for(int i=0;i<pairs.Length;i+=2)
            {
                var cell=Tile(strip);cell.style.width=Length.Percent(100f/(pairs.Length/2)-1.5f);
                AddLabel(cell,pairs[i],theme.section_size,true).style.unityFontStyleAndWeight=FontStyle.BoldAndItalic;
                var caption=AddLabel(cell,pairs[i+1],theme.caption_size);caption.style.color=FrontEndTheme.Color(theme.muted);caption.style.marginBottom=0;
            }
        }
        private void DeltaMeter(VisualElement parent,string label,double delta)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;parent.Add(row);
            AddLabel(row,label,theme.caption_size).style.color=FrontEndTheme.Color(theme.muted);
            AddLabel(row,(delta>0?"+":"")+(delta*100).ToString("0.#")+"%",theme.caption_size,true).style.color=delta<0?FrontEndTheme.Color("#985746"):accent;
            var axis=new VisualElement();axis.style.height=4;axis.style.backgroundColor=focus;axis.style.marginBottom=14;parent.Add(axis);
            var fill=new VisualElement();fill.style.position=Position.Absolute;fill.style.height=4;fill.style.width=Length.Percent((float)Math.Min(50,Math.Abs(delta)*1000));
            fill.style.left=Length.Percent(delta<0?50-(float)Math.Min(50,Math.Abs(delta)*1000):50);fill.style.backgroundColor=delta<0?FrontEndTheme.Color("#985746"):accent;axis.Add(fill);
            axis.tooltip=L("Шкала относительно выбранных лыж: −5% … +5%","Compared with selected skis: −5% … +5%");
        }
        private void ReadinessMeter(VisualElement parent,string label,int percent,Color color)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;parent.Add(row);
            AddLabel(row,label,theme.caption_size);AddLabel(row,percent+"%",theme.caption_size,true);
            var track=new VisualElement();track.style.height=6;track.style.backgroundColor=focus;track.style.marginBottom=16;parent.Add(track);
            var fill=new VisualElement();fill.style.height=6;fill.style.width=Length.Percent(Mathf.Clamp(percent,0,100));fill.style.backgroundColor=color;track.Add(fill);
        }
        private void InfoRow(VisualElement parent,string label,string value)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;row.style.marginBottom=8;parent.Add(row);
            var caption=AddLabel(row,label,theme.caption_size);caption.style.width=Length.Percent(35);caption.style.color=FrontEndTheme.Color(theme.muted);
            var detail=AddLabel(row,value,theme.body_size,true);detail.style.width=Length.Percent(63);
        }
        private void PageRows(VisualElement parent,VisualElement rows,int page,int perPage,Action<int> change)
        {
            int pages=Math.Max(1,(rows.childCount+perPage-1)/perPage);page=Mathf.Clamp(page,0,pages-1);
            for(int i=0;i<rows.childCount;i++)rows[i].style.display=i/perPage==page?DisplayStyle.Flex:DisplayStyle.None;
            var bar=new VisualElement();bar.style.flexDirection=FlexDirection.Row;bar.style.alignItems=Align.Center;bar.style.justifyContent=Justify.SpaceBetween;parent.Add(bar);
            AddButton(bar,rows.name+"_prev","←",()=>{change(Math.Max(0,page-1));Render();},enabled:page>0);
            AddLabel(bar,(page+1)+" / "+pages,theme.caption_size,true);
            AddButton(bar,rows.name+"_next","→",()=>{change(Math.Min(pages-1,page+1));Render();},enabled:page+1<pages);
        }
        private void AddCourseMap(VisualElement parent)
        {
            var track=TrackDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/track"));
            var visual=TrackVisualDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/visual"),track);
            var map=new CourseMapGraphic(visual);map.name="venue_map";map.style.height=190;map.style.flexShrink=0;parent.Add(map);
        }
    }
    internal sealed class SkiPairGraphic:VisualElement
    {
        internal SkiPairGraphic(Color color,Color accentColor)
        {
            style.height=72;pickingMode=PickingMode.Ignore;generateVisualContent+=ctx=>
            {
                var p=ctx.painter2D;p.lineCap=LineCap.Round;
                for(int i=0;i<2;i++)
                {
                    float y=25+i*19;p.strokeColor=color;p.lineWidth=7;p.BeginPath();p.MoveTo(new Vector2(12,y));p.LineTo(new Vector2(contentRect.width-32,y));p.QuadraticCurveTo(new Vector2(contentRect.width-10,y),new Vector2(contentRect.width-6,y-12));p.Stroke();
                    p.strokeColor=accentColor;p.lineWidth=11;p.BeginPath();p.MoveTo(new Vector2(contentRect.width*.45f,y));p.LineTo(new Vector2(contentRect.width*.55f,y));p.Stroke();
                }
            };
        }
    }
    /// <summary>Actual authored course geometry, painted only when layout changes.</summary>
    internal sealed class CourseMapGraphic:VisualElement
    {
        private readonly List<Vector2[]> lines=new List<Vector2[]>();private Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        internal CourseMapGraphic(TrackVisualDefinition visual)
        {
            for(int i=0;i<visual.Count;i++)
            {
                var loop=visual.Get(i);if(loop.LoopId!="main"&&loop.LoopId!="start_approach"&&loop.LoopId!="finish_spur"&&loop.LoopId!="penalty_150"&&loop.LoopId!="sprint_extension")continue;
                var points=new List<Vector2>();for(int j=0;j<loop.Count;j+=Math.Max(1,loop.Count/100)){var k=loop.Get(j);var p=new Vector2((float)k.X,(float)-k.Z);points.Add(p);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
                lines.Add(points.ToArray());
            }
            pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;
        }
        private void Draw(MeshGenerationContext ctx)
        {
            if(lines.Count==0||contentRect.width<1)return;
            float scale=Mathf.Min((contentRect.width-40)/Mathf.Max(1,max.x-min.x),(contentRect.height-28)/Mathf.Max(1,max.y-min.y));
            Vector2 origin=contentRect.center-(min+max)*.5f*scale;var painter=ctx.painter2D;
            foreach(var line in lines)
            {
                painter.strokeColor=FrontEndTheme.Color("#126F78");painter.lineWidth=4;painter.lineCap=LineCap.Round;painter.lineJoin=LineJoin.Round;painter.BeginPath();
                for(int i=0;i<line.Length;i++){var p=origin+line[i]*scale;if(i==0)painter.MoveTo(p);else painter.LineTo(p);}painter.Stroke();
                painter.fillColor=FrontEndTheme.Color("#183849");painter.BeginPath();painter.Arc(origin+line[0]*scale,5,Angle.Degrees(0),Angle.Degrees(360));painter.Fill();
            }
        }
    }
}
