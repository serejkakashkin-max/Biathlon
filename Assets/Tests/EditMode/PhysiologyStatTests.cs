using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysiologyStatTests
    {
        public static PhysiologyStatCurves Curves() => PhysiologyStatCurves.FromJson(PhysiologyContractTests.DefinitionJson("physiology_stat_curves_v1.json"));
        [Test]
        public void EndpointsInterpolationAndAllStatsAreMonotone()
        {
            var c = Curves();
            Assert.That(c.EndurancePower.Evaluate(0), Is.EqualTo(0.75));
            Assert.That(c.EndurancePower.Evaluate(25), Is.EqualTo(0.875));
            Assert.That(c.EndurancePower.Evaluate(100), Is.EqualTo(1.2));
            for (int i = 1; i <= 100; i++)
            {
                Assert.That(c.EndurancePower.Evaluate(i), Is.GreaterThanOrEqualTo(c.EndurancePower.Evaluate(i - 1)));
                Assert.That(c.AnaerobicCapacity.Evaluate(i), Is.GreaterThanOrEqualTo(c.AnaerobicCapacity.Evaluate(i - 1)));
                Assert.That(c.Recovery.Evaluate(i), Is.GreaterThanOrEqualTo(c.Recovery.Evaluate(i - 1)));
                Assert.That(c.StrengthFatigueGain.Evaluate(i), Is.LessThanOrEqualTo(c.StrengthFatigueGain.Evaluate(i - 1)));
            }
            Assert.That(PhysiologyStatCurves.FromJson(c.ToJson()).ToJson(), Is.EqualTo(c.ToJson()));
        }
        [Test]
        public void MappingPreservesTemplateAndProducesStableVersionedIdentity()
        {
            var template = PhysiologyContractTests.Definition();
            string original = template.ToJson();
            var c = Curves();
            var high = c.Apply(template, new AthletePhysiologyStats(80, 80, 80, 80));
            Assert.That(high.SustainablePowerWatts, Is.GreaterThan(template.SustainablePowerWatts));
            Assert.That(high.AnaerobicCapacityJoules, Is.GreaterThan(template.AnaerobicCapacityJoules));
            Assert.That(high.RecoveryModifier, Is.GreaterThan(template.RecoveryModifier));
            Assert.That(high.FatigueGainPerSecond, Is.LessThan(template.FatigueGainPerSecond));
            Assert.That(template.ToJson(), Is.EqualTo(original));
            Assert.That(PhysiologyDefinition.FromJson(high.ToJson()).ToJson(), Is.EqualTo(high.ToJson()));
            Assert.That(c.Apply(template, new AthletePhysiologyStats(80, 80, 80, 80)).Id, Is.EqualTo(high.Id));
        }
        [Test]
        public void RecoveryAndCapacity40Versus80HaveExpectedPhysicalEffects()
        {
            var template = PhysiologyContractTests.Definition();
            var c = Curves();
            var low = c.Apply(template, new AthletePhysiologyStats(50, 40, 40, 50));
            var high = c.Apply(template, new AthletePhysiologyStats(50, 80, 80, 50));
            var lowHr = new HeartRateSystem(low, 20); var highHr = new HeartRateSystem(high, 20);
            double l = 180, h = 180;
            for (int i = 0; i < 1200; i++) { l = lowHr.Step(l, 0.5, 0).HeartRateBpm; h = highHr.Step(h, 0.5, 0).HeartRateBpm; }
            Assert.That(h, Is.LessThan(l));
            Assert.That(new AnaerobicSystem(high, 20).Step(1, 1.2, 0).Value,
                Is.GreaterThan(new AnaerobicSystem(low, 20).Step(1, 1.2, 0).Value));
            Assert.That(high.SustainablePowerWatts, Is.EqualTo(low.SustainablePowerWatts));
        }
        [Test]
        public void InvalidStatsAndMalformedCurvesAreRejected()
        {
            Assert.Catch(() => new AthletePhysiologyStats(-1, 0, 0, 0));
            Assert.Catch(() => new AthletePhysiologyStats(0, 101, 0, 0));
            Assert.Catch(() => Curves().Recovery.Evaluate(101));
            string json = Curves().ToJson();
            foreach (string malformed in new[] { "null", "{}", json.Replace("\"schema_version\":1", "\"schema_version\":2"),
                json.Replace("\"stat\":0", "\"stat\":1"), json.Replace("\"stat\":50", "\"stat\":100"),
                json.Replace("\"factor\":0.75", "\"factor\":-1"), json.Replace("\"factor\":0.75", "\"factor\":2") })
                Assert.Catch(() => PhysiologyStatCurves.FromJson(malformed));
        }
    }
}
