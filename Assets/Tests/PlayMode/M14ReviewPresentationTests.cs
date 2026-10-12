using System;
using System.Collections;
using System.IO;
using Biathlon.Career;
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
    public sealed class M14ReviewPresentationTests
    {
        private sealed class Store:ICareerProfileStore
        {private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;public void WriteNew(string id,string value)=>json=value;public void Replace(string id,string expected,string value){Assert.That(json,Is.EqualTo(expected));json=value;}}
        private GameObject menuObject,raceObject;private FrontEndMenu menu;
        [SetUp] public void Setup()
        {menuObject=new GameObject("Review menu test");menu=menuObject.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());menu.Session.OpenNewCareer();menu.Session.CreateCareer("review_ui","Алекс","NOR",11);}
        [TearDown] public void Cleanup(){if(raceObject!=null)Object.DestroyImmediate(raceObject);Object.DestroyImmediate(menuObject);}
        private IEnumerator Capture(UIDocument document,string name,int width=1600,int height=900)
        {
            var rt=new RenderTexture(width,height,0);rt.Create();document.panelSettings.targetTexture=rt;
            yield return null;yield return null;yield return null;
            foreach(string id in new[]{"career_calendar_next","training_tiles_next","equipment_tiles_next"})
            {
                var pager=menu.Root.Q<Button>(id);if(pager==null)continue;
                var viewport=pager.GetFirstAncestorOfType<ScrollView>();
                Assert.That(pager.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax-8),id+" must fit without scrolling.");
            }
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14/review-20261012"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            Object.Destroy(image);RenderTexture.active=old;document.panelSettings.targetTexture=null;Object.Destroy(rt);
        }
        [UnityTest] public IEnumerator RedesignedPagesUseCardsPaginationAndRetainEveryEvent()
        {
            menu.RefreshFrontEnd();yield return Capture(menuObject.GetComponent<UIDocument>(),"dashboard-modern");
            Assert.That(menu.Root.Q("career_calendar").childCount,Is.EqualTo(16));
            Assert.That(menu.Root.Q<Button>("career_calendar_next").worldBound.yMax,Is.LessThan(menu.Root.worldBound.yMax));
            foreach(var page in new[]{FrontEndScreen.Training,FrontEndScreen.Equipment})
            {menu.Session.OpenCareerPage(page);menu.RefreshFrontEnd();yield return Capture(menuObject.GetComponent<UIDocument>(),page.ToString().ToLowerInvariant()+"-modern");menu.Back();}
            menu.ApplyPreferences("en",2);yield return Capture(menuObject.GetComponent<UIDocument>(),"dashboard-modern-large-4x3",1024,768);
            Assert.That(menu.Root.Q<Button>("career_calendar_next").worldBound.yMax,Is.LessThan(menu.Root.worldBound.yMax));
            menu.Root.Query<ScrollView>().ForEach(scroll=>Assert.That(scroll.verticalScrollerVisibility,Is.EqualTo(ScrollerVisibility.Hidden)));
        }
        [UnityTest,Timeout(240000)] public IEnumerator FinishedPlayerCanWatchOthersAndCompleteTheSameAuthorityField()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            raceObject=new GameObject("Post-finish review");var host=raceObject.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_route")),RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),3);
            Assert.Throws<InvalidOperationException>(()=>host.RequestFastFinish());
            int guard=0;while(!host.Runner.Field.GetAthlete(host.PlayerIndex).HasFinished&&guard++<1000)
            {for(int i=0;i<64&&!host.Runner.Field.GetAthlete(host.PlayerIndex).HasFinished;i++)host.Runner.Step();yield return null;}
            Assert.That(host.Runner.Field.GetAthlete(host.PlayerIndex).HasFinished,Is.True);Assert.That(host.Runner.Field.IsComplete,Is.False);
            host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));host.Feed.SetInterpolationAlpha(1);yield return null;
            var spectator=raceObject.AddComponent<LiveRaceSpectator>();spectator.Initialize(host,menu);yield return null;
            Assert.That(spectator.SelectedIndex,Is.Not.EqualTo(host.PlayerIndex));Assert.That(host.ChaseCamera.enabled,Is.False);Assert.That(host.GameplayHud.enabled,Is.False);
            var dropdown=spectator.Root.Q<DropdownField>("spectator_camera");dropdown.value=dropdown.choices[1];yield return null;Assert.That(spectator.CameraMode,Is.EqualTo(1));
            yield return Capture(GetSpectatorDocument(spectator),"post-finish-overhead");
            host.RequestFastFinish();guard=0;while(!host.Runner.Field.IsComplete&&guard++<3000){host.AdvanceFinishedFieldFrame();yield return null;}
            Assert.That(host.HasSimulationFault,Is.False);Assert.That(host.Runner.Field.IsComplete,Is.True);Assert.That(host.Runner.Field.Result.Count,Is.EqualTo(3));
        }
        private static UIDocument GetSpectatorDocument(LiveRaceSpectator spectator)
        {foreach(var doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))if(doc.rootVisualElement==spectator.Root)return doc;throw new InvalidOperationException("Missing spectator document.");}
    }
}
