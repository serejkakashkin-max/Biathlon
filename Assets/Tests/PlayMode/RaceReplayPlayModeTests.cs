using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RaceReplayPlayModeTests
    {
        private sealed class Store:ICareerProfileStore
        {
            private string json;
            public bool HasProfile=>json!=null;
            public string ReadLatest()=>json;
            public void WriteNew(string id,string value)=>json=value;
            public void Replace(string id,string previous,string value){Assert.That(json,Is.EqualTo(previous));json=value;}
        }
        private GameObject root;
        private FrontEndMenu menu;
        private string folder;
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        [TearDown]public void Cleanup()
        {
            if(root!=null)Object.DestroyImmediate(root);
            if(Directory.Exists(folder)){foreach(var f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder,false);}
        }
        [UnityTest]public IEnumerator ReplaySeeksBothWaysWithIndependentVisibleUiAndNeverWritesCareer()=>CheckReplay(0);
        [UnityTest]public IEnumerator ReplayKeepsTheRecordedBroadcastCheckpointVersion()=>CheckReplay(1);
        private IEnumerator CheckReplay(int checkpointVersion)
        {
            folder=Path.Combine(Path.GetTempPath(),"biathlon-viewer-"+Guid.NewGuid().ToString("N"));var library=new RaceReplayLibrary(folder);
            root=new GameObject("Replay navigation test");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store(),library);
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("viewer_test","Алекс","NOR",11);
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();var week=menu.Session.PreparedWeek;
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource)
                .Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed,broadcastCheckpointVersion:checkpointVersion);
            var source=ManualProductionFieldComposition.Create(prepared.Runner.Field,prepared.Venue,prepared.ManualRead,prepared.CourseReactionPolicy);
            prepared.Runner.BindManualScopedCandidate(source,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")),completeSnowSearch:prepared.AiSnowSearchVersion==1);
            var info=new RaceReplayInfo{Id="navigation_fixture",RecordedUtc=DateTime.UtcNow.ToString("O"),CareerSnapshotJson=week.RaceSnapshotJson,RaceWeatherJson=prepared.Weather.ToJson(),StartContinuationVersion=prepared.Runner.Field.StartContinuationVersion,WeatherTimelineVersion=prepared.Timeline==null?0:CareerRaceProvider.CurrentWeatherTimelineVersion,WeatherTimelineJson=prepared.Timeline?.ToJson(),EquipmentVersion=CareerRaceProvider.CurrentEquipmentVersion,EquipmentSnapshotJson=prepared.Equipment.ToJson(),StartOrderVersion=CareerRaceProvider.CurrentStartOrderVersion,CourseReactionVersion=(int)prepared.CourseReactionPolicy,EventId=week.Event.Id,VenueId=week.Event.VenueId,Format=(int)week.Event.Format,Seed=week.RaceSeed,PlayerId=menu.Session.Career.PlayerId,PlayerName="Алекс",TickFrequency=prepared.Runner.Field.TickFrequency,TickCount=1000,ResultSha256=new string('a',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=new[]{new RaceHighlight(200,menu.Session.Career.PlayerId,RaceHighlightKind.Overtake)}};
            info.AiSnowSearchVersion=prepared.Runner.AiSnowSearchVersion;
            info.BroadcastCheckpointVersion=checkpointVersion;
            object pose=null;
            using(var file=library.CreatePending(info.Id))using(var writer=new ManualRaceJournalWriter(file,source,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            {
                source.CommandObserver=writer;for(int i=0;i<1000;i++){prepared.Runner.Step();if(i==199)pose=source.World.GetActor(0).Pose;}writer.Complete();Assert.That(writer.Failure,Is.Null);
            }
            library.Publish(info);menu.Session.Back();menu.Session.OpenCareerPage(FrontEndScreen.Replays);menu.RefreshFrontEnd();yield return null;
            string before=menu.Session.Career.ToJson();menu.Replays.LoadEnvironment=true;menu.Replays.Launch(menu,info);
            for(int i=0;i<20&&menu.Replays.Loading;i++)yield return null;
            var replay=menu.Replays;Assert.That(replay.Active,Is.True,menu.LastMessage);Assert.That(replay.LastFailure,Is.Null);
            Assert.That(replay.Host.Runner.Field.Track.CheckpointCount,Is.EqualTo(checkpointVersion==0?3:7));
            for(int i=0;i<replay.Host.Runner.Field.Count;i++)
            {
                string id=replay.Host.Runner.Field.GetAthlete(i).AthleteId.Value;CareerAthlete athlete=null;
                for(int j=0;j<menu.Session.Career.AthleteCount;j++)if(menu.Session.Career.Athlete(j).Id==id)athlete=menu.Session.Career.Athlete(j);
                Assert.That(athlete,Is.Not.Null);
                Assert.That(replay.Host.Proxies.GetRig(id).GetComponent<AthleteAppearancePresentation>().Nation,Is.EqualTo(athlete.Nation));
            }
            replay.TogglePause();Assert.That(replay.Paused,Is.True);yield return null;
            Assert.That(menu.Root.style.display.value,Is.EqualTo(DisplayStyle.None));Assert.That(replay.Root.panel,Is.Not.Null);
            Assert.That(replay.Root.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));Assert.That(replay.Root.worldBound.width,Is.GreaterThan(500));
            Assert.That(replay.Host.Proxies.enabled,Is.False);Assert.That(replay.Host.GameplayHud.enabled,Is.False);Assert.That(replay.Host.enabled,Is.False);
            replay.SetSpeed(4);replay.SetCamera("wide");replay.SelectAthlete(1);replay.SetCamera("wide");
            replay.Seek(200);for(int i=0;i<180&&replay.Seeking;i++)yield return null;
            Assert.That(replay.Tick,Is.EqualTo(200),replay.LastFailure);Assert.That(replay.LastFailure,Is.Null);
            Assert.That(replay.Host.Feed.Current.Tick,Is.EqualTo(200));
            replay.Seek(50);for(int i=0;i<180&&replay.Seeking;i++)yield return null;
            Assert.That(replay.Tick,Is.EqualTo(50));replay.Seek(200);for(int i=0;i<180&&replay.Seeking;i++)yield return null;
            Assert.That(replay.Host.Proxies.GetRig(menu.Session.Career.PlayerId).GetComponent<AthleteAppearancePresentation>().Nation,Is.EqualTo("NOR"));
            Assert.That(replay.Tick,Is.EqualTo(200));Assert.That(replay.LastFailure,Is.Null);
            var playback=(ManualScopedFieldStepper)typeof(RaceReplayPresentation).GetField("stepper",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(replay);
            Assert.That(playback.World.GetActor(0).Pose,Is.EqualTo(pose));
            Assert.That(replay.Host.Runner.TacticalDecisions,Is.Zero);
            yield return CaptureUi(replay,1600,900);yield return CaptureUi(replay,1024,768);
            var pause=replay.Root.Q<Button>("replay_pause");pause.Focus();using(var e=NavigationMoveEvent.GetPooled(Vector2.down))pause.SendEvent(e);yield return null;
            Assert.That((replay.Root.panel.focusController.focusedElement as Button)?.name,Is.EqualTo("replay_back"));
            replay.Close();yield return null;Assert.That(menu.Session.Career.ToJson(),Is.EqualTo(before));Assert.That(menu.Session.Screen,Is.EqualTo(FrontEndScreen.Replays));
            Assert.That(menu.Root.style.display.value,Is.EqualTo(DisplayStyle.Flex));Assert.That(library.List().Length,Is.EqualTo(1));
        }
        private IEnumerator CaptureUi(RaceReplayPresentation replay,int width,int height)
        {
            var doc=replay.Root;var ui=Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            PanelSettings panel=null;foreach(var candidate in ui)if(candidate.rootVisualElement==doc)panel=candidate.panelSettings;
            Assert.That(panel,Is.Not.Null);var target=new RenderTexture(width,height,24);target.Create();panel.targetTexture=target;
            var previous=RenderTexture.active;Texture2D pixels=null;
            try
            {
                yield return null;yield return null;yield return null;
                var toolbar=doc.Q("replay_controls");Assert.That(toolbar.worldBound.xMax,Is.LessThanOrEqualTo(doc.worldBound.xMax));Assert.That(toolbar.worldBound.yMax,Is.LessThanOrEqualTo(doc.worldBound.yMax));
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                string evidence=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m14-wow/after"));Directory.CreateDirectory(evidence);File.WriteAllBytes(Path.Combine(evidence,"replay-ui-"+width+".png"),pixels.EncodeToPNG());
            }
            finally{panel.targetTexture=null;RenderTexture.active=previous;if(pixels!=null)Object.Destroy(pixels);Object.Destroy(target);}
        }
    }
}
