using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AnaerobicTests
    {
        [Test]
        public void ConstantDrainMatchesJoulesAndExhaustionSaturates()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new AnaerobicSystem(d, 20);
            double reserve = 1;
            for (int i = 0; i < 200; i++) reserve = s.Step(reserve, 1.2, 0).Value;
            double expected = 1 - 10 * Math.Pow(1.2 - d.AerobicThreshold, d.AnaerobicDrainExponent) * d.AnaerobicDrainWatts / d.AnaerobicCapacityJoules;
            Assert.That(reserve, Is.EqualTo(expected).Within(1e-6));
            for (int i = 0; i < 10000; i++) reserve = s.Step(reserve, d.MaximumRelativeEffort, 0).Value;
            Assert.That(reserve, Is.Zero);
            Assert.That(s.Step(0, 1.2, 0).ChangePerSecond, Is.Zero);
        }
        [Test]
        public void ThresholdIsNeutralAndRecoveryIsBoundedAndFatigueSensitive()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new AnaerobicSystem(d, 20);
            Assert.That(s.Step(0.5, d.AerobicThreshold, 0).Value, Is.EqualTo(0.5));
            Assert.That(s.Step(0.5, d.AerobicThreshold + 0.001, 0).Value, Is.LessThan(0.5));
            Assert.That(s.Step(0.5, d.AerobicThreshold - 0.001, 0).Value, Is.GreaterThan(0.5));
            Assert.That(s.Step(0.5, 0.5, 1).Value, Is.LessThan(s.Step(0.5, 0.5, 0).Value));
            double reserve = 0;
            for (int i = 0; i < 10000; i++) reserve = s.Step(reserve, 0, 0).Value;
            Assert.That(reserve, Is.EqualTo(1));
            Assert.That(s.Step(1, 0, 0).ChangePerSecond, Is.Zero);
        }
        [Test]
        public void CapacityControlsDrainAndRefillTimeWithoutHiddenPowerBoost()
        {
            var d = PhysiologyContractTests.Definition();
            var larger = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "anaerobic_capacity_joules", "40000"));
            var s = new AnaerobicSystem(d, 20);
            var l = new AnaerobicSystem(larger, 20);
            Assert.That(l.Step(0.5, 1.2, 0).Value, Is.GreaterThan(s.Step(0.5, 1.2, 0).Value));
            Assert.That(l.Step(0.5, 0.5, 0).Value, Is.LessThan(s.Step(0.5, 0.5, 0).Value));
            Assert.That(new EffortSystem(larger).Evaluate(1000, PhysiologyState.Rested(larger), 1).DeliveredPowerWatts,
                Is.EqualTo(new EffortSystem(d).Evaluate(1000, PhysiologyState.Rested(d), 1).DeliveredPowerWatts));
        }
        [Test]
        public void IntegratedAttackCostsReserveAndRecoveryRestoresCapability()
        {
            var d = PhysiologyContractTests.Definition();
            var effort = new EffortSystem(d);
            var reserve = new AnaerobicSystem(d, 20);
            var state = PhysiologyState.Rested(d);
            for (int i = 0; i < 1200; i++)
            {
                var output = effort.Evaluate(1000, state, 1);
                state = new PhysiologyState(state.HeartRateBpm, reserve.Step(state.AnaerobicReserve, output.DeliveredRelativeEffort, 0).Value, 0, 0);
            }
            double attacked = state.AnaerobicReserve;
            Assert.That(attacked, Is.LessThan(0.8));
            for (int i = 0; i < 1200; i++) state = new PhysiologyState(state.HeartRateBpm, reserve.Step(state.AnaerobicReserve, 0.5, 0).Value, 0, 0);
            Assert.That(state.AnaerobicReserve, Is.GreaterThan(attacked));
        }
        [Test]
        public void RepeatedIntervalsAllocateNothingAndInvalidInputFails()
        {
            var d = PhysiologyContractTests.Definition();
            var s = new AnaerobicSystem(d, 20);
            Assert.Catch(() => s.Step(double.NaN, 1, 0));
            Assert.Catch(() => s.Step(0.5, -1, 0));
            Assert.Catch(() => s.Step(0.5, 1, -1));
            double value = s.Step(1, 1.2, 0).Value;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) value = s.Step(value, i % 400 < 200 ? 1.2 : 0.6, 0).Value;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(value, Is.InRange(0, 1));
        }
    }
}
