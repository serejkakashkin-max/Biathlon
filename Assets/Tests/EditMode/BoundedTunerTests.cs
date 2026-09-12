using System;
using System.Collections.Generic;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BoundedTunerTests
    {
        private static BoundedTuningDefinition Definition()=>BoundedTuningDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/shooting-tuner-v1.json"));
        [Test] public void SearchIsBoundedReadOnlyAndTiesUseDeclaredOrder()
        {
            var d=Definition();int calls=0;
            var p=BoundedTuner.Search(d,values=>
            {
                calls++;Assert.That(values["reaction_seconds"],Is.InRange(.2,.35));
                Assert.Throws<NotSupportedException>(()=>((IDictionary<string,double>)values).Add("hit_probability",1));
                return new Dictionary<string,double>{{"prone_hit_rate",.88},{"standing_hit_rate",.805}};
            });
            Assert.That(calls,Is.EqualTo(32));Assert.That(p.TargetsMet,Is.True);
            Assert.That(p.Values["reaction_seconds"],Is.EqualTo(.2));Assert.That(p.Values["tracking_error_rad"],Is.EqualTo(.00015));
            Assert.That(Definition().ToJson(),Is.EqualTo(d.ToJson()));
        }
        [Test] public void UndeclaredParametersBudgetsAndInvalidMetricsFailClosed()
        {
            string json=Definition().ToJson();
            Assert.Catch(()=>BoundedTuningDefinition.FromJson(json.Replace("reaction_seconds","hit_probability")));
            Assert.Catch(()=>BoundedTuningDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"maximum_evaluations","31")));
            Assert.Catch(()=>BoundedTuningDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"initial","99")));
            Assert.Catch(()=>BoundedTuner.Search(Definition(),v=>new Dictionary<string,double>()));
            Assert.Catch(()=>BoundedTuner.Search(Definition(),v=>new Dictionary<string,double>{{"prone_hit_rate",double.NaN},{"standing_hit_rate",.8}}));
            Assert.Catch(()=>BoundedTuner.Search(Definition(),v=>new Dictionary<string,double>{{"prone_hit_rate",double.MaxValue},{"standing_hit_rate",.8}}));
            var p=BoundedTuner.Search(Definition(),v=>new Dictionary<string,double>{{"prone_hit_rate",0},{"standing_hit_rate",0}});
            Assert.That(p.TargetsMet,Is.False);
        }
#if UNITY_EDITOR
        [UnityEngine.TestTools.UnityTest,Timeout(300000)] public System.Collections.IEnumerator GeometricTrainingSearchReproducesSelectedProfile()
        {var work=System.Threading.Tasks.Task.Run((Action)Run);while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}
#else
        [Test] public void GeometricTrainingSearchReproducesSelectedProfile()=>Run();
#endif
        private static void Run()
        {
            var suite=ShootingBalanceSuite.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/shooting-tuner-training-v1.json"));
            var evaluator=new ShootingTuningEvaluator(suite,PhysiologyContractTests.DefinitionJson);
            var proposal=BoundedTuner.Search(Definition(),evaluator.Evaluate);
            Assert.That(proposal.TargetsMet,Is.True);Assert.That(proposal.Evaluations,Is.EqualTo(32));
            Assert.That(proposal.Values["reaction_seconds"],Is.EqualTo(.25));Assert.That(proposal.Values["tracking_error_rad"],Is.EqualTo(.0002));
            Assert.That(proposal.Metrics["prone_hit_rate"],Is.EqualTo(.865));Assert.That(proposal.Metrics["standing_hit_rate"],Is.EqualTo(.855));
        }
    }
}
