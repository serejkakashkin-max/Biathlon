using Biathlon.Career;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerAgeingTests
    {
        [Test] public void ProspectsGrowThroughTrainingAndVeteransDeclineOnTheSameAuthoredCurve()
        {
            var young=CareerFoundationTests.Create().Athlete(0);var old=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"age_years","36")).Athlete(0);
            var d=CareerAdaptationTests.Definition();int youngSkill=young.Stat(CareerStat.Endurance),oldSkill=old.Stat(CareerStat.Endurance);
            Assert.That(CareerAgeing.AdvanceYear(young,d).Decline,Is.Zero);Assert.That(young.Stat(CareerStat.Endurance),Is.EqualTo(youngSkill));
            var veteran=CareerAgeing.AdvanceYear(old,d);Assert.That(veteran.Decline,Is.GreaterThan(0));Assert.That(old.Stat(CareerStat.Endurance),Is.LessThan(oldSkill));
            Assert.That(d.TrainingFactor(young.Age),Is.GreaterThan(d.TrainingFactor(old.Age)));
            for(int i=0;i<60;i++)CareerAgeing.AdvanceYear(old,d);for(int i=0;i<CareerUnits.StatCount;i++)Assert.That(old.Stat((CareerStat)i),Is.GreaterThanOrEqualTo(0));
        }
    }
}
