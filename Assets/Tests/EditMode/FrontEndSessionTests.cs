using System;
using System.IO;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class FrontEndSessionTests
    {
        private sealed class Store : ICareerProfileStore
        {
            public string Json;
            public bool Fail;
            public bool HasProfile => Json!=null;
            public string ReadLatest() => Json??throw new FileNotFoundException();
            public void WriteNew(string id,string json) { if(Fail)throw new IOException("disk test");Json=json; }
            public void Replace(string id,string expectedJson,string json){if(Fail||Json!=expectedJson)throw new IOException("disk/stale test");Json=json;}
        }
        private static FrontEndSession Session(ICareerProfileStore store)=>new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,s=>new SeededRandomSource(s));
        private static FrontEndSession RaceWeek(Store store)
        {
            var ui=Session(store);ui.OpenNewCareer();ui.CreateCareer("career_week","Alex","NOR",81000);
            for(int i=0;i<5;i++)ui.RestWeek();return ui;
        }
        // Boundary fixture only: real authority composition/collection is covered separately.
        private static CareerRaceRecord FixtureResult(FrontEndSession ui)
        {
            var week=ui.PreparedWeek;var save=CareerSave.FromJson(week.RaceSnapshotJson);
            var rows=new CareerFinish[save.AthleteCount];for(int i=0;i<rows.Length;i++)rows[i]=new CareerFinish(save.Athlete(i).Id,300000+i*1000,300000+i*1000);
            return new CareerRaceRecord(save.SeasonId+"_"+week.Event.Id,save.SeasonId,week.Event.Id,week.Event.Format,week.RaceSeed,new string('a',64),rows);
        }
        [Test] public void PreparingCancelingAndRetryingRaceNeverConsumesAWeekOrRandomness()
        {
            var store=new Store();var ui=RaceWeek(store);string before=store.Json;ui.OpenPreRace();
            ulong seed=ui.PreparedWeek.RaceSeed;string snapshot=ui.PreparedWeek.RaceSnapshotJson;
            ui.BeginRace();Assert.Throws<InvalidOperationException>(()=>ui.Back());ui.AbandonRace();
            Assert.That(ui.PreparedWeek.RaceSeed,Is.EqualTo(seed));ui.Back();ui.OpenPreRace();
            Assert.That(ui.PreparedWeek.RaceSnapshotJson,Is.EqualTo(snapshot));Assert.That(ui.PreparedWeek.RaceSeed,Is.EqualTo(seed));
            Assert.That(store.Json,Is.EqualTo(before));Assert.That(ui.Career.ToJson(),Is.EqualTo(before));
        }
        [Test] public void CompletedRaceSaveFailureCanRetryExactlyOnceAndContinueAtNextEvent()
        {
            var store=new Store();var ui=RaceWeek(store);string before=store.Json;ui.OpenPreRace();var result=FixtureResult(ui);ui.BeginRace();
            store.Fail=true;Assert.Throws<IOException>(()=>ui.FinishRace(result));
            Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Race));Assert.That(ui.Career.ToJson(),Is.EqualTo(before));Assert.That(store.Json,Is.EqualTo(before));
            Assert.That(ui.ResultsSummary,Is.Null);
            store.Fail=false;ui.FinishRace(result);Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Results));
            Assert.That(ui.Career.Week,Is.EqualTo(7));Assert.That(ui.Career.RaceCount,Is.EqualTo(1));Assert.That(ui.Player.Races,Is.EqualTo(1));
            Assert.That(ui.ResultsSummary.Player.Finish.AthleteId,Is.EqualTo(ui.Career.PlayerId));
            Assert.That(ui.ResultsSummary.PointsAfter,Is.EqualTo(ui.Definitions.Scoring.Points(1)));
            Assert.Throws<InvalidOperationException>(()=>ui.FinishRace(result));ui.ReturnFromResults();
            Assert.That(ui.NextEvent().Format,Is.EqualTo(Biathlon.Domain.CompetitionFormat.Pursuit));
            var restored=Session(store);restored.ContinueCareer();Assert.That(restored.Career.ToJson(),Is.EqualTo(ui.Career.ToJson()));
        }
        [Test] public void RestCannotSkipRaceAndInvalidResultPreservesWeek()
        {
            var store=new Store();var ui=RaceWeek(store);Assert.Throws<InvalidOperationException>(()=>ui.RestWeek());
            Assert.That(ui.Career.Week,Is.EqualTo(6));ui.OpenPreRace();ui.BeginRace();Assert.Catch(()=>ui.FinishRace(null));
            Assert.That(ui.Career.RaceCount,Is.Zero);Assert.That(ui.Career.Week,Is.EqualTo(6));
        }
        [Test] public void PreRaceFactsComeFromPreparedAthleteAndProductionRules()
        {
            var ui=RaceWeek(new Store());ui.OpenPreRace();
            var facts=new PreRaceSummary(ui.PreparedWeek,
                Biathlon.Domain.CompetitionProfile.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/Venues/woodland_basin_v1/sprint_profile.json")),
                Biathlon.Domain.RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),
                Biathlon.Domain.SkiEquipmentCatalog.FromJson(PhysiologyContractTests.DefinitionJson("ski_equipment_provisional_v1.json")));
            Assert.That(facts.Player.Id,Is.EqualTo(ui.Career.PlayerId));Assert.That(facts.Skis.Id,Is.EqualTo(ui.Player.SelectedSki));
            Assert.That(facts.EntrantCount,Is.EqualTo(30));Assert.That(facts.Profile.BoutCount,Is.EqualTo(2));Assert.That(facts.SnowProfileId,Is.EqualTo("packed_neutral"));
        }
