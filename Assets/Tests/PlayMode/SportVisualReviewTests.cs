using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Career;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class SportVisualReviewTests
    {
        private sealed class Store:ICareerProfileStore
        {private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;public void WriteNew(string id,string value)=>json=value;public void Replace(string id,string expected,string value){Assert.That(json,Is.EqualTo(expected));json=value;}}
        private GameObject root,race;private FrontEndMenu menu;
        [SetUp] public void Setup(){root=new GameObject("Sport visual acceptance");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());menu.Session.OpenNewCareer();menu.Session.CreateCareer("visual_review","Алекс","NOR",11);}
        [TearDown] public void Cleanup(){if(race!=null)Object.DestroyImmediate(race);Object.DestroyImmediate(root);}
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/visual-review-20261012"));
        private IEnumerator Capture(PanelSettings panel,Action prepare,string file,int width=1600,int height=900,Action inspect=null)
        {
            var rt=new RenderTexture(width,height,0);rt.Create();var old=panel.targetTexture;var active=RenderTexture.active;Texture2D pixels=null;
            try
            {
                panel.targetTexture=rt;prepare();for(int i=0;i<6;i++)yield return null;
                RenderTexture.active=rt;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                Directory.CreateDirectory(Folder);File.WriteAllBytes(Path.Combine(Folder,file+".png"),pixels.EncodeToPNG());
                if(!file.StartsWith("timing")&&!file.StartsWith("race-panels"))AssertArtworkPaints(pixels);
                inspect?.Invoke();
            }
            finally{RenderTexture.active=active;panel.targetTexture=old;if(pixels!=null)Object.DestroyImmediate(pixels);Object.DestroyImmediate(rt);}
        }
        private void AssertArtworkPaints(Texture2D pixels)
        {
            var bounds=menu.Root.worldBound;int checkedImages=0;
            menu.Root.Query<Image>().ForEach(image=>
            {
                if(image.name!="ski_product_image"&&image.name!="venue_preview")return;
                for(var p=(VisualElement)image;p!=null;p=p.parent)if(p.resolvedStyle.display==DisplayStyle.None)return;
                var r=image.worldBound;if(r.width<10||r.height<10||r.yMax>bounds.yMax)return;
                float min=1,max=0;
                for(int y=1;y<10;y++)for(int x=1;x<10;x++)
                {
                    int px=Mathf.Clamp(Mathf.RoundToInt((r.xMin+r.width*x/10-bounds.xMin)/bounds.width*pixels.width),0,pixels.width-1);
                    int py=Mathf.Clamp(Mathf.RoundToInt(pixels.height-(r.yMin+r.height*y/10-bounds.yMin)/bounds.height*pixels.height),0,pixels.height-1);
                    float v=pixels.GetPixel(px,py).grayscale;min=Mathf.Min(min,v);max=Mathf.Max(max,v);
                }
                Assert.That(max-min,Is.GreaterThan(.15f),image.name+" must actually paint art, not a blank clipping mask.");checkedImages++;
            });
            Assert.That(checkedImages,Is.GreaterThan(0));
        }
        private void Fits(string id)
        {
            var element=menu.Root.Q(id);Assert.That(element,Is.Not.Null,id);var scroll=element.GetFirstAncestorOfType<ScrollView>();
            Assert.That(element.worldBound.yMax,Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax+1),id+" must fit without scrolling.");
            Assert.That(element.worldBound.xMax,Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.xMax+1));
        }
        [UnityTest] public IEnumerator ShopArtworkMatchesCatalogAndPurchaseSurvivesRedesign()
        {
            menu.Session.OpenCareerPage(FrontEndScreen.Equipment);
            var panel=root.GetComponent<UIDocument>().panelSettings;
            yield return Capture(panel,()=>menu.ApplyPreferences("ru",0),"shop-ru",inspect:()=>
            {
                for(int i=0;i<menu.Session.EquipmentCatalog.Count;i++)
                {
                    var item=menu.Session.EquipmentCatalog.Get(i);var image=menu.Root.Q("ski_art_"+item.Id).Q<Image>();
                    Assert.That(image.image,Is.SameAs(Resources.Load<Texture2D>("Biathlon/UI/Art/"+item.Id+"_v1")));
                    Assert.That(image.worldBound.height,Is.GreaterThan(100));Fits("equip_"+item.Id);
                }
            });
            yield return Capture(panel,()=>menu.ApplyPreferences("en",2),"shop-en-large",1024,768,()=>Fits("equipment_tiles_next"));
            menu.ApplyPreferences("ru",0);yield return null;var buy=menu.Root.Q<Button>("equip_ski_glide");Assert.That(buy.enabledInHierarchy,Is.True);
            buy.Focus();using(var submit=NavigationSubmitEvent.GetPooled())buy.SendEvent(submit);yield return null;
            Assert.That(menu.Session.Player.SelectedSki,Is.EqualTo("ski_glide"));Assert.That(menu.Root.Q<Button>("equip_ski_glide").enabledSelf,Is.False);
        }
        [UnityTest] public IEnumerator CalendarAndPreRaceUseSharedVenueArtWithAccessiblePagination()
        {
            var before=menu.Session.Career.ToJson();var panel=root.GetComponent<UIDocument>().panelSettings;
            yield return Capture(panel,()=>menu.ApplyPreferences("ru",0),"calendar-ru",inspect:()=>Fits("career_calendar_next"));
            var art=Resources.Load<Texture2D>("Biathlon/UI/Art/woodland_basin_v1");Assert.That(art,Is.Not.Null);
            var calendar=menu.Root.Q("career_calendar");Assert.That(calendar.childCount,Is.EqualTo(16));
            foreach(var entry in calendar.Children())Assert.That(entry.Q<Image>("venue_preview").image,Is.SameAs(art));
            yield return Capture(panel,()=>menu.ApplyPreferences("en",2),"calendar-en-large",1024,768,()=>Fits("career_calendar_next"));
            Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();
            yield return Capture(panel,()=>menu.ApplyPreferences("ru",0),"pre-race-ru",inspect:()=>Fits("pre_race_controls"));
            yield return Capture(panel,()=>menu.ApplyPreferences("en",2),"pre-race-en-large",1024,768,()=>Fits("pre_race_controls"));
            Assert.That(menu.Root.Q<Image>("venue_preview").image,Is.SameAs(art));
        }
        [UnityTest,Timeout(180000)] public IEnumerator ActualCheckpointTableBindsCareerFlagsAndPreservesRaceClock()
        {
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();var week=menu.Session.PreparedWeek;
            string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
            CareerCalendarEvent mass=null;for(int w=1;w<=52;w++){var e=menu.Session.Definitions.Calendar.AtWeek(w);if(e?.Format==CompetitionFormat.MassStart){mass=e;break;}}
            Assert.That(mass,Is.Not.Null);
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),mass,week.RaceSeed);
            race=new GameObject("Timing visuals acceptance");var host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);
            int steps=0;CheckpointStandings table=null;
            // Pass the first split in the actual deterministic career field, not fabricated standings.
            while(steps++<6000)
            {
                host.Runner.Step();
                if(steps%100!=0)continue;
                var s=RacePresentationSnapshot.Capture(host.Runner.Field);var player=s.Get(host.PlayerIndex);
                table=CheckpointStandings.Capture(host.Runner.Field,player.Hud.CheckpointOrdinal);
                if(table.Count>=20){host.Feed.Publish(s);break;}
            }
            Assert.That(table.Count,Is.GreaterThanOrEqualTo(20));var hud=host.GameplayHud;hud.SetBroadcastRoster(menu.Session.Career);
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var tick=host.Runner.Field.Tick;menu.SetRaceVisible(true);
            yield return Capture(panel,()=>{hud.SetPreferences("ru",0);hud.SetCheckpointTableVisible(true);},"timing-ru",inspect:()=>
            {
                for(int i=0;i<table.Count;i++)
                {
                    var row=hud.Root.Q<TimingAthleteRow>("checkpoint_row_"+i);Assert.That(row.AthleteId,Is.EqualTo(table.Get(i).AthleteId));
                    string nation=null;for(int a=0;a<menu.Session.Career.AthleteCount;a++)if(menu.Session.Career.Athlete(a).Id==row.AthleteId)nation=menu.Session.Career.Athlete(a).Nation;
                    Assert.That(row.Q<NationFlagGraphic>().Nation,Is.EqualTo(nation));Assert.That(row.worldBound.yMax,Is.LessThan(hud.Root.worldBound.yMax));
                }
            });
            yield return Capture(panel,()=>{hud.SetPreferences("en",2);hud.SetCheckpointTableVisible(true);},"timing-en-large",1024,768,()=>Assert.That(hud.Root.Q("checkpoint_table").worldBound.yMax,Is.LessThan(hud.Root.worldBound.yMax)));
            yield return Capture(panel,()=>{hud.SetPreferences("ru",0);hud.SetCheckpointTableVisible(false);},"race-panels-ru");
            Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick),"Presentation cannot advance the simulation.");Assert.That(hud.Root.Q("pulse_trace"),Is.Not.Null);
        }
    }
}
