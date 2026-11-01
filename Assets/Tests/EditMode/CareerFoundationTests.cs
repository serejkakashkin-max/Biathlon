using System;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerFoundationTests
    {
        public static CareerRosterDefinition Rules()=>CareerRosterDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_roster_v1.json"));
        public static CareerCalendar Calendar()=>CareerCalendar.FromJson(PhysiologyContractTests.DefinitionJson("career_calendar_v1.json"));
        public static CareerSave Create(ulong seed=71000)=>CareerFactory.Create(Rules(),Calendar(),"career_test","Alex North","NOR","player_appearance",new SeededRandomSource(seed));
        [Test] public void SeededPlayerRosterAndVersionedSaveRoundTripExactly()
        {
            var a=Create();var b=Create();Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));Assert.That(Create(71001).ToJson(),Is.Not.EqualTo(a.ToJson()));
            var restored=CareerFactory.Restore(a.ToJson(),Rules(),Calendar(),new SeededRandomSource(a.RandomState));
            Assert.That(restored.ToJson(),Is.EqualTo(a.ToJson()));Assert.That(restored.AthleteCount,Is.EqualTo(30));
            Assert.That(restored.Athlete(0).Name,Is.EqualTo("Alex North"));Assert.That(restored.Athlete(0).AppearanceId,Is.EqualTo("player_appearance"));
            Assert.That(restored.Athlete(1).AppearanceId,Is.EqualTo("appearance_default"));
            Assert.That(restored.Athlete(0).Age,Is.EqualTo(Rules().PlayerAge));
            for(int i=0;i<restored.AthleteCount;i++)
            {
                var athlete=restored.Athlete(i);Assert.That(athlete.Id,Is.EqualTo("career_test_athlete_"+i));
                for(int stat=0;stat<CareerUnits.StatCount;stat++)Assert.That(athlete.Stat((CareerStat)stat),Is.InRange(0,athlete.Potential));
            }
            Assert.Catch(()=>CareerFactory.Restore(a.ToJson(),Rules(),Calendar(),new SeededRandomSource(1)));
            TestContext.Out.WriteLine("career save sha256="+BalanceRegression.Hash(a.ToJson()));
            // Keep the original v2 ability/RNG fixture; additive appearance metadata
            // and the explicit version bump do not silently rebaseline race state.
            string legacyShape=Regex.Replace(a.ToJson(),"\"appearance_profile\":\\{[^{}]*\\},","");
            legacyShape=M166EquipmentTests.WithoutRifles(legacyShape);
            legacyShape=PhysiologyContractTests.ReplaceNumber(legacyShape,"money_minor","200000");
            legacyShape=PhysiologyContractTests.ReplaceNumber(legacyShape,"save_version","2");
            Assert.That(BalanceRegression.Hash(legacyShape),Is.EqualTo(PhysiologyContractTests.ProjectFile("tests/fixtures/career-foundation-v1.sha256").Trim()));
        }
        [Test] public void LegacyConfidenceMigrationIsExplicitAndZeroSurvivesCurrentSaves()
        {
            string json=Create().ToJson();string legacy=PhysiologyContractTests.ReplaceNumber(json,"save_version","1");legacy=Regex.Replace(legacy,"\"confidence\":-?[0-9]+,","");
            var migrated=CareerSave.FromJson(legacy);Assert.That(migrated.SaveVersion,Is.EqualTo(CareerSave.CurrentVersion));
            for(int i=0;i<migrated.AthleteCount;i++)Assert.That(migrated.Athlete(i).Confidence,Is.EqualTo(migrated.Athlete(i).Morale));
            var zero=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"confidence","0"));Assert.That(CareerSave.FromJson(zero.ToJson()).Athlete(0).Confidence,Is.Zero);
            Assert.Catch(()=>CareerSave.FromJson(Regex.Replace(json,"\"confidence\":-?[0-9]+,","")));
            Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"save_version","999")));
        }
        [Test] public void InvalidIdentityAllocationAndStateAreRejectedBeforeUse()
        {
            string json=Create().ToJson();
            Assert.Catch(()=>CareerSave.FromJson(json.Replace("career_test_athlete_1\"","career_test_athlete_0\"")));
            Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"next_athlete_ordinal","29")));
            Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"fatigue","10001")));
            Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"money_minor","-1")));
            var random=new SeededRandomSource(7);Assert.Catch(()=>CareerFactory.Create(Rules(),Calendar(),"career","Name","INVALID","appearance",random));Assert.That(random.State,Is.EqualTo(7));
            Assert.Catch(()=>CareerRosterDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Rules().ToJson(),"population","0")));
        }
        [Test] public void CalendarHasTrainingRestWindowsAndValidPursuitDependencies()
        {
            var calendar=Calendar();Assert.That(calendar.WeeksPerSeason,Is.EqualTo(52));Assert.That(calendar.Count,Is.EqualTo(6));
            Assert.That(calendar.TrainingSlots(1),Is.EqualTo(3));Assert.That(calendar.TrainingSlots(8),Is.EqualTo(1));
            Assert.That(calendar.AtWeek(1),Is.Null);Assert.That(calendar.AtWeek(9).SourceEventId,Is.EqualTo("opening_sprint"));
            Assert.That(CareerCalendar.FromJson(calendar.ToJson()).ToJson(),Is.EqualTo(calendar.ToJson()));
            Assert.Catch(()=>CareerCalendar.FromJson(calendar.ToJson().Replace("\"source_event_id\":\"opening_sprint\"","\"source_event_id\":\"missing\"")));
            Assert.Catch(()=>CareerCalendar.FromJson(PhysiologyContractTests.ReplaceNumber(calendar.ToJson(),"weeks_per_season","1")));
        }
    }
}