#if !UNITY_EDITOR
        [Test,Timeout(900000)] public void ProductionRaceResultReturnsThroughCareerSaveAndRestoresNextEvent()
        {
            var store=new Store();var ui=RaceWeek(store);ui.OpenPreRace();var week=ui.PreparedWeek;
            var provider=new Biathlon.AI.CareerRaceProvider(PhysiologyContractTests.DefinitionJson,ui.Definitions,
                id=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json"));
            var race=provider.Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed);
            Assert.That(race.Runner.Field.Tick.Value,Is.Zero);Assert.That(race.PlayerIndex,Is.GreaterThanOrEqualTo(0));
            Assert.That(race.Runner.Field.GetAthlete(race.PlayerIndex).AthleteId.Value,Is.EqualTo(ui.Career.PlayerId));
            var movement=ManualProductionFieldComposition.Create(race.Runner.Field,race.Venue,PhysiologyContractTests.DefinitionJson);
            race.Runner.BindManualScopedCandidate(movement,
                Biathlon.Domain.ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),
                Biathlon.Domain.ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),
                Biathlon.Domain.PlayerCourseAssistanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("player_course_assistance_v1.json")));
            Assert.Throws<InvalidOperationException>(()=>race.CollectResult());ui.BeginRace();
            while(!race.Runner.Field.IsComplete&&race.Runner.Field.Tick.Value<200000)race.Runner.Step();
            var result=race.CollectResult();Assert.That(result.AuthoritySha256,Is.EqualTo(Biathlon.AI.BalanceRegression.Hash(result.AuthorityResultJson)));
            Assert.That(race.CollectResult().ToJson(),Is.EqualTo(result.ToJson()),"Collection must be read-only and repeatable.");
            var actor=race.Runner.Field.GetAthlete(race.PlayerIndex);CareerFinish playerFinish=null;
            for(int i=0;i<result.Count;i++)if(result.Get(i).AthleteId==actor.AthleteId.Value)playerFinish=result.Get(i);
            Assert.That(playerFinish.Details.SplitCount,Is.EqualTo(actor.Checkpoints.Count));
            Assert.That(playerFinish.Details.BoutCount,Is.EqualTo(race.Runner.Field.Profile.BoutCount));
            for(int b=0;b<playerFinish.Details.BoutCount;b++)Assert.That(playerFinish.Details.ClosedMask(b),Is.EqualTo(actor.GetBoutClosedMask(b)));
            ui.FinishRace(result);Assert.That(ui.Career.RaceCount,Is.EqualTo(1));Assert.That(ui.Career.Week,Is.EqualTo(7));
            var restored=Session(store);restored.ContinueCareer();Assert.That(restored.Career.ToJson(),Is.EqualTo(ui.Career.ToJson()));
            Assert.That(restored.NextEvent().SourceEventId,Is.EqualTo(result.CalendarEventId));
            TestContext.WriteLine("career_result_ticks="+race.Runner.Field.Tick.Value+" sha256="+result.AuthoritySha256);
        }
