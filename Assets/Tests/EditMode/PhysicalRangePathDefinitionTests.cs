using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathDefinitionTests
    {
        public static string Fixture(string name) => PhysiologyContractTests.ProjectFile("tests/fixtures/venues/range_path_contract_v1/" + name + ".json");
        public static PhysicalRangePathDefinition Create(out TrackDefinition track, out PhysicalJunctionDefinition junctions,
            out PhysicalConnectionDefinition connections, string paths = null, string junctionJson = null, string connectionJson = null)
        {
            track = TrackDefinition.FromJson(Fixture("track"));
            junctions = PhysicalJunctionDefinition.FromJson(junctionJson ?? Fixture("junctions"), track);
            connections = PhysicalConnectionDefinition.FromJson(connectionJson ?? Fixture("connections"), junctions);
            return PhysicalRangePathDefinition.FromJson(paths ?? Fixture("paths"), track, junctions, connections);
        }
        private static string Slice(string loop, string start, string end) =>
            "{\"loop_id\":\"" + loop + "\",\"start_m\":" + start + ",\"end_m\":" + end + "}";
        private static string Lane(string id, string mat, string fork, string matDistance = "18", string slices = null) =>
            "{\"id\":\"" + id + "\",\"mat_anchor\":{\"loop_id\":\"" + mat + "\",\"distance_m\":" + matDistance +
            "},\"slices\":[" + (slices ?? string.Join(",", Slice("main", "0", fork), Slice(mat, "0", "40"), Slice("exit_aisle", fork, "60"))) + "]}";
        private static string Paths(params string[] lanes) =>
            "{\"schema_version\":1,\"id\":\"test_paths\",\"track_id\":\"range_path_contract_track_v1\",\"main_loop_id\":\"main\",\"entry_distance_m\":0,\"exit_distance_m\":100,\"lanes\":[" + string.Join(",", lanes) + "]}";
        private static PhysicalRangePathDefinition Read(string json) => Create(out _, out _, out _, json);
        private static string ChangeExitMerge(string junctionJson, string oldDistance, string newDistance) =>
            Regex.Replace(junctionJson, "(\"loop_id\"\\s*:\\s*\"exit_aisle\"\\s*,\\s*\"distance_m\"\\s*:\\s*)" + oldDistance + "(?=\\s*[,}])", "${1}" + newDistance);

        [Test] public void SharedSlicesPreserveIdentityExactTravelAndDistinctDerivedFiringCoordinates()
        {
            var definition = Create(out var track, out var junctions, out var connections);
            Assert.That(definition.Count, Is.EqualTo(2));
            Assert.That(definition.Entry.Millimeters, Is.Zero); Assert.That(definition.Exit.Millimeters, Is.EqualTo(100000));
            Assert.That(definition.TrackSnapshotJson, Is.EqualTo(track.ToJson()));
            Assert.That(definition.JunctionSnapshotJson, Is.EqualTo(junctions.ToJson()));
            Assert.That(definition.ConnectionSnapshotJson, Is.EqualTo(connections.ToJson()));
            for (int i = 0; i < 2; i++)
            {
                var lane = definition.GetLane(i);
                Assert.That(lane.Id, Is.EqualTo("lane_" + (i + 1))); Assert.That(lane.Length.Millimeters, Is.EqualTo(100000));
                Assert.That(lane.GetSlice(0).LoopId, Is.EqualTo("main")); Assert.That(lane.GetSlice(2).LoopId, Is.EqualTo("exit_aisle"));
                Assert.That(lane.GetSlice(0).Offset.Millimeters, Is.Zero);
                Assert.That(lane.FiringSliceIndex, Is.EqualTo(1));
                Assert.That(lane.FiringOffset.Millimeters, Is.EqualTo(i == 0 ? 28000 : 48000));
                Assert.That(lane.Fork.Millimeters, Is.EqualTo(i == 0 ? 10000 : 30000));
            }
            var matPhysics = track.GetLoop(definition.GetLane(0).MatAnchor.LoopId).GetSegment(0);
            Assert.That(matPhysics.CurvaturePerMeter, Is.EqualTo(.08)); Assert.That(matPhysics.WidthMeters, Is.EqualTo(2));
            Assert.That(matPhysics.CurvaturePerMeter, Is.Not.EqualTo(track.GetLoop("main").GetSegment(0).CurvaturePerMeter));
            Assert.That(definition.ToJson(), Does.Not.Contain("firing_offset"), "Firing offset is derived, not duplicated authoring data.");
        }
        [Test] public void AuthoredLaneOrderAndCanonicalRoundTripAreCultureIndependentAndDomainOnly()
        {
            var definition = Read(Paths(Lane("last_named_first", "mat_2", "30"), Lane("first_named_second", "mat_1", "10")));
            Assert.That(definition.GetLane(0).Id, Is.EqualTo("last_named_first"));
            string canonical = definition.ToJson(); var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
                Assert.That(Read(canonical).ToJson(), Is.EqualTo(canonical));
            }
            finally { CultureInfo.CurrentCulture = old; }
            foreach (var reference in typeof(PhysicalRangePathDefinition).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name.StartsWith("Unity", StringComparison.Ordinal), Is.False);
        }
        [Test] public void SameLoopSlicesRequireExactAdjacencyAndMatMustRemainInsideOneSlice()
        {
            string slices = string.Join(",", Slice("main", "0", "10"), Slice("mat_1", "0", "20"), Slice("mat_1", "20", "40"), Slice("exit_aisle", "10", "60"));
            var definition = Read(Paths(Lane("lane_1", "mat_1", "10", slices: slices)));
            Assert.That(definition.GetLane(0).SliceCount, Is.EqualTo(4));
            Assert.That(definition.GetLane(0).GetSlice(2).Offset.Millimeters, Is.EqualTo(30000));
            Assert.That(definition.GetLane(0).FiringOffset.Millimeters, Is.EqualTo(28000));
            Assert.Catch(() => Read(Paths(Lane("lane_1", "mat_1", "10", slices: slices.Replace("\"start_m\":20", "\"start_m\":21")))));
            Assert.Catch(() => Read(Paths(Lane("lane_1", "mat_1", "10", "20", slices))));
        }
        [Test] public void PhysicalLengthCannotBeShortenedOrLengthenedBehindUnchangedMainProgress()
        {
            foreach (string merge in new[] { "9", "11" })
            {
                string changed = Paths(Lane("lane_1", "mat_1", "10", slices: string.Join(",", Slice("main", "0", "10"), Slice("mat_1", "0", "40"), Slice("exit_aisle", merge, "60"))));
                string junctions = ChangeExitMerge(Fixture("junctions"), "10", merge);
                var error = Assert.Throws<ArgumentException>(() => Create(out _, out _, out _, changed, junctions));
                Assert.That(error.Message, Does.Contain("exactly equal"));
            }
        }
        [TestCase("0")][TestCase("40")][TestCase("41")][TestCase("18.0001")]
        public void InvalidMatPositionAndSubMillimetreCoordinateFail(string mat)
        { Assert.Catch(() => Read(Paths(Lane("lane_1", "mat_1", "10", mat)))); }
        [Test] public void UnknownLoopsReverseZeroOutsideAndAmbiguousMatSlicesFail()
        {
            foreach (string slices in new[] {
                string.Join(",", Slice("main", "0", "10"), Slice("missing", "0", "40"), Slice("exit_aisle", "10", "60")),
                string.Join(",", Slice("main", "0", "10"), Slice("mat_1", "40", "0"), Slice("exit_aisle", "10", "60")),
                string.Join(",", Slice("main", "0", "10"), Slice("mat_1", "0", "0"), Slice("exit_aisle", "10", "60")),
                string.Join(",", Slice("main", "0", "10"), Slice("mat_1", "0", "41"), Slice("exit_aisle", "10", "60")),
                string.Join(",", Slice("main", "1", "10"), Slice("mat_1", "0", "40"), Slice("exit_aisle", "10", "60")),
                string.Join(",", Slice("main", "0", "5"), Slice("main", "5", "10"), Slice("mat_1", "0", "40"), Slice("exit_aisle", "10", "60")) })
                Assert.Catch(() => Read(Paths(Lane("lane_1", "mat_1", "10", slices: slices))));
            Assert.Catch(() => Read(Paths(Lane("lane_1", "missing", "10"))));
            Assert.Catch(() => Read(Paths(Lane("lane_1", "main", "10"))));
        }
        [Test] public void MissingReversedAndMisplacedDirectedConnectionsFailIncludingFinalRejoin()
        {
            foreach (string changed in new[] {
                Fixture("connections").Replace("\"connections\": [", "\"connections\": [] ,\"ignored_connections\": ["),
                Fixture("connections").Replace("\"from_port_id\": \"enter_1_from\"", "\"from_port_id\": \"enter_1_to\"").Replace("\"to_port_id\": \"enter_1_to\"", "\"to_port_id\": \"enter_1_from\""),
                Fixture("connections").Replace("\"from_port_id\": \"return_main_from\"", "\"from_port_id\": \"return_main_to\"").Replace("\"to_port_id\": \"return_main_to\"", "\"to_port_id\": \"return_main_from\"") })
                Assert.Catch(() => Create(out _, out _, out _, connectionJson: changed));
            Assert.Catch(() => Create(out _, out _, out _, junctionJson: ChangeExitMerge(Fixture("junctions"), "10", "11")));
        }
        [Test] public void ExactSnapshotsAreMandatoryEvenWhenIdsMatch()
        {
            Create(out var track, out var junctions, out var connections);
            var changedTrack = TrackDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(track.ToJson(), "width_m", "7"));
            Assert.Catch(() => PhysicalRangePathDefinition.FromJson(Fixture("paths"), changedTrack, junctions, connections));
            string changedJunctionJson = new Regex("\"guard_after_m\":1(?=,)").Replace(junctions.ToJson(), "\"guard_after_m\":2", 1);
            var changedJunctions = PhysicalJunctionDefinition.FromJson(changedJunctionJson, track);
            Assert.Catch(() => PhysicalRangePathDefinition.FromJson(Fixture("paths"), track, changedJunctions, connections));
            Assert.Throws<ArgumentNullException>(() => PhysicalRangePathDefinition.FromJson(Fixture("paths"), track, null, connections));
            Assert.Throws<ArgumentNullException>(() => PhysicalRangePathDefinition.FromJson(Fixture("paths"), track, junctions, null));
        }
        [Test] public void InvalidSchemaIdentitiesNullsAndDuplicateLaneOrMatAssignmentsFail()
        {
            foreach (string json in new[] {
                Fixture("paths").Replace("\"schema_version\": 1", "\"schema_version\": 2"),
                Fixture("paths").Replace("range_path_contract_track_v1", "another_track"),
                Paths(), Paths("null"), Paths(Lane("lane_1", "mat_1", "10"), Lane("lane_1", "mat_2", "30")),
                Paths(Lane("lane_1", "mat_1", "10"), Lane("lane_2", "mat_1", "10")),
                Paths(Lane(" ", "mat_1", "10")), Paths(Lane("lane_1", "mat_1", "10", slices: "null,null")),
                Paths(Lane("lane_1", "mat_1", "10")).Replace("\"mat_anchor\":", "\"unused_anchor\":") })
                Assert.Catch(() => Read(json));
            Assert.Catch(() => Read(PhysiologyContractTests.ReplaceNumber(Fixture("paths"), "entry_distance_m", "6")));
            Assert.Catch(() => Read(PhysiologyContractTests.ReplaceNumber(Fixture("paths"), "exit_distance_m", "101")));
        }
        [Test] public void LaneAndSliceCapacityIsRejectedBeforeUnboundedContractTraversal()
        {
            string excessiveLanes = Paths(Enumerable.Range(0, PhysicalRangePathDefinition.MaximumLanes + 1)
                .Select(i => Lane("lane_" + i, "mat_1", "10")).ToArray());
            var laneError = Assert.Catch(() => Read(excessiveLanes)); Assert.That(laneError.Message, Does.Contain("at most 60"));
            string excessiveSlices = string.Join(",", Enumerable.Repeat("null", PhysicalRangePathDefinition.MaximumSlicesPerLane + 1));
            var sliceError = Assert.Catch(() => Read(Paths(Lane("lane_1", "mat_1", "10", slices: excessiveSlices))));
            Assert.That(sliceError.Message, Does.Contain("2..32"));
        }
    }
}
