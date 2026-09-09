using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShotResolutionTests
    {
        public static ShotResolutionSystem Create(RaceEventLog log, ShootingStance stance = ShootingStance.Prone) => new ShotResolutionSystem(
            ShootingStateTests.Definition(), TargetGeometryTests.Definition(), ShootingWindTests.Definition(), ShootingStanceTests.Definition().Get(stance),
            new AthleteId("shot"), "range", 20, log);
        private static ShotRay Ray(int target) => new WeaponAimState(Math.Atan(TargetGeometryTests.Definition().CenterX(target) / 50), 0).Ray;
        private static void Enter(ShotResolutionSystem s)
        { s.Step(new SimulationTick(1), ShootingAction.EnterRange); s.Step(new SimulationTick(2), ShootingAction.AssignLane, new ShootingLaneAssignment("range", "lane", 1)); }
        [TestCase(ShootingStance.Prone)] [TestCase(ShootingStance.Standing)]
        public void PerfectAimCompletesFiveIndependentTargetsAtEitherStance(ShootingStance stance)
        {
            var log = new RaceEventLog(100); var s = Create(log, stance); Enter(s);
            for (int i = 3; i <= 500 && s.Phase != ShootingPhase.Complete; i++)
                s.Step(new SimulationTick(i), ShootingAction.Trigger, ray: Ray(Math.Min(4, s.ShotsFired)));
            Assert.That(s.Phase, Is.EqualTo(ShootingPhase.Complete)); Assert.That(s.ShotsFired, Is.EqualTo(5)); Assert.That(s.ClosedMask, Is.EqualTo(31));
            int resolved = 0;
            for (int i = 0; i < log.Count; i++) if (log[i].EventType == RaceEventType.ShotResolved) { resolved++; Assert.That(log[i].Payload, Is.EqualTo(resolved)); }
            Assert.That(resolved, Is.EqualTo(5));
        }
        [Test] public void RepeatedTargetAndWindMissConsumeRoundsWithoutDuplicateClosure()
        {
            var repeat = Create(new RaceEventLog(100)); var miss = Create(new RaceEventLog(100)); Enter(repeat); Enter(miss);
            int duplicates = 0, misses = 0;
            for (int i = 3; i <= 500; i++)
            {
                if (repeat.Step(new SimulationTick(i), ShootingAction.Trigger, ray: Ray(2)).Outcome == ShotOutcome.AlreadyClosed) duplicates++;
                if (miss.Step(new SimulationTick(i), ShootingAction.Trigger, ray: Ray(2), wind: new WindVector(5, 0)).Outcome == ShotOutcome.Miss) misses++;
            }
            Assert.That(repeat.ClosedMask, Is.EqualTo(4)); Assert.That(duplicates, Is.EqualTo(4)); Assert.That(miss.ClosedMask, Is.Zero); Assert.That(misses, Is.EqualTo(5));
        }
        [Test] public void InvalidRayWindAndPrematureTriggerCannotPartlyDischarge()
        {
            var log = new RaceEventLog(100); var s = Create(log); Enter(s); int events = log.Count;
            Assert.Catch(() => s.Step(new SimulationTick(3), ShootingAction.Trigger));
            Assert.Catch(() => s.Step(new SimulationTick(3), ShootingAction.Trigger, ray: Ray(2), wind: new WindVector(40, 40)));
            Assert.That(log.Count, Is.EqualTo(events)); Assert.That(s.ShotsFired, Is.Zero);
            Assert.That(s.Step(new SimulationTick(3), ShootingAction.Trigger, ray: Ray(2)).Outcome, Is.EqualTo(ShotOutcome.NoShot));
            Assert.That(s.ClosedMask, Is.Zero);
        }
        [Test] public void ShotFramesAndEventsReplayAndActiveResolutionDoesNotAllocate()
        {
            var la = new RaceEventLog(100); var lb = new RaceEventLog(100); var a = Create(la); var b = Create(lb); Enter(a); Enter(b);
            var ray = Ray(2);
            for (int i = 3; i <= 500; i++) Assert.That(a.Step(new SimulationTick(i), ShootingAction.Trigger, ray: ray), Is.EqualTo(b.Step(new SimulationTick(i), ShootingAction.Trigger, ray: ray)));
            Assert.That(la.Count, Is.EqualTo(lb.Count)); for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
            var active = Create(new RaceEventLog(100)); Enter(active);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 3; i <= 500; i++) active.Step(new SimulationTick(i), ShootingAction.Trigger, ray: ray);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(active.ShotsFired, Is.EqualTo(5));
        }
    }
}
