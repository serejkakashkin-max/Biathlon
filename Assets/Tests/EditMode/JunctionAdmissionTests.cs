using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class JunctionAdmissionTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private sealed class Log : IRaceEventSink
        {
            public readonly List<RaceEvent> Events = new List<RaceEvent>();
            public void Record(RaceEvent value) => Events.Add(value);
        }
        private static (PhysicalTrackMap map, PhysicalJunctionDefinition junctions) Geometry()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            var map = new PhysicalTrackMap(track, new RaceRoute(track,
                RaceRoutePlan.TimeTrial("admission", track.Id, "main_long", requirements), requirements));
            return (map, PhysicalJunctionDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                "tests/fixtures/tracks/synthetic_flat_junctions_v1.json"), track));
        }
        private static PhysicalOccupancySweep At(PhysicalTrackMap map, string id, string loop, long position, long travel = 0) =>
            new PhysicalOccupancySweep(new AthleteId(id), map.MapLoop(loop, position), travel, 0, 0);
        private static PhysicalOccupancySweep Move(JunctionAdmissionSession session, int index, long travel)
        { var state = session.GetState(index); return new PhysicalOccupancySweep(state.AthleteId, state.Start, travel, 0, 0); }
        private static PhysicalConnectionDefinition Connections(PhysicalJunctionDefinition junctions) =>
            PhysicalConnectionDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_flat_connections_v1.json"), junctions);
        private static PhysicalOccupancySweep PlanMove(JunctionAdmissionTickPlan plan, int index, long travel)
        { var state = plan.GetState(index); return new PhysicalOccupancySweep(state.AthleteId, state.Start, travel, state.LateralAfterMeters, state.LateralAfterMeters); }

        [Test] public void WholeTickPlanKeepsAllSeamStateAndEventsPrivateUntilCommit()
        {
            var g = Geometry(); var log = new Log(); var initial = new[] { At(g.map, "a", "main_long", 900 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var plan = new JunctionAdmissionTickPlan(session, 16); plan.Begin(new SimulationTick(1));
            plan.AddSubstep(0, false, initial, new[] { "penalty_enter" });
            plan.AddSubstep(1000000000, true, new[] { PlanMove(plan, 0, 3 * M) }, Array.Empty<string>());
            Assert.That(plan.IsComplete, Is.True); Assert.That(session.Tick.Value, Is.Zero);
            Assert.That(session.GetState(0), Is.EqualTo(initial[0])); Assert.That(log.Events.Count, Is.Zero);
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a")); Assert.That(plan.GetOwner(0).IsValid, Is.False);
            plan.Validate(); plan.Commit(); Assert.Catch(() => plan.Commit());
            Assert.That(session.Tick.Value, Is.EqualTo(1)); Assert.That(session.GetState(0).Start, Is.EqualTo(g.map.MapPenalty(3 * M)));
            Assert.That(log.Events.Count, Is.EqualTo(2)); Assert.That(log.Events[0].EventType, Is.EqualTo(RaceEventType.PhysicalSeamCrossed));
            Assert.That(log.Events[1].EventType, Is.EqualTo(RaceEventType.JunctionReleased));
        }

        [Test] public void LateFailureAndEventOverflowDiscardEntirePlannedTick()
        {
            var g = Geometry(); var log = new Log(); var initial = new[] { At(g.map, "a", "main_long", 900 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var plan = new JunctionAdmissionTickPlan(session, 8); plan.Begin(new SimulationTick(1));
            plan.AddSubstep(0, false, initial, new[] { "penalty_enter" });
            plan.AddSubstep(500000000, false, new[] { PlanMove(plan, 0, M) }, Array.Empty<string>());
            Assert.Catch(() => plan.AddSubstep(400000000, true, new[] { PlanMove(plan, 0, 0) }, Array.Empty<string>()));
            Assert.Catch(() => plan.Commit()); Assert.That(session.GetState(0), Is.EqualTo(initial[0]));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(log.Events.Count, Is.Zero);
            var small = new JunctionAdmissionTickPlan(session, 1); small.Begin(new SimulationTick(1));
            small.AddSubstep(0, false, initial, new[] { "penalty_enter" });
            Assert.Catch(() => small.AddSubstep(1000000000, true, new[] { PlanMove(small, 0, 3 * M) }, Array.Empty<string>()));
            Assert.Catch(() => small.Commit()); Assert.That(session.Tick.Value, Is.Zero); Assert.That(log.Events.Count, Is.Zero);
            plan.Begin(new SimulationTick(1)); plan.AddSubstep(1000000000, true, initial, Array.Empty<string>()); plan.Commit();
            Assert.That(session.GetState(0), Is.EqualTo(initial[0])); Assert.That(log.Events.Count, Is.Zero);
        }

        [Test] public void ConcurrentPlansAndZeroTimePartialAuthorityCannotCommitStaleState()
        {
            var g = Geometry(); var initial = new[] { At(g.map, "a", "main_long", 100 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, new Log());
            var first = new JunctionAdmissionTickPlan(session, 8); var second = new JunctionAdmissionTickPlan(session, 8);
            first.Begin(new SimulationTick(1)); second.Begin(new SimulationTick(1));
            first.AddSubstep(1000000000, true, new[] { PlanMove(first, 0, M) }, Array.Empty<string>());
            second.AddSubstep(1000000000, true, initial, Array.Empty<string>());
            first.Commit(); Assert.That(second.IsComplete, Is.False); Assert.Catch(() => second.Commit());
            Assert.That(session.GetState(0).Start.LocalNanometers, Is.EqualTo(101 * M));
            session.Commit(session.PrepareSubstep(new SimulationTick(2), 0, 0, false,
                new[] { Move(session, 0, 0) }, Array.Empty<string>()));
            Assert.Catch(() => first.Begin(new SimulationTick(2)));
        }

        [Test] public void ReusedWholeTickPlansRetainTailsAndAllocateNothing()
        {
            var g = Geometry(); var log = new Log(); var initial = new[] { At(g.map, "a", "main_long", 900 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var plan = new JunctionAdmissionTickPlan(session, 8); plan.Begin(new SimulationTick(1));
            plan.AddSubstep(0, false, initial, new[] { "penalty_enter" });
            var row = new[] { PlanMove(plan, 0, 0) }; var none = Array.Empty<string>();
            plan.AddSubstep(1000000000, true, row, none); plan.Commit();
            plan.Begin(new SimulationTick(2));
            Assert.Catch(() => plan.AddSubstep(0, false, row, new[] { "penalty_exit" }));
            Assert.That(session.Tick.Value, Is.EqualTo(1));
            plan.Begin(new SimulationTick(2)); plan.AddSubstep(1000000000, true, row, none); plan.Commit();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 3; tick < 1003; tick++)
            { plan.Begin(new SimulationTick(tick)); plan.AddSubstep(500000000, false, row, none); plan.AddSubstep(1000000000, true, row, none); plan.Commit(); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a")); Assert.That(log.Events.Count, Is.EqualTo(1));
        }

        [Test] public void MovingSeamAndOutgoingSubstepsShareOneTickWithoutBoundaryDelay()
        {
            var g = Geometry(); var log = new Log();
            var initial = new[] { At(g.map, "a", "main_long", 900 * M - 100000000) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var tick = new SimulationTick(1);
            session.Commit(session.PrepareSubstep(tick, 0, 500000000, false, new[] { Move(session, 0, 100000000) }, Array.Empty<string>()));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(session.PendingFraction, Is.EqualTo(500000000));
            var seam = session.PrepareSubstep(tick, 500000000, 500000000, false,
                new[] { Move(session, 0, 0) }, new[] { "penalty_enter" });
            Assert.That(seam.StartFraction, Is.EqualTo(seam.EndFraction));
            session.Commit(seam);
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(session.PendingFraction, Is.EqualTo(500000000));
            session.Commit(session.PrepareSubstep(tick, 500000000, JunctionAdmissionSession.FractionParts, true,
                new[] { Move(session, 0, 100000000) }, Array.Empty<string>()));
            Assert.That(session.Tick.Value, Is.EqualTo(1)); Assert.That(session.PendingFraction, Is.Zero);
            Assert.That(session.GetState(0).Start, Is.EqualTo(g.map.MapPenalty(100000000)));
            Assert.That(log.Events.Count, Is.EqualTo(1)); Assert.That(log.Events[0].Tick.Value, Is.EqualTo(1));
            Assert.That(log.Events[0].EventType, Is.EqualTo(RaceEventType.PhysicalSeamCrossed));
        }

        [Test] public void FractionalCoverageRejectsGapsOverlapsEarlySealAndZeroTimeMotion()
        {
            var g = Geometry(); var log = new Log();
            var initial = new[] { At(g.map, "a", "main_long", 900 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var tick = new SimulationTick(1); var none = Array.Empty<string>();
            Assert.Catch(() => session.PrepareSubstep(tick, 1, 100, false, initial, none));
            Assert.Catch(() => session.PrepareSubstep(tick, 0, -1, false, initial, none));
            Assert.Catch(() => session.PrepareSubstep(tick, 0, 1000000001, false, initial, none));
            Assert.Catch(() => session.PrepareSubstep(tick, 0, 100, true, initial, none));
            Assert.Catch(() => session.PrepareSubstep(tick, 0, 0, false, new[] { Move(session, 0, 1) }, none));
            Assert.Catch(() => session.PrepareSubstep(tick, 0, 100, false, initial, new[] { "penalty_enter" }));
            session.Commit(session.PrepareSubstep(tick, 0, 100, false, initial, none));
            var ready = session.PrepareSubstep(tick, 100, 200, false, initial, none);
            Assert.Catch(() => session.PrepareSubstep(tick, 99, 200, false, initial, none));
            Assert.Catch(() => session.Commit(ready));
            Assert.Catch(() => session.Prepare(new SimulationTick(2), initial));
            Assert.Catch(() => session.Prepare(tick, initial));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(session.PendingFraction, Is.EqualTo(100)); Assert.That(log.Events.Count, Is.Zero);
            session.Commit(session.PrepareSubstep(tick, 100, 1000000000, false, initial, none));
            Assert.That(session.Tick.Value, Is.Zero);
            // An endpoint can host a zero-time seam before the same tick is sealed.
            session.Commit(session.PrepareSubstep(tick, 1000000000, 1000000000, true, initial, new[] { "penalty_enter" }));
            Assert.That(session.Tick.Value, Is.EqualTo(1)); Assert.That(session.PendingFraction, Is.Zero);
        }

        [Test] public void FractionalAndWholeTickMotionHaveIdenticalFinalStateAndAllocateNothing()
        {
            var g = Geometry(); var initial = new[] { At(g.map, "a", "main_long", 100 * M) };
            var log = new Log(); var wholeLog = new Log();
            var split = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log);
            var whole = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, wholeLog);
            var none = Array.Empty<string>(); var row = new PhysicalOccupancySweep[1];
            for (int t = 1; t <= 10; t++)
            {
                var tick = new SimulationTick(t);
                split.Commit(split.PrepareSubstep(tick, 0, 400000000, false, new[] { Move(split, 0, 40000000) }, none));
                split.Commit(split.PrepareSubstep(tick, 400000000, 1000000000, true, new[] { Move(split, 0, 60000000) }, none));
                whole.Step(tick, new[] { Move(whole, 0, 100000000) });
                Assert.That(split.GetState(0), Is.EqualTo(whole.GetState(0)));
            }
            Assert.That(log.Events, Is.EqualTo(wholeLog.Events));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int t = 11; t < 1011; t++)
            {
                row[0] = Move(split, 0, 1);
                split.Commit(split.PrepareSubstep(new SimulationTick(t), 0, 500000000, false, row, none));
                row[0] = Move(split, 0, 1);
                split.Commit(split.PrepareSubstep(new SimulationTick(t), 500000000, 1000000000, true, row, none));
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void PreparedSeamCommitsOnceAndRetainsReservationAcrossBranchAndStop()
        {
            var g = Geometry(); var log = new Log();
            var initial = new[] { At(g.map, "a", "main_long", 900 * M), At(g.map, "b", "main_long", 890 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var preview = session.Prepare(new SimulationTick(1), initial, new[] { "penalty_enter", null });
            Assert.That(session.GetState(0).Start.LoopId, Is.EqualTo("main_long")); Assert.That(log.Events.Count, Is.Zero);
            Assert.That(session.GetPreparedState(preview, 0).Start.LoopId, Is.EqualTo("penalty_150"));
            session.Commit(preview); Assert.Catch(() => session.Commit(preview));
            Assert.That(log.Events.Count, Is.EqualTo(1)); Assert.That(log.Events[0].EventType, Is.EqualTo(RaceEventType.PhysicalSeamCrossed));
            session.Step(new SimulationTick(2), new[] { Move(session, 0, 0), Move(session, 1, 20 * M) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a")); Assert.That(session.WasAdmitted(1), Is.False);
            // Cannot replace an uncleared incoming branch with the reverse transition.
            Assert.Catch(() => session.Prepare(new SimulationTick(3), new[] { Move(session, 0, 0), Move(session, 1, 0) }, new[] { "penalty_exit", null }));
            Assert.That(session.Tick.Value, Is.EqualTo(2)); Assert.That(log.Events.Count, Is.EqualTo(1));
            session.Step(new SimulationTick(3), new[] { Move(session, 0, 3 * M), Move(session, 1, 20 * M) });
            Assert.That(session.GetOwner(0).IsValid, Is.False); Assert.That(session.WasAdmitted(1), Is.False);
            session.Step(new SimulationTick(4), new[] { Move(session, 0, 0), Move(session, 1, 20 * M) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("b"));
        }

        [Test] public void FullPenaltyCircuitAndExitReplayIgnoreProposalOrdering()
        {
            var g = Geometry(); var firstLog = new Log(); var secondLog = new Log();
            var initial = new[] { At(g.map, "a", "main_long", 900 * M), At(g.map, "b", "main_short", 100 * M) };
            var first = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, firstLog, Connections(g.junctions));
            Array.Reverse(initial);
            var second = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, secondLog, Connections(g.junctions));
            for (int tick = 1; tick <= 4; tick++)
            {
                long distance = tick == 2 ? 150 * M : tick == 4 ? 23 * M : 0;
                string connection = tick == 1 ? "penalty_enter" : tick == 3 ? "penalty_exit" : null;
                first.Commit(first.Prepare(new SimulationTick(tick), new[] { Move(first, 0, distance), Move(first, 1, 0) }, new[] { connection, null }));
                second.Commit(second.Prepare(new SimulationTick(tick), new[] { Move(second, 1, 0), Move(second, 0, distance) }, new[] { null, connection }));
                for (int i = 0; i < 2; i++) Assert.That(second.GetState(i), Is.EqualTo(first.GetState(i)));
            }
            Assert.That(first.GetState(0).Start, Is.EqualTo(g.map.MapRoute(923 * M)));
            Assert.That(firstLog.Events, Is.EqualTo(secondLog.Events));
            Assert.That(firstLog.Events.FindAll(e => e.EventType == RaceEventType.PhysicalSeamCrossed).Count, Is.EqualTo(2));
            Assert.That(first.GetOwner(0).IsValid, Is.False);
        }

        [Test] public void InvalidSeamRowsInvalidatePreviewWithoutChangingAuthority()
        {
            var g = Geometry(); var log = new Log();
            var initial = new[] { At(g.map, "a", "main_long", 900 * M) };
            var session = new JunctionAdmissionSession(g.map, g.junctions, OccupancyTests.Definition(), initial, 20, log, Connections(g.junctions));
            var preview = session.Prepare(new SimulationTick(1), initial, new[] { "penalty_enter" });
            Assert.Catch(() => session.Prepare(new SimulationTick(1), new[] { Move(session, 0, 1) }, new[] { "penalty_enter" }));
            Assert.Catch(() => session.Commit(preview));
            Assert.Catch(() => session.Prepare(new SimulationTick(1), initial, new[] { "unknown" }));
            Assert.Catch(() => session.Prepare(new SimulationTick(1), initial, new[] { "penalty_enter", null }));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(session.GetState(0), Is.EqualTo(initial[0])); Assert.That(log.Events.Count, Is.Zero);
            session.Commit(session.Prepare(new SimulationTick(1), initial, new[] { "penalty_enter" }));
            var stationary = new[] { Move(session, 0, 0) };
            session.Step(new SimulationTick(2), stationary);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 3; tick < 1003; tick++) session.Step(new SimulationTick(tick), stationary);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void RetainedIncomingBodyProtectsSpaceOutsideTheAuthoredGuard()
        {
            var g = Geometry(); var log = new Log();
            string json = g.junctions.ToJson().Replace("\"guard_before_m\":2", "\"guard_before_m\":0.1")
                .Replace("\"guard_after_m\":22", "\"guard_after_m\":0.1");
            var junctions = PhysicalJunctionDefinition.FromJson(json, SyntheticTrackTests.Generated("synthetic_flat_v1"));
            var initial = new[] { At(g.map, "a", "main_long", 900 * M), At(g.map, "b", "main_long", 898 * M) };
            var session = new JunctionAdmissionSession(g.map, junctions, OccupancyTests.Definition(), initial, 20, log, Connections(junctions));
            session.Commit(session.Prepare(new SimulationTick(1), initial, new[] { "penalty_enter", null }));
            // The follower's centre/body does not touch the narrow guard, but approaches the retained incoming body.
            Assert.Throws<InvalidOperationException>(() => session.Prepare(new SimulationTick(2),
                new[] { Move(session, 0, 0), Move(session, 1, 800000000) }));
            Assert.That(session.Tick.Value, Is.EqualTo(1)); Assert.That(log.Events.Count, Is.EqualTo(1));
            Assert.That(session.GetState(1).Start.LocalNanometers, Is.EqualTo(898 * M));
        }

        [Test] public void SimultaneousBranchesKeepOwnerUntilInclusiveWholeBodyClearance()
        {
            var geometry = Geometry(); var log = new Log();
            var initial = new[] { At(geometry.map, "b", "penalty_150", 140 * M), At(geometry.map, "a", "main_long", 890 * M) };
            var session = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, log);
            session.Step(new SimulationTick(1), new[] { Move(session, 1, 10 * M), Move(session, 0, 20 * M) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a"));
            Assert.That(session.WasAdmitted(0), Is.True); Assert.That(session.WasAdmitted(1), Is.False);
            Assert.That(session.GetState(1).Start.LocalNanometers, Is.EqualTo(140 * M));
            long halfBody = (long)Math.Ceiling(OccupancyTests.Definition().LongitudinalSeparation * M / 2);
            session.Step(new SimulationTick(2), new[] { Move(session, 0, 12 * M + halfBody), Move(session, 1, 10 * M) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a"), "Touching the expanded guard retains ownership.");
            // A forged branch/coordinate switch cannot drop ownership, even with unchanged athlete ID.
            Assert.Catch(() => session.Step(new SimulationTick(3), new[] {
                At(geometry.map, "a", "penalty_150", 20 * M), Move(session, 1, 10 * M) }));
            Assert.That(session.Tick.Value, Is.EqualTo(2)); Assert.That(log.Events.Count, Is.EqualTo(1));
            session.Step(new SimulationTick(3), new[] { Move(session, 0, 1), Move(session, 1, 10 * M) });
            Assert.That(session.GetOwner(0).IsValid, Is.False); Assert.That(session.WasAdmitted(1), Is.False);
            session.Step(new SimulationTick(4), new[] { Move(session, 0, 0), Move(session, 1, 10 * M) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("b"));
            Assert.That(log.Events.Count, Is.EqualTo(3));
            Assert.That(log.Events[0].EventType, Is.EqualTo(RaceEventType.JunctionReserved));
            Assert.That(log.Events[1].EventType, Is.EqualTo(RaceEventType.JunctionReleased));
            Assert.That(log.Events[1].Tick.Value, Is.EqualTo(3));
            Assert.That(log.Events[2].AthleteId.Value, Is.EqualTo("b"));
        }

        [Test] public void DeniedEntrantCannotBeHitByFollowerAndRejectedBatchChangesNothing()
        {
            var geometry = Geometry(); var log = new Log();
            var initial = new[] { At(geometry.map, "a", "main_long", 890 * M),
                At(geometry.map, "b", "penalty_150", 140 * M), At(geometry.map, "c", "penalty_150", 135 * M) };
            var session = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, log);
            Assert.Throws<InvalidOperationException>(() => session.Step(new SimulationTick(1), new[] {
                Move(session, 0, 20 * M), Move(session, 1, 10 * M), Move(session, 2, 10 * M) }));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(log.Events.Count, Is.Zero);
            Assert.That(session.GetOwner(0).IsValid, Is.False);
            for (int i = 0; i < 3; i++) Assert.That(session.GetState(i), Is.EqualTo(initial[i]));
            session.Step(new SimulationTick(1), new[] { Move(session, 0, 20 * M), Move(session, 1, 10 * M), Move(session, 2, 0) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("a"));
            var bad = new[] { Move(session, 0, 0), Move(session, 1, 0), Move(session, 1, 0) };
            Assert.Catch(() => session.Step(new SimulationTick(2), bad));
            Assert.That(session.Tick.Value, Is.EqualTo(1)); Assert.That(log.Events.Count, Is.EqualTo(1));
        }

        [Test] public void ReorderedReplayAndPassThroughEventsAreExactAndActiveStepsAllocateNothing()
        {
            var geometry = Geometry(); var firstLog = new Log(); var secondLog = new Log();
            var initial = new[] { At(geometry.map, "a", "main_long", 890 * M), At(geometry.map, "b", "penalty_150", 140 * M) };
            var first = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, firstLog);
            Array.Reverse(initial);
            var second = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, secondLog);
            // A full crossing with both endpoints outside still reserves for the complete interval.
            first.Step(new SimulationTick(1), new[] { Move(first, 0, 40 * M), Move(first, 1, 20 * M) });
            second.Step(new SimulationTick(1), new[] { Move(second, 1, 20 * M), Move(second, 0, 40 * M) });
            Assert.That(first.GetOwner(0).IsValid, Is.False);
            Assert.That(first.WasAdmitted(1), Is.False);
            Assert.That(firstLog.Events.Count, Is.EqualTo(2));
            Assert.That(secondLog.Events, Is.EqualTo(firstLog.Events));
            for (int i = 0; i < 2; i++) Assert.That(second.GetState(i), Is.EqualTo(first.GetState(i)));
            // Move the first athlete away; let the other enter and stay physically inside its guard.
            first.Step(new SimulationTick(2), new[] { Move(first, 0, 0), Move(first, 1, 10 * M) });
            var wishes = new[] { Move(first, 0, 0), Move(first, 1, 0) };
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 3; i < 1003; i++) first.Step(new SimulationTick(i), wishes);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void ConflictingInitialOwnersAreRejectedAcrossDistinctBranches()
        {
            var geometry = Geometry();
            var initial = new[] { At(geometry.map, "a", "main_long", 910 * M), At(geometry.map, "b", "penalty_150", 0) };
            Assert.Catch(() => new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, new Log()));
        }

        [Test] public void ExistingOccupantOutranksLowerIdNewcomer()
        {
            var geometry = Geometry(); var log = new Log();
            var initial = new[] { At(geometry.map, "z", "main_long", 910 * M), At(geometry.map, "a", "penalty_150", 140 * M) };
            var session = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, log);
            session.Step(new SimulationTick(1), new[] { Move(session, 0, 10 * M), Move(session, 1, 0) });
            Assert.That(session.GetOwner(0).Value, Is.EqualTo("z"));
            Assert.That(session.WasAdmitted(0), Is.False); Assert.That(session.WasAdmitted(1), Is.True);
            Assert.That(log.Events.Count, Is.Zero, "Initial ownership belongs to context, and cannot be reacquired or evicted by a new request.");
        }

        [Test] public void PreparedAdmissionLeavesAuthorityUntouchedAndMatchesDirectReplay()
        {
            var geometry = Geometry(); var log = new Log(); var directLog = new Log();
            var initial = new[] { At(geometry.map, "a", "main_long", 890 * M), At(geometry.map, "b", "penalty_150", 140 * M) };
            var staged = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, log);
            var direct = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, directLog);
            var commands = new[] { Move(staged, 0, 20 * M), Move(staged, 1, 10 * M) };
            var preview = staged.Prepare(new SimulationTick(1), commands);
            Assert.That(staged.Tick.Value, Is.Zero); Assert.That(log.Events.Count, Is.Zero);
            Assert.That(staged.GetOwner(0).IsValid, Is.False);
            for (int i = 0; i < 2; i++) Assert.That(staged.GetState(i), Is.EqualTo(initial[i]));
            Assert.That(staged.GetPreparedOwner(preview, 0).Value, Is.EqualTo("a"));
            Assert.That(staged.IsPreparedAdmissionGranted(preview, 1), Is.False);
            Assert.That(staged.GetPreparedSweep(preview, 1).TravelNanometers, Is.Zero);
            Assert.That(staged.GetPreparedState(preview, 0).Start.LocalNanometers, Is.EqualTo(910 * M));
            direct.Step(new SimulationTick(1), commands);
            // Caller mutation after preparation cannot rewrite accepted motion.
            commands[0] = default;
            staged.Commit(preview);
            for (int i = 0; i < 2; i++) Assert.That(staged.GetState(i), Is.EqualTo(direct.GetState(i)));
            Assert.That(log.Events, Is.EqualTo(directLog.Events));
            Assert.Catch(() => staged.Commit(preview));
        }

        [Test] public void ReplacementFailureAndForeignHandlesCannotCommitScratchState()
        {
            var geometry = Geometry(); var log = new Log();
            var initial = new[] { At(geometry.map, "a", "main_long", 890 * M) };
            var session = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, log);
            var other = new JunctionAdmissionSession(geometry.map, geometry.junctions, OccupancyTests.Definition(), initial, 20, new Log());
            var first = session.Prepare(new SimulationTick(1), new[] { Move(session, 0, 20 * M) });
            Assert.Catch(() => other.Commit(first)); Assert.Catch(() => session.Commit(default));
            var replacement = session.Prepare(new SimulationTick(1), initial);
            Assert.Catch(() => session.GetPreparedState(first, 0));
            Assert.Catch(() => session.Prepare(new SimulationTick(1), new PhysicalOccupancySweep[0]));
            Assert.Catch(() => session.Commit(replacement));
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(log.Events.Count, Is.Zero);
            session.Step(new SimulationTick(1), initial);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i < 1002; i++)
            {
                var preview = session.Prepare(new SimulationTick(i), initial);
                session.ValidatePreview(preview); session.GetPreparedState(preview, 0); session.Commit(preview);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
