using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class MuscularSwayTests
    {
        public static MuscularSwayDefinition Definition() => MuscularSwayDefinition.FromJson(PhysiologyContractTests.DefinitionJson("muscular_sway_v1.json"));
        [Test] public void FixedSeedReplaysAndDifferentSeedChangesControllableNoiseOnly()
        {
            var d = Definition(); var rng = new SeededRandomSource(123);
            var a = new MuscularSwaySystem(d, 20, rng); var b = new MuscularSwaySystem(d, 20, new SeededRandomSource(123));
            var c = new MuscularSwaySystem(d, 20, new SeededRandomSource(124)); ulong state = rng.State;
            bool differs = false;
            for (int i = 1; i <= 2000; i++)
            {
                var fa = a.Step(new SimulationTick(i), 0.7, 0.5, 50);
                Assert.That(fa, Is.EqualTo(b.Step(new SimulationTick(i), 0.7, 0.5, 50)));
                if (!fa.Equals(c.Step(new SimulationTick(i), 0.7, 0.5, 50))) differs = true;
                Assert.That(Math.Abs(fa.HorizontalRadians), Is.LessThanOrEqualTo(fa.AmplitudeRadians + 1e-9));
                Assert.That(Math.Abs(fa.VerticalRadians), Is.LessThanOrEqualTo(fa.AmplitudeRadians * d.VerticalRatio + 1e-9));
            }
            Assert.That(differs, Is.True); Assert.That(rng.State, Is.EqualTo(state), "Random source is never consumed by ticks or shots.");
        }
        [Test] public void FatigueSkillSmoothnessAndAtomicityAreBounded()
        {
            var d = Definition();
            MuscularSwayFrame Evaluate(double fatigue, double arm, int skill) => new MuscularSwaySystem(d, 20, new SeededRandomSource(1)).Step(new SimulationTick(1), fatigue, arm, skill);
            Assert.That(Evaluate(1, 0, 0).AmplitudeRadians, Is.GreaterThan(Evaluate(0, 0, 0).AmplitudeRadians));
            Assert.That(Evaluate(0, 1, 0).AmplitudeRadians, Is.GreaterThan(Evaluate(0, 0, 0).AmplitudeRadians));
            Assert.That(Evaluate(1, 1, 100).AmplitudeRadians, Is.LessThan(Evaluate(1, 1, 0).AmplitudeRadians));
            var s = new MuscularSwaySystem(d, 20, new SeededRandomSource(1));
            Assert.Catch(() => s.Step(new SimulationTick(1), double.NaN, 0, 0));
            var previous = s.Step(new SimulationTick(1), 0, 0, 0);
            Assert.That(previous, Is.EqualTo(Evaluate(0, 0, 0)));
            double weightedRate = 0; for (int i = 0; i < d.ToneCount; i++) weightedRate += d.Weight(i) * d.Frequency(i);
            for (int i = 2; i <= 1000; i++)
            {
                var f = s.Step(new SimulationTick(i), 0, 0, 0);
                Assert.That(Math.Abs(f.HorizontalRadians - previous.HorizontalRadians), Is.LessThanOrEqualTo(d.Amplitude * 2 * Math.PI * weightedRate / 20 + 2e-9));
                previous = f;
            }
            Assert.Catch(() => new MuscularSwaySystem(d, 4, new SeededRandomSource(1)));
        }
        [Test] public void ActiveNoiseAllocatesNothing()
        {
            var s = new MuscularSwaySystem(Definition(), 20, new SeededRandomSource(1)); s.Step(new SimulationTick(1), 1, 1, 50);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10001; i++) s.Step(new SimulationTick(i), 1, 1, 50);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
