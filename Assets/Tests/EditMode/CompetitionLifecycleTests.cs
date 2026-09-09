using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CompetitionLifecycleTests
    {
        private static CompetitionLifecycle Create(RaceEventLog events) => new CompetitionLifecycle(CompetitionProfileTests.TestProfile(), new AthleteId("racer"), 20, events);
        [Test] public void AllRaceObligationsAndFractionalElapsedTimeArePreserved()
        {
            var s = Create(new RaceEventLog(100));
            s.Step(new SimulationTick(1)); s.Step(new SimulationTick(2), CompetitionAction.Start);
            s.Step(new SimulationTick(3), CompetitionAction.EnterRange);
            s.Step(new SimulationTick(4), CompetitionAction.CompleteBout, 31, 5);
            s.Step(new SimulationTick(5), CompetitionAction.ReachLapBoundary);
            s.Step(new SimulationTick(6), CompetitionAction.EnterRange);
            s.Step(new SimulationTick(7), CompetitionAction.CompleteBout, 7, 5);
            Assert.That(s.PendingPenaltyLoops, Is.EqualTo(2));
            s.Step(new SimulationTick(8), CompetitionAction.CompletePenaltyLoop);
            Assert.That(s.Phase, Is.EqualTo(CompetitionPhase.Penalty));
            s.Step(new SimulationTick(9), CompetitionAction.CompletePenaltyLoop);
            s.Step(new SimulationTick(10), CompetitionAction.ReachLapBoundary);
            s.Step(new SimulationTick(11), CompetitionAction.Finish, finish: new FinishTime(10, 500000000, 20));
            Assert.That(s.ElapsedFinishTime.WholeTicks, Is.EqualTo(8));
            Assert.That(s.ElapsedFinishTime.FractionParts, Is.EqualTo(500000000));
            Assert.That(s.ElapsedFinishTime.Seconds, Is.EqualTo(8.5 / 20).Within(1e-15));
            Assert.That(s.CompletedLaps, Is.EqualTo(3)); Assert.That(s.CompletedBouts, Is.EqualTo(2));
            s.Step(new SimulationTick(12), CompetitionAction.Classify);
            Assert.That(s.Step(new SimulationTick(13)).ElapsedSeconds, Is.EqualTo(s.ElapsedFinishTime.Seconds));
        }
        [Test] public void SkippedBoutPenaltyAndInvalidOutcomeFailAtomically()
        {
            var log = new RaceEventLog(100); var s = Create(log); s.Step(new SimulationTick(1), CompetitionAction.Start);
            Assert.Catch(() => s.Step(new SimulationTick(2), CompetitionAction.Finish, finish: new FinishTime(2, 0, 20)));
            Assert.Catch(() => s.Step(new SimulationTick(2), CompetitionAction.ReachLapBoundary));
            s.Step(new SimulationTick(2), CompetitionAction.EnterRange); int count = log.Count;
            Assert.Catch(() => s.Step(new SimulationTick(3), CompetitionAction.ReachLapBoundary));
            Assert.Catch(() => s.Step(new SimulationTick(3), CompetitionAction.CompleteBout, 31, 4));
            Assert.That(log.Count, Is.EqualTo(count)); Assert.That(s.Tick.Value, Is.EqualTo(2));
            s.Step(new SimulationTick(3), CompetitionAction.CompleteBout, 30, 5);
            Assert.Catch(() => s.Step(new SimulationTick(4), CompetitionAction.ReachLapBoundary));
            Assert.That(s.PendingPenaltyLoops, Is.EqualTo(1));
        }
        [Test] public void RangeBeforeLapEndDoesNotPrematurelyAdvanceLapAndCannotBeEnteredTwice()
        {
            var s = Create(new RaceEventLog(100)); s.Step(new SimulationTick(1), CompetitionAction.Start);
            s.Step(new SimulationTick(2), CompetitionAction.EnterRange);
            Assert.That(s.CompletedLaps, Is.Zero);
            Assert.Catch(() => s.Step(new SimulationTick(3), CompetitionAction.EnterRange));
            s.Step(new SimulationTick(3), CompetitionAction.CompleteBout, 31, 5);
            Assert.That(s.CompletedLaps, Is.Zero); Assert.That(s.CompletedBouts, Is.EqualTo(1));
            Assert.Catch(() => s.Step(new SimulationTick(4), CompetitionAction.EnterRange));
            s.Step(new SimulationTick(4), CompetitionAction.ReachLapBoundary);
            Assert.That(s.CompletedLaps, Is.EqualTo(1)); Assert.That(s.Phase, Is.EqualTo(CompetitionPhase.Skiing));
            s.Step(new SimulationTick(5), CompetitionAction.EnterRange);
            Assert.That(s.CompletedLaps, Is.EqualTo(1));
        }
        [Test] public void DnsDnfAndReplayKeepTerminalStateWithoutAllocations()
        {
            var dns = Create(new RaceEventLog(100)); dns.Step(new SimulationTick(1), CompetitionAction.MarkDidNotStart);
            Assert.Catch(() => dns.Step(new SimulationTick(2), CompetitionAction.Start));
            var la = new RaceEventLog(100); var lb = new RaceEventLog(100); var a = Create(la); var b = Create(lb);
            for (int i = 1; i <= 100; i++)
            {
                var action = i == 1 ? CompetitionAction.Start : i == 90 ? CompetitionAction.MarkDidNotFinish : CompetitionAction.None;
                Assert.That(a.Step(new SimulationTick(i), action), Is.EqualTo(b.Step(new SimulationTick(i), action)));
            }
            Assert.That(la.Count, Is.EqualTo(lb.Count)); for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(a.Phase, Is.EqualTo(CompetitionPhase.DidNotFinish));
            Assert.That(a.LastFrame.ElapsedSeconds, Is.EqualTo(89 / 20.0));
        }
    }
}
