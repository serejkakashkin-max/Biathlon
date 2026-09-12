using Biathlon.Career;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerMentalTests
    {
        public static CareerMentalDefinition Definition()=>CareerMentalDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_mental_v1.json"));
        [Test] public void OutcomesChangeBoundedConfidenceAndPressureWithoutPowerOrStatChanges()
        {
            var good=CareerFoundationTests.Create().Athlete(0);var bad=CareerFoundationTests.Create().Athlete(0);var d=Definition();int skill=good.Stat(CareerStat.Endurance);
            CareerMental.Race(good,d,1,15,30);CareerMental.Race(bad,d,30,15,30);
            Assert.That(good.Confidence,Is.GreaterThan(bad.Confidence));Assert.That(CareerMental.Pressure(good,d),Is.LessThan(CareerMental.Pressure(bad,d)));
            for(int i=0;i<1000;i++){CareerMental.Race(good,d,1,30,30);CareerMental.Race(bad,d,30,1,30);}
            Assert.That(good.Confidence,Is.EqualTo(10000));Assert.That(bad.Confidence,Is.Zero);Assert.That(CareerMental.Pressure(bad,d),Is.LessThanOrEqualTo(.35));Assert.That(good.Stat(CareerStat.Endurance),Is.EqualTo(skill));
            string before=bad.ToJson();Assert.Catch(()=>CareerMental.Race(bad,d,31,15,30));Assert.That(bad.ToJson(),Is.EqualTo(before));
            CareerMental.Rest(bad,d);Assert.That(bad.Confidence,Is.GreaterThan(0));int morale=bad.Morale;CareerMental.Training(bad,d,true);Assert.That(bad.Morale,Is.GreaterThan(morale));
        }
    }
}
