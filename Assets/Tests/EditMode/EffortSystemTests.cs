using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class EffortSystemTests
    {
        [TestCase(0.0, 1.0)]
        [TestCase(1.0, 1.0)]
        [TestCase(0.5, 0.0)]
        public void FullControlSweepIsMonotoneAndEffortEqualsRequestedOverSustainable(double fatigue, double reserve)
        {
            var definition = PhysiologyContractTests.Definition();
            var system = new EffortSystem(definition);
            var state = new PhysiologyState(definition.RestingHrBpm, reserve, fatigue, 0);
            double previousRequested = -1, previousDelivered = -1;
            for (int control = 0; control <= 1000; control++)
            {
                var output = system.Evaluate(control, state, 1);
                Assert.That(output.RequestedPowerWatts, Is.GreaterThan(previousRequested));
                Assert.That(output.DeliveredPowerWatts, Is.GreaterThanOrEqualTo(previousDelivered));
                Assert.That(output.RelativeEffort, Is.EqualTo(output.RequestedPowerWatts / output.SustainablePowerWatts).Within(1e-12));
                Assert.That(output.DeliveredPowerWatts, Is.LessThanOrEqualTo(output.RequestedPowerWatts));
                previousRequested = output.RequestedPowerWatts;
                previousDelivered = output.DeliveredPowerWatts;
            }
        }

        [Test]
        public void ExhaustionCapsAttackButRetainsSubthresholdControl()
        {
            var definition = PhysiologyContractTests.Definition();
            var system = new EffortSystem(definition);
            var exhausted = new PhysiologyState(definition.RestingHrBpm, 0, 0, 0);
            var peak = system.Evaluate(1000, exhausted, 1);
            Assert.That(peak.DeliveredRelativeEffort, Is.EqualTo(definition.AerobicThreshold));
            Assert.That(peak.RelativeEffort, Is.EqualTo(definition.MaximumRelativeEffort));
            var recovery = system.Evaluate(400, exhausted, 1);
            Assert.That(recovery.DeliveredPowerWatts, Is.EqualTo(recovery.RequestedPowerWatts));
        }

        [Test]
        public void FatigueAndFormAffectPhysicalWattsWithoutChangingInputMeaning()
        {
            var definition = PhysiologyContractTests.Definition();
            var system = new EffortSystem(definition);
            var fresh = system.Evaluate(500, PhysiologyState.Rested(definition), 1);
            var tired = system.Evaluate(500, new PhysiologyState(definition.RestingHrBpm, 1, 1, 0), 0.9);
            Assert.That(fresh.SustainablePowerWatts, Is.EqualTo(definition.SustainablePowerWatts));
            Assert.That(tired.SustainablePowerWatts / fresh.SustainablePowerWatts, Is.EqualTo(definition.MinimumPowerEfficiency * 0.9).Within(1e-12));
            Assert.That(tired.RelativeEffort, Is.EqualTo(fresh.RelativeEffort));
            var noCapability = system.Evaluate(1000, PhysiologyState.Rested(definition), 0);
            Assert.That(noCapability.DeliveredPowerWatts, Is.Zero);
            Assert.That(noCapability.RelativeEffort, Is.Zero);
        }

        [Test]
        public void InvalidInputsCannotLeakNonfinitePower()
        {
            var definition = PhysiologyContractTests.Definition();
            var system = new EffortSystem(definition);
            var state = PhysiologyState.Rested(definition);
            Assert.Catch(() => system.Evaluate(-1, state, 1));
            Assert.Catch(() => system.Evaluate(1001, state, 1));
            Assert.Catch(() => system.Evaluate(500, state, double.NaN));
            Assert.Catch(() => system.Evaluate(500, state, 1.11));
            Assert.Catch(() => system.Evaluate(500, default, 1));
        }

        [Test]
        public void EvaluateIsRepeatableAndAllocationFree()
        {
            var definition = PhysiologyContractTests.Definition();
            var system = new EffortSystem(definition);
            var state = new PhysiologyState(definition.RestingHrBpm, 0.4, 0.3, 0);
            var expected = system.Evaluate(937, state, 0.97);
            double sum = 0;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += system.Evaluate(937, state, 0.97).DeliveredPowerWatts;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(sum, Is.EqualTo(expected.DeliveredPowerWatts * 10000).Within(1e-6));
            Assert.That(system.Evaluate(937, state, 0.97).DeliveredPowerWatts, Is.EqualTo(expected.DeliveredPowerWatts));
            // Shared .NET/Unity numerical qualification checkpoint.
            Assert.That(expected.DeliveredPowerWatts, Is.EqualTo(346.0184).Within(1e-6));
        }
    }
}
