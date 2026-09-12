using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerAdaptationTests
    {
        public static CareerAdaptationDefinition Definition()=>CareerAdaptationDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_adaptation_v1.json"));
        private static CareerAthlete Changed(string field,string value)=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),field,value)).Athlete(0);
        [Test] public void QualityRecoveryAgeAndPotentialHaveMeasuredAdaptationEffects()
        {
            var session=CareerTrainingTests.Definition().Get(CareerTrainingKind.Endurance);var d=Definition();
            int Gain(CareerAthlete a,int q=10000)=>CareerAdaptation.Apply(a,session,d,q).TotalGain;
            int normal=Gain(CareerFoundationTests.Create().Athlete(0));Assert.That(normal,Is.GreaterThan(0));
            Assert.That(Gain(CareerFoundationTests.Create().Athlete(0),5000),Is.LessThan(normal));
            Assert.That(Gain(Changed("fatigue","9000")),Is.LessThan(normal));
            Assert.That(Gain(Changed("age_years","40")),Is.LessThan(normal));
            Assert.That(Gain(Changed("potential","100000")),Is.GreaterThanOrEqualTo(normal));
            Assert.That(Gain(CareerFoundationTests.Create().Athlete(0),0),Is.Zero);
        }
        [Test] public void DiminishingReturnsCapStatsAndInvalidQualityIsAtomic()
        {
            var a=CareerFoundationTests.Create().Athlete(0);var session=CareerTrainingTests.Definition().Get(CareerTrainingKind.Endurance);var d=Definition();
            int first=CareerAdaptation.Apply(a,session,d,10000).TotalGain;
            for(int i=0;i<2000;i++)CareerAdaptation.Apply(a,session,d,10000);
            Assert.That(CareerAdaptation.Apply(a,session,d,10000).TotalGain,Is.LessThan(first));
            for(int i=0;i<CareerUnits.StatCount;i++)Assert.That(a.Stat((CareerStat)i),Is.LessThanOrEqualTo(a.Potential));
            string before=a.ToJson();Assert.Catch(()=>CareerAdaptation.Apply(a,session,d,12001));Assert.That(a.ToJson(),Is.EqualTo(before));
            Assert.Catch(()=>CareerAdaptationDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),"minimum_recovery_factor","-1")));
        }
    }
}
