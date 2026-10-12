using Biathlon.Career;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>The same bib fabric palette as the actual athlete; no simulated state.</summary>
    public sealed class LeaderBibGraphic:VisualElement
    {
        public CareerLeaderBib Role {get;}
        public LeaderBibGraphic(CareerLeaderBib role,string description)
        {
            Role=role;name="leader_bib";tooltip=description;pickingMode=PickingMode.Ignore;
            style.width=20;style.height=24;style.flexShrink=0;style.marginRight=6;
            generateVisualContent+=Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            var p=context.painter2D;float w=contentRect.width,h=contentRect.height;
            void Half(float left,float right,Color color)
            {
                p.fillColor=color;p.BeginPath();p.MoveTo(new Vector2(left*w,0));p.LineTo(new Vector2(right*w,0));
                p.LineTo(new Vector2(right*w,h));p.LineTo(new Vector2(left*w,h));p.ClosePath();p.Fill();
            }
            Half(.1f,.9f,AthleteBibPresentation.FabricColor(Role));
            if(Role==(CareerLeaderBib.Total|CareerLeaderBib.Discipline))Half(.5f,.9f,AthleteBibPresentation.FabricColor(CareerLeaderBib.Discipline));
            p.fillColor=new Color(.04f,.1f,.14f);p.BeginPath();p.MoveTo(new Vector2(.36f*w,0));p.LineTo(new Vector2(.64f*w,0));
            p.LineTo(new Vector2(.59f*w,.18f*h));p.LineTo(new Vector2(.41f*w,.18f*h));p.ClosePath();p.Fill();
            p.strokeColor=new Color(.04f,.1f,.14f);p.lineWidth=2;p.BeginPath();p.MoveTo(new Vector2(.5f*w,.4f*h));p.LineTo(new Vector2(.5f*w,.78f*h));p.Stroke();
        }
    }

    public sealed partial class FrontEndMenu
    {
        private string LeaderDescription(CareerLeaderBib role)=>role==(CareerLeaderBib.Total|CareerLeaderBib.Discipline)?
            L("Лидер общего зачёта и дисциплины","Overall and discipline leader"):
            (role&CareerLeaderBib.Total)!=0?L("Лидер общего зачёта","Overall leader"):L("Лидер дисциплины","Discipline leader");
        private void AddNationalKit(VisualElement parent,string nation)
        {
            var chip=new VisualElement{name="national_kit_"+nation};chip.style.flexDirection=FlexDirection.Row;chip.style.alignItems=Align.Center;
            chip.style.marginRight=14;chip.style.height=30;chip.style.flexShrink=0;parent.Add(chip);
            chip.Add(new NationFlagGraphic(nation));
            var swatch=new VisualElement{name="kit_swatch"};swatch.style.width=10;swatch.style.height=18;
            swatch.style.backgroundColor=AthleteAppearancePresentation.NationColor(nation);swatch.style.marginRight=6;
            swatch.style.borderLeftWidth=swatch.style.borderRightWidth=swatch.style.borderTopWidth=swatch.style.borderBottomWidth=1;
            swatch.style.borderLeftColor=swatch.style.borderRightColor=swatch.style.borderTopColor=swatch.style.borderBottomColor=ink;
            chip.Add(swatch);AddLabel(chip,nation,theme.caption_size,true).style.marginBottom=0;
            chip.tooltip=L("Цвет национального комбинезона","National suit colour")+" · "+nation;
        }
        private void AddPreRaceKit(VisualElement parent,CareerCalendarEvent race,CareerAthlete player)
        {
            var row=new VisualElement{name="pre_race_kit"};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;parent.Add(row);
            AddNationalKit(row,player.Nation);
            var role=CareerLeaderBibs.Capture(Session.Career,Session.Definitions.Calendar,Session.Definitions.Scoring,race).ForAthlete(player.Id);
            if(role==CareerLeaderBib.None)return;
            row.Add(new LeaderBibGraphic(role,LeaderDescription(role)));
            var description=AddLabel(row,LeaderDescription(role),theme.caption_size,true);description.style.marginBottom=0;
            description.style.flexShrink=1;description.style.minWidth=0;
        }
        private void AddNationalKitLegend(VisualElement parent)
        {
            var row=new VisualElement{name="national_kit_legend"};row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;row.style.marginBottom=8;parent.Add(row);
            for(int i=0;i<AthleteAppearancePresentation.NationCount;i++)AddNationalKit(row,AthleteAppearancePresentation.NationAt(i));
        }
        private CareerLeaderBibs NextStartLeaders()
        {
            var calendar=Session.Definitions.Calendar;
            for(int i=0;i<calendar.Count;i++)if(calendar.Get(i).Week>=Session.Career.Week)
                return CareerLeaderBibs.Capture(Session.Career,calendar,Session.Definitions.Scoring,calendar.Get(i));
            return null;
        }
    }
}
