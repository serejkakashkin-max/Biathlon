using System;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerDashboardSummaryTests
    {
        private static CareerDefinitions Definitions()=>CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson);
        private static CareerSave Save(CareerDefinitions d)=>CareerFactory.Create(d.Roster,d.Calendar,"dashboard_test","Alex","NOR","player_appearance",new SeededRandomSource(81000));
        [Test] public void NewCareerCalendarUsesProductionDataAndHasNoInventedRanking()
        {
            var d=Definitions();var save=Save(d);string before=save.ToJson();var model=new CareerDashboardSummary(save,d);
            Assert.That(model.CalendarCount,Is.EqualTo(16));Assert.That(model.Standing,Is.Null);Assert.That(model.Points,Is.Zero);
            Assert.That(model.Next.Event.Id,Is.EqualTo("woodland_open_sprint"));Assert.That(model.Next.Event.Week,Is.EqualTo(6));
            Assert.That(model.Player.Form,Is.EqualTo(save.Athlete(0).Form));Assert.That(save.ToJson(),Is.EqualTo(before));
        }
        [Test] public void CompletedCalendarAndRankingComeFromSavedRaceAndDoNotSkipUpcomingEvent()
        {
            var d=Definitions();var save=Save(d);var result=CareerStandingsTests.Record(save,"woodland_open_sprint",CompetitionFormat.Sprint);
            CareerStandings.Register(save,result);string before=save.ToJson();var model=new CareerDashboardSummary(save,d);
            Assert.That(model.Calendar(0).Completed,Is.True);Assert.That(model.Calendar(0).PlayerRank,Is.EqualTo(1));
            Assert.That(model.Calendar(0).PlayerPoints,Is.EqualTo(d.Scoring.Points(1)));Assert.That(model.Standing,Is.EqualTo(1));
            Assert.That(model.Points,Is.EqualTo(d.Scoring.Points(1)));Assert.That(model.Wins,Is.EqualTo(1));Assert.That(model.Podiums,Is.EqualTo(1));
            Assert.That(model.Next.Event.Id,Is.EqualTo("woodland_open_pursuit"));Assert.That(save.ToJson(),Is.EqualTo(before));
        }
        [Test] public void LegacyCalendarIsPreservedAndMismatchedDefinitionsAreRejected()
        {
            var legacy=new CareerDefinitions(PhysiologyContractTests.DefinitionJson);var save=Save(legacy);
            Assert.That(new CareerDashboardSummary(save,legacy).CalendarCount,Is.EqualTo(legacy.Calendar.Count));
            Assert.Throws<ArgumentException>(()=>new CareerDashboardSummary(save,Definitions()));
        }
    }
}
