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
        }
        private static FrontEndSession Session(ICareerProfileStore store)=>new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,s=>new SeededRandomSource(s));
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
