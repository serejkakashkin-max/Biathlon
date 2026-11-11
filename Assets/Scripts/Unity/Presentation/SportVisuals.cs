using UnityEngine;
using UnityEngine.UIElements;
using Biathlon.Career;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Presentation assets only; shared textures are loaded when a page is built, never on race ticks.</summary>
    internal static class SportArt
    {
        internal static Image Image(string id,float height,string name)
        {
            var image=new Image {name=name,image=Resources.Load<Texture2D>("Biathlon/UI/Art/"+id),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};
            image.style.height=height;image.style.flexShrink=0;image.style.width=Length.Percent(100);
            return image;
        }
    }

    /// <summary>Exact vector flags for the supported career nations; no emoji/font dependency.</summary>
    public sealed class NationFlagGraphic:VisualElement
    {
        public string Nation {get;private set;}
        public NationFlagGraphic(string nation)
        {
            name="nation_flag";style.width=27;style.height=18;style.flexShrink=0;style.marginRight=9;
            pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;SetNation(nation);
        }
        public void SetNation(string nation)
        {if(Nation==nation)return;Nation=nation??"";tooltip=Nation;MarkDirtyRepaint();}
        private void Draw(MeshGenerationContext context)
        {
            var p=context.painter2D;float w=contentRect.width,h=contentRect.height;
            void Rect(float x,float y,float width,float height,string hex)
            {p.fillColor=FrontEndTheme.Color(hex);p.BeginPath();p.MoveTo(new Vector2(x,y));p.LineTo(new Vector2(x+width,y));p.LineTo(new Vector2(x+width,y+height));p.LineTo(new Vector2(x,y+height));p.ClosePath();p.Fill();}
            switch(Nation)
            {
                case "NOR":Rect(0,0,w,h,"#BA0C2F");Rect(w*6/22,0,w*4/22,h,"#FFFFFF");Rect(0,h*6/16,w,h*4/16,"#FFFFFF");Rect(w*7/22,0,w*2/22,h,"#00205B");Rect(0,h*7/16,w,h*2/16,"#00205B");break;
                case "SWE":Rect(0,0,w,h,"#006AA7");Rect(w*5/16,0,w*2/16,h,"#FECC00");Rect(0,h*4/10,w,h*2/10,"#FECC00");break;
                case "FIN":Rect(0,0,w,h,"#FFFFFF");Rect(w*5/18,0,w*3/18,h,"#002F6C");Rect(0,h*4/11,w,h*3/11,"#002F6C");break;
                case "RUS":Rect(0,0,w,h/3,"#FFFFFF");Rect(0,h/3,w,h/3,"#0039A6");Rect(0,h*2/3,w,h/3,"#D52B1E");break;
                case "GER":Rect(0,0,w,h/3,"#151515");Rect(0,h/3,w,h/3,"#DD0000");Rect(0,h*2/3,w,h/3,"#FFCE00");break;
                case "FRA":case "ITA":Rect(0,0,w/3,h,Nation=="FRA"?"#002395":"#009246");Rect(w/3,0,w/3,h,"#FFFFFF");Rect(w*2/3,0,w/3,h,"#ED2939");break;
                default:Rect(0,0,w,h,"#69818D");Rect(3,h/2-1,w-6,2,"#DCE8ED");break;
            }
        }
    }

    /// <summary>Retained split row with stable columns and a roster-backed national flag.</summary>
    public sealed class TimingAthleteRow:VisualElement
    {
        private readonly Label rank,athlete,time,gap;private readonly NationFlagGraphic flag;
        private readonly LeaderBibGraphic leader;
        public string AthleteId {get;private set;}
        public TimingAthleteRow(float scale,bool broadcast=false)
        {
            style.flexDirection=FlexDirection.Row;style.alignItems=Align.Center;style.height=31*scale;style.marginBottom=2;
            style.paddingLeft=6;style.paddingRight=8;style.borderBottomWidth=1;style.borderBottomColor=new Color(1,1,1,.07f);
            rank=Cell(30*scale,true);rank.name="timing_rank";flag=new NationFlagGraphic("");Add(flag);athlete=Cell(0,true);athlete.name="timing_athlete";athlete.style.flexGrow=1;athlete.style.flexBasis=0;
            leader=new LeaderBibGraphic(CareerLeaderBib.None,"");leader.style.width=18*scale;leader.style.height=22*scale;leader.style.marginLeft=4*scale;leader.style.marginRight=5*scale;Add(leader);
            time=Cell(66*scale,false);gap=Cell(62*scale,true);time.name="timing_elapsed";gap.name="timing_gap";
            time.style.unityTextAlign=gap.style.unityTextAlign=TextAnchor.MiddleRight;time.style.color=new Color(.69f,.79f,.84f);
            if(broadcast)
            {
                leader.style.display=DisplayStyle.None;
                flag.style.width=21*scale;flag.style.height=13*scale;
                flag.style.marginRight=5*scale;
                FrostlineStyle.Text(rank,true);FrostlineStyle.Text(time,true);FrostlineStyle.Text(gap,true);
                rank.style.fontSize=time.style.fontSize=gap.style.fontSize=14*scale;
                athlete.style.fontSize=13*scale;
                rank.style.width=19*scale;
                rank.style.unityTextAlign=TextAnchor.MiddleLeft;
                time.style.width=gap.style.width=StyleKeyword.Auto;
                time.style.marginLeft=7*scale;
                gap.style.marginLeft=6*scale;
            }
            Label Cell(float width,bool bold)
            {
                var label=new Label{enableRichText=false};label.style.fontSize=13*scale;label.style.color=new Color(.93f,.97f,.98f);
                label.style.marginLeft=label.style.marginRight=0;label.style.paddingLeft=label.style.paddingRight=0;
                label.style.whiteSpace=WhiteSpace.NoWrap;label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;
                label.style.width=width;label.style.flexShrink=0;label.style.minWidth=0;
                if(bold)label.style.unityFontStyleAndWeight=FontStyle.Bold;Add(label);return label;
            }
        }
        internal void SetBroadcastGapVisible(bool visible) => gap.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void Bind(string id,int place,string name,string nation,string elapsed,string interval,bool player,CareerLeaderBib leaderRole=CareerLeaderBib.None,string leaderDescription="")
        {
            AthleteId=id;rank.text=place.ToString("00");athlete.text=name;time.text=elapsed;gap.text=interval;flag.SetNation(nation);
            leader.SetRole(leaderRole,leaderDescription);
            tooltip=place+" · "+name+" · "+nation+" · "+elapsed+" · "+interval;
            style.backgroundColor=player?new Color(.12f,.48f,.51f,.55f):place%2==0?new Color(1,1,1,.025f):Color.clear;
            style.borderLeftWidth=3;style.borderLeftColor=player?new Color(.41f,.84f,.85f):Color.clear;
            gap.style.color=player?new Color(.55f,.95f,.92f):Color.white;
        }
        public void BindCheckpoint(string id,int place,string name,string nation,double elapsed,double secondsBehind,string locale,bool player,CareerLeaderBib leaderRole=CareerLeaderBib.None,string leaderDescription="")
        {
            var culture=System.Globalization.CultureInfo.GetCultureInfo(locale=="ru"?"ru-RU":"en-US");
            Bind(id,place,name,nation,Biathlon.Presentation.GameplayHudModel.FormatClock(elapsed),secondsBehind==0?(locale=="ru"?"ЛИДЕР":"LEADER"):Biathlon.Presentation.GameplayHudModel.FormatGap(secondsBehind,culture),player,leaderRole,leaderDescription);
        }
    }
}
