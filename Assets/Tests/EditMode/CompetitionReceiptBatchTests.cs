using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CompetitionReceiptBatchTests
    {
        [Test] public void MultipleSpatialBoundariesUseOneClockTickAndInvalidLaterReceiptRollsBackAll()
        {
            var log = new RaceEventLog(100); var s = new CompetitionLifecycle(CompetitionProfileTests.TestProfile(), new AthleteId("a"), 20, log);
            s.Step(new SimulationTick(1), CompetitionAction.Start);
            s.Step(new SimulationTick(2), CompetitionAction.EnterRange);
            s.Step(new SimulationTick(3), CompetitionAction.CompleteBout, 7, 5);
            var invalid = new[] { new CompetitionReceipt(CompetitionAction.CompletePenaltyLoop), new CompetitionReceipt(CompetitionAction.ReachLapBoundary) };
            int count = log.Count;
            Assert.Catch(() => s.StepBatch(new SimulationTick(4), invalid));
            Assert.That(s.Tick.Value, Is.EqualTo(3)); Assert.That(s.PendingPenaltyLoops, Is.EqualTo(2)); Assert.That(log.Count, Is.EqualTo(count));
            var valid = new[] { new CompetitionReceipt(CompetitionAction.CompletePenaltyLoop), new CompetitionReceipt(CompetitionAction.CompletePenaltyLoop),
                new CompetitionReceipt(CompetitionAction.ReachLapBoundary) };
            s.StepBatch(new SimulationTick(4), valid);
            Assert.That(s.CompletedLaps, Is.EqualTo(1)); Assert.That(s.PendingPenaltyLoops, Is.Zero); Assert.That(s.Tick.Value, Is.EqualTo(4));
            int penalties = 0;
            for (int i = count; i < log.Count; i++) { Assert.That(log[i].Tick.Value, Is.EqualTo(4)); if (log[i].EventType == RaceEventType.PenaltyLoopCompleted) penalties++; }
            Assert.That(penalties, Is.EqualTo(2));
        }
        [Test] public void ValidatedBatchDoesNotAllocateOrDuplicateEvents()
        {
            var log = new RaceEventLog(100); var s = new CompetitionLifecycle(CompetitionProfileTests.TestProfile(), new AthleteId("a"), 20, log);
            var start = new[] { new CompetitionReceipt(CompetitionAction.Start) }; s.StepBatch(new SimulationTick(1), start);
            var none = new[] { new CompetitionReceipt(CompetitionAction.None) }; int count = log.Count;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10001; i++) s.StepBatch(new SimulationTick(i), none);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(log.Count, Is.EqualTo(count));
            Assert.That(s.LastFrame.ElapsedSeconds, Is.EqualTo(500));
        }
    }
}
