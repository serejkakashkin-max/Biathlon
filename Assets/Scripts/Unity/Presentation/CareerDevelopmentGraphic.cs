using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Retained seasonal observations, drawn only on repaint. No animation timer or generated textures.</summary>
    internal sealed class CareerDevelopmentGraphic:VisualElement
    {
        public int ObservationCount=>summary.ObservationCount;
        private readonly AthleteCareerSummary summary;
        private readonly int weeks;
        internal CareerDevelopmentGraphic(AthleteCareerSummary summary,int weeksPerSeason)
        {
            this.summary=summary;weeks=Mathf.Max(1,weeksPerSeason);name="athlete_development_graph";
            style.height=170;style.flexShrink=0;style.marginTop=8;style.marginBottom=10;pickingMode=PickingMode.Ignore;
            generateVisualContent+=Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            if(contentRect.width<30||summary.ObservationCount==0)return;
            var painter=context.painter2D;var area=new Rect(16,10,contentRect.width-32,contentRect.height-24);
            float first=Time(summary.Observation(0)),last=Time(summary.Observation(summary.ObservationCount-1));
            int minimum=99,maximum=1;
            for(int i=0;i<summary.ObservationCount;i++){minimum=Mathf.Min(minimum,summary.Observation(i).Overall);maximum=Mathf.Max(maximum,summary.Observation(i).Overall);}
            minimum=Mathf.Max(1,minimum-4);maximum=Mathf.Min(99,maximum+4);
            Vector2 Point(AthleteRatingObservation r)=>new Vector2(area.x+area.width*(last>first?(Time(r)-first)/(last-first):.5f),area.yMax-area.height*(r.Overall-minimum)/Mathf.Max(1,maximum-minimum));
            painter.lineWidth=1;painter.strokeColor=new Color(.69f,.77f,.82f,.15f);
            for(int i=0;i<=3;i++){float y=area.y+i*area.height/3;painter.BeginPath();painter.MoveTo(new Vector2(area.x,y));painter.LineTo(new Vector2(area.xMax,y));painter.Stroke();}
            painter.lineWidth=3;painter.lineCap=LineCap.Round;painter.lineJoin=LineJoin.Round;painter.strokeColor=FrostlineStyle.Accent;painter.BeginPath();
            for(int i=0;i<summary.ObservationCount;i++){Vector2 p=Point(summary.Observation(i));if(i==0)painter.MoveTo(p);else painter.LineTo(p);}painter.Stroke();
            for(int i=0;i<summary.ObservationCount;i++)
            {
                var r=summary.Observation(i);bool peak=r.Overall==summary.PeakOverall;Vector2 p=Point(r);
                painter.fillColor=peak?FrostlineStyle.Gold:r.Current?FrostlineStyle.Ink:FrostlineStyle.Accent;painter.BeginPath();painter.Arc(p,peak?5:r.Current?4:3,Angle.Degrees(0),Angle.Degrees(360));painter.Fill();
            }
        }
        private float Time(AthleteRatingObservation r)=>r.Season+(r.Week-1)/(float)weeks;
    }
    internal sealed class CareerMilestoneMedal:VisualElement
    {
        internal CareerMilestoneMedal(bool earned)
        {
            style.width=30;style.height=32;style.flexShrink=0;style.marginRight=12;pickingMode=PickingMode.Ignore;
            generateVisualContent+=context=>
            {
                var p=context.painter2D;var color=earned?FrostlineStyle.Gold:FrostlineStyle.Muted;
                p.fillColor=new Color(color.r,color.g,color.b,earned ? .65f : .25f);p.BeginPath();p.MoveTo(new Vector2(6,4));p.LineTo(new Vector2(13,4));p.LineTo(new Vector2(19,17));p.LineTo(new Vector2(11,20));p.ClosePath();p.Fill();
                p.BeginPath();p.MoveTo(new Vector2(17,4));p.LineTo(new Vector2(24,4));p.LineTo(new Vector2(19,20));p.LineTo(new Vector2(11,17));p.ClosePath();p.Fill();
                p.fillColor=earned?FrostlineStyle.Gold:FrostlineStyle.Surface;p.strokeColor=color;p.lineWidth=1.5f;p.BeginPath();p.Arc(new Vector2(15,22),7,Angle.Degrees(0),Angle.Degrees(360));p.Fill();p.Stroke();
            };
        }
    }
}
