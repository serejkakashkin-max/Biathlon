using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathCapacityTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string Slice(string loop, int start, int end) =>
            "{\"loop_id\":\"" + loop + "\",\"start_m\":" + start + ",\"end_m\":" + end + "}";
        private static string DenseLoop(string name)
        {
            var json = new StringBuilder("{\"id\":\"" + name + "\",\"kind\":\"race\",\"closed\":false,\"segments\":[");
            for (int i = 0; i < 1000; i++)
            {
                if (i > 0) json.Append(',');
                json.Append("{\"id\":\"millimetre_").Append(i).Append("\",\"length_m\":0.001,\"start_grade\":0,\"end_grade\":0,\"curvature_per_m\":0,\"width_m\":2,\"snow_profile_id\":\"packed\",\"wind_exposure\":1,\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false,\"risk_level\":0,\"semantic_zone\":\"range_approach\"}");
            }
            return json.Append("]}").ToString();
        }
        private static string AppendArrayBefore(string json, string followingProperty, string additions) =>
            new Regex("\\]\\s*,\\s*\"" + followingProperty + "\"").Replace(json, "," + additions + "],\"" + followingProperty + "\"", 1);
        private static void AddSeam(List<string> junctions, List<string> connections, string id,
            string fromLoop, int from, bool fromEnd, string toLoop, int to)
        {
            string a = id + "_from", b = id + "_to";
            string Port(string port, string loop, int at, bool end) =>
                "{\"id\":\"" + port + "\",\"anchor\":{\"loop_id\":\"" + loop + "\",\"distance_m\":" + at +
                "},\"guard_before_m\":" + (at == 0 ? "0" : "0.001") + ",\"guard_after_m\":" + (end ? "0" : "0.001") + "}";
            junctions.Add("{\"id\":\"" + id + "\",\"ports\":[" + Port(a, fromLoop, from, fromEnd) + "," + Port(b, toLoop, to, false) + "]}");
            connections.Add("{\"id\":\"" + id + "\",\"junction_id\":\"" + id + "\",\"from_port_id\":\"" + a + "\",\"to_port_id\":\"" + b + "\",\"kind\":\"coincident_seam\"}");
        }
        private static SprintAthleteSession Setup(RaceEventLog events, out TrackDefinition track, out RaceRoute route,
            out PhysicalRangePathMap path, out PhysicalRangePathDefinition definition, out SkiModelDefinitions model, bool usePath = true)
        {
            // This is a contract/preview stress fixture, not admissible production world geometry.
            string trackJson = AppendArrayBefore(PhysicalRangePathDefinitionTests.Fixture("track"), "checkpoints", DenseLoop("repeat_a") + "," + DenseLoop("repeat_b"));
            track = TrackDefinition.FromJson(trackJson);
            var extraJunctions = new List<string>(); var extraConnections = new List<string>();
            AddSeam(extraJunctions, extraConnections, "dense_main_a", "main", 10, false, "repeat_a", 0);
            AddSeam(extraJunctions, extraConnections, "dense_a_b", "repeat_a", 1, true, "repeat_b", 0);
            AddSeam(extraJunctions, extraConnections, "dense_b_a", "repeat_b", 1, true, "repeat_a", 0);
            AddSeam(extraJunctions, extraConnections, "dense_b_mat", "repeat_b", 1, true, "mat_1", 0);
            AddSeam(extraJunctions, extraConnections, "dense_mat_exit", "mat_1", 40, true, "exit_aisle", 36);
            var junctions = PhysicalJunctionDefinition.FromJson(AppendArrayBefore(PhysicalRangePathDefinitionTests.Fixture("junctions"), "fixture_scope", string.Join(",", extraJunctions)), track);
            var connections = PhysicalConnectionDefinition.FromJson(AppendArrayBefore(PhysicalRangePathDefinitionTests.Fixture("connections"), "fixture_scope", string.Join(",", extraConnections)), junctions);
            var slices = new List<string> { Slice("main", 0, 10) };
            for (int i = 0; i < 13; i++) { slices.Add(Slice("repeat_a", 0, 1)); slices.Add(Slice("repeat_b", 0, 1)); }
            slices.Add(Slice("mat_1", 0, 40)); slices.Add(Slice("exit_aisle", 36, 60));
            string pathsJson = "{\"schema_version\":1,\"id\":\"dense_repeat_contract\",\"track_id\":\"range_path_contract_track_v1\",\"main_loop_id\":\"main\",\"entry_distance_m\":0,\"exit_distance_m\":100,\"lanes\":[{\"id\":\"lane_1\",\"mat_anchor\":{\"loop_id\":\"mat_1\",\"distance_m\":18},\"slices\":[" + string.Join(",", slices) + "]}]}";
            definition = PhysicalRangePathDefinition.FromJson(pathsJson, track, junctions, connections);
            var profile = PhysicalRangePathTraversalTests.Profile(); var requirements = profile.ToRouteRequirements();
            route = new RaceRoute(track, RaceRoutePlan.TimeTrial("dense_repeat_route", track.Id, "main", requirements), requirements);
            path = new PhysicalRangePathMap(new PhysicalTrackMap(track, route), track, route, definition, 0);
            model = new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson); var ski = TimeTrialTests.Athlete();
            var shooting = ShootingSessionTests.Athlete(); shooting = ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id, ski.Id));
            return new SprintAthleteSession(new SimulationConfig(1, "dense_repeat_sim", 20, 123), profile, model, ShootingSessionTests.Model(), ski, shooting,
                track, route, new CheckpointLayout(track, route), RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                "ski_neutral", new SimulationTick(1), new ShootingLaneAssignment(track.Range.Id, path.LaneId, path.LaneOrdinal), events,
                physicalRangePath: usePath ? path : null);
        }
        private static RaceMotionSegment[] Buffer(SprintAthleteSession session) =>
            (RaceMotionSegment[])typeof(SprintAthleteSession).GetField("segments", PrivateInstance).GetValue(session);
        private static int TerrainIntervals(PhysicalRangePathMap path) =>
            (int)typeof(PhysicalRangePathMap).GetProperty("TerrainIntervalCount", PrivateInstance).GetValue(path);
        private static int TrackSegments(TrackDefinition track)
        { int count = 0; for (int i = 0; i < track.LoopCount; i++) count = checked(count + track.GetLoop(i).SegmentCount); return count; }

        [Test] public void RepeatedDenseTerrainUsesOwnedSessionCapacityAndPurePreviewExceedsTheOldBound()
        {
            var events = new RaceEventLog(20);
            var session = Setup(events, out var track, out var route, out var path, out var definition, out var model);
            Assert.That(definition.GetLane(0).SliceCount, Is.EqualTo(29)); Assert.That(definition.GetLane(0).Length.Millimeters, Is.EqualTo(100000));
            Assert.That(path.FiringRouteNanometers(0), Is.EqualTo(54 * M));
            Assert.That(TerrainIntervals(path), Is.EqualTo(26004), "Repeated occurrences of dense physical loops each contribute their own terrain boundaries.");
            int oldCapacity = checked((route.SectionCount + 5 + 1) * (TrackSegments(track) + 8 + definition.GetLane(0).SliceCount));
            var buffer = Buffer(session); Assert.That(buffer.Length, Is.GreaterThan(oldCapacity));
            var sessionTraversal = ((RangePenaltyTraversal[])typeof(SprintAthleteSession).GetField("ranges", PrivateInstance).GetValue(session))[0];
            // The complete shooting composition requires a higher sampling frequency. Buffer sizing is frequency-independent;
            // a standalone legal 1 Hz motion/traversal preview exercises the full movement envelope with the owned buffer.
            var traversal = new RangePenaltyTraversal(PhysicalRangePathTraversalTests.Profile(), track, route, 0, session.AthleteId, 1, events, rangePath: path);
            var initialTraversal = traversal.LastFrame; var initialAthlete = session.LastFrame; var initialPhysiology = session.PhysiologyState;
            var initialSessionTraversal = sessionTraversal.LastFrame;
            var motion = new RacePathMotion(model, TimeTrialTests.Athlete(), track, route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")), "ski_neutral", 1);
            var input = new TimeTrialInput(new SimulationInput(new SimulationTick(1), session.AthleteId, 800), 1, 1, 0);
            var initial = new SkiState(5 * M, model.Physics.MaximumSpeedMetersPerSecond);
            var error = Assert.Throws<ArgumentException>(() => motion.Preview(initial, 5 * M, 100 * M, traversal, 350, 0, input,
                new RaceMotionSegment[oldCapacity], physicalRangePath: path));
            Assert.That(error.Message, Does.Contain("output capacity is insufficient"));
            var frame = motion.Preview(initial, 5 * M, 100 * M, traversal, 350, 0, input, buffer, physicalRangePath: path);
            Assert.That(frame.SegmentCount, Is.GreaterThan(oldCapacity)); Assert.That(frame.SegmentCount, Is.GreaterThanOrEqualTo(26000));
            Assert.That(frame.RouteNanometers, Is.GreaterThan(36 * M));
            long before = GC.GetAllocatedBytesForCurrentThread();
            var repeated = motion.Preview(initial, 5 * M, 100 * M, traversal, 350, 0, input, buffer, physicalRangePath: path);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(repeated, Is.EqualTo(frame)); Assert.That(allocated, Is.Zero);
            Assert.That(session.Tick.Value, Is.Zero); Assert.That(session.LastFrame, Is.EqualTo(initialAthlete));
            Assert.That(session.PhysiologyState, Is.EqualTo(initialPhysiology)); Assert.That(traversal.LastFrame, Is.EqualTo(initialTraversal));
            Assert.That(sessionTraversal.LastFrame, Is.EqualTo(initialSessionTraversal));
            Assert.That(traversal.TravelledNanometers, Is.Zero); Assert.That(events.Count, Is.Zero);
            TestContext.WriteLine("dense_path_slices=" + definition.GetLane(0).SliceCount + ";terrain_intervals=" + TerrainIntervals(path) +
                ";legacy_capacity=" + oldCapacity + ";owned_capacity=" + buffer.Length + ";preview_receipts=" + frame.SegmentCount);
        }
        [Test] public void NullPathRetainsTheOriginalConstructorCapacity()
        {
            var session = Setup(new RaceEventLog(20), out var track, out var route, out _, out _, out _, false);
            Assert.That(Buffer(session).Length, Is.EqualTo(checked((route.SectionCount + 5 + 1) * (TrackSegments(track) + 8))));
        }
        [Test] public void ColdOverlapCountIncludesOnlyPositiveTerrainIntersections()
        {
            PhysicalRangePathMapTests.Setup(out _, out _, out _, out var first, out var second);
            Assert.That(TerrainIntervals(first), Is.EqualTo(4)); Assert.That(TerrainIntervals(second), Is.EqualTo(4));
        }
    }
}
