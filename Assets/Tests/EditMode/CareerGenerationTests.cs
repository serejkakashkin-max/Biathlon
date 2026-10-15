using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerGenerationTests
    {
        public static CareerGenerationDefinition Definition()=>CareerGenerationDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_generation_v1.json"));
        [Test] public void LegacyCalendarCanUseIndividualRetirementWithoutChangingItsActivePopulation()
        {
            var d=CareerSeasonTests.Definitions();var save=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"age_years","37"));
            CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s));var expected=new HashSet<string>();
            for(int i=1;i<save.AthleteCount;i++)if(save.Athlete(i).Age>=save.Athlete(i).Growth.RetirementAge)expected.Add(save.Athlete(i).Id);
            string before=save.ToJson();var result=CareerGeneration.ReplaceRetirees(save,d.Roster,d.Generation,s=>new SeededRandomSource(s),d.Growth,d.Ratings);
            Assert.That(expected.Count,Is.GreaterThan(0));Assert.That(result.RetiredCount,Is.EqualTo(expected.Count));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.That(result.Save.AthleteCount,Is.EqualTo(30));Assert.That(result.Save.NextAthleteOrdinal,Is.EqualTo(30+expected.Count));
            for(int i=0;i<result.RetiredCount;i++)Assert.That(expected.Contains(result.RetiredId(i)),Is.True);
            for(int i=0;i<30;i++)Assert.That(result.Save.Athlete(i).Growth,Is.Not.Null);
            Assert.That(CareerSave.FromJson(result.Save.ToJson()).ToJson(),Is.EqualTo(result.Save.ToJson()));
        }
        [Test] public void FiftyYearsPreservePopulationAndNeverReuseRetiredIdentities()
        {
            var save=CareerFoundationTests.Create();var seen=new HashSet<string>();for(int i=0;i<save.AthleteCount;i++)seen.Add(save.Athlete(i).Id);
            int retired=0;string player=save.PlayerId;
            for(int year=0;year<50;year++)
            {
                for(int i=0;i<save.AthleteCount;i++)CareerAgeing.AdvanceYear(save.Athlete(i),CareerAdaptationTests.Definition());
                string before=save.ToJson();var result=CareerGeneration.ReplaceRetirees(save,CareerFoundationTests.Rules(),Definition(),s=>new SeededRandomSource(s));
                var repeat=CareerGeneration.ReplaceRetirees(CareerSave.FromJson(before),CareerFoundationTests.Rules(),Definition(),s=>new SeededRandomSource(s));
                Assert.That(save.ToJson(),Is.EqualTo(before));Assert.That(result.Save.ToJson(),Is.EqualTo(repeat.Save.ToJson()));
                var old=new HashSet<string>();for(int i=0;i<save.AthleteCount;i++)old.Add(save.Athlete(i).Id);
                save=CareerSave.FromJson(result.Save.ToJson());retired+=result.RetiredCount;
                Assert.That(save.AthleteCount,Is.EqualTo(30));Assert.That(save.PlayerId,Is.EqualTo(player));
                for(int i=1;i<save.AthleteCount;i++)
                {
                    var athlete=save.Athlete(i);Assert.That(athlete.Age,Is.LessThan(38));
                    if(!old.Contains(athlete.Id)){Assert.That(seen.Add(athlete.Id),Is.True);Assert.That(athlete.Age,Is.InRange(18,20));Assert.That(athlete.Stat(CareerStat.Endurance),Is.InRange(35000,55000));}
                }
            }
            Assert.That(retired,Is.GreaterThan(58));Assert.That(save.NextAthleteOrdinal,Is.EqualTo(30+retired));
        }
        [Test] public void UnarchivedResultsAndWrongRandomCheckpointAreRejectedWithoutMutation()
        {
            var save=CareerFoundationTests.Create();string before=save.ToJson();
            Assert.Catch(()=>CareerGeneration.ReplaceRetirees(save,CareerFoundationTests.Rules(),Definition(),s=>new SeededRandomSource(1)));Assert.That(save.ToJson(),Is.EqualTo(before));
            CareerStandings.Register(save,CareerStandingsTests.Record(save,"opening_sprint",Biathlon.Domain.CompetitionFormat.Sprint));before=save.ToJson();
            Assert.Catch(()=>CareerGeneration.ReplaceRetirees(save,CareerFoundationTests.Rules(),Definition(),s=>new SeededRandomSource(s)));Assert.That(save.ToJson(),Is.EqualTo(before));
        }
    }
}
