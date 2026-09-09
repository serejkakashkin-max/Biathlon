using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingBalanceTests
    {
        public static ShootingBalanceSuite Suite() => ShootingBalanceSuite.FromJson(PhysiologyContractTests.DefinitionJson("shooting_balance_v1.json"));
        [Test]
        public void AllFixedSeedBoutsMatchCliAndPreserveBroadConditionComparisons()
        {
            var suite = Suite(); var model = ShootingSessionTests.Model(); var athlete = ShootingSessionTests.Athlete();
            var csv = new StringBuilder("scenario,seed,ticks,shots,closed_mask,hits\n");
            var totals = new Dictionary<string, int>();
            Assert.That(suite.Samples, Is.GreaterThanOrEqualTo(200));
            for (int i = 0; i < suite.Count; i++)
            {
                var scenario = suite.Get(i); int hits = 0;
                var profile = ShootingAiDefinition.FromJson(PhysiologyContractTests.DefinitionJson(scenario.AiDefinition + ".json"));
                for (int sample = 0; sample < suite.Samples; sample++)
                {
                    var r = ShootingBalanceRunner.Run(suite, scenario, model, athlete, profile, suite.SeedStart + (ulong)sample);
                    Assert.That(r.Shots, Is.EqualTo(5)); Assert.That(r.Ticks, Is.LessThanOrEqualTo(suite.MaximumTicks));
                    hits += r.Hits;
                    csv.Append(scenario.Id).Append(',').Append(r.Seed).Append(',').Append(r.Ticks).Append(',').Append(r.Shots).Append(',').Append(r.ClosedMask).Append(',').Append(r.Hits).Append('\n');
                }
                totals.Add(scenario.Id, hits);
            }
            using (var sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(csv.ToString()))).Replace("-", "").ToLowerInvariant();
                string expected = PhysiologyContractTests.ProjectFile("tests/fixtures/shooting-balance-v1.sha256").Split(' ')[0];
                Assert.That(actual, Is.EqualTo(expected));
            }
            Assert.That(totals["prone_high_hr"], Is.LessThan(totals["prone_rest"]));
            Assert.That(totals["standing_high_hr"], Is.LessThan(totals["standing_rest"]));
            Assert.That(totals["standing_tired"], Is.LessThan(totals["standing_high_hr"]));
            Assert.That(totals["standing_windy"], Is.LessThan(totals["standing_standard"]));
            Assert.That(totals["standing_easy"], Is.LessThan(totals["standing_standard"]));
            Assert.That(totals["standing_hard"], Is.GreaterThanOrEqualTo(totals["standing_standard"]));
        }
        [Test]
        public void InvalidSuiteAndInsufficientTickBudgetFailExplicitly()
        {
            string json = Suite().ToJson();
            Assert.Catch(() => ShootingBalanceSuite.FromJson(PhysiologyContractTests.ReplaceNumber(json, "samples_per_scenario", "0")));
            Assert.Catch(() => ShootingBalanceSuite.FromJson(json.Replace("shooting_ai_v1", "../shooting_ai_v1")));
            var shortSuite = ShootingBalanceSuite.FromJson(PhysiologyContractTests.ReplaceNumber(json, "maximum_ticks", "2"));
            Assert.Throws<InvalidOperationException>(() => ShootingBalanceRunner.Run(shortSuite, shortSuite.Get(0), ShootingSessionTests.Model(),
                ShootingSessionTests.Athlete(), ShootingAiTests.Definition(), shortSuite.SeedStart));
        }
    }
}
