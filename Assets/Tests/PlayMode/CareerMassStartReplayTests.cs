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
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class CareerMassStartReplayTests
    {
        private GameObject root;private string folder;
        private static string Read(string n)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(n));
        private sealed class Store:ICareerProfileStore
        {
            public string Json;public bool HasProfile=>true;public string ReadLatest()=>Json;
            public void WriteNew(string id,string value)=>throw new InvalidOperationException();
            public void Replace(string id,string expected,string value)=>throw new InvalidOperationException();
        }
        [TearDown]public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);if(folder!=null&&Directory.Exists(folder)){foreach(var f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder,false);}}
        [UnityTest,Timeout(300000)]public IEnumerator RankedIceMassReplayAndBackwardSeekKeepRecordedSolverAndStartList()=>Run(false);
        [UnityTest,Timeout(300000)]public IEnumerator LegacyV2CareerReplayKeepsRecordedSolverPosesAndOriginalSnapshotBytes()=>Run(true);
        [UnityTest,Timeout(300000)]public IEnumerator VersionOneWeatherJournalRetainsOriginalContextAndBackwardSeek()=>Run(false,true);
        [UnityTest,Timeout(300000)]public IEnumerator VersionOneStartCorridorJournalRetainsOriginalContextAndBackwardSeek()=>Run(false,false,true);
        [UnityTest,Timeout(300000)]public IEnumerator MovingOvertakePolicyAndBackwardSeekRestoreRecordedVersion()=>Run(false,movingPass:true);
        [UnityTest,Timeout(300000)]public IEnumerator MatApproachMarginAndBackwardSeekRestoreRecordedVersion()=>Run(false,movingPass:true,matMargin:true);
        private IEnumerator Run(bool legacyV2,bool oldWeather=false,bool oldStart=false,bool movingPass=false,bool matMargin=false)
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var s=CareerFactory.Create(d.Roster,d.Calendar,"mass_seek","Alex","NOR","default",new SeededRandomSource(71000));
            for(int e=0;e<2;e++)CareerStandings.Register(s,new CareerRaceRecord("seek_result_"+e,s.SeasonId,d.Calendar.Get(e).Id,d.Calendar.Get(e).Format,123,new string('a',64),
                new[]{new CareerFinish(s.Athlete(1).Id,300000,300000),new CareerFinish(s.Athlete(2).Id,301000,301000),new CareerFinish(s.PlayerId,302000,302000)}));
            string snapshot=s.ToJson();
            if(legacyV2)
            {
                snapshot=System.Text.RegularExpressions.Regex.Replace(snapshot,"\"appearance_profile\":\\{[^{}]*\\},","");
                snapshot=System.Text.RegularExpressions.Regex.Replace(snapshot,"(\"save_version\"\\s*:\\s*)[0-9]+","${1}2");
                Assert.That(snapshot,Does.Not.Contain("appearance_profile"));
            }
            var race=d.Calendar.Get(2);var p=new CareerRaceProvider(Read,d,ProductionVenueHost.ReadResource).Prepare(s,race,123,legacyStartContinuation:oldWeather,legacyWeatherHeading:oldWeather,startContinuationVersion:oldStart?1:2,legacyAiSnowSearch:oldWeather||oldStart);
            var source=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,p.ManualRead);
            int policyVersion=matMargin?2:movingPass?1:0;
            p.Runner.BindManualScopedCandidate(source,ManualAiSteeringDefinition.FromJson(Read(ManualAiSteeringDefinition.ResourceForVersion(policyVersion))),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")),completeSnowSearch:p.AiSnowSearchVersion==1);
            folder=Path.Combine(Path.GetTempPath(),"biathlon-ranked-mass-replay-"+Guid.NewGuid().ToString("N"));var library=new RaceReplayLibrary(folder);var store=new Store{Json=snapshot};
            var info=new RaceReplayInfo{Id="ranked_mass",RecordedUtc=DateTime.UtcNow.ToString("O"),CareerSnapshotJson=snapshot,RaceWeatherJson=p.Weather.ToJson(),StartContinuationVersion=p.Runner.Field.StartContinuationVersion,WeatherTimelineVersion=p.Timeline==null?0:oldWeather?1:CareerRaceProvider.CurrentWeatherTimelineVersion,WeatherTimelineJson=p.Timeline?.ToJson(),EquipmentVersion=CareerRaceProvider.CurrentEquipmentVersion,EquipmentSnapshotJson=p.Equipment.ToJson(),StartOrderVersion=1,CourseReactionVersion=(int)p.CourseReactionPolicy,EventId=race.Id,VenueId=race.VenueId,Format=(int)race.Format,Seed=123,PlayerId=s.PlayerId,PlayerName="Alex",TickFrequency=20,TickCount=120,ResultSha256=new string('a',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()};
            info.AiSnowSearchVersion=p.Runner.AiSnowSearchVersion;
            info.MovingOvertakeVersion=p.Runner.MovingOvertakeVersion;
            object pose=null;
            using(var stream=library.CreatePending(info.Id))using(var writer=new ManualRaceJournalWriter(stream,source,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            {source.CommandObserver=writer;for(int i=0;i<120;i++){p.Runner.Step();if(i==59)pose=source.World.GetActor(p.PlayerIndex).Pose;}writer.Complete();Assert.That(writer.Failure,Is.Null);}
            library.Publish(info);root=new GameObject("Ranked Mass replay");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(store,library);
            menu.Session.ContinueCareer();menu.Session.OpenCareerPage(FrontEndScreen.Replays);menu.RefreshFrontEnd();menu.Replays.LoadEnvironment=false;menu.Replays.Launch(menu,info);
            for(int i=0;i<60&&menu.Replays.Loading;i++)yield return null;
            var replay=menu.Replays;Assert.That(replay.Active,Is.True,menu.LastMessage);replay.TogglePause();Assert.That(replay.Paused,Is.True);
            Assert.That(replay.Host.PlayerIndex,Is.EqualTo(p.PlayerIndex));Assert.That(replay.Host.ManualStepper.World.ContextJson,Does.Contain("manual_course_contact_ice_v1"));
            Assert.That(replay.Host.Runner.MovingOvertakeVersion,Is.EqualTo(policyVersion));
            replay.Seek(100);for(int i=0;i<180&&replay.Seeking;i++)yield return null;
            replay.Seek(60);for(int i=0;i<180&&replay.Seeking;i++)yield return null;
            Assert.That(replay.LastFailure,Is.Null);Assert.That(replay.Tick,Is.EqualTo(60));
            Assert.That(replay.Host.ManualStepper.World.GetActor(p.PlayerIndex).Pose,Is.EqualTo(pose));
            Assert.That(replay.Host.Runner.MovingOvertakeVersion,Is.EqualTo(policyVersion));
            Assert.That(replay.Host.ManualStepper.World.ContextJson,Does.Contain("manual_course_contact_ice_v1"));
            Assert.That(store.Json,Is.EqualTo(snapshot));replay.Close();yield return null;
        }
    }
}
