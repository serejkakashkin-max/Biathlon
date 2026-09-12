using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerTuningLockTests
    {
        [Test] public void CareerBaselineIncludesAllCareerDefinitionsAndPreservesTheM8Lock()
        {
            var d=BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/career-tuning-lock-v1.json"));
            Assert.That(d.Id,Is.EqualTo("biathlon_career_tuning_v1"));Assert.That(TuningLockVerification.Verify(d,PhysiologyContractTests.ProjectFile),Is.Empty);
            var old=BalanceTuningLock.FromJson(PhysiologyContractTests.ProjectFile("docs/balance/tuning-lock-v1.json"));Assert.That(TuningLockVerification.Verify(old,PhysiologyContractTests.ProjectFile),Is.Empty);
            string[] names={"adaptation","calendar","economy","fatigue","form","generation","health","history","mental","progression","race","roster","scoring","training","training_policy"};
            foreach(string name in names){bool found=false;for(int i=0;i<d.Count;i++)if(d.Get(i).Path=="Assets/Resources/Biathlon/career_"+name+"_v1.json")found=true;Assert.That(found,Is.True,name);}
            Assert.That(d.Count,Is.EqualTo(old.Count+names.Length));
        }
    }
}
