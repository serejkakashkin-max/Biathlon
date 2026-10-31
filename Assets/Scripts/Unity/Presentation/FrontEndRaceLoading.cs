using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class FrontEndMenu
    {
        private void RenderRaceLoading()
        {
            Root.style.paddingTop=Root.style.paddingBottom=Root.style.paddingLeft=Root.style.paddingRight=0;
            var screen=new VisualElement{name="race_loading_screen"};screen.style.flexGrow=1;screen.style.justifyContent=Justify.SpaceBetween;
            screen.style.backgroundColor=FrostlineStyle.Deep;Root.Add(screen);
            var image=SportArt.Image("woodland_basin_v1",0,"race_loading_backdrop");
            image.style.position=Position.Absolute;image.style.left=image.style.right=image.style.top=image.style.bottom=0;image.style.width=image.style.height=Length.Percent(100);screen.Add(image);
            var tint=new VisualElement{pickingMode=PickingMode.Ignore};tint.style.position=Position.Absolute;tint.style.left=tint.style.right=tint.style.top=tint.style.bottom=0;tint.style.backgroundColor=FrontEndTheme.Color("#0B19284D");screen.Add(tint);
            var brand=new Label("BIATHLON  /  FROSTLINE"){pickingMode=PickingMode.Ignore};FrostlineStyle.Text(brand,true);brand.style.fontSize=20*Scale;brand.style.letterSpacing=2;brand.style.color=FrostlineStyle.Ink;
            brand.style.marginLeft=brand.style.marginTop=32;screen.Add(brand);
            var card=new VisualElement{name="race_loading_card"};
            card.style.backgroundColor=HighContrast?Color.black:FrontEndTheme.Color("#0B1928F5");card.style.paddingLeft=card.style.paddingRight=compactLayout?24:40;card.style.paddingTop=28;card.style.paddingBottom=28;
            card.style.borderTopWidth=2;card.style.borderTopColor=FrostlineStyle.Accent;screen.Add(card);
            var content=new VisualElement();content.style.maxWidth=1100;content.style.width=Length.Percent(100);content.style.alignSelf=Align.Center;card.Add(content);
            var eventRow=new VisualElement();eventRow.style.flexDirection=FlexDirection.Row;eventRow.style.alignItems=Align.Center;eventRow.style.marginBottom=8;content.Add(eventRow);
            var marker=new VisualElement();marker.style.width=5;marker.style.height=14;marker.style.marginRight=9;marker.style.backgroundColor=FrostlineStyle.Accent;eventRow.Add(marker);
            LoadingText(eventRow,"race_loading_event",T("format."+(int)Session.PreparedWeek.Event.Format).ToUpperInvariant()+"  /  WOODLAND BASIN",theme.caption_size,FrostlineStyle.Accent);
            LoadingText(content,"race_loading_title",L("ГОТОВИМ ГОНКУ","PREPARING YOUR RACE"),compactLayout?36:48,FrostlineStyle.Ink,true);
            var venue=LoadingText(content,"race_loading_venue",T("woodland"),theme.body_size,FrostlineStyle.Muted);venue.style.marginTop=2;venue.style.marginBottom=22;
            var statusRow=new VisualElement();statusRow.style.flexDirection=FlexDirection.Row;statusRow.style.alignItems=Align.Center;statusRow.style.flexWrap=Wrap.Wrap;content.Add(statusRow);
            var targets=new VisualElement{name="race_loading_targets"};targets.style.flexDirection=FlexDirection.Row;targets.style.marginRight=18;targets.style.marginBottom=5;statusRow.Add(targets);
            var dots=new VisualElement[5];
            for(int i=0;i<dots.Length;i++)
            {
                var dot=new VisualElement{pickingMode=PickingMode.Ignore};dot.style.width=dot.style.height=12;dot.style.marginRight=6;
                dot.style.borderTopLeftRadius=dot.style.borderTopRightRadius=dot.style.borderBottomLeftRadius=dot.style.borderBottomRightRadius=6;
                dot.style.borderTopWidth=dot.style.borderRightWidth=dot.style.borderBottomWidth=dot.style.borderLeftWidth=1;
                dot.style.borderTopColor=dot.style.borderRightColor=dot.style.borderBottomColor=dot.style.borderLeftColor=FrostlineStyle.Accent;
                dot.style.backgroundColor=i==0?FrostlineStyle.Accent:Color.clear;targets.Add(dot);dots[i]=dot;
            }
            string stage=Race.LoadingStage==CareerRaceLoadingStage.Preparation?L("Готовим состав и условия гонки…","Preparing the field and race conditions…"):
                Race.LoadingStage==CareerRaceLoadingStage.Athletes?L("Спортсмены готовятся к старту…","Athletes are getting ready…"):
                Race.LoadingStage==CareerRaceLoadingStage.Venue?L("Готовим трассу и стрельбище…","Preparing the course and shooting range…"):
                L("Всё готово. Выходим на старт…","Ready. Heading to the start…");
            var status=LoadingText(statusRow,"race_loading_status",stage,theme.body_size,FrostlineStyle.Ink);status.style.marginBottom=5;
            var note=LoadingText(content,"race_loading_note",L("После загрузки — обратный отсчёт перед вашим стартом.","A countdown will prepare you for your start after loading."),theme.caption_size,FrostlineStyle.Muted);note.style.marginTop=14;
            screen.pickingMode=PickingMode.Position;screen.focusable=true;screen.Focus();
            if(!ReducedMotion)
            {
                int active=0;backdropMotion=targets.schedule.Execute(()=>
                {dots[active].style.backgroundColor=Color.clear;active=(active+1)%dots.Length;dots[active].style.backgroundColor=FrostlineStyle.Accent;}).Every(260);
            }
        }
        private Label LoadingText(VisualElement parent,string name,string value,int size,Color color,bool display=false)
        {
            var label=new Label(value){name=name,pickingMode=PickingMode.Ignore};FrostlineStyle.Text(label,display);label.style.fontSize=size*Scale;
            label.style.color=color;label.style.whiteSpace=WhiteSpace.Normal;label.style.marginLeft=label.style.marginRight=label.style.marginTop=label.style.marginBottom=0;parent.Add(label);return label;
        }
    }
}
