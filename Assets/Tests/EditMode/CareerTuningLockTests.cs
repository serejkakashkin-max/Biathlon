using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerTuningLockTests
    {
        [Test] public void CareerBaselineIncludesAllCareerDefinitionsAndPreservesTheM8Lock()
        {
            var d=BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/career-tuning-lock-v2.json"));
            Assert.That(d.Id,Is.EqualTo("biathlon_career_tuning_v2"));Assert.That(TuningLockVerification.Verify(d,PhysiologyContractTests.ProjectFile),Is.Empty);
            var historical=BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/career-tuning-lock-v1.json"));
            Assert.That(TuningLockVerification.Verify(historical,PhysiologyContractTests.ProjectFile),Is.EquivalentTo(new[]{"Changed: Assets/Resources/Biathlon/career_economy_v1.json","Changed: Assets/Resources/Biathlon/career_roster_v1.json"}),"M16.6 authorizes these two existing financial definitions only; preserve the original manifest.");
            var old=BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/tuning-lock-v1.json"));Assert.That(TuningLockVerification.Verify(old,PhysiologyContractTests.ProjectFile),Is.Empty);
            string[] names={"adaptation","calendar","economy","fatigue","form","generation","health","history","mental","progression","race","roster","scoring","training","training_policy"};
            foreach(string name in names){bool found=false;for(int i=0;i<d.Count;i++)if(d.Get(i).Path=="Assets/Resources/Biathlon/career_"+name+"_v1.json")found=true;Assert.That(found,Is.True,name);}
            Assert.That(d.Count,Is.EqualTo(old.Count+29),"All28current career/weather inputs plus the metadata-only calibration revision supplement the unchanged M8lock.");
            foreach(string file in new[]{"career_ski_models_v2.json","career_rifles_v1.json","career_equipment_policy_v1.json","race_weather_policy_v1.json"})
            {bool found=false;for(int i=0;i<d.Count;i++)found|=d.Get(i).Path=="Assets/Resources/Biathlon/"+file;Assert.That(found,Is.True,file);}
        }
    }
}
