using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class FatigueTests
    {
        [Test]
        public void ThresholdGainMatchesConfiguredRateAndTerrainScalesDemand()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new FatigueSystem(d, 20);
            double fatigue = 0;
            for (int i = 0; i < 3600; i++) fatigue = s.Step(fatigue, d.AerobicThreshold, 1).Value;
            double expected = 180 * Math.Pow(d.AerobicThreshold, d.FatigueExponent) * d.FatigueGainPerSecond;
            Assert.That(fatigue, Is.EqualTo(expected).Within(2e-6));
            Assert.That(s.Step(0.2, 1, 2).Value, Is.GreaterThan(s.Step(0.2, 1, 1).Value));
        }
        [Test]
        public void RecoveryRestoresFreshnessAndPowerCapability()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new FatigueSystem(d, 20);
            double fatigue = 1;
            for (int i = 0; i < 12000; i++) fatigue = s.Step(fatigue, 0.5, 1).Value;
            Assert.That(fatigue, Is.LessThan(1));
            var power = new EffortSystem(d);
            Assert.That(power.Evaluate(1000, new PhysiologyState(d.RestingHrBpm, 1, fatigue, 0), 1).DeliveredPowerWatts,
                Is.GreaterThan(power.Evaluate(1000, new PhysiologyState(d.RestingHrBpm, 1, 1, 0), 1).DeliveredPowerWatts));
            Assert.That(s.Step(0, 0, 1).Value, Is.Zero);
            Assert.That(s.Step(1, d.MaximumRelativeEffort, d.MaximumTerrainMuscleFactor).Value, Is.EqualTo(1));
        }
        [Test]
        public void FullDomainStaysBoundedWithoutAllocations()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new FatigueSystem(d, 20);
            Assert.Catch(() => s.Step(double.NaN, 1, 1));
            Assert.Catch(() => s.Step(0, 1, d.MaximumTerrainMuscleFactor + 1));
            double value = s.Step(0, 1, 1).Value;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) value = s.Step(value, i % 1001 / 1000.0 * d.MaximumRelativeEffort, 1).Value;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(value, Is.InRange(0, 1));
        }
    }
}
