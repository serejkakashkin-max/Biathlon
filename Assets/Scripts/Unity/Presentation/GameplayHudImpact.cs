using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class GameplayHud
    {
        private VisualElement impactFeedback,impactFigure,impactCircle,impactDot;
        private Label impactStatus,impactDistance;
        private const float ImpactDiagramSize=52;
        private long impactTick=-1;private int impactBout=-1;
        private string impactActor;private ShotImpactPresentation impactSource;
        private void CreateImpactFeedback()
        {
            impactTick=-1;impactBout=-1;impactActor=null;impactSource=null;
            impactFeedback=new VisualElement{name="shot_impact_feedback"};impactFeedback.style.flexDirection=FlexDirection.Row;
            impactFeedback.style.marginTop=8;impactFeedback.style.paddingTop=6;impactFeedback.style.display=DisplayStyle.None;shootingPanel.Add(impactFeedback);
            impactFigure=new VisualElement{name="shot_impact_diagram"};impactFigure.style.width=impactFigure.style.height=ImpactDiagramSize;impactFigure.style.flexShrink=0;impactFigure.style.marginRight=10;
            impactCircle=new VisualElement();impactCircle.style.position=Position.Absolute;
            impactCircle.style.backgroundColor=FrostlineStyle.Deep;impactCircle.style.borderLeftWidth=impactCircle.style.borderTopWidth=impactCircle.style.borderRightWidth=impactCircle.style.borderBottomWidth=1;
            impactCircle.style.borderLeftColor=impactCircle.style.borderTopColor=impactCircle.style.borderRightColor=impactCircle.style.borderBottomColor=FrostlineStyle.Ink;
            impactDot=new VisualElement{name="shot_impact_dot"};impactDot.style.position=Position.Absolute;impactFigure.Add(impactCircle);impactFigure.Add(impactDot);impactFeedback.Add(impactFigure);
            var text=new VisualElement();text.style.flexShrink=1;impactFeedback.Add(text);
            impactStatus=Text(text,"shot_impact_status","",settings.caption_size,true);
            impactDistance=Text(text,"shot_impact_distance","",settings.caption_size);impactDistance.style.color=FrostlineStyle.Muted;
        }
        private void RefreshImpactFeedback(Biathlon.Simulation.AthletePresentationState athlete)
        {
            if(impactSource==null)impactSource=GetComponent<ShotImpactPresentation>();var source=impactSource;var evidence=source?.Ledger?.Last(athlete.AthleteId)??default;
            bool visible=Model.Shooting&&evidence.Valid&&evidence.Bout==athlete.CompletedBouts;
            impactFeedback.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
            if(!visible){impactTick=-1;return;}
            if(impactTick==evidence.Tick&&impactBout==evidence.Bout&&impactActor==evidence.AthleteId)return;impactTick=evidence.Tick;impactBout=evidence.Bout;impactActor=evidence.AthleteId;
            impactStatus.text=evidence.Outcome==Biathlon.Simulation.ShotOutcome.AlreadyClosed?L("Уже закрыта","Already closed"):
                evidence.ClosedTarget?(evidence.NearEdge?L("Габарит · попадание","Edge hit"):L("Попадание","Target hit")):
                evidence.NearEdge?L("Промах у кромки","Edge miss"):L("Промах","Miss");
            var color=evidence.ClosedTarget?FrostlineStyle.Accent:FrostlineStyle.Danger;impactStatus.style.color=color;impactDot.style.backgroundColor=color;
            impactFigure.style.display=evidence.Impact.ReachesPlane?DisplayStyle.Flex:DisplayStyle.None;
            if(!evidence.Impact.ReachesPlane){impactDistance.text=L("Вне мишени","Off target");return;}
            var geometry=source.Geometry;
            double radius=geometry.Radius(evidence.Stance),dx=evidence.Impact.X-geometry.CenterX(evidence.NearestTarget),dy=evidence.Impact.Y;
            double bullet=source.Definition.bullet_diameter_m,extent=Math.Max(radius+bullet*1.5,Math.Max(Math.Abs(dx),Math.Abs(dy))+bullet);
            float half=ImpactDiagramSize*.5f,scale=(float)(half/extent),diameter=(float)(radius*2)*scale,dot=(float)bullet*scale;
            impactCircle.style.width=impactCircle.style.height=diameter;impactCircle.style.left=impactCircle.style.top=half-diameter*.5f;
            impactCircle.style.borderTopLeftRadius=impactCircle.style.borderTopRightRadius=impactCircle.style.borderBottomLeftRadius=impactCircle.style.borderBottomRightRadius=diameter*.5f;
            impactDot.style.width=impactDot.style.height=dot;impactDot.style.left=half+(float)dx*scale-dot*.5f;impactDot.style.top=half-(float)dy*scale-dot*.5f;
            impactDot.style.borderTopLeftRadius=impactDot.style.borderTopRightRadius=impactDot.style.borderBottomLeftRadius=impactDot.style.borderBottomRightRadius=dot*.5f;
            impactDistance.text=L("Выстрел ","Shot ")+evidence.Ordinal+L(" · край "," · edge ")+Math.Abs(evidence.EdgeClearanceMeters*1000).ToString("F1")+L(" мм"," mm");
        }
    }
}
