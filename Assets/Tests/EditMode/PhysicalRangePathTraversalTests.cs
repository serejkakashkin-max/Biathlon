using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathTraversalTests
    {
        private const long M = SkiState.NanometersPerMeter;
        public static CompetitionProfile Profile() => CompetitionProfile.FromJson(
            PhysiologyContractTests.ReplaceNumber(CompetitionProfileTests.TestProfile().ToJson(), "distance_m", "900"));
        public static void Setup(out CompetitionProfile profile, out TrackDefinition track, out RaceRoute route,
            out PhysicalRangePathMap first, out PhysicalRangePathMap second)
        {
            var definition = PhysicalRangePathDefinitionTests.Create(out track, out _, out _); profile = Profile();
            var requirements = profile.ToRouteRequirements();
            route = new RaceRoute(track, RaceRoutePlan.TimeTrial("personal_mat_traversal_route", track.Id, "main", requirements), requirements);
            var physical = new PhysicalTrackMap(track, route);
            first = new PhysicalRangePathMap(physical, track, route, definition, 0);
            second = new PhysicalRangePathMap(physical, track, route, definition, 1);
        }
        private static RangePenaltyTraversal Create(CompetitionProfile profile, TrackDefinition track, RaceRoute route,
            PhysicalRangePathMap path, RaceEventLog events, int bout = 0, long initialTick = 0) =>
            new RangePenaltyTraversal(profile, track, route, bout, new AthleteId("path_athlete"), 20, events,
                new SimulationTick(initialTick), path);
        private static ShotResolutionFrame Completed(long tick, int mask) => new ShotResolutionFrame(
            new ShootingFrame(new SimulationTick(tick), ShootingPhase.Complete, 5, false, 0), default, default, ShotOutcome.NoShot, mask);

        [TestCase(0, 28)][TestCase(1, 48)]
        public void EachLaneStopsExactlyAtItsOwnMatOnBothShootingLaps(int lane, int localStop)
        {
            Setup(out var profile, out var track, out var route, out var first, out var second);
            var path = lane == 0 ? first : second;
            for (int bout = 0; bout < profile.BoutCount; bout++)
            {
                var events = new RaceEventLog(20); var traversal = Create(profile, track, route, path, events, bout, 44);
                Assert.That(events.Count, Is.Zero); Assert.That(traversal.RangePath, Is.SameAs(path));
                long start = route.GetLapStart(bout).Millimeters * SkiState.NanometersPerMillimeter;
                long expected = start + localStop * M;
                Assert.That(traversal.FiringRouteNanometers, Is.EqualTo(expected));
                Assert.That(traversal.FiringRouteNanometers, Is.EqualTo(path.FiringRouteNanometers(bout)));
                var at = path.MapRoute(expected); Assert.That(at.LocalNanometers, Is.EqualTo(path.MatAnchor.Distance.Millimeters * SkiState.NanometersPerMillimeter));
                Assert.That(at.LoopId, Is.EqualTo(path.MatAnchor.LoopId));
                var frame = traversal.Step(new SimulationTick(45), 200 * M);
                Assert.That(frame.Phase, Is.EqualTo(RangeTraversalPhase.Shooting)); Assert.That(frame.RouteNanometers, Is.EqualTo(expected));
                Assert.That(frame.ConsumedNanometers, Is.EqualTo((localStop - 5) * M));
                Assert.That(frame.UnconsumedNanometers, Is.EqualTo((200 - localStop + 5) * M));
                Assert.That(events.Count, Is.EqualTo(1)); Assert.That(events[0].Tick.Value, Is.EqualTo(45));
                Assert.That(traversal.Step(new SimulationTick(46), long.MaxValue).ConsumedNanometers, Is.Zero);
                Assert.That(traversal.RouteNanometers, Is.EqualTo(expected));
            }
        }
        [Test] public void PersonalMatPreviewIsReadOnlyAndNeverCreatesShootingOrPenaltyAuthority()
        {
            Setup(out var profile, out var track, out var route, out var path, out _);
            var events = new RaceEventLog(20); var traversal = Create(profile, track, route, path, events);
            var initial = traversal.LastFrame;
            var before = traversal.PreviewMotion(23 * M - 1);
            Assert.That(before.Phase, Is.EqualTo(RangeTraversalPhase.Approach)); Assert.That(before.RouteNanometers, Is.EqualTo(28 * M - 1));
            Assert.That(before.DistanceToBoundaryNanometers, Is.EqualTo(1));
            foreach (long distance in new[] { 23 * M, 24 * M, long.MaxValue })
            {
                var point = traversal.PreviewMotion(distance);
                Assert.That(point.Phase, Is.EqualTo(RangeTraversalPhase.Shooting)); Assert.That(point.RouteNanometers, Is.EqualTo(28 * M));
                Assert.That(point.DistanceToBoundaryNanometers, Is.Zero);
            }
            Assert.That(traversal.LastFrame, Is.EqualTo(initial)); Assert.That(traversal.Tick.Value, Is.Zero);
            Assert.That(traversal.RouteNanometers, Is.EqualTo(5 * M)); Assert.That(traversal.RequiredLoops, Is.Zero);
            Assert.That(traversal.TravelledNanometers, Is.Zero); Assert.That(events.Count, Is.Zero);
            Assert.That(typeof(RangePenaltyTraversal).GetProperty(nameof(RangePenaltyTraversal.RangePath)).GetSetMethod(true), Is.Null);
            Assert.That(typeof(RangePenaltyTraversal).GetProperty(nameof(RangePenaltyTraversal.FiringRouteNanometers)).GetSetMethod(true), Is.Null);
        }
        [TestCase(0)][TestCase(1)]
        public void PersonalStopPreservesOrderedExitAndRequiresACompleteExactPenaltyCircuit(int lane)
        {
            Setup(out var profile, out var track, out var route, out var first, out var second);
            var path = lane == 0 ? first : second; var events = new RaceEventLog(30);
            var traversal = Create(profile, track, route, path, events);
            traversal.Step(new SimulationTick(1), 100 * M);
            traversal.Step(new SimulationTick(2), 0, Completed(2, 30));
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.RangeExit)); Assert.That(traversal.RequiredLoops, Is.EqualTo(1));
            long toExit = 95 * M - traversal.FiringRouteNanometers;
            int count = events.Count;
            var exit = traversal.PreviewMotion(toExit);
            Assert.That(exit.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyApproach)); Assert.That(exit.RouteNanometers, Is.EqualTo(95 * M));
            Assert.That(exit.DistanceToBoundaryNanometers, Is.EqualTo(5 * M));
            var penaltyEntry = traversal.PreviewMotion(toExit + 5 * M);
            Assert.That(penaltyEntry.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
            Assert.That(penaltyEntry.RouteNanometers, Is.EqualTo(100 * M)); Assert.That(penaltyEntry.PenaltyNanometers, Is.Zero);
            Assert.That(events.Count, Is.EqualTo(count)); Assert.That(traversal.CompletedLoops, Is.Zero);
            traversal.Step(new SimulationTick(3), toExit + 5 * M + 150 * M - 1);
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop)); Assert.That(traversal.CompletedLoops, Is.Zero);
            Assert.That(traversal.RouteNanometers, Is.EqualTo(100 * M)); Assert.That(traversal.PenaltyNanometers, Is.EqualTo(150 * M - 1));
            count = events.Count;
            Assert.That(traversal.PreviewMotion(1).Phase, Is.EqualTo(RangeTraversalPhase.Complete));
            Assert.That(traversal.CompletedLoops, Is.Zero); Assert.That(events.Count, Is.EqualTo(count));
            var last = traversal.Step(new SimulationTick(4), 1);
            Assert.That(last.CompletedLoops, Is.EqualTo(1)); Assert.That(last.Phase, Is.EqualTo(RangeTraversalPhase.Complete));
            Assert.That(last.TravelledNanometers, Is.EqualTo(245 * M)); Assert.That(last.UnconsumedNanometers, Is.Zero);
            Assert.That(last.RouteNanometers, Is.EqualTo(traversal.RangeRejoinNanometers));
            int completed = 0;
            for (int i = 0; i < events.Count; i++) if (events[i].EventType == RaceEventType.PenaltyLoopCompleted) completed++;
            Assert.That(completed, Is.EqualTo(1));
        }
        [Test] public void ForeignTrackAndRouteAssociationsFailBeforeAnyEventsEvenWhenSnapshotsMatch()
        {
            Setup(out var profile, out var track, out var route, out var path, out _);
            var events = new RaceEventLog(20);
            var copiedTrack = TrackDefinition.FromJson(track.ToJson());
            Assert.Throws<ArgumentException>(() => Create(profile, copiedTrack, route, path, events));
            var requirements = profile.ToRouteRequirements();
            var copiedRoute = new RaceRoute(track, RaceRoutePlan.FromJson(route.PlanSnapshotJson), requirements);
            Assert.That(copiedRoute.PlanSnapshotJson, Is.EqualTo(route.PlanSnapshotJson));
            Assert.Throws<ArgumentException>(() => Create(profile, track, copiedRoute, path, events));
            Assert.That(events.Count, Is.Zero);
        }
        [Test] public void AMapMustExposeTheParticularBoutShootingLap()
        {
            Setup(out var profile, out var track, out _, out _, out _);
            var requirements = CompetitionRouteRequirements.FromJson(profile.ToRouteRequirements().ToJson().Replace("[1,2]", "[1]"));
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("missing_bout_range", track.Id, "main", requirements), requirements);
            var definition = PhysicalRangePathDefinitionTests.Create(out _, out _, out _);
            var path = new PhysicalRangePathMap(new PhysicalTrackMap(track, route), track, route, definition, 0);
            var events = new RaceEventLog(20);
            Assert.Throws<ArgumentException>(() => Create(profile, track, route, path, events, 1));
            Assert.That(events.Count, Is.Zero);
        }
        [Test] public void WarmPersonalApproachPenaltyStepsAndReadOnlyPreviewsAllocateNothing()
        {
            Setup(out var profile, out var track, out var route, out var path, out _);
            var approach = Create(profile, track, route, path, new RaceEventLog(30));
            var penalty = Create(profile, track, route, path, new RaceEventLog(30));
            penalty.Step(new SimulationTick(1), 100 * M); penalty.Step(new SimulationTick(2), 0, Completed(2, 0));
            penalty.Step(new SimulationTick(3), 100 * M - penalty.FiringRouteNanometers);
            long checksum = 0;
            for (int i = 0; i < 1000; i++)
            {
                approach.Step(approach.Tick.Next(), 1000000); penalty.Step(penalty.Tick.Next(), 1000000);
                checksum += approach.PreviewMotion(1000000).RouteNanometers + penalty.PreviewMotion(1000000).PenaltyNanometers;
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                approach.Step(approach.Tick.Next(), 1000000); penalty.Step(penalty.Tick.Next(), 1000000);
                checksum += approach.PreviewMotion(1000000).RouteNanometers + penalty.PreviewMotion(1000000).PenaltyNanometers;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(checksum, Is.GreaterThan(0));
            Assert.That(approach.Phase, Is.EqualTo(RangeTraversalPhase.Approach)); Assert.That(penalty.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
        }
        [Test] public void OmittedAndExplicitNullPathsPreserveTheLegacyFramesAndEventSequence()
        {
            var leftEvents = new RaceEventLog(30); var rightEvents = new RaceEventLog(30);
            var original = RangePenaltyTraversalTests.Create(leftEvents);
            var profile = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = profile.ToRouteRequirements();
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", requirements), requirements);
            var explicitNull = new RangePenaltyTraversal(profile, track, route, 0, new AthleteId("shot"), 20, rightEvents, rangePath: null);
            Assert.That(original.RangePath, Is.Null); Assert.That(explicitNull.RangePath, Is.Null);
            Assert.That(original.FiringRouteNanometers, Is.EqualTo(850 * M));
            for (int tick = 1; tick <= 3; tick++)
            {
                long movement = tick == 1 ? 30 * M : tick == 2 ? 0 : long.MaxValue;
                var bout = tick == 2 ? Completed(2, 0) : (ShotResolutionFrame?)null;
                Assert.That(original.Step(new SimulationTick(tick), movement, bout), Is.EqualTo(explicitNull.Step(new SimulationTick(tick), movement, bout)));
            }
            Assert.That(original.RouteNanometers, Is.EqualTo(920 * M)); Assert.That(original.TravelledNanometers, Is.EqualTo(850 * M));
            Assert.That(original.CompletedLoops, Is.EqualTo(5)); Assert.That(leftEvents.Count, Is.EqualTo(11));
            Assert.That(rightEvents.Count, Is.EqualTo(leftEvents.Count));
            for (int i = 0; i < leftEvents.Count; i++) Assert.That(leftEvents[i], Is.EqualTo(rightEvents[i]));
            Assert.That(leftEvents[0].EventType, Is.EqualTo(RaceEventType.RangeTraversalPhaseChanged));
            Assert.That(leftEvents[0].Payload, Is.EqualTo((int)RangeTraversalPhase.Shooting));
            Assert.That(leftEvents[10].Payload, Is.EqualTo((int)RangeTraversalPhase.Complete));
        }
    }
}
