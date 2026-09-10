using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitStartTests
    {
        private static PursuitQualificationSnapshot Qualification() => PursuitQualificationSnapshot.Create(
            "gap_test", PursuitQualificationTests.Source(), PursuitQualificationTests.Profile());

        [Test] public void TiedStartsAndRegisteredGapsDriveIndividualLifecyclesExactly()
        {
            var q = Qualification(); var log = new RaceEventLog(20);
            var scheduler = new PursuitStartScheduler(q, 20, new SimulationTick(10), log);
            var races = new CompetitionLifecycle[q.Count]; var due = new ScheduledStart[q.Count];
            for (int i = 0; i < races.Length; i++) races[i] = new CompetitionLifecycle(
                PursuitQualificationTests.Profile(), new AthleteId(q.Get(i).AthleteId), 20, new RaceEventLog(10));
            for (int tick = 1; tick <= 15; tick++)
            {
                int count = scheduler.Step(new SimulationTick(tick), due);
                for (int i = 0; i < races.Length; i++)
                {
                    bool start = false;
                    for (int j = 0; j < count; j++) if (due[j].AthleteId.Equals(races[i].AthleteId)) start = true;
                    races[i].Step(new SimulationTick(tick), start ? CompetitionAction.Start : CompetitionAction.None);
                }
            }
            Assert.That(log.Count, Is.EqualTo(3)); Assert.That(log[0].Tick.Value, Is.EqualTo(10));
            Assert.That(log[1].Tick.Value, Is.EqualTo(10)); Assert.That(log[2].Tick.Value, Is.EqualTo(12));
            Assert.That(log[0].AthleteId.Value, Is.EqualTo("a")); Assert.That(log[1].AthleteId.Value, Is.EqualTo("z"));
            for (int i = 0; i < races.Length; i++)
                Assert.That((races[i].StartTick.Value - races[0].StartTick.Value) * q.GapUnitsPerSecond,
                    Is.EqualTo(q.Get(i).StartGapUnits * 20));
            Assert.That(scheduler.EmittedCount, Is.EqualTo(3));
        }

        [Test] public void InsufficientTieBufferInvalidClockAndOverflowFailBeforeEmission()
        {
            var q = Qualification(); var log = new RaceEventLog(20);
            Assert.Catch(() => new PursuitStartScheduler(q, 3, new SimulationTick(1), log));
            Assert.Catch(() => new PursuitStartScheduler(q, 0, new SimulationTick(1), log));
            Assert.Catch(() => new PursuitStartScheduler(q, 20, new SimulationTick(0), log));
            Assert.Catch(() => new PursuitStartScheduler(q, 20, new SimulationTick(long.MaxValue), log));
            var scheduler = new PursuitStartScheduler(q, 20, new SimulationTick(1), log);
            Assert.Catch(() => scheduler.Step(new SimulationTick(1), new ScheduledStart[1]));
            Assert.Catch(() => scheduler.Step(new SimulationTick(2), new ScheduledStart[3]));
            Assert.That(scheduler.Tick.Value, Is.Zero); Assert.That(scheduler.EmittedCount, Is.Zero); Assert.That(log.Count, Is.Zero);
            Assert.That(scheduler.Step(new SimulationTick(1), new ScheduledStart[2]), Is.EqualTo(2));
        }

        [Test] public void RestoredDependencyReplaysStartsWithoutTickAllocations()
        {
            var q = Qualification(); var restored = PursuitQualificationSnapshot.FromJson(q.ToJson(), q.SourceResultId, PursuitQualificationTests.Profile());
            var logA = new RaceEventLog(10); var logB = new RaceEventLog(10);
            var a = new PursuitStartScheduler(q, 20, new SimulationTick(2), logA);
            var b = new PursuitStartScheduler(restored, 20, new SimulationTick(2), logB);
            var due = new ScheduledStart[3]; a.Step(new SimulationTick(1), due); b.Step(new SimulationTick(1), due);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 2; tick <= 1000; tick++) { a.Step(new SimulationTick(tick), due); b.Step(new SimulationTick(tick), due); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(logA.Count, Is.EqualTo(3));
            Assert.That(a.QualificationJson, Is.EqualTo(b.QualificationJson));
            for (int i = 0; i < logA.Count; i++) Assert.That(logA[i], Is.EqualTo(logB[i]));
        }
    }
}
