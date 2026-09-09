using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class RacePathMotionTests
    {
        private static RacePathMotion Create()
        {
            var p = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", p.ToRouteRequirements()), p.ToRouteRequirements());
            return new RacePathMotion(new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), TimeTrialTests.Athlete(), track, route,
                SnowConditionTests.Conditions("packed_neutral"), "ski_neutral", 20);
        }
        private static TimeTrialInput Input(long tick) => new TimeTrialInput(new SimulationInput(new SimulationTick(tick), new AthleteId(TimeTrialTests.Athlete().Id), 800), 1, 1, 0);
        [Test] public void SurfaceAndRouteStopsProduceContinuousBoundedTimeSegments()
        {
            var system = Create(); var segments = new RaceMotionSegment[10];
            var frame = system.Preview(new SkiState(799900000000, 10), 799900000000, 800200000000, null, 350, 0, Input(1), segments);
            Assert.That(frame.AtRouteStop, Is.True); Assert.That(frame.RouteNanometers, Is.EqualTo(800200000000));
            Assert.That(frame.SegmentCount, Is.EqualTo(2)); Assert.That(frame.RemainingSeconds, Is.GreaterThan(0));
            Assert.That(segments[0].RouteAfter, Is.EqualTo(800000000000));
            Assert.That(segments[1].StartSeconds, Is.EqualTo(segments[0].EndSeconds));
            Assert.That(segments[1].EndSeconds, Is.EqualTo(frame.ConsumedSeconds));
            Assert.That(frame.ConsumedSeconds + frame.RemainingSeconds, Is.EqualTo(0.05));
        }
        [TestCase(0)] [TestCase(2)] [TestCase(5)]
        public void SharedPhysicsTraversesRequiredPenaltyDistanceBeforeRejoining(int misses)
        {
            var system = Create(); var range = RangePenaltyTraversalTests.Create(new RaceEventLog(100)); var segments = new RaceMotionSegment[20];
            long tick = 0, main = range.RangeEntryNanometers; var motion = new SkiState(main, 8);
            while (range.Phase != RangeTraversalPhase.Shooting)
            {
                var frame = system.Preview(motion, main, 3000000000000, range, 350, 0.2, Input(++tick), segments);
                motion = frame.Motion; main = frame.RouteNanometers; range.Step(new SimulationTick(tick), frame.RangeTravelNanometers);
                if (frame.AtFiringLine) Assert.That(main, Is.EqualTo(850000000000));
            }
            var waiting = system.Preview(motion, main, 3000000000000, range, 350, 0.2, Input(tick + 1), segments);
            Assert.That(waiting.RangeTravelNanometers, Is.Zero); Assert.That(waiting.SegmentCount, Is.Zero); Assert.That(waiting.RemainingSeconds, Is.EqualTo(0.05));
            range.Step(new SimulationTick(++tick), 0, new ShotResolutionFrame(new ShootingFrame(new SimulationTick(tick), ShootingPhase.Complete, 5, false, 0),
                default, default, ShotOutcome.NoShot, (1 << (5 - misses)) - 1));
            motion = new SkiState(motion.DistanceNanometers, 0); // Stationary shooting stop is committed by the orchestrator.
            bool sawPenalty = false;
            while (range.Phase != RangeTraversalPhase.Complete)
            {
                var frame = system.Preview(motion, main, 920000000000, range, 350, 0.2, Input(++tick), segments);
                for (int i = 0; i < frame.SegmentCount; i++)
                    if (segments[i].OnPenaltyLoop) { sawPenalty = true; Assert.That(segments[i].RouteAfter, Is.EqualTo(segments[i].RouteBefore)); }
                motion = frame.Motion; main = frame.RouteNanometers; range.Step(new SimulationTick(tick), frame.RangeTravelNanometers);
                Assert.That(tick, Is.LessThan(20000));
            }
            Assert.That(sawPenalty, Is.EqualTo(misses > 0)); Assert.That(range.CompletedLoops, Is.EqualTo(misses));
            Assert.That(main, Is.EqualTo(920000000000)); Assert.That(motion.DistanceNanometers, Is.EqualTo((920 + misses * 150) * 1000000000L));
        }
        [Test] public void InsufficientPreviewBufferLeavesAuthoritativeRangeUnchangedAndPreviewDoesNotAllocate()
        {
            var system = Create(); var range = RangePenaltyTraversalTests.Create(new RaceEventLog(100)); var input = Input(1);
            Assert.Catch(() => system.Preview(new SkiState(range.RouteNanometers, 8), range.RouteNanometers, 3000000000000, range, 350, 0, input, Array.Empty<RaceMotionSegment>()));
            Assert.That(range.Tick.Value, Is.Zero); Assert.That(range.TravelledNanometers, Is.Zero);
            var buffer = new RaceMotionSegment[10]; var state = new SkiState(range.RouteNanometers, 8);
            var expected = system.Preview(state, range.RouteNanometers, 3000000000000, range, 350, 0, input, buffer); RacePathMotionFrame actual = default;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) actual = system.Preview(state, range.RouteNanometers, 3000000000000, range, 350, 0, input, buffer);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(actual, Is.EqualTo(expected));
            Assert.That(range.Tick.Value, Is.Zero);
        }
    }
}
