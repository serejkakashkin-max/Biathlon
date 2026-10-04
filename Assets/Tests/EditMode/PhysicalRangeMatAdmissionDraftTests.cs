using System;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PhysicalRangeMatAdmissionDraftTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private static PhysicalRangeMatAdmissionMap Load(out PhysicalRangeFieldLayout layout, double stopDepth = 1)
        {
            var dir = PhysicalApronDraftFixtureFiles.Root();
            string Read(string id) => File.ReadAllText(Path.Combine(dir.FullName, "candidate/resources", id + ".json")); const string V = "Biathlon/Venues/woodland_basin_v1/";
            var track = TrackDefinition.FromJson(Read(V + "track")); var visual = TrackVisualDefinition.FromJson(Read(V + "visual"), track);
            var profile = CompetitionProfile.FromJson(Read(V + "mass_start_profile")); var j = PhysicalJunctionDefinition.FromJson(Read(V + "junctions"), track);
            var c = PhysicalConnectionDefinition.FromJson(Read(V + "connections"), j); var paths = PhysicalRangePathDefinition.FromJson(Read(V + "paths"), track, j, c);
            var route = new RaceRoute(track, RaceRoutePlan.FromJson(Read(V + "mass_start_route")), profile.ToRouteRequirements(), j, c);
            layout = new PhysicalRangeFieldLayout(new PhysicalTrackMap(track, route), track, route, paths);
            var body = OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            var mat = VenueRangeMatDefinition.FromJson("{\"schema_version\":1,\"id\":\"compact_physical_mat_v1\",\"width_m\":1.5,\"length_m\":2,\"thickness_m\":0.015,\"athlete_stop_depth_m\":" + stopDepth.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "}");
            return new PhysicalRangeMatAdmissionMap(track, route, profile, layout, visual, body, mat, 0, 1, .05);
        }
        private static RaceMotionSegment Segment(long from, long to, long before, long after, double start, double end, bool penalty = false)
            => (RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment), BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { from, to, before, after, start, end, penalty }, null);
        [Test] public void MatFootprintDerivesTheAllowedLineWithoutChangingCorridorOrBody()
        {
            var gate = Load(out _);
            for (int path = 0; path < 30; path++)
            {
                Assert.That(gate.ContainsBody(path, 0), Is.True); Assert.That(gate.ContainsBody(path, .30), Is.True); Assert.That(gate.ContainsBody(path, -.30), Is.True);
                Assert.That(gate.ContainsBody(path, .31), Is.False); Assert.That(gate.ContainsBody(path, -.31), Is.False);
                Assert.That(gate.ContainsBody(path, .85), Is.False); Assert.That(gate.ContainsBody(path, -.85), Is.False);
            }
            Assert.That(gate.ContextJson, Does.Contain("no_line_clamp_snap_or_input_automation"));
        }
        [Test] public void OffMatArrivalKeepsPaidForwardPrefixBeforeTheAuthoritativeStop()
        {
            var gate = Load(out _); long stop = gate.FiringRouteNanometers(0, 0), initial = stop - 2000000;
            var segments = new[] { Segment(initial, stop, 50 * M, 50 * M + 2000000, 0, .05) };
            var origin = new RaceMotionPoint(initial, 50 * M);
            Assert.That(gate.MaximumAcceptedTravel(0, 0, origin, segments, .85, .85, true), Is.EqualTo(1000000));
            Assert.That(gate.MaximumAcceptedTravel(0, 0, origin, segments, 0, 0, true), Is.EqualTo(2000000));
            Assert.That(gate.MaximumAcceptedTravel(0, 0, origin, segments, .85, .85, false), Is.EqualTo(2000000));
        }
        [Test] public void FractionalArrivalAndFinalLineBothMustBeOnTheMat()
        {
            var gate = Load(out _); long stop = gate.FiringRouteNanometers(0, 0), from = stop - 2000000; var initial = new RaceMotionPoint(from, 0);
            var early = new[] { Segment(from, stop, 0, 2000000, 0, .025) };
            Assert.That(gate.MaximumAcceptedTravel(0, 0, initial, early, .85, 0, true), Is.EqualTo(1000000), "The body is still off-mat at half-tick arrival.");
            Assert.That(gate.MaximumAcceptedTravel(0, 0, initial, early, 0, .85, true), Is.EqualTo(1000000), "After-arrival lateral motion cannot take a shooting body off-mat.");
            var end = new[] { Segment(from, stop, 0, 2000000, 0, .05) };
            Assert.That(gate.MaximumAcceptedTravel(0, 0, initial, end, .85, 0, true), Is.EqualTo(2000000));
        }
        [Test] public void EveryAcceptedDistanceIncludingAnEarlierPenaltyReceiptRemainsCounted()
        {
            var gate = Load(out _); long stop = gate.FiringRouteNanometers(0, 0), from = stop - 2000000;
            var receipts = new[] { Segment(from, from, 0, 100000000, 0, .01, true), Segment(from, stop, 100000000, 102000000, .01, .05) };
            Assert.That(gate.MaximumAcceptedTravel(0, 0, new RaceMotionPoint(from, 0), receipts, .85, .85, true), Is.EqualTo(101000000));
        }
        [Test] public void MalformedIncompleteOrAlreadyPublishedOffMatArrivalCannotAuthorize()
        {
            var gate = Load(out _); long stop = gate.FiringRouteNanometers(0, 0), from = stop - 2000000; var origin = new RaceMotionPoint(from, 0);
            Assert.That(() => gate.MaximumAcceptedTravel(0, 0, origin, new[] { Segment(from, stop - 1, 0, 1999999, 0, .05) }, 0, 0, true), Throws.ArgumentException);
            Assert.That(() => gate.MaximumAcceptedTravel(0, 0, new RaceMotionPoint(stop, 0), Array.Empty<RaceMotionSegment>(), .85, .85, true), Throws.ArgumentException);
            Assert.That(() => gate.MaximumAcceptedTravel(0, 0, origin, new[] { Segment(from, stop, 0, 2000000, 0, .05) }, double.NaN, 0, true), Throws.Exception);
        }
        [Test] public void MatAdmissionHotChecksDoNotAllocateOrModifyTheAcceptedLine()
        {
            var gate = Load(out _); long stop = gate.FiringRouteNanometers(0, 0), from = stop - 2000000;
            var segments = new[] { Segment(from, stop, 0, 2000000, 0, .05) }; var initial = new RaceMotionPoint(from, 0); double line = .85;
            for (int i = 0; i < 50; i++) gate.MaximumAcceptedTravel(0, 0, initial, segments, line, line, true);
            long before = GC.GetAllocatedBytesForCurrentThread(), cap = 0;
            for (int i = 0; i < 1000; i++) cap = gate.MaximumAcceptedTravel(0, 0, initial, segments, line, line, true);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(cap, Is.EqualTo(1000000)); Assert.That(line, Is.EqualTo(.85));
        }
        [Test] public void AsymmetricMatDatumUsesTheActualAcceptedWorldCenterSign()
        {
            var gate = Load(out _, .9);
            for (int path = 0; path < 30; path++)
            {
                // This frozen native stop tangent points toward -X, so positive visible
                // line shifts toward +Z; no input/presentation sign is reversed here.
                // The authored mat center is .1m rearward of the athlete stop.
                Assert.That(gate.ContainsBody(path, .35), Is.False);
                Assert.That(gate.ContainsBody(path, -.35), Is.True);
            }
            Assert.That(gate.ContextJson, Does.Contain("actual_path_right_normal_center_plus_fz_minus_fx"));
        }
    }
}