#endif
        [Test] public void AtomicProfileReplacementRetainsBackupAndRejectsStaleWriter()
        {
            string folder=Path.Combine(Path.GetTempPath(),"biathlon-replace-"+Guid.NewGuid().ToString("N"));
            try
            {
                var files=new FileCareerProfileStore(folder);var ui=Session(files);ui.OpenNewCareer();ui.CreateCareer("career_disk","Alex","NOR",9);
                string before=files.ReadLatest();ui.RestWeek();string after=files.ReadLatest();
                Assert.That(CareerSave.FromJson(after).Week,Is.EqualTo(2));
                Assert.That(File.ReadAllText(Path.Combine(folder,"career_disk.json.bak")),Is.EqualTo(before));
                Assert.Throws<IOException>(()=>files.Replace("career_disk",before,before));Assert.That(files.ReadLatest(),Is.EqualTo(after));
                Assert.That(Directory.GetFiles(folder,"*.tmp"),Is.Empty);
            }
            finally{if(Directory.Exists(folder)){foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder,false);}}
        }
        [Test] public void NewCareerIsSavedBeforeDashboardAndUsesProductionCalendar()
        {
            var store=new Store();var ui=Session(store);
            Assert.That(ui.CanContinue,Is.False);ui.OpenNewCareer();ui.CreateCareer("career_test"," Алекс ","NOR",81000);
            Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(ui.Player.Name,Is.EqualTo("Алекс"));
            Assert.That(ui.Career.CalendarId,Is.EqualTo("woodland_career_calendar_v1"));
            Assert.That(ui.Career.AthleteCount,Is.EqualTo(30));Assert.That(ui.Career.RaceCount,Is.Zero);
            Assert.That(ui.NextEvent().Id,Is.EqualTo("woodland_open_sprint"));Assert.That(ui.NextEvent().Week,Is.EqualTo(6));
            var restored=Session(store);restored.ContinueCareer();Assert.That(restored.Career.ToJson(),Is.EqualTo(ui.Career.ToJson()));
        }
        [Test] public void FailedSaveOrValidationNeverPublishesOrDestroysPriorProfile()
        {
            var store=new Store();var ui=Session(store);ui.OpenNewCareer();ui.CreateCareer("career_first","Alex","NOR",4);
            string old=store.Json;ui.Back();ui.OpenNewCareer();store.Fail=true;
            Assert.Throws<IOException>(()=>ui.CreateCareer("career_second","Next","SWE",5));
            Assert.That(store.Json,Is.EqualTo(old));Assert.That(ui.Career.Id,Is.EqualTo("career_first"));
            Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.NewCareer));
            store.Fail=false;Assert.Catch(()=>ui.CreateCareer("career_bad"," ","NOR",5));Assert.That(store.Json,Is.EqualTo(old));
        }
        [Test] public void SettingsReturnsToItsCallerAndInvalidContinueIsAtomic()
        {
            var store=new Store();var ui=Session(store);ui.OpenSettings();ui.Back();Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Home));
            Assert.Catch(()=>ui.ContinueCareer());Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Home));
            ui.OpenNewCareer();Assert.Throws<InvalidOperationException>(()=>ui.ContinueCareer());ui.CreateCareer("career_nav","Alex","NOR",7);
            ui.OpenSettings();ui.Back();Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Career));
            ui.Back();store.Json="{}";Assert.Catch(()=>ui.ContinueCareer());Assert.That(ui.Career.Id,Is.EqualTo("career_nav"));
        }
        [Test] public void ContinueUsesSavedLegacyCalendarWithoutMigratingItToWoodland()
        {
            var d=new CareerDefinitions(PhysiologyContractTests.DefinitionJson);var old=CareerFactory.Create(d.Roster,d.Calendar,"career_legacy","Alex","NOR","player_appearance",new SeededRandomSource(5));
            var ui=Session(new Store{Json=old.ToJson()});ui.ContinueCareer();Assert.That(ui.Career.ToJson(),Is.EqualTo(old.ToJson()));
            Assert.That(ui.Definitions.Calendar.Id,Is.EqualTo("career_calendar_v1"));
        }
        [Test] public void FileProfilesAreValidatedIndependentAndNeverOverwritten()
        {
            string folder=Path.Combine(Path.GetTempPath(),"biathlon-ui-"+Guid.NewGuid().ToString("N"));
            try
            {
                var files=new FileCareerProfileStore(folder);var ui=Session(files);Assert.That(files.HasProfile,Is.False);
                ui.OpenNewCareer();ui.CreateCareer("career_a","Alex","NOR",6);string first=files.ReadLatest();
                Assert.Throws<IOException>(()=>files.WriteNew("career_a",first));Assert.That(files.ReadLatest(),Is.EqualTo(first));
                Assert.Throws<ArgumentException>(()=>files.WriteNew("../escape",first));Assert.Catch(()=>files.WriteNew("career_bad","{}"));
                ui.Back();ui.OpenNewCareer();ui.CreateCareer("career_b","Next","SWE",7);
                Assert.That(Directory.GetFiles(folder,"*.json").Length,Is.EqualTo(2));Assert.That(File.ReadAllText(Path.Combine(folder,"career_a.json")),Is.EqualTo(first));
                Assert.That(Directory.GetFiles(folder,"*.tmp").Length,Is.Zero);
            }
            finally { if(Directory.Exists(folder)){foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder,false);} }
        }
        [Test] public void BothLocalesHaveRealTextAndUnknownKeysFailClearly()
        {
            var text=UiTextCatalog.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/front_end_text_v1.json"));
            Assert.That(text.Text("new_career","ru"),Is.EqualTo("Новая карьера"));Assert.That(text.Text("new_career","en"),Is.EqualTo("New career"));
            Assert.Throws<ArgumentException>(()=>text.Text("missing","ru"));Assert.Throws<ArgumentException>(()=>text.Text("new_career","de"));
        }
    }
}
