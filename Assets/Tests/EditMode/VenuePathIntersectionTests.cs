using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenuePathIntersectionTests
    {
        [Test] public void FlatFigureEightReportsOneStableCrossing()
        {
            var loop = Loop(true, P(-10, 0, -10), P(10, 0, 10), P(-10, 0, 10), P(10, 0, -10), P(-10, 0, -10));
            Assert.That(Find(loop), Is.EqualTo(new[] { "loop.self_intersection|loops/main_short/edges/0/2|Nonadjacent centreline edges intersect with vertical separation 0 m." }));
        }

        [Test] public void ClosedSquareAllowsAdjacentEdgesAndClosure()
        {
            Assert.That(Find(Loop(true, P(0, 0, 0), P(10, 0, 0), P(10, 0, 10), P(0, 0, 10), P(0, 0, 0))), Is.Empty);
        }

        [Test] public void NonadjacentEndpointTouchIsReported()
        {
            Assert.That(Find(Loop(false, P(0, 0, 0), P(10, 0, 0), P(10, 0, 10), P(5, 0, 0))).Count, Is.EqualTo(1));
        }

        [Test] public void CollinearOverlapNeedsClearanceThroughoutOverlap()
        {
            var flat = Loop(false, P(0, 0, 0), P(10, 0, 0), P(12, 0, 5), P(8, 0, 0), P(2, 0, 0));
            Assert.That(Find(flat).Exists(x => x.Contains("/edges/0/3|")), Is.True);
            var raised = Loop(false, P(0, 0, 0), P(10, 0, 0), P(12, 8, 5), P(8, 8, 0), P(2, 8, 0));
            Assert.That(Find(raised).Exists(x => x.Contains("/edges/0/3|")), Is.False);
            var changingHeight = Loop(false, P(0, 0, 0), P(10, 0, 0), P(12, 8, 5), P(8, 8, 0), P(2, -8, 0));
            Assert.That(Find(changingHeight).Exists(x => x.Contains("/edges/0/3|")), Is.True);
        }

        [Test] public void RaisedCrossingUsesInterpolatedHeightAndExactClearanceBoundary()
        {
            var loop = Loop(false, P(-10, 0, -10), P(10, 0, 10), P(-10, 4, 10), P(10, 8, -10));
            Assert.That(Find(loop, 6), Is.Empty);
            Assert.That(Find(loop, 6.01).Count, Is.EqualTo(1));
        }

        [Test] public void NearParallelDisjointSegmentsDoNotCross()
        {
            Assert.That(Find(Loop(false, P(0, 0, 0), P(10, 0, 1e-8), P(12, 0, 1), P(10, 0, 2e-8), P(0, 0, 1e-8))), Is.Empty);
        }

        [Test] public void ReportingIsRepeatableAndCultureIndependent()
        {
            var loop = Loop(false, P(-10, 0, -10), P(10, 0, 10), P(-10, 0.5, 10), P(10, 1, -10));
            var expected = Find(loop);
            var original = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU"); Assert.That(Find(loop), Is.EqualTo(expected)); }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [Test] public void InvalidArgumentsAreRejected()
        {
            var loop = Loop(false, P(0, 0, 0), P(10, 0, 0));
            Assert.Throws<ArgumentNullException>(() => VenuePathIntersections.FindSelfIntersections(null, 5, (_, __, ___) => { }));
            Assert.Throws<ArgumentNullException>(() => VenuePathIntersections.FindSelfIntersections(loop, 5, null));
            foreach (double value in new[] { 0, -1, double.NaN, double.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => Find(loop, value));
        }

        private static List<string> Find(TrackVisualLoop loop, double clearance = 5)
        {
            var issues = new List<string>();
            VenuePathIntersections.FindSelfIntersections(loop, clearance, (code, path, detail) => issues.Add(code + "|" + path + "|" + detail));
            return issues;
        }
        private static double[] P(double x, double y, double z) => new[] { x, y, z };
        private static TrackVisualLoop Loop(bool closed, params double[][] points)
        {
            var track = closed ? TrackContractTests.Track()
                : TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_v1.json"));
            string id = closed ? "main_short" : "range_lane_1";
            var json = new StringBuilder("{\"schema_version\":1,\"id\":\"intersection_visual\",\"track_id\":\"")
                .Append(track.Id).Append("\",\"sample_spacing_m\":1,\"loops\":[{\"loop_id\":\"").Append(id).Append("\",\"knots\":[");
            for (int i = 0; i < points.Length; i++)
            {
                if (i != 0) json.Append(',');
                json.Append("{\"distance_m\":").Append((track.GetLoop(id).Length.Meters * i / (points.Length - 1)).ToString("R", CultureInfo.InvariantCulture));
                json.Append(",\"x\":").Append(points[i][0].ToString("R", CultureInfo.InvariantCulture));
                json.Append(",\"y\":").Append(points[i][1].ToString("R", CultureInfo.InvariantCulture));
                json.Append(",\"z\":").Append(points[i][2].ToString("R", CultureInfo.InvariantCulture)).Append('}');
            }
            return TrackVisualDefinition.FromJson(json.Append("]}]}").ToString(), track).Get(id);
        }
    }
}
