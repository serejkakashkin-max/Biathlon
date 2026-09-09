using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class HeartbeatTests
    {
        public static HeartbeatDefinition Definition() => HeartbeatDefinition.FromJson(PhysiologyContractTests.DefinitionJson("heartbeat_v1.json"));
        [TestCase(20, 60)] [TestCase(30, 60)] [TestCase(144, 60)] [TestCase(20, 180)] [TestCase(30, 123)]
        public void MinuteHasExactBeatCountWithoutPhaseRoundingDrift(int hz, int bpm)
        {
            var s = new HeartbeatSystem(Definition(), PhysiologyContractTests.Definition(), hz); int starts = 0, quiet = 0;
            for (int i = 1; i <= hz * 60; i++)
            {
                var f = s.Step(new SimulationTick(i), bpm);
                if (f.BeatStarted) starts++; if (f.Envelope == 0) quiet++;
                Assert.That(f.Envelope, Is.InRange(0, 1));
            }
            Assert.That(starts, Is.EqualTo(bpm)); Assert.That(s.LastFrame.CompletedBeats, Is.EqualTo(bpm));
            Assert.That(s.LastFrame.PhaseCycles, Is.Zero); Assert.That(quiet, Is.GreaterThan(hz));
        }
        [Test] public void VariableHeartRateAndPulseShapeAreContinuousInPhase()
        {
            var s = new HeartbeatSystem(Definition(), PhysiologyContractTests.Definition(), 100);
            var first = s.Step(new SimulationTick(1), 60);
            var second = s.Step(new SimulationTick(2), 120);
            Assert.That(first.PhaseCycles, Is.EqualTo(0.01)); Assert.That(second.PhaseCycles, Is.EqualTo(0.03));
            Assert.That(second.AmplitudeRadians, Is.GreaterThan(first.AmplitudeRadians));
            Assert.That(second.Envelope, Is.EqualTo(1));
            var third = s.Step(new SimulationTick(3), 120);
            Assert.That(third.Envelope, Is.LessThan(second.Envelope)); Assert.That(third.Envelope, Is.GreaterThan(0));
        }
        [Test] public void ReplayAtomicityAndAllocationAreStable()
        {
            var d = Definition(); var p = PhysiologyContractTests.Definition(); var a = new HeartbeatSystem(d, p, 20); var b = new HeartbeatSystem(d, p, 20);
            Assert.Catch(() => a.Step(new SimulationTick(1), double.NaN));
            for (int i = 1; i <= 100; i++) Assert.That(a.Step(new SimulationTick(i), 123.456), Is.EqualTo(b.Step(new SimulationTick(i), 123.456)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new SimulationTick(i), 123.456);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
