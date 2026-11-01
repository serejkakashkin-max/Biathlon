using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerSprintStartDrawTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        private static string Venue(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static string SmallRead(string name)=>name=="career_roster_v1.json"?Read(name).Replace("\"population\": 30","\"population\": 2"):Read(name);
        private static CareerDefinitions Definitions(bool small=false)=>new CareerDefinitions(small?(Func<string,string>)SmallRead:Read,"woodland_development_calendar_v1.json");
        private static CareerSave Save(CareerDefinitions d)=>CareerFactory.Create(d.Roster,d.Calendar,"sprint_draw_test","Alex","NOR","default",new SeededRandomSource(71000));
        [Test]public void DrawIsRepeatableAfterReloadWithoutMutatingSavedCareer()
        {
            var d=Definitions();var save=Save(d);string before=save.ToJson();
            var first=CareerSprintStartDraw.Capture(save,d.Calendar.Get(0),new SeededRandomSource(123));
            var again=CareerSprintStartDraw.Capture(CareerSave.FromJson(before),d.Calendar.Get(0),new SeededRandomSource(123));
            Assert.That(again,Is.EqualTo(first));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.That(CareerSprintStartDraw.Capture(save,d.Calendar.Get(0),new SeededRandomSource(124)),Is.Not.EqualTo(first));
        }
        [Test]public void FixedSixThousandSeedsCoverEveryPlayerSlotWithoutMissingOrDuplicatingEntrants()
        {
            var d=Definitions();var save=Save(d);var expected=new HashSet<string>();for(int i=0;i<save.AthleteCount;i++)expected.Add(save.Athlete(i).Id);
            var slots=new int[save.AthleteCount];bool complete=true;
            for(ulong seed=0;seed<6000;seed++)
            {
                var draw=CareerSprintStartDraw.Capture(save,d.Calendar.Get(0),new SeededRandomSource(seed));
                complete&=draw.Length==expected.Count&&new HashSet<string>(draw).SetEquals(expected);
                for(int i=0;i<draw.Length;i++)if(draw[i]==save.PlayerId)slots[i]++;
            }
            Assert.That(complete,Is.True);foreach(int count in slots)Assert.That(count,Is.InRange(130,270));
            TestContext.WriteLine("player_slot_counts="+string.Join(",",slots));
        }
        [Test]public void NonSprintAndMissingDependenciesAreRejected()
        {
            var d=Definitions();var save=Save(d);
            Assert.Throws<ArgumentException>(()=>CareerSprintStartDraw.Capture(save,d.Calendar.Get(1),new SeededRandomSource(1)));
            Assert.Throws<ArgumentNullException>(()=>CareerSprintStartDraw.Capture(null,d.Calendar.Get(0),new SeededRandomSource(1)));
            Assert.Throws<ArgumentNullException>(()=>CareerSprintStartDraw.Capture(save,d.Calendar.Get(0),null));
        }
        [Test]public void PreparedFieldUsesDrawnIdentitiesAndUnchangedIntervalInsteadOfPlayerFirst()
        {
            var d=Definitions();var save=Save(d);var e=d.Calendar.Get(0);string before=save.ToJson();
            var provider=new CareerRaceProvider(Read,d,Venue);var first=provider.Prepare(save,e,123);var second=provider.Prepare(CareerSave.FromJson(before),e,123);
            long interval=(long)(first.Runner.Field.Profile.StartIntervalSeconds*first.Runner.Field.TickFrequency);
            var ids=new HashSet<string>();
            for(int i=0;i<first.Runner.Field.Count;i++)
            {
                var actor=first.Runner.Field.GetAthlete(i);ids.Add(actor.AthleteId.Value);
                Assert.That(second.Runner.Field.GetAthlete(i).AthleteId,Is.EqualTo(actor.AthleteId));
                Assert.That(actor.ScheduledStart.Value,Is.EqualTo(1+i*interval));
            }
            Assert.That(ids.Count,Is.EqualTo(save.AthleteCount));Assert.That(first.PlayerIndex,Is.GreaterThan(0));
            Assert.That(first.Runner.Field.GetAthlete(first.PlayerIndex).AthleteId.Value,Is.EqualTo(save.PlayerId));
            Assert.That(save.ToJson(),Is.EqualTo(before));
            var old=provider.Prepare(save,e,123,legacySprintStartOrder:true);Assert.That(old.PlayerIndex,Is.Zero);Assert.That(old.Runner.Field.GetAthlete(0).ScheduledStart.Value,Is.EqualTo(1));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)]public void OldAndDrawnSprintJournalsRetainTheirOwnRosterAndExactPlayback(int version)
        {
            var d=Definitions(true);var save=Save(d);var e=d.Calendar.Get(0);var provider=new CareerRaceProvider(SmallRead,d,Venue);
            var policy=version==0?ManualCourseReactionPolicy.LegacyV25:ManualCourseReactionPolicy.PassiveCurvedV26;
            CareerPreparedRace Prepare()=>provider.Prepare(CareerSave.FromJson(save.ToJson()),e,123,legacyNeutralWeather:version==0,legacyMassStartOrder:version==0,courseReactionPolicy:policy,legacySprintStartOrder:version<2,legacyEquipment:true);
            var source=Prepare();var stepper=Bind(source,SmallRead);byte[] bytes;long ticks=Math.Max(120,source.Runner.Field.GetAthlete(source.PlayerIndex).ScheduledStart.Value+40);
            using(var stream=new MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(stream,stepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
                {stepper.CommandObserver=writer;for(int i=0;i<ticks;i++)source.Runner.Step();writer.Complete();Assert.That(writer.Failure,Is.Null);bytes=stream.ToArray();}
            }
            var target=Prepare();var playback=Bind(target,SmallRead);
            using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,playback,ticks))while(reader.HasNext)reader.Step();
            for(int i=0;i<source.Runner.Field.Count;i++)
            {
                Assert.That(target.Runner.Field.GetAthlete(i).AthleteId,Is.EqualTo(source.Runner.Field.GetAthlete(i).AthleteId));
                Assert.That(playback.World.GetActor(i).Pose,Is.EqualTo(stepper.World.GetActor(i).Pose));
                Assert.That(target.Runner.Field.GetAthlete(i).LastFrame,Is.EqualTo(source.Runner.Field.GetAthlete(i).LastFrame));
            }
            if(version<2)Assert.That(source.PlayerIndex,Is.Zero);
            var info=new RaceReplayInfo{Id="sprint_order_v"+version,StartOrderVersion=version,CourseReactionVersion=version==0?0:26,RecordedUtc="2026-10-30T00:00:00Z",CareerSnapshotJson=save.ToJson(),EventId=e.Id,VenueId=e.VenueId,Format=(int)e.Format,Seed=123,PlayerId=save.PlayerId,PlayerName="Alex",TickFrequency=20,TickCount=ticks,CommandSha256=new string('a',64),ResultSha256=new string('b',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()};
            string json=info.ToJson();Assert.That(RaceReplayInfo.FromJson(json).StartOrderVersion,Is.EqualTo(version));if(version==0)StringAssert.DoesNotContain("\"start_order_version\"",json);
        }
        private static ManualScopedFieldStepper Bind(CareerPreparedRace p,Func<string,string> read)
        {
            var stepper=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,p.ManualRead,p.CourseReactionPolicy);
            p.Runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(read("player_course_assistance_v1.json")));
            return stepper;
        }
