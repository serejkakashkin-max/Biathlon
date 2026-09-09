using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalMotionProjectionTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private static PhysicalTrackMap Map()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1"); var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            return new PhysicalTrackMap(track, new RaceRoute(track, RaceRoutePlan.TimeTrial("projection", track.Id, "main_long", requirements), requirements));
        }
        private static RaceMotionSegment Segment(long rb, long ra, long tb, long ta, bool penalty) => (RaceMotionSegment)Activator.CreateInstance(
            typeof(RaceMotionSegment), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, new object[] { rb, ra, tb, ta, 0.0, 0.05, penalty }, null);
        [Test] public void SeamAndFullCircuitSlicesConserveNanometresWithoutEndpointSubtraction()
        {
            var map = Map(); var projection = new PhysicalMotionProjection(map, 0.05); var athlete = new AthleteId("a");
            var receipt = projection.Project(Segment(900 * M, 900 * M, 0, 150 * M, true), map.MapPenalty(149 * M));
            Assert.That(receipt.End, Is.EqualTo(receipt.Start));
            var a = projection.Slice(receipt, athlete, 0, 333333333, 0, 0);
            var b = projection.Slice(receipt, athlete, 333333333, 1000000000, 0, 0);
            Assert.That(a.TravelNanometers + b.TravelNanometers, Is.EqualTo(150 * M));
            Assert.That(b.Start.LocalNanometers, Is.LessThan(receipt.Start.LocalNanometers));
            var main = projection.Project(Segment(999 * M, 1000 * M, 0, M, false), map.MapRoute(999 * M));
            Assert.That(main.End.LocalNanometers, Is.Zero);
            projection.Slice(receipt, athlete, 0, 1000000000, 0, 0);
            var segment = Segment(999 * M, 1000 * M, 0, M, false); var start = map.MapRoute(999 * M);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { projection.Project(segment, start); projection.Slice(receipt, athlete, 0, 333333333, 0, 0); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void InvalidProjectionCannotHideBranchCrossingOrForeignCoordinates()
        {
            var map = Map(); var projection = new PhysicalMotionProjection(map, 0.05);
            Assert.Catch(() => projection.Project(Segment(999 * M, 1001 * M, 0, 2 * M, false), map.MapRoute(999 * M)));
            Assert.Catch(() => projection.Project(Segment(0, M, 0, M, false), map.MapRoute(1)));
            Assert.Catch(() => projection.Project(Segment(0, 0, 0, M, true), map.MapRoute(0)));
            Assert.Catch(() => projection.Project(Segment(0, 0, 0, 151 * M, true), map.MapPenalty(0)));
            Assert.Catch(() => projection.Project(Segment(0, M, 0, M, false), Map().MapRoute(0)));
            var receipt = projection.Project(Segment(0, M, 0, M, false), map.MapRoute(0));
            Assert.Catch(() => projection.Slice(default, new AthleteId("a"), 0, 1, 0, 0));
            Assert.Catch(() => projection.Slice(receipt, new AthleteId("a"), -1, 1, 0, 0));
            Assert.Catch(() => projection.Slice(receipt, new AthleteId("a"), 2, 1, 0, 0));
            Assert.Catch(() => new PhysicalMotionProjection(map, 0.05).Slice(receipt, new AthleteId("a"), 0, 1, 0, 0));
        }
    }
}
