using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintFinishTests
    {
        public static FinalSprintDefinition Definition() => FinalSprintDefinition.FromJson(PhysiologyContractTests.DefinitionJson("final_sprint_provisional_v1.json"));
        private static SkiState At(double meters) => new SkiState((long)(meters * SkiState.NanometersPerMeter), 10);
        [Test]
        public void SprintPresetRequiresRequestAndFinalZoneWithoutPowerBonus()
        {
            var system = new FinalSprintSystem(Definition(), TrackDistance.FromMeters(1000));
            Assert.That(system.Evaluate(true, 800, At(799)).Active, Is.False);
            Assert.That(system.Evaluate(true, 800, At(800)).EffectiveEffortControl, Is.EqualTo(1000));
            Assert.That(system.Evaluate(false, 800, At(900)).EffectiveEffortControl, Is.EqualTo(800));
            Assert.That(system.Evaluate(true, 800, At(1000)).Active, Is.False);
            Assert.That(system.Evaluate(true, 800, At(1001)).RemainingMeters, Is.Zero);
            Assert.That(FinalSprintDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
            Assert.Catch(() => FinalSprintDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "effort_control", "1001")));
        }
        [Test]
        public void SprintConsumesReserveAndSustainedCapabilityDeclines()
        {
            var d = PhysiologyContractTests.Definition();
            var fresh = PhysiologyIntegrationTests.Create(new RaceEventLog(100));
            var depleted = PhysiologyIntegrationTests.Create(new RaceEventLog(100), new PhysiologyState(d.RestingHrBpm, 0, 0, 0));
            int control = new FinalSprintSystem(Definition(), TrackDistance.FromMeters(1000)).Evaluate(true, 800, At(850)).EffectiveEffortControl;
            double first = fresh.Step(PhysiologyIntegrationTests.Input(fresh, control)).Effort.DeliveredPowerWatts;
            double empty = depleted.Step(PhysiologyIntegrationTests.Input(depleted, control)).Effort.DeliveredPowerWatts;
            Assert.That(empty, Is.LessThan(first));
            for (int i = 0; i < 1200; i++) fresh.Step(PhysiologyIntegrationTests.Input(fresh, control));
            Assert.That(fresh.State.AnaerobicReserve, Is.LessThan(0.7));
            Assert.That(fresh.State.MuscularFatigue, Is.GreaterThan(0));
            Assert.That(fresh.LastFrame.Effort.DeliveredPowerWatts, Is.LessThan(first));
        }
        [Test]
        public void SameTickFinishersUseOvershootFractionAndRecordOneFinishEvent()
        {
            var log = new RaceEventLog(100);
            var a = new FinishTracker(TrackDistance.FromMeters(10), new AthleteId("a"), 20, log);
            var b = new FinishTracker(TrackDistance.FromMeters(10), new AthleteId("b"), 20, log);
            Assert.That(a.Observe(new SimulationTick(1), At(0), At(20)), Is.True);
            Assert.That(b.Observe(new SimulationTick(1), At(0), At(25)), Is.True);
            Assert.That(a.Time.WholeTicks, Is.Zero); Assert.That(a.Time.FractionParts, Is.EqualTo(500000000));
            Assert.That(b.Time.CompareTo(a.Time), Is.LessThan(0));
            Assert.That(a.Time.Seconds, Is.EqualTo(0.025)); Assert.That(b.Time.Seconds, Is.EqualTo(0.02));
            Assert.That(a.Observe(new SimulationTick(2), At(20), At(21)), Is.False);
            Assert.That(log.Count, Is.EqualTo(2)); Assert.That(log[0].EventType, Is.EqualTo(RaceEventType.RaceFinished));
        }
        [Test]
        public void ExactEndpointAndSubMillimetreCrossingsDoNotUseDisplayDistance()
        {
            var endpoint = new FinishTracker(TrackDistance.FromMeters(10), new AthleteId("a"), 20, new RaceEventLog(10));
            endpoint.Observe(new SimulationTick(1), At(0), At(10));
            Assert.That(endpoint.Time.WholeTicks, Is.EqualTo(1)); Assert.That(endpoint.Time.FractionParts, Is.Zero);
            long finish = 10000000000;
            var fine = new FinishTracker(TrackDistance.FromMeters(10), new AthleteId("a"), 20, new RaceEventLog(10), finish - 1);
            fine.Observe(new SimulationTick(1), new SkiState(finish - 1, 1), new SkiState(finish + 3, 1));
            Assert.That(fine.Time.FractionParts, Is.EqualTo(250000000));
        }
        [Test]
        public void InvalidObservationsAreAtomicAndFinishTrackingAllocatesNothing()
        {
            var log = new RaceEventLog(100); var tracker = new FinishTracker(TrackDistance.FromMeters(100000), new AthleteId("a"), 20, log);
            Assert.Catch(() => tracker.Observe(new SimulationTick(2), At(0), At(1)));
            Assert.Catch(() => tracker.Observe(new SimulationTick(1), At(1), At(2)));
            Assert.That(tracker.Tick.Value, Is.Zero); Assert.That(log.Count, Is.Zero); Assert.That(tracker.HasFinished, Is.False);
            Assert.Catch(() => { double unused = tracker.Time.Seconds; });
            tracker.Observe(new SimulationTick(1), At(0), At(1));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10000; i++) tracker.Observe(new SimulationTick(i), At(i - 1), At(i));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.Catch(() => new FinishTime(1, 0, 20).CompareTo(new FinishTime(1, 0, 30)));
        }
    }
}
