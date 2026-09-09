using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingStateTests
    {
        public static ShootingDefinition Definition() => ShootingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_lifecycle_v1.json"));
        private static ShootingStateMachine Create(RaceEventLog log, int hz = 20) => new ShootingStateMachine(Definition(), new AthleteId("shooter"), "range", hz, log);
        private static ShootingLaneAssignment Lane => new ShootingLaneAssignment("range", "lane_1", 1);
        [TestCase(20)] [TestCase(30)] [TestCase(144)]
        public void CompleteBoutHonorsSetupBoltExitAndDoesNotResolveHits(int hz)
        {
            var log = new RaceEventLog(100); var s = Create(log, hz); var d = Definition();
            s.Step(new SimulationTick(1), ShootingAction.EnterRange);
            Assert.That(s.Phase, Is.EqualTo(ShootingPhase.AwaitingLane));
            s.Step(new SimulationTick(2), ShootingAction.AssignLane, Lane);
            long first = 2 + (long)Math.Ceiling(d.SetupSeconds * hz), lastShot = first + (d.Rounds - 1) * (long)Math.Ceiling(d.BoltSeconds * hz);
            long complete = lastShot + (long)Math.Ceiling(d.ExitSeconds * hz);
            for (long i = 3; i <= complete; i++)
            {
                var f = s.Step(new SimulationTick(i), ShootingAction.Trigger);
                bool expectedShot = i >= first && i <= lastShot && (i - first) % (long)Math.Ceiling(d.BoltSeconds * hz) == 0;
                Assert.That(f.ShotAccepted, Is.EqualTo(expectedShot));
                if (i < complete) Assert.That(f.Phase, Is.Not.EqualTo(ShootingPhase.Complete));
            }
            Assert.That(s.Phase, Is.EqualTo(ShootingPhase.Complete)); Assert.That(s.ShotsFired, Is.EqualTo(5));
        }
        [Test] public void BadTransitionsAreAtomicAndLaneCannotBeStolenOrReassigned()
        {
            var log = new RaceEventLog(100); var s = Create(log);
            Assert.Catch(() => s.Step(new SimulationTick(1), ShootingAction.AssignLane, Lane));
            Assert.That(log.Count, Is.Zero);
            s.Step(new SimulationTick(1), ShootingAction.EnterRange);
            int count = log.Count;
            Assert.Catch(() => s.Step(new SimulationTick(2), ShootingAction.AssignLane, new ShootingLaneAssignment("other", "lane", 1)));
            Assert.That(log.Count, Is.EqualTo(count)); Assert.That(s.Phase, Is.EqualTo(ShootingPhase.AwaitingLane));
            s.Step(new SimulationTick(2), ShootingAction.AssignLane, Lane);
            Assert.Catch(() => s.Step(new SimulationTick(3), ShootingAction.AssignLane, Lane));
            Assert.That(s.Step(new SimulationTick(3), ShootingAction.Trigger).ShotAccepted, Is.False);
        }
        [Test] public void IdenticalActionsProduceIdenticalFramesAndEventsWithoutAllocation()
        {
            var la = new RaceEventLog(100); var lb = new RaceEventLog(100); var a = Create(la); var b = Create(lb);
            for (int i = 1; i <= 300; i++)
            {
                var action = i == 1 ? ShootingAction.EnterRange : i == 2 ? ShootingAction.AssignLane : ShootingAction.Trigger;
                var lane = i == 2 ? Lane : default;
                Assert.That(a.Step(new SimulationTick(i), action, lane), Is.EqualTo(b.Step(new SimulationTick(i), action, lane)));
            }
            Assert.That(la.Count, Is.EqualTo(lb.Count));
            for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 301; i <= 10300; i++) a.Step(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void ActiveBoutDoesNotAllocateAndInvalidDurationsAreRejected()
        {
            Assert.Catch(() => ShootingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "bolt_seconds", "0")));
            var s = Create(new RaceEventLog(100));
            s.Step(new SimulationTick(1), ShootingAction.EnterRange);
            s.Step(new SimulationTick(2), ShootingAction.AssignLane, Lane);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 3; i <= 250; i++) s.Step(new SimulationTick(i), ShootingAction.Trigger);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(s.Phase, Is.EqualTo(ShootingPhase.Complete));
        }
    }
}
