using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalConnectionTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private static (PhysicalTrackMap map, PhysicalJunctionDefinition junctions, PhysicalConnectionDefinition connections) Geometry()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            var map = new PhysicalTrackMap(track, new RaceRoute(track,
                RaceRoutePlan.TimeTrial("connections", track.Id, "main_long", requirements), requirements));
            var junctions = PhysicalJunctionDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                "tests/fixtures/tracks/synthetic_flat_junctions_v1.json"), track);
            return (map, junctions, PhysicalConnectionDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                "tests/fixtures/tracks/synthetic_flat_connections_v1.json"), junctions));
        }
        [Test] public void ExplicitDirectionAndExactAnchorAreRequiredAndSnapshotsBind()
        {
            var g = Geometry(); var map = new PhysicalConnectionMap(g.map, g.junctions, g.connections);
            var entry = map.Transition("penalty_enter", g.map.MapRoute(900 * M), 123 * M);
            Assert.That(entry.Incoming.LoopId, Is.EqualTo("main_long"));
            Assert.That(entry.Outgoing.LoopId, Is.EqualTo("penalty_150"));
            Assert.That(entry.Outgoing.LocalNanometers, Is.Zero); Assert.That(entry.TravelNanometers, Is.EqualTo(123 * M));
            Assert.Catch(() => map.Transition("penalty_enter", entry.Outgoing, 123 * M));
            Assert.Catch(() => map.Transition("penalty_enter", g.map.MapRoute(900 * M - 1), 0));
            Assert.Catch(() => map.Transition("penalty_exit", g.map.MapRoute(920 * M), 0));
            Assert.Catch(() => map.Transition("unknown", entry.Incoming, 0));
            Assert.Catch(() => map.Transition("penalty_enter", Geometry().map.MapRoute(900 * M), 0));
            Assert.Catch(() => map.Transition("penalty_enter", entry.Incoming, -1));
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var changed = PhysicalJunctionDefinition.FromJson(g.junctions.ToJson().Replace("\"guard_after_m\":22", "\"guard_after_m\":23"), track);
            Assert.That(changed.ToJson(), Is.Not.EqualTo(g.junctions.ToJson()));
            Assert.Catch(() => new PhysicalConnectionMap(g.map, changed, g.connections));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) map.Transition("penalty_enter", entry.Incoming, 123 * M);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void SchemaRejectsImplicitOrAmbiguousConnectivity()
        {
            var g = Geometry(); string json = g.connections.ToJson();
            Assert.That(PhysicalConnectionDefinition.FromJson(json, g.junctions).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => PhysicalConnectionDefinition.FromJson(json.Replace("coincident_seam", "finite_connector"), g.junctions));
            Assert.Catch(() => PhysicalConnectionDefinition.FromJson(json.Replace("penalty_exit", "penalty_enter"), g.junctions));
            Assert.Catch(() => PhysicalConnectionDefinition.FromJson(json.Replace("penalty_access", "unknown"), g.junctions));
            Assert.Catch(() => PhysicalConnectionDefinition.FromJson(json.Replace("penalty_seam", "main_rejoin"), g.junctions));
            Assert.Catch(() => PhysicalConnectionDefinition.FromJson(json.Replace("synthetic_flat_junctions_v1", "wrong"), g.junctions));
        }
        [TestCase(1)] [TestCase(3)] [TestCase(5)]
        public void ActualPenaltyTraversalRetainsEveryCircuitAndEntryToRejoinDistance(int misses)
        {
            var g = Geometry(); var map = new PhysicalConnectionMap(g.map, g.junctions, g.connections);
            var traversal = RangePenaltyTraversalTests.Create(new RaceEventLog(100));
            traversal.Step(new SimulationTick(1), 30 * M);
            int mask = (1 << (5 - misses)) - 1;
            var completed = new ShotResolutionFrame(new ShootingFrame(new SimulationTick(2), ShootingPhase.Complete, 5, false, 0),
                default, default, ShotOutcome.NoShot, mask);
            traversal.Step(new SimulationTick(2), 0, completed);
            traversal.Step(new SimulationTick(3), 50 * M);
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
            var entering = map.Transition("penalty_enter", g.map.MapRoute(traversal.RouteNanometers), traversal.TravelledNanometers);
            Assert.That(entering.Outgoing, Is.EqualTo(g.map.MapPenalty(traversal.PenaltyNanometers)));
            for (int i = 0; i < misses; i++) traversal.Step(new SimulationTick(4 + i), 150 * M);
            var leaving = map.Transition("penalty_exit", g.map.MapPenalty(traversal.PenaltyNanometers), traversal.TravelledNanometers);
            Assert.That(leaving.Outgoing, Is.EqualTo(g.map.MapRoute(traversal.RouteNanometers)));
            Assert.That(leaving.TravelNanometers - entering.TravelNanometers, Is.EqualTo(misses * 150 * M));
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.Rejoin));
            traversal.Step(new SimulationTick(4 + misses), 20 * M);
            Assert.That(traversal.Phase, Is.EqualTo(RangeTraversalPhase.Complete));
            Assert.That(traversal.TravelledNanometers, Is.EqualTo((100 + misses * 150) * M));
            Assert.That(traversal.RouteNanometers, Is.EqualTo(920 * M));
        }

        [Test] public void IncomingTailShrinksByActualTravelAndClosedCircuitsNeverRestoreIt()
        {
            var g = Geometry(); var system = new PhysicalTransitionFootprints(g.map, g.junctions, g.connections, OccupancyTests.Definition());
            long half = (long)Math.Ceiling(OccupancyTests.Definition().LongitudinalSeparation * M / 2);
            var state = system.Begin("penalty_exit", g.map.MapPenalty(0), 300 * M);
            Assert.That(state.HasIncomingTail, Is.True); Assert.That(state.IncomingTailNanometers, Is.EqualTo(half));
            Assert.That(state.Current, Is.EqualTo(g.map.MapRoute(900 * M)));
            var stopped = system.Advance(state, 0); Assert.That(stopped, Is.EqualTo(state));
            var boundary = system.Advance(state, half);
            Assert.That(boundary.HasIncomingTail, Is.True); Assert.That(boundary.IncomingTailNanometers, Is.Zero);
            var clear = system.Advance(boundary, 1);
            Assert.That(clear.HasIncomingTail, Is.False);
            Assert.That(clear.TotalTravelNanometers, Is.EqualTo(300 * M + half + 1));
            Assert.That(state.IncomingTailNanometers, Is.EqualTo(half), "Preview values are immutable.");
            var loop = system.Advance(clear, 1000 * M);
            Assert.That(loop.Current, Is.EqualTo(clear.Current)); Assert.That(loop.HasIncomingTail, Is.False);
            Assert.That(loop.TotalTravelNanometers, Is.EqualTo(clear.TotalTravelNanometers + 1000 * M));
            Assert.That(system.TouchesJunction(0, clear), Is.True, "Outgoing body still holds the shared region after incoming tail clears.");
            var beyond = system.Advance(state, 23 * M);
            Assert.That(system.TouchesJunction(0, beyond), Is.False);
            Assert.That(system.TouchesJunctionDuringAdvance(0, state, 23 * M), Is.True);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { system.Advance(state, half); system.TouchesJunctionDuringAdvance(0, state, 23 * M); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void TailRetainsOtherIncomingJunctionsUntilTheirOwnGeometryClears()
        {
            var g = Geometry();
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            string json = g.junctions.ToJson(); int end = json.LastIndexOf(']');
            string extra = ",{\"id\":\"upstream\",\"ports\":[" +
                "{\"id\":\"upstream_main\",\"anchor\":{\"loop_id\":\"main_long\",\"distance_m\":899.5},\"guard_before_m\":0.05,\"guard_after_m\":0.05}," +
                "{\"id\":\"upstream_other\",\"anchor\":{\"loop_id\":\"main_short\",\"distance_m\":500},\"guard_before_m\":0.05,\"guard_after_m\":0.05}]}";
            var junctions = PhysicalJunctionDefinition.FromJson(json.Insert(end, extra), track);
            var connections = PhysicalConnectionDefinition.FromJson(g.connections.ToJson(), junctions);
            var system = new PhysicalTransitionFootprints(g.map, junctions, connections, OccupancyTests.Definition());
            var state = system.Begin("penalty_enter", g.map.MapRoute(900 * M), 0);
            Assert.That(system.TouchesJunction(1, state), Is.True, "Old branch remains occupied although the centre changed loops.");
            long half = state.IncomingTailNanometers;
            var touching = system.Advance(state, half - 450000000);
            Assert.That(system.TouchesJunction(1, touching), Is.True);
            Assert.That(system.TouchesJunction(1, system.Advance(touching, 1)), Is.False);
            Assert.That(system.TouchesJunctionDuringAdvance(1, state, M), Is.True);
        }

        [Test] public void InvalidFootprintAdvancesCannotEraseOrReplaceRetainedState()
        {
            var g = Geometry(); var system = new PhysicalTransitionFootprints(g.map, g.junctions, g.connections, OccupancyTests.Definition());
            var state = system.Begin("penalty_enter", g.map.MapRoute(900 * M), long.MaxValue);
            Assert.Catch(() => system.Advance(state, 1)); Assert.Catch(() => system.Advance(state, -1));
            Assert.Catch(() => system.Advance(state, 150 * M + 1));
            Assert.Catch(() => system.Advance(default, 0));
            var other = new PhysicalTransitionFootprints(g.map, g.junctions, g.connections, OccupancyTests.Definition());
            Assert.Catch(() => other.TouchesJunction(0, state));
            Assert.Catch(() => system.TouchesJunctionDuringAdvance(0, state, -1));
            Assert.That(state.TotalTravelNanometers, Is.EqualTo(long.MaxValue)); Assert.That(state.HasIncomingTail, Is.True);
        }
    }
}
