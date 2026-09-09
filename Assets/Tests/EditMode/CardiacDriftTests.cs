using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CardiacDriftTests
    {
        [Test]
        public void ExposureSaturatesAndRecoversWithoutRunaway()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new CardiacDriftSystem(d, 20);
            double exposure = 0;
            for (int i = 0; i < 100000; i++) exposure = s.Step(exposure, 1, 1).HighLoadSeconds;
            Assert.That(exposure, Is.EqualTo(d.DriftSaturationSeconds));
            Assert.That(s.Step(exposure, 1, 1).DriftBpm, Is.EqualTo(d.DriftStrengthBpm));
            Assert.That(s.Step(exposure, 1, 0).DriftBpm, Is.Zero);
            for (int i = 0; i < 100000; i++) exposure = s.Step(exposure, d.DriftThreshold, 1).HighLoadSeconds;
            Assert.That(exposure, Is.Zero);
            Assert.That(s.Step(exposure, 0, 1).DriftBpm, Is.Zero);
        }
        [Test]
        public void FatigueAndExposureRaiseHrRequirementButRespectCeiling()
        {
            var d = PhysiologyContractTests.Definition();
            var drift = new CardiacDriftSystem(d, 20).Step(d.DriftSaturationSeconds, 0.9, 0.5);
            var hr = new HeartRateSystem(d, 20);
            var before = hr.Step(150, 0.9, 0.5);
            var after = hr.Step(150, 0.9, 0.5, drift.DriftBpm);
            Assert.That(drift.DriftBpm, Is.EqualTo(d.DriftStrengthBpm * 0.5));
            Assert.That(after.TargetBpm, Is.GreaterThan(before.TargetBpm));
            Assert.That(after.TargetBpm, Is.LessThanOrEqualTo(d.HrSafetyCeilingBpm));
        }
        [Test]
        public void DriftStepValidatesInputsAndAllocatesNothing()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new CardiacDriftSystem(d, 20);
            Assert.Catch(() => s.Step(-1, 1, 0));
            Assert.Catch(() => s.Step(0, double.NaN, 0));
            Assert.Catch(() => s.Step(0, 1, 2));
            double exposure = s.Step(0, 1, 0.5).HighLoadSeconds;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) exposure = s.Step(exposure, 1, 0.5).HighLoadSeconds;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(exposure, Is.EqualTo(500.05).Within(1e-9));
        }
    }
}
