using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerFormTests
    {
        public static CareerFormDefinition Definition()=>CareerFormDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_form_v1.json"));
        [Test] public void ProductiveTrainingAndTaperPeakWithoutChangingPermanentSkills()
        {
            var a=CareerFoundationTests.Create().Athlete(0);var d=Definition();var stats=new int[CareerUnits.StatCount];for(int i=0;i<stats.Length;i++)stats[i]=a.Stat((CareerStat)i);
            int before=a.Form;for(int week=0;week<8;week++)CareerForm.Advance(a,d,true,false);Assert.That(a.Form,Is.GreaterThan(before));
            int trained=a.Form;for(int week=0;week<3;week++)CareerForm.Advance(a,d,false,true);Assert.That(a.Form,Is.GreaterThan(trained));
            for(int i=0;i<stats.Length;i++)Assert.That(a.Stat((CareerStat)i),Is.EqualTo(stats[i]));
            Assert.That(CareerForm.RaceMultiplier(a,d),Is.InRange(.92,1.04));
            for(int week=0;week<20;week++)CareerForm.Advance(a,d,false,false);Assert.That(a.Form,Is.LessThan(trained));
        }
        [Test] public void ExcessFatigueSuppressesPeakingAndStateRoundTrips()
        {
            var save=CareerFoundationTests.Create();var a=save.Athlete(0);var d=Definition();
            for(int i=0;i<10;i++)CareerFatigue.Training(a,CareerTrainingTests.Definition().Get(CareerTrainingKind.Intensity));
            int before=a.Form;CareerForm.Advance(a,d,true,true);Assert.That(a.Form,Is.LessThan(before));
            Assert.That(CareerSave.FromJson(save.ToJson()).Athlete(0).Form,Is.EqualTo(a.Form));
            Assert.Catch(()=>CareerFormDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.DefinitionJson("career_form_v1.json"),"overload_fatigue","10000")));
        }
    }
}
