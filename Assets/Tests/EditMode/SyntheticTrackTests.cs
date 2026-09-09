using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SyntheticTrackTests
    {
        public static TrackDefinition Generated(string id) => TrackDefinition.FromJson(
            PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/" + id + ".json"));

        [TestCase("synthetic_flat_v1", 0, 0, 6, TrackSemanticZone.Flat)]
        [TestCase("synthetic_climb_v1", 0.1, 0, 6, TrackSemanticZone.ClimbLong)]
        [TestCase("synthetic_rolling_v1", 0.08, 0, 6, TrackSemanticZone.ClimbLong)]
        [TestCase("synthetic_technical_descent_v1", -0.1, 0.06, 3, TrackSemanticZone.DescentTechnical)]
        [TestCase("synthetic_range_approach_v1", 0, 0, 8, TrackSemanticZone.RangeApproach)]
        public void GeneratedProfilesCarryExpectedPhysicalMetadataAndSharedRange(string id, double grade, double curvature, double width, TrackSemanticZone zone)
        {
            var track = Generated(id);
            var loop = track.GetLoop("main_long");
            var sampler = new TrackSampler(loop);
            Assert.That(track.Id, Is.EqualTo(id));
            Assert.That(loop.Length.Meters, Is.EqualTo(1000));
            Assert.That(loop.SegmentCount, Is.EqualTo(5));
            var profile = sampler.Sample(TrackDistance.FromMeters(100));
            Assert.That(profile.Grade, Is.EqualTo(grade));
            Assert.That(profile.CurvaturePerMeter, Is.EqualTo(curvature));
            Assert.That(profile.WidthMeters, Is.EqualTo(width));
            Assert.That(profile.SemanticZone, Is.EqualTo(zone));
            var tail = sampler.Sample(TrackDistance.FromMeters(800));
            Assert.That(tail.Grade, Is.Zero);
            Assert.That(tail.SemanticZone, Is.EqualTo(TrackSemanticZone.RangeApproach));
            Assert.That(track.Range.Entry.LoopId, Is.EqualTo(loop.Id));
            Assert.That(track.Range.Entry.Distance.Meters, Is.EqualTo(820));
            Assert.That(track.Range.FiringLine.Distance.Meters, Is.EqualTo(850));
            Assert.That(track.Penalty.Rejoin.Distance.Meters, Is.EqualTo(920));
            Assert.That(track.GetCheckpoint(2).Anchor.Distance, Is.EqualTo(loop.Length));
        }
        [Test]
        public void RollingSignsAlternateAndMonotoneProfilesDoNotClaimClosure()
        {
            var loop = Generated("synthetic_rolling_v1").GetLoop("main_long");
            for (int i = 0; i < 4; i++)
                Assert.That(loop.GetSegment(i).StartGrade, Is.EqualTo(i % 2 == 0 ? 0.08 : -0.08));
            Assert.That(Generated("synthetic_climb_v1").GetLoop("main_long").Closed, Is.False);
            Assert.That(Generated("synthetic_technical_descent_v1").GetLoop("main_long").Closed, Is.False);
            Assert.That(Generated("synthetic_technical_descent_v1").GetLoop("main_long").GetSegment(0).OvertakeAllowed, Is.False);
            Assert.That(TrackContractTests.Track().GetLoop("main_long").Length.Meters, Is.EqualTo(1500), "Generator must preserve the source template.");
        }
    }
}
