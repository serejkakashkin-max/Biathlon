using System;
using System.Collections.Generic;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class WoodlandCareerCalendarTests
    {
        private sealed class FixedRaceBoundary : ICareerRaceProvider
        {
            public CareerRaceRecord Run(CareerSave save,CareerCalendarEvent e,ulong seed)
            {
                var finishes=new CareerFinish[save.AthleteCount];
                for(int i=0;i<finishes.Length;i++)
                    finishes[i]=new CareerFinish(save.Athlete(i).Id,300000+i*1000,300000+i*1000);
                return new CareerRaceRecord(save.SeasonId+"_"+e.Id,save.SeasonId,e.Id,e.Format,seed,
                    new string('a',64),finishes);
            }
        }
        private const string CalendarFile="woodland_career_calendar_v1.json";
        private static string ReadVenue(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static CareerDefinitions Definitions()=>new CareerDefinitions(PhysiologyContractTests.DefinitionJson,CalendarFile);
        private static CareerSave NewCareer(CareerDefinitions definitions)=>CareerFactory.Create(definitions.Roster,
            definitions.Calendar,"woodland_career_test","Alex North","NOR","player_appearance",new SeededRandomSource(71000));
        private static void ReportFinishSpread(CareerRaceRecord result)
        {
            int penalties=0;
            for(int i=0;i<result.Count;i++)penalties+=result.Get(i).Penalties;
            TestContext.WriteLine("woodland_finish_spread format="+result.Format+
                " entrants="+result.Count+
                " winner_elapsed_ms="+result.Get(0).ElapsedMilliseconds+
                " middle_rank_elapsed_ms="+result.Get(result.Count/2).ElapsedMilliseconds+
                " last_elapsed_ms="+result.Get(result.Count-1).ElapsedMilliseconds+
                " total_penalties="+penalties);
        }

        [Test]
        public void OneVenueSeasonHasSixQualifiedPursuitsAndPreservesLegacySaveIdentity()
        {
            var definitions=Definitions();var calendar=definitions.Calendar;
            Assert.That(calendar.Id,Is.EqualTo("woodland_career_calendar_v1"));
            Assert.That(calendar.Count,Is.EqualTo(16));
            int sprint=0,pursuit=0,mass=0;
            var sources=new Dictionary<string,CareerCalendarEvent>(StringComparer.Ordinal);
            for(int i=0;i<calendar.Count;i++)
            {
                var e=calendar.Get(i);
                Assert.That(e.VenueId,Is.EqualTo("woodland_basin_v1"));
                if(e.Format==CompetitionFormat.Sprint){sprint++;sources.Add(e.Id,e);}
                else if(e.Format==CompetitionFormat.MassStart)mass++;
                else
                {
                    pursuit++;
                    Assert.That(sources.ContainsKey(e.SourceEventId),Is.True);
                    Assert.That(e.Week,Is.EqualTo(sources[e.SourceEventId].Week+1));
                }
            }
            Assert.That((sprint,pursuit,mass),Is.EqualTo((6,6,4)));
            Assert.That(calendar.TrainingSlots(6),Is.EqualTo(1));
            Assert.That(calendar.TrainingSlots(8),Is.EqualTo(3));
            var save=NewCareer(definitions);
            Assert.That(save.CalendarId,Is.EqualTo(calendar.Id));
            Assert.That(CareerFactory.Restore(save.ToJson(),definitions.Roster,definitions.Calendar,
                new SeededRandomSource(save.RandomState)).ToJson(),Is.EqualTo(save.ToJson()));
            Assert.Catch(()=>CareerFactory.Restore(save.ToJson(),definitions.Roster,
                CareerFoundationTests.Calendar(),new SeededRandomSource(save.RandomState)));
            Assert.That(CareerFoundationTests.Calendar().Count,Is.EqualTo(6));
        }

        [Test]
        public void NewCareerSelectsProductionCalendarAndSavedCareerSelectsItsOwnVersion()
        {
            Func<string,string> read=PhysiologyContractTests.DefinitionJson;
            var production=CareerDefinitions.ForNewProductionCareer(read);
            var productionSave=NewCareer(production);
            Assert.That(production.Calendar.Id,Is.EqualTo("woodland_career_calendar_v1"));
            Assert.That(CareerDefinitions.ForSave(read,productionSave).Calendar.Id,
                Is.EqualTo(productionSave.CalendarId));
            var legacy=new CareerDefinitions(read);
            var legacySave=CareerFactory.Create(legacy.Roster,legacy.Calendar,
                "legacy_calendar_selection","Alex North","NOR","player_appearance",
                new SeededRandomSource(71001));
            Assert.That(CareerDefinitions.ForSave(read,legacySave).Calendar.Id,
                Is.EqualTo(legacySave.CalendarId));
            Assert.That(legacy.Calendar.Count,Is.EqualTo(6));
            var unknown=CareerSave.FromJson(productionSave.ToJson().Replace(
                "\"calendar_id\":\"woodland_career_calendar_v1\"",
                "\"calendar_id\":\"unknown_calendar_v1\""));
            Assert.That(unknown.CalendarId,Is.EqualTo("unknown_calendar_v1"));
            Assert.Throws<ArgumentException>(()=>CareerDefinitions.ForSave(read,unknown));
        }

        [Test]
        public void AllSixteenCalendarEventsAdvanceThroughOneSavedSeason()
        {
            var definitions=Definitions();var save=NewCareer(definitions);
            var races=new FixedRaceBoundary();
            for(int week=1;week<=52;week++)
            {
                Assert.That(save.Week,Is.EqualTo(week));
                save=CareerSeasonEngine.AdvanceWeek(save,definitions,
                    CareerSeasonTests.Plan(save,definitions),races,s=>new SeededRandomSource(s));
                save=CareerSave.FromJson(save.ToJson());
            }
            Assert.That(save.Season,Is.EqualTo(2));
            Assert.That(save.Week,Is.EqualTo(1));
            Assert.That(save.History(0).RaceCount,Is.EqualTo(16));
            Assert.That(save.Athlete(0).Races,Is.EqualTo(16));
        }

        [Test,Timeout(900000)]
        public void WoodlandCareerSprintUsesProductionVenueAndAuthoritativeResult()
        {
            var definitions=Definitions();var save=NewCareer(definitions);
            var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,definitions,ReadVenue);
            var first=definitions.Calendar.Get(0);
            var result=provider.Run(save,first,123);
            Assert.That(result.CalendarEventId,Is.EqualTo(first.Id));
            Assert.That(result.Count,Is.EqualTo(save.AthleteCount));
            Assert.That(result.AuthorityResultJson,Is.Not.Empty);
            Assert.That(result.AuthoritySha256,Is.EqualTo(BalanceRegression.Hash(result.AuthorityResultJson)));
            Assert.That(provider.LastShots,Is.EqualTo(save.AthleteCount*10));
            Assert.That(provider.LastTicks,Is.GreaterThan(0));
            ReportFinishSpread(result);
            TestContext.WriteLine("woodland_career_sprint_ticks="+provider.LastTicks+
                " event_hash="+provider.LastEventHash);
        }

        [Test,Timeout(900000)]
        public void WoodlandCareerPursuitQualifiesFromSavedProductionSprint()
        {
            var definitions=Definitions();var save=NewCareer(definitions);
            var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,definitions,ReadVenue);
            for(int week=1;week<=7;week++)
            {
                save=CareerSeasonEngine.AdvanceWeek(save,definitions,
                    CareerSeasonTests.Plan(save,definitions),provider,s=>new SeededRandomSource(s));
                save=CareerSave.FromJson(save.ToJson());
            }
            Assert.That(save.RaceCount,Is.EqualTo(2));
            Assert.That(save.Race(0).Format,Is.EqualTo(CompetitionFormat.Sprint));
            Assert.That(save.Race(1).Format,Is.EqualTo(CompetitionFormat.Pursuit));
            Assert.That(save.Race(0).Count,Is.EqualTo(save.AthleteCount));
            Assert.That(save.Race(1).Count,Is.EqualTo(save.AthleteCount));
            Assert.That(save.Race(1).AuthoritySha256,
                Is.EqualTo(BalanceRegression.Hash(save.Race(1).AuthorityResultJson)));
            ReportFinishSpread(save.Race(1));
            TestContext.WriteLine("woodland_career_qualified_pursuit_ticks="+provider.LastTicks+
                " event_hash="+provider.LastEventHash);
        }

        [Test,Timeout(900000)]
        public void WoodlandCareerMassStartUsesProductionStartAndResult()
        {
            var definitions=Definitions();var save=NewCareer(definitions);
            var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,definitions,ReadVenue);
            var mass=definitions.Calendar.AtWeek(10);
            Assert.That(mass.Format,Is.EqualTo(CompetitionFormat.MassStart));
            var result=provider.Run(save,mass,456);
            Assert.That(result.Count,Is.EqualTo(save.AthleteCount));
            Assert.That(result.AuthoritySha256,Is.EqualTo(BalanceRegression.Hash(result.AuthorityResultJson)));
            Assert.That(provider.LastShots,Is.EqualTo(save.AthleteCount*20));
            ReportFinishSpread(result);
            TestContext.WriteLine("woodland_career_mass_start_ticks="+provider.LastTicks+
                " event_hash="+provider.LastEventHash);
        }

        // Long production races are qualified in a separate headless process.
        // The bounded native methods above cover the Unity import/compiler path.
