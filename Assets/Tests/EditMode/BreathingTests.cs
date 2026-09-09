using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BreathingTests
    {
        public static BreathingDefinition Definition() => BreathingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("breathing_v1.json"));
        [TestCase(20)] [TestCase(30)] [TestCase(144)]
        public void RestingCycleIsBoundedAndClosesAtFiveSeconds(int hz)
        {
            var p = PhysiologyContractTests.Definition(); var d = Definition(); var s = new BreathingSystem(d, p, hz);
            double maximum = 0, minimum = 0;
            for (int i = 1; i <= hz * 5; i++)
            {
                var f = s.Step(new SimulationTick(i), p.RestingHrBpm, 0, 0);
                maximum = Math.Max(maximum, f.VerticalRadians); minimum = Math.Min(minimum, f.VerticalRadians);
                Assert.That(Math.Abs(f.VerticalRadians), Is.LessThanOrEqualTo(f.AmplitudeRadians + 1e-9));
            }
            Assert.That(Math.Abs(s.LastFrame.VerticalRadians), Is.LessThan(1e-8));
            Assert.That(maximum, Is.GreaterThan(d.BaseAmplitude * 0.99)); Assert.That(minimum, Is.LessThan(-d.BaseAmplitude * 0.99));
        }
        [Test] public void LoadAndFatigueIncreaseSwayWhileSkillReducesIt()
        {
            var p = PhysiologyContractTests.Definition(); var d = Definition();
            BreathingFrame Evaluate(double hr, double fatigue, int skill) => new BreathingSystem(d, p, 20).Step(new SimulationTick(1), hr, fatigue, skill);
            var rest = Evaluate(p.RestingHrBpm, 0, 0); var high = Evaluate(p.MaxHrBpm, 0, 0);
            var tired = Evaluate(p.MaxHrBpm, 1, 0); var expert = Evaluate(p.MaxHrBpm, 1, 100);
            Assert.That(high.AmplitudeRadians, Is.GreaterThan(rest.AmplitudeRadians));
            Assert.That(high.FrequencyHz, Is.GreaterThan(rest.FrequencyHz));
            Assert.That(tired.AmplitudeRadians, Is.GreaterThan(high.AmplitudeRadians));
            Assert.That(expert.AmplitudeRadians, Is.LessThan(tired.AmplitudeRadians));
        }
        [Test] public void ReplayInvalidInputAndActiveAllocationAreControlled()
        {
            var p = PhysiologyContractTests.Definition(); var d = Definition(); var a = new BreathingSystem(d, p, 20); var b = new BreathingSystem(d, p, 20);
            Assert.Catch(() => a.Step(new SimulationTick(1), double.NaN, 0, 0));
            Assert.Catch(() => a.Step(new SimulationTick(2), p.RestingHrBpm, 0, 0));
            for (int i = 1; i <= 100; i++)
                Assert.That(a.Step(new SimulationTick(i), p.MaxHrBpm, 0.7, 50), Is.EqualTo(b.Step(new SimulationTick(i), p.MaxHrBpm, 0.7, 50)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new SimulationTick(i), p.MaxHrBpm, 0.7, 50);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
