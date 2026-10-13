using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Native UI design specimens, deliberately independent of production state.</summary>
    public sealed class FrostlineConceptTests
    {
        private GameObject host;
        private PanelSettings panel;
        private RenderTexture target;
        private Font display, body;
        private static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        private static readonly Color Ice=C("#E8F5F8"), Ink=C("#0B1928"), Glacier=C("#85D5DF"), Gold=C("#D6AA69");

        [UnityTest]
        public IEnumerator CaptureThreeDistinctNativeConceptPairs()
        {
            display=Resources.Load<Font>("Biathlon/UI/Fonts/Oswald");
            body=Resources.Load<Font>("Biathlon/UI/Fonts/Roboto");
            Assert.That(display,Is.Not.Null);Assert.That(body,Is.Not.Null);
            panel=Object.Instantiate(Resources.Load<PanelSettings>("Biathlon/UI/front_end_panel_v1"));
            panel.scaleMode=PanelScaleMode.ConstantPixelSize;panel.scale=1;
            target=new RenderTexture(1280,720,24);target.Create();panel.targetTexture=target;panel.clearColor=true;
            host=new GameObject("Frostline native DESIGN STUDY");
            var doc=host.AddComponent<UIDocument>();doc.panelSettings=panel;
            var root=doc.rootVisualElement;root.style.width=1280;root.style.height=720;root.style.unityFont=body;
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/ui/m14-wow/concepts"));Directory.CreateDirectory(directory);
            for(int variant=0;variant<3;variant++)
            for(int screen=0;screen<2;screen++)
            {
                root.Clear();root.style.backgroundColor=Ink;
                if(screen==0) Menu(root,variant);else Hud(root,variant);
                Stamp(root,variant,screen==0?"MENU / CAREER":"RACE HUD");
                for(int frame=0;frame<4;frame++)yield return null;
                Assert.That(root.worldBound.width,Is.EqualTo(1280).Within(1));
                var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
                try { image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(directory,((char)('a'+variant))+"-"+(screen==0?"menu":"hud")+".png"),image.EncodeToPNG()); }
                finally { RenderTexture.active=previous;Object.DestroyImmediate(image); }
            }
        }
        [TearDown] public void Cleanup()
        {
            if(host!=null)Object.DestroyImmediate(host);
            if(panel!=null){panel.targetTexture=null;Object.DestroyImmediate(panel);}
            if(target!=null){target.Release();Object.DestroyImmediate(target);}
        }
        private VisualElement Box(VisualElement p,float x,float y,float w,float h,Color color)
        {
            var v=new VisualElement();v.style.position=Position.Absolute;v.style.left=x;v.style.top=y;v.style.width=w;v.style.height=h;v.style.backgroundColor=color;p.Add(v);return v;
        }
        private Label Text(VisualElement p,string value,float x,float y,float w,float h,int size,Color color,bool title=false)
        {
            var t=new Label(value);t.style.position=Position.Absolute;t.style.left=x;t.style.top=y;t.style.width=w;t.style.height=h;
            t.style.marginLeft=0;t.style.marginRight=0;t.style.marginTop=0;t.style.marginBottom=0;t.style.paddingLeft=0;t.style.paddingRight=0;t.style.paddingTop=0;t.style.paddingBottom=0;
            t.style.fontSize=size;t.style.color=color;t.style.unityFont=title?display:body;t.style.whiteSpace=WhiteSpace.Normal;p.Add(t);return t;
        }
        private void Picture(VisualElement p,string resource,float x,float y,float w,float h)
        {
            var image=new Image {image=Resources.Load<Texture2D>(resource),scaleMode=ScaleMode.ScaleAndCrop};
            Assert.That(image.image,Is.Not.Null,resource);image.style.position=Position.Absolute;image.style.left=x;image.style.top=y;image.style.width=w;image.style.height=h;p.Add(image);
        }
        private void Rule(VisualElement p,float x,float y,float w,Color color)=>Box(p,x,y,w,1,color);
        private void Button(VisualElement p,string label,float x,float y,float w,Color fill,Color foreground)
        { Box(p,x,y,w,54,fill);Text(p,label,x+20,y+10,w-40,36,23,foreground,true);Text(p,"→",x+w-43,y+11,32,34,24,foreground); }
        private void Stamp(VisualElement p,int variant,string screen)
        {
            Box(p,0,692,1280,28,Ink);
            Text(p,"DESIGN STUDY  /  "+new[]{"A · FROSTLINE BROADCAST","B · ALPINE STUDIO","C · NIGHT PURSUIT"}[variant]+"  /  "+screen,28,698,875,20,11,Glacier);
            Text(p,"SAMPLE DATA · NOT A LIVE CAREER",996,698,270,20,10,Ice);
        }
        private void Menu(VisualElement r,int variant)
        {
            if(variant==0)
            {
                Picture(r,"Biathlon/UI/menu_background",445,0,835,692);
                Box(r,0,0,475,692,Ink);Box(r,475,0,1,692,Glacier);
                Text(r,"BIATHLON",42,29,410,48,36,Ice,true);Text(r,"F R O S T L I N E",43,80,330,24,12,Glacier);
                Rule(r,42,122,350,C("#2A4353"));
                Text(r,"ТВОЙ СЕЗОН.",40,157,420,85,57,Ice,true);Text(r,"ТВОЙ СЛЕД.",40,229,420,85,57,Ice,true);
                Text(r,"Снег. Пульс. Пять точных выстрелов.",44,330,355,34,16,C("#B6CAD4"));
                Button(r,"ПРОДОЛЖИТЬ КАРЬЕРУ",42,391,350,Glacier,Ink);
                Text(r,"Новая карьера",44,472,350,36,21,Ice);Text(r,"Свободная гонка",44,519,350,36,21,Ice);Text(r,"Настройки",44,566,350,36,21,Ice);
                Box(r,516,498,720,153,C("#0B1928EB"));Box(r,516,498,5,153,Gold);
                Text(r,"СЛЕДУЮЩИЙ СТАРТ",541,516,340,24,12,Gold);
                Text(r,"WOODLAND BASIN",539,547,445,54,36,Ice,true);
                Text(r,"Масс-старт  /  30 спортсменов",542,605,500,25,16,C("#B6CAD4"));
                Text(r,"К ЭТАПУ  →",1040,561,180,42,23,Glacier,true);
            }
            else if(variant==1)
            {
                Box(r,0,0,1280,692,C("#EAF0F2"));Box(r,0,0,236,692,C("#F8FBFC"));
                Text(r,"BIATHLON",27,30,210,48,34,Ink,true);Text(r,"ALPINE STUDIO",29,80,190,22,11,C("#51727D"));
                string[] items={"Обзор сезона","Календарь","Подготовка","Экипировка","Результаты"};
                for(int i=0;i<items.Length;i++){if(i==0){Box(r,15,150,206,47,Ink);Box(r,15,150,4,47,C("#D85144"));}Text(r,items[i],32,162+i*61,185,34,17,i==0?Ice:Ink);}
                Text(r,"Настройки  /  Выход",28,627,200,30,13,C("#51727D"));
                Text(r,"ДЕНЬ ГОНКИ",276,32,400,64,43,Ink,true);Text(r,"Обзор карьеры · макет данных",279,95,720,27,14,C("#51727D"));
                Picture(r,"Biathlon/UI/Art/woodland_basin_v1",278,152,579,290);
                Box(r,278,381,579,61,C("#0B1928E8"));Text(r,"WOODLAND BASIN",299,388,510,48,31,Ice,true);
                Box(r,880,152,358,290,C("#FFFFFF"));Text(r,"ПРОФИЛЬ ТРАССЫ",903,173,311,36,21,Ink,true);
                Course(r,913,230,283,139,C("#D85144"));Text(r,"Схема текущей трассы",903,406,307,23,12,C("#51727D"));
                Box(r,278,465,302,173,C("#FFFFFF"));Text(r,"ПОДГОТОВКА",298,484,270,29,17,C("#51727D"),true);Text(r,"Всё к старту",298,520,270,44,28,Ink,true);Text(r,"Лыжи  /  смазка  /  винтовка",298,592,266,25,13,C("#51727D"));
                Box(r,601,465,637,173,Ink);Text(r,"МАСС-СТАРТ",625,484,570,38,27,Ice,true);Text(r,"Проверь снаряжение и отправляйся на старт.",625,528,570,32,15,C("#B6CAD4"));Button(r,"К ГОНКЕ",925,568,288,C("#D85144"),Color.white);
            }
            else
            {
                Picture(r,"Biathlon/UI/menu_background",0,0,1280,692);Box(r,0,0,1280,692,C("#161C353B"));
                Box(r,0,0,1280,112,C("#11162CEB"));Text(r,"BIATHLON",45,25,500,60,42,Ice,true);Text(r,"NIGHT PURSUIT",1024,48,232,24,13,C("#EAC47F"));
                Box(r,48,153,397,371,C("#11162CEB"));Text(r,"СЕЗОН НАЧИНАЕТСЯ\nС ОДНОГО СТАРТА",69,179,359,148,41,Ice,true);
                Rule(r,73,343,315,C("#616986"));Text(r,"Woodland Basin",72,363,322,41,27,Ice,true);Text(r,"Готовность к следующей гонке",74,411,314,26,14,C("#CDD1E0"));Button(r,"ПРОДОЛЖИТЬ",72,452,320,C("#EAC47F"),C("#11162C"));
                Box(r,195,572,890,75,C("#11162CF0"));Text(r,"КАРЬЕРА",229,591,173,40,24,C("#EAC47F"),true);Text(r,"ГОНКА",439,591,153,40,24,Ice,true);Text(r,"КОЛЛЕКЦИЯ",626,591,235,40,24,Ice,true);Text(r,"НАСТРОЙКИ",864,591,207,40,24,Ice,true);
                Box(r,229,641,124,3,C("#EAC47F"));
            }
        }
        private void Hud(VisualElement r,int variant)
        {
            Picture(r,"Biathlon/UI/Art/woodland_basin_v1",0,0,1280,692);
            Text(r,"КОНЦЕПТ HUD · ПРЕВЬЮ ТРАССЫ",467,327,430,30,14,C("#102631"));
            if(variant==0)
            {
                var dark=C("#0B1928E8");Box(r,26,25,250,78,dark);Box(r,26,25,4,78,Glacier);
                Text(r,"МАСС-СТАРТ",43,35,217,24,12,Glacier);Text(r,"2 / 5",43,59,110,35,25,Ice,true);Text(r,"КРУГ",113,72,59,20,11,C("#B6CAD4"));Text(r,"12:48.6",165,61,100,34,24,Ice,true);
                Box(r,1031,25,223,91,dark);Text(r,"ПОСЛЕДНЯЯ ОТСЕЧКА",1047,35,201,20,10,C("#B6CAD4"));Text(r,"07",1046,57,60,51,36,Ice,true);Text(r,"+ 08.4",1118,64,125,36,26,Gold,true);
                Box(r,26,533,293,131,dark);Text(r,"24.8",43,543,157,69,47,Ice,true);Text(r,"КМ/Ч",170,577,84,22,12,C("#B6CAD4"));Text(r,"ТЕМП",43,615,74,21,11,Glacier);Bar(r,104,619,183,6,.72f,Glacier);Text(r,"W / S",250,642,55,19,10,C("#B6CAD4"));
                Box(r,1024,547,230,117,dark);Text(r,"♥",1040,558,45,44,34,Gold);Text(r,"164",1084,551,150,60,39,Ice,true);Text(r,"ПУЛЬС · ПОВЫШЕН",1041,615,201,26,11,Gold);Bar(r,1041,646,192,3,.68f,Gold);
                Box(r,26,107,172,3,Glacier);Box(r,198,107,78,3,C("#406476"));
            }
            else if(variant==1)
            {
                var light=C("#F8FBFCF2");Box(r,30,27,489,60,light);Text(r,"МАСС-СТАРТ",48,43,181,28,18,Ink,true);Text(r,"2 / 5",247,41,107,32,23,Ink,true);Text(r,"12:48.6",366,41,134,32,23,Ink,true);Box(r,30,84,489,3,C("#D85144"));
                Box(r,1021,27,229,92,light);Text(r,"ОТСЕЧКА",1038,37,188,21,11,C("#51727D"));Text(r,"7 место",1038,61,129,40,27,Ink,true);Text(r,"+08.4",1153,68,90,31,20,C("#C84639"),true);
                Box(r,30,533,214,128,light);Text(r,"ПУЛЬС",48,544,171,21,11,C("#51727D"));Text(r,"164",49,570,139,56,39,Ink,true);Text(r,"♥",164,580,60,47,31,C("#C84639"));Bar(r,48,642,178,5,.67f,C("#C84639"));
                Box(r,967,543,283,118,light);Text(r,"24.8",986,550,176,61,43,Ink,true);Text(r,"КМ/Ч",1123,586,113,22,12,C("#51727D"));Text(r,"ТЕМП",986,622,81,20,11,C("#51727D"));Bar(r,1048,628,177,6,.72f,C("#3A8192"));
            }
            else
            {
                var dark=C("#11162CEA");var amber=C("#EAC47F");Box(r,383,25,514,68,dark);
                Text(r,"2 / 5",405,42,99,35,24,Ice,true);Text(r,"12:48.6",561,36,153,43,30,Ice,true);Text(r,"07  +08.4",755,44,133,32,21,amber,true);
                Box(r,333,566,614,98,dark);Rule(r,354,566,572,amber);Text(r,"24.8",352,579,160,66,44,Ice,true);Text(r,"КМ/Ч",479,612,66,21,11,C("#CDD1E0"));
                Text(r,"ТЕМП",552,582,96,23,11,C("#CDD1E0"));Bar(r,552,620,168,6,.72f,amber);Text(r,"♥",752,592,51,47,30,amber);Text(r,"164",802,580,118,61,39,Ice,true);
                Text(r,"TAB  ОТСЕЧКА",37,32,204,24,12,Ice);Text(r,"МАСС-СТАРТ",1050,32,217,24,12,Ice);
            }
        }
        private void Bar(VisualElement r,float x,float y,float w,float h,float fraction,Color color)
        {Box(r,x,y,w,h,C("#49617166"));Box(r,x,y,w*fraction,h,color);}
        [Serializable] private sealed class VisualData { public LoopData[] loops; }
        [Serializable] private sealed class LoopData { public string loop_id; public Knot[] knots; }
        [Serializable] private sealed class Knot { public float x,z; }
        private void Course(VisualElement root,float x,float y,float w,float h,Color color)
        {
            var data=JsonUtility.FromJson<VisualData>(Resources.Load<TextAsset>("Biathlon/Venues/woodland_basin_v1/visual").text);
            var knots=Array.Find(data.loops,l=>l.loop_id=="main").knots;
            float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;
            foreach(var k in knots){minX=Mathf.Min(minX,k.x);maxX=Mathf.Max(maxX,k.x);minZ=Mathf.Min(minZ,k.z);maxZ=Mathf.Max(maxZ,k.z);}
            var points=new Vector2[knots.Length];float scale=Mathf.Min((w-10)/(maxX-minX),(h-10)/(maxZ-minZ));
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(5+(knots[i].x-minX)*scale,5+(maxZ-knots[i].z)*scale);
            var v=Box(root,x,y,w,h,Color.clear);v.generateVisualContent+=ctx=>{var p=ctx.painter2D;p.strokeColor=color;p.lineWidth=3;p.BeginPath();p.MoveTo(points[0]);for(int i=1;i<points.Length;i++)p.LineTo(points[i]);p.ClosePath();p.Stroke();};
        }
    }
}