#if !UNITY_EDITOR
        [Test,Timeout(3600000)]
        public void CompleteWoodlandSeasonRunsAllSixteenAuthoritativeEvents()
        {
            var definitions=Definitions();var save=NewCareer(definitions);
            var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,definitions,ReadVenue);
            int completed=0,sprint=0,pursuit=0,mass=0;
            for(int week=1;week<=definitions.Calendar.WeeksPerSeason;week++)
            {
                var scheduled=definitions.Calendar.AtWeek(week);
                save=CareerSeasonEngine.AdvanceWeek(save,definitions,
                    CareerSeasonTests.Plan(save,definitions),provider,s=>new SeededRandomSource(s));
                save=CareerSave.FromJson(save.ToJson());
                if(scheduled==null)continue;
                Assert.That(save.RaceCount,Is.EqualTo(++completed),"week "+week);
                var record=save.Race(completed-1);
                Assert.That(record.CalendarEventId,Is.EqualTo(scheduled.Id));
                Assert.That(record.Format,Is.EqualTo(scheduled.Format));
                Assert.That(record.Count,Is.EqualTo(save.AthleteCount));
                Assert.That(record.AuthoritySha256,
                    Is.EqualTo(BalanceRegression.Hash(record.AuthorityResultJson)));
                Assert.That(provider.LastTicks,Is.GreaterThan(0));
                if(record.Format==CompetitionFormat.Sprint)sprint++;
                else if(record.Format==CompetitionFormat.Pursuit)pursuit++;
                else mass++;
                // The record/save has already been serialized and restored. The
                // completed runner is local to the provider call; reclaim its
                // temporary field graph before starting the next test race.
                GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
                GC.WaitForPendingFinalizers();
                TestContext.WriteLine("woodland_season_event="+record.CalendarEventId+
                    " ticks="+provider.LastTicks+" event_hash="+provider.LastEventHash+
                    " retained_mb="+Math.Round(GC.GetTotalMemory(false)/1048576.0,1));
            }
            Assert.That((completed,sprint,pursuit,mass),Is.EqualTo((16,6,6,4)));
            Assert.That(save.Season,Is.EqualTo(2));
            Assert.That(save.History(0).RaceCount,Is.EqualTo(16));
        }
#endif
    }
}
