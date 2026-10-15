using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerWorldTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        private static CareerSave NewOwn()
        {
            var draft=CareerCreationDraftTests.Draft();var d=CareerDefinitions.ForNewProductionCareer(Read,draft.StartProfileId);
            var save=CareerWorld.Initialize(draft.Confirm("persistent_test",d.Calendar,s=>new SeededRandomSource(s)),d,s=>new SeededRandomSource(s));
            CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s),draft.Tier.BaselineStat);return save;
        }
        private static CareerSave LastWeek(CareerSave save)
        {string json=save.ToJson();int at=json.LastIndexOf("\"week\":",StringComparison.Ordinal);return CareerSave.FromJson(json.Substring(0,at)+Regex.Replace(json.Substring(at),"^(\"week\":)[0-9]+","${1}52"));}
        [Test] public void CatalogSelectionRetainsEveryKnownAthleteAndExactHiddenDevelopmentAcrossCircuits()
        {
            var catalog=CareerAthleteCatalogTests.Catalog();var save=catalog.Confirm(catalog.Get(42).Id);
            Assert.That(save.AthleteCount,Is.EqualTo(30));Assert.That(save.PoolCount,Is.EqualTo(60));Assert.That(save.WorldAthleteCount,Is.EqualTo(90));
            var ids=new HashSet<string>();for(int i=0;i<save.WorldAthleteCount;i++)Assert.That(ids.Add(save.WorldAthlete(i).Id),Is.True);
            for(int i=0;i<catalog.Count;i++)
            {
                var row=catalog.Get(i);var a=save.FindWorldAthlete(row.Id);Assert.That(a,Is.Not.Null);Assert.That(a.Name,Is.EqualTo(row.Name));Assert.That(a.Age,Is.EqualTo(row.Age));
                var original=catalog.Confirm(row.Id).FindWorldAthlete(row.Id);Assert.That(a.Growth.ToJson(),Is.EqualTo(original.Growth.ToJson()));
                for(int s=0;s<13;s++)Assert.That(a.Stat((CareerStat)s),Is.EqualTo(row.Stat((CareerStat)s)));
            }
            Assert.That(CareerSave.FromJson(save.ToJson()).ToJson(),Is.EqualTo(save.ToJson()));
        }
        [Test] public void WorldInitializationIsDetachedDeterministicAndDoesNotConsumeRaceCheckpoint()
        {
            var draft=CareerCreationDraftTests.Draft();var d=CareerDefinitions.ForNewProductionCareer(Read,draft.StartProfileId);var source=draft.Confirm("persistent_init",d.Calendar,s=>new SeededRandomSource(s));string before=source.ToJson();
            var a=CareerWorld.Initialize(source,d,s=>new SeededRandomSource(s));var b=CareerWorld.Initialize(source,d,s=>new SeededRandomSource(s));
            Assert.That(source.ToJson(),Is.EqualTo(before));Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));Assert.That(a.RandomState,Is.EqualTo(source.RandomState));Assert.That(a.NextAthleteOrdinal,Is.EqualTo(90));
            Assert.That(a.PoolCount,Is.EqualTo(60));Assert.That(CareerWorld.Initialize(a,d,s=>throw new Exception("no reroll")),Is.SameAs(a));
            int calls=0;Assert.Catch(()=>CareerWorld.Initialize(source,d,s=>++calls==15?null:new SeededRandomSource(s)));Assert.That(source.ToJson(),Is.EqualTo(before));
            var legacy=CareerFoundationTests.Create();Assert.That(CareerWorld.Initialize(legacy,CareerSeasonTests.Definitions(),s=>new SeededRandomSource(s)),Is.SameAs(legacy));
        }
        [Test] public void BackgroundAthletesUseTheSameWeeklyTrainingRecoveryAndAgeingWithoutInventingResults()
        {
            var save=NewOwn();var d=CareerDefinitions.ForSave(Read,save);string before=save.ToJson();var expected=CareerSave.FromJson(before).Pool(0).Athlete;var circuit=save.Pool(0).Circuit;
            var calendar=CareerCalendar.FromJson(Read(d.World.Circuit(circuit).CalendarResource));
            var plan=CareerAiTraining.Plan(expected,d.Training,calendar,d.Policy,save.Week,d.Growth,d.Adaptation);
            CareerTrainingWeek.Apply(expected,plan,d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,d.Policy.Quality,false,d.Growth);
            CareerFatigue.Rest(expected,d.Fatigue,calendar.TrainingSlots(save.Week)-plan.UsedSlots);CareerMental.Rest(expected,d.Mental);
            var prepared=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));var candidate=CareerSave.FromJson(prepared.RaceSnapshotJson);var actual=candidate.Pool(0).Athlete;
            Assert.That(actual.BackgroundWeeks,Is.EqualTo(1));for(int s=0;s<13;s++)Assert.That(actual.Stat((CareerStat)s),Is.EqualTo(expected.Stat((CareerStat)s)));
            Assert.That(actual.Fatigue,Is.EqualTo(expected.Fatigue));Assert.That(actual.TrainingSessions,Is.EqualTo(expected.TrainingSessions));Assert.That(actual.Form,Is.EqualTo(expected.Form));
            Assert.That(actual.Races,Is.Zero);Assert.That(actual.Wins,Is.Zero);Assert.That(candidate.RaceCount,Is.Zero);Assert.That(candidate.RandomState,Is.EqualTo(save.RandomState));Assert.That(save.ToJson(),Is.EqualTo(before));
        }
        [Test] public void AnnualTransitionsKeepFamiliarIdentitiesTheirProgressAndBoundedFields()
        {
            var save=NewOwn();var seen=new HashSet<string>();for(int i=0;i<save.WorldAthleteCount;i++)seen.Add(save.WorldAthlete(i).Id);
            var initial=new Dictionary<string,string>();for(int i=0;i<save.WorldAthleteCount;i++)initial.Add(save.WorldAthlete(i).Id,save.WorldAthlete(i).Growth.ToJson());
            int retired=0;long previousOrdinal=save.NextAthleteOrdinal;string player=save.PlayerId;
            for(int year=0;year<15;year++)
            {
                var d=CareerDefinitions.ForSave(Read,save);
                // Explicit aggregate development experiment: no fabricated race result or authority tick count.
                var prepared=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));
                save=CareerSeasonEngine.CompleteWeek(prepared,d,null,s=>new SeededRandomSource(s));save=LastWeek(save);
                prepared=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));
                var next=CareerSeasonEngine.CompleteWeek(prepared,d,null,s=>new SeededRandomSource(s));
                Assert.That(next.AthleteCount,Is.EqualTo(30));Assert.That(next.WorldAthleteCount,Is.InRange(d.World.MinimumPopulation,d.World.MaximumPopulation));Assert.That(next.PlayerId,Is.EqualTo(player));Assert.That(next.NextAthleteOrdinal,Is.GreaterThan(previousOrdinal));
                var retained=new HashSet<string>();for(int i=0;i<next.WorldAthleteCount;i++)
                {
                    var a=next.WorldAthlete(i);Assert.That(retained.Add(a.Id),Is.True);if(initial.TryGetValue(a.Id,out var profile))Assert.That(a.Growth.ToJson(),Is.EqualTo(profile));
                    if(!seen.Contains(a.Id)){Assert.That(a.Age,Is.InRange(18,20));Assert.That(a.DevelopmentCount,Is.EqualTo(1));}
                    seen.Add(a.Id);Assert.That(a.Growth,Is.Not.Null);
                }
                for(int i=0;i<next.RetiredCount;i++)retained.Add(next.Retired(i).Id);
                foreach(string known in initial.Keys)Assert.That(retained.Contains(known),Is.True,"Known identity was lost: "+known);
                Assert.That(next.RetiredCount,Is.LessThanOrEqualTo(d.World.RetiredHistoryLimit));retired=next.RetiredCount;
                Assert.That(CareerSave.FromJson(next.ToJson()).ToJson(),Is.EqualTo(next.ToJson()));previousOrdinal=next.NextAthleteOrdinal;save=next;
            }
            Assert.That(retired,Is.GreaterThan(10));Assert.That(save.FindWorldAthlete(player).Age,Is.EqualTo(33));
            TestContext.Out.WriteLine("15 aggregate transitions, world="+save.WorldAthleteCount+" compact="+save.RetiredCount+" total identities="+save.NextAthleteOrdinal+" bytes="+save.ToJson().Length);
        }
        [Test] public void InvalidWorldMetadataDuplicateIdsAndRepeatedSeasonPublicationAreRejected()
        {
            var save=NewOwn();string json=save.ToJson();var d=CareerDefinitions.ForSave(Read,save);string pool=save.Pool(0).Athlete.Id;
            Assert.Catch(()=>CareerSave.FromJson(json.Replace("\"id\":\""+pool+"\"","\"id\":\""+save.PlayerId+"\"")));
            Assert.Catch(()=>CareerSave.FromJson(json.Replace("\"growth_profile\":","\"background_weeks\":-1,\"growth_profile\":")));
            Assert.Catch(()=>CareerWorld.AdvanceSeason(save,d,s=>new SeededRandomSource(s)));Assert.That(save.ToJson(),Is.EqualTo(json));
            var bad=d.World.ToJson();Assert.Catch(()=>CareerWorldDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(bad,"maximum_population","300")));
            Assert.Catch(()=>CareerWorldDefinition.FromJson(bad.Replace("woodland_junior_calendar_v1.json","../private.json")));
        }
    }
}
