using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathStagingTests
    {
        private const long M = SkiState.NanometersPerMeter;
        // Open-instance delegate keeps internal ownership operations out of the public authority API
        // and permits a real allocation measurement without reflection invocation allocations.
        private static readonly Action<RangePenaltyTraversal, RangePenaltyTraversal, long> Stage =
            (Action<RangePenaltyTraversal, RangePenaltyTraversal, long>)typeof(RangePenaltyTraversal)
                .GetMethod("StageAcceptedStateFrom", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Action<RangePenaltyTraversal, RangePenaltyTraversal, long>));

        private static RangePenaltyTraversal Create(CompetitionProfile profile, TrackDefinition track, RaceRoute route,
            PhysicalRangePathMap path, IRaceEventSink events, int bout = 0, string athlete = "staging_athlete", int frequency = 20,
            long initialTick = 0) => new RangePenaltyTraversal(profile, track, route, bout, new AthleteId(athlete), frequency,
                events, new SimulationTick(initialTick), path);

        private static void AssertState(RangePenaltyTraversal traversal, RangeTraversalFrame expected)
        {
            Assert.That(traversal.Tick, Is.EqualTo(expected.Tick));
            Assert.That(traversal.Phase, Is.EqualTo(expected.Phase));
            Assert.That(traversal.RouteNanometers, Is.EqualTo(expected.RouteNanometers));
            Assert.That(traversal.PenaltyNanometers, Is.EqualTo(expected.PenaltyNanometers));
            Assert.That(traversal.TravelledNanometers, Is.EqualTo(expected.TravelledNanometers));
            Assert.That(traversal.RequiredLoops, Is.EqualTo(expected.RequiredLoops));
            Assert.That(traversal.CompletedLoops, Is.EqualTo(expected.CompletedLoops));
            Assert.That(traversal.LastFrame, Is.EqualTo(expected));
        }

        private static ShotResolutionFrame Completed(long tick, int mask) => new ShotResolutionFrame(
            new ShootingFrame(new SimulationTick(tick), ShootingPhase.Complete, 5, false, 0), default, default, ShotOutcome.NoShot, mask);

        [Test] public void BeforeLogicalEntryCopiesExactAcceptedStateWithoutAdmissionPauseEventsOrPathChange()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20);
            var accepted = Create(profile, track, route, first, events);
            var scratch = Create(profile, track, route, second, events, initialTick: 93);
            accepted.Step(new SimulationTick(1), 0);
            var saved = accepted.LastFrame;
            Assert.That(accepted.RouteNanometers, Is.EqualTo(5 * M));

            Stage(scratch, accepted, 0);

            AssertState(accepted, saved); AssertState(scratch, saved);
            Assert.That(events.Count, Is.Zero); Assert.That(scratch.RangePath, Is.SameAs(second));
            Assert.That(scratch.FiringRouteNanometers, Is.EqualTo(48 * M));
            Assert.That(accepted.FiringRouteNanometers, Is.EqualTo(28 * M));
            Assert.That(first.MapRoute(0), Is.EqualTo(second.MapRoute(0)));
            // The staged traversal can consume the same next movement immediately. No preparation Step occurs.
            var moved = scratch.Step(new SimulationTick(2), 43 * M);
            Assert.That(moved.Phase, Is.EqualTo(RangeTraversalPhase.Shooting));
            Assert.That(moved.RouteNanometers, Is.EqualTo(48 * M));
            Assert.That(moved.ConsumedNanometers, Is.EqualTo(43 * M));
            Assert.That(events.Count, Is.EqualTo(1)); AssertState(accepted, saved);
        }

        [Test] public void AcceptedSharedAisleStateCopiesExactlyAndRetainsTheDestinationMatStop()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20);
            var accepted = Create(profile, track, route, first, events);
            var scratch = Create(profile, track, route, second, events);
            accepted.Step(new SimulationTick(1), 2 * M);
            var saved = accepted.LastFrame;

            Stage(scratch, accepted, 7 * M);

            AssertState(accepted, saved); AssertState(scratch, saved);
            Assert.That(saved.TravelledNanometers, Is.EqualTo(2 * M));
            Assert.That(saved.ConsumedNanometers, Is.EqualTo(2 * M));
            Assert.That(saved.UnconsumedNanometers, Is.Zero);
            Assert.That(scratch.PreviewMotion(41 * M).RouteNanometers, Is.EqualTo(48 * M));
            Assert.That(accepted.PreviewMotion(41 * M).RouteNanometers, Is.EqualTo(28 * M));
            Assert.That(scratch.RangePath, Is.SameAs(second));
            Assert.That(events.Count, Is.Zero);
            AssertState(accepted, saved); AssertState(scratch, saved);

            // Rejected owner previews may reuse the same scratch with the next exact accepted frame.
            accepted.Step(new SimulationTick(2), M);
            Stage(scratch, accepted, 8 * M);
            AssertState(scratch, accepted.LastFrame); Assert.That(events.Count, Is.Zero);
        }

        [Test] public void GlobalFirstForkBoundsBothLanesAndEveryScheduledShootingLap()
        {
            PhysicalRangePathTraversalTests.Setup(out _, out _, out _, out var first, out var second);
            foreach (var map in new[] { first, second })
            {
                Assert.That(map.FirstForkRouteNanometers(0), Is.EqualTo(10 * M));
                Assert.That(map.FirstForkRouteNanometers(1), Is.EqualTo(310 * M));
                Assert.Throws<ArgumentException>(() => map.FirstForkRouteNanometers(2));
                Assert.Throws<ArgumentOutOfRangeException>(() => map.FirstForkRouteNanometers(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => map.FirstForkRouteNanometers(3));
            }
        }

        [Test] public void OneNanometreBeforeEarliestForkStagesButExactForkRejectsBeforeWrites()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20);
            var accepted = Create(profile, track, route, second, events);
            var scratch = Create(profile, track, route, first, events);
            accepted.Step(new SimulationTick(1), 5 * M - 1);
            Stage(scratch, accepted, 10 * M - 1);
            var scratchSaved = scratch.LastFrame;
            accepted.Step(new SimulationTick(2), 1);
            var acceptedSaved = accepted.LastFrame;

            Assert.Throws<ArgumentException>(() => Stage(scratch, accepted, 10 * M));

            AssertState(scratch, scratchSaved); AssertState(accepted, acceptedSaved);
            Assert.That(events.Count, Is.Zero);
        }

        [TestCase(-1)][TestCase(0)][TestCase(5)][TestCase(6)][TestCase(8)][TestCase(10)]
        public void InconsistentBodyCoordinateRejectsWithoutCopyOrEvents(long bodyMeters)
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20);
            var accepted = Create(profile, track, route, first, events);
            var scratch = Create(profile, track, route, second, events, initialTick: 27);
            accepted.Step(new SimulationTick(1), 2 * M);
            var acceptedSaved = accepted.LastFrame; var scratchSaved = scratch.LastFrame;

            Assert.Throws<ArgumentException>(() => Stage(scratch, accepted, bodyMeters * M));

            AssertState(scratch, scratchSaved); AssertState(accepted, acceptedSaved);
            Assert.That(events.Count, Is.Zero);
        }

        [Test] public void FutureBoutCannotStageWhileActualBodyStillOccupiesThePreviousPrivateExit()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20);
            var accepted = Create(profile, track, route, first, events, bout: 1);
            var scratch = Create(profile, track, route, second, events, bout: 1);
            accepted.Step(new SimulationTick(1), 0);
            var saved = scratch.LastFrame;
            Assert.That(accepted.RouteNanometers, Is.EqualTo(305 * M));
            Assert.That(first.MapRoute(80 * M).LoopId, Is.EqualTo("exit_aisle"));

            Assert.Throws<ArgumentException>(() => Stage(scratch, accepted, 80 * M));

            AssertState(scratch, saved); Assert.That(events.Count, Is.Zero);
            // A completed previous exit on ordinary main can stage the next bout without repositioning.
            Stage(scratch, accepted, 101 * M);
            AssertState(scratch, accepted.LastFrame); Assert.That(events.Count, Is.Zero);
        }

        [TestCase(RangeTraversalPhase.Shooting)]
        [TestCase(RangeTraversalPhase.RangeExit)]
        [TestCase(RangeTraversalPhase.PenaltyLoop)]
        [TestCase(RangeTraversalPhase.Complete)]
        public void SourceBeyondApproachRejectsBeforeCopyEvenWithPretendedSharedPrefix(RangeTraversalPhase phase)
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(30);
            var accepted = Create(profile, track, route, first, events);
            var scratch = Create(profile, track, route, second, events);
            accepted.Step(new SimulationTick(1), 100 * M);
            if (phase != RangeTraversalPhase.Shooting) accepted.Step(new SimulationTick(2), 0, Completed(2, 30));
            if (phase == RangeTraversalPhase.PenaltyLoop) accepted.Step(new SimulationTick(3), 100 * M - 28 * M);
            if (phase == RangeTraversalPhase.Complete) accepted.Step(new SimulationTick(3), 500 * M);
            Assert.That(accepted.Phase, Is.EqualTo(phase));
            var scratchSaved = scratch.LastFrame; var acceptedSaved = accepted.LastFrame; int count = events.Count;

            Assert.Throws<ArgumentException>(() => Stage(scratch, accepted, 7 * M));

            AssertState(scratch, scratchSaved); AssertState(accepted, acceptedSaved);
            Assert.That(events.Count, Is.EqualTo(count));
        }

        [TestCase("profile")][TestCase("track")][TestCase("route")][TestCase("athlete")]
        [TestCase("bout")][TestCase("frequency")][TestCase("events")]
        [TestCase("definition")][TestCase("physical_map")][TestCase("null_destination")][TestCase("null_source")]
        public void ForeignImmutableContextAndMapFamilyRejectBeforeWrites(string foreign)
        {
            var definition = PhysicalRangePathDefinitionTests.Create(out var track, out _, out _);
            var profile = PhysicalRangePathTraversalTests.Profile(); var requirements = profile.ToRouteRequirements();
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("staging_context_route", track.Id, "main", requirements), requirements);
            var physical = new PhysicalTrackMap(track, route);
            var sourceMap = new PhysicalRangePathMap(physical, track, route, definition, 0);
            var targetProfile = foreign == "profile" ? CompetitionProfile.FromJson(profile.ToJson()) : profile;
            var targetTrack = foreign == "track" ? TrackDefinition.FromJson(track.ToJson()) : track;
            var targetRoute = foreign == "route" ? new RaceRoute(track, RaceRoutePlan.FromJson(route.PlanSnapshotJson), requirements) : route;
            var targetDefinition = foreign == "definition" ? PhysicalRangePathDefinitionTests.Create(out _, out _, out _) : definition;
            var targetPhysical = foreign == "physical_map" ? new PhysicalTrackMap(track, route) : physical;
            var targetMap = new PhysicalRangePathMap(targetPhysical, targetTrack, targetRoute, targetDefinition, 1);
            var events = new RaceEventLog(20); var targetEvents = foreign == "events" ? new RaceEventLog(20) : events;
            var accepted = Create(profile, track, route, foreign == "null_source" ? null : sourceMap, events);
            var scratch = Create(targetProfile, targetTrack, targetRoute, foreign == "null_destination" ? null : targetMap,
                targetEvents, foreign == "bout" ? 1 : 0, foreign == "athlete" ? "other_athlete" : "staging_athlete",
                foreign == "frequency" ? 30 : 20, 12);
            accepted.Step(new SimulationTick(1), 2 * M);
            var acceptedSaved = accepted.LastFrame; var scratchSaved = scratch.LastFrame;

            Assert.Throws<ArgumentException>(() => Stage(scratch, accepted, 7 * M));

            AssertState(scratch, scratchSaved); AssertState(accepted, acceptedSaved);
            Assert.That(events.Count, Is.Zero); Assert.That(targetEvents.Count, Is.Zero);
        }

        [Test] public void SelfAndNullSourceRejectWithoutAuthorityMutation()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out _);
            var events = new RaceEventLog(20); var accepted = Create(profile, track, route, first, events);
            accepted.Step(new SimulationTick(1), 2 * M); var saved = accepted.LastFrame;
            Assert.Throws<ArgumentException>(() => Stage(accepted, accepted, 7 * M));
            Assert.Throws<ArgumentNullException>(() => Stage(accepted, null, 7 * M));
            AssertState(accepted, saved); Assert.That(events.Count, Is.Zero);
            Assert.That(typeof(RangePenaltyTraversal).GetMethod("StageAcceptedStateFrom", BindingFlags.Public | BindingFlags.Instance), Is.Null);
        }

        [Test] public void WarmStagingAndGlobalForkQueriesAllocateNothingAndNeverRecordEvents()
        {
            PhysicalRangePathTraversalTests.Setup(out var profile, out var track, out var route, out var first, out var second);
            var events = new RaceEventLog(20); var accepted = Create(profile, track, route, first, events);
            var scratch = Create(profile, track, route, second, events);
            accepted.Step(new SimulationTick(1), 2 * M); var saved = accepted.LastFrame; long checksum = 0;
            for (int i = 0; i < 1000; i++)
            {
                Stage(scratch, accepted, 7 * M);
                checksum += first.FirstForkRouteNanometers(i % 2) + scratch.PreviewMotion().RouteNanometers;
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                Stage(scratch, accepted, 7 * M);
                checksum += second.FirstForkRouteNanometers(i % 2) + scratch.PreviewMotion().RouteNanometers;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(checksum, Is.GreaterThan(0));
            AssertState(accepted, saved); AssertState(scratch, saved); Assert.That(events.Count, Is.Zero);
        }
    }
}
