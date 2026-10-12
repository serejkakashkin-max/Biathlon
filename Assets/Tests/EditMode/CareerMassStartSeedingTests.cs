using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerMassStartSeedingTests
    {
        public static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        public static string Venue(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        public static CareerSave Create(CareerDefinitions d)=>CareerFactory.Create(d.Roster,d.Calendar,"mass_seed_test","Alex","NOR","default",new SeededRandomSource(71000));
        public static void Register(CareerSave save,CareerCalendar calendar,int eventIndex,int first=1,int second=2)
        {
            var e=calendar.Get(eventIndex);var order=new int[save.AthleteCount];order[0]=first;order[1]=second;int n=2;
            for(int i=0;i<save.AthleteCount;i++)if(i!=first&&i!=second)order[n++]=i;
            var finishes=new CareerFinish[order.Length];for(int i=0;i<order.Length;i++)finishes[i]=new CareerFinish(save.Athlete(order[i]).Id,300000+i*1000,300000+i*1000);
            CareerStandings.Register(save,new CareerRaceRecord("seed_result_"+eventIndex,save.SeasonId,e.Id,e.Format,123,new string('a',64),finishes));
        }
        [Test] public void DevelopmentFirstMassHasOverallLeaderBibButNoInventedDisciplineLeader()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);
            Register(save,d.Calendar,0);Register(save,d.Calendar,1);
            var leaders=CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,d.Calendar.Get(2));
            Assert.That(leaders.ForAthlete(save.Athlete(1).Id),Is.EqualTo(CareerLeaderBib.Total));
            Assert.That(leaders.ForAthlete(save.PlayerId),Is.EqualTo(CareerLeaderBib.None));
        }
        [Test] public void ContinuedCareerMassUsesRankedStartAndMatchingBibInsteadOfPlayerFirst()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);
            Register(save,d.Calendar,0);Register(save,d.Calendar,1);string before=save.ToJson();
            var expected=CareerStandings.Table(save,d.Scoring);var p=new CareerRaceProvider(Read,d,Venue).Prepare(CareerSave.FromJson(before),d.Calendar.Get(2),123);
            Assert.That(p.Runner.Field.StartGrid.Get(0).AthleteId.Value,Is.EqualTo(expected[0].AthleteId));
            Assert.That(p.PlayerIndex,Is.GreaterThan(0),"Player must not keep start slot one when another athlete leads.");
            Assert.That(p.Runner.Field.StartGrid.Get(p.PlayerIndex).StartOrdinal,Is.EqualTo(p.PlayerIndex+1));
            Assert.That(p.Runner.Field.StartGrid.Get(0).LateralNanometers,Is.GreaterThan(p.Runner.Field.StartGrid.Get(2).LateralNanometers),"IBU numbering runs right to left facing the start.");
            Assert.That(save.ToJson(),Is.EqualTo(before));
        }
        [Test] public void WorldCupFirstMassAndLaterMassUseTheirOwnDisciplineClassification()
        {
            string ReadWorld(string name)=>name=="woodland_world_cup_calendar_v1.json"?Read(name).Replace("\"minimum_tier\": 2","\"minimum_tier\": 0"):Read(name);
            var d=new CareerDefinitions(ReadWorld,"woodland_world_cup_calendar_v1.json");var save=Create(d);
            Register(save,d.Calendar,0);Register(save,d.Calendar,1);
            var first=CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,d.Calendar.Get(2));
            Assert.That(first.ForAthlete(save.Athlete(1).Id),Is.EqualTo(CareerLeaderBib.Total));
            for(int i=0;i<save.AthleteCount;i++)Assert.That(first.ForAthlete(save.Athlete(i).Id)&CareerLeaderBib.Discipline,Is.EqualTo(CareerLeaderBib.None));
            Register(save,d.Calendar,2,2,1);
            var later=CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,d.Calendar.Get(5));
            Assert.That(later.ForAthlete(save.Athlete(2).Id)&CareerLeaderBib.Discipline,Is.EqualTo(CareerLeaderBib.Discipline));
        }
        [Test] public void WorldCupPriorityUsesTotalThenCurrentMeetingAndIgnoresFutureRace()
        {
            string ReadWorld(string name)=>name=="woodland_world_cup_calendar_v1.json"?Read(name).Replace("\"minimum_tier\": 2","\"minimum_tier\": 0"):Read(name);
            var d=new CareerDefinitions(ReadWorld,"woodland_world_cup_calendar_v1.json");var save=Create(d);
            Register(save,d.Calendar,0);Register(save,d.Calendar,1);Register(save,d.Calendar,2);Register(save,d.Calendar,3,29,28);Register(save,d.Calendar,4,28,29);
            var e=d.Calendar.Get(5);var total=CareerStandings.BeforeStart(save,d.Scoring,d.Calendar,e);
            var order=CareerMassStartSeeding.Capture(save,d.Calendar,d.Scoring,e);
            Assert.That(d.Calendar.MassStartTotalPriority,Is.EqualTo(20));
            for(int i=0;i<20;i++)Assert.That(order[i],Is.EqualTo(total[i].AthleteId));
            var current=CareerStandings.BeforeStart(save,d.Scoring,d.Calendar,e,meetingOnly:true);
            var top=new System.Collections.Generic.HashSet<string>();for(int i=0;i<20;i++)top.Add(order[i]);
            int expected=20;foreach(var row in current)if(!top.Contains(row.AthleteId))Assert.That(order[expected++],Is.EqualTo(row.AthleteId));
            Register(save,d.Calendar,5,0,2);
            Assert.That(CareerMassStartSeeding.Capture(save,d.Calendar,d.Scoring,e),Is.EqualTo(order),"An event cannot reseed its own recorded start.");
        }
        [Test] public void DevelopmentLeaderNeverBecomesRedAndLegacySeasonRetainsVisibleLeaders()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);
            Register(save,d.Calendar,0);Register(save,d.Calendar,1);Register(save,d.Calendar,2);
            var later=CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,d.Calendar.Get(5));
            Assert.That(later.ForAthlete(save.Athlete(1).Id),Is.EqualTo(CareerLeaderBib.Total));
            d=new CareerDefinitions(Read,"woodland_career_calendar_v1.json");save=Create(d);Register(save,d.Calendar,0);Register(save,d.Calendar,1);
            Assert.That(CareerLeaderBibs.Capture(save,d.Calendar,d.Scoring,d.Calendar.Get(2)).ForAthlete(save.Athlete(1).Id),Is.EqualTo(CareerLeaderBib.Total));
        }
        [Test] public void LegacyReplayRetainsOldRosterAndLeftToRightSlots()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);Register(save,d.Calendar,0);Register(save,d.Calendar,1);
            var legacy=new CareerRaceProvider(Read,d,Venue).Prepare(save,d.Calendar.Get(2),123,legacyMassStartOrder:true);
            Assert.That(legacy.PlayerIndex,Is.Zero);Assert.That(legacy.Runner.Field.StartGrid.Get(0).AthleteId.Value,Is.EqualTo(save.PlayerId));
            Assert.That(legacy.Runner.Field.StartGrid.Get(0).LateralNanometers,Is.LessThan(legacy.Runner.Field.StartGrid.Get(2).LateralNanometers));
            Assert.That(ManualCourseContactDefinition.FromJson(legacy.ManualRead("manual_course_contact_gameplay_v2.json")).NormalSampleColumns,Is.EqualTo(5));
        }
        [TestCase(false),TestCase(true)] public void RankedAndHistoricalMassStartJournalsReproduceTheirOwnInitialRoster(bool historical)
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);Register(save,d.Calendar,0);Register(save,d.Calendar,1);string json=save.ToJson();
            var provider=new CareerRaceProvider(Read,d,Venue);var p=provider.Prepare(save,d.Calendar.Get(2),123,legacyMassStartOrder:historical);
            var source=ManualProductionFieldComposition.Create(p.Runner.Field,p.Venue,p.ManualRead);
            p.Runner.BindManualScopedCandidate(source,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")));
            byte[] bytes;using(var stream=new System.IO.MemoryStream())
            {
                using(var writer=new Biathlon.Replay.ManualRaceJournalWriter(stream,source,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
                {source.CommandObserver=writer;for(int i=0;i<120;i++)p.Runner.Step();writer.Complete();Assert.That(writer.Failure,Is.Null);bytes=stream.ToArray();}
            }
            var replay=provider.Prepare(CareerSave.FromJson(json),d.Calendar.Get(2),123,legacyMassStartOrder:historical);
            var target=ManualProductionFieldComposition.Create(replay.Runner.Field,replay.Venue,replay.ManualRead);
            using(var input=new System.IO.MemoryStream(bytes))using(var reader=new Biathlon.Replay.ManualRaceJournalReader(input,target,120))
            {while(reader.TickCount<120)reader.Step();}
            for(int i=0;i<source.Field.Count;i++)Assert.That(target.World.GetActor(i).Pose,Is.EqualTo(source.World.GetActor(i).Pose));
            Assert.That(target.World.Tick,Is.EqualTo(source.World.Tick));
        }
#if !UNITY_EDITOR
        [Test,Timeout(900000)] public void SeededMassStartCompletesDeterministicallyAfterSavedCareerReload()
        {
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=Create(d);Register(save,d.Calendar,0);Register(save,d.Calendar,1);string json=save.ToJson();
            var a=new CareerRaceProvider(Read,d,Venue);var b=new CareerRaceProvider(Read,d,Venue);
            var first=a.Run(save,d.Calendar.Get(2),123);var second=b.Run(CareerSave.FromJson(json),d.Calendar.Get(2),123);
            Assert.That(first.AuthorityResultJson,Is.EqualTo(second.AuthorityResultJson));Assert.That(first.AuthoritySha256,Is.EqualTo(second.AuthoritySha256));
            Assert.That(a.LastEventHash,Is.EqualTo(b.LastEventHash));Assert.That(a.LastTicks,Is.EqualTo(b.LastTicks));Assert.That(a.LastShots,Is.EqualTo(save.AthleteCount*20));
            TestContext.WriteLine("mass_start_seed_regression hash="+first.AuthoritySha256+" ticks="+a.LastTicks+" event_hash="+a.LastEventHash+" shots="+a.LastShots);
        }
#endif
    }
}
