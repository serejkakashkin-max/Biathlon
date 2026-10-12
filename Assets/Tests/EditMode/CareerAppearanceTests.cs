using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerAppearanceTests
    {
        private static CareerCalendar Calendar(bool worldCup = true) => CareerCalendar.FromJson(
            PhysiologyContractTests.DefinitionJson("career_calendar_v1.json").Replace("\"schema_version\": 1,", "\"schema_version\": 1,\"circuit\": " + (worldCup ? "3" : "0") + ","));
        private static CareerSave Save(CareerCalendar calendar) => CareerFactory.Create(CareerFoundationTests.Rules(), calendar, "appearance_test", "Alex North", "NOR", "player_appearance", new SeededRandomSource(71000));
        private static CareerScoringDefinition Scoring() => CareerScoringDefinition.FromJson("{\"schema_version\":1,\"id\":\"appearance_scoring\",\"rank_points\":[6,4,3,2,1]}");
        private static void Register(CareerSave save, CareerCalendar calendar, int eventIndex, params int[] order)
        {
            var definition = calendar.Get(eventIndex); var finishes = new CareerFinish[order.Length];
            for (int i = 0; i < order.Length; i++) finishes[i] = new CareerFinish(save.Athlete(order[i]).Id, 100000 + i * 1000, 100000 + order[i] * 50000);
            CareerStandings.Register(save, new CareerRaceRecord("appearance_race_" + eventIndex, save.SeasonId, definition.Id, definition.Format, 1, new string('a', 64), finishes));
        }
        [Test] public void NoResultsAndRegionalCircuitsCannotInventWorldCupLeaders()
        {
            var calendar = Calendar(); var save = Save(calendar);
            Assert.That(CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(0)).ForAthlete(save.PlayerId), Is.EqualTo(CareerLeaderBib.None));
            calendar = Calendar(false); save = Save(calendar); Register(save, calendar, 0, 0, 1, 2);
            Assert.That(CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(3)).ForAthlete(save.PlayerId), Is.EqualTo(CareerLeaderBib.None));
        }
        [Test] public void YellowAndCurrentDisciplineRedComeFromDifferentActualClassifications()
        {
            var calendar = Calendar(); var save = Save(calendar);
            Register(save, calendar, 0, 0, 1, 2); Register(save, calendar, 1, 1, 2, 0);
            var leaders = CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(3));
            Assert.That(leaders.ForAthlete(save.Athlete(0).Id), Is.EqualTo(CareerLeaderBib.Discipline));
            Assert.That(leaders.ForAthlete(save.Athlete(1).Id), Is.EqualTo(CareerLeaderBib.Total));
            Assert.That(leaders.ForAthlete(save.Athlete(2).Id), Is.EqualTo(CareerLeaderBib.None));
            var pursuit = CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(4));
            Assert.That(pursuit.ForAthlete(save.Athlete(1).Id), Is.EqualTo(CareerLeaderBib.Total | CareerLeaderBib.Discipline));
        }
        [Test] public void EqualPointsBreakBySecondThenThirdPlacesAndNeverByElapsedTime()
        {
            var calendar = Calendar(); var save = Save(calendar);
            Register(save, calendar, 0, 0, 3, 1, 2); // A first, B third.
            Register(save, calendar, 1, 1, 0, 3, 2); // B first, A second.
            Register(save, calendar, 2, 2, 3, 1, 0); // B third, A fourth: both twelve points and one win.
            var leaders = CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(3));
            Assert.That(leaders.ForAthlete(save.Athlete(0).Id), Is.EqualTo(CareerLeaderBib.Total | CareerLeaderBib.Discipline));
            Assert.That(leaders.ForAthlete(save.Athlete(1).Id), Is.EqualTo(CareerLeaderBib.None));
        }
        [Test] public void RedBibCannotCarryOverFromSprintIntoTheFirstMassStart()
        {
            var calendar=Calendar();var save=Save(calendar);Register(save,calendar,0,0,1,2);
            var before=save.ToJson();var firstMass=CareerLeaderBibs.Capture(save,calendar,Scoring(),calendar.Get(2));
            Assert.That(firstMass.ForAthlete(save.Athlete(0).Id),Is.EqualTo(CareerLeaderBib.Total));
            Assert.That(save.ToJson(),Is.EqualTo(before),"Pre-start bib capture must leave saves and RNG unchanged.");
            Register(save,calendar,1,1,2,0);
            var nextSprint=CareerLeaderBibs.Capture(save,calendar,Scoring(),calendar.Get(3));
            Assert.That(nextSprint.ForAthlete(save.Athlete(0).Id),Is.EqualTo(CareerLeaderBib.Discipline));
            Assert.That(nextSprint.ForAthlete(save.Athlete(1).Id),Is.EqualTo(CareerLeaderBib.Total));
        }
        [Test] public void UnresolvedTiesRemainLeadersTogetherAndCurrentEventNeverChangesItsOwnBib()
        {
            var calendar = Calendar(); var save = Save(calendar);
            Register(save, calendar, 0, 0, 1); Register(save, calendar, 1, 1, 0);
            var leaders = CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(3));
            Assert.That(leaders.ForAthlete(save.Athlete(0).Id) & CareerLeaderBib.Total, Is.EqualTo(CareerLeaderBib.Total));
            Assert.That(leaders.ForAthlete(save.Athlete(1).Id) & CareerLeaderBib.Total, Is.EqualTo(CareerLeaderBib.Total));
            Register(save, calendar, 3, 1, 0);
            var sameStart = CareerLeaderBibs.Capture(save, calendar, Scoring(), calendar.Get(3));
            Assert.That(sameStart.ForAthlete(save.Athlete(0).Id), Is.EqualTo(leaders.ForAthlete(save.Athlete(0).Id)));
            Assert.That(sameStart.ForAthlete(save.Athlete(1).Id), Is.EqualTo(leaders.ForAthlete(save.Athlete(1).Id)));
        }
        [Test] public void EquipmentPaletteCoversTheAuthorityCatalogAndPersistsByStableSelectedId()
        {
            var visuals = SkiAppearanceCatalog.FromJson(PhysiologyContractTests.DefinitionJson("UI/ski_appearance_v1.json"));
            var equipment = SkiEquipmentCatalog.FromJson(PhysiologyContractTests.DefinitionJson("ski_equipment_provisional_v1.json"));
            Assert.That(visuals.Count, Is.EqualTo(3));
            for (int i = 0; i < visuals.Count; i++) Assert.That(equipment.GetSki(visuals.GetAt(i).Id).Id, Is.EqualTo(visuals.GetAt(i).Id));
            Assert.That(visuals.Get("ski_glide").BaseColorHex, Is.EqualTo("#E34A45"));
            Assert.Throws<ArgumentException>(() => visuals.Get("missing_ski"));
            Assert.Catch(() => SkiAppearanceCatalog.FromJson("{\"schema_version\":1,\"id\":\"bad\",\"skis\":[{\"id\":\"ski\",\"base_color\":\"red\",\"accent_color\":\"#FFFFFF\"}]}"));
        }
    }
}
