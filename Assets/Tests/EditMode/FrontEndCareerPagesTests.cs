using System;
using System.IO;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class FrontEndCareerPagesTests
    {
        private sealed class Store:ICareerProfileStore
        {
            public string Json;public bool Fail;public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json;
            public void WriteNew(string id,string json)=>Json=json;
            public void Replace(string id,string expected,string json){if(Fail||Json!=expected)throw new IOException();Json=json;}
        }
        private static FrontEndSession New(Store store)
        {var s=new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,x=>new SeededRandomSource(x));s.OpenNewCareer();s.CreateCareer("pages_test","Alex","NOR",10);return s;}
        [Test] public void ForecastAndDraftDoNotSpendAWeekOrConsumeSavedRandomness()
        {
            var store=new Store();var s=New(store);string before=store.Json;s.OpenCareerPage(FrontEndScreen.Training);
            s.AddTraining(CareerTrainingKind.Endurance);s.TrainingPreview();s.TrainingPreview();
            Assert.That(s.Career.ToJson(),Is.EqualTo(before));Assert.That(store.Json,Is.EqualTo(before));
            Assert.That(s.TrainingCount,Is.EqualTo(1));s.Back();s.OpenCareerPage(FrontEndScreen.Training);Assert.That(s.TrainingCount,Is.EqualTo(1));
            while(s.TrainingPlan().UsedSlots+s.Definitions.Training.Get(CareerTrainingKind.Recovery).TimeSlots<=s.Definitions.Calendar.TrainingSlots(s.Career.Week))s.AddTraining(CareerTrainingKind.Recovery);
            int count=s.TrainingCount;Assert.Throws<ArgumentException>(()=>s.AddTraining(CareerTrainingKind.Recovery));Assert.That(s.TrainingCount,Is.EqualTo(count));
        }
        [Test] public void TrainingCommitIsAtomicAndRetainsPlanForSaveRetry()
        {
            var store=new Store();var s=New(store);s.OpenCareerPage(FrontEndScreen.Training);s.AddTraining(CareerTrainingKind.Endurance);s.Back();string before=store.Json;
            store.Fail=true;Assert.Throws<IOException>(()=>s.CompleteTrainingWeek());Assert.That(s.Career.ToJson(),Is.EqualTo(before));Assert.That(s.TrainingCount,Is.EqualTo(1));
            store.Fail=false;s.CompleteTrainingWeek();Assert.That(s.Career.Week,Is.EqualTo(2));Assert.That(s.TrainingCount,Is.Zero);Assert.That(store.Json,Is.EqualTo(s.Career.ToJson()));
        }
        [Test] public void EquipmentPurchaseAndSelectionAreAtomicAndChargeOnce()
        {
            var store=new Store();New(store);
            // Purchase atomicity needs earned funds; the revised starting budget
            // deliberately cannot buy every upgrade before the first race.
            store.Json=PhysiologyContractTests.ReplaceNumber(store.Json,"money_minor","200000");
            var s=new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,x=>new SeededRandomSource(x));s.ContinueCareer();s.OpenCareerPage(FrontEndScreen.Equipment);string target=null;
            for(int i=0;i<s.EquipmentCatalog.Count;i++){var item=s.EquipmentCatalog.Get(i);if(item.Id!=s.Player.SelectedSki&&CareerInventory.Compare(s.Player,s.EquipmentCatalog,s.Definitions.Economy,item.Id,"packed_neutral").PurchasePrice<=s.Player.Money){target=item.Id;break;}}
            Assert.That(target,Is.Not.Null);string before=store.Json;store.Fail=true;Assert.Throws<IOException>(()=>s.Equip(target));Assert.That(s.Career.ToJson(),Is.EqualTo(before));
            store.Fail=false;s.Equip(target);long money=s.Player.Money;Assert.That(s.Player.SelectedSki,Is.EqualTo(target));s.Equip(target);Assert.That(s.Player.Money,Is.EqualTo(money));
            s.Back();Assert.That(s.Screen,Is.EqualTo(FrontEndScreen.Career));
        }
        [TestCase(FrontEndScreen.Standings)][TestCase(FrontEndScreen.History)][TestCase(FrontEndScreen.Rivalries)][TestCase(FrontEndScreen.Replays)]
        public void ReadOnlyCareerPagesReturnWithoutChangingTheSave(FrontEndScreen page)
        {var store=new Store();var s=New(store);string before=store.Json;s.OpenCareerPage(page);s.Back();Assert.That(s.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(s.Career.ToJson(),Is.EqualTo(before));}
    }
}
