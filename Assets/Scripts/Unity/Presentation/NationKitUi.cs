using Biathlon.Career;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>The same bib fabric palette as the actual athlete; no simulated state.</summary>
    public sealed class LeaderBibGraphic:VisualElement
    {
        public CareerLeaderBib Role {get;private set;}
        public LeaderBibGraphic(CareerLeaderBib role,string description)
        {
            name="leader_bib";pickingMode=PickingMode.Ignore;
            style.width=20;style.height=24;style.flexShrink=0;style.marginRight=6;
            generateVisualContent+=Draw;SetRole(role,description);
        }
        public void SetRole(CareerLeaderBib role,string description)
        {
            if(role<CareerLeaderBib.None||role>(CareerLeaderBib.Total|CareerLeaderBib.Discipline))throw new System.ArgumentOutOfRangeException(nameof(role));
            style.display=role==CareerLeaderBib.None?DisplayStyle.None:DisplayStyle.Flex;
            if(tooltip!=description)tooltip=description;
            if(Role==role)return;Role=role;MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext context)
        {
            var p=context.painter2D;float w=contentRect.width,h=contentRect.height;
            void Outline(bool rightOnly)
            {
                p.BeginPath();p.MoveTo(new Vector2((rightOnly?.5f:.34f)*w,(rightOnly?.23f:.04f)*h));
                if(!rightOnly){p.LineTo(new Vector2(.34f*w,.18f*h));p.LineTo(new Vector2(.5f*w,.23f*h));}
                p.LineTo(new Vector2(.66f*w,.18f*h));p.LineTo(new Vector2(.66f*w,.04f*h));p.LineTo(new Vector2(.84f*w,.04f*h));
                p.LineTo(new Vector2(.8f*w,.27f*h));p.LineTo(new Vector2(.96f*w,.42f*h));p.LineTo(new Vector2(.9f*w,.96f*h));
                p.LineTo(new Vector2((rightOnly?.5f:.1f)*w,.96f*h));
                if(!rightOnly){p.LineTo(new Vector2(.04f*w,.42f*h));p.LineTo(new Vector2(.2f*w,.27f*h));p.LineTo(new Vector2(.16f*w,.04f*h));}
                p.ClosePath();
            }
            p.fillColor=AthleteBibPresentation.FabricColor(Role);Outline(false);p.Fill();
            if(Role==(CareerLeaderBib.Total|CareerLeaderBib.Discipline)){p.fillColor=AthleteBibPresentation.FabricColor(CareerLeaderBib.Discipline);Outline(true);p.Fill();}
            p.strokeColor=new Color(1,1,1,.7f);p.lineWidth=.75f;Outline(false);p.Stroke();
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
            AddLabel(chip,nation,theme.caption_size,true).style.marginBottom=0;
            chip.tooltip=nation;
        }
        private void AddPreRaceKit(VisualElement parent,CareerCalendarEvent race,CareerAthlete player)
        {
            var row=new VisualElement{name="pre_race_kit"};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;parent.Add(row);
            AddNationalKit(row,player.Nation);
            if(race.Format==Biathlon.Domain.CompetitionFormat.MassStart)
            {
                var starts=CareerMassStartSeeding.Capture(Session.Career,Session.Definitions.Calendar,Session.Definitions.Scoring,race);
                int index=System.Array.IndexOf(starts,player.Id);
                if(index>=0){var position=AddLabel(row,L("Старт № ","Start bib ")+(index+1),theme.caption_size,true);position.name="pre_race_start_position";position.style.marginLeft=12;position.style.marginBottom=0;}
            }
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
