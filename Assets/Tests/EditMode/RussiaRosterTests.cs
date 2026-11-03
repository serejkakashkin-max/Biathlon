using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RussiaRosterTests
    {
        [Test] public void NewRosterSupportsRussiaAndLegacySavesKeepTheirOriginalDefinition()
        {
            var current=CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson);
            var legacy=new CareerDefinitions(PhysiologyContractTests.DefinitionJson);
            Assert.That(current.Roster.HasNation("RUS"),Is.True);Assert.That(current.Roster.Id,Is.EqualTo("career_roster_v2"));
            Assert.That(legacy.Roster.NationCount,Is.EqualTo(6));Assert.That(legacy.Roster.HasNation("RUS"),Is.False);
            foreach(var roster in new[]{legacy.Roster,current.Roster})
            {
                var save=CareerFactory.Create(roster,current.Calendar,"nation_restore","Alex",roster.HasNation("RUS")?"RUS":"NOR","appearance_default",new SeededRandomSource(11));
                string before=save.ToJson();var definitions=CareerDefinitions.ForSave(PhysiologyContractTests.DefinitionJson,save);
                var restored=CareerFactory.Restore(before,definitions.Roster,definitions.Calendar,new SeededRandomSource(save.RandomState));
                Assert.That(restored.ToJson(),Is.EqualTo(before));Assert.That(definitions.Roster.Id,Is.EqualTo(roster.Id));
            }
        }
        [Test] public void AppendingANationDoesNotChangeGeneratedAbilitiesMoneyOrRandomState()
        {
            var current=CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson);
            var legacy=new CareerDefinitions(PhysiologyContractTests.DefinitionJson);
            var a=CareerFactory.Create(legacy.Roster,current.Calendar,"nation_parity","Alex","NOR","appearance_default",new SeededRandomSource(11));
            var b=CareerFactory.Create(current.Roster,current.Calendar,"nation_parity","Alex","NOR","appearance_default",new SeededRandomSource(11));
            Assert.That(a.RandomState,Is.EqualTo(b.RandomState));Assert.That(a.AthleteCount,Is.EqualTo(b.AthleteCount));
            bool russian=false;
            for(int i=0;i<a.AthleteCount;i++)
            {
                var x=a.Athlete(i);var y=b.Athlete(i);russian|=y.Nation=="RUS";
                Assert.That(x.Name,Is.EqualTo(y.Name));Assert.That(x.Age,Is.EqualTo(y.Age));Assert.That(x.Money,Is.EqualTo(y.Money));
                Assert.That(x.Potential,Is.EqualTo(y.Potential));for(int s=0;s<CareerUnits.StatCount;s++)Assert.That(x.Stat((CareerStat)s),Is.EqualTo(y.Stat((CareerStat)s)));
            }
            Assert.That(russian,Is.True);
        }
    }
}
