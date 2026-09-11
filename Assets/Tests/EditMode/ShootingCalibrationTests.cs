using System;
using System.Collections.Generic;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingCalibrationTests
    {
        private static ShootingBalanceSuite Suite() => ShootingBalanceSuite.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/shooting-calibration-v1.json"));
#if UNITY_EDITOR
        [UnityEngine.TestTools.UnityTest, Timeout(300000)]
        public System.Collections.IEnumerator HeldOutCohortsMatchCliAndGddWindows()
        {
            var work = System.Threading.Tasks.Task.Run((Action)Run);
            while (!work.IsCompleted) yield return null;
            work.GetAwaiter().GetResult();
        }
#else
        [Test] public void HeldOutCohortsMatchCliAndGddWindows() => Run();
#endif
        private static void Run()
        {
            var suite = Suite(); var model = ShootingSessionTests.Model();
            var csv = new StringBuilder("scenario,seed,ticks,shots,closed_mask,hits\n");
            var rates = new Dictionary<string,double>();
            for (int i = 0; i < suite.Count; i++)
            {
                var scenario = suite.Get(i); int hits = 0;
                var athlete = ShootingAthleteDefinition.FromJson(PhysiologyContractTests.DefinitionJson(scenario.AthleteDefinition + ".json"));
                var ai = ShootingAiDefinition.FromJson(PhysiologyContractTests.DefinitionJson(scenario.AiDefinition + ".json"));
                for (int j = 0; j < suite.Samples; j++)
                {
                    var r = ShootingBalanceRunner.Run(suite, scenario, model, athlete, ai, suite.SeedStart + (ulong)j);
                    Assert.That(r.Shots, Is.EqualTo(5)); hits += r.Hits;
                    csv.Append(scenario.Id).Append(',').Append(r.Seed).Append(',').Append(r.Ticks).Append(',').Append(r.Shots).Append(',').Append(r.ClosedMask).Append(',').Append(r.Hits).Append('\n');
                }
                rates.Add(scenario.Id, hits / (5.0 * suite.Samples));
            }
            Assert.That(BalanceRegression.Hash(csv.ToString()), Is.EqualTo(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/shooting-calibration-v1.sha256").Trim()));
            Assert.That(rates["top_0_normal"], Is.InRange(.82,.94));
            Assert.That(rates["top_1_normal"], Is.InRange(.72,.89));
            foreach (string stance in new[]{"0","1"})
            {
                Assert.That(rates["top_"+stance+"_normal"], Is.GreaterThan(rates["mid_"+stance+"_normal"]));
                Assert.That(rates["mid_"+stance+"_normal"], Is.GreaterThan(rates["developing_"+stance+"_normal"]));
                foreach (string tier in new[]{"top","mid","developing"})
                    Assert.That(rates[tier+"_"+stance+"_tired"], Is.LessThan(rates[tier+"_"+stance+"_normal"]));
            }
        }
        [Test] public void EstimatedWindIsAnAimCommandAndCannotOverrideGeometry()
        {
            var targets = TargetGeometryTests.Definition(); var d = ShootingAiTests.Definition();
            ShootingAiController Controller() => new ShootingAiController(d, targets, WeaponAimTests.Definition(), ShootingStanceTests.Definition().Prone,
                new AthleteId("wind_reader"), 20, new SeededRandomSource(7));
            double yaw = Math.Atan(targets.CenterX(0) / targets.DistanceMeters);
            var calm = Controller().Step(new SimulationTick(1), new ShootingAiObservation(ShootingPhase.Aiming,0,0,yaw,0));
            var wind = Controller().Step(new SimulationTick(1), new ShootingAiObservation(ShootingPhase.Aiming,0,0,yaw,0,1,.03));
            Assert.That(wind.Aim.Horizontal, Is.LessThan(calm.Aim.Horizontal));
            Assert.Catch(() => new ShootingAiObservation(ShootingPhase.Aiming,0,0,0,0,1,double.NaN));
            Assert.Catch(() => ShootingBalanceSuite.FromJson(Suite().ToJson().Replace("shooting_top_v1","../shooting_top_v1")));
            Assert.That(ShootingBalanceTests.Suite().ToJson(), Does.Not.Contain("estimated_crosswind_mps"));
            Assert.That(ShootingBalanceTests.Suite().ToJson(), Does.Not.Contain("athlete_definition"));
        }
    }
}
