using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerSoakClimbRegressionTests
    {
#if UNITY_EDITOR
        [UnityEngine.TestTools.UnityTest,Timeout(900000)] public System.Collections.IEnumerator ActualSecondSeasonMassStartFinishesTwiceWithIdenticalAuthority()
        {var work=System.Threading.Tasks.Task.Run((Action)Run);while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}
#else
        [Test,Timeout(900000)] public void ActualSecondSeasonMassStartFinishesTwiceWithIdenticalAuthority()=>Run();
#endif
        private static void Run()
        {
            string hash=null;long ticks=0;
            for(int repeat=0;repeat<2;repeat++)
            {
                var race=AiClimbRestartTests.Witness();var field=race.Runner.Field;
                var movement=ManualProductionFieldComposition.Create(field,race.Venue,race.ManualRead,race.CourseReactionPolicy);
                race.Runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")));
                while(!field.IsComplete&&field.Tick.Value<80000)race.Runner.Step();
                Assert.That(field.IsComplete,Is.True,"Actual second-season 30-athlete race must complete within the existing ordinary race budget; timeout was not widened.");
                var result=race.CollectResult();Assert.That(result.Count,Is.EqualTo(30));
                if(repeat==0){hash=result.AuthoritySha256;ticks=field.Tick.Value;}
                else{Assert.That(result.AuthoritySha256,Is.EqualTo(hash));Assert.That(field.Tick.Value,Is.EqualTo(ticks));}
                TestContext.Out.WriteLine("second_season_mass_repeat="+repeat+" ticks="+field.Tick.Value+" authority_sha256="+result.AuthoritySha256);
            }
        }
    }
}
