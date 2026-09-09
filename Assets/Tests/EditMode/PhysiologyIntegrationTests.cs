using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysiologyIntegrationTests
    {
        public static readonly AthleteId Athlete = new AthleteId("physiology_test_athlete");
        public static PhysiologySession Create(RaceEventLog log, PhysiologyState? state = null, ulong seed = 12345)
        {
            var d = PhysiologyContractTests.Definition();
            return new PhysiologySession(new SimulationConfig(1, "physiology_scenario_v1", 20, seed), d, Athlete,
                state ?? PhysiologyState.Rested(d), log);
        }
        public static PhysiologyInput Input(PhysiologySession s, int control, double form = 1, double terrain = 1) =>
            new PhysiologyInput(new SimulationInput(s.Clock.Tick.Next(), s.AthleteId, control), form, terrain);

        [Test]
        public void AttackHasCostAndRecoveryRestoresAllResources()
        {
            var s = Create(new RaceEventLog());
            for (int i = 0; i < 3600; i++) s.Step(Input(s, 1000));
            var attack = s.State;
            Assert.That(attack.HeartRateBpm, Is.GreaterThan(180));
            Assert.That(attack.AnaerobicReserve, Is.LessThan(0.6));
            Assert.That(attack.MuscularFatigue, Is.GreaterThan(0));
            for (int i = 0; i < 1200; i++) s.Step(Input(s, 385));
            Assert.That(s.State.HeartRateBpm, Is.LessThan(attack.HeartRateBpm));
            Assert.That(s.State.AnaerobicReserve, Is.GreaterThan(attack.AnaerobicReserve));
            Assert.That(s.State.MuscularFatigue, Is.LessThan(attack.MuscularFatigue));
        }
        [Test]
        public void InvalidWorkloadIsAtomicAcrossKernelStateTelemetryAndEvents()
        {
            var log = new RaceEventLog();
            var s = Create(log);
            var state = s.State;
            Assert.Catch(() => s.Step(Input(s, 900, terrain: 10)));
            Assert.Catch(() => s.Step(new PhysiologyInput(new SimulationInput(new SimulationTick(2), Athlete, 900), 1, 1)));
            Assert.Catch(() => s.Step(default));
            Assert.That(s.Clock.Tick.Value, Is.Zero);
            Assert.That(s.State, Is.EqualTo(state));
            Assert.That(s.LastFrame.Tick.Value, Is.Zero);
            Assert.That(log.Count, Is.Zero);
        }
        [Test]
        public void ReplayingFullInputsReproducesStateAndOrderedEventsRegardlessOfSeed()
        {
            var leftLog = new RaceEventLog(12000);
            var rightLog = new RaceEventLog(12000);
            var left = Create(leftLog);
            var right = Create(rightLog, seed: 999);
            for (int i = 0; i < 10000; i++)
            {
                var input = Input(left, i % 1001, i % 200 < 100 ? 0.9 : 1.05, i % 50 < 25 ? 1 : 2);
                left.Step(input); right.Step(input);
                Assert.That(right.State, Is.EqualTo(left.State));
            }
            Assert.That(rightLog.Count, Is.EqualTo(leftLog.Count));
            for (int i = 0; i < leftLog.Count; i++) Assert.That(rightLog[i], Is.EqualTo(leftLog[i]));
            Assert.That(left.RandomState, Is.EqualTo(12345UL));
            Assert.That(right.RandomState, Is.EqualTo(999UL));
        }
        [Test]
        public void ResourceBoundaryEventsAreOrderedAfterEffortAndNotRepeated()
        {
            var log = new RaceEventLog();
            var d = PhysiologyContractTests.Definition();
            var s = Create(log, new PhysiologyState(d.RestingHrBpm, 0.999999999, 0.000000001, 0));
            s.Step(Input(s, 1));
            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[0].EventType, Is.EqualTo(RaceEventType.EffortChanged));
            Assert.That(log[1].EventType, Is.EqualTo(RaceEventType.AnaerobicRefilled));
            Assert.That(log[2].EventType, Is.EqualTo(RaceEventType.MusclesFullyRecovered));
            s.Step(Input(s, 1));
            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[2].Tick.Value, Is.EqualTo(1));
        }
        [Test]
        public void IntegratedHotLoopAllocatesZeroAfterWarmup()
        {
            var s = Create(new RaceEventLog(12000));
            s.Step(Input(s, 1000));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) s.Step(Input(s, i % 1001));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(s.Clock.Tick.Value, Is.EqualTo(10001));
        }
    }
}