#if !UNITY_EDITOR
        [Test,Timeout(900000)]public void FullThirtyAthleteDrawnSprintReexecutesExactlyAndPreservesSprintQualificationForPursuit()
        {
            var d=Definitions();var save=Save(d);string snapshot=save.ToJson();var e=d.Calendar.Get(0);
            var firstProvider=new CareerRaceProvider(Read,d,Venue);var secondProvider=new CareerRaceProvider(Read,d,Venue);
            var first=firstProvider.Run(save,e,123);var second=secondProvider.Run(CareerSave.FromJson(snapshot),e,123);
            Assert.That(first.AuthorityResultJson,Is.EqualTo(second.AuthorityResultJson));Assert.That(first.AuthoritySha256,Is.EqualTo(second.AuthoritySha256));
            Assert.That(firstProvider.LastTicks,Is.EqualTo(secondProvider.LastTicks));Assert.That(firstProvider.LastEventHash,Is.EqualTo(secondProvider.LastEventHash));Assert.That(firstProvider.LastShots,Is.EqualTo(300));
            CareerStandings.Register(save,first);var pursuit=firstProvider.Prepare(save,d.Calendar.Get(1),456);
            var qualified=PursuitQualificationSnapshot.Create("draw_qualification",SprintResultSnapshot.FromJson(first.AuthorityResultJson),pursuit.Runner.Field.Profile);
            for(int i=0;i<qualified.Count;i++)Assert.That(pursuit.Runner.Field.GetAthlete(i).AthleteId.Value,Is.EqualTo(qualified.Get(i).AthleteId));
            var directory=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
            Assert.That(directory,Is.Not.Null);string folder=Path.Combine(directory.FullName,"docs/quality/m16-2/sprint-draw");
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"regression.json"),"{\"schema_version\":1,\"id\":\"m16_2_sprint_draw_regression\",\"status\":\"PASS\",\"athletes\":30,\"complete_runs\":2,\"seed\":123,\"ticks\":"+firstProvider.LastTicks+",\"shots\":"+firstProvider.LastShots+",\"result_sha256\":\""+first.AuthoritySha256+"\",\"event_hash\":\""+firstProvider.LastEventHash+"\",\"pursuit_qualification_preserved\":true}");
        }
#endif
    }
}
