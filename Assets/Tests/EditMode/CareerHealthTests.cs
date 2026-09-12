using Biathlon.Career;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerHealthTests
    {
        public static CareerHealthDefinition Definition()=>CareerHealthDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_health_v1.json"));
        [Test] public void RiskIsBoundedObservableAndDoesNotApplyPunitiveInjury()
        {
            var save=CareerFoundationTests.Create();var a=save.Athlete(0);var d=Definition();int health=a.Health;
            int rested=CareerHealth.Evaluate(a,d,80).TotalRisk;
            for(int i=0;i<10;i++)CareerFatigue.Training(a,CareerTrainingTests.Definition().Get(CareerTrainingKind.Intensity));
            int skill=a.Stat(CareerStat.Endurance);var frame=CareerHealth.RecordRisk(a,d,80);
            Assert.That(frame.TotalRisk,Is.GreaterThan(rested));Assert.That(frame.TotalRisk,Is.LessThanOrEqualTo(d.MaximumRisk));Assert.That(frame.FatigueRisk,Is.GreaterThan(0));
            Assert.That(a.Health,Is.EqualTo(health));Assert.That(a.Stat(CareerStat.Endurance),Is.EqualTo(skill));
            Assert.That(CareerSave.FromJson(save.ToJson()).Athlete(0).HealthRisk,Is.EqualTo(frame.TotalRisk));
            string before=a.ToJson();Assert.Catch(()=>CareerHealth.RecordRisk(a,d,-1));Assert.That(a.ToJson(),Is.EqualTo(before));
        }
    }
}
