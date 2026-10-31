using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Two retained vector layers: course paints on layout changes, only actors repaint on observations.</summary>
    public sealed class RaceMiniMapGraphic:VisualElement
    {
        private readonly RaceMiniMapGeometry geometry;
        private readonly RaceMiniMapSettings settings;
        private readonly bool contrast;
        private readonly ActorLayer actors;
        public RaceMiniMapState State{get;}
        public int CoursePaints{get;private set;}
        public int ActorPaints=>actors.Paints;
        public RaceMiniMapGraphic(RaceMiniMapGeometry geometry,RaceMiniMapSettings settings,RaceMiniMapState state,bool highContrast)
        {
            this.geometry=geometry;this.settings=settings;State=state;contrast=highContrast;name="minimap_course";
            pickingMode=PickingMode.Ignore;style.overflow=Overflow.Hidden;
            generateVisualContent+=Draw;
            actors=new ActorLayer(this){name="minimap_actors"};actors.style.position=Position.Absolute;
            actors.style.left=actors.style.right=actors.style.top=actors.style.bottom=0;Add(actors);
        }
        public void Observe()=>actors.MarkDirtyRepaint();
        public Vector2 Project(Vector2 point)=>geometry.Project(point,new Rect(0,0,contentRect.width,contentRect.height),settings.padding);
        public Vector2 MarkerPosition(int index)
        {
            var p=Project(State.Get(index).Point);float edge=settings.player_radius+2;
            return new Vector2(Mathf.Clamp(p.x,edge,Mathf.Max(edge,contentRect.width-edge)),Mathf.Clamp(p.y,edge,Mathf.Max(edge,contentRect.height-edge)));
        }
        private void Draw(MeshGenerationContext context)
        {
            if(contentRect.width<1||contentRect.height<1)return;
            CoursePaints++;var p=context.painter2D;
            p.lineCap=LineCap.Round;p.lineJoin=LineJoin.Round;
            // Quiet cartographic grid; no terrain render, texture, blur or extra world pass.
            p.lineWidth=1;p.strokeColor=new Color(FrostlineStyle.Accent.r,FrostlineStyle.Accent.g,FrostlineStyle.Accent.b,contrast?.13f:.055f);p.BeginPath();
            for(int i=1;i<4;i++){float x=contentRect.width*i/4,y=contentRect.height*i/4;p.MoveTo(new Vector2(x,0));p.LineTo(new Vector2(x,contentRect.height));p.MoveTo(new Vector2(0,y));p.LineTo(new Vector2(contentRect.width,y));}p.Stroke();
            foreach(var line in geometry.Lines)
            {
                Stroke(line,settings.course_width+3,new Color(0,0,0,.46f));
                Stroke(line,line.Penalty?1.7f:settings.course_width,line.Penalty?FrostlineStyle.Gold:contrast?Color.white:new Color(.58f,.72f,.77f,.76f));
            }
            var left=Project(geometry.RangeLeft);var right=Project(geometry.RangeRight);
            p.lineWidth=3;p.strokeColor=FrostlineStyle.Gold;p.BeginPath();p.MoveTo(left);p.LineTo(right);p.Stroke();
            for(int i=0;i<5;i++)Circle(p,Vector2.Lerp(left,right,(i+.5f)/5),1.3f,FrostlineStyle.Deep);
            var finish=Project(geometry.Finish);
            for(int x=0;x<3;x++)for(int y=0;y<3;y++)
            {
                p.fillColor=(x+y)%2==0?FrostlineStyle.Ink:FrostlineStyle.Deep;var a=finish+new Vector2((x-1.5f)*2.4f,(y-1.5f)*2.4f);
                p.BeginPath();p.MoveTo(a);p.LineTo(a+Vector2.right*2.4f);p.LineTo(a+Vector2.one*2.4f);p.LineTo(a+Vector2.up*2.4f);p.ClosePath();p.Fill();
            }
            void Stroke(RaceMiniMapLine line,float width,Color color)
            {p.lineWidth=width;p.strokeColor=color;p.BeginPath();for(int i=0;i<line.Points.Length;i++){var a=Project(line.Points[i]);if(i==0)p.MoveTo(a);else p.LineTo(a);}p.Stroke();}
        }
        private static void Circle(Painter2D p,Vector2 at,float radius,Color color)
        {p.fillColor=color;p.BeginPath();p.Arc(at,radius,0,360);p.ClosePath();p.Fill();}
        private sealed class ActorLayer:VisualElement
        {
            private readonly RaceMiniMapGraphic map;
            public int Paints{get;private set;}
            public ActorLayer(RaceMiniMapGraphic owner){map=owner;pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
            private void Draw(MeshGenerationContext context)
            {
                Paints++;if(map.State.Count==0||map.contentRect.width<1)return;var p=context.painter2D;var state=map.State;
                for(int i=0;i<state.Count;i++)
                {
                    var actor=state.Get(i);if(actor.Player||!actor.Visible)continue;
                    var at=map.MarkerPosition(i);float radius=map.settings.rival_radius;
                    Circle(p,at,radius+1,FrostlineStyle.Deep);
                    var color=actor.Shooting?FrostlineStyle.Gold:FrostlineStyle.Ink;
                    if(actor.Waiting)
                    {p.strokeColor=new Color(color.r,color.g,color.b,.55f);p.lineWidth=1;p.BeginPath();p.Arc(at,radius,0,360);p.ClosePath();p.Stroke();}
                    else Circle(p,at,radius,color);
                }
                if(state.PlayerIndex<0)return;
                var player=state.Get(state.PlayerIndex);var center=map.MarkerPosition(state.PlayerIndex);float r=map.settings.player_radius;
                // Draw last, with both shape and colour, so a dense start pack never hides the player.
                Circle(p,center,r+3,new Color(FrostlineStyle.Accent.r,FrostlineStyle.Accent.g,FrostlineStyle.Accent.b,.16f));
                var forward=player.Direction.normalized;var side=new Vector2(-forward.y,forward.x);
                p.fillColor=FrostlineStyle.Accent;p.strokeColor=map.contrast?Color.white:FrostlineStyle.Deep;p.lineWidth=1.5f;
                p.BeginPath();p.MoveTo(center+forward*r*1.3f);p.LineTo(center-forward*r*.8f+side*r*.8f);p.LineTo(center-forward*r*.38f);p.LineTo(center-forward*r*.8f-side*r*.8f);p.ClosePath();p.Fill();p.Stroke();
            }
        }
    }
}
