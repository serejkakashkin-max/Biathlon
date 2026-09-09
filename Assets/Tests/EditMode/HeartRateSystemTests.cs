using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class HeartRateSystemTests
    {
        private static double Run(HeartRateSystem system, double bpm, double effort, int ticks)
        {
            for (int i = 0; i < ticks; i++) bpm = system.Step(bpm, effort, 0).HeartRateBpm;
            return bpm;
        }
        [TestCase(20)]
        [TestCase(30)]
        public void WorkAndRecoveryMatchAnalyticalCurve(int hz)
        {
            var d = PhysiologyContractTests.Definition();
            var system = new HeartRateSystem(d, hz);
            double peak = Run(system, d.RestingHrBpm, 1, 180 * hz);
            double expectedPeak = d.MaxHrBpm - (d.MaxHrBpm - d.RestingHrBpm) * Math.Exp(-180 / d.HrRiseTauSeconds);
            Assert.That(peak, Is.EqualTo(expectedPeak).Within(1e-6));
            double recovered = Run(system, peak, 0, 60 * hz);
            double expectedRecovery = d.RestingHrBpm + (peak - d.RestingHrBpm) * Math.Exp(-60 * d.RecoveryModifier / d.HrRecoveryTauSeconds);
            Assert.That(recovered, Is.EqualTo(expectedRecovery).Within(1e-6));
            Assert.That(peak, Is.GreaterThan(180));
            Assert.That(recovered, Is.LessThan(100));
            Assert.That(Run(system, d.RestingHrBpm, 1, 180 * hz), Is.EqualTo(peak));
        }
        [Test]
        public void RecoveryModifierImprovesRecoveryWithoutIncreasingPeakCapability()
        {
            var low = PhysiologyContractTests.Definition();
            var high = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(low.ToJson(), "recovery_modifier", "1.5"));
            var lowSystem = new HeartRateSystem(low, 20);
            var highSystem = new HeartRateSystem(high, 20);
            Assert.That(Run(highSystem, 180, 0, 1200), Is.LessThan(Run(lowSystem, 180, 0, 1200)));
            Assert.That(Run(highSystem, low.RestingHrBpm, 1, 3600), Is.EqualTo(Run(lowSystem, low.RestingHrBpm, 1, 3600)));
        }
        [Test]
        public void TargetsAreObservableBoundedAndResponseHasInertia()
        {
            var d = PhysiologyContractTests.Definition();
            var system = new HeartRateSystem(d, 20);
            var first = system.Step(d.RestingHrBpm, 1, 0);
            Assert.That(first.TargetBpm, Is.EqualTo(d.MaxHrBpm));
            Assert.That(first.HeartRateBpm, Is.InRange(d.RestingHrBpm, d.RestingHrBpm + 1));
            var overloaded = system.Step(d.MaxHrBpm, d.MaximumRelativeEffort, 1, d.DriftStrengthBpm);
            Assert.That(overloaded.TargetBpm, Is.EqualTo(d.HrSafetyCeilingBpm));
            Assert.That(overloaded.RelativeLoad, Is.EqualTo(d.HrMaximumLoad));
            Assert.That(system.Step(d.RestingHrBpm, 0, 0).HeartRateBpm, Is.EqualTo(d.RestingHrBpm));
        }
        [Test]
        public void InvalidStatesAreRejected()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new HeartRateSystem(d, 20);
            Assert.Catch(() => new HeartRateSystem(d, 0));
            Assert.Catch(() => s.Step(double.NaN, 1, 0));
            Assert.Catch(() => s.Step(d.RestingHrBpm, -1, 0));
            Assert.Catch(() => s.Step(d.RestingHrBpm, 1, 1.1));
            Assert.Catch(() => s.Step(d.RestingHrBpm, 1, 0, d.DriftStrengthBpm + 1));
        }
        [Test]
        public void LongAlternatingWorkloadIsAllocationFreeAndRepeatable()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new HeartRateSystem(d, 20);
            s.Step(d.RestingHrBpm, 1, 0);
            double first = d.RestingHrBpm;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) first = s.Step(first, i % 400 < 200 ? 1.2 : 0.6, 0).HeartRateBpm;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            double second = d.RestingHrBpm;
            for (int i = 0; i < 10000; i++) second = s.Step(second, i % 400 < 200 ? 1.2 : 0.6, 0).HeartRateBpm;
            Assert.That(allocated, Is.Zero);
            Assert.That(second, Is.EqualTo(first));
            TestContext.WriteLine("hr_alternating_10000_bpm=" + first.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
