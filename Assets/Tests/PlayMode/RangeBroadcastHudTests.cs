using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.AI;
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
    public sealed class RangeBroadcastHudTests
    {
        private sealed class Store:ICareerProfileStore
        {private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;public void WriteNew(string id,string value)=>json=value;public void Replace(string id,string expected,string value){Assert.That(json,Is.EqualTo(expected));json=value;}}
        private GameObject root,race;
        [TearDown]public void Cleanup(){if(race!=null)Object.DestroyImmediate(race);if(root!=null)Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        [UnityTest]public IEnumerator DisabledBroadcastHudIgnoresFeedUntilItsDocumentIsReenabled()
        {
            var venue=PathPresentationFixture.Plan();var profile=CompetitionProfile.FromJson(Read("sprint_test_v1.json").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("broadcast_lifecycle",venue.Track.Id,"main",profile.ToRouteRequirements());
            var snow=RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json").Replace("snow_test_packed","packed"));
            race=new GameObject("Disabled broadcast lifecycle");var host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=false;host.Initialize(venue,profile,route,snow,count:1);
            var hud=host.GameplayHud;hud.Refresh();int refreshes=hud.RefreshCount;
            hud.enabled=false;yield return null;Assert.That(hud.Root,Is.Null);
            for(int i=0;i<3;i++){host.Runner.Step();Assert.DoesNotThrow(()=>host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field)),"Hidden HUD must not render into its released UIDocument during replay.");Assert.DoesNotThrow(()=>hud.Refresh());}
            Assert.That(hud.RefreshCount,Is.EqualTo(refreshes));
            hud.enabled=true;yield return null;Assert.That(hud.Root,Is.Not.Null);Assert.That(hud.RefreshCount,Is.GreaterThan(refreshes));
            host.Runner.Step();Assert.DoesNotThrow(()=>host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field)));
            hud.enabled=false;hud.enabled=true;yield return null;
            host.Runner.Step();Assert.DoesNotThrow(()=>host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field)));
        }
        [UnityTest,Timeout(240000)]public IEnumerator ActualCareerMassStartShowsLiveQueueFlagsMissFlashAndRetainedSeriesWithoutPaging()
        {
            root=new GameObject("Range board isolated career");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("range_board_test","Алекс Северин","NOR",11);
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();var week=menu.Session.PreparedWeek;
            CareerCalendarEvent mass=null;for(int w=1;w<=52;w++){var e=menu.Session.Definitions.Calendar.AtWeek(w);if(e?.Format==CompetitionFormat.MassStart){mass=e;break;}}
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),mass,week.RaceSeed);
            race=new GameObject("Range board production field");var host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);
            var hud=host.GameplayHud;hud.SetBroadcastRoster(menu.Session.Career);host.Proxies.ConfigureCareerAppearance(menu.Session.Career,menu.Session.Definitions.Calendar,menu.Session.Definitions.Scoring,mass);
            menu.gameObject.SetActive(false);bool opened=false,flashed=false,completed=false,closed=false,sharedApproach=false;int ticks=0,aimingFrames=0;
            while(ticks++<16000&&!closed)
            {
                // Exercise a real miss through normal player aim/trigger/bolt
                // commands. An AI seed no longer guarantees a miss in this
                // particular first-window view after versioned tactical changes.
                var actor=host.Runner.Field.GetAthlete(host.PlayerIndex);var next=host.Runner.Field.Tick.Next();var shooting=actor.ActiveShooting;
                aimingFrames=shooting?.Phase==ShootingPhase.Aiming?aimingFrames+1:0;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,actor.AthleteId,1000),1,1,0),
                    new WeaponAimCommand(next,actor.AthleteId,0,1000),aimingFrames>=40,false,host.Runner.RaceWind,cycleBolt:shooting?.NeedsBoltCycle==true,manualBoltCycle:true);
                host.Runner.StepWithManualPlayer(host.PlayerIndex,input,0,true);
                var s=RacePresentationSnapshot.Capture(host.Runner.Field);host.Feed.Publish(s);hud.Refresh();
                var player=s.Get(host.PlayerIndex);var row=hud.RangeBroadcast.Get(host.PlayerIndex);
                if(!opened&&hud.RangeBroadcastVisible){opened=true;Assert.That(player.RouteMeters,Is.GreaterThanOrEqualTo(505.553));}
                if(!sharedApproach&&hud.RangeBroadcastVisible&&!player.ShootingActive&&
                    hud.Root.Q("checkpoint_notice").style.display.value==DisplayStyle.Flex&&
                    hud.Root.Q("shooting_readiness").style.display.value==DisplayStyle.Flex)
                {
                    sharedApproach=true;
                    yield return Capture(hud,1920,1080,"ru",0,"simultaneous-1080-ru",true);
                    yield return Capture(hud,2560,1152,"ru",0,"simultaneous-wide-ru",true);
                    yield return Capture(hud,1024,768,"en",2,"simultaneous-4x3-large-en",true);
                    hud.SetPreferences("ru",0);
                }
                if(hud.RangeBroadcastVisible&&hud.RangeBroadcastRows>0&&!flashed)
                {
                    for(int i=0;i<hud.RangeBroadcastRows;i++)
                    {
                        var r=hud.RangeBroadcast.Get(hud.RangeBroadcast.Displayed(i));
                        if(!r.Flashing(s.Tick,s.TickFrequency,1))continue;
                        flashed=true;var strip=hud.Root.Q<RangeTargetStrip>("range_rival_targets_"+i);
                        Assert.That(strip.Misses,Is.GreaterThan(0));Assert.That(hud.Root.Q("range_rival_"+i).style.backgroundColor.value.r,Is.GreaterThan(.3));
                        Assert.That(hud.Root.Q<RangeStatusIcon>("range_rival_state_"+i).Complete,Is.EqualTo(!r.Active));
                        var flag=hud.Root.Q("range_rival_"+i).Q<NationFlagGraphic>();Assert.That(flag.Nation,Is.Not.Empty);
                        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
                        foreach(string locale in new[]{"ru","en"})yield return Capture(hud,resolution.x,resolution.y,locale,0,"live-"+resolution.x+"-"+locale);
                        yield return Capture(hud,1280,720,"en",2,"live-720-large");
                        hud.SetPreferences("ru",0);break;
                    }
                }
                if(player.CompletedBouts==1&&!player.ShootingActive&&!completed)
                {
                    completed=true;Assert.That(row.Shots,Is.EqualTo(5));Assert.That(row.ClosedMask,Is.EqualTo(player.LastCompletedBoutClosedMask));
                    Assert.That(row.Misses,Is.GreaterThan(0),"The ordinary off-target controls must produce an actual accepted miss.");
                    Assert.That(hud.RangeBroadcastVisible,Is.True,"Completed series stays visible after leaving the mat, including penalty transit.");
                }
                if(opened&&completed&&player.RouteMeters>1160.005){closed=true;Assert.That(hud.RangeBroadcastVisible,Is.False);}
                if(ticks%100==0)yield return null;
            }
            Assert.That(opened&&flashed&&completed&&closed,Is.True,$"opened={opened},flash={flashed},completed={completed},closed={closed},tick={ticks}");
            Assert.That(sharedApproach,Is.True,"The test must show split and breathing guidance together before the mat.");
            long before=host.Runner.Field.Tick.Value;for(int i=0;i<100;i++)hud.Refresh();Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(before));
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m16-2"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"actual-field.json"),"{\"schema_version\":1,\"id\":\"range_broadcast_mass30\",\"athletes\":"+host.Runner.Field.Count+",\"tick\":"+before+",\"automatic_window\":true,\"miss_flash\":true,\"completed_series_preserved\":true,\"authority_read_only\":true}");
        }
        private static IEnumerator Capture(GameplayHud hud,int width,int height,string locale,int size,string name,bool sharedApproach=false)
        {
            var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            var target=new RenderTexture(width,height,24);target.Create();LiveShootingPlayerReview.ConfigureCapturePanel(panel,target);Texture2D pixels=null;var prior=RenderTexture.active;
            try
            {
                hud.SetPreferences(locale,size);yield return null;yield return null;yield return null;
                var board=hud.Root.Q("range_broadcast");var bounds=board.worldBound;var screen=hud.Root.worldBound;
                Assert.That(bounds.xMin,Is.GreaterThan(screen.center.x+30));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(screen.xMax));
                Assert.That(bounds.yMax,Is.LessThan(hud.Root.Q("right_status_stack").worldBound.yMin-6));
                if(sharedApproach)
                {
                    var split=hud.Root.Q("checkpoint_notice");var readiness=hud.Root.Q("shooting_readiness");var pulse=hud.Root.Q("condition_panel");
                    Assert.That(split.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(readiness.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(split.worldBound.xMax,Is.LessThan(readiness.worldBound.xMin-4));
                    Assert.That(readiness.worldBound.yMax,Is.LessThan(pulse.worldBound.yMin-4));
                    Assert.That(readiness.worldBound.xMax,Is.EqualTo(pulse.worldBound.xMax).Within(.1f));
                    Assert.That(split.worldBound.xMin,Is.GreaterThan(hud.Root.Q("motion_panel").worldBound.xMax+4));
                }
                Assert.That(hud.Root.Q("help_hint"),Is.Null);Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(screen.yMin));
                board.Query<VisualElement>().ForEach(v=>Assert.That(v.pickingMode,Is.EqualTo(PickingMode.Ignore)));
                foreach(var strip in board.Query<RangeTargetStrip>().ToList())Assert.That(strip.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                if(width==1920&&size==0&&hud.Root.Q("shooting_readiness").resolvedStyle.display==DisplayStyle.None)
                    Assert.That(hud.RangeBroadcastCapacity,Is.EqualTo(20),"1080p uses all 20 rails when the readiness card is absent.");
                if(width==1280&&size==0)Assert.That(hud.RangeBroadcastCapacity,Is.LessThan(20));
                Assert.That(hud.RangeBroadcastRows,Is.LessThanOrEqualTo(hud.RangeBroadcastCapacity),"A resized board must recompose its retained rows even without a new accepted tick.");
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,sharedApproach?"../artifacts/m16-8-split-hud":"../docs/quality/m16-2"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally{panel.targetTexture=null;RenderTexture.active=prior;if(pixels!=null)Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
