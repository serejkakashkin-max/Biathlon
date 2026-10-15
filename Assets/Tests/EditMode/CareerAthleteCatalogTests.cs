using System;
using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerAthleteCatalogTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        public static CareerAthleteCatalog Catalog()=>new CareerAthleteCatalog(Read,"career_catalog",71000,s=>new SeededRandomSource(s));
        [Test] public void GeneratedCatalogContainsUniqueStableAthletesAcrossAllThreeCircuits()
        {
            var catalog=Catalog();var repeat=Catalog();var ids=new HashSet<string>();var counts=new int[4];
            Assert.That(catalog.Count,Is.EqualTo(90));Assert.That(catalog.RosterVersion,Is.EqualTo("career_roster_v1"));
            for(int i=0;i<catalog.Count;i++)
            {
                var a=catalog.Get(i);var b=repeat.Get(i);Assert.That(ids.Add(a.Id),Is.True);counts[(int)a.Circuit]++;
                Assert.That(a.Id,Is.EqualTo(b.Id));Assert.That(a.Name,Is.EqualTo(b.Name));Assert.That(a.Name,Is.Not.EqualTo("Catalog athlete"));Assert.That(a.Age,Is.EqualTo(b.Age));
                Assert.That(a.Ratings.Overall,Is.EqualTo(b.Ratings.Overall));Assert.That(a.Appearance.ToJson(),Is.EqualTo(b.Appearance.ToJson()));
                for(int stat=0;stat<13;stat++)Assert.That(a.Stat((CareerStat)stat),Is.EqualTo(b.Stat((CareerStat)stat)));
                Assert.That(a.Stat(a.StrongestStat),Is.GreaterThanOrEqualTo(a.Stat(a.WeakestStat)));
            }
            Assert.That(counts,Is.EqualTo(new[]{0,30,30,30}));
        }
        [Test] public void AuthenticSelectionPreservesEveryAbilityIdentityAndContextWithoutDuplicatingTheAthlete()
        {
            var catalog=Catalog();
            foreach(int index in new[]{0,12,29,30,42,59,60,73,89})
            {
                var selected=catalog.Get(index);var save=catalog.Confirm(selected.Id);CareerAthlete player=null;int matches=0;
                for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==selected.Id){player=save.Athlete(i);matches++;}
                Assert.That(matches,Is.EqualTo(1));Assert.That(save.AthleteCount,Is.EqualTo(30));Assert.That(save.PlayerId,Is.EqualTo(selected.Id));Assert.That(save.Id,Is.EqualTo(catalog.CareerId));
                Assert.That(player.Name,Is.EqualTo(selected.Name));Assert.That(player.Nation,Is.EqualTo(selected.Nation));Assert.That(player.Age,Is.EqualTo(selected.Age));Assert.That(player.Appearance.ToJson(),Is.EqualTo(selected.Appearance.ToJson()));
                for(int stat=0;stat<13;stat++){Assert.That(player.Stat((CareerStat)stat),Is.EqualTo(selected.Stat((CareerStat)stat)));Assert.That(save.Creation.InitialStat((CareerStat)stat),Is.EqualTo(selected.Stat((CareerStat)stat)));}
                Assert.That(CareerDefinitions.ForSave(Read,save).Calendar.Circuit,Is.EqualTo(selected.Circuit));Assert.That(save.Creation.Mode,Is.EqualTo(CareerCreationMode.ExistingAthlete));
                Assert.That(save.NextAthleteOrdinal,Is.EqualTo(90));Assert.That(CareerSave.FromJson(save.ToJson()).ToJson(),Is.EqualTo(save.ToJson()));
            }
        }
        [Test] public void BrowsePreviewAndRepeatedSelectionDoNotRerollOrMutateTheWorld()
        {
            int calls=0;var catalog=new CareerAthleteCatalog(Read,"career_catalog",71000,s=>{calls++;return new SeededRandomSource(s);});var selected=catalog.Get(42);
            var before=catalog.Confirm(selected.Id);string json=before.ToJson();
            for(int i=0;i<30;i++){catalog.Search(new CareerAthleteFilter{MinimumRating=40+i});catalog.Get(selected.Id);catalog.Confirm(selected.Id);}
            Assert.That(calls,Is.EqualTo(1));Assert.That(catalog.Confirm(selected.Id).ToJson(),Is.EqualTo(json));
            CareerAdaptation.Apply(before.Athlete(1),CareerTrainingTests.Definition().Get(CareerTrainingKind.Endurance),CareerAdaptationTests.Definition(),10000);
            Assert.That(catalog.Confirm(selected.Id).ToJson(),Is.EqualTo(json));Assert.Catch(()=>catalog.Confirm("unknown"));
        }
        [Test] public void CatalogFiltersAreActualStableSelectionsAndSortedByOvr()
        {
            var catalog=Catalog();var selected=catalog.Get(45);
            var rows=catalog.Search(new CareerAthleteFilter{Nation=selected.Nation,Circuit=selected.Circuit,MinimumAge=selected.Age,MaximumAge=selected.Age,MinimumRating=selected.Ratings.Overall,MaximumRating=selected.Ratings.Overall,Name=selected.Name.ToUpperInvariant(),SpecializationId=selected.SpecializationId});
            Assert.That(Array.Exists(rows,a=>a.Id==selected.Id),Is.True);
            foreach(var a in rows){Assert.That(a.Nation,Is.EqualTo(selected.Nation));Assert.That(a.Age,Is.EqualTo(selected.Age));Assert.That(a.Ratings.Overall,Is.EqualTo(selected.Ratings.Overall));}
            rows=catalog.Search(new CareerAthleteFilter());for(int i=1;i<rows.Length;i++)Assert.That(rows[i-1].Ratings.Overall,Is.GreaterThanOrEqualTo(rows[i].Ratings.Overall));
            Assert.Catch(()=>catalog.Search(new CareerAthleteFilter{MinimumAge=30,MaximumAge=20}));Assert.Catch(()=>catalog.Search(new CareerAthleteFilter{MinimumRating=0}));Assert.Catch(()=>catalog.Search(new CareerAthleteFilter{SpecializationId="hidden_boost"}));
            Assert.That(catalog.Search(new CareerAthleteFilter{Name="not a catalog name"}),Is.Empty);
        }
        [Test] public void ChosenAthleteMapsExactlyOnceIntoTheActualProductionRaceField()
        {
            var catalog=Catalog();
            foreach(int ordinal in new[]{42,73})
            {
                var save=catalog.Confirm(catalog.Get(ordinal).Id);var definitions=CareerDefinitions.ForSave(Read,save);
                var prepared=new Biathlon.AI.CareerRaceProvider(Read,definitions,id=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json")).Prepare(save,definitions.Calendar.Get(0),123);
                Assert.That(prepared.Runner.Field.Count,Is.EqualTo(30));Assert.That(prepared.PlayerIndex,Is.EqualTo(ordinal%30));int copies=0;
                for(int i=0;i<prepared.Runner.Field.Count;i++)if(prepared.Runner.Field.GetAthlete(i).AthleteId.Value==save.PlayerId)copies++;
                Assert.That(copies,Is.EqualTo(1));Assert.That(prepared.Runner.Field.GetAthlete(prepared.PlayerIndex).AthleteId.Value,Is.EqualTo(save.PlayerId));
            }
        }
        private sealed class Store:ICareerProfileStore
        {
            public bool Fail;public string Json;public bool HasProfile=>Json!=null;public string ReadLatest()=>Json;
            public void WriteNew(string id,string json){if(Fail)throw new System.IO.IOException("disk fixture");if(Json!=null)throw new InvalidOperationException();Json=json;}
            public void Replace(string id,string expected,string json)=>throw new InvalidOperationException();
        }
        [Test] public void CatalogSelectionUsesExistingAtomicProfilePublicationAndCanContinue()
        {
            var store=new Store{Fail=true};var ui=new FrontEndSession(Read,store,s=>new SeededRandomSource(s));ui.OpenNewCareer();var catalog=Catalog();string id=catalog.Get(72).Id;
            Assert.Throws<System.IO.IOException>(()=>ui.CreateCareer(catalog,id));Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.NewCareer));Assert.That(ui.Career,Is.Null);Assert.That(store.HasProfile,Is.False);
            store.Fail=false;ui.CreateCareer(catalog,id);Assert.That(ui.Player.Id,Is.EqualTo(id));Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Career));Assert.That(ui.Definitions.Calendar.Circuit,Is.EqualTo(CareerCircuit.WorldCup));
            var restored=new FrontEndSession(Read,store,s=>new SeededRandomSource(s));restored.ContinueCareer();Assert.That(restored.Career.ToJson(),Is.EqualTo(ui.Career.ToJson()));
        }
    }
}
