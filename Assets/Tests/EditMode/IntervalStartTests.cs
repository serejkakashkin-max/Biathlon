using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class IntervalStartTests
    {
        [Test] public void StableRosterStartsAtExactIntervalsAndLifecyclesUseIndividualClocks()
        {
            var p = CompetitionProfileTests.TestProfile(); var roster = new[] { new AthleteId("z"), new AthleteId("a"), new AthleteId("b") };
            var log = new RaceEventLog(100); var s = new IntervalStartScheduler(p, roster, 20, new SimulationTick(10), log);
            var due = new ScheduledStart[1]; var races = new CompetitionLifecycle[3];
            for (int i = 0; i < races.Length; i++) races[i] = new CompetitionLifecycle(p, roster[i], 20, new RaceEventLog(100));
            roster[0] = new AthleteId("owner_changed_input");
            for (int i = 1; i <= 1300; i++)
            {
                int count = s.Step(new SimulationTick(i), due);
                for (int a = 0; a < races.Length; a++) races[a].Step(new SimulationTick(i), count == 1 && due[0].StartOrdinal == a + 1 ? CompetitionAction.Start : CompetitionAction.None);
            }
            Assert.That(s.EmittedCount, Is.EqualTo(3)); Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[0].AthleteId.Value, Is.EqualTo("z")); Assert.That(log[1].Tick.Value, Is.EqualTo(610)); Assert.That(log[2].Tick.Value, Is.EqualTo(1210));
            Assert.That(races[0].LastFrame.ElapsedSeconds - races[1].LastFrame.ElapsedSeconds, Is.EqualTo(30));
            Assert.That(races[1].LastFrame.ElapsedSeconds - races[2].LastFrame.ElapsedSeconds, Is.EqualTo(30));
        }
        [Test] public void DuplicateRosterSubTickIntervalOverflowAndOutputCapacityFailBeforeEmission()
        {
            var p = CompetitionProfileTests.TestProfile(); var a = new AthleteId("a"); var log = new RaceEventLog(100);
            Assert.Catch(() => new IntervalStartScheduler(p, new[] { a, a }, 20, new SimulationTick(1), log));
            Assert.Catch(() => new IntervalStartScheduler(p, new[] { a, new AthleteId("b") }, 20, new SimulationTick(long.MaxValue), log));
            var fractional = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(), "start_interval_s", "0.07"));
            Assert.Catch(() => new IntervalStartScheduler(fractional, new[] { a }, 20, new SimulationTick(1), log));
            var s = new IntervalStartScheduler(p, new[] { a }, 20, new SimulationTick(1), log);
            Assert.Catch(() => s.Step(new SimulationTick(1), Array.Empty<ScheduledStart>()));
            Assert.That(s.Tick.Value, Is.Zero); Assert.That(s.EmittedCount, Is.Zero); Assert.That(log.Count, Is.Zero);
            Assert.That(s.Step(new SimulationTick(1), new ScheduledStart[1]), Is.EqualTo(1));
        }
        [Test] public void RepeatedScheduleProducesSameEventsAndNoTickAllocations()
        {
            var p = CompetitionProfileTests.TestProfile(); var roster = new AthleteId[60];
            for (int i = 0; i < roster.Length; i++) roster[i] = new AthleteId("racer_" + i);
            var logA = new RaceEventLog(100); var logB = new RaceEventLog(100);
            var a = new IntervalStartScheduler(p, roster, 20, new SimulationTick(1), logA);
            var b = new IntervalStartScheduler(p, roster, 20, new SimulationTick(1), logB);
            var output = new ScheduledStart[1]; a.Step(new SimulationTick(1), output); b.Step(new SimulationTick(1), output);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 36000; i++) { a.Step(new SimulationTick(i), output); b.Step(new SimulationTick(i), output); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(logA.Count, Is.EqualTo(60));
            for (int i = 0; i < logA.Count; i++) Assert.That(logA[i], Is.EqualTo(logB[i]));
        }
    }
}
