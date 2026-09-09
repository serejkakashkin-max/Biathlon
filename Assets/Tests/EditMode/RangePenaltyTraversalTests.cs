using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class RangePenaltyTraversalTests
    {
        private const long Meter = 1000000000;
        public static RangePenaltyTraversal Create(RaceEventLog log, int boutIndex = 0)
        {
            var p = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", p.ToRouteRequirements()), p.ToRouteRequirements());
            return new RangePenaltyTraversal(p, track, route, boutIndex, new AthleteId("shot"), 20, log);
        }
        private static ShotResolutionFrame Completed(long tick, int mask) => new ShotResolutionFrame(
            new ShootingFrame(new SimulationTick(tick), ShootingPhase.Complete, 5, false, 0), default, default, ShotOutcome.NoShot, mask);

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void RealGeometricMissesRequireExactExtraDistanceAndTime(int misses)
        {
            var log = new RaceEventLog(200); var traversal = Create(log);
            for (int i = 1; i <= 30; i++) traversal.Step(new SimulationTick(i), Meter);
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.Shooting));
            var s = new ShotResolutionSystem(ShootingStateTests.Definition(), TargetGeometryTests.Definition(), ShootingWindTests.Definition(),
                ShootingStanceTests.Definition().Get(ShootingStance.Prone), new AthleteId("shot"), traversal.RangeId, 20, log, traversal.Tick);
            long tick = 30;
            while (s.Phase != ShootingPhase.Complete)
            {
                tick++;
                var action = tick == 31 ? ShootingAction.EnterRange : tick == 32 ? ShootingAction.AssignLane : ShootingAction.Trigger;
                int target = s.ShotsFired;
                var ray = target < 5 - misses ? new ShotRay(TargetGeometryTests.Definition().CenterX(target), 0, 0, 0, 0, 1) : new ShotRay(10, 0, 0, 0, 0, 1);
                var frame = s.Step(new SimulationTick(tick), action,
                    action == ShootingAction.AssignLane ? new ShootingLaneAssignment(traversal.RangeId, "lane", 1) : default, ray);
                traversal.Step(new SimulationTick(tick), 0, s.Phase == ShootingPhase.Complete ? frame : (ShotResolutionFrame?)null);
                Assert.That(tick, Is.LessThan(1000));
            }
            Assert.That(traversal.RequiredLoops, Is.EqualTo(misses));
            long shootingEnd = tick;
            while (traversal.Phase != RangeTraversalPhase.Complete) traversal.Step(new SimulationTick(++tick), Meter);
            Assert.That(traversal.CompletedLoops, Is.EqualTo(misses));
            Assert.That(traversal.TravelledNanometers, Is.EqualTo((100 + 150 * misses) * Meter));
            Assert.That(traversal.RouteNanometers, Is.EqualTo(920 * Meter));
            Assert.That(tick - shootingEnd, Is.EqualTo(70 + 150 * misses));
            int completed = 0;
            for (int i = 0; i < log.Count; i++) if (log[i].EventType == RaceEventType.PenaltyLoopCompleted) completed++;
            Assert.That(completed, Is.EqualTo(misses));
        }

        [Test] public void FiringLineClampsMotionAndPenaltyCannotCompleteOneNanometerEarly()
        {
            var s = Create(new RaceEventLog(100), 1);
            var approach = s.Step(new SimulationTick(1), 100 * Meter);
            Assert.That(approach.RouteNanometers, Is.EqualTo(1850 * Meter));
            Assert.That(approach.ConsumedNanometers, Is.EqualTo(30 * Meter));
            Assert.That(approach.UnconsumedNanometers, Is.EqualTo(70 * Meter));
            var wait = s.Step(new SimulationTick(2), long.MaxValue);
            Assert.That(wait.ConsumedNanometers, Is.Zero);
            s.Step(new SimulationTick(3), 0, Completed(3, 30));
            s.Step(new SimulationTick(4), 200 * Meter - 1);
            Assert.That(s.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop)); Assert.That(s.CompletedLoops, Is.Zero);
            Assert.That(s.PenaltyNanometers, Is.EqualTo(150 * Meter - 1));
            Assert.That(s.SamplePenalty().SegmentId, Is.EqualTo("penalty_arc"));
            s.Step(new SimulationTick(5), 1);
            Assert.That(s.CompletedLoops, Is.EqualTo(1)); Assert.That(s.Phase, Is.EqualTo(RangeTraversalPhase.Rejoin));
            var last = s.Step(new SimulationTick(6), 25 * Meter);
            Assert.That(last.Phase, Is.EqualTo(RangeTraversalPhase.Complete)); Assert.That(last.UnconsumedNanometers, Is.EqualTo(5 * Meter));
            Assert.That(last.RouteNanometers, Is.EqualTo(1920 * Meter));
        }

        [Test] public void InvalidOrDuplicateBoutCannotChangeDistanceClockOrEvents()
        {
            var log = new RaceEventLog(100); var s = Create(log);
            Assert.Catch(() => s.Step(new SimulationTick(1), 0, Completed(1, 31)));
            Assert.That(s.Tick.Value, Is.Zero); Assert.That(log.Count, Is.Zero);
            s.Step(new SimulationTick(1), 30 * Meter); int count = log.Count;
            Assert.Catch(() => s.Step(new SimulationTick(2), -1));
            Assert.Catch(() => s.Step(new SimulationTick(3), 0));
            Assert.Catch(() => s.Step(new SimulationTick(2), 1, Completed(2, 31)));
            Assert.Catch(() => s.Step(new SimulationTick(2), 0, Completed(1, 31)));
            Assert.Catch(() => s.Step(new SimulationTick(2), 0, Completed(2, 32)));
            Assert.That(s.Tick.Value, Is.EqualTo(1)); Assert.That(log.Count, Is.EqualTo(count));
            s.Step(new SimulationTick(2), 0, Completed(2, 31));
            Assert.Catch(() => s.Step(new SimulationTick(3), 0, Completed(3, 31)));
            Assert.That(s.RequiredLoops, Is.Zero);
        }

        [Test] public void MismatchedPenaltyLengthAndAmbiguousRangeOccurrenceFailAtConstruction()
        {
            var p = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = p.ToRouteRequirements();
            var plan = RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", requirements);
            var route = new RaceRoute(track, plan, requirements);
            var mismatch = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(), "penalty_loop_m", "100"));
            Assert.Catch(() => new RangePenaltyTraversal(mismatch, track, route, 0, new AthleteId("shot"), 20, new RaceEventLog(10)));
            var doubled = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(), "distance_m", "6000"));
            var repeated = RaceRoutePlan.FromJson(plan.ToJson().Replace("[\"main_long\"]", "[\"main_long\",\"main_long\"]"));
            var ambiguous = new RaceRoute(track, repeated, doubled.ToRouteRequirements());
            Assert.Catch(() => new RangePenaltyTraversal(doubled, track, ambiguous, 0, new AthleteId("shot"), 20, new RaceEventLog(10)));
        }

        [Test] public void MultiBoundaryTraversalReplaysAndActiveMovementDoesNotAllocate()
        {
            var la = new RaceEventLog(100); var lb = new RaceEventLog(100); var a = Create(la); var b = Create(lb);
            for (int tick = 1; tick <= 3; tick++)
            {
                long motion = tick == 1 ? 30 * Meter : tick == 2 ? 0 : long.MaxValue;
                var bout = tick == 2 ? Completed(2, 0) : (ShotResolutionFrame?)null;
                Assert.That(a.Step(new SimulationTick(tick), motion, bout), Is.EqualTo(b.Step(new SimulationTick(tick), motion, bout)));
            }
            Assert.That(a.CompletedLoops, Is.EqualTo(5)); Assert.That(a.TravelledNanometers, Is.EqualTo(850 * Meter));
            Assert.That(la.Count, Is.EqualTo(lb.Count)); for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
            var active = Create(new RaceEventLog(100)); active.Step(new SimulationTick(1), 30 * Meter);
            active.Step(new SimulationTick(2), 0, Completed(2, 0));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 3; tick < 100003; tick++) active.Step(new SimulationTick(tick), 1000000);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(active.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
        }
    }
}
